using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoderJudicial.ViewModels
{
    public class NuevoRegistroViewModel : BaseViewModel
    {
        // ── Historial para autocomplete ──────────────────
        public List<Audiencia> HistorialAudiencias { get; private set; } = new();
        public List<Ejecucion> HistorialEjecuciones { get; private set; } = new();
        public List<string> Jueces { get; private set; } = new();
        private Task<(List<Audiencia> Audiencias, List<Ejecucion> Ejecuciones,
            List<string> Jueces)>? _tareaInicializacion;
        private long _versionInicializacion = -1;
        private readonly Dictionary<string, Task<int>> _foliosVisuales = new();

        public NuevoRegistroViewModel()
        {
        }

        public async Task InicializarAsync()
        {
            long versionActual = CacheSesionAccess.ObtenerVersionActual();
            if (_tareaInicializacion == null ||
                _versionInicializacion != versionActual)
            {
                _versionInicializacion = versionActual;
                _tareaInicializacion = CargarDatosInicialesCoordinadosAsync();
            }

            var datos = await _tareaInicializacion;
            HistorialAudiencias = datos.Audiencias;
            HistorialEjecuciones = datos.Ejecuciones;
            Jueces = datos.Jueces;
        }

        private static async Task<(List<Audiencia> Audiencias,
            List<Ejecucion> Ejecuciones, List<string> Jueces)>
            CargarDatosInicialesCoordinadosAsync()
        {
            const string clave = "NuevoRegistro.CatalogosHistoricos";
            using var medicion = RendimientoLog.IniciarModulo(
                "Nuevo Registro", "NuevoRegistroViewModel.InicializarAsync");
            if (CacheSesionAccess.IntentarObtener(
                clave, out ValueTuple<List<Audiencia>, List<Ejecucion>,
                    List<string>> datosCache))
                return datosCache;

            using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                "NuevoRegistro.Inicializar");

            if (CacheSesionAccess.IntentarObtener(clave, out datosCache))
                return datosCache;

            long version = CacheSesionAccess.ObtenerVersionActual();
            var datos = await Task.Run(() =>
            {
                List<Audiencia> audiencias = new AudienciaData().ObtenerAudiencias();
                List<Ejecucion> ejecuciones = new EjecucionData().ObtenerEjecuciones();
                List<string> jueces = new JuezRepository().ObtenerJueces();
                return (audiencias, ejecuciones, jueces);
            });
            CacheSesionAccess.GuardarSiVersion(clave, datos, version);
            return datos;
        }

        public Task<int> ObtenerSiguienteIdAsync(string tipoCausa)
        {
            string clave = tipoCausa == "EXP" ? "EXP" : "AUD";
            if (!_foliosVisuales.TryGetValue(clave, out Task<int>? tarea))
            {
                tarea = Task.Run(() =>
                {
                    using var medicion = RendimientoLog.IniciarModulo(
                        "Nuevo Registro", "NuevoRegistroViewModel.ObtenerSiguienteId");
                    return ObtenerSiguienteId(tipoCausa);
                });
                _foliosVisuales[clave] = tarea;
            }

            return tarea;
        }

        public void InvalidarFoliosVisuales() => _foliosVisuales.Clear();

        // ── Siguiente ID visual ──────────────────────────
        public int ObtenerSiguienteId(string tipoCausa)
        {
            return tipoCausa == "EXP"
                ? new EjecucionData().ObtenerSiguienteId()
                : new AudienciaData().ObtenerSiguienteIdVisual();
        }

        // ── Delitos filtrados por tipo de causa ──────────
        public List<string> ObtenerDelitosFiltrados(string tipoCausa)
        {
            if (tipoCausa == "EXP")
                return HistorialEjecuciones
                    .Where(x => !string.IsNullOrWhiteSpace(x.Delito))
                    .Select(x => x.Delito).Distinct().ToList();

            return HistorialAudiencias
                .Where(x => x.TipoCausa == tipoCausa &&
                            !string.IsNullOrWhiteSpace(x.Delito))
                .Select(x => x.Delito).Distinct().ToList();
        }

        // ── Tipos de audiencia filtrados ─────────────────
        public List<string> ObtenerAudienciasFiltradas(string tipoCausa)
        {
            if (tipoCausa == "EXP")
                return HistorialEjecuciones
                    .Where(x => !string.IsNullOrWhiteSpace(x.TipoAudiencia))
                    .Select(x => x.TipoAudiencia).Distinct().ToList();

            return HistorialAudiencias
                .Where(x => x.TipoCausa == tipoCausa &&
                            !string.IsNullOrWhiteSpace(x.TipoAudiencia))
                .Select(x => x.TipoAudiencia).Distinct().ToList();
        }

        // ── Guardar Audiencia ────────────────────────────
        public void GuardarAudiencia(Audiencia registro)
        {
            new AudienciaData().Insertar(registro);
        }

        // ── Guardar Ejecucion ────────────────────────────
        public void GuardarEjecucion(Ejecucion expediente)
        {
            new EjecucionData().Insertar(expediente);
        }

        // ── Actualizar Audiencia (modo edición) ───────────
        public void ActualizarAudiencia(Audiencia registro)
        {
            new AudienciaData().Actualizar(registro);
        }

        // ── Actualizar Ejecucion (modo edición) ───────────
        public void ActualizarEjecucion(Ejecucion expediente)
        {
            new EjecucionData().Actualizar(expediente);
        }

        // ── Construir modelo Audiencia ───────────────────
        public Audiencia ConstruirAudiencia(
            int id, string noCausa, string nuc,
            DateTime? fechaAudiencia, DateTime? fechaRecibo,
            string tipoAudiencia, string tipoCausa,
            string juzgado, string juez, string imputado,
            string delito, string agraviado, string sala,
            DateTime? horaConclusion, string noCausaJuicio,
            int? totDiscos, string totDiscoAudiencia,
            bool esVideoconferencia = false, bool esConcentrada = false)
        {
            return new Audiencia
            {
                Id = id,
                NoCausa = noCausa,
                NUC = nuc,
                FechaAudiencia = fechaAudiencia,
                FechaRecibo = fechaRecibo,
                TipoAudiencia = tipoAudiencia,
                TipoCausa = tipoCausa,
                Juzgado = juzgado,
                Juez = juez,
                Imputado = imputado,
                Delito = delito,
                Agraviado = agraviado,
                Sala = sala,
                HoraConclusion = horaConclusion,
                NoCausaJuicio = noCausaJuicio,
                Diferida = string.Empty,
                QuienRealiza = ModalidadAudienciaHelper.ConstruirRegistro(SesionActual.Usuario, esVideoconferencia, esConcentrada),
                TotDiscos = totDiscos,
                TipoDisco = "Archivo",
                TotDiscoAudiencia = totDiscoAudiencia
            };
        }

        // ── Construir modelo Ejecucion ───────────────────
        public Ejecucion ConstruirEjecucion(
            int id, DateTime? fechaAudiencia,
            string totalDiscos, string juez,
            string expedienteNumero, string causa,
            string tipoAudiencia, string horaTermino,
            string imputado, string delito,
            string victima, string sala,
            bool esVideoconferencia = false, bool esConcentrada = false)
        {
            return new Ejecucion
            {
                Id = id,
                FechaAudiencia = fechaAudiencia,
                TotalDiscos = totalDiscos,
                Juez = juez,
                ExpedienteNumero = expedienteNumero,
                Causa = causa,
                TipoAudiencia = tipoAudiencia,
                HoraTermino = horaTermino,
                Imputado = imputado,
                Delito = delito,
                Victima = victima,
                Sala = sala,
                Observaciones = ModalidadAudienciaHelper.ConstruirRegistro(SesionActual.Usuario, esVideoconferencia, esConcentrada)
            };
        }
    }
}
