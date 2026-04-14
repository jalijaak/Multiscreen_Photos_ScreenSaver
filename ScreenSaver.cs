using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading.Tasks;

namespace ScreenSaver
{
    public class DotNETScreenSaver
    {
        private static List<Form> activeForms = new List<Form>();
        private static System.Windows.Forms.Timer monitorChangeTimer;
        private static bool isDebugMode;

        [STAThread]
        static void Main(string[] args)
        {
            RegisterGlobalExceptionHandlers();

            try
            {
                // Use per-monitor DPI aware context so monitor bounds map to physical pixels
                // on mixed-DPI multi-monitor setups.
                NativeMethods.TryEnablePerMonitorDpiAwareness();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                RegistryManager registryManager = new RegistryManager();

                // Check for debug argument in any position
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i].ToLower().Trim() == "/debug")
                    {
                        string debugMode = args[i + 1].ToLower().Trim();
                        if (debugMode == "on")
                        {
                            registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, true);
                            MessageBox.Show("Debug mode enabled", "Debug Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (debugMode == "off")
                        {
                            registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);
                            MessageBox.Show("Debug mode disabled", "Debug Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                }

                isDebugMode = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG);
                Logger.WriteDebugLog($"Application started with arguments: {string.Join(", ", args)}");

                // Process screensaver standard arguments:
                // /c, /s, /p <HWND>, /a <HWND> (also supports '-' prefix and ':value' form)
                string mode;
                string modeValue;
                ParseScreenSaverArgs(args, out mode, out modeValue);
                if (isDebugMode)
                {
                    Logger.WriteDebugLog($"Parsed screensaver args: mode='{mode}', value='{modeValue}'");
                }

                if (mode == "c") // settings/config mode
                {
                    Application.Run(new Settings());
                }
                else if (mode == "s") // screensaver mode
                {
                    RunScreenSaver();
                    return;
                }
                else if (mode == "p") // preview mode
                {
                    // Get the preview window handle from the arguments
                    IntPtr previewHandle = ParsePreviewHandle(modeValue);
                    if (isDebugMode)
                    {
                        Logger.WriteDebugLog($"Preview mode start. handleRaw='{modeValue}', handleParsed=0x{previewHandle.ToInt64():X}");
                    }

                    // Stability fallback: if Windows does not supply a valid preview host,
                    // do not enter preview mode.
                    if (previewHandle == IntPtr.Zero)
                    {
                        Logger.WriteDebugLog("Preview mode aborted: missing or invalid preview host handle.");
                        return;
                    }

                    // Create settings instance but don't show it
                    Settings settings = new Settings();
                    settings.Hide();

                    NativeMethods.RECT previewRect = new NativeMethods.RECT();
                    bool hasPreviewRect = NativeMethods.GetClientRect(previewHandle, out previewRect);
                    if (isDebugMode)
                    {
                        Logger.WriteDebugLog($"Preview host rect available={hasPreviewRect}");
                    }

                    // Create a preview form
                    Form previewForm = new Form1(settings, Screen.PrimaryScreen, true);
                    previewForm.FormBorderStyle = FormBorderStyle.None;
                    previewForm.StartPosition = FormStartPosition.Manual;
                    previewForm.TopLevel = false;
                    if (isDebugMode)
                    {
                        previewForm.Shown += (s, e) =>
                        {
                            IntPtr parent = NativeMethods.GetParent(previewForm.Handle);
                            Logger.WriteDebugLog(
                                $"Preview shown: child=0x{previewForm.Handle.ToInt64():X}, parent=0x{parent.ToInt64():X}, " +
                                $"bounds={previewForm.Bounds}, visible={previewForm.Visible}");
                        };
                    }

                    // Set the preview window as the parent
                    NativeMethods.SetParent(previewForm.Handle, previewHandle);
                    if (isDebugMode)
                    {
                        IntPtr parent = NativeMethods.GetParent(previewForm.Handle);
                        Logger.WriteDebugLog(
                            $"SetParent called. child=0x{previewForm.Handle.ToInt64():X}, requestedParent=0x{previewHandle.ToInt64():X}, actualParent=0x{parent.ToInt64():X}");
                    }
                    if (hasPreviewRect)
                    {
                        int previewWidth = previewRect.Right - previewRect.Left;
                        int previewHeight = previewRect.Bottom - previewRect.Top;
                        if (isDebugMode)
                        {
                            Logger.WriteDebugLog($"Preview host client size: {previewWidth}x{previewHeight}");
                        }
                        NativeMethods.SetWindowPos(
                            previewForm.Handle,
                            IntPtr.Zero,
                            0,
                            0,
                            previewWidth,
                            previewHeight,
                            0x0040 // SWP_NOZORDER
                        );
                        previewForm.Bounds = new Rectangle(0, 0, previewWidth, previewHeight);
                        previewForm.Location = Point.Empty;
                    }

                    Application.Run(previewForm);

                }
                else if (mode == "a") // change password (legacy)
                {
                    // Modern Windows versions do not use screensaver password change.
                    // Keep compatibility by opening settings UI.
                    if (isDebugMode)
                    {
                        Logger.WriteDebugLog("Legacy '/a' mode requested; falling back to settings UI.");
                    }
                    Application.Run(new Settings());
                }
                else
                {
                    Application.Run(new Settings());
                }
            }
            catch (Exception ex)
            {
                Logger.WriteDebugLog($"Fatal exception in Main: {ex}");
                throw;
            }
        }

