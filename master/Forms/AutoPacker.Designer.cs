namespace TTG_Tools
{
    partial class AutoPacker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoPacker));
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.buttonDecrypt = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rbSwitchSwizzle = new System.Windows.Forms.RadioButton();
            this.rbPS4Swizzle = new System.Windows.Forms.RadioButton();
            this.rbXbox360Swizzle = new System.Windows.Forms.RadioButton();
            this.rbPSVitaSwizzle = new System.Windows.Forms.RadioButton();
            this.rbWiiSwizzle = new System.Windows.Forms.RadioButton();
            this.rbPS2Swizzle = new System.Windows.Forms.RadioButton();
            this.rbNoSwizzle = new System.Windows.Forms.RadioButton();
            this.labelUnicode = new System.Windows.Forms.Label();
            this.checkIOS = new System.Windows.Forms.CheckBox();
            this.CheckNewEngine = new System.Windows.Forms.CheckBox();
            this.checkEncDDS = new System.Windows.Forms.CheckBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.checkEncLangdb = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkCustomKey = new System.Windows.Forms.CheckBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sortLabel = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.DisplayMember = "0";
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "Before The Wolf Among Us",
            "Sam & Max 201",
            "Sam & Max 202",
            "Sam & Max 203",
            "Sam & Max 204",
            "Sam & Max 205",
            "After The Wolf Among Us"});
            this.comboBox1.Location = new System.Drawing.Point(22, 63);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(328, 21);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(24, 128);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 21);
            this.button1.TabIndex = 1;
            this.button1.Text = "Encrypt, Pack, Import";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(10, 206);
            this.listBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.listBox1.Name = "listBox1";
            this.listBox1.ScrollAlwaysVisible = true;
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox1.Size = new System.Drawing.Size(801, 381);
            this.listBox1.TabIndex = 2;
            // 
            // buttonDecrypt
            // 
            this.buttonDecrypt.Location = new System.Drawing.Point(209, 128);
            this.buttonDecrypt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonDecrypt.Name = "buttonDecrypt";
            this.buttonDecrypt.Size = new System.Drawing.Size(124, 21);
            this.buttonDecrypt.TabIndex = 1;
            this.buttonDecrypt.Text = "Decrypt, Export";
            this.buttonDecrypt.UseVisualStyleBackColor = true;
            this.buttonDecrypt.Click += new System.EventHandler(this.buttonDecrypt_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.labelUnicode);
            this.groupBox1.Controls.Add(this.checkIOS);
            this.groupBox1.Controls.Add(this.CheckNewEngine);
            this.groupBox1.Controls.Add(this.checkEncDDS);
            this.groupBox1.Controls.Add(this.comboBox2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.checkEncLangdb);
            this.groupBox1.Location = new System.Drawing.Point(368, 35);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(442, 168);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Some functions";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rbSwitchSwizzle);
            this.groupBox2.Controls.Add(this.rbPS4Swizzle);
            this.groupBox2.Controls.Add(this.rbXbox360Swizzle);
            this.groupBox2.Controls.Add(this.rbPSVitaSwizzle);
            this.groupBox2.Controls.Add(this.rbWiiSwizzle);
            this.groupBox2.Controls.Add(this.rbPS2Swizzle);
            this.groupBox2.Controls.Add(this.rbNoSwizzle);
            this.groupBox2.Location = new System.Drawing.Point(276, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(126, 168);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Swizzle methods";
            // 
            // rbSwitchSwizzle
            // 
            this.rbSwitchSwizzle.AutoSize = true;
            this.rbSwitchSwizzle.Location = new System.Drawing.Point(16, 63);
            this.rbSwitchSwizzle.Name = "rbSwitchSwizzle";
            this.rbSwitchSwizzle.Size = new System.Drawing.Size(103, 17);
            this.rbSwitchSwizzle.TabIndex = 2;
            this.rbSwitchSwizzle.TabStop = true;
            this.rbSwitchSwizzle.Text = "Nintendo Switch";
            this.rbSwitchSwizzle.UseVisualStyleBackColor = true;
            this.rbSwitchSwizzle.CheckedChanged += new System.EventHandler(this.rbSwitchSwizzle_CheckedChanged);
            // 
            // rbPS4Swizzle
            // 
            this.rbPS4Swizzle.AutoSize = true;
            this.rbPS4Swizzle.Location = new System.Drawing.Point(16, 42);
            this.rbPS4Swizzle.Name = "rbPS4Swizzle";
            this.rbPS4Swizzle.Size = new System.Drawing.Size(91, 17);
            this.rbPS4Swizzle.TabIndex = 1;
            this.rbPS4Swizzle.TabStop = true;
            this.rbPS4Swizzle.Text = "PlayStation 4";
            this.rbPS4Swizzle.UseVisualStyleBackColor = true;
            this.rbPS4Swizzle.CheckedChanged += new System.EventHandler(this.rbPS4Swizzle_CheckedChanged);
            // 
            // rbXbox360Swizzle
            // 
            this.rbXbox360Swizzle.AutoSize = true;
            this.rbXbox360Swizzle.Location = new System.Drawing.Point(16, 101);
            this.rbXbox360Swizzle.Name = "rbXbox360Swizzle";
            this.rbXbox360Swizzle.Size = new System.Drawing.Size(70, 17);
            this.rbXbox360Swizzle.TabIndex = 4;
            this.rbXbox360Swizzle.TabStop = true;
            this.rbXbox360Swizzle.Text = "Xbox 360";
            this.rbXbox360Swizzle.UseVisualStyleBackColor = true;
            this.rbXbox360Swizzle.CheckedChanged += new System.EventHandler(this.rbXbox360Swizzle_CheckedChanged);
            // 
            // rbPSVitaSwizzle
            // 
            this.rbPSVitaSwizzle.AutoSize = true;
            this.rbPSVitaSwizzle.Location = new System.Drawing.Point(16, 83);
            this.rbPSVitaSwizzle.Name = "rbPSVitaSwizzle";
            this.rbPSVitaSwizzle.Size = new System.Drawing.Size(60, 17);
            this.rbPSVitaSwizzle.TabIndex = 3;
            this.rbPSVitaSwizzle.TabStop = true;
            this.rbPSVitaSwizzle.Text = "PS Vita";
            this.rbPSVitaSwizzle.UseVisualStyleBackColor = true;
            this.rbPSVitaSwizzle.CheckedChanged += new System.EventHandler(this.rbPSVitaSwizzle_CheckedChanged);
            // 
            // rbWiiSwizzle
            // 
            this.rbWiiSwizzle.AutoSize = true;
            this.rbWiiSwizzle.Location = new System.Drawing.Point(16, 120);
            this.rbWiiSwizzle.Name = "rbWiiSwizzle";
            this.rbWiiSwizzle.Size = new System.Drawing.Size(86, 17);
            this.rbWiiSwizzle.TabIndex = 5;
            this.rbWiiSwizzle.TabStop = true;
            this.rbWiiSwizzle.Text = "Nintendo Wii";
            this.rbWiiSwizzle.UseVisualStyleBackColor = true;
            this.rbWiiSwizzle.CheckedChanged += new System.EventHandler(this.rbWiiSwizzle_CheckedChanged);
            // 
            // rbPS2Swizzle
            // 
            this.rbPS2Swizzle.AutoSize = true;
            this.rbPS2Swizzle.Location = new System.Drawing.Point(16, 139);
            this.rbPS2Swizzle.Name = "rbPS2Swizzle";
            this.rbPS2Swizzle.Size = new System.Drawing.Size(87, 17);
            this.rbPS2Swizzle.TabIndex = 6;
            this.rbPS2Swizzle.TabStop = true;
            this.rbPS2Swizzle.Text = "PlayStation 2";
            this.rbPS2Swizzle.UseVisualStyleBackColor = true;
            this.rbPS2Swizzle.CheckedChanged += new System.EventHandler(this.rbPS2Swizzle_CheckedChanged);
            // 
            // rbNoSwizzle
            // 
            this.rbNoSwizzle.AutoSize = true;
            this.rbNoSwizzle.Location = new System.Drawing.Point(16, 19);
            this.rbNoSwizzle.Name = "rbNoSwizzle";
            this.rbNoSwizzle.Size = new System.Drawing.Size(51, 17);
            this.rbNoSwizzle.TabIndex = 0;
            this.rbNoSwizzle.TabStop = true;
            this.rbNoSwizzle.Text = "None";
            this.rbNoSwizzle.UseVisualStyleBackColor = true;
            this.rbNoSwizzle.CheckedChanged += new System.EventHandler(this.rbNoSwizzle_CheckedChanged);
            // 
            // labelUnicode
            // 
            this.labelUnicode.AutoSize = true;
            this.labelUnicode.Location = new System.Drawing.Point(5, 134);
            this.labelUnicode.Name = "labelUnicode";
            this.labelUnicode.Size = new System.Drawing.Size(72, 13);
            this.labelUnicode.TabIndex = 16;
            this.labelUnicode.Text = "Unicode label";
            // 
            // checkIOS
            // 
            this.checkIOS.AutoSize = true;
            this.checkIOS.Location = new System.Drawing.Point(8, 107);
            this.checkIOS.Margin = new System.Windows.Forms.Padding(2);
            this.checkIOS.Name = "checkIOS";
            this.checkIOS.Size = new System.Drawing.Size(121, 17);
            this.checkIOS.TabIndex = 14;
            this.checkIOS.Text = "iOS (for new games)";
            this.checkIOS.UseVisualStyleBackColor = true;
            this.checkIOS.CheckedChanged += new System.EventHandler(this.checkIOS_CheckedChanged);
            // 
            // CheckNewEngine
            // 
            this.CheckNewEngine.AutoSize = true;
            this.CheckNewEngine.Location = new System.Drawing.Point(8, 86);
            this.CheckNewEngine.Margin = new System.Windows.Forms.Padding(2);
            this.CheckNewEngine.Name = "CheckNewEngine";
            this.CheckNewEngine.Size = new System.Drawing.Size(150, 17);
            this.CheckNewEngine.TabIndex = 13;
            this.CheckNewEngine.Text = "Lua scripts for new engine";
            this.CheckNewEngine.UseVisualStyleBackColor = true;
            this.CheckNewEngine.CheckedChanged += new System.EventHandler(this.CheckNewEngine_CheckedChanged);
            // 
            // checkEncDDS
            // 
            this.checkEncDDS.AutoSize = true;
            this.checkEncDDS.Location = new System.Drawing.Point(8, 38);
            this.checkEncDDS.Margin = new System.Windows.Forms.Padding(2);
            this.checkEncDDS.Name = "checkEncDDS";
            this.checkEncDDS.Size = new System.Drawing.Size(146, 17);
            this.checkEncDDS.TabIndex = 12;
            this.checkEncDDS.Text = "Encrypt DDS header only";
            this.checkEncDDS.UseVisualStyleBackColor = true;
            this.checkEncDDS.CheckedChanged += new System.EventHandler(this.checkEncDDS_CheckedChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "Versions 2-6",
            "Versions 7-9"});
            this.comboBox2.Location = new System.Drawing.Point(120, 61);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(2);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(108, 21);
            this.comboBox2.TabIndex = 9;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 64);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Method encryption:";
            // 
            // checkEncLangdb
            // 
            this.checkEncLangdb.AutoSize = true;
            this.checkEncLangdb.Location = new System.Drawing.Point(8, 15);
            this.checkEncLangdb.Margin = new System.Windows.Forms.Padding(2);
            this.checkEncLangdb.Name = "checkEncLangdb";
            this.checkEncLangdb.Size = new System.Drawing.Size(263, 17);
            this.checkEncLangdb.TabIndex = 7;
            this.checkEncLangdb.Text = "Encrypt langdb/dlog/d3dtx (fully encrypt d3dtx file)";
            this.checkEncLangdb.UseVisualStyleBackColor = true;
            this.checkEncLangdb.CheckedChanged += new System.EventHandler(this.checkEncLangdb_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 46);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(345, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Blowfish keys for encryption some old files or new compressed archives:";
            // 
            // checkCustomKey
            // 
            this.checkCustomKey.AutoSize = true;
            this.checkCustomKey.Location = new System.Drawing.Point(10, 97);
            this.checkCustomKey.Margin = new System.Windows.Forms.Padding(2);
            this.checkCustomKey.Name = "checkCustomKey";
            this.checkCustomKey.Size = new System.Drawing.Size(102, 17);
            this.checkCustomKey.TabIndex = 12;
            this.checkCustomKey.Text = "Set custom key:";
            this.checkCustomKey.UseVisualStyleBackColor = true;
            this.checkCustomKey.CheckedChanged += new System.EventHandler(this.checkCustomKey_CheckedChanged);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(112, 95);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(239, 20);
            this.textBox1.TabIndex = 13;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(821, 24);
            this.menuStrip1.TabIndex = 15;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // sortLabel
            // 
            this.sortLabel.AutoSize = true;
            this.sortLabel.Location = new System.Drawing.Point(19, 169);
            this.sortLabel.Name = "sortLabel";
            this.sortLabel.Size = new System.Drawing.Size(51, 13);
            this.sortLabel.TabIndex = 16;
            this.sortLabel.Text = "Sort label";
            // 
            // AutoPacker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(821, 612);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.sortLabel);
            this.Controls.Add(this.checkCustomKey);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.buttonDecrypt);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(414, 293);
            this.Name = "AutoPacker";
            this.Text = "Auto(De)Packer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutoPacker_FormClosing);
            this.Load += new System.EventHandler(this.AutoPacker_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button buttonDecrypt;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkEncLangdb;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox CheckNewEngine;
        private System.Windows.Forms.CheckBox checkEncDDS;
        private System.Windows.Forms.CheckBox checkCustomKey;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox checkIOS;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.Label labelUnicode;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbSwitchSwizzle;
        private System.Windows.Forms.RadioButton rbPS4Swizzle;
        private System.Windows.Forms.RadioButton rbXbox360Swizzle;
        private System.Windows.Forms.RadioButton rbPSVitaSwizzle;
        private System.Windows.Forms.RadioButton rbWiiSwizzle;
        private System.Windows.Forms.RadioButton rbPS2Swizzle;
        private System.Windows.Forms.RadioButton rbNoSwizzle;
        private System.Windows.Forms.Label sortLabel;
    }
}

