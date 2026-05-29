namespace StockMonitor.Core.Models;

/// <summary>
/// 预警类型枚举
/// </summary>
public enum AlertType
{
    PriceAbove,
    PriceBelow,
    ChangeRateAbove,
    ChangeRateBelow
}

/// <summary>
/// 预警规则
/// </summary>
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StockCode { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public decimal Threshold { get; set; }
    public bool IsTriggered { get; set; }
    public bool IsOneTime { get; set; } = true;
}
