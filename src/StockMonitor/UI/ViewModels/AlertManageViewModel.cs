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
    /// 股票选项列表，用于添加预警规则时的选择
    /// </summary>
    public ObservableCollection<StockOptionItem> StockOptions { get; }

    /// <summary>
    /// 预警类型中文选项列表
    /// </summary>
    public ObservableCollection<AlertTypeItem> AlertTypeItems { get; }

    [ObservableProperty] private StockOptionItem? _selectedStockOption;
    [ObservableProperty] private AlertTypeItem? _selectedAlertTypeItem;
    [ObservableProperty] private string _thresholdText = string.Empty;
    [ObservableProperty] private bool _isOneTime = true;

    public AlertManageViewModel(IAlertService alertService, IPositionService positionService)
    {
        _alertService = alertService;
        _positionService = positionService;

        Rules = new ObservableCollection<AlertRule>(alertService.GetRules());

        // 构建股票选项，标记当前有持仓的股票（数量 > 0）
        var allPositions = positionService.GetAllPositions();
        StockOptions = new ObservableCollection<StockOptionItem>(
            allPositions.Select(p => new StockOptionItem(p.Code, p.Name, p.GetTotalQuantity() > 0)));

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
    /// 获取选中股票选项的代码
    /// </summary>
    private string? GetCodeFromOption(StockOptionItem? option)
    {
        return option?.Code;
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

/// <summary>
/// 股票选项，用于 ComboBox 绑定，包含持仓标记用于高亮显示
/// </summary>
public class StockOptionItem
{
    public string Code { get; }
    public string Name { get; }
    public string DisplayText => $"{Code} {Name}";
    /// <summary>
    /// 是否已有持仓（包含今日卖出后持仓为0的情况）
    /// </summary>
    public bool HasPosition { get; }

    public StockOptionItem(string code, string name, bool hasPosition)
    {
        Code = code;
        Name = name;
        HasPosition = hasPosition;
    }

    public override string ToString() => DisplayText;
}
