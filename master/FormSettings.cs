using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
        }

        public string SetFolder(string inputPath)
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;

            if (Directory.Exists(inputPath))
            {
                folderDialog.InitialDirectory = inputPath;
            }
            else
            {
                folderDialog.InitialDirectory = Application.StartupPath;
            }

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return folderDialog.FileName;
            }
            else { return inputPath; }
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            MainMenu.settings.ASCII_N = (int)numericUpDownASCII.Value;
            MainMenu.settings.pathForInputFolder = textBoxInputFolder.Text;
            MainMenu.settings.pathForOutputFolder = textBoxOutputFolder.Text;
            
            if (rbNormalUnicode.Checked == true) MainMenu.settings.unicodeSettings = 0;
            else if (rbNonNormalUnicode2.Checked == true) MainMenu.settings.unicodeSettings = 1;
            else MainMenu.settings.unicodeSettings = 2;

            MainMenu.settings.supportTwdNintendoSwitch = rbTwdNintendoSwitch.Checked;

            MainMenu.settings.languageIndex = -1;
            if (checkLanguage.Checked)
            {
                MainMenu.settings.languageIndex = languageComboBox.SelectedIndex;

                string selectedLanguage = languageComboBox.Text;
                
                // Check if text contains parentheses to extract ASCII code
                if (selectedLanguage.Contains("(") && selectedLanguage.Contains(")"))
                {
                    int start = selectedLanguage.IndexOf("(") + 1;
                    int end = selectedLanguage.IndexOf(")");
                    
                    if (start < end && start > 0)
                    {
                        string str_num = selectedLanguage.Substring(start, end - start).Trim();
                        
                        if (int.TryParse(str_num, out int asciiValue) && asciiValue > 0)
                        {
                            MainMenu.settings.ASCII_N = asciiValue;
                        }
                        else
                        {
                            // Fallback to language name mapping
                            ApplyLanguageASCIIDefault(selectedLanguage);
                        }
                    }
                    else
                    {
                        ApplyLanguageASCIIDefault(selectedLanguage);
                    }
                }
                else
                {
                    // If no parentheses, use name mapping
                    ApplyLanguageASCIIDefault(selectedLanguage);
                }

                numericUpDownASCII.Value = MainMenu.settings.ASCII_N;
            }

            if (((MainMenu.settings.pathForInputFolder != "") && (Directory.Exists(MainMenu.settings.pathForInputFolder)))
                && ((MainMenu.settings.pathForOutputFolder != "") && (Directory.Exists(MainMenu.settings.pathForOutputFolder))))
            {
                Settings.SaveConfig(MainMenu.settings);

                if (Program.FirstTime)
                {
                    MessageBox.Show("Please restart application to confirm settings");
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Please set correct paths for input and output folders!");
            }
        }

        private void ApplyLanguageASCIIDefault(string languageName)
        {
            // Remove any content in parentheses to get only the language name
            if (languageName.Contains("("))
            {
                languageName = languageName.Substring(0, languageName.IndexOf("(")).Trim();
            }

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
                    MainMenu.settings.ASCII_N = 1252; // Safe default value
                    break;
            }
        }

        private void buttonOkSettings_Click(object sender, EventArgs e)
        {
            buttonSaveSettings_Click(sender, e);
            if(!Program.FirstTime) this.Close();
        }

        private void buttonCloseSettingsForm_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void numericUpDownASCII_ValueChanged(object sender, EventArgs e)
        {
            int asciiValue = (int)numericUpDownASCII.Value;
            
            switch (asciiValue)
            {
                case 873:
                    numericUpDownASCII.Value = 874;
                    break;
                case 875:
                    numericUpDownASCII.Value = 1250;
                    break;
                case 1249:
                    numericUpDownASCII.Value = 874;
                    break;
                case 1259:
                    numericUpDownASCII.Value = 1258;
                    break;
            }

            // Terrible fix for users windows-1252 encoding
            if ((int)numericUpDownASCII.Value == 1252)
            {
                if(rbNonNormalUnicode2.Checked) rbNormalUnicode.Checked = true;
                rbNonNormalUnicode2.Enabled = false;
            }
            else
            {
                rbNonNormalUnicode2.Enabled = true;

                switch (MainMenu.settings.unicodeSettings)
                {
                    case 0:
                        rbNormalUnicode.Checked = true;
                        break;

                    case 1:
                        rbNonNormalUnicode2.Checked = true;
                        break;

                    case 2:
                        rbNewBttF.Checked = true;
                        break;
                }
            }
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            numericUpDownASCII.Value = MainMenu.settings.ASCII_N;
            textBoxInputFolder.Text = MainMenu.settings.pathForInputFolder;
            textBoxOutputFolder.Text = MainMenu.settings.pathForOutputFolder;

            buttonSaveSettings.Enabled = !Program.FirstTime;

            foreach(string lang in MainMenu.languagesASCII)
            {
                languageComboBox.Items.Add(lang);
            }

            checkLanguage.Checked = MainMenu.settings.languageIndex != -1;
            languageComboBox.Enabled = MainMenu.settings.languageIndex != -1;
            languageComboBox.SelectedIndex = MainMenu.settings.languageIndex != -1 ? languageComboBox.SelectedIndex = MainMenu.settings.languageIndex : 0;

            switch (MainMenu.settings.unicodeSettings)
            {
                case 1:
                    rbNonNormalUnicode2.Checked = true;
                    break;
                case 2:
                    rbNewBttF.Checked = true;
                    break;
                default:
                    rbNormalUnicode.Checked = true;
                    break;
            }

            rbTwdNintendoSwitch.Checked = MainMenu.settings.supportTwdNintendoSwitch;
        }

        private void buttonInputFolder_Click(object sender, EventArgs e)
        {
            textBoxInputFolder.Text = SetFolder(textBoxInputFolder.Text);
        }

        private void buttonOutputFolder_Click(object sender, EventArgs e)
        {
            textBoxOutputFolder.Text = SetFolder(textBoxOutputFolder.Text);
        }

        private void checkLanguage_CheckedChanged(object sender, EventArgs e)
        {
            languageComboBox.SelectedIndex = 0;
            languageComboBox.Enabled = checkLanguage.Checked;            
        }
    }
}