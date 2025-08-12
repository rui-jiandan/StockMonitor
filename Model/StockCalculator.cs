using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace StockMonitor.Model
{
    public class StockCalculator
    {
        private readonly decimal _commissionRate;
        private readonly decimal _taxRate;

        public StockCalculator(decimal CommissionRate= 0.00023m, decimal TaxRate= 0)
        {
            _commissionRate = CommissionRate;
            _taxRate = TaxRate;
        }

        /// <summary>
        /// 加仓操作，自动计算佣金
        /// </summary>
        public void AddPosition(StockConfig config, decimal buyPrice, int buyQuantity)
        {
            if (buyQuantity <= 0)
                throw new ArgumentException("加仓数量必须大于0");

            //当日的操作,另外计算
            //decimal totalCost = config.Cost * config.Position + buyPrice * buyQuantity;
            //config.Position += buyQuantity;
            //config.Cost = totalCost / config.Position;

            // 计算佣金
            decimal turnover = buyPrice * buyQuantity;
            decimal commission = turnover * _commissionRate;
            if (commission < 5)
            {
                //不满5元收5元
                commission = 5;
            }
            if (config.OpList == null)
            {
                config.OpList = new List<StockOperate>();
            }
            // 添加操作记录
            config.OpList.Add(new StockOperate
            {
                Type = StockOperate.OperationType.Buy,
                Position = buyQuantity,
                Price = buyPrice,
                Time = DateTime.Now,
                Commission = commission,
                Tax = 0 // 买入不收印花税
            });
        }

        /// <summary>
        /// 减仓操作，自动计算佣金与印花税
        /// </summary>
        public void ReducePosition(StockConfig config, decimal sellPrice, int sellQuantity)
        {
            if (sellQuantity <= 0)
                throw new ArgumentException("减仓数量必须大于0");
            if (sellQuantity > config.Position)
                throw new InvalidOperationException("减仓数量超过当前持仓");

            // 更新持仓
            config.Position -= sellQuantity;

            // 计算成交额
            decimal turnover = sellPrice * sellQuantity;

            // 计算佣金和印花税
            decimal commission = Math.Round(turnover * _commissionRate, 2);
            if (commission < 5)
            {
                //不满5元收5元
                commission = 5;
            }
            decimal tax = Math.Round(turnover * _taxRate, 2);
            
            if (config.OpList == null)
            {
                config.OpList = new List<StockOperate>();
            }
            // 添加操作记录
            config.OpList.Add(new StockOperate
            {
                Type = StockOperate.OperationType.Sell,
                Position = -sellQuantity,
                Price = sellPrice,
                Time = DateTime.Now,
                Commission = commission,
                Tax = tax
            });
        }

        /// <summary>
        /// 计算今日盈亏（扣除交易费用）
        /// </summary>
        public static (decimal Realized, decimal Unrealized, decimal Total) CalculateTodayPnL(StockConfig config, decimal currentPrice)
        {
            var today = DateTime.Today;

            // 已实现盈亏
            decimal realizedPnL = 0;

            // 累计佣金与印花税
            decimal totalCommission = 0;
            decimal totalTax = 0;

            foreach (var op in config.OpList)
            {
                if (op.Time.Date != today)
                    continue;

                if (op.Type == StockOperate.OperationType.Buy)
                {
                    // 买入不计入盈亏
                    totalCommission += op.Commission;
                }
                else if (op.Type == StockOperate.OperationType.Sell)
                {
                    // 卖出盈亏 = (卖出价 - 成本价) * 数量
                    int quantity = -op.Position;
                    realizedPnL += (op.Price - config.Cost) * quantity;
                    totalCommission += op.Commission;
                    totalTax += op.Tax;
                }
            }

            // 浮动盈亏 = 当前持仓 × (现价 - 成本价)
            decimal unrealizedPnL = (currentPrice - config.Cost) * config.Position;

            // 总盈亏 = 已实现 + 浮动盈亏 - 总佣金 - 总印花税
            decimal totalPnL = realizedPnL + unrealizedPnL - totalCommission - totalTax;

            return (realizedPnL, unrealizedPnL, totalPnL);
        }

        /// <summary>
        /// 获取今日盈亏金额
        /// </summary>
        /// <returns></returns>
        public static decimal GetTodayMoney(StockView t, decimal currentPrice)
        {
            if (t != null && t.OpList != null && t.OpList.Count > 0)
            {
                var today = DateTime.Today;
                // 已实现盈亏
                decimal realizedPnL = 0;

                // 累计佣金与印花税
                decimal totalCommission = 0;
                decimal totalTax = 0;

                foreach (var op in t.OpList)
                {
                    if (op.Time.Date != today)
                        continue;
                    int quantity = Math.Abs(op.Position);
                    if (op.Type == StockOperate.OperationType.Buy)
                    {
                        //今日买入盈亏 = (当前价 - 买入价) * 数量
                        realizedPnL += (currentPrice - op.Price) * quantity;
                        totalCommission += op.Commission;
                    }
                    else if (op.Type == StockOperate.OperationType.Sell)
                    {
                        // 今日卖出盈亏 = (卖出价 - 昨日开盘价) * 数量
                        realizedPnL += (op.Price - t.YestClose) * quantity;
                        totalCommission += op.Commission;
                        totalTax += op.Tax;
                    }
                }
                return realizedPnL - totalCommission - totalTax;
            }
            return 0;
        }
    }
}