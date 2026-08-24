using System.Collections.ObjectModel;
using PoderJudicial.Data;

namespace PoderJudicial.ViewModels
{
    public class CrearTablaViewModel : BaseViewModel
    {
        private readonly CreadorTablasRepository _repo = new CreadorTablasRepository();

        public ObservableCollection<string> Plantillas { get; } = new ObservableCollection<string>();

        private string _plantillaSeleccionada;
        public string PlantillaSeleccionada
        {
            get => _plantillaSeleccionada;
            set { _plantillaSeleccionada = value; OnPropertyChanged(); }
        }

        private string _nombreNuevaTabla = "";
        public string NombreNuevaTabla
        {
            get => _nombreNuevaTabla;
            set { _nombreNuevaTabla = value; OnPropertyChanged(); }
        }

        private string _mensajeError = "";
        public string MensajeError
        {
            get => _mensajeError;
            set { _mensajeError = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True cuando no hay ninguna tabla de Audiencias todavía en la BD
        /// configurada. La vista usa esto para explicar por qué el combo de
        /// plantillas está vacío, en vez de dejarlo ambiguo.
        /// </summary>
        public bool SinPlantillasDisponibles => Plantillas.Count == 0;

        public CrearTablaViewModel()
        {
            CargarPlantillas();
        }

        private void CargarPlantillas()
        {
            Plantillas.Clear();

            foreach (string tabla in _repo.ObtenerPlantillasDisponibles())
                Plantillas.Add(tabla);

            if (Plantillas.Count > 0)
            {
                PlantillaSeleccionada = Plantillas[Plantillas.Count - 1];
                NombreNuevaTabla = _repo.SugerirSiguienteNombre();
            }

            OnPropertyChanged(nameof(SinPlantillasDisponibles));
        }

        /// <summary>
        /// Intenta crear la tabla. Devuelve true si se creó correctamente;
        /// si no, deja el motivo en MensajeError y devuelve false — la
        /// vista decide qué hacer con eso (aquí: mostrarlo en pantalla, sin
        /// cerrar el diálogo, para que el usuario pueda corregir y
        /// reintentar sin perder lo que ya había escrito).
        /// </summary>
        public bool Crear()
        {
            MensajeError = "";

            if (string.IsNullOrWhiteSpace(PlantillaSeleccionada))
            {
                MensajeError = "Selecciona una tabla plantilla.";
                return false;
            }

            string errorNombre = _repo.ValidarNombre(NombreNuevaTabla);
            if (errorNombre != null)
            {
                MensajeError = errorNombre;
                return false;
            }

            try
            {
                _repo.CrearTablaDesdeTemplate(PlantillaSeleccionada, NombreNuevaTabla);
                return true;
            }
            catch (PlantillaIncompatibleException ex)
            {
                MensajeError = ex.Message;
                return false;
            }
            catch (System.Exception ex)
            {
                MensajeError = "No se pudo crear la tabla:\n" + ex.Message;
                return false;
            }
        }
    }
}