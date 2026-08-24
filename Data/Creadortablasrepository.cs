using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using PoderJudicial.Helpers;

namespace PoderJudicial.Data
{
    /// <summary>
    /// Se lanza cuando la tabla elegida como plantilla no tiene la
    /// estructura mínima que <see cref="AudienciaData"/> necesita para
    /// insertar/consultar registros (ver ColumnasRequeridas). Evita crear
    /// una tabla que "se ve igual" pero luego truena al usarla.
    /// </summary>
    public class PlantillaIncompatibleException : Exception
    {
        public PlantillaIncompatibleException(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Permite crear una nueva tabla de Audiencias en la base de datos
    /// configurada, copiando únicamente la ESTRUCTURA (sin registros) de
    /// una tabla existente del mismo tipo ("Audiencias YYYY-YYYY").
    ///
    /// Solo se ofrecen como plantilla las tablas que ya detecta
    /// <see cref="TableDetector"/> (prefijo "Audiencias "). CopiasAudiencias
    /// y Ejecucion son tablas únicas referenciadas por nombre fijo en el
    /// resto del código — no tiene sentido "clonarlas", la aplicación nunca
    /// buscaría la copia.
    /// </summary>
    public class CreadorTablasRepository
    {
        /// <summary>
        /// Columnas que AudienciaData.Insertar()/ObtenerAudiencias() usan
        /// por nombre literal. Cualquier tabla usada como plantilla debe
        /// tenerlas todas, o la tabla nueva quedaría incompatible con los
        /// repositorios existentes aunque visualmente "tenga los mismos
        /// campos".
        /// </summary>
        private static readonly string[] ColumnasRequeridas =
        {
            "Id", "FeAudiencia", "FeRecibo", "TotDiscos", "TipoDisco",
            "Juzgado", "TotDiscoAudiencia", "Juez", "NoCausa", "NUC",
            "TipoCausa", "TipoAudiencia", "Hora conclusion", "Imputado",
            "Delito", "Agraviado", "Sala", "NoCausaJuicio", "Diferida",
            "Quien Realiza"
        };

        // Igual que Access: nombre de tabla sin ".", "!", "[", "]", "'" ni
        // comillas dobles, y sin empezar/terminar con espacio.
        private static readonly Regex CaracteresInvalidos = new Regex(@"[.!\[\]'""]");

        /// <summary>
        /// Tablas que pueden usarse como plantilla. Reutiliza
        /// TableDetector.TodasLasTablas; si la base de datos configurada
        /// todavía no tiene ninguna tabla de Audiencias, se devuelve una
        /// lista vacía en vez de dejar que la excepción de TableDetector
        /// suba sin control (aquí sí es un caso válido: "aún no hay nada
        /// que ofrecer como plantilla", no un error).
        /// </summary>
        public List<string> ObtenerPlantillasDisponibles()
        {
            try
            {
                // Copia defensiva: TodasLasTablas devuelve la lista cacheada
                // de TableDetector: no se debe reordenar/mutar esa instancia.
                return new List<string>(TableDetector.TodasLasTablas);
            }
            catch (InvalidOperationException)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Calcula el siguiente periodo a partir de la tabla más reciente
        /// (ej. "Audiencias 2026-2028" → "Audiencias 2028-2030"), usando el
        /// mismo patrón e intervalo que ya maneja el proyecto. Devuelve
        /// cadena vacía si no hay una tabla anterior o si su nombre no
        /// sigue el patrón "Audiencias YYYY-YYYY" — en ambos casos el
        /// usuario debe escribir el nombre manualmente, no se inventa un
        /// periodo a partir de un nombre que no es un rango de años.
        /// </summary>
        public string SugerirSiguienteNombre()
        {
            List<string> plantillas = ObtenerPlantillasDisponibles();
            if (plantillas.Count == 0)
                return "";

            // La más reciente por año final, igual que TableDetector.
            string masReciente = plantillas
                .OrderBy(ExtraerAnoFinal)
                .Last();

            Match m = Regex.Match(masReciente, @"(\d{4})-(\d{4})");
            if (!m.Success)
                return "";

            int inicio = int.Parse(m.Groups[1].Value);
            int fin = int.Parse(m.Groups[2].Value);
            int intervalo = fin - inicio;

            if (intervalo <= 0)
                return "";

            return $"Audiencias {fin}-{fin + intervalo}";
        }

        private static int ExtraerAnoFinal(string nombreTabla)
        {
            Match m = Regex.Match(nombreTabla, @"\d{4}-(\d{4})");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        /// <summary>
        /// Valida el nombre propuesto para la tabla nueva. Devuelve null si
        /// es válido, o el mensaje de error a mostrar si no lo es.
        /// </summary>
        public string ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "El nombre de la tabla no puede estar vacío.";

            nombre = nombre.Trim();

            if (nombre.Length > 64)
                return "El nombre no puede tener más de 64 caracteres.";

            if (CaracteresInvalidos.IsMatch(nombre))
                return "El nombre no puede contener los caracteres . ! [ ] ' \" ";

            if (ExisteTabla(nombre))
                return $"Ya existe una tabla llamada \"{nombre}\".";

            return null;
        }

        /// <summary>
        /// True si ya existe una tabla con ese nombre (comparación sin
        /// distinguir mayúsculas/minúsculas, igual que Access). Revisa el
        /// esquema completo de la base de datos, no solo las tablas de
        /// Audiencias, para no permitir colisiones con CopiasAudiencias,
        /// Ejecucion, etc.
        /// </summary>
        public bool ExisteTabla(string nombre)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();
                return TablaExiste(conn, nombre);
            }
        }

        private static bool TablaExiste(OleDbConnection conn, string nombre)
        {
            DataTable schema = conn.GetSchema("Tables");

            return schema.AsEnumerable().Any(r =>
                string.Equals(
                    r["TABLE_NAME"].ToString(),
                    nombre,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Crea "nombreNuevo" copiando únicamente la estructura de
        /// "plantilla" (sin registros). Usa "SELECT * INTO ... WHERE 1=0",
        /// que es la forma nativa en Jet/ACE de clonar la estructura exacta
        /// de una tabla (tipos, tamaños de texto, etc.) sin tener que
        /// traducir manualmente cada tipo de columna a SQL DDL — evita
        /// tener que mantener una tabla de conversión de tipos OleDb → SQL
        /// que sería una fuente de errores sutiles si falta un caso.
        /// No preserva clave primaria/autonumérico/índices de la plantilla,
        /// pero AudienciaData no depende de ninguno de ellos (el Id se
        /// calcula con MAX(Id)+1 desde código), así que esto no afecta el
        /// funcionamiento de la tabla nueva.
        /// </summary>
        public void CrearTablaDesdeTemplate(string plantilla, string nombreNuevo)
        {
            if (string.IsNullOrWhiteSpace(plantilla))
                throw new ArgumentException("Selecciona una tabla plantilla.");

            string errorNombre = ValidarNombre(nombreNuevo);
            if (errorNombre != null)
                throw new ArgumentException(errorNombre);

            nombreNuevo = nombreNuevo.Trim();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                // Revalidar aquí (no solo en el ViewModel): la plantilla
                // pudo dejar de existir, o el nombre pudo colisionar, entre
                // que se abrió el diálogo y que el usuario dio clic en
                // "Crear" (por ejemplo, si alguien más cambió la BD).
                if (!TablaExiste(conn, plantilla))
                    throw new InvalidOperationException(
                        $"La tabla plantilla \"{plantilla}\" ya no existe en la base de datos.");

                if (TablaExiste(conn, nombreNuevo))
                    throw new InvalidOperationException(
                        $"Ya existe una tabla llamada \"{nombreNuevo}\".");

                string columnaFaltante = ColumnaRequeridaFaltante(conn, plantilla);
                if (columnaFaltante != null)
                    throw new PlantillaIncompatibleException(
                        $"\"{plantilla}\" no puede usarse como plantilla: le falta el campo " +
                        $"\"{columnaFaltante}\", que la aplicación necesita para funcionar con " +
                        "esta tabla.");

                string sql =
                    $"SELECT * INTO [{nombreNuevo}] FROM [{plantilla}] WHERE 1=0";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (OleDbException ex)
                    {
                        throw new InvalidOperationException(
                            "Access rechazó la creación de la tabla:\n" + ex.Message, ex);
                    }
                }

                // Verificación posterior: confirmar que la tabla resultante
                // tiene el mismo número de columnas que la plantilla. Si
                // Access la creó incompleta por cualquier motivo, se limpia
                // en vez de dejar una tabla a medias en la base de datos.
                int columnasPlantilla = ContarColumnas(conn, plantilla);
                int columnasNueva = ContarColumnas(conn, nombreNuevo);

                if (columnasNueva != columnasPlantilla || columnasNueva == 0)
                {
                    try
                    {
                        using (OleDbCommand drop =
                            new OleDbCommand($"DROP TABLE [{nombreNuevo}]", conn))
                        {
                            drop.ExecuteNonQuery();
                        }
                    }
                    catch (OleDbException)
                    {
                        // Si ni siquiera se pudo limpiar, se deja que el
                        // mensaje de abajo avise al usuario para que revise
                        // la base de datos manualmente.
                    }

                    throw new InvalidOperationException(
                        "La tabla se creó de forma incompleta y fue eliminada. " +
                        "Intenta de nuevo o revisa la base de datos.");
                }
            }

            // La tabla nueva ya existe en Access: refrescar el caché de
            // TableDetector para que Sidebar, autocompletados y "Nuevo
            // registro" la reconozcan sin reiniciar la aplicación.
            TableDetector.InvalidarCache();
            CacheSesionAccess.InvalidarRutaActual();
        }

        private static string ColumnaRequeridaFaltante(OleDbConnection conn, string tabla)
        {
            DataTable columnas = conn.GetOleDbSchemaTable(
                OleDbSchemaGuid.Columns,
                new object[] { null, null, tabla, null });

            HashSet<string> presentes = new HashSet<string>(
                columnas.AsEnumerable().Select(r => r["COLUMN_NAME"].ToString()),
                StringComparer.OrdinalIgnoreCase);

            return ColumnasRequeridas.FirstOrDefault(c => !presentes.Contains(c));
        }

        private static int ContarColumnas(OleDbConnection conn, string tabla)
        {
            DataTable columnas = conn.GetOleDbSchemaTable(
                OleDbSchemaGuid.Columns,
                new object[] { null, null, tabla, null });

            return columnas.Rows.Count;
        }
    }
}
