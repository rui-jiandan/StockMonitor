using StockMonitor.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StockMonitor
{
    public interface IScraper
    {
        /// <summary>
        /// 是否初始化完成
        /// </summary>
        bool Initialize { get;}
        /// <summary>
        /// 初始化浏览器
        /// </summary>
        Task InitializeBrowser();
        /// <summary>
        /// 加载网页
        /// </summary>
        /// <param name="code"></param>
        /// <param name="url"></param>
        Task LoadUrl(string code, string url);
        /// <summary>
        /// 移除网页
        /// </summary>
        /// <param name="code"></param>
        Task RemoveUrl(string code);
        /// <summary>
        /// 释放浏览器资源
        /// </summary>
        Task ReleaseBrowser();
        /// <summary>
        /// 获取指定股票信息
        /// </summary>
        /// <param name="code"></param>
        /// <param name="parsingRules"></param>
        /// <returns></returns>
        Task<StockInfo> ScrapeAsync(string code);
        /// <summary>
        /// 获取所有股票信息
        /// </summary>
        /// <param name="parsingRules"></param>
        /// <returns></returns>
        Task<IEnumerable<StockInfo>> ScrapeAllAsync();
        /// <summary>
        /// 获取所有股票代码
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetCurrentCodes();
    }
}
