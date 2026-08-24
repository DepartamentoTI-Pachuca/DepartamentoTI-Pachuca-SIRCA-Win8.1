using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using PoderJudicial.Helpers;

namespace PoderJudicial.Data
{
    public class CopiasData
    {
        public int ObtenerSiguienteIdVisual()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string sql = "SELECT MAX(Id) FROM CopiasAudiencias";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    object resultado = cmd.ExecuteScalar();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("CopiasData.ObtenerSiguienteIdVisual",
                        "CopiasAudiencias", "ExecuteScalar",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: 1,
                        totalMs: totalMetodo.ElapsedMilliseconds);

                    if (resultado == null || resultado == DBNull.Value)
                    {
                        return 1;
                    }

                    return Convert.ToInt32(resultado) + 1;
                }
            }
        }

        /// <summary>
        /// Actualiza un registro existente.
        /// </summary>
        public void Actualizar(RegistroCopia registro)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string sql = @"
UPDATE CopiasAudiencias SET
    FeAudiencia            = ?,
    FeRecibo               = ?,
    TotDiscosEntregados    = ?,
    TipoDisco              = ?,
    NoCausa                = ?,
    NUC                    = ?,
    TipoCausa              = ?,
    DiscosExternos         = ?,
    [Etiquetas entregadas] = ?,
    [A quien se entraga]   = ?,
    Observaciones          = ?
WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.FeAudiencia.HasValue
                            ? (object)registro.FeAudiencia.Value
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.FeRecibo.HasValue
                            ? (object)registro.FeRecibo.Value
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TotDiscosEntregados.HasValue
                            ? (object)registro.TotDiscosEntregados.Value
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TipoDisco ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.NoCausa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.NUC ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TipoCausa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.DiscosExternos ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.EtiquetasEntregadas ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.AQuienSeEntrega ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.Observaciones ?? string.Empty);

                    cmd.Parameters.AddWithValue("?", registro.Id);

                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("CopiasData.Actualizar", "CopiasAudiencias",
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }
        }

        /// <summary>
        /// Inserta un nuevo registro.
        /// </summary>
        public void Insertar(RegistroCopia registro)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string sql = @"
INSERT INTO CopiasAudiencias
(
    Id,
    FeAudiencia,
    FeRecibo,
    TotDiscosEntregados,
    TipoDisco,
    NoCausa,
    NUC,
    TipoCausa,
    DiscosExternos,
    [Etiquetas entregadas],
    [A quien se entraga],
    Observaciones,
    [Quien Realiza]
)
VALUES
(
    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
)";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", registro.Id);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.FeAudiencia ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.FeRecibo ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TotDiscosEntregados ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TipoDisco ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.NoCausa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.NUC ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.TipoCausa ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.DiscosExternos?.ToString() ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.EtiquetasEntregadas?.ToString() ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.AQuienSeEntrega ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.Observaciones ?? string.Empty);

                    cmd.Parameters.AddWithValue(
                        "?",
                        registro.QuienRegistra ?? string.Empty);

                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("CopiasData.Insertar", "CopiasAudiencias",
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }
        }

        public (int Simples, int Autenticas) ObtenerTotalesReporte(
            int anio, int? mes, string tipoCausa)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using OleDbConnection conn = Conexion.ObtenerConexion();
            Stopwatch apertura = Stopwatch.StartNew();
            conn.Open();
            apertura.Stop();

            string sql = @"
SELECT
    SUM(IIF(InStr(1, UCase([TipoDisco]), 'SIMP') > 0, 1, 0)) AS Simples,
    SUM(IIF(InStr(1, UCase([TipoDisco]), 'AUT') > 0, 1, 0)) AS Autenticas
