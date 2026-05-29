using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 盈亏颜色转换器：Red→暗红色，#00FF00→暗绿色，其他→灰白色
/// </summary>
public class PnLToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush UpBrush = new(Color.FromRgb(0xCC, 0x44, 0x44));
    private static readonly SolidColorBrush DownBrush = new(Color.FromRgb(0x44, 0xAA, 0x44));
    private static readonly SolidColorBrush FlatBrush = new(Color.FromRgb(0xAA, 0xAA, 0xAA));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorStr)
        {
            return colorStr switch
            {
                "Red" => UpBrush,
                "#00FF00" => DownBrush,
                _ => FlatBrush
            };
        }
        return FlatBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
