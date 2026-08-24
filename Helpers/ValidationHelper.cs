using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoderJudicial.Helpers
{
    /// <summary>
    /// Punto único de validación de formato/obligatoriedad para todos los
    /// formularios de captura (NuevoRegistro, RegistroCopias, EditarRegistro).
    /// Mantener aquí las reglas evita duplicar lógica entre módulos.
    /// </summary>
    public static class ValidationHelper
    {
        // Formatos reales que la propia UI genera automáticamente
        // (ver NuevoRegistro.Inputs.cs / RegistroCopias.xaml.cs / EditarRegistro.xaml.cs).
        private static readonly string[] FormatosFecha = { "dd/MM/yyyy" };
        private static readonly string[] FormatosHora = { "HH:mm", "H:mm", "hh:mm tt", "h:mm tt" };

        // ──────────────────────────────────────────
        // SOLO NÚMEROS
        // ──────────────────────────────────────────
        public static bool SoloNumeros(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            return texto.All(char.IsDigit);
        }

        // ──────────────────────────────────────────
        // NÚMEROS Y "/"   (No. Causa, Expediente, Causa)
        // ──────────────────────────────────────────
        public static bool NumerosYDiagonal(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            return texto.All(c =>
                char.IsDigit(c) || c == '/');
        }

        // ──────────────────────────────────────────
        // NÚMEROS Y "-"   (NUC)
        // ──────────────────────────────────────────
        public static bool NumerosYGuion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            return texto.All(c =>
                char.IsDigit(c) || c == '-');
        }

        /// <summary>
        /// Igual que NumerosYDiagonal, pero además acepta letras — se usa
        /// cuando el usuario confirmó la excepción de "permitir letras"
        /// para ese campo en el registro actual (ver sección 7 del prompt).
        /// </summary>
        public static bool NumerosYDiagonalConExcepcion(string texto, bool permitirLetras)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            if (!permitirLetras) return NumerosYDiagonal(texto);

            return texto.All(c => char.IsDigit(c) || c == '/' || char.IsLetter(c));
        }

        /// <summary>Variante con excepción de letras para NUC.</summary>
        public static bool NumerosYGuionConExcepcion(string texto, bool permitirLetras)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            if (!permitirLetras) return NumerosYGuion(texto);

            return texto.All(c => char.IsDigit(c) || c == '-' || char.IsLetter(c));
        }

        // ──────────────────────────────────────────
        // FECHA VÁLIDA (estricta, formato dd/MM/yyyy)
        // ──────────────────────────────────────────
        public static bool FechaValida(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha)) return false;

            return DateTime.TryParseExact(
                fecha.Trim(),
                FormatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
        }

        // ──────────────────────────────────────────
        // HORA VÁLIDA (24h "HH:mm" o 12h "hh:mm tt")
        // Se intenta con la cultura actual (la que usó la UI para
        // formatear AM/PM) y con InvariantCulture como respaldo.
        // ──────────────────────────────────────────
        public static bool HoraValida(string hora)
        {
            if (string.IsNullOrWhiteSpace(hora)) return false;

            hora = hora.Trim();

            return DateTime.TryParseExact(hora, FormatosHora, CultureInfo.CurrentCulture,
                       DateTimeStyles.None, out _)
                || DateTime.TryParseExact(hora, FormatosHora, CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _);
        }

        // ──────────────────────────────────────────
        // OBLIGATORIO
        // ──────────────────────────────────────────
        public static bool CampoVacio(TextBox txt)
        {
            return PlaceholderHelper.IsPlaceholder(txt)
                || string.IsNullOrWhiteSpace(txt.Text);
        }

        // ──────────────────────────────────────────
        // CONFIRMACIÓN: guardar con un campo opcional vacío
        // (ej. "Causa" en Ejecución, según reglas del negocio)
        // ──────────────────────────────────────────
        public static bool ConfirmarCampoVacio(string nombreCampo)
        {
            var resultado = MessageBox.Show(
                $"El campo '{nombreCampo}' está vacío. ¿Está seguro de que desea continuar?",
                "Confirmación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return resultado == MessageBoxResult.Yes;
        }

        // ──────────────────────────────────────────
        // CONFIRMACIÓN: permitir letras en un campo que
        // normalmente solo admite números (excepción por registro)
        // ──────────────────────────────────────────
        public static bool ConfirmarPermitirLetras(string nombreCampo)
        {
            var resultado = MessageBox.Show(
                "Este campo normalmente solo admite números. " +
                "¿Desea permitir caracteres alfabéticos para este registro?",
                $"Excepción de formato — {nombreCampo}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return resultado == MessageBoxResult.Yes;
        }

        // ──────────────────────────────────────────
        // Evalúa un carácter recién tecleado contra un filtro base
        // (dígitos + separador). Si el carácter es una letra y aún no
        // se ha autorizado la excepción para este campo/registro,
        // pregunta una sola vez; si el usuario acepta, la deja pasar
        // a partir de ese momento. Cualquier otro símbolo se bloquea
        // siempre. Compartido por NuevoRegistro y RegistroCopias.
        // ──────────────────────────────────────────
        public static bool EvaluarCaracterConExcepcion(string textoIngresado, Func<char, bool> filtroBase,
            ref bool permitirLetras, string nombreCampo)
        {
            char c = textoIngresado.FirstOrDefault();
            if (filtroBase(c)) return true;
            if (permitirLetras && char.IsLetter(c)) return true;

            if (char.IsLetter(c) && ConfirmarPermitirLetras(nombreCampo))
            {
                permitirLetras = true;
                return true;
            }

            return false;
        }
    }
}
