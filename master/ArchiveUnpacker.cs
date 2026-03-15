using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using TTG_Tools.ClassesStructs;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class ArchiveUnpacker : Form
    {
        public ArchiveUnpacker()
        {
            InitializeComponent();
            filesDataGridView.AllowUserToAddRows = false;
        }

        private static ClassesStructs.TtarchClass ttarch;
        private static ClassesStructs.Ttarch2Class ttarch2;
        private bool decrypt = false;
        private byte[] key = null;

        private string SelectFolder()
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;
            return folderDialog.ShowDialog() == CommonFileDialogResult.Ok ? folderDialog.FileName : null;
        }

        private async Task OpenArchiveFile(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                if(fi.Attributes.HasFlag(FileAttributes.ReadOnly) || !fi.Attributes.HasFlag(FileAttributes.Normal)) fi.Attributes = FileAttributes.Normal;

                ttarch = null;
                ttarch2 = null;

                if (progressBar1.Value > 0) progressBar1.Value = 0;

                byte[] key = MainMenu.gamelist[gameListCB.SelectedIndex].key;

                switch (fi.Extension.ToLower())
                {
                    case ".ttarch":
                        ttarch = new ClassesStructs.TtarchClass();
                        await Task.Run(() => ReadHeaderTtarch(fi.FullName, key));

                        if (ttarch != null)
                        {
                            fileFormatsCB.Items.Clear();

                            getArchiveInfo();
                            populateFileFormats(ttarch.fileFormats);
                            applyFilters();
                        }
                        break;

                    case ".ttarch2":
                    case ".obb":
                        ttarch2 = new ClassesStructs.Ttarch2Class();
                        await Task.Run(() => ReadHeaderTtarch2(fi.FullName, key));

                        if(ttarch2 != null)
                        {
                            fileFormatsCB.Items.Clear();

                            getArchiveInfo();
                            populateFileFormats(ttarch2.fileFormats);
                            applyFilters();
                        }

                        break;

                    default:
                        MessageBox.Show("Unsupported file format. Please choose a .ttarch, .ttarch2 or .obb file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                }

                if (Form.ActiveForm != null) Form.ActiveForm.Text = "Archive unpacker. Opened file: " + fi.Name;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open archive. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void populateFileFormats(List<string> formats)
        {
            fileFormatsCB.Items.Clear();

            if (formats != null && formats.Count > 0)
            {
                if (formats.Count > 1) fileFormatsCB.Items.Add("All files");

                formats.Sort();

                for (int i = 0; i < formats.Count; i++)
                {
                    fileFormatsCB.Items.Add(formats[i]);
                }

                fileFormatsCB.SelectedIndex = 0;
            }
            else
            {
                fileFormatsCB.Items.Add("All files");
                fileFormatsCB.SelectedIndex = 0;
            }
        }

        private string getSelectedFormat()
        {
            if (fileFormatsCB.Items.Count == 0) return "All files";
            return string.IsNullOrEmpty(fileFormatsCB.Text) ? "All files" : fileFormatsCB.Text;
        }

        private bool isSearchEnabled()
        {
            return searchFilesByNameCB.Checked && !string.IsNullOrWhiteSpace(searchTB.Text);
        }

        private bool hasFilteredResults()
        {
            if (ttarch != null)
            {
                return getFilteredTtarchFiles().Length > 0;
            }

            if (ttarch2 != null)
            {
                return getFilteredTtarch2Files().Length > 0;
            }

            return false;
        }

        private void updateExtractionActions(bool hasResults)
        {
            actionsToolStripMenuItem.Enabled = ttarch != null || ttarch2 != null;
            unpackToolStripMenuItem.Enabled = hasResults;
            unpackSelectedToolStripMenuItem.Enabled = hasResults;
        }


        private ClassesStructs.TtarchClass.ttarchFiles[] getFilteredTtarchFiles()
        {
            var files = ttarch.files.AsEnumerable();
            string format = getSelectedFormat();

            if (format != "All files")
            {
                files = files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower());
            }

            if (isSearchEnabled())
            {
                string pattern = searchTB.Text.ToLower();
                files = files.Where(x => x.fileName.ToLower().Contains(pattern));
            }

            return files.ToArray();
        }

        private ClassesStructs.Ttarch2Class.Ttarch2files[] getFilteredTtarch2Files()
        {
            var files = ttarch2.files.AsEnumerable();
            string format = getSelectedFormat();

            if (format != "All files")
            {
                files = files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower());
            }

            if (isSearchEnabled())
            {
                string pattern = searchTB.Text.ToLower();
                files = files.Where(x => x.fileName.ToLower().Contains(pattern));
            }

            return files.ToArray();
        }

        private void applyFilters()
        {
            if (ttarch != null)
            {
                var filteredFiles = getFilteredTtarchFiles();
                loadTtarchData(filteredFiles);
                updateExtractionActions(filteredFiles.Length > 0);
            }
            else if (ttarch2 != null)
            {
                var filteredFiles = getFilteredTtarch2Files();
                loadTtarch2Data(filteredFiles);
                updateExtractionActions(filteredFiles.Length > 0);
            }
            else
            {
                updateExtractionActions(false);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        void Progress(int i)
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

        void SetMinimum(int i)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressHandler(SetMinimum), i);
            }
            else
            {
                progressBar1.Minimum = i;
            }
        }

        void SetMaximum(int i)
        {
            if(progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new ProgressHandler(SetMaximum), i);
            }
            else
            {
                progressBar1.Maximum = i;
            }
        }

        private static byte[] decompressBlock(byte[] bytes, int algorithmCompress)
        {
            try
            {
                byte[] retBuf = null;

                if(algorithmCompress == -1)
                {
                    retBuf = decompressBlock(bytes);
                }

                switch (algorithmCompress)
                {
                    case 0:
                        retBuf = ZLibDecompressor(bytes);
                        break;

                    case 1:
                        retBuf = DeflateDecompressor(bytes);
                        break;

                    case 2:
                        retBuf = OodleDecompressor(bytes);
                        break;
                        
                }

                return retBuf;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] decompressBlock(byte[] bytes)
        {
            try
            {
                byte[] buf = ZLibDecompressor(bytes);
                if (ttarch != null) ttarch.compressAlgorithm = 0;
                else ttarch2.compressAlgorithm = 0;
                return buf;
            }
            catch
            {
                try
                {
                    //Try deflate decompress
                    byte[] buf = DeflateDecompressor(bytes);
                    if (ttarch != null) ttarch.compressAlgorithm = 1;
                    else ttarch2.compressAlgorithm = 1;
                    return buf;
                }
                catch
                {
                    try
                    {
                        byte[] buf = OodleDecompressor(bytes);
                        if (ttarch != null) ttarch.compressAlgorithm = -1;
                        else ttarch2.compressAlgorithm = 2;
                        return buf;
                    }
                    catch
                    {
                        //Else return empty bytes
                        if (ttarch != null) ttarch.compressAlgorithm = -1; //Unknown algorithm
                        else ttarch2.compressAlgorithm = -1;
                        return null;
                    }
                }
            }
        }

        private static byte[] DeflateDecompressor(byte[] bytes) //Для старых (версии 8 и 9) и новых архивов
        {
            byte[] retVal;
            using (MemoryStream decompressedMemoryStream = new MemoryStream(bytes))
            {
                using (System.IO.Compression.DeflateStream decompressStream = new System.IO.Compression.DeflateStream(decompressedMemoryStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    using (MemoryStream memOutStream = new MemoryStream())
                    {
                        decompressStream.CopyTo(memOutStream);
                        retVal = memOutStream.ToArray();
                    }
                }
            }
            return retVal;
        }

        private static byte[] ZLibDecompressor(byte[] bytes)
        {
            byte[] retBytes = new byte[bytes.Length];

            using (Stream inMemoryStream = new MemoryStream(bytes))
            {
                using (Joveler.ZLibWrapper.ZLibStream inZStream = new Joveler.ZLibWrapper.ZLibStream(inMemoryStream, Joveler.ZLibWrapper.ZLibMode.Decompress))
                {
                    using (MemoryStream outMemoryStream = new MemoryStream())
                    {

                        Methods.CopyStream(inZStream, outMemoryStream);
                        inZStream.Flush();
                        retBytes = outMemoryStream.ToArray();
                    }
                }
            }

            return retBytes;
        }

        private static byte[] OodleDecompressor(byte[] bytes)
        {
            byte[] retBytes = new byte[bytes.Length];

            long bufSize = bytes.Length;
            long decBufSize = bytes.Length;
            if (ttarch2 != null)
            {
                decBufSize = ttarch2.chunkSize;
                retBytes = new byte[decBufSize];
            }

            int size = OodleTools.Imports.OodleLZ_Decompress(bytes, bufSize, retBytes, decBufSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            byte[] tmp = new byte[size];
            Array.Copy(retBytes, 0, tmp, 0, tmp.Length);

            return tmp;
        }

        private void ReadHeaderTtarch(string path, byte[] key)
        {
            try
            {
                ttarch.filePath = path;
                ttarch.fileFormats = new List<string>();
                FileStream fs = new FileStream(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                ttarch.version = br.ReadInt32();
                int encryption = br.ReadInt32();
                int two = br.ReadInt32();

                int countCompressedBlocks;
                int val = 0;

                if (ttarch.version > 2)
                {
                    val = br.ReadInt32();
                    countCompressedBlocks = br.ReadInt32();

                    if(val == 2)
                    {
                        ttarch.compressedBlocks = new int[countCompressedBlocks];
                        ttarch.isCompressed = true;

                        for(int k = 0; k < countCompressedBlocks; k++)
                        {
                            ttarch.compressedBlocks[k] = br.ReadInt32();
                        }
                    }

                    uint arcSize = br.ReadUInt32(); //Size of block with files

                    if (ttarch.version >= 4)
                    {
                        int priority = br.ReadInt32();
                        int priority2 = br.ReadInt32();

                        if(ttarch.version >= 7)
                        {
                            int someVal = br.ReadInt32();
                            int someVal2 = br.ReadInt32();

                            ttarch.isXmode = someVal == 1 || someVal2 == 1;
                            ttarch.chunkSize = br.ReadInt32();

                            if(ttarch.version > 7)
                            {
                                byte b = br.ReadByte();

                                if(ttarch.version == 9)
                                {
                                    uint crc32 = br.ReadUInt32();
                                }
                            }
                        }
                    }
                }

                int headerSize = br.ReadInt32();
                int cHeaderSize = -1;

                if (ttarch.version >= 7 && val == 2)
                {
                    cHeaderSize = br.ReadInt32();
                }

                byte[] header = ttarch.version >= 7 && val == 2 ? br.ReadBytes(cHeaderSize) : br.ReadBytes(headerSize);

                ttarch.filesOffset = (uint)br.BaseStream.Position;

                if(ttarch.version >= 7 && val == 2)
                {
                    header = decompressBlock(header);
                }

                if(encryption == 1)
                {
                    ttarch.isEncrypted = true;
                    BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, ttarch.version);
                    header = dec.Crypt_ECB(header, ttarch.version, true);
                }

                using (MemoryStream ms = new MemoryStream(header))
                {
                    using (BinaryReader mbr = new BinaryReader(ms))
                    {
                        int dirsCount = mbr.ReadInt32();
                        
                        for(int d = 0; d < dirsCount; d++)
                        {
                            int nameLen = mbr.ReadInt32();
                            byte[] tmpName = mbr.ReadBytes(nameLen);
                            string name = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmpName);
                        }

                        int filesCount = mbr.ReadInt32();

                        ttarch.files = new ClassesStructs.TtarchClass.ttarchFiles[filesCount];

                        for(int f = 0; f < filesCount; f++)
                        {
                            int nameLen = mbr.ReadInt32();
                            byte[] tmpName = mbr.ReadBytes(nameLen);
                            ttarch.files[f].fileName = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(tmpName);
                            int zeroVal = mbr.ReadInt32(); //always shows 0 value
                            ttarch.files[f].fileOffset = mbr.ReadUInt32();
                            ttarch.files[f].fileSize = mbr.ReadInt32();

                            if (((Methods.GetExtension(ttarch.files[f].fileName).ToLower() == ".lenc") || (Methods.GetExtension(ttarch.files[f].fileName).ToLower() == ".lua"))
                                && !ttarch.isEncryptedLua)
                            {
                                ttarch.isEncryptedLua = Methods.GetExtension(ttarch.files[f].fileName).ToLower() == ".lenc";

                                byte[] tmp = getTtarchFile(ttarch, ttarch.files[f], key, ttarch.chunkSize * 1024, br);

                                ttarch.isEncryptedLua = Methods.isLuaEncrypted(tmp);
                            }

                            string ext = Methods.GetExtension(ttarch.files[f].fileName);

                            if ((ext != "") && !ttarch.fileFormats.Contains(ext))
                            {
                                ttarch.fileFormats.Add(ext);
                            }
                        }
                    }
                }

                //Check oldest compressed archives. Need to find out compression algorithm (default it must be zlib)
                if(ttarch.version < 7 && ttarch.isCompressed)
                {
                    byte[] tmp = br.ReadBytes(ttarch.compressedBlocks[0]);

                    if(ttarch.isEncrypted)
                    {
                        BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, ttarch.version);
                        tmp = dec.Crypt_ECB(tmp, ttarch.version, true);
                    }

                    tmp = decompressBlock(tmp);
                    tmp = null;
                }

                br.Close();
                fs.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error. Please try another archive or change encryption key.\r\nGot exception:\r\n" + ex.Message, "Something goes wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ttarch = null;
            }
        }

        private void UnpackTtarch(string folderPath, string format, int[] indexes = null)
        {
            var files = getFilteredTtarchFiles();

            FileStream fs = new FileStream(ttarch.filePath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            int count = indexes != null ? indexes.Length : files.Length;

            SetMinimum(0);
            SetMaximum(count);

            int chunkSz = ttarch.chunkSize * 1024;

            for (int i = 0; i < count; i++)
            {
                int ind = indexes != null ? indexes[i] : i;

                byte[] file = getTtarchFile(ttarch, files[ind], key, chunkSz, br);

                string fileName = files[ind].fileName;
                if ((fileName.Substring(fileName.Length - 5, 5) == ".lenc") && decrypt)
                {
                    fileName = fileName.Remove(fileName.Length - 4, 4) + "lua";
                    file = Methods.decryptLua(file, key, ttarch.version);
                }

                File.WriteAllBytes(folderPath + Path.DirectorySeparatorChar + fileName, file);

                Progress(i + 1);
            }

            br.Close();
            fs.Close();
        }

        private void ReadHeaderTtarch2(string path, byte[] key)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                ulong foffset = 0;
                ttarch2.fileFormats = new List<string>();
                ttarch2.fileName = path;

                byte[] header = br.ReadBytes(4);
                foffset += 4;
                ttarch2.compressAlgorithm = -1;
                ttarch2.isCompressed = Encoding.ASCII.GetString(header) != "NCTT"; //If it's a NCTT header then archive is not compressed
                ttarch2.isEncrypted = Encoding.ASCII.GetString(header) == "ECTT" || Encoding.ASCII.GetString(header) == "eCTT";

                if (ttarch2.isCompressed)
                {
                    ttarch2.compressAlgorithm = Encoding.ASCII.GetString(header) == "eCTT" || Encoding.ASCII.GetString(header) == "zCTT" ? 2 : 1;
                    if (Encoding.ASCII.GetString(header) == "eCTT" || Encoding.ASCII.GetString(header) == "zCTT")
                    {
                        int one = br.ReadInt32();
                        foffset += 4;
                    }
                    ttarch2.chunkSize = br.ReadUInt32();
                    int blocksCount = br.ReadInt32();
                    foffset += 4 + 4;
                    ttarch2.compressedBlocks = new ulong[blocksCount];

                    ulong val1 = br.ReadUInt64();
                    ulong val2 = 0;

                    for (int i = 0; i < blocksCount; i++)
                    {
                        val2 = br.ReadUInt64();
                        ttarch2.compressedBlocks[i] = val2 - val1;

                        val1 = val2;
                        foffset += 8;
                    }

                    long pos = br.BaseStream.Position;
                    ttarch2.cFilesOffset = (ulong)br.BaseStream.Position;

                    byte[] tmp = br.ReadBytes((int)ttarch2.compressedBlocks[0]);

                    if(ttarch2.isEncrypted)
                    {
                        BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, 7);
                        tmp = dec.Crypt_ECB(tmp, 7, true);
                    }

                    tmp = decompressBlock(tmp, ttarch2.compressAlgorithm);

                    int suboff = 0;
                    uint filesCount = 0;

                    using (MemoryStream ms = new MemoryStream(tmp))
                    {
                        using (BinaryReader mbr = new BinaryReader(ms))
                        {
                            byte[] subHeader = mbr.ReadBytes(4);
                            suboff += 4;
                            //foffset += 4;
                            if (Encoding.ASCII.GetString(subHeader) == "3ATT")
                            {
                                int two = mbr.ReadInt32();
                                suboff += 4;
                                //foffset += 4;
                            }

                            ttarch2.version = Encoding.ASCII.GetString(subHeader) == "3ATT" ? 1 : 2;

                            uint nameSize = mbr.ReadUInt32();
                            suboff += 4;
                            //foffset += 4;
                            filesCount = mbr.ReadUInt32();
                            suboff += 4;

                            ttarch2.filesOffset = (ulong)suboff + (28 * filesCount) + nameSize;
                            ttarch2.files = new ClassesStructs.Ttarch2Class.Ttarch2files[filesCount];
                        }
                    }

                    if (ttarch2.filesOffset > (ulong)tmp.Length)
                    {
                        br.BaseStream.Seek(pos, SeekOrigin.Begin);
                        int index = (int)(ttarch2.filesOffset / ttarch2.chunkSize) + 1;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            for (int i = 0; i < index; i++)
                            {
                                tmp = br.ReadBytes((int)ttarch2.compressedBlocks[i]);

                                if(ttarch2.isEncrypted)
                                {
                                    BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, 7);
                                    tmp = dec.Crypt_ECB(tmp, 7, true);
                                }

                                tmp = decompressBlock(tmp, ttarch2.compressAlgorithm);

                                ms.Write(tmp, 0, tmp.Length);
                            }

                            tmp = ms.ToArray();
                        }
                    }

                    using (MemoryStream ms = new MemoryStream(tmp))
                    {
                        using (BinaryReader mbr = new BinaryReader(ms))
                        {
                            mbr.BaseStream.Seek(suboff, SeekOrigin.Begin);

                            for(int i = 0; i < (int)filesCount; i++)
                            {
                                ttarch2.files[i].fileNameCRC64 = mbr.ReadUInt64();
                                ttarch2.files[i].fileOffset = mbr.ReadUInt64();
                                ttarch2.files[i].fileSize = mbr.ReadInt32();
                                int unknown = mbr.ReadInt32();
                                ushort nameBlock = mbr.ReadUInt16();
                                ushort nameOff = mbr.ReadUInt16();
                                pos = mbr.BaseStream.Position;
                                ulong nameOffset = (ulong)suboff + (28 * (ulong)filesCount) + (ulong)nameOff + ((ulong)nameBlock * 0x10000);
                                mbr.BaseStream.Seek((long)nameOffset, SeekOrigin.Begin);

                                using (MemoryStream mms = new MemoryStream())
                                {
                                    byte[] bytes = null;

                                    while (true)
                                    {
                                        bytes = mbr.ReadBytes(1);
                                        if (bytes[0] == 0) break;
                                        mms.Write(bytes, 0, bytes.Length);
                                    }

                                    bytes = mms.ToArray();
                                    ttarch2.files[i].fileName = Encoding.ASCII.GetString(bytes);

                                    string ext = Methods.GetExtension(ttarch2.files[i].fileName);

                                    if (((ext == ".lenc") || (ext == ".lua")) && !ttarch2.isEncryptedLua)
                                    {
                                        if (ext == ".lenc")
                                        {
                                            ttarch2.isEncryptedLua = true;
                                        }
                                        else
                                        {
                                            byte[] lua = getTtarch2File(ttarch2, ttarch2.files[i], key, br);

                                            ttarch2.isEncryptedLua = Methods.isLuaEncrypted(lua);
                                        }
                                    }

                                    if ((ext != "") && !ttarch2.fileFormats.Contains(ext))
                                    {
                                        ttarch2.fileFormats.Add(ext);
                                    }
                                }

                                mbr.BaseStream.Seek(pos, SeekOrigin.Begin);
                            }
                        }
                    }
                }
                else
                {
                    ulong archSize = br.ReadUInt64();
                    foffset += 8;
                    byte[] subHeader = br.ReadBytes(4);
                    foffset += 4;
                    if (Encoding.ASCII.GetString(subHeader) == "3ATT")
                    {
                        int two = br.ReadInt32();
                        foffset += 4;
                    }

                    ttarch2.version = Encoding.ASCII.GetString(subHeader) == "3ATT" ? 1 : 2;

                    uint nameSize = br.ReadUInt32();
                    foffset += 4;
                    uint filesCount = br.ReadUInt32();
                    foffset += 4;
                    ttarch2.files = new ClassesStructs.Ttarch2Class.Ttarch2files[filesCount];
                    ttarch2.filesOffset = foffset + (28 * (ulong)filesCount) + (ulong)nameSize;

                    for (int i = 0; i < filesCount; i++)
                    {
                        ttarch2.files[i].fileNameCRC64 = br.ReadUInt64();
                        ttarch2.files[i].fileOffset = br.ReadUInt64();
                        ttarch2.files[i].fileSize = br.ReadInt32();
                        int unknown = br.ReadInt32();
                        ushort nameBlock = br.ReadUInt16();
                        ushort nameOff = br.ReadUInt16();
                        long pos = br.BaseStream.Position;
                        ulong nameOffset = foffset + (28 * (ulong)filesCount) + (ulong)nameOff + ((ulong)nameBlock * 0x10000);
                        br.BaseStream.Seek((long)nameOffset, SeekOrigin.Begin);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] bytes = null;

                            while (true)
                            {
                                bytes = br.ReadBytes(1);
                                if (bytes[0] == 0) break;
                                ms.Write(bytes, 0, bytes.Length);
                            }

                            bytes = ms.ToArray();
                            ttarch2.files[i].fileName = Encoding.ASCII.GetString(bytes);

                            string ext = Methods.GetExtension(ttarch2.files[i].fileName);

                            if(((ext == ".lenc") || (ext == ".lua")) && !ttarch2.isEncryptedLua)
                            {
                                if (ext == ".lenc")
                                {
                                    ttarch2.isEncryptedLua = true;
                                }
                                else
                                {
                                    byte[] tmp = getTtarch2File(ttarch2, ttarch2.files[i], key, br);

                                    if (tmp != null)
                                    {
                                        ttarch2.isEncryptedLua = Methods.isLuaEncrypted(tmp);
                                    }
                                }
                            }

                            if ((ext != "") && !ttarch2.fileFormats.Contains(ext))
                            {
                                ttarch2.fileFormats.Add(ext);
                            }
                        }

                        br.BaseStream.Seek(pos, SeekOrigin.Begin);
                    }
                }

                br.Close();
                fs.Close();
            }
            catch
            {
                MessageBox.Show("Unknown error. Please try another archive or change encryption key.", "Something goes wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ttarch2 = null;
            }
        }

        private void UnpackTtarch2(string folderPath, string format, int[] indexes = null)
        {
            var files = getFilteredTtarch2Files();
            List<string> failedFiles = new List<string>();

            int count = indexes != null ? indexes.Length : files.Length;
            
            SetMinimum(0);
            SetMaximum(count);

            FileStream fs = new FileStream(ttarch2.fileName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            for (int i = 0; i < count; i++)
            {
                int ind = indexes != null ? indexes[i] : i;

                byte[] file = getTtarch2File(ttarch2, files[ind], key, br);

                string fileName = files[ind].fileName;

                if (file == null)
                {
                    failedFiles.Add(fileName);
                    Progress(i + 1);
                    continue;
                }

                if (((fileName.Substring(fileName.Length - 5, 5).ToLower() == ".lenc") || (fileName.Substring(fileName.Length - 4, 4).ToLower() == ".lua")) && decrypt)
                {
                    fileName = fileName.Substring(fileName.Length - 5, 5).ToLower() == ".lenc" ? fileName.Remove(fileName.Length - 4, 4) + "lua" : fileName.Remove(fileName.Length - 3, 3) + "lua";
                    file = Methods.decryptLua(file, key, 7);
                }

                File.WriteAllBytes(folderPath + Path.DirectorySeparatorChar + fileName, file);

                Progress(i + 1);
            }

            br.Close();
            fs.Close();

            if (failedFiles.Count > 0)
            {
                MessageBox.Show("Não foi possível extrair os seguintes arquivos:\n\n" + string.Join("\n", failedFiles), "Arquivos não extraídos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void getArchiveInfo()
        {
            string compressedStr = "Compressed: ";
            string encryptedStr = "Encrypted: ";
            string xmodeStr = "Has X mode: ";
            string chunkSzStr = "Chunk size: ";
            string encLua = "Lua scripts encrypted: ";
            string arcVersion = "Version: ";

            if (ttarch != null)
            {
                compressedStr += ttarch.isCompressed ? "Yes" : "No";
                if (ttarch.isCompressed)
                {
                    compressedStr += " (";
                    compressedStr += ttarch.compressAlgorithm == 0 ? "zlib)" : "deflate)";
                }
                
                encryptedStr += ttarch.isEncrypted ? "Yes" : "No";
                xmodeStr += ttarch.isXmode ? "Yes" : "No";
                chunkSzStr += Convert.ToString(ttarch.chunkSize) + "KB";

                encLua += ttarch.isEncryptedLua ? "Yes" : "No";
                arcVersion += Convert.ToString(ttarch.version);
            }
            else if (ttarch2 != null)
            {
                compressedStr += ttarch2.isCompressed ? "Yes" : "No";
                if (ttarch2.isCompressed)
                {
                    compressedStr += " (";
                    compressedStr += ttarch2.compressAlgorithm == 1 ? "deflate)" : "oodle LZ)";
                }

                encryptedStr += ttarch2.isEncrypted ? "Yes" : "No";
                xmodeStr = "Has X mode: No";
                chunkSzStr += Convert.ToString(ttarch2.chunkSize / 1024) + "KB";
                encLua += ttarch2.isEncryptedLua ? "Yes" : "No";
                arcVersion += Convert.ToString(ttarch2.version);
            }

            compressionLabel.Text = compressedStr;
            encryptionLabel.Text = encryptedStr;
            xmodeLabel.Text = xmodeStr;
            chunkSizeLabel.Text = chunkSzStr;
            encrLuaLabel.Text = encLua;
            versionLabel.Text = arcVersion;
        }

        private void showNoResultsMessage()
        {
            filesDataGridView.RowCount = 1;
            filesDataGridView[0, 0].Value = "-";
            filesDataGridView[1, 0].Value = "No results found.";
            filesDataGridView[2, 0].Value = "-";
            filesDataGridView[3, 0].Value = "-";

            filesDataGridView.Columns[0].Width = 60;
            filesDataGridView.Columns[1].Width = 520;
            filesDataGridView.Columns[2].Width = 150;
            filesDataGridView.Columns[3].Width = 150;
            filesDataGridView.ClearSelection();
        }

        private void loadTtarchData(string format)
        {
            filesDataGridView.ColumnCount = 4;

            var files = format == "All files" ? ttarch.files : ttarch.files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower()).ToArray();

            filesDataGridView.Columns[0].HeaderText = "No.";
            filesDataGridView.Columns[1].HeaderText = "File name";
            filesDataGridView.Columns[2].HeaderText = "File offset";
            filesDataGridView.Columns[3].HeaderText = "File size";

            filesDataGridView.RowCount = Math.Max(1, files.Length);

            if (files.Length == 0)
            {
                showNoResultsMessage();
                return;
            }

            int maxnameLen = 0;
            int maxOffLen = 0;
            int maxSizeLen = 0;
            int maxNoLen = 0;

            for (int i = 0; i < files.Length; i++)
            {
                filesDataGridView[0, i].Value = Convert.ToString(i + 1);
                filesDataGridView[1, i].Value = files[i].fileName;
                filesDataGridView[2, i].Value = Convert.ToString(files[i].fileOffset);
                filesDataGridView[3, i].Value = Convert.ToString(files[i].fileSize);

                maxNoLen = Convert.ToString(i + 1).Length > maxNoLen ? Convert.ToString(i + 1).Length : maxNoLen;
                maxnameLen = files[i].fileName.Length > maxnameLen ? files[i].fileName.Length : maxnameLen;
                maxSizeLen = Convert.ToString(files[i].fileSize).Length > maxSizeLen ? Convert.ToString(files[i].fileSize).Length : maxSizeLen;
                maxOffLen = Convert.ToString(files[i].fileOffset).Length > maxOffLen ? Convert.ToString(files[i].fileOffset).Length : maxOffLen;
            }

            filesDataGridView.Columns[0].Width = maxNoLen * 10;
            filesDataGridView.Columns[1].Width = maxnameLen * 8;
            filesDataGridView.Columns[2].Width = maxOffLen * 10;
            filesDataGridView.Columns[3].Width = maxSizeLen * 10;

            filesDataGridView.ClearSelection();
        }

        private void loadTtarchData(ClassesStructs.TtarchClass.ttarchFiles[] files)
        {
            filesDataGridView.ColumnCount = 4;

            filesDataGridView.Columns[0].HeaderText = "No.";
            filesDataGridView.Columns[1].HeaderText = "File name";
            filesDataGridView.Columns[2].HeaderText = "File offset";
            filesDataGridView.Columns[3].HeaderText = "File size";

            filesDataGridView.RowCount = Math.Max(1, files.Length);

            if (files.Length == 0)
            {
                showNoResultsMessage();
                return;
            }

            int maxnameLen = 0;
            int maxOffLen = 0;
            int maxSizeLen = 0;
            int maxNoLen = 0;

            for (int i = 0; i < files.Length; i++)
            {
                filesDataGridView[0, i].Value = Convert.ToString(i + 1);
                filesDataGridView[1, i].Value = files[i].fileName;
                filesDataGridView[2, i].Value = Convert.ToString(files[i].fileOffset);
                filesDataGridView[3, i].Value = Convert.ToString(files[i].fileSize);

                maxNoLen = Convert.ToString(i + 1).Length > maxNoLen ? Convert.ToString(i + 1).Length : maxNoLen;
                maxnameLen = files[i].fileName.Length > maxnameLen ? files[i].fileName.Length : maxnameLen;
                maxSizeLen = Convert.ToString(files[i].fileSize).Length > maxSizeLen ? Convert.ToString(files[i].fileSize).Length : maxSizeLen;
                maxOffLen = Convert.ToString(files[i].fileOffset).Length > maxOffLen ? Convert.ToString(files[i].fileOffset).Length : maxOffLen;
            }

            filesDataGridView.Columns[0].Width = maxNoLen * 10;
            filesDataGridView.Columns[1].Width = maxnameLen * 8;
            filesDataGridView.Columns[2].Width = maxOffLen * 10;
            filesDataGridView.Columns[3].Width = maxSizeLen * 10;

            filesDataGridView.ClearSelection();
        }

        private static byte[] getTtarchFile(ClassesStructs.TtarchClass ttarch, ClassesStructs.TtarchClass.ttarchFiles file, byte[] key, int chunkSz, BinaryReader br)
        {
            byte[] result = null;

            if (!ttarch.isCompressed)
            {
                br.BaseStream.Seek(file.fileOffset + ttarch.filesOffset, SeekOrigin.Begin);
                result = br.ReadBytes(file.fileSize);
                Methods.meta_crypt(result, key, ttarch.version, true);
            }
            else
            {
                int index = (int)file.fileOffset / chunkSz;
                int index2 = (int)(file.fileOffset + file.fileSize) / chunkSz;
                uint off = 0;

                if (index > ttarch.compressedBlocks.Length)
                {
                    MessageBox.Show("Something wrong with offset in compressed archive", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return null;
                }

                if (index2 > ttarch.compressedBlocks.Length)
                {
                    MessageBox.Show("Something wrong with offset in compressed archive", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return null;
                }

                for (int c = 0; c < index; c++)
                {
                    off += (uint)ttarch.compressedBlocks[c];
                }
                br.BaseStream.Seek(ttarch.filesOffset + off, SeekOrigin.Begin);

                uint c_off = (uint)(file.fileOffset - (chunkSz * index));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter mbw = new BinaryWriter(ms))
                    {
                        for (int c = index; c <= index2; c++)
                        {
                            byte[] tmp = br.ReadBytes(ttarch.compressedBlocks[c]);

                            if (ttarch.isEncrypted)
                            {
                                BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, ttarch.version);
                                tmp = dec.Crypt_ECB(tmp, ttarch.version, true);
                            }

                            if (tmp.Length != chunkSz)
                            {
                                tmp = decompressBlock(tmp, ttarch.compressAlgorithm);
                            }

                            mbw.Write(tmp);
                        }

                        byte[] block = ms.ToArray();

                        result = new byte[file.fileSize];

                        Array.Copy(block, c_off, result, 0, result.Length);
                    }
                }
            }

            return result;
        }

        private static byte[] getTtarch2File(Ttarch2Class ttarch2, Ttarch2Class.Ttarch2files file, byte[] key, BinaryReader br)
        {
            byte[] result = null;

            if (ttarch2.isCompressed)
            {
                int index = (int)((ttarch2.filesOffset + file.fileOffset) / ttarch2.chunkSize);
                int index2 = (int)((ttarch2.filesOffset + file.fileOffset + (ulong)file.fileSize) / (ulong)(ttarch2.chunkSize));

                if (index > ttarch2.compressedBlocks.Length)
                {
                    MessageBox.Show("Something wrong with offset in compressed archive", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return null;
                }

                if (index2 > ttarch2.compressedBlocks.Length)
                {
                    MessageBox.Show("Something wrong with offset in compressed archive", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return null;
                }

                ulong cOff = 0;

                for (int c = 0; c < index; c++)
                {
                    cOff += ttarch2.compressedBlocks[c];
                }

                br.BaseStream.Seek((long)(cOff + ttarch2.cFilesOffset), SeekOrigin.Begin);

                using (MemoryStream ms = new MemoryStream())
                {
                    for (int c = index; c <= index2; c++)
                    {
                        var posi = br.BaseStream.Position;
                        byte[] tmp = br.ReadBytes((int)ttarch2.compressedBlocks[c]);

                        if (ttarch2.isEncrypted)
                        {
                            BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, 7);
                            tmp = dec.Crypt_ECB(tmp, 7, true);
                        }

                        if (tmp.Length == ttarch2.chunkSize)
                        {
                            ms.Write(tmp, 0, tmp.Length);
                        }
                        else
                        {
                            tmp = decompressBlock(tmp, ttarch2.compressAlgorithm);

                            if (tmp == null || tmp.Length == 0)
                            {
                                br.Close();
                                return null;
                            }

                            ms.Write(tmp, 0, tmp.Length);
                        }
                    }

                    byte[] block = ms.ToArray();
                    result = new byte[file.fileSize];
                    ulong dOff = (ttarch2.filesOffset + file.fileOffset) - (ulong)(ttarch2.chunkSize * index);
                    Array.Copy(block, (long)dOff, result, 0, result.Length);
                }
            }
            else
            {
                br.BaseStream.Seek((long)ttarch2.filesOffset + (long)file.fileOffset, SeekOrigin.Begin);
                result = br.ReadBytes(file.fileSize);
            }

            return result;
        }

        private void loadTtarch2Data(string format)
        {
            filesDataGridView.ColumnCount = 4;
            var files = format == "All files" ? ttarch2.files : ttarch2.files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower()).ToArray();

            filesDataGridView.Columns[0].HeaderText = "No.";
            filesDataGridView.Columns[1].HeaderText = "File name";
            filesDataGridView.Columns[2].HeaderText = "File offset";
            filesDataGridView.Columns[3].HeaderText = "File size";

            filesDataGridView.RowCount = Math.Max(1, files.Length);

            if (files.Length == 0)
            {
                showNoResultsMessage();
                return;
            }

            int maxnameLen = 0;
            int maxOffLen = 0;
            int maxSizeLen = 0;
            int maxNoLen = 0;

            for (int i = 0; i < files.Length; i++)
            {
                filesDataGridView[0, i].Value = Convert.ToString(i + 1);
                filesDataGridView[1, i].Value = files[i].fileName;
                filesDataGridView[2, i].Value = Convert.ToString(files[i].fileOffset);
                filesDataGridView[3, i].Value = Convert.ToString(files[i].fileSize);

                maxNoLen = Convert.ToString(i + 1).Length > maxNoLen ? Convert.ToString(i + 1).Length : maxNoLen;
                maxnameLen = files[i].fileName.Length > maxnameLen ? files[i].fileName.Length : maxnameLen;
                maxSizeLen = Convert.ToString(files[i].fileSize).Length > maxSizeLen ? Convert.ToString(files[i].fileSize).Length : maxSizeLen;
                maxOffLen = Convert.ToString(files[i].fileOffset).Length > maxOffLen ? Convert.ToString(files[i].fileOffset).Length : maxOffLen;
            }

            filesDataGridView.Columns[0].Width = maxNoLen * 10;
            filesDataGridView.Columns[1].Width = maxnameLen * 8;
            filesDataGridView.Columns[2].Width = maxOffLen * 10;
            filesDataGridView.Columns[3].Width = maxSizeLen * 10;

            filesDataGridView.ClearSelection();
        }

        private void loadTtarch2Data(ClassesStructs.Ttarch2Class.Ttarch2files[] files)
        {
            filesDataGridView.ColumnCount = 4;

            filesDataGridView.Columns[0].HeaderText = "No.";
            filesDataGridView.Columns[1].HeaderText = "File name";
            filesDataGridView.Columns[2].HeaderText = "File offset";
            filesDataGridView.Columns[3].HeaderText = "File size";

            filesDataGridView.RowCount = Math.Max(1, files.Length);

            if (files.Length == 0)
            {
                showNoResultsMessage();
                return;
            }

            int maxnameLen = 0;
            int maxOffLen = 0;
            int maxSizeLen = 0;
            int maxNoLen = 0;

            for (int i = 0; i < files.Length; i++)
            {
                filesDataGridView[0, i].Value = Convert.ToString(i + 1);
                filesDataGridView[1, i].Value = files[i].fileName;
                filesDataGridView[2, i].Value = Convert.ToString(files[i].fileOffset);
                filesDataGridView[3, i].Value = Convert.ToString(files[i].fileSize);

                maxNoLen = Convert.ToString(i + 1).Length > maxNoLen ? Convert.ToString(i + 1).Length : maxNoLen;
                maxnameLen = files[i].fileName.Length > maxnameLen ? files[i].fileName.Length : maxnameLen;
                maxSizeLen = Convert.ToString(files[i].fileSize).Length > maxSizeLen ? Convert.ToString(files[i].fileSize).Length : maxSizeLen;
                maxOffLen = Convert.ToString(files[i].fileOffset).Length > maxOffLen ? Convert.ToString(files[i].fileOffset).Length : maxOffLen;
            }

            filesDataGridView.Columns[0].Width = maxNoLen * 10;
            filesDataGridView.Columns[1].Width = maxnameLen * 8;
            filesDataGridView.Columns[2].Width = maxOffLen * 10;
            filesDataGridView.Columns[3].Width = maxSizeLen * 10;

            filesDataGridView.ClearSelection();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All supported files (*.ttarch, *.ttarch2, *.obb) | *.ttarch;*.ttarch2;*.obb| TTARCH archives (*.ttarch) | *.ttarch| TTARCH2/OBB archives (*.ttarch2;*.obb) | *.ttarch2;*.obb";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                await OpenArchiveFile(ofd.FileName);
            }
        }

        private void ArchiveUnpacker_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < MainMenu.gamelist.Count; i++)
            {
                gameListCB.Items.Add(i + ". " + MainMenu.gamelist[i].gamename);
            }

            gameListCB.SelectedIndex = MainMenu.settings.encKeyIndex;
            customKeyTB.Text = MainMenu.settings.encCustomKey;
            useCustomKeyCB.Checked = MainMenu.settings.customKey;

            searchTB.Enabled = searchFilesByNameCB.Checked;
            searchBtn.Enabled = searchFilesByNameCB.Checked;

            updateExtractionActions(false);
        }

        private async void unpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            decrypt = decryptLuaCB.Checked;
            key = MainMenu.gamelist[gameListCB.SelectedIndex].key;
            string format = fileFormatsCB.Text;

            if (!hasFilteredResults())
            {
                MessageBox.Show("No files found to extract.", "No results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (ttarch != null)
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    await Task.Run(() => UnpackTtarch(selectedFolder, format));
                }
            }
            else if(ttarch2 != null)
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    await Task.Run(() => UnpackTtarch2(selectedFolder, format));
                }
            }
            else
            {
                MessageBox.Show("Nothing to extract. Please open ttarch/ttarch2/obb file and then extract.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileFormatsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            applyFilters();
        }

        private void gameListCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encKeyIndex = gameListCB.SelectedIndex;

            Settings.SaveConfig(MainMenu.settings);
        }

        private void useCustomKeyCB_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.customKey = useCustomKeyCB.Checked;

            MainMenu.settings.encCustomKey = customKeyTB.Text != "" ? customKeyTB.Text : MainMenu.settings.encCustomKey;

            Settings.SaveConfig(MainMenu.settings);
        }

        private async void unpackSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasFilteredResults())
            {
                MessageBox.Show("No files found to extract.", "No results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (filesDataGridView.SelectedRows.Count > 0)
            {
                key = MainMenu.gamelist[gameListCB.SelectedIndex].key;
                decrypt = decryptLuaCB.Checked;

                string format = fileFormatsCB.Text;
                var indexesList = new List<int>();

                for (int i = 0; i < filesDataGridView.SelectedRows.Count; i++)
                {
                    object cellValue = filesDataGridView.SelectedRows[i].Cells[0].Value;
                    int rowIndex;

                    if (cellValue != null && int.TryParse(cellValue.ToString(), out rowIndex) && rowIndex > 0)
                    {
                        indexesList.Add(rowIndex - 1);
                    }
                }

                if (indexesList.Count == 0)
                {
                    MessageBox.Show("Please select valid files from list first.", "No selected files from list", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int[] indexes = indexesList.ToArray();

                if(ttarch != null)
                {
                    string selectedFolder = SelectFolder();

                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        await Task.Run(() => UnpackTtarch(selectedFolder, format, indexes));
                    }
                }
                else if(ttarch2 != null)
                {
                    string selectedFolder = SelectFolder();

                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        await Task.Run(() => UnpackTtarch2(selectedFolder, format, indexes));
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select files from list first.", "No selected files from list", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void searchFilesByNameCB_CheckedChanged(object sender, EventArgs e)
        {
            searchBtn.Enabled = searchFilesByNameCB.Checked;
            searchTB.Enabled = searchFilesByNameCB.Checked;
            applyFilters();
        }

        private void searchBtn_Click(object sender, EventArgs e)
        {
            if((ttarch == null) && (ttarch2 == null))
            {
                MessageBox.Show("Nothing to search.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                applyFilters();
            }
        }

        private void searchTB_TextChanged(object sender, EventArgs e)
        {
            applyFilters();
        }

        private void ArchiveUnpacker_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".ttarch" || ext == ".ttarch2" || ext == ".obb")
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }

            e.Effect = DragDropEffects.None;
        }

        private async void ArchiveUnpacker_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            await OpenArchiveFile(files[0]);
        }
    }
}
