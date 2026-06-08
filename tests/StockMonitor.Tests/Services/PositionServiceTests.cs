using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class PositionServiceTests
{
    private readonly Mock<IRepository<List<StockPosition>>> _mockRepo;
    private readonly List<StockPosition> _samplePositions;
    private readonly PositionService _service;

    public PositionServiceTests()
    {
        _samplePositions = new List<StockPosition>
        {
            new()
            {
                Code = "sh600036",
                Name = "招商银行",
                Quantity = 300,
                AvgCostPrice = 43.267m,
                TodayTrades = new List<TradeRecord>()
            },
            new()
            {
                Code = "sz512890",
                Name = "红利低波",
                Quantity = 16100,
                AvgCostPrice = 1.112m,
                TodayTrades = new List<TradeRecord>()
            }
        };

        _mockRepo = new Mock<IRepository<List<StockPosition>>>();
        _mockRepo.Setup(r => r.Load()).Returns(_samplePositions);
        _service = new PositionService(_mockRepo.Object);
    }

    [Fact]
    public void GetAllPositions_ReturnsAll()
    {
        var result = _service.GetAllPositions();

        result.Should().HaveCount(2);
        result[0].Code.Should().Be("sh600036");
        result[1].Code.Should().Be("sz512890");
    }

    [Fact]
    public void GetPosition_ExistingCode_ReturnsPosition()
    {
        var result = _service.GetPosition("sh600036");

        result.Should().NotBeNull();
        result!.Code.Should().Be("sh600036");
        result.Quantity.Should().Be(300);
    }

    [Fact]
    public void GetPosition_NonExistentCode_ReturnsNull()
    {
        var result = _service.GetPosition("sz000001");

        result.Should().BeNull();
    }

    [Fact]
    public void AddPosition_CreatesTradeRecord()
    {
        _service.AddPosition("sh600036", 44.0m, 100);

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(300);
        position.AvgCostPrice.Should().Be(43.267m);
        position.TodayTrades.Should().HaveCount(1);
        position.TodayTrades[0].Type.Should().Be(TradeRecord.TradeType.Buy);
        position.TodayTrades[0].Quantity.Should().Be(100);
        position.TodayTrades[0].Price.Should().Be(44.0m);
    }

    [Fact]
    public void ReducePosition_CreatesTradeRecord()
    {
        _service.ReducePosition("sh600036", 45.0m, 100);

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(300);
        position.AvgCostPrice.Should().Be(43.267m);
        position.TodayTrades.Should().HaveCount(1);
        position.TodayTrades[0].Type.Should().Be(TradeRecord.TradeType.Sell);
        position.TodayTrades[0].Quantity.Should().Be(100);
        position.TodayTrades[0].Price.Should().Be(45.0m);
    }

    [Fact]
    public void ReducePosition_ExceedsTotal_ThrowsException()
    {
        var act = () => _service.ReducePosition("sh600036", 45.0m, 500);

        act.Should().Throw<InsufficientPositionException>();
    }

    [Fact]
    public void AddPosition_ThenReducePosition_T0_Success()
    {
        _service.AddPosition("sh600036", 44.0m, 100);
        _service.ReducePosition("sh600036", 45.0m, 100);

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(300);
        position.GetTotalQuantity().Should().Be(300);
        position.TodayTrades.Should().HaveCount(2);
    }

    [Fact]
    public void AddPosition_ThenReducePosition_ExceedsTotal_ThrowsException()
    {
        _service.AddPosition("sh600036", 44.0m, 100);

        var act = () => _service.ReducePosition("sh600036", 45.0m, 500);

        act.Should().Throw<InsufficientPositionException>();
    }

    [Fact]
    public void MergeDayTrades_UpdatesQuantityAndCost()
    {
        _service.AddPosition("sh600036", 44.0m, 100);

        _service.MergeDayTrades();

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(400);
        position.AvgCostPrice.Should().BeApproximately(
            (300 * 43.267m + 100 * 44.0m + 5m) / 400m, 0.0001m);
        position.TodayTrades.Should().BeEmpty();
    }

    [Fact]
    public void MergeDayTrades_WithSell_DecreasesQuantity()
    {
        _service.ReducePosition("sh600036", 45.0m, 100);

        _service.MergeDayTrades();

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(200);
        position.AvgCostPrice.Should().Be(43.267m);
        position.TodayTrades.Should().BeEmpty();
    }

    [Fact]
    public void AddStock_CreatesNewPosition()
    {
        _service.AddStock("sz000001");

        var position = _service.GetPosition("sz000001");
        position.Should().NotBeNull();
        position!.Code.Should().Be("sz000001");
        position.Quantity.Should().Be(0);
        position.AvgCostPrice.Should().Be(0m);
    }

    [Fact]
    public void RemoveStock_DeletesPosition()
    {
        _service.RemoveStock("sz512890");

        _service.GetPosition("sz512890").Should().BeNull();
        _service.GetAllPositions().Should().HaveCount(1);
    }

    [Fact]
    public void UpdatePosition_SetsQuantityAndCost()
    {
        _service.UpdatePosition("sh600036", 500, 42.5m);

        var position = _service.GetPosition("sh600036");
        position.Should().NotBeNull();
        position!.Quantity.Should().Be(500);
        position.AvgCostPrice.Should().Be(42.5m);
    }

    [Fact]
    public void PositionChanged_FiredOnAddPosition()
    {
        string? changedCode = null;
        _service.PositionChanged += (_, e) => changedCode = e.Code;

        _service.AddPosition("sh600036", 44.0m, 100);

        changedCode.Should().Be("sh600036");
    }

    /// <summary>
    /// 验证核心 Bug 修复：昨日加仓的交易记录在次日 MergeDayTrades 时应被正确合并
    /// </summary>
    [Fact]
    public void MergeDayTrades_YesterdayBuyTrades_MergesCorrectly()
    {
        // 模拟昨日加仓：手动添加一条日期为昨天的买入记录
        var position = _service.GetPosition("sh600036")!;
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = 100,
            Price = 44.0m,
            Time = DateTime.Today.AddDays(-1),
            Commission = 5m,
            Tax = 0m
        });

        _service.MergeDayTrades();

        position.Quantity.Should().Be(400);
        position.AvgCostPrice.Should().BeApproximately(
            (300 * 43.267m + 100 * 44.0m + 5m) / 400m, 0.0001m);
        position.TodayTrades.Should().BeEmpty();
    }

    /// <summary>
    /// 验证核心 Bug 修复：昨日减仓的交易记录在次日 MergeDayTrades 时应被正确合并
    /// </summary>
    [Fact]
    public void MergeDayTrades_YesterdaySellTrades_MergesCorrectly()
    {
        var position = _service.GetPosition("sh600036")!;
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = 100,
            Price = 45.0m,
            Time = DateTime.Today.AddDays(-1),
            Commission = 5m,
            Tax = 0m
        });

        _service.MergeDayTrades();

        position.Quantity.Should().Be(200);
        position.AvgCostPrice.Should().Be(43.267m);
        position.TodayTrades.Should().BeEmpty();
    }

    /// <summary>
    /// 验证跨多日的未合并交易能一次性全部合并
    /// </summary>
    [Fact]
    public void MergeDayTrades_MultiDayTrades_MergesAll()
    {
        var position = _service.GetPosition("sh600036")!;
        // 前天买入 100 股
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = 100,
            Price = 42.0m,
            Time = DateTime.Today.AddDays(-2),
            Commission = 5m,
            Tax = 0m
        });
        // 昨天卖出 50 股
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = 50,
            Price = 44.0m,
            Time = DateTime.Today.AddDays(-1),
            Commission = 5m,
            Tax = 0m
        });

        _service.MergeDayTrades();

        // 净增 50 股，总持仓 350
        position.Quantity.Should().Be(350);
        position.TodayTrades.Should().BeEmpty();
    }

    /// <summary>
    /// 验证 GetTodayNetQuantity 对昨日交易也能正确计算净数量
    /// </summary>
    [Fact]
    public void GetTodayNetQuantity_YesterdayTrades_IncludedInCalculation()
    {
        var position = _service.GetPosition("sh600036")!;
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = 100,
            Price = 44.0m,
            Time = DateTime.Today.AddDays(-1),
            Commission = 5m,
            Tax = 0m
        });

        position.GetTodayNetQuantity().Should().Be(100);
        position.GetTotalQuantity().Should().Be(400);
    }

    /// <summary>
    /// 验证 GetTotalQuantity 对昨日减仓交易也能正确计算
    /// </summary>
    [Fact]
    public void GetTotalQuantity_YesterdaySellTrades_ReflectsCorrectly()
    {
        var position = _service.GetPosition("sh600036")!;
        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = 100,
            Price = 45.0m,
            Time = DateTime.Today.AddDays(-1),
            Commission = 5m,
            Tax = 0m
        });

        position.GetTotalQuantity().Should().Be(200);
    }
}
