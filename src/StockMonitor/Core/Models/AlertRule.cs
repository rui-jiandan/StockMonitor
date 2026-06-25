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

    /// <summary>
    /// 预警类型中文显示名称映射
    /// </summary>
    private static readonly Dictionary<AlertType, string> TypeDisplayNames = new()
    {
        { AlertType.PriceAbove, "价格上穿" },
        { AlertType.PriceBelow, "价格下穿" },
        { AlertType.ChangeRateAbove, "涨幅超过" },
        { AlertType.ChangeRateBelow, "跌幅超过" }
    };

    /// <summary>
    /// 获取预警类型的中文显示名称
    /// </summary>
    public string TypeDisplayName => TypeDisplayNames.GetValueOrDefault(Type, Type.ToString());
}
