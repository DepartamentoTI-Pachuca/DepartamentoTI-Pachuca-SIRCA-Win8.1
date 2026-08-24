using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using PoderJudicial.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading.Tasks;


namespace PoderJudicial.Views
{
    /// <summary>
    /// Formulario de "Registro de Copias". Es un UserControl (igual que
    /// AudienciaFormControl para Audiencias/Ejecución) para que "crear" y
    /// "editar" compartan exactamente los mismos controles, validaciones,
    /// autocompletados y lógica de No. Causa → NUC/Tipo Causa/Fecha
    /// Audiencia, sin duplicar código entre ambos flujos.
    /// </summary>
    public partial class CopiasFormControl : UserControl
    {
        // ──────────────────────────────────────────
        //  CAMPOS PRIVADOS
        // ──────────────────────────────────────────
        internal readonly RegistroCopiasViewModel VM;
        private DispatcherTimer _timer;

        private bool _esEdicion = false;
        private int _idEdicion;
        private bool _cargaInicialIniciada;
        private bool _busquedaCausaEnCurso;

        /// <summary>Se dispara cuando el usuario presiona "Guardar Registro".</summary>
        public event EventHandler GuardarClick;

        // ──────────────────────────────────────────
        //  CONSTRUCTOR
        // ──────────────────────────────────────────
        public CopiasFormControl()
        {
            InitializeComponent();

            VM = new RegistroCopiasViewModel();
            DataContext = VM;

            TxtId.Text = "---";
            Loaded += CopiasFormControl_Loaded;
            IniciarReloj();
            RegistrarPlaceholders();
        }

        private async void CopiasFormControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cargaInicialIniciada)
                return;

            _cargaInicialIniciada = true;
            try
            {
                await VM.CargarHistorialAsync();
                if (!_esEdicion && IsLoaded)
                    TxtId.Text = VM.SiguienteIdInicial.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudieron cargar los datos auxiliares de copias:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ──────────────────────────────────────────
        //  ID VISUAL
        // ──────────────────────────────────────────
        private async Task CargarIdVisualAsync()
        {
            // En modo edición el Id ya es el del registro real (ver
            // CargarParaEditar) y no debe reemplazarse por el "siguiente
            // folio disponible".
            if (_esEdicion || !IsLoaded) return;

            try
            {
                int id = await Task.Run(() =>
                {
                    using var medicion = RendimientoLog.IniciarModulo(
                        "Registro Copias", "CopiasFormControl.CargarIdVisual");
                    return new CopiasData().ObtenerSiguienteIdVisual();
                });

                if (!_esEdicion && IsLoaded)
                    TxtId.Text = id.ToString();
            }
            catch
            {
                if (!_esEdicion && IsLoaded)
                    TxtId.Text = "---";
            }
        }

        // ──────────────────────────────────────────
        //  RELOJ
        // ──────────────────────────────────────────
        private void IniciarReloj()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            ActualizarFechaHora();
        }

        private void Timer_Tick(object? sender, EventArgs e)
            => ActualizarFechaHora();

        private void ActualizarFechaHora()
        {
            DateTime ahora = DateTime.Now;
            CultureInfo cultura = new CultureInfo("es-MX");

            TxtHora.Text = ahora.ToString("hh:mm tt");
            TxtFecha.Text = ahora.ToString("dddd, dd MMMM yyyy", cultura);

            // Fecha recibo es automática (solo lectura) — pero solo al
            // capturar un registro nuevo. En modo edición ya viene del
            // registro original (ver CargarParaEditar) y no debe cambiar
            // sola con cada tick del reloj.
            if (_esEdicion) return;

            TxtFeRecibo.Text = ahora.ToString("dd/MM/yyyy");
        }

        /// <summary>Detiene el reloj interno. El host debe llamarlo si descarta el control.</summary>
        internal void DetenerReloj() => _timer?.Stop();


        //  PLACEHOLDERS

