using PoderJudicial.Data;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Serializa únicamente lecturas pesadas que apuntan al mismo archivo
    /// Access. Cada ruta tiene su propio semáforo y cada operación conserva
    /// sus conexiones locales habituales.
    /// </summary>
    public static class CoordinadorLecturasAccess
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaforos =
            new(StringComparer.OrdinalIgnoreCase);

        public static async Task<IDisposable> AdquirirAsync(
            string operacion,
            CancellationToken cancellationToken = default)
        {
            string ruta = NormalizarRuta(Conexion.RutaBD);
            SemaphoreSlim semaforo = Semaforos.GetOrAdd(
                ruta, _ => new SemaphoreSlim(1, 1));

            Stopwatch espera = Stopwatch.StartNew();
            await semaforo.WaitAsync(cancellationToken);
            espera.Stop();

            RendimientoLog.Registrar(
                $"CoordinadorLecturasAccess.{operacion}",
                Path.GetFileName(ruta),
                "Permiso adquirido",
                queryMs: espera.ElapsedMilliseconds);

            return new Permiso(semaforo, operacion, ruta);
        }

        private static string NormalizarRuta(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return "<sin-ruta>";

            try
            {
                return Path.GetFullPath(ruta.Trim());
            }
            catch
            {
                return ruta.Trim();
            }
        }

        private sealed class Permiso : IDisposable
        {
            private readonly SemaphoreSlim _semaforo;
            private readonly string _operacion;
            private readonly string _ruta;
            private readonly Stopwatch _duracion = Stopwatch.StartNew();
            private bool _liberado;

            public Permiso(SemaphoreSlim semaforo, string operacion, string ruta)
            {
                _semaforo = semaforo;
                _operacion = operacion;
                _ruta = ruta;
            }

            public void Dispose()
            {
                if (_liberado) return;
                _liberado = true;
                _duracion.Stop();

                RendimientoLog.Registrar(
                    $"CoordinadorLecturasAccess.{_operacion}",
                    Path.GetFileName(_ruta),
                    "Permiso liberado",
                    totalMs: _duracion.ElapsedMilliseconds);

                _semaforo.Release();
            }
        }
    }
}
