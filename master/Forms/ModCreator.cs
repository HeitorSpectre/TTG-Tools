using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly MinecraftStoryModeSeasonTwoProfile minecraftStoryModeSeasonTwoProfile = new MinecraftStoryModeSeasonTwoProfile();
        private readonly ResourceDescriptorProfile walkingDeadSeasonTwoProfile = CreateWalkingDeadSeasonTwoProfile();
        private readonly ResourceDescriptorProfile wolfAmongUsProfile = CreateWolfAmongUsProfile();
        private readonly ResourceDescriptorProfile walkingDeadMichonneProfile = CreateWalkingDeadMichonneProfile();
        private readonly ResourceDescriptorProfile batmanProfile = CreateBatmanProfile();
        private readonly ResourceDescriptorProfile samAndMaxBeyondTimeAndSpaceRemasterProfile = CreateSamAndMaxBeyondTimeAndSpaceRemasterProfile();
        private readonly ResourceDescriptorProfile gameOfThronesProfile = CreateGameOfThronesProfile();
        private readonly ResourceDescriptorProfile samAndMaxDevilsPlayhouseRemasterProfile = CreateSamAndMaxDevilsPlayhouseRemasterProfile();
        private readonly ResourceDescriptorProfile guardiansOfTheGalaxyProfile = CreateGuardiansOfTheGalaxyProfile();
        private readonly ResourceDescriptorProfile talesFromTheBorderlandsProfile = CreateTalesFromTheBorderlandsProfile();
        private readonly ResourceDescriptorProfile minecraftStoryModeSeasonOneProfile = CreateMinecraftStoryModeSeasonOneProfile();
        private readonly List<IModCreatorProfile> profiles = new List<IModCreatorProfile>();
        private bool loadingSettings;

        private Label platformLabel;
        private ComboBox platformComboBox;

        //Games in release order. A game shows up when at least one platform has a profile for it.
        private static readonly string[] GameOrder =
        {
            "The Walking Dead: Season One",
            "The Wolf Among Us",
            "The Walking Dead: Season Two",
            "Tales From the Borderlands (2014/2015)",
            "Game of Thrones: A Telltale Games Series",
            "Back to the Future: The Game - 30th Anniversary Edition",
            "Minecraft: Story Mode - Season One",
            "The Walking Dead: Michonne",
            "Batman: Telltale Series",
            "The Walking Dead: A New Frontier",
            "Guardians of the Galaxy: A Telltale Games Series",
            "Minecraft: Story Mode - Season Two",
            "Batman: The Enemy Within",
            "The Walking Dead: The Final Season",
            "The Walking Dead: The Telltale Series - Collection",
            "The Walking Dead: The Telltale Definitive Series",
            "Sam & Max: Save the World - Remastered",
            "Sam & Max: Beyond Time and Space - Remastered",
            "Sam & Max: The Devil's Playhouse - Remastered",
            "Poker Night at the Inventory - Remastered",
            "The Wolf Among Us - Remastered"
        };

        //Order of the game list before platforms were added; older configs saved the selection by this index
        private static readonly string[] LegacyGameOrder =
        {
            "The Wolf Among Us",
            "The Walking Dead: Season Two",
            "Tales From the Borderlands (2014/2015)",
            "Game of Thrones: A Telltale Games Series",
            "Minecraft: Story Mode - Season One",
            "The Walking Dead: Michonne",
            "Batman: Telltale Series",
            "Guardians of the Galaxy: A Telltale Games Series",
            "Minecraft: Story Mode - Season Two",
            "Sam & Max: Save the World - Remastered",
            "Sam & Max: Beyond Time and Space - Remastered",
            "Sam & Max: The Devil's Playhouse - Remastered",
            "Poker Night at the Inventory - Remastered"
        };

        private static readonly string[] PlatformCodes = { "PC", "PS3", "PS4", "Xbox", "Switch", "PS Vita", "iOS" };
        private static readonly string[] PlatformNames = { "PC", "PlayStation 3", "PlayStation 4", "Xbox", "Nintendo Switch", "PlayStation Vita", "iOS" };

        private class PlatformItem
        {
            public string Code { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public ModCreator()
        {
            InitializeComponent();
            CreatePlatformControls();
            AppIcon.Apply(this);
            Localizer.Localize(this);
            AlignLocalizedLayout();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;

            AllowDrop = true;
            DragEnter += InputFolder_DragEnter;
            DragDrop += InputFolder_DragDrop;
            inputFolderTextBox.AllowDrop = true;
            inputFolderTextBox.DragEnter += InputFolder_DragEnter;
            inputFolderTextBox.DragDrop += InputFolder_DragDrop;

            profiles.Add(wolfAmongUsProfile);
            profiles.Add(walkingDeadSeasonTwoProfile);
            profiles.Add(talesFromTheBorderlandsProfile);
            profiles.Add(gameOfThronesProfile);
            profiles.Add(minecraftStoryModeSeasonOneProfile);
            profiles.Add(walkingDeadMichonneProfile);
            profiles.Add(batmanProfile);
            profiles.Add(guardiansOfTheGalaxyProfile);
            profiles.Add(minecraftStoryModeSeasonTwoProfile);
            profiles.Add(samAndMaxSaveWorldProfile);
            profiles.Add(samAndMaxBeyondTimeAndSpaceRemasterProfile);
            profiles.Add(samAndMaxDevilsPlayhouseRemasterProfile);
            profiles.Add(pokerNightProfile);
            profiles.AddRange(CreatePlatformProfiles());
        }

        private void CreatePlatformControls()
        {
            platformLabel = new Label();
            platformLabel.Name = "platformLabel";
            platformLabel.AutoSize = true;
            platformLabel.Text = "Platform:";

            platformComboBox = new ComboBox();
            platformComboBox.Name = "platformComboBox";
            platformComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            platformComboBox.SelectedIndexChanged += platformComboBox_SelectedIndexChanged;

            Controls.Add(platformLabel);
            Controls.Add(platformComboBox);
        }

        private void AlignLocalizedLayout()
        {
            Label[] labels =
            {
                inputFolderLabel,
                outputFolderLabel,
                modNameLabel,
                gameLabel,
                modLayoutLabel
            };

            int labelWidth = 0;
            foreach (Label label in labels)
                labelWidth = Math.Max(labelWidth, label.PreferredWidth);
            labelWidth += 4;

            const int leftMargin = 12;
            const int labelGap = 8;
            const int rightMargin = 15;
            const int buttonGap = 7;
            int fieldLeft = leftMargin + labelWidth + labelGap;
            int actionWidth = Math.Max(
                Math.Max(browseInputButton.Width, browseOutputButton.Width),
                createModButton.Width);
            int actionLeft = Math.Max(fieldLeft + 260, ClientSize.Width - rightMargin - actionWidth);
            int longFieldWidth = actionLeft - buttonGap - fieldLeft;

            foreach (Label label in labels)
            {
                label.AutoSize = false;
                label.Left = leftMargin;
                label.Width = labelWidth;
                label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            }

            inputFolderTextBox.SetBounds(fieldLeft, inputFolderTextBox.Top, longFieldWidth, inputFolderTextBox.Height);
            outputFolderTextBox.SetBounds(fieldLeft, outputFolderTextBox.Top, longFieldWidth, outputFolderTextBox.Height);
            modNameTextBox.Left = fieldLeft;
            platformLabel.Left = modNameTextBox.Right + 16;
            platformLabel.Top = modNameTextBox.Top + 3;
            platformComboBox.SetBounds(
                platformLabel.Right + labelGap,
                modNameTextBox.Top - 1,
                Math.Max(140, actionLeft - buttonGap - (platformLabel.Right + labelGap)),
                platformComboBox.Height);
            gameComboBox.SetBounds(fieldLeft, gameComboBox.Top, longFieldWidth, gameComboBox.Height);
            modLayoutComboBox.SetBounds(
                fieldLeft,
                modLayoutComboBox.Top,
                actionLeft + actionWidth - fieldLeft,
                modLayoutComboBox.Height);

            browseInputButton.SetBounds(actionLeft, browseInputButton.Top, actionWidth, browseInputButton.Height);
            browseOutputButton.SetBounds(actionLeft, browseOutputButton.Top, actionWidth, browseOutputButton.Height);
            createModButton.SetBounds(actionLeft, createModButton.Top, actionWidth, createModButton.Height);

            int contentWidth = actionLeft + actionWidth - leftMargin;
            createProgressBar.SetBounds(leftMargin, createProgressBar.Top, contentWidth, createProgressBar.Height);
            logListBox.SetBounds(leftMargin, logListBox.Top, contentWidth, logListBox.Height);
            ClientSize = new System.Drawing.Size(
                actionLeft + actionWidth + rightMargin,
                ClientSize.Height);
        }

        private void ModCreator_Load(object sender, EventArgs e)
        {
            loadingSettings = true;
            gameComboBox.Items.Clear();
            foreach (string game in GameOrder)
            {
                if (profiles.Any(p => p.GameDisplayName == game)) gameComboBox.Items.Add(game);
            }

            string savedGame = MainMenu.settings.modCreatorGame;
            if (string.IsNullOrEmpty(savedGame))
            {
                int legacyIndex = GetSafeSelectedIndex(MainMenu.settings.modCreatorGameIndex, LegacyGameOrder.Length);
                savedGame = legacyIndex >= 0 ? LegacyGameOrder[legacyIndex] : null;
            }

            int gameIndex = savedGame != null ? gameComboBox.Items.IndexOf(savedGame) : -1;
            gameComboBox.SelectedIndex = gameIndex >= 0 ? gameIndex : GetSafeSelectedIndex(0, gameComboBox.Items.Count);
            gameComboBox.Enabled = true;

            UpdatePlatformOptions(MainMenu.settings.modCreatorPlatform);
            UpdateLayoutOptions();

            SetProgress(0);
            loadingSettings = false;
        }

        private static int GetSafeSelectedIndex(int index, int itemCount)
        {
            if (itemCount <= 0) return -1;
            if (index < 0) return 0;
            if (index >= itemCount) return itemCount - 1;
            return index;
        }

        private void browseInputButton_Click(object sender, EventArgs e)
        {
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                inputFolderTextBox.Text = folderDialog.FileName;

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
                MessageBox.Show(Loc.T("ModCreator.msgSelectGame", "Please select a supported game."), Loc.T("Common.error", "Error"));
                return;
            }

            ModLayoutOption selectedLayoutOption = GetSelectedLayoutOption(selectedProfile);
            if (selectedLayoutOption == null)
            {
                MessageBox.Show(Loc.T("ModCreator.msgSelectLayout", "Please select a valid mod layout."), Loc.T("Common.error", "Error"));
                return;
            }

            string inputFolder = inputFolderTextBox.Text.Trim();
            string outputFolder = ResolveOutputFolder(inputFolder, outputFolderTextBox.Text.Trim());
            string modName = NormalizeModName(modNameTextBox.Text.Trim());

            outputFolderTextBox.Text = outputFolder;

            if (!Directory.Exists(inputFolder))
            {
                MessageBox.Show(Loc.T("ModCreator.msgInputNotExist", "Input folder doesn't exist."), Loc.T("Common.error", "Error"));
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            if (string.IsNullOrWhiteSpace(modName))
            {
                MessageBox.Show(Loc.T("ModCreator.msgValidName", "Please provide a valid mod name."), Loc.T("Common.error", "Error"));
                return;
            }

            string archiveFileName = selectedProfile.BuildArchiveFileName(modName, selectedLayoutOption);
            string packageOutputFolder = ResolveLayoutOutputFolder(outputFolder, selectedLayoutOption);
            if (!Directory.Exists(packageOutputFolder))
            {
                Directory.CreateDirectory(packageOutputFolder);
            }

            string archivePath = Path.Combine(packageOutputFolder, archiveFileName);
            string luaPath = Path.Combine(packageOutputFolder, selectedProfile.BuildLuaFileName(modName, selectedLayoutOption));

            SetUiEnabled(false);
            SetProgress(0);
            logListBox.Items.Clear();
            AddLog("Starting mod creation for " + selectedProfile.GameDisplayName + " (" + selectedProfile.Platform + ")...");

            try
            {
                await Task.Run(() => CreateModPackage(inputFolder, packageOutputFolder, archivePath, luaPath, modName, archiveFileName, selectedProfile, selectedLayoutOption));
                SetProgress(100);
                AddLog("Mod created successfully.");
                MessageBox.Show(Loc.T("ModCreator.msgCreated", "Mod created successfully."), Loc.T("Common.success", "Success"));
            }
            catch (Exception ex)
            {
                AddLog("Error: " + ex.Message);
                MessageBox.Show(Loc.T("ModCreator.msgFailed", "Failed to create mod. Check logs for details."), Loc.T("Common.error", "Error"));
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
            if (profile.OodleCompression && !OodleTools.Imports.IsAvailable())
            {
                throw new InvalidOperationException(string.Format(Loc.T("ArchivePacker.reportOodleMissing", "Oodle compression needs {0} in the TTG Tools folder. Copy it from a newer Telltale PC game or choose zlib/deflate compression."), OodleTools.Imports.DllName));
            }

            bool needsKey = profile.EncryptArchive || profile.EncryptLuaInsideArchive || profile.EncryptDescriptor;
            byte[] gameKey = needsKey ? GetEncryptionKeyForGame(profile.KeyGameName) : null;

            AddLog("Platform: " + profile.Platform + (profile.OodleCompression ? " (Oodle)" : " (zlib)"));
            AddLog("Output folder: " + outputFolder);
            AddLog("Creating archive: " + Path.GetFileName(archivePath));
            SetProgress(5);

            ttarch2BuilderLegacy1132(
                inputFolder,
                archivePath,
                profile.CompressArchive,
                profile.OodleCompression,
                profile.EncryptArchive,
                profile.EncryptLuaInsideArchive,
                gameKey,
                profile.Ttarch2Version,
                profile.PlatformId,
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
            byte[] luaBytes = new UTF8Encoding(false).GetBytes(luaContent);

            if (profile.CompileDescriptor)
            {
                AddLog("Compiling Lua descriptor before encryption...");
                luaBytes = CompileLuaDescriptor(luaBytes, profile.DescriptorLuaVersionIndex);
            }

            File.WriteAllBytes(luaPath, luaBytes);

            //Console and mobile builds (and The Wolf Among Us - Remastered) read plain descriptors
            if (profile.EncryptDescriptor)
            {
                AddLog("Encrypting Lua descriptor in-place...");
                SetProgress(95);
                byte[] encryptedLua = Methods.encryptLua(File.ReadAllBytes(luaPath), gameKey, profile.NewEngineLua, profile.DescriptorEncryptionVersion);
                File.WriteAllBytes(luaPath, encryptedLua);
            }

            SetProgress(100);
        }

        private static byte[] CompileLuaDescriptor(byte[] sourceBytes, int luaVersionIndex)
        {
            string folder = luaVersionIndex == 0 ? "LuaP Files" : (luaVersionIndex == 2 ? "LuaR Files" : "LuaQ Files");
            string baseDir = Path.GetDirectoryName(typeof(ModCreator).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            string toolsDir = Path.Combine(baseDir, "LencTools", folder);
            string luac = Path.Combine(toolsDir, "luac.exe");
            if (!File.Exists(luac))
            {
                throw new FileNotFoundException("luac.exe not found in " + toolsDir);
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "TTGModCreator_Lua_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string input = Path.Combine(tempDir, "descriptor.lua");
                File.WriteAllBytes(input, sourceBytes);

                foreach (string file in Directory.GetFiles(toolsDir, "*.exe").Concat(Directory.GetFiles(toolsDir, "*.dll")))
                {
                    File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);
                }

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = Path.Combine(tempDir, "luac.exe");
                    process.StartInfo.Arguments = "\"" + input + "\"";
                    process.StartInfo.WorkingDirectory = tempDir;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException("luac failed: " + error);
                    }
                }

                string output = Path.Combine(tempDir, "luac.out");
                if (!File.Exists(output))
                {
                    throw new FileNotFoundException("luac.out was not produced.");
                }

                return File.ReadAllBytes(output);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
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
            PlatformItem selectedPlatform = platformComboBox.SelectedItem as PlatformItem;
            if (selectedGame == null || selectedPlatform == null) return null;

            return profiles.FirstOrDefault(p => p.GameDisplayName == selectedGame && p.Platform == selectedPlatform.Code);
        }

        //Lists only the platforms that have a profile for the selected game
        private void UpdatePlatformOptions(string preferredPlatform)
        {
            string selectedGame = gameComboBox.SelectedItem as string;
            platformComboBox.Items.Clear();

            for (int i = 0; i < PlatformCodes.Length; i++)
            {
                string code = PlatformCodes[i];
                if (profiles.Any(p => p.GameDisplayName == selectedGame && p.Platform == code))
                {
                    platformComboBox.Items.Add(new PlatformItem { Code = code, Name = PlatformNames[i] });
                }
            }

            int index = 0;
            for (int i = 0; i < platformComboBox.Items.Count; i++)
            {
                if (((PlatformItem)platformComboBox.Items[i]).Code == preferredPlatform) index = i;
            }

            if (platformComboBox.Items.Count > 0) platformComboBox.SelectedIndex = index;
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

        private static string ResolveOutputFolder(string inputFolder, string selectedOutputFolder)
        {
            if (!string.IsNullOrWhiteSpace(selectedOutputFolder))
            {
                return selectedOutputFolder;
            }

            return Path.Combine(inputFolder, "ModCreator_Output");
        }

        private static string ResolveLayoutOutputFolder(string outputFolder, ModLayoutOption layoutOption)
        {
            if (layoutOption == null || string.IsNullOrWhiteSpace(layoutOption.OutputSubfolder))
            {
                return outputFolder;
            }

            return Path.Combine(outputFolder, layoutOption.OutputSubfolder);
        }

        private static string NormalizeModName(string modName)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(modName.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        }

        private static byte[] GetEncryptionKeyForGame(string gameName)
        {
            if (string.Equals(gameName, "Tales From the Borderlands (2014/2015)", StringComparison.Ordinal))
            {
                gameName = "Tales From the Borderlands";
            }

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
            platformComboBox.Enabled = enabled;
            modLayoutComboBox.Enabled = enabled && GetSelectedProfile() != null && GetSelectedProfile().RequiresLayoutSelection;
            modLayoutLabel.Enabled = enabled && GetSelectedProfile() != null && GetSelectedProfile().RequiresLayoutSelection;
        }

        private void gameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlatformItem currentPlatform = platformComboBox.SelectedItem as PlatformItem;
            UpdatePlatformOptions(currentPlatform != null ? currentPlatform.Code : MainMenu.settings.modCreatorPlatform);
            UpdateLayoutOptions();
            if (loadingSettings) return;
            if (gameComboBox.SelectedIndex < 0) return;

            MainMenu.settings.modCreatorGameIndex = gameComboBox.SelectedIndex;
            MainMenu.settings.modCreatorGame = gameComboBox.SelectedItem as string;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void platformComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLayoutOptions();
            if (loadingSettings) return;

            PlatformItem platform = platformComboBox.SelectedItem as PlatformItem;
            if (platform == null) return;

            MainMenu.settings.modCreatorPlatform = platform.Code;
            Settings.SaveConfig(MainMenu.settings);
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

            }
        }

        private static void ttarch2BuilderLegacy1132(
            string inputFolder,
            string outputPath,
            bool compression,
            bool oodle,
            bool encryption,
            bool encLua,
            byte[] key,
            int versionArchive,
            int platformId,
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
                    fs.Write(BitConverter.GetBytes(platformId), 0, 4);
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
                byte[] compressedHeader;
                if (oodle) compressedHeader = encryption ? Encoding.ASCII.GetBytes("eCTT") : Encoding.ASCII.GetBytes("zCTT");
                else compressedHeader = encryption ? Encoding.ASCII.GetBytes("ECTT") : Encoding.ASCII.GetBytes("ZCTT");
                byte[] chunkSize = { 0x00, 0x00, 0x01, 0x00 };
                ulong chunkTableSize = 8 * blocksCount + 8;
                int tableOffset = oodle ? 16 : 12; //Oodle archives keep an extra int (1) after the magic
                offset = chunkTableSize + (ulong)tableOffset;
                byte[] chunkTable = new byte[chunkTableSize];

                Array.Copy(BitConverter.GetBytes(offset), 0, chunkTable, 0, 8);

                fs.Write(compressedHeader, 0, compressedHeader.Length);
                if (oodle) fs.Write(BitConverter.GetBytes(1), 0, 4);
                fs.Write(chunkSize, 0, 4);
                fs.Write(BitConverter.GetBytes(blocksCount), 0, 4);
                fs.Write(chunkTable, 0, chunkTable.Length);

                tempFr.Seek(12, SeekOrigin.Begin); //Skip "NCTT" and the size; "3ATT"/"4ATT" belongs to the compressed stream

                for (int i = 0; i < blocksCount; i++)
                {
                    byte[] temp = new byte[0x10000];
                    tempFr.Read(temp, 0, temp.Length);
                    byte[] compressedBlock;

                    if (oodle)
                    {
                        //A chunk Oodle can't shrink is stored raw; readers recognise it by its full chunk size
                        compressedBlock = ArchivePacker.OodleCompress(temp);
                        if (compressedBlock.Length >= temp.Length) compressedBlock = temp;
                    }
                    else compressedBlock = DeflateCompressor(temp);

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

                fs.Seek(tableOffset, SeekOrigin.Begin);
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
            public int DescriptionPriority { get; set; }
            public bool AppendArchiveSegmentToName { get; set; }
            public bool UsePriorityForGameData { get; set; }
            public bool UsePriorityForDescription { get; set; }
            public string OutputSubfolder { get; set; }
            public string LogicalDestination { get; set; }
            public string Version { get; set; } = "trunk";
            public string DescriptionFilenameOverride { get; set; } = "";
            public bool ExcludePackaging { get; set; } = true;
            public int EffectiveGameDataPriority => UsePriorityForGameData ? Priority : GameDataPriority;
            public int EffectiveDescriptionPriority => UsePriorityForDescription ? Priority : DescriptionPriority;

            public override string ToString()
            {
                return DisplayName;
            }
        }

        private interface IModCreatorProfile
        {
            string GameDisplayName { get; }
            string Platform { get; }
            int PlatformId { get; }
            bool OodleCompression { get; }
            string KeyGameName { get; }
            bool EncryptDescriptor { get; }
            bool CompressArchive { get; }
            bool EncryptArchive { get; }
            bool EncryptLuaInsideArchive { get; }
            bool NewEngineLua { get; }
            int Ttarch2Version { get; }
            bool RequiresLayoutSelection { get; }
            int DescriptorEncryptionVersion { get; }
            bool CompileDescriptor { get; }
            int DescriptorLuaVersionIndex { get; }

            List<ModLayoutOption> GetLayoutOptions();
            string BuildArchiveFileName(string modName, ModLayoutOption layoutOption);
            string BuildLuaFileName(string modName, ModLayoutOption layoutOption);
            string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption);
        }

        private class PokerNightRemasterProfile : IModCreatorProfile
        {
            public string GameDisplayName => "Poker Night at the Inventory - Remastered";
            public string Platform => "PC";
            public int PlatformId => 2;
            public bool OodleCompression => false;
            public string KeyGameName => GameDisplayName;
            public bool EncryptDescriptor => true;
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => true;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;
            public bool RequiresLayoutSelection => true;
            public int DescriptorEncryptionVersion => 7;
            public bool CompileDescriptor => false;
            public int DescriptorLuaVersionIndex => 1;

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>
                {
                    new ModLayoutOption { DisplayName = "Boot", ArchiveSegment = "Boot", LogicalName = "Boot", Priority = 10, EnableMode = "bootable", GameDataPriority = 10, DescriptionPriority = 10 },
                    new ModLayoutOption { DisplayName = "Common", ArchiveSegment = "Common", LogicalName = "Common", Priority = 100, EnableMode = "bootable", GameDataPriority = 100, DescriptionPriority = 100 },
                    new ModLayoutOption { DisplayName = "Menu", ArchiveSegment = "Menu", LogicalName = "Menu", Priority = 20, EnableMode = "bootable", GameDataPriority = 20, DescriptionPriority = 20 },
                    new ModLayoutOption { DisplayName = "Project", ArchiveSegment = "Project", LogicalName = "Project", Priority = -8888, EnableMode = "constant", GameDataPriority = -8888, DescriptionPriority = -8888 }
                };
            }

            public string BuildArchiveFileName(string modName, ModLayoutOption layoutOption)
            {
                return "CP_pc_" + layoutOption.ArchiveSegment + "_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName, ModLayoutOption layoutOption)
            {
                return "_resdesc_50_" + layoutOption.ArchiveSegment + "_" + modName + ".lua";
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("local set = {}");
                sb.AppendLine("set.name = \"" + modName + "\"");
                sb.AppendLine("set.setName = \"" + modName + "\"");
                sb.AppendLine("set.descriptionFilenameOverride = \"\"");
                sb.AppendLine("set.logicalName = \"<" + layoutOption.LogicalName + ">\"");
                sb.AppendLine("set.logicalDestination = \"<>\"");
                sb.AppendLine("set.priority = " + layoutOption.Priority);
                sb.AppendLine("set.localDir = _currentDirectory");
                sb.AppendLine("set.enableMode = \"" + layoutOption.EnableMode + "\"");
                sb.AppendLine("set.version = \"trunk\"");
                sb.AppendLine("set.descriptionPriority = " + layoutOption.DescriptionPriority);
                sb.AppendLine("set.gameDataName = \"" + modName + " Game Data\"");
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

        private class SamAndMaxSaveWorldRemasterProfile : IModCreatorProfile
        {
            public string GameDisplayName => "Sam & Max: Save the World - Remastered";
            public string Platform => "PC";
            public int PlatformId => 2;
            public bool OodleCompression => false;
            public string KeyGameName => GameDisplayName;
            public bool EncryptDescriptor => true;
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => false;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;
            public bool RequiresLayoutSelection => true;
            public int DescriptorEncryptionVersion => 7;
            public bool CompileDescriptor => false;
            public int DescriptorLuaVersionIndex => 1;

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>
                {
                    new ModLayoutOption { DisplayName = "Boot", ArchiveSegment = "Boot", LogicalName = "Boot", Priority = 10, EnableMode = "bootable", GameDataPriority = 10, DescriptionPriority = 10 },
                    new ModLayoutOption { DisplayName = "UI", ArchiveSegment = "UI", LogicalName = "UI", Priority = 30, EnableMode = "bootable", GameDataPriority = 30, DescriptionPriority = 30 },
                    new ModLayoutOption { DisplayName = "Common", ArchiveSegment = "Common", LogicalName = "Common", Priority = 100, EnableMode = "bootable", GameDataPriority = 100, DescriptionPriority = 100 },
                    new ModLayoutOption { DisplayName = "Menu", ArchiveSegment = "Menu", LogicalName = "Menu", Priority = 20, EnableMode = "bootable", GameDataPriority = 20, DescriptionPriority = 20 },
                    new ModLayoutOption { DisplayName = "Project", ArchiveSegment = "Project", LogicalName = "Project", Priority = -8888, EnableMode = "constant", GameDataPriority = -8888, DescriptionPriority = -8888 },
                    new ModLayoutOption { DisplayName = "SamMax101", ArchiveSegment = "SamMax101", LogicalName = "SamMax101", Priority = 101, EnableMode = "bootable", GameDataPriority = 101, DescriptionPriority = 101, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax102", ArchiveSegment = "SamMax102", LogicalName = "SamMax102", Priority = 102, EnableMode = "bootable", GameDataPriority = 102, DescriptionPriority = 102, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax103", ArchiveSegment = "SamMax103", LogicalName = "SamMax103", Priority = 103, EnableMode = "bootable", GameDataPriority = 103, DescriptionPriority = 103, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax104", ArchiveSegment = "SamMax104", LogicalName = "SamMax104", Priority = 104, EnableMode = "bootable", GameDataPriority = 104, DescriptionPriority = 104, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax105", ArchiveSegment = "SamMax105", LogicalName = "SamMax105", Priority = 105, EnableMode = "bootable", GameDataPriority = 105, DescriptionPriority = 105, AppendArchiveSegmentToName = true },
                    new ModLayoutOption { DisplayName = "SamMax106", ArchiveSegment = "SamMax106", LogicalName = "SamMax106", Priority = 106, EnableMode = "bootable", GameDataPriority = 106, DescriptionPriority = 106, AppendArchiveSegmentToName = true }
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
                sb.AppendLine("set.descriptionPriority = " + layoutOption.DescriptionPriority);
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

        private class ResourceDescriptorProfile : IModCreatorProfile
        {
            private readonly string archivePrefix;
            private readonly string descriptorPrefix;
            private readonly string descriptorExtension;
            private readonly List<ModLayoutOption> layouts;

            public ResourceDescriptorProfile(
                string gameDisplayName,
                string archivePrefix,
                string descriptorPrefix,
                string descriptorExtension,
                bool compileDescriptor,
                int descriptorLuaVersionIndex,
                List<ModLayoutOption> layouts)
            {
                GameDisplayName = gameDisplayName;
                this.archivePrefix = archivePrefix;
                this.descriptorPrefix = descriptorPrefix;
                this.descriptorExtension = descriptorExtension;
                CompileDescriptor = compileDescriptor;
                DescriptorLuaVersionIndex = descriptorLuaVersionIndex;
                this.layouts = layouts;
            }

            public virtual string GameDisplayName { get; }
            public virtual string Platform => "PC";
            public virtual int PlatformId => 2;
            public virtual bool OodleCompression => false;
            public virtual string KeyGameName => GameDisplayName;
            public virtual bool EncryptDescriptor => true;
            public virtual bool CompressArchive => true;
            public virtual bool EncryptArchive => true;
            public virtual bool EncryptLuaInsideArchive => true;
            public virtual bool NewEngineLua => true;
            public virtual int Ttarch2Version => 2;
            public virtual bool RequiresLayoutSelection => true;
            public virtual int DescriptorEncryptionVersion => 7;
            public virtual bool CompileDescriptor { get; }
            public virtual int DescriptorLuaVersionIndex { get; }

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>(layouts);
            }

            public string BuildArchiveFileName(string modName, ModLayoutOption layoutOption)
            {
                return archivePrefix + layoutOption.ArchiveSegment + "_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName, ModLayoutOption layoutOption)
            {
                return descriptorPrefix + layoutOption.ArchiveSegment + "_" + modName + descriptorExtension;
            }

            //Name registered for the set; the newer profiles make it unique per layout (Boot_MyMod, Menu_MyMod...)
            protected virtual string GetSetName(string modName, ModLayoutOption layoutOption)
            {
                return modName;
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption)
            {
                string logicalDestination = string.IsNullOrEmpty(layoutOption.LogicalDestination) ? "<>" : "<" + layoutOption.LogicalDestination + ">";
                string setName = GetSetName(modName, layoutOption);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("local set = {}");
                sb.AppendLine("set.name = \"" + setName + "\"");
                sb.AppendLine("set.setName = \"" + setName + "\"");
                sb.AppendLine("set.descriptionFilenameOverride = \"" + layoutOption.DescriptionFilenameOverride + "\"");
                sb.AppendLine("set.logicalName = \"<" + layoutOption.LogicalName + ">\"");
                sb.AppendLine("set.logicalDestination = \"" + logicalDestination + "\"");
                sb.AppendLine("set.priority = " + layoutOption.Priority);
                sb.AppendLine("set.localDir = _currentDirectory");
                sb.AppendLine("set.enableMode = \"" + layoutOption.EnableMode + "\"");
                sb.AppendLine("set.version = \"" + layoutOption.Version + "\"");
                sb.AppendLine("set.descriptionPriority = " + layoutOption.EffectiveDescriptionPriority);
                sb.AppendLine("set.gameDataName = \"" + setName + " Game Data\"");
                sb.AppendLine("set.gameDataPriority = " + layoutOption.EffectiveGameDataPriority);
                sb.AppendLine("set.gameDataEnableMode = \"constant\"");
                sb.AppendLine("set.localDirIncludeBase = true");
                sb.AppendLine("set.localDirRecurse = false");
                sb.AppendLine("set.localDirIncludeOnly = nil");
                sb.AppendLine("set.localDirExclude =");
                sb.AppendLine("{");
                if (layoutOption.ExcludePackaging)
                {
                    sb.AppendLine("    \"Packaging/\",");
                }
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

        private enum DescriptorMode
        {
            PlainText,      //consoles, mobile and The Wolf Among Us - Remastered ship readable descriptors
            EncryptedText   //newer PC games: Lua source encrypted with the "LEo" header
        }

        //Platform id stored after "3ATT" in version 1 .ttarch2 archives (version 2 archives don't keep it)
        private static int GetPlatformId(string platform)
        {
            switch (platform)
            {
                case "Xbox": return 4;
                case "PS3": return 5;
                case "iOS": return 7;
                case "PS Vita": return 9;
                case "PS4": return 11;
                default: return 2;
            }
        }

        private enum Compression
        {
            Zlib,   //"ZCTT"/"ECTT" archives
            Oodle   //"zCTT"/"eCTT" archives (Oodle Kraken)
        }

        private class PlatformProfile : ResourceDescriptorProfile
        {
            private readonly Compression compression;
            private readonly string platform;
            private readonly string keyGameName;
            private readonly int ttarch2Version;
            private readonly bool encryptArchive;
            private readonly bool encryptLua;
            private readonly DescriptorMode descriptorMode;

            public PlatformProfile(
                string gameDisplayName,
                string keyGameName,
                string platform,
                string archivePrefix,
                string descriptorPrefix,
                int ttarch2Version,
                bool encryptArchive,
                bool encryptLua,
                DescriptorMode descriptorMode,
                Compression compression,
                List<ModLayoutOption> layouts)
                : base(gameDisplayName, archivePrefix, descriptorPrefix, ".lua", false, 1, layouts)
            {
                this.compression = compression;
                this.platform = platform;
                this.keyGameName = keyGameName;
                this.ttarch2Version = ttarch2Version;
                this.encryptArchive = encryptArchive;
                this.encryptLua = encryptLua;
                this.descriptorMode = descriptorMode;
            }

            public override string Platform => platform;
            public override int PlatformId => GetPlatformId(platform);
            public override bool OodleCompression => compression == Compression.Oodle;
            public override string KeyGameName => keyGameName;
            public override bool EncryptDescriptor => descriptorMode == DescriptorMode.EncryptedText;
            public override bool CompressArchive => true;
            public override bool EncryptArchive => encryptArchive;
            public override bool EncryptLuaInsideArchive => encryptLua;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => ttarch2Version;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            protected override string GetSetName(string modName, ModLayoutOption layoutOption)
            {
                return layoutOption.ArchiveSegment + "_" + modName;
            }
        }

        private static ModLayoutOption MakeLayout(
            string displayName,
            string archiveSegment,
            string logicalName,
            int priority,
            string enableMode,
            int descriptionPriority = 0,
            int gameDataPriority = 0,
            string logicalDestination = null,
            string outputSubfolder = null,
            bool excludePackaging = true)
        {
            return new ModLayoutOption
            {
                DisplayName = displayName,
                ArchiveSegment = archiveSegment,
                LogicalName = logicalName,
                Priority = priority,
                EnableMode = enableMode,
                DescriptionPriority = descriptionPriority,
                GameDataPriority = gameDataPriority,
                UsePriorityForDescription = true,
                UsePriorityForGameData = true,
                LogicalDestination = logicalDestination,
                OutputSubfolder = outputSubfolder,
                ExcludePackaging = excludePackaging
            };
        }

        private static ResourceDescriptorProfile CreateWalkingDeadSeasonTwoProfile()
        {
            return new WalkingDeadSeasonTwoProfile();
        }

        private static ResourceDescriptorProfile CreateWolfAmongUsProfile()
        {
            return new WolfAmongUsProfile();
        }

        private static ResourceDescriptorProfile CreateWalkingDeadMichonneProfile()
        {
            return new WalkingDeadMichonneProfile();
        }

        private static ResourceDescriptorProfile CreateBatmanProfile()
        {
            return new BatmanProfile();
        }

        private static ResourceDescriptorProfile CreateSamAndMaxBeyondTimeAndSpaceRemasterProfile()
        {
            return new SamAndMaxBeyondTimeAndSpaceRemasterProfile();
        }

        private static ResourceDescriptorProfile CreateGameOfThronesProfile()
        {
            return new GameOfThronesProfile();
        }

        private static ResourceDescriptorProfile CreateSamAndMaxDevilsPlayhouseRemasterProfile()
        {
            return new SamAndMaxDevilsPlayhouseRemasterProfile();
        }

        private static ResourceDescriptorProfile CreateGuardiansOfTheGalaxyProfile()
        {
            return new GuardiansOfTheGalaxyProfile();
        }

        private static ResourceDescriptorProfile CreateTalesFromTheBorderlandsProfile()
        {
            return new TalesFromTheBorderlandsProfile();
        }

        private static ResourceDescriptorProfile CreateMinecraftStoryModeSeasonOneProfile()
        {
            return new MinecraftStoryModeSeasonOneProfile();
        }

        private class WalkingDeadSeasonTwoProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "The Walking Dead: Season Two";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => false;
            public override int Ttarch2Version => 1;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 2;
            public override bool CompileDescriptor => true;
            public override int DescriptorLuaVersionIndex => 1;

            public WalkingDeadSeasonTwoProfile()
                : base(
                    "The Walking Dead: Season Two",
                    "WalkingDead_pc_",
                    "_resdesc_50_",
                    ".lenc",
                    true,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 10, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("WalkingDead201", "WalkingDead201", "WalkingDead201", 100, "bootable"),
                        MakeLayout("WalkingDead202", "WalkingDead202", "WalkingDead202", 100, "bootable"),
                        MakeLayout("WalkingDead203", "WalkingDead203", "WalkingDead203", 100, "bootable"),
                        MakeLayout("WalkingDead204", "WalkingDead204", "WalkingDead204", 100, "bootable"),
                        MakeLayout("WalkingDead205", "WalkingDead205", "WalkingDead205", 100, "bootable")
                    })
            {
            }
        }

        private class WolfAmongUsProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "The Wolf Among Us";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => false;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => false;
            public override int Ttarch2Version => 1;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 2;
            public override bool CompileDescriptor => true;
            public override int DescriptorLuaVersionIndex => 1;

            public WolfAmongUsProfile()
                : base(
                    "The Wolf Among Us",
                    "Fables_pc_",
                    "_resourcedescriptions_500_",
                    ".lenc",
                    true,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 10, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("Fables101", "Fables101", "Fables101", 20, "bootable"),
                        MakeLayout("Fables102", "Fables102", "Fables102", 20, "bootable"),
                        MakeLayout("Fables103", "Fables103", "Fables103", 20, "bootable"),
                        MakeLayout("Fables104", "Fables104", "Fables104", 20, "bootable"),
                        MakeLayout("Fables105", "Fables105", "Fables105", 20, "bootable")
                    })
            {
            }
        }

        private class WalkingDeadMichonneProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "The Walking Dead: Michonne";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public WalkingDeadMichonneProfile()
                : base(
                    "The Walking Dead: Michonne",
                    "WDM_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("WalkingDeadM101", "WalkingDeadM101", "WalkingDeadM101", 100, "bootable"),
                        MakeLayout("WalkingDeadM102", "WalkingDeadM102", "WalkingDeadM102", 100, "bootable"),
                        MakeLayout("WalkingDeadM103", "WalkingDeadM103", "WalkingDeadM103", 100, "bootable")
                    })
            {
            }
        }

        private class BatmanProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Batman: Telltale Series";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public BatmanProfile()
                : base(
                    "Batman: Telltale Series",
                    "BM_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("Batman101", "Batman101", "Batman101", 101, "bootable"),
                        MakeLayout("Batman102", "Batman102", "Batman102", 102, "bootable"),
                        MakeLayout("Batman103", "Batman103", "Batman103", 103, "bootable"),
                        MakeLayout("Batman104", "Batman104", "Batman104", 104, "bootable"),
                        MakeLayout("Batman105", "Batman105", "Batman105", 105, "bootable")
                    })
            {
            }
        }

        private class SamAndMaxBeyondTimeAndSpaceRemasterProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Sam & Max: Beyond Time and Space - Remastered";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public SamAndMaxBeyondTimeAndSpaceRemasterProfile()
                : base(
                    "Sam & Max: Beyond Time and Space - Remastered",
                    "SM2_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Common", "Common", "Common", 100, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("SamMax201", "SamMax201", "SamMax201", 101, "bootable"),
                        MakeLayout("SamMax202", "SamMax202", "SamMax202", 102, "bootable"),
                        MakeLayout("SamMax203", "SamMax203", "SamMax203", 103, "bootable"),
                        MakeLayout("SamMax204", "SamMax204", "SamMax204", 104, "bootable"),
                        MakeLayout("SamMax205", "SamMax205", "SamMax205", 105, "bootable")
                    })
            {
            }
        }

        private class GameOfThronesProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Game of Thrones: A Telltale Games Series";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 1;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => true;
            public override int DescriptorLuaVersionIndex => 2;

            public GameOfThronesProfile()
                : base(
                    "Game of Thrones: A Telltale Games Series",
                    "GameOfThrones_pc_",
                    "_resdesc_50_",
                    ".lua",
                    true,
                    2,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("GameOfThrones101", "GameOfThrones101", "GameOfThrones101", 41, "bootable"),
                        MakeLayout("GameOfThrones102", "GameOfThrones102", "GameOfThrones102", 42, "bootable"),
                        MakeLayout("GameOfThrones103", "GameOfThrones103", "GameOfThrones103", 43, "bootable"),
                        MakeLayout("GameOfThrones104", "GameOfThrones104", "GameOfThrones104", 44, "bootable"),
                        MakeLayout("GameOfThrones105", "GameOfThrones105", "GameOfThrones105", 45, "bootable"),
                        MakeLayout("GameOfThrones106", "GameOfThrones106", "GameOfThrones106", 46, "bootable")
                    })
            {
            }
        }

        private class SamAndMaxDevilsPlayhouseRemasterProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Sam & Max: The Devil's Playhouse - Remastered";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public SamAndMaxDevilsPlayhouseRemasterProfile()
                : base(
                    "Sam & Max: The Devil's Playhouse - Remastered",
                    "SM3_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Common", "Common", "Common", 100, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("SamMax301", "SamMax301", "SamMax301", 101, "bootable"),
                        MakeLayout("SamMax302", "SamMax302", "SamMax302", 102, "bootable"),
                        MakeLayout("SamMax303", "SamMax303", "SamMax303", 103, "bootable"),
                        MakeLayout("SamMax304", "SamMax304", "SamMax304", 104, "bootable"),
                        MakeLayout("SamMax305", "SamMax305", "SamMax305", 105, "bootable")
                    })
            {
            }
        }

        private class GuardiansOfTheGalaxyProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Guardians of the Galaxy: A Telltale Games Series";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public GuardiansOfTheGalaxyProfile()
                : base(
                    "Guardians of the Galaxy: A Telltale Games Series",
                    "GoG_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("Guardians101", "Guardians101", "Guardians101", 101, "bootable")
                    })
            {
            }
        }

        private class TalesFromTheBorderlandsProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Tales From the Borderlands (2014/2015)";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 1;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => true;
            public override int DescriptorLuaVersionIndex => 2;

            public TalesFromTheBorderlandsProfile()
                : base(
                    "Tales From the Borderlands (2014/2015)",
                    "Borderlands_pc_",
                    "_resdesc_50_",
                    ".lua",
                    true,
                    2,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("Borderlands101", "Borderlands101", "Borderlands101", 41, "bootable"),
                        MakeLayout("Borderlands102", "Borderlands102", "Borderlands102", 42, "bootable"),
                        MakeLayout("Borderlands103", "Borderlands103", "Borderlands103", 43, "bootable"),
                        MakeLayout("Borderlands104", "Borderlands104", "Borderlands104", 44, "bootable"),
                        MakeLayout("Borderlands105", "Borderlands105", "Borderlands105", 45, "bootable")
                    })
            {
            }
        }

        private class MinecraftStoryModeSeasonOneProfile : ResourceDescriptorProfile
        {
            public override string GameDisplayName => "Minecraft: Story Mode - Season One";
            public override bool CompressArchive => true;
            public override bool EncryptArchive => true;
            public override bool EncryptLuaInsideArchive => true;
            public override bool NewEngineLua => true;
            public override int Ttarch2Version => 2;
            public override bool RequiresLayoutSelection => true;
            public override int DescriptorEncryptionVersion => 7;
            public override bool CompileDescriptor => false;
            public override int DescriptorLuaVersionIndex => 1;

            public MinecraftStoryModeSeasonOneProfile()
                : base(
                    "Minecraft: Story Mode - Season One",
                    "MCSM_pc_",
                    "_resdesc_50_",
                    ".lua",
                    false,
                    1,
                    new List<ModLayoutOption>
                    {
                        MakeLayout("Boot", "Boot", "Boot", 10, "bootable"),
                        MakeLayout("Menu", "Menu", "Menu", 20, "bootable"),
                        MakeLayout("Project", "Project", "Project", -8888, "constant"),
                        MakeLayout("UI", "UI", "UI", 30, "bootable"),
                        MakeLayout("Minecraft101", "Minecraft101", "Minecraft101", 101, "bootable"),
                        MakeLayout("Minecraft102", "Minecraft102", "Minecraft102", 102, "bootable", outputSubfolder: "102"),
                        MakeLayout("Minecraft103", "Minecraft103", "Minecraft103", 103, "bootable", outputSubfolder: "103"),
                        MakeLayout("Minecraft104", "Minecraft104", "Minecraft104", 104, "bootable", outputSubfolder: "104"),
                        MakeLayout("Minecraft105", "Minecraft105", "Minecraft105", 105, "bootable", outputSubfolder: "105"),
                        MakeLayout("Minecraft106", "Minecraft106", "Minecraft106", 106, "bootable", outputSubfolder: "106"),
                        MakeLayout("Minecraft107", "Minecraft107", "Minecraft107", 107, "bootable", outputSubfolder: "107"),
                        MakeLayout("Minecraft108", "Minecraft108", "Minecraft108", 108, "bootable", outputSubfolder: "108"),
                        MakeLayout("JesseMale", "JesseMale", "JesseMale", 125, "localization", excludePackaging: false),
                        MakeLayout("JesseMale101", "JesseMale101", "JesseMale101", 130, "localization", logicalDestination: "Minecraft101", excludePackaging: false),
                        MakeLayout("JesseMale102", "JesseMale102", "JesseMale102", 130, "localization", logicalDestination: "Minecraft102", outputSubfolder: "102", excludePackaging: false),
                        MakeLayout("JesseMale103", "JesseMale103", "JesseMale103", 130, "localization", logicalDestination: "Minecraft103", outputSubfolder: "103", excludePackaging: false),
                        MakeLayout("JesseMale104", "JesseMale104", "JesseMale104", 133, "localization", logicalDestination: "Minecraft104", outputSubfolder: "104", excludePackaging: false),
                        MakeLayout("JesseMale105", "JesseMale105", "JesseMale105", 134, "localization", logicalDestination: "Minecraft105", outputSubfolder: "105", excludePackaging: false),
                        MakeLayout("JesseMale106", "JesseMale106", "JesseMale106", 135, "localization", logicalDestination: "Minecraft106", outputSubfolder: "106", excludePackaging: false),
                        MakeLayout("JesseMale107", "JesseMale107", "JesseMale107", 136, "localization", logicalDestination: "Minecraft107", outputSubfolder: "107", excludePackaging: false),
                        MakeLayout("JesseMale108", "JesseMale108", "JesseMale108", 137, "localization", logicalDestination: "Minecraft108", outputSubfolder: "108", excludePackaging: false)
                    })
            {
            }
        }

        private class MinecraftStoryModeSeasonTwoProfile : IModCreatorProfile
        {
            public string GameDisplayName => "Minecraft: Story Mode - Season Two";
            public string Platform => "PC";
            public int PlatformId => 2;
            public bool OodleCompression => false;
            public string KeyGameName => GameDisplayName;
            public bool EncryptDescriptor => true;
            public bool CompressArchive => true;
            public bool EncryptArchive => true;
            public bool EncryptLuaInsideArchive => true;
            public bool NewEngineLua => true;
            public int Ttarch2Version => 2;
            public bool RequiresLayoutSelection => true;
            public int DescriptorEncryptionVersion => 7;
            public bool CompileDescriptor => false;
            public int DescriptorLuaVersionIndex => 1;

            public List<ModLayoutOption> GetLayoutOptions()
            {
                return new List<ModLayoutOption>
                {
                    CreateLayout("Boot", "Boot", "Boot", 10, "bootable"),
                    CreateLayout("Menu", "Menu", "Menu", 20, "bootable"),
                    CreateLayout("UI", "UI", "UI", 30, "bootable"),
                    CreateLayout("Project", "Project", "Project", -8888, "constant"),
                    CreateLayout("Minecraft201", "Minecraft201", "Minecraft201", 101, "bootable"),
                    CreateLayout("Minecraft202", "Minecraft202", "Minecraft202", 102, "bootable", outputSubfolder: "202"),
                    CreateLayout("Minecraft203", "Minecraft203", "Minecraft203", 103, "bootable", outputSubfolder: "203"),
                    CreateLayout("Minecraft204", "Minecraft204", "Minecraft204", 104, "bootable", outputSubfolder: "204"),
                    CreateLayout("Minecraft205", "Minecraft205", "Minecraft205", 105, "bootable", outputSubfolder: "205"),
                    CreateLayout("JesseMale", "JesseMale", "JesseMale", 125, "localization", excludePackaging: false),
                    CreateLayout("JesseMale201", "JesseMale201", "JesseMale201", 230, "localization", logicalDestination: "Minecraft201", excludePackaging: false),
                    CreateLayout("JesseMale202", "JesseMale202", "JesseMale202", 231, "localization", logicalDestination: "Minecraft202", outputSubfolder: "202", excludePackaging: false),
                    CreateLayout("JesseMale203", "JesseMale203", "JesseMale203", 232, "localization", logicalDestination: "Minecraft203", outputSubfolder: "203", excludePackaging: false),
                    CreateLayout("JesseMale204", "JesseMale204", "JesseMale204", 233, "localization", logicalDestination: "Minecraft204", outputSubfolder: "204", excludePackaging: false),
                    CreateLayout("JesseMale205", "JesseMale205", "JesseMale205", 234, "localization", logicalDestination: "Minecraft205", outputSubfolder: "205", excludePackaging: false)
                };
            }

            private static ModLayoutOption CreateLayout(
                string displayName,
                string archiveSegment,
                string logicalName,
                int priority,
                string enableMode,
                string logicalDestination = null,
                string outputSubfolder = null,
                bool excludePackaging = true)
            {
                return new ModLayoutOption
                {
                    DisplayName = displayName,
                    ArchiveSegment = archiveSegment,
                    LogicalName = logicalName,
                    Priority = priority,
                    EnableMode = enableMode,
                    LogicalDestination = logicalDestination,
                    OutputSubfolder = outputSubfolder,
                    ExcludePackaging = excludePackaging,
                    UsePriorityForDescription = true,
                    UsePriorityForGameData = true
                };
            }

            public string BuildArchiveFileName(string modName, ModLayoutOption layoutOption)
            {
                return "MC2_pc_" + layoutOption.ArchiveSegment + "_" + modName + ".ttarch2";
            }

            public string BuildLuaFileName(string modName, ModLayoutOption layoutOption)
            {
                return "_resdesc_50_" + layoutOption.ArchiveSegment + "_" + modName + ".lua";
            }

            public string BuildLuaDescriptor(string modName, string archiveFileName, ModLayoutOption layoutOption)
            {
                string logicalDestination = string.IsNullOrEmpty(layoutOption.LogicalDestination) ? "<>" : "<" + layoutOption.LogicalDestination + ">";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("local set = {}");
                sb.AppendLine("set.name = \"" + modName + "\"");
                sb.AppendLine("set.setName = \"" + modName + "\"");
                sb.AppendLine("set.descriptionFilenameOverride = \"" + layoutOption.DescriptionFilenameOverride + "\"");
                sb.AppendLine("set.logicalName = \"<" + layoutOption.LogicalName + ">\"");
                sb.AppendLine("set.logicalDestination = \"" + logicalDestination + "\"");
                sb.AppendLine("set.priority = " + layoutOption.Priority);
                sb.AppendLine("set.localDir = _currentDirectory");
                sb.AppendLine("set.enableMode = \"" + layoutOption.EnableMode + "\"");
                sb.AppendLine("set.version = \"" + layoutOption.Version + "\"");
                sb.AppendLine("set.descriptionPriority = " + layoutOption.EffectiveDescriptionPriority);
                sb.AppendLine("set.gameDataName = \"" + modName + " Game Data\"");
                sb.AppendLine("set.gameDataPriority = " + layoutOption.EffectiveGameDataPriority);
                sb.AppendLine("set.gameDataEnableMode = \"constant\"");
                sb.AppendLine("set.localDirIncludeBase = true");
                sb.AppendLine("set.localDirRecurse = false");
                sb.AppendLine("set.localDirIncludeOnly = nil");
                sb.AppendLine("set.localDirExclude = ");
                sb.AppendLine("{");
                if (layoutOption.ExcludePackaging)
                {
                    sb.AppendLine("    \"Packaging/\",");
                }
                sb.AppendLine("    \"_dev/\"");
                sb.AppendLine("}");
                sb.AppendLine("set.gameDataArchives = ");
                sb.AppendLine("{");
                sb.AppendLine("    _currentDirectory .. \"" + archiveFileName + "\"");
                sb.AppendLine("}");
                sb.AppendLine("RegisterSetDescription(set)");

                return sb.ToString();
            }
        }
    }
}
