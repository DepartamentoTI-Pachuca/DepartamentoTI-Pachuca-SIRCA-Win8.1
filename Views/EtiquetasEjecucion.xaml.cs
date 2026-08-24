using PoderJudicial.Helpers;
using PoderJudicial.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoderJudicial.Views
{
    public partial class EtiquetasEjecucion : Window
    {
        private readonly EtiquetaEjecucionData _datos;
        private readonly DispatcherTimer _timerGuardado;

        private string _archivoGenerado = string.Empty;
        private DateTime _fechaBaseModificacionUtc;
        private bool _listoParaConsolidar;

        public EtiquetasEjecucion(EtiquetaEjecucionData datos)
        {
            InitializeComponent();

            _datos = datos
                ?? throw new ArgumentNullException(nameof(datos));

            EtiquetaEjecucionPowerPointService
                .LimpiarEtiquetasTemporalesAnteriores();

            TxtTitulo.Text =
                $"Etiquetas - Expediente {_datos.Expediente}";

            _timerGuardado = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _timerGuardado.Tick += TimerGuardado_Tick;
        }

        private void BtnGenerarEtiqueta_Click(
            object sender,
            RoutedEventArgs e)
        {
            var ventana =
                new GenerarEtiquetaEjecucion(_datos)
                {
                    Owner = this
                };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true &&
                ventana.EtiquetaGenerada &&
                File.Exists(ventana.ArchivoGenerado))
            {
                _archivoGenerado =
                    ventana.ArchivoGenerado;

                _fechaBaseModificacionUtc =
                    ventana.FechaBaseModificacion;

                MostrarEtiquetaGenerada();

                _timerGuardado.Start();
            }
        }

        private void MostrarEtiquetaGenerada()
        {
            PanelSinEtiqueta.Visibility =
                Visibility.Collapsed;

            PanelEtiquetaGenerada.Visibility =
                Visibility.Visible;

            TxtNombreArchivo.Text =
                Path.GetFileName(_archivoGenerado);

            TxtRutaArchivo.Text =
                _archivoGenerado;

            TxtFechaArchivo.Text =
                $"Generada: {File.GetLastWriteTime(_archivoGenerado):dd/MM/yyyy hh:mm tt}";

            EstablecerPendienteDeGuardar();
        }

        private void TimerGuardado_Tick(
            object? sender,
            EventArgs e)
        {
            if (_listoParaConsolidar ||
                string.IsNullOrWhiteSpace(_archivoGenerado) ||
                !File.Exists(_archivoGenerado))
            {
                return;
            }

            DateTime modificacionActual =
                File.GetLastWriteTimeUtc(_archivoGenerado);

            if (modificacionActual >
                _fechaBaseModificacionUtc.AddMilliseconds(500))
            {
                EstablecerListoParaConsolidar();
                _timerGuardado.Stop();
            }
        }

        private void EstablecerPendienteDeGuardar()
        {
            _listoParaConsolidar = false;
            BtnConsolidarEtiqueta.IsEnabled = false;
            BtnGuardarPrimeraEtiqueta.IsEnabled = false;

            TxtMensajePrincipal.Text =
                "✓  Etiqueta generada correctamente.";

            TxtMensajeSecundario.Text =
                "Se abrió el archivo en PowerPoint. Completa la información faltante y guarda los cambios.";

            TxtEstado.Text =
                "Pendiente de guardar";

            TxtAyudaEstado.Text =
                "Guarda el archivo en PowerPoint para poder consolidarlo.";

            BadgeEstado.Background =
                new SolidColorBrush(
                    Color.FromRgb(255, 246, 216));

            BadgeEstado.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(240, 212, 125));
        }

        private void EstablecerListoParaConsolidar()
        {
            _listoParaConsolidar = true;
            BtnConsolidarEtiqueta.IsEnabled = true;
            BtnGuardarPrimeraEtiqueta.IsEnabled = true;

            TxtMensajePrincipal.Text =
                "✓  El archivo se guardó correctamente.";

            TxtMensajeSecundario.Text =
                "Ya puedes consolidar esta etiqueta a un PowerPoint existente.";

            TxtFechaArchivo.Text =
                $"Última modificación: {File.GetLastWriteTime(_archivoGenerado):dd/MM/yyyy hh:mm tt}";

            TxtEstado.Text =
                "Listo para consolidar";

            TxtAyudaEstado.Text =
                "La etiqueta está lista para agregarse a un PowerPoint existente.";

            BadgeEstado.Background =
                new SolidColorBrush(
                    Color.FromRgb(232, 248, 237));

            BadgeEstado.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(167, 222, 183));
        }

        private void BtnAbrirEtiqueta_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                EtiquetaEjecucionPowerPointService
                    .AbrirArchivo(_archivoGenerado);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Abrir etiqueta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnConsolidarEtiqueta_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_listoParaConsolidar ||
                !File.Exists(_archivoGenerado))
            {
                return;
            }

            var ventana =
                new ConsolidarEtiquetaEjecucion(
                    _archivoGenerado)
                {
                    Owner = this
                };

            ventana.ShowDialog();
        }

        private void BtnGuardarPrimeraEtiqueta_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_listoParaConsolidar ||
                string.IsNullOrWhiteSpace(_archivoGenerado) ||
                !File.Exists(_archivoGenerado))
            {
                return;
            }

            var dialogo = new SaveFileDialog
            {
                Title = "Guardar primera etiqueta",
                Filter = "Presentación de PowerPoint (*.pptx)|*.pptx",
                DefaultExt = ".pptx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Path.GetFileName(_archivoGenerado)
            };

            if (dialogo.ShowDialog() != true)
                return;

            try
            {
                string origen = Path.GetFullPath(_archivoGenerado);
                string destino = Path.GetFullPath(dialogo.FileName);
                if (string.Equals(origen, destino,
                    StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Selecciona una ubicación definitiva diferente a la carpeta temporal.",
                        "Ubicación no válida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                File.Copy(origen, destino, true);

                MessageBox.Show(
                    "La primera etiqueta se guardó correctamente.",
                    "Etiqueta guardada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No fue posible guardar la primera etiqueta.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerGuardado?.Stop();
            base.OnClosed(e);
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
