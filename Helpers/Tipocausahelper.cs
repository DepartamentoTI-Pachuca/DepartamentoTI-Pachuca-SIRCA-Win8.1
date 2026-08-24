using System;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Regla única sobre qué campos corresponden a cada Tipo de Causa.
    /// La usan tanto "Nuevo Registro" (AudienciaFormControl, al capturar)
    /// como "Ver Detalle" (VerDetalleRegistro, al consultar), para que la
    /// regla nunca se defina dos veces ni pueda quedar desincronizada entre
    /// ambos formularios.
    /// </summary>
    public static class TipoCausaHelper
    {
        /// <summary>
        /// "No. Causa Juicio" solo corresponde al tipo de causa JO.
        /// </summary>
        public static bool MuestraNoCausaJuicio(string tipoCausa)
            => string.Equals(tipoCausa?.Trim(), "JO", StringComparison.OrdinalIgnoreCase);
    }
}