FROM CopiasAudiencias
WHERE FeAudiencia IS NOT NULL
  AND Year(FeAudiencia) = ?";

            bool filtrarTipo = !string.IsNullOrWhiteSpace(tipoCausa) &&
                !tipoCausa.Equals("Todos", StringComparison.OrdinalIgnoreCase) &&
                !tipoCausa.Equals("Todas", StringComparison.OrdinalIgnoreCase);

            if (mes.HasValue)
                sql += " AND Month(FeAudiencia) = ?";
            if (filtrarTipo)
                sql += " AND UCase(Trim(TipoCausa)) = ?";

            using OleDbCommand cmd = new OleDbCommand(sql, conn);
            cmd.Parameters.AddWithValue("?", anio);
            if (mes.HasValue)
                cmd.Parameters.AddWithValue("?", mes.Value);
            if (filtrarTipo)
                cmd.Parameters.AddWithValue("?", tipoCausa.Trim().ToUpperInvariant());

            Stopwatch consulta = Stopwatch.StartNew();
            using OleDbDataReader reader = cmd.ExecuteReader();
            consulta.Stop();
            Stopwatch mapeo = Stopwatch.StartNew();

            int simples = 0;
            int autenticas = 0;
            int filas = 0;
            if (reader.Read())
            {
                filas = 1;
                simples = reader["Simples"] == DBNull.Value
                    ? 0 : Convert.ToInt32(reader["Simples"]);
                autenticas = reader["Autenticas"] == DBNull.Value
                    ? 0 : Convert.ToInt32(reader["Autenticas"]);
            }

            mapeo.Stop();
            totalMetodo.Stop();
            RendimientoLog.Registrar("CopiasData.ObtenerTotalesReporte",
                "CopiasAudiencias", "Consulta agregada ExecuteReader",
                openMs: apertura.ElapsedMilliseconds,
                queryMs: consulta.ElapsedMilliseconds,
                mappingMs: mapeo.ElapsedMilliseconds,
                rows: filas, totalMs: totalMetodo.ElapsedMilliseconds);

            return (simples, autenticas);
        }

        /// <summary>
        /// Obtiene los valores distintos registrados en
        /// "A quien se entrega".
        /// </summary>
        public List<string> ObtenerValoresAQuienSeEntrega()
        {
            var lista = new List<string>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                string sql = @"
SELECT DISTINCT [A quien se entraga]
FROM CopiasAudiencias
WHERE [A quien se entraga] IS NOT NULL";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string valor = reader[0]?.ToString();

                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            lista.Add(valor);
                        }
                    }
                }
            }

            return lista;
        }

        public (List<string> Historial, int SiguienteId) ObtenerDatosIniciales()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            var historial = new List<string>();

            using OleDbConnection conn = Conexion.ObtenerConexion();
            Stopwatch apertura = Stopwatch.StartNew();
            conn.Open();
            apertura.Stop();

            const string sqlHistorial = @"
SELECT DISTINCT [A quien se entraga]
FROM CopiasAudiencias
WHERE [A quien se entraga] IS NOT NULL";

            using (OleDbCommand cmd = new OleDbCommand(sqlHistorial, conn))
            {
                Stopwatch consulta = Stopwatch.StartNew();
                using OleDbDataReader reader = cmd.ExecuteReader();
                consulta.Stop();
                Stopwatch mapeo = Stopwatch.StartNew();
                while (reader.Read())
                {
                    string valor = reader[0]?.ToString();
                    if (!string.IsNullOrWhiteSpace(valor))
                        historial.Add(valor);
                }
                mapeo.Stop();
                RendimientoLog.Registrar("CopiasData.ObtenerDatosIniciales",
                    "CopiasAudiencias", "Catálogo ExecuteReader",
                    openMs: apertura.ElapsedMilliseconds,
                    queryMs: consulta.ElapsedMilliseconds,
                    mappingMs: mapeo.ElapsedMilliseconds, rows: historial.Count);
            }

            int siguienteId;
            using (OleDbCommand cmd =
                new OleDbCommand("SELECT MAX(Id) FROM CopiasAudiencias", conn))
            {
                Stopwatch consulta = Stopwatch.StartNew();
                object resultado = cmd.ExecuteScalar();
                consulta.Stop();
                siguienteId = resultado == null || resultado == DBNull.Value
                    ? 1
                    : Convert.ToInt32(resultado) + 1;

                totalMetodo.Stop();
                RendimientoLog.Registrar("CopiasData.ObtenerDatosIniciales",
                    "CopiasAudiencias", "Folio ExecuteScalar",
                    queryMs: consulta.ElapsedMilliseconds, rows: 1,
                    totalMs: totalMetodo.ElapsedMilliseconds);
            }

            return (historial, siguienteId);
        }

        /// <summary>
        /// Obtiene todos los registros necesarios para calcular
        /// el total de discos entregados.
        /// </summary>
        public List<RegistroCopia> ObtenerTodas()
        {
            var lista = new List<RegistroCopia>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                string sql = @"
SELECT
    Id,
    TotDiscosEntregados
FROM CopiasAudiencias";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new RegistroCopia
                        {
                            Id = reader["Id"] != DBNull.Value
                                ? Convert.ToInt32(reader["Id"])
                                : 0,

                            TotDiscosEntregados =
                                int.TryParse(
                                    reader["TotDiscosEntregados"]?.ToString(),
                                    out int total)
                                    ? total
                                    : (int?)null
                        });
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Obtiene un registro completo de Registro de Copias por Id.
        /// </summary>
        public RegistroCopia ObtenerCopiaPorId(int id)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                string sql =
                    "SELECT * FROM CopiasAudiencias WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", id);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new RegistroCopia
                            {
                                Id = Convert.ToInt32(reader["Id"]),

                                FeAudiencia =
                                    DateTime.TryParse(
                                        reader["FeAudiencia"]?.ToString(),
                                        out DateTime fechaAudiencia)
                                        ? fechaAudiencia
                                        : (DateTime?)null,

                                FeRecibo =
                                    DateTime.TryParse(
                                        reader["FeRecibo"]?.ToString(),
                                        out DateTime fechaRecibo)
                                        ? fechaRecibo
                                        : (DateTime?)null,

                                TotDiscosEntregados =
                                    int.TryParse(
                                        reader["TotDiscosEntregados"]?.ToString(),
                                        out int totalDiscos)
                                        ? totalDiscos
                                        : (int?)null,

                                TipoDisco =
                                    reader["TipoDisco"]?.ToString()
                                    ?? string.Empty,

                                NoCausa =
                                    reader["NoCausa"]?.ToString()
                                    ?? string.Empty,

                                NUC =
                                    reader["NUC"]?.ToString()
                                    ?? string.Empty,

                                TipoCausa =
                                    reader["TipoCausa"]?.ToString()
                                    ?? string.Empty,

                                DiscosExternos =
                                    reader["DiscosExternos"]?.ToString()
                                    ?? string.Empty,

                                EtiquetasEntregadas =
                                    reader["Etiquetas entregadas"]?.ToString()
                                    ?? string.Empty,

                                AQuienSeEntrega =
                                    reader["A quien se entraga"]?.ToString()
                                    ?? string.Empty,

                                Observaciones =
                                    reader["Observaciones"]?.ToString()
                                    ?? string.Empty,

                                QuienRegistra =
                                    reader["Quien Realiza"]?.ToString()
                                    ?? string.Empty
                            };
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Obtiene todos los registros de CopiasAudiencias.
        /// Se utiliza para reportes y listados completos.
        /// </summary>
        public List<RegistroCopia> ObtenerCopias()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            var lista = new List<RegistroCopia>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                const string sql = @"
