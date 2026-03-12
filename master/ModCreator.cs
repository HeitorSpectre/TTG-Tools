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
            gameComboBox.SelectedIndex = 0;
            gameComboBox.Enabled = false; // estrutura preparada, mas bloqueada para este jogo nesta fase.

            if (string.IsNullOrWhiteSpace(outputFolderTextBox.Text) && Directory.Exists(inputFolderTextBox.Text))
            {
                outputFolderTextBox.Text = Path.Combine(inputFolderTextBox.Text, "ModCreator_Output");
            }
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

            string archiveFileName = pokerNightProfile.BuildArchiveFileName(modName);
            string archivePath = Path.Combine(outputFolder, archiveFileName);
            string luaPath = Path.Combine(outputFolder, pokerNightProfile.BuildLuaFileName(modName));

            SetUiEnabled(false);
            logListBox.Items.Clear();
            AddLog("Starting mod creation for Poker Night at the Inventory - Remastered...");

            try
            {
                await Task.Run(() => CreateModPackage(inputFolder, outputFolder, archivePath, luaPath, modName, archiveFileName));
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
                SetUiEnabled(true);
            }
        }

        private void CreateModPackage(string inputFolder, string outputFolder, string archivePath, string luaPath, string modName, string archiveFileName)
        {
            byte[] gameKey = GetEncryptionKeyForGame(pokerNightProfile.GameDisplayName);

            AddLog("Output folder: " + outputFolder);
            AddLog("Creating archive: " + Path.GetFileName(archivePath));

            ttarch2BuilderLegacy1132(
                inputFolder,
                archivePath,
                pokerNightProfile.CompressArchive,
                pokerNightProfile.EncryptArchive,
                pokerNightProfile.EncryptLuaInsideArchive,
                gameKey,
                pokerNightProfile.Ttarch2Version,
                pokerNightProfile.NewEngineLua,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Path.GetFullPath(archivePath),
                    Path.GetFullPath(luaPath)
                });

            AddLog("Generating Lua descriptor: " + Path.GetFileName(luaPath));
            string luaContent = pokerNightProfile.BuildLuaDescriptor(modName, archiveFileName);

            File.WriteAllText(luaPath, luaContent, new UTF8Encoding(false));

            AddLog("Encrypting Lua descriptor in-place (Lua Scripts for New Engine / method 7-9)...");
            byte[] encryptedLua = Methods.encryptLua(File.ReadAllBytes(luaPath), gameKey, pokerNightProfile.NewEngineLua, 7);
            File.WriteAllBytes(luaPath, encryptedLua);
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
            gameComboBox.Enabled = false;
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
            ISet<string> excludedPaths)
        {
            DirectoryInfo di = new DirectoryInfo(inputFolder);
            FileInfo[] fi = di.GetFiles("*", SearchOption.AllDirectories)
                .Where(f => excludedPaths == null || !excludedPaths.Contains(Path.GetFullPath(f.FullName)))
                .GroupBy(f => f.Name)
                .Select(g => g.First())
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

            for (int i = 0; i < fi.Length - 1; i++)
            {
                for (int j = i + 1; j < fi.Length; j++)
                {
                    if (nameCrc[j] < nameCrc[i])
                    {
                        FileInfo tempFi = fi[i];
                        fi[i] = fi[j];
                        fi[j] = tempFi;

                        ulong tempCrc = nameCrc[i];
                        nameCrc[i] = nameCrc[j];
                        nameCrc[j] = tempCrc;

                        string tempName = name[i];
                        name[i] = name[j];
                        name[j] = tempName;
                    }
                }
            }

            uint nameSize = 0;
            for (int i = 0; i < fi.Length; i++)
            {
                nameSize += (uint)name[i].Length + 1;
            }

            ulong infoSize = (ulong)fi.Length * 28;
            uint dataSize = 0;
            for (int i = 0; i < fi.Length; i++)
            {
                dataSize += (uint)fi[i].Length;
            }

            ulong commonSize = infoSize + nameSize + dataSize + 24;
            byte[] ncttHeader = Encoding.ASCII.GetBytes("NCTT");
            byte[] att = { 65, 84, 84, 84 };
            byte[] infoTable = new byte[infoSize];
            byte[] namesTable = new byte[nameSize];

            uint fileOffset = 0;
            uint ns = 0;

            for (int k = 0; k < fi.Length; k++)
            {
                byte[] tmpName = Encoding.ASCII.GetBytes(name[k]);
                Array.Copy(tmpName, 0, namesTable, ns, tmpName.Length);
                ns += (uint)tmpName.Length + 1;

                Array.Copy(BitConverter.GetBytes(nameCrc[k]), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes((ulong)fileOffset), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes((uint)fi[k].Length), 0, infoTable, (long)offset, 4);
                offset += 4;
                Array.Copy(BitConverter.GetBytes(0), 0, infoTable, (long)offset, 4);
                offset += 4;

                uint tmp = ns - nameSize;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp / 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp % 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                fileOffset += (uint)fi[k].Length;
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

                tempFr.Seek(12, SeekOrigin.Begin);

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

        private class PokerNightRemasterProfile
        {
            public string GameDisplayName => "Poker Night at the Inventory - Remastered";
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => true;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;

            public string BuildArchiveFileName(string modName)
            {
                return "CP_pc_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName)
            {
                return "_resdesc_50_" + modName + ".lua";
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName)
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
    }
}
