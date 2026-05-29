using FluentAssertions;
using StockMonitor.Data;
using System.IO;
using System.Text.Json;
using Xunit;

namespace StockMonitor.Tests.Data;

public class DataMigratorTests : IDisposable
{
    private readonly string _tempDir;

    public DataMigratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"DataMigratorTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string GetFilePath() => Path.Combine(_tempDir, "stocks.json");

    [Fact]
    public void MigrateStocks_OldFormat_ConvertsToNewFormat()
    {
        var filePath = GetFilePath();
        var oldJson = """
            [
              {
                "Code": "600036",
                "Name": "招商银行",
                "Position": 1000,
                "Cost": 35.5
              }
            ]
            """;
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("sh600036");
        result[0].Name.Should().Be("招商银行");
        result[0].Quantity.Should().Be(1000);
        result[0].AvgCostPrice.Should().Be(35.5m);
    }

    [Fact]
    public void MigrateStocks_ShenzhenCode_GetsSzPrefix()
    {
        var filePath = GetFilePath();
        var oldJson = """
            [
              {
                "Code": "000001",
                "Name": "平安银行",
                "Position": 500,
                "Cost": 12.3
              }
            ]
            """;
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sz000001");
    }

    [Fact]
    public void MigrateStocks_EtfShanghai_GetsShPrefix()
    {
        var filePath = GetFilePath();
        var oldJson = """
            [
              {
                "Code": "510050",
                "Name": "50ETF",
                "Position": 2000,
                "Cost": 2.8
              }
            ]
            """;
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sh510050");
    }

    [Fact]
    public void MigrateStocks_EtfShenzhen_GetsSzPrefix()
    {
        var filePath = GetFilePath();
        var oldJson = """
            [
              {
                "Code": "159941",
                "Name": "纳指ETF",
                "Position": 3000,
                "Cost": 1.5
              }
            ]
            """;
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sz159941");
    }

    [Fact]
    public void MigrateStocks_AlreadyNewFormat_NoChange()
    {
        var filePath = GetFilePath();
        var newJson = """
            [
              {
                "code": "sh600036",
                "name": "招商银行",
                "quantity": 1000,
                "avgCostPrice": 35.5,
                "todayTrades": []
              }
            ]
            """;
        File.WriteAllText(filePath, newJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("sh600036");
        result[0].Quantity.Should().Be(1000);
        result[0].AvgCostPrice.Should().Be(35.5m);

        var savedJson = File.ReadAllText(filePath);
        savedJson.Should().Contain("\"quantity\"");
        savedJson.Should().NotContain("\"Position\"");
    }

    [Fact]
    public void MigrateStocks_WithOpList_ConvertsToTodayTrades()
    {
        var filePath = GetFilePath();
        var oldJson = """
            [
              {
                "Code": "600036",
                "Name": "招商银行",
                "Position": 1000,
                "Cost": 35.5,
                "OpList": [
                  {
                    "Type": 0,
                    "Position": 500,
                    "Price": 35.0,
                    "Time": "2025-01-15T10:30:00",
                    "Commission": 5.0,
                    "Tax": 0.0
                  },
                  {
                    "Type": 1,
                    "Position": -200,
                    "Price": 36.0,
                    "Time": "2025-01-15T14:00:00",
                    "Commission": 3.0,
                    "Tax": 1.0
                  }
                ]
              }
            ]
            """;
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].TodayTrades.Should().HaveCount(2);

        result[0].TodayTrades[0].Type.Should().Be(Core.Models.TradeRecord.TradeType.Buy);
        result[0].TodayTrades[0].Quantity.Should().Be(500);
        result[0].TodayTrades[0].Price.Should().Be(35.0m);

        result[0].TodayTrades[1].Type.Should().Be(Core.Models.TradeRecord.TradeType.Sell);
        result[0].TodayTrades[1].Quantity.Should().Be(200);
        result[0].TodayTrades[1].Price.Should().Be(36.0m);

        var savedJson = File.ReadAllText(filePath);
        savedJson.Should().Contain("\"todayTrades\"");
        savedJson.Should().NotContain("\"opList\"");
    }
}