SELECT
    Id,
    FeAudiencia,
    FeRecibo,
    TotDiscosEntregados,
    TipoDisco,
    NoCausa,
    NUC,
    TipoCausa,
    DiscosExternos,
    [Etiquetas entregadas],
    [A quien se entraga],
    Observaciones,
    [Quien Realiza]
FROM CopiasAudiencias
ORDER BY FeRecibo, Id";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    using OleDbDataReader reader = cmd.ExecuteReader();
                    consulta.Stop();
                    Stopwatch mapeo = Stopwatch.StartNew();
                    while (reader.Read())
                    {
                        var registro = new RegistroCopia
                        {
                            Id = Convert.ToInt32(reader["Id"]),

                            FeAudiencia = DateTime.TryParse(
                                reader["FeAudiencia"]?.ToString(),
                                out DateTime fechaAudiencia)
                                ? fechaAudiencia
                                : (DateTime?)null,

                            FeRecibo = DateTime.TryParse(
                                reader["FeRecibo"]?.ToString(),
                                out DateTime fechaRecibo)
                                ? fechaRecibo
                                : (DateTime?)null,

                            TotDiscosEntregados = int.TryParse(
                                reader["TotDiscosEntregados"]?.ToString(),
                                out int totalDiscos)
                                ? totalDiscos
                                : (int?)null,

                            TipoDisco =
                                reader["TipoDisco"]?.ToString()
                                ?? string.Empty,

                            NoCausa =
                                reader["NoCausa"]?.ToString()
                                ?? string.Empty,

                            NUC =
                                reader["NUC"]?.ToString()
                                ?? string.Empty,

                            TipoCausa =
                                reader["TipoCausa"]?.ToString()
                                ?? string.Empty,

                            DiscosExternos =
                                reader["DiscosExternos"]?.ToString()
                                ?? string.Empty,

                            EtiquetasEntregadas =
                                reader["Etiquetas entregadas"]?.ToString()
                                ?? string.Empty,

                            AQuienSeEntrega =
                                reader["A quien se entraga"]?.ToString()
                                ?? string.Empty,

                            Observaciones =
                                reader["Observaciones"]?.ToString()
                                ?? string.Empty,

                            QuienRegistra =
                                reader["Quien Realiza"]?.ToString()
                                ?? string.Empty
                        };

                        lista.Add(registro);
                    }
                    mapeo.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("CopiasData.ObtenerCopias",
                        "CopiasAudiencias", "ExecuteReader/Mapping",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: lista.Count,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return lista;
        }
















    }
}
