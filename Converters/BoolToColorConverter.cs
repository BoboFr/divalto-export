using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Divalto.Converters
{
    /// <summary>
    /// Convertisseur pour convertir un état booléen en couleur (Vert/Rouge)
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                if (isConnected)
                {
                    // Vert pour connecté
                    return new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    // Rouge pour déconnecté
                    return new SolidColorBrush(Color.FromArgb(255, 255, 107, 107));
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
