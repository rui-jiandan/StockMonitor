using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 预警管理 ViewModel，负责预警规则的添加、删除和展示。
/// 列表项使用 <see cref="AlertRuleDisplayItem"/> 包装，额外携带股票名称，
/// 名称通过 <see cref="IStockDataService"/> 异步拉取行情后填充。
/// </summary>
public partial class AlertManageViewModel : ObservableObject
{
    private readonly IAlertService _alertService;
    private readonly IPositionService _positionService;
    private readonly IStockDataService _stockDataService;

    /// <summary>
    /// 股票代码到名称的映射，由行情数据异步填充
    /// </summary>
    private Dictionary<string, string> _codeToNameMap = new();

    public ObservableCollection<AlertRuleDisplayItem> Rules { get; }

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

    public AlertManageViewModel(
        IAlertService alertService,
        IPositionService positionService,
        IStockDataService stockDataService)
    {
        _alertService = alertService;
        _positionService = positionService;
        _stockDataService = stockDataService;

        Rules = new ObservableCollection<AlertRuleDisplayItem>(
            alertService.GetRules().Select(r => new AlertRuleDisplayItem(r)));

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

        // 异步加载股票名称：行情数据可能来自缓存（瞬时返回），也可能发起新请求。
        // 加载完成后在 UI 线程刷新列表，使已设置规则展示股票名称。
        _ = LoadStockNamesAsync();
    }

    /// <summary>
    /// 异步拉取行情数据构建 Code→Name 映射，完成后在 UI 线程刷新规则列表
    /// </summary>
    private async Task LoadStockNamesAsync()
    {
        try
        {
            var quotes = await _stockDataService.GetQuotesAsync();
            var map = new Dictionary<string, string>();
            foreach (var q in quotes)
            {
                if (!string.IsNullOrEmpty(q.Name))
                    map[q.Code] = q.Name;
            }
            _codeToNameMap = map;

            Application.Current?.Dispatcher.Invoke(RefreshRules);
        }
        catch
        {
            // 加载失败时保持现状（无名称），不影响规则展示与删除
        }
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

    /// <summary>
    /// 删除指定预警规则
    /// </summary>
    /// <param name="item">列表中绑定的展示项，从中取规则 ID 进行删除</param>
    [RelayCommand]
    private void DeleteRule(AlertRuleDisplayItem item)
    {
        if (item == null) return;
        _alertService.RemoveRule(item.Id);
        RefreshRules();
    }

    /// <summary>
    /// 重新加载规则列表，并填充股票名称
    /// </summary>
    private void RefreshRules()
    {
        Rules.Clear();
        foreach (var r in _alertService.GetRules())
        {
            var display = new AlertRuleDisplayItem(r);
            if (_codeToNameMap.TryGetValue(r.StockCode, out var name))
                display.StockName = name;
            Rules.Add(display);
        }
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

/// <summary>
/// 预警规则列表展示项，包装 <see cref="AlertRule"/> 并附带股票名称
/// </summary>
public class AlertRuleDisplayItem
{
    public string Id { get; }
    public string StockCode { get; }
    /// <summary>
    /// 股票名称，由行情数据异步填充；未知时为空字符串
    /// </summary>
    public string StockName { get; set; } = string.Empty;
    public string TypeDisplayName { get; }
    public decimal Threshold { get; }
    public bool IsOneTime { get; }

    public AlertRuleDisplayItem(AlertRule rule)
    {
        Id = rule.Id;
        StockCode = rule.StockCode;
        TypeDisplayName = rule.TypeDisplayName;
        Threshold = rule.Threshold;
        IsOneTime = rule.IsOneTime;
    }
}
