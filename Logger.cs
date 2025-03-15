using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

public static class Logger
{
    private const string LOG_FILE_NAME = "JJ_screensaver_debug.log";
    private static readonly string LOG_FILE_PATH = Path.Combine(Path.GetTempPath(), LOG_FILE_NAME);

    /// <summary>
    /// Writes a debug log message to a file, automatically including the caller's method name and line number.
    /// </summary>
    /// <param name="message">The log message.</param>
    /// <param name="methodName">Automatically set to caller's method name.</param>
    /// <param name="lineNumber">Automatically includes the line number from where the log was called.</param>
    public static void WriteDebugLog(
        string message,
        [System.Runtime.CompilerServices.CallerMemberName] string methodName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] ({methodName}:{lineNumber}) - {message}";

            File.AppendAllText(LOG_FILE_PATH, formattedMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write log entry: {ex.Message}");
        }
    }
}
