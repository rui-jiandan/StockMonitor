# Stock Monitor

Stock Monitor 是一个用于监控股票信息的桌面应用程序。从网页中提取股票信息，并在界面上显示。

## 特性

- 实时更新股票信息
- 可配置的抓取规则和刷新间隔
- 支持系统托盘图标


## 配置

在运行应用程序之前，请确保配置文件 `config.json` 已正确设置。以下是 `config.json` 的示例：
```json
{
	"StockBaseURL": "https://quote.eastmoney.com/{0}.html",
	"RefreshInterval": 1000,
	"LableFormat": "{0} {1} {2} {3}\n",
	"ParsingRules": {
		"Name": "//span[contains(@class,'quote_title_name')]",
		"Price": "//div[@class='zxj']",
		"Change": "//div[@class='zd']/span[1]",
		"ChangeRate": "//div[@class='zd']/span[2]"
	},
	"ChromiumPath": ""
}
```

- `StockBaseURL`：股票信息的基础 URL，使用 `{0}` 作为股票代码的占位符。
- `RefreshInterval`：刷新间隔（毫秒）。
- `LableFormat`：显示股票信息的格式。
- `ParsingRules`：抓取股票信息的 XPath 规则。
- `ChromiumPath`：Chromium 浏览器的路径（用于 PuppeteerSharp）。, 如果为空则使用默认路径。

## 使用

1. 运行应用程序：
    
    