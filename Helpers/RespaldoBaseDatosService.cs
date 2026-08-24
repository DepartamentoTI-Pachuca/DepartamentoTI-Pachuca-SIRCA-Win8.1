using PoderJudicial.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Genera y administra respaldos automáticos de la base de datos Access.
    ///
    /// La ruta de la BD se obtiene exclusivamente desde Conexion.RutaBD,
    /// por lo que el respaldo no mantiene una segunda configuración de la
    /// ubicación del archivo original.
    ///
    /// La coordinación entre equipos se realiza mediante un bloqueo de
    /// archivo en la carpeta compartida. El bloqueo se mantiene abierto
    /// durante toda la operación; SMB/Windows libera el bloqueo si el
    /// proceso termina inesperadamente.
    /// </summary>
    public static class RespaldoBaseDatosService
    {
        private const string PrefijoArchivo = "Respaldo_BaseDatos_";
        private const string Extension = ".accdb";
        private const string NombreBloqueo = ".PoderJudicial_Backup.lock";
        private const string PatronFecha = "yyyy-MM-dd_HH-mm-ss-fff";
        private const string NombreTemporal = ".tmp";

        private static readonly string CarpetaLog =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PoderJudicial");

        private static readonly string RutaLog =
            Path.Combine(CarpetaLog, "respaldos.log");

        /// <summary>
        /// Comprueba si corresponde realizar un respaldo y, si corresponde,
        /// intenta realizarlo. Cualquier error queda aislado del flujo
        /// principal de la aplicación.
        /// </summary>
        public static async Task VerificarYCrearRespaldoAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracionBD config = ConfiguracionBD.Cargar();

                if (config == null ||
                    !config.RespaldosAutomaticos ||
                    string.IsNullOrWhiteSpace(config.RutaArchivo))
                {
                    return;
                }

                string rutaBD = Conexion.RutaBD;

                if (string.IsNullOrWhiteSpace(rutaBD) ||
                    !File.Exists(rutaBD))
                {
                    Registrar(
                        "Respaldo omitido: la base de datos configurada no está disponible.");
                    return;
                }

                string carpetaRespaldos =
                    ObtenerCarpetaRespaldos(rutaBD, config);

                Directory.CreateDirectory(carpetaRespaldos);

                // El último respaldo válido se determina en la carpeta
                // compartida, no en un archivo local por equipo. Así varios
                // equipos no toman decisiones contradictorias.
                DateTime? ultimoRespaldo =
                    ObtenerFechaUltimoRespaldo(carpetaRespaldos);

                if (ultimoRespaldo.HasValue &&
                    DateTime.Now - ultimoRespaldo.Value <
                    TimeSpan.FromDays(config.FrecuenciaRespaldoDias))
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                using FileStream? bloqueo =
                    IntentarObtenerBloqueo(carpetaRespaldos);

                if (bloqueo == null)
                {
                    // Otro equipo ya está realizando el respaldo.
                    return;
                }

                // Volver a comprobar después de adquirir el bloqueo.
                // Otro equipo pudo haber terminado el respaldo mientras
                // este proceso esperaba/intentaba obtenerlo.
                ultimoRespaldo =
                    ObtenerFechaUltimoRespaldo(carpetaRespaldos);

                if (ultimoRespaldo.HasValue &&
                    DateTime.Now - ultimoRespaldo.Value <
                    TimeSpan.FromDays(config.FrecuenciaRespaldoDias))
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                string rutaRespaldo =
                    CrearRutaRespaldo(carpetaRespaldos);

                string rutaTemporal =
                    rutaRespaldo + NombreTemporal;

                try
                {
                    // Si quedó un temporal de una ejecución anterior de
                    // esta aplicación, solo se elimina porque tiene nuestro
                    // nombre controlado y nunca coincide con un .accdb final.
                    if (File.Exists(rutaTemporal))
                        File.Delete(rutaTemporal);

                    await CopiarYValidarAsync(
                        rutaBD,
                        rutaTemporal,
                        cancellationToken);

                    // El nombre definitivo solo aparece cuando la copia
                    // terminó y pudo abrirse correctamente con ACE.
                    File.Move(rutaTemporal, rutaRespaldo);

                    Registrar(
                        $"Respaldo creado correctamente: {rutaRespaldo}");

                    AplicarRetencion(
                        carpetaRespaldos,
                        config.MaximoRespaldos);
                }
                finally
                {
                    // Nunca dejar un .tmp como si fuera un respaldo válido.
                    if (File.Exists(rutaTemporal))
                    {
                        try
                        {
                            File.Delete(rutaTemporal);
                        }
                        catch
                        {
                            Registrar(
                                $"No se pudo eliminar el temporal: {rutaTemporal}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Registrar("Respaldo cancelado.");
            }
            catch (Exception ex)
            {
                // Un fallo de respaldo jamás debe cerrar el sistema.
                Registrar(
                    "Error durante el respaldo: " +
                    ex.GetType().Name + " - " + ex.Message);
            }
        }

        /// <summary>
        /// Obtiene la fecha del respaldo más reciente reconocido por este
        /// servicio para la base activa, sin crear ni modificar archivos.
        /// </summary>
        public static DateTime? ObtenerFechaUltimoRespaldoActual()
        {
            ConfiguracionBD? config = ConfiguracionBD.Cargar();
            string rutaBD = Conexion.RutaBD;

            if (config == null || string.IsNullOrWhiteSpace(rutaBD))
                return null;

            string carpetaRespaldos =
                ObtenerCarpetaRespaldos(rutaBD, config);

            return ObtenerFechaUltimoRespaldo(carpetaRespaldos);
        }

        private static string ObtenerCarpetaRespaldos(
            string rutaBD,
            ConfiguracionBD config)
        {
            if (!string.IsNullOrWhiteSpace(config.CarpetaRespaldos))
                return config.CarpetaRespaldos.Trim();

            string? carpetaBD =
                Path.GetDirectoryName(rutaBD);

            if (string.IsNullOrWhiteSpace(carpetaBD))
                throw new InvalidOperationException(
                    "No se pudo determinar la carpeta de la base de datos.");

            return Path.Combine(carpetaBD, "Respaldos");
        }

        private static FileStream? IntentarObtenerBloqueo(
            string carpetaRespaldos)
        {
            string rutaBloqueo =
                Path.Combine(carpetaRespaldos, NombreBloqueo);

            try
            {
                // OpenOrCreate + FileShare.None funciona también cuando el
                // archivo ya existe. Lo importante es que el HANDLE se
                // mantenga abierto durante toda la copia.
                FileStream stream = new FileStream(
                    rutaBloqueo,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);

                try
                {
                    stream.SetLength(0);

                    string texto =
                        $"PoderJudicial backup lock{Environment.NewLine}" +
                        $"Equipo: {Environment.MachineName}{Environment.NewLine}" +
                        $"Proceso: {System.Diagnostics.Process.GetCurrentProcess().Id}{Environment.NewLine}" +
                        $"Inicio: {DateTime.Now:O}";

                    byte[] datos =
                        Encoding.UTF8.GetBytes(texto);

                    stream.Write(datos, 0, datos.Length);
                    stream.Flush(true);

                    return stream;
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }
            catch (IOException)
            {
                // Otro proceso/equipo posee el HANDLE exclusivo.
                return null;
            }
        }

        private static async Task CopiarYValidarAsync(
            string origen,
            string destinoTemporal,
            CancellationToken cancellationToken)
        {
            FileInfo originalAntes = new FileInfo(origen);

            if (!originalAntes.Exists || originalAntes.Length <= 0)
                throw new IOException(
                    "La base de datos original no está disponible para respaldo.");

            DateTime ultimaEscrituraAntes =
                originalAntes.LastWriteTimeUtc;

            // FileStream permite controlar explícitamente los accesos y
            // cerrar ambos handles antes de validar/mover el archivo.
            using (FileStream entrada = new FileStream(
                origen,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true))
            using (FileStream salida = new FileStream(
                destinoTemporal,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 1024,
                useAsync: true))
            {
                await entrada.CopyToAsync(
                    salida,
                    1024 * 1024,
                    cancellationToken);

                await salida.FlushAsync(cancellationToken);
            }

            FileInfo copia = new FileInfo(destinoTemporal);

            if (!copia.Exists || copia.Length <= 0)
                throw new IOException(
                    "El respaldo temporal no se creó correctamente.");

            // Una copia de un archivo Access que cambia mientras se lee no
            // se considera válida. No elimina/modifica el original: se
            // descarta únicamente el temporal.
            FileInfo originalDespues = new FileInfo(origen);

            if (copia.Length != originalAntes.Length ||
                originalDespues.Length != originalAntes.Length ||
                originalDespues.LastWriteTimeUtc != ultimaEscrituraAntes)
            {
                throw new IOException(
                    "La base de datos cambió durante la copia. El respaldo fue descartado.");
            }

            // Validación adicional: ACE debe poder abrir la copia.
            string error = Conexion.ProbarConexion(destinoTemporal);

            if (error != null)
                throw new IOException(
                    "La copia terminó, pero Access no pudo abrirla: " + error);
        }

        private static string CrearRutaRespaldo(
            string carpetaRespaldos)
        {
            string marcaTiempo =
                DateTime.Now.ToString(
                    PatronFecha,
                    CultureInfo.InvariantCulture);

            string ruta =
                Path.Combine(
                    carpetaRespaldos,
                    $"{PrefijoArchivo}{marcaTiempo}{Extension}");

            // El bloqueo compartido evita ejecuciones simultáneas. Esta
            // defensa adicional evita reutilizar un nombre ya existente.
            while (File.Exists(ruta))
            {
                Thread.Sleep(5);

                marcaTiempo =
                    DateTime.Now.ToString(
                        PatronFecha,
                        CultureInfo.InvariantCulture);

                ruta = Path.Combine(
                    carpetaRespaldos,
                    $"{PrefijoArchivo}{marcaTiempo}{Extension}");
            }

            return ruta;
        }

        private static DateTime? ObtenerFechaUltimoRespaldo(
            string carpetaRespaldos)
        {
            if (!Directory.Exists(carpetaRespaldos))
                return null;

            DateTime? ultimo = null;

            foreach (string archivo in
                     Directory.EnumerateFiles(
                         carpetaRespaldos,
                         $"{PrefijoArchivo}*{Extension}",
                         SearchOption.TopDirectoryOnly))
            {
                if (!TryObtenerFechaDesdeNombre(
                        Path.GetFileName(archivo),
                        out DateTime fecha))
                {
                    // No es un archivo que la aplicación pueda identificar
                    // inequívocamente: no se usa para decidir ni se elimina.
                    continue;
                }

                if (!ultimo.HasValue || fecha > ultimo.Value)
                    ultimo = fecha;
            }

            return ultimo;
        }

        private static List<(string Ruta, DateTime Fecha)> ObtenerRespaldosPropios(
            string carpetaRespaldos)
        {
            var respaldos = new List<(string Ruta, DateTime Fecha)>();

            if (!Directory.Exists(carpetaRespaldos))
                return respaldos;

            foreach (string archivo in
                     Directory.EnumerateFiles(
                         carpetaRespaldos,
                         $"{PrefijoArchivo}*{Extension}",
                         SearchOption.TopDirectoryOnly))
            {
                if (TryObtenerFechaDesdeNombre(
                        Path.GetFileName(archivo),
                        out DateTime fecha))
                {
                    respaldos.Add((archivo, fecha));
                }
            }

            return respaldos
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        private static bool TryObtenerFechaDesdeNombre(
            string nombre,
            out DateTime fecha)
        {
            fecha = default;

            if (!nombre.StartsWith(
                    PrefijoArchivo,
                    StringComparison.OrdinalIgnoreCase) ||
                !nombre.EndsWith(
                    Extension,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string parteFecha =
                nombre.Substring(
                    PrefijoArchivo.Length,
                    nombre.Length -
                    PrefijoArchivo.Length -
                    Extension.Length);

            // Solo se consideran nombres generados por este servicio.
            return DateTime.TryParseExact(
                parteFecha,
                PatronFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha);
        }

        private static void AplicarRetencion(
            string carpetaRespaldos,
            int maximoRespaldos)
        {
            if (maximoRespaldos < 1)
                maximoRespaldos = 1;

            List<(string Ruta, DateTime Fecha)> respaldos =
                ObtenerRespaldosPropios(carpetaRespaldos);

            foreach (var respaldo in
                     respaldos.Skip(maximoRespaldos))
            {
                try
                {
                    File.Delete(respaldo.Ruta);

                    Registrar(
                        $"Respaldo antiguo eliminado por retención: {respaldo.Ruta}");
                }
                catch (Exception ex)
                {
                    // Un problema al borrar un respaldo antiguo no debe
                    // convertir un respaldo nuevo y válido en un fallo.
                    Registrar(
                        $"No se pudo eliminar respaldo antiguo {respaldo.Ruta}: {ex.Message}");
                }
            }
        }

        private static void Registrar(string mensaje)
        {
            try
            {
                Directory.CreateDirectory(CarpetaLog);

                File.AppendAllText(
                    RutaLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}{Environment.NewLine}");
            }
            catch
            {
                // El registro de errores nunca debe afectar a la aplicación.
            }
        }
    }
}
