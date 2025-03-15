using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenSaver
{
    public partial class Form1 : Form
    {
        // Add debug constants
        private const string LOG_FILE_NAME = "screensaver_debug.log";
        private static readonly string LOG_FILE_PATH = Path.Combine(Path.GetTempPath(), LOG_FILE_NAME);
        private bool isDebugMode = false;

        private RegistryManager registryManager;
        private Boolean useEffects;
        private Boolean showFileName;
        private int fileNameDisplayMode;
        private Font fileNameFont;
        private Color fileNameColor;
        private float effectDurationVal;
        private int delayBetweenImages;
        private List<AnimationTypes> effectsList = new List<AnimationTypes>();
        private List<String> images = new List<string>();
        private AnimationControl animationControl; // Single control instead of list
        private int imagesCount = 0;
        private Stack<string> imageStack;
        private int animationSteps = 15;
        private int animationStepInterval;
        private int previous = 0;
        private Screen screen;
        private Settings parent;
        private bool isPreviewMode = false;

        private int ScreenNumber
        {
            get
            {
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    if (Screen.AllScreens[i] == screen)
                        return i;
                }
                return 0;
            }
        }

        private void WriteDebugLog(string message)
        {
            if (!isDebugMode) return;

            try
            {
                string screenInfo = screen != null ? screen.DeviceName : "NoScreen";
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{screenInfo}] {message}";
                File.AppendAllText(LOG_FILE_PATH, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        public Form1(Settings parent, Screen screen, bool isPreview = false)
        {
            // Check debug mode
            this.registryManager = new RegistryManager();
            isDebugMode = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);

            InitializeComponent();

            this.screen = screen;
            this.parent = parent;
            this.isPreviewMode = isPreview;

            WriteDebugLog($"Initializing screensaver form (Preview: {isPreview})");
            WriteDebugLog($"Command line arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");

            // Load delay between images setting (in seconds) and set timer interval (in milliseconds)
            delayBetweenImages = int.Parse(registryManager.getRegistryProperty(RegistryConstants.REG_KEY_DELAY_BETWEEN_IMAGES, "5"));
            timer1.Interval = delayBetweenImages * 1000;
            WriteDebugLog($"Setting image refresh interval to {delayBetweenImages} seconds");

            this.FormBorderStyle = FormBorderStyle.None;
            this.Top = screen.Bounds.Top;
            this.Left = screen.Bounds.Left;
            this.ClientSize = new System.Drawing.Size(screen.Bounds.Width, screen.Bounds.Height);
            this.ShowInTaskbar = false;

            WriteDebugLog($"Loading settings for screen {screen.DeviceName} at {screen.Bounds}");

            // Load all settings first
            LoadFileNameSettings();
            LoadEffectSettings();
            LoadImageFiles();

            // Initialize single animation control with all settings
            InitializeAnimationControl();

            animationStepInterval = (int)(effectDurationVal * 1000 / animationSteps);
            timer1_Tick(null, null);

            WriteDebugLog("Screensaver form initialization complete");
        }

        private void InitializeAnimationControl()
        {
            WriteDebugLog("Creating animation control");
            
            animationControl = new AnimationControl();
            animationControl.Dock = DockStyle.Fill;
            
            // Apply all settings immediately
            animationControl.AnimationSpeed = effectDurationVal;
            animationControl.BorderColor = Color.Transparent;
            animationControl.BackColor = Color.Black;
            
            // Always set font settings, let control handle visibility
            animationControl.showFileName = showFileName;
            animationControl.FileNameFont = fileNameFont;
            animationControl.FileNameColor = fileNameColor;
            animationControl.FileNameDisplayMode = fileNameDisplayMode;

            this.Controls.Add(animationControl);
            
            WriteDebugLog($"Animation control created with settings: ShowFileName={showFileName}, FileNameDisplayMode={fileNameDisplayMode}");
        }

        private void LoadFileNameSettings()
        {
            showFileName = bool.Parse(RegistryManager.GetValue(RegistryConstants.REG_KEY_SHOW_FILENAME, "False"));
            fileNameDisplayMode = int.Parse(RegistryManager.GetValue(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE, "2"));
            string fontString = RegistryManager.GetValue(RegistryConstants.REG_KEY_FILENAME_FONT, "");
            string colorString = RegistryManager.GetValue(RegistryConstants.REG_KEY_FILENAME_COLOR, "White");

            // Always set font and color
            fileNameFont = !string.IsNullOrEmpty(fontString) ? StringToFont(fontString) : new Font("Arial", 12, FontStyle.Regular);
            fileNameColor = !string.IsNullOrEmpty(colorString) ? ColorTranslator.FromHtml(colorString) : Color.White;
        }

        private void LoadEffectSettings()
        {
            // Get transitions setting directly from Settings form
            useEffects = parent.UseTransitions;
            effectDurationVal = float.Parse(RegistryManager.GetValue(RegistryConstants.REG_KEY_EFFECT_DURATION, "2"));

            // Clear existing effects list
            effectsList.Clear();

            // Only load effects list if transitions are enabled
            if (useEffects)
            {
                string effectsStr = RegistryManager.GetValue(RegistryConstants.REG_KEY_EFFECTS, "");
                if (!string.IsNullOrEmpty(effectsStr))
                {
                    string[] effects = effectsStr.Split(';');
                    foreach (string effect in effects)
                    {
                        if (Enum.TryParse(effect, out AnimationTypes animationType))
                        {
                            effectsList.Add(animationType);
                        }
                    }
                }
            }

            // If transitions are disabled or no effects were loaded, use None
            if (!useEffects || effectsList.Count == 0)
            {
                effectsList.Clear();
                effectsList.Add(AnimationTypes.None);
            }
        }

        private void LoadImageFiles()
        {
            images.Clear();
            imageStack = new Stack<string>();

            // Get folders from registry
            SortedDictionary<string, bool> imageFolders = registryManager.getImageFolders();
            
            // If no folders configured, add user's Pictures folder as default
            if (imageFolders.Count == 0)
            {
                string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (Directory.Exists(picturesPath))
                {
                    WriteDebugLog($"No folders configured, adding default Pictures folder: {picturesPath}");
                    imageFolders.Add(picturesPath, false); // Add without subfolders by default
                }
            }
            
            // Get selected file types from registry
            string fileTypes = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILE_TYPES);
            if (string.IsNullOrEmpty(fileTypes))
            {
                fileTypes = "*.jpg;*.jpeg;*.png;*.bmp;*.gif"; // Default file types if none selected
            }
            string[] supportedExtensions = fileTypes.Split(';');

            // Load files from each folder
            foreach (KeyValuePair<string, bool> folderEntry in imageFolders)
            {
                string folder = folderEntry.Key;
                bool includeSubfolders = folderEntry.Value;
                
                if (Directory.Exists(folder))
                {
                    try
                    {
                        foreach (string extension in supportedExtensions)
                        {
                            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                            images.AddRange(Directory.GetFiles(folder, extension, searchOption));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"Error accessing folder {folder}: {ex.Message}");
                        Console.WriteLine($"Error accessing folder {folder}: {ex.Message}");
                    }
                }
                else
                {
                    WriteDebugLog($"{folder} doesn't exists");
                }
            }

            //check for empty list
            imagesCount = images.Count;
            if (imagesCount == 0)
            {
                WriteDebugLog("No images found in configured folders, using embedded resources");
                
                // Get all embedded resources
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(name => name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

                foreach (string resourceName in resourceNames)
                {
                    try
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        using (var image = Image.FromStream(stream))
                        {
                            string tempPath = Path.Combine(
                                Path.GetTempPath(), 
                                $"screensaver_resource_{Path.GetFileName(resourceName)}"
                            );
                            image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                            images.Add(tempPath);
                            WriteDebugLog($"Added embedded resource: {resourceName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"Error loading embedded resource {resourceName}: {ex.Message}");
                    }
                }

            }

            // Randomize the list
            Random rnd = new Random();
            for (int i = images.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                string temp = images[i];
                images[i] = images[j];
                images[j] = temp;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            showNextImage();
        }

        private void showNextImage()
        {
            // Set animation type
            AnimationTypes selectedEffect = useEffects ? effectsList.PickRandom() : AnimationTypes.None;
            animationControl.AnimationType = selectedEffect;
            //WriteDebugLog($"Selected animation effect: {selectedEffect}");

            // Select next file
            String fileName;
            if (previous < imageStack.Count - 1)
            {
                String[] imageStackArray = imageStack.ToArray();
                fileName = imageStackArray[previous++];
            }
            else
            {
                fileName = images.PickRandom();
                imageStack.Push(fileName);
                previous = imageStack.Count - 1;
            }

            // Check if file exists
            if (!File.Exists(fileName))
            {
                LoadImageFiles();
                fileName = images.PickRandom();
                imageStack.Push(fileName);
                previous = imageStack.Count - 1;
            }

            try
            {
                Bitmap bmp = new Bitmap(fileName);
                animationControl.AnimatedImage = bmp;

                // Update filename display based on selected mode
                string displayName;
                switch (fileNameDisplayMode)
                {
                    case 0: // Full Path
                        displayName = fileName;
                        break;
                    case 1: // Relative Path
                        string rootPath = Path.GetDirectoryName(Application.ExecutablePath);
                        displayName = GetRelativePath(rootPath, fileName);
                        break;
                    case 2: // File Name Only
                        displayName = Path.GetFileName(fileName);
                        break;
                    default:
                        displayName = fileName;
                        break;
                }
                
                animationControl.imageName = displayName;
                //WriteDebugLog($"Displaying image: {fileName} (Shown as: {displayName})");

                animationControl.Animate(animationStepInterval);
            }
            catch (Exception ex)
            {
                WriteDebugLog($"Error loading image {fileName}: {ex.Message}");
                showNextImage();
                return;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1_Tick(null, null);
            timer1.Enabled = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Back:
                case Keys.Left:
                    //reset timer
                    timer1.Stop();
                    timer1.Start();
                    showPreviousImage();
                    return true;
                case Keys.Next:
                case Keys.Right:
                    timer1.Stop();
                    timer1.Start();
                    showNextImage();
                    return true;
                case Keys.Escape:
                    if (isPreviewMode)
                    {
                        this.Close();
                        Application.Exit();
                    }
                    else
                    {
                        parent?.CloseAllFrames();
                    }
                    return true;
                default:
                    return true;
            }
        }

        private void showPreviousImage()
        {
            String[] imageStackArray = imageStack.ToArray();
            if (imageStackArray.Length == 0)
            {
                return;
            }

            if (previous >= 0)
            {
                // Set animation type
                if (useEffects)
                    animationControl.AnimationType = effectsList.PickRandom();
                else
                    animationControl.AnimationType = AnimationTypes.None;

                try
                {
                    string fileName = imageStackArray[previous];
                    Bitmap bmp = new Bitmap(fileName);
                    animationControl.AnimatedImage = bmp;

                    // Update filename display based on selected mode
                    switch (fileNameDisplayMode)
                    {
                        case 0: // Full Path
                            animationControl.imageName = fileName;
                            break;
                        case 1: // Relative Path
                            string rootPath = Path.GetDirectoryName(Application.ExecutablePath);
                            animationControl.imageName = GetRelativePath(rootPath, fileName);
                            break;
                        case 2: // File Name Only
                            animationControl.imageName = Path.GetFileName(fileName);
                            break;
                    }

                    animationControl.Animate(animationStepInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading file {imageStackArray[previous]}: {ex.Message}");
                    if (previous > 0) previous--;
                    showPreviousImage();
                    return;
                }
            }
            if (previous > 0) { previous--; }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Top = screen.Bounds.Top;
            this.Left = screen.Bounds.Left;
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
                return new Font("Arial", 12, FontStyle.Regular);
            }
        }

        public void UpdateFontSettings()
        {
            LoadFileNameSettings();
            if (animationControl != null)
            {
                animationControl.showFileName = showFileName;
                animationControl.FileNameFont = fileNameFont;
                animationControl.FileNameColor = fileNameColor;
                animationControl.FileNameDisplayMode = fileNameDisplayMode;
            }
        }

        private string GetRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return fullPath;

            try
            {
                // Convert paths to use the same directory separator
                rootPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar);
                fullPath = Path.GetFullPath(fullPath);

                // Check if paths are on different drives
                if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                    return fullPath;

                // Remove the root path and leading separator
                string relativePath = fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
                return string.IsNullOrEmpty(relativePath) ? "." : relativePath;
            }
            catch
            {
                // If any error occurs (like invalid paths), return the full path
                return fullPath;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            WriteDebugLog($"Form closing (Preview: {isPreviewMode})");
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WriteDebugLog($"Disposing form resources (Preview: {isPreviewMode})");
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
