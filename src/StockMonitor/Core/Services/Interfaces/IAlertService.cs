using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 预警触发事件参数
/// </summary>
public class AlertTriggeredEventArgs : EventArgs
{
    public AlertRule Rule { get; init; } = null!;
    public StockQuote Quote { get; init; } = null!;
}

/// <summary>
/// 预警服务接口
/// </summary>
public interface IAlertService
{
    void AddRule(AlertRule rule);
    void RemoveRule(string ruleId);
    IReadOnlyList<AlertRule> GetRules();
    void CheckAlerts(IEnumerable<StockQuote> quotes);
    event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;
}
