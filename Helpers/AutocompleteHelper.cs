using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoderJudicial.Helpers
{
    public static class AutocompleteHelper
    {
        // FILTRAR
        public static void Filtrar(
            TextBox txt,
            ListBox lst,
            List<string> origen)
        {
            string texto =
                txt.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                lst.Visibility =
                    Visibility.Collapsed;

                return;
            }

            var resultados = origen
                .Where(x =>
                    x.ToLower().Contains(texto))
                .OrderBy(x => x.Length)
                .ToList();

            lst.ItemsSource = resultados;

            lst.SelectedIndex = -1;

            lst.Visibility =
                resultados.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ENTER / ESC / FLECHAS
        public static void ManejarTecladoTextBox(
            TextBox txt,
            ListBox lst,
            KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (lst.Items.Count > 0)
                {
                    lst.Focus();

                    lst.SelectedIndex = 0;
                }

                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                lst.Visibility =
                    Visibility.Collapsed;

                e.Handled = true;
            }
        }

        
// LISTBOX TECLADO
public static void ManejarTecladoListBox(
    ListBox lst,
    KeyEventArgs e)
        {
            // ENTER
            if (e.Key == Key.Enter)
            {
                SeleccionarItem(lst);

                e.Handled = true;
            }

            // TAB
            if (e.Key == Key.Tab)
            {
                SeleccionarItem(lst);

                e.Handled = true;
            }

            // ESC
            if (e.Key == Key.Escape)
            {
                lst.Visibility =
                    Visibility.Collapsed;

                StackPanel stack =
                    (StackPanel)lst.Parent;

                TextBox txt = stack.Children
                    .OfType<TextBox>()
                    .First();

                txt.Focus();

                e.Handled = true;
            }

            // BACKSPACE
            if (e.Key == Key.Back)
            {
                StackPanel stack =
                    (StackPanel)lst.Parent;

                TextBox txt = stack.Children
                    .OfType<TextBox>()
                    .First();

                lst.Visibility =
                    Visibility.Collapsed;

                txt.Focus();

                txt.CaretIndex =
                    txt.Text.Length;

                e.Handled = true;
            }
        }


        // CLICK MOUSE
        public static void ManejarClickMouse(
            ListBox lst)
        {
            SeleccionarItem(lst);
        }

        // SELECCIONAR
        private static void SeleccionarItem(ListBox lst)
        {
            if (lst.SelectedItem == null)
                return;

            StackPanel stack =
                (StackPanel)lst.Parent;

            TextBox txt =
                stack.Children
                .OfType<TextBox>()
                .First();

            txt.Text =
                lst.SelectedItem.ToString();

            txt.CaretIndex =
                txt.Text.Length;

            lst.Visibility =
                Visibility.Collapsed;

            txt.Focus();
        }

        public static void FiltrarDesdeSender(object sender, List<string> origen)
        {
            TextBox txt = (TextBox)sender;

            StackPanel stack =
                (StackPanel)txt.Parent;

            ListBox lst = stack.Children
                .OfType<ListBox>()
                .First();

            Filtrar(txt, lst, origen);
        }
    }
}