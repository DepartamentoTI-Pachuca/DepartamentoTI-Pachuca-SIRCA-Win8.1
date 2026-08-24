using System;
using System.IO;

namespace PoderJudicial.Helpers
{
    public static class RutasInformes
    {
        private const string NombreAplicacion = "PoderJudicial";

        public static string CarpetaBase =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                NombreAplicacion);

        public static string CarpetaInformes =>
            Path.Combine(
                CarpetaBase,
                "Informes");

        public static string CarpetaTemporales =>
            Path.Combine(
                CarpetaInformes,
                "Temporales");

        public static string CarpetaPendientes =>
            Path.Combine(
                CarpetaInformes,
                "Pendientes");

        public static string CarpetaConfiguracion =>
            Path.Combine(
                CarpetaBase,
                "Configuracion");

        public static string ObtenerRutaSimples(DateTime fecha)
        {
            return Path.Combine(
                CarpetaTemporales,
                $"Copias_Simples_{fecha:yyyy-MM-dd}.docx");
        }

        public static string ObtenerRutaAutenticas(DateTime fecha)
        {
            return Path.Combine(
                CarpetaTemporales,
                $"Copias_Autenticas_{fecha:yyyy-MM-dd}.docx");
        }

        public static string ObtenerRutaConsolidado(DateTime fecha)
        {
            return Path.Combine(
                CarpetaTemporales,
                $"Informe_Consolidado_{fecha:yyyy-MM-dd}.docx");
        }

        public static string ObtenerRutaInformeAnual(int anio)
        {
            return Path.Combine(
                CarpetaInformes,
                $"Informes_{anio}.docx");
        }

        public static string ObtenerRutaCatalogoPersonas()
        {
            return Path.Combine(
                CarpetaConfiguracion,
                "personas.json");
        }

        public static void CrearEstructura()
        {
            Directory.CreateDirectory(CarpetaBase);
            Directory.CreateDirectory(CarpetaInformes);
            Directory.CreateDirectory(CarpetaTemporales);
            Directory.CreateDirectory(CarpetaPendientes);
            Directory.CreateDirectory(CarpetaConfiguracion);
        }
    }
}
