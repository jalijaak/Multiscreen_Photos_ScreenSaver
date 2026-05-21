using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScreenSaver
{
    public partial class Form1 : Form
    {
        private static MediaCatalog sharedCatalog;
        private static readonly object CatalogLock = new object();

        private bool isDebugMode;
        private RegistryManager registryManager;
        private MediaCatalog mediaCatalog;
        private bool useEffects;
        private bool showFileName;
        private int fileNameDisplayMode;
        private Font fileNameFont;
        private Color fileNameColor;
        private float effectDurationVal;
        private int delayBetweenImages;
        private int videoDurationSeconds;
        private List<AnimationTypes> effectsList = new List<AnimationTypes>();
        private SortedDictionary<string, bool> imageFolders = new SortedDictionary<string, bool>();

        private TableLayoutPanel frameGrid;
        private MediaFrameSlot[] frames;
        private Timer[] frameTimers;
        private FrameMediaHistory[] frameHistories;
        private int? activeVideoFrameIndex;

        private DateTime lastKeyPressTime = DateTime.MinValue;
        private readonly TimeSpan KEY_PRESS_DELAY = TimeSpan.FromMilliseconds(200);

        private int animationSteps = 15;
        private int animationStepInterval;
        private Screen screen;
        private Settings parent;
        private bool isPreviewMode;

        public static void ResetSharedCatalog()
        {
            lock (CatalogLock)
            {
                sharedCatalog = null;
            }
        }

        private static MediaCatalog GetSharedCatalog(RegistryManager registryManager)
        {
            lock (CatalogLock)
            {
                if (sharedCatalog == null)
                    sharedCatalog = new MediaCatalog(registryManager);
                return sharedCatalog;
            }
        }

        private void WriteDebugLog(string message)
        {
            if (!isDebugMode) return;
            try
            {
                string screenInfo = screen != null ? screen.DeviceName : "NoScreen";
                Logger.WriteDebugLog($"[{screenInfo}] {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        public Form1(Settings parent, Screen screen, bool isPreview = false)
        {
            registryManager = new RegistryManager();
            isDebugMode = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);

            InitializeComponent();

            this.screen = screen;
            this.parent = parent;
            this.isPreviewMode = isPreview;

            ResetSharedCatalog();
            mediaCatalog = GetSharedCatalog(registryManager);

            string delayStr = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_DELAY_BETWEEN_IMAGES, "10");
            if (!int.TryParse(delayStr, out delayBetweenImages))
                delayBetweenImages = 10;
            int durationIndex;
            if (!int.TryParse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_VideoDuration, "2"), out durationIndex))
                durationIndex = 2;
            videoDurationSeconds = (durationIndex + 1) * 10;

            FormBorderStyle = FormBorderStyle.None;
            Top = screen.Bounds.Top;
            Left = screen.Bounds.Left;
            ClientSize = new Size(screen.Bounds.Width, screen.Bounds.Height);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;

            LoadFileNameSettings();
            LoadEffectSettings();
            imageFolders = registryManager.getImageFolders();

            InitializeFrameGrid();
            animationStepInterval = (int)(effectDurationVal * 1000 / animationSteps);

            for (int i = 0; i < frames.Length; i++)
                NavigateFrameNext(i);
        }

        private void InitializeFrameGrid()
        {
            int frameCount = 1;
            if (!int.TryParse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FRAMES_ON_SCREEN, "1"), out frameCount)
                || frameCount < 1)
            {
                frameCount = 1;
            }

            FrameLayout.GetGridSize(frameCount, out int columns, out int rows);

            frameGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = rows,
                BackColor = Color.Black
            };

            for (int c = 0; c < columns; c++)
                frameGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            for (int r = 0; r < rows; r++)
                frameGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            frames = new MediaFrameSlot[frameCount];
            frameTimers = new Timer[frameCount];
            frameHistories = new FrameMediaHistory[frameCount];

            bool mute = registryManager.IsVideoMuted();

            for (int i = 0; i < frameCount; i++)
            {
                frameHistories[i] = new FrameMediaHistory();

                MediaFrameSlot slot = new MediaFrameSlot { Dock = DockStyle.Fill, Margin = new Padding(1) };
                slot.ConfigureDisplay(showFileName, fileNameFont, fileNameColor, fileNameDisplayMode);
                slot.ConfigureVideo(mute, videoDurationSeconds);

                int slotIndex = i;
                slot.VideoEnded += (s, e) => NavigateFrameNext(slotIndex);
                slot.VideoStopped += (s, e) =>
                {
                    if (activeVideoFrameIndex == slotIndex)
                        activeVideoFrameIndex = null;
                    if (!frames[slotIndex].IsVideoActive)
                        frameTimers[slotIndex].Start();
                };

                FrameLayout.GetCellPosition(i, columns, out int col, out int row);
                frameGrid.Controls.Add(slot, col, row);
                frames[i] = slot;

                Timer frameTimer = new Timer { Interval = delayBetweenImages * 1000 };
                frameTimer.Tick += (s, e) => NavigateFrameNext(slotIndex);
                frameTimer.Start();
                frameTimers[i] = frameTimer;
            }

            Controls.Add(frameGrid);
            WriteDebugLog($"Created {frameCount} independent frame(s) on {columns}x{rows} grid");
        }

        private void NavigateFrameNext(int frameIndex)
        {
            if (!CanNavigateFrame(frameIndex)) return;

            // Image delay timer must not advance frames while video is playing.
            if (frames[frameIndex].IsVideoActive)
                return;

            string filePath = frameHistories[frameIndex].GoNext(() => PickRandomForFrame(frameIndex));
            if (string.IsNullOrEmpty(filePath)) return;

            DisplayFileOnFrame(frameIndex, filePath, "next");
        }

        private void NavigateFramePrevious(int frameIndex)
        {
            if (!CanNavigateFrame(frameIndex)) return;

            if (frames[frameIndex].IsVideoActive)
                return;

            string filePath = frameHistories[frameIndex].GoPrevious(() => PickRandomForFrame(frameIndex));
            if (string.IsNullOrEmpty(filePath)) return;

            DisplayFileOnFrame(frameIndex, filePath, "previous");
        }

        private bool CanNavigateFrame(int frameIndex)
        {
            if (frames == null || frameHistories == null
                || frameIndex < 0 || frameIndex >= frames.Length)
                return false;

            if (mediaCatalog == null || !mediaCatalog.HasMedia)
            {
                WriteDebugLog("No media available.");
                return false;
            }
            return true;
        }

        private string PickRandomForFrame(int frameIndex)
        {
            bool screenHasVideo = activeVideoFrameIndex.HasValue;
            string path = mediaCatalog.PickRandomForFrame(screenHasVideo);

            if (string.IsNullOrEmpty(path)) return null;

            if (MediaCatalog.IsVideoFile(path)
                && screenHasVideo
                && activeVideoFrameIndex != frameIndex)
            {
                path = mediaCatalog.PickRandomForFrame(true);
            }

            return path;
        }

        private void DisplayFileOnFrame(int frameIndex, string filePath, string reason)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            if (MediaCatalog.IsVideoFile(filePath))
            {
                if (activeVideoFrameIndex.HasValue && activeVideoFrameIndex != frameIndex)
                {
                    string fallback = mediaCatalog.PickRandomForFrame(true);
                    if (string.IsNullOrEmpty(fallback) || MediaCatalog.IsVideoFile(fallback))
                        return;

                    frameHistories[frameIndex].SetCurrentEntry(fallback);
                    ShowImageOnFrame(frameIndex, fallback);
                    frameTimers[frameIndex].Start();
                    WriteDebugLog($"Frame {frameIndex} ({reason}): image fallback {Path.GetFileName(fallback)}");
                    return;
                }

                if (activeVideoFrameIndex == frameIndex)
                    frames[frameIndex].StopVideoPlayback();

                activeVideoFrameIndex = frameIndex;
                frames[frameIndex].ShowVideo(filePath);
                frameTimers[frameIndex].Stop();
                WriteDebugLog($"Frame {frameIndex} ({reason}): video {Path.GetFileName(filePath)} [history {frameHistories[frameIndex].CurrentIndex + 1}/{frameHistories[frameIndex].Count}]");
            }
            else
            {
                if (activeVideoFrameIndex == frameIndex)
                {
                    frames[frameIndex].StopVideoPlayback();
                    activeVideoFrameIndex = null;
                }

                ShowImageOnFrame(frameIndex, filePath);
                frameTimers[frameIndex].Start();
                WriteDebugLog($"Frame {frameIndex} ({reason}): image {Path.GetFileName(filePath)} [history {frameHistories[frameIndex].CurrentIndex + 1}/{frameHistories[frameIndex].Count}]");
            }
        }

        private void ResetFrameTimer(int frameIndex)
        {
            if (frameTimers == null || frameIndex < 0 || frameIndex >= frameTimers.Length)
                return;

            if (frames[frameIndex].IsVideoActive)
                return;

            frameTimers[frameIndex].Stop();
            frameTimers[frameIndex].Start();
        }

        private void ShowImageOnFrame(int frameIndex, string filePath)
        {
            if (!File.Exists(filePath))
            {
                ResetSharedCatalog();
                mediaCatalog = GetSharedCatalog(registryManager);
                return;
            }

            frames[frameIndex].ConfigureDisplay(showFileName, fileNameFont, fileNameColor, fileNameDisplayMode);
            AnimationTypes effect = useEffects ? effectsList.PickRandom() : AnimationTypes.None;
            frames[frameIndex].ShowImage(filePath, effect, animationStepInterval, effectDurationVal, FormatDisplayName(filePath));
        }

        private string FormatDisplayName(string filePath)
        {
            switch (fileNameDisplayMode)
            {
                case 0: return filePath;
                case 1: return GetRelativePath(filePath);
                default: return Path.GetFileName(filePath);
            }
        }

        private void LoadFileNameSettings()
        {
            showFileName = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_SHOW_FILENAME, true);
            if (!int.TryParse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE, "2"), out fileNameDisplayMode))
                fileNameDisplayMode = 2;

            string fontString = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_FONT, "");
            string colorString = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILENAME_COLOR, "White");
            fileNameFont = !string.IsNullOrEmpty(fontString) ? StringToFont(fontString) : new Font("Arial", 12, FontStyle.Regular);
            fileNameColor = !string.IsNullOrEmpty(colorString) ? ColorTranslator.FromHtml(colorString) : Color.White;
        }

        private void LoadEffectSettings()
        {
            useEffects = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_USE_EFFECTS, true);

            string durationStr = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_EFFECT_DURATION, "1");
            if (!float.TryParse(durationStr, out effectDurationVal))
                effectDurationVal = 1f;
            effectsList.Clear();

            if (useEffects)
            {
                string effectsStr = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_EFFECTS, "");
                if (!string.IsNullOrEmpty(effectsStr))
                {
                    foreach (string effect in effectsStr.Split(';'))
                    {
                        if (Enum.TryParse(effect, out AnimationTypes animationType))
                            effectsList.Add(animationType);
                    }
                }
            }

            if (!useEffects || effectsList.Count == 0)
            {
                effectsList.Clear();
                effectsList.Add(AnimationTypes.None);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((DateTime.Now - lastKeyPressTime) < KEY_PRESS_DELAY)
                return true;
            lastKeyPressTime = DateTime.Now;

            switch (keyData)
            {
                case Keys.Back:
                case Keys.Left:
                    if (frames != null)
                    {
                        for (int i = 0; i < frames.Length; i++)
                        {
                            NavigateFramePrevious(i);
                            ResetFrameTimer(i);
                        }
                    }
                    return true;
                case Keys.Next:
                case Keys.Right:
                    if (frames != null)
                    {
                        for (int i = 0; i < frames.Length; i++)
                        {
                            NavigateFrameNext(i);
                            ResetFrameTimer(i);
                        }
                    }
                    return true;
                case Keys.Escape:
                    if (isPreviewMode)
                    {
                        Close();
                        Application.Exit();
                    }
                    else if (parent != null)
                    {
                        parent.CloseAllFrames();
                    }
                    else
                    {
                        Application.Exit();
                    }
                    return true;
                default:
                    return true;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (!isPreviewMode && screen != null)
                Bounds = screen.Bounds;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (isPreviewMode) return;

            try
            {
                Screen actualScreen = Screen.FromHandle(Handle);
                if (screen == null || !screen.DeviceName.Equals(actualScreen.DeviceName, StringComparison.OrdinalIgnoreCase))
                    screen = actualScreen;
                Bounds = screen.Bounds;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Form1_Shown error: {ex.Message}");
            }
        }

        private Font StringToFont(string fontString)
        {
            try
            {
                string[] parts = fontString.Split(';');
                if (parts.Length < 3)
                    throw new ArgumentException("Invalid font string format.");
                if (!Enum.TryParse(parts[2], true, out FontStyle style))
                    style = FontStyle.Regular;
                return new Font(parts[0], float.Parse(parts[1]), style);
            }
            catch
            {
                return new Font("Arial", 12, FontStyle.Regular);
            }
        }

        public void UpdateFontSettings()
        {
            LoadFileNameSettings();
            if (frames == null) return;
            foreach (MediaFrameSlot slot in frames)
                slot.ConfigureDisplay(showFileName, fileNameFont, fileNameColor, fileNameDisplayMode);
        }

        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || imageFolders == null)
                return fullPath;

            try
            {
                string bestMatch = null;
                int bestMatchLength = -1;
                fullPath = Path.GetFullPath(fullPath);

                foreach (var folderEntry in imageFolders)
                {
                    string baseFolder = Path.GetFullPath(folderEntry.Key.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                    if (fullPath.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase)
                        && baseFolder.Length > bestMatchLength)
                    {
                        bestMatch = baseFolder;
                        bestMatchLength = baseFolder.Length;
                    }
                }

                return bestMatch != null ? fullPath.Substring(bestMatchLength) : fullPath;
            }
            catch
            {
                return fullPath;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (frameTimers != null)
                {
                    foreach (Timer t in frameTimers)
                    {
                        if (t != null)
                        {
                            t.Stop();
                            t.Dispose();
                        }
                    }
                }
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
