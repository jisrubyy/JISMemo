using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JISMemo;

public partial class App : System.Windows.Application
{
    private string _logDir = string.Empty;
    private string _exeDir = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            InitializeLogging();
            LogInfo("=== App Startup ===");
            LogEnvironment(e);

            // Global exception handlers
            this.DispatcherUnhandledException += (_, args) =>
            {
                LogError("DispatcherUnhandledException", args.Exception);
                ShowFatalMessage(args.Exception);
                args.Handled = true;
                Shutdown(-1);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args2) =>
            {
                var ex = args2.ExceptionObject as Exception;
                LogError("AppDomain.UnhandledException", ex);
                // Cannot show UI reliably here; best-effort logging only
            };

            TaskScheduler.UnobservedTaskException += (_, args3) =>
            {
                LogError("TaskScheduler.UnobservedTaskException", args3.Exception);
                args3.SetObserved();
            };

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            try
            {
                InitializeLogging();
                LogError("Startup exception", ex);
            }
            catch { /* ignore */ }
            ShowFatalMessage(ex);
            Shutdown(-1);
        }
    }

    private void InitializeLogging()
    {
        try
        {
            _exeDir = AppContext.BaseDirectory;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logDir = Path.Combine(localAppData, "JISMemo", "logs");
            Directory.CreateDirectory(_logDir);
        }
        catch
        {
            // Fallback to exe directory if LocalAppData unavailable
            _logDir = _exeDir;
        }
    }

    private void LogEnvironment(StartupEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Version: {AppInfo.Version}");
        sb.AppendLine($".NET: {Environment.Version}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Is64BitProcess: {Environment.Is64BitProcess}");
        sb.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
        sb.AppendLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
        sb.AppendLine($"Args: {string.Join(" ", e.Args ?? Array.Empty<string>())}");
        sb.AppendLine($"Assembly: {Assembly.GetEntryAssembly()?.FullName}");
        LogInfo(sb.ToString());
    }

    private void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    private void LogError(string tag, Exception? ex)
    {
        var msg = ex == null ? "(null)" : ex.ToString();
        WriteLog("ERROR", $"[{tag}] {msg}");
    }

    private void WriteLog(string level, string message)
    {
        try
        {
            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff");
            var line = $"{ts} [{level}] {message}";
            var file = Path.Combine(_logDir, "JISMemo.startup.log");
            File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);

            // also drop a copy next to the exe for convenience
            var file2 = Path.Combine(_exeDir, "JISMemo.startup.log");
            File.AppendAllText(file2, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { /* ignore IO errors */ }
    }

    private void ShowFatalMessage(Exception ex)
    {
        try
        {
            System.Windows.MessageBox.Show(
                "JISMemo 실행 중 오류가 발생했습니다.\n로그 파일(JISMemo.startup.log)을 개발자에게 전달해주세요.",
                "JISMemo 오류",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch { /* ignore */ }
    }
}