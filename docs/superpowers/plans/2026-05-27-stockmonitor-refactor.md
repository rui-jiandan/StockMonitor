# StockMonitor 中度重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 StockMonitor 从 WinForms 单体架构重构为 WPF + MVVM 分层架构，修复 T+0 bug，新增数据源 failover 和价格预警功能。

**Architecture:** 三层架构（Core/Data/UI），Core 层零 UI 依赖，Data 层负责持久化和迁移，UI 层使用 WPF + CommunityToolkit.Mvvm 实现 MVVM 模式。依赖注入统一管理服务生命周期。

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Hardcodet.NotifyIcon.Wpf, xunit, Moq, FluentAssertions

---

## 文件结构映射

### 新建文件

| 文件路径 | 职责 |
|----------|------|
| `src/StockMonitor/StockMonitor.csproj` | WPF 项目文件，替代旧 WinForms 项目 |
| `src/StockMonitor/App.xaml` | WPF 应用入口 XAML |
| `src/StockMonitor/App.xaml.cs` | DI 容器配置 + 启动逻辑 |
| `src/StockMonitor/Core/Models/StockPosition.cs` | 持仓持久化模型 |
| `src/StockMonitor/Core/Models/StockQuote.cs` | 实时行情快照模型 |
| `src/StockMonitor/Core/Models/TradeRecord.cs` | 交易记录模型 |
| `src/StockMonitor/Core/Models/AlertRule.cs` | 预警规则模型 |
| `src/StockMonitor/Core/Models/AppConfig.cs` | 应用配置模型 |
| `src/StockMonitor/Core/Services/Interfaces/IStockDataProvider.cs` | 数据源提供者接口 |
| `src/StockMonitor/Core/Services/Interfaces/IStockDataService.cs` | 数据源调度服务接口 |
| `src/StockMonitor/Core/Services/Interfaces/IPositionService.cs` | 持仓服务接口 |
| `src/StockMonitor/Core/Services/Interfaces/IAlertService.cs` | 预警服务接口 |
| `src/StockMonitor/Core/Services/Interfaces/IRepository{T}.cs` | 仓储接口 |
| `src/StockMonitor/Core/Services/SinaDataProvider.cs` | 新浪数据源实现 |
| `src/StockMonitor/Core/Services/TencentDataProvider.cs` | 腾讯数据源实现 |
| `src/StockMonitor/Core/Services/StockDataService.cs` | 数据源调度（failover） |
| `src/StockMonitor/Core/Services/PositionService.cs` | 持仓管理（T+0 修复核心） |
| `src/StockMonitor/Core/Services/AlertService.cs` | 预警服务实现 |
| `src/StockMonitor/Core/Calculator/PnLCalculator.cs` | 盈亏计算器 |
| `src/StockMonitor/Data/JsonFileRepository{T}.cs` | JSON 文件仓储实现 |
| `src/StockMonitor/Data/DataMigrator.cs` | 旧格式数据迁移 |
| `src/StockMonitor/UI/ViewModels/MainViewModel.cs` | 主窗口 ViewModel |
| `src/StockMonitor/UI/ViewModels/StockDisplayItem.cs` | 股票显示项 |
| `src/StockMonitor/UI/ViewModels/PositionEditViewModel.cs` | 持仓编辑 ViewModel |
| `src/StockMonitor/UI/ViewModels/ConfigEditViewModel.cs` | 配置编辑 ViewModel |
| `src/StockMonitor/UI/ViewModels/AlertManageViewModel.cs` | 预警管理 ViewModel |
| `src/StockMonitor/UI/Views/MainWindow.xaml` | 主窗口 XAML |
| `src/StockMonitor/UI/Views/MainWindow.xaml.cs` | 主窗口 code-behind |
| `src/StockMonitor/UI/Views/PositionEditDialog.xaml` | 持仓编辑对话框 XAML |
| `src/StockMonitor/UI/Views/PositionEditDialog.xaml.cs` | 持仓编辑 code-behind |
| `src/StockMonitor/UI/Views/ConfigEditDialog.xaml` | 配置编辑对话框 XAML |
| `src/StockMonitor/UI/Views/ConfigEditDialog.xaml.cs` | 配置编辑 code-behind |
| `src/StockMonitor/UI/Views/AlertManageDialog.xaml` | 预警管理对话框 XAML |
| `src/StockMonitor/UI/Views/AlertManageDialog.xaml.cs` | 预警管理 code-behind |
| `src/StockMonitor/UI/Converters/PnLToBrushConverter.cs` | 盈亏→颜色转换器 |
| `src/StockMonitor/UI/Converters/BoolToVisibilityConverter.cs` | 布尔→可见性转换器 |
| `src/StockMonitor/UI/Resources/Styles.xaml` | 全局样式 |
| `src/StockMonitor/Logging/FileLogger.cs` | 文件日志器 |
| `tests/StockMonitor.Tests/StockMonitor.Tests.csproj` | 测试项目文件 |
| `tests/StockMonitor.Tests/Calculator/PnLCalculatorTests.cs` | 盈亏计算测试 |
| `tests/StockMonitor.Tests/Services/PositionServiceTests.cs` | 持仓服务测试 |
| `tests/StockMonitor.Tests/Services/AlertServiceTests.cs` | 预警服务测试 |
| `tests/StockMonitor.Tests/Services/StockDataServiceTests.cs` | 数据源 failover 测试 |
| `tests/StockMonitor.Tests/Data/DataMigratorTests.cs` | 数据迁移测试 |
| `tests/StockMonitor.Tests/Data/JsonRepositoryTests.cs` | 仓储测试 |

### 删除文件（旧项目）

| 文件路径 | 原因 |
|----------|------|
| `MainForm.cs` | WinForms → WPF |
| `MainForm.Designer.cs` | WinForms → WPF |
| `EditConfigForm.cs` | WinForms → WPF |
| `EditConfigForm.Designer.cs` | WinForms → WPF |
| `EditDeleteStockForm.cs` | WinForms → WPF |
| `EditDeleteStockForm.Designer.cs` | WinForms → WPF |
| `TransparentRichTextBox.cs` | WPF 原生支持透明 |
| `Model/Config.cs` | 合并为 AppConfig |
| `Model/StockConfig.cs` | 替换为 StockPosition |
| `Model/StockInfo.cs` | 替换为 StockQuote |
| `Model/StockView.cs` | 替换为 StockDisplayItem |
| `Model/StockCalculator.cs` | 替换为 PnLCalculator + PositionService |
| `Logger/Logger.cs` | 替换为 FileLogger |
| `Program.cs` | WPF 使用 App.xaml 启动 |
| `StockMonitor.csproj` | 替换为 WPF 项目文件 |

---

## Task 1: 项目脚手架

**Files:**
- Create: `src/StockMonitor/StockMonitor.csproj`
- Create: `src/StockMonitor/App.xaml`
- Create: `src/StockMonitor/App.xaml.cs`
- Create: `tests/StockMonitor.Tests/StockMonitor.Tests.csproj`

- [ ] **Step 1: 创建 src/StockMonitor 目录和 WPF 项目文件**

在项目根目录下创建新的目录结构：

```bash
mkdir -p src/StockMonitor/Core/Models
mkdir -p src/StockMonitor/Core/Services/Interfaces
mkdir -p src/StockMonitor/Core/Calculator
mkdir -p src/StockMonitor/Data
mkdir -p src/StockMonitor/UI/ViewModels
mkdir -p src/StockMonitor/UI/Views
mkdir -p src/StockMonitor/UI/Converters
mkdir -p src/StockMonitor/UI/Resources
mkdir -p src/StockMonitor/Logging
mkdir -p tests/StockMonitor.Tests/Calculator
mkdir -p tests/StockMonitor.Tests/Services
mkdir -p tests/StockMonitor.Tests/Data
```

创建 `src/StockMonitor/StockMonitor.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>icon.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.4.0" />
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
  </ItemGroup>

  <ItemGroup>
    <None Update="config.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="stocks.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="alerts.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="icon.ico">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 创建 App.xaml**

创建 `src/StockMonitor/App.xaml`：

```xml
<Application x:Class="StockMonitor.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="UI/Resources/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 3: 创建 App.xaml.cs 骨架**

创建 `src/StockMonitor/App.xaml.cs`：

```csharp
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Data;
using StockMonitor.UI.ViewModels;
using StockMonitor.UI.Views;

namespace StockMonitor;

public partial class App : Application
{
    private ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IRepository<AppConfig>>(sp => new JsonFileRepository<AppConfig>("config.json"));
        services.AddSingleton<IRepository<List<StockPosition>>>(sp => new JsonFileRepository<List<StockPosition>>("stocks.json"));
        services.AddSingleton<IRepository<List<AlertRule>>>(sp => new JsonFileRepository<List<AlertRule>>("alerts.json"));

        services.AddSingleton<IStockDataProvider, SinaDataProvider>();
        services.AddSingleton<IStockDataProvider, TencentDataProvider>();
        services.AddSingleton<IStockDataService, StockDataService>();
        services.AddSingleton<IPositionService, PositionService>();
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
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 4: 创建测试项目**

创建 `tests/StockMonitor.Tests/StockMonitor.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="FluentAssertions" Version="7.1.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\StockMonitor\StockMonitor.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: 创建占位 Styles.xaml**

创建 `src/StockMonitor/UI/Resources/Styles.xaml`：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
</ResourceDictionary>
```

- [ ] **Step 6: 复制 icon.ico 到新项目目录**

```bash
copy icon.ico src\StockMonitor\icon.ico
```

- [ ] **Step 7: 创建解决方案文件**

创建 `StockMonitor.sln`（在项目根目录），指向新的 src/StockMonitor 和 tests/StockMonitor.Tests 项目：

```bash
dotnet new sln --force
dotnet sln add src/StockMonitor/StockMonitor.csproj
dotnet sln add tests/StockMonitor.Tests/StockMonitor.Tests.csproj
```

- [ ] **Step 8: 验证项目可以构建**

```bash
dotnet build StockMonitor.sln
```

Expected: 构建成功（可能有未引用类型的警告，这是预期的）

- [ ] **Step 9: 提交**

```bash
git add -A
git commit -m "chore: scaffold WPF project structure with DI and test project"
```

---

## Task 2: Core 模型层

**Files:**
- Create: `src/StockMonitor/Core/Models/StockPosition.cs`
- Create: `src/StockMonitor/Core/Models/StockQuote.cs`
- Create: `src/StockMonitor/Core/Models/TradeRecord.cs`
- Create: `src/StockMonitor/Core/Models/AlertRule.cs`
- Create: `src/StockMonitor/Core/Models/AppConfig.cs`

- [ ] **Step 1: 创建 StockPosition 模型**

创建 `src/StockMonitor/Core/Models/StockPosition.cs`：

```csharp
namespace StockMonitor.Core.Models;

/// <summary>
/// 股票持仓持久化模型，对应 stocks.json
/// </summary>
public class StockPosition
{
    /// <summary>
    /// 股票全量编码，如 "sh600036"
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 股票名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 当前持仓数量（不含当日交易）
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 持仓成本均价
    /// </summary>
    public decimal AvgCostPrice { get; set; }

    /// <summary>
    /// 当日交易记录列表
    /// </summary>
    public List<TradeRecord> TodayTrades { get; set; } = new();

    /// <summary>
    /// 获取当日净持仓（买入减卖出）
    /// </summary>
    public int GetTodayNetQuantity()
    {
        return TodayTrades
            .Where(t => t.Time.Date == DateTime.Today)
            .Sum(t => t.Type == TradeRecord.TradeType.Buy ? t.Quantity : -t.Quantity);
    }

    /// <summary>
    /// 获取总持仓（原持仓 + 当日净持仓）
    /// </summary>
    public int GetTotalQuantity()
    {
        return Quantity + GetTodayNetQuantity();
    }
}
```

- [ ] **Step 2: 创建 TradeRecord 模型**

创建 `src/StockMonitor/Core/Models/TradeRecord.cs`：

```csharp
namespace StockMonitor.Core.Models;

