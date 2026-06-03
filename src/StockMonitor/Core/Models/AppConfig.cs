namespace StockMonitor.Core.Models;

/// <summary>
/// 应用配置模型，兼容现有 config.json
/// </summary>
public class AppConfig
{
    public string ShowFormat { get; set; } = "#name(#code) #makemoney\r\n#price #change #rate%";
    public int RefreshTime { get; set; } = 2000;
    public string ShowTodaySumFormat { get; set; } = "今日盈亏:#money,今日比例 #rate%";
    public string OrderBy { get; set; } = "havepos desc,makemoney desc";
    public decimal CommissionRate { get; set; } = 0.000086m;
    public decimal CommissionMinAmount { get; set; } = 5m;
    public decimal EtfCommissionRate { get; set; } = 0.00005m;
    public decimal EtfCommissionMinAmount { get; set; } = 0.1m;
    public decimal TaxRate { get; set; } = 0m;
    public string PrimaryDataSource { get; set; } = "Sina";
    public string FallbackDataSource { get; set; } = "Tencent";
    public int MaxVisibleStocks { get; set; } = 5;
}
