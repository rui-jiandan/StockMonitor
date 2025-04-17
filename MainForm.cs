
using StockMonitor.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StockMonitor
{
    public partial class MainForm : Form
    {
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuStrip;
        private List<StockConfig> stocks = new List<StockConfig>();
        private const string dataFilePath = "stocks.json";
        private const string configFilePath = "config.json";
        private HttpClient httpClient = new HttpClient();
        private TransparentRichTextBox stockInfoTextBox;
        private List<StockView> stockViews = new List<StockView>();
        private PropertyInfo[] viewproperties = null;
        private ConfigV1 config = null;

        public MainForm()
        {
            InitializeComponent();
            this.Icon = new Icon("icon.ico");
            // 移除放大、缩小和关闭按钮，仅保留系统托盘菜单
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;

            // 添加双击事件
            this.MouseDoubleClick += MainForm_MouseDoubleClick;

            // 添加默认请求头
            httpClient.DefaultRequestHeaders.Add("Referer", "https://finance.sina.com.cn/");

            // 注册编码提供程序
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            InitializeNotifyIcon();
            LoadStocksFromFile();
            LoadConfig();
            StartDataRefresh();
            SetupForm();
            // 设置窗体样式以支持透明度
            this.TransparencyKey = Color.LimeGreen;
            this.BackColor = Color.LimeGreen;
            this.Opacity = 0.5;
            // 设置主窗体置顶
            this.TopMost = true;
            viewproperties=typeof(StockView).GetProperties();
        }


        private void SetupForm()
        {
            this.Size = new Size(250, 130);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width,
                                      Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            stockInfoTextBox = new TransparentRichTextBox();
            stockInfoTextBox.Dock = DockStyle.Fill;
            stockInfoTextBox.ReadOnly = true;
            stockInfoTextBox.DoubleClick += (s, e) => this.Hide();
            this.Controls.Add(stockInfoTextBox);
            this.notifyIcon.Icon= new Icon("icon.ico");
        }

        private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            stockInfoTextBox.SelectionStart = e.NewValue;
            stockInfoTextBox.ScrollToCaret();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem editDeleteStockMenuItem = new ToolStripMenuItem("操作");
            editDeleteStockMenuItem.Click += EditDeleteStockMenuItem_Click;
            contextMenuStrip.Items.Add(editDeleteStockMenuItem);

            ToolStripMenuItem showFormMenuItem = new ToolStripMenuItem("显示");
            showFormMenuItem.Click += ShowFormMenuItem_Click;
            contextMenuStrip.Items.Add(showFormMenuItem);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenuStrip.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenuStrip;
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "监控";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            this.Hide();
        }

        private void EditDeleteStockMenuItem_Click(object sender, EventArgs e)
        {
            EditDeleteStockForm editDeleteStockForm = new EditDeleteStockForm(stocks);
            if (editDeleteStockForm.ShowDialog() == DialogResult.OK)
            {
                SaveStocksToFile();
                UpdateUI();
            }
        }

        private void ShowFormMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        private void LoadStocksFromFile()
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                stocks = JsonSerializer.Deserialize<List<StockConfig>>(json);
            }
        }

        private void SaveStocksToFile()
        {
            string json = JsonSerializer.Serialize(stocks);
            File.WriteAllText(dataFilePath, json);
        }

        private void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                config = JsonSerializer.Deserialize<ConfigV1>(json);
            }
        }

        private async void StartDataRefresh()
        {
            while (true)
            {
                await RefreshStockData();
                await Task.Delay(config.RefreshTime); // 根据配置文件设置刷新间隔
                if (DateTime.Now.Hour >= 15)
                {
                    break; 
                }
            }
        }

        private async Task RefreshStockData()
        {
            if (stocks.Count == 0) return;
            string codeList = string.Join(",", stocks.Select(s => "sh" + s.Code).Concat(stocks.Select(s => "sz" + s.Code)).Distinct());
            string url = $"https://hq.sinajs.cn/list={codeList}";
            try
            {
                var response = await httpClient.GetStreamAsync(url);
                using (var reader = new StreamReader(response, Encoding.GetEncoding("GB18030")))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    string[] lines = responseContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    stockViews.Clear(); // 清空之前的股票视图数据
                    foreach (string line in lines)
                    {
                        var view = GetStockView(line);
                        if (view != null)
                        {
                            stockViews.Add(view);
                        }
                    }
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message,ex);
            }
        }

        private void UpdateUI()
        {
            stockInfoTextBox.Clear();
            SetTodaySumStr();
            foreach (var stock in stockViews)
            {
                string info = GetStockShowStr(stock);
                stockInfoTextBox.SelectionColor = stock.Color;
                stockInfoTextBox.AppendText(info);
            }
        }

        private void SetTodaySumStr()
        {
            if (!string.IsNullOrEmpty(config?.ShowTodaySumFormat) && stockViews.Any(x => x.Position > 0))
            {
                var sum = stockViews.Sum(x => x.Change * x.Position+ x.NowTMoney);
                var sumcost = stockViews.Sum(x => x.Cost * x.Position);
                var rate = Math.Round((sum / sumcost) * 100, 2);
                var result = config.ShowTodaySumFormat;
                result = result
                    .Replace("#money", sum.ToString("F2"))
                    .Replace("#rate", rate.ToString());
                stockInfoTextBox.SelectionColor = sum > 0 ? Color.Red : sum < 0 ? Color.Green : Color.White;
                stockInfoTextBox.AppendText(result);
            }
        }

        private string GetStockShowStr(StockView stockView)
        {
            string result = config.ShowFormat;
            foreach (var property in viewproperties)
            {
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    string placeholder = $"#{descriptionAttribute.Description}";
                    var valuestr = property.GetValue(stockView)?.ToString();
                    if (valuestr != null)
                    {
                        result = result.Replace(placeholder, valuestr);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 解析数据获取股票视图
        /// </summary>
        /// <param name="apistr"></param>
        /// <returns></returns>
        private StockView GetStockView(string apistr)
        {
            /*var hq_str_sh600036="招商银行,41.680,41.510,41.680,41.910,41.520,41.680,41.690,9158766,382049591.000,23400,41.680,6000,41.670,18900,41.660,44500,41.650,10200,41.640,1800,41.690,58900,41.700,8100,41.710,235300,41.720,16000,41.740,2025-04-14,09:49:29,00,";
             * let open = params[1];
            let yestclose = params[2];
            let price = params[3];
            let high = params[4];
            let low = params[5];
             * */
            string[] parts = apistr.Split('=');
            var matchpattern = @"\d+";
            if (parts.Length == 2)
            {
                string fullcode = parts[0].Split("var hq_str_")[1];
                string data = parts[1].Trim('"');
                string[] dataParts = data.Split(',');
                if (dataParts.Length > 3)
                {
                    var rmatch = Regex.Match(fullcode, matchpattern);
                    var r = new StockView
                    {
                        FullCode= fullcode,
                        Code = rmatch.Success?rmatch.Value: fullcode,
                        Name = dataParts[0],
                        OpenPrice = decimal.Parse(dataParts[1]),
                        Price = decimal.Parse(dataParts[3]),
                        HighPrice = decimal.Parse(dataParts[4]),
                        LowPrice = decimal.Parse(dataParts[5]),
                        YestClose=decimal.Parse(dataParts[2]),
                    };
                    StockConfig stock = stocks.FirstOrDefault(s => s.IncreaseTime==0&&s.Code==r.Code);
                    if (stock != null)
                    {
                        r.Position = stock.Position;
                        r.Cost = stock.Cost;
                    }
                    SetToDayTMoney(r);
                    return r;
                }
            }
            return null;
        }

        private void SetToDayTMoney(StockView t)
        {
            if (t == null) return;
            var tlist = stocks.Where(x => x.Code == t.Code && x.IncreaseTime != 0);
            if (tlist.Any())
            {
                var price = t.Price;
                var Tmoney = 0M;
                foreach (var item in tlist)
                {
                    Tmoney+=item.IncreaseTime>0 ? item.Position * (price - item.Cost) : item.Position * (item.Cost - price);
                }
                t.NowTMoney = Tmoney;
            }
        }

        #region 窗体事件
        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Visible)
            {
                this.Hide(); // 隐藏窗体
            }
            else
            {
                this.Show(); // 显示窗体
            }
        }

        #endregion
    }

}