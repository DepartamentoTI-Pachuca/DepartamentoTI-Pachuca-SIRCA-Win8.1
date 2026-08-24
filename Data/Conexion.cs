using PoderJudicial.Helpers;
using System;
using System.Data.OleDb;
using System.IO;

namespace PoderJudicial.Data
{
    /// <summary>
    /// Se lanza cuando todavía no hay una base de datos configurada, o la
    /// ruta configurada ya no existe/no es accesible. Los llamadores (Login,
    /// Dashboard) la detectan para mostrar la ventana de configuración en
    /// vez de dejar que la excepción original de OleDb suba sin control.
    /// </summary>
    public class BaseDatosNoConfiguradaException : Exception
    {
        public BaseDatosNoConfiguradaException(string mensaje) : base(mensaje) { }
        public BaseDatosNoConfiguradaException(string mensaje, Exception inner) : base(mensaje, inner) { }
    }

    public static class Conexion
    {
        /// <summary>
        /// Ruta actualmente configurada (o cadena vacía si no hay ninguna).
        /// Se mantiene en memoria para no releer el JSON en cada conexión;
        /// InvalidarConfiguracion() fuerza a releerlo la próxima vez.
        /// </summary>
        private static ConfiguracionBD _configCacheada;

        private static ConfiguracionBD ObtenerConfiguracion()
        {
            _configCacheada ??= ConfiguracionBD.Cargar();
            return _configCacheada;
        }

        /// <summary>
        /// Llamar después de guardar una nueva configuración (ej. desde la
        /// ventana de Configuración de Base de Datos) para que la próxima
        /// conexión use la ruta nueva sin tener que reiniciar la app.
        /// </summary>
        public static void InvalidarConfiguracion()
        {
            _configCacheada = null;
        }

        public static bool EstaConfigurada => ObtenerConfiguracion() != null;

        /// <summary>Ruta actualmente configurada, o cadena vacía si no hay ninguna.</summary>
        public static string RutaBD => ObtenerConfiguracion()?.RutaArchivo ?? "";

        /// <summary>
        /// Construye (sin abrir) una conexión a la base de datos configurada.
        /// Lanza BaseDatosNoConfiguradaException — controlada — si todavía
        /// no hay ninguna ruta guardada, o si el archivo configurado ya no
        /// existe. No valida más allá de eso (permisos, red caída, etc. se
        /// detectan recién al hacer .Open(), como ya hacía cada llamador).
        /// </summary>
        public static OleDbConnection ObtenerConexion()
        {
            ConfiguracionBD config = ObtenerConfiguracion();

            if (config == null || string.IsNullOrWhiteSpace(config.RutaArchivo))
            {
                throw new BaseDatosNoConfiguradaException(
                    "No hay una base de datos configurada todavía.");
            }

            if (!File.Exists(config.RutaArchivo))
            {
                throw new BaseDatosNoConfiguradaException(
                    $"La base de datos configurada no se encuentra:\n{config.RutaArchivo}");
            }

            string connectionString =
                $@"Provider=Microsoft.ACE.OLEDB.12.0;
                   Data Source={config.RutaArchivo};";

            return new OleDbConnection(connectionString);
        }

        /// <summary>
        /// Prueba una ruta candidata (aún no guardada) abriendo y cerrando
        /// una conexión real — usado por la ventana de Configuración antes
        /// de permitir Guardar. Devuelve el mensaje de error, o null si la
        /// conexión fue exitosa.
        /// </summary>
        public static string ProbarConexion(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                return "Selecciona un archivo de base de datos.";

            if (!File.Exists(rutaArchivo))
                return "El archivo seleccionado no existe o no es accesible.";

            string connectionString =
                $@"Provider=Microsoft.ACE.OLEDB.12.0;
                   Data Source={rutaArchivo};";

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                }
                return null;
            }
            catch (Exception ex)
            {
                return $"No se pudo conectar: {ex.Message}";
            }
        }
    }
}