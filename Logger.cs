using System;
using System.IO;
using System.Text;

namespace Blueshot
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Blueshot", "Logs");
        
        private static readonly string LogFile = Path.Combine(LogDirectory, 
            $"blueshot_{DateTime.Now:yyyyMMdd}.log");
        
        private static readonly object LockObject = new object();

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
            }
            catch
            {
                // If we can't create the log directory, we'll log to temp
                var tempLogDir = Path.Combine(Path.GetTempPath(), "Blueshot", "Logs");
                Directory.CreateDirectory(tempLogDir);
            }
        }

        public static void LogInfo(string message, string source = null)
        {
            WriteLog("INFO", message, source);
        }

        public static void LogWarning(string message, string source = null)
        {
            WriteLog("WARN", message, source);
        }

        public static void LogError(string message, string source = null, Exception exception = null)
        {
            var errorMessage = message;
            if (exception != null)
            {
                errorMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                errorMessage += $"\nStack Trace: {exception.StackTrace}";
                
                if (exception.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
                }
            }
            WriteLog("ERROR", errorMessage, source);
        }

        public static void LogDebug(string message, string source = null)
        {
#if DEBUG
            WriteLog("DEBUG", message, source);
#endif
        }

        private static void WriteLog(string level, string message, string source)
        {
            try
            {
                lock (LockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var sourceInfo = string.IsNullOrEmpty(source) ? "" : $" [{source}]";
                    var logEntry = $"{timestamp} {level}{sourceInfo}: {message}{Environment.NewLine}";

                    File.AppendAllText(LogFile, logEntry, Encoding.UTF8);
                    
                    // Also write to debug output for development
                    System.Diagnostics.Debug.WriteLine($"[{level}]{sourceInfo}: {message}");
                }
            }
            catch
            {
                // If logging fails, we don't want to crash the application
                // Write to debug output as fallback
                System.Diagnostics.Debug.WriteLine($"[LOGGING ERROR] Failed to write log: {message}");
            }
        }

        public static void LogApplicationStart()
        {
            LogInfo("=== Blueshot Application Started ===", "Application");
            LogInfo($"Version: 1.0", "Application");
            LogInfo($"OS: {Environment.OSVersion}", "Application");
            LogInfo($".NET Version: {Environment.Version}", "Application");
            LogInfo($"Working Directory: {Environment.CurrentDirectory}", "Application");
        }

        public static void LogApplicationEnd()
        {
            LogInfo("=== Blueshot Application Ended ===", "Application");
        }

        public static string GetLogFilePath()
        {
            return LogFile;
        }

        public static void OpenLogDirectory()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to open log directory", "Logger", ex);
            }
        }
    }
}
