using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class AlertServiceTests
{
    private readonly Mock<IRepository<List<AlertRule>>> _mockRepo;
    private AlertService _service;

    public AlertServiceTests()
    {
        _mockRepo = new Mock<IRepository<List<AlertRule>>>();
        _mockRepo.Setup(r => r.Load()).Returns(new List<AlertRule>());
        _service = new AlertService(_mockRepo.Object);
    }

    [Fact]
    public void AddRule_SavesAndReturnsRule()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 50m
        };

        _service.AddRule(rule);

        _mockRepo.Verify(r => r.Save(It.IsAny<List<AlertRule>>()), Times.Once);
        _service.GetRules().Should().Contain(r => r.StockCode == "sh600036" && r.Threshold == 50m);
    }

    [Fact]
    public void RemoveRule_RemovesFromList()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 50m
        };
        _service.AddRule(rule);

        _service.RemoveRule(rule.Id);

        _service.GetRules().Should().NotContain(r => r.Id == rule.Id);
        _mockRepo.Verify(r => r.Save(It.IsAny<List<AlertRule>>()), Times.Exactly(2));
    }

    [Fact]
    public void CheckAlerts_PriceAboveThreshold_TriggersEvent()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 42m,
            IsOneTime = false
        };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", Name = "招商银行", CurrentPrice = 43m, YestClose = 41m }
        };

        AlertTriggeredEventArgs? triggeredArgs = null;
        _service.AlertTriggered += (_, e) => triggeredArgs = e;

        _service.CheckAlerts(quotes);

        triggeredArgs.Should().NotBeNull();
        triggeredArgs!.Rule.StockCode.Should().Be("sh600036");
        triggeredArgs.Quote.CurrentPrice.Should().Be(43m);
        rule.IsTriggered.Should().BeTrue();
    }

    [Fact]
    public void CheckAlerts_PriceBelowThreshold_DoesNotTrigger()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 50m,
            IsOneTime = false
        };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", Name = "招商银行", CurrentPrice = 43m, YestClose = 41m }
        };

        bool eventFired = false;
        _service.AlertTriggered += (_, _) => eventFired = true;

        _service.CheckAlerts(quotes);

        eventFired.Should().BeFalse();
        rule.IsTriggered.Should().BeFalse();
    }

    [Fact]
    public void CheckAlerts_AlreadyTriggered_DoesNotFireAgain()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 42m,
            IsTriggered = true,
            IsOneTime = false
        };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", Name = "招商银行", CurrentPrice = 43m, YestClose = 41m }
        };

        int fireCount = 0;
        _service.AlertTriggered += (_, _) => fireCount++;

        _service.CheckAlerts(quotes);

        fireCount.Should().Be(0);
    }

    [Fact]
    public void CheckAlerts_OneTimeRule_TriggeredThenRemoved()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.PriceAbove,
            Threshold = 42m,
            IsOneTime = true
        };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", Name = "招商银行", CurrentPrice = 43m, YestClose = 41m }
        };

        _service.CheckAlerts(quotes);

        _service.GetRules().Should().NotContain(r => r.Id == rule.Id);
    }

    [Fact]
    public void CheckAlerts_ChangeRateAbove_TriggersCorrectly()
    {
        var rule = new AlertRule
        {
            StockCode = "sh600036",
            Type = AlertType.ChangeRateAbove,
            Threshold = 5m,
            IsOneTime = false
        };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", Name = "招商银行", CurrentPrice = 44.1m, YestClose = 41m }
        };

        AlertTriggeredEventArgs? triggeredArgs = null;
        _service.AlertTriggered += (_, e) => triggeredArgs = e;

        _service.CheckAlerts(quotes);

        triggeredArgs.Should().NotBeNull();
        triggeredArgs!.Rule.Type.Should().Be(AlertType.ChangeRateAbove);
        triggeredArgs.Quote.ChangeRate.Should().BeGreaterThan(5m);
    }
}
