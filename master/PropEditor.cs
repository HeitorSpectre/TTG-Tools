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

        public PropEditor()
        {
            Text = "Prop Editor";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            openBtn = new Button { Text = "Abrir .prop", Left = 12, Top = 12, Width = 120 };
            saveBtn = new Button { Text = "Salvar", Left = 140, Top = 12, Width = 120, Enabled = false };
            saveAsBtn = new Button { Text = "Salvar como", Left = 268, Top = 12, Width = 120, Enabled = false };
            gameLbl = new Label { Left = 400, Top = 17, Width = 470, AutoEllipsis = true };
            gameSelector = new ComboBox { Left = 400, Top = 40, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList };
            statusLbl = new Label { Left = 12, Top = 48, Width = 380, Height = 22 };

            entriesGrid = new DataGridView
            {
                Left = 12,
                Top = 76,
                Width = 860,
                Height = 470,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntryIndex",
                HeaderText = "#",
                FillWeight = 12,
                ReadOnly = true
            });
            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntryText",
                HeaderText = "Texto",
                FillWeight = 88
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

            gameLbl.Text = "Jogos suportados (Telltale Inspector / TelltaleToolLib):";
            foreach (var game in SupportedGames)
            {
                gameSelector.Items.Add(string.Format("{0} ({1})", game.Value, game.Key));
            }

            if (gameSelector.Items.Count > 0)
            {
                gameSelector.SelectedIndex = 0;
            }
        }



        private static readonly Dictionary<string, string> SupportedGames = new Dictionary<string, string>
        {
            { "texasholdem", "Telltale Texas Hold'em" },
            { "boneville", "Bone: Out from Boneville" },
            { "csi3dimensions", "CSI: 3 Dimensions of Murder" },
            { "cowrace", "Bone: The Great Cow Race" },
            { "sammax101", "Sam and Max: S1E1" },
            { "sammax102", "Sam and Max: S1E2" },
            { "sammax103", "Sam and Max: S1E3" },
            { "sammax104", "Sam and Max: S1E4" },
            { "sammax105", "Sam and Max: S1E5" },
            { "sammax106", "Sam and Max: S1E6" },
            { "csihard", "CSI: Hard Evidence" },
            { "sammax201", "Sam and Max: S2E1" },
            { "sammax202", "Sam and Max: S2E2" },
            { "sammax203", "Sam and Max: S2E3" },
            { "sammax204", "Sam and Max: S2E4" },
            { "sammax205", "Sam and Max: S2E5" },
            { "sbcg4ap101", "Strong Bad CG4AP S1E1" },
            { "sbcg4ap102", "Strong Bad CG4AP S1E2" },
            { "sbcg4ap103", "Strong Bad CG4AP S1E3" },
            { "sbcg4ap104", "Strong Bad CG4AP S1E4" },
            { "sbcg4ap105", "Strong Bad CG4AP S1E5" },
            { "wag101", "Wallace And Gromit: S1E1" },
            { "wag102", "Wallace And Gromit: S1E2" },
            { "wag103", "Wallace And Gromit: S1E3" },
            { "wag104", "Wallace And Gromit: S1E4" },
            { "monkeyisland101", "Tales of Monkey Island S1E1" },
            { "monkeyisland102", "Tales of Monkey Island S1E2" },
            { "monkeyisland103", "Tales of Monkey Island S1E3" },
            { "monkeyisland104", "Tales of Monkey Island S1E4" },
            { "monkeyisland105", "Tales of Monkey Island S1E5" },
            { "csideadly", "CSI: Deadly Intent" },
            { "hector101", "Hector: Badge of Carnage E1" },
            { "hector102", "Hector: Badge of Carnage E2" },
            { "hector103", "Hector: Badge of Carnage E3" },
            { "sammax301", "Sam and Max: S3E1" },
            { "sammax302", "Sam and Max: S3E2" },
            { "sammax303", "Sam and Max: S3E3" },
            { "sammax304", "Sam and Max: S3E4" },
            { "sammax305", "Sam and Max: S3E5" },
            { "grickle101", "Puzzle Agent 1" },
            { "csifatal", "CSI: Fatal Conspiracy" },
            { "celebritypoker", "Poker Night 1" },
            { "bttf101", "Back to the Future S1E1" },
            { "bttf102", "Back to the Future S1E2" },
            { "bttf103", "Back to the Future S1E3" },
            { "bttf104", "Back to the Future S1E4" },
            { "bttf105", "Back to the Future S1E5" },
            { "grickle102", "Puzzle Agent 2" },
            { "jurassicpark", "Jurassic Park" },
            { "lawandorder", "Law and Order" },
            { "TWD1", "The Walking Dead: Season 1" },
            { "celebritypoker2", "Poker Night 2" },
            { "Fables", "The Wolf Among Us S1" },
            { "WD2", "The Walking Dead: Season 2" },
            { "Borderlands", "Tales from the Borderlands" },
            { "GameOfThrones", "Game of Thrones" },
            { "MCSM", "Minecraft Story Mode: Season 1" },
            { "WDM", "The Walking Dead: Michonne" },
            { "BAT", "Batman: Season 1" },
            { "WD3", "The Walking Dead: Season 3" },
            { "GoG", "Marvel's Guardians of the Galaxy" },
            { "MC2", "Minecraft Story Mode: Season 2" },
            { "BAT2", "Batman: Season 2" },
            { "WD4", "The Walking Dead: Season 4" },
            { "WDC", "The Walking Dead: Definitive Series" },
            { "SM1", "Sam and Max: Remastered" }
        };
        private void OpenBtn_Click(object sender, EventArgs e)
        {
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
                string txtName = Path.GetFileNameWithoutExtension(propPath) + ".txt";
                worker.ExportPROP(new FileInfo(propPath), txtName, tempDir);

                string txtPath = Path.Combine(tempDir, txtName);
                if (!File.Exists(txtPath))
                {
                    throw new InvalidDataException("Não foi possível exportar o PROP para edição.");
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
                statusLbl.Text = "Arquivo carregado: " + propPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir PROP: " + ex.Message, "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                List<string> failed = new List<string>();
                string targetFolder = Path.GetDirectoryName(savePath);

                worker.ImportTXTinPROP(new FileInfo(currentPropPath), new FileInfo(txtPath), targetFolder, failed);

                string generatedPath = Path.Combine(targetFolder, Path.GetFileName(currentPropPath));
                if (!string.Equals(generatedPath, savePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }

                    File.Copy(generatedPath, savePath);
                }

                statusLbl.Text = "Arquivo salvo: " + savePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar PROP: " + ex.Message, "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
