using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class ConsolidarEtiqueta : Window
    {
        private readonly string _archivoOrigen;
        private string _archivoDestino = string.Empty;

        public ConsolidarEtiqueta(string archivoOrigen)
        {
            InitializeComponent();

            _archivoOrigen =
                archivoOrigen
                ?? throw new ArgumentNullException(nameof(archivoOrigen));

            TxtNombreOrigen.Text =
                Path.GetFileName(_archivoOrigen);

            TxtRutaOrigen.Text =
                _archivoOrigen;
        }

        private void BtnBuscar_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Seleccionar PowerPoint existente",
                Filter = "Presentación de PowerPoint (*.pptx)|*.pptx",
                Multiselect = false
            };

            if (dialogo.ShowDialog() != true)
                return;

            if (string.Equals(
                    Path.GetFullPath(dialogo.FileName),
                    Path.GetFullPath(_archivoOrigen),
                    StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Selecciona un PowerPoint diferente al archivo de etiqueta generado.",
                    "Archivo no válido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _archivoDestino = dialogo.FileName;

            TxtRutaDestino.Text = _archivoDestino;
            TxtNombreDestino.Text =
                Path.GetFileName(_archivoDestino);
            TxtRutaDestinoDetalle.Text =
                _archivoDestino;
            TxtFechaDestino.Text =
                $"Última modificación: {File.GetLastWriteTime(_archivoDestino):dd/MM/yyyy hh:mm tt}";

            PanelArchivoSeleccionado.Visibility =
                Visibility.Visible;

            BtnSiguiente.IsEnabled = true;
        }

        private void BtnSiguiente_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_archivoDestino) ||
                !File.Exists(_archivoDestino))
            {
                return;
            }

            var confirmar =
                new ConfirmarConsolidacion(
                    _archivoOrigen,
                    _archivoDestino)
                {
                    Owner = this
                };

            bool? resultado = confirmar.ShowDialog();

            if (resultado == true)
            {
                DialogResult = true;
                Close();
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
