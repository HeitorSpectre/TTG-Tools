using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TTG_Tools
{
    /// <summary>
    /// Unified settings window. Replaces the old FormSettings (main menu) and
    /// AutoDePackerSettings (packer) screens with a single, category-tabbed dialog.
    /// The whole UI is built in code so it keeps the native WinForms look (default
    /// theme, Tahoma font pinned by Program.ApplyDefaultFont). A profile bar at the
    /// top lets users keep one named configuration per game (Issue #84); it is fully
    /// optional — ignoring it keeps the classic single-config behaviour.
    /// </summary>
    public class SettingsForm : Form
    {
        // Profile bar
        private ComboBox cbProfile;
        private Button btnNewProfile, btnRenameProfile, btnDeleteProfile;

        // General
        private TextBox txtInput, txtOutput;
        private CheckBox chkClearMessages;

        // Language
        private NumericUpDown numAscii;
        private CheckBox chkLanguage;
        private ComboBox cbLanguage;
        private RadioButton rbNormalUnicode, rbNonNormalUnicode2, rbNewBttF, rbTwdNintendoSwitch;

        // Text
        private RadioButton rbTxt, rbTsv, rbNewTxt, rbTelltaleExplorer;
        private CheckBox chkChangeLangFlags, chkImportNames, chkSortStrings, chkExportRealID, chkIgnoreEmpty;

        // Image / textures
        private CheckBox chkDeleteDDS, chkDeleteD3DTX, chkExtractPng;

        // Normalization
        private CheckBox chkNormNewline, chkRemoveBlanksCjk, chkReplaceDot;

        private Button btnSave, btnOk, btnCancel;

        private bool _updatingUnicodeMode;
        private bool _loadingUi;

        public SettingsForm()
        {
            BuildUi();
            Load += SettingsForm_Load;
        }

        #region UI construction
        private void BuildUi()
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(474, 358);

            // ---- Profile bar ----
            Label lblProfile = new Label { Left = 12, Top = 16, Width = 48, Text = "Profile" };
            cbProfile = new ComboBox { Left = 62, Top = 12, Width = 176, DropDownStyle = ComboBoxStyle.DropDownList };
            cbProfile.SelectedIndexChanged += cbProfile_SelectedIndexChanged;
            btnNewProfile = new Button { Left = 244, Top = 11, Width = 56, Text = "New" };
            btnNewProfile.Click += btnNewProfile_Click;
            btnRenameProfile = new Button { Left = 304, Top = 11, Width = 72, Text = "Rename" };
            btnRenameProfile.Click += btnRenameProfile_Click;
            btnDeleteProfile = new Button { Left = 380, Top = 11, Width = 68, Text = "Delete" };
            btnDeleteProfile.Click += btnDeleteProfile_Click;
            Controls.Add(lblProfile);
            Controls.Add(cbProfile);
            Controls.Add(btnNewProfile);
            Controls.Add(btnRenameProfile);
            Controls.Add(btnDeleteProfile);

            // ---- Tabs ----
            TabControl tabs = new TabControl { Left = 12, Top = 44, Width = 450, Height = 268 };
            TabPage tabGeneral = new TabPage("General / folders");
            TabPage tabLanguage = new TabPage("Language");
            TabPage tabText = new TabPage("Text");
            TabPage tabImage = new TabPage("Image / textures");
            TabPage tabNorm = new TabPage("Normalization");
            tabs.TabPages.AddRange(new[] { tabGeneral, tabLanguage, tabText, tabImage, tabNorm });
            Controls.Add(tabs);

            BuildGeneralTab(tabGeneral);
            BuildLanguageTab(tabLanguage);
            BuildTextTab(tabText);
            BuildImageTab(tabImage);
            BuildNormalizationTab(tabNorm);

            // ---- Bottom buttons ----
            btnSave = new Button { Left = 206, Top = 322, Width = 72, Text = "Save" };
            btnSave.Click += btnSave_Click;
            btnOk = new Button { Left = 300, Top = 322, Width = 72, Text = "OK" };
            btnOk.Click += btnOk_Click;
            btnCancel = new Button { Left = 390, Top = 322, Width = 72, Text = "Cancel" };
            btnCancel.Click += (s, e) => Close();
            Controls.Add(btnSave);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void BuildGeneralTab(TabPage tab)
        {
            Label lblIn = new Label { Left = 12, Top = 14, AutoSize = true, Text = "Input folder" };
            txtInput = new TextBox { Left = 12, Top = 32, Width = 330 };
            Button btnIn = new Button { Left = 348, Top = 31, Width = 78, Text = "Browse…" };
            btnIn.Click += (s, e) => txtInput.Text = SettingsShared.PickFolder(txtInput.Text);

            Label lblOut = new Label { Left = 12, Top = 62, AutoSize = true, Text = "Output folder" };
            txtOutput = new TextBox { Left = 12, Top = 80, Width = 330 };
            Button btnOut = new Button { Left = 348, Top = 79, Width = 78, Text = "Browse…" };
            btnOut.Click += (s, e) => txtOutput.Text = SettingsShared.PickFolder(txtOutput.Text);

            chkClearMessages = new CheckBox { Left = 12, Top = 116, AutoSize = true, Text = "Clear messages after each operation" };

            Label lblCfg = new Label { Left = 12, Top = 150, AutoSize = true, Text = "Configuration folder (config file and profiles)" };
            TextBox txtCfg = new TextBox { Left = 12, Top = 168, Width = 330, ReadOnly = true, Text = Settings.ConfigDirectory };
            Button btnCfg = new Button { Left = 348, Top = 167, Width = 78, Text = "Open" };
            btnCfg.Click += (s, e) => OpenConfigFolder();

            tab.Controls.Add(lblIn);
            tab.Controls.Add(txtInput);
            tab.Controls.Add(btnIn);
            tab.Controls.Add(lblOut);
            tab.Controls.Add(txtOutput);
            tab.Controls.Add(btnOut);
            tab.Controls.Add(chkClearMessages);
            tab.Controls.Add(lblCfg);
            tab.Controls.Add(txtCfg);
            tab.Controls.Add(btnCfg);
        }

        //Opens the folder where the config file and profiles live, so users can see exactly
        //where their settings are stored on disk.
        private void OpenConfigFolder()
        {
            try
            {
                string dir = Settings.ConfigDirectory;
                Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start(dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't open the settings folder:\r\n" + ex.Message);
            }
        }

        private void BuildLanguageTab(TabPage tab)
        {
            Label lblAscii = new Label { Left = 12, Top = 17, AutoSize = true, Text = "ASCII code page" };
            numAscii = new NumericUpDown { Left = 120, Top = 14, Width = 70, Minimum = 0, Maximum = 1258, Value = 1251, ReadOnly = true };
            numAscii.ValueChanged += numericUpDownASCII_ValueChanged;

            chkLanguage = new CheckBox { Left = 12, Top = 46, AutoSize = true, Text = "Pick by language" };
            chkLanguage.CheckedChanged += checkLanguage_CheckedChanged;
            cbLanguage = new ComboBox { Left = 130, Top = 44, Width = 288, DropDownStyle = ComboBoxStyle.DropDownList };

            GroupBox grp = new GroupBox { Left = 12, Top = 76, Width = 418, Height = 112, Text = "Unicode mode" };
            rbNormalUnicode = new RadioButton { Left = 12, Top = 18, Width = 398, Text = "Normal Unicode" };
            rbNonNormalUnicode2 = new RadioButton { Left = 12, Top = 40, Width = 398, Text = "NOT normal unicode (recommended for new Tales From the Borderlands)" };
            rbNewBttF = new RadioButton { Left = 12, Top = 62, Width = 398, Text = "ASCII support for Back to the Future Xbox360 / PS4" };
            rbTwdNintendoSwitch = new RadioButton { Left = 12, Top = 84, Width = 398, Text = "Support for The Walking Dead Nintendo Switch" };
            rbNormalUnicode.CheckedChanged += UnicodeMode_CheckedChanged;
            rbNonNormalUnicode2.CheckedChanged += UnicodeMode_CheckedChanged;
            rbNewBttF.CheckedChanged += UnicodeMode_CheckedChanged;
            rbTwdNintendoSwitch.CheckedChanged += UnicodeMode_CheckedChanged;
            grp.Controls.Add(rbNormalUnicode);
            grp.Controls.Add(rbNonNormalUnicode2);
            grp.Controls.Add(rbNewBttF);
            grp.Controls.Add(rbTwdNintendoSwitch);

            tab.Controls.Add(lblAscii);
            tab.Controls.Add(numAscii);
            tab.Controls.Add(chkLanguage);
            tab.Controls.Add(cbLanguage);
            tab.Controls.Add(grp);
        }

        private void BuildTextTab(TabPage tab)
        {
            GroupBox grp = new GroupBox { Left = 12, Top = 10, Width = 418, Height = 108, Text = "Text file format" };
            rbTxt = new RadioButton { Left = 12, Top = 18, Width = 398, Text = "Plain text (.txt)" };
            rbTsv = new RadioButton { Left = 12, Top = 40, Width = 398, Text = "TSV (.tsv)" };
            rbNewTxt = new RadioButton { Left = 12, Top = 62, Width = 398, Text = "New txt format (langid / actor / speech)" };
            rbTelltaleExplorer = new RadioButton { Left = 12, Top = 84, Width = 398, Text = "Telltale Explorer Style ([id] / Category / Speech)" };
            rbNewTxt.CheckedChanged += newTxtFormatRB_CheckedChanged;
            grp.Controls.Add(rbTxt);
            grp.Controls.Add(rbTsv);
            grp.Controls.Add(rbNewTxt);
            grp.Controls.Add(rbTelltaleExplorer);

            chkChangeLangFlags = new CheckBox { Left = 26, Top = 124, AutoSize = true, Text = "Change language flags" };
            chkImportNames = new CheckBox { Left = 12, Top = 146, AutoSize = true, Text = "Import actor names" };
            chkSortStrings = new CheckBox { Left = 12, Top = 168, AutoSize = true, Text = "Sort identical strings" };
            chkExportRealID = new CheckBox { Left = 12, Top = 190, AutoSize = true, Text = "Export real ID" };
            chkIgnoreEmpty = new CheckBox { Left = 12, Top = 212, AutoSize = true, Text = "Ignore empty strings" };

            tab.Controls.Add(grp);
            tab.Controls.Add(chkChangeLangFlags);
            tab.Controls.Add(chkImportNames);
            tab.Controls.Add(chkSortStrings);
            tab.Controls.Add(chkExportRealID);
            tab.Controls.Add(chkIgnoreEmpty);
        }

        private void BuildImageTab(TabPage tab)
        {
            chkExtractPng = new CheckBox { Left = 12, Top = 16, AutoSize = true, Text = "Extract textures as PNG (convert back on import)" };
            Label pngHint = new Label { Left = 28, Top = 38, AutoSize = true, MaximumSize = new Size(400, 0), ForeColor = SystemColors.GrayText, Text = "Beginner-friendly. Unsupported formats (BC6/BC7/PVRTC) fall back to DDS automatically." };
            chkDeleteDDS = new CheckBox { Left = 12, Top = 72, AutoSize = true, Text = "Delete DDS files after import" };
            chkDeleteD3DTX = new CheckBox { Left = 12, Top = 96, AutoSize = true, Text = "Delete D3DTX files after import" };
            tab.Controls.Add(chkExtractPng);
            tab.Controls.Add(pngHint);
            tab.Controls.Add(chkDeleteDDS);
            tab.Controls.Add(chkDeleteD3DTX);
        }

        private void BuildNormalizationTab(TabPage tab)
        {
            Label hint = new Label { Left = 12, Top = 12, AutoSize = true, Text = "Applied to translated text during import (mainly useful for CJK localizations)." };
            chkNormNewline = new CheckBox { Left = 12, Top = 40, AutoSize = true, Text = "Fix punctuation before line breaks (\\n。 -> 。\\n)" };
            chkRemoveBlanksCjk = new CheckBox { Left = 12, Top = 64, AutoSize = true, Text = "Remove spaces between CJK characters" };
            chkReplaceDot = new CheckBox { Left = 12, Top = 88, AutoSize = true, Text = "Convert dots near CJK to Chinese period (。)" };
            tab.Controls.Add(hint);
            tab.Controls.Add(chkNormNewline);
            tab.Controls.Add(chkRemoveBlanksCjk);
            tab.Controls.Add(chkReplaceDot);
        }
        #endregion

        #region Load / apply settings
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            foreach (string lang in MainMenu.languagesASCII) cbLanguage.Items.Add(lang);

            RefreshProfilesList(MainMenu.settings.activeProfile);
            LoadSettingsToUi();

            btnSave.Enabled = !Program.FirstTime;
        }

        //Pushes the global settings into every control.
        private void LoadSettingsToUi()
        {
            _loadingUi = true;
            try
            {
                txtInput.Text = MainMenu.settings.pathForInputFolder;
                txtOutput.Text = MainMenu.settings.pathForOutputFolder;
                chkClearMessages.Checked = MainMenu.settings.clearMessages;

                numAscii.Value = Clamp(MainMenu.settings.ASCII_N, numAscii.Minimum, numAscii.Maximum);
                chkLanguage.Checked = MainMenu.settings.languageIndex != -1;
                cbLanguage.Enabled = MainMenu.settings.languageIndex != -1;
                cbLanguage.SelectedIndex = MainMenu.settings.languageIndex != -1 && MainMenu.settings.languageIndex < cbLanguage.Items.Count
                    ? MainMenu.settings.languageIndex : (cbLanguage.Items.Count > 0 ? 0 : -1);

                SettingsShared.LoadUnicodeMode(rbNormalUnicode, rbNonNormalUnicode2, rbNewBttF, rbTwdNintendoSwitch);

                if (MainMenu.settings.tsvFormat) rbTsv.Checked = true;
                else if (MainMenu.settings.newTxtFormat) rbNewTxt.Checked = true;
                else if (MainMenu.settings.telltaleExplorerFormat) rbTelltaleExplorer.Checked = true;
                else rbTxt.Checked = true;

                chkChangeLangFlags.Enabled = MainMenu.settings.newTxtFormat;
                chkChangeLangFlags.Visible = MainMenu.settings.newTxtFormat;
                chkChangeLangFlags.Checked = MainMenu.settings.changeLangFlags;

                chkImportNames.Checked = MainMenu.settings.importingOfName;
                chkSortStrings.Checked = MainMenu.settings.sortSameString;
                chkExportRealID.Checked = MainMenu.settings.exportRealID;
                chkIgnoreEmpty.Checked = MainMenu.settings.ignoreEmptyStrings;

                chkDeleteDDS.Checked = MainMenu.settings.deleteDDSafterImport;
                chkExtractPng.Checked = MainMenu.settings.extractTexturesAsPng;
                chkDeleteD3DTX.Checked = MainMenu.settings.deleteD3DTXafterImport;

                chkNormNewline.Checked = MainMenu.settings.normalizePunctuationBeforeNewlineInImport;
                chkRemoveBlanksCjk.Checked = MainMenu.settings.removeBlanksBetweenCjkCharsInImport;
                chkReplaceDot.Checked = MainMenu.settings.replaceDotToChinesePeriodInImport;
            }
            finally
            {
                _loadingUi = false;
            }
        }

        //Reads every control back into the global settings.
        private void ApplyUiToSettings()
        {
            if (Directory.Exists(txtInput.Text)) MainMenu.settings.pathForInputFolder = txtInput.Text;
            if (Directory.Exists(txtOutput.Text)) MainMenu.settings.pathForOutputFolder = txtOutput.Text;

            MainMenu.settings.clearMessages = chkClearMessages.Checked;
            MainMenu.settings.sortSameString = chkSortStrings.Checked;
            MainMenu.settings.deleteD3DTXafterImport = chkDeleteD3DTX.Checked;
            MainMenu.settings.deleteDDSafterImport = chkDeleteDDS.Checked;
            MainMenu.settings.extractTexturesAsPng = chkExtractPng.Checked;
            MainMenu.settings.exportRealID = chkExportRealID.Checked;
            MainMenu.settings.importingOfName = chkImportNames.Checked;
            MainMenu.settings.changeLangFlags = chkChangeLangFlags.Checked;
            MainMenu.settings.ignoreEmptyStrings = chkIgnoreEmpty.Checked;

            MainMenu.settings.normalizePunctuationBeforeNewlineInImport = chkNormNewline.Checked;
            MainMenu.settings.removeBlanksBetweenCjkCharsInImport = chkRemoveBlanksCjk.Checked;
            MainMenu.settings.replaceDotToChinesePeriodInImport = chkReplaceDot.Checked;

            SettingsShared.SaveUnicodeMode(rbNonNormalUnicode2, rbNewBttF, rbTwdNintendoSwitch);

            //The four format radios are mutually exclusive, so each flag mirrors its radio.
            MainMenu.settings.tsvFormat = rbTsv.Checked;
            MainMenu.settings.newTxtFormat = rbNewTxt.Checked;
            MainMenu.settings.telltaleExplorerFormat = rbTelltaleExplorer.Checked;

            MainMenu.settings.ASCII_N = (int)numAscii.Value;

            MainMenu.settings.languageIndex = -1;
            if (chkLanguage.Checked && cbLanguage.SelectedIndex >= 0)
            {
                MainMenu.settings.languageIndex = cbLanguage.SelectedIndex;
                ApplyLanguageASCII(cbLanguage.Text);
                numAscii.Value = Clamp(MainMenu.settings.ASCII_N, numAscii.Minimum, numAscii.Maximum);
            }
        }

        private static decimal Clamp(int value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        #endregion

        #region Language helpers (ported from the old FormSettings)
        private void ApplyLanguageASCII(string selectedLanguage)
        {
            if (selectedLanguage.Contains("(") && selectedLanguage.Contains(")"))
            {
                int start = selectedLanguage.IndexOf("(") + 1;
                int end = selectedLanguage.IndexOf(")");
                if (start < end && start > 0)
                {
                    string strNum = selectedLanguage.Substring(start, end - start).Trim();
                    int asciiValue;
                    if (int.TryParse(strNum, out asciiValue) && asciiValue > 0)
                    {
                        MainMenu.settings.ASCII_N = asciiValue;
                        return;
                    }
                }
            }

            ApplyLanguageASCIIDefault(selectedLanguage);
        }

        private void ApplyLanguageASCIIDefault(string languageName)
        {
            if (languageName.Contains("(")) languageName = languageName.Substring(0, languageName.IndexOf("(")).Trim();

            switch (languageName)
            {
                case "Thai":
                    MainMenu.settings.ASCII_N = 874;
                    break;
                case "Czech":
                case "Polish":
                case "Slovak":
                case "Hungarian":
                case "Serbo-Croatian":
                case "Montenegrin":
                case "Gagauz":
                    MainMenu.settings.ASCII_N = 1250;
                    break;
                case "Belarusian":
                case "Bulgarian":
                case "Macedonian":
                case "Russian":
                case "Rusyn":
                case "Ukrainian":
                    MainMenu.settings.ASCII_N = 1251;
                    break;
                case "Basque":
                case "Catalan":
                case "Faroese":
                case "Occitan":
                case "Romansh":
                case "Swahili":
                    MainMenu.settings.ASCII_N = 1252;
                    break;
                case "Dutch":
                case "Greek":
                    MainMenu.settings.ASCII_N = 1253;
                    break;
                case "Turkish":
                    MainMenu.settings.ASCII_N = 1254;
                    break;
                case "Hebrew":
                    MainMenu.settings.ASCII_N = 1255;
                    break;
                case "Arabic":
                case "Persian":
                case "Urdu":
                    MainMenu.settings.ASCII_N = 1256;
                    break;
                case "Latvian":
                case "Lithuanian":
                case "Latgalian":
                case "Icelandic":
                    MainMenu.settings.ASCII_N = 1257;
                    break;
                case "Vietnamese":
                    MainMenu.settings.ASCII_N = 1258;
                    break;
                default:
                    MainMenu.settings.ASCII_N = 1252;
                    break;
            }
        }

        private void numericUpDownASCII_ValueChanged(object sender, EventArgs e)
        {
            int asciiValue = (int)numAscii.Value;

            switch (asciiValue)
            {
                case 873: numAscii.Value = 874; break;
                case 875: numAscii.Value = 1250; break;
                case 1249: numAscii.Value = 874; break;
                case 1259: numAscii.Value = 1258; break;
            }

            //Terrible fix for users' windows-1252 encoding (kept from the old form).
            if ((int)numAscii.Value == 1252)
            {
                if (rbNonNormalUnicode2.Checked) rbNormalUnicode.Checked = true;
                rbNonNormalUnicode2.Enabled = false;
            }
            else
            {
                rbNonNormalUnicode2.Enabled = true;

                if (!_loadingUi)
                {
                    switch (MainMenu.settings.unicodeSettings)
                    {
                        case 0: rbNormalUnicode.Checked = true; break;
                        case 1: rbNonNormalUnicode2.Checked = true; break;
                        case 2: rbNewBttF.Checked = true; break;
                    }
                }
            }
        }

        private void checkLanguage_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLanguage.Items.Count > 0) cbLanguage.SelectedIndex = 0;
            cbLanguage.Enabled = chkLanguage.Checked;
        }
        #endregion

        #region Radio helpers
        private void UnicodeMode_CheckedChanged(object sender, EventArgs e)
        {
            SettingsShared.HandleUnicodeModeChanged(sender, ref _updatingUnicodeMode,
                rbNormalUnicode, rbNonNormalUnicode2, rbNewBttF, rbTwdNintendoSwitch);
        }

        private void newTxtFormatRB_CheckedChanged(object sender, EventArgs e)
        {
            chkChangeLangFlags.Enabled = rbNewTxt.Checked;
            chkChangeLangFlags.Visible = rbNewTxt.Checked;
        }
        #endregion

        #region Save / OK
        private void btnSave_Click(object sender, EventArgs e)
        {
            ApplyUiToSettings();
            Settings.SaveConfig(MainMenu.settings);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ApplyUiToSettings();

            if (Program.FirstTime)
            {
                bool pathsOk = !string.IsNullOrEmpty(MainMenu.settings.pathForInputFolder) && Directory.Exists(MainMenu.settings.pathForInputFolder)
                    && !string.IsNullOrEmpty(MainMenu.settings.pathForOutputFolder) && Directory.Exists(MainMenu.settings.pathForOutputFolder);

                if (!pathsOk)
                {
                    MessageBox.Show("Please set correct paths for input and output folders!");
                    return;
                }

                Settings.SaveConfig(MainMenu.settings);
                MessageBox.Show("Please restart application to confirm settings");
                Close();
                return;
            }

            Settings.SaveConfig(MainMenu.settings);
            Close();
        }
        #endregion

        #region Profiles (Issue #84)
        private void RefreshProfilesList(string selectName)
        {
            _loadingUi = true;
            try
            {
                cbProfile.Items.Clear();
                string[] profiles = Settings.ListProfiles();
                if (profiles.Length == 0) profiles = new[] { Settings.SanitizeProfileName(MainMenu.settings.activeProfile) };
                cbProfile.Items.AddRange(profiles);

                int idx = cbProfile.Items.IndexOf(Settings.SanitizeProfileName(selectName ?? MainMenu.settings.activeProfile));
                cbProfile.SelectedIndex = idx >= 0 ? idx : 0;
            }
            finally
            {
                _loadingUi = false;
            }
        }

        private void cbProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingUi) return;

            string target = cbProfile.SelectedItem as string;
            if (string.IsNullOrEmpty(target) || target == MainMenu.settings.activeProfile) return;

            //Persist the edits made to the current profile before switching away.
            ApplyUiToSettings();
            Settings.SaveConfig(MainMenu.settings);

            Settings loaded = Settings.LoadProfile(target);
            if (loaded != null) MainMenu.settings = loaded;
            MainMenu.settings.activeProfile = target;
            Settings.SaveConfig(MainMenu.settings);

            LoadSettingsToUi();
        }

        private void btnNewProfile_Click(object sender, EventArgs e)
        {
            string name = SettingsShared.PromptText(this, "New profile", "Profile name:", "");
            if (name == null) return;

            name = Settings.SanitizeProfileName(name);
            if (Settings.ProfileExists(name))
            {
                MessageBox.Show("A profile with that name already exists.");
                return;
            }

            //A new profile starts as a copy of the current settings, then becomes active.
            ApplyUiToSettings();
            MainMenu.settings.activeProfile = name;
            Settings.SaveProfile(name, MainMenu.settings);
            Settings.SaveConfig(MainMenu.settings);

            RefreshProfilesList(name);
        }

        private void btnRenameProfile_Click(object sender, EventArgs e)
        {
            string current = Settings.SanitizeProfileName(MainMenu.settings.activeProfile);
            string name = SettingsShared.PromptText(this, "Rename profile", "New name:", current);
            if (name == null) return;

            name = Settings.SanitizeProfileName(name);
            if (name == current) return;
            if (Settings.ProfileExists(name))
            {
                MessageBox.Show("A profile with that name already exists.");
                return;
            }

            ApplyUiToSettings();
            MainMenu.settings.activeProfile = name;
            Settings.SaveProfile(name, MainMenu.settings);
            Settings.DeleteProfile(current);
            Settings.SaveConfig(MainMenu.settings);

            RefreshProfilesList(name);
        }

        private void btnDeleteProfile_Click(object sender, EventArgs e)
        {
            string[] profiles = Settings.ListProfiles();
            if (profiles.Length <= 1)
            {
                MessageBox.Show("You can't delete the last profile.");
                return;
            }

            string current = Settings.SanitizeProfileName(MainMenu.settings.activeProfile);
            if (MessageBox.Show("Delete profile \"" + current + "\"?", "Delete profile",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Settings.DeleteProfile(current);

            string next = null;
            foreach (string p in profiles) { if (p != current) { next = p; break; } }
            if (next == null) next = "Default";

            Settings loaded = Settings.LoadProfile(next);
            if (loaded != null) MainMenu.settings = loaded;
            MainMenu.settings.activeProfile = next;
            Settings.SaveConfig(MainMenu.settings);

            RefreshProfilesList(next);
            LoadSettingsToUi();
        }
        #endregion
    }
}
