using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoderJudicial.Data;
using PoderJudicial.Helpers;
using PoderJudicial.Models;

namespace PoderJudicial.ViewModels
{
    public class ReportesViewModel
    {
        private readonly AudienciaData _data = new AudienciaData();
        private readonly CopiasData _copiasData = new CopiasData();
        private readonly EjecucionData _ejecucionData = new EjecucionData();

        public List<Audiencia> Todas { get; private set; } = new();
        public List<Audiencia> ResultadosFiltrados { get; private set; } = new();
        public List<Audiencia> EjecucionesParaReportes { get; private set; } = new();
        public List<RegistroCopia> TodasCopias { get; private set; } = new();
        public List<RegistroCopia> CopiasFiltradas { get; private set; } = new();
        private int _totalCopiasSimples;
        private int _totalCopiasAutenticas;

        public ObservableCollection<string> CatalogoEntreganSimples { get; } = new();
        public ObservableCollection<string> CatalogoRecibenSimples { get; } = new();
        public ObservableCollection<string> CatalogoEntreganAutenticas { get; } = new();
        public ObservableCollection<string> CatalogoRecibenAutenticas { get; } = new();
        public ObservableCollection<string> RecibieronSimples { get; } = new();
        public ObservableCollection<string> RecibieronAutenticas { get; } = new();

        public DateTime FechaInforme => DateTime.Today;

        public int TotalRegistros => ResultadosFiltrados.Count;

        public int TotalDiscos => ResultadosFiltrados.Sum(x =>
        {
            if (string.IsNullOrWhiteSpace(x.TotDiscoAudiencia))
                return 0;

            string numeros = new string(x.TotDiscoAudiencia.Where(char.IsDigit).ToArray());
            return int.TryParse(numeros, out int valor) ? valor : 0;
        });

        public int TotalCopiasSimples => _totalCopiasSimples;

        public int TotalCopiasAutenticas => _totalCopiasAutenticas;

        public Task InicializarDatosAsync()
        {
            return Task.Run(() =>
            {
                RutasInformes.CrearEstructura();
                InformeCopiasService.ArchivarTemporalesVencidos(FechaInforme);
            });
        }

        public async Task ConsultarAsync(
            int anio, string tipoCausa, string mes, string juzgado, string sala)
        {
            bool esExp = NormalizarTexto(tipoCausa) == "EXP";
            string claveAnual = esExp
                ? $"Reportes.Ejecuciones.Anio.{anio}"
                : $"Reportes.Audiencias.Anio.{anio}";
            int? mesNumero = ObtenerNumeroMes(mes);
            string claveCopias = $"Reportes.CopiasTotales.{anio}." +
                $"{mesNumero?.ToString() ?? "Todos"}.{NormalizarTexto(tipoCausa)}";

            bool tieneAnual = CacheSesionAccess.IntentarObtener(
                claveAnual, out List<Audiencia>? datosAnuales);
            bool tieneCopias = CacheSesionAccess.IntentarObtener(
                claveCopias, out ValueTuple<int, int> totalesCopias);
            bool requiereLectura = !tieneAnual || !tieneCopias;

            IDisposable? permiso = null;
            if (requiereLectura)
                permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                    "Reportes.Consultar");

            try
            {
                if (!tieneAnual)
                {
                    tieneAnual = CacheSesionAccess.IntentarObtener(
                        claveAnual, out datosAnuales);
                    if (!tieneAnual)
                    {
                        long version = CacheSesionAccess.ObtenerVersionActual();
                        datosAnuales = esExp
                            ? await Task.Run(() => _ejecucionData
                                .ObtenerEjecucionesParaReportes(anio)
                                .Select(MapearEjecucionParaReporte)
                                .ToList())
                            : await Task.Run(() =>
                                _data.ObtenerAudienciasParaReportes(anio));
                        CacheSesionAccess.GuardarSiVersion(
                            claveAnual, datosAnuales, version);
                    }
                }

                if (!tieneCopias)
                {
                    tieneCopias = CacheSesionAccess.IntentarObtener(
                        claveCopias, out totalesCopias);
                    if (!tieneCopias)
                    {
                        long version = CacheSesionAccess.ObtenerVersionActual();
                        totalesCopias = await Task.Run(() =>
                            _copiasData.ObtenerTotalesReporte(
                                anio, mesNumero, tipoCausa));
                        CacheSesionAccess.GuardarSiVersion(
                            claveCopias, totalesCopias, version);
                    }
                }

                if (esExp)
                    EjecucionesParaReportes = datosAnuales ?? new();
                else
                    Todas = datosAnuales ?? new();

                _totalCopiasSimples = totalesCopias.Item1;
                _totalCopiasAutenticas = totalesCopias.Item2;

                ResultadosFiltrados = new();
            }
            finally
            {
                permiso?.Dispose();
            }
        }

