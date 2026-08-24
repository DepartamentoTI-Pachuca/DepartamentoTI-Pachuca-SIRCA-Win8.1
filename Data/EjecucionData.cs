using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using PoderJudicial.Helpers;

namespace PoderJudicial.Data
{
    public class EjecucionData
    {
        public void Insertar(Ejecucion ejecucion)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query = @"
INSERT INTO Ejecucion
(
    Id,
    FechaAudiencia,
    TotalDiscos,
    Juez,
    Expediente,
    Causa,
    TipoAudiencia,
    HoraTermino,
    Imputado,
    Delito,
    Victima,
    Sala,
    Observaciones
)
VALUES
(
    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
)";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", ejecucion.Id);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.FechaAudiencia.HasValue
                            ? (object)ejecucion.FechaAudiencia.Value
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.TotalDiscos ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Juez ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.ExpedienteNumero ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Causa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.TipoAudiencia ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.HoraTermino ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Imputado ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Delito ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Victima ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Sala ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Observaciones ?? string.Empty);
                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("EjecucionData.Insertar", "Ejecucion",
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }
        }

        public void Actualizar(Ejecucion ejecucion)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query = @"
UPDATE Ejecucion SET
    FechaAudiencia = ?,
    TotalDiscos    = ?,
    Juez           = ?,
    Expediente     = ?,
    Causa          = ?,
    TipoAudiencia  = ?,
    HoraTermino    = ?,
    Imputado       = ?,
    Delito         = ?,
    Victima        = ?,
    Sala           = ?,
    Observaciones  = ?
WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.FechaAudiencia.HasValue
                            ? (object)ejecucion.FechaAudiencia.Value
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.TotalDiscos ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Juez ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.ExpedienteNumero ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Causa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.TipoAudiencia ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.HoraTermino ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Imputado ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Delito ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Victima ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Sala ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Observaciones ?? string.Empty);
cmd.Parameters.AddWithValue(
                        "?",
                        ejecucion.Id);

                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("EjecucionData.Actualizar", "Ejecucion",
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }
        }

        public int ObtenerSiguienteId()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query = "SELECT MAX(Id) FROM Ejecucion";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    object resultado = cmd.ExecuteScalar();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("EjecucionData.ObtenerSiguienteId",
                        "Ejecucion", "ExecuteScalar",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: 1,
                        totalMs: totalMetodo.ElapsedMilliseconds);

                    if (resultado == DBNull.Value || resultado == null)
                    {
                        return 1;
                    }

                    return Convert.ToInt32(resultado) + 1;
                }
            }
        }

        public Ejecucion ObtenerEjecucionPorId(int id)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                string query =
                    "SELECT * FROM Ejecucion WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", id);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Ejecucion
                            {
                                Id = Convert.ToInt32(reader["Id"]),

                                FechaAudiencia =
                                    DateTime.TryParse(
                                        reader["FechaAudiencia"]?.ToString(),
                                        out DateTime fecha)
                                            ? fecha
                                            : (DateTime?)null,

                                TotalDiscos =
                                    reader["TotalDiscos"]?.ToString(),

                                Juez =
                                    reader["Juez"]?.ToString(),

                                ExpedienteNumero =
                                    reader["Expediente"]?.ToString(),

                                Causa =
                                    reader["Causa"]?.ToString(),

                                TipoAudiencia =
                                    reader["TipoAudiencia"]?.ToString(),

                                HoraTermino =
                                    reader["HoraTermino"]?.ToString(),

                                Imputado =
                                    reader["Imputado"]?.ToString(),

                                Delito =
                                    reader["Delito"]?.ToString(),

                                Victima =
                                    reader["Victima"]?.ToString(),

                                Sala =
                                    reader["Sala"]?.ToString(),

                                Observaciones =
                                    reader["Observaciones"]?.ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Listado completo (con Id y TotalDiscos incluidos) usado por los
        /// indicadores "Total de registros" / "Total Discos Audiencia" en
        /// Consultar Registros. No confundir con ObtenerEjecuciones(),
        /// que solo trae Delito/TipoAudiencia para el autocompletado de
        /// Nuevo Registro y no debe tocarse.
        /// </summary>
        public List<Ejecucion> ObtenerTodas()
        {
            List<Ejecucion> lista = new();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                string query =
                    "SELECT Id, TotalDiscos FROM Ejecucion";

                using (OleDbCommand cmd =
                       new OleDbCommand(query, conn))
                using (OleDbDataReader reader =
                       cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Ejecucion
                        {
                            Id =
                                reader["Id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["Id"])
                                    : 0,

                            TotalDiscos =
                                reader["TotalDiscos"]?.ToString()
                        });
                    }
                }
            }

            return lista;
        }

        public List<Ejecucion> ObtenerEjecuciones()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<Ejecucion> lista = new();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string query =
                    "SELECT * FROM Ejecucion";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    using OleDbDataReader reader = cmd.ExecuteReader();
                    consulta.Stop();
                    Stopwatch mapeo = Stopwatch.StartNew();
                    while (reader.Read())
                    {
                        lista.Add(new Ejecucion
                        {
                            Delito =
                                reader["Delito"]?.ToString(),

                            TipoAudiencia =
                                reader["TipoAudiencia"]?.ToString()
                        });
                    }
                    mapeo.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("EjecucionData.ObtenerEjecuciones",
                        "Ejecucion", "ExecuteReader/Mapping",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: lista.Count,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return lista;
        }

        /// <summary>
        /// Carga los campos reales de Ejecucion requeridos únicamente por
        /// Reportes. No se mezcla con las tablas históricas de audiencias.
        /// </summary>
        public List<Ejecucion> ObtenerEjecucionesParaReportes()
        {
            List<Ejecucion> lista = new();

            using OleDbConnection conn = Conexion.ObtenerConexion();
            conn.Open();

            const string query = "SELECT * FROM Ejecucion";

            using OleDbCommand cmd = new OleDbCommand(query, conn);
            using OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Ejecucion
                {
                    Id = reader["Id"] != DBNull.Value
                        ? Convert.ToInt32(reader["Id"])
                        : 0,
                    FechaAudiencia = DateTime.TryParse(
                        reader["FechaAudiencia"]?.ToString(), out DateTime fecha)
                            ? fecha
                            : null,
                    TotalDiscos = reader["TotalDiscos"]?.ToString() ?? "",
                    Juez = reader["Juez"]?.ToString() ?? "",
                    ExpedienteNumero = reader["Expediente"]?.ToString() ?? "",
                    Causa = reader["Causa"]?.ToString() ?? "",
                    TipoAudiencia = reader["TipoAudiencia"]?.ToString() ?? "",
                    HoraTermino = reader["HoraTermino"]?.ToString() ?? "",
                    Imputado = reader["Imputado"]?.ToString() ?? "",
                    Delito = reader["Delito"]?.ToString() ?? "",
                    Victima = reader["Victima"]?.ToString() ?? "",
                    Sala = reader["Sala"]?.ToString() ?? "",
                    Observaciones = reader["Observaciones"]?.ToString() ?? ""
                });
            }

            return lista;
        }

        public List<Ejecucion> ObtenerEjecucionesParaReportes(int anio)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<Ejecucion> lista = new();
            DateTime desde = new DateTime(anio, 1, 1);
            DateTime hasta = desde.AddYears(1);

            using OleDbConnection conn = Conexion.ObtenerConexion();
            Stopwatch apertura = Stopwatch.StartNew();
            conn.Open();
            apertura.Stop();

            const string query = @"
