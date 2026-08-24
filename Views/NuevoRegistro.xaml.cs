using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Input;
using PoderJudicial.ViewModels;

namespace PoderJudicial.Views
{
    /// <summary>
    /// Host de "Nuevo Registro". Administra una cadena de 1 a 7
    /// <see cref="AudienciaFormControl"/> (Audiencias Concentradas):
    /// agrega/quita formularios, valida todos antes de guardar y persiste
    /// cada uno como un registro independiente en la base de datos.
    /// </summary>
    public partial class NuevoRegistro : Page
    {
        private const int MaximoFormularios = 7;

        private readonly List<AudienciaFormControl> _formularios = new();
        private readonly NuevoRegistroViewModel _viewModelCompartido;
        private bool _guardando;

        public NuevoRegistro()
        {
            InitializeComponent();
            _viewModelCompartido = new NuevoRegistroViewModel();
            AgregarFormulario();
        }

        private void ScrollPrincipal_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            DependencyObject origen = e.OriginalSource as DependencyObject;
            if (origen != null && BuscarPadre<ComboBox>(origen) != null)
                return;

            ScrollPrincipal.ScrollToVerticalOffset(
                ScrollPrincipal.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private static T BuscarPadre<T>(DependencyObject actual) where T : DependencyObject
        {
            while (actual != null)
            {
                T encontrado = actual as T;
                if (encontrado != null) return encontrado;
                actual = System.Windows.Media.VisualTreeHelper.GetParent(actual);
            }
            return null;
        }

        // ══════════════════════════════════════════════
        //  ALTA / BAJA DE FORMULARIOS (Concentrada)
        // ══════════════════════════════════════════════
        private void AgregarFormulario()
        {
            // Todos los formularios concentrados usan los mismos catálogos
            // históricos. No tiene sentido volver a leer Access por cada
            // formulario agregado.
            var control = new AudienciaFormControl(
                _viewModelCompartido,
                cargarIdVisual: _formularios.Count == 0);
            control.ConcentradaClick += Formulario_ConcentradaClick;
            control.GuardarClick += Formulario_GuardarClick;

            _formularios.Add(control);
            PanelFormularios.Children.Add(control);

            ActualizarNumeracionYBotones();
        }

        private void Formulario_ConcentradaClick(object sender, EventArgs e)
        {
            if (_guardando || _formularios.Count >= MaximoFormularios) return;
            AgregarFormulario();
        }

        /// <summary>
        /// Renumera los formularios ("Audiencia concentrada #N") y solo deja
        /// visible el botón "Concentrada" en el último, ocultándolo por
        /// completo al llegar al máximo permitido.
        /// </summary>
        private void ActualizarNumeracionYBotones()
        {
            for (int i = 0; i < _formularios.Count; i++)
            {
                _formularios[i].EstablecerNumero(i + 1);

                bool esUltimo = i == _formularios.Count - 1;
                bool puedeAgregarMas = _formularios.Count < MaximoFormularios;
                _formularios[i].MostrarBotonConcentrada(esUltimo && puedeAgregarMas);
            }
        }

        // ══════════════════════════════════════════════
        //  GUARDADO CONJUNTO
        // ══════════════════════════════════════════════
        private async void Formulario_GuardarClick(object sender, EventArgs e)
        {
            if (_guardando)
                return;

            // 1) Validar TODOS los formularios antes de guardar cualquiera.
            for (int i = 0; i < _formularios.Count; i++)
            {
                if (!_formularios[i].Validar(out string mensajeError))
                {
                    string prefijo = _formularios.Count > 1 ? $"Formulario {i + 1}: " : string.Empty;
                    MessageBox.Show(prefijo + mensajeError, "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 2) Calcular folios (Id) secuenciales en memoria: como ninguno
            //    de los formularios concentrados se ha insertado aún, cada
            //    uno pediría el mismo "siguiente Id" a la base de datos si
            //    se le preguntara por separado. Se reserva un contador por
            //    tabla destino (Audiencia vs Ejecución) y se incrementa a
            //    medida que se recorren los formularios, en el mismo orden
            //    en que se van a guardar.
            bool esConcentrada = _formularios.Count > 1;
            var modelos = new List<object>();
            List<AudienciaFormControl> formularios = _formularios.ToList();
            List<string> tiposCausa = formularios
                .Select(formulario => formulario.TipoCausaActual)
                .ToList();

            _guardando = true;

            try
            {
                Dictionary<string, int> siguienteIdPorTabla = await Task.Run(() =>
                {
                    var ids = new Dictionary<string, int>();
                    foreach (string tipoCausa in tiposCausa)
                    {
                        string tabla = tipoCausa == "EXP" ? "EXP" : "AUD";
                        if (!ids.ContainsKey(tabla))
                            ids[tabla] = _viewModelCompartido.ObtenerSiguienteId(tipoCausa);
                    }
                    return ids;
                });

                for (int i = 0; i < formularios.Count; i++)
                {
                    string tabla = tiposCausa[i] == "EXP" ? "EXP" : "AUD";
                    int id = siguienteIdPorTabla[tabla]++;

                    modelos.Add(formularios[i].ConstruirModelo(id, esConcentrada));
                }

                // Cada formulario concentrado sigue siendo un registro
                // independiente y se guarda en el mismo orden original.
                await Task.Run(() =>
                {
                    using var medicion = PoderJudicial.Helpers.RendimientoLog.IniciarModulo(
                        "Nuevo Registro", "NuevoRegistro.Guardar");
                    for (int i = 0; i < formularios.Count; i++)
                        formularios[i].PersistirModelo(modelos[i]);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ocurrió un error al guardar y el proceso se detuvo. " +
                    $"Verifique los registros ya guardados antes de reintentar.\n\n{ex.Message}",
                    "Error al guardar", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                _guardando = false;
            }

            MessageBox.Show(
                esConcentrada
                    ? $"Se guardaron {_formularios.Count} audiencias concentradas correctamente."
                    : "Registro guardado correctamente.",
                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            ReiniciarCadena();
        }

        /// <summary>Tras guardar, vuelve a dejar un único formulario limpio.</summary>
        private void ReiniciarCadena()
        {
            _viewModelCompartido.InvalidarFoliosVisuales();

            foreach (var formulario in _formularios.Skip(1))
                formulario.DetenerReloj();

            PanelFormularios.Children.Clear();
            _formularios.Clear();

            AgregarFormulario();
        }
    }
}
