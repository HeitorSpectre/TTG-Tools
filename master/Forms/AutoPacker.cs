using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace TTG_Tools
{
    public partial class AutoPacker : Form
    {
        private readonly int _logBottomMargin;
        private bool loadingSettings;

        public AutoPacker()
        {
            InitializeComponent();
            AppIcon.Apply(this);
            Localizer.Localize(this);
            LocalizeEncryptionVersions();
            _logBottomMargin = Math.Max(12, ClientSize.Height - listBox1.Bottom);
            AlignTopAreaAndLog();
        }

        private void LocalizeEncryptionVersions()
        {
            int selected = comboBox2.SelectedIndex;
            comboBox2.Items.Clear();
            comboBox2.Items.Add(Loc.T("AutoPacker.encVersionsOld", "Versions 2-6"));
            comboBox2.Items.Add(Loc.T("AutoPacker.encVersionsNew", "Versions 7-9"));
            comboBox2.SelectedIndex = GetSafeSelectedIndex(selected, comboBox2.Items.Count);
        }

        private static int GetSafeSelectedIndex(int index, int itemCount)
        {
            if (itemCount <= 0) return -1;
            return index >= 0 && index < itemCount ? index : 0;
        }

        private static bool IsEnglishUi()
        {
            string code = Localization.ActiveLanguageCode;
            return string.IsNullOrEmpty(code) || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase);
        }

        private void AlignTopAreaAndLog()
        {
            const int gapBeforeLog = 14;
            const int rightMargin = 12;
            if (!IsEnglishUi())
                ArrangeLocalizedCompactLayout(rightMargin);

            int topAreaBottom = Math.Max(groupBox1.Bottom, sortLabel.Bottom);
            int logTop = topAreaBottom + gapBeforeLog;
            int logBottom = ClientSize.Height - _logBottomMargin;

            listBox1.SetBounds(
                listBox1.Left,
                logTop,
                listBox1.Width,
                Math.Max(80, logBottom - logTop));
        }

        private void ArrangeLocalizedCompactLayout(int rightMargin)
        {
            //Keep the English visual rhythm: same rows, same compact top area. Longer
            //translations grow horizontally and push the right-side groups/window wider.
            label1.AutoSize = true;
            checkCustomKey.AutoSize = true;
            checkEncLangdb.AutoSize = true;
            checkEncDDS.AutoSize = true;
            CheckNewEngine.AutoSize = true;
            checkIOS.AutoSize = true;
            labelUnicode.AutoSize = true;
            label2.AutoSize = true;
            sortLabel.AutoSize = true;

            int importWidth = Math.Max(124, button1.PreferredSize.Width + 12);
            int exportWidth = Math.Max(124, buttonDecrypt.PreferredSize.Width + 12);
            button1.Width = importWidth;
            buttonDecrypt.SetBounds(button1.Right + 16, buttonDecrypt.Top, exportWidth, buttonDecrypt.Height);

            textBox1.Left = Math.Max(114, checkCustomKey.Right + 8);
            if (textBox1.Right < comboBox1.Right)
                textBox1.Width = comboBox1.Right - textBox1.Left;

            int leftContentRight = Math.Max(label1.Right, Math.Max(textBox1.Right, Math.Max(buttonDecrypt.Right, sortLabel.Right)));
            groupBox1.Left = Math.Max(368, leftContentRight + 18);

            int optionRight = 0;
            foreach (Control control in new Control[] { checkEncLangdb, checkEncDDS, label2, comboBox2, CheckNewEngine, checkIOS, labelUnicode })
            {
                if (control.Visible && control.Right > optionRight)
                    optionRight = control.Right;
            }

            int swizzleRight = TextRenderer.MeasureText(groupBox2.Text, groupBox2.Font).Width + 24;
            foreach (Control control in groupBox2.Controls)
            {
                if (control.Visible && control.Right > swizzleRight)
                    swizzleRight = control.Right;
            }

            groupBox2.Width = Math.Max(126, swizzleRight + 12);
            groupBox2.Left = Math.Max(276, optionRight + 16);
            groupBox1.Width = Math.Max(442, groupBox2.Right + 10);

            if (groupBox1.Right + rightMargin > ClientSize.Width)
            {
                ClientSize = new System.Drawing.Size(groupBox1.Right + rightMargin, ClientSize.Height);
            }
        }

        public static FileInfo[] fi;
        public static FileInfo[] fi_temp;

        public static int numKey;
        public static int selected_index;
        public static int EncVersion;

        Thread threadExport;
        Thread threadImport;

        public struct langdb
        {
            public byte[] head;
            public byte[] hz_data;
            public byte[] lenght_of_name;
            public string name;
            public byte[] lenght_of_text;
            public string text;
            public byte[] lenght_of_waw;
            public string waw;
            public byte[] lenght_of_animation;
            public string animation;
            public byte[] magic_bytes;
            public byte[] realID;
        }


        public static int number;
        langdb[] database = new langdb[5000];

        // MODIFICADO: Lógica do Pop-up adicionada aqui
        public void AddNewReport(string report)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new ReportHandler(AddNewReport), report);
            }
            else
            {
                // DETECTA A MENSAGEM ESPECIAL DE ERRO
                if (report.StartsWith("##POPUP##"))
                {
                    string realMessage = report.Substring(9); // Remove o prefixo
                    MessageBox.Show(realMessage, Loc.T("AutoPacker.titleErrorReport", "Error Report"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Adiciona um aviso no log apenas para constar
                    listBox1.Items.Add(Loc.T("AutoPacker.reportShownOnScreen", ">>> Error report displayed on screen. <<<"));

                    // Rola para o final
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                    listBox1.SelectedIndex = -1;
                    return;
                }

                // Comportamento normal para mensagens que não são de erro crítico
                listBox1.Items.Add(report);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.SelectedIndex = -1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MainMenu.settings.clearMessages) listBox1.Items.Clear();

            try
            {
                DirectoryInfo di = new DirectoryInfo(MainMenu.settings.pathForInputFolder);
                fi = di.GetFiles();
            }
            catch
            {
                MessageBox.Show(Loc.T("AutoPacker.msgFixConfigPath", "Open and close program or fix path in config.xml!"), Loc.T("Common.errorExcl", "Error!"));
                return;
            }

            /*if (checkUnicode.Checked) MainMenu.settings.unicodeSettings = 0;
            else MainMenu.settings.unicodeSettings = 1;*/

            EncVersion = comboBox2.SelectedIndex != 1 ? 2 : 7;

            string versionOfGame = MainMenu.gamelist[comboBox1.SelectedIndex].gamename;
            numKey = comboBox1.SelectedIndex;
            selected_index = comboBox2.SelectedIndex;
            byte[] encKey = MainMenu.settings.customKey ? Methods.stringToKey(MainMenu.settings.encCustomKey) : MainMenu.gamelist[numKey].key;

            //Create import files thread
            var processImport = new ForThreads();
            processImport.ReportForWork += AddNewReport;
            List<string> parametresImport = new List<string>();
            parametresImport.Add(versionOfGame);
            parametresImport.Add(".dds");
            parametresImport.Add(MainMenu.settings.pathForInputFolder);
            parametresImport.Add(MainMenu.settings.pathForOutputFolder);
            parametresImport.Add(MainMenu.settings.deleteD3DTXafterImport.ToString());
            parametresImport.Add(MainMenu.settings.deleteDDSafterImport.ToString());
            parametresImport.Add(Convert.ToString(EncVersion));
            parametresImport.Add(MainMenu.settings.encLangdb.ToString());
            parametresImport.Add(MainMenu.settings.encNewLua.ToString());
            parametresImport.Add(BitConverter.ToString(encKey).Replace("-", ""));

            threadImport = new Thread(new ParameterizedThreadStart(processImport.DoImportEncoding));
            threadImport.Start(parametresImport);
        }

        public static string GetNameOnly(int i)
        {
            return fi[i].Name.Substring(0, (fi[i].Name.Length - fi[i].Extension.Length));
        }

        private void buttonDecrypt_Click(object sender, EventArgs e)
        {
            if (MainMenu.settings.clearMessages) listBox1.Items.Clear();

            string versionOfGame = MainMenu.gamelist[comboBox1.SelectedIndex].gamename;
            numKey = comboBox1.SelectedIndex;
            selected_index = comboBox2.SelectedIndex;

            byte[] encKey = MainMenu.settings.customKey ? Methods.stringToKey(MainMenu.settings.encCustomKey) : MainMenu.gamelist[comboBox1.SelectedIndex].key;

            string debug = null;

            int arc_version = comboBox2.SelectedIndex != 1 ? 2 : 7;

            Methods.DeleteCurrentFile("\\del.me");
            try
            {
                DirectoryInfo di = new DirectoryInfo(MainMenu.settings.pathForInputFolder);
                fi = di.GetFiles();
            }
            catch
            {
                MessageBox.Show(Loc.T("AutoPacker.msgFixConfigPath", "Open and close program or fix path in config.xml!"), Loc.T("Common.errorExcl", "Error!"));
                return;
            }

            //Создаем нить для экспорта текста из LANGDB
            var processExport = new ForThreads();
            processExport.ReportForWork += AddNewReport;
            List<string> parametresExport = new List<string>();
            parametresExport.Add(MainMenu.settings.pathForInputFolder);
            parametresExport.Add(MainMenu.settings.pathForOutputFolder);
            parametresExport.Add(versionOfGame);
            parametresExport.Add(BitConverter.ToString(encKey).Replace("-", ""));
            parametresExport.Add(Convert.ToString(arc_version));

            threadExport = new Thread(new ParameterizedThreadStart(processExport.DoExportEncoding));
            threadExport.Start(parametresExport);

            if (debug != null)
            {
                StreamWriter sw = new StreamWriter(MainMenu.settings.pathForOutputFolder + "\\bugs.txt");
                sw.Write(debug);
                sw.Close();
                listBox1.Items.Add(Loc.T("AutoPacker.reportBugsWritten", "Bugs have been written in file") + " " + MainMenu.settings.pathForOutputFolder + "\\bugs.txt");
            }
        }

        public class Prop
        {
            public byte[] id;
            public byte[] lenght_of_text;
            public string text;

            public Prop() { }
            public Prop(byte[] id, byte[] lenght_of_text, string text)
            {
                this.id = id;
                this.lenght_of_text = lenght_of_text;
                this.text = text;
            }
        }

        private void AutoPacker_Load(object sender, EventArgs e)
        {
            loadingSettings = true;
            try
            {

            #region Load blowfish key list

            comboBox1.Items.Clear();

            for (int i = 0; i < MainMenu.gamelist.Count; i++)
            {
                comboBox1.Items.Add(i + ". " + MainMenu.gamelist[i].gamename);
            }

            #endregion

            comboBox1.SelectedIndex = GetSafeSelectedIndex(MainMenu.settings.encKeyIndex, comboBox1.Items.Count);
            comboBox2.SelectedIndex = GetSafeSelectedIndex(MainMenu.settings.versionEnc, comboBox2.Items.Count);
            bool hasGameKeys = comboBox1.SelectedIndex >= 0;
            button1.Enabled = hasGameKeys;
            buttonDecrypt.Enabled = hasGameKeys;
            labelUnicode.Text = MainMenu.settings.unicodeSettings == 0
                ? Loc.T("AutoPacker.unicodeSet", "Unicode is set.")
                : Loc.T("AutoPacker.unicodeNotSet", "Unicode is not set.");
            sortLabel.Text = MainMenu.settings.sortSameString ? Loc.T("AutoPacker.sortWarning", "Warning! Some files may be slowly extract due enabled sort strings.") : "";
            AlignTopAreaAndLog();
            checkEncDDS.Checked = MainMenu.settings.encDDSonly;
            checkIOS.Checked = MainMenu.settings.iOSsupport;
            checkEncLangdb.Checked = MainMenu.settings.encLangdb;
            CheckNewEngine.Checked = MainMenu.settings.encNewLua;

            if (MainMenu.settings.swizzlePS4 || MainMenu.settings.swizzleNintendoSwitch || MainMenu.settings.swizzleXbox360 || MainMenu.settings.swizzlePSVita || MainMenu.settings.swizzleNintendoWii || MainMenu.settings.swizzlePS2 || MainMenu.settings.swizzleNintendoWiiU || MainMenu.settings.swizzlePS3)
            {
                if (MainMenu.settings.swizzleNintendoSwitch) rbSwitchSwizzle.Checked = true;
                else if (MainMenu.settings.swizzlePS4) rbPS4Swizzle.Checked = true;
                else if (MainMenu.settings.swizzleXbox360) rbXbox360Swizzle.Checked = true;
                else if (MainMenu.settings.swizzlePSVita) rbPSVitaSwizzle.Checked = true;
                else if (MainMenu.settings.swizzleNintendoWii) rbWiiSwizzle.Checked = true;
                else if (MainMenu.settings.swizzlePS2) rbPS2Swizzle.Checked = true;
                else if (MainMenu.settings.swizzleNintendoWiiU) rbWiiUSwizzle.Checked = true;
                else if (MainMenu.settings.swizzlePS3) rbPS3Swizzle.Checked = true;
            }
            else rbNoSwizzle.Checked = true;

            if (MainMenu.settings.customKey && Methods.stringToKey(MainMenu.settings.encCustomKey) != null)
            {
                checkCustomKey.Checked = MainMenu.settings.customKey;
                textBox1.Text = MainMenu.settings.encCustomKey;
            }

            if (MainMenu.settings.ASCII_N == 1252)
            {
                //Make unvisible that option for users with windows-1252 encoding
                labelUnicode.Visible = false;
            }
            }
            finally
            {
                loadingSettings = false;
            }
        }

        private void AutoPacker_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((threadExport != null) && threadExport.IsAlive)
            {
                threadExport.Abort();
            }

            if ((threadImport != null) && threadImport.IsAlive)
            {
                threadImport.Abort();
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void CheckNewEngine_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.encNewLua = CheckNewEngine.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkEncDDS_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.encDDSonly = checkEncDDS.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkEncLangdb_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.encLangdb = checkEncLangdb.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkIOS_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.iOSsupport = checkIOS.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkCustomKey_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.customKey = checkCustomKey.Checked;
            Settings.SaveConfig(MainMenu.settings);

            if ((MainMenu.settings.customKey == true) &&
                ((MainMenu.settings.encCustomKey != "") && (MainMenu.settings.encCustomKey != null)))
            {
                textBox1.Text = MainMenu.settings.encCustomKey;
            }
            else
            {
                textBox1.Text = "";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.encKeyIndex = comboBox1.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.versionEnc = comboBox2.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (checkCustomKey.Checked && Methods.stringToKey(textBox1.Text) != null)
            {
                MainMenu.settings.customKey = checkCustomKey.Checked;
                MainMenu.settings.encCustomKey = textBox1.Text;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.FormClosed += new FormClosedEventHandler(Form_Closed);
            settingsForm.Show(this);
        }

        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            labelUnicode.Text = MainMenu.settings.unicodeSettings == 0
                ? Loc.T("AutoPacker.unicodeSet", "Unicode is set.")
                : Loc.T("AutoPacker.unicodeNotSet", "Unicode is not set.");

            sortLabel.Text = MainMenu.settings.sortSameString ? Loc.T("AutoPacker.sortWarning", "Warning! Some files may be slowly extract due enabled sort strings.") : "";
            AlignTopAreaAndLog();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void rbNoSwizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbNoSwizzle.Checked)
            {
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbPS4Swizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbPS4Swizzle.Checked)
            {
                MainMenu.settings.swizzlePS4 = true;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbSwitchSwizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbSwitchSwizzle.Checked)
            {
                MainMenu.settings.swizzleNintendoSwitch = true;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbXbox360Swizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbXbox360Swizzle.Checked)
            {
                MainMenu.settings.swizzleXbox360 = true;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbPSVitaSwizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbPSVitaSwizzle.Checked)
            {
                MainMenu.settings.swizzlePSVita = true;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }


        private void rbWiiSwizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbWiiSwizzle.Checked)
            {
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzleNintendoWii = true;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbPS2Swizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbPS2Swizzle.Checked)
            {
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = true;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbWiiUSwizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbWiiUSwizzle.Checked)
            {
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = true;
                MainMenu.settings.swizzlePS3 = false;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void rbPS3Swizzle_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            if (rbPS3Swizzle.Checked)
            {
                MainMenu.settings.swizzlePSVita = false;
                MainMenu.settings.swizzlePS4 = false;
                MainMenu.settings.swizzleNintendoSwitch = false;
                MainMenu.settings.swizzleXbox360 = false;
                MainMenu.settings.swizzleNintendoWii = false;
                MainMenu.settings.swizzlePS2 = false;
                MainMenu.settings.swizzleNintendoWiiU = false;
                MainMenu.settings.swizzlePS3 = true;
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private void convertArgb8888Cb_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
