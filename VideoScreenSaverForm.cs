using System;
using System.Drawing;
using System.Windows.Forms;
using WMPLib;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms.VisualStyles;

namespace ScreenSaver
{
    public class VideoScreenSaverForm : ScreenSaverForm
    {
        private RegistryManager registryManager;

        private AxWMPLib.AxWindowsMediaPlayer mediaPlayer;
        private List<string> videoFiles;
        private int currentVideoIndex;
        private int videoDurationSeconds;
        private DateTime videoStartTime;
        private Timer durationTimer;
        private Label fileNameLabel;
        private Font fileNameFont;
        private int fileNameDisplayMode;
        private string initialVideoPath;
        private SortedDictionary<string, bool> imageFolders = new SortedDictionary<string, bool>();

        public VideoScreenSaverForm(int screenNumber, string videoPath) : base(screenNumber)
        {
            this.initialVideoPath = videoPath;
            InitializeComponent();
            LoadSettings();
            LoadVideoFiles();
            InitializeTimers();
            PlayNextVideo();
        }

        private void InitializeComponent()
        {
            this.registryManager = new RegistryManager();

            // Initialize Windows Media Player
            mediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).BeginInit();
            
            // Configure basic properties that can be set before initialization
            mediaPlayer.Dock = DockStyle.Fill;
            mediaPlayer.PlayStateChange += MediaPlayer_PlayStateChange;
            mediaPlayer.Enabled = true;
            
            // Add media player to controls
            Controls.Add(mediaPlayer);
            
            // Initialize file name label
            fileNameLabel = new Label();
            fileNameLabel.AutoSize = true;
            fileNameLabel.BackColor = Color.Transparent;
            fileNameLabel.Visible = false;
            Controls.Add(fileNameLabel);
            
            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).EndInit();

