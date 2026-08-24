using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class GenerarEtiqueta : Window
    {
        private readonly EtiquetaRegistroData _datos;

        public bool EtiquetaGenerada { get; private set; }
        public string ArchivoGenerado { get; private set; } = string.Empty;
        public DateTime FechaBaseModificacion { get; private set; }

        public GenerarEtiqueta(EtiquetaRegistroData datos)
        {
            InitializeComponent();

            _datos = datos ?? throw new ArgumentNullException(nameof(datos));

            CargarDatos();
        }

        private void CargarDatos()
        {
            TxtTitulo.Text =
                $"Generar etiqueta - Causa {_datos.NoCausa}";

            TxtNoCausa.Text = _datos.NoCausa;
            TxtNoCausaJuicio.Text = _datos.NoCausaJuicio;
            TxtNUC.Text = _datos.NUC;
            TxtImputado.Text = _datos.Imputado;
            TxtDelito.Text = _datos.Delito;
            TxtAgraviado.Text = _datos.Agraviado;
            TxtTipoAudiencia.Text = _datos.TipoAudiencia;
            TxtFechaAudiencia.Text = _datos.FechaAudiencia;
            TxtHoraConclusion.Text = _datos.HoraConclusion;
            TxtJuez.Text = _datos.Juez;

            bool esJO =
                TipoCausaHelper.MuestraNoCausaJuicio(
                    _datos.TipoCausa);

            LblCausaJO.Visibility =
                esJO ? Visibility.Visible : Visibility.Collapsed;

            TxtNoCausaJuicio.Visibility =
                esJO ? Visibility.Visible : Visibility.Collapsed;
        }

        private int ObtenerCantidad()
        {
            if (CmbCantidad.SelectedItem is ComboBoxItem item &&
                int.TryParse(
                    item.Content?.ToString(),
                    out int cantidad))
            {
                return cantidad;
            }

            return 1;
        }

        private void BtnGenerar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                int cantidad = ObtenerCantidad();

                string ruta =
                    EtiquetaPowerPointService
                        .GenerarArchivoTemporal(
                            _datos,
                            cantidad);

                // Se toma la fecha después de terminar de crear el archivo.
                FechaBaseModificacion =
                    File.GetLastWriteTimeUtc(ruta);

                ArchivoGenerado = ruta;
                EtiquetaGenerada = true;

                EtiquetaPowerPointService.AbrirArchivo(ruta);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No fue posible generar la etiqueta.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
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
