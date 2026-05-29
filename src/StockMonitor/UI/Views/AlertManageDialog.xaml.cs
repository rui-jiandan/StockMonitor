using System.Windows;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

/// <summary>
/// 预警管理对话框占位实现，后续任务将完善
/// </summary>
public partial class AlertManageDialog : Window
{
    public AlertManageDialog(AlertManageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