SELECT Id, FechaAudiencia, TotalDiscos, Juez, Expediente, Causa,
       TipoAudiencia, HoraTermino, Imputado, Delito, Victima, Sala, Observaciones
FROM Ejecucion
WHERE FechaAudiencia >= ? AND FechaAudiencia < ?";

            using OleDbCommand cmd = new OleDbCommand(query, conn);
            cmd.Parameters.AddWithValue("?", desde);
            cmd.Parameters.AddWithValue("?", hasta);
            Stopwatch consulta = Stopwatch.StartNew();
            using OleDbDataReader reader = cmd.ExecuteReader();
            consulta.Stop();
            Stopwatch mapeo = Stopwatch.StartNew();

            while (reader.Read())
            {
                lista.Add(new Ejecucion
                {
                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                    FechaAudiencia = DateTime.TryParse(reader["FechaAudiencia"]?.ToString(), out DateTime fecha) ? fecha : null,
                    TotalDiscos = reader["TotalDiscos"]?.ToString() ?? "",
                    Juez = reader["Juez"]?.ToString() ?? "",
                    ExpedienteNumero = reader["Expediente"]?.ToString() ?? "",
                    Causa = reader["Causa"]?.ToString() ?? "",
                    TipoAudiencia = reader["TipoAudiencia"]?.ToString() ?? "",
                    HoraTermino = reader["HoraTermino"]?.ToString() ?? "",
                    Imputado = reader["Imputado"]?.ToString() ?? "",
                    Delito = reader["Delito"]?.ToString() ?? "",
                    Victima = reader["Victima"]?.ToString() ?? "",
                    Sala = reader["Sala"]?.ToString() ?? "",
                    Observaciones = reader["Observaciones"]?.ToString() ?? ""
                });
            }

            mapeo.Stop();
            totalMetodo.Stop();
            RendimientoLog.Registrar("EjecucionData.ObtenerEjecucionesParaReportes",
                "Ejecucion", "ExecuteReader/Mapping",
                openMs: apertura.ElapsedMilliseconds,
                queryMs: consulta.ElapsedMilliseconds,
                mappingMs: mapeo.ElapsedMilliseconds, rows: lista.Count,
                totalMs: totalMetodo.ElapsedMilliseconds);
            return lista;
        }
    }
}
