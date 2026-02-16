namespace TTG_Tools
{
    partial class AutoDePackerSettings
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
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.newTxtFormatRB = new System.Windows.Forms.RadioButton();
            this.tsvFilesRB = new System.Windows.Forms.RadioButton();
            this.txtFilesRB = new System.Windows.Forms.RadioButton();
            this.clearMessagesCB = new System.Windows.Forms.CheckBox();
            this.checkBoxSortStrings = new System.Windows.Forms.CheckBox();
            this.checkBoxExportRealID = new System.Windows.Forms.CheckBox();
            this.checkBoxImportingOfNames = new System.Windows.Forms.CheckBox();
            this.checkBoxDDS_after_import = new System.Windows.Forms.CheckBox();
            this.checkBoxD3DTX_after_import = new System.Windows.Forms.CheckBox();
            this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
            this.textBoxInputFolder = new System.Windows.Forms.TextBox();
            this.buttonOutputFolder = new System.Windows.Forms.Button();
            this.buttonInputFolder = new System.Windows.Forms.Button();
            this.okBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.checkBoxChangeLangFlags = new System.Windows.Forms.CheckBox();
            this.cbIgnoreEmptyStrings = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rbNewBttF = new System.Windows.Forms.RadioButton();
            this.rbTwdNintendoSwitch = new System.Windows.Forms.RadioButton();
            this.rbNonNormalUnicode2 = new System.Windows.Forms.RadioButton();
            this.rbNormalUnicode = new System.Windows.Forms.RadioButton();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.newTxtFormatRB);
            this.groupBox3.Controls.Add(this.tsvFilesRB);
            this.groupBox3.Controls.Add(this.txtFilesRB);
            this.groupBox3.Location = new System.Drawing.Point(11, 11);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox3.Size = new System.Drawing.Size(182, 87);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Save files in:";
            // 
            // newTxtFormatRB
            // 
            this.newTxtFormatRB.AutoSize = true;
            this.newTxtFormatRB.Location = new System.Drawing.Point(16, 37);
            this.newTxtFormatRB.Name = "newTxtFormatRB";
            this.newTxtFormatRB.Size = new System.Drawing.Size(91, 17);
            this.newTxtFormatRB.TabIndex = 2;
            this.newTxtFormatRB.TabStop = true;
            this.newTxtFormatRB.Text = "txt new format";
            this.newTxtFormatRB.UseVisualStyleBackColor = true;
            this.newTxtFormatRB.CheckedChanged += new System.EventHandler(this.newTxtFormatRB_CheckedChanged);
            // 
            // tsvFilesRB
            // 
            this.tsvFilesRB.AutoSize = true;
            this.tsvFilesRB.Location = new System.Drawing.Point(16, 59);
            this.tsvFilesRB.Margin = new System.Windows.Forms.Padding(2);
            this.tsvFilesRB.Name = "tsvFilesRB";
            this.tsvFilesRB.Size = new System.Drawing.Size(160, 17);
            this.tsvFilesRB.TabIndex = 1;
            this.tsvFilesRB.TabStop = true;
            this.tsvFilesRB.Text = "tsv format (for Google tables)";
            this.tsvFilesRB.UseVisualStyleBackColor = true;
            // 
            // txtFilesRB
            // 
            this.txtFilesRB.AutoSize = true;
            this.txtFilesRB.Location = new System.Drawing.Point(16, 15);
            this.txtFilesRB.Margin = new System.Windows.Forms.Padding(2);
            this.txtFilesRB.Name = "txtFilesRB";
            this.txtFilesRB.Size = new System.Drawing.Size(85, 17);
            this.txtFilesRB.TabIndex = 0;
            this.txtFilesRB.TabStop = true;
            this.txtFilesRB.Text = "txt old format";
            this.txtFilesRB.UseVisualStyleBackColor = true;
            // 
            // clearMessagesCB
            // 
            this.clearMessagesCB.AutoSize = true;
            this.clearMessagesCB.Location = new System.Drawing.Point(216, 71);
            this.clearMessagesCB.Margin = new System.Windows.Forms.Padding(2);
            this.clearMessagesCB.Name = "clearMessagesCB";
            this.clearMessagesCB.Size = new System.Drawing.Size(193, 17);
            this.clearMessagesCB.TabIndex = 21;
            this.clearMessagesCB.Text = "Clear messages in Auto (De)Packer";
            this.clearMessagesCB.UseVisualStyleBackColor = true;
            // 
            // checkBoxSortStrings
            // 
            this.checkBoxSortStrings.AutoSize = true;
            this.checkBoxSortStrings.Location = new System.Drawing.Point(11, 309);
            this.checkBoxSortStrings.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxSortStrings.Name = "checkBoxSortStrings";
            this.checkBoxSortStrings.Size = new System.Drawing.Size(78, 17);
            this.checkBoxSortStrings.TabIndex = 20;
            this.checkBoxSortStrings.Text = "Sort strings";
            this.checkBoxSortStrings.UseVisualStyleBackColor = true;
            // 
            // checkBoxExportRealID
            // 
            this.checkBoxExportRealID.AutoSize = true;
            this.checkBoxExportRealID.Location = new System.Drawing.Point(11, 288);
            this.checkBoxExportRealID.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxExportRealID.Name = "checkBoxExportRealID";
            this.checkBoxExportRealID.Size = new System.Drawing.Size(238, 17);
            this.checkBoxExportRealID.TabIndex = 18;
            this.checkBoxExportRealID.Text = "Use a real ID of text in LANDB and LANGDB";
            this.checkBoxExportRealID.UseVisualStyleBackColor = true;
            // 
            // checkBoxImportingOfNames
            // 
            this.checkBoxImportingOfNames.AutoSize = true;
            this.checkBoxImportingOfNames.Location = new System.Drawing.Point(276, 288);
            this.checkBoxImportingOfNames.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxImportingOfNames.Name = "checkBoxImportingOfNames";
            this.checkBoxImportingOfNames.Size = new System.Drawing.Size(116, 17);
            this.checkBoxImportingOfNames.TabIndex = 19;
            this.checkBoxImportingOfNames.Text = "Import actor names";
            this.checkBoxImportingOfNames.UseVisualStyleBackColor = true;
            // 
            // checkBoxDDS_after_import
            // 
            this.checkBoxDDS_after_import.AutoSize = true;
            this.checkBoxDDS_after_import.Location = new System.Drawing.Point(216, 48);
            this.checkBoxDDS_after_import.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxDDS_after_import.Name = "checkBoxDDS_after_import";
            this.checkBoxDDS_after_import.Size = new System.Drawing.Size(138, 17);
            this.checkBoxDDS_after_import.TabIndex = 16;
            this.checkBoxDDS_after_import.Text = "Delete DDS after import";
            this.checkBoxDDS_after_import.UseVisualStyleBackColor = true;
            // 
            // checkBoxD3DTX_after_import
            // 
            this.checkBoxD3DTX_after_import.AutoSize = true;
            this.checkBoxD3DTX_after_import.Location = new System.Drawing.Point(216, 26);
            this.checkBoxD3DTX_after_import.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxD3DTX_after_import.Name = "checkBoxD3DTX_after_import";
            this.checkBoxD3DTX_after_import.Size = new System.Drawing.Size(151, 17);
            this.checkBoxD3DTX_after_import.TabIndex = 17;
            this.checkBoxD3DTX_after_import.Text = "Delete D3DTX after import";
            this.checkBoxD3DTX_after_import.UseVisualStyleBackColor = true;
            // 
            // textBoxOutputFolder
            // 
            this.textBoxOutputFolder.Location = new System.Drawing.Point(11, 261);
            this.textBoxOutputFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxOutputFolder.Name = "textBoxOutputFolder";
            this.textBoxOutputFolder.ReadOnly = true;
            this.textBoxOutputFolder.Size = new System.Drawing.Size(317, 20);
            this.textBoxOutputFolder.TabIndex = 24;
            // 
            // textBoxInputFolder
            // 
            this.textBoxInputFolder.Location = new System.Drawing.Point(11, 232);
            this.textBoxInputFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxInputFolder.Name = "textBoxInputFolder";
            this.textBoxInputFolder.ReadOnly = true;
            this.textBoxInputFolder.Size = new System.Drawing.Size(317, 20);
            this.textBoxInputFolder.TabIndex = 25;
            // 
            // buttonOutputFolder
            // 
            this.buttonOutputFolder.Location = new System.Drawing.Point(335, 259);
            this.buttonOutputFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonOutputFolder.Name = "buttonOutputFolder";
            this.buttonOutputFolder.Size = new System.Drawing.Size(90, 23);
            this.buttonOutputFolder.TabIndex = 22;
            this.buttonOutputFolder.Text = "Output Folder";
            this.buttonOutputFolder.UseVisualStyleBackColor = true;
            this.buttonOutputFolder.Click += new System.EventHandler(this.buttonOutputFolder_Click);
            // 
            // buttonInputFolder
            // 
            this.buttonInputFolder.Location = new System.Drawing.Point(335, 230);
            this.buttonInputFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonInputFolder.Name = "buttonInputFolder";
            this.buttonInputFolder.Size = new System.Drawing.Size(90, 23);
            this.buttonInputFolder.TabIndex = 23;
            this.buttonInputFolder.Text = "Input Folder";
            this.buttonInputFolder.UseVisualStyleBackColor = true;
            this.buttonInputFolder.Click += new System.EventHandler(this.buttonInputFolder_Click);
            // 
            // okBtn
            // 
            this.okBtn.Location = new System.Drawing.Point(216, 365);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(75, 23);
            this.okBtn.TabIndex = 26;
            this.okBtn.Text = "OK";
            this.okBtn.UseVisualStyleBackColor = true;
            this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Location = new System.Drawing.Point(317, 365);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 23);
            this.cancelBtn.TabIndex = 27;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // checkBoxChangeLangFlags
            // 
            this.checkBoxChangeLangFlags.AutoSize = true;
            this.checkBoxChangeLangFlags.Location = new System.Drawing.Point(276, 327);
            this.checkBoxChangeLangFlags.Name = "checkBoxChangeLangFlags";
            this.checkBoxChangeLangFlags.Size = new System.Drawing.Size(135, 17);
            this.checkBoxChangeLangFlags.TabIndex = 28;
            this.checkBoxChangeLangFlags.Text = "Change language flags";
            this.checkBoxChangeLangFlags.UseVisualStyleBackColor = true;
            // 
            // cbIgnoreEmptyStrings
            // 
            this.cbIgnoreEmptyStrings.AutoSize = true;
            this.cbIgnoreEmptyStrings.Location = new System.Drawing.Point(11, 348);
            this.cbIgnoreEmptyStrings.Name = "cbIgnoreEmptyStrings";
            this.cbIgnoreEmptyStrings.Size = new System.Drawing.Size(120, 17);
            this.cbIgnoreEmptyStrings.TabIndex = 29;
            this.cbIgnoreEmptyStrings.Text = "Ignore empty strings";
            this.cbIgnoreEmptyStrings.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rbTwdNintendoSwitch);
            this.groupBox2.Controls.Add(this.rbNewBttF);
            this.groupBox2.Controls.Add(this.rbNonNormalUnicode2);
            this.groupBox2.Controls.Add(this.rbNormalUnicode);
            this.groupBox2.Location = new System.Drawing.Point(27, 102);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size = new System.Drawing.Size(354, 114);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Coding for new games (From \"Tales From the Borderlands\" game)";
            // 
            // rbNewBttF
            // 
            this.rbNewBttF.AutoSize = true;
            this.rbNewBttF.Location = new System.Drawing.Point(5, 71);
            this.rbNewBttF.Name = "rbNewBttF";
            this.rbNewBttF.Size = new System.Drawing.Size(327, 17);
            this.rbNewBttF.TabIndex = 3;
            this.rbNewBttF.TabStop = true;
            this.rbNewBttF.Text = "ASCII support for Back to the Future Xbox360 and PS4 versions";
            this.rbNewBttF.UseVisualStyleBackColor = true;
            // 
            // 
            // rbTwdNintendoSwitch
            // 
            this.rbTwdNintendoSwitch.AutoSize = true;
            this.rbTwdNintendoSwitch.Location = new System.Drawing.Point(5, 88);
            this.rbTwdNintendoSwitch.Name = "rbTwdNintendoSwitch";
            this.rbTwdNintendoSwitch.Size = new System.Drawing.Size(251, 17);
            this.rbTwdNintendoSwitch.TabIndex = 4;
            this.rbTwdNintendoSwitch.TabStop = true;
            this.rbTwdNintendoSwitch.Text = "Support for The Walking Dead Nintendo Switch";
            this.rbTwdNintendoSwitch.UseVisualStyleBackColor = true;
            // 
            // rbNonNormalUnicode2
            // 
            this.rbNonNormalUnicode2.AutoSize = true;
            this.rbNonNormalUnicode2.Location = new System.Drawing.Point(5, 35);
            this.rbNonNormalUnicode2.Margin = new System.Windows.Forms.Padding(2);
            this.rbNonNormalUnicode2.Name = "rbNonNormalUnicode2";
            this.rbNonNormalUnicode2.Size = new System.Drawing.Size(330, 30);
            this.rbNonNormalUnicode2.TabIndex = 2;
            this.rbNonNormalUnicode2.TabStop = true;
            this.rbNonNormalUnicode2.Text = "NOT normal unicode (convert your language into non-latin chars.\r\nRecommend for ne" +
    "w version TftB.)";
            this.rbNonNormalUnicode2.UseVisualStyleBackColor = true;
            // 
            // rbNormalUnicode
            // 
            this.rbNormalUnicode.AutoSize = true;
            this.rbNormalUnicode.Location = new System.Drawing.Point(5, 17);
            this.rbNormalUnicode.Margin = new System.Windows.Forms.Padding(2);
            this.rbNormalUnicode.Name = "rbNormalUnicode";
            this.rbNormalUnicode.Size = new System.Drawing.Size(101, 17);
            this.rbNormalUnicode.TabIndex = 0;
            this.rbNormalUnicode.TabStop = true;
            this.rbNormalUnicode.Text = "Normal Unicode";
            this.rbNormalUnicode.UseVisualStyleBackColor = true;
            // 
            // AutoDePackerSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 405);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cbIgnoreEmptyStrings);
            this.Controls.Add(this.checkBoxChangeLangFlags);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.okBtn);
            this.Controls.Add(this.textBoxOutputFolder);
            this.Controls.Add(this.textBoxInputFolder);
            this.Controls.Add(this.buttonOutputFolder);
            this.Controls.Add(this.buttonInputFolder);
            this.Controls.Add(this.clearMessagesCB);
            this.Controls.Add(this.checkBoxSortStrings);
            this.Controls.Add(this.checkBoxExportRealID);
            this.Controls.Add(this.checkBoxImportingOfNames);
            this.Controls.Add(this.checkBoxDDS_after_import);
            this.Controls.Add(this.checkBoxD3DTX_after_import);
            this.Controls.Add(this.groupBox3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AutoDePackerSettings";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.AutoDePackerSettings_Load);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton newTxtFormatRB;
        private System.Windows.Forms.RadioButton tsvFilesRB;
        private System.Windows.Forms.RadioButton txtFilesRB;
        private System.Windows.Forms.CheckBox clearMessagesCB;
        private System.Windows.Forms.CheckBox checkBoxSortStrings;
        private System.Windows.Forms.CheckBox checkBoxExportRealID;
        private System.Windows.Forms.CheckBox checkBoxImportingOfNames;
        private System.Windows.Forms.CheckBox checkBoxDDS_after_import;
        private System.Windows.Forms.CheckBox checkBoxD3DTX_after_import;
        private System.Windows.Forms.TextBox textBoxOutputFolder;
        private System.Windows.Forms.TextBox textBoxInputFolder;
        private System.Windows.Forms.Button buttonOutputFolder;
        private System.Windows.Forms.Button buttonInputFolder;
        private System.Windows.Forms.Button okBtn;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.CheckBox checkBoxChangeLangFlags;
        private System.Windows.Forms.CheckBox cbIgnoreEmptyStrings;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbNewBttF;
        private System.Windows.Forms.RadioButton rbTwdNintendoSwitch;
        private System.Windows.Forms.RadioButton rbNonNormalUnicode2;
        private System.Windows.Forms.RadioButton rbNormalUnicode;
    }
}