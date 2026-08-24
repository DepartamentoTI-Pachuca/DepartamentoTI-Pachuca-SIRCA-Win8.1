using PoderJudicial.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class ConfirmarConsolidacion : Window
    {
        private readonly string _archivoOrigen;
        private readonly string _archivoDestino;

        public ConfirmarConsolidacion(
            string archivoOrigen,
            string archivoDestino)
        {
            InitializeComponent();

            _archivoOrigen =
                archivoOrigen
                ?? throw new ArgumentNullException(nameof(archivoOrigen));

            _archivoDestino =
                archivoDestino
                ?? throw new ArgumentNullException(nameof(archivoDestino));

            TxtNombreDestino.Text =
                Path.GetFileName(_archivoDestino);

            TxtRutaDestino.Text =
                _archivoDestino;

            TxtFechaDestino.Text =
                $"Última modificación: {File.GetLastWriteTime(_archivoDestino):dd/MM/yyyy hh:mm tt}";
        }

        private void BtnConsolidar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                EtiquetaPowerPointService.Consolidar(
                    _archivoOrigen,
                    _archivoDestino);

                var finalizada =
                    new ConsolidacionCompletada(
                        _archivoDestino)
                    {
                        Owner = this
                    };

                finalizada.ShowDialog();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No fue posible consolidar la etiqueta.\n\n{ex.Message}",
                    "Error de consolidación",
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
