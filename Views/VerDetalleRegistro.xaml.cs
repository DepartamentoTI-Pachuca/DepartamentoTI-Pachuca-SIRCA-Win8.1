using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class VerDetalleRegistro : Window
    {
        public VerDetalleRegistro()
        {
            InitializeComponent();
        }

        public void CargarDatos(
            string Id,
            string noCausa,
            string nuc,
            string fechaAudiencia,
            string fechaRecibo,
            string horaConclusion,
            string tipoAudiencia,
            string tipoCausa,
            string juzgado,
            string juez,
            string sala,
            string totalDiscos,
            string tipoDisco,
            string totalDiscoAudiencia,
            string imputado,
            string delito,
            string agraviado,
            string noCausaJuicio,
            string diferida,
            string quienRealiza)
        {
            TxtID.Text = Id;
            TxtNoCausa.Text = noCausa;
            TxtNUC.Text = nuc;
            TxtFechaAudiencia.Text = fechaAudiencia;
            TxtFechaRecibo.Text = fechaRecibo;
            TxtHoraConclusion.Text = horaConclusion;
            TxtTipoAudiencia.Text = tipoAudiencia;
            TxtTipoCausa.Text = tipoCausa;
            TxtJuzgado.Text = juzgado;
            TxtJuez.Text = juez;
            TxtSala.Text = sala;
            TxtTotalDiscos.Text = totalDiscos;
            TxtTipoDisco.Text = tipoDisco;
            TxtTotalDiscoAudiencia.Text = totalDiscoAudiencia;
            TxtImputado.Text = imputado;
            TxtDelito.Text = delito;
            TxtAgraviado.Text = agraviado;
            TxtNoCausaJuicio.Text = noCausaJuicio;

            PanelNoCausaJuicioDetalle.Visibility =
                TipoCausaHelper.MuestraNoCausaJuicio(tipoCausa)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            TxtQuienRealiza.Text = quienRealiza;
        }

        private void BtnEtiquetas_Click(
            object sender,
            RoutedEventArgs e)
        {
            var datos = new EtiquetaRegistroData
            {
                TipoCausa = TxtTipoCausa.Text,
                NoCausa = TxtNoCausa.Text,
                NoCausaJuicio = TxtNoCausaJuicio.Text,
                NUC = TxtNUC.Text,
                Imputado = TxtImputado.Text,
                Delito = TxtDelito.Text,
                Agraviado = TxtAgraviado.Text,
                TipoAudiencia = TxtTipoAudiencia.Text,
                FechaAudiencia = TxtFechaAudiencia.Text,
                HoraConclusion = TxtHoraConclusion.Text,
                Juez = TxtJuez.Text
            };

            var ventana = new Etiquetas(datos)
            {
                Owner = this
            };

            ventana.ShowDialog();
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(
            MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
