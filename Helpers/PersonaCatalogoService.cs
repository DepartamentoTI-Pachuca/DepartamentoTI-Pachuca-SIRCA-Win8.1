using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PoderJudicial.Helpers
{
    public class CatalogoPersonasData
    {
        public List<string> EntreganSimples { get; set; } = new();
        public List<string> RecibenSimples { get; set; } = new();
        public List<string> EntreganAutenticas { get; set; } = new();
        public List<string> RecibenAutenticas { get; set; } = new();

        // Propiedades legacy para poder leer un personas.json anterior
        // sin provocar errores. No se vuelven a guardar.
        public List<string> Entregan { get; set; } = new();
        public List<string> Reciben { get; set; } = new();
    }

    public static class PersonaCatalogoService
    {
        public static CatalogoPersonasData Cargar()
        {
            RutasInformes.CrearEstructura();

            string ruta =
                RutasInformes.ObtenerRutaCatalogoPersonas();

            if (!File.Exists(ruta))
                return new CatalogoPersonasData();

            string json =
                File.ReadAllText(ruta);

            if (string.IsNullOrWhiteSpace(json))
                return new CatalogoPersonasData();

            CatalogoPersonasData datos =
                JsonSerializer.Deserialize<CatalogoPersonasData>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? new CatalogoPersonasData();

            datos.EntreganSimples ??= new();
            datos.RecibenSimples ??= new();
            datos.EntreganAutenticas ??= new();
            datos.RecibenAutenticas ??= new();
            datos.Entregan ??= new();
            datos.Reciben ??= new();

            // Migración segura del formato viejo:
            // los nombres viejos no indican si eran simples o auténticas.
            // Se conservan solo como respaldo en las propiedades legacy
            // y NO se mezclan automáticamente con los nuevos catálogos.

            return datos;
        }

        public static void Guardar(
            IEnumerable<string> entreganSimples,
            IEnumerable<string> recibenSimples,
            IEnumerable<string> entreganAutenticas,
            IEnumerable<string> recibenAutenticas)
        {
            RutasInformes.CrearEstructura();

            CatalogoPersonasData datos =
                new CatalogoPersonasData
                {
                    EntreganSimples =
                        Normalizar(entreganSimples),

                    RecibenSimples =
                        Normalizar(recibenSimples),

                    EntreganAutenticas =
                        Normalizar(entreganAutenticas),

                    RecibenAutenticas =
                        Normalizar(recibenAutenticas)
                };

            string json =
                JsonSerializer.Serialize(
                    datos,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                RutasInformes.ObtenerRutaCatalogoPersonas(),
                json);
        }

        private static List<string> Normalizar(
            IEnumerable<string>? nombres)
        {
            return (nombres ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }
    }
}
