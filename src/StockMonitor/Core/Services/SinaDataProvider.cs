using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 新浪财经数据源，通过新浪行情接口获取股票实时报价
/// </summary>
public class SinaDataProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;

    public string Name => "Sina";
    public bool IsAvailable { get; private set; } = true;

    public SinaDataProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://finance.sina.com.cn/");
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
            var url = $"https://hq.sinajs.cn/list={string.Join(",", codeList)}";
            var response = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            var text = Encoding.GetEncoding("GB18030").GetString(response);

            var quotes = new List<StockQuote>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"var hq_str_(\w+)=""(.*)""");
                if (!match.Success) continue;

                var code = match.Groups[1].Value;
                var data = match.Groups[2].Value;
                var parts = data.Split(',');

                if (parts.Length < 6) continue;

                var name = parts[0];
                if (!decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var openPrice)) continue;
                if (!decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var yestClose)) continue;
                if (!decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var currentPrice)) continue;
                if (!decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var highPrice)) continue;
                if (!decimal.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var lowPrice)) continue;

                quotes.Add(new StockQuote
                {
                    Code = code,
                    Name = name,
                    OpenPrice = openPrice,
                    YestClose = yestClose,
                    CurrentPrice = currentPrice,
                    HighPrice = highPrice,
                    LowPrice = lowPrice
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
