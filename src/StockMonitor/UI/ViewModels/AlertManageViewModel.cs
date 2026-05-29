using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 预警管理 ViewModel 占位实现，后续任务将完善
/// </summary>
public partial class AlertManageViewModel : ObservableObject
{
    private readonly IAlertService _alertService;
    private readonly IPositionService _positionService;

    public ObservableCollection<AlertRule> Rules { get; }
    public ObservableCollection<string> StockCodes { get; }
    public Array AlertTypes { get; } = Enum.GetValues(typeof(AlertType));

    [ObservableProperty] private string? _selectedStockCode;
    [ObservableProperty] private AlertType _selectedAlertType;
    [ObservableProperty] private string _thresholdText = string.Empty;
    [ObservableProperty] private bool _isOneTime = true;

    public AlertManageViewModel(IAlertService alertService, IPositionService positionService)
    {
        _alertService = alertService;
        _positionService = positionService;

        Rules = new ObservableCollection<AlertRule>(alertService.GetRules());
        StockCodes = new ObservableCollection<string>(
            positionService.GetAllPositions().Select(p => p.Code));
    }

    [RelayCommand]
    private void AddRule()
    {
        if (string.IsNullOrEmpty(SelectedStockCode)) return;
        if (!decimal.TryParse(ThresholdText, out var threshold)) return;

        var rule = new AlertRule
        {
            StockCode = SelectedStockCode,
            Type = SelectedAlertType,
            Threshold = threshold,
            IsOneTime = IsOneTime
        };

        _alertService.AddRule(rule);
        RefreshRules();
    }

    [RelayCommand]
    private void DeleteRule(AlertRule rule)
    {
        _alertService.RemoveRule(rule.Id);
        RefreshRules();
    }

    private void RefreshRules()
    {
        Rules.Clear();
        foreach (var r in _alertService.GetRules())
            Rules.Add(r);
    }
}
