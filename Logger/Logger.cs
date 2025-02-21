using System;
using System.IO;

namespace StockMonitor
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs",DateTime.Now.ToString("yyyyMMdd"), "logfile.log");

        static Logger()
        {
            // 确保日志目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            
        }

        public static void LogDebug(string message)
        {
            Log("DEBUG", message);
        }

        public static void LogError(string message, Exception ex)
        {
            Log("ERROR", $"{message}: {ex}");
        }

        private static void Log(string level, string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            File.AppendAllTextAsync(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
