namespace ScreenSaver
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.ImageFolders = new System.Windows.Forms.TabPage();
            this.cbx_AllScreens = new System.Windows.Forms.CheckBox();
            this.btnChangeFont = new System.Windows.Forms.Button();
            this.cbx_showFileNames = new System.Windows.Forms.CheckBox();
            this.btnAddFolder = new System.Windows.Forms.Button();
            this.btnRemoveFolder = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.fileTypesList = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.chkRandomOrder = new System.Windows.Forms.CheckBox();
            this.dgvFoldersList = new System.Windows.Forms.DataGridView();
            this.FolderName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Subfolders = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxFileNameDisplay = new System.Windows.Forms.ComboBox();
            this.labelFileNameSample = new System.Windows.Forms.Label();
            this.labelFileNameDisplay = new System.Windows.Forms.Label();
            this.Effects = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lstvEffects = new System.Windows.Forms.ListView();
            this.chkUseTransitions = new System.Windows.Forms.CheckBox();
            this.lblEffectDuration = new System.Windows.Forms.Label();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.About = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.btnPreview = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripLabel();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.cbx_debug = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.ImageFolders.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFoldersList)).BeginInit();
            this.Effects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            this.About.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.ImageFolders);
            this.tabControl1.Controls.Add(this.Effects);
            this.tabControl1.Controls.Add(this.About);
            this.tabControl1.Location = new System.Drawing.Point(6, 7);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(577, 390);
            this.tabControl1.TabIndex = 0;
            // 
            // ImageFolders
            // 
            this.ImageFolders.Controls.Add(this.cbx_AllScreens);
            this.ImageFolders.Controls.Add(this.btnChangeFont);
            this.ImageFolders.Controls.Add(this.cbx_showFileNames);
            this.ImageFolders.Controls.Add(this.btnAddFolder);
            this.ImageFolders.Controls.Add(this.btnRemoveFolder);
            this.ImageFolders.Controls.Add(this.groupBox1);
            this.ImageFolders.Controls.Add(this.label2);
            this.ImageFolders.Controls.Add(this.numericUpDown1);
            this.ImageFolders.Controls.Add(this.comboBox1);
            this.ImageFolders.Controls.Add(this.chkRandomOrder);
            this.ImageFolders.Controls.Add(this.dgvFoldersList);
            this.ImageFolders.Controls.Add(this.label1);
            this.ImageFolders.Controls.Add(this.comboBoxFileNameDisplay);
            this.ImageFolders.Controls.Add(this.labelFileNameSample);
            this.ImageFolders.Controls.Add(this.labelFileNameDisplay);
            this.ImageFolders.Location = new System.Drawing.Point(4, 22);
            this.ImageFolders.Name = "ImageFolders";
            this.ImageFolders.Padding = new System.Windows.Forms.Padding(3);
            this.ImageFolders.Size = new System.Drawing.Size(569, 364);
            this.ImageFolders.TabIndex = 0;
            this.ImageFolders.Text = "Media Files";
            this.ImageFolders.UseVisualStyleBackColor = true;
            // 
            // cbx_AllScreens
            // 
            this.cbx_AllScreens.AutoSize = true;
            this.cbx_AllScreens.Location = new System.Drawing.Point(6, 15);
            this.cbx_AllScreens.Name = "cbx_AllScreens";
            this.cbx_AllScreens.Size = new System.Drawing.Size(101, 17);
            this.cbx_AllScreens.TabIndex = 0;
            this.cbx_AllScreens.Tag = "UseMultipleScreens";
            this.cbx_AllScreens.Text = "Use All Screens";
            this.cbx_AllScreens.UseVisualStyleBackColor = true;
            // 
            // btnChangeFont
            // 
            this.btnChangeFont.Location = new System.Drawing.Point(14, 164);
            this.btnChangeFont.Name = "btnChangeFont";
            this.btnChangeFont.Size = new System.Drawing.Size(101, 23);
            this.btnChangeFont.TabIndex = 11;
            this.btnChangeFont.Text = "Change Font";
            this.btnChangeFont.UseVisualStyleBackColor = true;
            this.btnChangeFont.Click += new System.EventHandler(this.btnChangeFont_Click);
            // 
            // cbx_showFileNames
            // 
            this.cbx_showFileNames.AutoSize = true;
            this.cbx_showFileNames.Location = new System.Drawing.Point(7, 114);
            this.cbx_showFileNames.Name = "cbx_showFileNames";
            this.cbx_showFileNames.Size = new System.Drawing.Size(108, 17);
            this.cbx_showFileNames.TabIndex = 0;
            this.cbx_showFileNames.Tag = "ShowFileName";
            this.cbx_showFileNames.Text = "Show File Names";
            this.cbx_showFileNames.UseVisualStyleBackColor = true;
            this.cbx_showFileNames.CheckedChanged += new System.EventHandler(this.cbx_showFileNames_CheckedChanged);
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(310, 324);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new System.Drawing.Size(102, 25);
            this.btnAddFolder.TabIndex = 8;
            this.btnAddFolder.Text = "Add Folder";
            this.btnAddFolder.UseVisualStyleBackColor = true;
            this.btnAddFolder.Click += new System.EventHandler(this.btnAddFolder_Click);
            // 
            // btnRemoveFolder
            // 
            this.btnRemoveFolder.Location = new System.Drawing.Point(439, 324);
            this.btnRemoveFolder.Name = "btnRemoveFolder";
            this.btnRemoveFolder.Size = new System.Drawing.Size(102, 25);
            this.btnRemoveFolder.TabIndex = 7;
            this.btnRemoveFolder.Text = "Remove Folder";
            this.btnRemoveFolder.UseVisualStyleBackColor = true;
            this.btnRemoveFolder.Click += new System.EventHandler(this.btnRemoveFolder_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.fileTypesList);
            this.groupBox1.Location = new System.Drawing.Point(286, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 104);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "File Types";
            // 
            // fileTypesList
            // 
            this.fileTypesList.BackColor = System.Drawing.SystemColors.Control;
            this.fileTypesList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.fileTypesList.CheckBoxes = true;
            this.fileTypesList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.fileTypesList.HideSelection = false;
            this.fileTypesList.Location = new System.Drawing.Point(6, 15);
            this.fileTypesList.Name = "fileTypesList";
            this.fileTypesList.Size = new System.Drawing.Size(262, 79);
            this.fileTypesList.TabIndex = 5;
            this.fileTypesList.Tag = "FileTypes";
            this.fileTypesList.UseCompatibleStateImageBehavior = false;
            this.fileTypesList.View = System.Windows.Forms.View.List;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Delay Between Images (Seconds)";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(186, 84);
            this.numericUpDown1.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(42, 20);
            this.numericUpDown1.TabIndex = 4;
            this.numericUpDown1.Tag = "delayBetweenImages";
            this.numericUpDown1.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(109, 57);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(42, 21);
            this.comboBox1.TabIndex = 4;
            this.comboBox1.Tag = "FramesOnScreen";
            this.comboBox1.Visible = false;
            // 
            // chkRandomOrder
            // 
            this.chkRandomOrder.AutoSize = true;
            this.chkRandomOrder.Checked = true;
            this.chkRandomOrder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRandomOrder.Location = new System.Drawing.Point(6, 37);
            this.chkRandomOrder.Name = "chkRandomOrder";
            this.chkRandomOrder.Size = new System.Drawing.Size(95, 17);
            this.chkRandomOrder.TabIndex = 0;
            this.chkRandomOrder.Tag = "imageOrder";
            this.chkRandomOrder.Text = "Random Order";
            this.chkRandomOrder.UseVisualStyleBackColor = true;
            // 
            // dgvFoldersList
            // 
            this.dgvFoldersList.AllowUserToAddRows = false;
            this.dgvFoldersList.AllowUserToDeleteRows = false;
            this.dgvFoldersList.AllowUserToResizeRows = false;
            this.dgvFoldersList.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dgvFoldersList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FolderName,
            this.Subfolders});
            this.dgvFoldersList.GridColor = System.Drawing.SystemColors.Control;
            this.dgvFoldersList.Location = new System.Drawing.Point(3, 213);
            this.dgvFoldersList.MultiSelect = false;
            this.dgvFoldersList.Name = "dgvFoldersList";
            this.dgvFoldersList.RowHeadersVisible = false;
            this.dgvFoldersList.Size = new System.Drawing.Size(565, 105);
            this.dgvFoldersList.TabIndex = 0;
            // 
            // FolderName
            // 
            this.FolderName.HeaderText = "Folder Name";
            this.FolderName.MaxInputLength = 327;
            this.FolderName.Name = "FolderName";
            this.FolderName.ReadOnly = true;
            this.FolderName.Width = 462;
            // 
            // Subfolders
            // 
            this.Subfolders.HeaderText = "With Subfolders";
            this.Subfolders.Name = "Subfolders";
            this.Subfolders.ReadOnly = true;
            this.Subfolders.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Subfolders.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Frames Per screen";
            this.label1.Visible = false;
            // 
            // comboBoxFileNameDisplay
            // 
            this.comboBoxFileNameDisplay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFileNameDisplay.Location = new System.Drawing.Point(120, 137);
            this.comboBoxFileNameDisplay.Name = "comboBoxFileNameDisplay";
            this.comboBoxFileNameDisplay.Size = new System.Drawing.Size(150, 21);
            this.comboBoxFileNameDisplay.TabIndex = 16;
            this.comboBoxFileNameDisplay.SelectedIndexChanged += new System.EventHandler(this.comboBoxFileNameDisplay_SelectedIndexChanged);
            // 
            // labelFileNameSample
            // 
            this.labelFileNameSample.AutoSize = true;
            this.labelFileNameSample.Location = new System.Drawing.Point(121, 169);
            this.labelFileNameSample.Name = "labelFileNameSample";
            this.labelFileNameSample.Size = new System.Drawing.Size(92, 13);
            this.labelFileNameSample.TabIndex = 28;
            this.labelFileNameSample.Text = "Sample File Name";
            // 
            // labelFileNameDisplay
            // 
            this.labelFileNameDisplay.AutoSize = true;
            this.labelFileNameDisplay.Location = new System.Drawing.Point(6, 140);
            this.labelFileNameDisplay.Name = "labelFileNameDisplay";
            this.labelFileNameDisplay.Size = new System.Drawing.Size(94, 13);
            this.labelFileNameDisplay.TabIndex = 15;
            this.labelFileNameDisplay.Text = "File Name Display:";
            // 
            // Effects
            // 
            this.Effects.Controls.Add(this.label4);
            this.Effects.Controls.Add(this.label3);
            this.Effects.Controls.Add(this.numericUpDown3);
            this.Effects.Controls.Add(this.groupBox2);
            this.Effects.Controls.Add(this.chkUseTransitions);
            this.Effects.Controls.Add(this.lblEffectDuration);
            this.Effects.Controls.Add(this.numericUpDown2);
            this.Effects.Location = new System.Drawing.Point(4, 22);
            this.Effects.Name = "Effects";
            this.Effects.Padding = new System.Windows.Forms.Padding(3);
            this.Effects.Size = new System.Drawing.Size(569, 364);
            this.Effects.TabIndex = 1;
            this.Effects.Text = "Effects";
            this.Effects.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Enabled = false;
            this.label4.Location = new System.Drawing.Point(184, 17);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(360, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Transitions are done screen by screen and may be effected by display card";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(362, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Effect Frame Rates";
            // 
            // numericUpDown3
            // 
            this.numericUpDown3.Location = new System.Drawing.Point(466, 61);
            this.numericUpDown3.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numericUpDown3.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDown3.Name = "numericUpDown3";
            this.numericUpDown3.Size = new System.Drawing.Size(41, 20);
            this.numericUpDown3.TabIndex = 5;
            this.numericUpDown3.Tag = "EffectFrames";
            this.numericUpDown3.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lstvEffects);
            this.groupBox2.Location = new System.Drawing.Point(12, 87);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(532, 250);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Effects";
            // 
            // lstvEffects
            // 
            this.lstvEffects.BackColor = System.Drawing.SystemColors.Control;
            this.lstvEffects.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstvEffects.CheckBoxes = true;
            this.lstvEffects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstvEffects.HideSelection = false;
            this.lstvEffects.Location = new System.Drawing.Point(6, 19);
            this.lstvEffects.Name = "lstvEffects";
            this.lstvEffects.Size = new System.Drawing.Size(520, 225);
            this.lstvEffects.TabIndex = 8;
            this.lstvEffects.Tag = "Effects";
            this.lstvEffects.UseCompatibleStateImageBehavior = false;
            this.lstvEffects.View = System.Windows.Forms.View.List;
            // 
            // chkUseTransitions
            // 
            this.chkUseTransitions.AutoSize = true;
            this.chkUseTransitions.Location = new System.Drawing.Point(12, 16);
            this.chkUseTransitions.Name = "chkUseTransitions";
            this.chkUseTransitions.Size = new System.Drawing.Size(130, 17);
            this.chkUseTransitions.TabIndex = 3;
            this.chkUseTransitions.Tag = "Use Effects";
            this.chkUseTransitions.Text = "Use Transition Effects";
            this.chkUseTransitions.UseVisualStyleBackColor = true;
            // 
            // lblEffectDuration
            // 
            this.lblEffectDuration.AutoSize = true;
            this.lblEffectDuration.Location = new System.Drawing.Point(9, 64);
            this.lblEffectDuration.Name = "lblEffectDuration";
            this.lblEffectDuration.Size = new System.Drawing.Size(78, 13);
            this.lblEffectDuration.TabIndex = 2;
            this.lblEffectDuration.Text = "Effect Duration";
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.DecimalPlaces = 1;
            this.numericUpDown2.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericUpDown2.Location = new System.Drawing.Point(93, 61);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(49, 20);
            this.numericUpDown2.TabIndex = 1;
            this.numericUpDown2.Tag = "EffectDuration";
            // 
            // About
            // 
            this.About.Controls.Add(this.label6);
            this.About.Controls.Add(this.richTextBox2);
            this.About.Controls.Add(this.label5);
            this.About.Controls.Add(this.richTextBox1);
            this.About.Location = new System.Drawing.Point(4, 22);
            this.About.Name = "About";
            this.About.Size = new System.Drawing.Size(569, 364);
            this.About.TabIndex = 2;
            this.About.Text = "About";
            this.About.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(3, 198);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(193, 20);
            this.label6.TabIndex = 3;
            this.label6.Text = "About the Screensaver";
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(3, 221);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.ReadOnly = true;
            this.richTextBox2.Size = new System.Drawing.Size(558, 88);
            this.richTextBox2.TabIndex = 2;
            this.richTextBox2.Text = resources.GetString("richTextBox2.Text");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(3, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(193, 20);
            this.label5.TabIndex = 1;
            this.label5.Text = "About the Screensaver";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(3, 33);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(558, 154);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(493, 403);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(85, 25);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Exit";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(392, 403);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(85, 25);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnPreview
            // 
            this.btnPreview.Location = new System.Drawing.Point(277, 403);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(85, 25);
            this.btnPreview.TabIndex = 3;
            this.btnPreview.Text = "Preview";
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 438);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(586, 25);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 22);
            // 
            // fontDialog1
            // 
            this.fontDialog1.ShowColor = true;
            // 
            // cbx_debug
            // 
            this.cbx_debug.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbx_debug.AutoSize = true;
            this.cbx_debug.Location = new System.Drawing.Point(16, 410);
            this.cbx_debug.Name = "cbx_debug";
            this.cbx_debug.Size = new System.Drawing.Size(58, 17);
            this.cbx_debug.TabIndex = 5;
            this.cbx_debug.Text = "Debug";
            this.toolTip1.SetToolTip(this.cbx_debug, "Save debug info to log in user temp folder");
            this.cbx_debug.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 463);
            this.Controls.Add(this.cbx_debug);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.tabControl1);
            this.Name = "Settings";
            this.Text = "Settings";
            this.tabControl1.ResumeLayout(false);
            this.ImageFolders.ResumeLayout(false);
            this.ImageFolders.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFoldersList)).EndInit();
            this.Effects.ResumeLayout(false);
            this.Effects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            this.About.ResumeLayout(false);
            this.About.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage ImageFolders;
        private System.Windows.Forms.DataGridView dgvFoldersList;
        private System.Windows.Forms.TabPage Effects;
        private System.Windows.Forms.TabPage About;
        private System.Windows.Forms.CheckBox chkRandomOrder;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label lblEffectDuration;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkUseTransitions;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView fileTypesList;
        private System.Windows.Forms.ListView lstvEffects;
        private System.Windows.Forms.Button btnAddFolder;
        private System.Windows.Forms.Button btnRemoveFolder;
        private System.Windows.Forms.DataGridViewTextBoxColumn FolderName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Subfolders;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDown3;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripStatusLabel;
        private System.Windows.Forms.CheckBox cbx_showFileNames;
        private System.Windows.Forms.Button btnChangeFont;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.CheckBox cbx_AllScreens;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxFileNameDisplay;
        private System.Windows.Forms.Label labelFileNameSample;
        private System.Windows.Forms.Label labelFileNameDisplay;
        private System.Windows.Forms.CheckBox cbx_debug;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}