        // Native method for setting parent window
        private static class NativeMethods
        {
            private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport("user32.dll")]
            public static extern IntPtr GetParent(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetProcessDPIAware();

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

            public static void TryEnablePerMonitorDpiAwareness()
            {
                try
                {
                    if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                    {
                        SetProcessDPIAware();
                    }
                }
                catch
                {
                    // Best effort only; continue with default process context.
                }
            }

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int X,
                int Y,
                int cx,
                int cy,
                uint uFlags);
        }

        private static IntPtr ParsePreviewHandle(string handleArg)
        {
            if (string.IsNullOrWhiteSpace(handleArg)) { return IntPtr.Zero; }

            long handleValue;
            if (long.TryParse(handleArg.Trim(), out handleValue))
            {
                return new IntPtr(handleValue);
            }
            return IntPtr.Zero;
        }

        private static void ParseScreenSaverArgs(string[] args, out string mode, out string value)
        {
            mode = "c"; // default: settings
            value = string.Empty;

            if (args == null || args.Length == 0) { return; }

            string first = (args[0] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(first)) { return; }

            // Normalize "/s", "-s", "/p:12345", etc.
            if (first.StartsWith("/") || first.StartsWith("-"))
            {
                first = first.Substring(1);
            }
            if (string.IsNullOrEmpty(first)) { return; }

            int sep = first.IndexOf(':');
            if (sep >= 0)
            {
                mode = first.Substring(0, sep).Trim().ToLowerInvariant();
                value = first.Substring(sep + 1).Trim();
            }
            else
            {
                mode = first.Trim().ToLowerInvariant();
                if (args.Length > 1)
                {
                    value = (args[1] ?? string.Empty).Trim();
                }
            }

            // Keep only supported modes.
            if (mode != "c" && mode != "s" && mode != "p" && mode != "a")
            {
                mode = "c";
                value = string.Empty;
            }
        }

        private static void RunScreenSaver()
        {
            // Create forms for all screens
            CreateScreenSaverForms();

            // Setup monitor change detection timer
            monitorChangeTimer = new System.Windows.Forms.Timer();
            monitorChangeTimer.Interval = 1000; // Check every second
            monitorChangeTimer.Tick += MonitorChangeTimer_Tick;
            monitorChangeTimer.Start();

            // Run the application
            Application.Run();
        }

        private static void CreateScreenSaverForms()
        {
            // Create forms for all screens
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen screen = Screen.AllScreens[i];
                Form form = new Form1(null, screen);
                activeForms.Add(form);
                form.Show();
            }
        }

        private static int GetScreenDisplayNumber(Screen screen)
        {
            if (screen == null || string.IsNullOrEmpty(screen.DeviceName)) { return int.MaxValue; }
            string device = screen.DeviceName;
            int idx = device.LastIndexOf("DISPLAY", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string suffix = device.Substring(idx + "DISPLAY".Length);
                int number;
                if (int.TryParse(suffix, out number))
                {
                    return number;
                }
            }
            return int.MaxValue;
        }

        private static void MonitorChangeTimer_Tick(object sender, EventArgs e)
        {
            // Get current screen count
            var currentScreens = Screen.AllScreens;

            // If screen count changed, recreate all forms
            if (currentScreens.Length != activeForms.Count)
            {
                foreach (var form in activeForms)
                {
                    form.Close();
                    form.Dispose();
                }
                activeForms.Clear();
                CreateScreenSaverForms();
            }
            else
            {
                // Check if any form's actual attached monitor bounds changed.
                bool screensChanged = false;
                for (int i = 0; i < activeForms.Count; i++)
                {
                    Screen actual = Screen.FromHandle(activeForms[i].Handle);
                    if (!activeForms[i].Bounds.Equals(actual.Bounds))
                    {
                        screensChanged = true;
                        break;
                    }
                }

                if (screensChanged)
                {
                    foreach (var form in activeForms)
                    {
                        form.Close();
                        form.Dispose();
                    }
                    activeForms.Clear();
                    CreateScreenSaverForms();
                }
            }
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                Logger.WriteDebugLog($"Unhandled UI thread exception: {e.Exception}");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Logger.WriteDebugLog($"Unhandled domain exception: {(ex != null ? ex.ToString() : e.ExceptionObject?.ToString())}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logger.WriteDebugLog($"Unobserved task exception: {e.Exception}");
                e.SetObserved();
            };
        }
    }
}
