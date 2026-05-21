using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;

namespace ScreenSaver
{
    public class VideoScreenSaverForm : Form
    {
        private readonly RegistryManager registryManager;
        private readonly Settings parent;
        private readonly Screen screen;
        private readonly string initialVideoPath;

        private AxWMPLib.AxWindowsMediaPlayer mediaPlayer;
        private List<string> videoFiles;
        private int videoDurationSeconds;
        private DateTime videoStartTime;
        private Timer durationTimer;
        private Label fileNameLabel;
        private Font fileNameFont;
        private int fileNameDisplayMode;
        private bool isPreviewMode;
        private bool videoMuted = true;
        private Point mouseXY = Point.Empty;

        public VideoScreenSaverForm(Settings parent, Screen screen, string videoPath = null, bool isPreview = false)
        {
            this.parent = parent;
            this.screen = screen;
            this.initialVideoPath = videoPath;
            this.isPreviewMode = isPreview;

            registryManager = new RegistryManager();
            InitializeComponent();
            LoadSettings();
            LoadVideoFiles();
            InitializeTimers();
            PlayCurrentVideo();
        }

        private void InitializeComponent()
        {
            mediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).BeginInit();

            mediaPlayer.Dock = DockStyle.Fill;
            mediaPlayer.PlayStateChange += MediaPlayer_PlayStateChange;
            mediaPlayer.Enabled = true;
            Controls.Add(mediaPlayer);

            fileNameLabel = new Label();
            fileNameLabel.AutoSize = true;
            fileNameLabel.BackColor = Color.Transparent;
            fileNameLabel.Visible = false;
            Controls.Add(fileNameLabel);

            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).EndInit();

            mediaPlayer.enableContextMenu = false;
            mediaPlayer.uiMode = "none";

            FormBorderStyle = FormBorderStyle.None;
            Top = screen.Bounds.Top;
            Left = screen.Bounds.Left;
            ClientSize = new Size(screen.Bounds.Width, screen.Bounds.Height);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;

            if (!isPreviewMode)
            {
                KeyDown += VideoScreenSaverForm_KeyDown;
                MouseDown += OnMouseEvent;
                MouseMove += OnMouseEvent;
            }

