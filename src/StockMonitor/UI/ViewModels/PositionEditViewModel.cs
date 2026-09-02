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

    /// <summary>
    /// 特别关注按钮的显示文本，根据选中股票的关注状态切换
    /// </summary>
    [ObservableProperty] private string _watchButtonText = "设为关注";

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
            WatchButtonText = value.IsWatched ? "取消关注" : "设为关注";
        }
    }

    /// <summary>
    /// 切换选中股票的特别关注状态：已关注则取消，未关注则设为关注
    /// </summary>
    [RelayCommand]
    private void ToggleWatch()
    {
        if (SelectedStock == null) return;
        // 先保存引用：RefreshList 清空集合会使选中项被重置为 null
        var stock = SelectedStock;
        _positionService.SetWatched(stock.Code, !stock.IsWatched);
        RefreshList();
        WatchButtonText = stock.IsWatched ? "取消关注" : "设为关注";
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
    /// 添加新股票：输入股票代码和名称创建持仓；若填写了数量和成本价则一并初始化仓位
    /// </summary>
    [RelayCommand]
    private void AddNewStock()
    {
        if (string.IsNullOrEmpty(NewCode)) return;
        var normalizedCode = DataMigrator.NormalizeCode(NewCode.Trim());
        var name = string.IsNullOrWhiteSpace(InputName) ? null : InputName.Trim();
        _positionService.AddStock(normalizedCode, name);

        // 数量>0 且价格有效时，把输入框中的仓位和成本价直接写入新股票
        if (int.TryParse(InputQuantity, out var qty) && qty > 0
            && decimal.TryParse(InputPrice, out var price) && price > 0)
        {
            _positionService.UpdatePosition(normalizedCode, qty, price, name);
        }

        RefreshList();
        // 清空输入框，避免残留上一只股票的数量/成本价被误带入下一次新增
        NewCode = string.Empty;
        InputName = string.Empty;
        InputQuantity = string.Empty;
        InputPrice = string.Empty;
    }

    private void RefreshList()
    {
        StockList.Clear();
        foreach (var p in _positionService.GetAllPositions())
            StockList.Add(p);
    }
}