            // Configure properties that must be set after initialization
            mediaPlayer.enableContextMenu = false;
            mediaPlayer.uiMode = "none";
            mediaPlayer.settings.mute = true; // Mute the video
        }

        private void LoadSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryConstants.REG_ROOT_PATH))
            {
                if (key != null)
                {
                    // Load video duration setting
                    int durationIndex = int.Parse((string)key.GetValue(RegistryConstants.REG_KEY_VideoDuration, "2"));
                    videoDurationSeconds = (durationIndex + 1) * 10; // Convert index to seconds (10, 20, 30, 40, 50)
                    
                    // Load file name display settings
                    bool showFileName = bool.Parse((string)key.GetValue(RegistryConstants.REG_KEY_SHOW_FILENAME, "False"));
                    fileNameDisplayMode = int.Parse((string)key.GetValue(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE, "2"));
                    string fontString = (string)key.GetValue(RegistryConstants.REG_KEY_FILENAME_FONT, "");
                    string colorString = (string)key.GetValue(RegistryConstants.REG_KEY_FILENAME_COLOR, "White");
                    
                    fileNameLabel.Visible = showFileName;
                    if (!string.IsNullOrEmpty(fontString))
                    {
                        fileNameFont = StringToFont(fontString);
                        fileNameLabel.Font = fileNameFont;
                    }
                    fileNameLabel.ForeColor = ColorTranslator.FromHtml(colorString);
                }
            }
        }

        private void LoadVideoFiles()
        {
            videoFiles = new List<string>();
            
            // If we have an initial video path, use only that
            if (!string.IsNullOrEmpty(initialVideoPath))
            {
                videoFiles.Add(initialVideoPath);
                return;
            }

            // Otherwise load from registry settings
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryConstants.REG_ROOT_PATH))
            {
                if (key != null)
                {
                    imageFolders = registryManager.getImageFolders();
                    if (imageFolders.Count > 0)
                    {
                        foreach (KeyValuePair<string, bool> folderEntry in imageFolders)
                        {
                            string folder = folderEntry.Key;
                            bool includeSubfolders = folderEntry.Value;

                            if (Directory.Exists(folder))
                            {
                                string[] supportedExtensions = "*.mp4;*.avi;*.wmv;*.mov".Split(';');
                                try
                                {
                                    foreach (string extension in supportedExtensions)
                                    {
                                        SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                                        videoFiles.AddRange(Directory.GetFiles(folder, extension, searchOption));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteDebugLog($"Error accessing folder {folder}: {ex.Message}");
                                    Console.WriteLine($"Error accessing folder {folder}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            if (videoFiles.Count > 0)
            {
                // Randomize video order
                Random rnd = new Random();
                int n = videoFiles.Count;
                while (n > 1)
                {
                    n--;
                    int k = rnd.Next(n + 1);
                    string temp = videoFiles[k];
                    videoFiles[k] = videoFiles[n];
                    videoFiles[n] = temp;
                }
            }
            else
            {
                MessageBox.Show("No video files found in the specified folders.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Windows.Forms.Application.Exit();
            }
        }

        private void InitializeTimers()
        {
            durationTimer = new Timer();
            durationTimer.Interval = 1000; // Check every second
            durationTimer.Tick += DurationTimer_Tick;
            durationTimer.Start();
        }

        private void PlayNextVideo()
        {
            if (videoFiles.Count == 0) return;

            // Always play the first video
            string videoPath = videoFiles[0];

            if (File.Exists(videoPath))
            {
                mediaPlayer.URL = videoPath;
                mediaPlayer.Ctlcontrols.play();
                videoStartTime = DateTime.Now;
                UpdateFileNameLabel(videoPath);
            }
            else
            {
                MessageBox.Show("Video file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void UpdateFileNameLabel(string videoPath)
        {
            if (!fileNameLabel.Visible) return;

            fileNameLabel.Text = GetFormattedFileName(videoPath);
            fileNameLabel.Location = new Point(10, this.Height - fileNameLabel.Height - 10);
        }

        private string GetFormattedFileName(string fullPath)
        {
            switch (fileNameDisplayMode)
            {
                case 0: // Full path
                    return fullPath;
                case 1: // Relative path
                    try {
                        string rootPath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                        return GetRelativePath(rootPath, fullPath);
                    }
                    catch {
                        return Path.GetFileName(fullPath);
                    }
                case 2: // File name only
                default:
                    return Path.GetFileName(fullPath);
            }
        }

        private string GetRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return fullPath;
            
            rootPath = rootPath.Replace('\\', '/').TrimEnd('/');
            fullPath = fullPath.Replace('\\', '/');
            
            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                return fullPath;
                
            string relativePath = fullPath.Substring(rootPath.Length).TrimStart('/');
            return string.IsNullOrEmpty(relativePath) ? "." : relativePath;
        }

        private Font StringToFont(string fontString)
        {
            try
            {
                string[] parts = fontString.Split(';');
                string fontFamily = parts[0];
                float fontSize = float.Parse(parts[1]);
                FontStyle style = (FontStyle)Enum.Parse(typeof(FontStyle), parts[2]);
                return new Font(fontFamily, fontSize, style);
            }
            catch
            {
                return new Font("Arial", 10, FontStyle.Regular);
            }
        }

        private void DurationTimer_Tick(object sender, EventArgs e)
        {
            if (videoStartTime != DateTime.MinValue)
            {
                TimeSpan elapsed = DateTime.Now - videoStartTime;
                if (elapsed.TotalSeconds >= videoDurationSeconds)
                {
                    // Close form when duration limit is reached
                    this.Close();
                }
            }
        }

        private void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            // State 8 means media ended
            if (e.newState == 8)
            {
                // Close the form when video ends
                this.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (durationTimer != null)
                {
                    durationTimer.Stop();
                    durationTimer.Dispose();
                }
                if (mediaPlayer != null)
                {
                    mediaPlayer.Dispose();
                }
                if (fileNameLabel != null)
                {
                    fileNameLabel.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                // Stop playback
                if (mediaPlayer != null)
                {
                    mediaPlayer.Ctlcontrols.stop();
                }
                // Stop timer
                if (durationTimer != null)
                {
                    durationTimer.Stop();
                }
                // Close the form
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
} 