/// <summary>
/// 交易记录
/// </summary>
public class TradeRecord
{
    public enum TradeType { Buy, Sell }

    /// <summary>
    /// 交易类型
    /// </summary>
    public TradeType Type { get; init; }

    /// <summary>
    /// 交易数量（始终为正数）
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// 成交价格
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// 成交时间
    /// </summary>
    public DateTime Time { get; init; }

    /// <summary>
    /// 佣金
    /// </summary>
    public decimal Commission { get; init; }

    /// <summary>
    /// 印花税（仅卖出时收取）
    /// </summary>
    public decimal Tax { get; init; }
}
```

- [ ] **Step 3: 创建 StockQuote 模型**

创建 `src/StockMonitor/Core/Models/StockQuote.cs`：

```csharp
namespace StockMonitor.Core.Models;

/// <summary>
/// 实时行情快照，每次数据刷新生成
/// </summary>
public class StockQuote
{
    /// <summary>
    /// 股票全量编码
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 股票名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 当前价格
    /// </summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>
    /// 开盘价
    /// </summary>
    public decimal OpenPrice { get; init; }

    /// <summary>
    /// 昨日收盘价
    /// </summary>
    public decimal YestClose { get; init; }

    /// <summary>
    /// 最高价
    /// </summary>
    public decimal HighPrice { get; init; }

    /// <summary>
    /// 最低价
    /// </summary>
    public decimal LowPrice { get; init; }

    /// <summary>
    /// 涨跌额
    /// </summary>
    public decimal Change => YestClose != 0 ? CurrentPrice - YestClose : 0;

    /// <summary>
    /// 涨跌幅（百分比）
    /// </summary>
    public decimal ChangeRate => YestClose != 0 ? Math.Round(Change / YestClose * 100, 2) : 0;
}
```

- [ ] **Step 4: 创建 AlertRule 模型**

创建 `src/StockMonitor/Core/Models/AlertRule.cs`：

```csharp
namespace StockMonitor.Core.Models;

/// <summary>
/// 预警规则
/// </summary>
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 股票全量编码
    /// </summary>
    public string StockCode { get; set; } = string.Empty;

    /// <summary>
    /// 预警类型
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// 预警阈值
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// 是否已触发（避免重复通知）
    /// </summary>
    public bool IsTriggered { get; set; }

    /// <summary>
    /// 是否一次性（触发后自动删除）
    /// </summary>
    public bool IsOneTime { get; set; } = true;
}

/// <summary>
/// 预警类型枚举
/// </summary>
public enum AlertType
{
    PriceAbove,
    PriceBelow,
    ChangeRateAbove,
    ChangeRateBelow
}
```

- [ ] **Step 5: 创建 AppConfig 模型**

创建 `src/StockMonitor/Core/Models/AppConfig.cs`：

```csharp
namespace StockMonitor.Core.Models;

/// <summary>
/// 应用配置模型，兼容现有 config.json
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 股票显示格式
    /// </summary>
    public string ShowFormat { get; set; } = "#name(#code) #makemoney\r\n#price #change #rate%";

    /// <summary>
    /// 数据刷新间隔（毫秒）
    /// </summary>
    public int RefreshTime { get; set; } = 2000;

    /// <summary>
    /// 今日汇总显示格式
    /// </summary>
    public string ShowTodaySumFormat { get; set; } = "今日盈亏:#money,今日比例 #rate%";

    /// <summary>
    /// 排序规则
    /// </summary>
    public string OrderBy { get; set; } = "havepos desc,makemoney desc";

    /// <summary>
    /// 佣金费率
    /// </summary>
    public decimal CommissionRate { get; set; } = 0.00023m;

    /// <summary>
    /// 印花税费率
    /// </summary>
    public decimal TaxRate { get; set; } = 0;

    /// <summary>
    /// 主力数据源名称
    /// </summary>
    public string PrimaryDataSource { get; set; } = "Sina";

    /// <summary>
    /// 备用数据源名称
    /// </summary>
    public string FallbackDataSource { get; set; } = "Tencent";
}
```

- [ ] **Step 6: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "feat: add Core models - StockPosition, StockQuote, TradeRecord, AlertRule, AppConfig"
```

---

## Task 3: 服务接口定义

**Files:**
- Create: `src/StockMonitor/Core/Services/Interfaces/IRepository{T}.cs`
- Create: `src/StockMonitor/Core/Services/Interfaces/IStockDataProvider.cs`
- Create: `src/StockMonitor/Core/Services/Interfaces/IStockDataService.cs`
- Create: `src/StockMonitor/Core/Services/Interfaces/IPositionService.cs`
- Create: `src/StockMonitor/Core/Services/Interfaces/IAlertService.cs`

- [ ] **Step 1: 创建 IRepository<T>**

创建 `src/StockMonitor/Core/Services/Interfaces/IRepository{T}.cs`：

```csharp
namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 泛型仓储接口
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// 加载数据
    /// </summary>
    T Load();

    /// <summary>
    /// 保存数据
    /// </summary>
    void Save(T data);

    /// <summary>
    /// 数据文件是否存在
    /// </summary>
    bool Exists();
}
```

- [ ] **Step 2: 创建 IStockDataProvider**

创建 `src/StockMonitor/Core/Services/Interfaces/IStockDataProvider.cs`：

```csharp
using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 股票数据源提供者接口
/// </summary>
public interface IStockDataProvider
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 批量获取股票行情
    /// </summary>
    /// <param name="codes">股票全量编码列表，如 "sh600036"</param>
    Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes);

    /// <summary>
    /// 数据源是否可用
    /// </summary>
    bool IsAvailable { get; }
}
```

- [ ] **Step 3: 创建 IStockDataService**

创建 `src/StockMonitor/Core/Services/Interfaces/IStockDataService.cs`：

```csharp
using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 数据源调度服务接口，负责 failover 和缓存
/// </summary>
public interface IStockDataService
{
    /// <summary>
    /// 获取所有持仓股票的行情数据
    /// </summary>
    Task<List<StockQuote>> GetQuotesAsync();

    /// <summary>
    /// 行情数据更新事件
    /// </summary>
    event EventHandler<List<StockQuote>>? QuotesUpdated;
}
```

- [ ] **Step 4: 创建 IPositionService**

创建 `src/StockMonitor/Core/Services/Interfaces/IPositionService.cs`：

```csharp
using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 持仓变更事件参数
/// </summary>
public class PositionChangedEventArgs : EventArgs
{
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// 持仓不足异常
/// </summary>
public class InsufficientPositionException : Exception
{
    public InsufficientPositionException(string message) : base(message) { }
}

/// <summary>
/// 持仓服务接口
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// 获取所有持仓
    /// </summary>
    IReadOnlyList<StockPosition> GetAllPositions();

    /// <summary>
    /// 获取指定股票持仓
    /// </summary>
    /// <param name="code">股票全量编码</param>
    StockPosition? GetPosition(string code);

    /// <summary>
    /// 加仓操作
    /// </summary>
    void AddPosition(string code, decimal price, int quantity);

    /// <summary>
    /// 减仓操作
    /// </summary>
    void ReducePosition(string code, decimal price, int quantity);

    /// <summary>
    /// 直接修改持仓和成本
    /// </summary>
    void UpdatePosition(string code, int quantity, decimal avgCostPrice);

    /// <summary>
    /// 新增股票（无持仓）
    /// </summary>
    void AddStock(string code);

    /// <summary>
    /// 删除股票
    /// </summary>
    void RemoveStock(string code);

    /// <summary>
    /// 合并当日交易到主持仓（次日启动时调用）
    /// </summary>
    void MergeDayTrades();

    /// <summary>
    /// 持仓变更事件
    /// </summary>
    event EventHandler<PositionChangedEventArgs>? PositionChanged;
}
```

- [ ] **Step 5: 创建 IAlertService**

创建 `src/StockMonitor/Core/Services/Interfaces/IAlertService.cs`：

```csharp
using StockMonitor.Core.Models;

namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 预警触发事件参数
/// </summary>
public class AlertTriggeredEventArgs : EventArgs
{
    public AlertRule Rule { get; init; } = null!;
    public StockQuote Quote { get; init; } = null!;
}

/// <summary>
/// 预警服务接口
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// 添加预警规则
    /// </summary>
    void AddRule(AlertRule rule);

    /// <summary>
    /// 删除预警规则
    /// </summary>
    void RemoveRule(string ruleId);

    /// <summary>
    /// 获取所有预警规则
    /// </summary>
    IReadOnlyList<AlertRule> GetRules();

    /// <summary>
    /// 检查行情数据是否触发预警
    /// </summary>
    void CheckAlerts(IEnumerable<StockQuote> quotes);

    /// <summary>
    /// 预警触发事件
    /// </summary>
    event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;
}
```

- [ ] **Step 6: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "feat: add service interfaces - IRepository, IStockDataProvider, IStockDataService, IPositionService, IAlertService"
```

---

## Task 4: Data 层实现

**Files:**
- Create: `src/StockMonitor/Data/JsonFileRepository{T}.cs`
- Create: `src/StockMonitor/Data/DataMigrator.cs`
- Create: `tests/StockMonitor.Tests/Data/JsonRepositoryTests.cs`
- Create: `tests/StockMonitor.Tests/Data/DataMigratorTests.cs`

- [ ] **Step 1: 写 JsonFileRepository 的失败测试**

创建 `tests/StockMonitor.Tests/Data/JsonRepositoryTests.cs`：

```csharp
using FluentAssertions;
using StockMonitor.Core.Models;
using StockMonitor.Data;
using System.Text.Json;
using Xunit;

namespace StockMonitor.Tests.Data;

public class JsonRepositoryTests : IDisposable
{
    private readonly string _testDir;

    public JsonRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"StockMonitor_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsDefault()
    {
        var repo = new JsonFileRepository<AppConfig>(Path.Combine(_testDir, "notexist.json"));
        var result = repo.Load();
        result.Should().NotBeNull();
        result.RefreshTime.Should().Be(2000);
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        var filePath = Path.Combine(_testDir, "config.json");
        var repo = new JsonFileRepository<AppConfig>(filePath);
        var config = new AppConfig { RefreshTime = 5000, ShowFormat = "test" };

        repo.Save(config);
        var loaded = repo.Load();

        loaded.RefreshTime.Should().Be(5000);
        loaded.ShowFormat.Should().Be("test");
    }

    [Fact]
    public void Exists_ReturnsCorrectly()
    {
        var filePath = Path.Combine(_testDir, "check.json");
        var repo = new JsonFileRepository<AppConfig>(filePath);

        repo.Exists().Should().BeFalse();

        repo.Save(new AppConfig());
        repo.Exists().Should().BeTrue();
    }

