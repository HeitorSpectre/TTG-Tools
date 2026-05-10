using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TTG_Tools.ClassesStructs.Text;
using TTG_Tools.InEngineWords;
using System.Windows.Forms.VisualStyles;
using System.Security.Cryptography;

namespace TTG_Tools.Texts
{
    public class DlogWorker
    {
        private static DlogClass GetStringsFromDlog(BinaryReader br)
        {
            DlogClass dlog = new DlogClass();
            try
            {
                dlog.unknown1 = br.ReadInt32();
                dlog.unknown2 = br.ReadInt32();

                dlog.longUnknown1 = br.ReadUInt64();
                dlog.zero = br.ReadInt32();
                dlog.blockSize = br.ReadInt32();
                dlog.block = br.ReadBytes(dlog.blockSize - 4);
                dlog.blockFileNameSize = br.ReadInt32();
                dlog.fileNameSize = br.ReadInt32();
                byte[] tmp = br.ReadBytes(dlog.fileNameSize);
                dlog.dlogFileName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                dlog.someValue1 = br.ReadInt32();
                dlog.someValue2 = br.ReadInt32();
                dlog.longUnknown2 = br.ReadUInt64();
                dlog.someValue3 = br.ReadInt32();
                dlog.langdbBlockSize = br.ReadInt32();
                dlog.newLangdbBlockSize = 4;

                dlog.landb = new DlogLandb();

                dlog.landb.blockSize1 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                dlog.landb.someValue1 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;

                dlog.landb.blockSize2 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                dlog.landb.someValue2 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;

                dlog.landb.blockLength = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                dlog.landb.landbCount = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                dlog.landb.newBlockLength = 8;

                dlog.landb.landbs = new DlogLandbs[dlog.landb.landbCount];

                uint c = 1;

                for (int i = 0; i < dlog.landb.landbCount; i++)
                {
                    dlog.landb.landbs[i].anmID = br.ReadUInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].wavID = br.ReadUInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].zero = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].blockAnmNameSize = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].anmNameSize = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    tmp = br.ReadBytes(dlog.landb.landbs[i].anmNameSize);
                    dlog.newLangdbBlockSize += dlog.landb.landbs[i].anmNameSize;
                    dlog.landb.newBlockLength += dlog.landb.landbs[i].anmNameSize;
                    dlog.landb.landbs[i].anmName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                    dlog.landb.landbs[i].blockWavNameSize = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].wavNameSize = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    tmp = br.ReadBytes(dlog.landb.landbs[i].wavNameSize);
                    dlog.newLangdbBlockSize += dlog.landb.landbs[i].wavNameSize;
                    dlog.landb.newBlockLength += dlog.landb.landbs[i].wavNameSize;
                    dlog.landb.landbs[i].wavName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                    dlog.landb.landbs[i].blockLangresSize = br.ReadInt32();
                    //dlog.newLangdbBlockSize += 4;
                    //dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].langresStrsCount = br.ReadInt32();
                    //dlog.newLangdbBlockSize += 4;
                    //dlog.landb.newBlockLength += 4;

                    if ((dlog.landb.landbs[i].blockLangresSize > 8) && (dlog.landb.landbs[i].langresStrsCount > 0))
                    {
                        dlog.landb.landbs[i].lang = new LangresDB[dlog.landb.landbs[i].langresStrsCount];

                        for (int j = 0; j < dlog.landb.landbs[i].langresStrsCount; j++)
                        {                            
                            dlog.landb.landbs[i].lang[j].stringNumber = c;
                            dlog.landb.landbs[i].lang[j].blockActorNameSize = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            dlog.landb.landbs[i].lang[j].actorNameSize = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            tmp = br.ReadBytes(dlog.landb.landbs[i].lang[j].actorNameSize);
                            //Don't calculate actor name's size!
                            dlog.landb.landbs[i].lang[j].actorName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                            dlog.landb.landbs[i].lang[j].blockActorSpeechSize = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            dlog.landb.landbs[i].lang[j].actorSpeechSize = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            tmp = br.ReadBytes(dlog.landb.landbs[i].lang[j].actorSpeechSize);
                            //And don't caclulate actor speech's size!
                            dlog.landb.landbs[i].lang[j].actorSpeech = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                            dlog.landb.landbs[i].lang[j].someValue1 = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            dlog.landb.landbs[i].lang[j].someValue2 = br.ReadInt32();
                            //dlog.newLangdbBlockSize += 4;
                            //dlog.landb.newBlockLength += 4;

                            c++;
                        }
                    }

                    dlog.landb.landbs[i].someValue3 = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;

                    dlog.landb.landbs[i].someValue4 = br.ReadInt32();
                    dlog.newLangdbBlockSize += 4;
                    dlog.landb.newBlockLength += 4;
                }

                dlog.landb.commonBlockLen = br.ReadInt32();
                dlog.newLangdbBlockSize += dlog.landb.commonBlockLen;
                //dlog.landb.newBlockLength += dlog.landb.commonBlockLen;
                dlog.landb.block = br.ReadBytes(dlog.landb.commonBlockLen - 4);

                dlog.landb.lastLandbData = new DlogLastLandbData();
                dlog.landb.lastLandbData.Unknown1 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                //dlog.landb.newBlockLength += 4;

                dlog.landb.lastLandbData.Unknown2 = br.ReadInt32();
                dlog.newLangdbBlockSize += 4;
                //dlog.landb.newBlockLength += 4;

                dlog.landb.lastLandbData.BlockLen = br.ReadInt32();
                //dlog.newLangdbBlockSize += 4;
                //dlog.landb.newBlockLength += 4;

                dlog.landb.lastLandbData.Block = br.ReadBytes(dlog.landb.lastLandbData.BlockLen - 4);
                dlog.newLangdbBlockSize += dlog.landb.lastLandbData.BlockLen;
                //dlog.landb.newBlockLength += 4;

                long tmpPos = br.BaseStream.Position;
                dlog.someDlogData = br.ReadBytes((int)(br.BaseStream.Length - tmpPos));

                return dlog;
            }
            catch
            {
                return null;
            }
        }

        private static int RebuildDlog(BinaryReader br, string outputFile, DlogClass dlog)
        {
            if (File.Exists(outputFile)) File.Delete(outputFile);
            FileStream fs = new FileStream(outputFile, FileMode.CreateNew);
            BinaryWriter bw = new BinaryWriter(fs);

            try
            {
                byte[] header = br.ReadBytes(4);
                bw.Write(header);

                int count = br.ReadInt32();
                bw.Write(count);

                byte[] tmp;

                for(int i = 0; i < count; i++)
                {
                    tmp = br.ReadBytes(8);
                    bw.Write(tmp);
                    tmp = br.ReadBytes(4);
                    bw.Write(tmp);
                }

                bw.Write(dlog.unknown1);
                bw.Write(dlog.unknown2);

                bw.Write(dlog.longUnknown1);
                bw.Write(dlog.zero);
                bw.Write(dlog.blockSize);
                bw.Write(dlog.block);
                bw.Write(dlog.blockFileNameSize);
                bw.Write(dlog.fileNameSize);
                
                tmp = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(dlog.dlogFileName);

                bw.Write(tmp);

                bw.Write(dlog.someValue1);
                bw.Write(dlog.someValue2);
                bw.Write(dlog.longUnknown2);
                bw.Write(dlog.someValue3);

                var landbFilePos = bw.BaseStream.Position;

                bw.Write(dlog.langdbBlockSize);

                bw.Write(dlog.landb.blockSize1);
                bw.Write(dlog.landb.someValue1);
                bw.Write(dlog.landb.blockSize2);
                bw.Write(dlog.landb.someValue2);

                var blockSizePose = bw.BaseStream.Position;

                bw.Write(dlog.landb.blockLength);
                bw.Write(dlog.landb.landbCount);

                for(int i = 0; i < dlog.landb.landbCount; i++)
                {
                    bw.Write(dlog.landb.landbs[i].anmID);
                    bw.Write(dlog.landb.landbs[i].wavID);
                    bw.Write(dlog.landb.landbs[i].zero);

                    bw.Write(dlog.landb.landbs[i].blockAnmNameSize);
                    bw.Write(dlog.landb.landbs[i].anmNameSize);

                    tmp = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(dlog.landb.landbs[i].anmName);
                    bw.Write(tmp);

                    bw.Write(dlog.landb.landbs[i].blockWavNameSize);
                    bw.Write(dlog.landb.landbs[i].wavNameSize);

                    tmp = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(dlog.landb.landbs[i].wavName);
                    bw.Write(tmp);

                    var langresPos = bw.BaseStream.Position;

                    bw.Write(dlog.landb.landbs[i].blockLangresSize);
                    bw.Write(dlog.landb.landbs[i].langresStrsCount);

                    dlog.landb.landbs[i].blockLangresSize = 8;

                    for (int j = 0; j < dlog.landb.landbs[i].langresStrsCount; j++)
                    {
                        byte[] tmpActorName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(dlog.landb.landbs[i].lang[j].actorName);
                        byte[] tmpActorSpeech = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(dlog.landb.landbs[i].lang[j].actorSpeech);

                        dlog.landb.landbs[i].lang[j].actorNameSize = tmpActorName.Length;
                        dlog.landb.landbs[i].lang[j].blockActorNameSize = tmpActorName.Length + 8;
                        dlog.landb.landbs[i].blockLangresSize += dlog.landb.landbs[i].lang[j].blockActorNameSize;

                        dlog.landb.landbs[i].lang[j].actorSpeechSize = tmpActorSpeech.Length;
                        dlog.landb.landbs[i].lang[j].blockActorSpeechSize = tmpActorSpeech.Length + 8;
                        dlog.landb.landbs[i].blockLangresSize += dlog.landb.landbs[i].lang[j].blockActorSpeechSize;

                        bw.Write(dlog.landb.landbs[i].lang[j].blockActorNameSize);
                        bw.Write(dlog.landb.landbs[i].lang[j].actorNameSize);
                        bw.Write(tmpActorName);

                        bw.Write(dlog.landb.landbs[i].lang[j].blockActorSpeechSize);
                        bw.Write(dlog.landb.landbs[i].lang[j].actorSpeechSize);
                        bw.Write(tmpActorSpeech);

                        bw.Write(dlog.landb.landbs[i].lang[j].someValue1);
                        bw.Write(dlog.landb.landbs[i].lang[j].someValue2);

                        dlog.landb.landbs[i].blockLangresSize += 8;
                    }

                    bw.Write(dlog.landb.landbs[i].someValue3);
                    bw.Write(dlog.landb.landbs[i].someValue4);

                    var tmpPos = bw.BaseStream.Position;

                    bw.BaseStream.Seek(langresPos, SeekOrigin.Begin);
                    bw.Write(dlog.landb.landbs[i].blockLangresSize);

                    bw.BaseStream.Seek(tmpPos, SeekOrigin.Begin);

                    dlog.newLangdbBlockSize += dlog.landb.landbs[i].blockLangresSize;
                    dlog.landb.newBlockLength += dlog.landb.landbs[i].blockLangresSize;
                }

                bw.Write(dlog.landb.commonBlockLen);
                bw.Write(dlog.landb.block);

                bw.Write(dlog.landb.lastLandbData.Unknown1);
                bw.Write(dlog.landb.lastLandbData.Unknown2);
                bw.Write(dlog.landb.lastLandbData.BlockLen);
                bw.Write(dlog.landb.lastLandbData.Block);

                bw.Write(dlog.someDlogData);

                bw.BaseStream.Seek(landbFilePos, SeekOrigin.Begin);
                bw.Write(dlog.newLangdbBlockSize);

                bw.BaseStream.Seek(blockSizePose, SeekOrigin.Begin);
                bw.Write(dlog.landb.newBlockLength);

                bw.Close();
                fs.Close();

                return 0;
            }
            catch
            {
                if (bw != null) bw.Close();
                if (fs != null) fs.Close();

                return -1;
            }
        }

        private static int CheckNumbers(List<CommonText> txts, DlogClass dlog)
        {
            int result = -1;
            int countLangres = 0;
            int countStrings = 0;

            for (int i = 0; i < dlog.landb.landbCount; i++)
            {
                for (int k = 0; k < dlog.landb.landbs[i].langresStrsCount; k++) 
                {
                    for (int j = 0; j < txts.Count; j++)
                    {
                        if (dlog.landb.landbs[i].anmID == txts[j].strNumber) countLangres++;
                        else if (dlog.landb.landbs[i].lang[k].stringNumber == txts[j].strNumber) countStrings++;
                    }
                }
            }

            if (countLangres < countStrings) result = 0;
            else if (countLangres > countStrings) result = 1;

            return result;
        }

        private static DlogClass ReplaceStrings(DlogClass dlog, List<CommonText> commonTexts)
        {
            int index = -1;
            for (int i = 0; i < dlog.landb.landbCount; i++)
            {
                for (int j = 0; j < dlog.landb.landbs[i].langresStrsCount; j++)
                {
                    index = -1;
                    if (MainMenu.settings.importingOfName)
                    {
                        index = Methods.GetIndex(commonTexts, dlog.landb.landbs[i].lang[j].stringNumber);
                        if(index != -1) dlog.landb.landbs[i].lang[j].actorName = commonTexts[index].actorName;
                    }

                    index = Methods.GetIndex(commonTexts, dlog.landb.landbs[i].lang[j].stringNumber);
                    if(index != -1) dlog.landb.landbs[i].lang[j].actorSpeech = commonTexts[index].actorSpeechTranslation;
                }
            }

            return dlog;
        }

        public static string DoWork(string InputFile, string TxtFile, bool extract, ref byte[] EncKey, ref int version)
        {
            string result = "";

            FileInfo fi = new FileInfo(InputFile);
            byte[] buffer = File.ReadAllBytes(fi.FullName);

            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);

            string additionalMessage = "";

            try
            {
                byte[] header = br.ReadBytes(4);

                if (Encoding.ASCII.GetString(header) != "ERTM") //Supposed this dlog file encrypted
                {
                    //First trying decrypt probably encrypted dlog
                    try
                    {
                        string info = Methods.FindLangresDecryptKey(buffer, ref EncKey, ref version);

                        if ((info != null) && (info != "OK"))
                        {
                            additionalMessage = "Dlog file was encrypted, but I decrypted. " + info;
                        }
                    }
                    catch
                    {
                        result = "Maybe that LANGDB file encrypted. Try to decrypt first: " + fi.Name;

                        return result;
                    }
                }

                int count = br.ReadInt32();

                byte[] tmp;
                int val;

                bool hasLandb = false;

                for(int i = 0; i < count; i++)
                {
                    tmp = br.ReadBytes(8);
                    val = br.ReadInt32();

                    if(BitConverter.ToString(tmp) == BitConverter.ToString(BitConverter.GetBytes(CRCs.CRC64(0, ClassStructsNames.languagedbClass.ToLower())))) hasLandb = true;
                }

                if(!hasLandb)
                {
                    br.Close();
                    ms.Close();

                    return "File " + fi.Name + " doesn't have langdb strings.";
                }

                DlogClass dlog = GetStringsFromDlog(br);

                br.Close();
                ms.Close();

                if(dlog == null)
                {
                    return "File " + fi.Name + ": unknown error.";
                }

                if((dlog != null) && (dlog.landb.landbCount == 0))
                {
                    dlog = null;
                    GC.Collect();
                    return fi.Name + " is EMPTY.";
                }

                if(extract)
                {
                    ClassesStructs.Text.CommonTextClass txts = new CommonTextClass();

                    txts.txtList = new List<CommonText>();

                    for (int i = 0; i < dlog.landb.landbCount; i++)
                    {
                        ClassesStructs.Text.CommonText txt;

                        txt.isBothSpeeches = true;

                        for (int j = 0; j < dlog.landb.landbs[i].langresStrsCount; j++)
                        {
                            txt.strNumber = dlog.landb.landbs[i].lang[j].stringNumber;
                            txt.actorName = dlog.landb.landbs[i].lang[j].actorName;
                            txt.actorSpeechOriginal = dlog.landb.landbs[i].lang[j].actorSpeech;
                            txt.actorSpeechTranslation = dlog.landb.landbs[i].lang[j].actorSpeech;
                            txt.flags = "000"; //default will be 000

                            if(((txt.actorSpeechOriginal == "") && !MainMenu.settings.ignoreEmptyStrings)
                                || (txt.actorSpeechOriginal != "")) txts.txtList.Add(txt);
                        }
                    }

                    if (MainMenu.settings.sortSameString) txts = Methods.SortString(txts);

                    string outputFile = MainMenu.settings.pathForOutputFolder + Path.DirectorySeparatorChar + fi.Name.Remove(fi.Name.Length - 4, 4);
                    outputFile += MainMenu.settings.tsvFormat ? "tsv" : "txt";

                    switch(MainMenu.settings.newTxtFormat)
                    {
                        case true:
                            Texts.SaveText.NewMethod(txts.txtList, false, outputFile);
                            break;

                        default:
                            Texts.SaveText.OldMethod(txts.txtList, false, false, outputFile);
                            break;
                    }

                    txts.txtList.Clear();
                    txts = null;

                    result = fi.Name + " successfully extracted.";
                }
                else
                {
                    ClassesStructs.Text.CommonTextClass txts = new CommonTextClass();
                    txts.txtList = ReadText.GetStrings(TxtFile);

                    int type = CheckNumbers(txts.txtList, dlog);

                    switch(type)
                    {
                        case -1:
                            return "I don't know which type of number strings select for " + fi.Name + " file.";

                        case 1:
                            return "Error in " + fi.Name + ": Please replace strings without langres numbers! You can disable export real ID and extract strings again.";
                    }

                    dlog = ReplaceStrings(dlog, txts.txtList);

                    ms = new MemoryStream(buffer);
                    br = new BinaryReader(ms);

                    string outputFile = MainMenu.settings.pathForOutputFolder + "\\" + fi.Name;

                    int rebuildResult = RebuildDlog(br, outputFile, dlog);

                    br.Close();
                    ms.Close();

                    result = "File " + fi.Name + " successfully imported.";

                    if (rebuildResult == -1)
                    {
                        result = "Unknown error while rebuild file " + fi.Name;
                    }

                    dlog = null;
                    buffer = null;

                    if ((EncKey != null) || MainMenu.settings.encLangdb)
                    {
                        buffer = File.ReadAllBytes(outputFile);

                        if ((EncKey != null) && !MainMenu.settings.encLangdb)
                        {
                            if (Methods.meta_crypt(buffer, EncKey, version, false) != 0)
                            {
                                File.WriteAllBytes(outputFile, buffer);
                                result += " Successfull encrypted back!";
                            }
                        }
                        else if (MainMenu.settings.encLangdb)
                        {
                            byte[] key = new byte[MainMenu.gamelist[MainMenu.settings.encKeyIndex].key.Length];
                            Array.Copy(MainMenu.gamelist[MainMenu.settings.encKeyIndex].key, 0, key, 0, key.Length);

                            if (MainMenu.settings.customKey)
                            {
                                key = Methods.stringToKey(MainMenu.settings.encCustomKey);
                            }

                            version = MainMenu.settings.versionEnc == 0 ? 2 : 7;

                            if (Methods.meta_crypt(buffer, key, version, false) != 0)
                            {
                                File.WriteAllBytes(outputFile, buffer);
                                result += " Successfull encrypted!";
                            }
                        }
                    }
                }
            }
            catch
            {
                if (br != null) br.Close();
                if (ms != null) ms.Close();

                result = "Something wrong with file " + fi.Name + ".";
            }

            GC.Collect();
            return result;
        }
    }
}
