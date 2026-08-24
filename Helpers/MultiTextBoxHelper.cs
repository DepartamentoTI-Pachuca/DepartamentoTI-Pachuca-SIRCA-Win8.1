using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PoderJudicial.Helpers
{
    public static class MultiTextBoxHelper
    {
        public static string ObtenerTextoConcatenado(
            TextBox txtPrincipal,
            StackPanel panelExtras)
        {
            List<string> valores =
                new List<string>();

            // ─────────────────────────────
            // TEXTBOX PRINCIPAL
            // ─────────────────────────────

            string principal =
                txtPrincipal.Text.Trim();

            if (!string.IsNullOrWhiteSpace(principal))
            {
                valores.Add(principal);
            }

            // ─────────────────────────────
            // TEXTBOX DINÁMICOS
            // ─────────────────────────────

            foreach (Grid grid in panelExtras.Children)
            {
                StackPanel stack =
                    grid.Children
                        .OfType<StackPanel>()
                        .FirstOrDefault();

                if (stack == null)
                    continue;

                TextBox txt =
                    stack.Children
                         .OfType<TextBox>()
                         .FirstOrDefault();

                if (txt == null)
                    continue;

                string texto = txt.Text.Trim();

                if (!string.IsNullOrWhiteSpace(texto))
                {
                    valores.Add(texto);
                }
            }

            // ─────────────────────────────
            // QUITAR DUPLICADOS
            // ─────────────────────────────

            valores = valores
                .Distinct()
                .ToList();

            // ─────────────────────────────
            // CONCATENAR
            // ─────────────────────────────

            return string.Join(" y ", valores);
        }
    }
}