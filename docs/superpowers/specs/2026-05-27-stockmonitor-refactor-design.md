# StockMonitor 中度重构设计文档

> 日期: 2026-05-27
> 状态: 待审核
> 方案: 方案 B — 分层架构 + MVVM

## 1. 背景与目标

### 1.1 当前问题

| 问题 | 描述 |
|------|------|
| T+0 Bug | 当天加仓再减仓无法完成，持仓和成本计算逻辑断裂 |
| 架构耦合 | MainForm 530 行包揽数据获取、解析、计算、渲染 |
| 数据源单一 | 新浪财经非官方 API，随时可能失效 |
| 模型混乱 | Config/ConfigV1 共存，StockView 继承 StockConfig 职责不清 |
| 编码问题 | 股票代码存纯数字，请求 API 时靠猜测市场前缀 |
| 无测试 | 零单元测试覆盖 |

### 1.2 重构目标

- 修复 T+0 加仓减仓 bug
- WinForms → WPF + MVVM 分层架构
- 数据源 failover（新浪主力 + 腾讯备用）
- 新增价格预警通知功能
- 股票代码统一为全量编码（如 `sh600036`）
- 保留现有配置项，向后兼容

### 1.3 约束

- 纯桌面端，不需要 Web/移动端
- 配置项保留（ShowFormat、RefreshTime、ShowTodaySumFormat、OrderBy、CommissionRate、TaxRate）
- 中度重构，不引入插件系统等过度设计

## 2. 项目结构

```
StockMonitor/
├── StockMonitor.sln
├── src/
│   └── StockMonitor/
│       ├── StockMonitor.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       │
│       ├── Core/
│       │   ├── Models/
│       │   │   ├── StockPosition.cs
│       │   │   ├── StockQuote.cs
│       │   │   ├── TradeRecord.cs
│       │   │   ├── AlertRule.cs
│       │   │   └── AppConfig.cs
│       │   │
│       │   ├── Services/
│       │   │   ├── Interfaces/
│       │   │   │   ├── IStockDataProvider.cs
│       │   │   │   ├── IStockDataService.cs
│       │   │   │   ├── IPositionService.cs
│       │   │   │   ├── IAlertService.cs
│       │   │   │   └── IRepository{T}.cs
│       │   │   │
│       │   │   ├── StockDataService.cs
│       │   │   ├── SinaDataProvider.cs
│       │   │   ├── TencentDataProvider.cs
│       │   │   ├── PositionService.cs
│       │   │   └── AlertService.cs
│       │   │
│       │   └── Calculator/
│       │       └── PnLCalculator.cs
│       │
│       ├── Data/
│       │   ├── JsonFileRepository{T}.cs
│       │   └── DataMigrator.cs
│       │
│       ├── UI/
│       │   ├── ViewModels/
│       │   │   ├── MainViewModel.cs
│       │   │   ├── StockDisplayItem.cs
│       │   │   ├── PositionEditViewModel.cs
│       │   │   ├── ConfigEditViewModel.cs
│       │   │   └── AlertManageViewModel.cs
│       │   │
│       │   ├── Views/
│       │   │   ├── MainWindow.xaml
│       │   │   ├── PositionEditDialog.xaml
│       │   │   ├── ConfigEditDialog.xaml
│       │   │   └── AlertManageDialog.xaml
│       │   │
│       │   ├── Converters/
│       │   │   ├── PnLToBrushConverter.cs
│       │   │   └── BoolToVisibilityConverter.cs
│       │   │
│       │   └── Resources/
│       │       └── Styles.xaml
│       │
│       ├── Logging/
│       │   └── FileLogger.cs
│       │
│       └── config.json / stocks.json / alerts.json
│
└── tests/
    └── StockMonitor.Tests/
        ├── StockMonitor.Tests.csproj
        ├── Calculator/
        │   └── PnLCalculatorTests.cs
        ├── Services/
        │   ├── PositionServiceTests.cs
        │   ├── AlertServiceTests.cs
        │   └── StockDataServiceTests.cs
        └── Data/
            ├── DataMigratorTests.cs
            └── JsonRepositoryTests.cs
```

## 3. Core 层设计

### 3.1 数据模型

#### StockPosition（持久化模型，对应 stocks.json）

```csharp
public class StockPosition
{
    public string Code { get; set; }              // 全量编码 "sh600036"
    public string Name { get; set; }              // 股票名称
    public int Quantity { get; set; }             // 当前持仓数量（不含当日交易）
    public decimal AvgCostPrice { get; set; }     // 持仓成本均价
    public List<TradeRecord> TodayTrades { get; set; } = new();  // 当日交易记录
}
```

#### StockQuote（实时行情快照，每次刷新生成）

```csharp
public class StockQuote
{
    public string Code { get; init; }             // 全量编码
    public string Name { get; init; }             // 股票名称
    public decimal CurrentPrice { get; init; }
    public decimal OpenPrice { get; init; }
    public decimal YestClose { get; init; }
    public decimal HighPrice { get; init; }
    public decimal LowPrice { get; init; }
    public decimal Change => CurrentPrice - YestClose;
    public decimal ChangeRate => YestClose != 0 ? Math.Round(Change / YestClose * 100, 2) : 0;
}
```

