using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 股票数据源提供者接口
/// </summary>
public interface IStockDataProvider
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 批量获取股票行情
    /// </summary>
    /// <param name="codes">股票全量编码列表，如 "sh600036"</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 数据源是否可用
    /// </summary>
    bool IsAvailable { get; }
}
