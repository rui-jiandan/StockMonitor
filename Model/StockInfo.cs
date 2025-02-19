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
        public StockInfo(string name,string price,string change,string rate) 
        {
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
        /// 股票名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 股票价格
        /// </summary>
        public string Price { get; set; }
        /// <summary>
        /// 股票变化价格
        /// </summary>
        public string Change { get; set; }
        /// <summary>
        /// 股票变化率
        /// </summary>
        public string ChangeRate { get; set; }
        /// <summary>
        /// 股票颜色
        /// </summary>
        public Color Color { get; set; }
    }
}
