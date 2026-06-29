using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 持仓状态到背景色转换器：已有持仓显示浅绿色背景
/// </summary>
public class HasPositionToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasPosition && hasPosition)
        {
            return new SolidColorBrush(Color.FromRgb(220, 245, 220)); // 浅绿色
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
