using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using PoderJudicial.Helpers;

namespace PoderJudicial.Data
{
    public static class TableDetector
    {
        private static readonly object CacheLock = new object();
        private static List<string>? _todasLasTablas;
        private static string? _tablaActual;
        private static string? _rutaCache;
        private const string Prefijo = "Audiencias ";

        /// <summary>
        /// Tabla más reciente — usada para INSERT, UPDATE, DELETE.
        /// </summary>
        public static string TablaActual
        {
            get
            {
                RefrescarSiExpiro();
                return _tablaActual;
            }
        }

        /// <summary>
        /// Todas las tablas de audiencias — usadas para autocompletado histórico.
        /// </summary>
        public static List<string> TodasLasTablas
        {
            get
            {
                RefrescarSiExpiro();
                return _todasLasTablas;
            }
        }

        /// <summary>
        /// Llama esto después de guardar un registro nuevo,
        /// por si se creó una tabla nueva en la BD.
        /// </summary>
        public static void InvalidarCache()
        {
            lock (CacheLock)
            {
                _todasLasTablas = null;
                _tablaActual = null;
                _rutaCache = null;
            }
        }

        /// <summary>
        /// Inicializa el detector con el mismo esquema que Dashboard acaba
        /// de leer. Solo acepta el resultado si todavía corresponde a la
        /// base activa; así una carga tardía de la BD anterior no puede
        /// contaminar la caché después de cambiar la configuración.
        /// </summary>
        public static void ActualizarCacheDesdeEsquema(
            IEnumerable<string> nombresTablas,
            string rutaBD)
        {
            if (nombresTablas == null ||
                !string.Equals(rutaBD, Conexion.RutaBD,
                    StringComparison.OrdinalIgnoreCase))
            {
                InvalidarCache();
                return;
            }

            List<string> tablasAudiencias = nombresTablas
                .Where(n => !string.IsNullOrWhiteSpace(n) &&
                    n.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => ExtraerAnoFinal(n))
                .ToList();

            // Conserva el comportamiento previo: si no hay tablas de
            // Audiencias, no publicar una caché aparentemente válida.
            if (tablasAudiencias.Count == 0)
            {
                InvalidarCache();
                return;
            }

            lock (CacheLock)
            {
                // Comprobar nuevamente dentro del lock por si la ruta
                // cambió mientras se procesaba la lista.
                if (!string.Equals(rutaBD, Conexion.RutaBD,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _todasLasTablas = null;
                    _tablaActual = null;
                    _rutaCache = null;
                    return;
                }

                _todasLasTablas = tablasAudiencias;
                _tablaActual = tablasAudiencias.LastOrDefault();
                _rutaCache = rutaBD;
            }
        }

        private static void RefrescarSiExpiro()
        {
            lock (CacheLock)
            {
                string rutaActual = Conexion.RutaBD;

                if (_todasLasTablas != null &&
                    string.Equals(_rutaCache, rutaActual,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                using (OleDbConnection conn = Conexion.ObtenerConexion())
                {
                    Stopwatch total = Stopwatch.StartNew();
                    Stopwatch apertura = Stopwatch.StartNew();
                    conn.Open();
                    apertura.Stop();

                    // OleDb expone el esquema sin queries frágiles
                    Stopwatch esquemaTiempo = Stopwatch.StartNew();
                    DataTable esquema = conn.GetSchema("Tables");
                    esquemaTiempo.Stop();

                    Stopwatch mapeo = Stopwatch.StartNew();
                    _todasLasTablas = esquema.AsEnumerable()
                        .Select(r => r["TABLE_NAME"].ToString())
                        .Where(n => n.StartsWith(Prefijo,
                                    StringComparison.OrdinalIgnoreCase))
                        .OrderBy(n => ExtraerAnoFinal(n))
                        .ToList();
                    mapeo.Stop();
                    total.Stop();
                    RendimientoLog.Registrar("TableDetector.RefrescarSiExpiro", "",
                        "GetSchema(Tables)", openMs: apertura.ElapsedMilliseconds,
                        queryMs: esquemaTiempo.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: esquema.Rows.Count,
                        totalMs: total.ElapsedMilliseconds);

                    if (_todasLasTablas.Count == 0)
                        throw new InvalidOperationException(
                            "No se encontró ninguna tabla de Audiencias en la base de datos.");

                    // La más reciente = año final más alto = última de la lista
                    _tablaActual = _todasLasTablas.Last();
                }

                _rutaCache = rutaActual;
            }
        }

        private static int ExtraerAnoFinal(string nombreTabla)
        {
            // Extrae el segundo año de "Audiencias YYYY-YYYY"
            Match m = Regex.Match(nombreTabla, @"\d{4}-(\d{4})");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }


        public static string ObtenerTabla(string prefijo)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                DataTable esquema = conn.GetSchema("Tables");

                return esquema.AsEnumerable()
                    .Select(r => r["TABLE_NAME"].ToString())
                    .FirstOrDefault(t =>
                        !t.StartsWith("MSys") &&
                        t.StartsWith(prefijo,
                            StringComparison.OrdinalIgnoreCase));
            }
        }


    }
}
