using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 腾讯财经数据源，通过腾讯行情接口获取股票实时报价
/// </summary>
public class TencentDataProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;

    public string Name => "Tencent";
    public bool IsAvailable { get; private set; } = true;

    public TencentDataProvider()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 批量获取股票行情数据
    /// </summary>
    /// <param name="codes">股票全量编码列表，如 "sh600036"</param>
    public async Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var codeList = codes.ToList();
        if (codeList.Count == 0)
            return new List<StockQuote>();

        try
        {
            var url = $"https://qt.gtimg.cn/q={string.Join(",", codeList)}";
            var response = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            var text = Encoding.GetEncoding("GBK").GetString(response);

            var quotes = new List<StockQuote>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"v_(\w+)=""(.*)""");
                if (!match.Success) continue;

                var code = match.Groups[1].Value;
                var data = match.Groups[2].Value;
                var parts = data.Split('~');

                if (parts.Length < 35) continue;

                var name = parts[1];
                var codeFromData = parts[2];
                if (!decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var currentPrice)) continue;
                if (!decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var yestClose)) continue;
                if (!decimal.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var openPrice)) continue;
                if (!decimal.TryParse(parts[33], NumberStyles.Any, CultureInfo.InvariantCulture, out var highPrice)) continue;
                if (!decimal.TryParse(parts[34], NumberStyles.Any, CultureInfo.InvariantCulture, out var lowPrice)) continue;

                // 停牌判断：有昨收价但现价和开盘价均为0，视为停牌
                bool isSuspended = yestClose > 0 && currentPrice == 0 && openPrice == 0;

                quotes.Add(new StockQuote
                {
                    Code = code,
                    Name = name,
                    OpenPrice = openPrice,
                    YestClose = yestClose,
                    CurrentPrice = currentPrice,
                    HighPrice = highPrice,
                    LowPrice = lowPrice,
                    IsSuspended = isSuspended
                });
            }

            IsAvailable = true;
            return quotes;
        }
        catch
        {
            IsAvailable = false;
            return new List<StockQuote>();
        }
    }
}
