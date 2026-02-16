namespace TTG_Tools
{
    partial class PropEditor
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

        private void InitializeComponent()
        {
            this.mainTabs = new System.Windows.Forms.TabControl();
            this.propEditorTab = new System.Windows.Forms.TabPage();
            this.statusLbl = new System.Windows.Forms.Label();
            this.collapseAllBtn = new System.Windows.Forms.Button();
            this.expandAllBtn = new System.Windows.Forms.Button();
            this.openPropBtn = new System.Windows.Forms.Button();
            this.propPathTB = new System.Windows.Forms.TextBox();
            this.propTreeView = new System.Windows.Forms.TreeView();
            this.mainTabs.SuspendLayout();
            this.propEditorTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabs
            // 
            this.mainTabs.Controls.Add(this.propEditorTab);
            this.mainTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabs.Location = new System.Drawing.Point(0, 0);
            this.mainTabs.Name = "mainTabs";
            this.mainTabs.SelectedIndex = 0;
            this.mainTabs.Size = new System.Drawing.Size(968, 579);
            this.mainTabs.TabIndex = 0;
            // 
            // propEditorTab
            // 
            this.propEditorTab.Controls.Add(this.statusLbl);
            this.propEditorTab.Controls.Add(this.collapseAllBtn);
            this.propEditorTab.Controls.Add(this.expandAllBtn);
            this.propEditorTab.Controls.Add(this.openPropBtn);
            this.propEditorTab.Controls.Add(this.propPathTB);
            this.propEditorTab.Controls.Add(this.propTreeView);
            this.propEditorTab.Location = new System.Drawing.Point(4, 22);
            this.propEditorTab.Name = "propEditorTab";
            this.propEditorTab.Padding = new System.Windows.Forms.Padding(3);
            this.propEditorTab.Size = new System.Drawing.Size(960, 553);
            this.propEditorTab.TabIndex = 0;
            this.propEditorTab.Text = "Prop Editor";
            this.propEditorTab.UseVisualStyleBackColor = true;
            // 
            // statusLbl
            // 
            this.statusLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLbl.Location = new System.Drawing.Point(8, 527);
            this.statusLbl.Name = "statusLbl";
            this.statusLbl.Size = new System.Drawing.Size(946, 20);
            this.statusLbl.TabIndex = 5;
            this.statusLbl.Text = "Open a .prop file to inspect all data.";
            // 
            // collapseAllBtn
            // 
            this.collapseAllBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.collapseAllBtn.Location = new System.Drawing.Point(877, 7);
            this.collapseAllBtn.Name = "collapseAllBtn";
            this.collapseAllBtn.Size = new System.Drawing.Size(77, 23);
            this.collapseAllBtn.TabIndex = 4;
            this.collapseAllBtn.Text = "Collapse";
            this.collapseAllBtn.UseVisualStyleBackColor = true;
            this.collapseAllBtn.Click += new System.EventHandler(this.collapseAllBtn_Click);
            // 
            // expandAllBtn
            // 
            this.expandAllBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.expandAllBtn.Location = new System.Drawing.Point(794, 7);
            this.expandAllBtn.Name = "expandAllBtn";
            this.expandAllBtn.Size = new System.Drawing.Size(77, 23);
            this.expandAllBtn.TabIndex = 3;
            this.expandAllBtn.Text = "Expand";
            this.expandAllBtn.UseVisualStyleBackColor = true;
            this.expandAllBtn.Click += new System.EventHandler(this.expandAllBtn_Click);
            // 
            // openPropBtn
            // 
            this.openPropBtn.Location = new System.Drawing.Point(8, 7);
            this.openPropBtn.Name = "openPropBtn";
            this.openPropBtn.Size = new System.Drawing.Size(81, 23);
            this.openPropBtn.TabIndex = 2;
            this.openPropBtn.Text = "Open .prop";
            this.openPropBtn.UseVisualStyleBackColor = true;
            this.openPropBtn.Click += new System.EventHandler(this.openPropBtn_Click);
            // 
            // propPathTB
            // 
            this.propPathTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propPathTB.Location = new System.Drawing.Point(95, 9);
            this.propPathTB.Name = "propPathTB";
            this.propPathTB.ReadOnly = true;
            this.propPathTB.Size = new System.Drawing.Size(693, 20);
            this.propPathTB.TabIndex = 1;
            // 
            // propTreeView
            // 
            this.propTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propTreeView.Location = new System.Drawing.Point(8, 36);
            this.propTreeView.Name = "propTreeView";
            this.propTreeView.Size = new System.Drawing.Size(946, 488);
            this.propTreeView.TabIndex = 0;
            // 
            // PropEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(968, 579);
            this.Controls.Add(this.mainTabs);
            this.Name = "PropEditor";
            this.Text = "TTG Tools - Prop Editor";
            this.mainTabs.ResumeLayout(false);
            this.propEditorTab.ResumeLayout(false);
            this.propEditorTab.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TabControl mainTabs;
        private System.Windows.Forms.TabPage propEditorTab;
        private System.Windows.Forms.TreeView propTreeView;
        private System.Windows.Forms.TextBox propPathTB;
        private System.Windows.Forms.Button openPropBtn;
        private System.Windows.Forms.Button expandAllBtn;
        private System.Windows.Forms.Button collapseAllBtn;
        private System.Windows.Forms.Label statusLbl;
    }
}
