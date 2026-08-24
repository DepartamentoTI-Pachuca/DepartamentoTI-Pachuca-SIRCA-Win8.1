using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using PoderJudicial.Views;

namespace PoderJudicial.ViewModels
{
    public class ConsultarRegistrosViewModel : BaseViewModel
    {

        private List<Audiencia> _listaCompleta = new List<Audiencia>();
        private DispatcherTimer _reloj;
        private bool _accionRegistroEnCurso;
        private Task? _cargaDatosActual;
        private string _tablaActual;

        // Caché de listados completos (Id + total de discos real de cada
        // tabla), usados solo para los indicadores — se piden una vez por
        // página, no en cada tecla que el usuario escribe al buscar.
        private List<Ejecucion>? _cacheEjecuciones;
        private List<RegistroCopia>? _cacheCopias;


        //  PROPIEDADES

        private ObservableCollection<Audiencia> _audiencias;
        public ObservableCollection<Audiencia> Audiencias
        {
            get => _audiencias;
            set { _audiencias = value; OnPropertyChanged(); }
        }

        private List<string> _sugerencias;
        public List<string> Sugerencias
        {
            get => _sugerencias;
            set { _sugerencias = value; OnPropertyChanged(); }
        }

        private string _textoBusqueda = "";
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                Filtrar();
                ActualizarSugerencias();
            }
        }

        private string _totalRegistros;

        private string _totalDiscosBusqueda;
        public string TotalDiscosBusqueda
        {
            get => _totalDiscosBusqueda;
            set
            {
                _totalDiscosBusqueda = value;
                OnPropertyChanged();
            }
        }

        public string TotalRegistros
        {
            get => _totalRegistros;
            set { _totalRegistros = value; OnPropertyChanged(); }
        }

        private string _hora;
        public string Hora
        {
            get => _hora;
            set { _hora = value; OnPropertyChanged(); }
        }

        private string _fecha;
        public string Fecha
        {
            get => _fecha;
            set { _fecha = value; OnPropertyChanged(); }
        }

        // ══════════════════════════════════════════════
        //  FILTROS AVANZADOS
        // ══════════════════════════════════════════════
        private readonly FiltroConsulta _filtroActivo = new FiltroConsulta();

        private string _filtroNUC;
        public string FiltroNUC
        {
            get => _filtroNUC;
            set { _filtroNUC = value; OnPropertyChanged(); }
        }

        private string _filtroNoCausa;
        public string FiltroNoCausa
        {
            get => _filtroNoCausa;
            set { _filtroNoCausa = value; OnPropertyChanged(); }
        }

        private DateTime? _filtroFechaDesde;
        public DateTime? FiltroFechaDesde
        {
            get => _filtroFechaDesde;
            set { _filtroFechaDesde = value; OnPropertyChanged(); }
        }

        private DateTime? _filtroFechaHasta;
        public DateTime? FiltroFechaHasta
        {
            get => _filtroFechaHasta;
            set { _filtroFechaHasta = value; OnPropertyChanged(); }
        }

        private DateTime? _filtroFechaReciboDesde;
        public DateTime? FiltroFechaReciboDesde
        {
            get => _filtroFechaReciboDesde;
            set { _filtroFechaReciboDesde = value; OnPropertyChanged(); }
        }

        private DateTime? _filtroFechaReciboHasta;
        public DateTime? FiltroFechaReciboHasta
        {
            get => _filtroFechaReciboHasta;
            set { _filtroFechaReciboHasta = value; OnPropertyChanged(); }
        }

        private string _filtroTipoCausa;
        public string FiltroTipoCausa
        {
            get => _filtroTipoCausa;
            set { _filtroTipoCausa = value; OnPropertyChanged(); }
        }

        private string _filtroJuzgado;
        public string FiltroJuzgado
        {
            get => _filtroJuzgado;
            set { _filtroJuzgado = value; OnPropertyChanged(); }
        }

        private string _filtroSala;
        public string FiltroSala
        {
            get => _filtroSala;
            set { _filtroSala = value; OnPropertyChanged(); }
        }

        private string _filtroImputado;
        public string FiltroImputado
        {
            get => _filtroImputado;
            set { _filtroImputado = value; OnPropertyChanged(); }
        }

        private string _filtroDelito;
        public string FiltroDelito
        {
            get => _filtroDelito;
            set { _filtroDelito = value; OnPropertyChanged(); }
        }

        private string _filtroJuez;
        public string FiltroJuez
        {
            get => _filtroJuez;
            set { _filtroJuez = value; OnPropertyChanged(); }
        }

        private string _filtroExpediente;
        public string FiltroExpediente
        {
            get => _filtroExpediente;
            set { _filtroExpediente = value; OnPropertyChanged(); }
        }

        private string _filtroAQuienEntrega;
        public string FiltroAQuienEntrega
        {
            get => _filtroAQuienEntrega;
            set { _filtroAQuienEntrega = value; OnPropertyChanged(); }
        }

        // Fuentes fijas para los combos de filtro (mismas opciones que
        // ya usa Nuevo Registro, para no inventar valores nuevos).
        public List<string> TiposCausaDisponibles { get; } =
            new List<string> { "C", "CP", "JO", "EXP" };

        public List<string> JuzgadosDisponibles { get; } =
            new List<string> { "Control", "Centro" };

        public List<string> SalasDisponibles { get; } =
            new List<string> { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "CJMP" };

        public ICommand BuscarAvanzadoCommand { get; }
        public ICommand LimpiarFiltrosCommand { get; }

        private void EjecutarBuscarAvanzado(object param)
        {
            _filtroActivo.NUC = FiltroNUC;
            _filtroActivo.NoCausa = FiltroNoCausa;
            _filtroActivo.FechaDesde = FiltroFechaDesde;
            _filtroActivo.FechaHasta = FiltroFechaHasta;
            _filtroActivo.FechaReciboDesde = FiltroFechaReciboDesde;
            _filtroActivo.FechaReciboHasta = FiltroFechaReciboHasta;
            _filtroActivo.TipoCausa = FiltroTipoCausa;
            _filtroActivo.Juzgado = FiltroJuzgado;
            _filtroActivo.Sala = FiltroSala;
            _filtroActivo.Imputado = FiltroImputado;
            _filtroActivo.Delito = FiltroDelito;
            _filtroActivo.Juez = FiltroJuez;
            _filtroActivo.Expediente = FiltroExpediente;
            _filtroActivo.AQuienEntrega = FiltroAQuienEntrega;

            Filtrar();
        }

        private void EjecutarLimpiarFiltros(object param)
        {
            _filtroActivo.Limpiar();

            FiltroNUC = FiltroNoCausa = FiltroTipoCausa = FiltroJuzgado =
                FiltroSala = FiltroImputado = FiltroDelito = FiltroJuez =
                FiltroExpediente = FiltroAQuienEntrega = null;
            FiltroFechaDesde = FiltroFechaHasta =
                FiltroFechaReciboDesde = FiltroFechaReciboHasta = null;

            Filtrar();
        }

        /// <summary>
        /// Aplica de entrada un filtro ya armado (ej. desde las tarjetas del
        /// Home: "Audiencias este mes", "Copias entregadas este mes", etc.)
        /// — refleja los valores en las propiedades bindeables (para que el
        /// panel de filtros avanzados los muestre) y ejecuta la búsqueda de
        /// una vez, sin esperar a que el usuario presione "Buscar".
        /// </summary>
        public void AplicarFiltroInicial(FiltroConsulta filtro)
        {
            if (filtro == null) return;

            FiltroNUC = filtro.NUC;
            FiltroNoCausa = filtro.NoCausa;
            FiltroFechaDesde = filtro.FechaDesde;
            FiltroFechaHasta = filtro.FechaHasta;
            FiltroFechaReciboDesde = filtro.FechaReciboDesde;
            FiltroFechaReciboHasta = filtro.FechaReciboHasta;
            FiltroTipoCausa = filtro.TipoCausa;
            FiltroJuzgado = filtro.Juzgado;
            FiltroSala = filtro.Sala;
            FiltroImputado = filtro.Imputado;
            FiltroDelito = filtro.Delito;
            FiltroJuez = filtro.Juez;
            FiltroExpediente = filtro.Expediente;
            FiltroAQuienEntrega = filtro.AQuienEntrega;

            EjecutarBuscarAvanzado(null);
        }


        //  COMANDOS

        public ICommand VerCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand EliminarCommand { get; }


        //  CONSTRUCTOR

        public ConsultarRegistrosViewModel(
    string tabla)
        {
            _tablaActual = tabla;

            VerCommand =
                new RelayCommand(EjecutarVer);

            EditarCommand =
                new RelayCommand(EjecutarEditar);

            EliminarCommand =
                new RelayCommand(EjecutarEliminar);

            BuscarAvanzadoCommand =
                new RelayCommand(EjecutarBuscarAvanzado);

            LimpiarFiltrosCommand =
                new RelayCommand(EjecutarLimpiarFiltros);

            IniciarReloj();
        }

        //  RELOJ

        private void IniciarReloj()
        {
            _reloj = new DispatcherTimer();
            _reloj.Interval = TimeSpan.FromSeconds(1);
            _reloj.Tick += (s, e) => ActualizarHora();
            _reloj.Start();
            ActualizarHora();
        }

        private void ActualizarHora()
        {
            Hora = DateTime.Now.ToString("hh:mm tt");
            Fecha = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        }


        //  DATOS

        private async Task CargarDatosAsync()
        {
            try
            {
                // OleDb es síncrono en la mayor parte de esta capa. Ejecutar la
                // lectura y los listados auxiliares fuera del hilo de WPF evita
                // congelar la página mientras Access responde.
                var datos = await Task.Run(() =>
                {
                    List<Audiencia> audiencias = new AudienciaData()
                        .ObtenerAudiencias(_tablaActual)
                        .OrderByDescending(a => a.Id)
                        .ToList();

                    List<Ejecucion>? ejecuciones = _tablaActual == TablaEjecucion
                        ? new EjecucionData().ObtenerTodas()
                        : null;

                    List<RegistroCopia>? copias = _tablaActual == TablaCopias
                        ? new CopiasData().ObtenerTodas()
                        : null;

                    return (audiencias, ejecuciones, copias);
                });

                _listaCompleta = datos.audiencias;
                _cacheEjecuciones = datos.ejecuciones;
                _cacheCopias = datos.copias;

                // Reaplica lo que ya esté escrito en la búsqueda rápida y/o
                // los filtros avanzados (vacíos en la primera carga, por lo
                // que el resultado es el mismo que antes: primeros 10 y
                // totales generales). Así CargarDatos() y RecargarDatos()
                // comparten una sola lógica de carga+filtrado.
                Filtrar();

                CargarSugerencias();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
            }
        }

        /// <summary>
        /// Vuelve a consultar la base de datos (misma lógica que la carga
        /// inicial, ver CargarDatos) y reaplica los filtros que el usuario
        /// tenga activos en este momento. Se usa al regresar a esta pantalla
        /// después de editar un registro, para reflejar el cambio de
        /// inmediato sin perder la búsqueda/filtros en curso, y también
        /// cuando la base de datos activa cambia desde Configuración.
        /// </summary>
        private Task ObtenerCargaDatosAsync()
        {
            if (_cargaDatosActual == null || _cargaDatosActual.IsCompleted)
                _cargaDatosActual = CargarDatosAsync();

            return _cargaDatosActual;
        }

        public Task InicializarAsync() => ObtenerCargaDatosAsync();

        public Task RecargarDatosAsync() => ObtenerCargaDatosAsync();


        /// <summary>
        /// Suma el total de discos del conjunto de registros que se está
        /// mostrando (respeta el filtro de búsqueda activo, ya que recibe
        /// la lista ya filtrada). La columna real de donde sale el dato
        /// cambia según la tabla:
        ///   - Audiencias (C, CP, JO): TotDiscoAudiencia (texto, ej. "3 discos")
        ///   - Ejecucion:              TotalDiscos (texto)
        ///   - CopiasAudiencias:       TotDiscosEntregados (numérico)
        /// _listaCompleta/filtrados siempre llegan mapeados como Audiencia
        /// (ver AudienciaData.ObtenerAudiencias), así que para Ejecución y
        /// Copias se cruza por Id contra un listado con las columnas reales
        /// de esa tabla, en vez de leer el campo equivocado.
        /// </summary>
        private int CalcularTotalDiscos(List<Audiencia> conjuntoVisible)
        {
            if (_tablaActual == TablaEjecucion)
            {
                var ids = conjuntoVisible.Select(a => a.Id).ToHashSet();
                _cacheEjecuciones ??= new EjecucionData().ObtenerTodas();

                return _cacheEjecuciones
                    .Where(e => ids.Contains(e.Id))
                    .Sum(e => BuscadorRegistros.ExtraerNumero(e.TotalDiscos));
            }

            if (_tablaActual == TablaCopias)
            {
                var ids = conjuntoVisible.Select(a => a.Id).ToHashSet();
                _cacheCopias ??= new CopiasData().ObtenerTodas();

                return _cacheCopias
                    .Where(c => ids.Contains(c.Id))
                    .Sum(c => c.TotDiscosEntregados ?? 0);
            }

            // Audiencias (C, CP, JO): comportamiento original, sin cambios.
            return conjuntoVisible.Sum(a => BuscadorRegistros.ExtraerNumero(a.TotDiscoAudiencia));
        }


        private void CargarSugerencias()
        {
            Sugerencias = _listaCompleta.SelectMany(x => new[] {x.NoCausa, x.NUC, x.Imputado,x.FechaAudiencia?.ToString("dd/MM/yyyy HH:mm")
                }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        }


        //  FILTRADO Y SUGERENCIAS

        private void Filtrar()
        {
            string texto = _textoBusqueda.Trim().ToLower();
            bool hayFiltrosAvanzados = _filtroActivo.TieneAlgunCriterio;

            if (string.IsNullOrWhiteSpace(texto) && !hayFiltrosAvanzados)
            {
                // Nada activo: comportamiento original (primeros 10, sin filtrar).
                Audiencias = new ObservableCollection<Audiencia>(
                    _listaCompleta.Take(10)
                );

                TotalRegistros = $"{_listaCompleta.Count} registro(s) en total";

                TotalDiscosBusqueda =
                    $"Total discos audiencia: {CalcularTotalDiscos(_listaCompleta)}";

                return;
            }

            IEnumerable<Audiencia> resultado = _listaCompleta;

            if (!string.IsNullOrWhiteSpace(texto))
                resultado = FiltrarPorTextoRapido(resultado, texto);

            if (hayFiltrosAvanzados)
                resultado = BuscadorRegistros.AplicarFiltro(resultado, _filtroActivo);

            var filtrados = resultado.ToList();

            Audiencias = new ObservableCollection<Audiencia>(filtrados);

            TotalRegistros = $"{filtrados.Count} registro(s) encontrado(s)";

            TotalDiscosBusqueda =
                $"Total discos audiencia: {CalcularTotalDiscos(filtrados)}";
        }

        /// <summary>
        /// Búsqueda rápida (cuadro de texto libre): igual que antes, un OR
        /// entre varios campos. Se combina con AND respecto a los filtros
        /// avanzados cuando ambos están activos.
        /// </summary>
        private static IEnumerable<Audiencia> FiltrarPorTextoRapido(IEnumerable<Audiencia> origen, string texto)
        {
            DateTime fechaBuscada;
            bool esFecha = DateTime.TryParse(texto, out fechaBuscada);

            return origen.Where(a =>

                (!string.IsNullOrWhiteSpace(a.NoCausa) &&
                 a.NoCausa.Trim().ToLower() == texto)

                ||

                (!string.IsNullOrWhiteSpace(a.NUC) &&
                 a.NUC.Trim().ToLower() == texto)

                ||

                (esFecha &&
                 a.FechaAudiencia.HasValue &&
                 a.FechaAudiencia.Value.Date == fechaBuscada.Date)

                ||

                (!string.IsNullOrWhiteSpace(a.TipoCausa) &&
                 a.TipoCausa.Trim().ToLower() == texto)

                ||

                (!string.IsNullOrWhiteSpace(a.TipoAudiencia) &&
                 a.TipoAudiencia.ToLower().Contains(texto))

                ||

                (!string.IsNullOrWhiteSpace(a.Imputado) &&
                 a.Imputado.ToLower().Contains(texto))

                ||

                (!string.IsNullOrWhiteSpace(a.Agraviado) &&
                 a.Agraviado.ToLower().Contains(texto))
            );
        }

        private void ActualizarSugerencias()
        {
            string texto = _textoBusqueda.Trim();

            if (string.IsNullOrWhiteSpace(texto))
            {
                Sugerencias = new List<string>();
                return;
            }

            var sugerencias = new List<string>();

            sugerencias.AddRange(
                _listaCompleta
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.NoCausa) &&
                    x.NoCausa.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => x.NoCausa)
            );

            sugerencias.AddRange(
                _listaCompleta
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.NUC) &&
                    x.NUC.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => x.NUC)
            );

            sugerencias.AddRange(
                _listaCompleta
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Imputado) &&
                    x.Imputado.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => x.Imputado)
            );

            Sugerencias = sugerencias
                .Distinct()
                .Take(8)
                .ToList();
        }

        //  ACCIONES

        // Nombres de tabla reales usados por EjecucionData/CopiasData
        // (a diferencia de las tablas "Audiencias*", estos dos son fijos).
        private const string TablaEjecucion = "Ejecucion";
        private const string TablaCopias = "CopiasAudiencias";

        /// <summary>
        /// Nombre amigable de la tabla que se está consultando ahora mismo,
        /// para el encabezado de "Consultar Registros" — así el usuario
        /// siempre sabe en qué módulo está, sin depender de fijarse en el
        /// Sidebar.
        /// </summary>
        public string TituloTabla
        {
            get
            {
                if (_tablaActual == TablaEjecucion) return "Ejecución";
                if (_tablaActual == TablaCopias) return "Registro de Copias";
                return "Audiencias";
            }
        }

        private async void EjecutarVer(object param)
        {
            if (param is not Audiencia audiencia || _accionRegistroEnCurso) return;

            _accionRegistroEnCurso = true;
            try
            {
                // El tipo de registro seleccionado se determina por la tabla
                // desde la que se cargó la consulta (_tablaActual), no por el
                // tipo en tiempo de compilación del objeto (todas las tablas
                // se leen hoy a través de un mismo modelo "Audiencia" con
                // columnas tolerantes — ver AudienciaData.MapearDesdeReader).
                if (_tablaActual == TablaEjecucion)
                {
                    await MostrarDetalleEjecucionAsync(audiencia.Id);
                }
                else if (_tablaActual == TablaCopias)
                {
                    await MostrarDetalleCopiasAsync(audiencia.Id);
                }
                else
                {
                    await MostrarDetalleAudienciaAsync(audiencia);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar detalle: " + ex.Message);
            }
            finally
            {
                _accionRegistroEnCurso = false;
            }
        }

        public string NombreTablaActual
        {
            get
            {
                if (_tablaActual == TablaEjecucion) return "Ejecución";
                if (_tablaActual == TablaCopias) return "Copias";
                return _tablaActual;
            }
        }

        private async Task MostrarDetalleAudienciaAsync(Audiencia audiencia)
        {
            Audiencia detalle = await Task.Run(() =>
                new AudienciaData().ObtenerAudienciaPorId(audiencia.Id, _tablaActual));

            if (detalle == null)
            {
                MessageBox.Show("No se encontró el registro.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            VerDetalleRegistro ventana = new VerDetalleRegistro();
            ventana.CargarDatos(
                Id: detalle.Id.ToString(),
                noCausa: detalle.NoCausa,
                nuc: detalle.NUC,
                fechaAudiencia: detalle.FechaAudiencia?.ToString("dd/MM/yyyy HH:mm") ?? "",
                fechaRecibo: detalle.FechaRecibo?.ToString("dd/MM/yyyy HH:mm") ?? "",
                horaConclusion: detalle.HoraConclusion?.ToString("HH:mm") ?? "",
                tipoAudiencia: detalle.TipoAudiencia,
                tipoCausa: detalle.TipoCausa,
                juzgado: detalle.Juzgado,
                juez: detalle.Juez,
                sala: detalle.Sala,
                totalDiscos: detalle.TotDiscos?.ToString() ?? "",
                tipoDisco: detalle.TipoDisco,
                totalDiscoAudiencia: detalle.TotDiscoAudiencia,
                imputado: detalle.Imputado,
                delito: detalle.Delito,
                agraviado: detalle.Agraviado,
                noCausaJuicio: detalle.NoCausaJuicio,
                diferida: detalle.Diferida,
                quienRealiza: detalle.QuienRealiza
            );
            ventana.ShowDialog();
        }

        private async Task MostrarDetalleEjecucionAsync(int id)
        {
            Ejecucion detalle = await Task.Run(() =>
                new EjecucionData().ObtenerEjecucionPorId(id));

            if (detalle == null)
            {
                MessageBox.Show("No se encontró el registro.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            VerDetalleEjecucion ventana = new VerDetalleEjecucion();
            ventana.CargarDatos(
            id: detalle.Id.ToString(),
            expediente: detalle.ExpedienteNumero,
            causa: detalle.Causa,
            fechaAudiencia: detalle.FechaAudiencia?.ToString("dd/MM/yyyy") ?? "",
            tipoAudiencia: detalle.TipoAudiencia,
             horaTermino: detalle.HoraTermino,
            juez: detalle.Juez,   
             sala: detalle.Sala,
             imputado: detalle.Imputado,
             delito: detalle.Delito,
             victima: detalle.Victima,
             totalDiscos: detalle.TotalDiscos,
             observaciones: detalle.Observaciones
            );
            ventana.ShowDialog();
        }

        private async Task MostrarDetalleCopiasAsync(int id)
        {
            RegistroCopia detalle = await Task.Run(() =>
                new CopiasData().ObtenerCopiaPorId(id));

            if (detalle == null)
            {
                MessageBox.Show("No se encontró el registro.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            VerDetalleCopias ventana = new VerDetalleCopias();
            ventana.CargarDatos(
                id: detalle.Id.ToString(),
                noCausa: detalle.NoCausa,
                nuc: detalle.NUC,
                tipoCausa: detalle.TipoCausa,
                fechaAudiencia: detalle.FeAudiencia?.ToString("dd/MM/yyyy") ?? "",
                fechaRecibo: detalle.FeRecibo?.ToString("dd/MM/yyyy") ?? "",
                totalDiscosEntregados: detalle.TotDiscosEntregados?.ToString() ?? "",
                tipoDisco: detalle.TipoDisco,
                discosExternos: detalle.DiscosExternos,
                etiquetasEntregadas: detalle.EtiquetasEntregadas,
                aQuienSeEntrega: detalle.AQuienSeEntrega,
                quienRegistra: detalle.QuienRegistra,
                observaciones: detalle.Observaciones
            );
            ventana.ShowDialog();
        }


        private async void EjecutarEditar(object param)
        {
            if (param is not Audiencia audiencia || _accionRegistroEnCurso) return;

            Dashboard dashboard = Application.Current.Windows
                .OfType<Dashboard>()
                .FirstOrDefault();

            if (dashboard == null) return;

            _accionRegistroEnCurso = true;
            try
            {
                // El tipo de registro seleccionado se determina por la tabla
                // desde la que se cargó la consulta (_tablaActual) — el mismo
                // criterio que ya usa "Ver Detalle" — no por el tipo en
                // tiempo de compilación del objeto (todas las tablas se leen
                // hoy a través de un mismo modelo "Audiencia" con columnas
                // tolerantes, ver AudienciaData.MapearDesdeReader).
                if (_tablaActual == TablaEjecucion)
                {
                    Ejecucion ejecucion = await Task.Run(() =>
                        new EjecucionData().ObtenerEjecucionPorId(audiencia.Id));

                    if (ejecucion == null)
                    {
                        MessageBox.Show("No se encontró el registro.", "Aviso",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Ejecución no tiene formulario propio: reutiliza el
                    // formulario de Audiencias configurado como tipo "EXP".
                    dashboard.FramePrincipal.Navigate(new EditarRegistro(ejecucion));
                }
                else if (_tablaActual == TablaCopias)
                {
                    RegistroCopia copia = await Task.Run(() =>
                        new CopiasData().ObtenerCopiaPorId(audiencia.Id));

                    if (copia == null)
                    {
                        MessageBox.Show("No se encontró el registro.", "Aviso",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    dashboard.FramePrincipal.Navigate(new EditarCopias(copia));
                }
                else
                {
                    Audiencia detalle = await Task.Run(() =>
                        new AudienciaData().ObtenerAudienciaPorId(audiencia.Id, _tablaActual));

                    if (detalle == null)
                    {
                        MessageBox.Show("No se encontró el registro.", "Aviso",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    dashboard.FramePrincipal.Navigate(new EditarRegistro(detalle));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir el registro para editar: " + ex.Message);
            }
            finally
            {
                _accionRegistroEnCurso = false;
            }
        }
        private async void EjecutarEliminar(object param)
        {
            if (param is Audiencia audiencia && !_accionRegistroEnCurso)
            {
                var resultado = MessageBox.Show(
                    $"¿Eliminar el registro {audiencia.NoCausa}?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    _accionRegistroEnCurso = true;
                    try
                    {
                        if (await EliminarDeBaseDeDatos(audiencia))
                        {
                            _listaCompleta.Remove(audiencia);
                            Filtrar();
                        }
                    }
                    finally
                    {
                        _accionRegistroEnCurso = false;
                    }
                }
            }
        }

        private async Task<bool> EliminarDeBaseDeDatos(Audiencia audiencia)
        {
            try
            {
                int filasAfectadas = await Task.Run(() =>
                {
                    using (var connection = Conexion.ObtenerConexion())
                    {
                        connection.Open();

                        string tabla = _tablaActual;
                        string query = $"DELETE FROM [{tabla}] WHERE Id = ?";

                        using (var command = new OleDbCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("?", audiencia.Id);
                            return command.ExecuteNonQuery();
                        }
                    }
                });

                if (filasAfectadas <= 0)
                {
                    MessageBox.Show(
                        "Access no eliminó ningún registro. La lista no se modificará.",
                        "Registro no eliminado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                CacheSesionAccess.InvalidarRutaActual();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al eliminar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}