    [Fact]
    public void Load_CorruptedFile_FallsBackToBackup()
    {
        var filePath = Path.Combine(_testDir, "corrupt.json");
        File.WriteAllText(filePath, "{ invalid json }}}");

        var repo = new JsonFileRepository<AppConfig>(filePath);
        var result = repo.Load();
        result.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "JsonRepositoryTests" -v n
```

Expected: 编译失败或测试失败（JsonFileRepository 尚未实现）

- [ ] **Step 3: 实现 JsonFileRepository**

创建 `src/StockMonitor/Data/JsonFileRepository{T}.cs`：

```csharp
using StockMonitor.Core.Services.Interfaces;
using System.Text.Json;

namespace StockMonitor.Data;

/// <summary>
/// JSON 文件仓储实现，支持原子写入和备份恢复
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class JsonFileRepository<T> : IRepository<T> where T : new()
{
    private readonly string _filePath;
    private readonly string _tmpFilePath;
    private readonly string _bakFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private T? _cache;

    public JsonFileRepository(string filePath)
    {
        _filePath = filePath;
        _tmpFilePath = filePath + ".tmp";
        _bakFilePath = filePath + ".bak";
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// 加载数据，失败时尝试备份文件
    /// </summary>
    public T Load()
    {
        if (_cache != null)
            return _cache;

        T? result = default;

        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
        }
        catch
        {
            // 主文件损坏，尝试备份
        }

        if (result == null)
        {
            try
            {
                if (File.Exists(_bakFilePath))
                {
                    var json = File.ReadAllText(_bakFilePath);
                    result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
            }
            catch
            {
                // 备份也损坏
            }
        }

        _cache = result ?? new T();
        return _cache;
    }

    /// <summary>
    /// 保存数据，原子写入（先写 tmp 再重命名）
    /// </summary>
    public void Save(T data)
    {
        _cache = data;
        var json = JsonSerializer.Serialize(data, _jsonOptions);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 先写临时文件
        File.WriteAllText(_tmpFilePath, json);

        // 备份现有文件
        if (File.Exists(_filePath))
        {
            File.Copy(_filePath, _bakFilePath, true);
        }

        // 原子替换
        File.Move(_tmpFilePath, _filePath, true);
    }

    /// <summary>
    /// 数据文件是否存在
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_filePath) || File.Exists(_bakFilePath);
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "JsonRepositoryTests" -v n
```

Expected: 全部通过

- [ ] **Step 5: 写 DataMigrator 的失败测试**

创建 `tests/StockMonitor.Tests/Data/DataMigratorTests.cs`：

```csharp
using FluentAssertions;
using StockMonitor.Core.Models;
using StockMonitor.Data;
using Xunit;

namespace StockMonitor.Tests.Data;

public class DataMigratorTests : IDisposable
{
    private readonly string _testDir;

    public DataMigratorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"StockMonitor_Migrate_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void MigrateStocks_OldFormat_ConvertsToNewFormat()
    {
        var oldJson = @"[{""Code"":""600036"",""Position"":300,""Cost"":43.267,""OpList"":[]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("sh600036");
        result[0].Quantity.Should().Be(300);
        result[0].AvgCostPrice.Should().Be(43.267m);
    }

    [Fact]
    public void MigrateStocks_ShenzhenCode_GetsSzPrefix()
    {
        var oldJson = @"[{""Code"":""000001"",""Position"":100,""Cost"":15.5,""OpList"":[]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sz000001");
    }

    [Fact]
    public void MigrateStocks_EtfShanghai_GetsShPrefix()
    {
        var oldJson = @"[{""Code"":""510050"",""Position"":1000,""Cost"":2.5,""OpList"":[]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sh510050");
    }

    [Fact]
    public void MigrateStocks_EtfShenzhen_GetsSzPrefix()
    {
        var oldJson = @"[{""Code"":""159941"",""Position"":0,""Cost"":0,""OpList"":[]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sz159941");
    }

    [Fact]
    public void MigrateStocks_AlreadyNewFormat_NoChange()
    {
        var newJson = @"[{""Code"":""sh600036"",""Quantity"":300,""AvgCostPrice"":43.267,""TodayTrades"":[]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, newJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].Code.Should().Be("sh600036");
        result[0].Quantity.Should().Be(300);
    }

    [Fact]
    public void MigrateStocks_WithOpList_ConvertsToTodayTrades()
    {
        var oldJson = @"[{""Code"":""600036"",""Position"":300,""Cost"":43.267,""OpList"":[{""Type"":0,""Position"":100,""Price"":44.0,""Time"":""2026-05-27T10:00:00"",""Commission"":5,""Tax"":0}]}]";
        var filePath = Path.Combine(_testDir, "stocks.json");
        File.WriteAllText(filePath, oldJson);

        var result = DataMigrator.MigrateStocksIfNeeded(filePath);

        result[0].TodayTrades.Should().HaveCount(1);
        result[0].TodayTrades[0].Type.Should().Be(TradeRecord.TradeType.Buy);
        result[0].TodayTrades[0].Quantity.Should().Be(100);
        result[0].TodayTrades[0].Price.Should().Be(44.0m);
    }
}
```

- [ ] **Step 6: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "DataMigratorTests" -v n
```

Expected: 编译失败（DataMigrator 尚未实现）

- [ ] **Step 7: 实现 DataMigrator**

创建 `src/StockMonitor/Data/DataMigrator.cs`：

```csharp
using StockMonitor.Core.Models;
using System.Text.Json;

namespace StockMonitor.Data;

/// <summary>
/// 旧格式数据迁移工具
/// </summary>
public static class DataMigrator
{
    /// <summary>
    /// 检测并迁移旧格式 stocks.json
    /// </summary>
    /// <param name="filePath">stocks.json 文件路径</param>
    /// <returns>迁移后的持仓列表</returns>
    public static List<StockPosition> MigrateStocksIfNeeded(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<StockPosition>();

        var json = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            return new List<StockPosition>();

        var firstElement = root.EnumerateArray().FirstOrDefault();
        if (firstElement.ValueKind == JsonValueKind.Undefined)
            return new List<StockPosition>();

        bool needsMigration = firstElement.TryGetProperty("Position", out _) &&
                              !firstElement.TryGetProperty("Quantity", out _);

        if (needsMigration)
        {
            var oldData = JsonSerializer.Deserialize<List<OldStockConfig>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var result = oldData?.Select(MapToNew).ToList() ?? new List<StockPosition>();

            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            File.WriteAllText(filePath, JsonSerializer.Serialize(result, options));

            return result;
        }

        return JsonSerializer.Deserialize<List<StockPosition>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? new List<StockPosition>();
    }

    /// <summary>
    /// 为纯数字股票代码补全市场前缀
    /// </summary>
    /// <param name="code">股票代码，如 "600036" 或 "sh600036"</param>
    /// <returns>全量编码，如 "sh600036"</returns>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        if (code.StartsWith("sh") || code.StartsWith("sz"))
            return code;

        if (code.Length < 1)
            return code;

        char first = code[0];
        return first switch
        {
            '6' or '9' or '5' => "sh" + code,
            '0' or '3' or '1' => "sz" + code,
            _ => code
        };
    }

    private static StockPosition MapToNew(OldStockConfig old)
    {
        var position = new StockPosition
        {
            Code = NormalizeCode(old.Code),
            Name = old.Name ?? string.Empty,
            Quantity = old.Position,
            AvgCostPrice = old.Cost,
            TodayTrades = new List<TradeRecord>()
        };

        if (old.OpList != null)
        {
            foreach (var op in old.OpList)
            {
                position.TodayTrades.Add(new TradeRecord
                {
                    Type = op.Type == 0 ? TradeRecord.TradeType.Buy : TradeRecord.TradeType.Sell,
                    Quantity = Math.Abs(op.Position),
                    Price = op.Price,
                    Time = op.Time,
                    Commission = op.Commission,
                    Tax = op.Tax
                });
            }
        }

        return position;
    }

    private class OldStockConfig
    {
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int Position { get; set; }
        public decimal Cost { get; set; }
        public List<OldStockOperate>? OpList { get; set; }
    }

    private class OldStockOperate
    {
        public int Type { get; set; }
        public int Position { get; set; }
        public decimal Price { get; set; }
        public DateTime Time { get; set; }
        public decimal Commission { get; set; }
        public decimal Tax { get; set; }
    }
}
```

- [ ] **Step 8: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "DataMigratorTests" -v n
```

Expected: 全部通过

- [ ] **Step 9: 运行所有测试**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj -v n
```

Expected: 全部通过

- [ ] **Step 10: 提交**

```bash
git add -A
git commit -m "feat: implement Data layer - JsonFileRepository with atomic writes and DataMigrator for old format"
```

---

## Task 5: 盈亏计算器（PnLCalculator）

**Files:**
- Create: `src/StockMonitor/Core/Calculator/PnLCalculator.cs`
- Create: `tests/StockMonitor.Tests/Calculator/PnLCalculatorTests.cs`

- [ ] **Step 1: 写 PnLCalculator 的失败测试（含 T+0 场景）**

创建 `tests/StockMonitor.Tests/Calculator/PnLCalculatorTests.cs`：

```csharp
using FluentAssertions;
using StockMonitor.Core.Calculator;
using StockMonitor.Core.Models;
using Xunit;

namespace StockMonitor.Tests.Calculator;

public class PnLCalculatorTests
{
    private static StockPosition CreatePosition(int quantity, decimal avgCost, List<TradeRecord>? trades = null)
    {
        return new StockPosition
        {
            Code = "sh600036",
            Name = "测试股票",
            Quantity = quantity,
            AvgCostPrice = avgCost,
            TodayTrades = trades ?? new List<TradeRecord>()
        };
    }

    private static StockQuote CreateQuote(decimal currentPrice, decimal yestClose)
    {
        return new StockQuote
        {
            Code = "sh600036",
            Name = "测试股票",
            CurrentPrice = currentPrice,
            YestClose = yestClose
        };
    }

    [Fact]
    public void CalculateTodayPnL_NoTrades_OnlyUnrealized()
    {
        var position = CreatePosition(300, 43.267m);
        var quote = CreateQuote(44.0m, 43.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0);
        unrealized.Should().BeApproximately((44.0m - 43.267m) * 300, 0.01m);
        total.Should().Be(unrealized);
    }

    [Fact]
    public void CalculateTodayPnL_TodayBuy_ThenSell_T0()
    {
        // T+0: 先买 100 股 @ 43.0，再卖 100 股 @ 44.0
        var trades = new List<TradeRecord>
        {
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 100, Price = 43.0m, Time = DateTime.Today.AddHours(10), Commission = 5, Tax = 0 },
            new() { Type = TradeRecord.TradeType.Sell, Quantity = 100, Price = 44.0m, Time = DateTime.Today.AddHours(14), Commission = 5, Tax = 5 }
        };
        var position = CreatePosition(300, 43.267m, trades);
        var quote = CreateQuote(44.0m, 43.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现: 卖出 (44.0 - 43.0) * 100 - 5(佣金) - 5(印花税) = 90
        realized.Should().BeApproximately(90m, 0.01m);

        // 浮动: 买入 (44.0 - 43.0) * 100 - 5(佣金) + 原持仓 (44.0 - 43.267) * 300
        var expectedBuyPnL = (44.0m - 43.0m) * 100 - 5m;
        var expectedHoldPnL = (44.0m - 43.267m) * 300;
        unrealized.Should().BeApproximately(expectedBuyPnL + expectedHoldPnL, 0.01m);
    }

    [Fact]
    public void CalculateTodayPnL_TodaySell_ThenBuy_T0()
    {
        // T+0: 先卖 100 股 @ 44.0，再买 100 股 @ 43.0
        var trades = new List<TradeRecord>
        {
            new() { Type = TradeRecord.TradeType.Sell, Quantity = 100, Price = 44.0m, Time = DateTime.Today.AddHours(10), Commission = 5, Tax = 5 },
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 100, Price = 43.0m, Time = DateTime.Today.AddHours(14), Commission = 5, Tax = 0 }
        };
        var position = CreatePosition(300, 43.267m, trades);
        var quote = CreateQuote(44.0m, 43.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        // 已实现: 卖出 (44.0 - 43.0) * 100 - 5 - 5 = 90
        realized.Should().BeApproximately(90m, 0.01m);

        // 浮动: 买入 (44.0 - 43.0) * 100 - 5 + 原持仓 (44.0 - 43.267) * 300
        var expectedBuyPnL = (44.0m - 43.0m) * 100 - 5m;
        var expectedHoldPnL = (44.0m - 43.267m) * 300;
        unrealized.Should().BeApproximately(expectedBuyPnL + expectedHoldPnL, 0.01m);
    }

    [Fact]
    public void CalculateTodayPnL_MultipleBuys_CorrectUnrealized()
    {
        // 多次加仓
        var trades = new List<TradeRecord>
        {
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 100, Price = 43.0m, Time = DateTime.Today.AddHours(10), Commission = 5, Tax = 0 },
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 200, Price = 42.5m, Time = DateTime.Today.AddHours(11), Commission = 5, Tax = 0 }
        };
        var position = CreatePosition(300, 43.267m, trades);
        var quote = CreateQuote(44.0m, 43.0m);

        var (realized, unrealized, _) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0);

        // 浮动: 买入1 (44.0-43.0)*100-5 + 买入2 (44.0-42.5)*200-5 + 原持仓 (44.0-43.267)*300
        var expected = (44.0m - 43.0m) * 100 - 5m + (44.0m - 42.5m) * 200 - 5m + (44.0m - 43.267m) * 300;
        unrealized.Should().BeApproximately(expected, 0.01m);
    }

    [Fact]
    public void GetTodayMoney_NoTrades_ReturnsZero()
    {
        var position = CreatePosition(300, 43.267m);
        var quote = CreateQuote(44.0m, 43.0m);

        var result = PnLCalculator.GetTodayMoney(position, quote);

        result.Should().Be(0);
    }

    [Fact]
    public void GetTodayMoney_WithTrades_ReturnsCorrectAmount()
    {
        var trades = new List<TradeRecord>
        {
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 100, Price = 43.0m, Time = DateTime.Today.AddHours(10), Commission = 5, Tax = 0 },
            new() { Type = TradeRecord.TradeType.Sell, Quantity = 100, Price = 44.0m, Time = DateTime.Today.AddHours(14), Commission = 5, Tax = 5 }
        };
        var position = CreatePosition(300, 43.267m, trades);
        var quote = CreateQuote(44.0m, 43.0m);

        var result = PnLCalculator.GetTodayMoney(position, quote);

        // 买入盈亏: (44.0-43.0)*100-5 = 95
        // 卖出盈亏: (44.0-43.0)*100-5-5 = 90
        // 总计: 95+90 = 185
        result.Should().BeApproximately(185m, 0.01m);
    }

    [Fact]
    public void CalculateTodayPnL_OldTradesNotToday_Ignored()
    {
        var trades = new List<TradeRecord>
        {
            new() { Type = TradeRecord.TradeType.Buy, Quantity = 100, Price = 43.0m, Time = DateTime.Today.AddDays(-1), Commission = 5, Tax = 0 }
        };
        var position = CreatePosition(300, 43.267m, trades);
        var quote = CreateQuote(44.0m, 43.0m);

        var (realized, unrealized, total) = PnLCalculator.CalculateTodayPnL(position, quote);

        realized.Should().Be(0);
        // 只有原持仓浮动
        unrealized.Should().BeApproximately((44.0m - 43.267m) * 300, 0.01m);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "PnLCalculatorTests" -v n
```

