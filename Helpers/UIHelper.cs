using PoderJudicial.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PoderJudicial.Helpers
{
    public static class UIHelper
    {
        private static readonly string[] Placeholders =
        {
            "Nombre del juez", "Escriba el nombre del juez",
            "Tipo de delito", "Tipo de Audiencia",
            "Escriba el tipo de audiencia",
            "Ej: 123/2024", "Ej: 89/2024", "Ej: 12-2024-00567",
            "hh:mm", "dd/MM/yyyy"
        };

        public static string ObtenerTexto(TextBox txt)
        {
            if (txt == null) return string.Empty;
            string texto = txt.Text?.Trim() ?? "";
            if (PlaceholderHelper.IsPlaceholder(txt)) return string.Empty;
            return Placeholders.Contains(texto) ? string.Empty : texto;
        }

        public static string ObtenerValorCombo(ComboBox combo)
        {
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return string.Empty;
            string content = item.Content?.ToString() ?? string.Empty;
            return content.StartsWith("Seleccione") ? string.Empty : content;
        }

        public static string ObtenerValorComboOtro(ComboBox combo, TextBox txtOtro)
        {
            var item = combo.SelectedItem as ComboBoxItem;
            if (item == null) return string.Empty;
            string content = item.Content?.ToString() ?? string.Empty;
            return (content == "Otro..." || content == "Otra...")
                ? txtOtro.Text.Trim()
                : content;
        }

        public static string ObtenerTextosPanelDinamico(StackPanel panel, TextBox txtPrincipal)
        {
            var valores = new List<string>();

            string valorPrincipal = ObtenerTexto(txtPrincipal);
            if (!string.IsNullOrWhiteSpace(valorPrincipal))
                valores.Add(valorPrincipal);

            foreach (var child in panel.Children)
            {
                if (child is not Grid grid) continue;
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is not StackPanel sp) continue;
                    TextBox txt = sp.Children.OfType<TextBox>().FirstOrDefault();
                    if (txt == null) continue;
                    string valor = ObtenerTexto(txt);
                    if (!string.IsNullOrWhiteSpace(valor))
                        valores.Add(valor);
                }
            }

            return string.Join(" / ", valores);
        }

        public static string ObtenerImputados(StackPanel panel, TextBox txtPrincipal)
        {
            List<string> valores = ObtenerValores(panel, txtPrincipal);
            if (valores.Count == 0) return string.Empty;
            if (valores.Count == 1) return valores[0];
            if (valores.Count == 2) return valores[0] + " y " + valores[1];

            return string.Join(", ", valores.Take(valores.Count - 1)) +
                   " y " + valores[valores.Count - 1];
        }

        public static List<string> SepararImputados(string valor)
        {
            var resultado = new List<string>();
            if (string.IsNullOrWhiteSpace(valor)) return resultado;

            string[] partesComa = valor.Split(
                new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string parte in partesComa)
            {
                string texto = parte.Trim();
                int ultimoY = texto.LastIndexOf(" y ", StringComparison.OrdinalIgnoreCase);
                if (ultimoY > 0)
                {
                    resultado.Add(texto.Substring(0, ultimoY).Trim());
                    resultado.Add(texto.Substring(ultimoY + 3).Trim());
                }
                else if (!string.IsNullOrWhiteSpace(texto))
                {
                    resultado.Add(texto);
                }
            }

            return resultado.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private static List<string> ObtenerValores(StackPanel panel, TextBox txtPrincipal)
        {
            var valores = new List<string>();
            string principal = ObtenerTexto(txtPrincipal);
            if (!string.IsNullOrWhiteSpace(principal)) valores.Add(principal);

            foreach (Grid grid in panel.Children.OfType<Grid>())
            {
                TextBox txt = grid.Children.OfType<StackPanel>()
                    .SelectMany(sp => sp.Children.OfType<TextBox>())
                    .FirstOrDefault();
                string valor = txt == null ? string.Empty : ObtenerTexto(txt);
                if (!string.IsNullOrWhiteSpace(valor)) valores.Add(valor);
            }

            return valores;
        }
    }
}
