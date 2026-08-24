using Microsoft.Win32;
using PoderJudicial.Data;
using PoderJudicial.Helpers;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Ventana única reutilizada en los 3 escenarios descritos en el
    /// análisis: primera ejecución (sin configuración todavía), la base de
    /// datos configurada dejó de estar disponible, y "Cambiar base de
    /// datos" manual desde Configuración.
    /// </summary>
    public partial class ConfiguracionBaseDatos : Window
    {
        private bool _probandoConexion;
        /// <param name="mensajeError">
        /// Si se pasa (ej. "no se pudo conectar"), se muestra un banner rojo
        /// explicando por qué se abrió esta ventana. Null en primera ejecución.
        /// </param>
        /// <param name="permiteCancelar">
        /// True cuando ya existe una configuración funcional a la que se
        /// puede volver (ej. "Cambiar base de datos" desde Configuración,
        /// o un simple "Reintentar" fallido pero la app puede seguir
        /// mostrando lo que no dependa de la BD). False en primera
        /// ejecución, donde no hay nada a lo que cancelar.
        /// </param>
        public ConfiguracionBaseDatos(string mensajeError = null, bool permiteCancelar = false)
        {
            InitializeComponent();

            ConfiguracionBD configActual = ConfiguracionBD.Cargar();
            if (configActual != null)
                TxtRuta.Text = configActual.RutaArchivo;

            if (!string.IsNullOrWhiteSpace(mensajeError))
            {
                TxtMensajeError.Text = mensajeError;
                PanelError.Visibility = Visibility.Visible;
            }

            BtnCancelar.Visibility = permiteCancelar ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtRuta_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // El usuario está escribiendo/pegando manualmente: el resultado
            // de una prueba anterior ya no aplica a este texto.
            TxtEstado.Text = "";
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Seleccionar base de datos",
                Filter = "Base de datos Access (*.accdb)|*.accdb|Todos los archivos (*.*)|*.*"
            };

            if (dialogo.ShowDialog() == true)
            {
                TxtRuta.Text = dialogo.FileName;
                TxtEstado.Text = "";
            }
        }

        private async void BtnProbar_Click(object sender, RoutedEventArgs e)
        {
            if (_probandoConexion)
                return;

            _probandoConexion = true;
            string ruta = TxtRuta.Text;
            string error;
            try
            {
                error = await Task.Run(() => Conexion.ProbarConexion(ruta));
            }
            finally
            {
                _probandoConexion = false;
            }

            if (!IsLoaded || !string.Equals(ruta, TxtRuta.Text,
                System.StringComparison.OrdinalIgnoreCase))
                return;

            if (error == null)
            {
                TxtEstado.Text = "✔ Conexión exitosa.";
                TxtEstado.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            }
            else
            {
                TxtEstado.Text = error;
                TxtEstado.Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C));
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_probandoConexion)
                return;

            _probandoConexion = true;
            string ruta = TxtRuta.Text;
            string error;
            try
            {
                error = await Task.Run(() => Conexion.ProbarConexion(ruta));
            }
            finally
            {
                _probandoConexion = false;
            }

            if (!IsLoaded || !string.Equals(ruta, TxtRuta.Text,
                System.StringComparison.OrdinalIgnoreCase))
                return;

            if (error != null)
            {
                TxtEstado.Text = error;
                TxtEstado.Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C));

                MessageBox.Show(
                    "No se puede guardar: la conexión de prueba falló.\n\n" + error,
                    "Conexión fallida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Cambiar la base activa no debe borrar preferencias de respaldo
            // ya guardadas en el mismo config.json.
            ConfiguracionBD nuevaConfig = ConfiguracionBD.Cargar()
                ?? new ConfiguracionBD();
            nuevaConfig.RutaArchivo = TxtRuta.Text.Trim();
            nuevaConfig.Guardar();

            // Para que la app use la ruta nueva de inmediato, sin reiniciar,
            // y para que el sidebar/consultas dinámicas no sigan mostrando
            // datos de la base de datos anterior.
            Conexion.InvalidarConfiguracion();
            TableDetector.InvalidarCache();
            CacheSesionAccess.InvalidarTodo();

            // Avisa a quien esté suscrito (hoy: Dashboard) para que
            // refresque el Sidebar y la sección actualmente abierta —
            // así no hace falta reiniciar la aplicación.
            EstadoBaseDatos.NotificarCambio();

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
