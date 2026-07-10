using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 预警服务实现，支持价格和涨跌幅预警，触发后可自动移除一次性规则
/// </summary>
public class AlertService : IAlertService
{
    private readonly IRepository<List<AlertRule>> _repository;
    private List<AlertRule> _rules;

    /// <summary>
    /// 集合竞价开始时间
    /// </summary>
    private static readonly TimeSpan MorningOpen = new(9, 25, 0);

    /// <summary>
    /// 上午收盘时间
    /// </summary>
    private static readonly TimeSpan MorningClose = new(11, 30, 0);

    /// <summary>
    /// 下午开盘时间
    /// </summary>
    private static readonly TimeSpan AfternoonOpen = new(13, 0, 0);

    /// <summary>
    /// 下午收盘时间
    /// </summary>
    private static readonly TimeSpan AfternoonClose = new(15, 0, 0);

    public AlertService(IRepository<List<AlertRule>> repository)
    {
        _repository = repository;
        _rules = repository.Load() ?? new List<AlertRule>();
    }

    /// <summary>
    /// 添加预警规则并持久化
    /// </summary>
    public void AddRule(AlertRule rule)
    {
        _rules.Add(rule);
        _repository.Save(_rules);
    }

    /// <summary>
    /// 按规则 ID 移除预警规则并持久化
    /// </summary>
    public void RemoveRule(string ruleId)
    {
        _rules.RemoveAll(r => r.Id == ruleId);
        _repository.Save(_rules);
    }

    /// <summary>
    /// 获取所有预警规则的只读列表
    /// </summary>
    public IReadOnlyList<AlertRule> GetRules() => _rules.AsReadOnly();

    /// <summary>
    /// 判断当前是否处于A股交易时段（周一至周五 9:25-11:30、13:00-15:00）
    /// </summary>
    private static bool IsInTradingHours()
    {
        var now = DateTime.Now;
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        var time = now.TimeOfDay;
        return (time >= MorningOpen && time <= MorningClose) ||
               (time >= AfternoonOpen && time <= AfternoonClose);
    }

    /// <summary>
    /// 检查行情数据是否触发预警规则，触发的规则将引发 AlertTriggered 事件。
    /// 仅在交易时段检查，停牌股票自动跳过。
    /// </summary>
    /// <param name="quotes">当前行情快照列表</param>
    public void CheckAlerts(IEnumerable<StockQuote> quotes)
    {
        if (!IsInTradingHours()) return;

        var quoteDict = quotes.ToDictionary(q => q.Code);
        var oneTimeTriggered = new List<AlertRule>();

        foreach (var rule in _rules.ToList())
        {
            if (rule.IsTriggered) continue;
            if (!quoteDict.TryGetValue(rule.StockCode, out var quote)) continue;
            if (quote.IsSuspended) continue;

            bool triggered = rule.Type switch
            {
                AlertType.PriceAbove => quote.CurrentPrice > rule.Threshold,
                AlertType.PriceBelow => quote.CurrentPrice < rule.Threshold,
                AlertType.ChangeRateAbove => quote.ChangeRate > rule.Threshold,
                AlertType.ChangeRateBelow => quote.ChangeRate < rule.Threshold,
                _ => false
            };

            if (!triggered) continue;

            rule.IsTriggered = true;
            AlertTriggered?.Invoke(this, new AlertTriggeredEventArgs { Rule = rule, Quote = quote });

            if (rule.IsOneTime)
                oneTimeTriggered.Add(rule);
        }

        if (oneTimeTriggered.Count > 0)
        {
            foreach (var rule in oneTimeTriggered)
                _rules.Remove(rule);
            _repository.Save(_rules);
        }
    }

    public event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;
}
