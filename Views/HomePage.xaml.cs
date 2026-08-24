using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using PoderJudicial.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PoderJudicial.Models;
using System.Windows.Input;
using System.Windows.Media;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Lógica de interacción para HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {

        private HomePageViewModel vm;

        private DispatcherTimer timer;
        private readonly CancellationTokenSource _cancelacionCarga = new();
        private bool _cargaDashboardIniciada;

        private sealed class ResultadoDashboard
        {
            public int TotalAudiencias { get; init; }
            public int TotalEjecuciones { get; init; }
            public int TotalCopias { get; init; }
            public int AudienciasHoy { get; init; }
            public string Version { get; init; } = "";
            public string NombreBD { get; init; } = "";
            public string Estado { get; init; } = "";
            public List<ActividadReciente> Actividades { get; init; } = new();
        }
        public HomePage()
        {

            InitializeComponent();

            vm = new HomePageViewModel();
            DataContext = vm;

            IniciarReloj();
            _ = CargarDashboardAsync();
            _ = CargarUltimoRespaldoAsync();

            Unloaded += (s, e) => PrepararNavegacion();

        }

        private Dashboard ObtenerDashboard()
        {
            return Window.GetWindow(this) as Dashboard;
        }

        private void ActualizarFechaHora()
        {
            DateTime ahora = DateTime.Now;
            CultureInfo cultura = new CultureInfo("es-MX");
            TxtHora.Text = ahora.ToString("hh:mm tt");
            TxtFecha.Text = ahora.ToString("dddd, dd MMMM yyyy", cultura);

        }

        private void IniciarReloj()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => ActualizarFechaHora();
            timer.Start();
            ActualizarFechaHora();
        }

        // Antes, todas las consultas del Home (totales del mes, audiencias
        // de hoy, actividad reciente — varias de ellas recorren TODAS las
        // tablas de Audiencias) se ejecutaban de forma síncrona en el
        // constructor de la página, en el hilo de UI. Como Home es la
        // pantalla más visitada (se vuelve a crear cada vez que se navega
        // a ella, incluida la recarga automática tras un cambio de base de
        // datos), eso significaba congelar la interfaz en cada visita
        // mientras Access respondía. Se movió el trabajo de BD a un hilo de
        // fondo con Task.Run; el "await" retoma en el hilo de UI
        // automáticamente (SynchronizationContext de WPF), así que asignar
        // las propiedades del ViewModel abajo sigue siendo seguro sin
        // Dispatcher.Invoke manual.
        private async Task CargarDashboardAsync()
        {
            if (_cargaDashboardIniciada)
                return;

            _cargaDashboardIniciada = true;
            CancellationToken token = _cancelacionCarga.Token;

            try
            {
                using var medicion = RendimientoLog.IniciarModulo(
                    "Home", "HomePage.CargarDashboardAsync");
                string claveCache = $"Home.Resumen.{DateTime.Today:yyyyMMdd}";
                if (CacheSesionAccess.IntentarObtener(
                    claveCache, out ResultadoDashboard? datosCache))
                {
                    if (!token.IsCancellationRequested)
                        AplicarResultadoDashboard(datosCache);
                    return;
                }

                using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                    "Home.CargarDashboard", token);

                if (CacheSesionAccess.IntentarObtener(claveCache, out datosCache))
                {
                    if (!token.IsCancellationRequested)
                        AplicarResultadoDashboard(datosCache);
                    return;
                }

                long versionCache = CacheSesionAccess.ObtenerVersionActual();

                ResultadoDashboard? datos = await Task.Run(() =>
                {
                    if (token.IsCancellationRequested)
                        return null;

                    DashboardData dashboard = new DashboardData();

                    var resumenAudiencias = dashboard.ObtenerResumenAudiencias();
                    if (token.IsCancellationRequested) return null;
                    int totalEjecuciones = dashboard.ObtenerTotalEjecucionesMes();
                    if (token.IsCancellationRequested) return null;
                    int totalCopias = dashboard.ObtenerTotalCopiasMes();
                    if (token.IsCancellationRequested) return null;
                    string version = dashboard.ObtenerVersionSistema();
                    string nombreBD = dashboard.ObtenerNombreBaseDatos();
                    // Todas las lecturas anteriores terminaron correctamente;
                    // no abrir otra conexión remota solo para probar lo mismo.
                    string estado = "Operativo";
                    if (token.IsCancellationRequested) return null;
                    List<ActividadReciente> actividades = dashboard.ObtenerActividadesRecientes();
                    if (token.IsCancellationRequested) return null;

                    return new ResultadoDashboard
                    {
                        TotalAudiencias = resumenAudiencias.TotalMes,
                        TotalEjecuciones = totalEjecuciones,
                        TotalCopias = totalCopias,
                        AudienciasHoy = resumenAudiencias.TotalHoy,
                        Version = version,
                        NombreBD = nombreBD,
                        Estado = estado,
                        Actividades = actividades
                    };
                });

                if (datos == null)
                    return;

                CacheSesionAccess.GuardarSiVersion(
                    claveCache, datos, versionCache);
                if (!token.IsCancellationRequested)
                    AplicarResultadoDashboard(datos);
            }
            catch (OperationCanceledException) when (_cancelacionCarga.IsCancellationRequested)
            {
                // La página dejó de estar visible; no continuar consultas ni
                // publicar resultados tardíos sobre una instancia abandonada.
            }
            catch (Exception ex)
            {
                // Mismo criterio que el resto de la app: avisar sin tirar
                // la pantalla completa. Home sigue mostrando reloj y
                // navegación aunque el panel de indicadores no cargue.
                MessageBox.Show(
                    "No se pudo cargar la información del panel principal:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AplicarResultadoDashboard(ResultadoDashboard datos)
        {
            vm.TotalAudienciasMes = datos.TotalAudiencias;
            vm.TotalEjecucionesMes = datos.TotalEjecuciones;
            vm.TotalCopiasMes = datos.TotalCopias;
            vm.AudienciasHoy = datos.AudienciasHoy;
            vm.VersionSistema = datos.Version;
            vm.NombreBaseDatos = datos.NombreBD;
            vm.EstadoSistema = datos.Estado;
            vm.Actividades = new ObservableCollection<ActividadReciente>(
                datos.Actividades);
        }

        private async Task CargarUltimoRespaldoAsync()
        {
            CancellationToken token = _cancelacionCarga.Token;
            vm.UltimaCopiaSeguridad = "No disponible";

            try
            {
                DateTime? ultimoRespaldo = await Task.Run(
                    RespaldoBaseDatosService.ObtenerFechaUltimoRespaldoActual,
                    token);

                if (token.IsCancellationRequested)
                    return;

                vm.UltimaCopiaSeguridad = ultimoRespaldo.HasValue
                    ? ultimoRespaldo.Value.ToString("dd/MM/yyyy hh:mm tt")
                    : "No disponible";
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // La página dejó de estar visible.
            }
            catch
            {
                if (!token.IsCancellationRequested)
                    vm.UltimaCopiaSeguridad = "No disponible";
            }
        }

        private void PrepararNavegacion()
        {
            if (!_cancelacionCarga.IsCancellationRequested)
                _cancelacionCarga.Cancel();

            timer?.Stop();
        }

        private static async Task<string?> ObtenerTablaActualAsync()
        {
            try
            {
                return await Task.Run(() => TableDetector.TablaActual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo determinar la tabla de audiencias:\n" + ex.Message,
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }


        private void CardNuevoRegistro_Click(
     object sender,
     RoutedEventArgs e)
        {
            PrepararNavegacion();
            ObtenerDashboard()?.AbrirNuevoRegistro();
        }

        private void CardConsultar_Click(
    object sender,
    RoutedEventArgs e)
        {
            PrepararNavegacion();
            ObtenerDashboard()?.AbrirConsultarRegistros();
        }
        private void CardCopias_Click(
    object sender,
    RoutedEventArgs e)
        {
            PrepararNavegacion();
            ObtenerDashboard()?.AbrirRegistroCopias();
        }

        private void CardReportes_Click(
    object sender,
    RoutedEventArgs e)
        {
            PrepararNavegacion();
            ObtenerDashboard()?.AbrirReportes();
        }

        private void CardConfiguracion_Click(
    object sender,
    RoutedEventArgs e)
        {
            ObtenerDashboard()?.AbrirConfiguracion();
        }

        // ══════════════════════════════════════════════
        //  TARJETAS DE ESTADÍSTICAS → accesos directos a
        //  Consultar Registros, ya filtrados.
        // ══════════════════════════════════════════════
        private async void CardAudienciasMes_Click(object sender, RoutedEventArgs e)
        {
            var (desde, hasta) = RangoMesActual();
            PrepararNavegacion();

            string? tabla = await ObtenerTablaActualAsync();
            if (tabla == null) return;

            ObtenerDashboard()?.AbrirConsultarRegistros(
                tabla,
                new FiltroConsulta { FechaDesde = desde, FechaHasta = hasta });
        }

        private void CardEjecucionesMes_Click(object sender, RoutedEventArgs e)
        {
            var (desde, hasta) = RangoMesActual();
            PrepararNavegacion();

            ObtenerDashboard()?.AbrirConsultarRegistros(
                "Ejecucion",
                new FiltroConsulta { FechaDesde = desde, FechaHasta = hasta });
        }

        private void CardCopiasMes_Click(object sender, RoutedEventArgs e)
        {
            var (desde, hasta) = RangoMesActual();
            PrepararNavegacion();

            // Copias: lo que importa es cuándo se ENTREGÓ la copia
            // (Fecha de Recibo), no la fecha de la audiencia original.
            ObtenerDashboard()?.AbrirConsultarRegistros(
                "CopiasAudiencias",
                new FiltroConsulta { FechaReciboDesde = desde, FechaReciboHasta = hasta });
        }

        private async void CardAudienciasHoy_Click(object sender, RoutedEventArgs e)
        {
            PrepararNavegacion();
            string? tabla = await ObtenerTablaActualAsync();
            if (tabla == null) return;

            ObtenerDashboard()?.AbrirConsultarRegistros(
                tabla,
                new FiltroConsulta { FechaDesde = DateTime.Today, FechaHasta = DateTime.Today });
        }

        private static (DateTime desde, DateTime hasta) RangoMesActual()
        {
            DateTime hoy = DateTime.Now;
            DateTime primerDia = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime ultimoDia = primerDia.AddMonths(1).AddDays(-1);
            return (primerDia, ultimoDia);
        }


        private void Actividad_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.SetResourceReference(
                    Border.BackgroundProperty,
                    "HoverBrush");
            }
        }

        private void Actividad_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
            }
        }


        private void Actividad_Click(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;

            if (border == null)
                return;

            ActividadReciente actividad =
                border.DataContext as ActividadReciente;

            if (actividad == null)
                return;

            PrepararNavegacion();
            ObtenerDashboard()?
                .AbrirConsultarRegistros(
                    actividad.TablaDestino);
        }


    }
}
