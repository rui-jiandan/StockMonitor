using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StockMonitor.Core.Models;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 持仓状态到背景色转换器：已有持仓（包括今日卖出持仓为0的）显示浅蓝色背景
/// </summary>
public class PositionToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StockPosition position)
        {
            // 当前有持仓数量 > 0 的股票高亮
            if (position.GetTotalQuantity() > 0)
            {
                return new SolidColorBrush(Color.FromRgb(200, 230, 255)); // 更明显的浅蓝色
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
