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
        private static readonly object LiveFormsLock = new object();
        private static readonly List<WeakReference<Form1>> liveForms = new List<WeakReference<Form1>>();

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
        private Timer catalogWaitTimer;
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

        public static void EnforceGlobalSingleVideoPlayback()
        {
            lock (LiveFormsLock)
            {
                PruneLiveForms();
                Form1 keeperForm = null;
                int keeperFrame = -1;

                foreach (WeakReference<Form1> wr in liveForms)
                {
                    if (!wr.TryGetTarget(out Form1 form) || form.frames == null)
                        continue;

                    for (int i = 0; i < form.frames.Length; i++)
                    {
                        if (!form.frames[i].IsVideoActive)
                            continue;

                        if (keeperForm == null)
                        {
                            keeperForm = form;
                            keeperFrame = i;
                            form.activeVideoFrameIndex = i;
                        }
                        else
                        {
                            Logger.WriteErrorLog($"EnforceGlobalSingleVideoPlayback: stopping extra video on frame {i}");
                            form.frames[i].StopVideoPlayback();
                            if (form.activeVideoFrameIndex == i)
                                form.activeVideoFrameIndex = null;
                        }
                    }
                }
            }
        }

        private static void RegisterLiveForm(Form1 form)
        {
            lock (LiveFormsLock)
            {
                PruneLiveForms();
                liveForms.Add(new WeakReference<Form1>(form));
            }
        }

        private static void UnregisterLiveForm(Form1 form)
        {
            lock (LiveFormsLock)
            {
                for (int i = liveForms.Count - 1; i >= 0; i--)
                {
                    if (!liveForms[i].TryGetTarget(out Form1 live) || live == form)
                        liveForms.RemoveAt(i);
                }
            }
        }

        private static void PruneLiveForms()
        {
            for (int i = liveForms.Count - 1; i >= 0; i--)
            {
                if (!liveForms[i].TryGetTarget(out _))
                    liveForms.RemoveAt(i);
            }
        }

        private static void StopVideoOnOtherForms(Form1 exceptForm)
        {
            lock (LiveFormsLock)
            {
                PruneLiveForms();
                foreach (WeakReference<Form1> wr in liveForms)
                {
                    if (!wr.TryGetTarget(out Form1 form) || form == exceptForm)
                        continue;

                    form.StopAllFrameVideo();
                }
            }
        }

        private void StopAllFrameVideo()
        {
            if (frames == null) return;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].IsVideoActive)
                    frames[i].StopVideoPlayback();
            }

            activeVideoFrameIndex = null;
        }

        private bool IsAnotherFormPlayingVideo()
        {
            lock (LiveFormsLock)
            {
                PruneLiveForms();
                foreach (WeakReference<Form1> wr in liveForms)
                {
                    if (wr.TryGetTarget(out Form1 form) && form != this && form.HasActiveVideoFrame())
                        return true;
                }
            }
            return false;
        }

        private bool HasActiveVideoFrame()
        {
            if (frames == null) return false;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].IsVideoActive)
                    return true;
            }
            return false;
        }

        private static MediaCatalog GetSharedCatalog(RegistryManager registryManager, bool loadSynchronously = false)
        {
            lock (CatalogLock)
            {
                if (sharedCatalog == null)
                    sharedCatalog = new MediaCatalog(registryManager, loadSynchronously);
                return sharedCatalog;
            }
        }

        private void WriteDebugLog(string message)
        {
            if (!isDebugMode) return;
            WriteLog(message, isError: false);
        }

        private void WriteErrorLog(string message)
        {
            WriteLog(message, isError: true);
        }

        private void WriteErrorLog(string message, Exception ex)
        {
            Logger.WriteErrorLog(FormatLogMessage(message), ex);
        }

        private void WriteLog(string message, bool isError)
        {
            try
            {
                string formatted = FormatLogMessage(message);
                if (isError)
                    Logger.WriteErrorLog(formatted);
                else
                    Logger.WriteDebugLog(formatted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        private string FormatLogMessage(string message)
        {
            string screenInfo = screen != null ? screen.DeviceName : "NoScreen";
            return $"[{screenInfo}] {message}";
        }

        private string GetFrameStateSummary(int frameIndex)
        {
            if (frames == null || frameIndex < 0 || frameIndex >= frames.Length)
                return "invalid-frame-index";

            FrameMediaHistory history = frameHistories != null && frameIndex < frameHistories.Length
                ? frameHistories[frameIndex]
                : null;

            string historyInfo = history != null
                ? $"history={history.CurrentIndex + 1}/{history.Count} path={history.CurrentPath ?? "(none)"}"
                : "history=(none)";

            return $"frame={frameIndex} activeVideo={activeVideoFrameIndex?.ToString() ?? "none"} " +
                   $"slot[{frames[frameIndex].GetDiagnosticState()}] {historyInfo}";
        }

        public Form1(Settings parent, Screen screen, bool isPreview = false)
        {
            registryManager = new RegistryManager();
            isDebugMode = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);

            InitializeComponent();

            this.screen = screen;
            this.parent = parent;
            this.isPreviewMode = isPreview;

            // Catalog loads in the background so preview/screensaver forms can show immediately.
            mediaCatalog = GetSharedCatalog(registryManager, loadSynchronously: false);

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
            // Keep Settings visible when launching preview from the settings dialog.
            TopMost = parent == null && !isPreviewMode;
            BackColor = Color.Black;

            LoadFileNameSettings();
            LoadEffectSettings();
            imageFolders = registryManager.getImageFolders();

            RegisterLiveForm(this);

            InitializeFrameGrid();
            animationStepInterval = (int)(effectDurationVal * 1000 / animationSteps);

            Load += Form1_CatalogLoad;
        }

        private void Form1_CatalogLoad(object sender, EventArgs e)
        {
            StartInitialFrameDisplay();
        }

        private void StartInitialFrameDisplay()
        {
            if (frames == null || frames.Length == 0)
                return;

            if (mediaCatalog.HasMedia)
            {
                DisplayInitialFrames();
                return;
            }

            if (!mediaCatalog.IsLoading)
            {
                WriteErrorLog("Media catalog is empty; preview cannot show media.");
                return;
            }

            if (catalogWaitTimer != null)
                return;

            catalogWaitTimer = new Timer { Interval = 50 };
            catalogWaitTimer.Tick += CatalogWaitTimer_Tick;
            catalogWaitTimer.Start();
        }

        private void CatalogWaitTimer_Tick(object sender, EventArgs e)
        {
            if (mediaCatalog.HasMedia)
            {
                StopCatalogWaitTimer();
                DisplayInitialFrames();
                return;
            }

            if (!mediaCatalog.IsLoading)
            {
                StopCatalogWaitTimer();
                WriteErrorLog("Media catalog is empty after folder scan completed.");
            }
        }

        private void StopCatalogWaitTimer()
        {
            if (catalogWaitTimer == null)
                return;

            catalogWaitTimer.Stop();
            catalogWaitTimer.Dispose();
            catalogWaitTimer = null;
        }

        private void DisplayInitialFrames()
        {
            for (int i = 0; i < frames.Length; i++)
                NavigateFrameNext(i);

            EnforceSingleVideoPlayback();
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
                slot.VideoEnded += (s, e) => OnVideoEnded(slotIndex);
                slot.VideoAborted += (s, e) => OnVideoAborted(slotIndex);
                slot.VideoStopped += (s, e) =>
                {
                    if (activeVideoFrameIndex == slotIndex)
                        activeVideoFrameIndex = null;
                    if (!frames[slotIndex].IsVideoActive && frames[slotIndex].HasDisplayableImage)
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

        private void OnVideoEnded(int frameIndex)
        {
            WriteDebugLog($"OnVideoEnded {GetFrameStateSummary(frameIndex)}");

            if (activeVideoFrameIndex == frameIndex)
                activeVideoFrameIndex = null;

            // SignalVideoEnded already released WMP; advance to next history item.
            NavigateFrameNext(frameIndex, navSource: "video-ended");
        }

        private void OnVideoAborted(int frameIndex)
        {
            WriteErrorLog($"OnVideoAborted (unexpected stop) {GetFrameStateSummary(frameIndex)}");

            if (activeVideoFrameIndex == frameIndex)
                activeVideoFrameIndex = null;

            NavigateFrameNext(frameIndex, navSource: "video-aborted");
        }

        private void NavigateFrameNext(int frameIndex, bool userInitiated = false, string navSource = null)
        {
            if (string.IsNullOrEmpty(navSource))
                navSource = userInitiated ? "arrow-next" : "timer-next";
            if (!CanNavigateFrame(frameIndex))
            {
                WriteErrorLog($"NavigateFrameNext blocked ({navSource}): {GetFrameStateSummary(frameIndex)}");
                return;
            }

            SyncActiveVideoFrameIndex();

            // Timer must not advance while video is playing; arrow keys may browse history.
            if (frames[frameIndex].IsVideoActive)
            {
                if (!userInitiated)
                    return;

                StopVideoForUserNavigation(frameIndex);
            }

            string filePath = frameHistories[frameIndex].GoNext(() => PickRandomForFrame(frameIndex));
            if (string.IsNullOrEmpty(filePath))
            {
                WriteErrorLog($"NavigateFrameNext: no file path ({navSource}). {GetFrameStateSummary(frameIndex)}");
                if (userInitiated)
                    TryRecoverBlackFrame(frameIndex, navSource);
                return;
            }

            WriteDebugLog($"NavigateFrameNext ({navSource}): {Path.GetFileName(filePath)}. {GetFrameStateSummary(frameIndex)}");
            DisplayFileOnFrame(frameIndex, filePath, navSource);
        }

        private void NavigateFramePrevious(int frameIndex, bool userInitiated = false, string navSource = null)
        {
            if (string.IsNullOrEmpty(navSource))
                navSource = userInitiated ? "arrow-previous" : "timer-previous";
            if (!CanNavigateFrame(frameIndex))
            {
                WriteErrorLog($"NavigateFramePrevious blocked ({navSource}): {GetFrameStateSummary(frameIndex)}");
                return;
            }

            SyncActiveVideoFrameIndex();

            if (frames[frameIndex].IsVideoActive)
            {
                if (!userInitiated)
                    return;

                StopVideoForUserNavigation(frameIndex);
            }

            string filePath = frameHistories[frameIndex].GoPrevious(() => PickRandomForFrame(frameIndex));
            if (string.IsNullOrEmpty(filePath))
            {
                WriteErrorLog($"NavigateFramePrevious: no file path ({navSource}). {GetFrameStateSummary(frameIndex)}");
                if (userInitiated)
                    TryRecoverBlackFrame(frameIndex, navSource);
                return;
            }

            WriteDebugLog($"NavigateFramePrevious ({navSource}): {Path.GetFileName(filePath)}. {GetFrameStateSummary(frameIndex)}");
            DisplayFileOnFrame(frameIndex, filePath, navSource);
        }

        private void TryRecoverBlackFrame(int frameIndex, string reason)
        {
            WriteErrorLog($"TryRecoverBlackFrame ({reason}): attempting recovery. {GetFrameStateSummary(frameIndex)}");

            for (int attempt = 0; attempt < 10; attempt++)
            {
                string path = mediaCatalog.PickRandomForFrame(screenHasActiveVideoFrame: false);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;

                if (MediaCatalog.IsVideoFile(path) && (IsAnotherFormPlayingVideo() || activeVideoFrameIndex.HasValue))
                    continue;

                frameHistories[frameIndex].SetCurrentEntry(path);
                WriteDebugLog($"TryRecoverBlackFrame: using {Path.GetFileName(path)} on attempt {attempt + 1}");
                DisplayFileOnFrame(frameIndex, path, "recover");
                return;
            }

            WriteErrorLog($"TryRecoverBlackFrame failed ({reason}): frame may stay black. {GetFrameStateSummary(frameIndex)}");
        }

        private void StopVideoForUserNavigation(int frameIndex)
        {
            if (activeVideoFrameIndex == frameIndex)
                activeVideoFrameIndex = null;

            frames[frameIndex].StopVideoPlayback(raiseAbortedEvent: false);
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
            SyncActiveVideoFrameIndex();
            bool screenHasVideo = activeVideoFrameIndex.HasValue || IsAnotherFormPlayingVideo();
            string path = mediaCatalog.PickRandomForFrame(screenHasVideo);

            if (string.IsNullOrEmpty(path))
            {
                WriteErrorLog($"PickRandomForFrame: catalog returned null (screenHasVideo={screenHasVideo}). {GetFrameStateSummary(frameIndex)}");
                return null;
            }

            if (MediaCatalog.IsVideoFile(path)
                && screenHasVideo
                && activeVideoFrameIndex != frameIndex)
            {
                string imagePath = mediaCatalog.PickRandomForFrame(true);
                if (string.IsNullOrEmpty(imagePath))
                    WriteErrorLog($"PickRandomForFrame: video pick blocked and no image alternative. {GetFrameStateSummary(frameIndex)}");
                path = imagePath;
            }

            return path;
        }

        private void DisplayFileOnFrame(int frameIndex, string filePath, string reason)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                WriteErrorLog($"DisplayFileOnFrame ({reason}): empty path. {GetFrameStateSummary(frameIndex)}");
                return;
            }

            if (!File.Exists(filePath))
            {
                WriteErrorLog($"DisplayFileOnFrame ({reason}): file missing '{filePath}'. {GetFrameStateSummary(frameIndex)}");
                RemoveInvalidFileFromCatalogAndHistory(frameIndex, filePath, "file missing on disk");
                TryRecoverBlackFrame(frameIndex, reason);
                return;
            }

            SyncActiveVideoFrameIndex();

            if (MediaCatalog.IsVideoFile(filePath))
            {
                if (IsAnotherFormPlayingVideo())
                {
                    string fallback = mediaCatalog.PickRandomForFrame(true);
                    if (string.IsNullOrEmpty(fallback) || MediaCatalog.IsVideoFile(fallback))
                    {
                        WriteErrorLog($"DisplayFileOnFrame ({reason}): video blocked (other screen), no image fallback. {GetFrameStateSummary(frameIndex)}");
                        if (reason.StartsWith("arrow", StringComparison.Ordinal))
                            TryRecoverBlackFrame(frameIndex, reason);
                        return;
                    }

                    frameHistories[frameIndex].SetCurrentEntry(fallback);
                    if (!ShowImageOnFrame(frameIndex, fallback))
                        TryRecoverBlackFrame(frameIndex, reason);
                    else
                        frameTimers[frameIndex].Start();
                    WriteDebugLog($"Frame {frameIndex} ({reason}): image fallback (video on another screen) {Path.GetFileName(fallback)}");
                    return;
                }

                if (activeVideoFrameIndex.HasValue && activeVideoFrameIndex != frameIndex)
                {
                    string fallback = mediaCatalog.PickRandomForFrame(true);
                    if (string.IsNullOrEmpty(fallback) || MediaCatalog.IsVideoFile(fallback))
                    {
                        WriteErrorLog($"DisplayFileOnFrame ({reason}): video blocked (other frame), no image fallback. {GetFrameStateSummary(frameIndex)}");
                        if (reason.StartsWith("arrow", StringComparison.Ordinal))
                            TryRecoverBlackFrame(frameIndex, reason);
                        return;
                    }

                    frameHistories[frameIndex].SetCurrentEntry(fallback);
                    if (!ShowImageOnFrame(frameIndex, fallback))
                        TryRecoverBlackFrame(frameIndex, reason);
                    else
                        frameTimers[frameIndex].Start();
                    WriteDebugLog($"Frame {frameIndex} ({reason}): image fallback {Path.GetFileName(fallback)}");
                    return;
                }

                StopVideoOnOtherForms(this);
                StopVideoOnAllFramesExcept(frameIndex);
                activeVideoFrameIndex = frameIndex;
                frames[frameIndex].ConfigureDisplay(showFileName, fileNameFont, fileNameColor, fileNameDisplayMode);
                bool afterVideoEnd = string.Equals(reason, "video-ended", StringComparison.Ordinal);
                if (!frames[frameIndex].ShowVideo(filePath, FormatDisplayName(filePath), afterVideoEnd))
                {
                    WriteErrorLog($"DisplayFileOnFrame ({reason}): ShowVideo failed. {GetFrameStateSummary(frameIndex)}");
                    activeVideoFrameIndex = null;
                    TryRecoverBlackFrame(frameIndex, reason);
                    return;
                }
                frameTimers[frameIndex].Stop();
                WriteDebugLog($"Frame {frameIndex} ({reason}): video {Path.GetFileName(filePath)} [history {frameHistories[frameIndex].CurrentIndex + 1}/{frameHistories[frameIndex].Count}]");
            }
            else
            {
                if (activeVideoFrameIndex == frameIndex)
                    activeVideoFrameIndex = null;

                if (frames[frameIndex].IsVideoActive)
                    frames[frameIndex].StopVideoPlayback();

                if (!ShowImageOnFrame(frameIndex, filePath))
                    TryRecoverBlackFrame(frameIndex, reason);
                else
                    frameTimers[frameIndex].Start();
                WriteDebugLog($"Frame {frameIndex} ({reason}): image {Path.GetFileName(filePath)} [history {frameHistories[frameIndex].CurrentIndex + 1}/{frameHistories[frameIndex].Count}]");
            }
        }

        private void SyncActiveVideoFrameIndex()
        {
            int? playingIndex = null;
            for (int i = 0; i < frames.Length; i++)
            {
                if (!frames[i].IsVideoActive) continue;

                if (!playingIndex.HasValue)
                    playingIndex = i;
            }

            activeVideoFrameIndex = playingIndex;
        }

        private void StopVideoOnAllFramesExcept(int? exceptFrameIndex)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (exceptFrameIndex.HasValue && i == exceptFrameIndex.Value)
                    continue;

                if (frames[i].IsVideoActive)
                    frames[i].StopVideoPlayback();
            }

            activeVideoFrameIndex = exceptFrameIndex;
        }

        private void EnforceSingleVideoPlayback()
        {
            if (frames == null) return;

            int? keeper = null;
            for (int i = 0; i < frames.Length; i++)
            {
                if (!frames[i].IsVideoActive) continue;

                if (!keeper.HasValue)
                    keeper = i;
                else
                    frames[i].StopVideoPlayback();
            }

            activeVideoFrameIndex = keeper;
            EnforceGlobalSingleVideoPlayback();
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

        private bool ShowImageOnFrame(int frameIndex, string filePath)
        {
            if (!File.Exists(filePath))
            {
                WriteErrorLog($"ShowImageOnFrame: file missing '{filePath}'. {GetFrameStateSummary(frameIndex)}");
                RemoveInvalidFileFromCatalogAndHistory(frameIndex, filePath, "file missing on disk");
                return false;
            }

            if (MediaCatalog.IsHeicOrHeifFile(filePath))
            {
                string ext = Path.GetExtension(filePath);
                RemoveInvalidFileFromCatalogAndHistory(
                    frameIndex,
                    filePath,
                    $"HEIC/HEIF image not supported (extension {ext})");
                return false;
            }

            frames[frameIndex].ConfigureDisplay(showFileName, fileNameFont, fileNameColor, fileNameDisplayMode);
            AnimationTypes effect = useEffects ? effectsList.PickRandom() : AnimationTypes.None;
            bool shown = frames[frameIndex].ShowImage(
                filePath, effect, animationStepInterval, effectDurationVal, FormatDisplayName(filePath));

            if (!shown)
            {
                WriteErrorLog($"ShowImageOnFrame failed for '{filePath}'. {GetFrameStateSummary(frameIndex)}");
                RemoveInvalidFileFromCatalogAndHistory(
                    frameIndex,
                    filePath,
                    "image decode failed (invalid or unsupported format)");
            }

            return shown;
        }

        private void RemoveInvalidFileFromCatalogAndHistory(int frameIndex, string filePath, string reason)
        {
            mediaCatalog?.RemoveInvalidFile(filePath, reason);

            if (frameHistories != null && frameIndex >= 0 && frameIndex < frameHistories.Length)
                frameHistories[frameIndex].RemovePath(filePath);
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
                            NavigateFramePrevious(i, userInitiated: true);
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
                            NavigateFrameNext(i, userInitiated: true);
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
                UnregisterLiveForm(this);
                StopCatalogWaitTimer();

                if (frames != null)
                {
                    foreach (MediaFrameSlot slot in frames)
                    {
                        if (slot != null && slot.IsVideoActive)
                            slot.StopVideoPlayback();
                    }
                }

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