        public async Task AsegurarCopiasCargadasAsync()
        {
            TodasCopias = await CargarCopiasCoordinadasAsync();
            AplicarFiltrosCopias();
        }

        private async Task<List<RegistroCopia>> CargarCopiasCoordinadasAsync()
        {
            const string clave = "Reportes.Copias.Completas";
            if (CacheSesionAccess.IntentarObtener(
                clave, out List<RegistroCopia>? copias))
                return copias;

            using var permiso = await CoordinadorLecturasAccess.AdquirirAsync(
                "Reportes.CargarCopias");
            if (CacheSesionAccess.IntentarObtener(clave, out copias))
                return copias;

            long version = CacheSesionAccess.ObtenerVersionActual();
            copias = await Task.Run(() => _copiasData.ObtenerCopias());
            CacheSesionAccess.GuardarSiVersion(clave, copias, version);
            return copias;
        }

        public void CargarCatalogosPersonas()
        {
            CatalogoPersonasData catalogo = PersonaCatalogoService.Cargar();

            CatalogoEntreganSimples.Clear();
            CatalogoRecibenSimples.Clear();
            CatalogoEntreganAutenticas.Clear();
            CatalogoRecibenAutenticas.Clear();

            CargarColeccion(CatalogoEntreganSimples, catalogo.EntreganSimples);
            CargarColeccion(CatalogoRecibenSimples, catalogo.RecibenSimples);
            CargarColeccion(CatalogoEntreganAutenticas, catalogo.EntreganAutenticas);
            CargarColeccion(CatalogoRecibenAutenticas, catalogo.RecibenAutenticas);
        }

