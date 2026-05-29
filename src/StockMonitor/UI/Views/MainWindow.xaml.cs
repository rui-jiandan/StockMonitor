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

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.ToggleVisibilityRequested += OnToggleVisibility;
        _viewModel.StartRefreshLoop();
    }

    /// <summary>
    /// 窗口加载完成后定位到右下角（此时 SizeToContent 已计算出 ActualHeight）
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
    /// 切换窗口显示/隐藏状态
    /// </summary>
    private void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
        }
        else
        {
            Show();
            PositionWindowBottomRight();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
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
        Show();
        PositionWindowBottomRight();
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
