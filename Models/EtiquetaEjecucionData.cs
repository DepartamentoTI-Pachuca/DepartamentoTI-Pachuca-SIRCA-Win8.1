namespace PoderJudicial.Models
{
    public class EtiquetaEjecucionData
    {
        public string Expediente { get; set; } = string.Empty;
        public string Causa { get; set; } = string.Empty;
        public string FechaAudiencia { get; set; } = string.Empty;
        public string TipoAudiencia { get; set; } = string.Empty;
        public string HoraTermino { get; set; } = string.Empty;
        public string Juez { get; set; } = string.Empty;
        public string Imputado { get; set; } = string.Empty;
        public string Delito { get; set; } = string.Empty;
        public string Victima { get; set; } = string.Empty;

        // Este dato NO viene de Access.
        // Se captura manualmente al generar la etiqueta.
        public string Juzgado { get; set; } = string.Empty;
    }
}