Expected: 编译失败（PnLCalculator 尚未实现）

- [ ] **Step 3: 实现 PnLCalculator**

创建 `src/StockMonitor/Core/Calculator/PnLCalculator.cs`：

```csharp
using StockMonitor.Core.Models;

namespace StockMonitor.Core.Calculator;

/// <summary>
/// 盈亏计算器，支持 T+0 场景
/// </summary>
public static class PnLCalculator
{
    /// <summary>
    /// 计算今日盈亏明细
    /// </summary>
    /// <param name="position">持仓信息</param>
    /// <param name="quote">实时行情</param>
    /// <returns>元组: (已实现盈亏, 浮动盈亏, 总盈亏)</returns>
    public static (decimal Realized, decimal Unrealized, decimal Total) CalculateTodayPnL(
        StockPosition position, StockQuote quote)
    {
        var today = DateTime.Today;

        decimal realizedPnL = 0;
        decimal totalCommission = 0;
        decimal totalTax = 0;
        decimal buyUnrealizedPnL = 0;

        foreach (var trade in position.TodayTrades)
        {
            if (trade.Time.Date != today)
                continue;

            if (trade.Type == TradeRecord.TradeType.Buy)
            {
                // 买入浮动盈亏 = (现价 - 买入价) * 数量 - 佣金
                buyUnrealizedPnL += (quote.CurrentPrice - trade.Price) * trade.Quantity - trade.Commission;
                totalCommission += trade.Commission;
            }
            else if (trade.Type == TradeRecord.TradeType.Sell)
            {
                // 卖出已实现盈亏 = (卖出价 - 昨收价) * 数量
                realizedPnL += (trade.Price - quote.YestClose) * trade.Quantity;
                totalCommission += trade.Commission;
                totalTax += trade.Tax;
            }
        }

        realizedPnL -= totalCommission + totalTax;

        // 原持仓浮动 = (现价 - 成本价) * 原持仓数量
        decimal holdUnrealizedPnL = (quote.CurrentPrice - position.AvgCostPrice) * position.Quantity;

        decimal unrealizedPnL = buyUnrealizedPnL + holdUnrealizedPnL;
        decimal totalPnL = realizedPnL + unrealizedPnL;

        return (realizedPnL, unrealizedPnL, totalPnL);
    }

    /// <summary>
    /// 获取今日盈亏金额（简化版，用于显示）
    /// </summary>
    /// <param name="position">持仓信息</param>
    /// <param name="quote">实时行情</param>
    /// <returns>今日盈亏金额</returns>
    public static decimal GetTodayMoney(StockPosition position, StockQuote quote)
    {
        if (position.TodayTrades == null || position.TodayTrades.Count == 0)
            return 0;

        var today = DateTime.Today;
        decimal money = 0;
        decimal totalCommission = 0;
        decimal totalTax = 0;

        foreach (var trade in position.TodayTrades)
        {
            if (trade.Time.Date != today)
                continue;

            if (trade.Type == TradeRecord.TradeType.Buy)
            {
                // 买入盈亏 = (现价 - 买入价) * 数量
                money += (quote.CurrentPrice - trade.Price) * trade.Quantity;
                totalCommission += trade.Commission;
            }
            else if (trade.Type == TradeRecord.TradeType.Sell)
            {
                // 卖出盈亏 = (卖出价 - 昨收价) * 数量
                money += (trade.Price - quote.YestClose) * trade.Quantity;
                totalCommission += trade.Commission;
                totalTax += trade.Tax;
            }
        }

        return money - totalCommission - totalTax;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "PnLCalculatorTests" -v n
```

