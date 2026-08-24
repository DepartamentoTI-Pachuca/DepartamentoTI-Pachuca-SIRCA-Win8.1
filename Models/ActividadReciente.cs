using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoderJudicial.Models
{
    public class ActividadReciente
    {
        // Se usa para ordenar las actividades
        public DateTime FechaHora { get; set; }

        // Se muestran automáticamente en el Dashboard
        public string Fecha => FechaHora.ToString("dd/MM/yyyy");

        public string Hora => FechaHora.TimeOfDay == TimeSpan.Zero
            ? string.Empty
            : FechaHora.ToString("hh:mm tt");

        // Emoji que identifica el módulo
        public string Icono { get; set; }

        // Ejemplo:
        // "Registro de Audiencia"
        // "Entrega de Copias"
        // "Registro de Ejecución"
        public string TipoActividad { get; set; }

        // Información importante del registro
        // Ejemplo:
        // NUC: 12345
        // Causa: 105/2026
        // Expediente: 45/2024
        public string Descripcion { get; set; }

        // Usuario que realizó la acción
        public string Usuario { get; set; }


        // NUEVO

        public string Modulo { get; set; }

        public int IdRegistro { get; set; }

        public string TablaDestino { get; set; }

        // Mismos campos que ya usa el total de discos de "Consultar
        // Registros" (ver Helpers.BuscadorRegistros.ExtraerNumero), y Sala
        // — cuando el tipo de registro no la tenga (Registro de Copias),
        // simplemente queda vacía.
        public string TotalDiscos { get; set; }

        public string Sala { get; set; }

    }
}
