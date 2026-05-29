# StockMonitor v2.0

一款轻量级 Windows 桌面实时股票监控工具，悬浮于屏幕右下角，不打扰日常工作，随时掌握持仓盈亏。

## 功能特性

- **实时行情刷新**：新浪主力 + 腾讯备用双数据源自动 failover，按配置间隔刷新
- **T+0 交易支持**：当日加仓/减仓实时计算盈亏，次日自动合并
- **持仓盈亏展示**：按股票显示价格、涨跌、盈亏，顶部汇总今日总盈亏
- **价格预警通知**：支持价格上穿/下穿、涨跌幅上穿/下穿四种预警类型
- **系统托盘驻留**：最小化到托盘，双击图标切换显示/隐藏
- **半透明悬浮窗**：暗色调半透明背景，始终置顶，紧贴屏幕右下角
- **收盘自动停止**：15:20 后自动停止刷新，节省资源
- **灵活配置**：通过 `config.json` 自定义显示格式、刷新间隔、佣金税率等
- **配置占位符**：编辑配置时显示可用字段，点击插入，实时预览效果
- **全量编码**：股票代码统一 `sh600036` 格式，旧数据自动迁移

## 技术架构

```
StockMonitor
├── Core/                    # 核心层（零 UI 依赖）
│   ├── Models/              # 数据模型
│   │   ├── StockPosition    # 持仓（含当日交易列表）
│   │   ├── StockQuote       # 行情快照
│   │   ├── TradeRecord      # 交易记录
│   │   ├── AlertRule        # 预警规则
│   │   └── AppConfig        # 应用配置
│   ├── Services/            # 业务服务
│   │   ├── StockDataService # 数据源调度（failover）
│   │   ├── PositionService  # 持仓管理（T+0）
│   │   ├── AlertService     # 预警检测
│   │   ├── SinaDataProvider # 新浪数据源
│   │   └── TencentDataProvider # 腾讯数据源
│   └── Calculator/          # 盈亏计算器
├── Data/                    # 数据层
│   ├── JsonFileRepository   # JSON 文件持久化（原子写入+备份）
│   └── DataMigrator         # 旧格式自动迁移
├── Logging/                 # 日志
│   └── FileLogger           # 文件日志（Debug/Info/Error 三级）
└── UI/                      # 表现层（WPF + MVVM）
    ├── ViewModels/          # CommunityToolkit.Mvvm
    ├── Views/               # XAML 窗口
    └── Converters/          # 值转换器
```

**关键技术选型**：
- .NET 10 + WPF
- CommunityToolkit.Mvvm（源码生成器 MVVM）
- Microsoft.Extensions.DependencyInjection
- Hardcodet.NotifyIcon.Wpf（系统托盘）

## 配置文件

### config.json

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `showFormat` | 单只股票显示格式 | `#name(#code) #makemoney\r\n#price #change #rate%` |
| `refreshTime` | 刷新间隔（毫秒） | `2000` |
| `showTodaySumFormat` | 今日汇总格式 | `今日盈亏:#money,今日比例 #rate%` |
| `orderBy` | 排序规则 | `havepos desc,makemoney desc` |
| `commissionRate` | 佣金费率 | `0.00023` |
| `taxRate` | 印花税率 | `0` |
| `primaryDataSource` | 主力数据源 | `Sina` |
| `fallbackDataSource` | 备用数据源 | `Tencent` |

**显示格式占位符**：

| 占位符 | 含义 |
|--------|------|
| `#name` | 股票名称 |
| `#code` | 股票代码 |
| `#price` | 当前价格 |
| `#change` | 涨跌额 |
| `#rate` | 涨跌幅 |
| `#makemoney` | 盈亏金额 |

**排序字段**：`havepos`（有持仓优先）、`makemoney`（盈亏金额）、`name`、`code`，支持 `asc`/`desc`

### stocks.json

持仓数据文件，股票代码使用全量编码格式：

```json
[
  {
    "code": "sh600036",
    "name": "招商银行",
    "quantity": 1000,
    "costPrice": 35.50,
    "todayTrades": []
  }
]
```

### alerts.json

预警规则文件：

```json
[
  {
    "stockCode": "sh600036",
    "type": "PriceAbove",
    "threshold": 40.00,
    "isOneTime": true
  }
]
```

**预警类型**：`PriceAbove`、`PriceBelow`、`ChangeRateAbove`、`ChangeRateBelow`

## 使用方法

1. 启动程序，主窗口自动悬浮在屏幕右下角
2. 双击主面板或托盘图标可切换显示/隐藏
3. 右键托盘图标打开菜单：
   - **操作**：添加/编辑/删除持仓股票
   - **预警管理**：设置价格预警规则
   - **编辑配置**：修改显示格式、刷新间隔等（支持占位符插入和实时预览）
   - **刷新**：手动刷新行情
   - **退出**：关闭程序

## T+0 交易说明

当日加仓/减仓记录在 `todayTrades` 列表中，主持仓数量和成本不变。程序实时合并计算当日盈亏。次日启动时自动将 `todayTrades` 合并到主持仓中。

## 日志

日志文件位于程序目录的 `logs/` 文件夹下，按日期命名。

- **DEBUG**：仅在调试模式（`#if DEBUG`）下记录，可通过 `FileLogger.IsDebugEnabled` 运行时控制
- **INFO**：业务关键节点日志（启动、数据源切换、收盘停止等）
- **ERROR**：异常错误日志

## 开发与构建

```bash
# 构建
dotnet build StockMonitor.sln

# 运行测试（41 个用例）
dotnet test StockMonitor.sln

# 运行
dotnet run --project src/StockMonitor
```

## 从 v1 迁移

程序首次启动时自动检测并迁移旧版数据：
- 纯数字股票代码自动补全市场前缀（`600036` → `sh600036`）
- 旧版 PascalCase 配置自动转为 camelCase
- 无需手动操作，迁移后原文件保留备份
