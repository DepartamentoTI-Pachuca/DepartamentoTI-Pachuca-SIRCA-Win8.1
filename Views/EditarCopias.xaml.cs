using PoderJudicial.Models;
using System;
using System.Windows.Controls;
using System.Windows;
using System.Threading.Tasks;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Host de "Editar Registro de Copias". Aloja una única instancia de
    /// <see cref="CopiasFormControl"/> precargada con el registro
    /// seleccionado (ver CopiasFormControl.CargarParaEditar) — mismo patrón
    /// que EditarRegistro para Audiencias/Ejecución.
    /// </summary>
    public partial class EditarCopias : Page
    {
        private readonly CopiasFormControl _control;
        private bool _guardando;

        public EditarCopias(RegistroCopia registro)
        {
            InitializeComponent();

            _control = new CopiasFormControl();
            _control.GuardarClick += Control_GuardarClick;
            PanelFormulario.Children.Add(_control);

            _control.CargarParaEditar(registro);
        }

        private async void Control_GuardarClick(object sender, EventArgs e)
        {
            if (_guardando)
                return;

            if (!_control.Validar(out string mensajeError))
            {
                MessageBox.Show(mensajeError, "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _guardando = true;
                var registro = _control.ConstruirModelo();
                await Task.Run(() =>
                {
                    using var medicion = PoderJudicial.Helpers.RendimientoLog.IniciarModulo(
                        "Registro Copias", "EditarCopias.Guardar");
                    _control.PersistirModelo(registro);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                _guardando = false;
            }

            MessageBox.Show("Registro actualizado correctamente.", "Éxito",
                MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService?.GoBack();
        }
    }
}
