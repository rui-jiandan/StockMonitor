using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class StockDataServiceTests
{
    private readonly Mock<IStockDataProvider> _mockPrimary;
    private readonly Mock<IStockDataProvider> _mockSecondary;
    private readonly Mock<IPositionService> _mockPositionService;

    private readonly List<StockPosition> _samplePositions = new()
    {
        new StockPosition { Code = "sh600036", Name = "招商银行", Quantity = 300, AvgCostPrice = 43.267m },
        new StockPosition { Code = "sz512890", Name = "红利低波", Quantity = 16100, AvgCostPrice = 1.112m }
    };

    private readonly List<StockQuote> _primaryQuotes = new()
    {
        new StockQuote { Code = "sh600036", Name = "招商银行", CurrentPrice = 41.68m, YestClose = 41.51m, OpenPrice = 41.51m, HighPrice = 41.91m, LowPrice = 41.52m },
        new StockQuote { Code = "sz512890", Name = "红利低波", CurrentPrice = 1.12m, YestClose = 1.11m, OpenPrice = 1.11m, HighPrice = 1.13m, LowPrice = 1.10m }
    };

    private readonly List<StockQuote> _secondaryQuotes = new()
    {
        new StockQuote { Code = "sh600036", Name = "招商银行", CurrentPrice = 44.0m, YestClose = 43.0m, OpenPrice = 43.5m, HighPrice = 44.5m, LowPrice = 42.5m },
        new StockQuote { Code = "sz512890", Name = "红利低波", CurrentPrice = 1.15m, YestClose = 1.12m, OpenPrice = 1.13m, HighPrice = 1.16m, LowPrice = 1.11m }
    };

    public StockDataServiceTests()
    {
        _mockPrimary = new Mock<IStockDataProvider>();
        _mockPrimary.SetupGet(p => p.Name).Returns("Sina");
        _mockPrimary.SetupGet(p => p.IsAvailable).Returns(true);

        _mockSecondary = new Mock<IStockDataProvider>();
        _mockSecondary.SetupGet(p => p.Name).Returns("Tencent");
        _mockSecondary.SetupGet(p => p.IsAvailable).Returns(true);

        _mockPositionService = new Mock<IPositionService>();
        _mockPositionService.Setup(p => p.GetAllPositions()).Returns(_samplePositions.AsReadOnly());
    }

    [Fact]
    public async Task PrimarySuccess_ReturnsPrimaryData()
    {
        _mockPrimary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(_primaryQuotes);

        var service = new StockDataService(
            new[] { _mockPrimary.Object, _mockSecondary.Object },
            _mockPositionService.Object);

        var result = await service.GetQuotesAsync();

        result.Should().BeEquivalentTo(_primaryQuotes);
        _mockSecondary.Verify(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task PrimaryFails_FallsBackToSecondary()
    {
        _mockPrimary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());
        _mockSecondary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(_secondaryQuotes);

        var service = new StockDataService(
            new[] { _mockPrimary.Object, _mockSecondary.Object },
            _mockPositionService.Object);

        var result = await service.GetQuotesAsync();

        result.Should().BeEquivalentTo(_secondaryQuotes);
    }

    [Fact]
    public async Task AllFail_ReturnsCachedData()
    {
        _mockPrimary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(_primaryQuotes);
        _mockSecondary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());

        var service = new StockDataService(
            new[] { _mockPrimary.Object, _mockSecondary.Object },
            _mockPositionService.Object);

        await service.GetQuotesAsync();

        _mockPrimary.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new HttpRequestException("网络错误"));
        _mockPrimary.SetupGet(p => p.IsAvailable).Returns(false);

        var result = await service.GetQuotesAsync();

        result.Should().BeEquivalentTo(_primaryQuotes);
    }
}
