using PoderJudicial.Models;
using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class VerDetalleEjecucion : Window
    {
        public VerDetalleEjecucion()
        {
            InitializeComponent();
        }

        public void CargarDatos(
            string id,
            string expediente,
            string causa,
            string fechaAudiencia,
            string tipoAudiencia,
            string horaTermino,
            string juez,
            string sala,
            string imputado,
            string delito,
            string victima,
            string totalDiscos,
            string observaciones)
        {
            TxtID.Text = id;
            TxtExpediente.Text = expediente;
            TxtCausa.Text = causa;
            TxtFechaAudiencia.Text = fechaAudiencia;
            TxtTipoAudiencia.Text = tipoAudiencia;
            TxtHoraTermino.Text = horaTermino;
            TxtJuez.Text = juez;
            TxtSala.Text = sala;
            TxtImputado.Text = imputado;
            TxtDelito.Text = delito;
            TxtVictima.Text = victima;
            TxtTotalDiscos.Text = totalDiscos;
            TxtObservaciones.Text = observaciones;
        }

        private void BtnEtiquetas_Click(
            object sender,
            RoutedEventArgs e)
        {
            var datos = new EtiquetaEjecucionData
            {
                Expediente = TxtExpediente.Text,
                Causa = TxtCausa.Text,
                FechaAudiencia = TxtFechaAudiencia.Text,
                TipoAudiencia = TxtTipoAudiencia.Text,
                HoraTermino = TxtHoraTermino.Text,
                Juez = TxtJuez.Text,
                Imputado = TxtImputado.Text,
                Delito = TxtDelito.Text,
                Victima = TxtVictima.Text
            };

            var ventana =
                new EtiquetasEjecucion(datos)
                {
                    Owner = this
                };

            ventana.ShowDialog();
        }

        protected override void OnMouseLeftButtonDown(
            MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
