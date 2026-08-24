using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using PoderJudicial.Helpers;
using System.Diagnostics;

namespace PoderJudicial.Data
{
    public class DashboardData
    {
        private List<string>? _tablasEsquema;
        private string? _rutaEsquema;

        public (int TotalMes, int TotalHoy) ObtenerResumenAudiencias()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            int totalMes = 0;
            int totalHoy = 0;

            DateTime inicioMes =
                new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1);

            DateTime inicioSiguienteMes =
                inicioMes.AddMonths(1);

            DateTime inicioDia = DateTime.Today;
            DateTime inicioSiguienteDia = inicioDia.AddDays(1);

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();
                RendimientoLog.Registrar("DashboardData.ObtenerResumenAudiencias", "",
                    "OleDbConnection.Open", openMs: apertura.ElapsedMilliseconds);

                foreach (string nombreTabla in ObtenerTablasAudiencias(conn))
                {
                    try
                    {
                        Stopwatch totalTabla = Stopwatch.StartNew();
                        string query = $@"
                SELECT
                    SUM(IIF(FeAudiencia >= ? AND FeAudiencia < ?, 1, 0)) AS TotalMes,
                    SUM(IIF(FeAudiencia >= ? AND FeAudiencia < ?, 1, 0)) AS TotalHoy
                FROM [{nombreTabla}]
                ";

                        using (OleDbCommand cmd =
                            new OleDbCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("?", inicioMes);
                            cmd.Parameters.AddWithValue("?", inicioSiguienteMes);
                            cmd.Parameters.AddWithValue("?", inicioDia);
                            cmd.Parameters.AddWithValue("?", inicioSiguienteDia);

                            Stopwatch consulta = Stopwatch.StartNew();
                            using (OleDbDataReader dr = cmd.ExecuteReader())
                            {
                                consulta.Stop();
                                Stopwatch mapeo = Stopwatch.StartNew();
                                int filas = 0;
                                if (dr.Read())
                                {
                                    filas = 1;
                                    if (dr["TotalMes"] != DBNull.Value)
                                        totalMes += Convert.ToInt32(dr["TotalMes"]);

                                    if (dr["TotalHoy"] != DBNull.Value)
                                        totalHoy += Convert.ToInt32(dr["TotalHoy"]);
                                }
                                mapeo.Stop();
                                totalTabla.Stop();
                                RendimientoLog.Registrar(
                                    "DashboardData.ObtenerResumenAudiencias", nombreTabla,
                                    "ExecuteReader/Agregados", queryMs: consulta.ElapsedMilliseconds,
                                    mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                                    totalMs: totalTabla.ElapsedMilliseconds);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            ex.Message,
                            nombreTabla);
                    }
                }
            }

