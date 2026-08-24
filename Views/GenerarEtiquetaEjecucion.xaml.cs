using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class GenerarEtiquetaEjecucion : Window
    {
        private readonly EtiquetaEjecucionData _datos;

        public bool EtiquetaGenerada { get; private set; }
        public string ArchivoGenerado { get; private set; } = string.Empty;
        public DateTime FechaBaseModificacion { get; private set; }

        public GenerarEtiquetaEjecucion(EtiquetaEjecucionData datos)
        {
            InitializeComponent();

            _datos = datos
                ?? throw new ArgumentNullException(nameof(datos));

            CargarDatos();
        }

        private void CargarDatos()
        {
            TxtTitulo.Text =
                $"Generar etiqueta - Expediente {_datos.Expediente}";

            TxtExpediente.Text = _datos.Expediente;
            TxtCausa.Text = _datos.Causa;
            TxtImputado.Text = _datos.Imputado;
            TxtDelito.Text = _datos.Delito;
            TxtVictima.Text = _datos.Victima;
            TxtTipoAudiencia.Text = _datos.TipoAudiencia;
            TxtFechaAudiencia.Text = _datos.FechaAudiencia;
            TxtHoraTermino.Text = _datos.HoraTermino;
            TxtJuez.Text = _datos.Juez;
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
                string juzgado =
                    TxtJuzgadoManual.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(juzgado))
                {
                    MessageBox.Show(
                        "Captura el Juzgado antes de generar la etiqueta.",
                        "Dato requerido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    TxtJuzgadoManual.Focus();
                    return;
                }

                _datos.Juzgado = juzgado;

                int cantidad = ObtenerCantidad();

                string ruta =
                    EtiquetaEjecucionPowerPointService
                        .GenerarArchivoTemporal(
                            _datos,
                            cantidad);

                FechaBaseModificacion =
                    File.GetLastWriteTimeUtc(ruta);

                ArchivoGenerado = ruta;
                EtiquetaGenerada = true;

                EtiquetaEjecucionPowerPointService
                    .AbrirArchivo(ruta);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No fue posible generar la etiqueta de ejecución.\n\n{ex.Message}",
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