            Shown += VideoScreenSaverForm_Shown;
        }

        private void VideoScreenSaverForm_Shown(object sender, EventArgs e)
        {
            if (isPreviewMode) { return; }

            try
            {
                Screen actualScreen = Screen.FromHandle(Handle);
                if (screen == null || !screen.DeviceName.Equals(actualScreen.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    // Keep assigned screen when possible; otherwise align to actual monitor.
                }
                Bounds = screen.Bounds;
            }
            catch
            {
                Bounds = screen.Bounds;
            }
        }

        private void LoadSettings()
        {
            videoMuted = registryManager.IsVideoMuted();

            int durationIndex;
            if (!int.TryParse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_VideoDuration, "2"), out durationIndex))
            {
                durationIndex = 2;
            }
            videoDurationSeconds = (durationIndex + 1) * 10;

            bool showFileName = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_SHOW_FILENAME, "No") == "Yes";
            if (!int.TryParse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE, "2"), out fileNameDisplayMode))
            {
                fileNameDisplayMode = 2;
            }

            string fontString = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_FONT, "");
            string colorString = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_COLOR, "White");

            fileNameLabel.Visible = showFileName;
            if (!string.IsNullOrEmpty(fontString))
            {
                fileNameFont = StringToFont(fontString);
                fileNameLabel.Font = fileNameFont;
            }
            fileNameLabel.ForeColor = ColorTranslator.FromHtml(colorString);
        }

        private void LoadVideoFiles()
        {
            videoFiles = new List<string>();
            HashSet<string> uniqueVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(initialVideoPath))
            {
                videoFiles.Add(initialVideoPath);
                return;
            }

            SortedDictionary<string, bool> imageFolders = registryManager.getImageFolders();
            string fileTypes = registryManager.getRegistryProperty(
                RegistryConstants.REG_KEY_VIDEO_FILE_TYPES,
                RegistryConstants.DefaultVideoFileTypes);
            if (string.IsNullOrEmpty(fileTypes))
            {
                fileTypes = RegistryConstants.DefaultVideoFileTypes;
            }
            string[] supportedExtensions = fileTypes.Split(';');

            foreach (KeyValuePair<string, bool> folderEntry in imageFolders)
            {
                string folder = folderEntry.Key;
                bool includeSubfolders = folderEntry.Value;

                if (!Directory.Exists(folder)) { continue; }

                try
                {
                    foreach (string extension in supportedExtensions)
                    {
                        if (string.IsNullOrWhiteSpace(extension)) { continue; }

                        SearchOption searchOption = includeSubfolders
                            ? SearchOption.AllDirectories
                            : SearchOption.TopDirectoryOnly;
                        foreach (string videoPath in Directory.EnumerateFiles(folder, extension.Trim(), searchOption))
                        {
                            uniqueVideos.Add(videoPath);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.WriteDebugLog($"Error accessing folder {folder}: {ex.Message}");
                }
                catch (DirectoryNotFoundException ex)
                {
                    Logger.WriteDebugLog($"Error accessing folder {folder}: {ex.Message}");
                }
            }

            videoFiles = new List<string>(uniqueVideos);

            if (videoFiles.Count == 0)
            {
                if (parent != null)
                {
                    MessageBox.Show("No video files found in the specified folders.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Close();
            }
        }

        private void InitializeTimers()
        {
            durationTimer = new Timer();
            durationTimer.Interval = 1000;
            durationTimer.Tick += DurationTimer_Tick;
            durationTimer.Start();
        }

        private void PlayCurrentVideo()
        {
            if (videoFiles == null || videoFiles.Count == 0) { return; }

            // Single video frame: always use the first (and only) entry in the playlist.
            string videoPath = videoFiles[0];

            if (File.Exists(videoPath))
            {
                mediaPlayer.URL = videoPath;
                mediaPlayer.Ctlcontrols.play();
                ApplyMuteSettings();
                videoStartTime = DateTime.Now;
                UpdateFileNameLabel(videoPath);
            }
            else
            {
                if (parent != null)
                {
                    MessageBox.Show("Video file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Close();
            }
        }

        private void RestartCurrentVideo()
        {
            videoStartTime = DateTime.Now;
            if (mediaPlayer != null)
            {
                mediaPlayer.Ctlcontrols.stop();
                mediaPlayer.Ctlcontrols.play();
                ApplyMuteSettings();
            }
        }

        private void ApplyMuteSettings()
        {
            if (mediaPlayer?.settings == null) { return; }

            mediaPlayer.settings.mute = videoMuted;
            mediaPlayer.settings.volume = videoMuted ? 0 : 100;
        }

        private void UpdateFileNameLabel(string videoPath)
        {
            if (!fileNameLabel.Visible) { return; }

            fileNameLabel.Text = GetFormattedFileName(videoPath);
            fileNameLabel.Location = new Point(10, Height - fileNameLabel.Height - 10);
        }

        private string GetFormattedFileName(string fullPath)
        {
            switch (fileNameDisplayMode)
            {
                case 0:
                    return fullPath;
                case 1:
                    try
                    {
                        string rootPath = Path.GetDirectoryName(Application.ExecutablePath);
                        return GetRelativePath(rootPath, fullPath);
                    }
                    catch
                    {
                        return Path.GetFileName(fullPath);
                    }
                case 2:
                default:
                    return Path.GetFileName(fullPath);
            }
        }

        private string GetRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrEmpty(rootPath)) { return fullPath; }

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

                if (parts.Length < 3)
                    throw new ArgumentException("Invalid font string format.");

                string fontFamily = parts[0];
                float fontSize = float.Parse(parts[1]);

                if (!Enum.TryParse(parts[2], true, out FontStyle style))
                    style = FontStyle.Regular;

                return new Font(fontFamily, fontSize, style);
            }
            catch
            {
                return new Font("Arial", 12, FontStyle.Regular);
            }
        }

        private void DurationTimer_Tick(object sender, EventArgs e)
        {
            if (videoStartTime == DateTime.MinValue) { return; }

            TimeSpan elapsed = DateTime.Now - videoStartTime;
            if (elapsed.TotalSeconds >= videoDurationSeconds)
            {
                RestartCurrentVideo();
            }
        }

        private void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            // State 3 = playing — WMP resets mute/volume when media opens.
            if (e.newState == 3)
            {
                ApplyMuteSettings();
            }
            // State 8 = media ended — loop the single video frame.
            else if (e.newState == 8)
            {
                RestartCurrentVideo();
            }
        }

        private void OnMouseEvent(object sender, MouseEventArgs e)
        {
            if (!mouseXY.IsEmpty)
            {
                if (Math.Abs(mouseXY.X - e.X) > 5 || Math.Abs(mouseXY.Y - e.Y) > 5)
                    ExitScreenSaver();
                if (e.Clicks > 0)
                    ExitScreenSaver();
            }
            mouseXY = new Point(e.X, e.Y);
        }

        private void VideoScreenSaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            ExitScreenSaver();
        }

        private void ExitScreenSaver()
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Ctlcontrols.stop();
            }
            if (durationTimer != null)
            {
                durationTimer.Stop();
            }

            if (isPreviewMode)
            {
                Close();
                Application.Exit();
            }
            else if (parent != null)
            {
                Close();
            }
            else
            {
                Application.Exit();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                ExitScreenSaver();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
    }
}
