using PoderJudicial.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Caché exclusivamente en memoria y por ruta de base de datos. No abre
    /// conexiones ni persiste información entre ejecuciones.
    /// </summary>
    public static class CacheSesionAccess
    {
        private sealed class CacheRuta
        {
            public long Version { get; set; }
            public Dictionary<string, object> Valores { get; } = new();
        }

        private static readonly object Sincronizacion = new();
        private static readonly Dictionary<string, CacheRuta> PorRuta =
            new(StringComparer.OrdinalIgnoreCase);

        public static long ObtenerVersionActual()
        {
            lock (Sincronizacion)
                return ObtenerCacheRuta().Version;
        }

        public static bool IntentarObtener<T>(string clave, out T? valor)
        {
            lock (Sincronizacion)
            {
                if (ObtenerCacheRuta().Valores.TryGetValue(clave, out object? dato) &&
                    dato is T convertido)
                {
                    valor = convertido;
                    return true;
                }
            }

            valor = default;
            return false;
        }

        public static bool GuardarSiVersion<T>(string clave, T valor, long version)
            where T : notnull
        {
            lock (Sincronizacion)
            {
                CacheRuta cache = ObtenerCacheRuta();
                if (cache.Version != version)
                    return false;

                cache.Valores[clave] = valor;
                return true;
            }
        }

        public static void InvalidarRutaActual()
        {
            lock (Sincronizacion)
            {
                CacheRuta cache = ObtenerCacheRuta();
                cache.Version++;
                cache.Valores.Clear();
            }
        }

        public static void InvalidarTodo()
        {
            lock (Sincronizacion)
                PorRuta.Clear();
        }

        private static CacheRuta ObtenerCacheRuta()
        {
            string ruta = NormalizarRuta(Conexion.RutaBD);
            if (!PorRuta.TryGetValue(ruta, out CacheRuta? cache))
            {
                cache = new CacheRuta();
                PorRuta[ruta] = cache;
            }

            return cache;
        }

        private static string NormalizarRuta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return string.Empty;

            try
            {
                return Path.GetFullPath(ruta.Trim());
            }
            catch
            {
                return ruta.Trim();
            }
        }
    }
}
