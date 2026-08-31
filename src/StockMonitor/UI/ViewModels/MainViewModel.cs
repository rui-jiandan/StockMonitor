using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Calculator;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Logging;
using StockMonitor.UI.Views;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 主窗口 ViewModel，负责行情刷新循环、持仓合并展示和预警通知
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IStockDataService _stockDataService;
    private readonly IPositionService _positionService;
    private readonly IAlertService _alertService;
    private readonly IRepository<AppConfig> _configRepo;
    private readonly AppConfig _config;
    private CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>
    /// 收盘时间，超过此时间停止自动刷新
    /// </summary>
    private static readonly TimeSpan MarketCloseTime = new(15, 20, 0);

    public ObservableCollection<StockDisplayItem> Stocks { get; } = new();

    [ObservableProperty] private string _todaySummary = string.Empty;
    [ObservableProperty] private string _summaryColor = "White";
    [ObservableProperty] private bool _isRefreshing;

    /// <summary>
    /// 切换窗口显示/隐藏状态的事件，由托盘图标双击触发
    /// </summary>
    public event Action? ToggleVisibilityRequested;

    /// <summary>
    /// 股票列表数据更新完成事件，通知 View 进行 UI 测量
    /// </summary>
    public event Action? StocksUpdated;

    /// <summary>
    /// 最大可见股票数量，从配置读取
    /// </summary>
    public int MaxVisibleStocks => _config.MaxVisibleStocks;

    /// <summary>
    /// 切换窗口显示/隐藏命令（绑定到托盘图标双击）
    /// </summary>
    [RelayCommand]
    private void ToggleVisibility() => ToggleVisibilityRequested?.Invoke();

    public MainViewModel(
        IStockDataService stockDataService,
        IPositionService positionService,
        IAlertService alertService,
        IRepository<AppConfig> configRepo)
    {
        _stockDataService = stockDataService;
        _positionService = positionService;
        _alertService = alertService;
        _configRepo = configRepo;
        _config = configRepo.Load();

        _alertService.AlertTriggered += OnAlertTriggered;

        if (_stockDataService is StockDataService sds)
        {
            sds.SetCancellationToken(_cts.Token);
        }
    }

    /// <summary>
    /// 手动刷新行情命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshData();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// 启动后台刷新循环，按配置的间隔定时拉取行情
    /// </summary>
    public void StartRefreshLoop()
    {
        _ = RefreshLoopAsync();
    }

    /// <summary>
    /// 后台循环：按 RefreshTime 间隔反复调用 RefreshData，收盘后自动停止
    /// </summary>
    private async Task RefreshLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (DateTime.Now.TimeOfDay > MarketCloseTime)
            {
                try
                {
                    await RefreshData();
                    FileLogger.LogInfo("已过收盘时间(15:20)，刷新一次后停止自动刷新");
                }
                catch (Exception ex)
                {
                    FileLogger.LogError("收盘后刷新行情失败", ex);
                }
                break;
            }

            try
            {
                await RefreshData();
            }
            catch (Exception ex)
            {
                FileLogger.LogError("刷新行情失败", ex);
            }

            try
            {
                await Task.Delay(_config.RefreshTime, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 刷新行情数据：获取报价 → 合并持仓 → 更新显示项 → 检查预警 → 更新汇总
    /// </summary>
    private async Task RefreshData()
    {
        var quotes = await _stockDataService.GetQuotesAsync();
        var positions = _positionService.GetAllPositions();
        var posDict = positions.ToDictionary(p => p.Code);

        var items = new List<StockDisplayItem>();

        foreach (var quote in quotes)
        {
            posDict.TryGetValue(quote.Code, out var position);
            items.Add(CreateDisplayItem(quote, position));
        }

        foreach (var pos in positions.Where(p => !quotes.Any(q => q.Code == p.Code)))
        {
            items.Add(CreateDisplayItem(null, pos));
        }

        var ordered = OrderDisplayItems(items);

        Application.Current.Dispatcher.Invoke(() =>
        {
            Stocks.Clear();
            foreach (var item in ordered)
                Stocks.Add(item);
        });

        StocksUpdated?.Invoke();
        _alertService.CheckAlerts(quotes);
        UpdateTodaySummary(positions, quotes);
    }

    /// <summary>
    /// 根据行情和持仓创建显示项，格式化价格、涨跌、盈亏等文本
    /// </summary>
    /// <param name="quote">实时行情，可能为 null（无行情数据）</param>
    /// <param name="position">持仓信息，可能为 null（无持仓）</param>
    private StockDisplayItem CreateDisplayItem(StockQuote? quote, StockPosition? position)
    {
        var code = position?.Code ?? quote?.Code ?? string.Empty;
        var name = !string.IsNullOrEmpty(position?.Name) ? position.Name : (quote?.Name ?? string.Empty);
        var quantity = position?.GetTotalQuantity() ?? 0;
        var avgCost = position?.AvgCostPrice ?? 0m;
        bool hasTodayTrades = position?.TodayTrades.Any(t => t.Time.Date == DateTime.Today) == true;
        var hasPosition = quantity > 0 || hasTodayTrades;

        string price = "--";
        string change = "--";
        string rate = "--";
        string pnl = string.Empty;
        decimal totalPnL = 0m;
        string color = "White";

        if (quote != null)
        {
            price = quote.CurrentPrice.ToString("G");
            change = quote.Change >= 0 ? $"+{quote.Change:G}" : $"{quote.Change:G}";
            rate = quote.ChangeRate >= 0 ? $"+{quote.ChangeRate:G}%" : $"{quote.ChangeRate:G}%";
            color = quote.Change > 0 ? "Red" : quote.Change < 0 ? "#00FF00" : "White";

            if (hasPosition)
            {
                (_, _, totalPnL) = PnLCalculator.CalculateTodayPnL(position!, quote);
                pnl = totalPnL >= 0 ? $"+{totalPnL:F0}" : $"{totalPnL:F0}";
            }
        }

        var displayName = FormatDisplayName(code, name, price, change, rate, pnl, quantity.ToString(), avgCost.ToString("G"));

        return new StockDisplayItem
        {
            Code = code,
            Name = name,
            DisplayName = displayName,
            PriceText = price,
            ChangeText = change,
            ChangeRateText = rate,
            PnlText = pnl,
            PriceColor = color,
            HasPosition = hasPosition,
            CurrentPrice = quote?.CurrentPrice ?? 0m,
            Change = quote?.Change ?? 0m,
            ChangeRate = quote?.ChangeRate ?? 0m,
            Pnl = totalPnL,
            Quantity = quantity,
            AvgCost = avgCost
        };
    }

    /// <summary>
    /// 根据配置的 ShowFormat 格式化显示名称
    /// </summary>
    private string FormatDisplayName(string code, string name, string price, string change, string rate, string pnl, string quantity, string cost)
    {
        var format = _config.ShowFormat;
        return format
            .Replace("#name", name)
            .Replace("#code", code)
            .Replace("#price", price)
            .Replace("#change", change)
            .Replace("#rate", rate)
            .Replace("#makemoney", pnl)
            .Replace("#quantity", quantity)
            .Replace("#cost", cost);
    }

    /// <summary>
    /// 按配置的排序规则对显示项排序，支持多字段，规则形如 "havepos desc,makemoney desc"
    /// </summary>
    private IEnumerable<StockDisplayItem> OrderDisplayItems(List<StockDisplayItem> items)
    {
        var orderBy = _config.OrderBy;
        if (string.IsNullOrWhiteSpace(orderBy))
            return items.OrderBy(i => i.Code);

        var rules = orderBy
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var field = tokens.Length > 0 ? tokens[0].ToLowerInvariant() : string.Empty;
                var desc = tokens.Length > 1 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
                return (field, desc);
            })
            .Where(r => !string.IsNullOrEmpty(r.field))
            .ToList();

        if (rules.Count == 0)
            return items.OrderBy(i => i.Code);

        IOrderedEnumerable<StockDisplayItem>? ordered = null;
        foreach (var (field, desc) in rules)
        {
            var selector = GetSortKeySelector(field);
            ordered = ordered == null
                ? (desc ? items.OrderByDescending(selector) : items.OrderBy(selector))
                : (desc ? ordered.ThenByDescending(selector) : ordered.ThenBy(selector));
        }
        return ordered!;
    }

    /// <summary>
    /// 将排序字段名映射为对应的可比较键选择器，未知字段回退到股票代码
    /// </summary>
    private static Func<StockDisplayItem, IComparable> GetSortKeySelector(string field) => field switch
    {
        "havepos" => i => i.HasPosition,
        "makemoney" => i => i.Pnl,
        "code" => i => i.Code,
        "name" => i => i.Name,
        "price" => i => i.CurrentPrice,
        "change" => i => i.Change,
        "rate" => i => i.ChangeRate,
        "quantity" => i => i.Quantity,
        "cost" => i => i.AvgCost,
        _ => i => i.Code
    };

    /// <summary>
    /// 更新今日盈亏汇总行，计算总盈亏金额、比例、市值、持仓只数、已实现盈亏等
    /// 支持的通配符：#money 今日总盈亏、#rate 盈亏比例、#value 总市值、
    /// #count 持仓只数、#realized 已实现盈亏、#time 当前时间
    /// </summary>
    private void UpdateTodaySummary(IReadOnlyList<StockPosition> positions, List<StockQuote> quotes)
    {
        var quoteDict = quotes.ToDictionary(q => q.Code);
        decimal totalPnL = 0m;
        decimal totalRealized = 0m;
        decimal totalMarketValue = 0m;
        int holdCount = 0;

        foreach (var pos in positions)
        {
            if (!quoteDict.TryGetValue(pos.Code, out var quote)) continue;

            var (realized, _, pnl) = PnLCalculator.CalculateTodayPnL(pos, quote);
            totalPnL += pnl;
            totalRealized += realized;

            int qty = pos.GetTotalQuantity();
            totalMarketValue += quote.CurrentPrice * qty;
            if (qty > 0) holdCount++;
        }

        var rate = totalMarketValue > 0 ? totalPnL / totalMarketValue * 100 : 0m;

        var format = _config.ShowTodaySumFormat;
        var summary = format
            .Replace("#money", $"{totalPnL:+#,#0;-#,#0;0}")
            .Replace("#rate", $"{rate:+0.00;-0.00;0.00}")
            .Replace("#value", $"{totalMarketValue:#,#0}")
            .Replace("#count", $"{holdCount}")
            .Replace("#realized", $"{totalRealized:+#,#0;-#,#0;0}")
            .Replace("#time", DateTime.Now.ToString("HH:mm:ss"));

        TodaySummary = summary;
        SummaryColor = totalPnL > 0 ? "Red" : totalPnL < 0 ? "#00FF00" : "White";
    }

    /// <summary>
    /// 预警触发回调，在屏幕右下角弹出美化通知窗口
    /// </summary>
    private void OnAlertTriggered(object? sender, AlertTriggeredEventArgs e)
    {
        var stockInfo = $"{e.Quote.Name}({e.Rule.StockCode})";
        var rateText = e.Quote.ChangeRate >= 0 ? $"+{e.Quote.ChangeRate}%" : $"{e.Quote.ChangeRate}%";
        var alertDetail = $"{e.Rule.TypeDisplayName} 阈值 {e.Rule.Threshold}\n最新价 {e.Quote.CurrentPrice}  涨幅 {rateText}";
        Application.Current.Dispatcher.Invoke(() =>
        {
            AlertNotificationWindow.ShowNotification(stockInfo, alertDetail);
        });
    }

    /// <summary>
    /// 释放资源，取消刷新循环并等待其退出
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _alertService.AlertTriggered -= OnAlertTriggered;
        _cts.Cancel();
        _cts.Dispose();
    }
}
