namespace TTG_Tools
{
    partial class TextEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextEditor));
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.firstFilePath = new System.Windows.Forms.TextBox();
            this.readyFilePath = new System.Windows.Forms.TextBox();
            this.firstFileBtn = new System.Windows.Forms.Button();
            this.readyFileBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.severalFilesRB = new System.Windows.Forms.RadioButton();
            this.singleFileRB = new System.Windows.Forms.RadioButton();
            this.tabPagesControl = new System.Windows.Forms.TabControl();
            this.mergeTextTabPage = new System.Windows.Forms.TabPage();
            this.sortStrsCB = new System.Windows.Forms.CheckBox();
            this.mergeBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.secondFilePath = new System.Windows.Forms.TextBox();
            this.secondFileBtn = new System.Windows.Forms.Button();
            this.replaceTextTabPage = new System.Windows.Forms.TabPage();
            this.sortDoubledCB = new System.Windows.Forms.CheckBox();
            this.replaceDuplicatedStringsBtn = new System.Windows.Forms.Button();
            this.readyDoubledFileBtn = new System.Windows.Forms.Button();
            this.secondDoubledFileBtn = new System.Windows.Forms.Button();
            this.firstDoubledFileBtn = new System.Windows.Forms.Button();
            this.readyDoubledFilePath = new System.Windows.Forms.TextBox();
            this.secondDoubledFilePath = new System.Windows.Forms.TextBox();
            this.firstDoubledFilePath = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.readyBrowseBtn = new System.Windows.Forms.Button();
            this.readyPath = new System.Windows.Forms.TextBox();
            this.checkNonTranslatedStrsBtn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.firstBrowseBtn = new System.Windows.Forms.Button();
            this.checkDuplicatedStrsBtn = new System.Windows.Forms.Button();
            this.firstPath = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.txtNewMethodRB = new System.Windows.Forms.RadioButton();
            this.txtOldMethodRB = new System.Windows.Forms.RadioButton();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.compareBtn = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.tabPagesControl.SuspendLayout();
            this.mergeTextTabPage.SuspendLayout();
            this.replaceTextTabPage.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(31, 244);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(709, 23);
            this.progressBar1.TabIndex = 8;
            // 
            // firstFilePath
            // 
            this.firstFilePath.Location = new System.Drawing.Point(78, 22);
            this.firstFilePath.Name = "firstFilePath";
            this.firstFilePath.Size = new System.Drawing.Size(447, 20);
            this.firstFilePath.TabIndex = 9;
            // 
            // readyFilePath
            // 
            this.readyFilePath.Location = new System.Drawing.Point(78, 96);
            this.readyFilePath.Name = "readyFilePath";
            this.readyFilePath.Size = new System.Drawing.Size(447, 20);
            this.readyFilePath.TabIndex = 10;
            // 
            // firstFileBtn
            // 
            this.firstFileBtn.Location = new System.Drawing.Point(537, 20);
            this.firstFileBtn.Name = "firstFileBtn";
            this.firstFileBtn.Size = new System.Drawing.Size(33, 23);
            this.firstFileBtn.TabIndex = 11;
            this.firstFileBtn.Text = "...";
            this.firstFileBtn.UseVisualStyleBackColor = true;
            this.firstFileBtn.Click += new System.EventHandler(this.firstFileBtn_Click);
            // 
            // readyFileBtn
            // 
            this.readyFileBtn.Location = new System.Drawing.Point(537, 94);
            this.readyFileBtn.Name = "readyFileBtn";
            this.readyFileBtn.Size = new System.Drawing.Size(33, 23);
            this.readyFileBtn.TabIndex = 12;
            this.readyFileBtn.Text = "...";
            this.readyFileBtn.UseVisualStyleBackColor = true;
            this.readyFileBtn.Click += new System.EventHandler(this.readyFileBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "First file:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 99);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Ready file:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.severalFilesRB);
            this.groupBox1.Controls.Add(this.singleFileRB);
            this.groupBox1.Location = new System.Drawing.Point(618, 35);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(122, 77);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Merge";
            // 
            // severalFilesRB
            // 
            this.severalFilesRB.AutoSize = true;
            this.severalFilesRB.Location = new System.Drawing.Point(18, 44);
            this.severalFilesRB.Name = "severalFilesRB";
            this.severalFilesRB.Size = new System.Drawing.Size(82, 17);
            this.severalFilesRB.TabIndex = 1;
            this.severalFilesRB.TabStop = true;
            this.severalFilesRB.Text = "Several files";
            this.severalFilesRB.UseVisualStyleBackColor = true;
            // 
            // singleFileRB
            // 
            this.singleFileRB.AutoSize = true;
            this.singleFileRB.Location = new System.Drawing.Point(18, 19);
            this.singleFileRB.Name = "singleFileRB";
            this.singleFileRB.Size = new System.Drawing.Size(70, 17);
            this.singleFileRB.TabIndex = 0;
            this.singleFileRB.TabStop = true;
            this.singleFileRB.Text = "Single file";
            this.singleFileRB.UseVisualStyleBackColor = true;
            this.singleFileRB.CheckedChanged += new System.EventHandler(this.mergeSingleRB_CheckedChanged);
            // 
            // tabPagesControl
            // 
            this.tabPagesControl.Controls.Add(this.mergeTextTabPage);
            this.tabPagesControl.Controls.Add(this.replaceTextTabPage);
            this.tabPagesControl.Controls.Add(this.tabPage1);
            this.tabPagesControl.Location = new System.Drawing.Point(15, 13);
            this.tabPagesControl.Name = "tabPagesControl";
            this.tabPagesControl.SelectedIndex = 0;
            this.tabPagesControl.Size = new System.Drawing.Size(597, 185);
            this.tabPagesControl.TabIndex = 16;
            this.tabPagesControl.SelectedIndexChanged += new System.EventHandler(this.tabPagesControl_SelectedIndexChanged);
            // 
            // mergeTextTabPage
            // 
            this.mergeTextTabPage.BackColor = System.Drawing.Color.Transparent;
            this.mergeTextTabPage.Controls.Add(this.sortStrsCB);
            this.mergeTextTabPage.Controls.Add(this.mergeBtn);
            this.mergeTextTabPage.Controls.Add(this.label3);
            this.mergeTextTabPage.Controls.Add(this.secondFilePath);
            this.mergeTextTabPage.Controls.Add(this.secondFileBtn);
            this.mergeTextTabPage.Controls.Add(this.firstFileBtn);
            this.mergeTextTabPage.Controls.Add(this.firstFilePath);
            this.mergeTextTabPage.Controls.Add(this.label2);
            this.mergeTextTabPage.Controls.Add(this.readyFilePath);
            this.mergeTextTabPage.Controls.Add(this.label1);
            this.mergeTextTabPage.Controls.Add(this.readyFileBtn);
            this.mergeTextTabPage.Location = new System.Drawing.Point(4, 22);
            this.mergeTextTabPage.Name = "mergeTextTabPage";
            this.mergeTextTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.mergeTextTabPage.Size = new System.Drawing.Size(589, 159);
            this.mergeTextTabPage.TabIndex = 0;
            this.mergeTextTabPage.Text = "Merge texts";
            // 
            // sortStrsCB
            // 
            this.sortStrsCB.AutoSize = true;
            this.sortStrsCB.Location = new System.Drawing.Point(318, 130);
            this.sortStrsCB.Name = "sortStrsCB";
            this.sortStrsCB.Size = new System.Drawing.Size(130, 17);
            this.sortStrsCB.TabIndex = 21;
            this.sortStrsCB.Text = "Sort duplicated strings";
            this.sortStrsCB.UseVisualStyleBackColor = true;
            // 
            // mergeBtn
            // 
            this.mergeBtn.Location = new System.Drawing.Point(462, 126);
            this.mergeBtn.Name = "mergeBtn";
            this.mergeBtn.Size = new System.Drawing.Size(88, 24);
            this.mergeBtn.TabIndex = 19;
            this.mergeBtn.Text = "Merge files";
            this.mergeBtn.UseVisualStyleBackColor = true;
            this.mergeBtn.Click += new System.EventHandler(this.mergeBtn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Second file:";
            // 
            // secondFilePath
            // 
            this.secondFilePath.Location = new System.Drawing.Point(78, 59);
            this.secondFilePath.Name = "secondFilePath";
            this.secondFilePath.Size = new System.Drawing.Size(447, 20);
            this.secondFilePath.TabIndex = 16;
            // 
            // secondFileBtn
            // 
            this.secondFileBtn.Location = new System.Drawing.Point(537, 57);
            this.secondFileBtn.Name = "secondFileBtn";
            this.secondFileBtn.Size = new System.Drawing.Size(33, 23);
            this.secondFileBtn.TabIndex = 17;
            this.secondFileBtn.Text = "...";
            this.secondFileBtn.UseVisualStyleBackColor = true;
            this.secondFileBtn.Click += new System.EventHandler(this.secondFileBtn_Click);
            // 
            // replaceTextTabPage
            // 
            this.replaceTextTabPage.BackColor = System.Drawing.Color.Transparent;
            this.replaceTextTabPage.Controls.Add(this.sortDoubledCB);
            this.replaceTextTabPage.Controls.Add(this.replaceDuplicatedStringsBtn);
            this.replaceTextTabPage.Controls.Add(this.readyDoubledFileBtn);
            this.replaceTextTabPage.Controls.Add(this.secondDoubledFileBtn);
            this.replaceTextTabPage.Controls.Add(this.firstDoubledFileBtn);
            this.replaceTextTabPage.Controls.Add(this.readyDoubledFilePath);
            this.replaceTextTabPage.Controls.Add(this.secondDoubledFilePath);
            this.replaceTextTabPage.Controls.Add(this.firstDoubledFilePath);
            this.replaceTextTabPage.Controls.Add(this.label6);
            this.replaceTextTabPage.Controls.Add(this.label5);
            this.replaceTextTabPage.Controls.Add(this.label4);
            this.replaceTextTabPage.Location = new System.Drawing.Point(4, 22);
            this.replaceTextTabPage.Name = "replaceTextTabPage";
            this.replaceTextTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.replaceTextTabPage.Size = new System.Drawing.Size(589, 159);
            this.replaceTextTabPage.TabIndex = 1;
            this.replaceTextTabPage.Text = "Replace texts";
            // 
            // sortDoubledCB
            // 
            this.sortDoubledCB.AutoSize = true;
            this.sortDoubledCB.Location = new System.Drawing.Point(180, 128);
            this.sortDoubledCB.Name = "sortDoubledCB";
            this.sortDoubledCB.Size = new System.Drawing.Size(130, 17);
            this.sortDoubledCB.TabIndex = 13;
            this.sortDoubledCB.Text = "Sort duplicated strings";
            this.sortDoubledCB.UseVisualStyleBackColor = true;
            // 
            // replaceDuplicatedStringsBtn
            // 
            this.replaceDuplicatedStringsBtn.Location = new System.Drawing.Point(326, 123);
            this.replaceDuplicatedStringsBtn.Name = "replaceDuplicatedStringsBtn";
            this.replaceDuplicatedStringsBtn.Size = new System.Drawing.Size(234, 24);
            this.replaceDuplicatedStringsBtn.TabIndex = 10;
            this.replaceDuplicatedStringsBtn.Text = "Replace strings from first duplicated file";
            this.replaceDuplicatedStringsBtn.UseVisualStyleBackColor = true;
            this.replaceDuplicatedStringsBtn.Click += new System.EventHandler(this.replaceDuplicatedStringsBtn_Click);
            // 
            // readyDoubledFileBtn
            // 
            this.readyDoubledFileBtn.Location = new System.Drawing.Point(548, 94);
            this.readyDoubledFileBtn.Name = "readyDoubledFileBtn";
            this.readyDoubledFileBtn.Size = new System.Drawing.Size(32, 23);
            this.readyDoubledFileBtn.TabIndex = 8;
            this.readyDoubledFileBtn.Text = "...";
            this.readyDoubledFileBtn.UseVisualStyleBackColor = true;
            this.readyDoubledFileBtn.Click += new System.EventHandler(this.readyDoubledFileBtn_Click);
            // 
            // secondDoubledFileBtn
            // 
            this.secondDoubledFileBtn.Location = new System.Drawing.Point(548, 57);
            this.secondDoubledFileBtn.Name = "secondDoubledFileBtn";
            this.secondDoubledFileBtn.Size = new System.Drawing.Size(32, 23);
            this.secondDoubledFileBtn.TabIndex = 7;
            this.secondDoubledFileBtn.Text = "...";
            this.secondDoubledFileBtn.UseVisualStyleBackColor = true;
            this.secondDoubledFileBtn.Click += new System.EventHandler(this.secondDoubledFileBtn_Click);
            // 
            // firstDoubledFileBtn
            // 
            this.firstDoubledFileBtn.Location = new System.Drawing.Point(548, 20);
            this.firstDoubledFileBtn.Name = "firstDoubledFileBtn";
            this.firstDoubledFileBtn.Size = new System.Drawing.Size(32, 23);
            this.firstDoubledFileBtn.TabIndex = 6;
            this.firstDoubledFileBtn.Text = "...";
            this.firstDoubledFileBtn.UseVisualStyleBackColor = true;
            this.firstDoubledFileBtn.Click += new System.EventHandler(this.firstDoubledFileBtn_Click);
            // 
            // readyDoubledFilePath
            // 
            this.readyDoubledFilePath.Location = new System.Drawing.Point(122, 96);
            this.readyDoubledFilePath.Name = "readyDoubledFilePath";
            this.readyDoubledFilePath.Size = new System.Drawing.Size(411, 20);
            this.readyDoubledFilePath.TabIndex = 5;
            // 
            // secondDoubledFilePath
            // 
            this.secondDoubledFilePath.Location = new System.Drawing.Point(122, 59);
            this.secondDoubledFilePath.Name = "secondDoubledFilePath";
            this.secondDoubledFilePath.Size = new System.Drawing.Size(411, 20);
            this.secondDoubledFilePath.TabIndex = 4;
            // 
            // firstDoubledFilePath
            // 
            this.firstDoubledFilePath.Location = new System.Drawing.Point(122, 22);
            this.firstDoubledFilePath.Name = "firstDoubledFilePath";
            this.firstDoubledFilePath.Size = new System.Drawing.Size(411, 20);
            this.firstDoubledFilePath.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 99);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(98, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Ready doubled file:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 62);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Second doubled file:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "First doubled file:";
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.Transparent;
            this.tabPage1.Controls.Add(this.compareBtn);
            this.tabPage1.Controls.Add(this.readyBrowseBtn);
            this.tabPage1.Controls.Add(this.readyPath);
            this.tabPage1.Controls.Add(this.checkNonTranslatedStrsBtn);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.firstBrowseBtn);
            this.tabPage1.Controls.Add(this.checkDuplicatedStrsBtn);
            this.tabPage1.Controls.Add(this.firstPath);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(589, 159);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Check non-translated strings";
            // 
            // readyBrowseBtn
            // 
            this.readyBrowseBtn.Location = new System.Drawing.Point(478, 61);
            this.readyBrowseBtn.Name = "readyBrowseBtn";
            this.readyBrowseBtn.Size = new System.Drawing.Size(26, 24);
            this.readyBrowseBtn.TabIndex = 8;
            this.readyBrowseBtn.Text = "...";
            this.readyBrowseBtn.UseVisualStyleBackColor = true;
            this.readyBrowseBtn.Click += new System.EventHandler(this.readyBrowseBtn_Click);
            // 
            // readyPath
            // 
            this.readyPath.Location = new System.Drawing.Point(78, 64);
            this.readyPath.Name = "readyPath";
            this.readyPath.Size = new System.Drawing.Size(385, 20);
            this.readyPath.TabIndex = 7;
            // 
            // checkNonTranslatedStrsBtn
            // 
            this.checkNonTranslatedStrsBtn.Location = new System.Drawing.Point(330, 100);
            this.checkNonTranslatedStrsBtn.Name = "checkNonTranslatedStrsBtn";
            this.checkNonTranslatedStrsBtn.Size = new System.Drawing.Size(220, 24);
            this.checkNonTranslatedStrsBtn.TabIndex = 23;
            this.checkNonTranslatedStrsBtn.Text = "Check non-translated strings in file";
            this.checkNonTranslatedStrsBtn.UseVisualStyleBackColor = true;
            this.checkNonTranslatedStrsBtn.Click += new System.EventHandler(this.button4_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 48);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(152, 13);
            this.label9.TabIndex = 6;
            this.label9.Text = "Non-translated strings file path:";
            // 
            // firstBrowseBtn
            // 
            this.firstBrowseBtn.Location = new System.Drawing.Point(478, 11);
            this.firstBrowseBtn.Name = "firstBrowseBtn";
            this.firstBrowseBtn.Size = new System.Drawing.Size(26, 24);
            this.firstBrowseBtn.TabIndex = 4;
            this.firstBrowseBtn.Text = "...";
            this.firstBrowseBtn.UseVisualStyleBackColor = true;
            this.firstBrowseBtn.Click += new System.EventHandler(this.firstBrowseBtn_Click);
            // 
            // checkDuplicatedStrsBtn
            // 
            this.checkDuplicatedStrsBtn.Location = new System.Drawing.Point(30, 100);
            this.checkDuplicatedStrsBtn.Name = "checkDuplicatedStrsBtn";
            this.checkDuplicatedStrsBtn.Size = new System.Drawing.Size(256, 24);
            this.checkDuplicatedStrsBtn.TabIndex = 20;
            this.checkDuplicatedStrsBtn.Text = "Check on duplicated original strings in file";
            this.checkDuplicatedStrsBtn.UseVisualStyleBackColor = true;
            this.checkDuplicatedStrsBtn.Click += new System.EventHandler(this.checkDuplicatedStrsBtn_Click);
            // 
            // firstPath
            // 
            this.firstPath.Location = new System.Drawing.Point(78, 12);
            this.firstPath.Name = "firstPath";
            this.firstPath.Size = new System.Drawing.Size(385, 20);
            this.firstPath.TabIndex = 2;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(27, 15);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Check file:";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.txtNewMethodRB);
            this.groupBox4.Controls.Add(this.txtOldMethodRB);
            this.groupBox4.Location = new System.Drawing.Point(618, 129);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(122, 69);
            this.groupBox4.TabIndex = 24;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Save txt file with";
            // 
            // txtNewMethodRB
            // 
            this.txtNewMethodRB.AutoSize = true;
            this.txtNewMethodRB.Location = new System.Drawing.Point(18, 44);
            this.txtNewMethodRB.Name = "txtNewMethodRB";
            this.txtNewMethodRB.Size = new System.Drawing.Size(85, 17);
            this.txtNewMethodRB.TabIndex = 1;
            this.txtNewMethodRB.TabStop = true;
            this.txtNewMethodRB.Text = "New method";
            this.txtNewMethodRB.UseVisualStyleBackColor = true;
            // 
            // txtOldMethodRB
            // 
            this.txtOldMethodRB.AutoSize = true;
            this.txtOldMethodRB.Location = new System.Drawing.Point(18, 20);
            this.txtOldMethodRB.Name = "txtOldMethodRB";
            this.txtOldMethodRB.Size = new System.Drawing.Size(79, 17);
            this.txtOldMethodRB.TabIndex = 0;
            this.txtOldMethodRB.TabStop = true;
            this.txtOldMethodRB.Text = "Old method";
            this.txtOldMethodRB.UseVisualStyleBackColor = true;
            // 
            // progressBar2
            // 
            this.progressBar2.Location = new System.Drawing.Point(31, 282);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(709, 23);
            this.progressBar2.TabIndex = 17;
            // 
            // compareBtn
            // 
            this.compareBtn.Location = new System.Drawing.Point(30, 130);
            this.compareBtn.Name = "compareBtn";
            this.compareBtn.Size = new System.Drawing.Size(520, 23);
            this.compareBtn.TabIndex = 24;
            this.compareBtn.Text = "Check 2 files after \"check non-translated strings in file\" button";
            this.compareBtn.UseVisualStyleBackColor = true;
            this.compareBtn.Click += new System.EventHandler(this.compareBtn_Click);
            // 
            // TextEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(766, 320);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.tabPagesControl);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "TextEditor";
            this.Text = "Text editor";
            this.Load += new System.EventHandler(this.TextEditor_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPagesControl.ResumeLayout(false);
            this.mergeTextTabPage.ResumeLayout(false);
            this.mergeTextTabPage.PerformLayout();
            this.replaceTextTabPage.ResumeLayout(false);
            this.replaceTextTabPage.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox firstFilePath;
        private System.Windows.Forms.TextBox readyFilePath;
        private System.Windows.Forms.Button firstFileBtn;
        private System.Windows.Forms.Button readyFileBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton severalFilesRB;
        private System.Windows.Forms.RadioButton singleFileRB;
        private System.Windows.Forms.TabControl tabPagesControl;
        private System.Windows.Forms.TabPage mergeTextTabPage;
        private System.Windows.Forms.TabPage replaceTextTabPage;
        private System.Windows.Forms.Button checkDuplicatedStrsBtn;
        private System.Windows.Forms.Button mergeBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox secondFilePath;
        private System.Windows.Forms.Button secondFileBtn;
        private System.Windows.Forms.TextBox readyDoubledFilePath;
        private System.Windows.Forms.TextBox secondDoubledFilePath;
        private System.Windows.Forms.TextBox firstDoubledFilePath;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox sortStrsCB;
        private System.Windows.Forms.Button replaceDuplicatedStringsBtn;
        private System.Windows.Forms.Button readyDoubledFileBtn;
        private System.Windows.Forms.Button secondDoubledFileBtn;
        private System.Windows.Forms.Button firstDoubledFileBtn;
        private System.Windows.Forms.CheckBox sortDoubledCB;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.Button checkNonTranslatedStrsBtn;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton txtNewMethodRB;
        private System.Windows.Forms.RadioButton txtOldMethodRB;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button readyBrowseBtn;
        private System.Windows.Forms.TextBox readyPath;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button firstBrowseBtn;
        private System.Windows.Forms.TextBox firstPath;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button compareBtn;
    }
}