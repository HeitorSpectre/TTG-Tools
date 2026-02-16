namespace TTG_Tools
{
    partial class MainMenu
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainMenu));
            this.autopackerBtn = new System.Windows.Forms.Button();
            this.aboutBtn = new System.Windows.Forms.Button();
            this.fontEditorBtn = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.textEditorBtn = new System.Windows.Forms.Button();
            this.settingsBtn = new System.Windows.Forms.Button();
            this.archivePackerBtn = new System.Windows.Forms.Button();
            this.arcUnpackerBtn = new System.Windows.Forms.Button();
            this.propEditorBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // autopackerBtn
            // 
            this.autopackerBtn.Location = new System.Drawing.Point(34, 29);
            this.autopackerBtn.Name = "autopackerBtn";
            this.autopackerBtn.Size = new System.Drawing.Size(112, 23);
            this.autopackerBtn.TabIndex = 0;
            this.autopackerBtn.Text = "Auto(De)Packer";
            this.autopackerBtn.UseVisualStyleBackColor = true;
            this.autopackerBtn.Click += new System.EventHandler(this.OpenAutopacker_Form_Click);
            // 
            // aboutBtn
            // 
            this.aboutBtn.Location = new System.Drawing.Point(127, 156);
            this.aboutBtn.Name = "aboutBtn";
            this.aboutBtn.Size = new System.Drawing.Size(112, 23);
            this.aboutBtn.TabIndex = 1;
            this.aboutBtn.Text = "About";
            this.aboutBtn.UseVisualStyleBackColor = true;
            this.aboutBtn.Click += new System.EventHandler(this.About_Click);
            // 
            // fontEditorBtn
            // 
            this.fontEditorBtn.Location = new System.Drawing.Point(230, 29);
            this.fontEditorBtn.Name = "fontEditorBtn";
            this.fontEditorBtn.Size = new System.Drawing.Size(111, 23);
            this.fontEditorBtn.TabIndex = 2;
            this.fontEditorBtn.Text = "Font Editor";
            this.fontEditorBtn.UseVisualStyleBackColor = true;
            this.fontEditorBtn.Click += new System.EventHandler(this.RunFontEditor_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "TTG Tools";
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // textEditorBtn
            // 
            this.textEditorBtn.Location = new System.Drawing.Point(230, 69);
            this.textEditorBtn.Name = "textEditorBtn";
            this.textEditorBtn.Size = new System.Drawing.Size(111, 23);
            this.textEditorBtn.TabIndex = 7;
            this.textEditorBtn.Text = "Text Editor";
            this.textEditorBtn.UseVisualStyleBackColor = true;
            this.textEditorBtn.Click += new System.EventHandler(this.button1_Click);
            // 
            // settingsBtn
            // 
            this.settingsBtn.Location = new System.Drawing.Point(33, 108);
            this.settingsBtn.Name = "settingsBtn";
            this.settingsBtn.Size = new System.Drawing.Size(111, 23);
            this.settingsBtn.TabIndex = 9;
            this.settingsBtn.Text = "Settings";
            this.settingsBtn.UseVisualStyleBackColor = true;
            this.settingsBtn.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // archivePackerBtn
            // 
            this.archivePackerBtn.Location = new System.Drawing.Point(34, 69);
            this.archivePackerBtn.Margin = new System.Windows.Forms.Padding(2);
            this.archivePackerBtn.Name = "archivePackerBtn";
            this.archivePackerBtn.Size = new System.Drawing.Size(111, 23);
            this.archivePackerBtn.TabIndex = 12;
            this.archivePackerBtn.Text = "Archive packer";
            this.archivePackerBtn.UseVisualStyleBackColor = true;
            this.archivePackerBtn.Click += new System.EventHandler(this.button5_Click);
            // 
            // arcUnpackerBtn
            // 
            this.arcUnpackerBtn.Location = new System.Drawing.Point(230, 108);
            this.arcUnpackerBtn.Margin = new System.Windows.Forms.Padding(2);
            this.arcUnpackerBtn.Name = "arcUnpackerBtn";
            this.arcUnpackerBtn.Size = new System.Drawing.Size(111, 23);
            this.arcUnpackerBtn.TabIndex = 13;
            this.arcUnpackerBtn.Text = "Archive unpacker";
            this.arcUnpackerBtn.UseVisualStyleBackColor = true;
            this.arcUnpackerBtn.Click += new System.EventHandler(this.arcUnpackerBtn_Click);
            // 
            // propEditorBtn
            // 
            this.propEditorBtn.Location = new System.Drawing.Point(127, 108);
            this.propEditorBtn.Name = "propEditorBtn";
            this.propEditorBtn.Size = new System.Drawing.Size(112, 23);
            this.propEditorBtn.TabIndex = 14;
            this.propEditorBtn.Text = "Prop Editor";
            this.propEditorBtn.UseVisualStyleBackColor = true;
            this.propEditorBtn.Click += new System.EventHandler(this.propEditorBtn_Click);
            // 
            // MainMenu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 191);
            this.Controls.Add(this.propEditorBtn);
            this.Controls.Add(this.arcUnpackerBtn);
            this.Controls.Add(this.archivePackerBtn);
            this.Controls.Add(this.settingsBtn);
            this.Controls.Add(this.textEditorBtn);
            this.Controls.Add(this.fontEditorBtn);
            this.Controls.Add(this.aboutBtn);
            this.Controls.Add(this.autopackerBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainMenu";
            this.Text = "TTG Tools by Den Em and Pashok6798";
            this.Load += new System.EventHandler(this.MainMenu_Load);
            this.Resize += new System.EventHandler(this.MainMenu_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button autopackerBtn;
        private System.Windows.Forms.Button aboutBtn;
        private System.Windows.Forms.Button fontEditorBtn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Button textEditorBtn;
        private System.Windows.Forms.Button settingsBtn;
        private System.Windows.Forms.Button archivePackerBtn;
        private System.Windows.Forms.Button arcUnpackerBtn;
        private System.Windows.Forms.Button propEditorBtn;
    }
}
