using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace StockMonitor.UI.Views;

/// <summary>
/// 预警通知弹窗，从屏幕右下角滑入显示，自动消失
/// </summary>
public partial class AlertNotificationWindow : Window
{
    private static readonly List<AlertNotificationWindow> _activeWindows = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 自动关闭时间（毫秒）
    /// </summary>
    private const int AutoCloseMs = 8000;

    /// <summary>
    /// 弹窗之间的垂直间距
    /// </summary>
    private const double VerticalGap = 10;

    private System.Threading.Timer? _autoCloseTimer;

    public AlertNotificationWindow(string stockInfo, string alertDetail)
    {
        InitializeComponent();

        // 添加阴影效果
        var innerBorder = (Border)RootBorder.Child;
        innerBorder.Effect = new DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 16,
            ShadowDepth = 4,
            Opacity = 0.15
        };

        StockInfoText.Text = stockInfo;
        AlertDetailText.Text = alertDetail;
        TimeText.Text = DateTime.Now.ToString("HH:mm:ss");

        // 鼠标悬停时暂停自动关闭
        MouseEnter += (s, e) => _autoCloseTimer?.Dispose();
        MouseLeave += (s, e) => StartAutoCloseTimer();
    }

    /// <summary>
    /// 显示预警通知弹窗，自动定位到屏幕右下角
    /// </summary>
    /// <param name="stockInfo">股票信息文本（如：招商银行(sh600036)）</param>
    /// <param name="alertDetail">预警详情文本</param>
    public static void ShowNotification(string stockInfo, string alertDetail)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new AlertNotificationWindow(stockInfo, alertDetail);
            lock (_lock)
            {
                _activeWindows.Add(window);
            }
            window.Show();
            PositionWindow(window);
        });
    }

    /// <summary>
    /// 将弹窗定位到屏幕右下角，多个弹窗时依次向上堆叠
    /// </summary>
    private static void PositionWindow(AlertNotificationWindow window)
    {
        window.UpdateLayout();

        var source = PresentationSource.FromVisual(window);
        var dpiScale = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

        var workArea = SystemParameters.WorkArea;
        double workAreaRight = workArea.Right * dpiScale.M11;
        double workAreaBottom = workArea.Bottom * dpiScale.M22;

        // 计算当前弹窗应放置的垂直位置（从底部向上堆叠）
        double offsetY = 0;
        lock (_lock)
        {
            int index = _activeWindows.IndexOf(window);
            for (int i = _activeWindows.Count - 1; i > index; i--)
            {
                var w = _activeWindows[i];
                offsetY += w.ActualHeight + VerticalGap;
            }
        }

        window.Left = (workAreaRight - window.ActualWidth - 16) / dpiScale.M11;
        window.Top = (workAreaBottom - window.ActualHeight - 16 - offsetY) / dpiScale.M22;
    }

    /// <summary>
    /// 重新计算所有活跃弹窗的位置
    /// </summary>
    private static void RepositionAll()
    {
        lock (_lock)
        {
            foreach (var w in _activeWindows)
            {
                PositionWindow(w);
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 播放滑入动画
        var storyboard = (Storyboard)FindResource("SlideInAnimation");
        storyboard = storyboard.Clone();
        storyboard.Begin(RootBorder);

        StartAutoCloseTimer();
    }

    /// <summary>
    /// 启动自动关闭计时器
    /// </summary>
    private void StartAutoCloseTimer()
    {
        _autoCloseTimer?.Dispose();
        _autoCloseTimer = new System.Threading.Timer(_ =>
        {
            Application.Current.Dispatcher.Invoke(() => CloseWithAnimation());
        }, null, AutoCloseMs, System.Threading.Timeout.Infinite);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer?.Dispose();
        CloseWithAnimation();
    }

    /// <summary>
    /// 播放滑出动画后关闭窗口
    /// </summary>
    private void CloseWithAnimation()
    {
        try
        {
            var storyboard = (Storyboard)FindResource("SlideOutAnimation");
            storyboard = storyboard.Clone();
            storyboard.Completed += (s, e) =>
            {
                RemoveFromActive();
                Close();
            };
            storyboard.Begin(RootBorder);
        }
        catch
        {
            RemoveFromActive();
            Close();
        }
    }

    private void RemoveFromActive()
    {
        lock (_lock)
        {
            _activeWindows.Remove(this);
        }
        Application.Current.Dispatcher.BeginInvoke(() => RepositionAll());
    }
}
