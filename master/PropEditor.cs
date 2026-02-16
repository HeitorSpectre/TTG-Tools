using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TTG_Tools.Texts;

namespace TTG_Tools
{
    public class PropEditor : Form
    {
        private readonly BindingList<PropEditorEntry> entries = new BindingList<PropEditorEntry>();
        private PropEditorDocument document;

        private TextBox filePathTextBox;
        private DataGridView grid;
        private TextBox findTextBox;
        private TextBox replaceTextBox;
        private Label metaLabel;

        public PropEditor()
        {
            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = "PROP Editor";
            Width = 920;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;

            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 72 };
            Controls.Add(topPanel);

            Button openButton = new Button { Text = "Open .prop", Left = 10, Top = 10, Width = 100 };
            openButton.Click += OpenButton_Click;
            topPanel.Controls.Add(openButton);

            Button saveButton = new Button { Text = "Save As", Left = 120, Top = 10, Width = 100 };
            saveButton.Click += SaveButton_Click;
            topPanel.Controls.Add(saveButton);

            Button reloadButton = new Button { Text = "Reload", Left = 230, Top = 10, Width = 100 };
            reloadButton.Click += ReloadButton_Click;
            topPanel.Controls.Add(reloadButton);

            filePathTextBox = new TextBox { Left = 10, Top = 41, Width = 560, ReadOnly = true };
            topPanel.Controls.Add(filePathTextBox);

            metaLabel = new Label { Left = 580, Top = 44, Width = 320, Text = "No file loaded" };
            topPanel.Controls.Add(metaLabel);

            Panel findPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            Controls.Add(findPanel);

            findPanel.Controls.Add(new Label { Left = 10, Top = 12, Width = 36, Text = "Find:" });
            findTextBox = new TextBox { Left = 50, Top = 9, Width = 210 };
            findPanel.Controls.Add(findTextBox);

            findPanel.Controls.Add(new Label { Left = 270, Top = 12, Width = 55, Text = "Replace:" });
            replaceTextBox = new TextBox { Left = 330, Top = 9, Width = 210 };
            findPanel.Controls.Add(replaceTextBox);

            Button replaceAllButton = new Button { Left = 550, Top = 8, Width = 110, Text = "Replace All" };
            replaceAllButton.Click += ReplaceAllButton_Click;
            findPanel.Controls.Add(replaceAllButton);

            Button addLineButton = new Button { Left = 670, Top = 8, Width = 110, Text = "Add Empty" };
            addLineButton.Click += AddLineButton_Click;
            findPanel.Controls.Add(addLineButton);

            Button removeLineButton = new Button { Left = 790, Top = 8, Width = 110, Text = "Remove Row" };
            removeLineButton.Click += RemoveLineButton_Click;
            findPanel.Controls.Add(removeLineButton);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                DataSource = entries
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Index",
                HeaderText = "#",
                Width = 60,
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Value",
                HeaderText = "Text",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            Controls.Add(grid);
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PROP files (*.prop)|*.prop|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            LoadProp(ofd.FileName);
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            if (document == null || string.IsNullOrEmpty(document.FilePath)) return;
            LoadProp(document.FilePath);
        }

        private void LoadProp(string filePath)
        {
            try
            {
                document = PropEditorWorker.Load(filePath);
                entries.Clear();
                foreach (PropEditorEntry entry in document.Entries)
                {
                    entries.Add(new PropEditorEntry { Index = entry.Index, Value = entry.Value });
                }

                filePathTextBox.Text = document.FilePath;
                metaLabel.Text = string.Format("{0} | marker {1} | {2} entries", document.Header, document.Marker, document.Entries.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load PROP: " + ex.Message, "PROP Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (document == null)
            {
                MessageBox.Show("Open a .prop file first.", "PROP Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PROP files (*.prop)|*.prop|All files (*.*)|*.*";
            sfd.FileName = Path.GetFileName(document.FilePath);

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                PropEditorWorker.Save(document, entries.Select(x => x.Value), sfd.FileName);
                MessageBox.Show("Saved successfully.", "PROP Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save PROP: " + ex.Message, "PROP Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReplaceAllButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(findTextBox.Text)) return;

            int replacements = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                string original = entries[i].Value ?? string.Empty;
                string updated = original.Replace(findTextBox.Text, replaceTextBox.Text ?? string.Empty);
                if (updated != original)
                {
                    entries[i].Value = updated;
                    replacements++;
                }
            }

            grid.Refresh();
            MessageBox.Show(string.Format("Replace finished. {0} row(s) changed.", replacements), "PROP Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddLineButton_Click(object sender, EventArgs e)
        {
            int nextIndex = entries.Count == 0 ? 1 : entries.Max(x => x.Index) + 1;
            entries.Add(new PropEditorEntry { Index = nextIndex, Value = string.Empty });
        }

        private void RemoveLineButton_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null) return;

            int selectedIndex = grid.CurrentRow.Index;
            if (selectedIndex < 0 || selectedIndex >= entries.Count) return;

            entries.RemoveAt(selectedIndex);

            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Index = i + 1;
            }

            grid.Refresh();
        }
    }
}
