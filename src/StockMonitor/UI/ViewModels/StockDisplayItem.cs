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
}
