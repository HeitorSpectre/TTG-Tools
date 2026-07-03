namespace TTG_Tools
{
    partial class LuaEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblGame = new System.Windows.Forms.Label();
            this.comboGame = new System.Windows.Forms.ComboBox();
            this.lblVer = new System.Windows.Forms.Label();
            this.comboLuaVersion = new System.Windows.Forms.ComboBox();
            this.chkNewEngine = new System.Windows.Forms.CheckBox();

            this.tabs = new System.Windows.Forms.TabControl();
            this.tabEditor = new System.Windows.Forms.TabPage();
            this.tabSingle = new System.Windows.Forms.TabPage();
            this.tabBatch = new System.Windows.Forms.TabPage();

            this.btnLoadEdit = new System.Windows.Forms.Button();
            this.lblLoaded = new System.Windows.Forms.Label();
            this.btnSaveRepack = new System.Windows.Forms.Button();
            this.editorToolbar = new System.Windows.Forms.Panel();
            this.editorContainer = new System.Windows.Forms.Panel();
            this.gutter = new TTG_Tools.BufferedPanel();
            this.rtbEditor = new TTG_Tools.CodeRichTextBox();
            this.btnToggleWhitespace = new System.Windows.Forms.Button();

            this.btnDecrypt = new System.Windows.Forms.Button();
            this.btnDecompile = new System.Windows.Forms.Button();
            this.btnCompile = new System.Windows.Forms.Button();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.btnSingleExtract = new System.Windows.Forms.Button();
            this.btnSingleRepack = new System.Windows.Forms.Button();
            this.lblSingleManual = new System.Windows.Forms.Label();
            this.lblSingleHelp = new System.Windows.Forms.Label();
            this.lblWorkflowMode = new System.Windows.Forms.Label();
            this.comboWorkflowMode = new System.Windows.Forms.ComboBox();
            this.lblEncMethod = new System.Windows.Forms.Label();
            this.comboEncMethod = new System.Windows.Forms.ComboBox();
            this.btnSmartExtract = new System.Windows.Forms.Button();
            this.btnSmartRepack = new System.Windows.Forms.Button();
            this.lblAdvancedBatch = new System.Windows.Forms.Label();

            this.txtBatchPath = new System.Windows.Forms.TextBox();
            this.btnBrowseBatch = new System.Windows.Forms.Button();
            this.btnBatchDecrypt = new System.Windows.Forms.Button();
            this.btnBatchDecompile = new System.Windows.Forms.Button();
            this.btnBatchCompile = new System.Windows.Forms.Button();
            this.btnBatchEncrypt = new System.Windows.Forms.Button();

            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.txtLog = new System.Windows.Forms.TextBox();

            this.panelTop.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabEditor.SuspendLayout();
            this.tabSingle.SuspendLayout();
            this.tabBatch.SuspendLayout();
            this.SuspendLayout();

            // panelTop
            this.panelTop.Controls.Add(this.lblGame);
            this.panelTop.Controls.Add(this.comboGame);
            this.panelTop.Controls.Add(this.lblVer);
            this.panelTop.Controls.Add(this.comboLuaVersion);
            this.panelTop.Controls.Add(this.chkNewEngine);
            this.panelTop.Controls.Add(this.lblWorkflowMode);
            this.panelTop.Controls.Add(this.comboWorkflowMode);
            this.panelTop.Controls.Add(this.lblEncMethod);
            this.panelTop.Controls.Add(this.comboEncMethod);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(900, 72);

            // lblGame
            this.lblGame.AutoSize = true;
            this.lblGame.Location = new System.Drawing.Point(10, 14);
            this.lblGame.Text = "Game:";

            // comboGame
            this.comboGame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboGame.Location = new System.Drawing.Point(55, 11);
            this.comboGame.Size = new System.Drawing.Size(320, 21);
            this.comboGame.Name = "comboGame";
            this.comboGame.SelectedIndexChanged += new System.EventHandler(this.comboGame_SelectedIndexChanged);

            // lblVer
            this.lblVer.AutoSize = true;
            this.lblVer.Location = new System.Drawing.Point(395, 14);
            this.lblVer.Text = "Lua version:";

            // comboLuaVersion
            this.comboLuaVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboLuaVersion.Location = new System.Drawing.Point(470, 11);
            this.comboLuaVersion.Size = new System.Drawing.Size(170, 21);
            this.comboLuaVersion.Name = "comboLuaVersion";

            // chkNewEngine
            this.chkNewEngine.AutoSize = true;
            this.chkNewEngine.Location = new System.Drawing.Point(660, 13);
            this.chkNewEngine.Text = "New engine encryption (LEn/LEo)";
            this.chkNewEngine.Checked = false;

            // tabs
            this.tabs.Controls.Add(this.tabEditor);
            this.tabs.Controls.Add(this.tabSingle);
            this.tabs.Controls.Add(this.tabBatch);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 72);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;

            // tabEditor
            this.tabEditor.Controls.Add(this.editorContainer);
            this.tabEditor.Controls.Add(this.editorToolbar);
            this.tabEditor.Text = "Editor";
            this.tabEditor.UseVisualStyleBackColor = true;
            this.tabEditor.Padding = new System.Windows.Forms.Padding(0);

            // editorToolbar
            this.editorToolbar.Controls.Add(this.btnLoadEdit);
            this.editorToolbar.Controls.Add(this.btnSaveRepack);
            this.editorToolbar.Controls.Add(this.lblLoaded);
            this.editorToolbar.Controls.Add(this.btnToggleWhitespace);
            this.editorToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorToolbar.Height = 40;
            this.editorToolbar.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.editorToolbar.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);

            // btnToggleWhitespace
            this.btnToggleWhitespace.Location = new System.Drawing.Point(862, 6);
            this.btnToggleWhitespace.Size = new System.Drawing.Size(34, 26);
            this.btnToggleWhitespace.Text = "¶";
            this.btnToggleWhitespace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleWhitespace.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 84);
            this.btnToggleWhitespace.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnToggleWhitespace.ForeColor = System.Drawing.Color.White;
            this.btnToggleWhitespace.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnToggleWhitespace.UseVisualStyleBackColor = false;

            // btnLoadEdit
            this.btnLoadEdit.Location = new System.Drawing.Point(8, 6);
            this.btnLoadEdit.Size = new System.Drawing.Size(120, 26);
            this.btnLoadEdit.Text = "Open";
            this.btnLoadEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadEdit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 84);
            this.btnLoadEdit.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnLoadEdit.ForeColor = System.Drawing.Color.White;
            this.btnLoadEdit.Click += new System.EventHandler(this.btnLoadEdit_Click);

            // btnSaveRepack
            this.btnSaveRepack.Location = new System.Drawing.Point(134, 6);
            this.btnSaveRepack.Size = new System.Drawing.Size(140, 26);
            this.btnSaveRepack.Text = "Save as...";
            this.btnSaveRepack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveRepack.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 84);
            this.btnSaveRepack.BackColor = System.Drawing.Color.FromArgb(62, 62, 66);
            this.btnSaveRepack.ForeColor = System.Drawing.Color.White;
            this.btnSaveRepack.Click += new System.EventHandler(this.btnSaveRepack_Click);

            // lblLoaded
            this.lblLoaded.AutoSize = true;
            this.lblLoaded.Location = new System.Drawing.Point(284, 13);
            this.lblLoaded.Text = "No file loaded.";
            this.lblLoaded.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);

            // editorContainer
            this.editorContainer.Controls.Add(this.rtbEditor);
            this.editorContainer.Controls.Add(this.gutter);
            this.editorContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorContainer.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.editorContainer.Padding = new System.Windows.Forms.Padding(0);

            // gutter
            this.gutter.Dock = System.Windows.Forms.DockStyle.Left;
            this.gutter.Width = 48;
            this.gutter.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);

            // rtbEditor
            this.rtbEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbEditor.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.rtbEditor.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this.rtbEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbEditor.Font = new System.Drawing.Font("Consolas", 10.5F);
            this.rtbEditor.AcceptsTab = true;
            this.rtbEditor.WordWrap = false;
            this.rtbEditor.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.rtbEditor.HideSelection = false;

            // tabSingle
            this.tabSingle.Controls.Add(this.lblSingleHelp);
            this.tabSingle.Controls.Add(this.btnSingleExtract);
            this.tabSingle.Controls.Add(this.btnSingleRepack);
            this.tabSingle.Controls.Add(this.lblSingleManual);
            this.tabSingle.Controls.Add(this.btnDecrypt);
            this.tabSingle.Controls.Add(this.btnDecompile);
            this.tabSingle.Controls.Add(this.btnCompile);
            this.tabSingle.Controls.Add(this.btnEncrypt);
            this.tabSingle.Text = "Single file";
            this.tabSingle.UseVisualStyleBackColor = true;

            this.lblSingleHelp.AutoSize = true;
            this.lblSingleHelp.Location = new System.Drawing.Point(10, 10);
            this.lblSingleHelp.Text = "Process a single file using the workflow mode selected at the top.";
            this.lblSingleHelp.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            this.btnSingleExtract.Location = new System.Drawing.Point(10, 35);
            this.btnSingleExtract.Size = new System.Drawing.Size(390, 48);
            this.btnSingleExtract.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSingleExtract.Text = "EXTRACT → editable text";
            this.btnSingleExtract.UseVisualStyleBackColor = true;
            this.btnSingleExtract.Click += new System.EventHandler(this.btnSingleExtract_Click);

            this.btnSingleRepack.Location = new System.Drawing.Point(410, 35);
            this.btnSingleRepack.Size = new System.Drawing.Size(390, 48);
            this.btnSingleRepack.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSingleRepack.Text = "REPACK → game file";
            this.btnSingleRepack.UseVisualStyleBackColor = true;
            this.btnSingleRepack.Click += new System.EventHandler(this.btnSingleRepack_Click);

            this.lblSingleManual.AutoSize = true;
            this.lblSingleManual.Location = new System.Drawing.Point(10, 100);
            this.lblSingleManual.Text = "Manual steps:";
            this.lblSingleManual.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            this.btnDecrypt.Location = new System.Drawing.Point(10, 125);
            this.btnDecrypt.Size = new System.Drawing.Size(190, 28);
            this.btnDecrypt.Text = "Decrypt   .lenc → .lua";
            this.btnDecrypt.UseVisualStyleBackColor = true;
            this.btnDecrypt.Click += new System.EventHandler(this.btnDecrypt_Click);

            this.btnDecompile.Location = new System.Drawing.Point(210, 125);
            this.btnDecompile.Size = new System.Drawing.Size(190, 28);
            this.btnDecompile.Text = "Decompile   binary → text";
            this.btnDecompile.UseVisualStyleBackColor = true;
            this.btnDecompile.Click += new System.EventHandler(this.btnDecompile_Click);

            this.btnCompile.Location = new System.Drawing.Point(410, 125);
            this.btnCompile.Size = new System.Drawing.Size(190, 28);
            this.btnCompile.Text = "Compile   text → binary";
            this.btnCompile.UseVisualStyleBackColor = true;
            this.btnCompile.Click += new System.EventHandler(this.btnCompile_Click);

            this.btnEncrypt.Location = new System.Drawing.Point(610, 125);
            this.btnEncrypt.Size = new System.Drawing.Size(190, 28);
            this.btnEncrypt.Text = "Encrypt   .lua → .lenc";
            this.btnEncrypt.UseVisualStyleBackColor = true;
            this.btnEncrypt.Click += new System.EventHandler(this.btnEncrypt_Click);

            // tabBatch
            this.tabBatch.Controls.Add(this.txtBatchPath);
            this.tabBatch.Controls.Add(this.btnBrowseBatch);
            this.tabBatch.Controls.Add(this.btnBatchDecrypt);
            this.tabBatch.Controls.Add(this.btnBatchDecompile);
            this.tabBatch.Controls.Add(this.btnBatchCompile);
            this.tabBatch.Controls.Add(this.btnBatchEncrypt);
            this.tabBatch.Controls.Add(this.btnSmartExtract);
            this.tabBatch.Controls.Add(this.btnSmartRepack);
            this.tabBatch.Controls.Add(this.lblAdvancedBatch);
            this.tabBatch.Text = "Batch";
            this.tabBatch.UseVisualStyleBackColor = true;

            this.txtBatchPath.Location = new System.Drawing.Point(10, 14);
            this.txtBatchPath.Size = new System.Drawing.Size(700, 20);
            this.txtBatchPath.Name = "txtBatchPath";

            this.btnBrowseBatch.Location = new System.Drawing.Point(715, 12);
            this.btnBrowseBatch.Size = new System.Drawing.Size(85, 24);
            this.btnBrowseBatch.Text = "Browse...";
            this.btnBrowseBatch.UseVisualStyleBackColor = true;
            this.btnBrowseBatch.Click += new System.EventHandler(this.btnBrowseBatch_Click);

            // Row 2: workflow mode + encryption method
            this.lblWorkflowMode.AutoSize = true;
            this.lblWorkflowMode.Location = new System.Drawing.Point(10, 45);
            this.lblWorkflowMode.Text = "Game format:";

            this.comboWorkflowMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboWorkflowMode.Location = new System.Drawing.Point(90, 42);
            this.comboWorkflowMode.Size = new System.Drawing.Size(295, 21);
            this.comboWorkflowMode.Name = "comboWorkflowMode";

            this.lblEncMethod.AutoSize = true;
            this.lblEncMethod.Location = new System.Drawing.Point(400, 45);
            this.lblEncMethod.Text = "Encryption:";

            this.comboEncMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboEncMethod.Location = new System.Drawing.Point(470, 42);
            this.comboEncMethod.Size = new System.Drawing.Size(175, 21);
            this.comboEncMethod.Name = "comboEncMethod";


            // Smart Extract / Repack — main workflow
            this.btnSmartExtract.Location = new System.Drawing.Point(10, 50);
            this.btnSmartExtract.Size = new System.Drawing.Size(390, 48);
            this.btnSmartExtract.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSmartExtract.Text = "EXTRACT → editable text";
            this.btnSmartExtract.UseVisualStyleBackColor = true;
            this.btnSmartExtract.Click += new System.EventHandler(this.btnSmartExtract_Click);

            this.btnSmartRepack.Location = new System.Drawing.Point(410, 50);
            this.btnSmartRepack.Size = new System.Drawing.Size(390, 48);
            this.btnSmartRepack.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSmartRepack.Text = "REPACK → game files";
            this.btnSmartRepack.UseVisualStyleBackColor = true;
            this.btnSmartRepack.Click += new System.EventHandler(this.btnSmartRepack_Click);

            // --- Manual steps ---
            this.lblAdvancedBatch.AutoSize = true;
            this.lblAdvancedBatch.Location = new System.Drawing.Point(10, 115);
            this.lblAdvancedBatch.Text = "Manual steps:";
            this.lblAdvancedBatch.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);

            this.btnBatchDecrypt.Location = new System.Drawing.Point(10, 140);
            this.btnBatchDecrypt.Size = new System.Drawing.Size(190, 28);
            this.btnBatchDecrypt.Text = "Decrypt  *.lenc → *.lua";
            this.btnBatchDecrypt.UseVisualStyleBackColor = true;
            this.btnBatchDecrypt.Click += new System.EventHandler(this.btnBatchDecrypt_Click);

            this.btnBatchDecompile.Location = new System.Drawing.Point(210, 140);
            this.btnBatchDecompile.Size = new System.Drawing.Size(190, 28);
            this.btnBatchDecompile.Text = "Decompile  *.lua → *.lua";
            this.btnBatchDecompile.UseVisualStyleBackColor = true;
            this.btnBatchDecompile.Click += new System.EventHandler(this.btnBatchDecompile_Click);

            this.btnBatchCompile.Location = new System.Drawing.Point(410, 140);
            this.btnBatchCompile.Size = new System.Drawing.Size(190, 28);
            this.btnBatchCompile.Text = "Compile  *.lua → *.lua";
            this.btnBatchCompile.UseVisualStyleBackColor = true;
            this.btnBatchCompile.Click += new System.EventHandler(this.btnBatchCompile_Click);

            this.btnBatchEncrypt.Location = new System.Drawing.Point(610, 140);
            this.btnBatchEncrypt.Size = new System.Drawing.Size(190, 28);
            this.btnBatchEncrypt.Text = "Encrypt  *.lua → *.lenc";
            this.btnBatchEncrypt.UseVisualStyleBackColor = true;
            this.btnBatchEncrypt.Click += new System.EventHandler(this.btnBatchEncrypt_Click);



            // progressBar
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar.Size = new System.Drawing.Size(900, 8);
            this.progressBar.Name = "progressBar";

            // txtLog
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.ReadOnly = true;
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(24, 24, 24);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Size = new System.Drawing.Size(900, 100);
            this.txtLog.Name = "txtLog";
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);

            // LuaEditor
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 921);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(820, 500);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Name = "LuaEditor";
            this.Text = "Lua Editor";
            this.Load += new System.EventHandler(this.LuaEditor_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LuaEditor_FormClosing);

            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.tabs.ResumeLayout(false);
            this.tabEditor.ResumeLayout(false);
            this.tabEditor.PerformLayout();
            this.tabSingle.ResumeLayout(false);
            this.tabSingle.PerformLayout();
            this.tabBatch.ResumeLayout(false);
            this.tabBatch.PerformLayout();
            this.ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblGame;
        private System.Windows.Forms.ComboBox comboGame;
        private System.Windows.Forms.Label lblVer;
        private System.Windows.Forms.ComboBox comboLuaVersion;
        private System.Windows.Forms.CheckBox chkNewEngine;

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabEditor;
        private System.Windows.Forms.TabPage tabSingle;
        private System.Windows.Forms.TabPage tabBatch;

        private System.Windows.Forms.Button btnLoadEdit;
        private System.Windows.Forms.Button btnSaveRepack;
        private System.Windows.Forms.Label lblLoaded;
        private System.Windows.Forms.Panel editorToolbar;
        private System.Windows.Forms.Panel editorContainer;
        private TTG_Tools.BufferedPanel gutter;
        private TTG_Tools.CodeRichTextBox rtbEditor;
        private System.Windows.Forms.Button btnToggleWhitespace;

        private System.Windows.Forms.Label lblSingleHelp;
        private System.Windows.Forms.Button btnSingleExtract;
        private System.Windows.Forms.Button btnSingleRepack;
        private System.Windows.Forms.Label lblSingleManual;
        private System.Windows.Forms.Button btnDecrypt;
        private System.Windows.Forms.Button btnDecompile;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Button btnEncrypt;

        private System.Windows.Forms.TextBox txtBatchPath;
        private System.Windows.Forms.Button btnBrowseBatch;
        private System.Windows.Forms.Button btnBatchDecrypt;
        private System.Windows.Forms.Button btnBatchDecompile;
        private System.Windows.Forms.Button btnBatchCompile;
        private System.Windows.Forms.Button btnBatchEncrypt;
        private System.Windows.Forms.Label lblWorkflowMode;
        private System.Windows.Forms.ComboBox comboWorkflowMode;
        private System.Windows.Forms.Label lblEncMethod;
        private System.Windows.Forms.ComboBox comboEncMethod;
        private System.Windows.Forms.Button btnSmartExtract;
        private System.Windows.Forms.Button btnSmartRepack;
        private System.Windows.Forms.Label lblAdvancedBatch;

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox txtLog;
    }
}
