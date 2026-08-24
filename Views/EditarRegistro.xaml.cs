using PoderJudicial.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Host de "Editar Registro". Aloja una única instancia de
    /// <see cref="AudienciaFormControl"/> precargada con el registro
    /// seleccionado y la deja en modo edición (ver
    /// AudienciaFormControl.Editar.cs). Cubre tanto Audiencias (C, CP, JO)
    /// como Ejecución — Ejecución no tiene formulario propio: reutiliza este
    /// mismo control configurado como tipo de causa "EXP".
    /// </summary>
    public partial class EditarRegistro : Page
    {
        private readonly AudienciaFormControl _control;
        private bool _guardando;

        public EditarRegistro(Audiencia audiencia)
        {
            InitializeComponent();
            _control = CrearControl();
            _control.CargarParaEditar(audiencia);
        }

        public EditarRegistro(Ejecucion ejecucion)
        {
            InitializeComponent();
            _control = CrearControl();
            _control.CargarParaEditar(ejecucion);
        }

        private AudienciaFormControl CrearControl()
        {
            var control = new AudienciaFormControl();
            control.GuardarClick += Control_GuardarClick;
            PanelFormulario.Children.Add(control);
            return control;
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
                // El Id pasado aquí se ignora en modo edición: AudienciaFormControl
                // usa internamente el Id real del registro cargado (ver
                // AudienciaFormControl.Guardar.cs → ConstruirModelo).
                object modelo = _control.ConstruirModelo(0, esConcentrada: false);
                await Task.Run(() => _control.PersistirModelo(modelo));
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
