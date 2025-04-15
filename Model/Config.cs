using System.Collections.Generic;

namespace StockMonitor.Model
{
    public class Config
    {
        /// <summary>
        /// 浏览器地址
        /// </summary>
        public string StockBaseURL { get; set; }
        /// <summary>
        /// 刷新时间
        /// </summary>
        public int RefreshInterval { get; set; }
        /// <summary>
        /// 展示结果的格式
        /// </summary>
        public string LableFormat { get; set; }
        /// <summary>
        /// 解析规则
        /// </summary>
        public ParsingRules ParsingRules { get; set; }
        /// <summary>
        /// 浏览器路径
        /// </summary>
        public string ChromiumPath { get; set; }
    }

    public class ParsingRules
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        public string Price { get; set; }
        /// <summary>
        /// 涨跌
        /// </summary>
        public string Change { get; set; }
        /// <summary>
        /// 涨跌幅
        /// </summary>
        public string ChangeRate { get; set; }
    }


    public class ConfigV1
    {
        /// <summary>
        /// 股票显示格式信息
        /// </summary>
        public string ShowFormat { get; set; }

        /// <summary>
        /// 数据刷新时间
        /// </summary>
        public int RefreshTime { get; set; }

        /// <summary>
        /// 今日汇总显示格式信息
        /// </summary>
        public string ShowTodaySumFormat { get; set; }
    }
}
