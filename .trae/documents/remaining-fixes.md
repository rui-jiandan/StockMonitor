# 剩余修复计划

## 背景
前一轮已完成：背景半透明、CancellationToken 传递、收盘停止刷新、Dispose 等待循环退出。
本轮需完成 3 个修改 + 构建验证。

---

## 修复1：FileLogger — 添加 IsDebugEnabled + LogInfo

**文件**: `src/StockMonitor/Logging/FileLogger.cs`

**改动**:
1. 添加 `IsDebugEnabled` 静态属性，默认值由 `#if DEBUG` 编译指令决定：
   - Debug 模式 = `true`
   - Release 模式 = `false`
2. 添加 `LogInfo(string message)` 公开方法，日志等级为 `INFO`
3. 修改 `LogDebug` 方法：只有 `IsDebugEnabled == true` 时才写入文件

**类比**: 就像家里的灯开关——调试模式是"开灯"状态，所有细节都能看到；正式模式是"关灯"，只保留重要的信息灯（Info/Error）。

---

## 修复2：App.xaml.cs — LogDebug → LogInfo

**文件**: `src/StockMonitor/App.xaml.cs`

**改动**: 将所有 `FileLogger.LogDebug(...)` 调用改为 `FileLogger.LogInfo(...)`

涉及行：
- L27: `LogDebug("StockMonitor 启动中...")` → `LogInfo`
- L36: `LogDebug($"数据迁移完成: {stocksPath}")` → `LogInfo`
- L40: `LogDebug($"配置加载完成...")` → `LogInfo`
- L44: `LogDebug($"持仓合并完成...")` → `LogInfo`
- L48: `LogDebug("主窗口已显示")` → `LogInfo`

同时检查 `StockDataService.cs` 中的 `LogDebug` 调用，将业务关键日志改为 `LogInfo`：
- L42: `LogDebug("无持仓股票，跳过行情获取")` → `LogInfo`
- L46: `LogDebug($"开始获取行情...")` → `LogInfo`
- L50: `LogDebug($"尝试数据源...")` → `LogInfo`
- L64: `LogDebug($"数据源...返回空结果")` → `LogInfo`

---

## 修复3：MainWindow.xaml.cs — Menu_Exit 先 Hide 再 Dispose

**文件**: `src/StockMonitor/UI/Views/MainWindow.xaml.cs`

**改动**: `Menu_Exit` 方法中，先 `Hide()` 窗口，再执行 Dispose 和 Shutdown

```csharp
private void Menu_Exit(object sender, RoutedEventArgs e)
{
    Hide();
    _viewModel.Dispose();
    NotifyIcon.Dispose();
    Application.Current.Shutdown();
}
```

**类比**: 就像关电视——先让屏幕黑下来（Hide），再慢慢断电（Dispose），用户不会看到断电过程中的闪烁。

---

## 修复4：构建验证

运行 `dotnet build` 确认所有修改无编译错误。

---

## 执行顺序
1. FileLogger.cs — 添加 IsDebugEnabled + LogInfo
2. App.xaml.cs — LogDebug → LogInfo
3. StockDataService.cs — LogDebug → LogInfo
4. MainWindow.xaml.cs — Menu_Exit 先 Hide
5. 构建验证
