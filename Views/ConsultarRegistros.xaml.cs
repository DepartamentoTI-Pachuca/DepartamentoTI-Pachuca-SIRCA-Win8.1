using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using PoderJudicial.ViewModels;
namespace PoderJudicial.Views
{
    public partial class ConsultarRegistros : Page
    {
        private DispatcherTimer _timerBusqueda;
        private ConsultarRegistrosViewModel _vm;
        private const string Placeholder = "Buscar por causa, NUC, imputado o fecha...";
        public string TablaActualSeleccionada = "";


        public ConsultarRegistros(
     string tabla, FiltroConsulta filtroInicial = null)
        {
            InitializeComponent();

            TablaActualSeleccionada =
                string.IsNullOrWhiteSpace(tabla)
                    ? "Audiencias"
                    : tabla;

            _vm =
                new ConsultarRegistrosViewModel(
                    TablaActualSeleccionada);

            DataContext = _vm;

            Loaded += ConsultarRegistros_Loaded;

            if (filtroInicial != null)
            {
                // Viene de un acceso directo (ej. tarjetas del Home): se
                // aplica de una vez y se deja el panel visible para que el
                // usuario vea con qué quedó filtrado.
                _vm.AplicarFiltroInicial(filtroInicial);
                PanelFiltrosAvanzados.Visibility = Visibility.Visible;
                BtnFiltrosAvanzados.Content = "Filtros avanzados ▴";
            }

            txtBuscar.Text = Placeholder;

            txtBuscar.Foreground =
                (Brush)Application.Current
                .Resources["SubTextBrush"];

            _timerBusqueda =
                new DispatcherTimer();

            _timerBusqueda.Interval =
                TimeSpan.FromMilliseconds(300);

            _timerBusqueda.Tick += (s, e) =>
            {
                _timerBusqueda.Stop();

                _vm.TextoBusqueda =
                    txtBuscar.Text;

                if (_vm.Sugerencias?.Count > 0)
                {
                    lstSugerencias.ItemsSource =
                        _vm.Sugerencias;

                    popupSugerencias.IsOpen = true;
                }
                else
                {
                    popupSugerencias.IsOpen = false;
                }
            };
        }


        // Evita recargar en el primer Loaded; en los siguientes Loaded
        // (ej. al regresar de Editar con
        // NavigationService.GoBack, que reutiliza esta misma instancia) sí
        // hace falta, para reflejar los cambios guardados.
        private bool _yaCargado = false;

        private async void ConsultarRegistros_Loaded(
    object sender,
    RoutedEventArgs e)
        {
            ConfigurarColumnas();
            ConfigurarFiltrosVisibles();

            if (_yaCargado)
                await _vm.RecargarDatosAsync();
            else
                await _vm.InicializarAsync();

            _yaCargado = true;
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnActualizar.IsEnabled) return;

            BtnActualizar.IsEnabled = false;
            try
            {
                await _vm.RecargarDatosAsync();
            }
            finally
            {
                if (IsLoaded) BtnActualizar.IsEnabled = true;
            }
        }

        // ── Mostrar/ocultar el panel de filtros avanzados ──
        private void BtnFiltrosAvanzados_Click(object sender, RoutedEventArgs e)
        {
            bool mostrar = PanelFiltrosAvanzados.Visibility != Visibility.Visible;
            PanelFiltrosAvanzados.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            BtnFiltrosAvanzados.Content = mostrar ? "Filtros avanzados ▴" : "Filtros avanzados ▾";
        }

        /// <summary>
        /// Oculta, dentro del panel de filtros avanzados, los campos que no
        /// aplican a la tabla que se está consultando — mismo criterio y
        /// mismo patrón (StartsWith sobre TablaActualSeleccionada) que ya
        /// usa ConfigurarColumnas() para las columnas de la grilla.
        /// </summary>
        private void ConfigurarFiltrosVisibles()
        {
            string tabla = TablaActualSeleccionada ?? "";

            bool esAudiencias = tabla.StartsWith("Audiencias ", StringComparison.OrdinalIgnoreCase);
            bool esEjecucion = tabla.StartsWith("Ejecucion", StringComparison.OrdinalIgnoreCase);
            bool esCopias = tabla.StartsWith("CopiasAudiencias", StringComparison.OrdinalIgnoreCase);

            // NUC: no existe en Ejecución.
            PanelFiltroNUC.Visibility = Vis(esAudiencias || esCopias);

            // No. Causa y Tipo Causa y Fecha: existen en las 3 tablas.
            PanelFiltroNoCausa.Visibility = Visibility.Visible;
            PanelFiltroTipoCausa.Visibility = Visibility.Visible;
            PanelFiltroFechaDesde.Visibility = Visibility.Visible;
            PanelFiltroFechaHasta.Visibility = Visibility.Visible;

            // Fecha de Recibo: solo tiene sentido como filtro en Registro de
            // Copias (ahí es lo que indica cuándo se entregó la copia).
            PanelFiltroFechaReciboDesde.Visibility = Vis(esCopias);
            PanelFiltroFechaReciboHasta.Visibility = Vis(esCopias);

            // Juzgado: solo Audiencias.
            PanelFiltroJuzgado.Visibility = Vis(esAudiencias);

            // Sala, Juez, Imputado, Delito: Audiencias y Ejecución.
            PanelFiltroSala.Visibility = Vis(esAudiencias || esEjecucion);
            PanelFiltroJuez.Visibility = Vis(esAudiencias || esEjecucion);
            PanelFiltroImputado.Visibility = Vis(esAudiencias || esEjecucion);
            PanelFiltroDelito.Visibility = Vis(esAudiencias || esEjecucion);

            // Expediente: solo Ejecución.
            PanelFiltroExpediente.Visibility = Vis(esEjecucion);

            // A Quien se Entrega: solo Copias.
            PanelFiltroAQuienEntrega.Visibility = Vis(esCopias);
        }

