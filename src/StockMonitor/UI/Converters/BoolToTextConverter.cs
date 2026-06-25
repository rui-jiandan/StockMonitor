using System.Globalization;
using System.Windows.Data;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 布尔值到中文文本转换器：true→"一次性"，false→"持续"
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "一次性" : "持续";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && s == "一次性";
    }
}
