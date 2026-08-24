using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Aplica un <see cref="FiltroConsulta"/> a una lista de registros ya
    /// unificados en el modelo <see cref="Audiencia"/> (ver
    /// AudienciaData.MapearDesdeReader). Combina con AND únicamente los
    /// criterios que el usuario llenó; los vacíos se ignoran. No sabe ni le
    /// importa de qué tabla vienen los registros — por eso sirve para
    /// Audiencias, Ejecución y Registro de Copias sin ramas por tabla, y
    /// para cualquier tabla que se agregue en el futuro, siempre que su
    /// mapeo hacia Audiencia exista.
    /// </summary>
    public static class BuscadorRegistros
    {
        public static List<Audiencia> AplicarFiltro(IEnumerable<Audiencia> origen, FiltroConsulta f)
        {
            IEnumerable<Audiencia> resultado = origen;

            if (!string.IsNullOrWhiteSpace(f.NUC))
                resultado = resultado.Where(a => Contiene(a.NUC, f.NUC));

            if (!string.IsNullOrWhiteSpace(f.NoCausa))
            {
                string buscado = NormalizarNoCausa(f.NoCausa);
                resultado = resultado.Where(a => NormalizarNoCausa(a.NoCausa) == buscado);
            }

            if (f.FechaDesde.HasValue)
                resultado = resultado.Where(a =>
                    a.FechaAudiencia.HasValue && a.FechaAudiencia.Value.Date >= f.FechaDesde.Value.Date);

            if (f.FechaHasta.HasValue)
                resultado = resultado.Where(a =>
                    a.FechaAudiencia.HasValue && a.FechaAudiencia.Value.Date <= f.FechaHasta.Value.Date);

            if (f.FechaReciboDesde.HasValue)
                resultado = resultado.Where(a =>
                    a.FechaRecibo.HasValue && a.FechaRecibo.Value.Date >= f.FechaReciboDesde.Value.Date);

            if (f.FechaReciboHasta.HasValue)
                resultado = resultado.Where(a =>
                    a.FechaRecibo.HasValue && a.FechaRecibo.Value.Date <= f.FechaReciboHasta.Value.Date);

            if (!string.IsNullOrWhiteSpace(f.TipoCausa))
                resultado = resultado.Where(a => Igual(a.TipoCausa, f.TipoCausa));

            if (!string.IsNullOrWhiteSpace(f.Juzgado))
                resultado = resultado.Where(a => Igual(a.Juzgado, f.Juzgado));

            if (!string.IsNullOrWhiteSpace(f.Sala))
                resultado = resultado.Where(a => Igual(a.Sala, f.Sala));

            if (!string.IsNullOrWhiteSpace(f.Imputado))
                resultado = resultado.Where(a => Contiene(a.Imputado, f.Imputado));

            if (!string.IsNullOrWhiteSpace(f.Delito))
                resultado = resultado.Where(a => Contiene(a.Delito, f.Delito));

            if (!string.IsNullOrWhiteSpace(f.Juez))
                resultado = resultado.Where(a => Contiene(a.Juez, f.Juez));

            if (!string.IsNullOrWhiteSpace(f.Expediente))
                resultado = resultado.Where(a => Contiene(a.Expediente, f.Expediente));

            if (!string.IsNullOrWhiteSpace(f.AQuienEntrega))
                resultado = resultado.Where(a => Contiene(a.AQuienEntrega, f.AQuienEntrega));

            return resultado.ToList();
        }

        // ── Coincidencia parcial (Contains), insensible a mayúsculas ──
        private static bool Contiene(string valorCampo, string buscado)
            => !string.IsNullOrWhiteSpace(valorCampo) &&
               valorCampo.IndexOf(buscado, StringComparison.OrdinalIgnoreCase) >= 0;

        // ── Coincidencia exacta, insensible a mayúsculas y espacios extremos ──
        private static bool Igual(string valorCampo, string buscado)
            => string.Equals(valorCampo?.Trim(), buscado.Trim(), StringComparison.OrdinalIgnoreCase);

        // ── No. Causa: exacto pero ignorando espacios internos, para que
        //    "123/2024" y "123 / 2024" se consideren el mismo valor. El '/'
        //    se conserva porque es parte real del formato, no un separador. ─
        private static string NormalizarNoCausa(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            return new string(texto.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        /// <summary>
        /// Extrae los dígitos de un texto tipo "3 discos" y los convierte a
        /// número; 0 si no hay. Único lugar donde vive esta lógica — la
        /// usan tanto el total de discos de Consultar Registros como la
        /// columna "Total Discos" de Actividad Reciente en el Home, para no
        /// duplicarla.
        /// </summary>
        public static int ExtraerNumero(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;

            string numeros = new string(texto.Where(char.IsDigit).ToArray());
            return int.TryParse(numeros, out int valor) ? valor : 0;
        }
    }
}
