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
            AppIcon.Apply(this);
            Localizer.Localize(this);
            ArrangeTopPanel();
            topPanel.Resize += (s, e) => ArrangeTopPanel();
            filesDataGridView.AllowUserToAddRows = false;
            filesDataGridView.AllowUserToDeleteRows = false;
            filesDataGridView.ReadOnly = true;
            filesDataGridView.EditMode = DataGridViewEditMode.EditProgrammatically;
            filesDataGridView.ColumnHeaderMouseClick += filesDataGridView_ColumnHeaderMouseClick;
            previewTextBox.MaxLength = 1000000;

            previewPictureBox.Resize += (s, e) => AdjustPreviewSizeMode();
            ShowPreviewMessage("");
        }

        private static ClassesStructs.TtarchClass ttarch;
        private static ClassesStructs.Ttarch2Class ttarch2;
        private bool decrypt = false;
        private byte[] key = null;

        //Files currently shown in the grid (after format filter/search), in row order. Row N of the
        //grid is always element N here, which is what the preview panel uses to find the file.
        private ClassesStructs.TtarchClass.ttarchFiles[] shownTtarchFiles;
        private ClassesStructs.Ttarch2Class.Ttarch2files[] shownTtarch2Files;
        private int fileGridSortColumn = -1;
        private bool fileGridSortAscending = true;
        private bool arrangingTopPanel;
        private bool suppressFormatFilterEvent;
        private bool fillingFileGrid;
        private bool suppressCustomKeyEvent;
        private bool loadingSettings;
        private int archiveInfoBaseWidth;

        private void ArrangeTopPanel()
        {
            if (arrangingTopPanel) return;
            arrangingTopPanel = true;

            if (archiveInfoBaseWidth == 0)
            {
                archiveInfoBaseWidth = groupBox1.Width;
                groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                label2.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                gameListCB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                label1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                fileFormatsCB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                useCustomKeyCB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                customKeyTB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                useCustomKeyCB.Visible = false;
                customKeyTB.Visible = false;
                searchFilesByNameCB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                searchTB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                searchBtn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                decryptLuaCB.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            int rightMargin = 6;
            int archiveInfoWidth = Math.Max(archiveInfoBaseWidth, Math.Max(xmodeLabel.Right, encrLuaLabel.Right) + 24);
            groupBox1.Width = archiveInfoWidth;
            groupBox1.Left = Math.Max(590, topPanel.ClientSize.Width - groupBox1.Width - rightMargin);
            groupBox1.Top = 3;

            int left = 10;
            int labelGap = 8;
            int controlGap = 12;
            int maxRight = Math.Max(520, groupBox1.Left - 14);
            int labelWidth = Math.Max(Math.Max(label2.PreferredWidth, label1.PreferredWidth), searchFilesByNameCB.PreferredSize.Width);
            int controlLeft = left + labelWidth + labelGap;

            label2.SetBounds(left, 12, label2.PreferredWidth, label2.Height);
            gameListCB.SetBounds(controlLeft, 9, Math.Max(220, Math.Min(300, maxRight - controlLeft)), gameListCB.Height);

            label1.SetBounds(left, 43, label1.PreferredWidth, label1.Height);
            int formatControlLeft = label1.Right + labelGap;
            fileFormatsCB.SetBounds(formatControlLeft, 40, 160, fileFormatsCB.Height);

            searchFilesByNameCB.SetBounds(left, 74, searchFilesByNameCB.PreferredSize.Width, searchFilesByNameCB.Height);
            searchTB.SetBounds(controlLeft, 72, 300, searchTB.Height);
            int searchButtonWidth = Math.Max(75, searchBtn.PreferredSize.Width + 12);
            searchBtn.SetBounds(searchTB.Right + controlGap, 70, searchButtonWidth, searchBtn.Height);
            decryptLuaCB.SetBounds(searchBtn.Right + controlGap, 74, decryptLuaCB.PreferredSize.Width, decryptLuaCB.Height);

            if (decryptLuaCB.Right > groupBox1.Left - 8)
            {
                topPanel.Height = 132;
                decryptLuaCB.SetBounds(left, 101, decryptLuaCB.PreferredSize.Width, decryptLuaCB.Height);
            }
            else
            {
                topPanel.Height = 108;
            }

            arrangingTopPanel = false;
        }

        // Prefixa com \\?\ para permitir caminhos maiores que MAX_PATH (260) no Win32.
        private static string ToLongPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.StartsWith(@"\\?\")) return path;
            string full = Path.GetFullPath(path);
            if (full.StartsWith(@"\\")) return @"\\?\UNC\" + full.Substring(2);
            return @"\\?\" + full;
        }

        private string SelectFolder()
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            folderDialog.EnsurePathExists = true;
            return folderDialog.ShowDialog() == CommonFileDialogResult.Ok ? folderDialog.FileName : null;
        }

        #region Automatic game detection

        //Tries to figure out which game an archive belongs to by locating the key from MainMenu.gamelist
        //that correctly decrypts it. Returns the game index, or -1 when the game can't be told apart
        //(archive is not encrypted, or no key in the list matched). Never throws and never touches the
        //shared ttarch/ttarch2 state, so it's safe to run on a background thread before the real read.
        private static int TryDetectGameIndex(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLower();

                if (ext == ".ttarch") return DetectTtarchKey(path);
                if (ext == ".ttarch2" || ext == ".obb") return DetectTtarch2Key(path);
            }
            catch { }

            return -1;
        }

        private static int DetectTtarchKey(string path)
        {
            byte[] header;
            int version;

            //Read the raw (still encrypted) directory header. Returns false when the archive isn't
            //Blowfish-encrypted, in which case the game can't be identified from the header.
            if (!TryReadTtarchRawHeader(path, out header, out version) || header == null) return -1;

            for (int g = 0; g < MainMenu.gamelist.Count; g++)
            {
                try
                {
                    byte[] copy = (byte[])header.Clone();
                    BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(MainMenu.gamelist[g].key, version);
                    byte[] decrypted = dec.Crypt_ECB(copy, version, true);

                    if (IsSaneTtarchHeader(decrypted)) return g;
                }
                catch { }
            }

            return -1;
        }

        private static bool TryReadTtarchRawHeader(string path, out byte[] header, out int version)
        {
            header = null;
            version = 0;

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                version = br.ReadInt32();
                int encryption = br.ReadInt32();
                int two = br.ReadInt32();

                int val = 0;

                if (version > 2)
                {
                    val = br.ReadInt32();
                    int countCompressedBlocks = br.ReadInt32();

                    if (val == 2)
                    {
                        for (int k = 0; k < countCompressedBlocks; k++) br.ReadInt32();
                    }

                    br.ReadUInt32(); //Size of block with files

                    if (version >= 4)
                    {
                        br.ReadInt32(); //priority
                        br.ReadInt32(); //priority2

                        if (version >= 7)
                        {
                            br.ReadInt32();
                            br.ReadInt32();
                            br.ReadInt32(); //chunkSize

                            if (version > 7)
                            {
                                br.ReadByte();
                                if (version == 9) br.ReadUInt32(); //crc32
                            }
                        }
                    }
                }

                int headerSize = br.ReadInt32();
                int cHeaderSize = -1;

                if (version >= 7 && val == 2) cHeaderSize = br.ReadInt32();

                byte[] raw = (version >= 7 && val == 2) ? br.ReadBytes(cHeaderSize) : br.ReadBytes(headerSize);

                if (version >= 7 && val == 2) raw = DecompressForDetect(raw, -1, 0);

                //Not Blowfish-encrypted: the header parses regardless of the key, so the game
                //can't be identified. Leave the current selection untouched.
                if (encryption != 1) return false;

                header = raw;
                return header != null;
            }
        }

        private static bool IsSaneTtarchHeader(byte[] header)
        {
            if (header == null || header.Length < 8) return false;

            using (MemoryStream ms = new MemoryStream(header))
            using (BinaryReader br = new BinaryReader(ms))
            {
                int dirsCount = br.ReadInt32();
                if (dirsCount < 0 || dirsCount > 100000) return false;

                for (int d = 0; d < dirsCount; d++)
                {
                    if (ms.Position + 4 > ms.Length) return false;
                    int nameLen = br.ReadInt32();
                    if (nameLen < 0 || nameLen > 4096 || ms.Position + nameLen > ms.Length) return false;
                    if (!IsPrintableAscii(br.ReadBytes(nameLen))) return false;
                }

                if (ms.Position + 4 > ms.Length) return false;
                int filesCount = br.ReadInt32();
                if (filesCount <= 0 || filesCount > 5000000) return false;

                if (ms.Position + 4 > ms.Length) return false;
                int fnameLen = br.ReadInt32();
                if (fnameLen <= 0 || fnameLen > 4096 || ms.Position + fnameLen > ms.Length) return false;

                return IsPrintableAscii(br.ReadBytes(fnameLen));
            }
        }

        private static int DetectTtarch2Key(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));

                //Only encrypted archives (ECTT/eCTT) can be told apart by their key.
                bool isEncrypted = magic == "ECTT" || magic == "eCTT";
                if (!isEncrypted) return -1;

                int compressAlgorithm = (magic == "eCTT" || magic == "zCTT") ? 2 : 1;
                if (magic == "eCTT" || magic == "zCTT") br.ReadInt32();

                uint chunkSize = br.ReadUInt32();
                int blocksCount = br.ReadInt32();
                if (blocksCount <= 0) return -1;

                ulong val1 = br.ReadUInt64();
                ulong firstBlock = 0;

                for (int i = 0; i < blocksCount; i++)
                {
                    ulong val2 = br.ReadUInt64();
                    if (i == 0) firstBlock = val2 - val1;
                    val1 = val2;
                }

                long cFilesOffset = br.BaseStream.Position;
                if (firstBlock == 0 || firstBlock > (ulong)(fs.Length - cFilesOffset)) return -1;

                byte[] block0 = br.ReadBytes((int)firstBlock);

                for (int g = 0; g < MainMenu.gamelist.Count; g++)
                {
                    try
                    {
                        byte[] tmp = (byte[])block0.Clone();
                        BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(MainMenu.gamelist[g].key, 7);
                        tmp = dec.Crypt_ECB(tmp, 7, true);

                        byte[] decompressed = DecompressForDetect(tmp, compressAlgorithm, chunkSize);

                        if (IsSaneTtarch2Sub(decompressed)) return g;
                    }
                    catch { }
                }
            }

            return -1;
        }

        private static bool IsSaneTtarch2Sub(byte[] data)
        {
            if (data == null || data.Length < 16) return false;

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                string sub = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (sub == "3ATT") br.ReadInt32();

                if (ms.Position + 8 > ms.Length) return false;
                uint nameSize = br.ReadUInt32();
                uint filesCount = br.ReadUInt32();

                if (filesCount == 0 || filesCount > 10000000) return false;
                if (nameSize == 0 || nameSize > 200000000) return false;

                return true;
            }
        }

        //Self-contained decompression for detection so the shared ttarch/ttarch2 state is never touched.
        //algorithm: -1 = try zlib/deflate/oodle in turn, 1 = deflate, 2 = oodle, anything else = zlib.
        private static byte[] DecompressForDetect(byte[] bytes, int algorithm, uint chunkSize)
        {
            try
            {
                switch (algorithm)
                {
                    case 1: return DeflateDecompressor(bytes);
                    case 2: return OodleDecompressForDetect(bytes, chunkSize);
                    case -1:
                        try { return ZLibDecompressor(bytes); }
                        catch
                        {
                            try { return DeflateDecompressor(bytes); }
                            catch { return OodleDecompressForDetect(bytes, chunkSize); }
                        }
                    default: return ZLibDecompressor(bytes);
                }
            }
            catch { return null; }
        }

        private static byte[] OodleDecompressForDetect(byte[] bytes, uint chunkSize)
        {
            long decBufSize = chunkSize > 0 ? (long)chunkSize : bytes.Length;
            byte[] outBuf = new byte[decBufSize];

            int size = OodleTools.Imports.OodleLZ_Decompress(bytes, bytes.Length, outBuf, decBufSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            if (size <= 0) return null;

            byte[] tmp = new byte[size];
            Array.Copy(outBuf, 0, tmp, 0, size);
            return tmp;
        }

        private static bool IsPrintableAscii(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                //Allow standard printable ASCII plus a few separators that show up in paths.
                if (b < 0x20 || b > 0x7E) return false;
            }

            return true;
        }

        #endregion

        private bool _isOpening;
        private string _pendingOpenPath;

        //Drag-and-drop (and the Open menu) can fire a new open while the previous archive is still
        //loading on a background thread. Serialize the opens so two of them never race on the shared
        //ttarch/ttarch2 state: while one is loading, a new request just replaces the "pending" path
        //and the running loop picks it up when it finishes, so the last file dropped always wins.
        private async Task OpenArchiveFile(string filePath)
        {
            if (_isOpening)
            {
                _pendingOpenPath = filePath;
                return;
            }

            _isOpening = true;
            try
            {
                while (filePath != null)
                {
                    _pendingOpenPath = null;
                    await OpenArchiveFileCore(filePath);
                    filePath = _pendingOpenPath;
                }
            }
            finally
            {
                _isOpening = false;
            }
        }

        private async Task OpenArchiveFileCore(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                if(fi.Attributes.HasFlag(FileAttributes.ReadOnly) || !fi.Attributes.HasFlag(FileAttributes.Normal)) fi.Attributes = FileAttributes.Normal;

                //Immediate feedback so the title reflects the drop right away, even while it loads.
                Text = Loc.T("ArchiveUnpacker.titleLoading", "Archive Unpacker. Loading:") + " " + fi.Name;

                ttarch = null;
                ttarch2 = null;

                if (progressBar1.Value > 0) progressBar1.Value = 0;

                //Auto-detect which game this archive belongs to by finding the key that decrypts it.
                //Skipped when the user opted for a custom key, so manual choice is always honored.
                if (!useCustomKeyCB.Checked)
                {
                    int detected = await Task.Run(() => TryDetectGameIndex(fi.FullName));

                    if (detected >= 0 && detected < gameListCB.Items.Count && detected != gameListCB.SelectedIndex)
                    {
                        gameListCB.SelectedIndex = detected;
                    }
                }

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
                        MessageBox.Show(Loc.T("ArchiveUnpacker.msgUnsupportedFormat", "Unsupported file format. Please choose a .ttarch, .ttarch2 or .obb file."), Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Text = Loc.T("ArchiveUnpacker.$this", "Archive Unpacker");
                        return;
                }

                Text = Loc.T("ArchiveUnpacker.titleOpened", "Archive Unpacker. Opened file:") + " " + fi.Name;
            }
            catch (Exception ex)
            {
                Text = Loc.T("ArchiveUnpacker.$this", "Archive Unpacker");
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgFailedOpen", "Failed to open archive.") + " " + ex.Message, Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void populateFileFormats(List<string> formats)
        {
            suppressFormatFilterEvent = true;
            try
            {
                fileFormatsCB.Items.Clear();

                if (formats != null && formats.Count > 0)
                {
                    if (formats.Count > 1) fileFormatsCB.Items.Add(Loc.T("ArchiveUnpacker.allFiles", "All files"));

                    formats.Sort();

                    for (int i = 0; i < formats.Count; i++)
                    {
                        fileFormatsCB.Items.Add(formats[i]);
                    }

                    fileFormatsCB.SelectedIndex = 0;
                }
                else
                {
                    fileFormatsCB.Items.Add(Loc.T("ArchiveUnpacker.allFiles", "All files"));
                    fileFormatsCB.SelectedIndex = 0;
                }
            }
            finally
            {
                suppressFormatFilterEvent = false;
            }
        }

        private string getSelectedFormat()
        {
            if (fileFormatsCB.Items.Count == 0) return "All files";
            return string.IsNullOrEmpty(fileFormatsCB.Text) ? "All files" : fileFormatsCB.Text;
        }

        // "All files" is a UI label and is therefore translated. Keep the filtering logic
        // language-neutral: a localized label must never be mistaken for a file extension.
        private static bool isAllFilesFormat(string format)
        {
            return string.IsNullOrEmpty(format)
                || string.Equals(format, "All files", StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    format,
                    Loc.T("ArchiveUnpacker.allFiles", "All files"),
                    StringComparison.CurrentCultureIgnoreCase);
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


        //Filter values (format/searchPattern) may be passed in so that extraction running on a
        //background thread never touches UI controls (searchTB/fileFormatsCB) directly. When called
        //from the UI thread the arguments are left null and the current control values are read.
        private ClassesStructs.TtarchClass.ttarchFiles[] getFilteredTtarchFiles(string format = null, string searchPattern = null)
        {
            var files = ttarch.files.AsEnumerable();
            if (format == null) format = getSelectedFormat();

            if (!isAllFilesFormat(format))
            {
                files = files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower());
            }

            if (searchPattern == null && isSearchEnabled())
                searchPattern = searchTB.Text.ToLower();

            if (!string.IsNullOrEmpty(searchPattern))
            {
                files = files.Where(x => x.fileName.ToLower().Contains(searchPattern));
            }

            return sortTtarchFiles(files).ToArray();
        }

        private ClassesStructs.Ttarch2Class.Ttarch2files[] getFilteredTtarch2Files(string format = null, string searchPattern = null)
        {
            var files = ttarch2.files.AsEnumerable();
            if (format == null) format = getSelectedFormat();

            if (!isAllFilesFormat(format))
            {
                files = files.Where(x => Methods.GetExtension(x.fileName).ToLower() == format.ToLower());
            }

            if (searchPattern == null && isSearchEnabled())
                searchPattern = searchTB.Text.ToLower();

            if (!string.IsNullOrEmpty(searchPattern))
            {
                files = files.Where(x => x.fileName.ToLower().Contains(searchPattern));
            }

            return sortTtarch2Files(files).ToArray();
        }

        private IEnumerable<ClassesStructs.TtarchClass.ttarchFiles> sortTtarchFiles(IEnumerable<ClassesStructs.TtarchClass.ttarchFiles> files)
        {
            switch (fileGridSortColumn)
            {
                case 1:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 2:
                    return fileGridSortAscending ? files.OrderBy(x => Methods.GetExtension(x.fileName).ToLower(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => Methods.GetExtension(x.fileName).ToLower(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 3:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileSize).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileSize).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 4:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileOffset).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileOffset).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                default:
                    return files;
            }
        }

        private IEnumerable<ClassesStructs.Ttarch2Class.Ttarch2files> sortTtarch2Files(IEnumerable<ClassesStructs.Ttarch2Class.Ttarch2files> files)
        {
            switch (fileGridSortColumn)
            {
                case 1:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 2:
                    return fileGridSortAscending ? files.OrderBy(x => Methods.GetExtension(x.fileName).ToLower(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => Methods.GetExtension(x.fileName).ToLower(), StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 3:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileSize).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileSize).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                case 4:
                    return fileGridSortAscending ? files.OrderBy(x => x.fileOffset).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase) : files.OrderByDescending(x => x.fileOffset).ThenBy(x => x.fileName, StringComparer.CurrentCultureIgnoreCase);
                default:
                    return files;
            }
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
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgUnknownErrorEx", "Unknown error. Please try another archive or change encryption key.\r\nGot exception:") + "\r\n" + ex.Message, Loc.T("ArchiveUnpacker.titleSomethingWrong", "Something goes wrong"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                ttarch = null;
            }
        }

        private void UnpackTtarch(string folderPath, string format, string searchPattern = null, int[] indexes = null)
        {
            var files = getFilteredTtarchFiles(format, searchPattern);
            List<string> failedFiles = new List<string>();

            FileStream fs = new FileStream(ttarch.filePath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            int count = indexes != null ? indexes.Length : files.Length;

            SetMinimum(0);
            SetMaximum(count);

            int chunkSz = ttarch.chunkSize * 1024;

            for (int i = 0; i < count; i++)
            {
                int ind = indexes != null ? indexes[i] : i;
                string fileName = files[ind].fileName;

                try
                {
                    byte[] file = getTtarchFile(ttarch, files[ind], key, chunkSz, br);

                    if (file == null)
                    {
                        failedFiles.Add(fileName + " (falha ao ler do archive)");
                        Progress(i + 1);
                        continue;
                    }

                    if ((fileName.Length >= 5) && (fileName.Substring(fileName.Length - 5, 5) == ".lenc") && decrypt)
                    {
                        fileName = fileName.Remove(fileName.Length - 4, 4) + "lua";
                        file = Methods.decryptLua(file, key, ttarch.version);
                    }

                    string outPath = folderPath + Path.DirectorySeparatorChar + fileName;
                    string outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(ToLongPath(outDir)))
                    {
                        Directory.CreateDirectory(ToLongPath(outDir));
                    }

                    File.WriteAllBytes(ToLongPath(outPath), file);
                }
                catch (Exception ex)
                {
                    failedFiles.Add(fileName + " (" + ex.GetType().Name + ": " + ex.Message + ")");
                }

                Progress(i + 1);
            }

            br.Close();
            fs.Close();

            if (failedFiles.Count > 0)
            {
                try
                {
                    string logPath = folderPath + Path.DirectorySeparatorChar + "_unpack_errors.log";
                    File.WriteAllText(ToLongPath(logPath), string.Join(Environment.NewLine, failedFiles));
                }
                catch { }

                MessageBox.Show(Loc.T("ArchiveUnpacker.msgFailedExtractList", "The following files could not be extracted:") + "\n\n" + string.Join("\n", failedFiles), Loc.T("ArchiveUnpacker.titleFilesNotExtracted", "Files not extracted"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgUnknownError", "Unknown error. Please try another archive or change encryption key."), Loc.T("ArchiveUnpacker.titleSomethingWrong", "Something goes wrong"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                ttarch2 = null;
            }
        }

        private void UnpackTtarch2(string folderPath, string format, string searchPattern = null, int[] indexes = null)
        {
            var files = getFilteredTtarch2Files(format, searchPattern);
            List<string> failedFiles = new List<string>();

            int count = indexes != null ? indexes.Length : files.Length;
            
            SetMinimum(0);
            SetMaximum(count);

            FileStream fs = new FileStream(ttarch2.fileName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            for (int i = 0; i < count; i++)
            {
                int ind = indexes != null ? indexes[i] : i;
                string fileName = files[ind].fileName;

                try
                {
                    byte[] file = getTtarch2File(ttarch2, files[ind], key, br);

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

                    string outPath = folderPath + Path.DirectorySeparatorChar + fileName;
                    string outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(ToLongPath(outDir)))
                    {
                        Directory.CreateDirectory(ToLongPath(outDir));
                    }

                    File.WriteAllBytes(ToLongPath(outPath), file);
                }
                catch (Exception ex)
                {
                    failedFiles.Add(fileName + " (" + ex.GetType().Name + ": " + ex.Message + ")");
                }

                Progress(i + 1);
            }

            br.Close();
            fs.Close();

            if (failedFiles.Count > 0)
            {
                try
                {
                    string logPath = folderPath + Path.DirectorySeparatorChar + "_unpack_errors.log";
                    File.WriteAllText(ToLongPath(logPath), string.Join(Environment.NewLine, failedFiles));
                }
                catch { }

                MessageBox.Show(Loc.T("ArchiveUnpacker.msgFailedExtractList", "The following files could not be extracted:") + "\n\n" + string.Join("\n", failedFiles), Loc.T("ArchiveUnpacker.titleFilesNotExtracted", "Files not extracted"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void getArchiveInfo()
        {
            string compressedStr = Loc.T("ArchiveUnpacker.compressionLabel", "Compressed:") + " ";
            string encryptedStr = Loc.T("ArchiveUnpacker.encryptionLabel", "Encrypted:") + " ";
            string xmodeStr = Loc.T("ArchiveUnpacker.xmodeLabel", "Has X mode:") + " ";
            string chunkSzStr = Loc.T("ArchiveUnpacker.chunkSizeLabel", "Chunk size:") + " ";
            string encLua = Loc.T("ArchiveUnpacker.encrLuaLabel", "Lua scripts encrypted:") + " ";
            string arcVersion = Loc.T("ArchiveUnpacker.versionLabel", "Version:") + " ";
            string yes = Loc.T("Common.yes", "Yes");
            string no = Loc.T("Common.no", "No");

            if (ttarch != null)
            {
                compressedStr += ttarch.isCompressed ? yes : no;
                if (ttarch.isCompressed)
                {
                    compressedStr += " (";
                    compressedStr += ttarch.compressAlgorithm == 0 ? "zlib)" : "deflate)";
                }
                
                encryptedStr += ttarch.isEncrypted ? yes : no;
                xmodeStr += ttarch.isXmode ? yes : no;
                chunkSzStr += Convert.ToString(ttarch.chunkSize) + "KB";

                encLua += ttarch.isEncryptedLua ? yes : no;
                arcVersion += Convert.ToString(ttarch.version);
            }
            else if (ttarch2 != null)
            {
                compressedStr += ttarch2.isCompressed ? yes : no;
                if (ttarch2.isCompressed)
                {
                    compressedStr += " (";
                    compressedStr += ttarch2.compressAlgorithm == 1 ? "deflate)" : "oodle LZ)";
                }

                encryptedStr += ttarch2.isEncrypted ? yes : no;
                xmodeStr = Loc.T("ArchiveUnpacker.xmodeLabel", "Has X mode:") + " " + no;
                chunkSzStr += Convert.ToString(ttarch2.chunkSize / 1024) + "KB";
                encLua += ttarch2.isEncryptedLua ? yes : no;
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
            filesDataGridView[1, 0].Value = Loc.T("ArchiveUnpacker.noResultsFound", "No results found.");
            filesDataGridView[2, 0].Value = "-";
            filesDataGridView[3, 0].Value = "-";
            filesDataGridView[4, 0].Value = "-";

            configureFileGridColumns();
            filesDataGridView.ClearSelection();
        }

        private void setupFileGridColumns()
        {
            filesDataGridView.ColumnCount = 5;

            filesDataGridView.Columns[0].HeaderText = Loc.T("ArchiveUnpacker.colNo", "No.");
            filesDataGridView.Columns[1].HeaderText = Loc.T("ArchiveUnpacker.colFileName", "File name");
            filesDataGridView.Columns[2].HeaderText = Loc.T("ArchiveUnpacker.colFileType", "Type");
            filesDataGridView.Columns[3].HeaderText = Loc.T("ArchiveUnpacker.colFileSize", "File size");
            filesDataGridView.Columns[4].HeaderText = Loc.T("ArchiveUnpacker.colFileOffset", "File offset");
        }

        private void fillFileGridRow(int row, string fileName, long fileSize, ulong fileOffset)
        {
            filesDataGridView[0, row].Value = Convert.ToString(row + 1);
            filesDataGridView[1, row].Value = fileName;
            filesDataGridView[2, row].Value = Methods.GetExtension(fileName).ToLower();
            filesDataGridView[3, row].Value = Convert.ToString(fileSize);
            filesDataGridView[4, row].Value = Convert.ToString(fileOffset);
        }

        private void configureFileGridColumns()
        {
            if (filesDataGridView.Columns.Count < 5) return;

            filesDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            filesDataGridView.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            filesDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            filesDataGridView.ColumnHeadersHeight = 24;
            filesDataGridView.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            filesDataGridView.RowHeadersWidth = 40;

            DataGridViewColumn number = filesDataGridView.Columns[0];
            DataGridViewColumn name = filesDataGridView.Columns[1];
            DataGridViewColumn type = filesDataGridView.Columns[2];
            DataGridViewColumn size = filesDataGridView.Columns[3];
            DataGridViewColumn offset = filesDataGridView.Columns[4];

            foreach (DataGridViewColumn col in filesDataGridView.Columns)
            {
                col.ReadOnly = true;
                col.SortMode = col.Index == 0 ? DataGridViewColumnSortMode.NotSortable : DataGridViewColumnSortMode.Programmatic;
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            number.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            number.MinimumWidth = 55;
            number.Width = 55;
            number.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            type.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            type.MinimumWidth = 70;
            type.Width = getTypeColumnWidth();
            type.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            size.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            size.MinimumWidth = 100;
            size.Width = 100;
            size.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            size.Visible = false;

            offset.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            offset.MinimumWidth = 110;
            offset.Width = 110;
            offset.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            offset.Visible = false;

            name.MinimumWidth = 200;
            name.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            name.FillWeight = 100;

            updateFileGridSortGlyph();
        }

        private int getTypeColumnWidth()
        {
            int width = TextRenderer.MeasureText(Loc.T("ArchiveUnpacker.colFileType", "Type"), filesDataGridView.Font).Width + 28;
            HashSet<string> types = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            if (shownTtarchFiles != null)
            {
                for (int i = 0; i < shownTtarchFiles.Length; i++)
                    types.Add(Methods.GetExtension(shownTtarchFiles[i].fileName).ToLower());
            }
            else if (shownTtarch2Files != null)
            {
                for (int i = 0; i < shownTtarch2Files.Length; i++)
                    types.Add(Methods.GetExtension(shownTtarch2Files[i].fileName).ToLower());
            }

            foreach (string type in types)
            {
                width = Math.Max(width, TextRenderer.MeasureText(type, filesDataGridView.Font).Width + 28);
            }

            return Math.Min(220, Math.Max(70, width));
        }

        private void updateFileGridSortGlyph()
        {
            if (filesDataGridView.Columns.Count < 5) return;

            foreach (DataGridViewColumn col in filesDataGridView.Columns)
            {
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (fileGridSortColumn > 0 && fileGridSortColumn < filesDataGridView.Columns.Count)
            {
                filesDataGridView.Columns[fileGridSortColumn].HeaderCell.SortGlyphDirection = fileGridSortAscending ? SortOrder.Ascending : SortOrder.Descending;
            }
        }

        private void filesDataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex <= 0 || e.ColumnIndex > 4) return;

            if (fileGridSortColumn == e.ColumnIndex)
            {
                fileGridSortAscending = !fileGridSortAscending;
            }
            else
            {
                fileGridSortColumn = e.ColumnIndex;
                fileGridSortAscending = true;
            }

            applyFilters();
        }

        private void loadTtarchData(ClassesStructs.TtarchClass.ttarchFiles[] files)
        {
            fillingFileGrid = true;
            previewSeq++;
            filesDataGridView.SuspendLayout();

            try
            {
                shownTtarchFiles = files;
                shownTtarch2Files = null;

                setupFileGridColumns();

                filesDataGridView.RowCount = Math.Max(1, files.Length);

                if (files.Length == 0)
                {
                    showNoResultsMessage();
                    return;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    fillFileGridRow(i, files[i].fileName, files[i].fileSize, files[i].fileOffset);
                }

                configureFileGridColumns();
                filesDataGridView.ClearSelection();
                ShowPreviewMessage(Loc.T("ArchiveUnpacker.previewSelectFile", "Select a file to preview it."));
            }
            finally
            {
                filesDataGridView.ResumeLayout();
                fillingFileGrid = false;
            }
        }

        public static void ExtractTtarchCli(string archivePath, string outputDir, byte[] key)
        {
            ttarch = new ClassesStructs.TtarchClass();
            var inst = (ArchiveUnpacker)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ArchiveUnpacker));
            var read = typeof(ArchiveUnpacker).GetMethod("ReadHeaderTtarch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            read.Invoke(inst, new object[] { archivePath, key });
            if (ttarch == null) throw new Exception("Failed to read header");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
            int chunkSz = ttarch.chunkSize * 1024;
            using (var fs = new FileStream(archivePath, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                for (int i = 0; i < ttarch.files.Length; i++)
                {
                    byte[] data = getTtarchFile(ttarch, ttarch.files[i], key, chunkSz, br);
                    if (data == null) { Console.WriteLine("FAIL: " + ttarch.files[i].fileName); continue; }
                    string outPath = Path.Combine(outputDir, ttarch.files[i].fileName);
                    File.WriteAllBytes(outPath, data);
                    if (i % 1000 == 0) Console.WriteLine("extracted " + i + "/" + ttarch.files.Length);
                }
            }
            Console.WriteLine("Extract done: " + ttarch.files.Length + " files");
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
                    Console.WriteLine("Something wrong with offset in compressed archive");
                    return null;
                }

                if (index2 > ttarch.compressedBlocks.Length)
                {
                    Console.WriteLine("Something wrong with offset in compressed archive");
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
                    MessageBox.Show(Loc.T("ArchiveUnpacker.msgOffsetWrong", "Something wrong with offset in compressed archive"), Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return null;
                }

                if (index2 > ttarch2.compressedBlocks.Length)
                {
                    MessageBox.Show(Loc.T("ArchiveUnpacker.msgOffsetWrong", "Something wrong with offset in compressed archive"), Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);

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

        private void loadTtarch2Data(ClassesStructs.Ttarch2Class.Ttarch2files[] files)
        {
            fillingFileGrid = true;
            previewSeq++;
            filesDataGridView.SuspendLayout();

            try
            {
                shownTtarch2Files = files;
                shownTtarchFiles = null;

                setupFileGridColumns();

                filesDataGridView.RowCount = Math.Max(1, files.Length);

                if (files.Length == 0)
                {
                    showNoResultsMessage();
                    return;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    fillFileGridRow(i, files[i].fileName, files[i].fileSize, files[i].fileOffset);
                }

                configureFileGridColumns();
                filesDataGridView.ClearSelection();
                ShowPreviewMessage(Loc.T("ArchiveUnpacker.previewSelectFile", "Select a file to preview it."));
            }
            finally
            {
                filesDataGridView.ResumeLayout();
                fillingFileGrid = false;
            }
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = Loc.T(
                    "ArchiveUnpacker.openFilter",
                    "All supported files (*.ttarch, *.ttarch2, *.obb) | *.ttarch;*.ttarch2;*.obb| TTARCH archives (*.ttarch) | *.ttarch| TTARCH2/OBB archives (*.ttarch2;*.obb) | *.ttarch2;*.obb");

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                await OpenArchiveFile(ofd.FileName);
            }
        }

        private void ArchiveUnpacker_Load(object sender, EventArgs e)
        {
            loadingSettings = true;
            try
            {
            for (int i = 0; i < MainMenu.gamelist.Count; i++)
            {
                gameListCB.Items.Add(i + ". " + MainMenu.gamelist[i].gamename);
            }

            gameListCB.SelectedIndex = MainMenu.settings.encKeyIndex >= 0 && MainMenu.settings.encKeyIndex < gameListCB.Items.Count ? MainMenu.settings.encKeyIndex : (gameListCB.Items.Count > 0 ? 0 : -1);
            customKeyTB.Text = MainMenu.settings.encCustomKey;
            suppressCustomKeyEvent = true;
            useCustomKeyCB.Checked = false;
            useCustomKeyCB.Visible = false;
            customKeyTB.Visible = false;
            suppressCustomKeyEvent = false;

            searchTB.Enabled = searchFilesByNameCB.Checked;
            searchBtn.Enabled = searchFilesByNameCB.Checked;

            updateExtractionActions(false);
            }
            finally
            {
                loadingSettings = false;
            }
        }

        private async void unpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            decrypt = decryptLuaCB.Checked;
            key = MainMenu.gamelist[gameListCB.SelectedIndex].key;
            //Capture filter values on the UI thread so background extraction never touches UI controls.
            string format = getSelectedFormat();
            string searchPattern = isSearchEnabled() ? searchTB.Text.ToLower() : null;

            if (!hasFilteredResults())
            {
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgNoFilesToExtract", "No files found to extract."), Loc.T("ArchiveUnpacker.titleNoResults", "No results"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (ttarch != null)
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    await Task.Run(() => UnpackTtarch(selectedFolder, format, searchPattern));
                }
            }
            else if(ttarch2 != null)
            {
                string selectedFolder = SelectFolder();

                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    await Task.Run(() => UnpackTtarch2(selectedFolder, format, searchPattern));
                }
            }
            else
            {
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgNothingToExtract", "Nothing to extract. Please open ttarch/ttarch2/obb file and then extract."), Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fileFormatsCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressFormatFilterEvent) return;
            applyFilters();
        }

        private void gameListCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSettings) return;
            MainMenu.settings.encKeyIndex = gameListCB.SelectedIndex;

            Settings.SaveConfig(MainMenu.settings);
        }

        private void useCustomKeyCB_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressCustomKeyEvent) return;
            MainMenu.settings.customKey = useCustomKeyCB.Checked;

            MainMenu.settings.encCustomKey = customKeyTB.Text != "" ? customKeyTB.Text : MainMenu.settings.encCustomKey;

            Settings.SaveConfig(MainMenu.settings);
        }

        private async void unpackSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasFilteredResults())
            {
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgNoFilesToExtract", "No files found to extract."), Loc.T("ArchiveUnpacker.titleNoResults", "No results"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (filesDataGridView.SelectedRows.Count > 0)
            {
                key = MainMenu.gamelist[gameListCB.SelectedIndex].key;
                decrypt = decryptLuaCB.Checked;

                //Capture filter values on the UI thread so background extraction never touches UI controls.
                string format = getSelectedFormat();
                string searchPattern = isSearchEnabled() ? searchTB.Text.ToLower() : null;
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
                    MessageBox.Show(Loc.T("ArchiveUnpacker.msgSelectValidFiles", "Please select valid files from list first."), Loc.T("ArchiveUnpacker.titleNoSelection", "No selected files from list"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int[] indexes = indexesList.ToArray();

                if(ttarch != null)
                {
                    string selectedFolder = SelectFolder();

                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        await Task.Run(() => UnpackTtarch(selectedFolder, format, searchPattern, indexes));
                    }
                }
                else if(ttarch2 != null)
                {
                    string selectedFolder = SelectFolder();

                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        await Task.Run(() => UnpackTtarch2(selectedFolder, format, searchPattern, indexes));
                    }
                }
            }
            else
            {
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgSelectFiles", "Please select files from list first."), Loc.T("ArchiveUnpacker.titleNoSelection", "No selected files from list"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgNothingToSearch", "Nothing to search."), Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        #region Preview panel

        //Bumped on every preview request; a finished background build only gets displayed when its
        //sequence still matches, so rapid selection changes never show a stale preview.
        private int previewSeq;

        private const long MaxPreviewFileSize = 64L * 1024 * 1024;

        private void filesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (fillingFileGrid) return;
            UpdatePreview();
        }

        private async void UpdatePreview()
        {
            int seq = ++previewSeq;

            if ((ttarch == null && ttarch2 == null) || filesDataGridView.SelectedRows.Count == 0)
            {
                ShowPreviewMessage((ttarch != null || ttarch2 != null) ? Loc.T("ArchiveUnpacker.previewSelectFile", "Select a file to preview it.") : "");
                return;
            }

            object cellValue = filesDataGridView.SelectedRows[0].Cells[0].Value;
            int number;

            if (cellValue == null || !int.TryParse(cellValue.ToString(), out number) || number <= 0)
            {
                ShowPreviewMessage(Loc.T("ArchiveUnpacker.previewSelectFile", "Select a file to preview it."));
                return;
            }

            int index = number - 1;

            string fileName;
            long fileSize;
            byte[] archKey = MainMenu.gamelist[gameListCB.SelectedIndex].key;
            int luaVersion;

            //Capture everything the background build needs on the UI thread, so a new "open"
            //replacing the shared state mid-build can't be observed halfway through.
            var arc1 = ttarch;
            var arc2 = ttarch2;

            if (arc1 != null)
            {
                if (shownTtarchFiles == null || index >= shownTtarchFiles.Length) return;
                fileName = shownTtarchFiles[index].fileName;
                fileSize = shownTtarchFiles[index].fileSize;
                luaVersion = arc1.version;
            }
            else
            {
                if (shownTtarch2Files == null || index >= shownTtarch2Files.Length) return;
                fileName = shownTtarch2Files[index].fileName;
                fileSize = shownTtarch2Files[index].fileSize;
                luaVersion = 7;
            }

            if (fileSize > MaxPreviewFileSize)
            {
                ShowPreviewMessage(Loc.T("ArchiveUnpacker.previewTooBig", "File is too large to preview."));
                return;
            }

            ShowPreviewMessage(Loc.T("ArchiveUnpacker.previewLoading", "Loading preview..."));

            var file1 = arc1 != null ? shownTtarchFiles[index] : default(ClassesStructs.TtarchClass.ttarchFiles);
            var file2 = arc2 != null ? shownTtarch2Files[index] : default(ClassesStructs.Ttarch2Class.Ttarch2files);

            PreviewBuilder.PreviewResult result = null;

            try
            {
                result = await Task.Run(() =>
                {
                    byte[] bytes = null;

                    if (arc1 != null)
                    {
                        using (FileStream fs = new FileStream(arc1.filePath, FileMode.Open, FileAccess.Read))
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            bytes = getTtarchFile(arc1, file1, archKey, arc1.chunkSize * 1024, br);
                        }
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(arc2.fileName, FileMode.Open, FileAccess.Read))
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            bytes = getTtarch2File(arc2, file2, archKey, br);
                        }
                    }

                    if (bytes == null) return null;

                    return PreviewBuilder.Build(fileName, bytes, archKey, luaVersion);
                });
            }
            catch
            {
                result = null;
            }

            if (seq != previewSeq)
            {
                if (result != null && result.Image != null) result.Image.Dispose();
                return;
            }

            ShowPreviewResult(result, fileName, fileSize);
        }

        private void ShowPreviewMessage(string message)
        {
            SwapPreviewImage(null);
            previewPictureBox.Visible = true;
            previewTextBox.Visible = false;
            previewTextBox.Text = "";
            previewInfoLabel.Text = message;
        }

        private void ShowPreviewResult(PreviewBuilder.PreviewResult result, string fileName, long fileSize)
        {
            string baseInfo = fileName + "  |  " + fileSize.ToString("N0") + " " + Loc.T("ArchiveUnpacker.previewBytes", "bytes");

            if (result == null || result.Kind == PreviewBuilder.PreviewKind.None)
            {
                ShowPreviewMessage("");
                return;
            }

            if (result.Kind == PreviewBuilder.PreviewKind.Image)
            {
                SwapPreviewImage(result.Image);
                previewTextBox.Visible = false;
                previewTextBox.Text = "";
                previewPictureBox.Visible = true;
                previewPictureBox.BringToFront();
                previewInfoLabel.BringToFront();
                AdjustPreviewSizeMode();
            }
            else
            {
                SwapPreviewImage(null);
                previewTextBox.Visible = true;
                previewPictureBox.Visible = false;
                previewTextBox.BringToFront();
                previewInfoLabel.BringToFront();
                previewTextBox.Clear();
                previewTextBox.AppendText(result.Text ?? "");
                previewTextBox.SelectionStart = 0;
                previewTextBox.SelectionLength = 0;
                previewTextBox.ScrollToCaret();
                previewTextBox.Refresh();
            }

            previewInfoLabel.Text = string.IsNullOrEmpty(result.Info) ? baseInfo : baseInfo + "  |  " + result.Info;
        }

        private void SwapPreviewImage(System.Drawing.Image image)
        {
            var old = previewPictureBox.Image;
            previewPictureBox.Image = image;
            if (old != null) old.Dispose();
        }

        //Small images (font atlases, icons) look blurry when Zoom scales them up, so only zoom
        //when the image is actually bigger than the panel.
        private void AdjustPreviewSizeMode()
        {
            var img = previewPictureBox.Image;
            if (img == null) return;

            bool fits = img.Width <= previewPictureBox.ClientSize.Width && img.Height <= previewPictureBox.ClientSize.Height;
            previewPictureBox.SizeMode = fits ? PictureBoxSizeMode.CenterImage : PictureBoxSizeMode.Zoom;
        }

        #endregion
    }
}
