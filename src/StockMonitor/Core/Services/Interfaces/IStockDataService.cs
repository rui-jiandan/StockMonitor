using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 数据源调度服务接口，负责 failover 和缓存
/// </summary>
public interface IStockDataService
{
    /// <summary>
    /// 获取所有持仓股票的行情数据
    /// </summary>
    Task<List<StockQuote>> GetQuotesAsync();

    /// <summary>
    /// 行情数据更新事件
    /// </summary>
    event EventHandler<List<StockQuote>>? QuotesUpdated;
}
