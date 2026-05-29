# 托盘图标切换 + 配置编辑增强 实施计划

## 问题 1：双击图标切换显示隐藏

### 根因

`Window_MouseDoubleClick` 绑定在窗口上，但窗口 `Background="Transparent"` + `AllowsTransparency=True`，透明区域的鼠标事件不会传递到窗口。用户实际双击的是**托盘图标**（TaskbarIcon），但 TaskbarIcon 没有绑定 DoubleClick 事件。

### 修复步骤

#### Step 1: 在 MainWindow.xaml 中为 TaskbarIcon 添加 DoubleClick 事件

文件: `src/StockMonitor/UI/Views/MainWindow.xaml`

在 `<tb:TaskbarIcon>` 标签上添加 `DoubleClick="NotifyIcon_DoubleClick"` 事件。

#### Step 2: 在 MainWindow.xaml.cs 中添加 DoubleClick 处理方法

文件: `src/StockMonitor/UI/Views/MainWindow.xaml.cs`

添加 `NotifyIcon_DoubleClick` 方法，逻辑与 `Window_MouseDoubleClick` 相同：可见→Hide，隐藏→Show+定位。

#### Step 3: 移除窗口级别的 MouseDoubleClick

从 Window 标签移除 `MouseDoubleClick="Window_MouseDoubleClick"`，因为透明窗口上双击不可靠。保留 `MouseLeftButtonDown` 用于拖拽。

---

## 问题 2：配置编辑增强 — 占位符展示 + 点击插入 + 预览

### 设计思路

在配置编辑对话框中，为 `ShowFormat` 和 `ShowTodaySumFormat` 两个格式字段：
1. 在输入框下方展示可用占位符标签（如 `#name` `#code` `#price` 等），用类似"芯片"的样式
2. 点击标签自动将占位符插入到对应输入框的光标位置
3. 在对话框底部增加预览区域，用示例数据实时渲染格式效果

### 可用占位符定义

**ShowFormat（股票显示格式）可用占位符**：
| 占位符 | 含义 | 示例值 |
|--------|------|--------|
| `#name` | 股票名称 | 招商银行 |
| `#code` | 股票代码 | sh600036 |
| `#price` | 当前价格 | 43.68 |
| `#change` | 涨跌额 | +0.17 |
| `#rate` | 涨跌幅 | 0.39% |
| `#makemoney` | 盈亏金额 | +45.20 |
| `#quantity` | 持仓数量 | 300 |
| `#cost` | 成本价 | 43.267 |

**ShowTodaySumFormat（今日汇总格式）可用占位符**：
| 占位符 | 含义 | 示例值 |
|--------|------|--------|
| `#money` | 今日总盈亏 | +126.50 |
| `#rate` | 今日盈亏比例 | 0.32% |

### 修复步骤

#### Step 4: 在 ConfigEditViewModel 中添加占位符定义和预览逻辑

文件: `src/StockMonitor/UI/ViewModels/ConfigEditViewModel.cs`

添加：
- `ShowFormatPlaceholders` 属性：`List<PlaceholderItem>`，每个包含 Tag 和 Description
- `TodaySumFormatPlaceholders` 属性：同上
- `InsertPlaceholderCommand`：`RelayCommand<PlaceholderItem>`，将占位符插入到当前活跃的输入框
- `PreviewText` 属性：`string`，根据当前格式 + 示例数据生成的预览文本
- `ActiveFormatField` 属性：`string`，标记当前编辑的是 ShowFormat 还是 ShowTodaySumFormat
- `UpdatePreview()` 方法：用示例数据替换占位符，生成预览
- 在 ShowFormat 和 ShowTodaySumFormat 的 setter 中调用 UpdatePreview()

PlaceholderItem 定义：
```csharp
public class PlaceholderItem
{
    public string Tag { get; init; }      // "#name"
    public string Description { get; init; } // "股票名称"
}
```

#### Step 5: 重写 ConfigEditDialog.xaml 布局

文件: `src/StockMonitor/UI/Views/ConfigEditDialog.xaml`

新布局结构：
```
ScrollViewer
  └── StackPanel
      ├── "显示格式:" 标签
      ├── TextBox (ShowFormat) + GotFocus 记录活跃字段
      ├── WrapPanel (ShowFormatPlaceholders) — 可点击的占位符标签
      ├── "今日汇总格式:" 标签
      ├── TextBox (ShowTodaySumFormat) + GotFocus 记录活跃字段
      ├── WrapPanel (TodaySumFormatPlaceholders) — 可点击的占位符标签
      ├── "刷新间隔(ms):" 标签
      ├── TextBox (RefreshTime)
      ├── "排序方式:" 标签
      ├── TextBox (OrderBy)
      ├── Separator
      ├── "预览效果:" 标签
      └── TextBlock (PreviewText) — 暗色背景模拟主窗口效果
按钮行: 保存 / 取消
```

占位符标签样式：小圆角 Border + TextBlock，浅灰背景，点击时调用 InsertPlaceholderCommand。

预览区域：深色背景 + 浅色文字，模拟主窗口显示效果。

#### Step 6: 在 ConfigEditDialog.xaml.cs 中添加辅助逻辑

文件: `src/StockMonitor/UI/Views/ConfigEditDialog.xaml.cs`

添加：
- TextBox 的 GotFocus 事件处理：记录当前活跃的格式字段（ShowFormat 或 ShowTodaySumFormat）
- 占位符标签的 Click 事件处理：获取活跃 TextBox，在光标位置插入占位符文本
- 保持对两个格式 TextBox 的引用以便操作光标位置

---

## 文件变更清单

| 文件 | 变更类型 |
|------|----------|
| `src/StockMonitor/UI/Views/MainWindow.xaml` | 修改：TaskbarIcon 添加 DoubleClick 事件，移除 Window 的 MouseDoubleClick |
| `src/StockMonitor/UI/Views/MainWindow.xaml.cs` | 修改：添加 NotifyIcon_DoubleClick 方法，移除 Window_MouseDoubleClick |
| `src/StockMonitor/UI/ViewModels/ConfigEditViewModel.cs` | 修改：添加占位符列表、InsertPlaceholderCommand、PreviewText、UpdatePreview |
| `src/StockMonitor/UI/Views/ConfigEditDialog.xaml` | 修改：重写布局，添加占位符标签区和预览区 |
| `src/StockMonitor/UI/Views/ConfigEditDialog.xaml.cs` | 修改：添加 GotFocus/Click 事件处理 |
