using System;
using System.Collections.Generic;
using System.Linq;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Encapsula el criterio para registrar la modalidad de una audiencia
    /// (videoconferencia / concentrada) dentro de columnas existentes
    /// ("Quien Realiza" / "Observaciones"), sin requerir cambios de esquema
    /// en la base de datos.
    /// </summary>
    public static class ModalidadAudienciaHelper
    {
        private const string EtiquetaVideoconferencia = "Videoconferencia";
        private const string EtiquetaConcentrada = "Concentrada";
        private const string Separador = " / ";

        /// <summary>
        /// Construye el texto a guardar en "Quien Realiza" u "Observaciones".
        /// Ej: "Antonio", "Antonio / Videoconferencia", "Antonio / Concentrada"
        /// o "Antonio / Videoconferencia / Concentrada".
        /// </summary>
        public static string ConstruirRegistro(string usuario, bool esVideoconferencia, bool esConcentrada = false)
        {
            var partes = new List<string> { usuario?.Trim() ?? string.Empty };

            if (esVideoconferencia) partes.Add(EtiquetaVideoconferencia);
            if (esConcentrada) partes.Add(EtiquetaConcentrada);

            return string.Join(Separador, partes);
        }

        /// <summary>
        /// Interpreta un texto ya guardado (de "Quien Realiza" u "Observaciones")
        /// para saber si corresponde a una audiencia por videoconferencia.
        /// Útil al cargar un registro para editarlo o visualizarlo.
        /// </summary>
        public static bool EsVideoconferencia(string texto)
            => ContieneEtiqueta(texto, EtiquetaVideoconferencia);

        /// <summary>
        /// Interpreta un texto ya guardado para saber si la audiencia
        /// perteneció a un lote de audiencias concentradas.
        /// </summary>
        public static bool EsConcentrada(string texto)
            => ContieneEtiqueta(texto, EtiquetaConcentrada);

        private static bool ContieneEtiqueta(string texto, string etiqueta)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return texto.IndexOf(etiqueta, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Devuelve únicamente el nombre de usuario, sin ninguna etiqueta de
        /// modalidad, por si se necesita mostrar/editar el nombre de forma aislada.
        /// </summary>
        public static string ExtraerUsuario(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            int idx = texto.IndexOf(Separador, StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? texto.Substring(0, idx).Trim() : texto.Trim();
        }
    }
}
