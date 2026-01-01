using System;
using System.IO;
using System.Text;

namespace JISMemo.Services;

public static class LogService
{
    private static readonly string LogDir;
    private static readonly string LogFile;
    private static readonly object _lock = new();

    static LogService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        LogDir = Path.Combine(appData, "JISMemo", "logs");
        LogFile = Path.Combine(LogDir, "app_log.txt");

        try
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }
        }
        catch { /* 로깅 초기화 실패 시 어쩔 수 없음 */ }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message, Exception? ex = null) 
    {
        var fullMessage = ex != null ? $"{message} | Exception: {ex}" : message;
        Write("ERROR", fullMessage);
    }

    private static void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(LogFile, logLine + Environment.NewLine, Encoding.UTF8);

                // 로그 파일 크기가 너무 커지면(예: 5MB) 백업하고 새로 시작하는 로직이 있으면 좋지만 여기서는 생략
            }
            catch { /* 로깅 실패는 최후의 수단으로 무시 */ }
        }
    }
}