        private static void CargarColeccion(
            ObservableCollection<string> destino,
            IEnumerable<string> origen)
        {
            foreach (string nombre in origen
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x))
            {
                destino.Add(nombre);
            }
        }

        public void GuardarCatalogosPersonas()
        {
            PersonaCatalogoService.Guardar(
                CatalogoEntreganSimples,
                CatalogoRecibenSimples,
                CatalogoEntreganAutenticas,
                CatalogoRecibenAutenticas);
        }

        public void CargarDatos()
        {
            Todas = _data.ObtenerTodasAudienciasParaReportes();
            EjecucionesParaReportes = _ejecucionData
                .ObtenerEjecucionesParaReportes()
                .Select(MapearEjecucionParaReporte)
                .ToList();
            TodasCopias = _copiasData.ObtenerCopias();
            AplicarFiltrosCopias();
        }

        public void AplicarFiltros(
            string mes,
            string anio,
            string juzgado,
            string sala,
            string tipoCausa)
        {
            string claveResultado = "Reportes.Resultado." +
                $"{NormalizarTexto(anio)}.{NormalizarTexto(mes)}." +
                $"{NormalizarTexto(juzgado)}.{NormalizarTexto(sala)}." +
                NormalizarTexto(tipoCausa);
            if (CacheSesionAccess.IntentarObtener(
                claveResultado, out List<Audiencia>? resultadoCache))
            {
                ResultadosFiltrados = resultadoCache;
                return;
            }

            long version = CacheSesionAccess.ObtenerVersionActual();
            int? mesNum = ObtenerNumeroMes(mes);
            int? anioNum = int.TryParse(anio, out int a) ? a : null;

            bool esExp = NormalizarTexto(tipoCausa) == "EXP";
            IEnumerable<Audiencia> filtradas = esExp
                ? EjecucionesParaReportes
                : Todas;

            if (mesNum.HasValue)
            {
                filtradas = filtradas.Where(x =>
                    x.FechaAudiencia.HasValue &&
                    x.FechaAudiencia.Value.Month == mesNum.Value);
            }

            if (anioNum.HasValue)
            {
                filtradas = filtradas.Where(x =>
                    x.FechaAudiencia.HasValue &&
                    x.FechaAudiencia.Value.Year == anioNum.Value);
            }

            if (!esExp && !EsFiltroTodos(juzgado))
            {
                filtradas = filtradas.Where(x =>
                    string.Equals(
                        x.Juzgado?.Trim(),
                        juzgado.Trim(),
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!EsFiltroTodos(sala))
            {
                filtradas = filtradas.Where(x =>
                    string.Equals(
                        x.Sala?.Trim(),
                        sala.Trim(),
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!esExp && !EsFiltroTodos(tipoCausa))
            {
                string valorFiltro = NormalizarTexto(tipoCausa);

                filtradas = filtradas.Where(x =>
                    !string.IsNullOrWhiteSpace(x.TipoCausa) &&
                    NormalizarTexto(x.TipoCausa) == valorFiltro);
            }

            ResultadosFiltrados = filtradas.ToList();
            CacheSesionAccess.GuardarSiVersion(
                claveResultado, ResultadosFiltrados, version);
        }

        public IReadOnlyList<Audiencia> ObtenerFuenteOpciones(string tipoCausa)
            => NormalizarTexto(tipoCausa) == "EXP"
                ? EjecucionesParaReportes
                : Todas;

        private static Audiencia MapearEjecucionParaReporte(Ejecucion ejecucion)
        {
            int? totalDiscos = null;
            if (!string.IsNullOrWhiteSpace(ejecucion.TotalDiscos))
            {
                string numeros = new string(
                    ejecucion.TotalDiscos.Where(char.IsDigit).ToArray());
                if (int.TryParse(numeros, out int total))
                    totalDiscos = total;
            }

            return new Audiencia
            {
                Id = ejecucion.Id,
                FechaAudiencia = ejecucion.FechaAudiencia,
                TotDiscos = totalDiscos,
                TotDiscoAudiencia = ejecucion.TotalDiscos,
                Juzgado = "",
                Juez = ejecucion.Juez,
                NoCausa = ejecucion.Causa,
                NUC = "",
                TipoCausa = "EXP",
                TipoAudiencia = ejecucion.TipoAudiencia,
                Imputado = ejecucion.Imputado,
                Delito = ejecucion.Delito,
                Agraviado = ejecucion.Victima,
                Sala = ejecucion.Sala,
                Expediente = ejecucion.ExpedienteNumero,
                Observaciones = ejecucion.Observaciones
            };
        }

        public void AplicarFiltrosCopias()
        {
            CopiasFiltradas = TodasCopias
                .Where(c =>
                    c.FeRecibo.HasValue &&
                    c.FeRecibo.Value.Date == FechaInforme.Date)
                .OrderBy(c => c.FeRecibo)
                .ThenBy(c => c.Id)
                .ToList();
        }

        public List<RegistroCopia> ObtenerCopiasSimples()
        {
            return CopiasFiltradas.Where(EsCopiaSimple).ToList();
        }

        public List<RegistroCopia> ObtenerCopiasAutenticas()
        {
            return CopiasFiltradas.Where(EsCopiaAutentica).ToList();
        }

        public static bool EsFiltroTodos(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ||
                   valor.Equals("Todos", StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals("Todas", StringComparison.OrdinalIgnoreCase);
        }

        public static void AgregarAlCatalogo(
            ObservableCollection<string> catalogo,
            string nombre)
        {
            nombre = nombre?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            bool yaExiste = catalogo.Any(persona =>
                string.Equals(persona, nombre, StringComparison.OrdinalIgnoreCase));

            if (!yaExiste)
                catalogo.Add(nombre);
        }

        private static bool EsCopiaSimple(RegistroCopia copia)
        {
            string tipo = NormalizarTexto(copia?.TipoDisco ?? string.Empty);
            return tipo.Contains("SIMP");
        }

        private static bool EsCopiaAutentica(RegistroCopia copia)
        {
            string tipo = NormalizarTexto(copia?.TipoDisco ?? string.Empty);
            return tipo.Contains("AUT");
        }

        private static int? ObtenerNumeroMes(string nombre) => nombre switch
        {
            "Enero" => 1,
            "Febrero" => 2,
            "Marzo" => 3,
            "Abril" => 4,
            "Mayo" => 5,
            "Junio" => 6,
            "Julio" => 7,
            "Agosto" => 8,
            "Septiembre" => 9,
            "Octubre" => 10,
            "Noviembre" => 11,
            "Diciembre" => 12,
            _ => null
        };

        public static string NormalizarTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto = valor
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var caracteres = texto.Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c) !=
                UnicodeCategory.NonSpacingMark);

            return new string(caracteres.ToArray())
                .Replace(" ", string.Empty)
                .Normalize(NormalizationForm.FormC);
        }
    }
}
