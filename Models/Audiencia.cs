namespace PoderJudicial.Models
{
    public class Audiencia
    {
        public int Id { get; set; }

        public DateTime? FechaAudiencia { get; set; }

        public DateTime? FechaRecibo { get; set; }

        public int? TotDiscos { get; set; }

        public string TipoDisco { get; set; }

        public string Juzgado { get; set; }

        public string TotDiscoAudiencia { get; set; }

        public string Juez { get; set; }

        public string NoCausa { get; set; }

        public string NUC { get; set; }

        public string TipoCausa { get; set; }

        public string TipoAudiencia { get; set; }

        public DateTime? HoraConclusion { get; set; }
        public string HoraConclusionTexto =>
            HoraConclusion.HasValue ? HoraConclusion.Value.ToString("hh:mm tt") : string.Empty;

        public string Imputado { get; set; }

        public string Delito { get; set; }

        public string Agraviado { get; set; }

        public string Sala { get; set; }

        public string NoCausaJuicio { get; set; }

        public string Diferida { get; set; }

        public string QuienRealiza { get; set; }

        public string TextoBusqueda { get; set; }


        public string Expediente { get; set; }

        public string Observaciones { get; set; }

        public string DiscosExternos { get; set; }

        public string EtiquetasEntregadas { get; set; }

        public string AQuienEntrega { get; set; }
    }
}
