using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 布尔值到可见性转换器：true→Visible，false→Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
