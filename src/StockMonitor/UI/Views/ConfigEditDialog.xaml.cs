using System.Windows;
using System.Windows.Input;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

public partial class ConfigEditDialog : Window
{
    private readonly ConfigEditViewModel _viewModel;

    public ConfigEditDialog(ConfigEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void ShowFormatBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.ActiveFormatField = "ShowFormat";
    }

    private void TodaySumFormatBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.ActiveFormatField = "ShowTodaySumFormat";
    }

    private void OrderByBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.ActiveFormatField = "OrderBy";
    }

    /// <summary>
    /// 占位符标签点击事件，在对应 TextBox 光标位置插入占位符文本
    /// </summary>
    private void Placeholder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not PlaceholderItem item) return;

        var targetBox = _viewModel.ActiveFormatField switch
        {
            "ShowTodaySumFormat" => TodaySumFormatBox,
            "OrderBy" => OrderByBox,
            _ => ShowFormatBox
        };

        var caretIndex = targetBox.CaretIndex;
        var text = targetBox.Text;
        targetBox.Text = text.Insert(caretIndex, item.Tag);
        targetBox.CaretIndex = caretIndex + item.Tag.Length;
        targetBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveCommand.Execute(null);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
