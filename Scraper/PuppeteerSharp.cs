using StockMonitor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;
using System.Drawing.Printing;
using HtmlAgilityPack;
using System.Drawing;

namespace StockMonitor
{
    public class PuppeteerSharpScrape : IScraper
    {
        private Dictionary<string,IPage> keyValuePairs = new Dictionary<string,IPage>();
        private IBrowser browser;
        private Config config;
        private bool isInitialize = false;
        private string Path;
        private List<string> errorcode=new List<string>();

        public PuppeteerSharpScrape(Config config,string path)
        {
            this.config = config;
            Path = path;
        }

        public bool Initialize => isInitialize;

        public IEnumerable<string> GetCurrentCodes()
        {
           return keyValuePairs.Keys;
        }

        public async Task  InitializeBrowser()
        {
            browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless=true,
                ExecutablePath = Path
            });
            isInitialize = true;
        }

        public async Task LoadUrl(string code, string url)
        {
            if (!isInitialize) return;
            if (!keyValuePairs.ContainsKey(code))
            {
                var page = await browser.NewPageAsync();
                await page.GoToAsync(url);
                keyValuePairs[code] = page;
            }
            else
            {
                await keyValuePairs[code].ReloadAsync();
            }
        }

        public async Task ReleaseBrowser()
        {
            isInitialize = false;
            await browser?.CloseAsync();
        }

        public async Task RemoveUrl(string code)
        {
            if (keyValuePairs.ContainsKey(code))
            {
                keyValuePairs.Remove(code);
            }
        }

        public async Task<IEnumerable<StockInfo>> ScrapeAllAsync()
        {
            List<StockInfo> stocks = new List<StockInfo>();
            foreach (var item in keyValuePairs)
            {
                var r= await ScrapeAsync(item.Key);
                if (r != null)
                    stocks.Add(r);
            }
            return stocks;
        }

        public async Task<StockInfo> ScrapeAsync(string code)
        {
            if (!keyValuePairs.ContainsKey(code))
            {
                return null;
            }
            try
            {
                var html = await keyValuePairs[code].GetContentAsync();
                var r = GetStockInfo(html, code);
                return r;
            }
            catch (Exception ex)
            {
                if(!errorcode.Contains(code)) errorcode.Add(code);
                Logger.LogError($"{code}读取数据出错", ex);
                return null;
            }
        }

        public async Task ReloadURL(IEnumerable<string> codes)
        {
            if (codes != null && codes.Any())
            {
                foreach (var code in codes)
                {
                    if (keyValuePairs.ContainsKey(code))
                    {
                        //刷新网页
                        var page=keyValuePairs[code];
                        await page?.ReloadAsync();
                        Logger.LogDebug($"{code}重新加载");
                    }
                }
            }
        }


        private StockInfo GetStockInfo(string html,string code)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            // 提取股票名称
            var nameNode = document.DocumentNode.SelectSingleNode(config.ParsingRules.Name);
            string stockName = nameNode?.InnerText ?? "错误代码";

            // 提取实时价格
            var priceNode = document.DocumentNode.SelectSingleNode(config.ParsingRules.Price);
            string stockPrice = priceNode?.InnerText ?? "N/A";

            // 提取涨跌值
            var changeNode = document.DocumentNode.SelectSingleNode(config.ParsingRules.Change);
            string stockChange = changeNode?.InnerText ?? "N/A";

            // 提取百分比
            var rateNode = document.DocumentNode.SelectSingleNode(config.ParsingRules.ChangeRate);
            string changeRate = rateNode?.InnerText ?? "N/A";

            return new StockInfo(code,stockName, stockPrice, stockChange, changeRate);
        }

        public async Task ReloadErrorCode()
        {
            await ReloadURL(errorcode);
            
            errorcode =new List<string>();
        }
    }
}
