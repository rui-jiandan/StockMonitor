using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
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

    private void Menu_PositionEdit(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<PositionEditDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void Menu_AlertManage(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<AlertManageDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void Menu_ConfigEdit(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<ConfigEditDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void Menu_Refresh(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshCommand.Execute(null);
    }

    private void Menu_Exit(object sender, RoutedEventArgs e)
    {
        Hide();
        _viewModel.Dispose();
        NotifyIcon.Dispose();
        Application.Current.Shutdown();
    }
}
