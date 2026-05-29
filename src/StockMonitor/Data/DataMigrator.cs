using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using StockMonitor.Core.Models;

namespace StockMonitor.Data;

/// <summary>
/// 旧格式股票数据迁移工具，负责将旧版 Position/Cost/OpList 格式转换为新版 Quantity/AvgCostPrice/TodayTrades 格式
/// </summary>
public static class DataMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 检测并迁移旧格式股票数据，若已是新格式则直接反序列化返回
    /// </summary>
    /// <param name="filePath">股票数据 JSON 文件路径</param>
    /// <returns>迁移后的股票持仓列表</returns>
    public static List<StockPosition> MigrateStocksIfNeeded(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<StockPosition>();

        var json = File.ReadAllText(filePath);

        if (!IsOldFormat(json))
        {
            return JsonSerializer.Deserialize<List<StockPosition>>(json, JsonOptions)
                   ?? new List<StockPosition>();
        }

        var result = ConvertOldFormat(json);

        File.WriteAllText(filePath, JsonSerializer.Serialize(result, JsonOptions));

        return result;
    }

    /// <summary>
    /// 规范化股票代码：纯数字代码添加市场前缀（sh/sz），已有前缀则不变
    /// </summary>
    /// <param name="code">原始股票代码</param>
    /// <returns>带市场前缀的股票代码</returns>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var lower = code.ToLowerInvariant();
        if (lower.StartsWith("sh") || lower.StartsWith("sz"))
            return lower;

        if (code.Length == 0)
            return code;

        var firstChar = code[0];
        if (firstChar == '6' || firstChar == '9' || firstChar == '5')
            return "sh" + code;

        if (firstChar == '0' || firstChar == '3' || firstChar == '1')
            return "sz" + code;

        return code;
    }

    /// <summary>
    /// 检测 JSON 是否为旧格式（包含 Position 字段但不包含 Quantity 字段）
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    private static bool IsOldFormat(string json)
    {
        return json.Contains("\"Position\"") && !json.Contains("\"Quantity\"");
    }

    /// <summary>
    /// 将旧格式 JSON 转换为新格式 StockPosition 列表
    /// </summary>
    /// <param name="json">旧格式 JSON 字符串</param>
    private static List<StockPosition> ConvertOldFormat(string json)
    {
        var arr = JsonNode.Parse(json)?.AsArray();
        if (arr == null)
            return new List<StockPosition>();

        var result = new List<StockPosition>();

        foreach (var item in arr)
        {
            if (item == null) continue;

            var position = new StockPosition
            {
                Code = NormalizeCode(item["Code"]?.GetValue<string>() ?? string.Empty),
                Name = item["Name"]?.GetValue<string>() ?? string.Empty,
                Quantity = item["Position"]?.GetValue<int>() ?? 0,
                AvgCostPrice = item["Cost"]?.GetValue<decimal>() ?? 0m
            };

            var opList = item["OpList"]?.AsArray();
            if (opList != null)
            {
                foreach (var op in opList)
                {
                    if (op == null) continue;

                    var tradeType = op["Type"]?.GetValue<int>() ?? 0;
                    var opPosition = op["Position"]?.GetValue<int>() ?? 0;

                    position.TodayTrades.Add(new TradeRecord
                    {
                        Type = tradeType == 0 ? TradeRecord.TradeType.Buy : TradeRecord.TradeType.Sell,
                        Quantity = Math.Abs(opPosition),
                        Price = op["Price"]?.GetValue<decimal>() ?? 0m,
                        Time = op["Time"]?.GetValue<DateTime>() ?? DateTime.MinValue,
                        Commission = op["Commission"]?.GetValue<decimal>() ?? 0m,
                        Tax = op["Tax"]?.GetValue<decimal>() ?? 0m
                    });
                }
            }

            result.Add(position);
        }

        return result;
    }
}
