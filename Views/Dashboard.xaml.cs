using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PoderJudicial.Views
{
    public partial class Dashboard : Window
    {

        private bool _submenuConsultasVisible = false;
        private Button _tablaSeleccionada = null;
        private bool _resolviendoTablaActual;
        private StackPanel _panelAudienciasAnteriores;


        public Dashboard(string usuario)
        {
            InitializeComponent();

            MainFrame.Navigate(new HomePage());

            ActivarBoton(BtnHome);

            Loaded += async (s, e) => await CargarTablasBDAsync();

            txtAvatar.Text = usuario.Substring(0, 1).ToUpper();
            txtNombreUsuario.Text = usuario;

            // Único suscriptor hoy: refresca el Sidebar y la sección
            // actualmente abierta cuando se cambia la BD desde
            // Configuración, sin tener que reiniciar la aplicación.
            // Se desuscribe al cerrar (logout puede crear un Dashboard
            // nuevo) para no acumular referencias a instancias viejas.
            EstadoBaseDatos.CambioBaseDatos += EstadoBaseDatos_CambioBaseDatos;
            Closed += (s, e) => EstadoBaseDatos.CambioBaseDatos -= EstadoBaseDatos_CambioBaseDatos;
        }
        public Frame FramePrincipal => MainFrame;

        private async Task CargarTablasBDAsync()
        {
            List<string> tablas = new();
            string rutaEsquema = "";

            try
            {
                using var medicion = RendimientoLog.IniciarModulo(
                    "Home", "Dashboard.CargarTablasBDAsync");
                using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                    "Dashboard.CargarTablas");

                var resultadoCarga = await Task.Run(() =>
                {
                    Stopwatch total = Stopwatch.StartNew();
                    List<string> resultado = new();
                    string rutaBD = Conexion.RutaBD;

                    using OleDbConnection conn = Conexion.ObtenerConexion();
                    Stopwatch apertura = Stopwatch.StartNew();
                    conn.Open();
                    apertura.Stop();

                    Stopwatch esquemaTiempo = Stopwatch.StartNew();
                    DataTable schema = conn.GetSchema("Tables");
                    esquemaTiempo.Stop();

                    Stopwatch mapeo = Stopwatch.StartNew();
                    foreach (DataRow row in schema.Rows)
                    {
                        string nombreTabla = row["TABLE_NAME"].ToString();

                        // IGNORAR tablas del sistema
                        if (nombreTabla.StartsWith("MSys"))
                            continue;

                        // IGNORAR tablas temporales
                        if (nombreTabla.StartsWith("~"))
                            continue;

                        resultado.Add(nombreTabla);
                    }
                    mapeo.Stop();
                    total.Stop();
                    RendimientoLog.Registrar("Dashboard.CargarTablasBDAsync", "",
                        "GetSchema(Tables)", openMs: apertura.ElapsedMilliseconds,
                        queryMs: esquemaTiempo.ElapsedMilliseconds,
                        mappingMs: mapeo.ElapsedMilliseconds, rows: schema.Rows.Count,
                        totalMs: total.ElapsedMilliseconds);

                    return (Tablas: resultado, RutaBD: rutaBD);
                });

                tablas = resultadoCarga.Tablas;
                rutaEsquema = resultadoCarga.RutaBD;

                // El mismo esquema alimenta el Sidebar y TableDetector;
                // el primer acceso directo ya no necesita abrir Access.
                TableDetector.ActualizarCacheDesdeEsquema(tablas, rutaEsquema);
            }
            catch (Exception ex)
            {
                // Defensa adicional: Login ya valida la conexión antes de
                // abrir este Dashboard, pero si la base de datos se vuelve
                // inaccesible justo en este instante (red caída, archivo
                // movido, etc.), no queremos que el constructor truene sin
                // control. Se avisa y se deja el sidebar de tablas vacío —
                // el resto del Dashboard (Home, navegación) sigue usable.
                MessageBox.Show(
                    "No se pudieron cargar las tablas de la base de datos:\n" + ex.Message,
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ConstruirMenuTablas(tablas);
        }

        private void ConstruirMenuTablas(List<string> tablas)
        {
            PanelTablas.Children.Clear();

            string ejecucion = tablas.FirstOrDefault(t =>
                string.Equals(t, "Ejecucion", StringComparison.OrdinalIgnoreCase));
            string copias = tablas.FirstOrDefault(t =>
                string.Equals(t, "CopiasAudiencias", StringComparison.OrdinalIgnoreCase));
            List<string> audiencias = tablas
                .Where(t => t.StartsWith("Audiencias ", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(ExtraerAnoFinal)
                .ToList();

            if (ejecucion != null) PanelTablas.Children.Add(CrearBotonTabla("Ejecución", ejecucion));
            if (copias != null) PanelTablas.Children.Add(CrearBotonTabla("Copias", copias));
            if (audiencias.Count > 0)
                PanelTablas.Children.Add(CrearBotonTabla(audiencias[0], audiencias[0]));

            if (audiencias.Count > 1)
            {
                var btnAnteriores = CrearBotonMenu("▸  Audiencias anteriores", 48);
                btnAnteriores.Click += (s, e) =>
                {
                    bool mostrar = _panelAudienciasAnteriores.Visibility != Visibility.Visible;
                    _panelAudienciasAnteriores.Visibility = mostrar
                        ? Visibility.Visible : Visibility.Collapsed;
                    btnAnteriores.Content = mostrar
                        ? "▾  Audiencias anteriores" : "▸  Audiencias anteriores";
                };
                PanelTablas.Children.Add(btnAnteriores);

                _panelAudienciasAnteriores = new StackPanel
                {
                    Visibility = Visibility.Collapsed
                };
                foreach (string tabla in audiencias.Skip(1))
                    _panelAudienciasAnteriores.Children.Add(CrearBotonTabla(tabla, tabla, 64));
                PanelTablas.Children.Add(_panelAudienciasAnteriores);
            }
        }

        private Button CrearBotonTabla(string texto, string tabla, double padding = 48)
        {
            Button boton = CrearBotonMenu(texto, padding);
            boton.Tag = tabla;
            boton.Click += BtnTablaDinamica_Click;
            return boton;
        }

        private static Button CrearBotonMenu(string texto, double padding)
        {
            return new Button
            {
                Content = texto,
                Height = 36,
                Margin = new Thickness(8, 2, 8, 2),
                Padding = new Thickness(padding, 0, 4, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8C1D1")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private static int ExtraerAnoFinal(string nombre)
        {
            Match coincidencia = Regex.Match(nombre ?? string.Empty, @"\d{4}-(\d{4})");
            int anio;
            return coincidencia.Success && int.TryParse(coincidencia.Groups[1].Value, out anio)
                ? anio : 0;
        }

        private void BtnTablaDinamica_Click(
    object sender,
    RoutedEventArgs e)
        {
            ActivarBoton(BtnConsultar);

            Button btn = (Button)sender;

            string nombreTabla = btn.Tag?.ToString();

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background =
                    Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(
                            "#B8C1D1"));
            }

            _tablaSeleccionada = btn;

            _tablaSeleccionada.Background =
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        "#2A3147"));

            _tablaSeleccionada.Foreground =
                new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        "#2ECC8F"));

            MainFrame.Navigate(
                new ConsultarRegistros(nombreTabla));
        }


        // ACTIVAR BOTÓN
        private void ActivarBoton(Button botonActivo)
        {
            // TODOS LOS BOTONES
            Button[] botones =
 {
    BtnConsultar,
    BtnNuevo,
    BtnCopias,
    BtnReportes,
    BtnConfig,
    BtnHome
};

            // Desactivar todos
            foreach (Button btn in botones)
            {
                btn.Background =
                    System.Windows.Media.Brushes.Transparent;

                btn.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)
                        System.Windows.Media.ColorConverter.ConvertFromString("#8B92A5"));
            }

            // activar solo uno
            botonActivo.Background =
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString("#2A3147"));

            botonActivo.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString("#2ECC8F"));
        }

        private void Navegar(Page pagina, Button boton)
        {
            ActivarBoton(boton);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background =
                    Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(
                            "#B8C1D1"));

                _tablaSeleccionada = null;
            }

            MainFrame.Navigate(pagina);
        }


        // Cosultar
        private void BtnConsultar_Click(
    object sender,
    RoutedEventArgs e)
        {
            _submenuConsultasVisible =
                !_submenuConsultasVisible;

            PanelTablas.Visibility =
                _submenuConsultasVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            TxtFlechaConsultar.Text =
                _submenuConsultasVisible
                ? "▲"
                : "▼";
        }

        // Nuevo
        private void BtnNuevo_Click(
    object sender,
    RoutedEventArgs e)
        {
            Navegar(
                new NuevoRegistro(),
                BtnNuevo);
        }


        // Copias
        private void BtnCopias_Click(
    object sender,
    RoutedEventArgs e)
        {
            Navegar(
                new RegistroCopias(),
                BtnCopias);
        }

        // REPORTES
        private void BtnReportes_Click(
     object sender,
     RoutedEventArgs e)
        {
            Navegar(
                new ReportesView(),
                BtnReportes);
        }




        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            SesionActual.Usuario = string.Empty;
            Login login = new Login();
            login.Show();


            this.Close();
        }


        // Config

        private void BtnConfig_Click(
    object sender,
    RoutedEventArgs e)
        {
            ActivarBoton(BtnConfig);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background =
                    Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(
                            "#B8C1D1"));

                _tablaSeleccionada = null;
            }

            BtnConfig.ContextMenu.PlacementTarget =
                BtnConfig;

            BtnConfig.ContextMenu.IsOpen = true;
        }

        //HOME

        private void BtnHome_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (MainFrame.Content is HomePage)
                return;

            Navegar(
                new HomePage(),
                BtnHome);
        }

        // El logo funciona como acceso directo a Home. La navegación vive
        // aquí, separada del recurso visual (hoy un emoji dentro del Border
        // "LOGO" en el XAML) — para reemplazarlo después por una imagen/logo
        // personalizado no hace falta tocar este método.
        private void Logo_Click(
    object sender,
    RoutedEventArgs e)
        {
            BtnHome_Click(sender, e);
        }

        // ══════════════════════════════════════════════
        //  SIDEBAR CONTRAÍBLE (expande al pasar el mouse)
        // ══════════════════════════════════════════════
        private void SidebarBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Storyboard)Resources["SidebarExpandStoryboard"]).Begin(this);
        }

        private void SidebarBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Storyboard)Resources["SidebarCollapseStoryboard"]).Begin(this);
        }


        public void AbrirNuevoRegistro()
        {
            Navegar(
                new NuevoRegistro(),
                BtnNuevo);
        }

        public void AbrirRegistroCopias()
        {
            Navegar(
                new RegistroCopias(),
                BtnCopias);
        }

        public void AbrirReportes()
        {
            Navegar(
                new ReportesView(),
                BtnReportes);
        }

        public void AbrirHome()
        {
            if (MainFrame.Content is HomePage)
                return;

            Navegar(
                new HomePage(),
                BtnHome);
        }

        public void AbrirConfiguracion()
        {
            ActivarBoton(BtnConfig);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background =
                    Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(
                            "#B8C1D1"));

                _tablaSeleccionada = null;
            }

            BtnConfig.ContextMenu.PlacementTarget =
                BtnConfig;

            BtnConfig.ContextMenu.IsOpen = true;
        }

        public async void AbrirConsultarRegistros()
        {
            if (_resolviendoTablaActual)
                return;

            _resolviendoTablaActual = true;
            ActivarBoton(BtnConsultar);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background =
                    Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(
                            "#B8C1D1"));

                _tablaSeleccionada = null;
            }

            try
            {
                string tabla = await Task.Run(() => TableDetector.TablaActual);

                if (IsLoaded)
                    MainFrame.Navigate(new ConsultarRegistros(tabla));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo determinar la tabla de audiencias:\n" + ex.Message,
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _resolviendoTablaActual = false;
            }
        }

        public void AbrirConsultarRegistros(string tabla)
        {
            ActivarBoton(BtnConsultar);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background = Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#B8C1D1"));

                _tablaSeleccionada = null;
            }

            MainFrame.Navigate(
                new ConsultarRegistros(tabla));
        }

        /// <summary>
        /// Igual que <see cref="AbrirConsultarRegistros(string)"/> pero
        /// además precarga un filtro (rango de fechas, etc.) — usado por los
        /// accesos directos de las tarjetas del Home ("Audiencias este mes",
        /// "Copias entregadas este mes", etc.).
        /// </summary>
        public void AbrirConsultarRegistros(string tabla, FiltroConsulta filtroInicial)
        {
            ActivarBoton(BtnConsultar);

            if (_tablaSeleccionada != null)
            {
                _tablaSeleccionada.Background = Brushes.Transparent;

                _tablaSeleccionada.Foreground =
                    new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#B8C1D1"));

                _tablaSeleccionada = null;
            }

            MainFrame.Navigate(
                new ConsultarRegistros(tabla, filtroInicial));
        }




        private void ModoClaro_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.CambiarTema("Light");
        }

        private void ModoOscuro_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.CambiarTema("Dark");
        }

        // Consultar/cambiar la ruta de la base de datos desde el propio
        // sistema (Configuración → Base de Datos...). Reutiliza la misma
        // ventana del primer arranque: ya llega con la ruta actual
        // precargada (consultar) y permite Buscar/Probar/Guardar una nueva
        // (cambiar). permiteCancelar: true porque aquí sí hay una
        // configuración funcionando a la que volver si el usuario se
        // arrepiente.
        private void BaseDatos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ConfiguracionBaseDatos(permiteCancelar: true);
            ventana.ShowDialog();
        }

        // Crear una nueva tabla de Audiencias desde una plantilla (ver
        // CrearTablaViewModel/CreadorTablasRepository). Reutiliza el mismo
        // mecanismo de refresco que el cambio de base de datos
        // (TableDetector.InvalidarCache() ya se llamó dentro del
        // repositorio; aquí solo falta avisar para que el Sidebar y la
        // sección actual se reconstruyan) — así no hace falta reiniciar la
        // aplicación para ver ni usar la tabla nueva.
        private async void CrearTabla_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new CrearTabla();
            bool? creada = ventana.ShowDialog();

            if (creada == true)
                await RecargarTrasCambioBDAsync();
        }

        // ══════════════════════════════════════════════
        //  CAMBIO DE BASE DE DATOS: refrescar todo lo que
        //  dependa de ella, sin reiniciar la aplicación.
        // ══════════════════════════════════════════════
        private async void EstadoBaseDatos_CambioBaseDatos(object sender, EventArgs e)
        {
            await RecargarTrasCambioBDAsync();
        }

        /// <summary>
        /// Vuelve a detectar las tablas (Sidebar) y reconstruye la sección
        /// que esté actualmente abierta en el Frame, para que deje de
        /// mostrar información de la base de datos anterior. Reutiliza los
        /// mismos métodos "Abrir*" que ya usa la navegación normal — no hay
        /// una segunda ruta de carga de datos.
        /// Si la tabla/registro que estaba abierto ya no existe en la BD
        /// nueva, el propio método "Abrir*" reutilizado ya maneja ese error
        /// de forma controlada (igual que cuando el usuario navega ahí
        /// manualmente); el try/catch de aquí es una defensa adicional para
        /// que, pase lo que pase, nunca se caiga toda la aplicación por
        /// esto.
        /// </summary>
        private async Task RecargarTrasCambioBDAsync()
        {
            try
            {
                await CargarTablasBDAsync();

                object actual = MainFrame.Content;

                if (actual is ConsultarRegistros consulta)
                    AbrirConsultarRegistros(consulta.TablaActualSeleccionada);
                else if (actual is HomePage)
                    Navegar(new HomePage(), BtnHome);
                else if (actual is NuevoRegistro)
                    AbrirNuevoRegistro();
                else if (actual is RegistroCopias)
                    AbrirRegistroCopias();
                else if (actual is ReportesView)
                    AbrirReportes();

                // EditarRegistro/EditarCopias y otras pantallas puntuales
                // (dependen de un registro ya cargado en memoria) se dejan
                // como están: no tiene sentido reconstruirlas a mitad de una
                // edición. Al guardar o cancelar, el usuario vuelve a
                // Consultar Registros, que sí se recarga.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "La base de datos cambió, pero no se pudo actualizar automáticamente esta pantalla:\n" + ex.Message,
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ModoDescanso_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.CambiarTema("EyeCare");
        }


    }
}