            totalMetodo.Stop();
            RendimientoLog.Registrar("DashboardData.ObtenerResumenAudiencias", "",
                "Método", totalMs: totalMetodo.ElapsedMilliseconds);
            return (totalMes, totalHoy);
        }



        public int ObtenerTotalEjecucionesMes()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            int total = 0;

            DateTime inicioMes =
                new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1);

            DateTime inicioSiguienteMes =
                inicioMes.AddMonths(1);

            using (OleDbConnection conn =
                Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query = @"
            SELECT COUNT(*)
            FROM Ejecucion
            WHERE FechaAudiencia >= ?
            AND FechaAudiencia < ?";

                using (OleDbCommand cmd =
                    new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "?",
                        inicioMes);

                    cmd.Parameters.AddWithValue(
                        "?",
                        inicioSiguienteMes);

                    Stopwatch consulta = Stopwatch.StartNew();
                    object resultado = cmd.ExecuteScalar();
                    consulta.Stop();

                    if (resultado != null &&
                        resultado != DBNull.Value)
                    {
                        total = Convert.ToInt32(resultado);
                    }

                    totalMetodo.Stop();
                    RendimientoLog.Registrar("DashboardData.ObtenerTotalEjecucionesMes",
                        "Ejecucion", "ExecuteScalar", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: 1,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return total;
        }



        public int ObtenerTotalCopiasMes()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            int total = 0;

            DateTime inicioMes =
                new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1);

            DateTime inicioSiguienteMes =
                inicioMes.AddMonths(1);

            using (OleDbConnection conn =
                Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query = @"
            SELECT SUM(Val(TotDiscosEntregados))
            FROM CopiasAudiencias
            WHERE FeRecibo >= ?
            AND FeRecibo < ?";

                using (OleDbCommand cmd =
                    new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "?",
                        inicioMes);

                    cmd.Parameters.AddWithValue(
                        "?",
                        inicioSiguienteMes);

                    Stopwatch consulta = Stopwatch.StartNew();
                    object resultado = cmd.ExecuteScalar();
                    consulta.Stop();

                    if (resultado != null &&
                        resultado != DBNull.Value)
                    {
                        total = Convert.ToInt32(resultado);
                    }

                    totalMetodo.Stop();
                    RendimientoLog.Registrar("DashboardData.ObtenerTotalCopiasMes",
                        "CopiasAudiencias", "ExecuteScalar", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: 1,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return total;
        }


        public string ObtenerVersionSistema()
        {
            Version version =
                Assembly.GetExecutingAssembly().GetName().Version;

            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        public string ObtenerEstadoSistema()
        {
            try
            {
                using (var cn = Conexion.ObtenerConexion())
                {
                    cn.Open();
                }

                return "Operativo";
            }
            catch
            {
                return "Sin conexión";
            }
        }


        public string ObtenerNombreBaseDatos()
        {
            return Path.GetFileName(Conexion.RutaBD);
        }


        public List<ActividadReciente> ObtenerActividadesRecientes()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<ActividadReciente> actividades = new List<ActividadReciente>();

            actividades.AddRange(ObtenerActividadesAudiencias());

            actividades.AddRange(ObtenerActividadesCopias());

            actividades.AddRange(ObtenerActividadesEjecuciones());

            List<ActividadReciente> resultado = actividades
                .OrderByDescending(x => x.FechaHora)
                .Take(8)
                .ToList();

            totalMetodo.Stop();
            RendimientoLog.Registrar("DashboardData.ObtenerActividadesRecientes", "",
                "Método", rows: resultado.Count, totalMs: totalMetodo.ElapsedMilliseconds);
            return resultado;
        }

        private List<ActividadReciente> ObtenerActividadesAudiencias()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<ActividadReciente> lista = new List<ActividadReciente>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();
                RendimientoLog.Registrar("DashboardData.ObtenerActividadesAudiencias", "",
                    "OleDbConnection.Open", openMs: apertura.ElapsedMilliseconds);

                foreach (string nombreTabla in ObtenerTablasAudiencias(conn))
                {
                    try
                    {
                        Stopwatch totalTabla = Stopwatch.StartNew();
                        string query = $@"
                    SELECT TOP 10 *
                    FROM [{nombreTabla}]
                    WHERE FeRecibo IS NOT NULL
                    ORDER BY FeRecibo DESC";

                        using (OleDbCommand cmd = new OleDbCommand(query, conn))
                        {
                            Stopwatch consulta = Stopwatch.StartNew();
                            using OleDbDataReader dr = cmd.ExecuteReader();
                            consulta.Stop();
                            Stopwatch mapeo = Stopwatch.StartNew();
                            int filas = 0;
                            while (dr.Read())
                            {
                                filas++;
                                lista.Add(new ActividadReciente
                                {
                                    FechaHora = Convert.ToDateTime(dr["FeRecibo"]),

                                    Icono = "⚖",

                                    TipoActividad = "Registro de audiencia",

                                    Descripcion =
        $"NUC: {dr["NUC"]} | Causa: {dr["NoCausa"]}",

                                    Usuario = dr["Quien Realiza"].ToString(),

                                    IdRegistro = Convert.ToInt32(dr["Id"]),

                                    TablaDestino = nombreTabla,

                                    Sala = TieneColumna(dr, "Sala")
                                        ? dr["Sala"]?.ToString() ?? ""
                                        : "",

                                    TotalDiscos = BuscadorRegistros
                                        .ExtraerNumero(TieneColumna(dr, "TotDiscoAudiencia")
                                            ? dr["TotDiscoAudiencia"]?.ToString()
                                            : "")
                                        .ToString(),
                                });
                            }
                            mapeo.Stop();
                            totalTabla.Stop();
                            RendimientoLog.Registrar(
                                "DashboardData.ObtenerActividadesAudiencias", nombreTabla,
                                "ExecuteReader/Mapping", queryMs: consulta.ElapsedMilliseconds,
                                mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                                totalMs: totalTabla.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            ex.Message,
                            nombreTabla);
                    }
                }
            }

            totalMetodo.Stop();
            RendimientoLog.Registrar("DashboardData.ObtenerActividadesAudiencias", "",
                "Recorrido histórico", totalMs: totalMetodo.ElapsedMilliseconds);
            return lista;
        }

        private List<ActividadReciente> ObtenerActividadesCopias()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<ActividadReciente> lista = new List<ActividadReciente>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();


                const string nombreTabla = "CopiasAudiencias";

                string query = $@"