        private static Visibility Vis(bool mostrar)
            => mostrar ? Visibility.Visible : Visibility.Collapsed;

        // Placeholder
        private void txtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == Placeholder)
            {
                txtBuscar.Text = "";
                txtBuscar.Foreground =
      (Brush)Application.Current.Resources["TextBrush"];
            }
        }

        private void txtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Text = Placeholder;

                txtBuscar.Foreground =
                    (Brush)Application.Current.Resources["SubTextBrush"];

                _vm.TextoBusqueda = "";
            }
        }

        // Buscador
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscar.Text == Placeholder)
                return;

            _timerBusqueda.Stop();
            _timerBusqueda.Start();
        }

        private void txtBuscar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                popupSugerencias.IsOpen = false;
                return;
            }

            if (e.Key == Key.Down && popupSugerencias.IsOpen && lstSugerencias.Items.Count > 0)
            {
                lstSugerencias.Focus();
                lstSugerencias.SelectedIndex = 0;
                var item = lstSugerencias.ItemContainerGenerator
                    .ContainerFromIndex(0) as ListBoxItem;
                item?.Focus();
            }
        }

        private void lstSugerencias_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lstSugerencias.SelectedItem != null)
                AplicarSugerencia(lstSugerencias.SelectedItem.ToString());
            else if (e.Key == Key.Escape)
            {
                popupSugerencias.IsOpen = false;
                txtBuscar.Focus();
            }
        }

        private void lstSugerencias_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lstSugerencias.SelectedItem != null)
                AplicarSugerencia(lstSugerencias.SelectedItem.ToString());
        }

        private void AplicarSugerencia(string valor)
        {
            txtBuscar.Text = valor;
            txtBuscar.Foreground =
    (Brush)Application.Current.Resources["TextBrush"];
            _vm.TextoBusqueda = valor;
            popupSugerencias.IsOpen = false;
            txtBuscar.CaretIndex = txtBuscar.Text.Length;
            txtBuscar.Focus();
        }


        private void ConfigurarColumnas()
        {
            if (dgAudiencias == null)
                return;

            string tabla =
                TablaActualSeleccionada ?? "";

            foreach (DataGridColumn col in dgAudiencias.Columns)
            {
                col.Visibility =
                    Visibility.Collapsed;
            }




            // =========================
            // AUDIENCIAS
            // =========================
            if (tabla.StartsWith(
        "Audiencias ",
        StringComparison.OrdinalIgnoreCase))
            {
                MostrarColumnas(
                    "Acciones",
                    "ID",
                    "Fecha Audiencia",
                    "Fecha Recibo",
                    "Total Discos",
                    "Tipo Disco",
                    "Juzgado",
                    "Total Disco Audiencia",
                    "Juez",
                    "No. Causa",
                    "NUC",
                    "Tipo Causa",
                    "Tipo Audiencia",
                    "Hora Conclusión",
                    "Imputado",
                    "Delito",
                    "Agraviado",
                    "Sala",
                    "No. Causa Juicio",
                    "Quien Realiza"
                );
            }

            // =========================
            // EJECUCION
            // =========================
            else if (tabla.StartsWith(
              "Ejecucion",
              StringComparison.OrdinalIgnoreCase))
            {
                MostrarColumnas(
                    "Acciones",
                    "ID",
                    "Fecha Audiencia",
                    "Total Discos",
                    "Juez",
                    "Expediente",
                    "No. Causa",
                    "Tipo Audiencia",
                    "Hora Conclusión",
                    "Imputado",
                    "Delito",
                    "Agraviado",
                    "Sala",
                    "Observaciones"
                );
            }

            // =========================
            // COPIAS
            // =========================
            else if (tabla.StartsWith(
             "CopiasAudiencias",
             StringComparison.OrdinalIgnoreCase))
            {
                MostrarColumnas(
                    "Acciones",
                    "ID",
                    "Fecha Audiencia",
                    "Fecha Recibo",
                    "Total Discos",
                    "Tipo Disco",
                    "No. Causa",
                    "NUC",
                    "Tipo Causa",
                    "Discos Externos",
                    "Etiquetas Entregadas",
                    "A Quien Se Entrega",
                    "Observaciones",
                    "Quien Realiza"
                );
            }
        }


        private void MostrarColumnas(params string[] headers)
        {
            foreach (string header in headers)
            {
                var columna = dgAudiencias.Columns
                    .FirstOrDefault(c => c.Header?.ToString() == header);

                if (columna != null)
                {
                    columna.Visibility = Visibility.Visible;
                }
            }
        }


    }
}
