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
        /// 股票代码全称
        /// </summary>
        [Description("fullcode")]
        public string FullCode { get; set; }        

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
                if (Price == 0) return 0;
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
        [Description("color")]
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
        public decimal NowMakeMoney
        {
            get
            {
                return (Position > 0 ? Change * Position : 0) + NowTMoney;
            }
        }

        /// <summary>
        /// 当日T金额
        /// </summary>
        [Description("tmoney")]
        public decimal NowTMoney { get; set; }

        /// <summary>
        /// 是否持仓
        /// </summary>
        [Description("havepos")]
        public bool HavePosition => Position > 0 || NowTMoney != 0;

        /// <summary>
        /// 持仓金额
        /// </summary>
        [Description("posmoney")]
        public decimal PositionMoney=> Position * Price;

        /// <summary>
        /// 持仓成本金额
        /// </summary>
        [Description("poscostmoney")]
        public decimal PositionCost=> Position > 0 ? Cost * Position : 0;
    }
}
