using System;
using System.Windows;

namespace PoderJudicial.Helpers
{
    public static class ThemeManager
    {
        public static void CambiarTema(string tema)
        {
            string ruta = "";

            switch (tema)
            {
                case "Dark":
                    ruta = "Themes/DarkTheme.xaml";
                    break;

                default:
                    ruta = "Themes/LightTheme.xaml";
                    break;
            }

            var nuevoTema = new ResourceDictionary()
            {
                Source = new Uri(ruta, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();

            Application.Current.Resources.MergedDictionaries.Add(nuevoTema);
        }
    }
}