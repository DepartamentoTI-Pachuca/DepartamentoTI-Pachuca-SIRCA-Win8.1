using System.Collections.Generic;
using System.Data.OleDb;

namespace PoderJudicial.Data
{
    public class DelitoRepository : BaseRepository
    {
        public System.Collections.Generic.List<string> ObtenerDelitos()
        {
            return ConsultarColumnaHistorica("Delito");
        }
    }
}