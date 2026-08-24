using System.Windows;
using PoderJudicial.ViewModels;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Diálogo modal para crear una nueva tabla de Audiencias a partir de
    /// una plantilla. DialogResult queda en true solo si la tabla se creó
    /// correctamente — quien abre esta ventana (Dashboard) usa eso para
    /// decidir si refresca el Sidebar/la sección actual.
    /// </summary>
    public partial class CrearTabla : Window
    {
        private readonly CrearTablaViewModel _vm;

        public CrearTabla()
        {
            InitializeComponent();

            _vm = new CrearTablaViewModel();
            DataContext = _vm;

            PanelSinPlantillas.Visibility =
                _vm.SinPlantillasDisponibles ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnCrear_Click(object sender, RoutedEventArgs e)
        {
            PanelError.Visibility = Visibility.Collapsed;

            bool creada = _vm.Crear();

            if (!creada)
            {
                TxtMensajeError.Text = _vm.MensajeError;
                PanelError.Visibility = Visibility.Visible;
                return;
            }

            MessageBox.Show(
                $"La tabla \"{_vm.NombreNuevaTabla.Trim()}\" se creó correctamente.",
                "Tabla creada", MessageBoxButton.OK, MessageBoxImage.Information);

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