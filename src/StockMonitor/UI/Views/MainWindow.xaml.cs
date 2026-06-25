using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using StockMonitor.Logging;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

/// <summary>
/// 主窗口，显示行情摘要和股票列表，支持拖拽和托盘图标操作
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isFirstShow = true;
    private bool _hasMeasuredItemHeight;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.ToggleVisibilityRequested += OnToggleVisibility;
        _viewModel.StocksUpdated += OnStocksUpdated;
        Closed += (s, e) => _viewModel.StocksUpdated -= OnStocksUpdated;
        _viewModel.StartRefreshLoop();
    }

    /// <summary>
    /// 窗口加载完成后，定位到右下角
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindowBottomRight();
    }

    /// <summary>
    /// ViewModel 请求切换窗口显示/隐藏
    /// </summary>
    private void OnToggleVisibility()
    {
        ToggleVisibility();
    }

    /// <summary>
    /// 将窗口定位到主屏幕右下角，紧贴任务栏上方和屏幕右侧
    /// </summary>
    private void PositionWindowBottomRight()
    {
        var source = PresentationSource.FromVisual(this);
        var dpiScale = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var screen = SystemParameters.WorkArea;

        var workAreaRight = screen.Right * dpiScale.M11;
        var workAreaBottom = screen.Bottom * dpiScale.M22;

        Left = (workAreaRight - ActualWidth) / dpiScale.M11;
        Top = (workAreaBottom - ActualHeight) / dpiScale.M22;
    }

    /// <summary>
    /// 切换窗口显示/隐藏状态，仅首次显示时定位到右下角
    /// </summary>
    private void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
        }
        else
        {
            if (_isFirstShow)
            {
                PositionWindowBottomRight();
                _isFirstShow = false;
            }
            Show();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    /// <summary>
    /// 窗口大小变化时重新定位到右下角，确保窗口始终锚定在屏幕底部
    /// </summary>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        PositionWindowBottomRight();
    }

    /// <summary>
    /// 股票数据更新后，延迟测量首项高度并动态设置 MaxHeight
    /// </summary>
    private void OnStocksUpdated()
    {
        if (_hasMeasuredItemHeight) return;

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            if (StockItemsControl.Items.Count == 0) return;

            var container = StockItemsControl.ItemContainerGenerator.ContainerFromIndex(0);
            if (container is not FrameworkElement element) return;

            double itemHeight = element.ActualHeight + element.Margin.Top + element.Margin.Bottom;
            if (itemHeight <= 0) return;

            double summaryHeight = SummaryText.ActualHeight + SummaryText.Margin.Top + SummaryText.Margin.Bottom;

            double borderPadding = 16;
            double maxContentHeight = summaryHeight + itemHeight * _viewModel.MaxVisibleStocks;
            double calculatedMaxHeight = maxContentHeight + borderPadding;

            var source = PresentationSource.FromVisual(this);
            var dpiScale = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;
            double screenMaxHeight = SystemParameters.WorkArea.Height * 0.85 / dpiScale;

            MaxHeight = Math.Min(calculatedMaxHeight, screenMaxHeight);
            PositionWindowBottomRight();

            _hasMeasuredItemHeight = true;
        });
    }

    /// <summary>
    /// 主面板预览鼠标按下事件（隧道事件），双击切换显示/隐藏
    /// 使用 PreviewMouseDown 而非 MouseDown，确保子元素不会吞掉事件
    /// </summary>
    private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleVisibility();
            e.Handled = true;
        }
    }

    private void Menu_Show(object sender, RoutedEventArgs e)
    {
        if (_isFirstShow)
        {
            PositionWindowBottomRight();
            _isFirstShow = false;
        }
        Show();
    }

    /// <summary>
    /// 打开对话框的通用方法。
    /// 定位策略：对话框始终显示在屏幕右下角。
    ///   只有当对话框的位置会与主窗口重叠时，才把对话框偏移到主窗口左侧，避免遮挡。
    ///   不跟随主窗口移动（主窗口被用户拖到其他地方时，对话框依旧在右下角）。
    /// </summary>
    private void ShowDialog<T>() where T : Window
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<T>();

        // 先调用 Measure 确保能获取对话框的实际尺寸（否则 ActualWidth/ActualHeight 可能是 0）
        dialog.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        dialog.Arrange(new Rect(0, 0, dialog.DesiredSize.Width, dialog.DesiredSize.Height));

        double dlgWidth = dialog.Width > 0 ? dialog.Width : dialog.DesiredSize.Width;
        double dlgHeight = dialog.Height > 0 ? dialog.Height : dialog.DesiredSize.Height;

        // 获取工作区（排除任务栏）
        var screen = SystemParameters.WorkArea;
        const int margin = 10;              // 对话框与屏幕边界的间距

        // 默认目标：右下角
        double targetLeft = screen.Right - dlgWidth - margin;
        double targetTop = screen.Bottom - dlgHeight - margin;

        // 如果主窗口可见，检查是否会与主窗口重叠；若重叠则移到主窗口左侧
        if (Visibility == Visibility.Visible && this.IsLoaded)
        {
            Rect dlgRect = new Rect(targetLeft, targetTop, dlgWidth, dlgHeight);
            Rect mainRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            dlgRect.Inflate(-1, -1);            // 留 1 像素容差，避免紧贴也算重叠
            mainRect.Inflate(-1, -1);

            if (dlgRect.IntersectsWith(mainRect))
            {
                // 重叠了：把对话框移到主窗口左侧，顶部与主窗口对齐
                double leftOfMain = Left - dlgWidth - margin;
                if (leftOfMain >= screen.Left + margin)
                {
                    // 左侧有空间
                    targetLeft = leftOfMain;
                    targetTop = Top;
                }
                else
                {
                    // 左侧也不够，改放在主窗口上方
                    double aboveMain = Top - dlgHeight - margin;
                    if (aboveMain >= screen.Top + margin)
                    {
                        targetLeft = Left;
                        targetTop = aboveMain;
                    }
                    else
                    {
                        // 上方也不行，改用主窗口右侧（用户手动把主窗口挪到了右下角）
                        targetLeft = Left + ActualWidth + margin;
                        targetTop = Top;
                    }
                }
            }
        }

        // 最终兜底：确保对话框不会跑出工作区
        if (targetLeft < screen.Left) targetLeft = screen.Left + margin;
        if (targetTop < screen.Top) targetTop = screen.Top + margin;
        if (targetLeft + dlgWidth > screen.Right) targetLeft = screen.Right - dlgWidth - margin;
        if (targetTop + dlgHeight > screen.Bottom) targetTop = screen.Bottom - dlgHeight - margin;

        dialog.Left = targetLeft;
        dialog.Top = targetTop;
        dialog.WindowStartupLocation = WindowStartupLocation.Manual;
        dialog.ShowInTaskbar = true;
        dialog.ShowDialog();
    }

    private void Menu_PositionEdit(object sender, RoutedEventArgs e)
    {
        ShowDialog<PositionEditDialog>();
    }

    private void Menu_AlertManage(object sender, RoutedEventArgs e)
    {
        ShowDialog<AlertManageDialog>();
    }

    private void Menu_ConfigEdit(object sender, RoutedEventArgs e)
    {
        ShowDialog<ConfigEditDialog>();
    }

    private void Menu_Refresh(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshCommand.Execute(null);
    }

    private void Menu_Exit(object sender, RoutedEventArgs e)
    {
        try
        {
            Hide();
            _viewModel.Dispose();
            NotifyIcon.Dispose();
        }
        catch (Exception ex)
        {
            FileLogger.LogError("退出时清理资源失败", ex);
        }
        Application.Current.Shutdown();
    }
}
