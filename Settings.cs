using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace ScreenSaver
{
    public partial class Settings : Form
    {
        private static RegistryManager registryManager;
        private List<Form> previewForms;

        public bool UseTransitions
        {
            get { return chkUseTransitions.Checked; }
        }        
        public bool UseAllScreens
        {
            get { return cbx_AllScreens.Checked; }
        }

        public Settings()
        {
            InitializeComponent();
            this.FormClosing += Settings_FormClosing;

            // Prevent form resizing
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // Set cogwheel icon using system icon
            try
            {
                this.Icon = System.Drawing.SystemIcons.Application;
            }
            catch
            {
                // Icon loading failed - continue without icon
            }

            // Add context menu to status label
            ContextMenuStrip statusLabelMenu = new ContextMenuStrip();
            ToolStripMenuItem copyMenuItem = new ToolStripMenuItem("Copy");
            copyMenuItem.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(toolStripStatusLabel.Text))
                {
                    Clipboard.SetText(toolStripStatusLabel.Text);
                }
            };
            statusLabelMenu.Items.Add(copyMenuItem);
            toolStripStatusLabel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && !string.IsNullOrEmpty(toolStripStatusLabel.Text))
                {
                    statusLabelMenu.Show(toolStrip1, e.Location);
                }
            };

            registryManager = new RegistryManager();

            // Initialize file name display controls
            comboBoxFileNameDisplay.Items.AddRange(new object[] { "Full Path", "Relative Path", "File Name Only" });
            comboBoxFileNameDisplay.SelectedIndex = 2;

            // Initialize file types list
            InitializeFileTypesList();

            // Load all settings from registry
            LoadSettings();

            dgvFoldersList.AllowDrop = true;
            dgvFoldersList.DragEnter += DgvFoldersList_DragEnter;
            dgvFoldersList.DragOver += DgvFoldersList_DragOver;
            dgvFoldersList.DragDrop += DgvFoldersList_DragDrop;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseAllFrames();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseAllFrames();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeFileTypesList()
        {
            fileTypesList.Items.Clear();
            string[] fileTypes = new string[] {
                "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.tif", "*.tiff",
                "*.mp4", "*.avi", "*.wmv", "*.mov"
            };

            foreach (string fileType in fileTypes)
            {
                ListViewItem item = new ListViewItem(fileType);
                item.Name = fileType;
                fileTypesList.Items.Add(item);
            }

            // Set the tag for the ListView to match the registry key
            fileTypesList.Tag = "FileTypes";
        }

        private void LoadSettings()
        {
            try
            {
                // Load debug setting (checkbox is on main form, not inside tabs)
                cbx_debug.Checked = RegistryManager.ParseYesNo(
                    registryManager.getRegistryProperty(RegistryConstants.REG_KEY_DEBUG, "No"), false);

                // Load all tagged controls
                foreach (Control c in this.Controls)
                {
                    LoadControlSettings(c);
                }

                // Load folders list
                LoadFoldersList();

                // Load file type selections
                LoadFileTypeSelections();

                // Load filename font + color explicitly (these aren't handled by LoadControlSettings).
                // This prevents overwriting saved registry values with designer defaults.
                string fontString = RegistryManager.GetValue(RegistryConstants.REG_KEY_FILENAME_FONT, "");
                labelFileNameSample.Font = !string.IsNullOrEmpty(fontString)
                    ? StringToFont(fontString)
                    : new Font("Arial", 12, FontStyle.Regular);

                string colorString = RegistryManager.GetValue(RegistryConstants.REG_KEY_FILENAME_COLOR, "White");
                labelFileNameSample.ForeColor = ColorTranslator.FromHtml(colorString);

                UpdateFileNameSamplePreviewBackground();

                LoadVideoSettings();

                // Update UI visibility based on loaded settings
                UpdateUIVisibility();
                UpdateDebugLogLinkVisibility();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "Error loading settings: " + ex.Message;
                toolStripStatusLabel.ForeColor = Color.Red;
            }
        }

        private void SaveControlSettings(Control parentCtrl)
        {
            foreach (Control cn in parentCtrl.Controls)
            {
                SaveControlSettings(cn);

                if (cn.Tag == null) continue;

                try
                {
                    string tag = cn.Tag.ToString();

                    switch (cn)
                    {
                        case CheckBox chkBx:
                            registryManager.setBooleanPropertyVal(tag, chkBx.Checked);
                            break;

                        case NumericUpDown nud:
                            registryManager.setRegistryProperty(tag, nud.Value.ToString());
                            break;

                        case ComboBox cb:
                            if (tag.Equals(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE)
                                || tag.Equals(RegistryConstants.REG_KEY_VideoDuration))
                            {
                                registryManager.setRegistryProperty(tag, cb.SelectedIndex.ToString());
                            }
                            else if (cb.SelectedItem != null)
                            {
                                registryManager.setRegistryProperty(tag, cb.SelectedItem.ToString());
                            }
                            break;

                        case ListView lv:
                            if (!lv.Tag.Equals("FileTypes"))
                                SaveListViewSettings(lv);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving control {cn.Name}: {ex.Message}");
                }
            }
        }

        private void SaveListViewSettings(ListView lv)
        {
            if (lv.Tag == null) return;

            StringBuilder selected = new StringBuilder();
            foreach (ListViewItem item in lv.Items)
            {
                if (!item.Checked) continue;

                if (selected.Length > 0)
                    selected.Append(";");
                selected.Append(item.Name);
            }
            registryManager.setRegistryProperty(lv.Tag.ToString(), selected.ToString());
        }

        private void LoadControlSettings(Control parentCtrl)
        {
            foreach (Control cn in parentCtrl.Controls)
            {
                LoadControlSettings(cn);

                if (cn.Tag == null) continue;

                try
                {
                    switch (cn)
                    {
                        case CheckBox chkBx:
                            chkBx.Checked = registryManager.getBooleanPropertyVal(chkBx.Tag.ToString());
                            break;

                        case NumericUpDown nud:
                            try
                            {
                                nud.Value = Convert.ToDecimal(registryManager.getRegistryProperty(nud.Tag.ToString()));
                            }
                            catch { }
                            break;

                        case ComboBox cb:
                            if (cb.Tag.ToString().Equals(RegistryConstants.REG_KEY_FILENAME_DISPLAY_MODE))
                            {
                                cb.SelectedIndex = Convert.ToInt32(registryManager.getRegistryProperty(cb.Tag.ToString()));
                            }
                            else if (cb.Tag.ToString().Equals(RegistryConstants.REG_KEY_VideoDuration))
                            {
                                // Loaded in LoadVideoSettings
                            }
                            else
                            {
                                if (cb.Items.Count == 0)
                                    cb.Items.AddRange(registryManager.getRegistryPropertyOptions(cb.Tag.ToString()).ToArray());
                                cb.SelectedItem = registryManager.getRegistryProperty(cb.Tag.ToString());
                            }
                            break;

                        case ListView lv:
                            LoadListViewSettings(lv);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"Error loading control {cn.Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Tag: {cn.Tag}");
                }
            }
        }

        private void LoadListViewSettings(ListView lv)
        {
            if (lv.Tag == null) return;

            // Skip adding items for file types as they're initialized separately
            if (!lv.Tag.Equals("FileTypes"))
            {
                foreach (String opt in registryManager.getRegistryPropertyOptions(lv.Tag.ToString()))
                {
                    ListViewItem lvi = lv.Items.Add(opt);
                    lvi.Name = opt;
                }
            }

            // Load selections
            string[] selections = registryManager.getRegistryProperty(lv.Tag.ToString()).Split(';');
            foreach (string val in selections)
            {
                if (string.IsNullOrEmpty(val)) continue;

                for (int i = 0; i < lv.Items.Count; i++)
                {
                    if (lv.Tag.Equals("FileTypes") ?
                        lv.Items[i].Name.Equals(val) :
                        lv.Items[i].Text.Equals(val))
                    {
                        lv.Items[i].Checked = true;
                        break;
                    }
                }
            }

            // Handle special case for effects
            if (lv.Tag.Equals("Effects"))
            {
                HandleEffectsListView(lv);
            }
        }

        private void HandleEffectsListView(ListView lv)
        {
            if (lv.Items.ContainsKey("None"))
            {
                lv.Items[lv.Items.IndexOfKey("None")].Remove();
            }

            foreach (ListViewItem lvi in lv.Items)
            {
                AnimationTypes type = (AnimationTypes)Enum.Parse(typeof(AnimationTypes), lvi.Name);
                Object[] attributes = type.GetType()
                    .GetField(type.ToString())
                    .GetCustomAttributes(typeof(DescriptionAttribute), false);
                lvi.Text = (attributes == null || attributes.Length == 0) ?
                    type.ToString() :
                    (attributes[0] as DescriptionAttribute).Description;
            }
        }

        private void LoadFoldersList()
        {
            try
            {
                dgvFoldersList.Rows.Clear();
                SortedDictionary<string, bool> imageFolders = registryManager.getImageFolders();
                
                foreach (KeyValuePair<string, bool> folder in imageFolders)
                {
                    dgvFoldersList.Rows.Add(folder.Key, folder.Value);
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "Error loading folders: " + ex.Message;
                toolStripStatusLabel.ForeColor = Color.Red;
            }
        }

        private void LoadFileTypeSelections()
        {
            string fileTypes = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_FILE_TYPES);
            string videoFileTypes = registryManager.getRegistryProperty(
                RegistryConstants.REG_KEY_VIDEO_FILE_TYPES,
                RegistryConstants.DefaultVideoFileTypes);
            string combinedTypes = string.IsNullOrEmpty(fileTypes) ? videoFileTypes : fileTypes + ";" + videoFileTypes;

            if (!string.IsNullOrEmpty(combinedTypes))
            {
                string[] selectedTypes = combinedTypes.Split(';');
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    item.Checked = Array.IndexOf(selectedTypes, item.Name) >= 0;
                }
            }
            else
            {
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    item.Checked = true;
                }
            }
        }

        private void LoadVideoSettings()
        {
            cbx_UseVideo.Checked = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_USE_VIDEO, false);
            cbx_VideoMute.Checked = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_VIDEO_MUTE, true);

            string durationIndex = registryManager.getRegistryProperty(RegistryConstants.REG_KEY_VideoDuration, "2");
            int index;
            if (int.TryParse(durationIndex, out index) && index >= 0 && index < comboBoxVideoDuration.Items.Count)
            {
                comboBoxVideoDuration.SelectedIndex = index;
            }
            else
            {
                comboBoxVideoDuration.SelectedIndex = 2;
            }
        }

        private void UpdateUIVisibility()
        {
            bool useVideo = cbx_UseVideo.Checked;

            comboBoxFileNameDisplay.Visible = cbx_showFileNames.Checked;
            labelFileNameDisplay.Visible = cbx_showFileNames.Checked;
            btnChangeFont.Visible = cbx_showFileNames.Checked;
            UpdateFileNameSample();

            cbx_VideoMute.Enabled = useVideo;
            comboBoxVideoDuration.Enabled = useVideo;
            labelVideoDuration.Enabled = useVideo;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                // Save folders list
                SortedDictionary<string, bool> imageFolders = new SortedDictionary<string, bool>();
                foreach (DataGridViewRow row in dgvFoldersList.Rows)
                {
                    if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                    {
                        string folderPath = row.Cells[0].Value.ToString();
                        bool includeSubfolders = Convert.ToBoolean(row.Cells[1].Value);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            imageFolders[folderPath] = includeSubfolders;
                        }
                    }
                }
                registryManager.setImageFolders(imageFolders);

                // Save file type selections (images and videos stored separately)
                StringBuilder selectedImageTypes = new StringBuilder();
                StringBuilder selectedVideoTypes = new StringBuilder();
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    if (!item.Checked) continue;

                    bool isVideoType = IsVideoExtension(item.Name);
                    StringBuilder target = isVideoType ? selectedVideoTypes : selectedImageTypes;
                    if (target.Length > 0)
                        target.Append(";");
                    target.Append(item.Name);
                }
                registryManager.setRegistryProperty(RegistryConstants.REG_KEY_FILE_TYPES, selectedImageTypes.ToString());
                registryManager.setRegistryProperty(RegistryConstants.REG_KEY_VIDEO_FILE_TYPES,
                    selectedVideoTypes.Length > 0 ? selectedVideoTypes.ToString() : RegistryConstants.DefaultVideoFileTypes);

                SaveControlSettings(this);
                registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, cbx_debug.Checked);

                registryManager.setRegistryProperty(RegistryConstants.REG_KEY_FILENAME_FONT, FontToString(labelFileNameSample.Font));
                registryManager.setRegistryProperty(RegistryConstants.REG_KEY_FILENAME_COLOR, ColorTranslator.ToHtml(labelFileNameSample.ForeColor));

                Form1.ResetSharedCatalog();

                // Show save confirmation
                toolStripStatusLabel.Text = "Settings saved successfully";
                toolStripStatusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "Error saving settings: " + ex.Message;
                toolStripStatusLabel.ForeColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                TryAddFolder(folderBrowserDialog1.SelectedPath, includeSubfolders: true);
        }

        private bool TryAddFolder(string folderPath, bool includeSubfolders)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return false;

            string normalized = Path.GetFullPath(folderPath.Trim());

            foreach (DataGridViewRow row in dgvFoldersList.Rows)
            {
                if (row.Cells[0].Value != null &&
                    string.Equals(row.Cells[0].Value.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            dgvFoldersList.Rows.Add(normalized, includeSubfolders);
            foreach (DataGridViewRow row in dgvFoldersList.Rows)
                row.Cells[1].ReadOnly = false;

            return true;
        }

        private void DgvFoldersList_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void DgvFoldersList_DragOver(object sender, DragEventArgs e)
        {
            DgvFoldersList_DragEnter(sender, e);
        }

        private void DgvFoldersList_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                if (Directory.Exists(path))
                    TryAddFolder(path, includeSubfolders: true);
            }
        }

        private void btnRemoveFolder_Click(object sender, EventArgs e)
        {
            //check something is selected in our list
            if (dgvFoldersList.SelectedCells.Count != 0 || dgvFoldersList.SelectedRows.Count != 0)
            {
                //find selected row
                DataGridViewRow selectedRow;
                if (dgvFoldersList.SelectedRows.Count != 0)
                {
                    selectedRow = dgvFoldersList.SelectedRows[0];
                }
                else
                {
                    //try by cell
                    int selectedIndex = dgvFoldersList.SelectedCells[0].RowIndex;
                    selectedRow = dgvFoldersList.Rows[selectedIndex];
                }
                //remove found row
                if (selectedRow != null)
                {
                    dgvFoldersList.Rows.Remove(selectedRow);
                }
            }
        }

        private void cbx_UseVideo_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUIVisibility();
        }

        private void cbx_debug_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDebugLogLinkVisibility();
        }

        private void UpdateDebugLogLinkVisibility()
        {
            bool debugActive = cbx_debug.Checked;
            linkDebugLog.Visible = debugActive;
            if (!debugActive) return;

            linkDebugLog.Text = Path.GetFileName(Logger.DebugLogFilePath);
            toolTip1.SetToolTip(linkDebugLog, Logger.DebugLogFilePath);
        }

        private void linkDebugLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string logPath = Logger.DebugLogFilePath;
                if (File.Exists(logPath))
                {
                    Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true });
                    return;
                }

                Process.Start(new ProcessStartInfo(Path.GetDirectoryName(logPath)) { UseShellExecute = true });
                toolStripStatusLabel.Text = "Debug log not created yet. Temp folder opened.";
                toolStripStatusLabel.ForeColor = SystemColors.GrayText;
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "Could not open debug log: " + ex.Message;
                toolStripStatusLabel.ForeColor = Color.Red;
            }
        }

        private static bool IsVideoExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            string ext = extension.ToLowerInvariant();
            return ext == "*.mp4" || ext == "*.avi" || ext == "*.wmv" || ext == "*.mov";
        }

        public void btnPreview_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Form1.ResetSharedCatalog();
            runScreensaver(cbx_AllScreens.Checked);
        }

        public void runScreensaver(Boolean allScreens, Boolean destroyOnClose = false)
        {
            CloseAllFrames();
            Form1.ResetSharedCatalog();
            List<Screen> orderedScreens = new List<Screen>(Screen.AllScreens);

            if (allScreens)
            {
                for (int i = 0; i < orderedScreens.Count; i++)
                {
                    Screen screen = orderedScreens[i];
                    Form form = new Form1(this, screen);
                    AttachScreensaverFormClosed(form, destroyOnClose);
                    form.Show();
                    previewForms.Add(form);
                }
            }
            else
            {
                Screen selectedScreen = orderedScreens.Count > 0 ? orderedScreens[0] : Screen.PrimaryScreen;
                Form form = new Form1(this, selectedScreen);
                AttachScreensaverFormClosed(form, destroyOnClose);
                form.Show();
                previewForms.Add(form);
            }

            Form1.EnforceGlobalSingleVideoPlayback();

            if (destroyOnClose && previewForms.Count > 0)
            {
                Application.Run(previewForms[0]);
            }
        }

        private void AttachScreensaverFormClosed(Form form, bool destroyOnClose)
        {
            if (destroyOnClose)
            {
                form.FormClosed += ScreensaverForm_FormClosed_Exit;
            }
            else
            {
                form.FormClosed += ScreensaverForm_FormClosed;
            }
        }

        private void ScreensaverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // When any screensaver form is closed, close all others but don't exit application
            CloseAllFrames();
        }

        private void ScreensaverForm_FormClosed_Exit(object sender, FormClosedEventArgs e)
        {
            // When any screensaver form is closed in screensaver mode, close everything and exit
            CloseAllFrames();
            this.Dispose();
            Environment.Exit(0); // Force the application to exit completely
        }

        public void CloseAllFrames()
        {
            if (previewForms == null)
                previewForms = new List<Form>();

            if (previewForms.Count > 0)
            {
                foreach (Form previewForm in previewForms)
                {
                    previewForm.FormClosed -= ScreensaverForm_FormClosed;
                    previewForm.FormClosed -= ScreensaverForm_FormClosed_Exit;
                    previewForm.Dispose();
                }
            }
            previewForms.Clear();
        }

        private void btnChangeFont_Click(object sender, EventArgs e)
        {
            // Load current values into the dialog so it opens with the correct selection.
            fontDialog1.Font = labelFileNameSample.Font;
            fontDialog1.Color = labelFileNameSample.ForeColor;

            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                // Update both labels with the new font and color
                this.labelFileNameSample.Font = fontDialog1.Font;
                this.labelFileNameSample.ForeColor = fontDialog1.Color;
                UpdateFileNameSamplePreviewBackground();
                SaveSettings();
            }
        }

        private void cbx_showFileNames_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxFileNameDisplay.Enabled = cbx_showFileNames.Checked;
            comboBoxFileNameDisplay.Visible = cbx_showFileNames.Checked;
            labelFileNameDisplay.Visible = cbx_showFileNames.Checked;
            btnChangeFont.Enabled = cbx_showFileNames.Checked;
            btnChangeFont.Visible = cbx_showFileNames.Checked;
            labelFileNameSample.Visible = cbx_showFileNames.Checked;
            UpdateFileNameSample();
        }

        private string FontToString(Font font)
        {
            return $"{font.FontFamily.Name};{font.Size};{font.Style}";
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

        private void comboBoxFileNameDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFileNameSample();
        }

        private void UpdateFileNameSample()
        {
            string samplePath = "Sample File.jpg";
            if (!string.IsNullOrEmpty(samplePath))
            {
                switch (comboBoxFileNameDisplay.SelectedIndex)
                {
                    case 0: // Full path
                        labelFileNameSample.Text = samplePath;
                        break;
                    case 1: // Relative path
                        try
                        {
                            string rootPath = Path.GetDirectoryName(Application.ExecutablePath);
                            string relativePath = GetRelativePath(rootPath, samplePath);
                            labelFileNameSample.Text = relativePath;
                        }
                        catch
                        {
                            labelFileNameSample.Text = Path.GetFileName(samplePath);
                        }
                        break;
                    case 2: // File name only
                        labelFileNameSample.Text = Path.GetFileName(samplePath);
                        break;
                }
            }
            else
            {
                labelFileNameSample.Text = "Sample Image.jpg";
            }

            UpdateFileNameSamplePreviewBackground();
        }

        private void UpdateFileNameSamplePreviewBackground()
        {
            // Give the preview label a contrasting background so the chosen font color
            // is always easy to read.
            Color fore = labelFileNameSample.ForeColor;

            // Relative luminance (0..255 scale-ish), weighted for perceptual brightness.
            double luminance = (0.2126 * fore.R) + (0.7152 * fore.G) + (0.0722 * fore.B);

            // If the font is light, use a dark background; otherwise use a light background.
            labelFileNameSample.BackColor = luminance > 140 ? Color.Black : Color.White;
            labelFileNameSample.BorderStyle = BorderStyle.FixedSingle;
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
    }
}
