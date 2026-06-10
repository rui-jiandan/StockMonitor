using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Data;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 持仓编辑 ViewModel，管理股票的增删改操作
/// </summary>
public partial class PositionEditViewModel : ObservableObject
{
    private readonly IPositionService _positionService;

    public ObservableCollection<StockPosition> StockList { get; }

    [ObservableProperty] private StockPosition? _selectedStock;
    [ObservableProperty] private string _inputName = string.Empty;
    [ObservableProperty] private string _inputQuantity = string.Empty;
    [ObservableProperty] private string _inputPrice = string.Empty;
    [ObservableProperty] private string _newCode = string.Empty;

    public PositionEditViewModel(IPositionService positionService)
    {
        _positionService = positionService;
        StockList = new ObservableCollection<StockPosition>(positionService.GetAllPositions());
    }

    /// <summary>
    /// 选中股票变更时，自动填充数量和成本价到输入框
    /// </summary>
    partial void OnSelectedStockChanged(StockPosition? value)
    {
        if (value != null)
        {
            InputName = value.Name;
            InputQuantity = value.GetTotalQuantity().ToString();
            InputPrice = value.AvgCostPrice.ToString("F2");
        }
    }

    /// <summary>
    /// 加仓操作：以指定价格和数量买入
    /// </summary>
    [RelayCommand]
    private void AddPosition()
    {
        if (SelectedStock == null) return;
        if (!int.TryParse(InputQuantity, out var qty) || !decimal.TryParse(InputPrice, out var price)) return;
        _positionService.AddPosition(SelectedStock.Code, price, qty);
        RefreshList();
    }

    /// <summary>
    /// 减仓操作：以指定价格和数量卖出
    /// </summary>
    [RelayCommand]
    private void ReducePosition()
    {
        if (SelectedStock == null) return;
        if (!int.TryParse(InputQuantity, out var qty) || !decimal.TryParse(InputPrice, out var price)) return;
        try
        {
            _positionService.ReducePosition(SelectedStock.Code, price, qty);
            RefreshList();
        }
        catch (InsufficientPositionException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "操作失败",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// 编辑持仓：直接修改名称、数量和成本价
    /// </summary>
    [RelayCommand]
    private void EditPosition()
    {
        if (SelectedStock == null) return;
        if (!int.TryParse(InputQuantity, out var qty) || !decimal.TryParse(InputPrice, out var price)) return;
        var name = string.IsNullOrWhiteSpace(InputName) ? null : InputName.Trim();
        _positionService.UpdatePosition(SelectedStock.Code, qty, price, name);
        RefreshList();
    }

    /// <summary>
    /// 删除股票：移除整只股票
    /// </summary>
    [RelayCommand]
    private void DeletePosition()
    {
        if (SelectedStock == null) return;
        _positionService.RemoveStock(SelectedStock.Code);
        RefreshList();
    }

    /// <summary>
    /// 添加新股票：输入股票代码创建空持仓
    /// </summary>
    [RelayCommand]
    private void AddNewStock()
    {
        if (string.IsNullOrEmpty(NewCode)) return;
        var normalizedCode = DataMigrator.NormalizeCode(NewCode.Trim());
        _positionService.AddStock(normalizedCode);
        RefreshList();
        NewCode = string.Empty;
    }

    private void RefreshList()
    {
        StockList.Clear();
        foreach (var p in _positionService.GetAllPositions())
            StockList.Add(p);
    }
}