#### TradeRecord（交易记录，替代 StockOperate）

```csharp
public class TradeRecord
{
    public enum TradeType { Buy, Sell }
    public TradeType Type { get; init; }
    public int Quantity { get; init; }            // 始终正数
    public decimal Price { get; init; }
    public DateTime Time { get; init; }
    public decimal Commission { get; init; }
    public decimal Tax { get; init; }
}
```

#### AlertRule（预警规则）

```csharp
public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StockCode { get; set; }         // 全量编码
    public AlertType Type { get; set; }           // PriceAbove / PriceBelow / ChangeRateAbove / ChangeRateBelow
    public decimal Threshold { get; set; }
    public bool IsTriggered { get; set; }
    public bool IsOneTime { get; set; } = true;
}

public enum AlertType
{
    PriceAbove,
    PriceBelow,
    ChangeRateAbove,
    ChangeRateBelow
}
```

#### AppConfig（应用配置，兼容现有 config.json）

```csharp
public class AppConfig
{
    public string ShowFormat { get; set; } = "#name(#code) #makemoney\r\n#price #change #rate%";
    public int RefreshTime { get; set; } = 2000;
    public string ShowTodaySumFormat { get; set; } = "今日盈亏:#money,今日比例 #rate%";
    public string OrderBy { get; set; } = "havepos desc,makemoney desc";
    public decimal CommissionRate { get; set; } = 0.00023m;
    public decimal TaxRate { get; set; } = 0;
    public string PrimaryDataSource { get; set; } = "Sina";
    public string FallbackDataSource { get; set; } = "Tencent";
}
```

### 3.2 T+0 修复方案

**核心原则**：当日交易记录在 `TodayTrades` 列表中，主持仓（Quantity/AvgCostPrice）不变，查询时实时合并。

**持仓计算**：

```
当日净持仓 = TodayTrades.Sum(买入) - TodayTrades.Sum(卖出)
总持仓 = Quantity + 当日净持仓
```

**盈亏计算**（PnLCalculator）：

```
当日已实现盈亏 = Sum(卖出记录: (卖出价 - 昨收价) * 数量 - 佣金 - 印花税)
当日浮动盈亏 = Sum(买入记录: (现价 - 买入价) * 数量 - 佣金)
               + 原持仓浮动: (现价 - AvgCostPrice) * Quantity
总盈亏 = 已实现 + 浮动
```

**减仓校验**：

```
总持仓 - 减仓数量 >= 0
即 Quantity + 当日净买入 - 当日净卖出 - 减仓数量 >= 0
不满足则抛出 InsufficientPositionException
```

**次日合并**（PositionService.MergeDayTrades）：

```
新Quantity = Quantity + 当日净持仓
新AvgCostPrice = (原持仓成本 + 当日买入总成本) / 新Quantity
清空 TodayTrades
```

### 3.3 服务接口

#### IStockDataProvider

```csharp
public interface IStockDataProvider
{
    string Name { get; }
    Task<List<StockQuote>> GetQuotesAsync(IEnumerable<string> codes);
    bool IsAvailable { get; }
}
```

#### IStockDataService（调度层）

```csharp
public interface IStockDataService
{
    Task<List<StockQuote>> GetQuotesAsync();
    event EventHandler<List<StockQuote>> QuotesUpdated;
}
```

#### IPositionService

```csharp
public interface IPositionService
{
    IReadOnlyList<StockPosition> GetAllPositions();
    StockPosition GetPosition(string code);
    void AddPosition(string code, decimal price, int quantity);
    void ReducePosition(string code, decimal price, int quantity);
    void UpdatePosition(string code, int quantity, decimal avgCostPrice);
    void AddStock(string code);
    void RemoveStock(string code);
    void MergeDayTrades();
    event EventHandler<PositionChangedEventArgs> PositionChanged;
}
```

#### IAlertService

```csharp
public interface IAlertService
{
    void AddRule(AlertRule rule);
    void RemoveRule(string ruleId);
    IReadOnlyList<AlertRule> GetRules();
    void CheckAlerts(IEnumerable<StockQuote> quotes);
    event EventHandler<AlertTriggeredEventArgs> AlertTriggered;
}
```

## 4. Data 层设计

### 4.1 仓储接口

```csharp
public interface IRepository<T>
{
    T Load();
    void Save(T data);
    bool Exists();
}
```

### 4.2 JsonFileRepository

- 泛型实现，构造时传入文件路径
- Save 时先写 `.tmp` 文件再重命名，防止写入中断导致文件损坏
- Load 失败时尝试加载 `.bak` 备份文件

### 4.3 旧格式迁移（DataMigrator）

**stocks.json 迁移**：

检测条件：JSON 含 `"Position"` 字段且不含 `"Quantity"`

