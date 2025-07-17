using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMonitor.Model
{
    public class StockConfig
    {
        /// <summary>
        /// 股票代码 纯数字
        /// </summary>
        [Description("code")]
        public string Code { get; set; }

        /// <summary>
        /// 持仓数量
        /// </summary>
        [Description("position")]
        public int Position { get; set; }

        /// <summary>
        /// 持仓成本
        /// </summary>
        [Description("cost")]
        public decimal Cost { get; set; }

        /// <summary>
        /// 操作集合
        /// </summary>
        public List<StockOperate> OpList { get; set; }
    }

    /// <summary>
    /// 操作记录
    /// </summary>
    public class StockOperate
    {
        public enum OperationType { Buy, Sell }

        public OperationType Type { get; set; }
        public int Position { get; set; }
        public decimal Price { get; set; }
        public DateTime Time { get; set; }

        public decimal Commission { get; set; } // 佣金
        public decimal Tax { get; set; }        // 印花税（仅卖出）
    }
}
