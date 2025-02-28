using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMonitor.Model
{
    public class StockInfo
    {
        public StockInfo(string code, string name,string price,string change,string rate) 
        {
            Code = code;
            Name = name;
            Price = price;
            Change = change;
            ChangeRate = rate;
            Color = Color.White;
            if (decimal.TryParse(change, out var pricechange))
            {
                if (pricechange > 0)
                {
                    Color = Color.Red;
                }
                else if (pricechange < 0)
                {
                    Color = Color.Green;
                }
            }
        }

        /// <summary>
        /// 股票代码
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 股票名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 股票价格
        /// </summary>
        public string Price { get; }
        /// <summary>
        /// 股票变化价格
        /// </summary>
        public string Change { get; }
        /// <summary>
        /// 股票变化率
        /// </summary>
        public string ChangeRate { get;  }
        /// <summary>
        /// 股票颜色
        /// </summary>
        public Color Color { get;}
    }
}
