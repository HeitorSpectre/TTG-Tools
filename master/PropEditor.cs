using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace TTG_Tools
{
    public class PropEditor : Form
    {
        private readonly DataGridView entriesGrid;
        private readonly Button openBtn;
        private readonly Button saveBtn;
        private readonly Button saveAsBtn;
        private readonly Label statusLbl;
        private readonly Label gameLbl;
        private readonly ComboBox gameSelector;

        private string currentPropPath;

        private static readonly Dictionary<string, string> SupportedGames = new Dictionary<string, string>
        {
            { "Fables", "The Wolf Among Us" },
            { "WD2", "The Walking Dead: Season 2" },
            { "Borderlands", "Tales from the Borderlands" },
            { "GameOfThrones", "Game of Thrones" },
            { "MCSM", "Minecraft: Story Mode - Season One" },
            { "WDM", "The Walking Dead: Michonne" },
            { "BAT", "Batman: The Telltale Series" },
            { "WD3", "The Walking Dead: A New Frontier" },
            { "GoG", "Marvel's Guardians of the Galaxy" },
            { "MC2", "Minecraft: Story Mode - Season Two" },
            { "BAT2", "Batman: The Enemy Within" },
            { "WD4", "The Walking Dead: The Final Season" },
            { "WDC", "The Walking Dead: The Definitive Series" },
            { "SM1", "Sam & Max Save the World Remastered" }
        };

        public PropEditor()
        {
            Text = "Prop Editor";
            Width = 980;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;

            openBtn = new Button { Text = "Open .prop", Left = 12, Top = 12, Width = 120 };
            saveBtn = new Button { Text = "Save", Left = 140, Top = 12, Width = 120, Enabled = false };
            saveAsBtn = new Button { Text = "Save As", Left = 268, Top = 12, Width = 120, Enabled = false };

            gameLbl = new Label
            {
                Left = 402,
                Top = 16,
                Width = 560,
                Text = "Compatible games (same scope as Inspector Prop Editor):"
            };

            gameSelector = new ComboBox
            {
                Left = 402,
                Top = 38,
                Width = 560,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            statusLbl = new Label { Left = 12, Top = 50, Width = 380, Height = 22, Text = "Ready." };

            entriesGrid = new DataGridView
            {
                Left = 12,
                Top = 78,
                Width = 950,
                Height = 510,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntryIndex",
                HeaderText = "#",
                FillWeight = 10,
                ReadOnly = true
            });

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntryText",
                HeaderText = "Text",
                FillWeight = 90
            });

            openBtn.Click += OpenBtn_Click;
            saveBtn.Click += SaveBtn_Click;
            saveAsBtn.Click += SaveAsBtn_Click;

            Controls.Add(openBtn);
            Controls.Add(saveBtn);
            Controls.Add(saveAsBtn);
            Controls.Add(gameLbl);
            Controls.Add(gameSelector);
            Controls.Add(statusLbl);
            Controls.Add(entriesGrid);

            foreach (KeyValuePair<string, string> game in SupportedGames)
            {
                gameSelector.Items.Add(string.Format("{0} ({1})", game.Value, game.Key));
            }

            if (gameSelector.Items.Count > 0)
            {
                gameSelector.SelectedIndex = 0;
            }
        }

        private void OpenBtn_Click(object sender, EventArgs e)
        {
            if (gameSelector.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a game first.", "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "PROP files (*.prop)|*.prop|All files (*.*)|*.*",
                FilterIndex = 1
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            currentPropPath = ofd.FileName;
            LoadProp(currentPropPath);
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPropPath))
            {
                return;
            }

            SaveProp(currentPropPath);
        }

        private void SaveAsBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPropPath))
            {
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PROP files (*.prop)|*.prop|All files (*.*)|*.*",
                FileName = Path.GetFileName(currentPropPath)
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            SaveProp(sfd.FileName);
        }

        private void LoadProp(string propPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "TTGTools_PropEditor", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                ForThreads worker = new ForThreads();
                worker.ReportForWork += _ => { };

                string txtName = Path.GetFileNameWithoutExtension(propPath) + ".txt";
                worker.ExportPROP(new FileInfo(propPath), txtName, tempDir);

                string txtPath = Path.Combine(tempDir, txtName);
                if (!File.Exists(txtPath))
                {
                    throw new InvalidDataException("This PROP file could not be exported by the current editor pipeline.");
                }

                string[] lines = File.ReadAllLines(txtPath);
                entriesGrid.Rows.Clear();

                for (int i = 0; i < lines.Length; i += 2)
                {
                    string indexText = lines[i].Trim();
                    string value = (i + 1 < lines.Length) ? lines[i + 1] : string.Empty;
                    entriesGrid.Rows.Add(indexText.TrimEnd(')'), value);
                }

                saveBtn.Enabled = true;
                saveAsBtn.Enabled = true;
                statusLbl.Text = "Loaded: " + propPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open PROP file: " + ex.Message, "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLbl.Text = "Open failed.";
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch
                {
                }
            }
        }

        private void SaveProp(string savePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "TTGTools_PropEditor", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string txtPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(currentPropPath) + ".txt");
                List<string> exportLines = new List<string>();

                for (int i = 0; i < entriesGrid.Rows.Count; i++)
                {
                    object cellValue = entriesGrid.Rows[i].Cells[1].Value;
                    string textValue = cellValue != null ? cellValue.ToString() : string.Empty;
                    exportLines.Add((i + 1).ToString() + ")");
                    exportLines.Add(textValue);
                }

                File.WriteAllLines(txtPath, exportLines.ToArray());

                ForThreads worker = new ForThreads();
                worker.ReportForWork += _ => { };

                List<string> failed = new List<string>();
                string targetFolder = Path.GetDirectoryName(savePath);
                if (string.IsNullOrEmpty(targetFolder))
                {
                    throw new InvalidOperationException("Invalid target path.");
                }

                worker.ImportTXTinPROP(new FileInfo(currentPropPath), new FileInfo(txtPath), targetFolder, failed);

                if (failed.Count > 0)
                {
                    throw new InvalidOperationException("Import failed for: " + string.Join(", ", failed.ToArray()));
                }

                string generatedPath = Path.Combine(targetFolder, Path.GetFileName(currentPropPath));
                if (!File.Exists(generatedPath))
                {
                    throw new FileNotFoundException("The editor could not generate the output PROP file.", generatedPath);
                }

                if (!string.Equals(generatedPath, savePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }

                    File.Copy(generatedPath, savePath);
                }

                statusLbl.Text = "Saved: " + savePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save PROP file: " + ex.Message, "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLbl.Text = "Save failed.";
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
