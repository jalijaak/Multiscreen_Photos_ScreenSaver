using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenSaver
{
    internal class MediaFrameSlot : Panel
    {
        private readonly AnimationControl imageControl;
        private readonly Label fileNameLabel;
        private AxWMPLib.AxWindowsMediaPlayer videoPlayer;
        private bool videoPlayerInitialized;
        private bool showFileName;
        private bool isVideoActive;
        private bool videoMuted = true;
        private int videoClipLengthSeconds = 30;
        private int effectiveMaxPlaySeconds = 30;
        private DateTime videoPlayStartTime = DateTime.MinValue;
        private Timer videoDurationTimer;
        private string currentVideoPath;
        private string currentVideoDisplayName;
        private bool videoEndSignaled;

        public event EventHandler VideoEnded;
        public event EventHandler VideoStopped;
        public event EventHandler VideoAborted;

        private int lastLoggedPlayState = -1;
        private static readonly Random videoRandom = new Random();

        public bool IsVideoActive => isVideoActive;

        public bool HasDisplayableImage => imageControl.AnimatedImage != null;

        public string GetDiagnosticState()
        {
            string videoPath = currentVideoPath ?? "(none)";
            string wmpState = videoPlayer == null ? "no-player" : $"visible={videoPlayer.Visible}";
            return $"videoActive={isVideoActive}, hasImage={HasDisplayableImage}, imageVisible={imageControl.Visible}, " +
                   $"wmp={wmpState}, path={videoPath}, videoEnded={videoEndSignaled}";
        }

        public MediaFrameSlot()
        {
            BackColor = Color.Black;
            BorderStyle = BorderStyle.FixedSingle;

            imageControl = new AnimationControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderColor = Color.Transparent
            };
            Controls.Add(imageControl);

            fileNameLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.Black,
                Location = new Point(10, 10),
                Visible = false,
                Padding = new Padding(4, 2, 4, 2)
            };
            Controls.Add(fileNameLabel);
        }

        public void ConfigureDisplay(bool showFileName, Font fileNameFont, Color fileNameColor, int fileNameDisplayMode)
        {
            this.showFileName = showFileName;
            imageControl.showFileName = showFileName;
            imageControl.FileNameFont = fileNameFont;
            imageControl.FileNameColor = fileNameColor;
            imageControl.FileNameDisplayMode = fileNameDisplayMode;

            fileNameLabel.Font = fileNameFont;
            fileNameLabel.ForeColor = fileNameColor;

            if (isVideoActive && !string.IsNullOrEmpty(currentVideoDisplayName))
                UpdateVideoFileNameLabel(currentVideoDisplayName);
            else
                fileNameLabel.Visible = false;
        }

        public void ConfigureVideo(bool mute, int clipLengthSeconds)
        {
            videoMuted = mute;
            videoClipLengthSeconds = Math.Max(1, clipLengthSeconds);
            ApplyMuteSettings();
        }

        public bool ShowImage(string filePath, AnimationTypes effect, int animationStepInterval, float effectDuration, string displayName = null)
        {
            StopVideoPlayback(raiseAbortedEvent: false);
            ClearAnimatedImage();

            if (!File.Exists(filePath))
            {
                Logger.WriteErrorLog($"ShowImage: file not found '{filePath}'. State: {GetDiagnosticState()}");
                HideEmptyImageSurface();
                return false;
            }

            long fileBytes = 0;
            try
            {
                fileBytes = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog($"ShowImage: cannot read file info '{filePath}'", ex);
            }

            try
            {
                imageControl.Visible = true;
                imageControl.BringToFront();
                imageControl.AnimationSpeed = effectDuration;
                imageControl.AnimationType = effect;
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (Image img = Image.FromStream(stream))
                {
                    imageControl.AnimatedImage = new Bitmap(img);
                }
                Logger.WriteDebugLog($"ShowImage OK: {Path.GetFileName(filePath)} ({fileBytes} bytes)");
                imageControl.imageName = displayName ?? Path.GetFileName(filePath);
                imageControl.Animate(animationStepInterval);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog($"ShowImage failed for '{filePath}' ({fileBytes} bytes)", ex);
                HideEmptyImageSurface();
                return false;
            }
        }

        public bool ShowVideo(string filePath, string displayName = null)
        {
            if (!File.Exists(filePath))
            {
                Logger.WriteErrorLog($"ShowVideo: file not found '{filePath}'. State: {GetDiagnosticState()}");
                return false;
            }

            EnsureVideoPlayer();
            ClearAnimatedImage();
            imageControl.Visible = false;
            currentVideoDisplayName = displayName ?? Path.GetFileName(filePath);
            UpdateVideoFileNameLabel(currentVideoDisplayName);

            if (isVideoActive)
            {
                try { videoPlayer.Ctlcontrols.stop(); } catch { }
            }

            currentVideoPath = filePath;
            isVideoActive = true;
            videoEndSignaled = false;
            lastLoggedPlayState = -1;
            effectiveMaxPlaySeconds = videoClipLengthSeconds;
            videoPlayStartTime = DateTime.MinValue;

            videoPlayer.URL = filePath;
            ApplyVideoDisplaySettings();
            videoPlayer.Visible = true;
            videoPlayer.BringToFront();
            if (fileNameLabel.Visible)
                fileNameLabel.BringToFront();
            videoPlayer.Ctlcontrols.play();
            ApplyMuteSettings();
            StartVideoDurationTimer();
            Logger.WriteDebugLog($"ShowVideo started: {Path.GetFileName(filePath)}");
            return true;
        }

        public void StopVideoPlayback(bool raiseAbortedEvent = true)
        {
            if (!isVideoActive && videoPlayer == null) return;

            bool wasActive = isVideoActive;
            bool endedNormally = videoEndSignaled;
            string pathForLog = currentVideoPath ?? "(none)";

            Logger.WriteDebugLog($"StopVideoPlayback. Before: {GetDiagnosticState()}");
            if (wasActive && !endedNormally)
            {
                Logger.WriteDebugLog(
                    $"StopVideoPlayback interrupted active video '{pathForLog}' (caller: {GetCallerName()}, raiseAborted={raiseAbortedEvent}). {GetDiagnosticState()}");
            }

            isVideoActive = false;
            currentVideoPath = null;
            currentVideoDisplayName = null;
            videoPlayStartTime = DateTime.MinValue;

            if (videoDurationTimer != null)
                videoDurationTimer.Stop();

            ReleaseWmpPlayback();
            HideVideoFileNameLabel();
            ShowImageSurfaceIfAvailable();

            VideoStopped?.Invoke(this, EventArgs.Empty);
            Logger.WriteDebugLog($"StopVideoPlayback done. After: {GetDiagnosticState()}");

            if (wasActive && !endedNormally && raiseAbortedEvent)
                VideoAborted?.Invoke(this, EventArgs.Empty);
        }

        private static string GetCallerName()
        {
            try
            {
                StackFrame frame = new StackFrame(2, false);
                return frame.GetMethod()?.Name ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private void ReleaseWmpPlayback()
        {
            if (videoPlayer == null) return;

            try { videoPlayer.Ctlcontrols.stop(); } catch (Exception ex) { Logger.WriteErrorLog("WMP stop failed", ex); }
            try { videoPlayer.close(); } catch (Exception ex) { Logger.WriteErrorLog("WMP close failed", ex); }

            videoPlayer.Visible = false;
        }

        private void ShowImageSurfaceIfAvailable()
        {
            if (HasDisplayableImage)
            {
                imageControl.Visible = true;
                imageControl.BringToFront();
            }
            else
            {
                HideEmptyImageSurface();
            }
        }

        private void HideEmptyImageSurface()
        {
            imageControl.Visible = false;
        }

        private void ClearAnimatedImage()
        {
            if (imageControl.AnimatedImage == null) return;

            imageControl.AnimatedImage.Dispose();
            imageControl.AnimatedImage = null;
        }

        private void EnsureVideoPlayer()
        {
            if (videoPlayerInitialized) return;

            videoPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(videoPlayer)).BeginInit();
            videoPlayer.Dock = DockStyle.Fill;
            videoPlayer.Enabled = true;
            videoPlayer.PlayStateChange += VideoPlayer_PlayStateChange;
            videoPlayer.ClickEvent += VideoPlayer_ClickEvent;
            Controls.Add(videoPlayer);
            ((System.ComponentModel.ISupportInitialize)(videoPlayer)).EndInit();

            videoPlayer.enableContextMenu = false;
            videoPlayer.uiMode = "none";
            videoPlayer.stretchToFit = true;
            videoPlayer.Visible = false;
            videoPlayer.Cursor = Cursors.Hand;
            videoPlayerInitialized = true;

            videoDurationTimer = new Timer { Interval = 1000 };
            videoDurationTimer.Tick += VideoDurationTimer_Tick;
        }

        private void VideoPlayer_ClickEvent(object sender, AxWMPLib._WMPOCXEvents_ClickEvent e)
        {
            if (!isVideoActive) return;
            ToggleVideoMute();
        }

        private void ToggleVideoMute()
        {
            videoMuted = !videoMuted;
            ApplyMuteSettings();
        }

        private void StartVideoDurationTimer()
        {
            if (videoDurationTimer == null) return;
            videoDurationTimer.Start();
        }

        private void VideoDurationTimer_Tick(object sender, EventArgs e)
        {
            if (!isVideoActive || videoPlayStartTime == DateTime.MinValue || videoEndSignaled)
                return;

            if ((DateTime.Now - videoPlayStartTime).TotalSeconds >= effectiveMaxPlaySeconds)
                SignalVideoEnded();
        }

        private void VideoPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState != lastLoggedPlayState)
            {
                lastLoggedPlayState = e.newState;
                Logger.WriteDebugLog($"WMP PlayStateChange: {e.newState} path={currentVideoPath ?? "(none)"}");
            }

            // State 3 = playing
            if (e.newState == 3)
            {
                ApplyVideoDisplaySettings();
                ApplyMuteSettings();
                UpdateEffectiveMaxPlaySeconds();
                ApplyRandomStartPositionIfNeeded();
                videoPlayStartTime = DateTime.Now;
            }
            // State 8 = media ended (natural video length)
            else if (e.newState == 8)
            {
                if (isVideoActive && !videoEndSignaled)
                    SignalVideoEnded();
            }
            // 1=stopped, 2=paused, 6=ready, 9=transition, 10=media failed
            else if (e.newState == 10)
            {
                HandleWmpMediaFailed();
            }
        }

        private void HandleWmpMediaFailed()
        {
            if (!isVideoActive || videoEndSignaled)
            {
                Logger.WriteDebugLog(
                    $"WMP state 10 ignored (cleanup). path={currentVideoPath ?? "(none)"} {GetDiagnosticState()}");
                return;
            }

            string wmpError = GetWmpErrorDescription();
            Logger.WriteErrorLog(
                $"WMP media failed for '{currentVideoPath}'. {wmpError} State: {GetDiagnosticState()}");
            SignalVideoEnded();
        }

        private string GetWmpErrorDescription()
        {
            try
            {
                if (videoPlayer != null)
                    return $"WMP playState={videoPlayer.playState}";
            }
            catch (Exception ex)
            {
                return $"WMP state unavailable ({ex.Message})";
            }

            if (!string.IsNullOrEmpty(currentVideoPath) && !File.Exists(currentVideoPath))
                return "file missing on disk";

            if (!string.IsNullOrEmpty(currentVideoPath))
            {
                try
                {
                    long bytes = new FileInfo(currentVideoPath).Length;
                    return $"file size={bytes} bytes (codec/OneDrive sync may block WMP)";
                }
                catch (Exception ex)
                {
                    return $"file size unreadable ({ex.Message})";
                }
            }

            return "no media path";
        }

        private void UpdateEffectiveMaxPlaySeconds()
        {
            effectiveMaxPlaySeconds = videoClipLengthSeconds;

            try
            {
                if (videoPlayer?.currentMedia != null)
                {
                    double naturalSeconds = videoPlayer.currentMedia.duration;
                    if (naturalSeconds > 0 && !double.IsInfinity(naturalSeconds) && !double.IsNaN(naturalSeconds))
                    {
                        int naturalLength = Math.Max(1, (int)Math.Ceiling(naturalSeconds));
                        effectiveMaxPlaySeconds = Math.Min(videoClipLengthSeconds, naturalLength);
                    }
                }
            }
            catch
            {
                effectiveMaxPlaySeconds = videoClipLengthSeconds;
            }
        }

        /// <summary>
        /// When the file is longer than the clip limit, start at a random point that still allows
        /// a full clip-length segment before the video ends.
        /// </summary>
        private void ApplyRandomStartPositionIfNeeded()
        {
            try
            {
                if (videoPlayer?.currentMedia == null || string.IsNullOrEmpty(currentVideoPath))
                    return;

                double naturalSeconds = videoPlayer.currentMedia.duration;
                if (naturalSeconds <= videoClipLengthSeconds
                    || naturalSeconds <= 0
                    || double.IsInfinity(naturalSeconds)
                    || double.IsNaN(naturalSeconds))
                {
                    return;
                }

                double maxStartSeconds = naturalSeconds - videoClipLengthSeconds;
                if (maxStartSeconds <= 0)
                    return;

                double startSeconds = videoRandom.NextDouble() * maxStartSeconds;
                videoPlayer.Ctlcontrols.currentPosition = startSeconds;
                Logger.WriteDebugLog(
                    $"Random video start at {startSeconds:F1}s (duration {naturalSeconds:F1}s, clip limit {videoClipLengthSeconds}s) for {Path.GetFileName(currentVideoPath)}");
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog($"ApplyRandomStartPosition failed for '{currentVideoPath}'", ex);
            }
        }

        private void SignalVideoEnded()
        {
            if (videoEndSignaled || !isVideoActive)
                return;

            videoEndSignaled = true;
            if (videoDurationTimer != null)
                videoDurationTimer.Stop();

            // End playback before notifying so coordinators see IsVideoActive=false.
            ReleaseWmpPlayback();
            isVideoActive = false;
            currentVideoPath = null;
            currentVideoDisplayName = null;
            videoPlayStartTime = DateTime.MinValue;
            HideVideoFileNameLabel();
            HideEmptyImageSurface();

            Logger.WriteDebugLog($"SignalVideoEnded. State: {GetDiagnosticState()}");
            VideoEnded?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateVideoFileNameLabel(string displayName)
        {
            if (!showFileName || string.IsNullOrEmpty(displayName))
            {
                fileNameLabel.Visible = false;
                return;
            }

            fileNameLabel.Text = displayName;
            fileNameLabel.Location = new Point(10, 10);
            fileNameLabel.Visible = true;
        }

        private void HideVideoFileNameLabel()
        {
            fileNameLabel.Visible = false;
            fileNameLabel.Text = string.Empty;
        }

        private void ApplyVideoDisplaySettings()
        {
            if (videoPlayer == null) return;

            try
            {
                videoPlayer.stretchToFit = true;
                videoPlayer.uiMode = "none";
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("ApplyVideoDisplaySettings failed", ex);
            }
        }

        private void ApplyMuteSettings()
        {
            if (videoPlayer?.settings == null) return;
            videoPlayer.settings.mute = videoMuted;
            videoPlayer.settings.volume = videoMuted ? 0 : 100;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (videoDurationTimer != null)
                {
                    videoDurationTimer.Stop();
                    videoDurationTimer.Dispose();
                }
                if (videoPlayer != null)
                    videoPlayer.Dispose();
                if (fileNameLabel != null)
                    fileNameLabel.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
