using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PoderJudicial.Helpers
{
    public static class DynamicFieldFactory
    {
        public static void CrearCampoAutocomplete(
            Panel panelDestino,
            string placeholder,
            TextChangedEventHandler textChanged,
            KeyEventHandler keyDownTextbox,
            KeyEventHandler keyDownListBox,
            MouseButtonEventHandler mouseClickListBox,
            RoutedEventHandler eliminarClick,
            Style inputStyle)
        {
            // GRID
            Grid grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            grid.ColumnDefinitions.Add(
                new ColumnDefinition());

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(8)
                });

            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(42)
                });

            // STACK
            StackPanel stack =
                new StackPanel();

            // TEXTBOX
            TextBox txt = new TextBox
            {
                Height = 42,
                FontSize = 13,
                Tag = placeholder,
                Style = inputStyle
            };

            if (textChanged != null)
                txt.TextChanged += textChanged;
            txt.PreviewKeyDown += keyDownTextbox;

            // LISTBOX
            ListBox lst = new ListBox
            {
                Height = 120,
                Visibility = Visibility.Collapsed
            };

            lst.PreviewKeyDown += keyDownListBox;
            lst.MouseDoubleClick += mouseClickListBox;

            // AGREGAR
            stack.Children.Add(txt);
            stack.Children.Add(lst);

            Grid.SetColumn(stack, 0);

            // BOTÓN X
            Button btnEliminar = new Button
            {
                Content = "X",
                Width = 42,
                Height = 42,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Background = Brushes.IndianRed,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            btnEliminar.Click += eliminarClick;

            Grid.SetColumn(btnEliminar, 2);

            // AGREGAR AL GRID
            grid.Children.Add(stack);
            grid.Children.Add(btnEliminar);

            // AGREGAR AL PANEL
            panelDestino.Children.Add(grid);

            // PLACEHOLDER
            PlaceholderHelper.AddPlaceholder(
                txt,
                placeholder);

            // FOCUS
            txt.Focus();
        }
    }
}
