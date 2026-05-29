# 半透明背景 + 退出卡顿 + 日志优化 + 收盘停止刷新

## 问题 1：背景半透明

### 修复

文件: `src/StockMonitor/UI/Views/MainWindow.xaml`

- Border 背景从 `#E6000000`（90%不透明）改为 `#CC000000`（80%不透明）
- 窗口高度从固定 200 改为 `SizeToContent="Height"` 自适应内容高度

---

## 问题 2：退出卡顿

### 根因

`_cts.Cancel()` 只取消 `Task.Delay`，不取消正在进行的 HTTP 请求。Dispose HttpClient 时会阻塞等待请求完成。

### 修复

**Step 1**: `MainViewModel.Dispose()` 等待刷新循环退出

文件: `src/StockMonitor/UI/ViewModels/MainViewModel.cs`

- 添加 `TaskCompletionSource _loopExited` 字段
- `RefreshLoopAsync` 退出时 `_loopExited.SetResult()`
- `Dispose()` 中：Cancel CTS → 等待 `_loopExited.Task`（带 3 秒超时）

**Step 2**: HTTP 请求支持 CancellationToken

文件: `src/StockMonitor/Core/Services/Interfaces/IStockDataProvider.cs`
- `GetQuotesAsync` 加 `CancellationToken cancellationToken = default`

文件: `src/StockMonitor/Core/Services/SinaDataProvider.cs`
- `_httpClient.GetStreamAsync(url)` → `_httpClient.GetStreamAsync(url, cancellationToken)`

文件: `src/StockMonitor/Core/Services/TencentDataProvider.cs`
- 同上

文件: `src/StockMonitor/Core/Services/StockDataService.cs`
- 传递 CancellationToken 给 Provider

**Step 3**: 退出时先隐藏窗口

文件: `src/StockMonitor/UI/Views/MainWindow.xaml.cs`

`Menu_Exit` 改为：先 Hide → Dispose ViewModel → Dispose NotifyIcon → Shutdown

---

## 问题 3：LogDebug 只在调试模式记录 + 增加 LogInfo

### 修复

文件: `src/StockMonitor/Logging/FileLogger.cs`

- 添加 `LogInfo(string message)` 方法（INFO 级别，始终记录）
- `LogDebug` 改为仅在 `#if DEBUG` 或通过配置开关控制
- 添加静态属性 `IsDebugEnabled`，默认 `false`，`#if DEBUG` 时默认 `true`
- `LogDebug` 内部检查 `IsDebugEnabled`，非调试模式直接返回不写文件

```csharp
public static bool IsDebugEnabled { get; set; } =
#if DEBUG
    true;
#else
    false;
#endif

public static void LogDebug(string message)
{
    if (!IsDebugEnabled) return;
    Log("DEBUG", message);
}

public static void LogInfo(string message) => Log("INFO", message);
```

文件: `src/StockMonitor/Core/Services/StockDataService.cs`
- 将启动/成功等关键日志从 `LogDebug` 改为 `LogInfo`

文件: `src/StockMonitor/App.xaml.cs`
- 将启动流程日志从 `LogDebug` 改为 `LogInfo`

---

## 问题 4：下午 3:20 停止刷新

### 修复

文件: `src/StockMonitor/UI/ViewModels/MainViewModel.cs`

在 `RefreshLoopAsync` 的循环开头添加收盘时间检查：

```csharp
private async Task RefreshLoopAsync()
{
    while (!_cts.IsCancellationRequested)
    {
        // 收盘检查：15:20 后停止自动刷新
        if (DateTime.Now.TimeOfDay > new TimeSpan(15, 20, 0))
        {
            FileLogger.LogInfo("已过收盘时间(15:20)，停止自动刷新");
            break;
        }

        try { await RefreshData(); }
        catch (Exception ex) { FileLogger.LogError("刷新行情失败", ex); }

        try { await Task.Delay(_config.RefreshTime, _cts.Token); }
        catch (OperationCanceledException) { break; }
    }
    _loopExited?.SetResult();
}
```

---

## 文件变更清单

| 文件 | 变更 |
|------|------|
| `src/StockMonitor/UI/Views/MainWindow.xaml` | 背景半透明 + 自适应高度 |
| `src/StockMonitor/UI/Views/MainWindow.xaml.cs` | Menu_Exit 先 Hide |
| `src/StockMonitor/UI/ViewModels/MainViewModel.cs` | Dispose 等待退出 + CancellationToken + 收盘停止 |
| `src/StockMonitor/Logging/FileLogger.cs` | IsDebugEnabled + LogInfo |
| `src/StockMonitor/Core/Services/Interfaces/IStockDataProvider.cs` | 加 CancellationToken 参数 |
| `src/StockMonitor/Core/Services/SinaDataProvider.cs` | HTTP 传 CancellationToken |
| `src/StockMonitor/Core/Services/TencentDataProvider.cs` | HTTP 传 CancellationToken |
| `src/StockMonitor/Core/Services/StockDataService.cs` | 传 CancellationToken + LogDebug→LogInfo |
| `src/StockMonitor/App.xaml.cs` | LogDebug→LogInfo |
