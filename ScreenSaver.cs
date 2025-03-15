using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenSaver
{
    public class DotNETScreenSaver
    {
        private static List<Form> activeForms = new List<Form>();
        private static System.Windows.Forms.Timer monitorChangeTimer;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check for debug argument in any position
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower().Trim() == "/debug")
                {
                    string debugMode = args[i + 1].ToLower().Trim();
                    if (debugMode == "on")
                    {
                        RegistryManager registryManager = new RegistryManager();
                        registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, true);
                        MessageBox.Show("Debug mode enabled", "Debug Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (debugMode == "off")
                    {
                        RegistryManager registryManager = new RegistryManager();
                        registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);
                        MessageBox.Show("Debug mode disabled", "Debug Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
            }

            // Process other arguments
            string arg = args.Length > 0 ? args[0].ToLower().Trim() : "";

            if (arg.StartsWith("/c")) //Modal console mode
            {
                Application.Run(new Settings());
            }
            else if (arg == "/s") //screensaver mode
            {
                // Create settings form but don't show it
                Settings settings = new Settings();
                settings.Hide();
                settings.runScreensaver(true, true); // true for all screens, true for destroyOnClose
                return;
            }
            else if (arg.StartsWith("/p")) //preview mode
            {
                // Get the preview window handle from the arguments
                IntPtr previewHandle = IntPtr.Zero;
                if (args.Length > 1)
                {
                    previewHandle = new IntPtr(long.Parse(args[1]));
                }

                // Create settings instance but don't show it
                Settings settings = new Settings();
                settings.Hide();

                // Create a preview form
                Form previewForm = new Form1(settings, Screen.PrimaryScreen, true);
                previewForm.FormBorderStyle = FormBorderStyle.None;

                // Set the preview window as the parent
                if (previewHandle != IntPtr.Zero)
                {
                    NativeMethods.SetParent(previewForm.Handle, previewHandle);
                }

                Application.Run(previewForm);

            }
            else
            {
                Application.Run(new Settings());
            }
        }

        // Native method for setting parent window
        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
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
                Form form = new Form1(null, Screen.AllScreens[i]);
                activeForms.Add(form);
                form.Show();
            }
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
                // Check if any screen bounds have changed
                bool screensChanged = false;
                for (int i = 0; i < activeForms.Count; i++)
                {
                    if (!activeForms[i].Bounds.Equals(Screen.AllScreens[i].Bounds))
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
    }
}
