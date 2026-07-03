using System;
using System.Collections.Generic;
using System.Text;
using TTG_Tools.ClassesStructs.Text;
using System.IO;

namespace TTG_Tools.Texts
{
    public class LangdbWorker
    {
        private static LangdbClass GetStringsFromLangdb(BinaryReader br, bool hasFlags)
        {
            bool tryAgain = false; //Silly check for oldest incorrect file sizes
            long pos = 0;
            tryAgainRead:
            try
            {
                LangdbClass langdb = new LangdbClass();
                langdb.blockLength = 0;
                if(!tryAgain) pos = br.BaseStream.Position;
                else br.BaseStream.Seek(pos, SeekOrigin.Begin);
                int checkBlockLength = br.ReadInt32();
                long checkSize = br.BaseStream.Length - pos + 4;

                langdb.isBlockLength = false;

                if (checkSize == checkBlockLength || tryAgain)
                {
                    langdb.blockLength = checkBlockLength;
                    langdb.newBlockLength = 8;
                    langdb.isBlockLength = true;
                    langdb.langdbCount = br.ReadInt32();
                }
                else
                {
                    langdb.blockLength = -1;
                    langdb.langdbCount = checkBlockLength;
                }

                langdb.langdbs = new langdb[langdb.langdbCount];

                langdb.flags = new ClassesStructs.FlagsClass.LangdbFlagClass[langdb.langdbCount];

                for (int i = 0; i < langdb.langdbCount; i++)
                {
                    langdb.langdbs[i].stringNumber = (uint)(i + 1);
                    langdb.langdbs[i].anmID = br.ReadUInt32();
                    if(langdb.isBlockLength) langdb.newBlockLength += 4;

                    langdb.langdbs[i].voxID = br.ReadUInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;

                    int blockSize = -1;

                    if (langdb.isBlockLength)
                    {
                        blockSize = br.ReadInt32();
                        if (langdb.isBlockLength) langdb.newBlockLength += 4;
                    }

                    int stringLength = br.ReadInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;

                    //Don't calculate actor name's length
                    byte[] tmp = br.ReadBytes(stringLength);
                    langdb.langdbs[i].actorName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                    if (langdb.isBlockLength)
                    {
                        blockSize = br.ReadInt32();
                        if (langdb.isBlockLength) langdb.newBlockLength += 4;
                    }

                    stringLength = br.ReadInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;

                    //Don't calculate actor speech's length
                    tmp = br.ReadBytes(stringLength);
                    langdb.langdbs[i].actorSpeech = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);

                    if (langdb.isBlockLength)
                    {
                        blockSize = br.ReadInt32();
                        if (langdb.isBlockLength) langdb.newBlockLength += 4;
                    }

                    stringLength = br.ReadInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;

                    tmp = br.ReadBytes(stringLength);
                    langdb.langdbs[i].anmFile = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);
                    if (langdb.isBlockLength) langdb.newBlockLength += stringLength;

                    if (langdb.isBlockLength)
                    {
                        blockSize = br.ReadInt32();
                        langdb.newBlockLength += 4;
                    }

                    stringLength = br.ReadInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;

                    tmp = br.ReadBytes(stringLength);
                    langdb.langdbs[i].voxFile = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmp);
                    if (langdb.isBlockLength) langdb.newBlockLength += stringLength;

                    langdb.flags[i] = new ClassesStructs.FlagsClass.LangdbFlagClass();
                    langdb.flags[i].flags = br.ReadBytes(3);
                    if (langdb.isBlockLength) langdb.newBlockLength += 3;

                    langdb.langdbs[i].zero = br.ReadInt32();
                    if (langdb.isBlockLength) langdb.newBlockLength += 4;
                }

