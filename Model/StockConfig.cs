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
        public int Position { get; set; }

        /// <summary>
        /// 持仓成本
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// 加仓正，减仓负
        /// </summary>
        public long IncreaseTime { get; set; }
    }
}
