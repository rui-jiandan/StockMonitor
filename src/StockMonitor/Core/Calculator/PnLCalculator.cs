using StockMonitor.Core.Models;

namespace StockMonitor.Core.Calculator;

/// <summary>
/// 盈亏计算器，支持 T+0 日内回转交易场景
/// </summary>
public static class PnLCalculator
{
    /// <summary>
    /// 计算今日盈亏，包含已实现盈亏、未实现盈亏和总盈亏。
    /// 今日盈亏的基准是昨收价：原始持仓按(当前价-昨收价)算今日涨跌；
    /// 今日买入按(当前价-买入价)算盈亏。卖出按FIFO先消耗今日买入再消耗原始持仓。
    /// </summary>
    /// <param name="position">持仓信息，包含原始持仓数量、成本价和当日交易记录</param>
    /// <param name="quote">实时行情，提供当前价、昨收价等</param>
    /// <returns>元组：(已实现盈亏, 未实现盈亏, 总盈亏)</returns>
    public static (decimal Realized, decimal Unrealized, decimal Total) CalculateTodayPnL(
        StockPosition position, StockQuote quote)
    {
        var todayTrades = position.TodayTrades
            .Where(t => t.Time.Date == DateTime.Today)
            .ToList();

        // 无今日交易：原始持仓的今日浮动盈亏 = (当前价 - 昨收价) × 持仓量
        if (todayTrades.Count == 0)
        {
            decimal holdPnL = position.Quantity > 0
                ? (quote.CurrentPrice - quote.YestClose) * position.Quantity
                : 0m;
            return (0m, holdPnL, holdPnL);
        }

        // 统计当天买入和卖出的交易数据
        int buyQty = 0, sellQty = 0;
        decimal buyAmount = 0m, buyCommission = 0m;
        decimal sellAmount = 0m, sellCommission = 0m, sellTax = 0m;

        foreach (var trade in todayTrades)
        {
            if (trade.Type == TradeRecord.TradeType.Buy)
            {
                buyQty += trade.Quantity;
                buyAmount += trade.Price * trade.Quantity;
                buyCommission += trade.Commission;
            }
            else
            {
                sellQty += trade.Quantity;
                sellAmount += trade.Price * trade.Quantity;
                sellCommission += trade.Commission;
                sellTax += trade.Tax;
            }
        }

        // 计算买入加权平均价
        decimal avgBuyPrice = buyQty > 0 ? buyAmount / buyQty : 0m;

        // FIFO：卖出先消耗今日买入，再消耗原始持仓
        int soldFromToday = Math.Min(sellQty, buyQty);
        int soldFromOriginal = Math.Max(0, sellQty - buyQty);

        // --- 已实现盈亏 ---
        decimal realizedPnL = 0m;
        if (sellQty > 0)
        {
            // 今日买入卖出的部分：用买入价作为成本
            decimal costOfSoldToday = soldFromToday * avgBuyPrice;
            // 原始持仓卖出的部分：用昨收价作为今日基准（衡量今日涨跌）
            decimal costOfSoldOriginal = soldFromOriginal * quote.YestClose;
            // 买入佣金按比例分摊到已卖出部分
            decimal buyCommissionForSold = buyQty > 0
                ? buyCommission * soldFromToday / buyQty
                : 0m;

            realizedPnL = sellAmount - costOfSoldToday - costOfSoldOriginal
                         - sellCommission - sellTax - buyCommissionForSold;
        }

        // --- 未实现盈亏 ---
        int remainingTodayQty = Math.Max(0, buyQty - sellQty);
        // 原始持仓减去已被卖出的部分，才是真正剩余的原始持仓
        int remainingOriginalQty = Math.Max(0, position.Quantity - soldFromOriginal);

        decimal unrealizedPnL = 0m;

        // 原始持仓剩余部分的今日浮动盈亏
        if (remainingOriginalQty > 0)
        {
            unrealizedPnL += (quote.CurrentPrice - quote.YestClose) * remainingOriginalQty;
        }

        // 今日买入未卖出部分的浮动盈亏
        if (remainingTodayQty > 0)
        {
            decimal todayRemainPnL = (quote.CurrentPrice - avgBuyPrice) * remainingTodayQty;
            decimal buyCommissionForRemain = buyCommission * remainingTodayQty / buyQty;
            unrealizedPnL += todayRemainPnL - buyCommissionForRemain;
        }

        decimal total = realizedPnL + unrealizedPnL;
        return (realizedPnL, unrealizedPnL, total);
    }

    /// <summary>
    /// 获取今日盈亏金额（简化版，用于界面展示）
    /// </summary>
    /// <param name="position">持仓信息</param>
    /// <param name="quote">实时行情</param>
    /// <returns>今日盈亏金额</returns>
    public static decimal GetTodayMoney(StockPosition position, StockQuote quote)
    {
        var (_, _, total) = CalculateTodayPnL(position, quote);
        return total;
    }
}