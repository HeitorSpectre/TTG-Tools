using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TTG_Tools.ClassesStructs.Text;
using System.IO;

namespace TTG_Tools.Texts
{
    public class SaveText
    {
        // Issue #82: TSV exports couldn't be re-imported because the old escape
        // chain was a series of `else if` clauses, so a single speech could
        // only escape one of {CRLF, CR, LF, TAB}. A line containing both CRLF
        // and TAB came out with raw tabs in it, which made the TSV parser see
        // 5+ columns instead of 3/4 and reject the row. We now escape every
        // embedded control character every time, in a fixed order: CRLF first
        // (so the standalone CR / LF passes don't double-escape it), then
        // standalone CR, LF, TAB. The reader's existing `\\r`/`\\n`/`\\t`
        // unescape (ReadText.GetStrings) round-trips this back perfectly.
        private static string EscapeForOldMethod(string s, bool tsvFormat)
        {
            if (s == null) return string.Empty;

            if (tsvFormat)
            {
                s = s.Replace("\r\n", "\\r\\n");
                s = s.Replace("\r", "\\r");
                s = s.Replace("\n", "\\n");
                s = s.Replace("\t", "\\t");
                return s;
            }

            // Plain-text (non-TSV) format. CSI3 PS2 langdb speeches use a
            // raw 0x0D (Mac-classic CR) as their internal line separator;
            // older logic converted those into \r\n on write and the reader
            // then collapsed \r\n back to \n, so the speech came back with
            // 0x0A bytes where the original had 0x0D — not byte-identical.
            // We now escape standalone CR as the literal "\r" sequence so it
            // survives the read/write cycle. Standalone LF still gets
            // normalised to \r\n (preserves multi-line speech rendering).
            if (s.Contains("\r") && !s.Contains("\r\n"))
            {
                s = s.Replace("\r", "\\r");
            }
            else if (s.Contains("\n") && !s.Contains("\r\n"))
            {
                s = s.Replace("\n", "\r\n");
            }
            return s;
        }

        // Central format dispatcher used by every text export path. outputPathNoExt already
        // ends with a dot (e.g. "name."), so the extension is appended here based on settings.
        public static void SaveByFormat(List<CommonText> txt, bool isDoubledFile, bool isUnicode, string outputPathNoExt)
        {
            if (MainMenu.settings.telltaleExplorerFormat)
            {
                TelltaleExplorerMethod(txt, isUnicode, outputPathNoExt + "txt");
                return;
            }

            string outputPath = outputPathNoExt + (MainMenu.settings.tsvFormat ? "tsv" : "txt");

            if (MainMenu.settings.newTxtFormat) NewMethod(txt, isUnicode, outputPath);
            else OldMethod(txt, isDoubledFile, isUnicode, outputPath);
        }

        // Telltale Explorer Style format: one entry per string, delimited by an [id] line,
        // followed by "Category=" (actor) and "Speech=" (text). Control characters are escaped
        // (\r \n \t) so each speech stays on a single line; ReadText reverses this on import.
        public static void TelltaleExplorerMethod(List<CommonText> txt, bool isUnicode, string outputPath)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);

