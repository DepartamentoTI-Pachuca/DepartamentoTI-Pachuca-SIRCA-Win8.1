using System.Windows;
using PoderJudicial.Data;

namespace PoderJudicial.Views
{
    public partial class CrearUsuario : Window
    {
        public CrearUsuario()
        {
            InitializeComponent();
        }

        private void btnCrear_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string pass = passNueva.Password;
            string passConf = passConfirmar.Password;
           

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Usuario y contraseña son obligatorios.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass != passConf)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass.Length < 4)
            {
                MessageBox.Show("La contraseña debe tener al menos 4 caracteres.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var repo = new UserRepository();
            bool ok = repo.Register(usuario, pass);

            if (ok)
            {
                MessageBox.Show($"Usuario '{usuario}' creado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                // Cierra únicamente este diálogo y devuelve el control al Login.
                DialogResult = true;
            }
            else
            {
                MessageBox.Show($"El usuario '{usuario}' ya existe.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
