using System.Collections.Generic;
using System.Data.OleDb;

namespace PoderJudicial.Data
{
    public class AudienciaRepository : BaseRepository
    {
        public System.Collections.Generic.List<string> ObtenerTiposAudiencia()
        {
            return ConsultarColumnaHistorica("TipoAudiencia");
        }
    }
}