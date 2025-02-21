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
using StockMonitor.Model;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace StockMonitor
{
    public partial class Form1 : Form
    {
        private List<StockInfo> stockInfoList = new List<StockInfo>();
        private string configFilePath;
        private string stocksFilePath;
        private FileSystemWatcher stocksWatcher;
        private List<string> currentStockCodes = new List<string>();
        private IScraper scraper;
        private Config config;
        private bool isRunning = true;

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

            // 启动单线程定时任务
            StartSingleThreadedTimer();

            stocksWatcher = new FileSystemWatcher();
            stocksWatcher.Path = appPath;
            stocksWatcher.Filter = Path.GetFileName(stocksFilePath);
            stocksWatcher.NotifyFilter = NotifyFilters.LastWrite;
            stocksWatcher.Changed += StocksWatcher_Changed;
            stocksWatcher.EnableRaisingEvents = true;
            Logger.LogDebug("启动");
        }

        private async void StartSingleThreadedTimer()
        {
            Logger.LogDebug("开始抓取");
            while (isRunning)
            {
                try
                {
                    await Timer_Tick();
                }
                catch (Exception ex)
                {
                    Logger.LogError("抓取出错", ex);
                    scraper?.ReloadURL(currentStockCodes);
                    Logger.LogDebug("重新加载页面");
                }
                finally
                {
                    await Task.Delay(config.RefreshInterval);
                }
            }
        }

        private async Task Timer_Tick()
        {
            stockInfoList.Clear();
            var newstockInfoList = await scraper.ScrapeAllAsync();
            stockInfoList.AddRange(newstockInfoList);
            // 刷新面板
            panel1.Invalidate();
        }

        private async void InitializeWebDrivers()
        {
            if (string.IsNullOrEmpty(config.StockBaseURL)) return;
            if (!scraper.Initialize)
            {
                await scraper.InitializeBrowser();
                Logger.LogDebug("抓取成功");
            }
            var oldCodeDrivers = scraper.GetCurrentCodes();
            foreach (var code in oldCodeDrivers)
            {
                if (!currentStockCodes.Contains(code))
                {
                    scraper.RemoveUrl(code);
                }
            }

            foreach (var code in currentStockCodes)
            {
                string url = string.Format(config.StockBaseURL, code);
                scraper.LoadUrl(code, url);
            }
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
                            string stockInfo = string.Format(config.LableFormat, info.Name, info.Price, info.Change, info.ChangeRate);

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
                config = JsonConvert.DeserializeObject<Config>(json);
                if (string.IsNullOrEmpty(config.ChromiumPath))
                    config.ChromiumPath = Path.Combine(Application.StartupPath, "chromium", "chrome.exe");

                scraper = new PuppeteerSharpScrape(config, Path.Combine(Application.StartupPath, "chromium", "chrome.exe"));
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
            Logger.LogDebug("配置刷新");
            LoadStocks();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.LogDebug("关闭");
            // 关闭所有WebDriver实例
            isRunning = false;
            scraper.ReleaseBrowser();
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