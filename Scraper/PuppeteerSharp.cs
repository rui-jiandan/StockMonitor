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
            //await new BrowserFetcher().DownloadAsync();
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
                await keyValuePairs[code].GoToAsync(url);
            }
        }

        public async Task ReleaseBrowser()
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
        }

        public async Task RemoveUrl(string code)
        {
            if (keyValuePairs.ContainsKey(code))
            {
                var page=keyValuePairs[code];
                await page.CloseAsync();
            }
        }

        public async Task<IEnumerable<StockInfo>> ScrapeAllAsync()
        {
            List<StockInfo> stocks = new List<StockInfo>();
            foreach (var item in keyValuePairs)
            {
                stocks.Add(await ScrapeAsync(item.Key));
            }
            return stocks;
        }

        public async Task<StockInfo> ScrapeAsync(string code)
        {
            if (!keyValuePairs.ContainsKey(code))
            {
                return null;
            }
            var html =await keyValuePairs[code].GetContentAsync();
           return GetStockInfo(html);

        }


        private StockInfo GetStockInfo(string html)
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

            return new StockInfo(stockName, stockPrice, stockChange, changeRate);
        }


    }
}