映射规则：
- `Position` → `Quantity`
- `Cost` → `AvgCostPrice`
- `OpList` → `TodayTrades`（类型兼容转换）
- `Code` 纯数字 → 补全市场前缀

市场前缀规则：
- 6/9 开头 → `sh`
- 0/3 开头 → `sz`
- 5 开头 → `sh`（上海 ETF）
- 1 开头 → `sz`（深圳 ETF）

**config.json 迁移**：

新增的 `PrimaryDataSource` 和 `FallbackDataSource` 字段有默认值，旧配置文件缺少时反序列化自动取默认值，无需特殊迁移。

## 5. UI 层设计

### 5.1 技术选型

| 组件 | 选择 |
|------|------|
| UI 框架 | WPF (.NET 10) |
| MVVM 框架 | CommunityToolkit.Mvvm |
| DI 容器 | Microsoft.Extensions.DependencyInjection |
| 系统托盘 | Hardcodet.NotifyIcon.Wpf |
| Toast 通知 | Microsoft.Toolkit.Uwp.Notifications |

### 5.2 主窗口

- `WindowStyle=None` + `AllowsTransparency=True` + `Topmost=True`
- 半透明黑色背景，红绿白文字
- 可拖拽、双击隐藏/显示
- 系统托盘右键菜单：显示/隐藏、操作、预警管理、编辑配置、刷新、退出

### 5.3 ViewModel

#### MainViewModel

- `ObservableCollection<StockDisplayItem> Stocks` — 绑定股票列表
- `string TodaySummary` — 今日汇总文本
- `Brush SummaryColor` — 汇总行颜色
- 订阅 StockDataService 数据刷新事件
- 订阅 AlertService 预警触发事件 → Toast 通知
- 数据到达时合并 StockPosition + StockQuote → StockDisplayItem

#### StockDisplayItem（视图专用模型）

- Code、DisplayName、PriceText、ChangeText、ChangeRateText、PnLText
- PriceColor（红/绿/白）
- HasPosition（是否持仓）

#### PositionEditViewModel

- 股票列表、选中股票、输入数量/价格
- 加仓/减仓/增改/删除/新增 命令

#### ConfigEditViewModel

- ShowFormat、RefreshTime、ShowTodaySumFormat、OrderBy
- 保存/取消命令

#### AlertManageViewModel

- 预警规则列表
- 添加/删除/启停规则命令

### 5.4 数据流

```
定时器/手动刷新
    │
    ▼
StockDataService.GetQuotesAsync()
    ├─→ SinaDataProvider (主力)
    ├─→ TencentDataProvider (备用，主力失败时)
    └─→ 返回缓存（全部失败时）
    │
    ▼
MainViewModel 收到 StockQuote 列表
    ├─→ 合并 PositionService 的 StockPosition → StockDisplayItem
    ├─→ 排序（按 AppConfig.OrderBy）
    ├─→ 更新 ObservableCollection → WPF 自动刷新 UI
    │
    └─→ AlertService.CheckAlerts(quotes)
          ├─→ 触发 → Toast 通知
          └─→ 未触发 → 无操作
```

## 6. 错误处理

| 层级 | 策略 |
|------|------|
| Core 层 | 抛出业务异常，不吞错 |
| Data 层 | 捕获 IO 异常，包装后抛 |
| Service 层 | 降级而非崩溃（数据源全挂返回缓存） |
| UI 层 | 捕获异常 → 用户友好提示 |
| 全局 | AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException → 日志记录 |

Logger 重写要点：
- 统一 UTF-8 编码
- 同步写入（File.AppendAllText）
- 统一方法签名：LogDebug(string)、LogError(string, Exception?)

## 7. NuGet 依赖

### 生产依赖

- CommunityToolkit.Mvvm 8.*
- Microsoft.Extensions.DependencyInjection 9.*
- Hardcodet.NotifyIcon.Wpf 1.*
- Microsoft.Toolkit.Uwp.Notifications 7.*

### 测试依赖

- xunit 2.*
- Moq 4.*
- FluentAssertions 7.*

### 移除依赖

- Newtonsoft.Json（改用 System.Text.Json）
- System.Text.Encoding.CodePages（WPF 不需要手动注册编码）

## 8. 测试重点

### T+0 场景

- 当天加仓 → 查询总持仓正确
- 当天减仓 → 查询总持仓正确
- 当天先加仓再减仓（T+0 先买后卖）→ 盈亏计算正确
- 当天先减仓再加仓（T+0 先卖后买）→ 盈亏计算正确
- 减仓超过（原持仓 + 当日加仓）→ 抛出异常
- 次日启动合并 → 持仓和成本价正确
- 当天多次加仓 → 加权平均成本正确

### 数据源 failover

- 主力成功 → 返回主力数据
- 主力失败 + 备用成功 → 返回备用数据
- 全部失败 → 返回缓存
- 首次全部失败且无缓存 → 返回空列表

### 数据迁移

- 旧格式 stocks.json 自动迁移
- 旧格式 config.json 向后兼容
