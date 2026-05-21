using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenSaver
{
    internal class MediaFrameSlot : Panel
    {
        private readonly AnimationControl imageControl;
        private AxWMPLib.AxWindowsMediaPlayer videoPlayer;
        private bool videoPlayerInitialized;
        private bool isVideoActive;
        private bool videoMuted = true;
        private int videoClipLengthSeconds = 30;
        private int effectiveMaxPlaySeconds = 30;
        private DateTime videoPlayStartTime = DateTime.MinValue;
        private Timer videoDurationTimer;
        private string currentVideoPath;
        private bool videoEndSignaled;

        public event EventHandler VideoEnded;
        public event EventHandler VideoStopped;

        public bool IsVideoActive => isVideoActive;

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
        }

        public void ConfigureDisplay(bool showFileName, Font fileNameFont, Color fileNameColor, int fileNameDisplayMode)
        {
            imageControl.showFileName = showFileName;
            imageControl.FileNameFont = fileNameFont;
            imageControl.FileNameColor = fileNameColor;
            imageControl.FileNameDisplayMode = fileNameDisplayMode;
        }

        public void ConfigureVideo(bool mute, int clipLengthSeconds)
        {
            videoMuted = mute;
            videoClipLengthSeconds = Math.Max(1, clipLengthSeconds);
            ApplyMuteSettings();
        }

        public void ShowImage(string filePath, AnimationTypes effect, int animationStepInterval, float effectDuration, string displayName = null)
        {
            StopVideoPlayback();

            if (!File.Exists(filePath)) return;

            try
            {
                imageControl.Visible = true;
                imageControl.BringToFront();
                imageControl.AnimationSpeed = effectDuration;
                imageControl.AnimationType = effect;
                using (Bitmap bmp = new Bitmap(filePath))
                {
                    imageControl.AnimatedImage = new Bitmap(bmp);
                }
                imageControl.imageName = displayName ?? Path.GetFileName(filePath);
                imageControl.Animate(animationStepInterval);
            }
            catch (Exception ex)
            {
                Logger.WriteDebugLog($"MediaFrameSlot image error {filePath}: {ex.Message}");
            }
        }

        public void ShowVideo(string filePath)
        {
            if (!File.Exists(filePath)) return;

            EnsureVideoPlayer();
            imageControl.Visible = false;

            currentVideoPath = filePath;
            isVideoActive = true;
            videoPlayer.Visible = true;
            videoPlayer.BringToFront();
            videoEndSignaled = false;
            effectiveMaxPlaySeconds = videoClipLengthSeconds;
            videoPlayStartTime = DateTime.MinValue;

            videoPlayer.URL = filePath;
            videoPlayer.Ctlcontrols.play();
            ApplyMuteSettings();
            StartVideoDurationTimer();
        }

        public void StopVideoPlayback()
        {
            if (!isVideoActive && videoPlayer == null) return;

            isVideoActive = false;
            currentVideoPath = null;
            videoPlayStartTime = DateTime.MinValue;
            videoEndSignaled = false;

            if (videoDurationTimer != null)
            {
                videoDurationTimer.Stop();
            }

            if (videoPlayer != null)
            {
                videoPlayer.Ctlcontrols.stop();
                videoPlayer.Visible = false;
            }

            imageControl.Visible = true;
            imageControl.BringToFront();
            VideoStopped?.Invoke(this, EventArgs.Empty);
        }

        private void EnsureVideoPlayer()
        {
            if (videoPlayerInitialized) return;

            videoPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(videoPlayer)).BeginInit();
            videoPlayer.Dock = DockStyle.Fill;
            videoPlayer.Enabled = true;
            videoPlayer.PlayStateChange += VideoPlayer_PlayStateChange;
            Controls.Add(videoPlayer);
            ((System.ComponentModel.ISupportInitialize)(videoPlayer)).EndInit();

            videoPlayer.enableContextMenu = false;
            videoPlayer.uiMode = "none";
            videoPlayer.Visible = false;
            videoPlayerInitialized = true;

            videoDurationTimer = new Timer { Interval = 1000 };
            videoDurationTimer.Tick += VideoDurationTimer_Tick;
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
            // State 3 = playing
            if (e.newState == 3)
            {
                ApplyMuteSettings();
                UpdateEffectiveMaxPlaySeconds();
                videoPlayStartTime = DateTime.Now;
            }
            // State 8 = media ended (natural video length)
            else if (e.newState == 8)
            {
                SignalVideoEnded();
            }
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

        private void SignalVideoEnded()
        {
            if (videoEndSignaled || !isVideoActive)
                return;

            videoEndSignaled = true;
            if (videoDurationTimer != null)
                videoDurationTimer.Stop();

            VideoEnded?.Invoke(this, EventArgs.Empty);
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
                {
                    videoPlayer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
