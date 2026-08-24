using System;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Única fuente de verdad para avisar que la base de datos activa
    /// cambió. No reemplaza a Conexion (que sigue siendo quien sabe la ruta
    /// actual) ni a TableDetector (que sigue siendo quien sabe las tablas
    /// actuales) — solo los conecta con quien necesite reaccionar cuando
    /// cambian, para no tener que sondear ("polling") ni duplicar la
    /// decisión de "cuándo refrescar" en varios lugares.
    /// Quien cambia la BD (ConfiguracionBaseDatos) llama a
    /// <see cref="NotificarCambio"/> después de guardar y validar la nueva
    /// configuración; quien necesita refrescarse (hoy: Dashboard, para el
    /// Sidebar y la sección actualmente abierta) se suscribe a
    /// <see cref="CambioBaseDatos"/>.
    /// </summary>
    public static class EstadoBaseDatos
    {
        public static event EventHandler CambioBaseDatos;

        public static void NotificarCambio()
        {
            CambioBaseDatos?.Invoke(null, EventArgs.Empty);
        }
    }
}