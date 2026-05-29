using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 占位符项，用于配置编辑对话框中展示可用的格式占位符
/// </summary>
public class PlaceholderItem
{
    /// <summary>
    /// 占位符标签，如 "#name"
    /// </summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>
    /// 占位符说明，如 "股票名称"
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public override string ToString() => $"{Tag} ({Description})";
}

/// <summary>
/// 配置编辑 ViewModel，支持占位符展示和格式预览
/// </summary>
public partial class ConfigEditViewModel : ObservableObject
{
    private readonly IRepository<AppConfig> _configRepo;
    private readonly AppConfig _config;

    [ObservableProperty] private string _showFormat;
    [ObservableProperty] private int _refreshTime;
    [ObservableProperty] private string _showTodaySumFormat;
    [ObservableProperty] private string _orderBy;
    [ObservableProperty] private string _previewText = string.Empty;

    /// <summary>
    /// 当前活跃的格式字段名称，用于占位符插入目标
    /// </summary>
    public string ActiveFormatField { get; set; } = "ShowFormat";

    /// <summary>
    /// 股票显示格式可用占位符列表
    /// </summary>
    public List<PlaceholderItem> ShowFormatPlaceholders { get; } = new()
    {
        new() { Tag = "#name", Description = "股票名称" },
        new() { Tag = "#code", Description = "股票代码" },
        new() { Tag = "#price", Description = "当前价格" },
        new() { Tag = "#change", Description = "涨跌额" },
        new() { Tag = "#rate", Description = "涨跌幅" },
        new() { Tag = "#makemoney", Description = "盈亏金额" },
        new() { Tag = "#quantity", Description = "持仓数量" },
        new() { Tag = "#cost", Description = "成本价" },
    };

    /// <summary>
    /// 今日汇总格式可用占位符列表
    /// </summary>
    public List<PlaceholderItem> TodaySumFormatPlaceholders { get; } = new()
    {
        new() { Tag = "#money", Description = "今日总盈亏" },
        new() { Tag = "#rate", Description = "今日盈亏比例" },
    };

    /// <summary>
    /// 占位符插入事件，通知 View 在对应 TextBox 光标位置插入文本
    /// </summary>
    public event Action<string>? PlaceholderInsertRequested;

    public ConfigEditViewModel(IRepository<AppConfig> configRepo)
    {
        _configRepo = configRepo;
        _config = configRepo.Load();

        _showFormat = _config.ShowFormat;
        _refreshTime = _config.RefreshTime;
        _showTodaySumFormat = _config.ShowTodaySumFormat;
        _orderBy = _config.OrderBy;

        UpdatePreview();
    }

    partial void OnShowFormatChanged(string value) => UpdatePreview();
    partial void OnShowTodaySumFormatChanged(string value) => UpdatePreview();

    /// <summary>
    /// 插入占位符到当前活跃的格式输入框
    /// </summary>
    [RelayCommand]
    private void InsertPlaceholder(PlaceholderItem item)
    {
        PlaceholderInsertRequested?.Invoke(item.Tag);

        if (ActiveFormatField == "ShowFormat")
        {
            ShowFormat += item.Tag;
        }
        else
        {
            ShowTodaySumFormat += item.Tag;
        }
    }

    /// <summary>
    /// 根据当前格式和示例数据生成预览文本
    /// </summary>
    private void UpdatePreview()
    {
        var sampleName = "招商银行";
        var sampleCode = "sh600036";
        var samplePrice = "43.68";
        var sampleChange = "+0.17";
        var sampleRate = "0.39%";
        var sampleMakeMoney = "+45.20";
        var sampleQuantity = "300";
        var sampleCost = "43.267";
        var sampleMoney = "+126.50";
        var sampleRateSum = "0.32%";

        var stockPreview = ShowFormat
            .Replace("#name", sampleName)
            .Replace("#code", sampleCode)
            .Replace("#price", samplePrice)
            .Replace("#change", sampleChange)
            .Replace("#rate", sampleRate)
            .Replace("#makemoney", sampleMakeMoney)
            .Replace("#quantity", sampleQuantity)
            .Replace("#cost", sampleCost);

        var todayPreview = ShowTodaySumFormat
            .Replace("#money", sampleMoney)
            .Replace("#rate", sampleRateSum);

        PreviewText = $"[汇总] {todayPreview}\n{stockPreview}";
    }

    [RelayCommand]
    private void Save()
    {
        _config.ShowFormat = ShowFormat;
        _config.RefreshTime = RefreshTime;
        _config.ShowTodaySumFormat = ShowTodaySumFormat;
        _config.OrderBy = OrderBy;
        _configRepo.Save(_config);
    }
}
