namespace TTG_Tools
{
    partial class ArchiveUnpacker
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchiveUnpacker));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unpackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unpackSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.gameListCB = new System.Windows.Forms.ComboBox();
            this.useCustomKeyCB = new System.Windows.Forms.CheckBox();
            this.customKeyTB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.fileFormatsCB = new System.Windows.Forms.ComboBox();
            this.decryptLuaCB = new System.Windows.Forms.CheckBox();
            this.searchFilesByNameCB = new System.Windows.Forms.CheckBox();
            this.searchTB = new System.Windows.Forms.TextBox();
            this.searchBtn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.encrLuaLabel = new System.Windows.Forms.Label();
            this.xmodeLabel = new System.Windows.Forms.Label();
            this.chunkSizeLabel = new System.Windows.Forms.Label();
            this.compressionLabel = new System.Windows.Forms.Label();
            this.encryptionLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.filesDataGridView = new System.Windows.Forms.DataGridView();
            this.previewPictureBox = new System.Windows.Forms.PictureBox();
            this.previewTextBox = new System.Windows.Forms.RichTextBox();
            this.previewInfoLabel = new System.Windows.Forms.Label();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.menuStrip1.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.filesDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.previewPictureBox)).BeginInit();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.actionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1184, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            //
            // fileToolStripMenuItem
            //
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            //
            // openToolStripMenuItem
            //
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // actionsToolStripMenuItem
            //
            this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.unpackToolStripMenuItem,
            this.unpackSelectedToolStripMenuItem});
            this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            this.actionsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.actionsToolStripMenuItem.Text = "Actions";
            //
            // unpackToolStripMenuItem
            //
            this.unpackToolStripMenuItem.Name = "unpackToolStripMenuItem";
            this.unpackToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.unpackToolStripMenuItem.Text = "Unpack";
            this.unpackToolStripMenuItem.Click += new System.EventHandler(this.unpackToolStripMenuItem_Click);
            //
            // unpackSelectedToolStripMenuItem
            //
            this.unpackSelectedToolStripMenuItem.Name = "unpackSelectedToolStripMenuItem";
            this.unpackSelectedToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.unpackSelectedToolStripMenuItem.Text = "Unpack selected";
            this.unpackSelectedToolStripMenuItem.Click += new System.EventHandler(this.unpackSelectedToolStripMenuItem_Click);
            // topPanel
            //
            this.topPanel.Controls.Add(this.label2);
            this.topPanel.Controls.Add(this.gameListCB);
            this.topPanel.Controls.Add(this.useCustomKeyCB);
            this.topPanel.Controls.Add(this.customKeyTB);
            this.topPanel.Controls.Add(this.label1);
            this.topPanel.Controls.Add(this.fileFormatsCB);
            this.topPanel.Controls.Add(this.decryptLuaCB);
            this.topPanel.Controls.Add(this.searchFilesByNameCB);
            this.topPanel.Controls.Add(this.searchTB);
            this.topPanel.Controls.Add(this.searchBtn);
            this.topPanel.Controls.Add(this.groupBox1);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 24);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(1184, 108);
            this.topPanel.TabIndex = 2;
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Encryption key for game:";
            //
            // gameListCB
            //
            this.gameListCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gameListCB.FormattingEnabled = true;
            this.gameListCB.Location = new System.Drawing.Point(142, 9);
            this.gameListCB.Name = "gameListCB";
            this.gameListCB.Size = new System.Drawing.Size(300, 21);
            this.gameListCB.TabIndex = 1;
            this.gameListCB.SelectedIndexChanged += new System.EventHandler(this.gameListCB_SelectedIndexChanged);
            //
            // useCustomKeyCB
            //
            this.useCustomKeyCB.AutoSize = true;
            this.useCustomKeyCB.Location = new System.Drawing.Point(455, 11);
            this.useCustomKeyCB.Name = "useCustomKeyCB";
            this.useCustomKeyCB.Size = new System.Drawing.Size(111, 17);
            this.useCustomKeyCB.TabIndex = 2;
            this.useCustomKeyCB.Text = "Use a custom key";
            this.useCustomKeyCB.UseVisualStyleBackColor = true;
            this.useCustomKeyCB.CheckedChanged += new System.EventHandler(this.useCustomKeyCB_CheckedChanged);
            //
            // customKeyTB
            //
            this.customKeyTB.Location = new System.Drawing.Point(572, 9);
            this.customKeyTB.Name = "customKeyTB";
            this.customKeyTB.Size = new System.Drawing.Size(200, 20);
            this.customKeyTB.TabIndex = 3;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "File formats:";
            //
            // fileFormatsCB
            //
            this.fileFormatsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fileFormatsCB.FormattingEnabled = true;
            this.fileFormatsCB.Location = new System.Drawing.Point(142, 40);
            this.fileFormatsCB.Name = "fileFormatsCB";
            this.fileFormatsCB.Size = new System.Drawing.Size(160, 21);
            this.fileFormatsCB.TabIndex = 5;
            this.fileFormatsCB.SelectedIndexChanged += new System.EventHandler(this.fileFormatsCB_SelectedIndexChanged);
            //
            // decryptLuaCB
            //
            this.decryptLuaCB.AutoSize = true;
            this.decryptLuaCB.Location = new System.Drawing.Point(455, 42);
            this.decryptLuaCB.Name = "decryptLuaCB";
            this.decryptLuaCB.Size = new System.Drawing.Size(113, 17);
            this.decryptLuaCB.TabIndex = 6;
            this.decryptLuaCB.Text = "Decrypt lua scripts";
            this.decryptLuaCB.UseVisualStyleBackColor = true;
            //
            // searchFilesByNameCB
            //
            this.searchFilesByNameCB.AutoSize = true;
            this.searchFilesByNameCB.Location = new System.Drawing.Point(10, 74);
            this.searchFilesByNameCB.Name = "searchFilesByNameCB";
            this.searchFilesByNameCB.Size = new System.Drawing.Size(89, 17);
            this.searchFilesByNameCB.TabIndex = 7;
            this.searchFilesByNameCB.Text = "Search name";
            this.searchFilesByNameCB.UseVisualStyleBackColor = true;
            this.searchFilesByNameCB.CheckedChanged += new System.EventHandler(this.searchFilesByNameCB_CheckedChanged);
            //
            // searchTB
            //
            this.searchTB.Location = new System.Drawing.Point(142, 72);
            this.searchTB.Name = "searchTB";
            this.searchTB.Size = new System.Drawing.Size(300, 20);
            this.searchTB.TabIndex = 8;
            this.searchTB.TextChanged += new System.EventHandler(this.searchTB_TextChanged);
            //
            // searchBtn
            //
            this.searchBtn.Location = new System.Drawing.Point(455, 70);
            this.searchBtn.Name = "searchBtn";
            this.searchBtn.Size = new System.Drawing.Size(75, 23);
            this.searchBtn.TabIndex = 9;
            this.searchBtn.Text = "Search";
            this.searchBtn.UseVisualStyleBackColor = true;
            this.searchBtn.Click += new System.EventHandler(this.searchBtn_Click);
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.encrLuaLabel);
            this.groupBox1.Controls.Add(this.xmodeLabel);
            this.groupBox1.Controls.Add(this.chunkSizeLabel);
            this.groupBox1.Controls.Add(this.compressionLabel);
            this.groupBox1.Controls.Add(this.encryptionLabel);
            this.groupBox1.Controls.Add(this.versionLabel);
            this.groupBox1.Location = new System.Drawing.Point(786, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(393, 100);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Archive info";
            //
            // encrLuaLabel
            //
            this.encrLuaLabel.AutoSize = true;
            this.encrLuaLabel.Location = new System.Drawing.Point(175, 34);
            this.encrLuaLabel.Name = "encrLuaLabel";
            this.encrLuaLabel.Size = new System.Drawing.Size(111, 13);
            this.encrLuaLabel.TabIndex = 5;
            this.encrLuaLabel.Text = "Lua scripts encrypted:";
            //
            // xmodeLabel
            //
            this.xmodeLabel.AutoSize = true;
            this.xmodeLabel.Location = new System.Drawing.Point(175, 16);
            this.xmodeLabel.Name = "xmodeLabel";
            this.xmodeLabel.Size = new System.Drawing.Size(173, 13);
            this.xmodeLabel.TabIndex = 4;
            this.xmodeLabel.Text = "Has X mode (in some old archives):";
            //
            // chunkSizeLabel
            //
            this.chunkSizeLabel.AutoSize = true;
            this.chunkSizeLabel.Location = new System.Drawing.Point(18, 71);
            this.chunkSizeLabel.Name = "chunkSizeLabel";
            this.chunkSizeLabel.Size = new System.Drawing.Size(62, 13);
            this.chunkSizeLabel.TabIndex = 3;
            this.chunkSizeLabel.Text = "Chunk size:";
            //
            // compressionLabel
            //
            this.compressionLabel.AutoSize = true;
            this.compressionLabel.Location = new System.Drawing.Point(18, 53);
            this.compressionLabel.Name = "compressionLabel";
            this.compressionLabel.Size = new System.Drawing.Size(68, 13);
            this.compressionLabel.TabIndex = 2;
            this.compressionLabel.Text = "Compressed:";
            //
            // encryptionLabel
            //
            this.encryptionLabel.AutoSize = true;
            this.encryptionLabel.Location = new System.Drawing.Point(18, 34);
            this.encryptionLabel.Name = "encryptionLabel";
            this.encryptionLabel.Size = new System.Drawing.Size(58, 13);
            this.encryptionLabel.TabIndex = 1;
            this.encryptionLabel.Text = "Encrypted:";
            //
            // versionLabel
            //
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(18, 16);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(45, 13);
            this.versionLabel.TabIndex = 0;
            this.versionLabel.Text = "Version:";
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 132);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.filesDataGridView);
            this.splitContainer1.Panel1MinSize = 320;
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.previewPictureBox);
            this.splitContainer1.Panel2.Controls.Add(this.previewTextBox);
            this.splitContainer1.Panel2.Controls.Add(this.previewInfoLabel);
            this.splitContainer1.Panel2MinSize = 240;
            this.splitContainer1.Size = new System.Drawing.Size(1184, 499);
            this.splitContainer1.SplitterDistance = 690;
            this.splitContainer1.TabIndex = 3;
            //
            // filesDataGridView
            //
            this.filesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.filesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filesDataGridView.Location = new System.Drawing.Point(0, 0);
            this.filesDataGridView.Name = "filesDataGridView";
            this.filesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.filesDataGridView.Size = new System.Drawing.Size(690, 499);
            this.filesDataGridView.TabIndex = 0;
            this.filesDataGridView.SelectionChanged += new System.EventHandler(this.filesDataGridView_SelectionChanged);
            //
            // previewPictureBox
            //
            this.previewPictureBox.BackColor = System.Drawing.SystemColors.Window;
            this.previewPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewPictureBox.Location = new System.Drawing.Point(0, 0);
            this.previewPictureBox.Name = "previewPictureBox";
            this.previewPictureBox.Size = new System.Drawing.Size(490, 479);
            this.previewPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.previewPictureBox.TabIndex = 0;
            this.previewPictureBox.TabStop = false;
            //
            // previewTextBox
            //
            this.previewTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.previewTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.previewTextBox.Location = new System.Drawing.Point(0, 0);
            this.previewTextBox.Name = "previewTextBox";
            this.previewTextBox.ReadOnly = true;
            this.previewTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.previewTextBox.Size = new System.Drawing.Size(490, 479);
            this.previewTextBox.TabIndex = 1;
            this.previewTextBox.Visible = false;
            this.previewTextBox.WordWrap = false;
            //
            // previewInfoLabel
            //
            this.previewInfoLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.previewInfoLabel.Location = new System.Drawing.Point(0, 479);
            this.previewInfoLabel.Name = "previewInfoLabel";
            this.previewInfoLabel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.previewInfoLabel.Size = new System.Drawing.Size(490, 20);
            this.previewInfoLabel.TabIndex = 2;
            this.previewInfoLabel.Text = "";
            this.previewInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // bottomPanel
            //
            this.bottomPanel.Controls.Add(this.progressBar1);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 631);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.bottomPanel.Size = new System.Drawing.Size(1184, 30);
            this.bottomPanel.TabIndex = 4;
            //
            // progressBar1
            //
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar1.Location = new System.Drawing.Point(6, 4);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1172, 22);
            this.progressBar1.TabIndex = 0;
            //
            // ArchiveUnpacker
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AllowDrop = true;
            this.ClientSize = new System.Drawing.Size(1184, 661);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(1024, 620);
            this.Name = "ArchiveUnpacker";
            this.Text = "Archive Unpacker";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.ArchiveUnpacker_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.ArchiveUnpacker_DragEnter);
            this.Load += new System.EventHandler(this.ArchiveUnpacker_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.filesDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.previewPictureBox)).EndInit();
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unpackToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unpackSelectedToolStripMenuItem;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.DataGridView filesDataGridView;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox fileFormatsCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ComboBox gameListCB;
        private System.Windows.Forms.CheckBox decryptLuaCB;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label chunkSizeLabel;
        private System.Windows.Forms.Label compressionLabel;
        private System.Windows.Forms.Label encryptionLabel;
        private System.Windows.Forms.Label xmodeLabel;
        private System.Windows.Forms.CheckBox useCustomKeyCB;
        private System.Windows.Forms.TextBox customKeyTB;
        private System.Windows.Forms.Label encrLuaLabel;
        private System.Windows.Forms.CheckBox searchFilesByNameCB;
        private System.Windows.Forms.TextBox searchTB;
        private System.Windows.Forms.Button searchBtn;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.PictureBox previewPictureBox;
        private System.Windows.Forms.RichTextBox previewTextBox;
        private System.Windows.Forms.Label previewInfoLabel;
        private System.Windows.Forms.Panel bottomPanel;
    }
}
