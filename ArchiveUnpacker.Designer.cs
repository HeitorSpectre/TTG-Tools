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
            this.filesDataGridView = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.encrLuaLabel = new System.Windows.Forms.Label();
            this.xmodeLabel = new System.Windows.Forms.Label();
            this.chunkSizeLabel = new System.Windows.Forms.Label();
            this.compressionLabel = new System.Windows.Forms.Label();
            this.encryptionLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.fileFormatsCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.gameListCB = new System.Windows.Forms.ComboBox();
            this.decryptLuaCB = new System.Windows.Forms.CheckBox();
            this.useCustomKeyCB = new System.Windows.Forms.CheckBox();
            this.customKeyTB = new System.Windows.Forms.TextBox();
            this.searchFilesByNameCB = new System.Windows.Forms.CheckBox();
            this.searchTB = new System.Windows.Forms.TextBox();
            this.searchBtn = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.filesDataGridView)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.actionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(978, 24);
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
            // 
            // filesDataGridView
            // 
            this.filesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.filesDataGridView.Location = new System.Drawing.Point(12, 226);
            this.filesDataGridView.Name = "filesDataGridView";
            this.filesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.filesDataGridView.Size = new System.Drawing.Size(953, 449);
            this.filesDataGridView.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.encrLuaLabel);
            this.groupBox1.Controls.Add(this.xmodeLabel);
            this.groupBox1.Controls.Add(this.chunkSizeLabel);
            this.groupBox1.Controls.Add(this.compressionLabel);
            this.groupBox1.Controls.Add(this.encryptionLabel);
            this.groupBox1.Controls.Add(this.versionLabel);
            this.groupBox1.Location = new System.Drawing.Point(572, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(393, 100);
            this.groupBox1.TabIndex = 2;
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
            // fileFormatsCB
            // 
            this.fileFormatsCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fileFormatsCB.FormattingEnabled = true;
            this.fileFormatsCB.Location = new System.Drawing.Point(406, 67);
            this.fileFormatsCB.Name = "fileFormatsCB";
            this.fileFormatsCB.Size = new System.Drawing.Size(144, 21);
            this.fileFormatsCB.TabIndex = 3;
            this.fileFormatsCB.SelectedIndexChanged += new System.EventHandler(this.fileFormatsCB_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(403, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "File formats:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Encryption key for game:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(16, 189);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(949, 23);
            this.progressBar1.TabIndex = 6;
            // 
            // gameListCB
            // 
            this.gameListCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gameListCB.FormattingEnabled = true;
            this.gameListCB.Location = new System.Drawing.Point(16, 67);
            this.gameListCB.Name = "gameListCB";
            this.gameListCB.Size = new System.Drawing.Size(343, 21);
            this.gameListCB.TabIndex = 7;
            this.gameListCB.SelectedIndexChanged += new System.EventHandler(this.gameListCB_SelectedIndexChanged);
            // 
            // decryptLuaCB
            // 
            this.decryptLuaCB.AutoSize = true;
            this.decryptLuaCB.Location = new System.Drawing.Point(16, 110);
            this.decryptLuaCB.Name = "decryptLuaCB";
            this.decryptLuaCB.Size = new System.Drawing.Size(113, 17);
            this.decryptLuaCB.TabIndex = 8;
            this.decryptLuaCB.Text = "Decrypt lua scripts";
            this.decryptLuaCB.UseVisualStyleBackColor = true;
            // 
            // useCustomKeyCB
            // 
            this.useCustomKeyCB.AutoSize = true;
            this.useCustomKeyCB.Location = new System.Drawing.Point(135, 111);
            this.useCustomKeyCB.Name = "useCustomKeyCB";
            this.useCustomKeyCB.Size = new System.Drawing.Size(111, 17);
            this.useCustomKeyCB.TabIndex = 9;
            this.useCustomKeyCB.Text = "Use a custom key";
            this.useCustomKeyCB.UseVisualStyleBackColor = true;
            this.useCustomKeyCB.CheckedChanged += new System.EventHandler(this.useCustomKeyCB_CheckedChanged);
            // 
            // customKeyTB
            // 
            this.customKeyTB.Location = new System.Drawing.Point(252, 108);
            this.customKeyTB.Name = "customKeyTB";
            this.customKeyTB.Size = new System.Drawing.Size(298, 20);
            this.customKeyTB.TabIndex = 10;
            // 
            // searchFilesByNameCB
            // 
            this.searchFilesByNameCB.AutoSize = true;
            this.searchFilesByNameCB.Location = new System.Drawing.Point(16, 146);
            this.searchFilesByNameCB.Name = "searchFilesByNameCB";
            this.searchFilesByNameCB.Size = new System.Drawing.Size(89, 17);
            this.searchFilesByNameCB.TabIndex = 11;
            this.searchFilesByNameCB.Text = "Search name";
            this.searchFilesByNameCB.UseVisualStyleBackColor = true;
            this.searchFilesByNameCB.CheckedChanged += new System.EventHandler(this.searchFilesByNameCB_CheckedChanged);
            // 
            // searchTB
            // 
            this.searchTB.Location = new System.Drawing.Point(135, 143);
            this.searchTB.Name = "searchTB";
            this.searchTB.Size = new System.Drawing.Size(415, 20);
            this.searchTB.TabIndex = 13;
            this.searchTB.TextChanged += new System.EventHandler(this.searchTB_TextChanged);
            // 
            // searchBtn
            // 
            this.searchBtn.Location = new System.Drawing.Point(572, 141);
            this.searchBtn.Name = "searchBtn";
            this.searchBtn.Size = new System.Drawing.Size(75, 23);
            this.searchBtn.TabIndex = 14;
            this.searchBtn.Text = "Search";
            this.searchBtn.UseVisualStyleBackColor = true;
            this.searchBtn.Click += new System.EventHandler(this.searchBtn_Click);
            // 
            // ArchiveUnpacker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AllowDrop = true;
            this.ClientSize = new System.Drawing.Size(978, 696);
            this.Controls.Add(this.searchBtn);
            this.Controls.Add(this.searchTB);
            this.Controls.Add(this.searchFilesByNameCB);
            this.Controls.Add(this.customKeyTB);
            this.Controls.Add(this.useCustomKeyCB);
            this.Controls.Add(this.decryptLuaCB);
            this.Controls.Add(this.gameListCB);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.fileFormatsCB);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.filesDataGridView);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "ArchiveUnpacker";
            this.Text = "Archive unpacker";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.ArchiveUnpacker_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.ArchiveUnpacker_DragEnter);
            this.Load += new System.EventHandler(this.ArchiveUnpacker_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.filesDataGridView)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
    }
}
