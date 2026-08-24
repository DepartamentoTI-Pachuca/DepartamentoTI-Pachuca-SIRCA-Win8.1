using PoderJudicial.Models;
using PoderJudicial.Views;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using PoderJudicial.Helpers;


namespace PoderJudicial.Data
{
    public class AudienciaData
    {
        // ──────────────────────────────────────────
        //  Mapeo centralizado — un solo lugar para
        //  leer columnas del reader al modelo
        private static Audiencia MapearDesdeReader(
    OleDbDataReader reader)
        {
            Audiencia a = new Audiencia
            {
                // ─────────────────────────────
                // ID
                // ─────────────────────────────
                Id = ExisteColumna(reader, "Id") &&
                     reader["Id"] != DBNull.Value
                    ? Convert.ToInt32(reader["Id"])
                    : 0,

                // ─────────────────────────────
                // FECHA AUDIENCIA
                // ─────────────────────────────
                FechaAudiencia =
    ExisteColumna(reader, "FeAudiencia") &&
    DateTime.TryParse(
        reader["FeAudiencia"]?.ToString(),
        out DateTime fechaAud1)

        ? fechaAud1

        : ExisteColumna(reader, "FechaAudiencia") &&
          DateTime.TryParse(
              reader["FechaAudiencia"]?.ToString(),
              out DateTime fechaAud2)

            ? fechaAud2

            : null,

                // ─────────────────────────────
                // FECHA RECIBO
                // ─────────────────────────────
                FechaRecibo =
    ExisteColumna(reader, "FeRecibo") &&
    DateTime.TryParse(
        reader["FeRecibo"]?.ToString(),
        out DateTime fechaRec)

        ? fechaRec

        : null,

                // ─────────────────────────────
                // TOTAL DISCOS
                // ─────────────────────────────

                TotDiscos =
    ExisteColumna(reader, "TotDiscos") &&
    int.TryParse(
        reader["TotDiscos"]?.ToString(),
        out int discosAud)
        ? discosAud

    : ExisteColumna(reader, "TotalDiscos") &&
      int.TryParse(
          new string(
              reader["TotalDiscos"]
                  ?.ToString()
                  .Where(char.IsDigit)
                  .ToArray()),
          out int discosEjec)
        ? discosEjec

    : ExisteColumna(reader, "TotDiscosEntregados") &&
      int.TryParse(
          reader["TotDiscosEntregados"]?.ToString(),
          out int discosCopias)
        ? discosCopias

    : null,
                // ─────────────────────────────
                // TIPO DISCO
                // ─────────────────────────────
                TipoDisco =
                    ExisteColumna(reader, "TipoDisco")
                        ? reader["TipoDisco"]?.ToString()
                        : "",

                // ─────────────────────────────
                // JUZGADO
                // ─────────────────────────────
                Juzgado =
                    ExisteColumna(reader, "Juzgado")
                        ? reader["Juzgado"]?.ToString()
                        : "",

                // ─────────────────────────────
                // TOTAL DISCO AUDIENCIA
                // ─────────────────────────────
                TotDiscoAudiencia =
    ExisteColumna(reader, "TotDiscoAudiencia")
        ? reader["TotDiscoAudiencia"]?.ToString()

    : ExisteColumna(reader, "DiscosExternos")
        ? reader["DiscosExternos"]?.ToString()

    : "",

                // ─────────────────────────────
                // JUEZ
                // ─────────────────────────────
                Juez =
                    ExisteColumna(reader, "Juez")
                        ? reader["Juez"]?.ToString()
                        : "",

                // ─────────────────────────────
                // NO CAUSA
                // ─────────────────────────────
                NoCausa =
                    ExisteColumna(reader, "NoCausa")
                        ? reader["NoCausa"]?.ToString()
                        : ExisteColumna(reader, "Causa")
                            ? reader["Causa"]?.ToString()
                            : "",

                // ─────────────────────────────
                // NUC
                // ─────────────────────────────
                NUC =
                    ExisteColumna(reader, "NUC")
                        ? reader["NUC"]?.ToString()
                        : "",

                // ─────────────────────────────
                // TIPO CAUSA
                // ─────────────────────────────
                TipoCausa =
                    ExisteColumna(reader, "TipoCausa")
                        ? reader["TipoCausa"]?.ToString()
                        : "EXP",

                // ─────────────────────────────
                // TIPO AUDIENCIA
                // ─────────────────────────────
                TipoAudiencia =
                    ExisteColumna(reader, "TipoAudiencia")
                        ? reader["TipoAudiencia"]?.ToString()
                        : "",

                // ─────────────────────────────
                // HORA CONCLUSION / TERMINO
                // ─────────────────────────────
                HoraConclusion =
    ExisteColumna(reader, "Hora conclusion") &&
    DateTime.TryParse(
        reader["Hora conclusion"]?.ToString(),
        out DateTime hora1)

        ? hora1

        : ExisteColumna(reader, "HoraTermino") &&
          DateTime.TryParse(
              reader["HoraTermino"]?.ToString(),
              out DateTime hora2)

            ? hora2

            : null,

                // ─────────────────────────────
                // IMPUTADO
                // ─────────────────────────────
                Imputado =
                    ExisteColumna(reader, "Imputado")
                        ? reader["Imputado"]?.ToString()
                        : "",

                // ─────────────────────────────
                // DELITO
                // ─────────────────────────────
                Delito =
                    ExisteColumna(reader, "Delito")
                        ? reader["Delito"]?.ToString()
                        : "",

                // ─────────────────────────────
                // AGRAVIADO / VICTIMA
                // ─────────────────────────────
                Agraviado =
    ExisteColumna(reader, "Agraviado")
        ? reader["Agraviado"]?.ToString()
        : ExisteColumna(reader, "Victima")
            ? reader["Victima"]?.ToString()
            : "",

                // ─────────────────────────────
                // SALA
                // ─────────────────────────────
                Sala =
                    ExisteColumna(reader, "Sala")
                        ? reader["Sala"]?.ToString()
                        : "",

                // ─────────────────────────────
                // NO CAUSA JUICIO
                // ─────────────────────────────
                NoCausaJuicio =
                    ExisteColumna(reader, "NoCausaJuicio")
                        ? reader["NoCausaJuicio"]?.ToString()
                        : "",

                // ─────────────────────────────
                // DIFERIDA
                // ─────────────────────────────
                Diferida =
                    ExisteColumna(reader, "Diferida")
                        ? reader["Diferida"]?.ToString()
                        : "",


                // ─────────────────────────────
                // QUIEN REALIZA
                // ─────────────────────────────
                QuienRealiza =
ExisteColumna(reader, "QuienRealiza")
    ? reader["QuienRealiza"]?.ToString()

: ExisteColumna(reader, "Quien Realiza")
    ? reader["Quien Realiza"]?.ToString()

: ExisteColumna(reader, "Observaciones")
    ? reader["Observaciones"]?.ToString()

: "",

                // ─────────────────────────────
                // OBSERVACIONES
                // ─────────────────────────────
                Observaciones =
    ExisteColumna(reader, "Observaciones")
        ? reader["Observaciones"]?.ToString()
        : "",


                //
                //EXPEDIENTE (NO EN TODOS LOS REGISTROS)  Y ETIQUETAS ENTREGADAS (NO EN TODOS LOS REGISTROS)
                //

                Expediente =
    ExisteColumna(reader, "Expediente")
        ? reader["Expediente"]?.ToString()
        : "",




                DiscosExternos =
    ExisteColumna(reader, "DiscosExternos")
        ? reader["DiscosExternos"]?.ToString()
        : "",

                EtiquetasEntregadas =
    ExisteColumna(reader, "Etiquetas entregadas")
        ? reader["Etiquetas entregadas"]?.ToString()
        : "",

                AQuienEntrega =
    ExisteColumna(reader, "A quien se entraga")
        ? reader["A quien se entraga"]?.ToString()
        : "",


            };

            // ÍNDICE DE BÚSQUEDA OPTIMIZADO
            a.TextoBusqueda = string.Join(" ", new[]
                {
        a.Id.ToString(),
        a.NoCausa,
        a.NUC,
        a.Imputado,
        a.Delito,
        a.Juez,
        a.Juzgado,
        a.TipoAudiencia,
        a.TipoCausa,
        a.Agraviado,
        a.Sala,
        a.NoCausaJuicio,
        a.QuienRealiza,
        a.FechaAudiencia?.ToString("dd/MM/yyyy"),
        a.FechaRecibo?.ToString("dd/MM/yyyy")
    }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .ToLower();

            return a;
        }

        // ──────────────────────────────────────────
        //  Parámetros centralizados — mismo orden
        //  que el INSERT y el UPDATE
        // ──────────────────────────────────────────
        private static void AgregarParametros(OleDbCommand cmd, Audiencia a)
        {
            // CRÍTICO en OleDb: el orden debe coincidir
            // exactamente con el orden del SQL
            cmd.Parameters.AddWithValue("@Id", a.Id);
            cmd.Parameters.AddWithValue("@FeAudiencia", a.FechaAudiencia.HasValue ? a.FechaAudiencia.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@FeRecibo", a.FechaRecibo.HasValue ? a.FechaRecibo.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TotDiscos", a.TotDiscos.HasValue ? a.TotDiscos.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoDisco", a.TipoDisco ?? string.Empty);
            cmd.Parameters.AddWithValue("@Juzgado", a.Juzgado ?? string.Empty);
            cmd.Parameters.AddWithValue("@TotDiscoAudiencia", a.TotDiscoAudiencia ?? string.Empty);
            cmd.Parameters.AddWithValue("@Juez", a.Juez ?? string.Empty);
            cmd.Parameters.AddWithValue("@NoCausa", a.NoCausa ?? string.Empty);
            cmd.Parameters.AddWithValue("@NUC", a.NUC ?? string.Empty);
            cmd.Parameters.AddWithValue("@TipoCausa", a.TipoCausa ?? string.Empty);
            cmd.Parameters.AddWithValue("@TipoAudiencia", a.TipoAudiencia ?? string.Empty);
            cmd.Parameters.AddWithValue("@HoraConclusion", a.HoraConclusion.HasValue ? a.HoraConclusion.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Imputado", a.Imputado ?? string.Empty);
            cmd.Parameters.AddWithValue("@Delito", a.Delito ?? string.Empty);
            cmd.Parameters.AddWithValue("@Agraviado", a.Agraviado ?? string.Empty);
            cmd.Parameters.AddWithValue("@Sala", a.Sala ?? string.Empty);
            cmd.Parameters.AddWithValue("@NoCausaJuicio", a.NoCausaJuicio ?? string.Empty);
            cmd.Parameters.AddWithValue("@Diferida", a.Diferida ?? string.Empty);
            cmd.Parameters.AddWithValue("@QuienRealiza", a.QuienRealiza ?? string.Empty);
        }

        // ──────────────────────────────────────────
        //  OBTENER TODOS
        // ──────────────────────────────────────────
        public List<Audiencia> ObtenerAudiencias()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<Audiencia> lista = new List<Audiencia>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();
                string query = $"SELECT * FROM [{TableDetector.TablaActual}]";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    using OleDbDataReader reader = cmd.ExecuteReader();
                    consulta.Stop();
                    Stopwatch mapeo = Stopwatch.StartNew();
                    while (reader.Read())
                    {
                        if (string.IsNullOrWhiteSpace(reader["NoCausa"].ToString()) &&
                            string.IsNullOrWhiteSpace(reader["NUC"].ToString()))
                            continue;

                        lista.Add(MapearDesdeReader(reader));
                    }
                    mapeo.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("AudienciaData.ObtenerAudiencias",
                        TableDetector.TablaActual, "ExecuteReader/Mapping",
                        openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: lista.Count,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                }
            }

            return lista;
        }

        // ──────────────────────────────────────────
        //  OBTENER UNO POR NoCausa
        // ──────────────────────────────────────────
        public Audiencia ObtenerAudienciaPorNoCausa(string noCausa)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();
                string query = $"SELECT * FROM [{TableDetector.TablaActual}] WHERE NoCausa = ?";

                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", noCausa);

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapearDesdeReader(reader);
                    }
                }
            }

            return null;
        }



        // ──────────────────────────────────────────
        //  OBTENER UNO POR ID
        // ──────────────────────────────────────────
        public Audiencia ObtenerAudienciaPorId(int id)
        {
            using (OleDbConnection conn =
                Conexion.ObtenerConexion())
            {
                conn.Open();

                string query =
                    $"SELECT * FROM [{TableDetector.TablaActual}] WHERE Id = ?";

                using (OleDbCommand cmd =
                    new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", id);

                    using (OleDbDataReader reader =
                        cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapearDesdeReader(reader);
                    }
                }
            }

            return null;
        }


        // ──────────────────────────────────────────
        //  INSERT
        // ──────────────────────────────────────────
        public void Insertar(Audiencia a)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();
                string tabla = TableDetector.TablaActual;

                string sql = $@"
                    INSERT INTO [{tabla}] (Id,
                        FeAudiencia, FeRecibo,
                        TotDiscos, TipoDisco,
                        Juzgado, TotDiscoAudiencia,
                        Juez, NoCausa, NUC,
                        TipoCausa, TipoAudiencia,
                        [Hora conclusion],
                        Imputado, Delito, Agraviado,
                        Sala, NoCausaJuicio,
                        Diferida, [Quien Realiza]
                    ) VALUES (
                       ?, ?, ?,
                        ?, ?,
                        ?, ?,
                        ?, ?, ?,
                        ?, ?,
                        ?,
                        ?, ?, ?,
                        ?, ?,
                        ?, ?
                    )";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    AgregarParametros(cmd, a);
                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("AudienciaData.Insertar", tabla,
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }

            TableDetector.InvalidarCache();
        }

        public int ObtenerSiguienteIdVisual()
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                string tabla = TableDetector.TablaActual;

                string sql = $"SELECT MAX(Id) FROM [{tabla}]";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    Stopwatch consulta = Stopwatch.StartNew();
                    object resultado = cmd.ExecuteScalar();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("AudienciaData.ObtenerSiguienteIdVisual",
                        tabla, "ExecuteScalar", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: 1,
                        totalMs: totalMetodo.ElapsedMilliseconds);

                    if (resultado == DBNull.Value || resultado == null)
                        return 1;

                    return Convert.ToInt32(resultado) + 1;
                }
            }
        }


        //  ACTUALIZAR
        public void Actualizar(Audiencia a)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();
                string tabla = TableDetector.TablaActual;

                string sql = $@"
            UPDATE [{tabla}] SET
                FeAudiencia        = ?,
                FeRecibo           = ?,
                TotDiscos          = ?,
                TipoDisco          = ?,
                Juzgado            = ?,
                TotDiscoAudiencia  = ?,
                Juez               = ?,
                NoCausa            = ?,
                NUC                = ?,
                TipoCausa          = ?,
                TipoAudiencia      = ?,
                [Hora conclusion]  = ?,
                Imputado           = ?,
                Delito             = ?,
                Agraviado          = ?,
                Sala               = ?,
                NoCausaJuicio      = ?,
                Diferida           = ?,
                [Quien Realiza]    = ?
            WHERE Id = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    // Mismo orden que los ? del SET (sin Id al inicio)
                    cmd.Parameters.AddWithValue("@FeAudiencia", a.FechaAudiencia.HasValue ? (object)a.FechaAudiencia.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@FeRecibo", a.FechaRecibo.HasValue ? (object)a.FechaRecibo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TotDiscos", a.TotDiscos.HasValue ? (object)a.TotDiscos.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TipoDisco", a.TipoDisco ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Juzgado", a.Juzgado ?? string.Empty);
                    cmd.Parameters.AddWithValue("@TotDiscoAudiencia", a.TotDiscoAudiencia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Juez", a.Juez ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NoCausa", a.NoCausa ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NUC", a.NUC ?? string.Empty);
                    cmd.Parameters.AddWithValue("@TipoCausa", a.TipoCausa ?? string.Empty);
                    cmd.Parameters.AddWithValue("@TipoAudiencia", a.TipoAudiencia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@HoraConclusion", a.HoraConclusion.HasValue ? (object)a.HoraConclusion.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Imputado", a.Imputado ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Delito", a.Delito ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Agraviado", a.Agraviado ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Sala", a.Sala ?? string.Empty);
                    cmd.Parameters.AddWithValue("@NoCausaJuicio", a.NoCausaJuicio ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Diferida", a.Diferida ?? string.Empty);
                    cmd.Parameters.AddWithValue("@QuienRealiza", a.QuienRealiza ?? string.Empty);
                    // WHERE Id = ? — va AL FINAL en OleDb
                    cmd.Parameters.AddWithValue("@Id", a.Id);

                    Stopwatch consulta = Stopwatch.StartNew();
                    int filas = cmd.ExecuteNonQuery();
                    consulta.Stop();
                    totalMetodo.Stop();
                    RendimientoLog.Registrar("AudienciaData.Actualizar", tabla,
                        "ExecuteNonQuery", openMs: apertura.ElapsedMilliseconds,
                        queryMs: consulta.ElapsedMilliseconds, rows: filas,
                        totalMs: totalMetodo.ElapsedMilliseconds);
                    if (filas > 0)
                        CacheSesionAccess.InvalidarRutaActual();
                }
            }

            TableDetector.InvalidarCache();
        }


        public List<Audiencia> ObtenerAudiencias(
    string tabla)
        {
            List<Audiencia> lista = new();

            using (OleDbConnection conn =
                Conexion.ObtenerConexion())
            {
                conn.Open();

                string query =
                    $"SELECT * FROM [{tabla}]";

                using (OleDbCommand cmd =
                    new OleDbCommand(query, conn))
                {
                    using (OleDbDataReader reader =
                        cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(
                                MapearDesdeReader(reader));
                        }
                    }
                }
            }

            return lista;
        }


        public Audiencia ObtenerAudienciaPorId(
    int id,
    string tabla)
        {
            using (OleDbConnection conn =
                Conexion.ObtenerConexion())
            {
                conn.Open();

                string query =
                    $"SELECT * FROM [{tabla}] WHERE Id = ?";

                using (OleDbCommand cmd =
                    new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "?",
                        id);

                    using (OleDbDataReader reader =
                        cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapearDesdeReader(reader);
                        }
                    }
                }
            }

            return null;
        }


        private static bool ExisteColumna(
    OleDbDataReader reader,
    string columna)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i)
                    .Equals(columna,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }



        public string ObtenerNUCPorNoCausa(string noCausa)
        {
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                DataTable schema =
                    conn.GetSchema("Tables");

                foreach (DataRow row in schema.Rows)
                {
                    string nombreTabla =
                        row["TABLE_NAME"].ToString();

                    if (!nombreTabla.Contains("Audiencias"))
                        continue;

                    if (nombreTabla.StartsWith("MSys"))
                        continue;

                    string query =
                        $"SELECT TOP 1 NUC FROM [{nombreTabla}] WHERE NoCausa = ?";

                    using (OleDbCommand cmd =
                        new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", noCausa);

                        object resultado =
                            cmd.ExecuteScalar();

                        if (resultado != null &&
                            resultado != DBNull.Value)
                        {
                            return resultado.ToString();
                        }
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Igual que <see cref="ObtenerNUCPorNoCausa"/> pero además devuelve
        /// el Tipo de Causa y la Fecha de Audiencia asociados, en una sola
        /// consulta. Usado por el autocompletado de "Registro de Copias"
        /// (No. Causa → Fecha de Audiencia + NUC + Tipo Causa).
        /// Usa SELECT * (en vez de columnas fijas) porque, igual que en
        /// <see cref="MapearDesdeReader"/>, distintas tablas de Audiencias
        /// nombran la fecha como "FeAudiencia" o "FechaAudiencia".
        /// </summary>
        public string ObtenerDatosPorNoCausa(string noCausa, out string tipoCausa, out DateTime? fechaAudiencia)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            tipoCausa = "";
            fechaAudiencia = null;

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                Stopwatch esquemaTiempo = Stopwatch.StartNew();
                DataTable schema = conn.GetSchema("Tables");
                esquemaTiempo.Stop();
                RendimientoLog.Registrar("AudienciaData.ObtenerDatosPorNoCausa", "",
                    "GetSchema(Tables)", openMs: apertura.ElapsedMilliseconds,
                    queryMs: esquemaTiempo.ElapsedMilliseconds, rows: schema.Rows.Count);

                foreach (DataRow row in schema.Rows)
                {
                    string nombreTabla = row["TABLE_NAME"].ToString();

                    if (!nombreTabla.Contains("Audiencias")) continue;
                    if (nombreTabla.StartsWith("MSys")) continue;

                    string query = $"SELECT TOP 1 * FROM [{nombreTabla}] WHERE NoCausa = ?";

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", noCausa);

                        Stopwatch totalTabla = Stopwatch.StartNew();
                        Stopwatch consulta = Stopwatch.StartNew();
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            consulta.Stop();
                            Stopwatch mapeo = Stopwatch.StartNew();
                            if (!reader.Read())
                            {
                                mapeo.Stop();
                                totalTabla.Stop();
                                RendimientoLog.Registrar("AudienciaData.ObtenerDatosPorNoCausa",
                                    nombreTabla, "ExecuteReader/Mapping",
                                    queryMs: consulta.ElapsedMilliseconds,
                                    mappingMs: mapeo.ElapsedMilliseconds, rows: 0,
                                    totalMs: totalTabla.ElapsedMilliseconds);
                                continue;
                            }

                            tipoCausa = ExisteColumna(reader, "TipoCausa")
                                ? reader["TipoCausa"]?.ToString() ?? ""
                                : "";

                            fechaAudiencia =
                                ExisteColumna(reader, "FeAudiencia") &&
                                DateTime.TryParse(reader["FeAudiencia"]?.ToString(), out DateTime f1)
                                    ? f1
                                    : ExisteColumna(reader, "FechaAudiencia") &&
                                      DateTime.TryParse(reader["FechaAudiencia"]?.ToString(), out DateTime f2)
                                        ? f2
                                        : null;

                            string resultado = ExisteColumna(reader, "NUC")
                                ? reader["NUC"]?.ToString() ?? ""
                                : "";
                            mapeo.Stop();
                            totalTabla.Stop();
                            totalMetodo.Stop();
                            RendimientoLog.Registrar("AudienciaData.ObtenerDatosPorNoCausa",
                                nombreTabla, "ExecuteReader/Mapping",
                                queryMs: consulta.ElapsedMilliseconds,
                                mappingMs: mapeo.ElapsedMilliseconds, rows: 1,
                                totalMs: totalTabla.ElapsedMilliseconds);
                            RendimientoLog.Registrar("AudienciaData.ObtenerDatosPorNoCausa", "",
                                "Método", rows: 1, totalMs: totalMetodo.ElapsedMilliseconds);
                            return resultado;
                        }
                    }
                }
            }

            totalMetodo.Stop();
            RendimientoLog.Registrar("AudienciaData.ObtenerDatosPorNoCausa", "",
                "Método", rows: 0, totalMs: totalMetodo.ElapsedMilliseconds);
            return "";
        }

        /// <summary>
        /// Devuelve TODAS las fechas de audiencia (sin duplicados, ordenadas)
        /// asociadas a un No. Causa. A diferencia de
        /// <see cref="ObtenerDatosPorNoCausa"/> —que se detiene en la
        /// primera coincidencia, porque NUC/TipoCausa no cambian entre
        /// audiencias de la misma causa—, aquí es indispensable recorrer
        /// TODAS las filas y TODAS las tablas "Audiencias*" que coincidan,
        /// porque un mismo No. Causa puede tener varias audiencias (y por lo
        /// tanto varios discos originales) en fechas distintas.
        /// Usado por "Registro de Copias" para que el usuario elija a cuál
        /// disco original corresponde la copia que se está registrando.
        /// </summary>
        public List<DateTime> ObtenerFechasAudienciaPorNoCausa(string noCausa)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            var fechas = new List<DateTime>();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                Stopwatch apertura = Stopwatch.StartNew();
                conn.Open();
                apertura.Stop();

                Stopwatch esquemaTiempo = Stopwatch.StartNew();
                DataTable schema = conn.GetSchema("Tables");
                esquemaTiempo.Stop();
                RendimientoLog.Registrar("AudienciaData.ObtenerFechasAudienciaPorNoCausa", "",
                    "GetSchema(Tables)", openMs: apertura.ElapsedMilliseconds,
                    queryMs: esquemaTiempo.ElapsedMilliseconds, rows: schema.Rows.Count);

                foreach (DataRow row in schema.Rows)
                {
                    string nombreTabla = row["TABLE_NAME"].ToString();

                    if (!nombreTabla.Contains("Audiencias")) continue;
                    if (nombreTabla.StartsWith("MSys")) continue;

                    string query = $"SELECT * FROM [{nombreTabla}] WHERE NoCausa = ?";

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", noCausa);

                        Stopwatch totalTabla = Stopwatch.StartNew();
                        Stopwatch consulta = Stopwatch.StartNew();
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            consulta.Stop();
                            Stopwatch mapeo = Stopwatch.StartNew();
                            int filas = 0;
                            while (reader.Read())
                            {
                                filas++;
                                DateTime? fecha =
                                    ExisteColumna(reader, "FeAudiencia") &&
                                    DateTime.TryParse(reader["FeAudiencia"]?.ToString(), out DateTime f1)
                                        ? f1
                                        : ExisteColumna(reader, "FechaAudiencia") &&
                                          DateTime.TryParse(reader["FechaAudiencia"]?.ToString(), out DateTime f2)
                                            ? f2
                                            : (DateTime?)null;

                                if (fecha.HasValue)
                                    fechas.Add(fecha.Value.Date);
                            }
                            mapeo.Stop();
                            totalTabla.Stop();
                            RendimientoLog.Registrar(
                                "AudienciaData.ObtenerFechasAudienciaPorNoCausa",
                                nombreTabla, "ExecuteReader/Mapping",
                                queryMs: consulta.ElapsedMilliseconds,
                                mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                                totalMs: totalTabla.ElapsedMilliseconds);
                        }
                    }
                }
            }

            List<DateTime> resultado = fechas.Distinct().OrderBy(f => f).ToList();
            totalMetodo.Stop();
            RendimientoLog.Registrar("AudienciaData.ObtenerFechasAudienciaPorNoCausa", "",
                "Recorrido histórico", rows: fechas.Count,
                totalMs: totalMetodo.ElapsedMilliseconds);
            return resultado;
        }





        public string ObtenerVersionSistema()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version
                ?.ToString() ?? "1.0.0";
        }





        public List<Audiencia> ObtenerTodasAudienciasParaReportes()
        {
            List<Audiencia> lista = new();

            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();

                DataTable schema = conn.GetSchema("Tables");

                foreach (DataRow row in schema.Rows)
                {
                    string nombreTabla =
                        row["TABLE_NAME"]?.ToString() ?? "";

                    if (string.IsNullOrWhiteSpace(nombreTabla))
                        continue;

                    if (nombreTabla.StartsWith(
                            "MSys",
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (nombreTabla.IndexOf(
                            "Audiencias",
                            StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    string query =
                        $"SELECT * FROM [{nombreTabla}]";

                    using OleDbCommand cmd =
                        new OleDbCommand(query, conn);

                    using OleDbDataReader reader =
                        cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Audiencia audiencia =
                            MapearDesdeReader(reader);

                        // Conservamos la misma regla que utiliza
                        // ObtenerAudiencias().
                        if (string.IsNullOrWhiteSpace(audiencia.NoCausa) &&
                            string.IsNullOrWhiteSpace(audiencia.NUC))
                        {
                            continue;
                        }

                        lista.Add(audiencia);
                    }
                }
            }

            return lista;
        }

        public Audiencia ObtenerAudienciaMasRecientePorNoCausa(string noCausa)
        {
            if (string.IsNullOrWhiteSpace(noCausa)) return null;

            Audiencia masReciente = null;
            using (OleDbConnection conn = Conexion.ObtenerConexion())
            {
                conn.Open();
                foreach (string tabla in TableDetector.TodasLasTablas)
                {
                    string query = $"SELECT * FROM [{tabla}] WHERE NoCausa = ?";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", noCausa.Trim());
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Audiencia candidata = MapearDesdeReader(reader);
                                if (masReciente == null ||
                                    Nullable.Compare(candidata.FechaAudiencia,
                                        masReciente.FechaAudiencia) > 0 ||
                                    (Nullable.Compare(candidata.FechaAudiencia,
                                         masReciente.FechaAudiencia) == 0 &&
                                     candidata.Id > masReciente.Id))
                                {
                                    masReciente = candidata;
                                }
                            }
                        }
                    }
                }
            }

            return masReciente;
        }

        public List<Audiencia> ObtenerAudienciasParaReportes(int anio)
        {
            Stopwatch totalMetodo = Stopwatch.StartNew();
            List<Audiencia> lista = new();
            DateTime desde = new DateTime(anio, 1, 1);
            DateTime hasta = desde.AddYears(1);

            using OleDbConnection conn = Conexion.ObtenerConexion();
            Stopwatch apertura = Stopwatch.StartNew();
            conn.Open();
            apertura.Stop();
            RendimientoLog.Registrar("AudienciaData.ObtenerAudienciasParaReportes", "",
                "OleDbConnection.Open", openMs: apertura.ElapsedMilliseconds);

            List<string> tablas = TableDetector.TodasLasTablas
                .Where(nombre => TablaPuedeContenerAnio(nombre, anio))
                .ToList();

            // Defensa para nombres históricos no convencionales: conservar
            // resultados antes que asumir que el nombre describe los datos.
            if (tablas.Count == 0)
                tablas = TableDetector.TodasLasTablas.ToList();

            foreach (string nombreTabla in tablas)
            {
                Stopwatch totalTabla = Stopwatch.StartNew();
                string query = $@"
SELECT *
FROM [{nombreTabla}]
WHERE FeAudiencia >= ? AND FeAudiencia < ?";

                using OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("?", desde);
                cmd.Parameters.AddWithValue("?", hasta);

                Stopwatch consulta = Stopwatch.StartNew();
                using OleDbDataReader reader = cmd.ExecuteReader();
                consulta.Stop();
                Stopwatch mapeo = Stopwatch.StartNew();
                int filas = 0;
                while (reader.Read())
                {
                    filas++;
                    Audiencia audiencia = MapearDesdeReader(reader);
                    if (string.IsNullOrWhiteSpace(audiencia.NoCausa) &&
                        string.IsNullOrWhiteSpace(audiencia.NUC))
                        continue;

                    lista.Add(audiencia);
                }
                mapeo.Stop();
                totalTabla.Stop();
                RendimientoLog.Registrar("AudienciaData.ObtenerAudienciasParaReportes",
                    nombreTabla, "ExecuteReader/Mapping",
                    queryMs: consulta.ElapsedMilliseconds,
                    mappingMs: mapeo.ElapsedMilliseconds, rows: filas,
                    totalMs: totalTabla.ElapsedMilliseconds);
            }

            totalMetodo.Stop();
            RendimientoLog.Registrar("AudienciaData.ObtenerAudienciasParaReportes", "",
                "Recorrido histórico", rows: lista.Count,
                totalMs: totalMetodo.ElapsedMilliseconds);
            return lista;
        }

        private static bool TablaPuedeContenerAnio(string nombreTabla, int anio)
        {
            MatchCollection coincidencias = Regex.Matches(nombreTabla, @"\d{4}");
            if (coincidencias.Count == 0)
                return false;

            int[] anios = coincidencias
                .Cast<Match>()
                .Select(m => int.Parse(m.Value))
                .ToArray();

            return anio >= anios.Min() && anio <= anios.Max();
        }





    }

}
