using PoderJudicial.Data;
using System.Collections.Generic;
using PoderJudicial.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PoderJudicial.ViewModels
{
    public class RegistroCopiasViewModel : INotifyPropertyChanged
    {
        // ── Historial para autocomplete de "A quien se entrega" ──
        public List<string> AQuienSeEntregaHistorial { get; private set; } = new();
        private Task<(List<string> Historial, int SiguienteId)>? _tareaCargaHistorial;
        private long _versionCargaHistorial = -1;
        public int SiguienteIdInicial { get; private set; }

        public RegistroCopiasViewModel()
        {
        }

        public async Task CargarHistorialAsync()
        {
            long versionActual = CacheSesionAccess.ObtenerVersionActual();
            if (_tareaCargaHistorial == null ||
                _versionCargaHistorial != versionActual)
            {
                _versionCargaHistorial = versionActual;
                _tareaCargaHistorial = CargarDatosInicialesCoordinadosAsync();
            }

            var datos = await _tareaCargaHistorial;
            AQuienSeEntregaHistorial = datos.Historial;
            SiguienteIdInicial = datos.SiguienteId;
            OnPropertyChanged(nameof(AQuienSeEntregaHistorial));
        }

        private static async Task<(List<string> Historial, int SiguienteId)>
            CargarDatosInicialesCoordinadosAsync()
        {
            const string clave = "RegistroCopias.CatalogoDestinatarios";
            using var medicion = RendimientoLog.IniciarModulo(
                "Registro Copias", "RegistroCopiasViewModel.CargarHistorialAsync");

            if (CacheSesionAccess.IntentarObtener(
                clave, out List<string>? historialCache))
            {
                int folio = await Task.Run(() =>
                    new CopiasData().ObtenerSiguienteIdVisual());
                return (historialCache, folio);
            }

            using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                "RegistroCopias.Inicializar");

            if (CacheSesionAccess.IntentarObtener(clave, out historialCache))
            {
                int folio = await Task.Run(() =>
                    new CopiasData().ObtenerSiguienteIdVisual());
                return (historialCache, folio);
            }

            long version = CacheSesionAccess.ObtenerVersionActual();
            var datos = await Task.Run(() =>
                new CopiasData().ObtenerDatosIniciales());
            CacheSesionAccess.GuardarSiVersion(
                clave, datos.Historial, version);
            return datos;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ──────────────────────────────────────────
        //  PROPIEDADES
        // ──────────────────────────────────────────
        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private DateTime? _feAudiencia;
        public DateTime? FeAudiencia
        {
            get => _feAudiencia;
            set { _feAudiencia = value; OnPropertyChanged(); }
        }

        private DateTime? _feRecibo;
        public DateTime? FeRecibo
        {
            get => _feRecibo;
            set { _feRecibo = value; OnPropertyChanged(); }
        }

        private int? _totDiscosEntregados;
        public int? TotDiscosEntregados
        {
            get => _totDiscosEntregados;
            set { _totDiscosEntregados = value; OnPropertyChanged(); }
        }

        private string _tipoDisco = string.Empty;
        public string TipoDisco
        {
            get => _tipoDisco;
            set { _tipoDisco = value; OnPropertyChanged(); }
        }

        private string _noCausa = string.Empty;
        public string NoCausa
        {
            get => _noCausa;
            set { _noCausa = value; OnPropertyChanged(); }
        }

        private string _nuc = string.Empty;
        public string NUC
        {
            get => _nuc;
            set { _nuc = value; OnPropertyChanged(); }
        }

        private string _tipoCausa = string.Empty;
        public string TipoCausa
        {
            get => _tipoCausa;
            set { _tipoCausa = value; OnPropertyChanged(); }
        }

        private int? _discosExternos;
        public int? DiscosExternos
        {
            get => _discosExternos;
            set { _discosExternos = value; OnPropertyChanged(); }
        }

        private int? _etiquetasEntregadas;
        public int? EtiquetasEntregadas
        {
            get => _etiquetasEntregadas;
            set { _etiquetasEntregadas = value; OnPropertyChanged(); }
        }

        private string _aQuienSeEntrega = string.Empty;
        public string AQuienSeEntrega
        {
            get => _aQuienSeEntrega;
            set { _aQuienSeEntrega = value; OnPropertyChanged(); }
        }

        private string _observaciones = string.Empty;
        public string Observaciones
        {
            get => _observaciones;
            set { _observaciones = value; OnPropertyChanged(); }
        }

        private string _quienRegistra = string.Empty;
        public string QuienRegistra
        {
            get => _quienRegistra;
            set { _quienRegistra = value; OnPropertyChanged(); }
        }
    }
}
