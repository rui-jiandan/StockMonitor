using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace StockMonitor
{
    public partial class Form1 : Form
    {
        private Dictionary<string, IWebDriver> drivers = new Dictionary<string, IWebDriver>();
        private string stockBaseURL = string.Empty;
        private string lableFormat = string.Empty;
        private Dictionary<string, string> parsingRules = new Dictionary<string, string>();
        private List<StockInfo> stockInfoList = new List<StockInfo>();
        private Timer timer;
        private string configFilePath;
        private string stocksFilePath;
        private FileSystemWatcher configWatcher;
        private FileSystemWatcher stocksWatcher;
        private List<string> currentStockCodes = new List<string>();

        public Form1()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            InitializeMouse();

            // 设置窗体位置在屏幕右下角
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            // 获取程序启动路径
            string appPath = Application.StartupPath;

            // 设置配置文件路径
            configFilePath = Path.Combine(appPath, "config.json");
            stocksFilePath = Path.Combine(appPath, "stocks.json");

            // 加载配置文件
            LoadConfig();
            LoadStocks();

            // 创建定时器，每5秒更新一次
            timer = new Timer();
            timer.Interval = parsingRules.ContainsKey("RefreshInterval") ? int.Parse(parsingRules["RefreshInterval"]) : 5000;
            timer.Tick += Timer_Tick;
            timer.Start();

            // 监听配置文件的变化
            configWatcher = new FileSystemWatcher();
            configWatcher.Path = appPath;
            configWatcher.Filter = Path.GetFileName(configFilePath);
            configWatcher.NotifyFilter = NotifyFilters.LastWrite;
            configWatcher.Changed += ConfigWatcher_Changed;
            configWatcher.EnableRaisingEvents = true;

            stocksWatcher = new FileSystemWatcher();
            stocksWatcher.Path = appPath;
            stocksWatcher.Filter = Path.GetFileName(stocksFilePath);
            stocksWatcher.NotifyFilter = NotifyFilters.LastWrite;
            stocksWatcher.Changed += StocksWatcher_Changed;
            stocksWatcher.EnableRaisingEvents = true;
        }



        private void InitializeWebDrivers()
        {
            if (string.IsNullOrEmpty(stockBaseURL)) return;
            var oldCodeDrivers = drivers.Keys;
            foreach (var code in oldCodeDrivers)
            {
                if (!currentStockCodes.Contains(code))
                {
                    drivers[code].Quit();
                    drivers.Remove(code);
                }
            }

            // 设置 ChromeDriver 的路径
            string chromeDriverPath = Path.Combine(Application.StartupPath, "chrome", "chromedriver.exe");

            foreach (var code in currentStockCodes)
            {
                if (drivers.ContainsKey(code))
                {
                    continue;
                }
                string url = string.Format(stockBaseURL, code);
                var service = ChromeDriverService.CreateDefaultService(chromeDriverPath);
                service.HideCommandPromptWindow = true;
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--headless"); // 无头模式
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("blink-settings=imagesEnabled=false");
                options.AddArgument("--log-level=3"); // 设置日志级别为ERROR
                options.AddArgument("--silent"); // 静默模式，减少日志输出
                IWebDriver driver = new ChromeDriver(service, options);
                driver.Navigate().GoToUrl(url);

                drivers[code] = driver;
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            stockInfoList.Clear();

            foreach (var kvp in drivers)
            {
                string code = kvp.Key;
                IWebDriver driver = kvp.Value;

                try
                {
                    driver.Navigate().Refresh();

                    string pageSource = driver.PageSource;
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(pageSource);

                    // 提取股票名称
                    var nameNode = document.DocumentNode.SelectSingleNode(parsingRules["Name"]);
                    string stockName = nameNode?.InnerText ?? "错误代码";

                    // 提取实时价格
                    var priceNode = document.DocumentNode.SelectSingleNode(parsingRules["Price"]);
                    string stockPrice = priceNode?.InnerText ?? "N/A";

                    // 提取涨跌值
                    var changeNode = document.DocumentNode.SelectSingleNode(parsingRules["Change"]);
                    string stockChange = changeNode?.InnerText ?? "N/A";

                    // 提取百分比
                    var rateNode = document.DocumentNode.SelectSingleNode(parsingRules["ChangeRate"]);
                    string changeRate = rateNode?.InnerText ?? "N/A";

                    // 判断涨跌
                    Color textColor = Color.White;
                    if(decimal.TryParse(stockChange,out var pricechange))
                    {
                        if (pricechange > 0)
                        {
                            textColor = Color.Red;
                        }
                        else if(pricechange<0)
                        {
                            textColor = Color.Green;
                        }
                    }

                    // 添加到股票信息列表
                    stockInfoList.Add(new StockInfo
                    {
                        Name = stockName,
                        Price = stockPrice,
                        Change = stockChange,
                        Color = textColor,
                        ChangeRate = changeRate
                    });
                }
                catch (Exception ex)
                {
                    stockInfoList.Add(new StockInfo
                    {
                        Name = code,
                        Price = "N/A",
                        Change = ex.Message,
                        Color = Color.Red
                    });
                }
            }

            // 刷新面板
            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            using (Bitmap bitmap = new Bitmap(panel1.Width, panel1.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Black);

                    int y = 0;
                    foreach (var info in stockInfoList)
                    {
                        using (Font font = new Font("Arial", 10, FontStyle.Bold))
                        {
                            string stockInfo =string.Format(lableFormat, info.Name, info.Price,info.Change,info.ChangeRate);
                            
                            SizeF size = g.MeasureString(stockInfo, font);
                            g.DrawString(stockInfo, font, new SolidBrush(info.Color), 0, y);
                            y += (int)size.Height;
                        }
                    }
                }

                e.Graphics.DrawImage(bitmap, 0, 0);
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                dynamic config = JsonConvert.DeserializeObject(json);

                stockBaseURL = config.StockBaseURL;

                lableFormat = config.LableFormat;

                parsingRules = new Dictionary<string, string>
                {
                    { "Name", config.ParsingRules.Name.ToString() },
                    { "Price", config.ParsingRules.Price.ToString() },
                    { "Change", config.ParsingRules.Change.ToString() },
                    { "ChangeRate", config.ParsingRules.ChangeRate.ToString() },
                    { "RefreshInterval", config.RefreshInterval.ToString() }
                };
            }
        }

        private void LoadStocks()
        {
            if (File.Exists(stocksFilePath))
            {
                string json = File.ReadAllText(stocksFilePath);
                currentStockCodes = JsonConvert.DeserializeObject<List<string>>(json);
            }
            else
            {
                // 默认股票代码
                currentStockCodes = new List<string>();
                SaveStocks();
            }

            // 初始化WebDriver实例
            InitializeWebDrivers();
        }

        private void SaveConfig()
        {
            var config = new
            {
                StockBaseURL = stockBaseURL,
                ParsingRules = parsingRules,
                RefreshInterval = parsingRules["RefreshInterval"]
            };

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void SaveStocks()
        {
            File.WriteAllText(stocksFilePath, JsonConvert.SerializeObject(currentStockCodes, Formatting.Indented));
        }

        private void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            LoadConfig();
        }

        private void StocksWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            LoadStocks();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadConfig();
            LoadStocks();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭所有WebDriver实例
            foreach (var driver in drivers.Values)
            {
                driver.Quit();
                driver.Dispose();
            }
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private class StockInfo
        {
            public string Name { get; set; }
            public string Price { get; set; }
            public string Change { get; set; }

            public string ChangeRate { get; set; }
            public Color Color { get; set; }
        }

        private void InitializeNotifyIcon()
        {
            // 初始化 NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("icon.ico"); // 替换为你的图标文件路径
            notifyIcon.Text = "Stock Monitor";
            notifyIcon.Visible = true;

            // 处理 NotifyIcon 的双击事件
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        #region 窗体移动和调整大小
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;

        private bool isResizing = false;
        private Rectangle resizeBounds;
        private Cursor resizeCursor;
        private void InitializeMouse()
        {
            // 设置窗体样式以支持调整大小
            //this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;

            // 处理窗体移动事件
            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseUp += Form1_MouseUp;

            // 处理窗体调整大小事件
            //this.MouseDown += Form1_MouseDown_Resize;
            //this.MouseMove += Form1_MouseMove_Resize;
            //this.MouseUp += Form1_MouseUp_Resize;
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursor = Cursor.Position;
                lastForm = this.Location;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                this.Location = new Point(
                    lastForm.X + (Cursor.Position.X - lastCursor.X),
                    lastForm.Y + (Cursor.Position.Y - lastCursor.Y));
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }

        private void Form1_MouseDown_Resize(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int tolerance = 10;
                Rectangle left = new Rectangle(0, 0, tolerance, this.Height);
                Rectangle right = new Rectangle(this.Width - tolerance, 0, tolerance, this.Height);
                Rectangle top = new Rectangle(0, 0, this.Width, tolerance);
                Rectangle bottom = new Rectangle(0, this.Height - tolerance, this.Width, tolerance);

                if (left.Contains(e.Location))
                {
                    isResizing = true;
                    resizeBounds = left;
                    resizeCursor = Cursors.SizeWE;
                }
                else if (right.Contains(e.Location))
                {
                    isResizing = true;
                    resizeBounds = right;
                    resizeCursor = Cursors.SizeWE;
                }
                else if (top.Contains(e.Location))
                {
                    isResizing = true;
                    resizeBounds = top;
                    resizeCursor = Cursors.SizeNS;
                }
                else if (bottom.Contains(e.Location))
                {
                    isResizing = true;
                    resizeBounds = bottom;
                    resizeCursor = Cursors.SizeNS;
                }

                if (isResizing)
                {
                    this.Cursor = resizeCursor;
                    lastCursor = Cursor.Position;
                }
            }
        }

        private void Form1_MouseMove_Resize(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                int deltaX = Cursor.Position.X - lastCursor.X;
                int deltaY = Cursor.Position.Y - lastCursor.Y;

                if (resizeBounds == new Rectangle(0, 0, 10, this.Height)) // Left
                {
                    this.Width -= deltaX;
                    this.Left += deltaX;
                }
                else if (resizeBounds == new Rectangle(this.Width - 10, 0, 10, this.Height)) // Right
                {
                    this.Width += deltaX;
                }
                else if (resizeBounds == new Rectangle(0, 0, this.Width, 10)) // Top
                {
                    this.Height -= deltaY;
                    this.Top += deltaY;
                }
                else if (resizeBounds == new Rectangle(0, this.Height - 10, this.Width, 10)) // Bottom
                {
                    this.Height += deltaY;
                }

                lastCursor = Cursor.Position;
            }
        }

        private void Form1_MouseUp_Resize(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isResizing = false;
                this.Cursor = Cursors.Default;
            }
        }
        #endregion
    }
}