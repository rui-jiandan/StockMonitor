using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Logging;

namespace StockMonitor.Core.Services;

/// <summary>
/// 数据源调度服务，按优先级尝试多个数据源，支持 failover 和缓存
/// </summary>
public class StockDataService : IStockDataService
{
    private readonly List<IStockDataProvider> _providers;
    private readonly IPositionService _positionService;
    private List<StockQuote> _cachedQuotes = new();
    private CancellationToken _cancellationToken;

    public StockDataService(
        IEnumerable<IStockDataProvider> providers,
        IPositionService positionService)
    {
        _providers = providers.ToList();
        _positionService = positionService;
    }

    /// <summary>
    /// 设置取消令牌，用于取消正在进行的 HTTP 请求
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public void SetCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// 获取所有持仓股票的行情数据，依次尝试各数据源，全部失败则返回缓存
    /// </summary>
    public async Task<List<StockQuote>> GetQuotesAsync()
    {
        var codes = _positionService.GetAllPositions().Select(p => p.Code).ToList();
        if (codes.Count == 0)
        {
            FileLogger.LogInfo("无持仓股票，跳过行情获取");
            return new List<StockQuote>();
        }

        //FileLogger.LogInfo($"开始获取行情，股票数: {codes.Count}，数据源数: {_providers.Count}");

        foreach (var provider in _providers)
        {
            //FileLogger.LogInfo($"尝试数据源: {provider.Name}，可用: {provider.IsAvailable}");

            if (!provider.IsAvailable) continue;
            try
            {
                var quotes = await provider.GetQuotesAsync(codes, _cancellationToken);
                if (quotes.Count > 0)
                {
                    //FileLogger.LogInfo($"数据源 {provider.Name} 返回 {quotes.Count} 条行情");
                    _cachedQuotes = quotes;
                    QuotesUpdated?.Invoke(this, quotes);
                    return quotes;
                }

                FileLogger.LogInfo($"数据源 {provider.Name} 返回空结果");
            }
            catch (OperationCanceledException)
            {
                FileLogger.LogInfo("行情获取已取消");
                return _cachedQuotes;
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"数据源 {provider.Name} 请求失败", ex);
            }
        }

        FileLogger.LogInfo($"所有数据源失败，返回缓存 {_cachedQuotes.Count} 条");
        return _cachedQuotes;
    }

    public event EventHandler<List<StockQuote>>? QuotesUpdated;
}
