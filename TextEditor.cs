using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using TTG_Tools.Texts;
using TTG_Tools.ClassesStructs.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class TextEditor : Form
    {
        public TextEditor()
        {
            InitializeComponent();
        }

        private string SelectFolder(string description = "")
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;

            if (!string.IsNullOrEmpty(description))
            {
                folderDialog.Title = description;
            }

            return folderDialog.ShowDialog() == CommonFileDialogResult.Ok ? folderDialog.FileName : null;
        }

        //For main progress bar
        void ProcessorProgress(int progress)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressHandler(ProcessorProgress), progress);
            }
            else
            {
                progressBar1.Value = progress;
            }
        }

        //For second progress bar (if user works with several files)
        void ProcessorProgress2(int progress)
        {
            if (progressBar2.InvokeRequired)
            {
                progressBar2.Invoke(new ProgressHandler(ProcessorProgress2), progress);
            }
            else
            {
                progressBar2.Value = progress;
            }
        }

        private void firstFileBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";
                ofd.FilterIndex = 0;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    firstFilePath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    firstFilePath.Text = selectedFolder;
                }
            }
        }

        private void secondFileBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";
                ofd.FilterIndex = 0;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    secondFilePath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    secondFilePath.Text = selectedFolder;
                }
            }
        }

        private void readyFileBtn_Click(object sender, EventArgs e)
        {
            string selectedFolder = SelectFolder();

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                readyFilePath.Text = selectedFolder;
            }
        }

        private void TextEditor_Load(object sender, EventArgs e)
        {
            singleFileRB.Checked = true;
            txtOldMethodRB.Checked = true;
        }

        private void mergeSingleRB_CheckedChanged(object sender, EventArgs e)
        {
            progressBar2.Enabled = severalFilesRB.Checked;
            progressBar2.Visible = severalFilesRB.Checked;
        }

        private void firstDoubledFileBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";

                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    firstDoubledFilePath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    firstDoubledFilePath.Text = selectedFolder;
                }
            }
        }

        private void secondDoubledFileBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    secondDoubledFilePath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    secondDoubledFilePath.Text = selectedFolder;
                }
            }
        }

        private void readyDoubledFileBtn_Click(object sender, EventArgs e)
        {
            string selectedFolder = SelectFolder();

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                readyDoubledFilePath.Text = selectedFolder;
            }
        }

        private List<CommonText> CheckStrings(string checkPath)
        {
            List<CommonText> checkTxts;
            checkTxts = Texts.ReadText.GetStrings(checkPath);
            List<CommonText> duplicatedStrs = new List<CommonText>();

            if ((checkTxts != null) && (checkTxts.Count > 0))
            {
                string firstString = "", secondString = "";
                string firstActorName = "", secondActorName = "";
                bool hasDuplicate;

                progressBar1.Maximum = checkTxts.Count;

                for (int i = 0; i < checkTxts.Count; i++)
                {
                    hasDuplicate = false;
                    firstString = checkTxts[i].actorSpeechOriginal;
                    firstString = Methods.DeleteCommentary(firstString, "[", "]");
                    firstString = Methods.DeleteCommentary(firstString, "{", "}");
                    firstString = System.Text.RegularExpressions.Regex.Replace(firstString, @"[^\w]", "");

                    firstActorName = checkTxts[i].actorName;

                    for (int j = i + 1; j < checkTxts.Count; j++)
                    {
                        secondString = checkTxts[j].actorSpeechOriginal;
                        secondString = Methods.DeleteCommentary(secondString, "[", "]");
                        secondString = Methods.DeleteCommentary(secondString, "{", "}");
                        secondString = System.Text.RegularExpressions.Regex.Replace(secondString, @"[^\w]", "");

                        secondActorName = checkTxts[j].actorName;

                        if ((firstString.ToLower() == secondString.ToLower()) && (firstString != "")
                            && (firstActorName.ToUpper() == secondActorName.ToUpper()))
                        {
                            hasDuplicate = true;
                            CommonText tmpTxt;
                            tmpTxt.isBothSpeeches = false;
                            tmpTxt.strNumber = checkTxts[j].strNumber;
                            tmpTxt.actorName = checkTxts[j].actorName;
                            tmpTxt.actorSpeechOriginal = checkTxts[j].actorSpeechOriginal;
                            tmpTxt.actorSpeechTranslation = checkTxts[j].actorSpeechTranslation;
                            tmpTxt.flags = checkTxts[j].flags;

                            duplicatedStrs.Add(tmpTxt);
                        }
                    }

                    if (hasDuplicate)
                    {
                        CommonText tmpTxt;
                        tmpTxt.isBothSpeeches = false;
                        tmpTxt.strNumber = checkTxts[i].strNumber;
                        tmpTxt.actorName = checkTxts[i].actorName;
                        tmpTxt.actorSpeechOriginal = checkTxts[i].actorSpeechOriginal;
                        tmpTxt.actorSpeechTranslation = checkTxts[i].actorSpeechTranslation;
                        tmpTxt.flags = checkTxts[i].flags;

                        duplicatedStrs.Add(tmpTxt);
                    }

                    ProcessorProgress(i + 1);
                }

                
                checkTxts.Clear();
                GC.Collect();
            }

            return duplicatedStrs;
        }

        private void checkDuplicatedStrsBtn_Click(object sender, EventArgs e)
        {
            bool tmpTSVFormat = MainMenu.settings.tsvFormat;
            List<CommonText> checkTxts;

            string filePath = firstPath.Text;
            string readyFilePath = readyPath.Text;

            if (singleFileRB.Checked)
            {
                if (File.Exists(filePath) && (readyFilePath != ""))
                {
                    checkTxts = CheckStrings(filePath);

                    if (checkTxts.Count > 0)
                    {
                        FileInfo fi = new FileInfo(readyFilePath);
                        MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";

                        if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(checkTxts, false, readyFilePath);
                        else Texts.SaveText.OldMethod(checkTxts, true, false, readyFilePath);

                        MessageBox.Show("File successfully saved!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please check paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if(severalFilesRB.Checked)
            {
                if (Directory.Exists(filePath) && Directory.Exists(readyFilePath))
                {
                    DirectoryInfo di = new DirectoryInfo(filePath);
                    FileInfo[] fi = di.GetFiles("*.*", SearchOption.AllDirectories);

                    progressBar2.Maximum = fi.Length;

                    for (int i = 0; i < fi.Length; i++)
                    {
                        if (fi[i].Extension.ToLower() == ".tsv" || fi[i].Extension.ToLower() == ".txt")
                        {
                            FileInfo tmpFI = new FileInfo(fi[i].FullName);
                            checkTxts = CheckStrings(fi[i].FullName);

                            if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(checkTxts, false, readyFilePath + "\\" + tmpFI.Name);
                            else Texts.SaveText.OldMethod(checkTxts, true, false, readyFilePath + "\\" + tmpFI.Name);
                        }

                        ProcessorProgress2(i + 1);
                    }
                }
                else
                {
                    MessageBox.Show("Please check paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            MainMenu.settings.tsvFormat = tmpTSVFormat;
        }

        private List<CommonText> MergeStrings(string originalPath, string translatePath)
        {
            List<CommonText> orStrings = Texts.ReadText.GetStrings(originalPath);
            List<CommonText> trStrings = Texts.ReadText.GetStrings(translatePath);

            progressBar1.Maximum = orStrings.Count;

            for (int i = 0; i < orStrings.Count; i++)
            {
                CommonText tmpTxt;
                tmpTxt.isBothSpeeches = orStrings[i].isBothSpeeches;
                tmpTxt.strNumber = orStrings[i].strNumber;
                tmpTxt.actorName = orStrings[i].actorName;
                tmpTxt.actorSpeechOriginal = orStrings[i].actorSpeechOriginal;
                tmpTxt.actorSpeechTranslation = orStrings[i].actorSpeechTranslation;
                tmpTxt.flags = orStrings[i].flags;

                for (int j = 0; j < trStrings.Count; j++)
                {
                    if ((orStrings[i].strNumber == trStrings[j].strNumber)
                        && (orStrings[i].actorName == trStrings[j].actorName))
                    {
                        tmpTxt.actorSpeechTranslation = trStrings[j].actorSpeechTranslation;
                        break;
                    }
                }

                orStrings[i] = tmpTxt;

                ProcessorProgress(i + 1);
            }

            if (sortStrsCB.Checked)
            {
                CommonTextClass tmp = new CommonTextClass();
                tmp.txtList = orStrings;

                tmp = Methods.SortString(tmp);

                orStrings = new List<CommonText>();

                for (int i = 0; i < tmp.txtList.Count; i++)
                {
                    orStrings.Add(tmp.txtList[i]);
                }

                tmp.txtList.Clear();
            }

            trStrings.Clear();

            return orStrings;
        }

        private void mergeBtn_Click(object sender, EventArgs e)
        {
            string originalPath = firstFilePath.Text;
            string translatePath = secondFilePath.Text;
            string readyPath = readyFilePath.Text;

            if(singleFileRB.Checked && File.Exists(originalPath)
                && File.Exists(translatePath) && Directory.Exists(readyPath))
            {
                bool tmpTSV = MainMenu.settings.tsvFormat;

                List<CommonText> result = MergeStrings(originalPath, translatePath);

                FileInfo fi = new FileInfo(originalPath);
                MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";
                string newPath = readyPath + "\\" + fi.Name.Remove(fi.Name.Length - fi.Extension.Length, fi.Extension.Length) + "_merged" + fi.Extension;

                if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(result, false, newPath);
                else Texts.SaveText.OldMethod(result, true, false, newPath);

                MainMenu.settings.tsvFormat = tmpTSV;
            }
            else if(severalFilesRB.Checked && Directory.Exists(originalPath)
                && Directory.Exists(translatePath))
            {
                DirectoryInfo originalDI = new DirectoryInfo(originalPath);
                DirectoryInfo translateDI = new DirectoryInfo(translatePath);

                FileInfo[] originalFI = originalDI.GetFiles("*.*", SearchOption.AllDirectories);
                FileInfo[] translateFI = translateDI.GetFiles("*.*", SearchOption.AllDirectories);

                progressBar2.Maximum = originalFI.Length;

                for(int i = 0; i < originalFI.Length; i++)
                {
                    for(int j = 0; j < translateFI.Length; j++)
                    {
                        if (translateFI[j].Name.IndexOf(originalFI[i].Name.Remove(originalFI[i].Name.Length - originalFI[i].Extension.Length, originalFI[i].Extension.Length)) == 0)
                        {
                            bool tmpTSV = MainMenu.settings.tsvFormat;

                            List<CommonText> result = MergeStrings(originalFI[i].FullName, translateFI[j].FullName);

                            FileInfo fi = new FileInfo(originalFI[i].FullName);
                            MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";
                            string newPath = readyPath + "\\" + fi.Name.Remove(fi.Name.Length - fi.Extension.Length, fi.Extension.Length) + "_merged" + fi.Extension;

                            if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(result, false, newPath);
                            else Texts.SaveText.OldMethod(result, true, false, newPath);

                            MainMenu.settings.tsvFormat = tmpTSV;
                        }
                    }

                    ProcessorProgress2(i + 1);
                }
            }
        }

        private List<CommonText> CheckNonTranslateStrs(string filePath)
        {
            List<CommonText> checkTxt = Texts.ReadText.GetStrings(filePath);
            CommonText tmpTxts;
            List<CommonText> nonTranslate = new List<CommonText>();

            progressBar1.Maximum = checkTxt.Count;

            string originalStr = "", translatedStr = "";

            for (int i = 0; i < checkTxt.Count; i++)
            {
                originalStr = Methods.DeleteCommentary(checkTxt[i].actorSpeechOriginal, "{", "}");
                originalStr = Methods.DeleteCommentary(originalStr, "[", "]");
                originalStr = Regex.Replace(originalStr, @"[^\w]", "");

                translatedStr = Methods.DeleteCommentary(checkTxt[i].actorSpeechTranslation, "{", "}");
                translatedStr = Methods.DeleteCommentary(translatedStr, "[", "]");
                translatedStr = Regex.Replace(translatedStr, @"[^\w]", "");

                if ((originalStr.ToLower() == translatedStr.ToLower()) && checkTxt[i].isBothSpeeches
                    && (originalStr != ""))
                {
                    tmpTxts.isBothSpeeches = checkTxt[i].isBothSpeeches;
                    tmpTxts.strNumber = checkTxt[i].strNumber;
                    tmpTxts.actorName = checkTxt[i].actorName;
                    tmpTxts.actorSpeechOriginal = checkTxt[i].actorSpeechOriginal;
                    tmpTxts.actorSpeechTranslation = checkTxt[i].actorSpeechTranslation;
                    tmpTxts.flags = checkTxt[i].flags;

                    nonTranslate.Add(tmpTxts);
                }

                ProcessorProgress(i + 1);
            }

            return nonTranslate;
        }

        //Check non-translated strings button in merge tab
        private void button4_Click(object sender, EventArgs e)
        {
            string filePath = firstPath.Text;
            string readyCheckedPath = readyPath.Text;

            if (singleFileRB.Checked)
            {
                if (File.Exists(filePath) && (readyCheckedPath != ""))
                {
                    List<CommonText> result = CheckNonTranslateStrs(filePath);

                    if (result.Count > 0)
                    {
                        FileInfo fi = new FileInfo(readyCheckedPath);

                        bool tmpTSV = MainMenu.settings.tsvFormat;
                        MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";

                        if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(result, false, fi.FullName);
                        else Texts.SaveText.OldMethod(result, true, false, fi.FullName);

                        MainMenu.settings.tsvFormat = tmpTSV;

                        MessageBox.Show("File successfully saved!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please check paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (Directory.Exists(filePath) && Directory.Exists(readyCheckedPath))
                {
                    DirectoryInfo di = new DirectoryInfo(filePath);
                    FileInfo[] fi = di.GetFiles("*.*", SearchOption.AllDirectories);
                    progressBar2.Maximum = fi.Length;

                    for (int i = 0; i < fi.Length; i++)
                    {
                        if (fi[i].Extension.ToLower() == ".txt" || fi[i].Extension.ToLower() == ".tsv")
                        {
                            List<CommonText> result = CheckNonTranslateStrs(fi[i].FullName);

                            if (result.Count > 0)
                            {
                                FileInfo tmpFi = new FileInfo(fi[i].FullName);

                                bool tmpTSV = MainMenu.settings.tsvFormat;
                                MainMenu.settings.tsvFormat = tmpFi.Extension.ToLower() == ".tsv";

                                string tmpFilePath = readyCheckedPath + "\\" + tmpFi.Name;

                                if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(result, false, tmpFilePath);
                                else Texts.SaveText.OldMethod(result, true, false, tmpFilePath);

                                MainMenu.settings.tsvFormat = tmpTSV;
                            }
                        }

                        ProcessorProgress2(i + 1);
                    }
                }
                else
                {
                    MessageBox.Show("Please check paths.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private List<CommonText> SortByOriginalStrs(List<CommonText> originalStrs, List<CommonText> translatedStrs)
        {
            List<CommonText> sortedStrs = new List<CommonText>();
            CommonText tmpText;

            string firstString = "", secondString = "";
            string firstActor = "", secondActor = "";

            progressBar1.Maximum = originalStrs.Count;

            for (int i = 0; i < originalStrs.Count; i++)
            {
                firstActor = originalStrs[i].actorName.ToUpper();
                firstString = originalStrs[i].actorSpeechOriginal.ToLower();
                firstString = Methods.DeleteCommentary(firstString, "{", "}");
                firstString = Methods.DeleteCommentary(firstString, "[", "]");
                firstString = Regex.Replace(firstString, @"[^\w]", "");

                for (int j = 0; j < translatedStrs.Count; j++)
                {
                    secondActor = translatedStrs[j].actorName.ToUpper();
                    secondString = translatedStrs[j].actorSpeechOriginal.ToLower();
                    secondString = Methods.DeleteCommentary(secondString, "{", "}");
                    secondString = Methods.DeleteCommentary(secondString, "[", "]");
                    secondString = Regex.Replace(secondString, @"[^\w]", "");

                    if((firstActor == secondActor) && (firstString == secondString))
                    {
                        tmpText.isBothSpeeches = true;
                        tmpText.actorName = originalStrs[i].actorName;
                        tmpText.actorSpeechOriginal = originalStrs[i].actorSpeechOriginal;
                        tmpText.actorSpeechTranslation = translatedStrs[j].actorSpeechTranslation;
                        tmpText.flags = translatedStrs[j].flags;
                        tmpText.strNumber = originalStrs[i].strNumber;

                        sortedStrs.Add(tmpText);
                        translatedStrs.RemoveAt(j);
                        break;
                    }
                }

                ProcessorProgress(i + 1);
            }

            return sortedStrs;
        }

        private void replaceDuplicatedStringsBtn_Click(object sender, EventArgs e)
        {
            string firstDuplicatedPath = firstDoubledFilePath.Text;
            string secondDuplicatedPath = secondDoubledFilePath.Text;
            string readyDuplicatedPath = readyDoubledFilePath.Text;

            if(singleFileRB.Checked && File.Exists(firstDuplicatedPath) && File.Exists(secondDuplicatedPath))
            { 
                List<CommonText> firstStrs = Texts.ReadText.GetStrings(firstDuplicatedPath);
                List<CommonText> secondStrs = Texts.ReadText.GetStrings(secondDuplicatedPath);

                List<CommonText> readyStrs = SortByOriginalStrs(firstStrs, secondStrs);

                if (sortDoubledCB.Checked)
                {
                    CommonTextClass tmpTxt = new CommonTextClass();
                    tmpTxt.txtList = new List<CommonText>();

                    for(int i = 0; i < readyStrs.Count; i++)
                    {
                        tmpTxt.txtList.Add(readyStrs[i]);
                    }

                    tmpTxt = Methods.SortString(tmpTxt);

                    readyStrs = new List<CommonText>();

                    for(int i = 0; i < tmpTxt.txtList.Count; i++)
                    {
                        readyStrs.Add(tmpTxt.txtList[i]);
                    }

                    tmpTxt = null;
                }

                if(readyStrs.Count > 0)
                {
                    FileInfo fi = new FileInfo(firstDuplicatedPath);
                    MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";
                    string newPath = readyDuplicatedPath + "\\" + fi.Name.Remove(fi.Name.Length - fi.Extension.Length, fi.Extension.Length) + "_replaced" + fi.Extension;

                    if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(readyStrs, false, newPath);
                    else Texts.SaveText.OldMethod(readyStrs, true, false, newPath);
                }
            }
            else if(severalFilesRB.Checked && Directory.Exists(firstDuplicatedPath) && Directory.Exists(secondDuplicatedPath))
            {
                DirectoryInfo firstDI = new DirectoryInfo(firstDuplicatedPath);
                DirectoryInfo secondDI = new DirectoryInfo(secondDuplicatedPath);
                FileInfo[] firstFI = firstDI.GetFiles("*.*", SearchOption.AllDirectories);
                FileInfo[] secondFI = secondDI.GetFiles("*.*", SearchOption.AllDirectories);

                for (int f = 0; f < firstFI.Length; f++)
                {
                    List<CommonText> firstStrs = Texts.ReadText.GetStrings(firstDuplicatedPath);
                    List<CommonText> secondStrs = Texts.ReadText.GetStrings(secondDuplicatedPath);

                    List<CommonText> readyStrs = SortByOriginalStrs(firstStrs, secondStrs);

                    if (sortDoubledCB.Checked)
                    {
                        CommonTextClass tmpTxt = new CommonTextClass();
                        tmpTxt.txtList = new List<CommonText>();

                        for (int i = 0; i < readyStrs.Count; i++)
                        {
                            tmpTxt.txtList.Add(readyStrs[i]);
                        }

                        tmpTxt = Methods.SortString(tmpTxt);

                        readyStrs = new List<CommonText>();

                        for (int i = 0; i < tmpTxt.txtList.Count; i++)
                        {
                            readyStrs.Add(tmpTxt.txtList[i]);
                        }

                        tmpTxt = null;
                    }

                    if (readyStrs.Count > 0)
                    {
                        FileInfo fi = new FileInfo(firstDuplicatedPath);
                        MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";
                        string newPath = readyDuplicatedPath + "\\" + fi.Name.Remove(fi.Name.Length - fi.Extension.Length, fi.Extension.Length) + "_replaced" + fi.Extension;

                        if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(readyStrs, false, newPath);
                        else Texts.SaveText.OldMethod(readyStrs, true, false, newPath);
                    }
                }
            }
        }

        private void tabPagesControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(tabPagesControl.SelectedIndex)
            {
                case 0:
                    groupBox1.Text = "Merge";
                    break;

                case 1:
                    groupBox1.Text = "Replace";
                    break;

                case 2:
                    groupBox1.Text = "Work with";
                    break;
            }
        }

        private void firstBrowseBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    firstPath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder("Set input directory with needed files");

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    firstPath.Text = selectedFolder;
                }
            }
        }

        private void readyBrowseBtn_Click(object sender, EventArgs e)
        {
            if (singleFileRB.Checked)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    readyPath.Text = ofd.FileName;
                }
            }
            else
            {
                string selectedFolder = SelectFolder("Set output directory for founded duplicated strings in file");

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    readyPath.Text = selectedFolder;
                }
            }
        }

        private void compareBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog firstOFD = new OpenFileDialog();
            OpenFileDialog secondOFD = new OpenFileDialog();
            SaveFileDialog SFD = new SaveFileDialog();

            firstOFD.Title = "Set first file with non-translated strings";
            secondOFD.Title = "Set second file with non-translated strings";
            SFD.Title = "Set path to save file with difference";

            firstOFD.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";
            secondOFD.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";
            SFD.Filter = "All supported files|*.txt;*.tsv|Text files (*.txt)|*.txt|TSV files (*.tsv)|*.tsv";

            if((firstOFD.ShowDialog() == DialogResult.OK) && (secondOFD.ShowDialog() == DialogResult.OK))
            {
                List<CommonText> firstStrs = Texts.ReadText.GetStrings(firstOFD.FileName);
                List<CommonText> secondStrs = Texts.ReadText.GetStrings(secondOFD.FileName);

                int i = 0;
                string firstStr = "", secondStr = "";
                string firstActorName = "", secondActorName = "";

                while(i < firstStrs.Count)
                {
                    firstStr = Methods.DeleteCommentary(firstStrs[i].actorSpeechOriginal, "[", "]");
                    firstStr = Methods.DeleteCommentary(firstStr, "{", "}");
                    firstStr = Regex.Replace(firstStr, @"[^\w]", "");
                    firstActorName = firstStrs[i].actorName.ToUpper();

                    for(int j = 0; j < secondStrs.Count; j++)
                    {
                        secondStr = Methods.DeleteCommentary(secondStrs[j].actorSpeechOriginal, "[", "]");
                        secondStr = Methods.DeleteCommentary(secondStr, "{", "}");
                        secondStr = Regex.Replace(secondStr, @"[^\w]", "");
                        secondActorName = secondStrs[j].actorName.ToUpper();

                        if((firstStr.ToLower() == secondStr.ToLower()) && (firstActorName == secondActorName))
                        {
                            firstStrs.RemoveAt(i);
                            secondStrs.RemoveAt(j);
                            i = 0;
                        }
                    }

                    i++;
                }

                if((firstStrs.Count > 0) || (secondStrs.Count > 0))
                {
                    SFD.FileName = firstOFD.FileName;

                    if(SFD.ShowDialog() == DialogResult.OK)
                    {
                        List<CommonText> newStrs = new List<CommonText>();

                        for(int f = 0; f < firstStrs.Count; f++)
                        {
                            newStrs.Add(firstStrs[f]);
                        }

                        for(int s = 0; s < secondStrs.Count; s++)
                        {
                            newStrs.Add(secondStrs[s]);
                        }

                        FileInfo fi = new FileInfo(SFD.FileName);

                        bool tmpTSV = MainMenu.settings.tsvFormat;
                        MainMenu.settings.tsvFormat = fi.Extension.ToLower() == ".tsv";

                        if (txtNewMethodRB.Checked && !MainMenu.settings.tsvFormat) Texts.SaveText.NewMethod(newStrs, false, fi.FullName);
                        else Texts.SaveText.OldMethod(newStrs, false, false, fi.FullName);

                        MainMenu.settings.tsvFormat = tmpTSV;

                        MessageBox.Show("File successfully saved.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("No difference in files", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
