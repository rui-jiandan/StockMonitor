using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 预警管理 ViewModel，负责预警规则的添加、删除和展示
/// </summary>
public partial class AlertManageViewModel : ObservableObject
{
    private readonly IAlertService _alertService;
    private readonly IPositionService _positionService;

    public ObservableCollection<AlertRule> Rules { get; }

    /// <summary>
    /// 股票选项列表，格式为 "代码 名称"
    /// </summary>
    public ObservableCollection<string> StockOptions { get; }

    /// <summary>
    /// 预警类型中文选项列表
    /// </summary>
    public ObservableCollection<AlertTypeItem> AlertTypeItems { get; }

    [ObservableProperty] private string? _selectedStockOption;
    [ObservableProperty] private AlertTypeItem? _selectedAlertTypeItem;
    [ObservableProperty] private string _thresholdText = string.Empty;
    [ObservableProperty] private bool _isOneTime = true;

    public AlertManageViewModel(IAlertService alertService, IPositionService positionService)
    {
        _alertService = alertService;
        _positionService = positionService;

        Rules = new ObservableCollection<AlertRule>(alertService.GetRules());

        // 构建 "代码 名称" 格式的股票选项
        StockOptions = new ObservableCollection<string>(
            positionService.GetAllPositions().Select(p => $"{p.Code} {p.Name}"));

        // 构建中文预警类型选项
        AlertTypeItems = new ObservableCollection<AlertTypeItem>
        {
            new(AlertType.PriceAbove, "价格上穿"),
            new(AlertType.PriceBelow, "价格下穿"),
            new(AlertType.ChangeRateAbove, "涨幅超过"),
            new(AlertType.ChangeRateBelow, "跌幅超过")
        };

        SelectedAlertTypeItem = AlertTypeItems.FirstOrDefault();
    }

    /// <summary>
    /// 从选中的股票选项中提取股票代码
    /// </summary>
    private string? GetCodeFromOption(string? option)
    {
        if (string.IsNullOrEmpty(option)) return null;
        return option.Split(' ')[0];
    }

    [RelayCommand]
    private void AddRule()
    {
        var code = GetCodeFromOption(SelectedStockOption);
        if (string.IsNullOrEmpty(code)) return;
        if (SelectedAlertTypeItem == null) return;
        if (!decimal.TryParse(ThresholdText, out var threshold)) return;

        var rule = new AlertRule
        {
            StockCode = code,
            Type = SelectedAlertTypeItem.Type,
            Threshold = threshold,
            IsOneTime = IsOneTime
        };

        _alertService.AddRule(rule);
        RefreshRules();
    }

    [RelayCommand]
    private void DeleteRule(AlertRule rule)
    {
        _alertService.RemoveRule(rule.Id);
        RefreshRules();
    }

    private void RefreshRules()
    {
        Rules.Clear();
        foreach (var r in _alertService.GetRules())
            Rules.Add(r);
    }
}

/// <summary>
/// 预警类型选项，用于 ComboBox 绑定，提供中文显示名称
/// </summary>
public class AlertTypeItem
{
    public AlertType Type { get; }
    public string DisplayName { get; }

    public AlertTypeItem(AlertType type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}
