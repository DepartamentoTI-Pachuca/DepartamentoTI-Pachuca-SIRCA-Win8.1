using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PoderJudicial.Views
{
    public partial class ConsolidacionCompletada : Window
    {
        private readonly string _archivo;

        public ConsolidacionCompletada(string archivo)
        {
            InitializeComponent();

            _archivo =
                archivo
                ?? throw new ArgumentNullException(nameof(archivo));

            TxtNombreArchivo.Text =
                Path.GetFileName(_archivo);

            TxtRutaArchivo.Text =
                _archivo;

            TxtFechaArchivo.Text =
                $"Última modificación: {File.GetLastWriteTime(_archivo):dd/MM/yyyy hh:mm tt}";
        }

        private void BtnCerrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(
            MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
