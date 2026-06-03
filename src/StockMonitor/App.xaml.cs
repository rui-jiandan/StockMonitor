using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Data;
using StockMonitor.Logging;
using StockMonitor.UI.ViewModels;
using StockMonitor.UI.Views;

namespace StockMonitor;

/// <summary>
/// 应用程序入口，配置 DI 容器并启动主窗口
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;

    /// <summary>
    /// 全局服务提供者，供 View 层获取 ViewModel 等服务
    /// </summary>
    public ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "StockMonitor_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("StockMonitor 已在运行中，不能重复启动。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        FileLogger.LogInfo("StockMonitor 启动中...");

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var stocksPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "stocks.json");
        DataMigrator.MigrateStocksIfNeeded(stocksPath);
        FileLogger.LogInfo($"数据迁移完成: {stocksPath}");

        var configRepo = ServiceProvider.GetRequiredService<IRepository<AppConfig>>();
        var config = configRepo.Load();
        FileLogger.LogInfo($"配置加载完成，刷新间隔: {config.RefreshTime}ms，主力数据源: {config.PrimaryDataSource}");

        var positionService = ServiceProvider.GetRequiredService<IPositionService>();
        positionService.MergeDayTrades();
        FileLogger.LogInfo($"持仓合并完成，持仓数: {positionService.GetAllPositions().Count}");

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        FileLogger.LogInfo("主窗口已创建（默认隐藏，通过托盘图标显示）");
    }

    /// <summary>
    /// 配置依赖注入容器，注册所有服务、ViewModel 和 View
    /// </summary>
    /// <param name="services">服务集合</param>
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IRepository<AppConfig>>(_ =>
            new JsonFileRepository<AppConfig>("config.json"));
        services.AddSingleton<IRepository<List<StockPosition>>>(_ =>
            new JsonFileRepository<List<StockPosition>>("stocks.json"));
        services.AddSingleton<IRepository<List<AlertRule>>>(_ =>
            new JsonFileRepository<List<AlertRule>>("alerts.json"));

        services.AddSingleton<IStockDataProvider, SinaDataProvider>();
        services.AddSingleton<IStockDataProvider, TencentDataProvider>();
        services.AddSingleton<IStockDataService, StockDataService>();

        services.AddSingleton<IPositionService>(sp =>
        {
            var repo = sp.GetRequiredService<IRepository<List<StockPosition>>>();
            var configRepo = sp.GetRequiredService<IRepository<AppConfig>>();
            var config = configRepo.Load();
            return new PositionService(repo,
                config.CommissionRate, config.CommissionMinAmount,
                config.EtfCommissionRate, config.EtfCommissionMinAmount,
                config.TaxRate);
        });

        services.AddSingleton<IAlertService, AlertService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<PositionEditViewModel>();
        services.AddTransient<ConfigEditViewModel>();
        services.AddTransient<AlertManageViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<PositionEditDialog>();
        services.AddTransient<ConfigEditDialog>();
        services.AddTransient<AlertManageDialog>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}