SELECT TOP 10 Id, FeRecibo, NUC, NoCausa, [Quien Realiza], TotDiscosEntregados
FROM [{nombreTabla}]
WHERE FeRecibo IS NOT NULL
ORDER BY FeRecibo DESC";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    using OleDbDataReader dr = cmd.ExecuteReader();
                    consulta.Stop();
                    Stopwatch mapeo = Stopwatch.StartNew();
                    int filas = 0;
                    while (dr.Read())
                    {
                        filas++;
                        string nuc = dr["NUC"]?.ToString() ?? "";
                        string causa = dr["NoCausa"]?.ToString() ?? "";

                        string descripcion = "";

                        if (!string.IsNullOrWhiteSpace(nuc))
                            descripcion = $"NUC: {nuc}";

                        if (!string.IsNullOrWhiteSpace(causa))
                        {
                            if (descripcion != "")
                                descripcion += " | ";

                            descripcion += $"Causa: {causa}";
                        }

                        lista.Add(new ActividadReciente
                        {
                            FechaHora = Convert.ToDateTime(dr["FeRecibo"]),
                            Icono = "💿",
                            TipoActividad = "Entrega de copias",
                            Descripcion = descripcion,
                            Usuario = dr["Quien Realiza"].ToString(),
                            IdRegistro = Convert.ToInt32(dr["Id"]),

                            TablaDestino = nombreTabla,

                            // Registro de Copias no tiene columna Sala —
                            // queda vacía, tal como lo pidió el usuario para
                            // cuando el campo no exista en ese tipo de registro.
                            Sala = "",

                            TotalDiscos = BuscadorRegistros
                                .ExtraerNumero(dr["TotDiscosEntregados"]?.ToString())
                                .ToString(),
                        });
                    }
                    mapeo.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("DashboardData.ObtenerActividadesCopias",
                        nombreTabla, "ExecuteReader/Mapping",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return lista;
        }

        private List<ActividadReciente> ObtenerActividadesEjecuciones()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<ActividadReciente> lista = new List<ActividadReciente>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                const string nombreTabla = "Ejecucion";

                string query = $@"
SELECT TOP 10 Id, FechaAudiencia, Expediente, Causa, Observaciones,
              Sala, TotalDiscos