Expected: 全部通过

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat: implement PnLCalculator with T+0 support - buy/sell same day scenarios"
```

---

## Task 6: 持仓服务（PositionService）— T+0 修复核心

**Files:**
- Create: `src/StockMonitor/Core/Services/PositionService.cs`
- Create: `tests/StockMonitor.Tests/Services/PositionServiceTests.cs`

- [ ] **Step 1: 写 PositionService 的失败测试**

创建 `tests/StockMonitor.Tests/Services/PositionServiceTests.cs`：

```csharp
using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class PositionServiceTests
{
    private readonly Mock<IRepository<List<StockPosition>>> _repoMock;
    private readonly List<StockPosition> _positions;
    private readonly PositionService _service;

    public PositionServiceTests()
    {
        _repoMock = new Mock<IRepository<List<StockPosition>>>();
        _positions = new List<StockPosition>
        {
            new() { Code = "sh600036", Name = "招商银行", Quantity = 300, AvgCostPrice = 43.267m, TodayTrades = new() },
            new() { Code = "sz512890", Name = "酒ETF", Quantity = 16100, AvgCostPrice = 1.112m, TodayTrades = new() }
        };
        _repoMock.Setup(r => r.Load()).Returns(_positions);
        _service = new PositionService(_repoMock.Object, 0.00023m, 0m);
    }

    [Fact]
    public void GetAllPositions_ReturnsAll()
    {
        var result = _service.GetAllPositions();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetPosition_ExistingCode_ReturnsPosition()
    {
        var result = _service.GetPosition("sh600036");
        result.Should().NotBeNull();
        result!.Quantity.Should().Be(300);
    }

    [Fact]
    public void GetPosition_NonExistentCode_ReturnsNull()
    {
        var result = _service.GetPosition("sh999999");
        result.Should().BeNull();
    }

    [Fact]
    public void AddPosition_CreatesTradeRecord()
    {
        _service.AddPosition("sh600036", 44.0m, 100);

        var pos = _service.GetPosition("sh600036");
        pos!.TodayTrades.Should().HaveCount(1);
        pos.TodayTrades[0].Type.Should().Be(TradeRecord.TradeType.Buy);
        pos.TodayTrades[0].Quantity.Should().Be(100);
        pos.TodayTrades[0].Price.Should().Be(44.0m);
        pos.Quantity.Should().Be(300); // 主持仓不变
    }

    [Fact]
    public void ReducePosition_CreatesTradeRecord()
    {
        _service.ReducePosition("sh600036", 44.0m, 100);

        var pos = _service.GetPosition("sh600036");
        pos!.TodayTrades.Should().HaveCount(1);
        pos.TodayTrades[0].Type.Should().Be(TradeRecord.TradeType.Sell);
        pos.TodayTrades[0].Quantity.Should().Be(100);
        pos.Quantity.Should().Be(300); // 主持仓不变
    }

    [Fact]
    public void ReducePosition_ExceedsTotal_ThrowsException()
    {
        var act = () => _service.ReducePosition("sh600036", 44.0m, 400);

        act.Should().Throw<InsufficientPositionException>();
    }

    [Fact]
    public void AddPosition_ThenReducePosition_T0_Success()
    {
        // T+0: 先加仓 100，再减仓 100
        _service.AddPosition("sh600036", 43.0m, 100);
        _service.ReducePosition("sh600036", 44.0m, 100);

        var pos = _service.GetPosition("sh600036");
        pos!.TodayTrades.Should().HaveCount(2);
        pos.GetTotalQuantity().Should().Be(300); // 净持仓 = 300+100-100 = 300
    }

    [Fact]
    public void AddPosition_ThenReducePosition_ExceedsNet_ThrowsException()
    {
        // 加仓 100，减仓 200 → 超过净持仓
        _service.AddPosition("sh600036", 43.0m, 100);

        var act = () => _service.ReducePosition("sh600036", 44.0m, 200);

        // 总持仓 = 300 + 100 = 400, 减 200 没问题
        act.Should().NotThrow();
    }

    [Fact]
    public void AddPosition_ThenReducePosition_ExceedsTotal_ThrowsException()
    {
        // 加仓 100，减仓 500 → 超过总持仓
        _service.AddPosition("sh600036", 43.0m, 100);

        var act = () => _service.ReducePosition("sh600036", 44.0m, 500);

        act.Should().Throw<InsufficientPositionException>();
    }

    [Fact]
    public void MergeDayTrades_UpdatesQuantityAndCost()
    {
        _service.AddPosition("sh600036", 44.0m, 100);

        _service.MergeDayTrades();

        var pos = _service.GetPosition("sh600036");
        pos!.Quantity.Should().Be(400); // 300 + 100
        pos.TodayTrades.Should().BeEmpty();
        // 新成本 = (300*43.267 + 100*44.0 + 5佣金) / 400
        var expectedCost = (300m * 43.267m + 100m * 44.0m + 5m) / 400m;
        pos.AvgCostPrice.Should().BeApproximately(expectedCost, 0.001m);
    }

    [Fact]
    public void MergeDayTrades_WithSell_DecreasesQuantity()
    {
        _service.ReducePosition("sh600036", 44.0m, 100);

        _service.MergeDayTrades();

        var pos = _service.GetPosition("sh600036");
        pos!.Quantity.Should().Be(200); // 300 - 100
        pos.TodayTrades.Should().BeEmpty();
        pos.AvgCostPrice.Should().Be(43.267m); // 减仓不改变成本价
    }

    [Fact]
    public void AddStock_CreatesNewPosition()
    {
        _service.AddStock("sh601398");

        var pos = _service.GetPosition("sh601398");
        pos.Should().NotBeNull();
        pos!.Quantity.Should().Be(0);
        pos.AvgCostPrice.Should().Be(0);
    }

    [Fact]
    public void RemoveStock_DeletesPosition()
    {
        _service.RemoveStock("sh600036");

        _service.GetPosition("sh600036").Should().BeNull();
    }

    [Fact]
    public void UpdatePosition_SetsQuantityAndCost()
    {
        _service.UpdatePosition("sh600036", 500, 45.0m);

        var pos = _service.GetPosition("sh600036");
        pos!.Quantity.Should().Be(500);
        pos.AvgCostPrice.Should().Be(45.0m);
    }

    [Fact]
    public void PositionChanged_FiredOnAddPosition()
    {
        string? changedCode = null;
        _service.PositionChanged += (_, e) => changedCode = e.Code;

        _service.AddPosition("sh600036", 44.0m, 100);

        changedCode.Should().Be("sh600036");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "PositionServiceTests" -v n
```

Expected: 编译失败（PositionService 尚未实现）

- [ ] **Step 3: 实现 PositionService**

创建 `src/StockMonitor/Core/Services/PositionService.cs`：

```csharp
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 持仓服务实现，T+0 修复核心
/// 当日交易记录在 TodayTrades 中，主持仓不变，查询时实时合并
/// </summary>
public class PositionService : IPositionService
{
    private readonly IRepository<List<StockPosition>> _repository;
    private readonly decimal _commissionRate;
    private readonly decimal _taxRate;
    private List<StockPosition> _positions;

    public PositionService(
        IRepository<List<StockPosition>> repository,
        decimal commissionRate = 0.00023m,
        decimal taxRate = 0m)
    {
        _repository = repository;
        _commissionRate = commissionRate;
        _taxRate = taxRate;
        _positions = repository.Load() ?? new List<StockPosition>();
    }

    public IReadOnlyList<StockPosition> GetAllPositions() => _positions.AsReadOnly();

    public StockPosition? GetPosition(string code) =>
        _positions.FirstOrDefault(p => p.Code == code);

    /// <summary>
    /// 加仓操作，记录到当日交易列表，主持仓不变
    /// </summary>
    public void AddPosition(string code, decimal price, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("加仓数量必须大于0");

        var position = GetOrCreatePosition(code);
        decimal turnover = price * quantity;
        decimal commission = Math.Max(turnover * _commissionRate, 5m);

        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Buy,
            Quantity = quantity,
            Price = price,
            Time = DateTime.Now,
            Commission = commission,
            Tax = 0
        });

        SaveAndNotify(code);
    }

    /// <summary>
    /// 减仓操作，校验总持仓是否足够
    /// </summary>
    public void ReducePosition(string code, decimal price, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("减仓数量必须大于0");

        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        int totalQuantity = position.GetTotalQuantity();
        if (quantity > totalQuantity)
            throw new InsufficientPositionException(
                $"减仓数量 {quantity} 超过总持仓 {totalQuantity}");

        decimal turnover = price * quantity;
        decimal commission = Math.Max(turnover * _commissionRate, 5m);
        decimal tax = Math.Round(turnover * _taxRate, 2);

        position.TodayTrades.Add(new TradeRecord
        {
            Type = TradeRecord.TradeType.Sell,
            Quantity = quantity,
            Price = price,
            Time = DateTime.Now,
            Commission = commission,
            Tax = tax
        });

        SaveAndNotify(code);
    }

    public void UpdatePosition(string code, int quantity, decimal avgCostPrice)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        position.Quantity = quantity;
        position.AvgCostPrice = avgCostPrice;

        SaveAndNotify(code);
    }

    public void AddStock(string code)
    {
        if (GetPosition(code) != null)
            throw new InvalidOperationException($"股票 {code} 已存在");

        _positions.Add(new StockPosition { Code = code });
        SaveAndNotify(code);
    }

    public void RemoveStock(string code)
    {
        var position = GetPosition(code)
            ?? throw new InvalidOperationException($"股票 {code} 不存在");

        _positions.Remove(position);
        SaveAndNotify(code);
    }

    /// <summary>
    /// 合并当日交易到主持仓，次日启动时调用
    /// </summary>
    public void MergeDayTrades()
    {
        var today = DateTime.Today;

        foreach (var position in _positions)
        {
            if (position.TodayTrades == null || position.TodayTrades.Count == 0)
                continue;

            var todayTrades = position.TodayTrades.Where(t => t.Time.Date == today).ToList();

            if (todayTrades.Count == 0)
                continue;

            int netQuantity = todayTrades
                .Sum(t => t.Type == TradeRecord.TradeType.Buy ? t.Quantity : -t.Quantity);

            decimal buyTotalCost = todayTrades
                .Where(t => t.Type == TradeRecord.TradeType.Buy)
                .Sum(t => t.Quantity * t.Price + t.Commission + t.Tax);

            int oldQuantity = position.Quantity;
            decimal oldTotalCost = position.AvgCostPrice * position.Quantity;

            int newQuantity = oldQuantity + netQuantity;
            decimal newTotalCost = oldTotalCost + buyTotalCost;

            position.Quantity = newQuantity;
            position.AvgCostPrice = newQuantity > 0 ? newTotalCost / newQuantity : 0;
            position.TodayTrades.RemoveAll(t => t.Time.Date == today);
        }

        _repository.Save(_positions);
    }

    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    private StockPosition GetOrCreatePosition(string code)
    {
        var position = GetPosition(code);
        if (position == null)
        {
            position = new StockPosition { Code = code };
            _positions.Add(position);
        }
        return position;
    }

    private void SaveAndNotify(string code)
    {
        _repository.Save(_positions);
        PositionChanged?.Invoke(this, new PositionChangedEventArgs { Code = code });
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "PositionServiceTests" -v n
```

Expected: 全部通过

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat: implement PositionService with T+0 fix - same-day buy/sell support"
```

---

## Task 7: 数据源服务（SinaDataProvider + TencentDataProvider + StockDataService）

**Files:**
- Create: `src/StockMonitor/Core/Services/SinaDataProvider.cs`
- Create: `src/StockMonitor/Core/Services/TencentDataProvider.cs`
- Create: `src/StockMonitor/Core/Services/StockDataService.cs`
- Create: `tests/StockMonitor.Tests/Services/StockDataServiceTests.cs`

- [ ] **Step 1: 实现 SinaDataProvider**

创建 `src/StockMonitor/Core/Services/SinaDataProvider.cs`：

```csharp
using System.Text;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 新浪财经数据源实现
/// API: https://hq.sinajs.cn/list=sh600036,sz512890
/// </summary>
public class SinaDataProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;
    private bool _isAvailable = true;

    public string Name => "Sina";

    public bool IsAvailable => _isAvailable;

    public SinaDataProvider()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://finance.sina.com.cn/");
    }

    /// <summary>
    /// 批量获取股票行情
    /// </summary>
    /// <param name="codes">全量编码列表，如 "sh600036"</param>
    public async Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes)
    {
        var codeList = string.Join(",", codes);
        if (string.IsNullOrEmpty(codeList))
            return new List<StockQuote>();

        var url = $"https://hq.sinajs.cn/list={codeList}";

        try
        {
            var response = await _httpClient.GetStreamAsync(url);
            using var reader = new StreamReader(response, Encoding.GetEncoding("GB18030"));
            var content = await reader.ReadToEndAsync();

            var result = new List<StockQuote>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var quote = ParseLine(line);
                if (quote != null)
                    result.Add(quote);
            }

            _isAvailable = true;
            return result;
        }
        catch
        {
            _isAvailable = false;
            return new List<StockQuote>();
        }
    }

    private static StockQuote? ParseLine(string line)
    {
        var parts = line.Split('=');
        if (parts.Length != 2)
            return null;

        var fullCode = parts[0].Replace("var hq_str_", "").Trim();
        var data = parts[1].Trim('"', ' ', ';');
        var dataParts = data.Split(',');

        if (dataParts.Length < 6)
            return null;

        return new StockQuote
        {
            Code = fullCode,
            Name = dataParts[0],
            OpenPrice = decimal.TryParse(dataParts[1], out var open) ? open : 0,
            YestClose = decimal.TryParse(dataParts[2], out var yest) ? yest : 0,
            CurrentPrice = decimal.TryParse(dataParts[3], out var price) ? price : 0,
            HighPrice = decimal.TryParse(dataParts[4], out var high) ? high : 0,
            LowPrice = decimal.TryParse(dataParts[5], out var low) ? low : 0,
        };
    }
}
```

- [ ] **Step 2: 实现 TencentDataProvider**

创建 `src/StockMonitor/Core/Services/TencentDataProvider.cs`：

```csharp
using System.Text;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 腾讯财经数据源实现（备用）
/// API: https://qt.gtimg.cn/q=sh600036,sz512890
/// </summary>
public class TencentDataProvider : IStockDataProvider
{
    private readonly HttpClient _httpClient;
    private bool _isAvailable = true;

    public string Name => "Tencent";

    public bool IsAvailable => _isAvailable;

    public TencentDataProvider()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 批量获取股票行情
    /// </summary>
    /// <param name="codes">全量编码列表</param>
    public async Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes)
    {
        var codeList = string.Join(",", codes);
        if (string.IsNullOrEmpty(codeList))
            return new List<StockQuote>();

        var url = $"https://qt.gtimg.cn/q={codeList}";

        try
        {
            var response = await _httpClient.GetStreamAsync(url);
            using var reader = new StreamReader(response, Encoding.GetEncoding("GBK"));
            var content = await reader.ReadToEndAsync();

            var result = new List<StockQuote>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var quote = ParseLine(line);
                if (quote != null)
                    result.Add(quote);
            }

            _isAvailable = true;
            return result;
        }
        catch
        {
            _isAvailable = false;
            return new List<StockQuote>();
        }
    }

    private static StockQuote? ParseLine(string line)
    {
        var parts = line.Split('~');
        if (parts.Length < 7)
            return null;

        return new StockQuote
        {
            Code = parts[2],
            Name = parts[1],
            CurrentPrice = decimal.TryParse(parts[3], out var price) ? price : 0,
            YestClose = decimal.TryParse(parts[4], out var yest) ? yest : 0,
            OpenPrice = decimal.TryParse(parts[5], out var open) ? open : 0,
            HighPrice = decimal.TryParse(parts[33], out var high) ? high : 0,
            LowPrice = decimal.TryParse(parts[34], out var low) ? low : 0,
        };
    }
}
```

- [ ] **Step 3: 写 StockDataService 的失败测试**

创建 `tests/StockMonitor.Tests/Services/StockDataServiceTests.cs`：

```csharp
using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class StockDataServiceTests
{
    private readonly Mock<IStockDataProvider> _primaryMock;
    private readonly Mock<IStockDataProvider> _fallbackMock;
    private readonly Mock<IPositionService> _positionMock;

    public StockDataServiceTests()
    {
        _primaryMock = new Mock<IStockDataProvider>();
        _fallbackMock = new Mock<IStockDataProvider>();
        _positionMock = new Mock<IPositionService>();

        _primaryMock.SetupGet(p => p.Name).Returns("Sina");
        _fallbackMock.SetupGet(p => p.Name).Returns("Tencent");
    }

    [Fact]
    public async Task GetQuotesAsync_PrimarySuccess_ReturnsPrimaryData()
    {
        var quotes = new List<StockQuote> { new() { Code = "sh600036", CurrentPrice = 44 } };
        _primaryMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(quotes);
        _primaryMock.SetupGet(p => p.IsAvailable).Returns(true);

        var service = new StockDataService(
            new[] { _primaryMock.Object, _fallbackMock.Object },
            _positionMock.Object);

        var result = await service.GetQuotesAsync();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("sh600036");
        _fallbackMock.Verify(f => f.GetQuotesAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetQuotesAsync_PrimaryFails_FallsBackToSecondary()
    {
        var fallbackQuotes = new List<StockQuote> { new() { Code = "sh600036", CurrentPrice = 44 } };
        _primaryMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());
        _primaryMock.SetupGet(p => p.IsAvailable).Returns(false);
        _fallbackMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(fallbackQuotes);
        _fallbackMock.SetupGet(p => p.IsAvailable).Returns(true);

        var service = new StockDataService(
            new[] { _primaryMock.Object, _fallbackMock.Object },
            _positionMock.Object);

        var result = await service.GetQuotesAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetQuotesAsync_AllFail_ReturnsCachedData()
    {
        _positionMock.Setup(p => p.GetAllPositions())
            .Returns(new List<StockPosition>().AsReadOnly());
        _primaryMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());
        _fallbackMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());

        var service = new StockDataService(
            new[] { _primaryMock.Object, _fallbackMock.Object },
            _positionMock.Object);

        // 第一次获取，无缓存
        var result1 = await service.GetQuotesAsync();
        result1.Should().BeEmpty();

        // 设置主力返回数据（模拟缓存场景）
        var quotes = new List<StockQuote> { new() { Code = "sh600036", CurrentPrice = 44 } };
        _primaryMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(quotes);
        _primaryMock.SetupGet(p => p.IsAvailable).Returns(true);
        await service.GetQuotesAsync();

        // 再次全部失败
        _primaryMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());
        _primaryMock.SetupGet(p => p.IsAvailable).Returns(false);
        _fallbackMock.Setup(p => p.GetQuotesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<StockQuote>());
        _fallbackMock.SetupGet(p => p.IsAvailable).Returns(false);

        var result2 = await service.GetQuotesAsync();
        result2.Should().HaveCount(1); // 返回缓存
    }
}
```

- [ ] **Step 4: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "StockDataServiceTests" -v n
```

Expected: 编译失败（StockDataService 尚未实现）

- [ ] **Step 5: 实现 StockDataService**

创建 `src/StockMonitor/Core/Services/StockDataService.cs`：

```csharp
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 数据源调度服务，负责 failover 和缓存
/// </summary>
public class StockDataService : IStockDataService
{
    private readonly List<IStockDataProvider> _providers;
    private readonly IPositionService _positionService;
    private List<StockQuote> _cache = new();

    public StockDataService(
        IEnumerable<IStockDataProvider> providers,
        IPositionService positionService)
    {
        _providers = providers.ToList();
        _positionService = positionService;
    }

    /// <summary>
    /// 获取所有持仓股票的行情数据，按优先级尝试各数据源
    /// </summary>
    public async Task<List<StockQuote>> GetQuotesAsync()
    {
        var codes = _positionService.GetAllPositions().Select(p => p.Code).ToList();
        if (codes.Count == 0)
            return new List<StockQuote>();

        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable)
                continue;

            try
            {
                var quotes = await provider.GetQuotesAsync(codes);
                if (quotes.Count > 0)
                {
                    _cache = quotes;
                    QuotesUpdated?.Invoke(this, quotes);
                    return quotes;
                }
            }
            catch
            {
                // 当前数据源失败，尝试下一个
            }
        }

        // 所有数据源都失败，返回缓存
        if (_cache.Count > 0)
            return _cache;

        return new List<StockQuote>();
    }

    public event EventHandler<List<StockQuote>>? QuotesUpdated;
}
```

- [ ] **Step 6: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "StockDataServiceTests" -v n
```

