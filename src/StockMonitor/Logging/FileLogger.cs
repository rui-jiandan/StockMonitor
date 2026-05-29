using System.IO;
using System.Text;

namespace StockMonitor.Logging;

/// <summary>
/// 文件日志器，线程安全地写入 UTF-8 编码的日志文件
/// </summary>
public static class FileLogger
{
    private static readonly string LogDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "logs");

    private static readonly object _lock = new();

#if DEBUG
    public static bool IsDebugEnabled { get; set; } = true;
#else
    public static bool IsDebugEnabled { get; set; } = false;
#endif

    static FileLogger()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    private static string GetLogFilePath() =>
        Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}logfile.log");

    /// <summary>
    /// 记录调试日志，仅在 IsDebugEnabled 为 true 时写入
    /// </summary>
    public static void LogDebug(string message)
    {
        if (IsDebugEnabled) Log("DEBUG", message);
    }

    /// <summary>
    /// 记录信息日志，用于业务关键节点
    /// </summary>
    public static void LogInfo(string message) => Log("INFO", message);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="message">错误描述</param>
    /// <param name="ex">可选的异常对象</param>
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
            File.AppendAllText(GetLogFilePath(), logLine, Encoding.UTF8);
        }
    }
}
