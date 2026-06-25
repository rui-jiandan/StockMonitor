using System.Windows;
using System.Windows.Threading;
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

        // 注册全局异常处理：防止未捕获异常导致程序无声退出
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        _singleInstanceMutex = new Mutex(true, "StockMonitor_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("StockMonitor 已在运行中，不能重复启动。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        FileLogger.LogInfo("StockMonitor 启动中...");

        // 先迁移数据，再构建 DI 容器，确保 PositionService 加载的是迁移后的数据
        var stocksPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "stocks.json");
        DataMigrator.MigrateStocksIfNeeded(stocksPath);
        FileLogger.LogInfo($"数据迁移完成: {stocksPath}");

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

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

    /// <summary>
    /// WPF UI 线程未捕获异常处理器：记录日志并防止程序立即崩溃退出
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        FileLogger.LogError("UI 线程未捕获异常", e.Exception);
        try
        {
            MessageBox.Show(
                $"程序发生异常：{e.Exception.Message}\n\n详细信息已记录到日志文件。",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { /* 忽略消息框自身的异常 */ }
        // 标记为已处理，让程序可以继续运行
        e.Handled = true;
    }

    /// <summary>
    /// 非 UI 线程（例如后台刷新线程）未捕获异常处理器
    /// </summary>
    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            FileLogger.LogError("后台线程未捕获异常", ex);
        }
        else
        {
            FileLogger.LogError($"后台线程未捕获异常: {e.ExceptionObject}");
        }
        // 注意：非 UI 线程的严重异常通常无法完全恢复，程序可能仍会终止
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            var positionService = ServiceProvider?.GetRequiredService<IPositionService>();
            positionService?.MergeDayTrades();
        }
        catch { /* 确保退出流程不被阻断 */ }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}