FROM [{nombreTabla}]
WHERE FechaAudiencia IS NOT NULL
ORDER BY FechaAudiencia DESC";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    using OleDbDataReader dr = cmd.ExecuteReader();
                    consulta.Stop();
                    Stopwatch mapeo = Stopwatch.StartNew();
                    int filas = 0;
                    while (dr.Read())
                    {
                        filas++;
                        string expediente = dr["Expediente"]?.ToString() ?? "";
                        string causa = dr["Causa"]?.ToString() ?? "";

                        string descripcion = "";

                        if (!string.IsNullOrWhiteSpace(expediente))
                            descripcion = $"Expediente: {expediente}";

                        if (!string.IsNullOrWhiteSpace(causa))
                        {
                            if (descripcion != "")
                                descripcion += " | ";

                            descripcion += $"Causa: {causa}";
                        }

                        lista.Add(new ActividadReciente
                        {
                            FechaHora = Convert.ToDateTime(dr["FechaAudiencia"]),
                            Icono = "✔",
                            TipoActividad = "Registro de ejecución",
                            Descripcion = descripcion,
                            Usuario = dr["Observaciones"].ToString(),
                            IdRegistro = Convert.ToInt32(dr["Id"]),
                            TablaDestino = nombreTabla,

                            Sala = TieneColumna(dr, "Sala")
                                ? dr["Sala"]?.ToString() ?? ""
                                : "",

                            TotalDiscos = BuscadorRegistros
                                .ExtraerNumero(TieneColumna(dr, "TotalDiscos")
                                    ? dr["TotalDiscos"]?.ToString()
                                    : "")
                                .ToString(),
                        });
                    }
                    mapeo.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("DashboardData.ObtenerActividadesEjecuciones",
                        nombreTabla, "ExecuteReader/Mapping",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return lista;
        }


        /// <summary>
        /// Igual que AudienciaData.ExisteColumna: comprueba si la fila
        /// actual trae una columna con ese nombre, para leer campos que no
        /// existen en todas las tablas (ej. Sala en tablas de Audiencias
        /// archivadas muy antiguas, o en Registro de Copias) sin que la
        /// consulta truene.
        /// </summary>
        private static bool TieneColumna(OleDbDataReader dr, string nombre)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(nombre, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private List<string> ObtenerTablasAudiencias(OleDbConnection conn)
        {
            List<string> tablas = new List<string>();

            foreach (string nombreTabla in ObtenerNombresTablas(conn))
            {
                if (nombreTabla.StartsWith("MSys"))
                    continue;

                if (nombreTabla.StartsWith(
                    "Audiencias ",
                    StringComparison.OrdinalIgnoreCase))
                {
                    tablas.Add(nombreTabla);
                }
            }

            return tablas;
        }


        /// <summary>
        /// Conserva la lista general de tablas únicamente durante la vida
        /// de esta instancia de DashboardData (una carga de Home). Si la
        /// ruta activa cambia, descarta inmediatamente la lista anterior.
        /// La conexión sigue perteneciendo al método llamador y se cierra
        /// con su bloque using habitual.
        /// </summary>
        private IReadOnlyList<string> ObtenerNombresTablas(OleDbConnection conn)
        {
            string rutaActual = Conexion.RutaBD;

            if (_tablasEsquema == null ||
                !string.Equals(_rutaEsquema, rutaActual,
                    StringComparison.OrdinalIgnoreCase))
            {
                Stopwatch esquemaTiempo = Stopwatch.StartNew();
                DataTable schema = conn.GetSchema("Tables");
                esquemaTiempo.Stop();

                Stopwatch mapeo = Stopwatch.StartNew();
                _tablasEsquema = schema.AsEnumerable()
                    .Select(row => row["TABLE_NAME"].ToString())
                    .ToList();
                mapeo.Stop();

                RendimientoLog.Registrar("DashboardData.ObtenerNombresTablas", "",
                    "GetSchema(Tables)", queryMs: esquemaTiempo.ElapsedMilliseconds,
                    mappingMs: mapeo.ElapsedMilliseconds, rows: schema.Rows.Count,
                    totalMs: esquemaTiempo.ElapsedMilliseconds + mapeo.ElapsedMilliseconds);

                _rutaEsquema = rutaActual;
            }

            return _tablasEsquema;
        }


    }
}