                return langdb;
            }
            catch
            {
                if (!tryAgain)
                {
                    tryAgain = true;
                    goto tryAgainRead;
                }
                
                return null;
            }
        }

        private static int RebuildLangdb(BinaryReader br, string outputFile, LangdbClass langdb)
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

                byte[] tmp = br.ReadBytes(8);
                br.BaseStream.Seek(8, SeekOrigin.Begin);

                if (BitConverter.ToString(tmp) == BitConverter.ToString(BitConverter.GetBytes(CRCs.CRC64(0, InEngineWords.ClassStructsNames.languagedatabaseClass.ToLower()))))
                {
                    for (int i = 0; i < count; i++)
                    {
                        tmp = br.ReadBytes(8);
                        bw.Write(tmp);

                        tmp = br.ReadBytes(4);
                        bw.Write(tmp);
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        int len = br.ReadInt32();
                        bw.Write(len);

                        tmp = br.ReadBytes(len);
                        bw.Write(tmp);

                        tmp = br.ReadBytes(4);
                        bw.Write(tmp);
                    }
                }

                var pos = br.BaseStream.Position;

                if(langdb.isBlockLength)
                {
                    bw.Write(langdb.blockLength);
                }

                bw.Write(langdb.langdbCount);

                for(int i = 0; i < langdb.langdbCount; i++)
                {
                    bw.Write(langdb.langdbs[i].anmID);
                    bw.Write(langdb.langdbs[i].voxID);

                    byte[] tmpActorName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(langdb.langdbs[i].actorName);
                    int actorNameLen = tmpActorName.Length;
                    int actorNameBlockLen = actorNameLen + 8;

                    byte[] tmpActorSpeech = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(langdb.langdbs[i].actorSpeech);
                    int actorSpeechLen = tmpActorSpeech.Length;
                    int actorSpeechBlockLen = tmpActorSpeech.Length + 8;

                    if (langdb.isBlockLength)
                    {
                        bw.Write(actorNameBlockLen);
                    }
                    
                    bw.Write(actorNameLen);
                    bw.Write(tmpActorName);
                    langdb.newBlockLength += tmpActorName.Length;

                    if(langdb.isBlockLength)
                    {
                        bw.Write(actorSpeechBlockLen);
                    }

                    bw.Write(actorSpeechLen);
                    bw.Write(tmpActorSpeech);
                    langdb.newBlockLength += tmpActorSpeech.Length;

                    int blockVoxLen = 0, blockAnmLen = 0;
                    int voxLen = 0, anmLen = 0;

                    tmp = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(langdb.langdbs[i].anmFile);
                    anmLen = tmp.Length;
                    blockAnmLen = anmLen + 8;

                    if(langdb.isBlockLength)
                    {
                        bw.Write(blockAnmLen);
                    }

                    bw.Write(anmLen);
                    bw.Write(tmp);

                    tmp = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(langdb.langdbs[i].voxFile);
                    voxLen = tmp.Length;
                    blockVoxLen = voxLen + 8;

                    if (langdb.isBlockLength)
                    {
                        bw.Write(blockVoxLen);
                    }

                    bw.Write(voxLen);
                    bw.Write(tmp);

                    bw.Write(langdb.flags[i].flags);
                    bw.Write(langdb.langdbs[i].zero);
                }

                if (langdb.isBlockLength)
                {
                    bw.BaseStream.Seek(pos, SeekOrigin.Begin);
                    bw.Write(langdb.newBlockLength);
                }

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

        private static LangdbClass ReplaceStrings(LangdbClass langdb, List<CommonText> commonTexts, int type)
        {
            int index;
            for (int i = 0; i < langdb.langdbCount; i++)
            {
                index = -1;

                if (MainMenu.settings.importingOfName)
                {
                    index = type == 1 ? Methods.GetIndex(commonTexts, langdb.langdbs[i].anmID) : Methods.GetIndex(commonTexts, langdb.langdbs[i].stringNumber);
                    if(index != -1) langdb.langdbs[i].actorName = commonTexts[index].actorName;
                }

                index = type == 1 ? Methods.GetIndex(commonTexts, langdb.langdbs[i].anmID) : Methods.GetIndex(commonTexts, langdb.langdbs[i].stringNumber);
                if (index != -1) langdb.langdbs[i].actorSpeech = Methods.NormalizeImportedText(commonTexts[index].actorSpeechTranslation);

                if(MainMenu.settings.newTxtFormat && MainMenu.settings.changeLangFlags && (index != -1))
                {
                    string tmpFlags = commonTexts[index].flags;

                    byte[] tmpBytesFlags = Encoding.ASCII.GetBytes(tmpFlags);

                    int count = tmpBytesFlags.Length > 3 ? 3 : tmpBytesFlags.Length;

                    for (int j = 0; j < count; j++)
                    {
                        langdb.flags[i].flags[j] = tmpBytesFlags[j];
                    }

                }
            }

            return langdb;
        }

        private static int CheckNumbers(List<CommonText> txts, LangdbClass langdb)
        {
            int result = -1;
            int countLangres = 0;
            int countStrings = 0;

            for (int i = 0; i < langdb.langdbCount; i++)
            {
                for (int j = 0; j < txts.Count; j++)
                {
                    if (langdb.langdbs[i].anmID == txts[j].strNumber) countLangres++;
                    if (langdb.langdbs[i].stringNumber == txts[j].strNumber) countStrings++;
                }
            }

            if (countLangres < countStrings) result = 0;
            else if (countLangres > countStrings) result = 1;

            return result;
        }

        public static string DoWork(string InputFile, string txtFile, bool extract, bool FullEncrypt, ref byte[] EncKey, int version)
        {
            string result = "";

            FileInfo fi = new FileInfo(InputFile);

            byte[] buffer = File.ReadAllBytes(InputFile);
            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);

            string additionalMessage = "";

            try
            {
                byte[] checkHeader = br.ReadBytes(4);

                // CSI: 3 Dimensions of Murder (PS2) ships its .langdb files as
                // a "BMS3" meta-stream variant. This is a different layout from
                // the regular ERTM/MBIN langdb the rest of the tool handles, so
                // we detect it up front and route to a dedicated parser.
                if (Encoding.ASCII.GetString(checkHeader) == "BMS3")
                {
                    br.Close();
                    ms.Close();
                    return DoWorkBMS3(InputFile, txtFile, extract);
                }

                if (Encoding.ASCII.GetString(checkHeader) != "ERTM") //Supposed this langdb file encrypted
                {
                    //First trying decrypt probably encrypted langdb
                    try
                    {
                        string info = Methods.FindLangresDecryptKey(buffer, ref EncKey, ref version);

                        if ((info != null) && (info != "OK"))
                        {
                            additionalMessage = "Langdb file was encrypted, but I decrypted. " + info;
                        }
                    }
                    catch
                    {
                        result = "Maybe that LANGDB file encrypted. Try to decrypt first: " + fi.Name;

                        return result;
                    }
                }

                int countBlocks = br.ReadInt32();

                string[] classes = new string[countBlocks];

                bool hasFlags = false;
                byte[] checkBlock = br.ReadBytes(8);
                br.BaseStream.Seek(8, SeekOrigin.Begin);
                ulong checkCRC64 = 0;
                bool isHashStrings = false;

                if (BitConverter.ToString(checkBlock) == BitConverter.ToString(BitConverter.GetBytes(CRCs.CRC64(checkCRC64, InEngineWords.ClassStructsNames.languagedatabaseClass.ToLower()))))
                {
                    isHashStrings = true;

                    for (int i = 0; i < countBlocks; i++)
                    {
                        byte[] tmp = br.ReadBytes(8);
                        classes[i] = BitConverter.ToString(tmp);
                        if (classes[i].ToLower() == BitConverter.ToString(BitConverter.GetBytes(CRCs.CRC64(checkCRC64, InEngineWords.ClassStructsNames.flagsClass.ToLower())))) hasFlags = true;
                        tmp = br.ReadBytes(4); //Some values (in oldest games I found some values in *.vers files)
                    }
                }
                else
                {
                    for (int i = 0; i < countBlocks; i++)
                    {
                        int len = br.ReadInt32();
                        byte[] tmp = br.ReadBytes(len);
                        classes[i] = Encoding.ASCII.GetString(tmp);
                        if (classes[i].ToLower() == "class flags") hasFlags = true;
                        tmp = br.ReadBytes(4); //Some values (in oldest games I found some values in *.vers files)
                    }
                }

                LangdbClass langdbs = GetStringsFromLangdb(br, hasFlags);
                br.Close();
                ms.Close();

                if (langdbs == null)
                {
                    return "File " + fi.Name + ": unknown error.";
                }
                if ((langdbs != null) && (langdbs.langdbCount == 0))
                {
                    langdbs = null;
                    GC.Collect();
                    return fi.Name + " is EMPTY.";
                }

                if (extract)
                {
                    ClassesStructs.Text.CommonTextClass txts = new CommonTextClass();

                    txts.txtList = new List<CommonText>();

                    for (int i = 0; i < langdbs.langdbCount; i++)
                    {
                        ClassesStructs.Text.CommonText txt;

                        txt.isBothSpeeches = true;
                        txt.strNumber = MainMenu.settings.exportRealID ? langdbs.langdbs[i].anmID : langdbs.langdbs[i].stringNumber;
                        txt.actorName = langdbs.langdbs[i].actorName;
                        txt.actorSpeechOriginal = langdbs.langdbs[i].actorSpeech;
                        txt.actorSpeechTranslation = langdbs.langdbs[i].actorSpeech;
                        txt.flags = Encoding.ASCII.GetString(langdbs.flags[i].flags);

                        if (((txt.actorSpeechOriginal == "") && !MainMenu.settings.ignoreEmptyStrings)
                              || (txt.actorSpeechOriginal != "")) txts.txtList.Add(txt);
                    }

                    if (MainMenu.settings.sortSameString) txts = Methods.SortString(txts);

                    string outputFile = MainMenu.settings.pathForOutputFolder + "\\" + fi.Name.Remove(fi.Name.Length - 6, 6);
                    outputFile += MainMenu.settings.tsvFormat ? "tsv" : "txt";

                    switch (MainMenu.settings.newTxtFormat)
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
                    ClassesStructs.Text.CommonTextClass txt = new CommonTextClass();
                    txt.txtList = ReadText.GetStrings(txtFile);

                    int type = CheckNumbers(txt.txtList, langdbs);
                    if (type == -1) return "I don't know which type of number strings select for " + fi.Name + " file.";

                    langdbs = ReplaceStrings(langdbs, txt.txtList, type);

                    ms = new MemoryStream(buffer);
                    br = new BinaryReader(ms);

                    string outputFile = MainMenu.settings.pathForOutputFolder + Path.DirectorySeparatorChar + fi.Name;

                    int rebuildResult = RebuildLangdb(br, outputFile, langdbs);

                    result = "File " + fi.Name + " successfully imported.";

                    if (rebuildResult == -1)
                    {
                        result = "Unknown error while rebuild file " + fi.Name;
                    }

                    br.Close();
                    ms.Close();

                    langdbs = null;

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

                            if(MainMenu.settings.customKey)
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

                buffer = null;
            }
            catch
            {
                if(br != null) br.Close();
                if(ms != null) ms.Close();

                result = "Something wrong with langdb file " + fi.Name;
            }

            GC.Collect();
            return result;
        }

        // =====================================================================
        //   CSI: 3 Dimensions of Murder (PS2)  -  "BMS3" .langdb variant
        // =====================================================================
        //
        // Layout reverse-engineered from english/french/german/italian/spanish
        // .langdb shipped with the PS2 release (no encryption — EMS3 isn't used
        // by this title, per the Telltale Editor's CSI3 game descriptor).
        //
        // FILE HEADER (20 bytes):
        //   00..03   "BMS3"                         (magic)
        //   04..07   0x02010000                     (BMS3 stream flags)
        //   08..0B   0x00000000                     (numClasses — empty: this
        //                                           stream dumps the
        //                                           LanguageDatabase contents
        //                                           directly, without the meta
        //                                           class table the rest of
        //                                           the BMS3 spec uses)
        //   0C..0F   uint32 numEntries              (count of LanguageResource
        //                                           records in the dump)
        //   10..13   uint32 entriesBlobSize         (approx size in bytes of
        //                                           the entries section that
        //                                           follows; preserved verbatim
        //                                           on rebuild)
        //
        // ENTRY (variable, 16-byte aligned within the entries section):
        //   +00      uint32 entrySize           (unaligned entry size, before
        //                                       the 16-byte padding)
        //   +04      uint32 runtime ptr         (Handle<...> pointer leaked
        //                                       from RAM; meaningless on disk)
        //   +08      uint32 prefixLength        (mPrefix length, no NUL)
        //   +0C      uint32 textLength          (mText length, no NUL)
        //   +10..13  3 bytes flags + 1 byte 0x78 fill
        //                                       (mShared / mAllowSharing /
        //                                        mbNoAnim followed by struct
        //                                        padding)
        //   +14      uint32 0
        //   +18      uint32 0x78787878          (uninitialised pad)
        //   +1C..23  uint64 0
        //   +24      uint32 0x78787878          (uninitialised pad)
        //   +28      prefix bytes + 0x00        (NUL-terminated)
        //   +..      text   bytes + 0x00        (NUL-terminated)
        //   +..      anmFile bytes + 0x00       (NUL-terminated C-string;
        //                                       empty = single 0x00 byte)
        //   +..      voxFile bytes + 0x00       (NUL-terminated C-string)
        //   +..      0..15 bytes of 0x78 fill   (alignment to 16-byte boundary
        //                                       relative to entries section
        //                                       start, file offset 0x14)
        //
        // FILE TAIL: pointer/index table dumped from runtime memory ending
        //            with the literal "End of buffer!\0". The game's loader
        //            doesn't need it to understand the entries; we preserve
        //            it verbatim on rebuild so the file shape stays familiar.

        private const uint BMS3_FLAGS = 0x02010000u;
        private const uint BMS3_PAD_PATTERN = 0x78787878u;
        private const long BMS3_ENTRIES_START = 0x14;

        private struct BMS3Entry
        {
            public uint mId;             // Unaligned entry size in CSI3 PS2 BMS3.
            public uint runtimePtr;
            public byte[] flags;        // 3 bytes (mShared / mAllowSharing / mbNoAnim)
            public byte flagsPad;       // 1 byte after flags (usually 0x78, sometimes leaks RAM)
            public uint mid1;           // U32 after flags (usually 0; mFlags-related)
            public uint mid2;           // U32 (usually 0x78787878)
            public ulong mid3;          // U64 (usually 0)
            public uint mid4;           // U32 (usually 0x78787878)
            public string actorName;
            public string actorSpeech;
            public string anmFile;
            public string voxFile;
            public byte[] padBytes;     // 0..15 trailing pad bytes, preserved verbatim

            public long oldEntryStart;
            public long oldNameStart;
            public long oldTextStart;
            public long oldAnmStart;
            public long oldVoxStart;
            public long oldPadStart;
            public long oldEntryEnd;

            public long newEntryStart;
            public long newNameStart;
            public long newTextStart;
            public long newAnmStart;
            public long newVoxStart;
            public long newPadStart;
            public long newEntryEnd;
        }

        private static long AlignTo16Rel(long pos, long origin)
        {
            long rel = pos - origin;
            long alignedRel = (rel + 15) & ~15L;
            return origin + alignedRel;
        }

        private static string ReadCString(BinaryReader br, Encoding enc)
        {
            var bytes = new List<byte>(16);
            while (true)
            {
                byte b = br.ReadByte();
                if (b == 0) break;
                bytes.Add(b);
            }
            return enc.GetString(bytes.ToArray());
        }

        private static List<BMS3Entry> ReadBMS3Entries(BinaryReader br, uint numEntries, Encoding enc, long entriesEndLimit, out long entriesEndPos)
        {
            var entries = new List<BMS3Entry>((int)Math.Min(numEntries, 0x10000u));
            long entriesStart = br.BaseStream.Position;

            for (uint i = 0; i < numEntries; i++)
            {
                BMS3Entry e = new BMS3Entry();
                e.oldEntryStart = br.BaseStream.Position - entriesStart;

                // 8 byte pre-header (entry size + runtimePtr)
                e.mId = br.ReadUInt32();
                e.runtimePtr = br.ReadUInt32();

                int prefixLen = br.ReadInt32();
                int textLen = br.ReadInt32();

                if (prefixLen < 0 || prefixLen > 0x4000 || textLen < 0 || textLen > 0x10000)
                {
                    throw new InvalidDataException(
                        "BMS3 langdb: tamanhos de string suspeitos no registro " + i +
                        " (prefix=" + prefixLen + ", text=" + textLen + ", pos=0x" + br.BaseStream.Position.ToString("X") + ")");
                }

                // 3 bytes flags + 1 byte fill (usually 0x78). Both preserved.
                e.flags = br.ReadBytes(3);
                e.flagsPad = br.ReadByte();

                // 4 + 4 + 8 + 4 mid fields. They're usually {0, 0x78787878, 0,
                // 0x78787878} but some entries carry non-zero values from
                // runtime memory; preserve verbatim for byte-perfect rebuild.
                e.mid1 = br.ReadUInt32();
                e.mid2 = br.ReadUInt32();
                e.mid3 = br.ReadUInt64();
                e.mid4 = br.ReadUInt32();

                e.oldNameStart = br.BaseStream.Position - entriesStart;
                byte[] prefixBytes = br.ReadBytes(prefixLen);
                br.ReadByte(); // explicit NUL
                e.actorName = Methods.DecodeCsiPs2ControllerTags(enc.GetString(prefixBytes));

                e.oldTextStart = br.BaseStream.Position - entriesStart;
                byte[] textBytes = br.ReadBytes(textLen);
                br.ReadByte(); // explicit NUL
                e.actorSpeech = Methods.DecodeCsiPs2ControllerTags(enc.GetString(textBytes));

                // anm and vox are NUL-terminated C-strings (no length prefix).
                // Empty = single NUL byte.
                e.oldAnmStart = br.BaseStream.Position - entriesStart;
                e.anmFile = ReadCString(br, enc);
                e.oldVoxStart = br.BaseStream.Position - entriesStart;
                e.voxFile = ReadCString(br, enc);

                // Pad to next 16-byte boundary relative to entries section
                // start. The only exception is the final entry: CSI3 PS2
                // places the first tail-table pair immediately after it, and
                // the header's blob size includes that 8-byte pair.
                long here = br.BaseStream.Position;
                long aligned = (i + 1 == numEntries)
                    ? entriesEndLimit
                    : AlignTo16Rel(here, entriesStart);
                int padLen = (int)(aligned - here);
                if (padLen < 0)
                    throw new InvalidDataException("BMS3 langdb: fim das entradas antes do esperado no registro " + i + ".");
                e.oldPadStart = here - entriesStart;
                e.padBytes = padLen > 0 ? br.ReadBytes(padLen) : new byte[0];
                e.oldEntryEnd = br.BaseStream.Position - entriesStart;

                entries.Add(e);
            }

            entriesEndPos = br.BaseStream.Position;
            return entries;
        }

        // BMS3 langdb has a tricky ambiguity: most CSI3 PS2 entries have
        // small mId values (typically 0..255), so when the user extracts with
        // exportRealID=false the strNumbers (1..N) collide with the mId range
            // and a naive count-based vote picks the wrong mode — every entry
        // ends up importing some *other* entry's translation.
        //
        // Resolution: if the imported txt's strNumbers form a contiguous
        // 1..N sequence whose length matches the entry list, it was emitted
        // in index mode (mode 1). Otherwise it was emitted with real mIds
            // (mode 0). This is unambiguous because exportRealID=true only writes
            // strNumber = the serialized entry size, which is never a perfect
            // 1..N sequence on real langdb files.
        private static int CheckNumbersBMS3(List<CommonText> txts, List<BMS3Entry> entries)
        {
            if (txts == null || txts.Count == 0 || entries == null || entries.Count == 0)
                return -1;

            // Index mode is identified by txts.strNumber forming the *set*
            // {1..N} where N == entries.Count. Any ordering counts — the user
            // may have run sortSameString=true on extract, which permutes
            // the txt rows but keeps the strNumber values intact. The match
            // step uses GetIndex(txts, i+1), which is order-independent.
            if (txts.Count == entries.Count)
            {
                System.Collections.Generic.HashSet<uint> seen = new System.Collections.Generic.HashSet<uint>();
                bool isIndexSet = true;
                for (int i = 0; i < txts.Count; i++)
                {
                    uint n = txts[i].strNumber;
                    if (n < 1 || n > (uint)txts.Count) { isIndexSet = false; break; }
                    if (!seen.Add(n)) { isIndexSet = false; break; }
                }
                if (isIndexSet) return 1; // index mode
            }

            // Fallback: real-mId mode if at least one mId resolves.
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = 0; j < txts.Count; j++)
                {
                    if (entries[i].mId == txts[j].strNumber) return 0;
                }
            }
            return -1;
        }

        private static void WriteBMS3Entry(BinaryWriter bw, ref BMS3Entry e, Encoding enc, long entriesStart, bool isLastEntry)
        {
            byte[] prefixBytes = enc.GetBytes(Methods.EncodeCsiPs2ControllerTags(e.actorName ?? string.Empty));
            byte[] textBytes = enc.GetBytes(Methods.EncodeCsiPs2ControllerTags(e.actorSpeech ?? string.Empty));
            byte[] anmBytes = enc.GetBytes(e.anmFile ?? string.Empty);
            byte[] voxBytes = enc.GetBytes(e.voxFile ?? string.Empty);
            long unalignedEntrySize = 44L + prefixBytes.Length + textBytes.Length + anmBytes.Length + voxBytes.Length;
            if (unalignedEntrySize < 0 || unalignedEntrySize > uint.MaxValue)
                throw new InvalidDataException("BMS3 langdb entry is too large.");

            e.newEntryStart = bw.BaseStream.Position - entriesStart;

            // 8-byte pre-header. CSI3 PS2 stores the unaligned size of this
            // entry here; keeping the old value after a longer translation
            // makes short UI strings bleed into neighbouring memory.
            bw.Write((uint)unalignedEntrySize);
            bw.Write(e.runtimePtr);

            // length-prefixed strings
            bw.Write(prefixBytes.Length);
            bw.Write(textBytes.Length);

            // 3 flag bytes + 1 byte fill (preserved from original)
            byte[] flags = e.flags ?? new byte[] { 1, 1, 0 };
            if (flags.Length < 3)
            {
                byte[] padded = new byte[3];
                Array.Copy(flags, padded, flags.Length);
                flags = padded;
            }
            bw.Write(flags, 0, 3);
            bw.Write(e.flagsPad);

            // 24 bytes of mid-fields (preserved verbatim — typically zero /
            // 0x78787878 / zero64 / 0x78787878 but not always).
            bw.Write(e.mid1);
            bw.Write(e.mid2);
            bw.Write(e.mid3);
            bw.Write(e.mid4);

            // prefix + NUL, speech + NUL
            e.newNameStart = bw.BaseStream.Position - entriesStart;
            bw.Write(prefixBytes);
            bw.Write((byte)0);
            e.newTextStart = bw.BaseStream.Position - entriesStart;
            bw.Write(textBytes);
            bw.Write((byte)0);

            // anm + NUL, vox + NUL  (NUL-terminated C-strings, no length prefix)
            e.newAnmStart = bw.BaseStream.Position - entriesStart;
            bw.Write(anmBytes);
            bw.Write((byte)0);
            e.newVoxStart = bw.BaseStream.Position - entriesStart;
            bw.Write(voxBytes);
            bw.Write((byte)0);

            // Trailing alignment padding: every entry except the last one is
            // 16-byte aligned. The last entry is followed by the first 8 bytes
            // of the runtime tail table; aligning it shifts the table and
            // causes short UI strings to resolve through garbage.
            long pos = bw.BaseStream.Position;
            long aligned = isLastEntry ? pos : AlignTo16Rel(pos, entriesStart);
            int needed = (int)(aligned - pos);
            e.newPadStart = pos - entriesStart;
            if (e.padBytes != null && e.padBytes.Length == needed)
            {
                bw.Write(e.padBytes);
            }
            else
            {
                for (int p = 0; p < needed; p++) bw.Write((byte)0x78);
            }
            e.newEntryEnd = bw.BaseStream.Position - entriesStart;
        }

        private static uint RotateRight8(uint value)
        {
            return (value >> 8) | (value << 24);
        }

        private static uint RotateLeft8(uint value)
        {
            return (value << 8) | (value >> 24);
        }

        private static bool TryMapBMS3EntryStartOffset(List<BMS3Entry> entries, long oldOffset, out long newOffset)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                BMS3Entry e = entries[i];
                if (oldOffset == e.oldEntryStart)
                {
                    newOffset = e.newEntryStart;
                    return true;
                }
            }

            newOffset = 0;
            return false;
        }

        private static void TryRewriteBMS3TailEntryStart(byte[] rebuilt, int pos, List<BMS3Entry> entries)
        {
            if (pos < 0 || pos + 4 > rebuilt.Length)
                return;

            uint stored = BitConverter.ToUInt32(rebuilt, pos);
            if (!TryMapBMS3EntryStartOffset(entries, stored, out long mappedOffset))
                return;
            if (mappedOffset < 0 || mappedOffset > uint.MaxValue)
                return;

            byte[] bytes = BitConverter.GetBytes((uint)mappedOffset);
            Buffer.BlockCopy(bytes, 0, rebuilt, pos, 4);
        }

        private static byte[] RebuildBMS3Tail(byte[] tailBytes, List<BMS3Entry> entries)
        {
            if (tailBytes == null || tailBytes.Length < 4 || entries == null || entries.Count == 0)
                return tailBytes ?? new byte[0];

            byte[] rebuilt = (byte[])tailBytes.Clone();
            byte[] sentinel = Encoding.ASCII.GetBytes("End of buffer!");
            int sentinelAt = -1;
            for (int i = 0; i <= rebuilt.Length - sentinel.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < sentinel.Length; j++)
                {
                    if (rebuilt[i + j] != sentinel[j]) { match = false; break; }
                }
                if (match) { sentinelAt = i; break; }
            }

            int tableEnd = sentinelAt >= 0 ? sentinelAt : rebuilt.Length;
            tableEnd -= tableEnd % 4;

            // The complete table is a sequence of 8-byte pairs:
            //   [runtime pointer word, entry-start offset]
            // The first pair lives just before the header-indicated tail
            // offset; the rest of the table plus "End of buffer!" follows it.
            // Runtime pointer words are preserved exactly. Only the offset
            // word in each pair is relocated after string sizes change.
            for (int pos = 4; pos + 4 <= tableEnd; pos += 8)
                TryRewriteBMS3TailEntryStart(rebuilt, pos, entries);

            return rebuilt;
        }

        public static string DoWorkBMS3(string InputFile, string txtFile, bool extract)
        {
            FileInfo fi = new FileInfo(InputFile);
            byte[] buffer = File.ReadAllBytes(InputFile);
            Encoding enc = Encoding.GetEncoding(1252);

            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryReader br = new BinaryReader(ms))
            {
                try
                {
                    string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (magic != "BMS3") return "Arquivo " + fi.Name + ": magic BMS3 não encontrado.";

                    uint flags = br.ReadUInt32();
                    if (flags != BMS3_FLAGS)
                        return "Arquivo " + fi.Name + ": flags BMS3 inesperadas (0x" + flags.ToString("X8") + ").";

                    uint numClasses = br.ReadUInt32();
                    if (numClasses != 0)
                        return "Arquivo " + fi.Name + ": variante BMS3 com tabela de classes não suportada.";

                    uint numEntries = br.ReadUInt32();
                    if (numEntries == 0) return fi.Name + " is EMPTY.";
                    if (numEntries > 0x10000)
                        return "Arquivo " + fi.Name + ": numEntries suspeito (" + numEntries + ").";

                    // Extra header field at file offset 0x10. Approx size of
                    // the entries section. Preserved verbatim on rebuild.
                    uint entriesBlobSize = br.ReadUInt32();

                    long entriesStartPos = br.BaseStream.Position;
                    long tailRestPos = BMS3_ENTRIES_START + entriesBlobSize;
                    long entriesEndLimit = tailRestPos - 8;
                    if (entriesEndLimit < entriesStartPos || tailRestPos > br.BaseStream.Length)
                        return "Arquivo " + fi.Name + ": header BMS3 aponta para uma tabela final inválida.";

                    long entriesEndPos;
                    List<BMS3Entry> entries = ReadBMS3Entries(br, numEntries, enc, entriesEndLimit, out entriesEndPos);

                    // CSI3 PS2 stores the first 8-byte tail table pair as the
                    // last bytes of the header-counted blob. The remaining
                    // pairs and the "End of buffer!" sentinel start exactly at
                    // entriesStart + entriesBlobSize.
                    byte[] tailLeadBytes = br.ReadBytes(8);
                    if (tailLeadBytes.Length != 8)
                        return "Arquivo " + fi.Name + ": tabela final BMS3 incompleta.";
                    int tailLen = (int)(br.BaseStream.Length - tailRestPos);
                    byte[] tailBytes = tailLen > 0 ? br.ReadBytes(tailLen) : new byte[0];

                    if (extract)
                    {
                        var txts = new ClassesStructs.Text.CommonTextClass();
                        txts.txtList = new List<CommonText>();

                        for (int i = 0; i < entries.Count; i++)
                        {
                            BMS3Entry e = entries[i];
                            CommonText txt = new CommonText();
                            txt.isBothSpeeches = true;
                            // Match the rest of the tool's convention: prefer
                            // the engine's real id (mId) when the user opted
                            // into "real ID" export; otherwise fall back to
                            // the 1-based row number.
                            txt.strNumber = MainMenu.settings.exportRealID
                                ? e.mId
                                : (uint)(i + 1);
                            txt.actorName = e.actorName;
                            txt.actorSpeechOriginal = e.actorSpeech;
                            txt.actorSpeechTranslation = e.actorSpeech;
                            txt.flags = enc.GetString(e.flags);

                            if ((txt.actorSpeechOriginal == "" && !MainMenu.settings.ignoreEmptyStrings)
                                || txt.actorSpeechOriginal != "")
                                txts.txtList.Add(txt);
                        }

                        if (MainMenu.settings.sortSameString) txts = Methods.SortString(txts);

                        string outputFile = MainMenu.settings.pathForOutputFolder + Path.DirectorySeparatorChar
                            + fi.Name.Remove(fi.Name.Length - 6, 6);
                        outputFile += MainMenu.settings.tsvFormat ? "tsv" : "txt";

                        if (MainMenu.settings.newTxtFormat)
                            Texts.SaveText.NewMethod(txts.txtList, false, outputFile);
                        else
                            Texts.SaveText.OldMethod(txts.txtList, false, false, outputFile);

                        txts.txtList.Clear();
                        return fi.Name + " (BMS3 / CSI3 PS2) successfully extracted.";
                    }
                    else
                    {
                        var imported = new ClassesStructs.Text.CommonTextClass();
                        imported.txtList = ReadText.GetStrings(txtFile);

                        int matchType = CheckNumbersBMS3(imported.txtList, entries);
                        if (matchType == -1)
                            return "I don't know which type of number strings select for " + fi.Name + " file.";

                        // Replace
                        for (int i = 0; i < entries.Count; i++)
                        {
                            BMS3Entry e = entries[i];
                            uint key = matchType == 0 ? e.mId : (uint)(i + 1);
                            int idx = Methods.GetIndex(imported.txtList, key);
                            if (idx == -1) { entries[i] = e; continue; }

                            if (MainMenu.settings.importingOfName)
                                e.actorName = imported.txtList[idx].actorName;
                            e.actorSpeech = Methods.NormalizeImportedText(imported.txtList[idx].actorSpeechTranslation);

                            if (MainMenu.settings.newTxtFormat && MainMenu.settings.changeLangFlags
                                && imported.txtList[idx].flags != null)
                            {
                                byte[] newFlags = Encoding.ASCII.GetBytes(imported.txtList[idx].flags);
                                int n = Math.Min(3, newFlags.Length);
                                if (e.flags == null || e.flags.Length < 3) e.flags = new byte[3];
                                for (int k = 0; k < n; k++) e.flags[k] = newFlags[k];
                            }

                            entries[i] = e;
                        }

                        string outputFile = MainMenu.settings.pathForOutputFolder + Path.DirectorySeparatorChar + fi.Name;
                        if (File.Exists(outputFile)) File.Delete(outputFile);

                        using (FileStream ofs = new FileStream(outputFile, FileMode.CreateNew))
                        using (BinaryWriter bw = new BinaryWriter(ofs))
                        {
                            // 20-byte header
                            bw.Write(Encoding.ASCII.GetBytes("BMS3"));
                            bw.Write(BMS3_FLAGS);
                            bw.Write((uint)0);                  // numClasses
                            bw.Write((uint)entries.Count);      // numEntries
                            long entriesBlobSizePos = bw.BaseStream.Position;
                            bw.Write((uint)0);                  // entries blob size (filled after writing entries)

                            long entriesStart = bw.BaseStream.Position;  // 0x14

                            // Entries
                            for (int i = 0; i < entries.Count; i++)
                            {
                                BMS3Entry e = entries[i];
                                WriteBMS3Entry(bw, ref e, enc, entriesStart, i + 1 == entries.Count);
                                entries[i] = e;
                            }

                            byte[] completeTail = new byte[tailLeadBytes.Length + tailBytes.Length];
                            Buffer.BlockCopy(tailLeadBytes, 0, completeTail, 0, tailLeadBytes.Length);
                            Buffer.BlockCopy(tailBytes, 0, completeTail, tailLeadBytes.Length, tailBytes.Length);
                            byte[] rebuiltTail = RebuildBMS3Tail(completeTail, entries);
                            if (rebuiltTail.Length < 8)
                                throw new InvalidDataException("BMS3 langdb tail table is too short.");

                            // The blob size points *after* the first tail pair.
                            // The rest of the table starts there.
                            bw.Write(rebuiltTail, 0, 8);
                            long tailRestStart = bw.BaseStream.Position;
                            long rebuiltBlobSize = tailRestStart - entriesStart;
                            if (rebuiltBlobSize < 0 || rebuiltBlobSize > uint.MaxValue)
                                throw new InvalidDataException("BMS3 langdb entries section is too large.");
                            bw.BaseStream.Position = entriesBlobSizePos;
                            bw.Write((uint)rebuiltBlobSize);
                            bw.BaseStream.Position = tailRestStart;

                            if (rebuiltTail.Length > 8)
                                bw.Write(rebuiltTail, 8, rebuiltTail.Length - 8);
                        }

                        return "File " + fi.Name + " (BMS3 / CSI3 PS2) successfully imported.";
                    }
                }
                catch (Exception ex)
                {
                    return "Erro processando BMS3 langdb " + fi.Name + ": " + ex.Message;
                }
            }
        }
    }
}
