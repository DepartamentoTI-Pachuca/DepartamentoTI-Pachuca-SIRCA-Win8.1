using System;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Mismo patrón que ModalidadAudienciaHelper (Videoconferencia en Nueva
    /// Audiencia), aplicado a "Registro de Copias": codifica si la copia se
    /// grabó directo dentro de la columna existente "Quien Realiza", sin
    /// requerir cambios de esquema en la base de datos.
    /// Se usa un helper propio (en vez de extender ModalidadAudienciaHelper)
    /// porque el formato pedido es distinto: separador "/" sin espacios y
    /// la etiqueta en minúsculas, tal como se guarda hoy para Copias.
    /// </summary>
    public static class ModalidadCopiaHelper
    {
        private const string EtiquetaGrabadoDirecto = "se grabó directo";
        private const string Separador = "/";

        /// <summary>
        /// Construye el texto a guardar en "Quien Realiza".
        /// Ej: "Rey" (no se grabó directo) o "Rey/se grabó directo".
        /// </summary>
        public static string ConstruirRegistro(string usuario, bool seGraboDirecto)
        {
            usuario = usuario?.Trim() ?? string.Empty;

            return seGraboDirecto
                ? $"{usuario}{Separador}{EtiquetaGrabadoDirecto}"
                : usuario;
        }

        /// <summary>
        /// Interpreta un texto ya guardado en "Quien Realiza" para saber si
        /// la copia se grabó directo. Útil al cargar un registro para
        /// editarlo.
        /// </summary>
        public static bool SeGraboDirecto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return texto.IndexOf(EtiquetaGrabadoDirecto, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Devuelve únicamente el nombre de usuario, sin la etiqueta, por si
        /// se necesita mostrar/editar el nombre de forma aislada.
        /// </summary>
        public static string ExtraerUsuario(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            int idx = texto.IndexOf(Separador, StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? texto.Substring(0, idx).Trim() : texto.Trim();
        }
    }
}