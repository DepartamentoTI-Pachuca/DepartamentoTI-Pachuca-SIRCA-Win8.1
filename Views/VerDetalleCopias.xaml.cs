using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class VerDetalleCopias : Window
    {
        public VerDetalleCopias()
        {
            InitializeComponent();
        }

        public void CargarDatos(
            string id, string noCausa, string nuc, string tipoCausa,
            string fechaAudiencia, string fechaRecibo,
            string totalDiscosEntregados, string tipoDisco,
            string discosExternos, string etiquetasEntregadas,
            string aQuienSeEntrega, string quienRegistra, string observaciones)
        {
            TxtID.Text = id;
            TxtNoCausa.Text = noCausa;
            TxtNUC.Text = nuc;
            TxtTipoCausa.Text = tipoCausa;
            TxtFechaAudiencia.Text = fechaAudiencia;
            TxtFechaRecibo.Text = fechaRecibo;
            TxtTotalDiscosEntregados.Text = totalDiscosEntregados;
            TxtTipoDisco.Text = tipoDisco;
            TxtDiscosExternos.Text = discosExternos;
            TxtEtiquetasEntregadas.Text = etiquetasEntregadas;
            TxtAQuienSeEntrega.Text = aQuienSeEntrega;
            TxtQuienRegistra.Text = quienRegistra;
            TxtObservaciones.Text = observaciones;
        }

        // Permite arrastrar la ventana desde cualquier parte
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
