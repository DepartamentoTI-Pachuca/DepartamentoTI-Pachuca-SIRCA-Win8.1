using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace PoderJudicial.Helpers
{
    /// <summary>Instrumentación temporal de tiempos para los flujos Access.</summary>
    public static class RendimientoLog
    {
        private static readonly object EscrituraLock = new();
        private static readonly AsyncLocal<string?> ModuloActual = new();
        private static readonly string RutaLog = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PoderJudicial", "rendimiento.log");

        static RendimientoLog()
        {
            Escribir($"{Environment.NewLine}========== EJECUCIÓN {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ==========");
        }

        public static IDisposable IniciarModulo(string modulo, string operacion)
        {
            string? anterior = ModuloActual.Value;
            ModuloActual.Value = modulo;
            Registrar(operacion, "", "Inicio");
            return new ContextoModulo(anterior, operacion);
        }

        public static void Registrar(
            string metodo,
            string tabla,
            string operacion,
            long? openMs = null,
            long? queryMs = null,
            long? mappingMs = null,
            int? rows = null,
            long? totalMs = null)
        {
            string? modulo = ModuloActual.Value;
            if (string.IsNullOrWhiteSpace(modulo))
                return;
            var linea = new StringBuilder($"[{modulo}] {metodo}");
            if (!string.IsNullOrWhiteSpace(tabla)) linea.Append($" | Tabla={tabla}");
            if (!string.IsNullOrWhiteSpace(operacion)) linea.Append($" | Operación={operacion}");
            if (openMs.HasValue) linea.Append($" | Open={openMs.Value}ms");
            if (queryMs.HasValue) linea.Append($" | Query={queryMs.Value}ms");
            if (mappingMs.HasValue) linea.Append($" | Mapping={mappingMs.Value}ms");
            if (rows.HasValue) linea.Append($" | Rows={rows.Value}");
            if (totalMs.HasValue) linea.Append($" | Total={totalMs.Value}ms");
            Escribir($"{DateTime.Now:HH:mm:ss.fff} {linea}");
        }

        private static void Escribir(string linea)
        {
            try
            {
                lock (EscrituraLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(RutaLog)!);
                    File.AppendAllText(RutaLog, linea + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Un fallo del archivo de diagnóstico no debe cambiar el
                // manejo de errores funcional de la aplicación.
            }
        }

        private sealed class ContextoModulo : IDisposable
        {
            private readonly string? _anterior;
            private readonly string _operacion;
            private readonly Stopwatch _total = Stopwatch.StartNew();
            private bool _disposed;

            public ContextoModulo(string? anterior, string operacion)
            {
                _anterior = anterior;
                _operacion = operacion;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _total.Stop();
                Registrar(_operacion, "", "Fin", totalMs: _total.ElapsedMilliseconds);
                ModuloActual.Value = _anterior;
            }
        }
    }
}
