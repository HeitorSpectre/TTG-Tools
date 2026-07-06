using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TTG_Tools.ClassesStructs.Text;
using System.Windows.Forms;

namespace TTG_Tools.Texts
{
    public class ReadText
    {
        private static int GetType(string FilePath)
        {
            FileInfo fi = new FileInfo(FilePath);

            if (fi.Extension.ToLower() == ".tsv") return 1;

            string[] strs = File.ReadAllLines(fi.FullName, Encoding.UTF8);
            if(strs.Length >= 5)
            {
                if ((strs[0].IndexOf("langid=") == 0) && (strs[1].IndexOf("actor=") == 0) && (strs[2].IndexOf("speechOriginal=") == 0) && (strs[3].IndexOf("speechTranslation=") == 0) && (strs[4].IndexOf("flags=") == 0))
                {
                    return 2;
                }
            }

            //Telltale Explorer Style: first line is an [id] header, followed by Category= and Speech=.
            if (strs.Length >= 3 && IsIdHeader(strs[0]) && strs[1].IndexOf("Category=") == 0 && strs[2].IndexOf("Speech=") == 0)
            {
                return 3;
            }

            return 0;
        }

        //A Telltale Explorer entry header looks like "[123456]" (only digits inside the brackets).
        private static bool IsIdHeader(string line)
        {
            if (line == null) return false;
            line = line.Trim();
            if (line.Length < 3 || line[0] != '[' || line[line.Length - 1] != ']') return false;
            return Methods.IsNumeric(line.Substring(1, line.Length - 2));
        }

