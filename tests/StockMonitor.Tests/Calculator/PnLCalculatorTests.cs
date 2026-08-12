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

    // ========== 场景1：加仓（只买不卖）==========

    /// <summary>
    /// 加仓：原始持仓今日涨跌 + 今日买入浮动盈亏
    /// </summary>
    [Fact]
    public void Scenario1_AddPosition_OnlyBuy()
    {
        // 原始1000@10，昨收10，买入500@10.5，当前11
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(500, 10.5m, commission: 5m)
        };
        var position = CreatePosition(1000, 10.0m, trades);
        var quote = CreateQuote(11.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        // 原始持仓今日浮动 (11-10)*1000=1000，今日买入浮动 (11-10.5)*500-5=245
        unrealized.Should().Be(1245m);
        total.Should().Be(1245m);
    }

    // ========== 场景2：做T（先买后卖，无原始持仓）==========

    /// <summary>
    /// 做T：买入后卖出，已实现盈亏 = 卖出收入 - 买入成本 - 佣金
    /// </summary>
    [Fact]
    public void Scenario2_T0_BuyThenSell_NoOriginal()
    {
        // 无原始持仓，买入1000@10.5，卖出1000@11
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(1000, 10.5m, commission: 5m),
            CreateSellTrade(1000, 11.0m, commission: 5m, tax: 10m)
        };
        var position = CreatePosition(0, 0m, trades);
        var quote = CreateQuote(11.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现 = 11000 - 10500 - 5 - 10 - 5 = 480
        realized.Should().Be(480m);
        unrealized.Should().Be(0m);
        total.Should().Be(480m);
    }

    // ========== 场景3：减仓（只卖不买）==========

    /// <summary>
    /// 减仓：卖出原始持仓，已实现用昨收价做基准，剩余按昨收价算浮动
    /// </summary>
    [Fact]
    public void Scenario3_ReducePosition_OnlySell()
    {
        // 原始1000@成本10，昨收10，卖出500@11，当前10.5
        var trades = new List<TradeRecord>
        {
            CreateSellTrade(500, 11.0m, commission: 5m, tax: 5m)
        };
        var position = CreatePosition(1000, 10.0m, trades);
        var quote = CreateQuote(10.5m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现 = (11-10)*500 - 5 - 5 = 490
        realized.Should().Be(490m);
        // 剩余500股浮动 = (10.5-10)*500 = 250
        unrealized.Should().Be(250m);
        total.Should().Be(740m);
    }

    /// <summary>
    /// 减仓且成本价≠昨收价：已实现盈亏应该用昨收价，不是成本价
    /// </summary>
    [Fact]
    public void Scenario3_ReducePosition_CostDiffersFromYestClose()
    {
        // 原始1000@成本8，昨收10，卖出500@11，当前10.5
        var trades = new List<TradeRecord>
        {
            CreateSellTrade(500, 11.0m, commission: 5m, tax: 5m)
        };
        var position = CreatePosition(1000, 8.0m, trades);
        var quote = CreateQuote(10.5m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现 = (11-昨收10)*500 - 5 - 5 = 490（不是(11-成本8)*500=1500）
        realized.Should().Be(490m);
        // 剩余500股浮动 = (10.5-10)*500 = 250（不是按1000股算）
        unrealized.Should().Be(250m);
        total.Should().Be(740m);
    }

    // ========== 场景4：多次做T（加200，减100，减100，加200）==========

    /// <summary>
    /// 多次做T：加200@10.5，减100@11，减100@10.8，加200@10.3
    /// 原始持仓1000@10，昨收10，当前10.6
    /// </summary>
    [Fact]
    public void Scenario4_MultipleT0_Buy200_Sell100_Sell100_Buy200()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(200, 10.5m, commission: 2m, time: DateTime.Today.AddHours(9)),
            CreateSellTrade(100, 11.0m, commission: 2m, tax: 2m, time: DateTime.Today.AddHours(10)),
            CreateSellTrade(100, 10.8m, commission: 2m, tax: 2m, time: DateTime.Today.AddHours(11)),
            CreateBuyTrade(200, 10.3m, commission: 2m, time: DateTime.Today.AddHours(14))
        };
        var position = CreatePosition(1000, 10.0m, trades);
        var quote = CreateQuote(10.6m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 买入统计：400股，总成本 200*10.5+200*10.3=4160，均价10.4，佣金4
        // 卖出统计：200股，收入 100*11+100*10.8=2180，佣金4，税4
        // FIFO：200股全来自今日买入
        // 已实现 = 2180 - 200*10.4 - 4 - 4 - 4*(200/400) = 2180-2080-4-4-2 = 90
        realized.Should().Be(90m);

        // 原始持仓剩余 = 1000-0=1000（没卖出原始持仓）
        // 原始浮动 = (10.6-10)*1000 = 600
        // 今日买入剩余 = 400-200=200
        // 今日买入浮动 = (10.6-10.4)*200 - 4*(200/400) = 40-2 = 38
        // 未实现 = 600+38 = 638
        unrealized.Should().Be(638m);
        total.Should().Be(728m);
    }

    /// <summary>
    /// 多次做T且卖出超过今日买入（消耗原始持仓）
    /// 加200@10.5，减500@11（200来自今日买入，300来自原始持仓）
    /// 原始持仓1000@成本8，昨收10，当前10.5
    /// </summary>
    [Fact]
    public void Scenario4_MultipleT0_SellExceedsTodayBuy()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(200, 10.5m, commission: 2m),
            CreateSellTrade(500, 11.0m, commission: 5m, tax: 5m)
        };
        var position = CreatePosition(1000, 8.0m, trades);
        var quote = CreateQuote(10.5m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // FIFO：200来自今日买入@10.5，300来自原始持仓（基准昨收10）
        // 已实现 = 5500 - 200*10.5 - 300*10 - 5 - 5 - 2*(200/200)
        //        = 5500 - 2100 - 3000 - 5 - 5 - 2 = 388
        realized.Should().Be(388m);

        // 原始剩余 = 1000-300=700
        // 原始浮动 = (10.5-10)*700 = 350
        // 今日买入剩余 = 0
        unrealized.Should().Be(350m);
        total.Should().Be(738m);
    }

    // ========== 补充场景：T+0 先卖后买 ==========

    /// <summary>
    /// 先卖后买：卖出原始持仓，再买回
    /// 原始200@40，卖出100@44，买入100@43，昨收42，当前44.5
    /// </summary>
    [Fact]
    public void Scenario5_SellThenBuy_T0()
    {
        var trades = new List<TradeRecord>
        {
            CreateSellTrade(100, 44.0m, commission: 5m, tax: 10m, time: DateTime.Today.AddHours(9)),
            CreateBuyTrade(100, 43.0m, commission: 5m, time: DateTime.Today.AddHours(10))
        };
        var position = CreatePosition(200, 40.0m, trades);
        var quote = CreateQuote(44.5m, 42.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // FIFO：100股全来自今日买入@43
        // 已实现 = 4400 - 100*43 - 5 - 10 - 5 = 4400-4300-5-10-5 = 80
        realized.Should().Be(80m);

        // 原始剩余 = 200-0=200（卖出100全部来自今日买入）
        // 原始浮动 = (44.5-42)*200 = 500
        // 今日买入剩余 = 100-100=0
        unrealized.Should().Be(500m);
        total.Should().Be(580m);
    }

    // ========== 用户真实场景 ==========

    /// <summary>
    /// 用户真实场景：ETF T+0 买入4200@1.486，卖出4200@1.468
    /// </summary>
    [Fact]
    public void UserScenario_ETF_T0_Complete()
    {
        var trades = new List<TradeRecord>
        {
            new()
            {
                Type = TradeRecord.TradeType.Buy,
                Quantity = 4200,
                Price = 1.486m,
                Commission = 0.31206m,
                Tax = 0m,
                Time = DateTime.Today.AddHours(14).AddMinutes(5).AddSeconds(26)
            },
            new()
            {
                Type = TradeRecord.TradeType.Sell,
                Quantity = 4200,
                Price = 1.468m,
                Commission = 0.30828m,
                Tax = 0m,
                Time = DateTime.Today.AddHours(14).AddMinutes(57).AddSeconds(23)
            }
        };
        var position = CreatePosition(0, 0m, trades);
        var quote = CreateQuote(1.468m, 1.486m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现 = 6165.6 - 6241.2 - 0.30828 - 0 - 0.31206 = -76.22034
        realized.Should().Be(-76.22034m);
        unrealized.Should().Be(0m);
        total.Should().Be(-76.22034m);
    }

    /// <summary>
    /// 用户真实场景：科创半导体ETF，加仓1000@1.032
    /// 原始2600@1.27，昨收1.053，当前1.044
    /// </summary>
    [Fact]
    public void UserScenario_AddPosition_ETF()
    {
        var trades = new List<TradeRecord>
        {
            new()
            {
                Type = TradeRecord.TradeType.Buy,
                Quantity = 1000,
                Price = 1.032m,
                Commission = 0.1m,
                Tax = 0m,
                Time = DateTime.Today.AddHours(14).AddMinutes(4).AddSeconds(34)
            }
        };
        var position = CreatePosition(2600, 1.27m, trades);
        var quote = CreateQuote(1.044m, 1.053m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        // 原始2600股今日浮动 = (1.044-1.053)*2600 = -23.4
        // 今日买入1000股浮动 = (1.044-1.032)*1000 - 0.1 = 12 - 0.1 = 11.9
        // 合计 = -23.4 + 11.9 = -11.5
        unrealized.Should().Be(-11.5m);
        total.Should().Be(-11.5m);
    }

    /// <summary>
    /// 用户真实场景：红利低波，无交易，今日涨跌
    /// 3200@1.12，昨收1.119，当前1.124
    /// </summary>
    [Fact]
    public void UserScenario_NoTrades_Holding()
    {
        var position = CreatePosition(3200, 1.12m);
        var quote = CreateQuote(1.124m, 1.119m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        // 今日浮动 = (1.124-1.119)*3200 = 16
        unrealized.Should().Be(16m);
        total.Should().Be(16m);
    }

    /// <summary>
    /// 用户真实场景：深科技，无交易，今日下跌
    /// 600@40.60，昨收41.010，当前40.560
    /// </summary>
    [Fact]
    public void UserScenario_NoTrades_Down()
    {
        var position = CreatePosition(600, 40.60m);
        var quote = CreateQuote(40.560m, 41.010m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        // 今日浮动 = (40.560-41.010)*600 = -270
        unrealized.Should().Be(-270m);
        total.Should().Be(-270m);
    }

    // ========== 边界场景 ==========

    /// <summary>
    /// 无交易无持仓
    /// </summary>
    [Fact]
    public void EdgeCase_NoPosition_NoTrades()
    {
        var position = CreatePosition(0, 0m);
        var quote = CreateQuote(10.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        unrealized.Should().Be(0m);
        total.Should().Be(0m);
    }

    /// <summary>
    /// 非今日交易应被忽略
    /// </summary>
    [Fact]
    public void EdgeCase_OldTradesIgnored()
    {
        var trades = new List<TradeRecord>
        {
            CreateBuyTrade(100, 9.0m, commission: 5m, time: DateTime.Today.AddDays(-1).AddHours(10))
        };
        var position = CreatePosition(100, 10.0m, trades);
        var quote = CreateQuote(11.0m, 10.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0m);
        unrealized.Should().Be(100m); // (11-10)*100 今日涨幅
        total.Should().Be(100m);
    }
}