
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
        private Dictionary<string, PropertyInfo> viewpropertiesdic = null;
        private ConfigV1 config = null;

        public MainForm()
        {
            InitializeComponent();
            InitViewProperties();
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
        }

        private void InitViewProperties()
        {
            viewproperties = typeof(StockView).GetProperties();
            viewpropertiesdic = new Dictionary<string, PropertyInfo>();
            foreach (var property in viewproperties)
            {
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    viewpropertiesdic[descriptionAttribute.Description] = property;
                }
            }
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

            ToolStripMenuItem editConfigMenuItem = new ToolStripMenuItem("编辑配置");
            editConfigMenuItem.Click += EditConfigMenuItem_Click;
            contextMenuStrip.Items.Add(editConfigMenuItem);

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

        private void EditConfigMenuItem_Click(object sender, EventArgs e)
        {
            EditConfigForm editConfigForm = new EditConfigForm(config);
            if (editConfigForm.ShowDialog() == DialogResult.OK)
            {
                SaveConfigToFile();
                UpdateUI();
            }
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
            System.Windows.Forms.Application.Exit();
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
                UpdateStocksCost(stocks);
            }
        }

        /// <summary>
        /// 更新股票成本
        /// </summary>
        private void UpdateStocksCost(List<StockConfig>  s)
        {
            var time = long.Parse(DateTime.Now.ToString("yyyyMMdd"));
            if (s.Any(x => x.IncreaseTime != 0&& time>Math.Abs(x.IncreaseTime)))
            {
                var codes = s.Where(x => x.IncreaseTime != 0).Select(x => x.Code).Distinct();
                var newsotcks = new List<StockConfig>();
                foreach (var item in codes)
                {
                    var codelist = stocks.Where(x => x.Code == item).ToList();
                    if (codelist.Any())
                    {
                        var costmoney = 0M;
                        var position = 0;
                        foreach (var r in codelist)
                        {
                            if (r.IncreaseTime >= 0)
                            {
                                costmoney += r.Cost * r.Position;
                                position += r.Position;
                            }
                            else
                            {
                                costmoney -= r.Cost * r.Position;
                                position -= r.Position;
                            }
                        }
                        newsotcks.Add(new StockConfig
                        {
                            Code = item,
                            Cost = costmoney / position,
                            Position = position,
                            IncreaseTime = 0
                        });
                    }
                }
                stocks.RemoveAll(x => codes.Contains(x.Code));
                stocks.AddRange(newsotcks);
                SaveStocksToFile();
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
            else
            {
                // 初始化默认配置
                config = new ConfigV1
                {
                    ShowFormat = "#name #price #change #rate% #makemoney \r\n",
                    RefreshTime = 2000,
                    ShowTodaySumFormat = "今日盈亏:#money, 今日比例 #rate% \r\n",
                    OrderBy = "name asc"
                };
                SaveConfigToFile();
            }
        }

        private void SaveConfigToFile()
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, json);
        }



        private async void StartDataRefresh()
        {
            while (true)
            {
                await RefreshStockData();
                await Task.Delay(config.RefreshTime); // 根据配置文件设置刷新间隔
                if (IsStop())
                {
                    break; 
                }
            }
        }

        private bool IsStop()
        {
            var now = DateTime.Now;
            return now > new DateTime(now.Year, now.Month, now.Day, 15, 5, 0);
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
            // 记录当前滚动条位置
            int scrollPosition = stockInfoTextBox.GetScrollPosition();
            stockInfoTextBox.Clear();
            StockViewSort();
            foreach (var r in stockViews)
            {
                SetToDayTMoney(r);
                string info = GetStockShowStr(r);
                stockInfoTextBox.SelectionColor = r.Color;
                stockInfoTextBox.AppendText(info);
            }
            SetTodaySumStr();
            // 恢复滚动条位置
            stockInfoTextBox.SetScrollPosition(scrollPosition);
        }

        private void StockViewSort()
        {
            if (string.IsNullOrEmpty(config?.OrderBy?.Trim())) return;
            // 解析 orderbystr，例如 "name desc,code asc"
            var orderClauses = config.OrderBy.Split(',')
                .Select(clause => clause.Trim().Split(' '))
                .Where(parts => parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                .Select(parts => new { Field = parts[0], IsDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase) })
                .ToList();

            if (orderClauses.Count == 0) return;
            // 动态排序
            IOrderedEnumerable<StockView> sortedStockViews = null;

            foreach (var clause in orderClauses)
            {
                if (!viewpropertiesdic.TryGetValue(clause.Field, out var property))
                {
                    continue;
                }
                // 排序逻辑
                if (sortedStockViews == null)
                {
                    sortedStockViews = clause.IsDescending
                        ? stockViews.OrderByDescending(x => property.GetValue(x))
                        : stockViews.OrderBy(x => property.GetValue(x));
                }
                else
                {
                    sortedStockViews = clause.IsDescending
                        ? sortedStockViews.ThenByDescending(x => property.GetValue(x))
                        : sortedStockViews.ThenBy(x => property.GetValue(x));
                }
            }

            // 更新排序后的列表
            if (sortedStockViews != null)
            {
                stockViews = sortedStockViews.ToList();
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
                // 设置选择起始位置为 0，即文本开头
                stockInfoTextBox.SelectionStart = 0;
                stockInfoTextBox.SelectionColor = sum > 0 ? Color.Red : sum < 0 ? Color.Green : Color.White;
                // 将文本插入到当前选择位置，也就是开头
                stockInfoTextBox.SelectedText = result;
                //stockInfoTextBox.AppendText(result);
            }
        }

        private string GetStockShowStr(StockView stockView)
        {
            string result = config.ShowFormat;
            foreach (var item in viewpropertiesdic)
            {
                string placeholder = $"#{item.Key}";
                var valuestr = item.Value.GetValue(stockView)?.ToString();
                if (valuestr != null)
                {
                    if (item.Key == "makemoney") valuestr = FormatDecimalStr(valuestr);
                    result = result.Replace(placeholder, valuestr);
                }
            }
            return result;
        }

        /// <summary>
        /// 格式化数字字符串
        /// </summary>
        /// <param name="valuestr">原字符串</param>
        /// <param name="emptystr">当做空字符串处理</param>
        /// <returns></returns>
        private string FormatDecimalStr(string valuestr,string emptystr= "0")
        {
            if (string.IsNullOrEmpty(valuestr) || emptystr == valuestr) return string.Empty;
            if(decimal.TryParse(valuestr, out decimal value))
            {
               return value.ToString("F2");
            }
            return valuestr;
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
                var addcostmoney = 0M;
                var reducecostmoney = 0M;
                var addposition = 0;
                var reduceposition = 0;
                var price = t.Price;
                var Tmoney = 0M;
                foreach (var item in tlist)
                {
                    if (item.IncreaseTime>0)
                    {
                        addcostmoney += (item.Position * item.Cost);
                        addposition += item.Position;
                    }
                    else
                    {
                        reducecostmoney += (item.Position * item.Cost);
                        reduceposition += item.Position;
                    }
                }
                if (addposition == reduceposition)
                {
                    Tmoney = reducecostmoney - addcostmoney;
                }
                else if (addposition < reduceposition)
                {
                    //减仓
                    Tmoney = (price - addcostmoney) * Math.Abs(addposition);
                }
                else
                {
                    //加仓
                    //加仓的平均价
                    var cost= addcostmoney/addposition;
                    if (reduceposition>0)
                    {
                        Tmoney += reducecostmoney - cost * reduceposition;
                    }
                    Tmoney += (price - cost) * (addposition - reduceposition);
                    
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