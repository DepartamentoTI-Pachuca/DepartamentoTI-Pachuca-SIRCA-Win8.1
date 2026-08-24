using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoderJudicial.Helpers
{
    public static class PlaceholderHelper
    {
        public static void AddPlaceholder(
            TextBox tb,
            string? placeholder = null)
        {
            string ph =
                placeholder ??
                (tb.Tag as string ?? "");

            if (string.IsNullOrEmpty(ph))
                return;

            tb.Foreground =
                (Brush)Application.Current.Resources["PlaceholderBrush"];

            tb.Text = ph;

            tb.GotFocus += (s, e) =>
            {
                if (tb.Text == ph)
                {
                    tb.Text = "";

                    tb.Foreground =
                        (Brush)Application.Current.Resources["InputTextBrush"];
                }
            };

            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = ph;

                    tb.Foreground =
                        (Brush)Application.Current.Resources["PlaceholderBrush"];
                }
            };
        }

        public static string GetText(TextBox tb)
        {
            string ph =
                tb.Tag as string ?? "";

            string val =
                tb.Text.Trim();

            return val == ph
                ? ""
                : val;
        }

        public static bool IsPlaceholder(TextBox tb)
        {
            string ph =
                tb.Tag as string ?? "";

            return tb.Text == ph;
        }
    }
}