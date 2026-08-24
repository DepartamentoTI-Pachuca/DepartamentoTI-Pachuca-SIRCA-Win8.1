using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using PoderJudicial.ViewModels;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoderJudicial.Views
{
    public partial class ReportesView : Page
    {
        private readonly ReportesViewModel _viewModel = new ReportesViewModel();

        private List<Audiencia> _resultadosFiltrados => _viewModel.ResultadosFiltrados;
        private List<RegistroCopia> _copiasFiltradas => _viewModel.CopiasFiltradas;

        private ObservableCollection<string> _catalogoEntreganSimples => _viewModel.CatalogoEntreganSimples;
        private ObservableCollection<string> _catalogoRecibenSimples => _viewModel.CatalogoRecibenSimples;
        private ObservableCollection<string> _catalogoEntreganAutenticas => _viewModel.CatalogoEntreganAutenticas;
        private ObservableCollection<string> _catalogoRecibenAutenticas => _viewModel.CatalogoRecibenAutenticas;
        private ObservableCollection<string> _recibieronSimples => _viewModel.RecibieronSimples;
        private ObservableCollection<string> _recibieronAutenticas => _viewModel.RecibieronAutenticas;

        private DateTime FechaInforme => _viewModel.FechaInforme;
        private bool _cargando = true;
        private bool _inicializado;
        private bool _inicializando;
        private bool _consultando;
        private bool _cargandoCopiasInforme;

        public ReportesView()
        {
            InitializeComponent();

            LstRecibieronSimples.ItemsSource = _recibieronSimples;
            LstRecibieronAutenticas.ItemsSource = _recibieronAutenticas;

            CmbEntregoSimples.ItemsSource =
     _catalogoEntreganSimples;

            CmbRecibioSimples.ItemsSource =
                _catalogoRecibenSimples;

            CmbEntregoAutenticas.ItemsSource =
                _catalogoEntreganAutenticas;

            CmbRecibioAutenticas.ItemsSource =
                _catalogoRecibenAutenticas;

            Loaded += ReportesView_Loaded;
        }

        // ═══════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═══════════════════════════════════════════════════════════════


        private async void ReportesView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_inicializado || _inicializando)
                return;

            try
            {
                _inicializando = true;
                _cargando = true;
                using (RendimientoLog.IniciarModulo(
                    "Reportes", "ReportesView.Inicializar"))
                {
                    await _viewModel.InicializarDatosAsync();
                }

                // Las colecciones están enlazadas a controles WPF y deben
                // actualizarse en el hilo de interfaz.
                _viewModel.CargarCatalogosPersonas();

                ActualizarFechaInformeUI();
                using (RendimientoLog.IniciarModulo(
                    "Reportes", "ReportesView.CargarAniosDisponibles"))
                {
                    await CargarAniosDisponiblesAsync();
                }
                RestablecerOpcionesDependientes();

                _cargando = false;
                ActualizarEstadoBotones();
                _inicializado = true;
            }
            catch (Exception ex)
            {
                _cargando = false;
                MessageBox.Show(
                    $"Error al cargar reportes:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _inicializando = false;
            }
        }

        private async Task CargarAniosDisponiblesAsync()
        {
            List<string> tablas = await Task.Run(() =>
                TableDetector.TodasLasTablas.ToList());

            List<int> anios = tablas
                .SelectMany(nombre => Regex.Matches(nombre, @"\d{4}")
                    .Cast<Match>()
                    .Select(coincidencia => int.Parse(coincidencia.Value)))
                .Distinct()
                .OrderByDescending(anio => anio)
                .ToList();

            CmbAnio.Items.Clear();
            CmbAnio.Items.Add(new ComboBoxItem { Content = "Seleccione..." });
            foreach (int anio in anios)
                CmbAnio.Items.Add(new ComboBoxItem { Content = anio.ToString() });
            CmbAnio.SelectedIndex = 0;
        }

        private void RestablecerOpcionesDependientes()
        {
            CmbJuzgado.Items.Clear();
            CmbJuzgado.Items.Add(new ComboBoxItem { Content = "Todos" });
            CmbJuzgado.SelectedIndex = 0;

            CmbSala.Items.Clear();
            CmbSala.Items.Add(new ComboBoxItem { Content = "Todas" });
            CmbSala.SelectedIndex = 0;
        }

        private void ActualizarFechaInformeUI()
        {
            int anioActual = FechaInforme.Year;

            TxtFechaInformeCopias.Text =
                $"Informe del {FechaInforme:dd/MM/yyyy}";

            TxtTituloInformeAnual.Text =
                $"2. Agregar al informe anual {anioActual}";

            TxtEncabezadoEstadoAnual.Text =
                $"Estado del informe anual {anioActual}";
        }

        private void CargarCatalogosPersonas()
        {
            _viewModel.CargarCatalogosPersonas();
        }

        private void GuardarCatalogosPersonas()
        {
            _viewModel.GuardarCatalogosPersonas();
        }

        private void CargarDatos()
        {
            _viewModel.CargarDatos();
        }

        // Llenar combox audiencias
        // ═══════════════════════════════════════════════════════════════

        private void LlenarComboAnios(
            IEnumerable<Audiencia>? fuente = null,
            string? seleccionPrevia = null)
        {
            var anios = (fuente ?? _viewModel.Todas)
                .Where(a => a.FechaAudiencia.HasValue)
                .Select(a => a.FechaAudiencia!.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            CmbAnio.Items.Clear();
            CmbAnio.Items.Add(new ComboBoxItem { Content = "Todos" });

            foreach (var anio in anios)
                CmbAnio.Items.Add(new ComboBoxItem { Content = anio.ToString() });

            CmbAnio.SelectedItem = CmbAnio.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(
                    item.Content?.ToString(), seleccionPrevia,
                    StringComparison.OrdinalIgnoreCase));

            if (CmbAnio.SelectedItem == null)
                CmbAnio.SelectedIndex = 0;
        }

        private void LlenarComboJuzgados(
            string anio = "Todos",
            string? seleccionPrevia = null,
            IEnumerable<Audiencia>? fuente = null)
        {
            int? anioSeleccionado = int.TryParse(anio, out int valorAnio)
                ? valorAnio
                : null;

            IEnumerable<Audiencia> audiencias = fuente ?? _viewModel.Todas;

            if (anioSeleccionado.HasValue)
            {
                audiencias = audiencias.Where(a =>
                    a.FechaAudiencia.HasValue &&
                    a.FechaAudiencia.Value.Year == anioSeleccionado.Value);
            }

            var juzgados = audiencias
                .Select(a => a.Juzgado)
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Distinct()
                .OrderBy(j => j)
                .ToList();

            CmbJuzgado.Items.Clear();
            CmbJuzgado.Items.Add(new ComboBoxItem { Content = "Todos" });

            foreach (var j in juzgados)
                CmbJuzgado.Items.Add(new ComboBoxItem { Content = j });

            CmbJuzgado.SelectedItem = CmbJuzgado.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(
                    item.Content?.ToString(),
                    seleccionPrevia,
                    StringComparison.OrdinalIgnoreCase));

            if (CmbJuzgado.SelectedItem == null)
                CmbJuzgado.SelectedIndex = 0;
        }

        private void LlenarComboSalas(
            string anio = "Todos",
            string? seleccionPrevia = null,
            IEnumerable<Audiencia>? fuente = null)
        {
            int? anioSeleccionado = int.TryParse(anio, out int valorAnio)
                ? valorAnio
                : null;

            IEnumerable<Audiencia> audiencias = fuente ?? _viewModel.Todas;

            if (anioSeleccionado.HasValue)
            {
                audiencias = audiencias.Where(a =>
                    a.FechaAudiencia.HasValue &&
                    a.FechaAudiencia.Value.Year == anioSeleccionado.Value);
            }

            var salas = audiencias
                .Select(a => a.Sala)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            CmbSala.Items.Clear();
            CmbSala.Items.Add(new ComboBoxItem { Content = "Todas" });

            foreach (var s in salas)
                CmbSala.Items.Add(new ComboBoxItem { Content = s });

            CmbSala.SelectedItem = CmbSala.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(
                    item.Content?.ToString(),
                    seleccionPrevia,
                    StringComparison.OrdinalIgnoreCase));

            if (CmbSala.SelectedItem == null)
                CmbSala.SelectedIndex = 0;
        }

        // FILTROS DE AUDIENCIAS
        //

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_cargando)
                return;

            TxtEstadoConsulta.Text = "Filtros modificados. Pulse Aplicar filtros / Consultar.";

            if (ReferenceEquals(sender, CmbTipoCausa) &&
                ReportesViewModel.NormalizarTexto(
                    (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "") == "EXP")
            {
                CmbJuzgado.SelectedIndex = 0;
            }
        }

        private async void BtnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (_consultando)
                return;

            string anioTexto =
                (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            if (!int.TryParse(anioTexto, out int anio))
            {
                TxtEstadoConsulta.Text = "Seleccione un año antes de consultar.";
                return;
            }

            string tipoCausa =
                (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            string mes =
                (CmbMes.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            string juzgado =
                (CmbJuzgado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            string sala =
                (CmbSala.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";

            _consultando = true;
            BtnAplicarFiltros.IsEnabled = false;
            TxtEstadoConsulta.Text = "Consultando...";
            try
            {
                using (RendimientoLog.IniciarModulo(
                    "Reportes", "ReportesView.AplicarFiltros"))
                {
                    await _viewModel.ConsultarAsync(
                        anio, tipoCausa, mes, juzgado, sala);
                }

                IReadOnlyList<Audiencia> fuente =
                    _viewModel.ObtenerFuenteOpciones(tipoCausa);
                bool esExp = ReportesViewModel.NormalizarTexto(tipoCausa) == "EXP";

                _cargando = true;
                LlenarComboJuzgados(anioTexto, "Todos",
                    esExp ? Enumerable.Empty<Audiencia>() : fuente);
                LlenarComboSalas(anioTexto, "Todas", fuente);
                _cargando = false;

                AplicarFiltros();
                TxtEstadoConsulta.Text = "Consulta completada.";
            }
            catch (Exception ex)
            {
                TxtEstadoConsulta.Text = "No se pudo completar la consulta.";
                MessageBox.Show($"Error al consultar reportes:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cargando = false;
                _consultando = false;
                BtnAplicarFiltros.IsEnabled = true;
            }
        }

        private void ActualizarJuzgadosYSalasPorAnio()
        {
            string anio =
                (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";

            string? juzgadoAnterior =
                (CmbJuzgado.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string? salaAnterior =
                (CmbSala.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string tipoCausa =
                (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";
            bool esExp = ReportesViewModel.NormalizarTexto(tipoCausa) == "EXP";
            IReadOnlyList<Audiencia> fuente =
                _viewModel.ObtenerFuenteOpciones(tipoCausa);

            _cargando = true;
            try
            {
                LlenarComboJuzgados(
                    anio,
                    esExp ? "Todos" : juzgadoAnterior,
                    esExp ? Enumerable.Empty<Audiencia>() : fuente);
                LlenarComboSalas(anio, salaAnterior, fuente);
            }
            finally
            {
                _cargando = false;
            }
        }

        private void ActualizarOpcionesPorTipoCausa()
        {
            string tipoCausa =
                (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";
            string? anioAnterior =
                (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string? salaAnterior =
                (CmbSala.SelectedItem as ComboBoxItem)?.Content?.ToString();
            bool esExp = ReportesViewModel.NormalizarTexto(tipoCausa) == "EXP";
            IReadOnlyList<Audiencia> fuente =
                _viewModel.ObtenerFuenteOpciones(tipoCausa);

            _cargando = true;
            try
            {
                LlenarComboAnios(fuente, anioAnterior);
                string anio =
                    (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString()
                    ?? "Todos";

                LlenarComboJuzgados(
                    anio,
                    "Todos",
                    esExp ? Enumerable.Empty<Audiencia>() : fuente);
                LlenarComboSalas(anio, salaAnterior, fuente);
            }
            finally
            {
                _cargando = false;
            }
        }

        private void AplicarFiltros()
        {
            string mes =
                (CmbMes.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            string anio =
                (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            string juzgado =
                (CmbJuzgado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            string sala =
                (CmbSala.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";

            string tipoCausa =
                (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            _viewModel.AplicarFiltros(mes, anio, juzgado, sala, tipoCausa);

            TxtTotalRegistros.Text = _viewModel.TotalRegistros.ToString();
            TxtTotalDiscos.Text = _viewModel.TotalDiscos.ToString();
            TxtCopiasSimples.Text = _viewModel.TotalCopiasSimples.ToString();
            TxtCopiasAutenticas.Text = _viewModel.TotalCopiasAutenticas.ToString();
        }

        private static bool EsFiltroTodos(string valor)
        {
            return ReportesViewModel.EsFiltroTodos(valor);
        }
        private void AplicarFiltrosCopias()
        {
            _viewModel.AplicarFiltrosCopias();
        }



        ///Generar el nombre del archivo de reporte basado en los filtros seleccionados
        private string GenerarNombreArchivoReporte(string extension)
        {
            string tipoCausa =
                (CmbTipoCausa.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";

            string mes =
                (CmbMes.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";

            string anio =
                (CmbAnio.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";

            string juzgado =
                (CmbJuzgado.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todos";

            string sala =
                (CmbSala.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? "Todas";

            List<string> filtros = new();

            if (!EsFiltroTodos(tipoCausa))
                filtros.Add(tipoCausa);

            if (!EsFiltroTodos(mes))
                filtros.Add($"-{mes}");

            if (!EsFiltroTodos(anio))
                filtros.Add($"-{anio}");

            if (!EsFiltroTodos(juzgado))
                filtros.Add($"-{juzgado}");

            if (!EsFiltroTodos(sala))
                filtros.Add($"-{sala}");
            string fecha =
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

            string nombre;

            if (filtros.Count == 0)
            {
                nombre =
                    $"Reporte_general_de_toda_la_base_de_datos_{fecha}";
            }
            else
            {
                nombre =
                    $"Reporte_Audiencias_{string.Join("_", filtros)}_{fecha}";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '-');

            return $"{nombre}.{extension}";
        }



        // EXPORTAR EXCEL
        // ═══════════════════════════════════════════════════════════════

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            var datos = _resultadosFiltrados;

            if (datos == null || datos.Count == 0)
            {
                MessageBox.Show(
                    "No hay datos para exportar.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar Excel",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = GenerarNombreArchivoReporte("xlsx")
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                ExcelReporteHelper.ExportarAudiencias(
                    datos,
                    dlg.FileName);

                MessageBoxResult respuesta = MessageBox.Show(
                    $"Excel exportado exitosamente.\n{dlg.FileName}\n\n¿Deseas abrirlo ahora?",
                    "Éxito",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (respuesta == MessageBoxResult.Yes)
                {
                    AbrirArchivo(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // EXPORTAR PDF
        // ═══════════════════════════════════════════════════════════════

        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            var datos = _resultadosFiltrados;

            string nombreArchivo =
                GenerarNombreArchivoReporte("html");

            PdfExporter.Exportar(
                datos,
                nombreArchivo);
        }


        // ═══════════════════════════════════════════════════════════════
        // PERSONAS QUE RECIBIERON
        // ═══════════════════════════════════════════════════════════════

        private void BtnAgregarRecibioSimples_Click(
            object sender,
            RoutedEventArgs e)
        {
            AgregarPersonaRecibida(
                CmbRecibioSimples,
                _recibieronSimples,
                _catalogoRecibenSimples);
        }

        private void BtnAgregarRecibioAutenticas_Click(
            object sender,
            RoutedEventArgs e)
        {
            AgregarPersonaRecibida(
                CmbRecibioAutenticas,
                _recibieronAutenticas,
                _catalogoRecibenAutenticas);
        }

        private void AgregarPersonaRecibida(
            ComboBox combo,
            ObservableCollection<string> destino,
            ObservableCollection<string> catalogo)
        {
            string nombre =
                combo.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show(
                    "Escribe o selecciona el nombre de la persona que recibió.",
                    "Información requerida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                combo.Focus();
                return;
            }

            bool yaExiste =
                destino.Any(persona =>
                    string.Equals(
                        persona,
                        nombre,
                        StringComparison.OrdinalIgnoreCase));

            if (yaExiste)
            {
                MessageBox.Show(
                    "Esta persona ya se encuentra en la lista.",
                    "Registro duplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                combo.Focus();
                return;
            }

            // Agregar a la lista temporal de personas
            // que recibieron este informe
            destino.Add(nombre);

            // Guardar también como sugerencia
            // únicamente en su catálogo correspondiente
            AgregarAlCatalogo(
                catalogo,
                nombre);

            LimpiarCombo(combo);
            combo.Focus();

            GuardarCatalogosPersonas();
        }

        private void BtnQuitarRecibioSimples_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is string nombre)
            {
                _recibieronSimples.Remove(nombre);
            }
        }

        private void BtnQuitarRecibioAutenticas_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is string nombre)
            {
                _recibieronAutenticas.Remove(nombre);
            }
        }


        // ═══════════════════════════════════════════════════════════════
        // CATÁLOGOS
        // ═══════════════════════════════════════════════════════════════

        private static void AgregarAlCatalogo(
            ObservableCollection<string> catalogo,
            string nombre)
        {
            ReportesViewModel.AgregarAlCatalogo(catalogo, nombre);
        }

        // ═══════════════════════════════════════════════════════════════
        // GENERAR INFORME DE COPIAS SIMPLES
        // ═══════════════════════════════════════════════════════════════

        private async void BtnGenerarCopiasSimples_Click(
            object sender,
            RoutedEventArgs e)
        {
            ActualizarFechaInformeUI();
            if (!await PrepararCopiasInformeAsync())
                return;

            string entrego =
                CmbEntregoSimples.Text?.Trim() ?? string.Empty;

            if (!ValidarDatosEntrega(
                    entrego,
                    _recibieronSimples,
                    "copias simples"))
            {
                return;
            }

            List<RegistroCopia> copiasSimples =
                ObtenerCopiasSimples();

            if (copiasSimples.Count == 0)
            {
                MessageBox.Show(
                    $"No se encontraron copias simples para el día " +
                    $"{FechaInforme:dd/MM/yyyy}.\n\n" +
                    $"Copias cargadas del día: {_copiasFiltradas.Count}",
                    "Sin registros",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            Button boton = (Button)sender;
            boton.IsEnabled = false;
            try
            {
                string ruta =
                    RutasInformes.ObtenerRutaSimples(FechaInforme);

                List<string> recibieron = _recibieronSimples.ToList();
                await Task.Run(() =>
                {
                    RutasInformes.CrearEstructura();
                    WordExporter.GenerarInformeCopias(
                        copiasSimples,
                        "Copias Simples",
                        "DVD-R",
                        entrego,
                        recibieron,
                        ruta,
                        FechaInforme);
                });

                if (!File.Exists(ruta))
                {
                    MessageBox.Show(
                        $"El proceso terminó, pero el archivo no fue encontrado.\n\n" +
                        $"Ruta esperada:\n{ruta}",
                        "Archivo no encontrado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Guardar "Entregó" únicamente en el catálogo de COPIAS SIMPLES.
                AgregarAlCatalogo(
                    _catalogoEntreganSimples,
                    entrego);

                // Guardar "Recibió" únicamente en el catálogo de COPIAS SIMPLES.
                foreach (string persona in _recibieronSimples)
                {
                    AgregarAlCatalogo(
                        _catalogoRecibenSimples,
                        persona);
                }

                GuardarCatalogosPersonas();

                TxtEstadoSimples.Text =
                    $"Estado: Generado a las {DateTime.Now:hh:mm tt}";

                ActualizarEstadoBotones();

                // Limpiar solamente los campos del informe que se acaba de generar.
                LimpiarCamposSimples();

                AbrirArchivoWord(ruta);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error al generar el informe de copias simples.\n\n" +
                    $"{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // GENERAR INFORME DE COPIAS AUTÉNTICAS
        // ═══════════════════════════════════════════════════════════════

        private async void BtnGenerarCopiasAutenticas_Click(
            object sender,
            RoutedEventArgs e)
        {
            ActualizarFechaInformeUI();
            if (!await PrepararCopiasInformeAsync())
                return;

            string entrego =
                CmbEntregoAutenticas.Text?.Trim() ?? string.Empty;

            if (!ValidarDatosEntrega(
                    entrego,
                    _recibieronAutenticas,
                    "copias auténticas"))
            {
                return;
            }

            List<RegistroCopia> copiasAutenticas =
                ObtenerCopiasAutenticas();

            if (copiasAutenticas.Count == 0)
            {
                MessageBox.Show(
                    $"No se encontraron copias auténticas para el día " +
                    $"{FechaInforme:dd/MM/yyyy}.\n\n" +
                    $"Copias cargadas del día: {_copiasFiltradas.Count}",
                    "Sin registros",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            Button boton = (Button)sender;
            boton.IsEnabled = false;
            try
            {
                string ruta =
                    RutasInformes.ObtenerRutaAutenticas(FechaInforme);

                List<string> recibieron = _recibieronAutenticas.ToList();
                await Task.Run(() =>
                {
                    RutasInformes.CrearEstructura();
                    WordExporter.GenerarInformeCopias(
                        copiasAutenticas,
                        "Copias Auténticas",
                        "DVD's",
                        entrego,
                        recibieron,
                        ruta,
                        FechaInforme);
                });

                if (!File.Exists(ruta))
                {
                    MessageBox.Show(
                        $"El proceso terminó, pero el archivo no fue encontrado.\n\n" +
                        $"Ruta esperada:\n{ruta}",
                        "Archivo no encontrado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Guardar "Entregó" únicamente en el catálogo de COPIAS AUTÉNTICAS.
                AgregarAlCatalogo(
                    _catalogoEntreganAutenticas,
                    entrego);

                // Guardar "Recibió" únicamente en el catálogo de COPIAS AUTÉNTICAS.
                foreach (string persona in _recibieronAutenticas)
                {
                    AgregarAlCatalogo(
                        _catalogoRecibenAutenticas,
                        persona);
                }

                GuardarCatalogosPersonas();

                TxtEstadoAutenticas.Text =
                    $"Estado: Generado a las {DateTime.Now:hh:mm tt}";

                ActualizarEstadoBotones();

                LimpiarCamposAutenticas();

                AbrirArchivoWord(ruta);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error al generar el informe de copias auténticas.\n\n" +
                    $"{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }

        private async Task<bool> PrepararCopiasInformeAsync()
        {
            if (_cargandoCopiasInforme)
                return false;

            _cargandoCopiasInforme = true;
            try
            {
                using (RendimientoLog.IniciarModulo(
                    "Reportes", "ReportesView.CargarCopiasInforme"))
                {
                    await _viewModel.AsegurarCopiasCargadasAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudieron cargar las copias del informe:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                _cargandoCopiasInforme = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIMPIEZA DE CAMPOS DESPUÉS DE GENERAR
        // ═══════════════════════════════════════════════════════════════

        private void LimpiarCamposSimples()
        {
            LimpiarCombo(CmbEntregoSimples);
            LimpiarCombo(CmbRecibioSimples);
            _recibieronSimples.Clear();
        }

        private void LimpiarCamposAutenticas()
        {
            LimpiarCombo(CmbEntregoAutenticas);
            LimpiarCombo(CmbRecibioAutenticas);
            _recibieronAutenticas.Clear();
        }

        private static void LimpiarCombo(ComboBox combo)
        {
            combo.SelectedItem = null;
            combo.SelectedIndex = -1;
            combo.Text = string.Empty;
        }

        private static void AbrirArchivoWord(string ruta)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
        }

        // ═══════════════════════════════════════════════════════════════
        // VALIDACIONES
        // ═══════════════════════════════════════════════════════════════

        private static bool ValidarDatosEntrega(
            string entrego,
            ObservableCollection<string> recibieron,
            string tipoInforme)
        {
            if (string.IsNullOrWhiteSpace(entrego))
            {
                MessageBox.Show(
                    $"Indica quién entregó el informe de {tipoInforme}.",
                    "Información requerida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (recibieron.Count == 0)
            {
                MessageBox.Show(
                    $"Agrega al menos una persona que recibió el informe de {tipoInforme}.",
                    "Información requerida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // SEPARAR COPIAS POR TIPO
        // ═══════════════════════════════════════════════════════════════

        private List<RegistroCopia> ObtenerCopiasSimples()
        {
            return _viewModel.ObtenerCopiasSimples();
        }

        private List<RegistroCopia> ObtenerCopiasAutenticas()
        {
            return _viewModel.ObtenerCopiasAutenticas();
        }





        private static string NormalizarTexto(string valor)
        {
            return ReportesViewModel.NormalizarTexto(valor);
        }

        // ═══════════════════════════════════════════════════════════════
        // ESTADOS
        // ═══════════════════════════════════════════════════════════════

        private void ActualizarEstadoBotones()
        {
            EstadoInformesResultado estado =
                EstadoInformesHelper.ObtenerEstado(FechaInforme);

            TxtEstadoSimples.Text = estado.EstadoSimples;
            TxtEstadoAutenticas.Text = estado.EstadoAutenticas;
            BtnConsolidarInformeDiario.IsEnabled = estado.PuedeConsolidar;
            BtnAgregarInformeAnual.IsEnabled = estado.PuedeAgregarInformeAnual;
            TxtEstadoConsolidado.Text = estado.EstadoConsolidado;
            TxtEstadoInformeAnual.Text = estado.EstadoInformeAnual;
            TxtNombreArchivoAnual.Text = estado.NombreArchivoAnual;
            TxtUltimaActualizacionAnual.Text = estado.UltimaActualizacionAnual;
        }


        // ═══════════════════════════════════════════════════════════════
        // CONSOLIDACIÓN 
       

        private async void BtnAgregarInformeAnual_Click(
     object sender,
     RoutedEventArgs e)
        {
            BtnAgregarInformeAnual.IsEnabled = false;
            try
            {
                string rutaConsolidado =
                    RutasInformes.ObtenerRutaConsolidado(
                        FechaInforme);

                if (!File.Exists(rutaConsolidado))
                {
                    MessageBox.Show(
                        "Primero debes consolidar los informes de copias simples y auténticas.",
                        "Consolidación requerida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    ActualizarEstadoBotones();
                    return;
                }

                bool yaExiste = await Task.Run(() =>
                    InformeCopiasService.EstaAgregadoAlAnual(FechaInforme));

                MessageBoxResult confirmar =
                    MessageBox.Show(
                        yaExiste
                            ? $"El informe del {FechaInforme:dd/MM/yyyy} ya existe en Informes_{FechaInforme.Year}.docx.\n\n" +
                              "Se reemplazará únicamente la versión de este día por la versión más reciente.\n\n" +
                              "¿Deseas continuar?"
                            : $"El informe del {FechaInforme:dd/MM/yyyy} se agregará a Informes_{FechaInforme.Year}.docx.\n\n" +
                              "¿Deseas continuar?",
                        yaExiste
                            ? "Actualizar informe anual"
                            : "Agregar al informe anual",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (confirmar != MessageBoxResult.Yes)
                    return;

                var resultado = await Task.Run(() =>
                {
                    string ruta = InformeCopiasService
                        .AgregarOActualizarInformeAnual(FechaInforme);
                    int total = InformeCopiasService
                        .ContarInformesEnAnual(FechaInforme.Year);
                    return (ruta, total);
                });

                // Actualiza todos los estados desde un solo lugar
                ActualizarEstadoBotones();

                int totalInformes = resultado.total;

                MessageBox.Show(
                    yaExiste
                        ? $"El informe del {FechaInforme:dd/MM/yyyy} fue actualizado correctamente.\n\n" +
                          $"Informes registrados en {FechaInforme.Year}: {totalInformes}."
                        : $"El informe del {FechaInforme:dd/MM/yyyy} fue agregado correctamente.\n\n" +
                          $"Informes registrados en {FechaInforme.Year}: {totalInformes}.",
                    "Informe anual actualizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AbrirArchivo(resultado.ruta);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"No fue posible actualizar Informes_{FechaInforme.Year}.docx.\n\n" +
                    "Si el documento anual está abierto en Word, ciérralo e inténtalo nuevamente.\n\n" +
                    ex.Message,
                    "Informe anual en uso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error al actualizar el informe anual.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ActualizarEstadoBotones();
            }
        }



        private async void BtnConsolidarInformeDiario_Click(
    object sender,
    RoutedEventArgs e)
        {
            BtnConsolidarInformeDiario.IsEnabled = false;
            try
            {
                string rutaSimples =
                    RutasInformes.ObtenerRutaSimples(
                        DateTime.Today);

                string rutaAutenticas =
                    RutasInformes.ObtenerRutaAutenticas(
                        DateTime.Today);

                if (!File.Exists(rutaSimples) ||
                    !File.Exists(rutaAutenticas))
                {
                    MessageBox.Show(
                        "Para consolidar primero deben existir los dos informes del día.",
                        "No se puede consolidar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                string rutaConsolidado = await Task.Run(() =>
                    InformeCopiasService.ConsolidarInformeDelDia(DateTime.Today));

                ActualizarEstadoBotones();

                MessageBoxResult respuesta =
                    MessageBox.Show(
                        "Los informes se consolidaron correctamente.\n\n" +
                        "¿Deseas abrir el documento consolidado?",
                        "Consolidación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                if (respuesta == MessageBoxResult.Yes)
                {
                    AbrirArchivo(rutaConsolidado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al consolidar:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ActualizarEstadoBotones();
            }
        }
        private static void AbrirArchivo(string ruta)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
        }

    }
}
