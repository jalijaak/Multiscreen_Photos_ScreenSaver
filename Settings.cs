using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

namespace ScreenSaver
{
    public partial class Settings : Form
    {
        private static RegistryManager registryManager;
        private List<Form1> previewForms;

        // Registry property keys
        private const string PROP_SHOW_FILENAME = "ShowFileName";
        private const string PROP_FILENAME_DISPLAY_MODE = "FileNameDisplayMode";
        private const string PROP_FILENAME_FONT = "FileNameFont";
        private const string PROP_FILENAME_COLOR = "FileNameColor";
        private const string PROP_FILE_TYPES = "FileTypes";

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
            string[] fileTypes = new string[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.tif", "*.tiff" };

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
                // Load debug setting
                cbx_debug.Checked = registryManager.getBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, false);

                // Load all tagged controls
                foreach (Control c in this.Controls)
                {
                    LoadControlSettings(c);
                }

                // Load folders list
                LoadFoldersList();

                // Load file type selections
                LoadFileTypeSelections();

                // Update UI visibility based on loaded settings
                UpdateUIVisibility();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "Error loading settings: " + ex.Message;
                toolStripStatusLabel.ForeColor = Color.Red;
            }
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
                            cb.Items.AddRange(registryManager.getRegistryPropertyOptions(cb.Tag.ToString()).ToArray());
                            cb.SelectedItem = registryManager.getRegistryProperty(cb.Tag.ToString());
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
            string fileTypes = registryManager.getRegistryProperty(PROP_FILE_TYPES);
            if (!string.IsNullOrEmpty(fileTypes))
            {
                string[] selectedTypes = fileTypes.Split(';');
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    item.Checked = Array.IndexOf(selectedTypes, item.Name) >= 0;
                }
            }
            else
            {
                // Default selections if no registry value exists
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    item.Checked = true;
                }
            }
        }

        private void UpdateUIVisibility()
        {
            comboBoxFileNameDisplay.Visible = cbx_showFileNames.Checked;
            labelFileNameDisplay.Visible = cbx_showFileNames.Checked;
            btnChangeFont.Visible = cbx_showFileNames.Checked;
            UpdateFileNameSample();

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

                // Save file type selections
                StringBuilder selectedFileTypes = new StringBuilder();
                foreach (ListViewItem item in fileTypesList.Items)
                {
                    if (item.Checked)
                    {
                        if (selectedFileTypes.Length > 0)
                            selectedFileTypes.Append(";");
                        selectedFileTypes.Append(item.Name);
                    }
                }
                registryManager.setRegistryProperty(PROP_FILE_TYPES, selectedFileTypes.ToString());

                // Save other settings
                registryManager.setRegistryProperty(PROP_SHOW_FILENAME, cbx_showFileNames.Checked.ToString());
                registryManager.setRegistryProperty(PROP_FILENAME_DISPLAY_MODE, comboBoxFileNameDisplay.SelectedIndex.ToString());
                registryManager.setRegistryProperty(PROP_FILENAME_FONT, FontToString(labelFileNameSample.Font));
                registryManager.setRegistryProperty(PROP_FILENAME_COLOR, ColorTranslator.ToHtml(labelFileNameSample.ForeColor));

                // Save debug setting
                registryManager.setBooleanPropertyVal(RegistryConstants.REG_KEY_DEBUG, cbx_debug.Checked);

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
            {
                string selectedPath = folderBrowserDialog1.SelectedPath;
                //check if this folder is already listed
                foreach (DataGridViewRow row in dgvFoldersList.Rows)
                {
                    if (string.Equals(row.Cells[0].Value.ToString(), selectedPath, StringComparison.OrdinalIgnoreCase))//compare ignore case
                    {
                        return;
                    }
                }

                //add the selected folder to our list
                this.dgvFoldersList.Rows.Add(new Object[] { selectedPath, true });
                //make sure the checkboxes are active
                foreach (DataGridViewRow row in dgvFoldersList.Rows)
                {
                    row.Cells[1].ReadOnly = false;
                }
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

        public void btnPreview_Click(object sender, EventArgs e)
        {
            SaveSettings();

            runScreensaver(cbx_AllScreens.Checked);
        }

        public void runScreensaver(Boolean allScreens, Boolean destroyOnClose = false)
        {
            CloseAllFrames();

            if (allScreens)
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    Form1 form = new Form1(this, screen);
                    if (destroyOnClose)
                    {
                        form.FormClosed += ScreensaverForm_FormClosed_Exit;
                    }
                    else
                    {
                        form.FormClosed += ScreensaverForm_FormClosed;
                    }
                    form.Show();
                    previewForms.Add(form);
                }
            }
            else
            {
                Form1 form = new Form1(this, Screen.PrimaryScreen);
                if (destroyOnClose)
                {
                    form.FormClosed += ScreensaverForm_FormClosed_Exit;
                }
                else
                {
                    form.FormClosed += ScreensaverForm_FormClosed;
                }
                form.Show();
                previewForms.Add(form);
            }

            // If in screensaver mode, run the application until forms are closed
            if (destroyOnClose)
            {
                Form1 mainForm = previewForms[0];
                Application.Run(mainForm);
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
                previewForms = new List<Form1>();

            if (previewForms.Count > 0)
            {
                foreach (Form1 previewForm in previewForms)
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
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                // Update both labels with the new font and color
                this.labelFileNameSample.Font = fontDialog1.Font;
                this.labelFileNameSample.ForeColor = fontDialog1.Color;
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