        private void RegistrarPlaceholders()
        {
            PlaceholderHelper.AddPlaceholder(TxtId);
            PlaceholderHelper.AddPlaceholder(TxtFeAudiencia, "Se completa según No. Causa");
            PlaceholderHelper.AddPlaceholder(TxtFeRecibo, "dd/MM/yyyy");
            PlaceholderHelper.AddPlaceholder(TxtNoCausa, "Ej: 123/2024");
            PlaceholderHelper.AddPlaceholder(TxtNUC, "Se completa según No. Causa");
            PlaceholderHelper.AddPlaceholder(TxtTipoCausa, "Se completa según No. Causa");

            PlaceholderHelper.AddPlaceholder(TxtAQuienSeEntrega, "Nombre de quien recibe");
            PlaceholderHelper.AddPlaceholder(TxtObservaciones, "Escriba observaciones adicionales...");
        }

        // ── Excepción por registro: permitir letras si el usuario lo confirma ─
        private bool _permitirLetrasNoCausa = false;

        private void NoCausa_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !ValidationHelper.EvaluarCaracterConExcepcion(e.Text, c => char.IsDigit(c) || c == '/',
                ref _permitirLetrasNoCausa, "No. Causa");

        // ── Autocomplete "A quien se entrega" (misma infraestructura que
        //    Delito/Juez/Tipo Audiencia en Nueva Audiencia) ─────────────
        private void TxtAQuienSeEntrega_TextChanged(object sender, TextChangedEventArgs e)
            => AutocompleteHelper.FiltrarDesdeSender(sender, VM.AQuienSeEntregaHistorial);

