using CommunityToolkit.Mvvm.ComponentModel;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 股票显示项，视图专用模型
/// </summary>
public partial class StockDisplayItem : ObservableObject
{
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _priceText = string.Empty;
    [ObservableProperty] private string _changeText = string.Empty;
    [ObservableProperty] private string _changeRateText = string.Empty;
    [ObservableProperty] private string _pnlText = string.Empty;
    [ObservableProperty] private string _priceColor = "White";
    [ObservableProperty] private bool _hasPosition;

    /// <summary>
    /// 股票名称（原始名称，供排序使用），与显示格式化后的 DisplayName 区分
    /// </summary>
    [ObservableProperty] private string _name = string.Empty;

    // 以下数值字段专供排序使用，不参与显示格式化
    [ObservableProperty] private decimal _currentPrice;
    [ObservableProperty] private decimal _change;
    [ObservableProperty] private decimal _changeRate;
    [ObservableProperty] private decimal _pnl;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _avgCost;
}
