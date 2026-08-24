using System;
using System.IO;

namespace PoderJudicial.Helpers
{
    public sealed class EstadoInformesResultado
    {
        public string EstadoSimples { get; set; } = "Estado: No generado";
        public string EstadoAutenticas { get; set; } = "Estado: No generado";
        public string EstadoConsolidado { get; set; } = "Pendiente de ambos informes";
        public string EstadoInformeAnual { get; set; } = "Estado: Sin agregar";
        public string NombreArchivoAnual { get; set; } = "El documento todavía no se ha creado.";
        public string UltimaActualizacionAnual { get; set; } = "Última actualización: Sin información";
        public bool PuedeConsolidar { get; set; }
        public bool PuedeAgregarInformeAnual { get; set; }
    }

    public static class EstadoInformesHelper
    {
        public static EstadoInformesResultado ObtenerEstado(DateTime fechaInforme)
        {
            var resultado = new EstadoInformesResultado();

            string rutaSimples = RutasInformes.ObtenerRutaSimples(fechaInforme);
            string rutaAutenticas = RutasInformes.ObtenerRutaAutenticas(fechaInforme);
            string rutaConsolidado = RutasInformes.ObtenerRutaConsolidado(fechaInforme);
            string rutaAnual = RutasInformes.ObtenerRutaInformeAnual(fechaInforme.Year);

            bool existeSimples = File.Exists(rutaSimples);
            bool existeAutenticas = File.Exists(rutaAutenticas);
            bool existeConsolidado = File.Exists(rutaConsolidado);
            bool existeAnual = File.Exists(rutaAnual);

            if (existeSimples)
            {
                DateTime modificacion = File.GetLastWriteTime(rutaSimples);
                resultado.EstadoSimples = $"Estado: Generado a las {modificacion:hh:mm tt}";
            }

            if (existeAutenticas)
            {
                DateTime modificacion = File.GetLastWriteTime(rutaAutenticas);
                resultado.EstadoAutenticas = $"Estado: Generado a las {modificacion:hh:mm tt}";
            }

            // Se puede consolidar siempre que existan los dos informes del día.
            // Si ya existe un consolidado, se permitirá volver a consolidar
            // para reemplazarlo con la versión más reciente.
            resultado.PuedeConsolidar =
                existeSimples && existeAutenticas;

            resultado.PuedeAgregarInformeAnual = existeConsolidado;

            if (existeConsolidado)
                resultado.EstadoConsolidado = "Estado: Informe diario consolidado";
            else if (existeSimples && existeAutenticas)
                resultado.EstadoConsolidado = "Estado: Listo para consolidar";

            if (existeAnual)
            {
                bool agregadoAlAnual =
                    InformeCopiasService.EstaAgregadoAlAnual(fechaInforme);

                resultado.NombreArchivoAnual = Path.GetFileName(rutaAnual);

                DateTime ultimaActualizacion = File.GetLastWriteTime(rutaAnual);
                resultado.UltimaActualizacionAnual =
                    $"Última actualización: {ultimaActualizacion:dd/MM/yyyy hh:mm tt}";

                resultado.EstadoInformeAnual =
                    agregadoAlAnual
                        ? $"Estado: Agregado al informe anual {fechaInforme.Year}"
                        : "Estado: Sin agregar";
            }

            return resultado;
        }
    }
}
