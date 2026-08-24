using System.Collections.Generic;
using System.Data.OleDb;

namespace PoderJudicial.Data
{
    public class JuezRepository : BaseRepository
    {
        public System.Collections.Generic.List<string> ObtenerJueces()
        {
            return ConsultarColumnaHistorica("Juez");
        }
    }
}