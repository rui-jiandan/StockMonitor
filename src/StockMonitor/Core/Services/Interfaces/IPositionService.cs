using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 持仓变更事件参数
/// </summary>
public class PositionChangedEventArgs : EventArgs
{
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// 持仓不足异常
/// </summary>
public class InsufficientPositionException : Exception
{
    public InsufficientPositionException(string message) : base(message) { }
}

/// <summary>
/// 持仓服务接口
/// </summary>
public interface IPositionService
{
    IReadOnlyList<StockPosition> GetAllPositions();
    StockPosition? GetPosition(string code);
    void AddPosition(string code, decimal price, int quantity);
    void ReducePosition(string code, decimal price, int quantity);
    void UpdatePosition(string code, int quantity, decimal avgCostPrice, string? name = null);
    void AddStock(string code, string? name = null);
    void RemoveStock(string code);

    /// <summary>
    /// 设置股票的特别关注状态并持久化
    /// </summary>
    /// <param name="code">股票代码</param>
    /// <param name="isWatched">是否特别关注</param>
    void SetWatched(string code, bool isWatched);
    void MergeDayTrades(bool mergeAll = false);
    event EventHandler<PositionChangedEventArgs>? PositionChanged;
}
