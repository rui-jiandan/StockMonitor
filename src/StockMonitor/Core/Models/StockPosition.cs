namespace StockMonitor.Core.Models;

/// <summary>
/// 股票持仓持久化模型，对应 stocks.json
/// </summary>
public class StockPosition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AvgCostPrice { get; set; }
    public List<TradeRecord> TodayTrades { get; set; } = new();

    public int GetTodayNetQuantity()
    {
        return TodayTrades
            .Where(t => t.Time.Date == DateTime.Today)
            .Sum(t => t.Type == TradeRecord.TradeType.Buy ? t.Quantity : -t.Quantity);
    }

    public int GetTotalQuantity()
    {
        return Quantity + GetTodayNetQuantity();
    }
}
