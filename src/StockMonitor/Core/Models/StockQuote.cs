namespace StockMonitor.Core.Models;

/// <summary>
/// 实时行情快照，每次数据刷新生成
/// </summary>
public class StockQuote
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public decimal OpenPrice { get; init; }
    public decimal YestClose { get; init; }
    public decimal HighPrice { get; init; }
    public decimal LowPrice { get; init; }
    public decimal Change => YestClose != 0 ? CurrentPrice - YestClose : 0;
    public decimal ChangeRate => YestClose != 0 ? Math.Round(Change / YestClose * 100, 2) : 0;
}
