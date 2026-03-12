namespace TTG_Tools
{
    partial class ModCreator
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.inputFolderLabel = new System.Windows.Forms.Label();
            this.inputFolderTextBox = new System.Windows.Forms.TextBox();
            this.browseInputButton = new System.Windows.Forms.Button();
            this.modNameLabel = new System.Windows.Forms.Label();
            this.modNameTextBox = new System.Windows.Forms.TextBox();
            this.gameLabel = new System.Windows.Forms.Label();
            this.gameComboBox = new System.Windows.Forms.ComboBox();
            this.createModButton = new System.Windows.Forms.Button();
            this.logListBox = new System.Windows.Forms.ListBox();
            this.outputFolderLabel = new System.Windows.Forms.Label();
            this.outputFolderTextBox = new System.Windows.Forms.TextBox();
            this.browseOutputButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // inputFolderLabel
            // 
            this.inputFolderLabel.AutoSize = true;
            this.inputFolderLabel.Location = new System.Drawing.Point(12, 15);
            this.inputFolderLabel.Name = "inputFolderLabel";
            this.inputFolderLabel.Size = new System.Drawing.Size(65, 13);
            this.inputFolderLabel.TabIndex = 0;
            this.inputFolderLabel.Text = "Input folder:";
            // 
            // inputFolderTextBox
            // 
            this.inputFolderTextBox.Location = new System.Drawing.Point(103, 12);
            this.inputFolderTextBox.Name = "inputFolderTextBox";
            this.inputFolderTextBox.Size = new System.Drawing.Size(407, 20);
            this.inputFolderTextBox.TabIndex = 1;
            // 
            // browseInputButton
            // 
            this.browseInputButton.Location = new System.Drawing.Point(516, 10);
            this.browseInputButton.Name = "browseInputButton";
            this.browseInputButton.Size = new System.Drawing.Size(75, 23);
            this.browseInputButton.TabIndex = 2;
            this.browseInputButton.Text = "Browse...";
            this.browseInputButton.UseVisualStyleBackColor = true;
            this.browseInputButton.Click += new System.EventHandler(this.browseInputButton_Click);
            // 
            // modNameLabel
            // 
            this.modNameLabel.AutoSize = true;
            this.modNameLabel.Location = new System.Drawing.Point(22, 73);
            this.modNameLabel.Name = "modNameLabel";
            this.modNameLabel.Size = new System.Drawing.Size(58, 13);
            this.modNameLabel.TabIndex = 3;
            this.modNameLabel.Text = "Mod name:";
            // 
            // modNameTextBox
            // 
            this.modNameTextBox.Location = new System.Drawing.Point(103, 70);
            this.modNameTextBox.Name = "modNameTextBox";
            this.modNameTextBox.Size = new System.Drawing.Size(196, 20);
            this.modNameTextBox.TabIndex = 4;
            // 
            // gameLabel
            // 
            this.gameLabel.AutoSize = true;
            this.gameLabel.Location = new System.Drawing.Point(43, 102);
            this.gameLabel.Name = "gameLabel";
            this.gameLabel.Size = new System.Drawing.Size(38, 13);
            this.gameLabel.TabIndex = 5;
            this.gameLabel.Text = "Game:";
            // 
            // gameComboBox
            // 
            this.gameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gameComboBox.FormattingEnabled = true;
            this.gameComboBox.Location = new System.Drawing.Point(103, 99);
            this.gameComboBox.Name = "gameComboBox";
            this.gameComboBox.Size = new System.Drawing.Size(407, 21);
            this.gameComboBox.TabIndex = 6;
            // 
            // createModButton
            // 
            this.createModButton.Location = new System.Drawing.Point(516, 97);
            this.createModButton.Name = "createModButton";
            this.createModButton.Size = new System.Drawing.Size(75, 23);
            this.createModButton.TabIndex = 7;
            this.createModButton.Text = "Create";
            this.createModButton.UseVisualStyleBackColor = true;
            this.createModButton.Click += new System.EventHandler(this.createModButton_Click);
            // 
            // logListBox
            // 
            this.logListBox.FormattingEnabled = true;
            this.logListBox.Location = new System.Drawing.Point(15, 136);
            this.logListBox.Name = "logListBox";
            this.logListBox.Size = new System.Drawing.Size(576, 147);
            this.logListBox.TabIndex = 8;
            // 
            // outputFolderLabel
            // 
            this.outputFolderLabel.AutoSize = true;
            this.outputFolderLabel.Location = new System.Drawing.Point(3, 44);
            this.outputFolderLabel.Name = "outputFolderLabel";
            this.outputFolderLabel.Size = new System.Drawing.Size(74, 13);
            this.outputFolderLabel.TabIndex = 9;
            this.outputFolderLabel.Text = "Output folder:";
            // 
            // outputFolderTextBox
            // 
            this.outputFolderTextBox.Location = new System.Drawing.Point(103, 41);
            this.outputFolderTextBox.Name = "outputFolderTextBox";
            this.outputFolderTextBox.Size = new System.Drawing.Size(407, 20);
            this.outputFolderTextBox.TabIndex = 10;
            // 
            // browseOutputButton
            // 
            this.browseOutputButton.Location = new System.Drawing.Point(516, 39);
            this.browseOutputButton.Name = "browseOutputButton";
            this.browseOutputButton.Size = new System.Drawing.Size(75, 23);
            this.browseOutputButton.TabIndex = 11;
            this.browseOutputButton.Text = "Browse...";
            this.browseOutputButton.UseVisualStyleBackColor = true;
            this.browseOutputButton.Click += new System.EventHandler(this.browseOutputButton_Click);
            // 
            // ModCreator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(606, 299);
            this.Controls.Add(this.browseOutputButton);
            this.Controls.Add(this.outputFolderTextBox);
            this.Controls.Add(this.outputFolderLabel);
            this.Controls.Add(this.logListBox);
            this.Controls.Add(this.createModButton);
            this.Controls.Add(this.gameComboBox);
            this.Controls.Add(this.gameLabel);
            this.Controls.Add(this.modNameTextBox);
            this.Controls.Add(this.modNameLabel);
            this.Controls.Add(this.browseInputButton);
            this.Controls.Add(this.inputFolderTextBox);
            this.Controls.Add(this.inputFolderLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ModCreator";
            this.Text = "Mod Creator";
            this.Load += new System.EventHandler(this.ModCreator_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label inputFolderLabel;
        private System.Windows.Forms.TextBox inputFolderTextBox;
        private System.Windows.Forms.Button browseInputButton;
        private System.Windows.Forms.Label modNameLabel;
        private System.Windows.Forms.TextBox modNameTextBox;
        private System.Windows.Forms.Label gameLabel;
        private System.Windows.Forms.ComboBox gameComboBox;
        private System.Windows.Forms.Button createModButton;
        private System.Windows.Forms.ListBox logListBox;
        private System.Windows.Forms.Label outputFolderLabel;
        private System.Windows.Forms.TextBox outputFolderTextBox;
        private System.Windows.Forms.Button browseOutputButton;
    }
}