            FileStream fs = new FileStream(outputPath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            try
            {
                for (int i = 0; i < txt.Count; i++)
                {
                    sw.Write("[" + txt[i].strNumber + "]\r\n");
                    sw.Write("Category=" + txt[i].actorName + "\r\n");

                    string speech = txt[i].actorSpeechTranslation ?? "";
                    if (speech.Contains("\r")) speech = speech.Replace("\r", "\\r");
                    if (speech.Contains("\n")) speech = speech.Replace("\n", "\\n");
                    if (speech.Contains("\t")) speech = speech.Replace("\t", "\\t");

                    speech = isUnicode && MainMenu.settings.unicodeSettings == 1 ? Methods.ConvertString(speech, true) : speech;

                    sw.Write("Speech=" + speech + "\r\n");
                }

                sw.Close();
                fs.Close();
            }
            catch
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }
        }

        public static void OldMethod(List<CommonText> txt, bool isDoubledFile, bool isUnicode, string outputPath)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            FileStream fs = new FileStream(outputPath, FileMode.CreateNew);
            StreamWriter tw = new StreamWriter(fs, Encoding.UTF8);

            try
            {
                string tmpString = "";

                for (int i = 0; i < txt.Count; i++)
                {
                    tmpString = MainMenu.settings.tsvFormat ? txt[i].strNumber + "\t" + txt[i].actorName + "\t" : txt[i].strNumber + ") " + txt[i].actorName + "\r\n";
                    tw.Write(tmpString);
                    tmpString = EscapeForOldMethod(txt[i].actorSpeechOriginal, MainMenu.settings.tsvFormat);

                    tmpString = isUnicode && MainMenu.settings.unicodeSettings == 1 ? Methods.ConvertString(tmpString, true) : tmpString;

                    tw.Write(tmpString);

                    if (isDoubledFile)
                    {
                        tmpString = MainMenu.settings.tsvFormat ? "\t" : "\r\n";
                        tw.Write(tmpString);

                        if(!MainMenu.settings.tsvFormat)
                        {
                            tmpString = txt[i].strNumber + ") " + txt[i].actorName + "\r\n";
                            tw.Write(tmpString);
                        }

                        tmpString = EscapeForOldMethod(txt[i].actorSpeechTranslation, MainMenu.settings.tsvFormat);

                        tmpString = isUnicode && MainMenu.settings.unicodeSettings == 1 ? Methods.ConvertString(tmpString, true) : tmpString;

                        tw.Write(tmpString);
                    }

                    tmpString = "\r\n";
                    tw.Write(tmpString);
                }

                tw.Close();
                fs.Close();
            }
            catch
            {
                if (fs != null) fs.Close();
                if (tw != null) tw.Close();
            }
        }

        public static void NewMethod(List<CommonText> txt, bool isUnicode, string outputPath)
        {
            if(File.Exists(outputPath)) File.Delete(outputPath);

            FileStream fs = new FileStream(outputPath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            try
            {
                string tmpString = "";

                for(int i = 0; i < txt.Count; i++)
                {
                    tmpString = "langid=" + txt[i].strNumber + "\r\n";
                    sw.Write(tmpString);
                    
                    tmpString = "actor=" + txt[i].actorName + "\r\n";
                    sw.Write(tmpString);
                    
                    tmpString = "speechOriginal=" + txt[i].actorSpeechOriginal;
                    if (tmpString.Contains("\r")) tmpString = tmpString.Replace("\r", "\\r");
                    if (tmpString.Contains("\n")) tmpString = tmpString.Replace("\n", "\\n");
                    if (tmpString.Contains("\t")) tmpString = tmpString.Replace("\t", "\\t");
                    tmpString += "\r\n";

                    tmpString = isUnicode && MainMenu.settings.unicodeSettings == 1 ? Methods.ConvertString(tmpString, true) : tmpString;
                    sw.Write(tmpString);

                    tmpString = "speechTranslation=" + txt[i].actorSpeechTranslation;
                    if (tmpString.Contains("\r")) tmpString = tmpString.Replace("\r", "\\r");
                    if (tmpString.Contains("\n")) tmpString = tmpString.Replace("\n", "\\n");
                    if (tmpString.Contains("\t")) tmpString = tmpString.Replace("\t", "\\t");
                    tmpString += "\r\n";

                    tmpString = isUnicode && MainMenu.settings.unicodeSettings == 1 ? Methods.ConvertString(tmpString, true) : tmpString;
                    sw.Write(tmpString);

                    tmpString = "flags=" + txt[i].flags + "\r\n\r\n";
                    sw.Write(tmpString);
                }

                sw.Close();
                fs.Close();
            }
            catch
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }
        }
    }
}