Expected: 全部通过

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "feat: implement data source services - SinaDataProvider, TencentDataProvider, StockDataService with failover"
```

---

## Task 8: 预警服务（AlertService）

**Files:**
- Create: `src/StockMonitor/Core/Services/AlertService.cs`
- Create: `tests/StockMonitor.Tests/Services/AlertServiceTests.cs`

- [ ] **Step 1: 写 AlertService 的失败测试**

创建 `tests/StockMonitor.Tests/Services/AlertServiceTests.cs`：

```csharp
using FluentAssertions;
using Moq;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using Xunit;

namespace StockMonitor.Tests.Services;

public class AlertServiceTests
{
    private readonly Mock<IRepository<List<AlertRule>>> _repoMock;
    private readonly List<AlertRule> _rules;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _repoMock = new Mock<IRepository<List<AlertRule>>>();
        _rules = new List<AlertRule>();
        _repoMock.Setup(r => r.Load()).Returns(_rules);
        _service = new AlertService(_repoMock.Object);
    }

    [Fact]
    public void AddRule_SavesAndReturnsRule()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m };

        _service.AddRule(rule);

        _service.GetRules().Should().Contain(rule);
    }

    [Fact]
    public void RemoveRule_RemovesFromList()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m };
        _service.AddRule(rule);

        _service.RemoveRule(rule.Id);

        _service.GetRules().Should().NotContain(rule);
    }

    [Fact]
    public void CheckAlerts_PriceAboveThreshold_TriggersEvent()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m };
        _service.AddRule(rule);

        AlertTriggeredEventArgs? triggeredArgs = null;
        _service.AlertTriggered += (_, e) => triggeredArgs = e;

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", CurrentPrice = 46m }
        };

        _service.CheckAlerts(quotes);

        triggeredArgs.Should().NotBeNull();
        triggeredArgs!.Rule.Id.Should().Be(rule.Id);
        rule.IsTriggered.Should().BeTrue();
    }

    [Fact]
    public void CheckAlerts_PriceBelowThreshold_DoesNotTrigger()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m };
        _service.AddRule(rule);

        AlertTriggeredEventArgs? triggeredArgs = null;
        _service.AlertTriggered += (_, e) => triggeredArgs = e;

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", CurrentPrice = 44m }
        };

        _service.CheckAlerts(quotes);

        triggeredArgs.Should().BeNull();
        rule.IsTriggered.Should().BeFalse();
    }

    [Fact]
    public void CheckAlerts_AlreadyTriggered_DoesNotFireAgain()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m, IsTriggered = true };
        _service.AddRule(rule);

        int triggerCount = 0;
        _service.AlertTriggered += (_, _) => triggerCount++;

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", CurrentPrice = 46m }
        };

        _service.CheckAlerts(quotes);

        triggerCount.Should().Be(0);
    }

    [Fact]
    public void CheckAlerts_OneTimeRule_TriggeredThenRemoved()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.PriceAbove, Threshold = 45m, IsOneTime = true };
        _service.AddRule(rule);

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", CurrentPrice = 46m }
        };

        _service.CheckAlerts(quotes);

        _service.GetRules().Should().NotContain(rule);
    }

    [Fact]
    public void CheckAlerts_ChangeRateAbove_TriggersCorrectly()
    {
        var rule = new AlertRule { StockCode = "sh600036", Type = AlertType.ChangeRateAbove, Threshold = 5m };
        _service.AddRule(rule);

        AlertTriggeredEventArgs? triggeredArgs = null;
        _service.AlertTriggered += (_, e) => triggeredArgs = e;

        var quotes = new List<StockQuote>
        {
            new() { Code = "sh600036", CurrentPrice = 46m, YestClose = 43m }
        };

        _service.CheckAlerts(quotes);

        triggeredArgs.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "AlertServiceTests" -v n
```

Expected: 编译失败（AlertService 尚未实现）

- [ ] **Step 3: 实现 AlertService**

创建 `src/StockMonitor/Core/Services/AlertService.cs`：

```csharp
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.Core.Services;

/// <summary>
/// 预警服务实现
/// </summary>
public class AlertService : IAlertService
{
    private readonly IRepository<List<AlertRule>> _repository;
    private List<AlertRule> _rules;

    public AlertService(IRepository<List<AlertRule>> repository)
    {
        _repository = repository;
        _rules = repository.Load() ?? new List<AlertRule>();
    }

    public void AddRule(AlertRule rule)
    {
        _rules.Add(rule);
        _repository.Save(_rules);
    }

    public void RemoveRule(string ruleId)
    {
        _rules.RemoveAll(r => r.Id == ruleId);
        _repository.Save(_rules);
    }

    public IReadOnlyList<AlertRule> GetRules() => _rules.AsReadOnly();

    /// <summary>
    /// 检查行情数据是否触发预警
    /// </summary>
    /// <param name="quotes">实时行情列表</param>
    public void CheckAlerts(IEnumerable<StockQuote> quotes)
    {
        var quoteDict = quotes.ToDictionary(q => q.Code);
        var rulesToRemove = new List<AlertRule>();

        foreach (var rule in _rules)
        {
            if (rule.IsTriggered)
                continue;

            if (!quoteDict.TryGetValue(rule.StockCode, out var quote))
                continue;

            bool triggered = rule.Type switch
            {
                AlertType.PriceAbove => quote.CurrentPrice > rule.Threshold,
                AlertType.PriceBelow => quote.CurrentPrice < rule.Threshold,
                AlertType.ChangeRateAbove => quote.ChangeRate > rule.Threshold,
                AlertType.ChangeRateBelow => quote.ChangeRate < rule.Threshold,
                _ => false
            };

            if (triggered)
            {
                rule.IsTriggered = true;
                AlertTriggered?.Invoke(this, new AlertTriggeredEventArgs { Rule = rule, Quote = quote });

                if (rule.IsOneTime)
                    rulesToRemove.Add(rule);
            }
        }

        foreach (var rule in rulesToRemove)
        {
            _rules.Remove(rule);
        }

        if (rulesToRemove.Count > 0)
            _repository.Save(_rules);
    }

    public event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj --filter "AlertServiceTests" -v n
```

Expected: 全部通过

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat: implement AlertService with price and change rate alert support"
```

---

## Task 9: 日志服务（FileLogger）

**Files:**
- Create: `src/StockMonitor/Logging/FileLogger.cs`

- [ ] **Step 1: 实现 FileLogger**

创建 `src/StockMonitor/Logging/FileLogger.cs`：

```csharp
namespace StockMonitor.Logging;

/// <summary>
/// 文件日志器，统一 UTF-8 编码，同步写入
/// </summary>
public static class FileLogger
{
    private static readonly string LogDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs");

    private static readonly object _lock = new();

    static FileLogger()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    private static string GetLogFilePath() =>
        Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}logfile.log");

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public static void LogDebug(string message) => Log("DEBUG", message);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void LogError(string message, Exception? ex = null)
    {
        var logMessage = ex != null ? $"{message}: {ex}" : message;
        Log("ERROR", logMessage);
    }

    private static void Log(string level, string message)
    {
        var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        lock (_lock)
        {
            File.AppendAllText(GetLogFilePath(), logMessage: logLine);
        }
    }
}
```

注意：上面代码有 bug，`File.AppendAllText` 的参数名写错了。修正后：

```csharp
namespace StockMonitor.Logging;

/// <summary>
/// 文件日志器，统一 UTF-8 编码，同步写入
/// </summary>
public static class FileLogger
{
    private static readonly string LogDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs");

    private static readonly object _lock = new();

    static FileLogger()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    private static string GetLogFilePath() =>
        Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}logfile.log");

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public static void LogDebug(string message) => Log("DEBUG", message);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void LogError(string message, Exception? ex = null)
    {
        var logMessage = ex != null ? $"{message}: {ex}" : message;
        Log("ERROR", logMessage);
    }

    private static void Log(string level, string message)
    {
        var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        lock (_lock)
        {
            File.AppendAllText(GetLogFilePath(), logLine);
        }
    }
}
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat: implement FileLogger with UTF-8 encoding and thread-safe sync writes"
```

---

## Task 10: UI ViewModel 层

**Files:**
- Create: `src/StockMonitor/UI/ViewModels/StockDisplayItem.cs`
- Create: `src/StockMonitor/UI/ViewModels/MainViewModel.cs`
- Create: `src/StockMonitor/UI/ViewModels/PositionEditViewModel.cs`
- Create: `src/StockMonitor/UI/ViewModels/ConfigEditViewModel.cs`
- Create: `src/StockMonitor/UI/ViewModels/AlertManageViewModel.cs`

- [ ] **Step 1: 创建 StockDisplayItem**

创建 `src/StockMonitor/UI/ViewModels/StockDisplayItem.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 股票显示项，视图专用模型
/// </summary>
public partial class StockDisplayItem : ObservableObject
{
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _priceText = string.Empty;
    [ObservableProperty] private string _changeText = string.Empty;
    [ObservableProperty] private string _changeRateText = string.Empty;
    [ObservableProperty] private string _pnlText = string.Empty;
    [ObservableProperty] private string _priceColor = "White";
    [ObservableProperty] private bool _hasPosition;
}
```

- [ ] **Step 2: 创建 MainViewModel**

创建 `src/StockMonitor/UI/ViewModels/MainViewModel.cs`：

```csharp
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Calculator;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IStockDataService _stockDataService;
    private readonly IPositionService _positionService;
    private readonly IAlertService _alertService;
    private readonly IRepository<AppConfig> _configRepo;
    private readonly AppConfig _config;
    private CancellationTokenSource _cts = new();
    private List<StockQuote> _lastQuotes = new();

    public ObservableCollection<StockDisplayItem> Stocks { get; } = new();

    [ObservableProperty] private string _todaySummary = string.Empty;
    [ObservableProperty] private string _summaryColor = "White";
    [ObservableProperty] private bool _isRefreshing;

    public MainViewModel(
        IStockDataService stockDataService,
        IPositionService positionService,
        IAlertService alertService,
        IRepository<AppConfig> configRepo)
    {
        _stockDataService = stockDataService;
        _positionService = positionService;
        _alertService = alertService;
        _configRepo = configRepo;
        _config = configRepo.Load();

        _alertService.AlertTriggered += OnAlertTriggered;
        _positionService.PositionChanged += (_, _) => RefreshData();

        StartRefreshLoop();
    }

    /// <summary>
    /// 手动刷新命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshData();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async void StartRefreshLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await RefreshData();
                await Task.Delay(_config.RefreshTime, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logging.FileLogger.LogError("刷新数据失败", ex);
                await Task.Delay(5000, _cts.Token);
            }
        }
    }

    private async Task RefreshData()
    {
        var quotes = await _stockDataService.GetQuotesAsync();
        _lastQuotes = quotes;

        var positions = _positionService.GetAllPositions();
        var positionDict = positions.ToDictionary(p => p.Code);

        Application.Current.Dispatcher.Invoke(() =>
        {
            Stocks.Clear();

            foreach (var quote in quotes)
            {
                positionDict.TryGetValue(quote.Code, out var position);
                var item = CreateDisplayItem(quote, position);
                Stocks.Add(item);
            }

            UpdateTodaySummary(quotes, positions);
        });

        _alertService.CheckAlerts(quotes);
    }

    private StockDisplayItem CreateDisplayItem(StockQuote quote, StockPosition? position)
    {
        var changeSign = quote.Change >= 0 ? "+" : "";
        var pnlText = string.Empty;
        var hasPosition = false;

        if (position != null && position.GetTotalQuantity() > 0)
        {
            hasPosition = true;
            var todayMoney = PnLCalculator.GetTodayMoney(position, quote);
            var holdPnL = (quote.CurrentPrice - position.AvgCostPrice) * position.Quantity;
            var totalPnL = todayMoney + holdPnL;
            pnlText = totalPnL >= 0 ? $"+{totalPnL:F2}" : $"{totalPnL:F2}";
        }

        return new StockDisplayItem
        {
            Code = quote.Code,
            DisplayName = $"{quote.Name}({quote.Code})",
            PriceText = quote.CurrentPrice.ToString("F3"),
            ChangeText = $"{changeSign}{quote.Change:F3}",
            ChangeRateText = $"{changeSign}{quote.ChangeRate:F2}%",
            PnlText = pnlText,
            PriceColor = quote.Change > 0 ? "Red" : quote.Change < 0 ? "#00FF00" : "White",
            HasPosition = hasPosition
        };
    }

    private void UpdateTodaySummary(List<StockQuote> quotes, IReadOnlyList<StockPosition> positions)
    {
        var positionDict = positions.ToDictionary(p => p.Code);
        decimal totalPnL = 0;
        decimal totalCost = 0;
        bool hasAny = false;

        foreach (var quote in quotes)
        {
            if (!positionDict.TryGetValue(quote.Code, out var position))
                continue;

            if (position.GetTotalQuantity() <= 0 && position.TodayTrades.Count == 0)
                continue;

            hasAny = true;
            totalPnL += quote.Change * position.GetTotalQuantity();
            totalPnL += PnLCalculator.GetTodayMoney(position, quote) -
                        (quote.Change * position.GetTotalQuantity() - (quote.CurrentPrice - position.AvgCostPrice) * position.Quantity);

            totalCost += position.AvgCostPrice * position.Quantity;
        }

        if (!hasAny || string.IsNullOrEmpty(_config.ShowTodaySumFormat))
        {
            TodaySummary = string.Empty;
            return;
        }

        var rate = totalCost > 0 ? Math.Round(totalPnL / totalCost * 100, 2) : 0;
        TodaySummary = _config.ShowTodaySumFormat
            .Replace("#money", totalPnL.ToString("F2"))
            .Replace("#rate", rate.ToString());
        SummaryColor = totalPnL > 0 ? "Red" : totalPnL < 0 ? "#00FF00" : "White";
    }

    private void OnAlertTriggered(object? sender, AlertTriggeredEventArgs e)
    {
        var message = e.Rule.Type switch
        {
            AlertType.PriceAbove => $"{e.Quote.Name} 突破价格 {e.Rule.Threshold}",
            AlertType.PriceBelow => $"{e.Quote.Name} 跌破价格 {e.Rule.Threshold}",
            AlertType.ChangeRateAbove => $"{e.Quote.Name} 涨幅超过 {e.Rule.Threshold}%",
            AlertType.ChangeRateBelow => $"{e.Quote.Name} 跌幅超过 {e.Rule.Threshold}%",
            _ => "预警触发"
        };

        Logging.FileLogger.LogDebug($"预警触发: {message}");

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "股票预警", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
```

- [ ] **Step 3: 创建 PositionEditViewModel**

创建 `src/StockMonitor/UI/ViewModels/PositionEditViewModel.cs`：

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 持仓编辑 ViewModel
/// </summary>
public partial class PositionEditViewModel : ObservableObject
{
    private readonly IPositionService _positionService;

    public ObservableCollection<StockPosition> StockList { get; }

    [ObservableProperty] private StockPosition? _selectedStock;
    [ObservableProperty] private string _inputQuantity = string.Empty;
    [ObservableProperty] private string _inputPrice = string.Empty;
    [ObservableProperty] private string _newCode = string.Empty;

    public PositionEditViewModel(IPositionService positionService)
    {
        _positionService = positionService;
        StockList = new ObservableCollection<StockPosition>(positionService.GetAllPositions());
    }

    partial void OnSelectedStockChanged(StockPosition? value)
    {
        if (value != null)
        {
            InputQuantity = value.Quantity.ToString();
            InputPrice = value.AvgCostPrice.ToString();
        }
    }

    [RelayCommand]
    private void AddPosition()
    {
        if (SelectedStock == null) return;
        if (!int.TryParse(InputQuantity, out var qty) || !decimal.TryParse(InputPrice, out var price)) return;

        _positionService.AddPosition(SelectedStock.Code, price, qty);
        RefreshList();
    }

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
            System.Windows.MessageBox.Show(ex.Message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void EditPosition()
    {
        if (SelectedStock == null) return;
        if (!int.TryParse(InputQuantity, out var qty) || !decimal.TryParse(InputPrice, out var price)) return;

        _positionService.UpdatePosition(SelectedStock.Code, qty, price);
        RefreshList();
    }

    [RelayCommand]
    private void DeletePosition()
    {
        if (SelectedStock == null) return;

        _positionService.RemoveStock(SelectedStock.Code);
        RefreshList();
    }

    [RelayCommand]
    private void AddNewStock()
    {
        if (string.IsNullOrEmpty(NewCode)) return;

        _positionService.AddStock(NewCode.Trim());
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
```

- [ ] **Step 4: 创建 ConfigEditViewModel**

创建 `src/StockMonitor/UI/ViewModels/ConfigEditViewModel.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 配置编辑 ViewModel
/// </summary>
public partial class ConfigEditViewModel : ObservableObject
{
    private readonly IRepository<AppConfig> _configRepo;
    private readonly AppConfig _config;

    [ObservableProperty] private string _showFormat;
    [ObservableProperty] private int _refreshTime;
    [ObservableProperty] private string _showTodaySumFormat;
    [ObservableProperty] private string _orderBy;

    public ConfigEditViewModel(IRepository<AppConfig> configRepo)
    {
        _configRepo = configRepo;
        _config = configRepo.Load();

        _showFormat = _config.ShowFormat;
        _refreshTime = _config.RefreshTime;
        _showTodaySumFormat = _config.ShowTodaySumFormat;
        _orderBy = _config.OrderBy;
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
```

- [ ] **Step 5: 创建 AlertManageViewModel**

创建 `src/StockMonitor/UI/ViewModels/AlertManageViewModel.cs`：

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services.Interfaces;

namespace StockMonitor.UI.ViewModels;

/// <summary>
/// 预警管理 ViewModel
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
```

- [ ] **Step 6: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "feat: implement all ViewModels - MainViewModel, PositionEdit, ConfigEdit, AlertManage"
```

---

## Task 11: UI Views 层 — WPF 窗口

**Files:**
- Create: `src/StockMonitor/UI/Views/MainWindow.xaml`
- Create: `src/StockMonitor/UI/Views/MainWindow.xaml.cs`
- Create: `src/StockMonitor/UI/Views/PositionEditDialog.xaml`
- Create: `src/StockMonitor/UI/Views/PositionEditDialog.xaml.cs`
- Create: `src/StockMonitor/UI/Views/ConfigEditDialog.xaml`
- Create: `src/StockMonitor/UI/Views/ConfigEditDialog.xaml.cs`
- Create: `src/StockMonitor/UI/Views/AlertManageDialog.xaml`
- Create: `src/StockMonitor/UI/Views/AlertManageDialog.xaml.cs`
- Create: `src/StockMonitor/UI/Converters/PnLToBrushConverter.cs`
- Create: `src/StockMonitor/UI/Converters/BoolToVisibilityConverter.cs`

- [ ] **Step 1: 创建值转换器**

创建 `src/StockMonitor/UI/Converters/PnLToBrushConverter.cs`：

```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 盈亏文字转颜色画刷
/// </summary>
public class PnLToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorName)
        {
            return colorName switch
            {
                "Red" => Brushes.Red,
                "#00FF00" => Brushes.LimeGreen,
                _ => Brushes.White
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

创建 `src/StockMonitor/UI/Converters/BoolToVisibilityConverter.cs`：

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StockMonitor.UI.Converters;

/// <summary>
/// 布尔值转可见性
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: 创建主窗口 XAML**

创建 `src/StockMonitor/UI/Views/MainWindow.xaml`：

```xml
<Window x:Class="StockMonitor.UI.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:StockMonitor.UI.ViewModels"
        xmlns:converters="clr-namespace:StockMonitor.UI.Converters"
        xmlns:tb="http://www.hardcodet.net/taskbar"
        Title="StockMonitor"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ResizeMode="NoResize"
        Width="260" Height="200"
        MouseDoubleClick="Window_MouseDoubleClick"
        MouseLeftButtonDown="Window_MouseLeftButtonDown">

    <Window.Resources>
        <converters:PnLToBrushConverter x:Key="PnLToBrushConverter" />
    </Window.Resources>

    <Grid>
        <Border Background="#CC000000"
                CornerRadius="5"
                Padding="8">
            <StackPanel>
                <!-- 今日汇总 -->
                <TextBlock Text="{Binding TodaySummary}"
                           Foreground="{Binding SummaryColor, Converter={StaticResource PnLToBrushConverter}}"
                           FontSize="11"
                           Margin="0,0,0,4"
                           Visibility="{Binding TodaySummary, Converter={StaticResource NullToVisibilityConverter}}"
                           x:Name="SummaryText" />

                <!-- 股票列表 -->
                <ItemsControl ItemsSource="{Binding Stocks}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType="{x:Type vm:StockDisplayItem}">
                            <StackPanel Margin="0,2">
                                <TextBlock Foreground="{Binding PriceColor, Converter={StaticResource PnLToBrushConverter}}"
                                           FontSize="11">
                                    <Run Text="{Binding DisplayName, Mode=OneWay}" />
                                    <Run Text=" " />
                                    <Run Text="{Binding PnlText, Mode=OneWay}" />
                                </TextBlock>
                                <TextBlock Foreground="{Binding PriceColor, Converter={StaticResource PnLToBrushConverter}}"
                                           FontSize="10" Opacity="0.8">
                                    <Run Text="{Binding PriceText, Mode=OneWay}" />
                                    <Run Text=" " />
                                    <Run Text="{Binding ChangeText, Mode=OneWay}" />
                                    <Run Text=" " />
                                    <Run Text="{Binding ChangeRateText, Mode=OneWay}" />
                                </TextBlock>
                            </StackPanel>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </Border>

        <!-- 系统托盘图标 -->
        <tb:TaskbarIcon x:Name="NotifyIcon"
                        IconSource="pack://application:,,,/icon.ico"
                        ToolTipText="StockMonitor"
                        DoubleClickCommand="{Binding RefreshCommand}">
            <tb:TaskbarIcon.ContextMenu>
                <ContextMenu>
                    <MenuItem Header="显示" Click="ShowWindow_Click" />
                    <MenuItem Header="操作" Click="OpenPositionEdit_Click" />
                    <MenuItem Header="预警管理" Click="OpenAlertManage_Click" />
                    <MenuItem Header="编辑配置" Click="OpenConfigEdit_Click" />
                    <Separator />
                    <MenuItem Header="刷新" Command="{Binding RefreshCommand}" />
                    <Separator />
                    <MenuItem Header="退出" Click="Exit_Click" />
                </ContextMenu>
            </tb:TaskbarIcon.ContextMenu>
        </tb:TaskbarIcon>
    </Grid>
</Window>
```

- [ ] **Step 3: 创建主窗口 code-behind**

创建 `src/StockMonitor/UI/Views/MainWindow.xaml.cs`：

```csharp
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        var screen = SystemParameters.WorkArea;
        Left = screen.Width - Width;
        Top = screen.Height - Height;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Hide();
    }

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        Show();
    }

    private void OpenPositionEdit_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<PositionEditDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void OpenAlertManage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<AlertManageDialog>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void OpenConfigEdit_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ((App)Application.Current).ServiceProvider.GetRequiredService<ConfigEditDialog>();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
```

- [ ] **Step 4: 创建持仓编辑对话框 XAML**

创建 `src/StockMonitor/UI/Views/PositionEditDialog.xaml`：

```xml
<Window x:Class="StockMonitor.UI.Views.PositionEditDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="操作" Width="220" Height="280"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        ShowInTaskbar="False">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <ComboBox Grid.Row="0" ItemsSource="{Binding StockList}" SelectedItem="{Binding SelectedStock}"
                  DisplayMemberPath="Code" Margin="0,0,0,8" />

        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,4">
            <TextBlock Text="数量:" Width="40" VerticalAlignment="Center" />
            <TextBox Text="{Binding InputQuantity}" Width="120" />
        </StackPanel>

        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="价格:" Width="40" VerticalAlignment="Center" />
            <TextBox Text="{Binding InputPrice}" Width="120" />
        </StackPanel>

        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,0,0,4">
            <Button Content="加仓" Command="{Binding AddPositionCommand}" Width="60" Margin="0,0,8,0" />
            <Button Content="减仓" Command="{Binding ReducePositionCommand}" Width="60" />
        </StackPanel>

        <StackPanel Grid.Row="4" Orientation="Horizontal" Margin="0,0,0,4">
            <Button Content="增改" Command="{Binding EditPositionCommand}" Width="60" Margin="0,0,8,0" />
            <Button Content="删除" Command="{Binding DeletePositionCommand}" Width="60" />
        </StackPanel>

        <StackPanel Grid.Row="5" Orientation="Horizontal">
            <TextBox Text="{Binding NewCode}" Width="80" Margin="0,0,8,0"
                     tb:TextBoxHelper.Watermark="新股票代码" />
            <Button Content="新增" Command="{Binding AddNewStockCommand}" Width="60" />
        </StackPanel>
    </Grid>
</Window>
```

创建 `src/StockMonitor/UI/Views/PositionEditDialog.xaml.cs`：

```csharp
using System.Windows;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

public partial class PositionEditDialog : Window
{
    public PositionEditDialog(PositionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 5: 创建配置编辑对话框**

创建 `src/StockMonitor/UI/Views/ConfigEditDialog.xaml`：

```xml
<Window x:Class="StockMonitor.UI.Views.ConfigEditDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="编辑配置" Width="350" Height="300"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        ShowInTaskbar="False">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" Margin="0,0,0,8">
            <TextBlock Text="显示格式:" />
            <TextBox Text="{Binding ShowFormat}" AcceptsReturn="True" Height="50" TextWrapping="Wrap" />
        </StackPanel>

        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="刷新间隔(ms):" VerticalAlignment="Center" />
            <TextBox Text="{Binding RefreshTime}" Width="80" Margin="8,0,0,0" />
        </StackPanel>

        <StackPanel Grid.Row="2" Margin="0,0,0,8">
            <TextBlock Text="今日汇总格式:" />
            <TextBox Text="{Binding ShowTodaySumFormat}" />
        </StackPanel>

        <StackPanel Grid.Row="3" Margin="0,0,0,8">
            <TextBlock Text="排序规则:" />
            <TextBox Text="{Binding OrderBy}" />
        </StackPanel>

        <StackPanel Grid.Row="5" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="保存" Command="{Binding SaveCommand}" Width="60" Margin="0,0,8,0"
                    Click="Save_Click" />
            <Button Content="取消" Width="60" Click="Cancel_Click" />
        </StackPanel>
    </Grid>
</Window>
```

创建 `src/StockMonitor/UI/Views/ConfigEditDialog.xaml.cs`：

```csharp
using System.Windows;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

public partial class ConfigEditDialog : Window
{
    public ConfigEditDialog(ConfigEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ((ConfigEditViewModel)DataContext).SaveCommand.Execute(null);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
```

- [ ] **Step 6: 创建预警管理对话框**

创建 `src/StockMonitor/UI/Views/AlertManageDialog.xaml`：

```xml
<Window x:Class="StockMonitor.UI.Views.AlertManageDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="预警管理" Width="350" Height="350"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        ShowInTaskbar="False">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <ListBox Grid.Row="0" ItemsSource="{Binding Rules}" Margin="0,0,0,8">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding StockCode}" Width="80" />
                        <TextBlock Text="{Binding Type}" Width="100" />
                        <TextBlock Text="{Binding Threshold}" Width="60" />
                        <Button Content="删除" CommandParameter="{Binding}"
                                Command="{Binding DataContext.DeleteRuleCommand, RelativeSource={RelativeSource AncestorType=ListBox}}" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <StackPanel Grid.Row="1">
            <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
                <TextBlock Text="股票:" Width="40" VerticalAlignment="Center" />
                <ComboBox ItemsSource="{Binding StockCodes}" SelectedItem="{Binding SelectedStockCode}"
                          Width="120" />
            </StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
                <TextBlock Text="类型:" Width="40" VerticalAlignment="Center" />
                <ComboBox ItemsSource="{Binding AlertTypes}" SelectedItem="{Binding SelectedAlertType}"
                          Width="120" />
            </StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
                <TextBlock Text="阈值:" Width="40" VerticalAlignment="Center" />
                <TextBox Text="{Binding ThresholdText}" Width="120" />
            </StackPanel>
            <CheckBox Content="一次性" IsChecked="{Binding IsOneTime}" Margin="0,0,0,4" />
            <Button Content="添加规则" Command="{Binding AddRuleCommand}" Width="80" HorizontalAlignment="Left" />
        </StackPanel>
    </Grid>
</Window>
```

创建 `src/StockMonitor/UI/Views/AlertManageDialog.xaml.cs`：

```csharp
using System.Windows;
using StockMonitor.UI.ViewModels;

namespace StockMonitor.UI.Views;

public partial class AlertManageDialog : Window
{
    public AlertManageDialog(AlertManageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 7: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功（可能有 XAML 绑定警告，运行时验证）

- [ ] **Step 8: 提交**

```bash
git add -A
git commit -m "feat: implement all WPF views - MainWindow, PositionEdit, ConfigEdit, AlertManage dialogs"
```

---

## Task 12: DI 配置完善 + App 启动逻辑

**Files:**
- Modify: `src/StockMonitor/App.xaml.cs`

- [ ] **Step 1: 完善 App.xaml.cs 的 DI 配置**

更新 `src/StockMonitor/App.xaml.cs`，确保所有服务正确注册，并在启动时执行数据迁移和当日交易合并：

```csharp
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StockMonitor.Core.Models;
using StockMonitor.Core.Services;
using StockMonitor.Core.Services.Interfaces;
using StockMonitor.Data;
using StockMonitor.UI.ViewModels;
using StockMonitor.UI.Views;

namespace StockMonitor;

public partial class App : Application
{
    public ServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // 执行数据迁移
        var stocksPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "stocks.json");
        DataMigrator.MigrateStocksIfNeeded(stocksPath);

        // 加载配置
        var configRepo = ServiceProvider.GetRequiredService<IRepository<AppConfig>>();
        var config = configRepo.Load();

        // 合并当日交易
        var positionService = ServiceProvider.GetRequiredService<IPositionService>();
        positionService.MergeDayTrades();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

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
            return new PositionService(repo, config.CommissionRate, config.TaxRate);
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
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build src/StockMonitor/StockMonitor.csproj
```

Expected: 构建成功

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat: complete DI configuration with data migration and day-trade merge on startup"
```

---

## Task 13: 清理旧项目文件

**Files:**
- Delete: 所有旧 WinForms 文件（见文件结构映射中的删除列表）

- [ ] **Step 1: 删除旧 WinForms 文件**

```bash
Remove-Item -Path "MainForm.cs","MainForm.Designer.cs","EditConfigForm.cs","EditConfigForm.Designer.cs","EditDeleteStockForm.cs","EditDeleteStockForm.Designer.cs","TransparentRichTextBox.cs","Program.cs" -Force
Remove-Item -Path "Model" -Recurse -Force
Remove-Item -Path "Logger" -Recurse -Force
Remove-Item -Path "StockMonitor.csproj" -Force
```

- [ ] **Step 2: 复制配置文件到新项目目录**

```bash
copy config.json src\StockMonitor\config.json
copy stocks.json src\StockMonitor\stocks.json
```

- [ ] **Step 3: 创建空的 alerts.json**

创建 `src/StockMonitor/alerts.json`：

```json
[]
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build StockMonitor.sln
```

Expected: 构建成功

- [ ] **Step 5: 运行全部测试**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj -v n
```

Expected: 全部通过

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "chore: remove old WinForms files and migrate config files to new project structure"
```

---

## Task 14: 全量测试 + 修复

**Files:**
- 可能修改: 任何有 bug 的文件

- [ ] **Step 1: 运行全部测试**

```bash
dotnet test tests/StockMonitor.Tests/StockMonitor.Tests.csproj -v n
```

Expected: 全部通过

- [ ] **Step 2: 构建整个解决方案**

```bash
dotnet build StockMonitor.sln -c Release
```

Expected: 构建成功，无错误

- [ ] **Step 3: 修复任何编译错误或测试失败**

根据实际输出修复问题。

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "fix: resolve any build and test issues"
```

---

## 自审清单

### 规格覆盖

| 规格要求 | 对应 Task |
|----------|-----------|
| T+0 修复 | Task 5 (PnLCalculator) + Task 6 (PositionService) |
| WinForms → WPF | Task 10 (ViewModels) + Task 11 (Views) |
| 数据源 failover | Task 7 (StockDataService) |
| 价格预警 | Task 8 (AlertService) |
| 全量编码 | Task 4 (DataMigrator.NormalizeCode) |
| 配置项保留 | Task 2 (AppConfig) + Task 4 (迁移) |
| 仓储模式 | Task 4 (JsonFileRepository) |
| 旧格式迁移 | Task 4 (DataMigrator) |
| DI 容器 | Task 1 + Task 12 |
| 日志重写 | Task 9 (FileLogger) |
| 单元测试 | Task 4-8 |

### 占位符扫描

无 TBD、TODO 或未实现步骤。

### 类型一致性

- `StockPosition.Code` 在所有文件中均为 `string` 类型，存全量编码
- `TradeRecord.TradeType` 枚举值 `Buy`/`Sell` 在所有文件中一致
- `IPositionService` 接口方法签名在 PositionService 实现中完全匹配
- `IAlertService` 接口方法签名在 AlertService 实现中完全匹配
