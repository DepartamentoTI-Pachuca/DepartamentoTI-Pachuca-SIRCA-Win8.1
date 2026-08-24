using System;

namespace PoderJudicial.Models
{
    public class Ejecucion
    {
        public int Id { get; set; }
        public DateTime? FechaAudiencia { get; set; }
        public string TotalDiscos { get; set; }
        public string Juez { get; set; }
        public string ExpedienteNumero { get; set; }
        public string Causa { get; set; }
        public string TipoAudiencia { get; set; }
        public string HoraTermino { get; set; }
        public string Imputado { get; set; }
        public string Delito { get; set; }
        public string Victima { get; set; }
        public string Sala { get; set; }
        public string Observaciones { get; set; }
    }
}
