using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StockMonitor.Model
{
    /// <summary>
    /// 股票信息视图模型
    /// </summary>
    public class StockView: StockConfig
    {

        /// <summary>
        /// 股票名称
        /// </summary>
        [Description("name")]
        public string Name { get; set; }

        /// <summary>
        /// 股票价格
        /// </summary>
        [Description("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// 股票开盘价
        /// </summary>
        [Description("open")]
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// 股票昨收价
        /// </summary>
        [Description("yest")]
        public decimal YestClose { get; set; }

        /// <summary>
        /// 股票最高价
        /// </summary>
        [Description("high")]
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 股票最低价
        /// </summary>
        [Description("low")]
        public decimal LowPrice { get; set; }

        /// <summary>
        /// 股票变化价格
        /// </summary>
        [Description("change")]
        public decimal Change
        {
            get
            {
                return Price - YestClose;
            }
        }

        /// <summary>
        /// 股票变化率
        /// </summary>
        [Description("rate")]
        public decimal ChangeRate
        {
            get
            {
                return Math.Round((Change / YestClose) *100,2);
            }
        }

        /// <summary>
        /// 股票颜色
        /// </summary>
        public Color Color
        {
            get
            {
                return YestClose > Price ? Color.Green : (YestClose < Price ? Color.Red : Color.White);
            }
        }

        /// <summary>
        /// 当前持仓盈亏
        /// </summary>
        [Description("makemoney")]
        public string NowMakeMoney
        {
            get
            {
                return Position > 0 ? (Change * Position).ToString("F2") : "";
            }
        }
    }
}