        private static List<CommonText> TelltaleExplorerMode(string FilePath)
        {
            List<CommonText> txts = new List<CommonText>();
            FileInfo fi = new FileInfo(FilePath);

            try
            {
                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                CommonText cur = new CommonText();
                bool have = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (IsIdHeader(line))
                    {
                        if (have) txts.Add(cur);

                        string idText = line.Trim();
                        cur = new CommonText();
                        cur.strNumber = Convert.ToUInt32(idText.Substring(1, idText.Length - 2));
                        cur.actorName = "";
                        cur.actorSpeechOriginal = "";
                        cur.actorSpeechTranslation = "";
                        cur.flags = "";
                        cur.isBothSpeeches = false;
                        have = true;
                    }
                    else if (have && line.IndexOf("Category=") == 0)
                    {
                        cur.actorName = line.Substring("Category=".Length);
                    }
                    else if (have && line.IndexOf("Speech=") == 0)
                    {
                        string speech = line.Substring("Speech=".Length);
                        cur.actorSpeechOriginal = speech;
                        cur.actorSpeechTranslation = speech;
                    }
                }

                if (have) txts.Add(cur);

                return txts;
            }
            catch
            {
                MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + fi.Name, "Error");
                return null;
            }
        }

        private static List<CommonText> OldTextMode(string FilePath)
        {
            List<CommonText> txts = new List<CommonText>();
            FileInfo fi = new FileInfo(FilePath);
            FileStream fs = new FileStream(FilePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            try
            {
                CommonText tmpTxt;
                bool hasNumber = false;
                int prevNum = -1;
                int curNum = -1;
                string tmpString = "";
                int lastListIndex = -1;

                while (!sr.EndOfStream)
                {
                    tmpString = sr.ReadLine();

                    if (tmpString.IndexOf(")") > -1 && !hasNumber)
                    {
                        if(Methods.IsNumeric(tmpString.Substring(0, tmpString.IndexOf(")"))))
                        {
                            curNum = Convert.ToInt32(tmpString.Substring(0, tmpString.IndexOf(")")));
                            tmpTxt.isBothSpeeches = false;

                            if(curNum == prevNum)
                            {
                                lastListIndex = txts.Count - 1;

                                tmpTxt.strNumber = txts[lastListIndex].strNumber;
                                tmpTxt.isBothSpeeches = true;
                                tmpTxt.actorName = txts[lastListIndex].actorName;
                                tmpTxt.actorSpeechOriginal = txts[lastListIndex].actorSpeechOriginal;
                                tmpTxt.actorSpeechTranslation = "";
                                tmpTxt.flags = txts[lastListIndex].flags;
                                txts[lastListIndex] = tmpTxt;
                            }

                            int diff = 0;

                            try
                            {
                                tmpTxt.strNumber = (uint)curNum;
                                int numPos = tmpString.IndexOf(")") + 1;
                                if ((numPos < tmpString.Length) && (tmpString[numPos] == ' ')) numPos++;
                                diff = tmpString.Length - numPos;
                                tmpTxt.actorName = tmpString.Substring(numPos, diff);
                                tmpTxt.actorSpeechOriginal = "";
                                tmpTxt.actorSpeechTranslation = "";
                                tmpTxt.flags = "";

                                if (!tmpTxt.isBothSpeeches) txts.Add(tmpTxt);

                                hasNumber = true;
                                prevNum = curNum;
                                lastListIndex = txts.Count - 1;
                            }
                            catch
                            {
                                MessageBox.Show(Loc.T("Common.msgErrorInFile", "Error in file") + " " + fi.Name + ". String \"" + tmpString + "\". " + Loc.T("ReadText.msgDiffEndStart", "Difference between end and start is") + " " + Convert.ToString(diff), Loc.T("Common.error", "Error"));
                                if (sr != null) sr.Close();
                                if (fs != null) fs.Close();

                                return null;
                            }
                        }
                        else
                        {
                            tmpTxt.strNumber = txts[lastListIndex].strNumber;
                            tmpTxt.isBothSpeeches = txts[lastListIndex].isBothSpeeches;
                            tmpTxt.actorName = txts[lastListIndex].actorName;
                            tmpTxt.actorSpeechOriginal = txts[lastListIndex].actorSpeechOriginal;
                            tmpTxt.actorSpeechTranslation = txts[lastListIndex].actorSpeechTranslation;
                            tmpTxt.flags = txts[lastListIndex].flags;

                            if (!txts[lastListIndex].isBothSpeeches)
                            {
                                tmpTxt.actorSpeechOriginal += "\r\n" + tmpString;
                                tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechOriginal;
                            }
                            else
                            {
                                tmpTxt.actorSpeechTranslation += "\r\n" + tmpString;
                            }

                            txts[lastListIndex] = tmpTxt;

                            hasNumber = false;
                        }
                    }
                    else
                    {
                        tmpTxt.strNumber = txts[lastListIndex].strNumber;
                        tmpTxt.isBothSpeeches = txts[lastListIndex].isBothSpeeches;
                        tmpTxt.actorName = txts[lastListIndex].actorName;
                        tmpTxt.actorSpeechOriginal = txts[lastListIndex].actorSpeechOriginal;
                        tmpTxt.actorSpeechTranslation = txts[lastListIndex].actorSpeechTranslation;
                        tmpTxt.flags = txts[lastListIndex].flags;

                        if (!txts[lastListIndex].isBothSpeeches)
                        {
                            tmpTxt.actorSpeechOriginal += "\r\n" + tmpString;
                            tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechOriginal;
                        }
                        else
                        {
                            tmpTxt.actorSpeechTranslation += "\r\n" + tmpString;
                        }

                        txts[lastListIndex] = tmpTxt;

                        hasNumber = false;
                    }
                }

                sr.Close();
                fs.Close();

                return txts;
            }
            catch
            {
                if(sr != null) sr.Close();
                if(fs != null) fs.Close();

                return null;
            }
        }

        private static List<CommonText> TsvTextMode(string FilePath)
        {
            List<CommonText> txts = new List<CommonText>();
            FileInfo fi = new FileInfo(FilePath);
            FileStream fs = new FileStream(FilePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            try
            {
                string tmpString = "";
                string[] tmpStrs;
                CommonText tmpTxt;

                while(!sr.EndOfStream)
                {
                    tmpString = sr.ReadLine();
                    tmpStrs = tmpString.Split('\t');

                    if((tmpStrs.Length) > 0 && ((tmpStrs.Length == 3) || (tmpStrs.Length == 4)))
                    {
                        try
                        {
                            tmpTxt.strNumber = Convert.ToUInt32(tmpStrs[0]);
                            tmpTxt.isBothSpeeches = tmpStrs.Length == 4;
                            tmpTxt.actorName = tmpStrs[1];
                            tmpTxt.actorSpeechOriginal = tmpStrs[2];
                            tmpTxt.actorSpeechTranslation = tmpStrs.Length == 4 ? tmpStrs[3] : tmpTxt.actorSpeechOriginal;
                            tmpTxt.flags = "";

                            if (tmpTxt.actorSpeechOriginal.Contains("\\r")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\r", "\r");
                            if (tmpTxt.actorSpeechOriginal.Contains("\n")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\n", "\n");
                            if (tmpTxt.actorSpeechOriginal.Contains("\t")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\t", "\t");

                            if (tmpTxt.actorSpeechTranslation.Contains("\\r")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\r", "\r");
                            if (tmpTxt.actorSpeechTranslation.Contains("\n")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\n", "\n");
                            if (tmpTxt.actorSpeechTranslation.Contains("\t")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\t", "\t");

                            txts.Add(tmpTxt);
                        }
                        catch
                        {
                            MessageBox.Show(Loc.T("Common.msgErrorInFile", "Error in file") + " " + fi.Name + ". String \"" + tmpString + "\".", Loc.T("Common.error", "Error"));

                            if (sr != null) sr.Close();
                            if (fs != null) fs.Close();

                            return null;
                        }
                    }
                    else
                    {
                        MessageBox.Show(Loc.T("Common.msgErrorInFile", "Error in file") + " " + fi.Name + ". String \"" + tmpString + "\".", Loc.T("Common.error", "Error"));

                        if (sr != null) sr.Close();
                        if (fs != null) fs.Close();

                        return null;
                    }
                }

                sr.Close();
                fs.Close();

                return txts;
            }
            catch
            {
                if (sr != null) sr.Close();
                if (fs != null) fs.Close();

                return null;
            }
        }

        private static List<CommonText> NewTextMode(string FilePath)
        {
            List<CommonText> txts = new List<CommonText>();
            FileInfo fi = new FileInfo(FilePath);
            FileStream fs = new FileStream(FilePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            try
            {
                string tmpString = "";
                CommonText tmpTxt;

                while (!sr.EndOfStream)
                {
                    tmpTxt.strNumber = 0;
                    tmpTxt.isBothSpeeches = true;
                    tmpTxt.actorName = "";
                    tmpTxt.actorSpeechOriginal = "";
                    tmpTxt.actorSpeechTranslation = "";
                    tmpTxt.flags = "";

                    for(int i = 0; i < 5; i++)
                    {
                        tmpString = sr.ReadLine();

                        switch(i)
                        {
                            case 0:
                                if (tmpString.IndexOf("langid=") != 0) throw new Exception("File " + fi.Name + ". Error in string " + tmpString + ".");
                                try
                                {
                                    tmpString = tmpString.Substring(7, tmpString.Length - 7);
                                    tmpTxt.strNumber = Convert.ToUInt32(tmpString);
                                }
                                catch
                                {
                                    MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + FilePath);

                                    if (sr != null) sr.Close();
                                    if (fs != null) fs.Close();

                                    return null;
                                }
                                break;

                            case 1:
                                if (tmpString.IndexOf("actor=") != 0) throw new Exception("File " + fi.Name + ". Error in string " + tmpString + ".");

                                try
                                {
                                    tmpTxt.actorName = tmpString.Substring(6, tmpString.Length - 6);
                                }
                                catch
                                {
                                    MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + fi.Name);

                                    if (sr != null) sr.Close();
                                    if (fs != null) fs.Close();

                                    return null;
                                }
                                break;

                            case 2:
                                if (tmpString.IndexOf("speechOriginal=") != 0) throw new Exception("File " + fi.Name + ". Error in string " + tmpString + ".");

                                try
                                {
                                    tmpTxt.actorSpeechOriginal = tmpString.Substring(15, tmpString.Length - 15);
                                }
                                catch
                                {
                                    MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + FilePath);

                                    if (sr != null) sr.Close();
                                    if (fs != null) fs.Close();

                                    return null;
                                }
                                break;

                            case 3:
                                if (tmpString.IndexOf("speechTranslation=") != 0) throw new Exception("File " + fi.Name + ". Error in string " + tmpString + ".");

                                try
                                {
                                    tmpTxt.actorSpeechTranslation = tmpString.Substring(18, tmpString.Length - 18);
                                }
                                catch
                                {
                                    MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + FilePath);

                                    if (sr != null) sr.Close();
                                    if (fs != null) fs.Close();

                                    return null;
                                }
                                break;

                            case 4:
                                if (tmpString.IndexOf("flags=") != 0) throw new Exception("File " + fi.Name + ". Error in string " + tmpString + ".");

                                try
                                {
                                    tmpTxt.flags = tmpString.Substring(6, tmpString.Length - 6);
                                }
                                catch
                                {
                                    MessageBox.Show(Loc.T("Common.msgSomethingWrongFile", "Something wrong with file") + "\r\n" + FilePath);

                                    if (sr != null) sr.Close();
                                    if (fs != null) fs.Close();

                                    return null;
                                }
                                break;
                        }
                    }

                    txts.Add(tmpTxt);
                    tmpString = sr.ReadLine();
                }

                sr.Close();
                fs.Close();

                return txts;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");

                if (sr != null) sr.Close();
                if (fs != null) fs.Close();

                return null;
            }
        }

        public static List<CommonText> GetStrings(string FilePath)
        {
            List<CommonText> strings = new List<CommonText>();
            int type = GetType(FilePath);

            switch(type)
            {
                case 0:
                    strings = OldTextMode(FilePath);

                    for (int i = 0; i < strings.Count; i++)
                    {
                        CommonText tmpTxt = strings[i];
                        tmpTxt.actorSpeechOriginal = strings[i].actorSpeechOriginal.Substring(2, strings[i].actorSpeechOriginal.Length - 2);
                        tmpTxt.actorSpeechTranslation = strings[i].actorSpeechTranslation.Substring(2, strings[i].actorSpeechTranslation.Length - 2);

                        if (tmpTxt.actorSpeechOriginal.Contains("\r\n")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\r\n", "\n");
                        if (tmpTxt.actorSpeechTranslation.Contains("\r\n")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\r\n", "\n");

                        // Restore raw CR escaped by SaveText.OldMethod's
                        // EscapeForOldMethod ("\\r" -> 0x0D). Required for
                        // byte-identical roundtrip of CSI3 PS2 BMS3 langdb
                        // (whose speeches use 0x0D as internal separators).
                        if (tmpTxt.actorSpeechOriginal.Contains("\\r")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\r")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\r", "\r");

                        strings[i] = tmpTxt;
                    }
                    break;

                case 1:
                    strings = TsvTextMode(FilePath);

                    for(int i = 0; i < strings.Count; i++)
                    {
                        CommonText tmpTxt = strings[i];

                        if (tmpTxt.actorSpeechTranslation.Contains("\\t")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\r")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\n")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\n", "\n");

                        if (tmpTxt.actorSpeechOriginal.Contains("\\t")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\r")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\n")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\n", "\n");

                        strings[i] = tmpTxt;
                    }
                    break;

                case 2:
                    strings = NewTextMode(FilePath);

                    for (int i = 0; i < strings.Count; i++)
                    {
                        CommonText tmpTxt = strings[i];

                        if (tmpTxt.actorSpeechTranslation.Contains("\\t")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\r")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\n")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\n", "\n");

                        if (tmpTxt.actorSpeechOriginal.Contains("\\t")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\r")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\n")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\n", "\n");

                        strings[i] = tmpTxt;
                    }
                    break;

                case 3:
                    strings = TelltaleExplorerMode(FilePath);

                    for (int i = 0; i < strings.Count; i++)
                    {
                        CommonText tmpTxt = strings[i];

                        if (tmpTxt.actorSpeechTranslation.Contains("\\t")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\r")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechTranslation.Contains("\\n")) tmpTxt.actorSpeechTranslation = tmpTxt.actorSpeechTranslation.Replace("\\n", "\n");

                        if (tmpTxt.actorSpeechOriginal.Contains("\\t")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\t", "\t");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\r")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\r", "\r");
                        if (tmpTxt.actorSpeechOriginal.Contains("\\n")) tmpTxt.actorSpeechOriginal = tmpTxt.actorSpeechOriginal.Replace("\\n", "\n");

                        strings[i] = tmpTxt;
                    }
                    break;
            }

            return strings;
        }
    }
}
