using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectIdeas
{
    public class BoolToTextDecorationConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return TextDecorations.Strikethrough;
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}