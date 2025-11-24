using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Divalto.Converters
{
    /// <summary>
    /// Convertisseur WPF pour convertir une string vide/null en Visibility.Visible, sinon Collapsed
    /// Utilisé pour afficher un placeholder quand un TextBox est vide
    /// </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
