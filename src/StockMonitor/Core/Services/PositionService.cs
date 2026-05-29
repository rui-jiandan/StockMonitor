using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 持仓服务实现，支持 T+0 日内交易：白天买卖仅记录 TradeRecord，日终 MergeDayTrades 统一合并
/// </summary>
public class PositionService : IPositionService
{
    private readonly IRepository<List<StockPosition>> _repository;
    private readonly decimal _commissionRate;
    private readonly decimal _taxRate;
    private List<StockPosition> _positions;

    public PositionService(
        IRepository<List<StockPosition>> repository,
        decimal commissionRate = 0.00023m,
        decimal taxRate = 0m)
    {
        _repository = repository;
        _commissionRate = commissionRate;
        _taxRate = taxRate;
        _positions = repository.Load() ?? new List<StockPosition>();
    }

    public IReadOnlyList<StockPosition> GetAllPositions() => _positions.AsReadOnly();

    public StockPosition? GetPosition(string code) =>
        _positions.FirstOrDefault(p => p.Code == code);

    public void AddPosition(string code, decimal price, int quantity)
    {
        var position = GetOrCreatePosition(code);
        decimal turnover = price * quantity;
        decimal commission = Math.Max(turnover * _commissionRate, 5m);

        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = quantity,
            Price = price,
            Time = DateTime.Now,
            Commission = commission,
            Tax = 0
        });

        SaveAndNotify(code);
    }

    public void ReducePosition(string code, decimal price, int quantity)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        int totalQuantity = position.GetTotalQuantity();
        if (quantity > totalQuantity)
            throw new InsufficientPositionException(
                $"减仓数量 {quantity} 超过总持仓 {totalQuantity}");

        decimal turnover = price * quantity;
        decimal commission = Math.Max(turnover * _commissionRate, 5m);
        decimal tax = Math.Round(turnover * _taxRate, 2);

        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = quantity,
            Price = price,
            Time = DateTime.Now,
            Commission = commission,
            Tax = tax
        });

        SaveAndNotify(code);
    }

    public void UpdatePosition(string code, int quantity, decimal avgCostPrice)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        position.Quantity = quantity;
        position.AvgCostPrice = avgCostPrice;

        SaveAndNotify(code);
    }

    public void AddStock(string code)
    {
        if (GetPosition(code) != null)
            throw new InvalidOperationException($"股票 {code} 已存在");

        _positions.Add(new StockPosition { Code = code });
        SaveAndNotify(code);
    }

    public void RemoveStock(string code)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        _positions.Remove(position);
        SaveAndNotify(code);
    }

    /// <summary>
    /// 合并当日交易到持仓：将 TodayTrades 中的买卖记录汇总更新 Quantity 和 AvgCostPrice，然后清除当日交易。
    /// T+0 修复核心——白天交易不直接改持仓数量/成本，日终统一合并。
    /// </summary>
    public void MergeDayTrades()
    {
        foreach (var position in _positions)
        {
            if (position.TodayTrades.Count == 0)
                continue;

            var todayTrades = position.TodayTrades
                .Where(t => t.Time.Date == DateTime.Today)
                .ToList();

            if (todayTrades.Count == 0)
                continue;

            int buyQty = todayTrades
                .Where(t => t.Type == TradeRecord.TradeType.Buy)
                .Sum(t => t.Quantity);
            int sellQty = todayTrades
                .Where(t => t.Type == TradeRecord.TradeType.Sell)
                .Sum(t => t.Quantity);
            int netQty = buyQty - sellQty;

            decimal buyTotalCost = todayTrades
                .Where(t => t.Type == TradeRecord.TradeType.Buy)
                .Sum(t => t.Quantity * t.Price + t.Commission + t.Tax);
            decimal sellCostDeduction = sellQty * position.AvgCostPrice;

            int newQty = position.Quantity + netQty;
            decimal newAvgCost = newQty > 0
                ? (position.Quantity * position.AvgCostPrice + buyTotalCost - sellCostDeduction) / newQty
                : 0m;

            position.Quantity = newQty;
            position.AvgCostPrice = newAvgCost;

            position.TodayTrades = position.TodayTrades
                .Where(t => t.Time.Date != DateTime.Today)
                .ToList();
        }

        _repository.Save(_positions);
    }

    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    private StockPosition GetOrCreatePosition(string code)
    {
        var position = GetPosition(code);
        if (position == null)
        {
            position = new StockPosition { Code = code };
            _positions.Add(position);
        }
        return position;
    }

    private void SaveAndNotify(string code)
    {
        _repository.Save(_positions);
        PositionChanged?.Invoke(this, new PositionChangedEventArgs { Code = code });
    }
}
