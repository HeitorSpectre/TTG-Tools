using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TTG_Tools
{
    public class PropEditor : Form
    {
        private class PropTextEntry
        {
            public string Label;
            public string Value;
            public string DisplayValue;
            public int FlatOrder;
        }

        private readonly Button openBtn;
        private readonly Button saveBtn;
        private readonly Button saveAsBtn;
        private readonly Button applyTextBtn;
        private readonly Label statusLbl;
        private readonly Label gameLbl;
        private readonly ComboBox gameSelector;
        private readonly TreeView propTree;
        private readonly TextBox valueEditor;
        private readonly SplitContainer splitContainer;

        private readonly List<PropTextEntry> flatEntries = new List<PropTextEntry>();

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
            Width = 1080;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            openBtn = new Button { Text = "Open .prop", Left = 12, Top = 12, Width = 120 };
            saveBtn = new Button { Text = "Save", Left = 140, Top = 12, Width = 120, Enabled = false };
            saveAsBtn = new Button { Text = "Save As", Left = 268, Top = 12, Width = 120, Enabled = false };

            gameLbl = new Label
            {
                Left = 402,
                Top = 16,
                Width = 660,
                Text = "Compatible games (same scope as Inspector Prop Editor):"
            };

            gameSelector = new ComboBox
            {
                Left = 402,
                Top = 38,
                Width = 660,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            statusLbl = new Label { Left = 12, Top = 50, Width = 380, Height = 22, Text = "Ready." };

            splitContainer = new SplitContainer
            {
                Left = 12,
                Top = 78,
                Width = 1050,
                Height = 570,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Orientation = Orientation.Vertical,
                SplitterDistance = 480
            };

            propTree = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };

            valueEditor = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Enabled = false
            };

            applyTextBtn = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Text = "Apply Text",
                Enabled = false
            };

            Panel rightPanel = new Panel { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(valueEditor);
            rightPanel.Controls.Add(applyTextBtn);

            splitContainer.Panel1.Controls.Add(propTree);
            splitContainer.Panel2.Controls.Add(rightPanel);

            openBtn.Click += OpenBtn_Click;
            saveBtn.Click += SaveBtn_Click;
            saveAsBtn.Click += SaveAsBtn_Click;
            propTree.AfterSelect += PropTree_AfterSelect;
            applyTextBtn.Click += ApplyTextBtn_Click;

            Controls.Add(openBtn);
            Controls.Add(saveBtn);
            Controls.Add(saveAsBtn);
            Controls.Add(gameLbl);
            Controls.Add(gameSelector);
            Controls.Add(statusLbl);
            Controls.Add(splitContainer);

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
            try
            {
                propTree.Nodes.Clear();
                flatEntries.Clear();
                valueEditor.Text = string.Empty;
                valueEditor.Enabled = false;
                applyTextBtn.Enabled = false;

                ParsePropIntoTree(propPath);

                saveBtn.Enabled = flatEntries.Count > 0;
                saveAsBtn.Enabled = flatEntries.Count > 0;
                statusLbl.Text = "Loaded: " + propPath + " | Entries: " + flatEntries.Count;

                if (propTree.Nodes.Count > 0)
                {
                    propTree.ExpandAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open PROP file: " + ex.Message, "Prop Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLbl.Text = "Open failed.";
            }
        }

        private void ParsePropIntoTree(string propPath)
        {
            using (FileStream fs = new FileStream(propPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                byte[] headerBytes = br.ReadBytes(4);
                if (headerBytes.Length != 4)
                {
                    throw new InvalidDataException("Invalid PROP header.");
                }

                string header = Encoding.ASCII.GetString(headerBytes);
                if ((header == "5VSM") || (header == "6VSM"))
                {
                    br.ReadInt32();
                    br.ReadInt64();
                }

                int countHeaders = br.ReadInt32();
                for (int i = 0; i < countHeaders; i++)
                {
                    br.ReadBytes(8);
                    br.ReadBytes(4);
                }

                br.ReadInt32();
                br.ReadInt32();

                if (header != "6VSM")
                {
                    int blSize1 = br.ReadInt32();
                    if (blSize1 < 4)
                    {
                        throw new InvalidDataException("Unsupported PROP block layout.");
                    }

                    br.ReadBytes(blSize1 - 4);
                }

                br.ReadInt32();
                br.ReadInt32();
                if (header == "6VSM")
                {
                    br.ReadInt32();
                }

                byte[] bValue = br.ReadBytes(8);
                if (bValue.Length != 8)
                {
                    throw new InvalidDataException("Unexpected end of PROP file.");
                }

                string signature = BitConverter.ToString(bValue);
                Encoding valueEncoding = header == "6VSM" ? Encoding.UTF8 : Encoding.GetEncoding(MainMenu.settings.ASCII_N);

                TreeNode root = new TreeNode(Path.GetFileName(propPath));

                if (header == "ERTM")
                {
                    br.ReadInt32();
                }

                int countBlocks = br.ReadInt32();

                if (signature == "25-03-C6-1F-D8-64-1B-4F")
                {
                    for (int i = 0; i < countBlocks; i++)
                    {
                        TreeNode blockNode = new TreeNode("Block " + (i + 1));

                        br.ReadBytes(8);
                        if (header == "ERTM") br.ReadInt32();
                        int countSubBlocks = br.ReadInt32();

                        for (int j = 0; j < countSubBlocks * 2; j++)
                        {
                            int len = br.ReadInt32();
                            byte[] raw = br.ReadBytes(len);
                            string text = valueEncoding.GetString(raw);

                            PropTextEntry entry = new PropTextEntry
                            {
                                Label = "Entry " + (j + 1),
                                Value = text,
                                DisplayValue = BuildDisplayValue(raw, text),
                                FlatOrder = flatEntries.Count
                            };
                            flatEntries.Add(entry);

                            TreeNode leaf = new TreeNode(entry.Label) { Tag = entry };
                            blockNode.Nodes.Add(leaf);
                        }

                        root.Nodes.Add(blockNode);
                    }
                }
                else
                {
                    ParseFallbackBlocks(br, header, countBlocks, valueEncoding, root, signature);
                }

                propTree.Nodes.Add(root);
            }
        }

        private void ParseFallbackBlocks(BinaryReader br, string header, int countBlocks, Encoding valueEncoding, TreeNode root, string signature)
        {
            long startPos = br.BaseStream.Position;
            bool parsedAsSubBlocks = TryParseFallbackAsSubBlocks(br, header, countBlocks, valueEncoding, root);

            if (parsedAsSubBlocks)
            {
                return;
            }

            br.BaseStream.Seek(startPos, SeekOrigin.Begin);

            for (int i = 0; i < countBlocks; i++)
            {
                TreeNode blockNode = new TreeNode("Block " + (i + 1) + " [" + signature + "]");

                byte[] symbol = br.ReadBytes(8);
                if (symbol.Length != 8)
                {
                    throw new InvalidDataException("Unexpected end of PROP file while reading fallback block symbol.");
                }

                if (header == "ERTM") br.ReadInt32();

                int len = br.ReadInt32();
                if (len < 0)
                {
                    throw new InvalidDataException("Negative string length encountered while parsing fallback PROP layout.");
                }

                byte[] raw = br.ReadBytes(len);
                if (raw.Length != len)
                {
                    throw new InvalidDataException("Unexpected end of PROP file in fallback parser.");
                }

                string text = valueEncoding.GetString(raw);

                PropTextEntry entry = new PropTextEntry
                {
                    Label = "Value",
                    Value = text,
                    DisplayValue = BuildDisplayValue(raw, text),
                    FlatOrder = flatEntries.Count
                };
                flatEntries.Add(entry);

                TreeNode symNode = new TreeNode("Symbol<" + ToHexLower(symbol) + ">") { Tag = entry };
                AddExtractedStringNodes(symNode, raw);
                blockNode.Nodes.Add(symNode);
                root.Nodes.Add(blockNode);
            }
        }

        private bool TryParseFallbackAsSubBlocks(BinaryReader br, string header, int countBlocks, Encoding valueEncoding, TreeNode root)
        {
            long parseStart = br.BaseStream.Position;
            List<TreeNode> pendingNodes = new List<TreeNode>();
            List<PropTextEntry> pendingEntries = new List<PropTextEntry>();

            try
            {
                for (int i = 0; i < countBlocks; i++)
                {
                    TreeNode blockNode = new TreeNode("Block " + (i + 1));

                    byte[] symbol = br.ReadBytes(8);
                    if (symbol.Length != 8) return false;
                    if (header == "ERTM") br.ReadInt32();

                    int countSubBlocks = br.ReadInt32();
                    if (countSubBlocks < 0 || countSubBlocks > 100000)
                    {
                        return false;
                    }

                    TreeNode symTree = new TreeNode("Symbol<" + ToHexLower(symbol) + "> [x" + countSubBlocks + "]");

                    List<PropTextEntry> localEntries = new List<PropTextEntry>();
                    for (int j = 0; j < countSubBlocks * 2; j++)
                    {
                        if (br.BaseStream.Position + 4 > br.BaseStream.Length)
                        {
                            return false;
                        }

                        int len = br.ReadInt32();
                        if (len < 0 || br.BaseStream.Position + len > br.BaseStream.Length)
                        {
                            return false;
                        }

                        byte[] raw = br.ReadBytes(len);
                        string text = valueEncoding.GetString(raw);

                        PropTextEntry entry = new PropTextEntry
                        {
                            Label = "Entry " + (j + 1),
                            Value = text,
                            DisplayValue = BuildDisplayValue(raw, text),
                            FlatOrder = flatEntries.Count + pendingEntries.Count + localEntries.Count
                        };

                        localEntries.Add(entry);
                    }

                    for (int p = 0; p < localEntries.Count; p += 2)
                    {
                        PropTextEntry keyEntry = localEntries[p];
                        TreeNode pairNode = new TreeNode("Property " + ((p / 2) + 1));
                        pairNode.Nodes.Add(new TreeNode("Key: " + ClipNodeText(keyEntry.DisplayValue)) { Tag = keyEntry });

                        if (p + 1 < localEntries.Count)
                        {
                            PropTextEntry valueEntry = localEntries[p + 1];
                            pairNode.Nodes.Add(new TreeNode("Value: " + ClipNodeText(valueEntry.DisplayValue)) { Tag = valueEntry });
                        }

                        symTree.Nodes.Add(pairNode);
                    }

                    pendingEntries.AddRange(localEntries);

                    blockNode.Nodes.Add(symTree);
                    pendingNodes.Add(blockNode);
                }

                // Heuristic: if this interpretation consumed a meaningful portion of data, accept it.
                if (pendingEntries.Count == 0)
                {
                    return false;
                }

                long consumed = br.BaseStream.Position - parseStart;
                if (consumed < 32)
                {
                    return false;
                }

                flatEntries.AddRange(pendingEntries);
                foreach (TreeNode node in pendingNodes)
                {
                    root.Nodes.Add(node);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ToHexLower(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            return hex;
        }


        private static string BuildDisplayValue(byte[] raw, string decoded)
        {
            if (!string.IsNullOrEmpty(decoded) && ContainsVisibleChars(decoded))
            {
                return decoded;
            }

            if (raw == null || raw.Length == 0)
            {
                return "<empty>";
            }

            return "0x" + BitConverter.ToString(raw).Replace("-", string.Empty);
        }

        private static bool ContainsVisibleChars(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsControl(c) && !char.IsWhiteSpace(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ClipNodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "<empty>";
            }

            string oneLine = text.Replace("\r", " ").Replace("\n", " ");
            if (oneLine.Length > 64)
            {
                return oneLine.Substring(0, 64) + "...";
            }

            return oneLine;
        }

        private static void AddExtractedStringNodes(TreeNode parent, byte[] raw)
        {
            List<string> strings = ExtractPrintableAsciiStrings(raw, 4);
            if (strings.Count == 0)
            {
                return;
            }

            TreeNode container = new TreeNode("Extracted Strings");
            int added = 0;
            foreach (string item in strings)
            {
                container.Nodes.Add(new TreeNode("String: " + ClipNodeText(item)));
                added++;
                if (added >= 32)
                {
                    container.Nodes.Add(new TreeNode("..."));
                    break;
                }
            }

            parent.Nodes.Add(container);
        }

        private static List<string> ExtractPrintableAsciiStrings(byte[] raw, int minLen)
        {
            List<string> result = new List<string>();
            if (raw == null || raw.Length == 0)
            {
                return result;
            }

            StringBuilder current = new StringBuilder();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < raw.Length; i++)
            {
                byte b = raw[i];
                bool printable = b >= 0x20 && b <= 0x7E;

                if (printable)
                {
                    current.Append((char)b);
                }
                else
                {
                    if (current.Length >= minLen)
                    {
                        string s = current.ToString();
                        if (!seen.Contains(s))
                        {
                            seen.Add(s);
                            result.Add(s);
                        }
                    }

                    current.Clear();
                }
            }

            if (current.Length >= minLen)
            {
                string s = current.ToString();
                if (!seen.Contains(s))
                {
                    seen.Add(s);
                    result.Add(s);
                }
            }

            return result;
        }

        private void PropTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            PropTextEntry entry = e.Node.Tag as PropTextEntry;
            if (entry == null)
            {
                valueEditor.Text = string.Empty;
                valueEditor.Enabled = false;
                applyTextBtn.Enabled = false;
                return;
            }

            valueEditor.Enabled = true;
            applyTextBtn.Enabled = true;
            valueEditor.Text = string.IsNullOrEmpty(entry.Value) ? entry.DisplayValue : entry.Value;
        }

        private void ApplyTextBtn_Click(object sender, EventArgs e)
        {
            TreeNode selected = propTree.SelectedNode;
            if (selected == null)
            {
                return;
            }

            PropTextEntry entry = selected.Tag as PropTextEntry;
            if (entry == null)
            {
                return;
            }

            entry.Value = valueEditor.Text;
            entry.DisplayValue = valueEditor.Text;
            statusLbl.Text = "Text updated (not saved yet).";
        }

        private void SaveProp(string savePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "TTGTools_PropEditor", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string txtPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(currentPropPath) + ".txt");
                List<string> exportLines = new List<string>();

                for (int i = 0; i < flatEntries.Count; i++)
                {
                    exportLines.Add((i + 1).ToString() + ")");
                    exportLines.Add(flatEntries[i].Value ?? string.Empty);
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