        private void TxtAutocomplete_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            ListBox lst = ((StackPanel)txt.Parent).Children.OfType<ListBox>().First();
            AutocompleteHelper.ManejarTecladoTextBox(txt, lst, e);
        }

        private void lstAutocomplete_PreviewKeyDown(object sender, KeyEventArgs e)
            => AutocompleteHelper.ManejarTecladoListBox((ListBox)sender, e);

        private void lstAutocomplete_MouseClick(object sender, MouseButtonEventArgs e)
            => AutocompleteHelper.ManejarClickMouse((ListBox)sender);

        //  GUARDAR (el clic solo avisa al host; ver RegistroCopias.xaml.cs /
        //  EditarCopias.xaml.cs, que llaman Validar()/ConstruirModelo()/
        //  PersistirModelo() — así crear y editar comparten toda esta lógica)

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
            => GuardarClick?.Invoke(this, EventArgs.Empty);

        // ── Validación (expuesta para el host) ─────────────
        public bool Validar(out string mensajeError)
        {
            mensajeError = null;

            if (string.IsNullOrWhiteSpace(TxtId.Text) || TxtId.Text == "---")
                return Falla(out mensajeError, "El campo 'Id' es obligatorio.");

            if (!ValidationHelper.FechaValida(ObtenerTexto(TxtFeAudiencia)))
                return Falla(out mensajeError, "La fecha de audiencia no pudo determinarse; verifique el No. Causa capturado.");

            if (CmbTotDiscosEntregados.SelectedIndex == 0)
                return Falla(out mensajeError, "Debe seleccionar el total de discos entregados.");

            string noCausa = ObtenerTexto(TxtNoCausa);
            if (string.IsNullOrWhiteSpace(noCausa))
                return Falla(out mensajeError, "El campo 'No. Causa' es obligatorio.");

            if (!ValidationHelper.NumerosYDiagonalConExcepcion(noCausa, _permitirLetrasNoCausa))
                return Falla(out mensajeError, "El campo 'No. Causa' solo permite números y '/'.");

            string nuc = ObtenerTexto(TxtNUC);
            if (string.IsNullOrWhiteSpace(nuc))
                return Falla(out mensajeError, "El campo 'NUC' no pudo determinarse; verifique el No. Causa capturado.");

            

            if (CmbTipoDisco.SelectedIndex == 0)
                return Falla(out mensajeError, "Debe seleccionar el tipo de disco.");

            if (string.IsNullOrWhiteSpace(ObtenerTexto(TxtTipoCausa)))
                return Falla(out mensajeError, "El campo 'Tipo Causa' no pudo determinarse; verifique el No. Causa capturado.");

            if (string.IsNullOrWhiteSpace(ObtenerTexto(TxtAQuienSeEntrega)))
                return Falla(out mensajeError, "El campo 'A Quien se Entrega' es obligatorio.");

            return true;
        }

        private static bool Falla(out string mensajeError, string mensaje)
        {
            mensajeError = mensaje;
            return false;
        }

        // ── Construcción del modelo (expuesta para el host) ─
        public RegistroCopia ConstruirModelo()
        {
            DateTime? fechaAudiencia = DateTime.TryParseExact(
                    ObtenerTexto(TxtFeAudiencia), "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fAud)
                ? fAud : (DateTime?)null;

            DateTime? fechaRecibo = DateTime.TryParseExact(
                    TxtFeRecibo.Text, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fRec)
                ? fRec : (DateTime?)null;

            int? totDiscos = int.TryParse(
                ObtenerValorCombo(CmbTotDiscosEntregados).Split(' ')[0], out int discos)
                ? discos : (int?)null;

            int id = _esEdicion ? _idEdicion
                : (int.TryParse(TxtId.Text, out int idNuevo) ? idNuevo : 0);

            return new RegistroCopia
            {
                Id = id,
                FeAudiencia = fechaAudiencia,
                FeRecibo = fechaRecibo,
                TotDiscosEntregados = totDiscos,
                TipoDisco = ObtenerValorCombo(CmbTipoDisco),
                NoCausa = ObtenerTexto(TxtNoCausa),
                NUC = ObtenerTexto(TxtNUC),
                TipoCausa = ObtenerTexto(TxtTipoCausa),
                DiscosExternos = ObtenerValorCombo(CmbDiscosExternos),
                EtiquetasEntregadas = ObtenerValorCombo(CmbEtiquetasEntregadas),
                AQuienSeEntrega = ObtenerTexto(TxtAQuienSeEntrega),
                Observaciones = ObtenerTexto(TxtObservaciones),
                QuienRegistra = ModalidadCopiaHelper.ConstruirRegistro(SesionActual.Usuario, EsGrabadoDirecto())
            };
        }

        // ── ¿Se grabó directo? (afecta "Quien Realiza", mismo patrón que
        //    Videoconferencia en AudienciaFormControl) ────────────────────
        private bool EsGrabadoDirecto()
            => ObtenerValorCombo(CmbGrabadoDirecto) == "Sí";

        // ── Persistencia (expuesta para el host): INSERT o UPDATE ──
        public void PersistirModelo(RegistroCopia registro)
        {
            if (_esEdicion) new CopiasData().Actualizar(registro);
            else new CopiasData().Insertar(registro);
        }

        /// <summary>Solo para el flujo de creación: limpia y prepara un folio nuevo.</summary>
        public void PrepararSiguienteRegistro()
        {
            LimpiarFormulario();
            _ = CargarIdVisualAsync();
        }

        // ──────────────────────────────────────────
        //  VALIDACIÓN (compatibilidad interna; ya no se usa desde
        //  BtnGuardar_Click, ver Validar() público arriba)
        // ──────────────────────────────────────────

        // ──────────────────────────────────────────
        //  LIMPIAR FORMULARIO
        // ──────────────────────────────────────────
        private void LimpiarFormulario()
        {
            TxtId.Text = string.Empty;
            TxtFeAudiencia.Text = string.Empty;

            TxtNoCausa.Text = string.Empty;
            TxtNUC.Text = string.Empty;
            TxtTipoCausa.Text = string.Empty;
            TxtAQuienSeEntrega.Text = string.Empty;
            TxtObservaciones.Text = string.Empty;
            CmbTipoDisco.SelectedIndex = 0;
            CmbTotDiscosEntregados.SelectedIndex = 0;

            CmbDiscosExternos.SelectedIndex = 0;
            CmbEtiquetasEntregadas.SelectedIndex = 0;
            CmbGrabadoDirecto.SelectedIndex = 0;

            OcultarComboFechas();

            _permitirLetrasNoCausa = false;

            RegistrarPlaceholders();
        }

        // ──────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────
        private string ObtenerTexto(TextBox txt)
        {
            if (txt == null) return string.Empty;

            if (PlaceholderHelper.IsPlaceholder(txt))
                return string.Empty;

            string[] placeholders =
            {
                "dd/MM/yyyy",
                "Ej: 123/2024",
                "Se completa según No. Causa",
                "Nombre de quien recibe",
                "Escriba observaciones adicionales..."
            };

            string texto = txt.Text?.Trim() ?? string.Empty;
            return placeholders.Contains(texto) ? string.Empty : texto;
        }

        private string ObtenerValorCombo(ComboBox combo)
        {
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return string.Empty;

            string content = item.Content?.ToString() ?? string.Empty;
            return content.StartsWith("Seleccione") || content == "Ninguno" || content == "Ninguna"
                ? string.Empty
                : content;
        }


        private async void TxtNoCausa_LostFocus(
    object sender,
    RoutedEventArgs e)
        {
            string causa =
                ObtenerTexto(TxtNoCausa);

            if (string.IsNullOrWhiteSpace(causa))
                return;

            if (_busquedaCausaEnCurso)
                return;

            _busquedaCausaEnCurso = true;

            try
            {
                using var medicion = RendimientoLog.IniciarModulo(
                    "Registro Copias", "CopiasFormControl.BuscarNoCausa");
                using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                    "RegistroCopias.BuscarNoCausa");

                if (!IsLoaded)
                    return;

                var datos = await Task.Run(() =>
                {
                    string nuc = new AudienciaData()
                        .ObtenerDatosPorNoCausa(causa, out string tipoCausa, out _);
                    List<DateTime> fechas = new AudienciaData()
                        .ObtenerFechasAudienciaPorNoCausa(causa);
                    return (Nuc: nuc, TipoCausa: tipoCausa, Fechas: fechas);
                });

                if (!IsLoaded || !string.Equals(causa, ObtenerTexto(TxtNoCausa),
                    StringComparison.OrdinalIgnoreCase))
                    return;

            // NUC y Tipo Causa: no cambian entre audiencias de la misma
            // causa, así que basta con la primera coincidencia (sin tocar).
            string nuc = datos.Nuc;
            string tipoCausa = datos.TipoCausa;

            var brushNormal = (Brush)Application.Current.Resources["PrimaryTextBrush"];

            if (!string.IsNullOrWhiteSpace(nuc))
            {
                TxtNUC.Text = nuc;
                TxtNUC.Foreground = brushNormal;
            }

            if (!string.IsNullOrWhiteSpace(tipoCausa))
            {
                TxtTipoCausa.Text = tipoCausa;
                TxtTipoCausa.Foreground = brushNormal;
            }

            // Fecha Audiencia: un mismo No. Causa puede tener varias
            // audiencias (varios discos originales) en fechas distintas —
            // hay que revisarlas TODAS, no solo la primera que aparezca.
            List<DateTime> fechas = datos.Fechas;

            if (fechas.Count == 0)
            {
                // No hay ninguna audiencia asociada: se limpia y se avisa,
                // en vez de dejar una fecha incorrecta o de una búsqueda anterior.
                TxtFeAudiencia.Text = string.Empty;
                OcultarComboFechas();

                MessageBox.Show(
                    "No se encontraron audiencias registradas para este No. Causa.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (fechas.Count == 1)
            {
                // Único disco original posible: se autocompleta como antes.
                TxtFeAudiencia.Text = fechas[0].ToString("dd/MM/yyyy");
                TxtFeAudiencia.Foreground = brushNormal;
                OcultarComboFechas();
            }
            else
            {
                // Varios discos originales posibles: el usuario elige a cuál
                // corresponde la copia. Se deja sin selección para forzar
                // una elección explícita (la validación ya exige que
                // Fecha Audiencia no quede vacía al guardar).
                CmbFeAudienciaOpciones.ItemsSource =
                    fechas.Select(f => f.ToString("dd/MM/yyyy")).ToList();
                CmbFeAudienciaOpciones.SelectedIndex = -1;

                TxtFeAudiencia.Text = string.Empty;
                TxtFeAudiencia.Visibility = Visibility.Collapsed;
                CmbFeAudienciaOpciones.Visibility = Visibility.Visible;
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudieron consultar los datos de la causa:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _busquedaCausaEnCurso = false;
            }
        }

        private void CmbFeAudienciaOpciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // El ComboBox solo decide cuál fecha usar; el resto del formulario
            // (validación, ConstruirModelo, etc.) sigue leyendo TxtFeAudiencia
            // sin cambios, así no se duplica esa lógica.
            if (CmbFeAudienciaOpciones.SelectedItem is string fechaTexto)
            {
                TxtFeAudiencia.Text = fechaTexto;
                TxtFeAudiencia.Foreground = (Brush)Application.Current.Resources["PrimaryTextBrush"];
            }
        }

        private void OcultarComboFechas()
        {
            CmbFeAudienciaOpciones.Visibility = Visibility.Collapsed;
            CmbFeAudienciaOpciones.ItemsSource = null;
            TxtFeAudiencia.Visibility = Visibility.Visible;
        }

        // ══════════════════════════════════════════════
        //  MODO EDICIÓN
        // ══════════════════════════════════════════════
        public void CargarParaEditar(RegistroCopia registro)
        {
            _esEdicion = true;
            _idEdicion = registro.Id;

            var brushNormal = (Brush)Application.Current.Resources["PrimaryTextBrush"];

            TxtId.Text = registro.Id.ToString();

            EstablecerTexto(TxtNoCausa, registro.NoCausa, brushNormal);
            EstablecerTexto(TxtNUC, registro.NUC, brushNormal);
            EstablecerTexto(TxtTipoCausa, registro.TipoCausa, brushNormal);

            if (registro.FeAudiencia.HasValue)
                EstablecerTexto(TxtFeAudiencia, registro.FeAudiencia.Value.ToString("dd/MM/yyyy"), brushNormal);

            // Al editar ya se conoce la fecha exacta que se usó para esta
            // copia — no hace falta volver a preguntar entre varias.
            OcultarComboFechas();

            if (registro.FeRecibo.HasValue)
                TxtFeRecibo.Text = registro.FeRecibo.Value.ToString("dd/MM/yyyy");

            SeleccionarComboPorTexto(CmbTotDiscosEntregados,
                registro.TotDiscosEntregados.HasValue ? $"{registro.TotDiscosEntregados} disco{(registro.TotDiscosEntregados == 1 ? "" : "s")}" : null);
            SeleccionarComboPorTexto(CmbTipoDisco, registro.TipoDisco);
            SeleccionarComboPorTexto(CmbDiscosExternos, registro.DiscosExternos);
            SeleccionarComboPorTexto(CmbEtiquetasEntregadas, registro.EtiquetasEntregadas);

            EstablecerTexto(TxtAQuienSeEntrega, registro.AQuienSeEntrega, brushNormal);
            EstablecerTexto(TxtObservaciones, registro.Observaciones, brushNormal);

            CmbGrabadoDirecto.SelectedIndex =
                ModalidadCopiaHelper.SeGraboDirecto(registro.QuienRegistra) ? 1 : 0;

            BtnGuardar.Content = "Guardar Cambios";
        }

        private static void EstablecerTexto(TextBox txt, string valor, Brush brushNormal)
        {
            txt.Text = valor ?? string.Empty;
            txt.Foreground = brushNormal;
        }

        private static void SeleccionarComboPorTexto(ComboBox combo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;

            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), valor, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }
    }
}
