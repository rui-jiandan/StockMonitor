using FluentAssertions;
using StockMonitor.Core.Calculator;
using StockMonitor.Core.Models;
using Xunit;

namespace StockMonitor.Tests.Calculator;

public class PnLCalculatorTests
{
    private static StockPosition CreatePosition(
        int quantity, decimal avgCostPrice, List<TradeRecord>? todayTrades = null)
    {
        return new StockPosition
        {
            Code = "600000",
            Name = "测试股票",
            Quantity = quantity,
            AvgCostPrice = avgCostPrice,
            TodayTrades = todayTrades ?? new List<TradeRecord>()
        };
    }

    private static StockQuote CreateQuote(
        decimal currentPrice, decimal yestClose, decimal openPrice = 0m)
    {
        return new StockQuote
        {
            Code = "600000",
            Name = "测试股票",
            CurrentPrice = currentPrice,
            YestClose = yestClose,
            OpenPrice = openPrice == 0m ? currentPrice : openPrice
        };
    }

    private static TradeRecord CreateBuyTrade(
        int quantity, decimal price, decimal commission = 0m, DateTime? time = null)
    {
        return new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = quantity,
            Price = price,
            Commission = commission,
            Tax = 0m,
            Time = time ?? DateTime.Today.AddHours(10)
        };
    }

    private static TradeRecord CreateSellTrade(
        int quantity, decimal price, decimal commission = 0m, decimal tax = 0m, DateTime? time = null)
    {
        return new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = quantity,
            Price = price,
            Commission = commission,
            Tax = tax,
            Time = time ?? DateTime.Today.AddHours(10)
        };
    }

    [Fact]
    public void CalculateTodayPnL_NoTrades_OnlyUnrealized()
    {
        var position = CreatePosition(100, 10.0m);
        var quote = CreateQuote(11.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        unrealized.Should().Be(100m);
        total.Should().Be(100m);
    }

    [Fact]
    public void CalculateTodayPnL_TodayBuy_ThenSell_T0()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(100, 43.0m, commission: 5m, time: DateTime.Today.AddHours(9)),
            CreateSellTrade(100, 44.0m, commission: 5m, tax: 10m, time: DateTime.Today.AddHours(10))
        };
        var position = CreatePosition(100, 42.0m, trades);
        var quote = CreateQuote(44.5m, 42.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(180m);
        unrealized.Should().Be(395m);
        total.Should().Be(575m);
    }

    [Fact]
    public void CalculateTodayPnL_TodaySell_ThenBuy_T0()
    {
        var trades = new List<TradeRecord>
        {
            CreateSellTrade(100, 44.0m, commission: 5m, tax: 10m, time: DateTime.Today.AddHours(9)),
            CreateBuyTrade(100, 43.0m, commission: 5m, time: DateTime.Today.AddHours(10))
        };
        var position = CreatePosition(200, 40.0m, trades);
        var quote = CreateQuote(44.5m, 42.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(180m);
        unrealized.Should().Be(1045m);
        total.Should().Be(1225m);
    }

    [Fact]
    public void CalculateTodayPnL_MultipleBuys_CorrectUnrealized()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(100, 41.0m, commission: 3m, time: DateTime.Today.AddHours(9)),
            CreateBuyTrade(100, 42.0m, commission: 3m, time: DateTime.Today.AddHours(10))
        };
        var position = CreatePosition(100, 40.0m, trades);
        var quote = CreateQuote(43.0m, 40.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(-6m);
        unrealized.Should().Be(594m);
        total.Should().Be(588m);
    }

    [Fact]
    public void GetTodayMoney_NoTrades_ReturnsZero()
    {
        var position = CreatePosition(100, 10.0m);
        var quote = CreateQuote(11.0m, 10.0m);

        var money = PnLCalculator.GetTodayMoney(position, quote);

        money.Should().Be(0m);
    }

    [Fact]
    public void GetTodayMoney_WithTrades_ReturnsCorrectAmount()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(100, 43.0m, commission: 5m, time: DateTime.Today.AddHours(9)),
            CreateSellTrade(100, 44.0m, commission: 5m, tax: 10m, time: DateTime.Today.AddHours(10))
        };
        var position = CreatePosition(100, 42.0m, trades);
        var quote = CreateQuote(44.5m, 42.0m);

        var money = PnLCalculator.GetTodayMoney(position, quote);

        money.Should().Be(330m);
    }

    [Fact]
    public void CalculateTodayPnL_OldTradesNotToday_Ignored()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(100, 9.0m, commission: 5m, time: DateTime.Today.AddDays(-1).AddHours(10))
        };
        var position = CreatePosition(100, 10.0m, trades);
        var quote = CreateQuote(11.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        unrealized.Should().Be(100m);
        total.Should().Be(100m);
    }
}
