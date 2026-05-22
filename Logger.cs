using System;
using System.IO;
using System.Runtime.CompilerServices;
using ScreenSaver;

public static class Logger
{
    private const string LOG_FILE_NAME = "JJ_screensaver.log";
    private static readonly string LOG_FILE_PATH = Path.Combine(Path.GetTempPath(), LOG_FILE_NAME);

    private static bool? debugLoggingEnabled;

    public static string LogFilePath => LOG_FILE_PATH;

    /// <summary>Alias for <see cref="LogFilePath"/>.</summary>
    public static string DebugLogFilePath => LOG_FILE_PATH;

    /// <summary>Re-read the Debug registry flag (call after changing debug mode in settings).</summary>
    public static void RefreshDebugLoggingEnabled()
    {
        debugLoggingEnabled = null;
    }

    private static bool IsDebugLoggingEnabled
    {
        get
        {
            if (!debugLoggingEnabled.HasValue)
            {
                try
                {
                    var registryManager = new RegistryManager();
                    debugLoggingEnabled = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);
                }
                catch
                {
                    debugLoggingEnabled = false;
                }
            }

            return debugLoggingEnabled.Value;
        }
    }

    /// <summary>
    /// Writes a debug log line only when Debug is enabled in screensaver settings.
    /// </summary>
    public static void WriteDebugLog(
        string message,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsDebugLoggingEnabled)
            return;

        WriteLogEntry("DEBUG", message, methodName, lineNumber, null);
    }

    /// <summary>
    /// Writes an error to the log file (always logged, regardless of debug mode).
    /// </summary>
    public static void WriteErrorLog(
        string message,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLogEntry("ERROR", message, methodName, lineNumber, null);
    }

    public static void WriteErrorLog(
        string message,
        Exception ex,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLogEntry("ERROR", message, methodName, lineNumber, ex);
    }

    private static void WriteLogEntry(
        string level,
        string message,
        string methodName,
        int lineNumber,
        Exception ex)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{level}] ({methodName}:{lineNumber}) - {message}";
            if (ex != null)
                formattedMessage += $"{Environment.NewLine}  Exception: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}  {ex.StackTrace}";

            File.AppendAllText(LOG_FILE_PATH, formattedMessage + Environment.NewLine);
        }
        catch (Exception writeEx)
        {
            Console.WriteLine($"Failed to write log entry: {writeEx.Message}");
        }
    }
}
