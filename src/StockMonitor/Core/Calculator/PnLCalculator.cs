using StockMonitor.Core.Models;

namespace StockMonitor.Core.Calculator;

/// <summary>
/// 盈亏计算器，支持 T+0 日内回转交易场景
/// </summary>
public static class PnLCalculator
{
    /// <summary>
    /// 计算今日盈亏，包含已实现盈亏、未实现盈亏和总盈亏
    /// </summary>
    /// <param name="position">持仓信息，<param name="position">包含原始持仓数量、成本价和当日交易记录</param></param>
    /// <param name="quote">实时行情，<param name="quote">提供当前价、昨收价等</param></param>
    /// <returns>元组：(已实现盈亏, 未实现盈亏, 总盈亏)</returns>
    public static (decimal Realized, decimal Unrealized, decimal Total) CalculateTodayPnL(
        StockPosition position, StockQuote quote)
    {
        decimal realizedPnL = 0m;
        decimal buyUnrealized = 0m;
        decimal totalCommission = 0m;
        decimal totalTax = 0m;

        foreach (var trade in position.TodayTrades.Where(t => t.Time.Date == DateTime.Today))
        {
            if (trade.Type == TradeRecord.TradeType.Buy)
            {
                buyUnrealized += (quote.CurrentPrice - trade.Price) * trade.Quantity - trade.Commission;
                totalCommission += trade.Commission;
            }
            else
            {
                realizedPnL += (trade.Price - quote.YestClose) * trade.Quantity;
                totalCommission += trade.Commission;
                totalTax += trade.Tax;
            }
        }

        realizedPnL -= (totalCommission + totalTax);

        decimal holdUnrealized = (quote.CurrentPrice - position.AvgCostPrice) * position.Quantity;
        decimal totalUnrealized = buyUnrealized + holdUnrealized;
        decimal total = realizedPnL + totalUnrealized;

        return (realizedPnL, totalUnrealized, total);
    }

    /// <summary>
    /// 获取今日盈亏金额（简化版，用于界面展示）
    /// </summary>
    /// <param name="position">持仓信息</param>
    /// <param name="quote">实时行情</param>
    /// <returns>今日盈亏金额</returns>
    public static decimal GetTodayMoney(StockPosition position, StockQuote quote)
    {
        if (position.TodayTrades == null || !position.TodayTrades.Any(t => t.Time.Date == DateTime.Today))
            return 0m;

        decimal money = 0m;

        foreach (var trade in position.TodayTrades.Where(t => t.Time.Date == DateTime.Today))
        {
            if (trade.Type == TradeRecord.TradeType.Buy)
            {
                money += (quote.CurrentPrice - trade.Price) * trade.Quantity - trade.Commission;
            }
            else
            {
                money += (trade.Price - quote.YestClose) * trade.Quantity - trade.Commission - trade.Tax;
            }
        }

        return money;
    }
}
