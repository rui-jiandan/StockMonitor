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
    private readonly decimal _commissionMinAmount;
    private readonly decimal _etfCommissionRate;
    private readonly decimal _etfCommissionMinAmount;
    private readonly decimal _taxRate;
    private List<StockPosition> _positions;

    public PositionService(
        IRepository<List<StockPosition>> repository,
        decimal commissionRate = 0.000086m,
        decimal commissionMinAmount = 5m,
        decimal etfCommissionRate = 0.00005m,
        decimal etfCommissionMinAmount = 0.1m,
        decimal taxRate = 0m)
    {
        _repository = repository;
        _commissionRate = commissionRate;
        _commissionMinAmount = commissionMinAmount;
        _etfCommissionRate = etfCommissionRate;
        _etfCommissionMinAmount = etfCommissionMinAmount;
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
        decimal commission = CalculateCommission(code, turnover);

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
        decimal commission = CalculateCommission(code, turnover);
        decimal tax = IsFund(code) ? 0m : Math.Round(turnover * _taxRate, 2);

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

    public void UpdatePosition(string code, int quantity, decimal avgCostPrice, string? name = null)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        position.Quantity = quantity;
        position.AvgCostPrice = avgCostPrice;
        if (name != null)
            position.Name = name;

        SaveAndNotify(code);
    }

    public void AddStock(string code, string? name = null)
    {
        if (GetPosition(code) != null)
            throw new InvalidOperationException($"股票 {code} 已存在");

        _positions.Add(new StockPosition { Code = code, Name = name ?? string.Empty });
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
    /// 设置股票的特别关注状态并持久化
    /// </summary>
    /// <param name="code">股票代码</param>
    /// <param name="isWatched">是否特别关注</param>
    public void SetWatched(string code, bool isWatched)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        position.IsWatched = isWatched;
        SaveAndNotify(code);
    }

    /// <summary>
    /// 合并交易到持仓。默认只合并非今天的交易（启动时调用），<paramref name="mergeAll"/> 为 true 时合并全部（退出时调用）。
    /// </summary>
    /// <param name="mergeAll">是否合并所有交易，包括今天的。退出时传 true 做日终结算</param>
    public void MergeDayTrades(bool mergeAll = false)
    {
        var today = DateTime.Today;

        foreach (var position in _positions)
        {
            if (position.TodayTrades.Count == 0)
                continue;

            var tradesToMerge = mergeAll
                ? position.TodayTrades.ToList()
                : position.TodayTrades.Where(t => t.Time.Date < today).ToList();

            if (tradesToMerge.Count == 0)
                continue;

            int buyQty = tradesToMerge
                .Where(t => t.Type == TradeRecord.TradeType.Buy)
                .Sum(t => t.Quantity);
            int sellQty = tradesToMerge
                .Where(t => t.Type == TradeRecord.TradeType.Sell)
                .Sum(t => t.Quantity);
            int netQty = buyQty - sellQty;

            decimal buyTotalCost = tradesToMerge
                .Where(t => t.Type == TradeRecord.TradeType.Buy)
                .Sum(t => t.Quantity * t.Price + t.Commission + t.Tax);
            decimal sellCostDeduction = sellQty * position.AvgCostPrice;

            int newQty = position.Quantity + netQty;
            decimal newAvgCost = newQty > 0
                ? (position.Quantity * position.AvgCostPrice + buyTotalCost - sellCostDeduction) / newQty
                : 0m;

            position.Quantity = newQty;
            position.AvgCostPrice = newAvgCost;

            foreach (var trade in tradesToMerge)
                position.TodayTrades.Remove(trade);
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

    private static bool IsFund(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length < 3) return false;
        var numPart = code[2..];
        return !numPart.StartsWith('6') && !numPart.StartsWith('0') && !numPart.StartsWith('3');
    }

    private decimal CalculateCommission(string code, decimal turnover)
    {
        if (IsFund(code))
        {
            return Math.Max(turnover * _etfCommissionRate, _etfCommissionMinAmount);
        }
        return Math.Max(turnover * _commissionRate, _commissionMinAmount);
    }
}
