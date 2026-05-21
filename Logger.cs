using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

public static class Logger
{
    private const string LOG_FILE_NAME = "JJ_screensaver_debug.log";
    private static readonly string LOG_FILE_PATH = Path.Combine(Path.GetTempPath(), LOG_FILE_NAME);

    public static string DebugLogFilePath => LOG_FILE_PATH;

    /// <summary>
    /// Writes a debug log message to a file, automatically including the caller's method name and line number.
    /// </summary>
    /// <param name="message">The log message.</param>
    /// <param name="methodName">Automatically set to caller's method name.</param>
    /// <param name="lineNumber">Automatically includes the line number from where the log was called.</param>
    public static void WriteDebugLog(
        string message,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLogEntry("DEBUG", message, methodName, lineNumber, null);
    }

    /// <summary>
    /// Writes an error to the same debug log file (always logged, not gated by screensaver debug mode).
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
