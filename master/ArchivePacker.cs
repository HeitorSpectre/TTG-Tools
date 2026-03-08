using System;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class ArchivePacker : Form
    {
        CommonOpenFileDialog fbd = new CommonOpenFileDialog(); //Для выбора папки
        SaveFileDialog sfd = new SaveFileDialog(); //Для сохранения архива

        public static FileInfo[] fi; //Получение списка файлов
        int archiveVersion;

        public ArchivePacker()
        {
            InitializeComponent();
            fbd.IsFolderPicker = true;
            fbd.EnsurePathExists = true;
        }

        public void AddNewReport(string report)
        {
            if (messageListBox.InvokeRequired)
            {
                messageListBox.Invoke(new ReportHandler(AddNewReport), report);
            }
            else
            {
                messageListBox.Items.Add(report);
                messageListBox.SelectedIndex = messageListBox.Items.Count - 1;
                messageListBox.SelectedIndex = -1;
            }
        }

        public void Progress(int i)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressHandler(Progress), i);
            }
            else
            {
                progressBar1.Value = i;
            }
        }

        public void SetMaximum(int i)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressHandler(SetMaximum), i);
            }
            else
            {
                progressBar1.Maximum = i;
            }
        }

        private static byte[] ZlibCompressor(byte[] bytes) //Для старых архивов (с версии 3 по 7)
        {
            byte[] retBytes = new byte[bytes.Length];

            using (MemoryStream outMemoryStream = new MemoryStream())
            {
                using (Joveler.ZLibWrapper.ZLibStream outZStream = new Joveler.ZLibWrapper.ZLibStream(outMemoryStream, Joveler.ZLibWrapper.ZLibMode.Compress, Joveler.ZLibWrapper.ZLibCompLevel.Level9))
                {
                    using (Stream inMemoryStream = new MemoryStream(bytes))
                    {

                        Methods.CopyStream(inMemoryStream, outZStream);
                        outZStream.Flush();
                        retBytes = outMemoryStream.ToArray();
                    }
                }
            }

            return retBytes;
        }

        private static byte[] OodleCompress(byte[] bytes)
        {
            byte[] retVal = new byte[bytes.Length];
            long bufSize = bytes.Length;
            int compressedSize = OodleTools.Imports.OodleLZ_Compress(OodleTools.OodleFormat.LZHLW, bytes, bufSize, retVal, OodleTools.OodleCompressionLevel.Optimal5, 0, 0, 0);
            bytes = new byte[compressedSize];
            Array.Copy(retVal, 0, bytes, 0, bytes.Length);
            retVal = null;
            return bytes;
        }

        private static byte[] DeflateCompressor(byte[] bytes) //Для старых (версии 8 и 9) и новых архивов
        {
            byte[] retVal;
            using (MemoryStream compressedMemoryStream = new MemoryStream())
            {
                using (System.IO.Compression.DeflateStream compressStream = new System.IO.Compression.DeflateStream(compressedMemoryStream, System.IO.Compression.CompressionMode.Compress))
                {
                    using(MemoryStream inMemStream = new MemoryStream(bytes))
                    {
                        inMemStream.CopyTo(compressStream);
                        compressStream.Close();
                        retVal = compressedMemoryStream.ToArray();
                    }
                }
            }
            return retVal;
        }

        

        private static byte[] encryptFunction(byte[] bytes, byte[] key, int archiveVersion)
        {
            BlowFishCS.BlowFish enc = new BlowFishCS.BlowFish(key, archiveVersion);
            //Methods.meta_crypt(bytes, key, archiveVersion, false);
            return enc.Crypt_ECB(bytes, archiveVersion, false);
        }

        private static bool UsesLegacy1132Ttarch2Builder(int selectedGameIndex)
        {
            if (selectedGameIndex < 0 || selectedGameIndex >= MainMenu.gamelist.Count)
            {
                return false;
            }

            string gameName = MainMenu.gamelist[selectedGameIndex].gamename;

            return (gameName == "Sam & Max: Save the World - Remastered")
                || (gameName == "Sam & Max: Beyond Time and Space - Remastered")
                || (gameName == "Sam & Max: The Devil's Playhouse - Remastered")
                || (gameName == "Poker Night at the Inventory Remastered");
        }

        void ttarch2BuilderLegacy1132(string inputFolder, string outputPath, bool compression, bool encryption, bool encLua, byte[] key, int versionArchive, bool newEngine)
        {
            DirectoryInfo di = new DirectoryInfo(inputFolder);
            fi = di.GetFiles("*", SearchOption.AllDirectories);
            ulong[] nameCrc = new ulong[fi.Length];
            string[] name = new string[fi.Length];
            ulong offset = 0;

            for (int i = 0; i < fi.Length; i++)
            {
                if ((fi[i].Extension.ToLower() == ".lua") && encLua)
                {
                    name[i] = !newEngine ? fi[i].Name.Replace(".lua", ".lenc") : fi[i].Name;
                }
                else
                {
                    name[i] = fi[i].Name;
                }

                nameCrc[i] = CRCs.CRC64(0, name[i].ToLower());
            }

            for (int k = 0; k < fi.Length - 1; k++)
            {
                for (int l = k + 1; l < fi.Length; l++)
                {
                    if (nameCrc[l] < nameCrc[k])
                    {
                        FileInfo temp = fi[k];
                        fi[k] = fi[l];
                        fi[l] = temp;

                        string tempStr = name[k];
                        name[k] = name[l];
                        name[l] = tempStr;

                        ulong tempCrc = nameCrc[k];
                        nameCrc[k] = nameCrc[l];
                        nameCrc[l] = tempCrc;
                    }
                }
            }

            uint infoSize = (uint)fi.Length * (8 + 8 + 4 + 4 + 2 + 2);
            uint dataSize = 0;
            uint nameSize = 0;

            for (int j = 0; j < fi.Length; j++)
            {
                nameSize += (uint)name[j].Length + 1;
                dataSize += (uint)fi[j].Length;
            }

            nameSize = (uint)Methods.pad_it(nameSize, 0x10000);
            byte[] infoTable = new byte[infoSize];
            byte[] namesTable = new byte[nameSize];

            uint nameOffset = 0;
            for (int d = 0; d < fi.Length; d++)
            {
                name[d] += "\0";
                Array.Copy(Encoding.ASCII.GetBytes(name[d]), 0, namesTable, nameOffset, name[d].Length);
                nameOffset += (uint)name[d].Length;
            }

            byte[] ncttHeader = Encoding.ASCII.GetBytes("NCTT");
            byte[] att = versionArchive == 1 ? Encoding.ASCII.GetBytes("3ATT") : Encoding.ASCII.GetBytes("4ATT");
            ulong commonSize = versionArchive == 1 ? dataSize + infoSize + nameSize + 16UL : dataSize + infoSize + nameSize + 12UL;

            uint ns = nameSize;
            uint tmp;
            ulong fileOffset = 0;

            SetMaximum(fi.Length);

            for (int k = 0; k < fi.Length; k++)
            {
                Array.Copy(BitConverter.GetBytes(nameCrc[k]), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes(fileOffset), 0, infoTable, (long)offset, 8);
                offset += 8;
                Array.Copy(BitConverter.GetBytes((int)fi[k].Length), 0, infoTable, (long)offset, 4);
                offset += 4;
                Array.Copy(BitConverter.GetBytes(0), 0, infoTable, (long)offset, 4);
                offset += 4;
                tmp = ns - nameSize;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp / 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                Array.Copy(BitConverter.GetBytes((ushort)(tmp % 0x10000)), 0, infoTable, (long)offset, 2);
                offset += 2;
                ns += (uint)name[k].Length;
                fileOffset += (uint)fi[k].Length;

                Progress(k + 1);
            }

            string format = Methods.GetExtension(outputPath).ToLower() == ".obb" ? ".obb" : ".ttarch2";
            string tempPath = outputPath.Replace(format, ".tmp");

            using (FileStream fs = new FileStream(tempPath, FileMode.Create))
            {
                fs.Write(ncttHeader, 0, 4);
                fs.Write(BitConverter.GetBytes(commonSize), 0, 8);
                fs.Write(att, 0, 4);

                if (versionArchive == 1)
                {
                    fs.Write(BitConverter.GetBytes(2), 0, 4);
                }

                fs.Write(BitConverter.GetBytes(nameSize), 0, 4);
                fs.Write(BitConverter.GetBytes(fi.Length), 0, 4);
                fs.Write(infoTable, 0, (int)infoSize);
                fs.Write(namesTable, 0, (int)nameSize);

                SetMaximum(fi.Length);
                for (int l = 0; l < fi.Length; l++)
                {
                    byte[] file = File.ReadAllBytes(fi[l].FullName);

                    if ((fi[l].Extension.ToLower() == ".lua") && encLua)
                    {
                        file = Methods.encryptLua(file, key, newEngine, 7);
                    }

                    fs.Write(file, 0, file.Length);
                    Progress(l + 1);
                }
            }

            if (!compression)
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(tempPath, outputPath);
                return;
            }

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (FileStream tempFr = new FileStream(tempPath, FileMode.Open))
            {
                ulong fullIt = Methods.pad_it(commonSize, 0x10000);
                uint blocksCount = (uint)fullIt / 0x10000;
                byte[] compressedHeader = encryption ? Encoding.ASCII.GetBytes("ECTT") : Encoding.ASCII.GetBytes("ZCTT");
                byte[] chunkSize = { 0x00, 0x00, 0x01, 0x00 };
                ulong chunkTableSize = 8 * blocksCount + 8;
                offset = chunkTableSize + 12;
                byte[] chunkTable = new byte[chunkTableSize];

                Array.Copy(BitConverter.GetBytes(offset), 0, chunkTable, 0, 8);

                fs.Write(compressedHeader, 0, compressedHeader.Length);
                fs.Write(chunkSize, 0, 4);
                fs.Write(BitConverter.GetBytes(blocksCount), 0, 4);
                fs.Write(chunkTable, 0, chunkTable.Length);

                tempFr.Seek(12, SeekOrigin.Begin);
                SetMaximum((int)blocksCount);

                for (int i = 0; i < blocksCount; i++)
                {
                    byte[] temp = new byte[0x10000];
                    tempFr.Read(temp, 0, temp.Length);
                    byte[] compressedBlock = DeflateCompressor(temp);

                    if (encryption)
                    {
                        compressedBlock = encryptFunction(compressedBlock, key, 7);
                    }

                    offset += (uint)compressedBlock.Length;
                    Array.Copy(BitConverter.GetBytes(offset), 0, chunkTable, 8 + (i * 8), 8);
                    fs.Write(compressedBlock, 0, compressedBlock.Length);
                    Progress(i + 1);
                }

                fs.Seek(12, SeekOrigin.Begin);
                fs.Write(chunkTable, 0, chunkTable.Length);
            }

            File.Delete(tempPath);
            AddNewReport("Legacy compatibility mode (v1.13.2 ttarch2 builder) applied.");
        }


        async Task ttarch2Builder(string inputFolder, string outputPath, bool compression, bool encryption, bool encLua, byte[] key, int versionArchive, bool newEngine, int compressAlgorithm)
        {
            DirectoryInfo di = new DirectoryInfo(inputFolder);
            fi = di.GetFiles("*", SearchOption.AllDirectories);

            if (fi.GroupBy(f => f.Name).Where(group => group.Count() > 1).Select(group => group.Key).Count() > 0)
            {
                fi = fi.Distinct(new FileNameComparer()).ToArray();
                AddNewReport("Found duplicated files in directories. Successfully removed duplicated files.");
            }

            ulong[] nameCRC = new ulong[fi.Length];
            string[] name = new string[fi.Length];
            ulong offset = 0;
            ulong tableSize = (ulong)fi.Length * (8 + 8 + 4 + 4 + 2 + 2);
            int chunkSize = 0x10000; //Default 64KB
            ulong nameSize = 0;
            ulong dataSize = 0;
            ulong commonSize = 0;

            for(int i = 0; i < fi.Length; i++)
            {
                name[i] = (fi[i].Extension == ".lua") && encLua && (!newEngine) ? fi[i].Name.Remove(fi[i].Name.Length - 3, 3) + "lenc" : fi[i].Name;
                nameCRC[i] = CRCs.CRC64(0, name[i].ToLower()); //Calculate crc64 file name with lower characters
                nameSize += (ulong)name[i].Length + 1;
                dataSize += (ulong)fi[i].Length;
            }

            for (int k = 0; k < fi.Length - 1; k++) //Sort file names by less crc64
            {
                for (int l = k + 1; l < fi.Length; l++)
                {
                    if (nameCRC[l] < nameCRC[k])
                    {
                        FileInfo temp = fi[k];
                        fi[k] = fi[l];
                        fi[l] = temp;

                        string temp_str = name[k];
                        name[k] = name[l];
                        name[l] = temp_str;

                        ulong temp_crc = nameCRC[k];
                        nameCRC[k] = nameCRC[l];
                        nameCRC[l] = temp_crc;
                    }
                }
            }

            if (Methods.GetExtension(outputPath).ToLower() == ".obb" && compression)
            {
                compression = false;
                AddNewReport("Unset compression for OBB file");
            }
            byte[] header = Encoding.ASCII.GetBytes("NCTT"); //Prepare non-compressed header (NCTT)

            //ZCTT - compressed archive with zlib/deflate libraries
            //ECTT - encrypted compressed archive with zlib/deflate libraries
            //zCTT - compressed archive with oo2core library
            //eCTT - encrypted compressed archive with oo2core library

            if (compression)
            {
                switch(compressAlgorithm)
                {
                    case 0:
                        header = encryption ? Encoding.ASCII.GetBytes("ECTT") : Encoding.ASCII.GetBytes("ZCTT");
                        break;

                    case 1:
                        header = encryption ? Encoding.ASCII.GetBytes("eCTT") : Encoding.ASCII.GetBytes("zCTT");
                        break;
                }
            }

            byte[] subHeader = versionArchive == 1 ? Encoding.ASCII.GetBytes("3ATT") : Encoding.ASCII.GetBytes("4ATT");

            nameSize = Methods.pad_it(nameSize, 0x10000); //Pad size of file name's block by 64KB
            commonSize = versionArchive == 1 ? dataSize + tableSize + nameSize + 4 + 4 + 4 + 4 : dataSize + tableSize + nameSize + 4 + 4 + 4; //Common archive's size

            int chunksCount = (int)(Methods.pad_it(commonSize, (ulong)chunkSize)) / chunkSize;
            ulong chunksFirstOffset = compressAlgorithm == 1 ? 24 + (ulong)(chunksCount * 8) : 20 + (ulong)(chunksCount * 8);
            ulong[] compressedOffset = new ulong[chunksCount];
            byte[] namesTable = new byte[nameSize];
            byte[] table;
            ulong nameOff = 0;
            int zeros = 0;
            ushort ns = (ushort)nameSize;
            ushort tmpNS = 0;
            uint chunkOff = 0;
            ulong chunkOffset = chunksFirstOffset;
            byte[] tmp;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter mbw = new BinaryWriter(ms))
                {
                    for (int i = 0; i < fi.Length; i++)
                    {
                        tmp = Encoding.ASCII.GetBytes(name[i] + '\0');
                        Array.Copy(tmp, 0, namesTable, (int)nameOff, tmp.Length);
                        nameOff += (ulong)tmp.Length;

                        mbw.Write(nameCRC[i]);
                        mbw.Write(offset);
                        mbw.Write((int)fi[i].Length);
                        mbw.Write(zeros);
                        tmpNS = (ushort)(ns - nameSize);
                        mbw.Write((ushort)(tmpNS / 0x10000)); //Get offset to file name
                        mbw.Write((ushort)(tmpNS % 0x10000)); //The same thing but calculates next offset
                        ns += (ushort)tmp.Length;
                        offset += (ulong)fi[i].Length;
                    }
                }

                table = ms.ToArray();
            }

            if (File.Exists(outputPath)) File.Delete(outputPath);

            FileStream fs = new FileStream(outputPath, FileMode.CreateNew);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(header);
            long pos = 0;

            if(!compression)
            {
                bw.Write(commonSize);
            }
            else
            {
                if(compression && compressAlgorithm == 1) bw.Write(BitConverter.GetBytes(1));
                bw.Write(chunkSize);
                bw.Write(chunksCount);
                bw.Write(chunksFirstOffset);
                pos = bw.BaseStream.Position;

                for(int i = 0; i < chunksCount; i++)
                {
                    bw.Write(compressedOffset[i]); //Write just empty values
                }
            }

            int ch = 0;
            int a = 0;
            SetMaximum(fi.Length);

            //Try write subheader and file table
            int tableChunksCount = versionArchive == 1 ? Methods.pad_size(16 + table.Length + namesTable.Length, chunkSize) / chunkSize : Methods.pad_size(12 + table.Length + namesTable.Length, chunkSize) / chunkSize;
            byte[] chunk = new byte[chunkSize];

            Array.Copy(subHeader, 0, chunk, chunkOff, subHeader.Length);
            chunkOff += (uint)subHeader.Length;
            
            if (versionArchive == 1)
            {
                tmp = BitConverter.GetBytes(2);
                Array.Copy(tmp, 0, chunk, chunkOff, tmp.Length);
                chunkOff += (uint)tmp.Length;
            }

            tmp = BitConverter.GetBytes(chunkSize);
            Array.Copy(tmp, 0, chunk, chunkOff, tmp.Length);
            chunkOff += (uint)tmp.Length;

            tmp = BitConverter.GetBytes((int)fi.Length);
            Array.Copy(tmp, 0, chunk, chunkOff, tmp.Length);
            chunkOff += (uint)tmp.Length;

            tmp = new byte[table.Length + namesTable.Length];
            Array.Copy(table, 0, tmp, 0, table.Length);
            Array.Copy(namesTable, 0, tmp, table.Length, namesTable.Length);
            uint chunkFile = (uint)tmp.Length;
            uint chunkFileOff = 0;

            for (int c = 0; c < tableChunksCount; c++)
            {
                uint chSize = (uint)chunkFile;

                if (chunkOff + chSize > chunkSize)
                {
                    chSize = chunkOff == 0 ? (uint)chunkSize : (uint)chunkSize - chunkOff;
                }

                Array.Copy(tmp, chunkFileOff, chunk, chunkOff, chSize);

                chunkOff += chSize;
                chunkFile -= chSize;
                chunkFileOff += chSize;

                if (chunkOff >= chunkSize)
                {
                    if (compression)
                    {
                        byte[] check = versionArchive == 2 && compressAlgorithm == 1 ? OodleCompress(chunk) : DeflateCompressor(chunk);

                        if (check.Length < chunk.Length)
                        {
                            chunk = new byte[check.Length];
                            Array.Copy(check, 0, chunk, 0, chunk.Length);
                            check = null;
                        }

                        if (encryption)
                        {
                            chunk = encryptFunction(chunk, key, 7);
                        }
                    }

                    bw.Write(chunk);

                    if (compression)
                    {
                        chunkOffset += (ulong)chunk.Length;
                        compressedOffset[ch] = chunkOffset;
                    }

                    chunkOff = 0;
                    chunk = new byte[chunkSize];
                    ch++;
                }
            }

            while (ch < chunksCount && a < fi.Length)
            {
                byte[] file = File.ReadAllBytes(fi[a].FullName);

                if (encLua && ((fi[a].Extension.ToLower() == ".lua") || (fi[a].Extension.ToLower() == ".lenc")))
                {
                    file = Methods.encryptLua(file, key, newEngine, 7);
                }

                int fileChunkCount = Methods.pad_size((int)chunkOff + file.Length, chunkSize) / chunkSize;
                chunkFile = (uint)file.Length;
                chunkFileOff = 0;

                for (int c = 0; c < fileChunkCount; c++)
                {
                    uint chSize = chunkFile;

                    if (chunkOff + chSize > chunkSize)
                    {
                        chSize = chunkOff == 0 ? (uint)chunkSize : (uint)chunkSize - chunkOff;
                    }

                    Array.Copy(file, chunkFileOff, chunk, chunkOff, chSize);

                    chunkOff += chSize;
                    chunkFile -= chSize;
                    chunkFileOff += chSize;

                    if ((chunkOff >= chunkSize) || ((ch + 1 == chunksCount) && (a + 1 == fi.Length)))
                    {                        
                        if ((ch + 1 == chunksCount) && (chunkOff < chunkSize))
                        {
                            tmp = new byte[chunkOff];
                            Array.Copy(chunk, 0, tmp, 0, tmp.Length);
                            chunk = new byte[tmp.Length];
                            Array.Copy(tmp, 0, chunk, 0, tmp.Length);
                        }

                        if (compression)
                        {
                            byte[] check = versionArchive == 2 && compressAlgorithm == 1 ? OodleCompress(chunk) : DeflateCompressor(chunk);

                            if (check.Length < chunk.Length)
                            {
                                chunk = new byte[check.Length];
                                Array.Copy(check, 0, chunk, 0, chunk.Length);
                                check = null;
                            }

                            if (encryption)
                            {
                                chunk = encryptFunction(chunk, key, 7);
                            }
                        }

                        bw.Write(chunk);

                        if (compression)
                        {
                            chunkOffset += (uint)chunk.Length;
                            compressedOffset[ch] = chunkOffset;
                        }

                        chunkOff = 0;
                        chunk = new byte[chunkSize];
                        ch++;
                    }
                }

                AddNewReport("File " + fi[a].Name + " packed");
                Progress(a + 1);
                a++;
            }

            if (compression)
            {
                bw.BaseStream.Seek(pos, SeekOrigin.Begin);

                for (int c = 0; c < chunksCount; c++)
                {
                    bw.Write(compressedOffset[c]);
                }
            }

            bw.Close();
            fs.Close();

            AddNewReport("Packing archive complete");
        }

        async Task ttarchBuilder(string inputFolder, string outputPath, byte[] key, bool compression, int versionArchive, bool encryptCheck, bool DontEncLua, int compressAlgorithm) //Функция сборки
        {
            DirectoryInfo di = new DirectoryInfo(inputFolder);
            DirectoryInfo[] di1 = di.GetDirectories("*", SearchOption.AllDirectories); //Just for fun if files were in different directories for Telltale Tool engine this optional

            FileInfo[] fi = di.GetFiles("*", SearchOption.AllDirectories);
            int chunkSize = versionArchive == 7 ? 0x20000 : 0x10000;
            int headerChunkSize = versionArchive == 7 ? 128 : 64;

            if(fi.GroupBy(f => f.Name).Where(group => group.Count() > 1).Select(group => group.Key).Count() > 0)
            {
                fi = fi.Distinct(new FileNameComparer()).ToArray();
                AddNewReport("Found duplicated files in directories. Successfully removed duplicated files.");
            }

            uint fileOffset = 0;
            uint tableSize = 0;
            uint[] compressedChunks = null;

            if (versionArchive == 2 && compression) compression = false;

            MemoryStream ms = new MemoryStream();
            BinaryWriter mbw = new BinaryWriter(ms);

            bool WithoutParentFolders = false;
            int directories = di1.Length;

            if (directories == 0)
            {
                directories = 1;
                WithoutParentFolders = true;
            }

            mbw.Write(directories);
            tableSize += 4;
            uint uncompressedHeaderSize = 0;

            for (int i = 0; i < directories; i++) //Get directories' name
            {
                byte[] dirName = !WithoutParentFolders ? Encoding.ASCII.GetBytes(di1[i].Parent.Name + Path.PathSeparator + di1[i].Name) : Encoding.ASCII.GetBytes(di.FullName);
                int dirNameSize = (int)dirName.Length;
                mbw.Write(dirNameSize);
                mbw.Write(dirName);

                tableSize += 4 + (uint)dirName.Length;
            }

            int filesCount = (int)fi.Length;
            mbw.Write(filesCount);
            tableSize += 4;

            for (int i = 0; i < fi.Length; i++)
            {
                string name = (fi[i].Extension.ToLower() == ".lua") && !DontEncLua ? fi[i].Name.Remove(fi[i].Name.Length - 3, 3) + "lenc" : fi[i].Name;

                if (Methods.meta_check(fi[i]) && compression)
                {
                    compression = false;
                    AddNewReport("Found file with meta encryption. Compression is unset.");
                }

                byte[] binFileName = Encoding.ASCII.GetBytes(name);
                int fileNameLen = binFileName.Length;
                int fileSize = (int)fi[i].Length;
                int zeroVal = 0;

                //Record file table with MemoryStream
                mbw.Write(fileNameLen);
                mbw.Write(binFileName);
                mbw.Write(zeroVal);
                mbw.Write(fileOffset);
                mbw.Write(fileSize);

                fileOffset += (uint)fileSize;

                tableSize += 4 + 4 + 4 + 4 + (uint)binFileName.Length;
            }

            if(versionArchive == 4)
            {
                int zero = 0;
                byte val = 0;

                mbw.Write(zero);
                mbw.Write(val);
            }

            if(versionArchive == 2)
            {
                tableSize += 8 + 8 + 4; //Add file offset and archive size + 0xFEEDFACE
                mbw.Write(tableSize);
                mbw.Write(fileOffset);
                uint feedface = 0xfeedface;
                mbw.Write(feedface);
            }

            byte[] table = ms.ToArray();
            uncompressedHeaderSize = (uint)table.Length;

            byte[] crc32Header = CRCs.CRC32_generator(table);

            if (versionArchive <= 2 || encryptCheck) table = encryptFunction(table, key, versionArchive);

            if (versionArchive >= 7 && compression)
            {
                switch (versionArchive)
                {
                    case 7:
                    case 8:
                        table = compressAlgorithm == 0 ? ZlibCompressor(table) : DeflateCompressor(table);
                        break;
                    default:
                        table = DeflateCompressor(table);
                        break;
                }
            }

            tableSize = (uint)table.Length;

            mbw.Close();
            ms.Close();

            int chunksCount = Methods.pad_size((int)fileOffset, (int)chunkSize) / chunkSize;

            compressedChunks = new uint[chunksCount];
            int encrypted = (versionArchive <= 2) || encryptCheck ? 1 : 0; //Set flag for encrypt archive
            int platform = 2; //2 - PC
            int unknown2 = compression ? 2 : 1;

            if (File.Exists(outputPath)) File.Delete(outputPath);

            FileStream fs = new FileStream(outputPath, FileMode.CreateNew);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(versionArchive);
            bw.Write(encrypted);
            bw.Write(platform);
            uint pos = 0;

            if(versionArchive >= 3)
            {
                bw.Write(unknown2);
                int headerChunksCount = compression ? chunksCount : 0;
                bw.Write(headerChunksCount);

                if (compression)
                {
                    for(int i = 0; i < chunksCount; i++)
                    {
                        bw.Write(compressedChunks[i]);
                    }
                }

                pos = (uint)bw.BaseStream.Position;
                bw.Write(fileOffset); //Archive size

                if (versionArchive == 4 || versionArchive >= 7)
                {
                    int priority = 0;
                    int unknownValue = MainMenu.settings.oldXmode ? 1 : 0;
                    byte[] binChunkSize = BitConverter.GetBytes(headerChunkSize);

                    bw.Write(priority);
                    bw.Write(priority);

                    if (versionArchive > 4)
                    {
                        bw.Write(unknownValue);
                        bw.Write(unknownValue);
                        bw.Write(binChunkSize[0]);

                        byte[] zeros = versionArchive == 7 ? new byte[] { 0, 0, 0 } : new byte[] { 0, 0, 0, 0 };

                        bw.Write(zeros);

                        if (versionArchive == 9)
                        {
                            bw.Write(crc32Header);
                            if(compression) bw.Write(uncompressedHeaderSize);
                        }
                    }
                }
            }

            bw.Write(tableSize);
            bw.Write(table);

            SetMaximum(fi.Length);

            int ch = 0;
            int a = 0;

            int chunkOff = 0;
            byte[] chunk = new byte[chunkSize];
            uint compressedArchive = 0;

            while(ch < chunksCount && a < fi.Length)
            {
                string name = (fi[a].Extension.ToLower() == ".lua") && !DontEncLua ? fi[a].Name.Remove(fi[a].Name.Length - 3, 3) + "lenc" : fi[a].Name;
                byte[] file = File.ReadAllBytes(fi[a].FullName);

                int res = Methods.meta_crypt(file, key, versionArchive, false);

                if (!DontEncLua && ((fi[a].Extension.ToLower() == ".lua") || (fi[a].Extension.ToLower() == ".lenc"))) file = Methods.encryptLua(file, key, false, versionArchive);

                int fileChunkCount = Methods.pad_size(chunkOff + file.Length, chunkSize) / chunkSize;
                int chunkFile = file.Length;
                int chunkFileOff = 0;

                for(int c = 0; c < fileChunkCount; c++)
                {
                    int chSize = chunkFile;
                    
                    if(chunkOff + chSize > chunkSize)
                    {
                        chSize = chunkOff == 0 ? chunkSize : chunkSize - chunkOff;
                    }

                    Array.Copy(file, chunkFileOff, chunk, chunkOff, chSize);
                    
                    chunkOff += chSize;
                    chunkFile -= chSize;
                    chunkFileOff += chSize;

                    if ((chunkOff >= chunkSize) || ((ch + 1 == chunksCount) && (a + 1 == fi.Length)))
                    {
                        if((ch + 1 == chunksCount) && (chunkOff < chunkSize))
                        {
                            byte[] tmp = new byte[chunkOff];
                            Array.Copy(chunk, 0, tmp, 0, tmp.Length);
                            chunk = new byte[tmp.Length];
                            Array.Copy(tmp, 0, chunk, 0, tmp.Length);
                        }

                        if (compression)
                        {
                            byte[] check = new byte[chunk.Length];
                            if (versionArchive >= 8) check = versionArchive == 8 && compressAlgorithm == 0 ? ZlibCompressor(chunk) : DeflateCompressor(chunk);
                            else check = ZlibCompressor(chunk);

                            if(check.Length < chunk.Length)
                            {
                                chunk = new byte[check.Length];
                                Array.Copy(check, 0, chunk, 0, chunk.Length);
                                check = null;
                            }

                            if (encryptCheck)
                            {
                                chunk = encryptFunction(chunk, key, versionArchive);
                            }
                        }

                        bw.Write(chunk);

                        if (compression)
                        {
                            compressedChunks[ch] = (uint)chunk.Length;
                            compressedArchive += (uint)chunk.Length;
                        }

                        chunkOff = 0;
                        chunk = new byte[chunkSize];
                        ch++;
                    }
                }

                AddNewReport("File " + fi[a].Name + " packed");
                Progress(a + 1);
                a++;
            }

            if (compression)
            {
                bw.BaseStream.Seek(20, SeekOrigin.Begin);
                
                for(int c = 0; c < chunksCount; c++)
                {
                    bw.Write(compressedChunks[c]);
                }

                bw.BaseStream.Seek(pos, SeekOrigin.Begin);
                bw.Write(compressedArchive);
            }

            bw.Close();
            fs.Close();

            AddNewReport("Packing archive complete");
        }
    

        private void ArchivePacker_Load(object sender, EventArgs e)
        {
            AllowDrop = true;
            DragEnter += ArchivePacker_DragEnter;
            DragDrop += ArchivePacker_DragDrop;
            textBox1.AllowDrop = true;
            textBox1.DragEnter += ArchivePacker_DragEnter;
            textBox1.DragDrop += ArchivePacker_DragDrop;

            for (int i = 0; i < MainMenu.gamelist.Count(); i++)
            {
                comboGameList.Items.Add(i + ". " + MainMenu.gamelist[i].gamename);
            }

            compressionCB.Items.Add("Deflate");
            compressionCB.Items.Add("Oodle LZ");
            compressionCB.SelectedIndex = 0;
            
            newEngineLua.Checked = MainMenu.settings.encNewLua;
            checkCompress.Checked = MainMenu.settings.compressArchive;
            checkXmode.Checked = MainMenu.settings.oldXmode;
            DontEncLuaCheck.Checked = MainMenu.settings.encryptLuaInArchive;
            EncryptIt.Checked = MainMenu.settings.encArchive;
            if (MainMenu.settings.inputDirPath != "") textBox1.Text = MainMenu.settings.inputDirPath;
            if (MainMenu.settings.archivePath != "") textBox2.Text = MainMenu.settings.archivePath;

            textBox3.Enabled = MainMenu.settings.customKey;
            int encKeyIndex = MainMenu.settings.versionArchiveIndex;
            if (MainMenu.settings.archiveFormat == 0) ttarchRB.Checked = true;
            else ttarch2RB.Checked = true;

            comboGameList.SelectedIndex = MainMenu.settings.encKeyIndex;
            versionSelection.SelectedIndex = encKeyIndex;


            if (MainMenu.settings.customKey && Methods.stringToKey(MainMenu.settings.encCustomKey) != null)
            {
                CheckCustomKey.Checked = MainMenu.settings.customKey;
                textBox3.Text = MainMenu.settings.encCustomKey;
            }
        }

        private void ttarchRB_CheckedChanged(object sender, EventArgs e)
        {
            versionSelection.Items.Clear();
            versionSelection.Items.Add("2");
            versionSelection.Items.Add("3");
            versionSelection.Items.Add("4");
            versionSelection.Items.Add("5");
            versionSelection.Items.Add("6");
            versionSelection.Items.Add("7");
            versionSelection.Items.Add("8");
            versionSelection.Items.Add("9");
            versionSelection.SelectedIndex = 0;

            checkXmode.Visible = true;
            checkXmode.Checked = MainMenu.settings.oldXmode;
            
            MainMenu.settings.archiveFormat = 0;
            MainMenu.settings.versionArchiveIndex = versionSelection.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void ttarch2RB_CheckedChanged(object sender, EventArgs e)
        {
            versionSelection.Items.Clear();
            versionSelection.Items.Add("1");
            versionSelection.Items.Add("2");
            versionSelection.SelectedIndex = 0;

            checkXmode.Visible = false;

            MainMenu.settings.archiveFormat = 1;
            MainMenu.settings.versionArchiveIndex = versionSelection.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox1.Text = fbd.FileName;

                MainMenu.settings.inputDirPath = textBox1.Text;
                Settings.SaveConfig(MainMenu.settings);
            }
            else
            {
                textBox1.Clear();
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            MainMenu.settings.inputDirPath = textBox1.Text.Trim();
            Settings.SaveConfig(MainMenu.settings);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ttarchRB.Checked)
            {
                sfd.Filter = "TTARCH archive (*.ttarch) | *.ttarch";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = sfd.FileName;
                    
                    MainMenu.settings.archivePath = textBox2.Text;
                    Settings.SaveConfig(MainMenu.settings);
                }
                else
                {
                    textBox2.Clear();
                }
            }
            else
            {
                sfd.Filter = "TTARCH2 archive (*.ttarch2) | *.ttarch2| Android archive (*.obb) | *.obb";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = sfd.FileName;

                    MainMenu.settings.archivePath = textBox2.Text;
                    Settings.SaveConfig(MainMenu.settings);
                }
                else
                {
                    textBox2.Clear();
                }
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            MainMenu.settings.archivePath = textBox2.Text.Trim();
            Settings.SaveConfig(MainMenu.settings);
        }

        private void ArchivePacker_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string[] droppedItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            if ((droppedItems == null) || (droppedItems.Length == 0))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string droppedPath = droppedItems[0];
            if (Directory.Exists(droppedPath))
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            if (!File.Exists(droppedPath))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string ext = Methods.GetExtension(droppedPath).ToLower();
            e.Effect = ((ext == ".ttarch") || (ext == ".ttarch2") || (ext == ".obb")) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ArchivePacker_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            if ((droppedItems == null) || (droppedItems.Length == 0)) return;

            string droppedPath = droppedItems[0];
            if (Directory.Exists(droppedPath))
            {
                textBox1.Text = droppedPath;
                MainMenu.settings.inputDirPath = droppedPath.Trim();
                Settings.SaveConfig(MainMenu.settings);
                return;
            }

            if (!File.Exists(droppedPath)) return;

            string ext = Methods.GetExtension(droppedPath).ToLower();
            if ((ext == ".ttarch") || (ext == ".ttarch2") || (ext == ".obb"))
            {
                textBox2.Text = droppedPath;
                MainMenu.settings.archivePath = droppedPath.Trim();
                Settings.SaveConfig(MainMenu.settings);
            }
        }

        private async void buildButton_Click(object sender, EventArgs e)
        {
            if ((MainMenu.settings.inputDirPath != "") && (MainMenu.settings.archivePath != ""))
            {
                DirectoryInfo checkDI = new DirectoryInfo(MainMenu.settings.inputDirPath);
                if (checkDI.Exists)
                {
                    string example = "96CA99A085CF988AE4DBE2CDA6968388C08B99E39ED89BB6D790DCBEAD9D9165B6A69EBBC2C69EB3E7E3E5D5AB6382A09CC4929FD1D5A4";
                    
                    archiveVersion = Convert.ToInt32(versionSelection.SelectedItem);

                    byte[] keyEnc;
                    if(CheckCustomKey.Checked) keyEnc = Methods.stringToKey(textBox3.Text);
                    else keyEnc = MainMenu.gamelist[comboGameList.SelectedIndex].key;
                    
                    if((keyEnc == null) && (MainMenu.settings.encArchive || !MainMenu.settings.encryptLuaInArchive))
                    {
                        if (!CheckCustomKey.Checked)
                        {
                            MainMenu.settings.encArchive = false;
                            MainMenu.settings.encryptLuaInArchive = true;
                        }
                        else
                        {
                            MessageBox.Show("Check string for correctly. Here's example of encryption key:\r\n" + example, "Error");
                            return;
                        }
                    }

                    if (Methods.GetExtension(MainMenu.settings.archivePath).ToLower() == ".obb") MainMenu.settings.compressArchive = false;

                    int algorithmCompress = 0;

                    if (ttarch2RB.Checked && (versionSelection.SelectedIndex == 1) && (compressionCB.SelectedIndex == 1)) algorithmCompress = 1;
                    else if (ttarchRB.Checked && (versionSelection.SelectedIndex == 5 || versionSelection.SelectedIndex == 6) && (compressionCB.SelectedIndex == 1)) algorithmCompress = 1;
                    else if (ttarchRB.Checked && versionSelection.SelectedIndex == 7) algorithmCompress = 1; //Latest version uses deflate algorithm

                    if (messageListBox.Items.Count > 1) messageListBox.Items.Clear();

                    if(ttarchRB.Checked)
                    {
                        await Task.Run(() => ttarchBuilder(MainMenu.settings.inputDirPath, MainMenu.settings.archivePath, keyEnc, MainMenu.settings.compressArchive, archiveVersion, MainMenu.settings.encArchive, MainMenu.settings.encryptLuaInArchive, algorithmCompress));
                    }
                    else
                    {
                        bool useLegacyBuilder = UsesLegacy1132Ttarch2Builder(comboGameList.SelectedIndex);

                        if (useLegacyBuilder)
                        {
                            AddNewReport("Enabled legacy compatibility mode.");
                            await Task.Run(() => ttarch2BuilderLegacy1132(MainMenu.settings.inputDirPath, MainMenu.settings.archivePath, MainMenu.settings.compressArchive, MainMenu.settings.encArchive, !MainMenu.settings.encryptLuaInArchive, keyEnc, archiveVersion, MainMenu.settings.encNewLua));
                        }
                        else
                        {
                            await Task.Run(() => ttarch2Builder(MainMenu.settings.inputDirPath, MainMenu.settings.archivePath, MainMenu.settings.compressArchive, MainMenu.settings.encArchive, !MainMenu.settings.encryptLuaInArchive, keyEnc, archiveVersion, MainMenu.settings.encNewLua, algorithmCompress));
                        }
                    }

                    /*if (ttarchRB.Checked == true) ttarchBuilder(MainMenu.settings.inputDirPath, MainMenu.settings.archivePath, keyEnc, MainMenu.settings.compressArchive, archiveVersion, MainMenu.settings.encArchive, MainMenu.settings.encryptLuaInArchive, algorithmCompress);
                    else builder_ttarch2(MainMenu.settings.inputDirPath, MainMenu.settings.archivePath, MainMenu.settings.compressArchive, MainMenu.settings.encArchive, !MainMenu.settings.encryptLuaInArchive, keyEnc, archiveVersion, MainMenu.settings.encNewLua, algorithmCompress);*/
                }
                else MessageBox.Show("This folder doesn't exist!", "Error");
                
            }
            else MessageBox.Show("Check paths!", "Error");
        }

        private void ArchivePacker_FormClosing(object sender, FormClosingEventArgs e)
        {
            comboGameList.Items.Clear();
        }

        private void comboGameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encKeyIndex = comboGameList.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void newEngineLua_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encNewLua = newEngineLua.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void CheckCustomKey_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.customKey = CheckCustomKey.Checked;
            textBox3.Enabled = MainMenu.settings.customKey;
            Settings.SaveConfig(MainMenu.settings);

            if ((MainMenu.settings.customKey == true) &&
               ((MainMenu.settings.encCustomKey != "") && (MainMenu.settings.encCustomKey != null)))
            {
                textBox3.Text = MainMenu.settings.encCustomKey;
            }
            else
            {
                textBox3.Text = "";
            }
        }

        private void DontEncLuaCheck_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encryptLuaInArchive = DontEncLuaCheck.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void EncryptIt_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encArchive = EncryptIt.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkCompress_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.compressArchive = checkCompress.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void checkXmode_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.oldXmode = checkXmode.Checked;
            Settings.SaveConfig(MainMenu.settings);
        }

        private void versionSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ttarchRB.Checked)
            {
                compressionCB.Items.Clear();

                compressionCB.Items.Add("Zlib");
                compressionCB.Items.Add("Deflate");
                compressionCB.SelectedIndex = 0;

                compressionCB.Visible = versionSelection.SelectedIndex == 5 || versionSelection.SelectedIndex == 6;
                compressionLabel.Visible = versionSelection.SelectedIndex == 5 || versionSelection.SelectedIndex == 6;

                checkXmode.Visible = versionSelection.SelectedIndex >= 5;
                checkCompress.Visible = versionSelection.SelectedIndex >= 1;
                newEngineLua.Visible = false;
            }
            else
            {
                compressionCB.Items.Clear();
                compressionCB.Items.Add("Deflate");
                compressionCB.Items.Add("Oodle LZ");
                compressionCB.SelectedIndex = 0;

                compressionLabel.Visible = ttarch2RB.Checked && versionSelection.SelectedIndex == 1;
                compressionCB.Visible = ttarch2RB.Checked && versionSelection.SelectedIndex == 1;
                newEngineLua.Visible = true;
                checkXmode.Visible = false;
            }

            MainMenu.settings.versionArchiveIndex = versionSelection.SelectedIndex;
            Settings.SaveConfig(MainMenu.settings);
        }
    }
}
