using System;
using System.Diagnostics;
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
        private ComboBox cbInterfaceLanguage; // UI language of the tool itself
        private string _initialInterfaceLanguage; // to detect a change and prompt for restart

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
        private Label lblSaveStatus;
        private Timer _restartTimer;

        private bool _updatingUnicodeMode;
        private bool _loadingUi;
        private bool _dirtyTrackingReady;
        private bool _saveStatusLayoutExpanded = true;
        private const int SaveStatusLayoutOffset = 28;

        public SettingsForm()
        {
            BuildUi();
            AppIcon.Apply(this);
            Load += SettingsForm_Load;
        }

        #region UI construction
        private void BuildUi()
        {
            Text = Loc.T("Settings.title", "Settings");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 382);

            //Native Windows title bars do not support coloring only part of their text, so the
            //save confirmation lives in a dedicated green status line at the top-left.
            lblSaveStatus = new Label
            {
                Left = 12,
                Top = 10,
                Width = 484,
                Height = 18,
                ForeColor = Color.ForestGreen,
                Visible = false
            };
            Controls.Add(lblSaveStatus);

            // ---- Profile bar ----
            Label lblProfile = new Label { Left = 12, Top = 36, Width = 64, Height = 23, TextAlign = ContentAlignment.MiddleLeft, Text = Loc.T("Settings.lblProfile", "Profile") };
            cbProfile = new ComboBox { Left = 80, Top = 36, Width = 204, DropDownStyle = ComboBoxStyle.DropDownList };
            cbProfile.SelectedIndexChanged += cbProfile_SelectedIndexChanged;
            btnNewProfile = new Button { Left = 290, Top = 35, Width = 56, Text = Loc.T("Settings.btnNewProfile", "New") };
            btnNewProfile.Click += btnNewProfile_Click;
            btnRenameProfile = new Button { Left = 350, Top = 35, Width = 72, Text = Loc.T("Settings.btnRenameProfile", "Rename") };
            btnRenameProfile.Click += btnRenameProfile_Click;
            btnDeleteProfile = new Button { Left = 426, Top = 35, Width = 68, Text = Loc.T("Settings.btnDeleteProfile", "Delete") };
            btnDeleteProfile.Click += btnDeleteProfile_Click;
            Controls.Add(lblProfile);
            Controls.Add(cbProfile);
            Controls.Add(btnNewProfile);
            Controls.Add(btnRenameProfile);
            Controls.Add(btnDeleteProfile);

            // ---- Tabs ----
            TabControl tabs = new TabControl { Left = 12, Top = 68, Width = 496, Height = 268 };
            TabPage tabGeneral = new TabPage(Loc.T("Settings.tabGeneral", "General / folders"));
            TabPage tabLanguage = new TabPage(Loc.T("Settings.tabLanguage", "Language"));
            TabPage tabText = new TabPage(Loc.T("Settings.tabText", "Text"));
            TabPage tabImage = new TabPage(Loc.T("Settings.tabImage", "Image / textures"));
            TabPage tabNorm = new TabPage(Loc.T("Settings.tabNorm", "Normalization"));
            tabs.TabPages.AddRange(new[] { tabGeneral, tabLanguage, tabText, tabImage, tabNorm });
            Controls.Add(tabs);

            BuildGeneralTab(tabGeneral);
            BuildLanguageTab(tabLanguage);
            BuildTextTab(tabText);
            BuildImageTab(tabImage);
            BuildNormalizationTab(tabNorm);

            // ---- Bottom buttons ----
            btnSave = new Button { Left = 252, Top = 346, Width = 72, Text = Loc.T("Settings.btnSave", "Save") };
            btnSave.Click += btnSave_Click;
            btnOk = new Button { Left = 346, Top = 346, Width = 72, Text = Loc.T("Settings.btnOk", "OK") };
            btnOk.Click += btnOk_Click;
            btnCancel = new Button { Left = 436, Top = 346, Width = 72, Text = Loc.T("Settings.btnCancel", "Cancel") };
            btnCancel.Click += btnCancel_Click;
            Controls.Add(btnSave);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
            SetSaveStatusLayoutVisible(false);
        }

        private void BuildGeneralTab(TabPage tab)
        {
            // Interface language of the tool itself (different from the game-text "Language" tab).
            Label lblUiLang = new Label { Left = 12, Top = 14, AutoSize = true, Text = Loc.T("Settings.lblInterfaceLanguage", "Interface language") };
            cbInterfaceLanguage = new ComboBox { Left = 150, Top = 11, Width = 276, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblIn = new Label { Left = 12, Top = 48, AutoSize = true, Text = Loc.T("Settings.lblInput", "Input folder") };
            txtInput = new TextBox { Left = 12, Top = 66, Width = 360 };
            Button btnIn = new Button { Left = 378, Top = 65, Width = 78, Text = Loc.T("Settings.btnBrowse", "Browse…") };
            btnIn.Click += (s, e) => txtInput.Text = SettingsShared.PickFolder(txtInput.Text);

            Label lblOut = new Label { Left = 12, Top = 96, AutoSize = true, Text = Loc.T("Settings.lblOutput", "Output folder") };
            txtOutput = new TextBox { Left = 12, Top = 114, Width = 360 };
            Button btnOut = new Button { Left = 378, Top = 113, Width = 78, Text = Loc.T("Settings.btnBrowse", "Browse…") };
            btnOut.Click += (s, e) => txtOutput.Text = SettingsShared.PickFolder(txtOutput.Text);

            chkClearMessages = new CheckBox { Left = 12, Top = 148, AutoSize = true, Text = Loc.T("Settings.chkClearMessages", "Clear messages after each operation") };

            Label lblCfg = new Label { Left = 12, Top = 178, AutoSize = true, Text = Loc.T("Settings.lblConfigFolder", "Configuration folder (config file and profiles)") };
            TextBox txtCfg = new TextBox { Left = 12, Top = 196, Width = 360, ReadOnly = true, Text = Settings.ConfigDirectory };
            Button btnCfg = new Button { Left = 378, Top = 195, Width = 78, Text = Loc.T("Settings.btnOpen", "Open") };
            btnCfg.Click += (s, e) => OpenConfigFolder();

            tab.Controls.Add(lblUiLang);
            tab.Controls.Add(cbInterfaceLanguage);
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
                MessageBox.Show(Loc.T("Settings.msgOpenFolderFailed", "Couldn't open the settings folder:") + "\r\n" + ex.Message);
            }
        }

        private void BuildLanguageTab(TabPage tab)
        {
            Label lblAscii = new Label { Left = 12, Top = 17, AutoSize = true, Text = Loc.T("Settings.lblAscii", "ASCII code page") };
            numAscii = new NumericUpDown { Left = 190, Top = 14, Width = 70, Minimum = 0, Maximum = 1258, Value = 1251, ReadOnly = true };
            numAscii.ValueChanged += numericUpDownASCII_ValueChanged;

            chkLanguage = new CheckBox { Left = 12, Top = 46, AutoSize = true, Text = Loc.T("Settings.chkPickByLanguage", "Pick by language") };
            chkLanguage.CheckedChanged += checkLanguage_CheckedChanged;
            cbLanguage = new ComboBox { Left = 195, Top = 44, Width = 223, DropDownStyle = ComboBoxStyle.DropDownList };

            GroupBox grp = new GroupBox { Left = 12, Top = 76, Width = 456, Height = 112, Text = Loc.T("Settings.grpUnicodeMode", "Unicode mode") };
            rbNormalUnicode = new RadioButton { Left = 12, Top = 18, Width = 430, Text = Loc.T("Settings.rbNormalUnicode", "Normal Unicode") };
            rbNonNormalUnicode2 = new RadioButton { Left = 12, Top = 40, Width = 430, Text = Loc.T("Settings.rbNonNormalUnicode", "NOT normal unicode (recommended for new Tales From the Borderlands)") };
            rbNewBttF = new RadioButton { Left = 12, Top = 62, Width = 430, Text = Loc.T("Settings.rbNewBttF", "ASCII support for Back to the Future Xbox360 / PS4") };
            rbTwdNintendoSwitch = new RadioButton { Left = 12, Top = 84, Width = 430, Text = Loc.T("Settings.rbTwdNintendoSwitch", "Support for The Walking Dead Nintendo Switch") };
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
            GroupBox grp = new GroupBox { Left = 12, Top = 10, Width = 418, Height = 108, Text = Loc.T("Settings.grpTextFormat", "Text file format") };
            rbTxt = new RadioButton { Left = 12, Top = 18, Width = 398, Text = Loc.T("Settings.rbTxt", "Plain text (.txt)") };
            rbTsv = new RadioButton { Left = 12, Top = 40, Width = 398, Text = Loc.T("Settings.rbTsv", "TSV (.tsv)") };
            rbNewTxt = new RadioButton { Left = 12, Top = 62, Width = 398, Text = Loc.T("Settings.rbNewTxt", "New txt format (langid / actor / speech)") };
            rbTelltaleExplorer = new RadioButton { Left = 12, Top = 84, Width = 398, Text = Loc.T("Settings.rbTelltaleExplorer", "Telltale Explorer Style ([id] / Category / Speech)") };
            rbNewTxt.CheckedChanged += newTxtFormatRB_CheckedChanged;
            grp.Controls.Add(rbTxt);
            grp.Controls.Add(rbTsv);
            grp.Controls.Add(rbNewTxt);
            grp.Controls.Add(rbTelltaleExplorer);

            chkChangeLangFlags = new CheckBox { Left = 26, Top = 124, AutoSize = true, Text = Loc.T("Settings.chkChangeLangFlags", "Change language flags") };
            chkImportNames = new CheckBox { Left = 12, Top = 146, AutoSize = true, Text = Loc.T("Settings.chkImportNames", "Import actor names") };
            chkSortStrings = new CheckBox { Left = 12, Top = 168, AutoSize = true, Text = Loc.T("Settings.chkSortStrings", "Sort identical strings") };
            chkExportRealID = new CheckBox { Left = 12, Top = 190, AutoSize = true, Text = Loc.T("Settings.chkExportRealID", "Export real ID") };
            chkIgnoreEmpty = new CheckBox { Left = 12, Top = 212, AutoSize = true, Text = Loc.T("Settings.chkIgnoreEmpty", "Ignore empty strings") };

            tab.Controls.Add(grp);
            tab.Controls.Add(chkChangeLangFlags);
            tab.Controls.Add(chkImportNames);
            tab.Controls.Add(chkSortStrings);
            tab.Controls.Add(chkExportRealID);
            tab.Controls.Add(chkIgnoreEmpty);
        }

        private void BuildImageTab(TabPage tab)
        {
            chkExtractPng = new CheckBox { Left = 12, Top = 16, AutoSize = true, Text = Loc.T("Settings.chkExtractPng", "Extract textures as PNG (convert back on import)") };
            Label pngHint = new Label { Left = 28, Top = 38, AutoSize = true, MaximumSize = new Size(400, 0), ForeColor = SystemColors.GrayText, Text = Loc.T("Settings.pngHint", "Beginner-friendly. Unsupported formats (BC6/BC7/PVRTC) fall back to DDS automatically.") };
            chkDeleteDDS = new CheckBox { Left = 12, Top = 72, AutoSize = true, Text = Loc.T("Settings.chkDeleteDDS", "Delete DDS files after import") };
            chkDeleteD3DTX = new CheckBox { Left = 12, Top = 96, AutoSize = true, Text = Loc.T("Settings.chkDeleteD3DTX", "Delete D3DTX files after import") };
            tab.Controls.Add(chkExtractPng);
            tab.Controls.Add(pngHint);
            tab.Controls.Add(chkDeleteDDS);
            tab.Controls.Add(chkDeleteD3DTX);
        }

        private void BuildNormalizationTab(TabPage tab)
        {
            Label hint = new Label { Left = 12, Top = 12, AutoSize = true, Text = Loc.T("Settings.normHint", "Applied to translated text during import (mainly useful for CJK localizations).") };
            chkNormNewline = new CheckBox { Left = 12, Top = 40, AutoSize = true, Text = Loc.T("Settings.chkNormNewline", "Fix punctuation before line breaks (\\n。 -> 。\\n)") };
            chkRemoveBlanksCjk = new CheckBox { Left = 12, Top = 64, AutoSize = true, Text = Loc.T("Settings.chkRemoveBlanksCjk", "Remove spaces between CJK characters") };
            chkReplaceDot = new CheckBox { Left = 12, Top = 88, AutoSize = true, Text = Loc.T("Settings.chkReplaceDot", "Convert dots near CJK to Chinese period (。)") };
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

            PopulateInterfaceLanguages();

            RefreshProfilesList(MainMenu.settings.activeProfile);
            LoadSettingsToUi();

            btnSave.Enabled = !Program.FirstTime;

            //Let the shared reflow grow buttons/labels and the window so longer translations fit
            //(no-op for English). Handles e.g. the profile bar buttons in German/Turkish/Russian.
            Localizer.AutoFit(this);

            WireDirtyTracking(this);
            _dirtyTrackingReady = true;
        }

        //Fills the interface-language dropdown from the .lang files found in the Languages folder
        //(plus the built-in English), and selects the currently active one.
        private void PopulateInterfaceLanguages()
        {
            cbInterfaceLanguage.Items.Clear();
            foreach (Localization.LanguageInfo li in Localization.AvailableLanguages)
                cbInterfaceLanguage.Items.Add(li);
            cbInterfaceLanguage.DisplayMember = "DisplayName";

            _initialInterfaceLanguage = string.IsNullOrEmpty(MainMenu.settings.interfaceLanguage)
                ? "en" : MainMenu.settings.interfaceLanguage;

            int select = 0;
            for (int i = 0; i < cbInterfaceLanguage.Items.Count; i++)
            {
                Localization.LanguageInfo li = (Localization.LanguageInfo)cbInterfaceLanguage.Items[i];
                if (string.Equals(li.Code, _initialInterfaceLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    select = i;
                    break;
                }
            }
            if (cbInterfaceLanguage.Items.Count > 0) cbInterfaceLanguage.SelectedIndex = select;
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

            Localization.LanguageInfo selLang = cbInterfaceLanguage.SelectedItem as Localization.LanguageInfo;
            if (selLang != null)
            {
                //Store "" for English (the in-source baseline) so old configs stay clean.
                MainMenu.settings.interfaceLanguage =
                    string.Equals(selLang.Code, "en", StringComparison.OrdinalIgnoreCase) ? "" : selLang.Code;
            }

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

            //Unicode storage mode belongs to the game/file format, not to the selected Windows
            //code page. The old UI disabled "NOT normal unicode" for code page 1252, which also
            //blocked valid TFTB workflows in Portuguese, English and several other languages.
            rbNonNormalUnicode2.Enabled = true;
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
            SaveSettings(false);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SaveSettings(true);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool SaveSettings(bool closeAfterSave)
        {
            ApplyUiToSettings();

            if (Program.FirstTime)
            {
                bool pathsOk = !string.IsNullOrEmpty(MainMenu.settings.pathForInputFolder) && Directory.Exists(MainMenu.settings.pathForInputFolder)
                    && !string.IsNullOrEmpty(MainMenu.settings.pathForOutputFolder) && Directory.Exists(MainMenu.settings.pathForOutputFolder);

                if (!pathsOk)
                {
                    MessageBox.Show(Loc.T("Settings.msgSetPaths", "Please set correct paths for input and output folders!"));
                    return false;
                }
            }

            try
            {
                Settings.SaveConfig(MainMenu.settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Loc.T("Settings.msgSaveFailed", "Could not save settings:") + "\r\n" + ex.Message,
                    Loc.T("Common.error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            string newLang = string.IsNullOrEmpty(MainMenu.settings.interfaceLanguage) ? "en" : MainMenu.settings.interfaceLanguage;
            bool languageChanged = !string.Equals(newLang, _initialInterfaceLanguage, StringComparison.OrdinalIgnoreCase);

            //Use the selected language for the title confirmation. The rest of the open forms
            //are recreated by the restart below, which also restores the English Designer text
            //correctly when switching from another language back to English.
            if (languageChanged)
                Localization.LoadLanguage(newLang);

            ShowSavedStatus();
            _initialInterfaceLanguage = newLang;

            //On first run the Settings window is the application's main form, so closing it would
            //end the program. Restart automatically and open the real main menu with the saved
            //language. A language change during normal use follows the same reliable path.
            if (Program.FirstTime || languageChanged)
            {
                BeginApplicationRestart();
                return true;
            }

            if (closeAfterSave)
            {
                DialogResult = DialogResult.OK;
                Close();
            }

            return true;
        }

        private void ShowSavedStatus()
        {
            Text = Loc.T("Settings.title", "Settings");
            lblSaveStatus.Text = Loc.T("Settings.savedTitle", "Your settings have been saved.");
            SetSaveStatusLayoutVisible(true);
            lblSaveStatus.Visible = true;
        }

        private void ClearSavedStatus(object sender, EventArgs e)
        {
            if (!_dirtyTrackingReady || _loadingUi || _restartTimer != null)
                return;

            lblSaveStatus.Text = "";
            lblSaveStatus.Visible = false;
            SetSaveStatusLayoutVisible(false);
        }

        private void SetSaveStatusLayoutVisible(bool visible)
        {
            if (_saveStatusLayoutExpanded == visible) return;

            int delta = visible ? SaveStatusLayoutOffset : -SaveStatusLayoutOffset;

            foreach (Control control in Controls)
            {
                if (control == lblSaveStatus) continue;
                control.Top += delta;
            }

            ClientSize = new Size(ClientSize.Width, ClientSize.Height + delta);
            _saveStatusLayoutExpanded = visible;
        }

        private void WireDirtyTracking(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                TextBoxBase text = child as TextBoxBase;
                ComboBox combo = child as ComboBox;
                CheckBox check = child as CheckBox;
                RadioButton radio = child as RadioButton;
                NumericUpDown number = child as NumericUpDown;

                if (text != null && !text.ReadOnly) text.TextChanged += ClearSavedStatus;
                if (combo != null) combo.SelectedIndexChanged += ClearSavedStatus;
                if (check != null) check.CheckedChanged += ClearSavedStatus;
                if (radio != null) radio.CheckedChanged += ClearSavedStatus;
                if (number != null) number.ValueChanged += ClearSavedStatus;

                if (child.Controls.Count > 0)
                    WireDirtyTracking(child);
            }
        }

        private void BeginApplicationRestart()
        {
            btnSave.Enabled = false;
            btnOk.Enabled = false;
            btnCancel.Enabled = false;

            if (_restartTimer != null)
            {
                _restartTimer.Stop();
                _restartTimer.Dispose();
            }

            //Leave the confirmation visible briefly before recreating the application.
            _restartTimer = new Timer { Interval = 900 };
            _restartTimer.Tick += RestartTimer_Tick;
            _restartTimer.Start();
        }

        private void RestartTimer_Tick(object sender, EventArgs e)
        {
            _restartTimer.Stop();
            _restartTimer.Dispose();
            _restartTimer = null;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    WorkingDirectory = Application.StartupPath,
                    UseShellExecute = true
                });
                Application.Exit();
            }
            catch (Exception ex)
            {
                btnSave.Enabled = !Program.FirstTime;
                btnOk.Enabled = true;
                btnCancel.Enabled = true;
                MessageBox.Show(
                    Loc.T("Settings.msgRestartFailed",
                        "The settings were saved, but the application could not restart automatically. Please restart it manually.")
                    + "\r\n" + ex.Message,
                    Loc.T("Common.error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
            string name = SettingsShared.PromptText(this, Loc.T("Settings.newProfileTitle", "New profile"), Loc.T("Settings.profileNameLabel", "Profile name:"), "");
            if (name == null) return;

            name = Settings.SanitizeProfileName(name);
            if (Settings.ProfileExists(name))
            {
                MessageBox.Show(Loc.T("Settings.msgProfileExists", "A profile with that name already exists."));
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
            string name = SettingsShared.PromptText(this, Loc.T("Settings.renameProfileTitle", "Rename profile"), Loc.T("Settings.newNameLabel", "New name:"), current);
            if (name == null) return;

            name = Settings.SanitizeProfileName(name);
            if (name == current) return;
            if (Settings.ProfileExists(name))
            {
                MessageBox.Show(Loc.T("Settings.msgProfileExists", "A profile with that name already exists."));
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
                MessageBox.Show(Loc.T("Settings.msgCantDeleteLast", "You can't delete the last profile."));
                return;
            }

            string current = Settings.SanitizeProfileName(MainMenu.settings.activeProfile);
            if (MessageBox.Show(Loc.T("Settings.msgDeleteProfileConfirm", "Delete profile") + " \"" + current + "\"?", Loc.T("Settings.deleteProfileTitle", "Delete profile"),
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
