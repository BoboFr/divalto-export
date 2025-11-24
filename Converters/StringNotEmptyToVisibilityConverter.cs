using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Divalto.Converters
{
    /// <summary>
    /// Convertisseur WPF pour convertir une string non vide en Visibility.Visible, sinon Collapsed
    /// </summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrEmpty(stringValue) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
