using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTG_Tools.ClassesStructs.Text;
using System.IO;
using System.Windows.Forms.VisualStyles;
using System.Security.Cryptography;

namespace TTG_Tools.Texts
{
    public class LandbWorker
    {
        private static LandbClass GetStringsFromLandb(BinaryReader br, bool hasCRC64Langres, bool newFormat, bool isUnicode)
        {
            LandbClass landb = new LandbClass();

            try
            { 
                landb.isNewFormat = newFormat;
                landb.hasMetaLangresName = hasCRC64Langres;
                landb.isUnicode = isUnicode;

                landb.newBlockLength = 0;
                landb.newLandbFileSize = 0;
                landb.newLandbLastFileSize = 0;

                if(landb.isNewFormat)
                {
                    var pos = br.BaseStream.Position;
                    br.BaseStream.Seek(4, SeekOrigin.Begin);
                    landb.landbFileSize = br.ReadInt32();
                    landb.landbLastFileSize = br.ReadInt32();
                    br.BaseStream.Seek(pos, SeekOrigin.Begin);
                }

                landb.blockSize1 = br.ReadInt32();
                landb.newLandbFileSize += 4;
                landb.someValue1 = br.ReadInt32();
                landb.newLandbFileSize += 4;
                landb.blockSize2 = br.ReadInt32();
                landb.newLandbFileSize += 4;
                landb.someValue2 = br.ReadInt32();
                landb.newLandbFileSize += 4;

                landb.blockLength = br.ReadInt32();
                landb.newLandbFileSize += 4;
                landb.landbCount = br.ReadInt32();
                landb.newLandbFileSize += 4;
                landb.newBlockLength = 8;

                landb.landbs = new Landb[landb.landbCount];
                landb.flags = new ClassesStructs.FlagsClass.LangdbFlagClass[landb.landbCount];

                byte[] tmp = null;                

                for (int i = 0; i < landb.landbCount; i++)
                {
                    landb.landbs[i].stringNumber = (uint)(i + 1);
                    landb.landbs[i].wavID = br.ReadUInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    if (landb.hasMetaLangresName)
                    {
                        landb.landbs[i].crc64Langres = br.ReadUInt64();
                        landb.newLandbFileSize += 8;
                        landb.newBlockLength += 8;
                    }

                    landb.landbs[i].anmID = br.ReadUInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].zero1 = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].blockAnmNameSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    if (landb.hasMetaLangresName)
                    {
                        tmp = br.ReadBytes(8);
                        landb.newLandbFileSize += 8;
                        landb.newBlockLength += 8;

                        landb.landbs[i].anmNameSize = 8;

                        landb.landbs[i].anmName = BitConverter.ToString(tmp);
                    }
                    else
                    {
                        landb.landbs[i].anmNameSize = br.ReadInt32();
                        landb.newLandbFileSize += 4;
                        landb.newBlockLength += 4;

                        tmp = br.ReadBytes(landb.landbs[i].anmNameSize);
                        landb.newLandbFileSize += landb.landbs[i].anmNameSize;
                        landb.newBlockLength += landb.landbs[i].anmNameSize;

                        landb.landbs[i].anmName = Methods.DecodeGameText(tmp, false);
                    }

                    landb.landbs[i].blockWavNameSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    if (landb.hasMetaLangresName)
                    {
                        tmp = br.ReadBytes(8);
                        landb.newLandbFileSize += 8;
                        landb.newBlockLength += 8;

                        landb.landbs[i].wavNameSize = 8;
                        landb.landbs[i].wavName = BitConverter.ToString(tmp);
                    }
                    else
                    {
                        landb.landbs[i].wavNameSize = br.ReadInt32();
                        landb.newLandbFileSize += 4;
                        landb.newBlockLength += 4;

                        tmp = br.ReadBytes(landb.landbs[i].wavNameSize);
                        landb.newLandbFileSize += landb.landbs[i].wavNameSize;
                        landb.newBlockLength += landb.landbs[i].wavNameSize;

                        landb.landbs[i].wavName = Methods.DecodeGameText(tmp, false);
                    }

                    landb.landbs[i].blockUnknownNameSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].unknownNameSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    tmp = br.ReadBytes(landb.landbs[i].unknownNameSize);
                    landb.newLandbFileSize += landb.landbs[i].unknownNameSize;
                    landb.newBlockLength += landb.landbs[i].unknownNameSize;

