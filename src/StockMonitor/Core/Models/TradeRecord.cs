namespace StockMonitor.Core.Models;

/// <summary>
/// 交易记录
/// </summary>
public class TradeRecord
{
    public enum TradeType { Buy, Sell }

    public TradeType Type { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public DateTime Time { get; init; }
    public decimal Commission { get; init; }
    public decimal Tax { get; init; }
}
