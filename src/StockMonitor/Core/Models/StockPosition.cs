namespace StockMonitor.Core.Models;

/// <summary>
/// 股票持仓持久化模型，对应 stocks.json
/// </summary>
public class StockPosition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否特别关注（主界面显示 ★ 标记，可用于排序置顶）
    /// </summary>
    public bool IsWatched { get; set; }

    public int Quantity { get; set; }
    public decimal AvgCostPrice { get; set; }
    public List<TradeRecord> TodayTrades { get; set; } = new();

    public int GetTodayNetQuantity()
    {
        return TodayTrades
            .Sum(t => t.Type == TradeRecord.TradeType.Buy ? t.Quantity : -t.Quantity);
    }

    public int GetTotalQuantity()
    {
        return Quantity + GetTodayNetQuantity();
    }
}
