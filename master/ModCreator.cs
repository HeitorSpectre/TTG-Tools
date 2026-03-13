using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class ModCreator : Form
    {
        private readonly CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
        private readonly PokerNightRemasterProfile pokerNightProfile = new PokerNightRemasterProfile();
        private readonly SamAndMaxSaveWorldRemasterProfile samAndMaxSaveWorldProfile = new SamAndMaxSaveWorldRemasterProfile();

        public ModCreator()
        {
            InitializeComponent();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;

            AllowDrop = true;
            DragEnter += InputFolder_DragEnter;
            DragDrop += InputFolder_DragDrop;
            inputFolderTextBox.AllowDrop = true;
            inputFolderTextBox.DragEnter += InputFolder_DragEnter;
            inputFolderTextBox.DragDrop += InputFolder_DragDrop;
        }

        private void ModCreator_Load(object sender, EventArgs e)
        {
            gameComboBox.Items.Clear();
            gameComboBox.Items.Add(pokerNightProfile.GameDisplayName);
            gameComboBox.Items.Add(samAndMaxSaveWorldProfile.GameDisplayName);
            gameComboBox.SelectedIndex = 0;
            gameComboBox.Enabled = true;

            UpdateLayoutOptions();

            if (string.IsNullOrWhiteSpace(outputFolderTextBox.Text) && Directory.Exists(inputFolderTextBox.Text))
            {
                outputFolderTextBox.Text = Path.Combine(inputFolderTextBox.Text, "ModCreator_Output");
            }

            SetProgress(0);
        }

        private void browseInputButton_Click(object sender, EventArgs e)
        {
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                inputFolderTextBox.Text = folderDialog.FileName;

                if (string.IsNullOrWhiteSpace(outputFolderTextBox.Text))
                {
                    outputFolderTextBox.Text = Path.Combine(folderDialog.FileName, "ModCreator_Output");
                }
            }
        }


        private void browseOutputButton_Click(object sender, EventArgs e)
        {
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                outputFolderTextBox.Text = folderDialog.FileName;
            }
        }

        private async void createModButton_Click(object sender, EventArgs e)
        {
            IModCreatorProfile selectedProfile = GetSelectedProfile();
            if (selectedProfile == null)
            {
                MessageBox.Show("Please select a supported game.", "Error");
                return;
            }

            ModLayoutOption selectedLayoutOption = GetSelectedLayoutOption(selectedProfile);
            if (selectedLayoutOption == null)
            {
                MessageBox.Show("Please select a valid mod layout.", "Error");
                return;
            }

            string inputFolder = inputFolderTextBox.Text.Trim();
            string outputFolder = outputFolderTextBox.Text.Trim();
            string modName = NormalizeModName(modNameTextBox.Text.Trim());

            if (!Directory.Exists(inputFolder))
            {
                MessageBox.Show("Input folder doesn't exist.", "Error");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            if (string.IsNullOrWhiteSpace(modName))
            {
                MessageBox.Show("Please provide a valid mod name.", "Error");
                return;
            }

            string archiveFileName = selectedProfile.BuildArchiveFileName(modName, selectedLayoutOption);
            string archivePath = Path.Combine(outputFolder, archiveFileName);
            string luaPath = Path.Combine(outputFolder, selectedProfile.BuildLuaFileName(modName, selectedLayoutOption));

            SetUiEnabled(false);
            SetProgress(0);
            logListBox.Items.Clear();
            AddLog("Starting mod creation for " + selectedProfile.GameDisplayName + "...");

            try
            {
                await Task.Run(() => CreateModPackage(inputFolder, outputFolder, archivePath, luaPath, modName, archiveFileName, selectedProfile, selectedLayoutOption));
                SetProgress(100);
                AddLog("Mod created successfully.");
                MessageBox.Show("Mod created successfully.", "Success");
            }
            catch (Exception ex)
            {
                AddLog("Error: " + ex.Message);
                MessageBox.Show("Failed to create mod. Check logs for details.", "Error");
            }
            finally
            {
                if (createProgressBar.Value < 100)
                {
                    SetProgress(0);
                }

                SetUiEnabled(true);
            }
        }

        private void CreateModPackage(
            string inputFolder,
            string outputFolder,
            string archivePath,
            string luaPath,
            string modName,
            string archiveFileName,
            IModCreatorProfile profile,
            ModLayoutOption layoutOption)
        {
            byte[] gameKey = GetEncryptionKeyForGame(profile.GameDisplayName);

            AddLog("Output folder: " + outputFolder);
            AddLog("Creating archive: " + Path.GetFileName(archivePath));
            SetProgress(5);

            ttarch2BuilderLegacy1132(
                inputFolder,
                archivePath,
                profile.CompressArchive,
                profile.EncryptArchive,
                profile.EncryptLuaInsideArchive,
                gameKey,
                profile.Ttarch2Version,
                profile.NewEngineLua,
                SetProgress,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Path.GetFullPath(archivePath),
                    Path.GetFullPath(luaPath)
                });

            AddLog("Generating Lua descriptor: " + Path.GetFileName(luaPath));
            SetProgress(90);
            string luaContent = profile.BuildLuaDescriptor(modName, archiveFileName, layoutOption);

            File.WriteAllText(luaPath, luaContent, new UTF8Encoding(false));

            AddLog("Encrypting Lua descriptor in-place (Lua Scripts for New Engine / method 7-9)...");
            SetProgress(95);
            byte[] encryptedLua = Methods.encryptLua(File.ReadAllBytes(luaPath), gameKey, profile.NewEngineLua, 7);
            File.WriteAllBytes(luaPath, encryptedLua);
            SetProgress(100);
        }

        private void SetProgress(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgress), value);
                return;
            }

            int normalized = Math.Max(createProgressBar.Minimum, Math.Min(createProgressBar.Maximum, value));
            createProgressBar.Value = normalized;
        }

        private IModCreatorProfile GetSelectedProfile()
        {
            string selectedGame = gameComboBox.SelectedItem as string;

            if (string.Equals(selectedGame, pokerNightProfile.GameDisplayName, StringComparison.Ordinal))
            {
                return pokerNightProfile;
            }

            if (string.Equals(selectedGame, samAndMaxSaveWorldProfile.GameDisplayName, StringComparison.Ordinal))
            {
                return samAndMaxSaveWorldProfile;
            }

            return null;
        }

        private ModLayoutOption GetSelectedLayoutOption(IModCreatorProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            if (!profile.RequiresLayoutSelection)
            {
                return profile.GetLayoutOptions().FirstOrDefault();
            }

            return modLayoutComboBox.SelectedItem as ModLayoutOption;
        }

        private void UpdateLayoutOptions()
        {
            IModCreatorProfile selectedProfile = GetSelectedProfile();

            modLayoutComboBox.Items.Clear();
            if (selectedProfile == null)
            {
                modLayoutComboBox.Enabled = false;
                modLayoutLabel.Enabled = false;
                return;
            }

            List<ModLayoutOption> options = selectedProfile.GetLayoutOptions();
            for (int i = 0; i < options.Count; i++)
            {
                modLayoutComboBox.Items.Add(options[i]);
            }

            bool requiresLayout = selectedProfile.RequiresLayoutSelection;
            modLayoutComboBox.Enabled = requiresLayout;
            modLayoutLabel.Enabled = requiresLayout;

            if (modLayoutComboBox.Items.Count > 0)
            {
                modLayoutComboBox.SelectedIndex = 0;
            }
        }

        private static string NormalizeModName(string modName)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(modName.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        }

        private static byte[] GetEncryptionKeyForGame(string gameName)
        {
            var gameKey = MainMenu.gamelist.FirstOrDefault(g => g.gamename == gameName);
            if (gameKey == null || gameKey.key == null)
            {
                throw new InvalidOperationException("Could not find encryption key for selected game.");
            }

            return gameKey.key;
        }

        private void AddLog(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddLog), text);
                return;
            }

            logListBox.Items.Add(text);
            logListBox.SelectedIndex = logListBox.Items.Count - 1;
            logListBox.SelectedIndex = -1;
        }

        private void SetUiEnabled(bool enabled)
        {
            inputFolderTextBox.Enabled = enabled;
            browseInputButton.Enabled = enabled;
            modNameTextBox.Enabled = enabled;
            outputFolderTextBox.Enabled = enabled;
            browseOutputButton.Enabled = enabled;
            createModButton.Enabled = enabled;
            gameComboBox.Enabled = enabled;
            modLayoutComboBox.Enabled = enabled && GetSelectedProfile() != null && GetSelectedProfile().RequiresLayoutSelection;
            modLayoutLabel.Enabled = enabled && GetSelectedProfile() != null && GetSelectedProfile().RequiresLayoutSelection;
        }

        private void gameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLayoutOptions();
        }


        private void InputFolder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0 && Directory.Exists(paths[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }

            e.Effect = DragDropEffects.None;
        }

        private void InputFolder_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths == null || paths.Length == 0)
            {
                return;
            }

            string droppedPath = paths[0];
            if (Directory.Exists(droppedPath))
            {
                inputFolderTextBox.Text = droppedPath;

                if (string.IsNullOrWhiteSpace(outputFolderTextBox.Text))
                {
                    outputFolderTextBox.Text = Path.Combine(droppedPath, "ModCreator_Output");
                }
            }
        }

        private static void ttarch2BuilderLegacy1132(
            string inputFolder,
            string outputPath,
            bool compression,
            bool encryption,
            bool encLua,
            byte[] key,
            int versionArchive,
            bool newEngine,
            Action<int> progressCallback,
            ISet<string> excludedPaths)
        {
            DirectoryInfo di = new DirectoryInfo(inputFolder);
            FileInfo[] fi = di.GetFiles("*", SearchOption.AllDirectories)
                .Where(f => excludedPaths == null || !excludedPaths.Contains(Path.GetFullPath(f.FullName)))
                .ToArray();

            ulong[] nameCrc = new ulong[fi.Length];
            string[] name = new string[fi.Length];
            ulong offset = 0;

            for (int i = 0; i < fi.Length; i++)
            {
                if ((fi[i].Extension.ToLower() == ".lua") && encLua)
                {
                    name[i] = !newEngine ? fi[i].Name.Replace(".lua", ".lenc") : fi[i].Name;
                }
                else
                {
                    name[i] = fi[i].Name;
                }

                nameCrc[i] = CRCs.CRC64(0, name[i].ToLower());
            }

            for (int k = 0; k < fi.Length - 1; k++)
            {
                for (int l = k + 1; l < fi.Length; l++)
                {
                    if (nameCrc[l] < nameCrc[k])
                    {
                        FileInfo temp = fi[k];
                        fi[k] = fi[l];
                        fi[l] = temp;

                        string tempStr = name[k];
                        name[k] = name[l];
                        name[l] = tempStr;

                        ulong tempCrc = nameCrc[k];
                        nameCrc[k] = nameCrc[l];
                        nameCrc[l] = tempCrc;
                    }
                }
            }

            uint infoSize = (uint)fi.Length * (8 + 8 + 4 + 4 + 2 + 2);
            uint dataSize = 0;
            uint nameSize = 0;

            for (int j = 0; j < fi.Length; j++)
            {
                nameSize += (uint)name[j].Length + 1;
                dataSize += (uint)fi[j].Length;
            }

            nameSize = (uint)Methods.pad_it(nameSize, 0x10000);
            byte[] infoTable = new byte[infoSize];
            byte[] namesTable = new byte[nameSize];

            uint nameOffset = 0;
            for (int d = 0; d < fi.Length; d++)
            {
                name[d] += "\0";
                Array.Copy(Encoding.ASCII.GetBytes(name[d]), 0, namesTable, nameOffset, name[d].Length);
                nameOffset += (uint)name[d].Length;
            }

            byte[] ncttHeader = Encoding.ASCII.GetBytes("NCTT");
            byte[] att = versionArchive == 1 ? Encoding.ASCII.GetBytes("3ATT") : Encoding.ASCII.GetBytes("4ATT");
            ulong commonSize = versionArchive == 1 ? dataSize + infoSize + nameSize + 16UL : dataSize + infoSize + nameSize + 12UL;

            uint ns = nameSize;
            uint tmp;
            ulong fileOffset = 0;

            for (int k = 0; k < fi.Length; k++)
            {
                Array.Copy(BitConverter.GetBytes(nameCrc[k]), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes(fileOffset), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes((int)fi[k].Length), 0, infoTable, (long)offset, 4);
                offset += 4;
                Array.Copy(BitConverter.GetBytes(0), 0, infoTable, (long)offset, 4);
                offset += 4;
                tmp = ns - nameSize;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp / 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp % 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                ns += (uint)name[k].Length;
                fileOffset += (uint)fi[k].Length;

                if (fi.Length > 0 && progressCallback != null)
                {
                    int p = 5 + (int)Math.Round(((k + 1) / (double)fi.Length) * 40.0);
                    progressCallback(p);
                }
            }

            string format = Methods.GetExtension(outputPath).ToLower() == ".obb" ? ".obb" : ".ttarch2";
            string tempPath = outputPath.Replace(format, ".tmp");

            using (FileStream fs = new FileStream(tempPath, FileMode.Create))
            {
                fs.Write(ncttHeader, 0, 4);
                fs.Write(BitConverter.GetBytes(commonSize), 0, 8);
                fs.Write(att, 0, 4);

                if (versionArchive == 1)
                {
                    fs.Write(BitConverter.GetBytes(2), 0, 4);
                }

                fs.Write(BitConverter.GetBytes(nameSize), 0, 4);
                fs.Write(BitConverter.GetBytes(fi.Length), 0, 4);
                fs.Write(infoTable, 0, (int)infoSize);
                fs.Write(namesTable, 0, (int)nameSize);

                for (int l = 0; l < fi.Length; l++)
                {
                    byte[] file = File.ReadAllBytes(fi[l].FullName);

                    if ((fi[l].Extension.ToLower() == ".lua") && encLua)
                    {
                        file = Methods.encryptLua(file, key, newEngine, 7);
                    }

                    fs.Write(file, 0, file.Length);

                    if (fi.Length > 0 && progressCallback != null)
                    {
                        int p = 45 + (int)Math.Round(((l + 1) / (double)fi.Length) * 20.0);
                        progressCallback(p);
                    }
                }
            }

            if (!compression)
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(tempPath, outputPath);
                return;
            }

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (FileStream tempFr = new FileStream(tempPath, FileMode.Open))
            {
                ulong fullIt = Methods.pad_it(commonSize, 0x10000);
                uint blocksCount = (uint)fullIt / 0x10000;
                byte[] compressedHeader = encryption ? Encoding.ASCII.GetBytes("ECTT") : Encoding.ASCII.GetBytes("ZCTT");
                byte[] chunkSize = { 0x00, 0x00, 0x01, 0x00 };
                ulong chunkTableSize = 8 * blocksCount + 8;
                offset = chunkTableSize + 12;
                byte[] chunkTable = new byte[chunkTableSize];

                Array.Copy(BitConverter.GetBytes(offset), 0, chunkTable, 0, 8);

                fs.Write(compressedHeader, 0, compressedHeader.Length);
                fs.Write(chunkSize, 0, 4);
                fs.Write(BitConverter.GetBytes(blocksCount), 0, 4);
                fs.Write(chunkTable, 0, chunkTable.Length);

                tempFr.Seek(versionArchive == 1 ? 16 : 12, SeekOrigin.Begin);

                for (int i = 0; i < blocksCount; i++)
                {
                    byte[] temp = new byte[0x10000];
                    tempFr.Read(temp, 0, temp.Length);
                    byte[] compressedBlock = DeflateCompressor(temp);

                    if (encryption)
                    {
                        compressedBlock = encryptFunction(compressedBlock, key, 7);
                    }

                    offset += (uint)compressedBlock.Length;
                    Array.Copy(BitConverter.GetBytes(offset), 0, chunkTable, 8 + (i * 8), 8);
                    fs.Write(compressedBlock, 0, compressedBlock.Length);

                    if (blocksCount > 0 && progressCallback != null)
                    {
                        int p = 65 + (int)Math.Round(((i + 1) / (double)blocksCount) * 25.0);
                        progressCallback(p);
                    }
                }

                fs.Seek(12, SeekOrigin.Begin);
                fs.Write(chunkTable, 0, chunkTable.Length);
            }

            File.Delete(tempPath);
        }

        private static byte[] DeflateCompressor(byte[] bytes)
        {
            byte[] retVal;
            using (MemoryStream compressedMemoryStream = new MemoryStream())
            {
                using (System.IO.Compression.DeflateStream compressStream = new System.IO.Compression.DeflateStream(compressedMemoryStream, System.IO.Compression.CompressionMode.Compress))
                {
                    using (MemoryStream inMemStream = new MemoryStream(bytes))
                    {
                        inMemStream.CopyTo(compressStream);
                        compressStream.Close();
                        retVal = compressedMemoryStream.ToArray();
                    }
                }
            }
            return retVal;
        }

        private static byte[] encryptFunction(byte[] bytes, byte[] key, int archiveVersion)
        {
            BlowFishCS.BlowFish enc = new BlowFishCS.BlowFish(key, archiveVersion);
            return enc.Crypt_ECB(bytes, archiveVersion, false);
        }

        private class ModLayoutOption
        {
            public string DisplayName { get; set; }
            public string ArchiveSegment { get; set; }
            public string LogicalName { get; set; }
            public int Priority { get; set; }
            public string EnableMode { get; set; }
            public int GameDataPriority { get; set; }
            public bool AppendArchiveSegmentToName { get; set; }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        private interface IModCreatorProfile
        {
            string GameDisplayName { get; }
            bool CompressArchive { get; }
            bool EncryptArchive { get; }
            bool EncryptLuaInsideArchive { get; }
            bool NewEngineLua { get; }
            int Ttarch2Version { get; }
            bool RequiresLayoutSelection { get; }

            List<ModLayoutOption> GetLayoutOptions();
            string BuildArchiveFileName(string modName, ModLayoutOption layoutOption);
            string BuildLuaFileName(string modName, ModLayoutOption layoutOption);
            string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption);
        }

        private class PokerNightRemasterProfile : IModCreatorProfile
        {
            public string GameDisplayName => "Poker Night at the Inventory - Remastered";
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => true;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;
            public bool RequiresLayoutSelection => false;

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>
                {
                    new ModLayoutOption
                    {
                        DisplayName = "Project",
                        ArchiveSegment = "Project",
                        LogicalName = "Project",
                        Priority = -8888,
                        EnableMode = "constant",
                        GameDataPriority = 0
                    }
                };
            }

            public string BuildArchiveFileName(string modName, ModLayoutOption layoutOption)
            {
                return "CP_pc_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName, ModLayoutOption layoutOption)
            {
                return "_resdesc_50_" + modName + ".lua";
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("local set = {}");
                sb.AppendLine("set.name = \"" + modName + "\"");
                sb.AppendLine("set.setName = \"" + modName + "\"");
                sb.AppendLine("set.descriptionFilenameOverride = \"\"");
                sb.AppendLine("set.logicalName = \"<Project>\"");
                sb.AppendLine("set.logicalDestination = \"<>\"");
                sb.AppendLine("set.priority = -8888");
                sb.AppendLine("set.localDir = _currentDirectory");
                sb.AppendLine("set.enableMode = \"constant\"");
                sb.AppendLine("set.version = \"trunk\"");
                sb.AppendLine("set.descriptionPriority = 0");
                sb.AppendLine("set.gameDataName = \"" + modName + " Game Data\"");
                sb.AppendLine("set.gameDataPriority = 0");
                sb.AppendLine("set.gameDataEnableMode = \"constant\"");
                sb.AppendLine("set.localDirIncludeBase = true");
                sb.AppendLine("set.localDirRecurse = false");
                sb.AppendLine("set.localDirIncludeOnly = nil");
                sb.AppendLine("set.localDirExclude =");
                sb.AppendLine("{");
                sb.AppendLine("    \"Packaging/\",");
                sb.AppendLine("    \"_dev/\"");
                sb.AppendLine("}");
                sb.AppendLine("set.gameDataArchives =");
                sb.AppendLine("{");
                sb.AppendLine("    _currentDirectory .. \"" + archiveFileName + "\"");
                sb.AppendLine("}");
                sb.AppendLine("RegisterSetDescription(set)");

                return sb.ToString();
            }
        }

        private class SamAndMaxSaveWorldRemasterProfile : IModCreatorProfile
        {
            public string GameDisplayName => "Sam & Max: Save the World - Remastered";
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => false;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;
            public bool RequiresLayoutSelection => true;

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>
                {
                    new ModLayoutOption { DisplayName = "Boot", ArchiveSegment = "Boot", LogicalName = "Boot", Priority = 10, EnableMode = "bootable", GameDataPriority = 0 },
                    new ModLayoutOption { DisplayName = "UI", ArchiveSegment = "UI", LogicalName = "UI", Priority = 30, EnableMode = "bootable", GameDataPriority = 0 },
                    new ModLayoutOption { DisplayName = "Common", ArchiveSegment = "Common", LogicalName = "Common", Priority = 100, EnableMode = "bootable", GameDataPriority = 0 },
                    new ModLayoutOption { DisplayName = "Menu", ArchiveSegment = "Menu", LogicalName = "Menu", Priority = 20, EnableMode = "bootable", GameDataPriority = 0 },
                    new ModLayoutOption { DisplayName = "Project", ArchiveSegment = "Project", LogicalName = "Project", Priority = -8888, EnableMode = "constant", GameDataPriority = 0 },
                    new ModLayoutOption { DisplayName = "SamMax101", ArchiveSegment = "SamMax101", LogicalName = "SamMax101", Priority = 101, EnableMode = "bootable", GameDataPriority = 101, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax102", ArchiveSegment = "SamMax102", LogicalName = "SamMax102", Priority = 102, EnableMode = "bootable", GameDataPriority = 102, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax103", ArchiveSegment = "SamMax103", LogicalName = "SamMax103", Priority = 103, EnableMode = "bootable", GameDataPriority = 103, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax104", ArchiveSegment = "SamMax104", LogicalName = "SamMax104", Priority = 104, EnableMode = "bootable", GameDataPriority = 104, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax105", ArchiveSegment = "SamMax105", LogicalName = "SamMax105", Priority = 105, EnableMode = "bootable", GameDataPriority = 105, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax106", ArchiveSegment = "SamMax106", LogicalName = "SamMax106", Priority = 106, EnableMode = "bootable", GameDataPriority = 106, AppendArchiveSegmentToName = true }
                };
            }

            public string BuildArchiveFileName(string modName, ModLayoutOption layoutOption)
            {
                return "SM1_pc_" + layoutOption.ArchiveSegment + "_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName, ModLayoutOption layoutOption)
            {
                return "_resdesc_50_" + modName + ".lua";
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption)
            {
                string descriptorName = layoutOption.AppendArchiveSegmentToName ? modName + layoutOption.ArchiveSegment.Replace("SamMax", string.Empty) : modName;
                string gameDataName = descriptorName + " Game Data";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("local set = {}");
                sb.AppendLine("set.name = \"" + descriptorName + "\"");
                sb.AppendLine("set.setName = \"" + descriptorName + "\"");
                sb.AppendLine("set.descriptionFilenameOverride = \"\"");
                sb.AppendLine("set.logicalName = \"<" + layoutOption.LogicalName + ">\"");
                sb.AppendLine("set.logicalDestination = \"<>\"");
                sb.AppendLine("set.priority = " + layoutOption.Priority);
                sb.AppendLine("set.localDir = _currentDirectory");
                sb.AppendLine("set.enableMode = \"" + layoutOption.EnableMode + "\"");
                sb.AppendLine("set.version = \"trunk\"");
                sb.AppendLine("set.descriptionPriority = 0");
                sb.AppendLine("set.gameDataName = \"" + gameDataName + "\"");
                sb.AppendLine("set.gameDataPriority = " + layoutOption.GameDataPriority);
                sb.AppendLine("set.gameDataEnableMode = \"constant\"");
                sb.AppendLine("set.localDirIncludeBase = true");
                sb.AppendLine("set.localDirRecurse = false");
                sb.AppendLine("set.localDirIncludeOnly = nil");
                sb.AppendLine("set.localDirExclude =");
                sb.AppendLine("{");
                sb.AppendLine("    \"Packaging/\",");
                sb.AppendLine("    \"_dev/\"");
                sb.AppendLine("}");
                sb.AppendLine("set.gameDataArchives =");
                sb.AppendLine("{");
                sb.AppendLine("    _currentDirectory .. \"" + archiveFileName + "\"");
                sb.AppendLine("}");
                sb.AppendLine("RegisterSetDescription(set)");

                return sb.ToString();
            }
        }
    }
}