                    landb.landbs[i].unknownName = Methods.DecodeGameText(tmp, false);

                    landb.landbs[i].zero2 = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].blockLangresSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].blockActorNameSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    //Don't calculate new size with actor name!
                    landb.landbs[i].actorNameSize = br.ReadInt32();
                    tmp = br.ReadBytes(landb.landbs[i].actorNameSize);
                    landb.landbs[i].actorName = Methods.DecodeGameText(tmp, landb.isUnicode);
                    if (MainMenu.settings.supportTwdNintendoSwitch && landb.isUnicode && (MainMenu.settings.unicodeSettings == 2))
                    {
                        landb.landbs[i].actorName = Methods.isUTF8String(tmp)
                            ? Encoding.UTF8.GetString(tmp)
                            : Methods.DecodeGameText(tmp, false);
                    }

                    landb.landbs[i].blockActorSpeechSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].actorSpeechSize = br.ReadInt32();
                    tmp = br.ReadBytes(landb.landbs[i].actorSpeechSize);
                    landb.landbs[i].actorSpeech = Methods.DecodeGameText(tmp, landb.isUnicode);
                    if (MainMenu.settings.supportTwdNintendoSwitch && landb.isUnicode && (MainMenu.settings.unicodeSettings == 2))
                    {
                        landb.landbs[i].actorSpeech = Methods.isUTF8String(tmp)
                            ? Encoding.UTF8.GetString(tmp)
                            : Methods.DecodeGameText(tmp, false);
                    }

                    //Don't calculate actor speech's size!
                    landb.landbs[i].blockSize = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    landb.landbs[i].someValue = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    if (landb.isUnicode)
                    {
                        landb.landbs[i].blockSizeUni = br.ReadInt32();
                        landb.newLandbFileSize += 4;
                        landb.newBlockLength += 4;

                        landb.landbs[i].someDataUni = br.ReadBytes(landb.landbs[i].blockSizeUni - 4);
                        landb.newLandbFileSize += landb.landbs[i].someDataUni.Length;
                        landb.newBlockLength += landb.landbs[i].someDataUni.Length;
                    }

                    landb.landbs[i].flags = br.ReadInt32();
                    landb.newLandbFileSize += 4;
                    landb.newBlockLength += 4;

                    tmp = Encoding.ASCII.GetBytes(Convert.ToString(landb.landbs[i].flags, 2).PadLeft(8, '0'));

                    landb.flags[i] = new ClassesStructs.FlagsClass.LangdbFlagClass();
                    landb.flags[i].flags = new byte[8];

                    int maxSize = tmp.Length < 8 ? tmp.Length : 8;

                    for(int f = 0; f < 8; f++)
                    {
                        landb.flags[i].flags[f] = Convert.ToByte('0');
                    }
                    
                    for(int f = 0; f < maxSize; f++)
                    {
                        landb.flags[i].flags[f] = tmp[f];
                    }

                    tmp = null;
                }

                landb.commonSomeDataLen = br.ReadInt32();
                landb.someData = br.ReadBytes(landb.commonSomeDataLen - 4);
                landb.newLandbFileSize += landb.commonSomeDataLen;

                landb.lastLandbData = new LastLandbData();
                landb.lastLandbData.Unknown1 = br.ReadInt32();
                landb.lastLandbData.Unknown2 = br.ReadInt32();
                landb.lastLandbData.Unknown3 = br.ReadInt32();
                landb.lastLandbData.Unknown4 = br.ReadInt32();
                landb.newLandbFileSize += 16;

                if (landb.isNewFormat) landb.lastNewBlockData = br.ReadBytes(landb.landbLastFileSize);
            }
            catch
            {
                return null;
            }

            return landb;
        }

        private static int RebuildLandb(BinaryReader br, string outputFile, LandbClass landb, string inputFileName, bool openingCreditsReplacementMode)
        {
            if (File.Exists(outputFile)) File.Delete(outputFile);

            FileStream fs = new FileStream(outputFile, FileMode.CreateNew);
            BinaryWriter bw = new BinaryWriter(fs);

            try
            {
                byte[] header = br.ReadBytes(4);
                bw.Write(header);

                byte[] tmp;

                if (landb.isNewFormat)
                {
                    tmp = br.ReadBytes(4);
                    bw.Write(landb.newBlockLength);

                    tmp = br.ReadBytes(4);
                    bw.Write(landb.landbLastFileSize);

                    tmp = br.ReadBytes(4);
                    bw.Write(tmp);
                }

                int count = br.ReadInt32();
                bw.Write(count);

                for(int i = 0; i < count; i++)
                {
                    tmp = br.ReadBytes(8);
                    bw.Write(tmp);

                    tmp = br.ReadBytes(4);
                    bw.Write(tmp);
                }

                bw.Write(landb.blockSize1);
                bw.Write(landb.someValue1);

                bw.Write(landb.blockSize2);
                bw.Write(landb.someValue2);

                var pos = bw.BaseStream.Position;

                bw.Write(landb.newBlockLength);
                bw.Write(landb.landbCount);

                for(int i = 0; i < landb.landbCount; i++)
                {
                    bw.Write(landb.landbs[i].wavID);

                    if(landb.hasMetaLangresName)
                    {
                        bw.Write(landb.landbs[i].crc64Langres);
                    }

                    bw.Write(landb.landbs[i].anmID);
                    bw.Write(landb.landbs[i].zero1);

                    bw.Write(landb.landbs[i].blockAnmNameSize);

                    if (landb.hasMetaLangresName)
                    {
                        string[] tmpStr = landb.landbs[i].anmName.Split('-');
                        tmp = new byte[tmpStr.Length];

                        for(int t = 0; t < tmp.Length; t++)
                        {
                            tmp[t] = Convert.ToByte(tmpStr[t], 16);
                        }
                    }
                    else
                    {
                        bw.Write(landb.landbs[i].anmNameSize);
                        tmp = Methods.EncodeGameText(landb.landbs[i].anmName, false);
                    }

                    bw.Write(tmp);

                    bw.Write(landb.landbs[i].blockWavNameSize);

                    if (landb.hasMetaLangresName)
                    {
                        string[] tmpStr = landb.landbs[i].wavName.Split('-');
                        tmp = new byte[tmpStr.Length];

                        for (int t = 0; t < tmp.Length; t++)
                        {
                            tmp[t] = Convert.ToByte(tmpStr[t], 16);
                        }
                    }
                    else
                    {
                        bw.Write(landb.landbs[i].wavNameSize);
                        tmp = Methods.EncodeGameText(landb.landbs[i].wavName, false);
                    }

                    bw.Write(tmp);

                    bw.Write(landb.landbs[i].blockUnknownNameSize);
                    bw.Write(landb.landbs[i].unknownNameSize);

                    tmp = Methods.EncodeGameText(landb.landbs[i].unknownName, false);
                    bw.Write(tmp);

                    bw.Write(landb.landbs[i].zero2);

                    byte[] tmpActorName = Methods.EncodeGameText(landb.landbs[i].actorName, landb.isUnicode);
                    if (MainMenu.settings.supportTwdNintendoSwitch && landb.isUnicode && (MainMenu.settings.unicodeSettings == 2))
                    {
                        bool useUtf8ForActorName = !Methods.IsTextRepresentableInActiveEncoding(landb.landbs[i].actorName)
                            || Methods.ShouldForceUtf8ForLandbString(inputFileName, landb.landbs[i].actorName, openingCreditsReplacementMode);
                        tmpActorName = Methods.EncodeGameText(landb.landbs[i].actorName, useUtf8ForActorName);
                    }
                    landb.landbs[i].actorNameSize = tmpActorName.Length;
                    landb.landbs[i].blockActorNameSize = landb.landbs[i].actorNameSize + 8;
                    landb.newBlockLength += 4 + landb.landbs[i].actorNameSize;
                    landb.newLandbFileSize += 4 + landb.landbs[i].actorNameSize;

                    byte[] tmpActorSpeech = Methods.EncodeGameText(landb.landbs[i].actorSpeech, landb.isUnicode);
                    if (MainMenu.settings.supportTwdNintendoSwitch && landb.isUnicode && (MainMenu.settings.unicodeSettings == 2))
                    {
                        string speechText = landb.landbs[i].actorSpeech;
                        bool endsUtf8Marker = (speechText.IndexOf("(utf8)") > 0) && (speechText.IndexOf("(utf8)") == speechText.Length - 6);
                        bool endsUtf8cMarker = (speechText.IndexOf("(utf8c)") > 0) && (speechText.IndexOf("(utf8c)") == speechText.Length - 7);

                        if (endsUtf8Marker)
                        {
                            speechText = speechText.Remove(speechText.Length - 6, 6);
                            tmpActorSpeech = Encoding.UTF8.GetBytes(speechText);
                        }
                        else if (endsUtf8cMarker)
                        {
                            speechText = speechText.Remove(speechText.Length - 7, 7);
                            speechText = Methods.ConvertString(speechText, false);
                            tmpActorSpeech = Encoding.UTF8.GetBytes(speechText);
                        }
                        else
                        {
                            bool useUtf8ForSpeech = !Methods.IsTextRepresentableInActiveEncoding(speechText)
                                || Methods.ShouldForceUtf8ForLandbString(inputFileName, speechText, openingCreditsReplacementMode);
                            tmpActorSpeech = Methods.EncodeGameText(speechText, useUtf8ForSpeech);
                        }
                    }
                    landb.landbs[i].actorSpeechSize = tmpActorSpeech.Length;
                    landb.landbs[i].blockActorSpeechSize = landb.landbs[i].actorSpeechSize + 8;
                    landb.newBlockLength += 4 + landb.landbs[i].actorSpeechSize;
                    landb.newLandbFileSize += 4 + landb.landbs[i].actorSpeechSize;

                    landb.landbs[i].blockLangresSize = 4 + landb.landbs[i].blockActorNameSize + landb.landbs[i].blockActorSpeechSize + landb.landbs[i].blockSize;

                    bw.Write(landb.landbs[i].blockLangresSize);
                    bw.Write(landb.landbs[i].blockActorNameSize);
                    bw.Write(landb.landbs[i].actorNameSize);
                    bw.Write(tmpActorName);
                    bw.Write(landb.landbs[i].blockActorSpeechSize);
                    bw.Write(landb.landbs[i].actorSpeechSize);
                    bw.Write(tmpActorSpeech);

                    bw.Write(landb.landbs[i].blockSize);
                    bw.Write(landb.landbs[i].someValue);

                    if(landb.isUnicode)
                    {
                        bw.Write(landb.landbs[i].blockSizeUni);
                        bw.Write(landb.landbs[i].someDataUni);
                    }

                    bw.Write(landb.landbs[i].flags);
                }

                bw.Write(landb.commonSomeDataLen);
                bw.Write(landb.someData);

                bw.Write(landb.lastLandbData.Unknown1);
                bw.Write(landb.lastLandbData.Unknown2);
                bw.Write(landb.lastLandbData.Unknown3);
                bw.Write(landb.lastLandbData.Unknown4);

                if (landb.isNewFormat) bw.Write(landb.lastNewBlockData);

                if (landb.isNewFormat)
                {
                    bw.BaseStream.Seek(4, SeekOrigin.Begin);
                    bw.Write(landb.newLandbFileSize);
                }

                bw.BaseStream.Seek(pos, SeekOrigin.Begin);
                bw.Write(landb.newBlockLength);

                return 0;
            }
            catch
            {
                if (bw != null) bw.Close();
                if (fs != null) fs.Close();
                return -1;
            }
        }

        private static int CheckNumbers(List<CommonText> txts, LandbClass landb)
        {
            int result = -1;
            int countLangres = 0;
            int countStrings = 0;

            for(int i = 0; i < landb.landbCount; i++)
            {
                for(int j = 0; j < txts.Count; j++)
                {
                    if (landb.landbs[i].anmID == txts[j].strNumber) countLangres++;
                    if (landb.landbs[i].stringNumber == txts[j].strNumber) countStrings++;
                }
            }

            if (countLangres < countStrings) result = 0;
            else if (countLangres > countStrings) result = 1;

            return result;
        }

        

        private static LandbClass ReplaceStrings(LandbClass landb, List<CommonText> commonTexts, int type)
        {
            int index;
            for(int i = 0; i < landb.landbCount; i++)
            {
                index = -1;
                if (MainMenu.settings.importingOfName)
                {
                    index = type == 1 ? Methods.GetIndex(commonTexts, landb.landbs[i].anmID) : Methods.GetIndex(commonTexts, landb.landbs[i].stringNumber);
                    if (index != -1) landb.landbs[i].actorName = commonTexts[index].actorName;
                }
                
                index = type == 1 ? Methods.GetIndex(commonTexts, landb.landbs[i].anmID) : Methods.GetIndex(commonTexts, landb.landbs[i].stringNumber);
                if (index != -1) landb.landbs[i].actorSpeech = commonTexts[index].actorSpeechTranslation;

                if (landb.isUnicode && MainMenu.settings.unicodeSettings == 1) landb.landbs[i].actorSpeech = Methods.ConvertString(landb.landbs[i].actorSpeech, false);
                /*if(landb.isUnicode && (MainMenu.settings.unicodeSettings == 2) && (landb.landbs[i].actorName.Contains("\""))) 
                {
                    landb.landbs[i].actorSpeech = Methods.ConvertString(landb.landbs[i].actorSpeech, false);
                }*/

                if(MainMenu.settings.newTxtFormat && MainMenu.settings.changeLangFlags
                    && (index != -1))
                {
                    string tmpFlags = commonTexts[index].flags;

                    landb.landbs[i].flags = Convert.ToInt32(tmpFlags, 2);
                }
            }

            return landb;
        }

        public static string DoWork(string InputFile, string TxtFile, bool extract, byte[] EncKey, int version)
        {
            string result = "";

            FileInfo fi = new FileInfo(InputFile);

            byte[] buffer = File.ReadAllBytes(InputFile);
            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);
            bool mapOpeningCreditsReplacement = Methods.ShouldMapOpeningCreditsReplacement(fi.Name, buffer);

            try
            {
                byte[] checkHeader = br.ReadBytes(4);

                bool newFormat = false;
                bool hasCRC64Langres = false;
                bool isUnicode = false;
                int pos = 4;
                string additionalMessage = "";

                if ((Encoding.ASCII.GetString(checkHeader) == "5VSM") || (Encoding.ASCII.GetString(checkHeader) == "6VSM"))
                {
                    newFormat = true;
                    pos = 16;
                }

                br.BaseStream.Seek(pos, SeekOrigin.Begin);

                int countBlocks = br.ReadInt32();

                string[] classes = new string[countBlocks];

                for (int i = 0; i < countBlocks; i++)
                {
                   byte[] tmp = br.ReadBytes(8);
                   classes[i] = BitConverter.ToString(tmp);
                   if(classes[i] == "B0-9F-D8-63-34-02-4F-00") hasCRC64Langres = true;
                   if(classes[i] == "53-DC-A5-33-DB-D6-DC-7E") isUnicode = true;
                   tmp = br.ReadBytes(4); //Some values (in oldest games I found some values in *.vers files)
                }

                LandbClass landbs = GetStringsFromLandb(br, hasCRC64Langres, newFormat, isUnicode);
                br.Close();
                ms.Close();

                if (landbs == null)
                {
                    return "File " + fi.Name + ": unknown error.";
                }
                if ((landbs != null) && (landbs.landbCount == 0))
                {
                    landbs = null;
                    GC.Collect();
                    return fi.Name + " is EMPTY.";
                }

                if (extract)
                {
                    ClassesStructs.Text.CommonTextClass txts = new CommonTextClass();

                    txts.txtList = new List<CommonText>();

                    for (int i = 0; i < landbs.landbCount; i++)
                    {
                        ClassesStructs.Text.CommonText txt;

                        txt.isBothSpeeches = true;
                        txt.strNumber = MainMenu.settings.exportRealID || MainMenu.settings.newTxtFormat ? landbs.landbs[i].anmID : landbs.landbs[i].stringNumber;
                        txt.actorName = landbs.landbs[i].actorName;
                        txt.actorSpeechOriginal = landbs.landbs[i].actorSpeech;
                        txt.actorSpeechTranslation = landbs.landbs[i].actorSpeech;

                        txt.actorName = Methods.MapReplacementCharToCopyright(txt.actorName, mapOpeningCreditsReplacement);
                        txt.actorSpeechOriginal = Methods.MapReplacementCharToCopyright(txt.actorSpeechOriginal, mapOpeningCreditsReplacement);
                        txt.actorSpeechTranslation = Methods.MapReplacementCharToCopyright(txt.actorSpeechTranslation, mapOpeningCreditsReplacement);

                        txt.flags = Encoding.ASCII.GetString(landbs.flags[i].flags);

                        if (((txt.actorSpeechOriginal == "") && !MainMenu.settings.ignoreEmptyStrings)
                              || (txt.actorSpeechOriginal != "")) txts.txtList.Add(txt);
                    }

                    if (MainMenu.settings.sortSameString) txts = Methods.SortString(txts);

                    string outputFile = MainMenu.settings.pathForOutputFolder + "\\" + fi.Name.Remove(fi.Name.Length - 5, 5);
                    outputFile += MainMenu.settings.tsvFormat ? "tsv" : "txt";

                    switch(MainMenu.settings.newTxtFormat)
                    {
                        case true:
                            Texts.SaveText.NewMethod(txts.txtList, landbs.isUnicode, outputFile);
                            break;

                        default:
                            Texts.SaveText.OldMethod(txts.txtList, false, landbs.isUnicode, outputFile);
                            break;
                    }

                    txts.txtList.Clear();
                    txts = null;

                    result = fi.Name + " successfully extracted.";
                    if (additionalMessage != "") result += " " + additionalMessage;
                }
                else
                {
                    ClassesStructs.Text.CommonTextClass txts = new CommonTextClass();
                    txts.txtList = ReadText.GetStrings(TxtFile);

                    if (mapOpeningCreditsReplacement && txts.txtList != null)
                    {
                        for (int i = 0; i < txts.txtList.Count; i++)
                        {
                            ClassesStructs.Text.CommonText tmpTxt = txts.txtList[i];
                            tmpTxt.actorName = Methods.MapCopyrightToReplacementChar(tmpTxt.actorName, true);
                            tmpTxt.actorSpeechOriginal = Methods.MapCopyrightToReplacementChar(tmpTxt.actorSpeechOriginal, true);
                            tmpTxt.actorSpeechTranslation = Methods.MapCopyrightToReplacementChar(tmpTxt.actorSpeechTranslation, true);
                            txts.txtList[i] = tmpTxt;
                        }
                    }

                    /*if (txts.txtList.Count < landbs.landbCount)
                    {
                        FileInfo txtFI = new FileInfo(TxtFile);
                        return "Not enough strings in " + txtFI.Name + " for " + fi.Name + " file.";
                    }*/

                    int type = CheckNumbers(txts.txtList, landbs);

                    if (type == -1) return "I don't know which type of number strings select for " + fi.Name + " file.";

                    landbs = ReplaceStrings(landbs, txts.txtList, type);

                    ms = new MemoryStream(buffer);
                    br = new BinaryReader(ms);

                    string outputFile = MainMenu.settings.pathForOutputFolder + "\\" + fi.Name;

                    int rebuildResult = RebuildLandb(br, outputFile, landbs, fi.Name, mapOpeningCreditsReplacement);
                    
                    br.Close();
                    ms.Close();

                    result = "File " + fi.Name + " successfully imported.";

                    if(rebuildResult == -1)
                    {
                        result = "Unknown error while rebuild file " + fi.Name;
                    }

                    landbs = null;
                }

                buffer = null;
            }
            catch
            {
                if (br != null) br.Close();
                if (ms != null) ms.Close();

                result = "Something wrong with langdb file " + fi.Name;
            }

            GC.Collect();
            return result;
        }
    }
}
