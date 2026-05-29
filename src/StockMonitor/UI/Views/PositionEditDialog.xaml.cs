using System.Windows;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

/// <summary>
/// 持仓编辑对话框占位实现，后续任务将完善
/// </summary>
public partial class PositionEditDialog : Window
{
    public PositionEditDialog(PositionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
