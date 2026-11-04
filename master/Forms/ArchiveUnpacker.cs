using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using TTG_Tools.ClassesStructs;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Drawing;

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
            filesDataGridView.PreviewMouseDown += filesDataGridView_MouseDown;
            filesDataGridView.MouseMove += filesDataGridView_MouseMove;
            previewTextBox.MaxLength = 1000000;
            previewTextBox.DetectUrls = true;
            previewTextBox.LinkClicked += previewTextBox_LinkClicked;

            SetupPreviewPanel();
            WireArchiveDropTargets(this);
            ShowPreviewMessage("");
            CleanupStaleDragFolders();
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
        private const int SearchButtonMinWidth = 75;
        private const int SearchButtonMaxWidth = 120;
        private Point fileDragStartPoint;
        private bool fileDragArmed;
        private bool preparingFileDrag;
        private int[] pendingFileDragIndexes;
        private static readonly string DragTempRoot = Path.Combine(Path.GetTempPath(), "TTGTools_DragOut");

        // Archives are input files. Opening them must never request write access or change their
        // attributes: both operations fail for ordinary users when a game is installed below
        // Program Files, even though reading the archive itself is permitted.
        private static FileStream OpenArchiveReadStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

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
                searchBtn.AutoSize = false;
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
            int searchButtonWidth = Math.Max(SearchButtonMinWidth,
                Math.Min(SearchButtonMaxWidth, TextRenderer.MeasureText(searchBtn.Text ?? "", searchBtn.Font).Width + 24));
            searchBtn.AutoSize = false;
            searchBtn.MinimumSize = new System.Drawing.Size(SearchButtonMinWidth, searchBtn.Height);
            searchBtn.MaximumSize = new System.Drawing.Size(SearchButtonMaxWidth, searchBtn.Height);
            searchBtn.SetBounds(searchTB.Right + controlGap, 70, searchButtonWidth, searchBtn.Height);
            decryptLuaCB.SetBounds(searchBtn.Right + controlGap, 74, decryptLuaCB.PreferredSize.Width, decryptLuaCB.Height);

            if (decryptLuaCB.Right > groupBox1.Left - 8)
            {
                decryptLuaCB.SetBounds(left, 101, decryptLuaCB.PreferredSize.Width, decryptLuaCB.Height);
                topPanel.Height = 132;
            }
            else
            {
                topPanel.Height = 108;
            }

            arrangingTopPanel = false;
        }

        // Prefixa com \\?\ para permitir caminhos maiores que MAX_PATH (260) no Win32.
        internal static string ToLongPath(string path)
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

            using (FileStream fs = OpenArchiveReadStream(path))
            using (BinaryReader br = new BinaryReader(fs))
            {
                version = br.ReadInt32();

                if (IsHeaderlessTtarch(version))
                {
                    byte[] oldHeader;
                    bool oldEncrypted;
                    uint oldFilesOffset;
                    ReadHeaderlessTtarch(br, out oldHeader, out oldEncrypted, out oldFilesOffset);

                    version = 0;
                    if (!oldEncrypted) return false;

                    header = oldHeader;
                    return header != null;
                }

                int encryption = br.ReadInt32();
                if (version > 1) br.ReadInt32(); //Version 1 archives don't have this field

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
                                if (version == 8) SkipV8Flags(br);
                                else br.ReadByte();
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
                    byte[] dirName = br.ReadBytes(nameLen);
                    if (nameLen > 0 && !IsPrintableAscii(dirName)) return false; //some v2 archives start with an empty folder name
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
            using (FileStream fs = OpenArchiveReadStream(path))
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

            int size = (int)OodleTools.Imports.OodleLZ_Decompress(bytes, bytes.Length, outBuf, decBufSize, 1, 0, 0, 0, 0, 0, 0, 0, 0, 3); //fuzzSafe: wrong-key garbage must fail, not crash the process
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

                //Immediate feedback so the title reflects the drop right away, even while it loads.
                Text = Loc.T("ArchiveUnpacker.titleLoading", "Archive Unpacker. Loading:") + " " + fi.Name;

                ttarch = null;
                ttarch2 = null;

                if (progressBar1.Value > 0) progressBar1.Value = 0;

                //Auto-detect which game this archive belongs to by finding the key that decrypts it.
                //Skipped when the user opted for a custom key, so manual choice is always honored.
                bool gameDetected = false;
                if (!useCustomKeyCB.Checked)
                {
                    int detected = await Task.Run(() => TryDetectGameIndex(fi.FullName));
                    gameDetected = detected >= 0;

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

                //Unencrypted containers (e.g. The Wolf Among Us .ttarch2) can't be identified from
                //their header, so the previous profile's game stayed selected and the encrypted Lua
                //scripts inside wouldn't decrypt. Identify the game from one of those scripts.
                if (!gameDetected && !useCustomKeyCB.Checked) await DetectGameFromLuaAsync();
            }
            catch (Exception ex)
            {
                Text = Loc.T("ArchiveUnpacker.$this", "Archive Unpacker");
                MessageBox.Show(Loc.T("ArchiveUnpacker.msgFailedOpen", "Failed to open archive.") + " " + ex.Message, Loc.T("Common.error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsLuaFileName(string name)
        {
            string ext = Methods.GetExtension(name).ToLower();
            return ext == ".lua" || ext == ".lenc";
        }

        //Decrypts a copy of an encrypted Lua script with one key and checks the result is real Lua:
        //compiled chunks restart with "\x1bLua" + version 0x50-0x52, format 0, little endian; source
        //scripts come out as readable text.
        private static bool LuaDecryptsWith(byte[] encrypted, byte[] key, int version)
        {
            try
            {
                byte[] d = Methods.decryptLua((byte[])encrypted.Clone(), key, version);
                if (d == null || d.Length < 8) return false;

                if (d[0] == 0x1B && d[1] == 'L' && d[2] == 'u' && d[3] == 'a')
                    return d[4] >= 0x50 && d[4] <= 0x52 && d[5] == 0 && d[6] == 1;

                int sample = Math.Min(d.Length, 512), printable = 0;
                for (int i = 0; i < sample; i++)
                {
                    byte c = d[i];
                    if (c == 9 || c == 10 || c == 13 || (c >= 0x20 && c < 0x7F)) printable++;
                }
                return printable >= sample * 95 / 100;
            }
            catch
            {
                return false;
            }
        }

        //The selected game first (keeps the user's choice when it already works), then the rest.
        private static int FindLuaGameIndex(byte[] encrypted, int version, int preferred)
        {
            if (preferred >= 0 && preferred < MainMenu.gamelist.Count && LuaDecryptsWith(encrypted, MainMenu.gamelist[preferred].key, version)) return preferred;

            for (int g = 0; g < MainMenu.gamelist.Count; g++)
            {
                if (g != preferred && LuaDecryptsWith(encrypted, MainMenu.gamelist[g].key, version)) return g;
            }

            return -1;
        }

        private async Task DetectGameFromLuaAsync()
        {
            var arc1 = ttarch;
            var arc2 = ttarch2;
            var file1 = default(ClassesStructs.TtarchClass.ttarchFiles);
            var file2 = default(ClassesStructs.Ttarch2Class.Ttarch2files);
            bool found = false;
            int version = 7;

            if (arc1 != null && arc1.files != null)
            {
                version = arc1.version;
                foreach (var f in arc1.files) if (IsLuaFileName(f.fileName)) { file1 = f; found = true; break; }
            }
            else if (arc2 != null && arc2.files != null)
            {
                foreach (var f in arc2.files) if (IsLuaFileName(f.fileName)) { file2 = f; found = true; break; }
            }

            if (!found) return;

            int selected = gameListCB.SelectedIndex;
            byte[] currentKey = MainMenu.gamelist[selected].key;

            int game = await Task.Run(() =>
            {
                try
                {
                    byte[] bytes = ReadPreviewBytes(arc1, file1, arc2, file2, currentKey);
                    if (bytes == null || !Methods.isLuaEncrypted(bytes)) return -1;
                    return FindLuaGameIndex(bytes, version, selected);
                }
                catch
                {
                    return -1;
                }
            });

            //Still the same archive? Then switch to the game whose key decrypts its scripts.
            if (game >= 0 && game != gameListCB.SelectedIndex && (arc1 == null || arc1 == ttarch) && (arc2 == null || arc2 == ttarch2))
                gameListCB.SelectedIndex = game;
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

            int size = (int)OodleTools.Imports.OodleLZ_Decompress(bytes, bufSize, retBytes, decBufSize, 1, 0, 0, 0, 0, 0, 0, 0, 0, 3); //fuzzSafe

            byte[] tmp = new byte[size];
            Array.Copy(retBytes, 0, tmp, 0, tmp.Length);

            return tmp;
        }

        private static void ReadTtarchFileTable(byte[] header, byte[] key, BinaryReader br)
        {
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
        }

        //Version 8 stores one flag byte after the chunk size. Some Wii archives (Tales of Monkey Island
        //episodes 4 and 5) have an extra int (1) before it; read normally, the header size would then be 0.
        private static void SkipV8Flags(BinaryReader br)
        {
            long pos = br.BaseStream.Position;
            byte flag = br.ReadByte();

            //Tales of Monkey Island Wii 102/104/105: flag 1 is followed by a table of
            //[int count] + count * [u64 name CRC + u32 value] before the header sizes. Reading the
            //first entry as the header size gave a negative compressed size ("Non-negative number
            //required"). Accept the table only when what follows it looks like real header sizes.
            if (flag == 1 && br.BaseStream.Position + 4 <= br.BaseStream.Length)
            {
                int count = br.ReadInt32();
                long after = br.BaseStream.Position + (long)count * 12;

                if (count > 0 && count < 1000000 && after + 8 <= br.BaseStream.Length)
                {
                    br.BaseStream.Seek(after, SeekOrigin.Begin);
                    int headerSize = br.ReadInt32();
                    int second = br.ReadInt32();

                    if (headerSize > 0 && second > 0 && after + 8 + Math.Min(headerSize, second) <= br.BaseStream.Length)
                    {
                        br.BaseStream.Seek(after, SeekOrigin.Begin);
                        return;
                    }
                }

                br.BaseStream.Seek(pos + 1, SeekOrigin.Begin);
            }

            if (br.BaseStream.Position + 4 <= br.BaseStream.Length && br.ReadInt32() == 0)
            {
                br.BaseStream.Seek(pos + 5, SeekOrigin.Begin);
                return;
            }

            br.BaseStream.Seek(pos + 1, SeekOrigin.Begin);
        }

        //The oldest archives (Bone, Sam & Max 101 2006 demo) have no version block: the file starts
        //either with the plain directory table or with the size of the Blowfish-encrypted table.
        private static bool IsHeaderlessTtarch(int firstValue)
        {
            return firstValue < 1 || firstValue > 9;
        }

        private static void ReadHeaderlessTtarch(BinaryReader br, out byte[] header, out bool encrypted, out uint filesOffset)
        {
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            long plainEnd = GetPlainTtarchHeaderEnd(br);
            br.BaseStream.Seek(0, SeekOrigin.Begin);

            if (plainEnd > 0)
            {
                header = br.ReadBytes((int)plainEnd);
                encrypted = false;
                filesOffset = br.ReadUInt32(); //absolute offset of the files block
                return;
            }

            int headerSize = br.ReadInt32();
            header = br.ReadBytes(headerSize);
            encrypted = true;
            filesOffset = (uint)br.BaseStream.Position;
        }

        //Returns the length of an unencrypted directory table starting at the current position,
        //or -1 when the bytes don't look like one.
        private static long GetPlainTtarchHeaderEnd(BinaryReader br)
        {
            try
            {
                long len = br.BaseStream.Length;
                int dirsCount = br.ReadInt32();
                if (dirsCount < 0 || dirsCount > 100000) return -1;

                for (int d = 0; d < dirsCount; d++)
                {
                    int nameLen = br.ReadInt32();
                    if (nameLen < 0 || nameLen > 4096 || br.BaseStream.Position + nameLen > len) return -1;
                    byte[] name = br.ReadBytes(nameLen);
                    if (nameLen > 0 && !IsPrintableAscii(name)) return -1;
                }

                int filesCount = br.ReadInt32();
                if (filesCount < 0 || filesCount > 5000000) return -1;

                //An empty placeholder archive (CSI 3DoM Disc2.ttarch, 16 bytes) is only the table
                //plus the files offset/size pair.
                if (filesCount == 0 && br.BaseStream.Position + 8 != len) return -1;

                for (int f = 0; f < filesCount; f++)
                {
                    int nameLen = br.ReadInt32();
                    if (nameLen <= 0 || nameLen > 4096 || br.BaseStream.Position + nameLen + 12 > len) return -1;
                    if (!IsPrintableAscii(br.ReadBytes(nameLen))) return -1;
                    br.BaseStream.Seek(12, SeekOrigin.Current);
                }

                return br.BaseStream.Position;
            }
            catch
            {
                return -1;
            }
        }

        private void ReadHeaderTtarch(string path, byte[] key)
        {
            try
            {
                ttarch.filePath = path;
                ttarch.fileFormats = new List<string>();
                FileStream fs = OpenArchiveReadStream(path);
                BinaryReader br = new BinaryReader(fs);
                ttarch.version = br.ReadInt32();

                //A headerless archive starts with its folder count, which can be 1..9 too
                //(CSI: 3 Dimensions of Murder APatch1 = 7, APatch1A = 1) and was then read as a
                //version. A complete plain directory/file table from offset 0 settles it.
                bool plainTable = false;
                if (!IsHeaderlessTtarch(ttarch.version))
                {
                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    plainTable = GetPlainTtarchHeaderEnd(br) > 0;
                    br.BaseStream.Seek(4, SeekOrigin.Begin);
                }

                if (IsHeaderlessTtarch(ttarch.version) || plainTable)
                {
                    //Bone / Sam & Max 2006 demo era: no version block at all
                    byte[] oldHeader;
                    bool oldEncrypted;
                    uint oldFilesOffset;
                    ReadHeaderlessTtarch(br, out oldHeader, out oldEncrypted, out oldFilesOffset);

                    ttarch.version = 0;
                    ttarch.filesOffset = oldFilesOffset;

                    if (oldEncrypted)
                    {
                        ttarch.isEncrypted = true;
                        BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, 0);
                        oldHeader = dec.Crypt_ECB(oldHeader, 0, true);
                    }

                    ReadTtarchFileTable(oldHeader, key, br);

                    br.Close();
                    fs.Close();
                    return;
                }

                int encryption = br.ReadInt32();
                if (ttarch.version > 1) br.ReadInt32(); //Version 1 archives don't have this field

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
                    else if (countCompressedBlocks > 0)
                    {
                        //Jurassic Park Xbox "sknm" archives are not compressed but still list their
                        //(raw 64 KB) chunks; the data stays contiguous, so just skip the list.
                        br.BaseStream.Seek((long)countCompressedBlocks * 4, SeekOrigin.Current);
                    }

                    uint arcSize = br.ReadUInt32(); //Size of block with files

                    if (ttarch.version >= 4)
                    {
                        int priority = br.ReadInt32();
                        int priority2 = br.ReadInt32();

                        //Some v5/v6 archives (Sam & Max 200 Wii "fontfix") already carry the two
                        //v7 X-mode ints (0/1 each) before the header size. A header size is
                        //never 0 or 1, so a 0/1 here can only be those flags.
                        if ((ttarch.version == 5 || ttarch.version == 6) && br.BaseStream.Position + 12 <= br.BaseStream.Length)
                        {
                            long flagsPos = br.BaseStream.Position;
                            int flagA = br.ReadInt32();
                            int flagB = br.ReadInt32();

                            if (!((flagA == 0 || flagA == 1) && (flagB == 0 || flagB == 1))) br.BaseStream.Seek(flagsPos, SeekOrigin.Begin);
                        }

                        if(ttarch.version >= 7)
                        {
                            int someVal = br.ReadInt32();
                            int someVal2 = br.ReadInt32();

                            ttarch.isXmode = someVal == 1 || someVal2 == 1;
                            ttarch.chunkSize = br.ReadInt32();

                            if(ttarch.version > 7)
                            {
                                if (ttarch.version == 8) SkipV8Flags(br);
                                else br.ReadByte();

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

                //Oldest compressed archives: find out the compression algorithm (zlib or deflate) from the first
                //block before the file table is read, since checking its Lua files already decompresses blocks.
                if(ttarch.version < 7 && ttarch.isCompressed)
                {
                    br.BaseStream.Seek(ttarch.filesOffset, SeekOrigin.Begin);
                    byte[] tmp = br.ReadBytes(ttarch.compressedBlocks[0]);

                    if(ttarch.isEncrypted)
                    {
                        BlowFishCS.BlowFish dec = new BlowFishCS.BlowFish(key, ttarch.version);
                        tmp = dec.Crypt_ECB(tmp, ttarch.version, true);
                    }

                    decompressBlock(tmp);
                }

                ReadTtarchFileTable(header, key, br);


                br.Close();
                fs.Close();
            }
            catch(Exception ex)
            {
                if (cliErrorSink != null) cliErrorSink(ex.ToString());
                else ShowArchiveMessage(Loc.T("ArchiveUnpacker.msgUnknownErrorEx", "Unknown error. Please try another archive or change encryption key.\r\nGot exception:") + "\r\n" + ex.Message, Loc.T("ArchiveUnpacker.titleSomethingWrong", "Something goes wrong"), MessageBoxIcon.Error);
                ttarch = null;
            }
        }

        private void UnpackTtarch(string folderPath, string format, string searchPattern = null, int[] indexes = null)
        {
            var files = getFilteredTtarchFiles(format, searchPattern);
            List<string> failedFiles = new List<string>();

            FileStream fs = OpenArchiveReadStream(ttarch.filePath);
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
                FileStream fs = OpenArchiveReadStream(path);
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

                    //Without the Oodle library every block fails to decompress and the archive looks like it
                    //has a wrong key, which is misleading: console archives aren't even encrypted.
                    if (ttarch2.compressAlgorithm == 2 && !OodleTools.Imports.IsAvailable())
                    {
                        br.Close();
                        fs.Close();
                        ShowArchiveMessage(string.Format(Loc.T("ArchiveUnpacker.msgOodleMissing", "This archive is compressed with Oodle, but {0} wasn't found.\r\nCopy {0} from the folder of a newer Telltale PC game (e.g. Batman, The Walking Dead: The Telltale Definitive Series) into the TTG Tools folder and open the archive again."), OodleTools.Imports.DllName), Loc.T("ArchiveUnpacker.titleOodleMissing", "Oodle library not found"), MessageBoxIcon.Warning);
                        ttarch2 = null;
                        return;
                    }

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
            catch (Exception ex)
            {
                if (cliErrorSink != null) cliErrorSink(ex.ToString());
                else ShowArchiveMessage(Loc.T("ArchiveUnpacker.msgUnknownError", "Unknown error. Please try another archive or change encryption key."), Loc.T("ArchiveUnpacker.titleSomethingWrong", "Something goes wrong"), MessageBoxIcon.Error);
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

            FileStream fs = OpenArchiveReadStream(ttarch2.fileName);
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

        //Header reading and preview extraction run on worker threads. A MessageBox shown there has
        //no owner and can open BEHIND the window: the open never finishes until it is closed, so
        //the window looked frozen and a newly dropped archive "didn't switch". Show it on the UI
        //thread, owned by the Archive Unpacker window.
        private static void ShowArchiveMessage(string text, string title, MessageBoxIcon icon)
        {
            Form owner = null;
            try { owner = Application.OpenForms.OfType<ArchiveUnpacker>().FirstOrDefault(); } catch { }

            if (owner != null && !owner.IsDisposed && owner.IsHandleCreated)
            {
                if (owner.InvokeRequired) owner.Invoke(new Action(() => MessageBox.Show(owner, text, title, MessageBoxButtons.OK, icon)));
                else MessageBox.Show(owner, text, title, MessageBoxButtons.OK, icon);
                return;
            }

            MessageBox.Show(text, title, MessageBoxButtons.OK, icon);
        }

        //Set by the --open-all CLI so header-reading failures are logged instead of shown.
        private static Action<string> cliErrorSink;

        //--open-all <folder> <log>: reads the header/file table of every archive below a folder
        //(with the same key auto-detection as the GUI) and logs the ones that fail.
        public static void OpenAllCli(string folder, string logPath)
        {
            var log = new List<string>();
            int ok = 0, fail = 0;
            var inst = (ArchiveUnpacker)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ArchiveUnpacker));
            var read1 = typeof(ArchiveUnpacker).GetMethod("ReadHeaderTtarch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var read2 = typeof(ArchiveUnpacker).GetMethod("ReadHeaderTtarch2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (string path in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".ttarch" && ext != ".ttarch2") continue;

                string error = null;
                cliErrorSink = e => error = e;
                File.WriteAllText(logPath + ".current", path); //names the culprit if a native crash kills the run

                try
                {
                    int detected = TryDetectGameIndex(path);
                    byte[] key = MainMenu.gamelist[detected >= 0 ? detected : 0].key;

                    if (ext == ".ttarch")
                    {
                        ttarch = new ClassesStructs.TtarchClass();
                        read1.Invoke(inst, new object[] { path, key });
                        if (ttarch == null && error == null) error = "null archive";
                    }
                    else
                    {
                        ttarch2 = new ClassesStructs.Ttarch2Class();
                        read2.Invoke(inst, new object[] { path, key });
                        if (ttarch2 == null && error == null) error = "null archive";
                    }
                }
                catch (Exception ex)
                {
                    error = (ex.InnerException ?? ex).ToString();
                }

                if (error == null) ok++;
                else
                {
                    fail++;
                    log.Add("FAIL " + path + Environment.NewLine + "    " + error);
                }
            }

            cliErrorSink = null;
            log.Add("OK " + ok + " / FAIL " + fail);
            File.WriteAllLines(logPath, log.ToArray());
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
            using (var fs = OpenArchiveReadStream(archivePath))
            using (var br = new BinaryReader(fs))
            {
                for (int i = 0; i < ttarch.files.Length; i++)
                {
                    byte[] data = getTtarchFile(ttarch, ttarch.files[i], key, chunkSz, br);
                    if (data == null) { Console.WriteLine("FAIL: " + ttarch.files[i].fileName); continue; }
                    string outPath = Path.Combine(outputDir, ttarch.files[i].fileName);
                    File.WriteAllBytes(ToLongPath(outPath), data); //Some old file names (Bone) exceed MAX_PATH
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

            DecryptMetaStream(result, key, ttarch.version);
            return result;
        }

        //Files inside old archives may be meta-encrypted (Tales of Monkey Island 102+ PC stores
        //every resource that way). They used to be decrypted only from uncompressed archives and
        //kept their "encrypted" magic, so an extracted .font/.d3dtx dropped loose in the game folder
        //was decrypted a second time by the engine and crashed it. Decrypt in both cases and give
        //the result the plain magic the content now has: MBES -> MBIN, every other variant -> ERTM.
        private static void DecryptMetaStream(byte[] data, byte[] key, int version)
        {
            if (data == null || data.Length < 4) return;

            uint magic = BitConverter.ToUInt32(data, 0);
            if (Methods.meta_crypt(data, key, version, true) != 1) return;

            uint plain = magic == 0x4D424553 ? 0x4D42494Eu : 0x4D545245u;
            Array.Copy(BitConverter.GetBytes(plain), 0, data, 0, 4);
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
                    ShowArchiveMessage(Loc.T("ArchiveUnpacker.msgOffsetWrong", "Something wrong with offset in compressed archive"), Loc.T("Common.error", "Error"), MessageBoxIcon.Error);

                    return null;
                }

                if (index2 > ttarch2.compressedBlocks.Length)
                {
                    ShowArchiveMessage(Loc.T("ArchiveUnpacker.msgOffsetWrong", "Something wrong with offset in compressed archive"), Loc.T("Common.error", "Error"), MessageBoxIcon.Error);

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
                int[] indexes = GetSelectedFileIndexes();

                if (indexes.Length == 0)
                {
                    MessageBox.Show(Loc.T("ArchiveUnpacker.msgSelectValidFiles", "Please select valid files from list first."), Loc.T("ArchiveUnpacker.titleNoSelection", "No selected files from list"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
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

        //Only the form itself was a drop target. OLE hands a drop on a plain child window to the
        //nearest registered ancestor (the form), but the RichTextBox text preview registers its
        //own OLE target and swallowed a .ttarch dropped on it, so "sometimes it doesn't open".
        //Route its drops to the same handlers. Other children must NOT get AllowDrop: registering
        //them made the audio player's handle creation fail ("DragDrop registration did not succeed").
        private void WireArchiveDropTargets(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                RichTextBox rtb = c as RichTextBox;
                if (rtb != null)
                {
                    rtb.EnableAutoDragDrop = false;
                    rtb.AllowDrop = true;
                    rtb.DragEnter += ArchiveUnpacker_DragEnter;
                    rtb.DragOver += ArchiveUnpacker_DragEnter;
                    rtb.DragDrop += ArchiveUnpacker_DragDrop;
                }

                WireArchiveDropTargets(c);
            }
        }

        private async void ArchiveUnpacker_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            await OpenArchiveFile(files[0]);
        }

        #region Drag selected archive files to Windows Explorer

        private sealed class DragExtractionResult
        {
            public readonly List<string> Files = new List<string>();
            public readonly List<string> Errors = new List<string>();
        }

        private void filesDataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            fileDragArmed = false;
            pendingFileDragIndexes = null;
            if (e.Button != MouseButtons.Left || preparingFileDrag) return;

            DataGridView.HitTestInfo hit = filesDataGridView.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;

            //DataGridView normally collapses Ctrl+A/multi-selection in its own MouseDown
            //processing. This preview event runs immediately before that happens, so keep the
            //original archive indexes only for an ordinary click on an already selected row.
            //With Ctrl/Shift, let the grid apply its native add/remove/range selection first.
            bool selectionModifier = (ModifierKeys & (Keys.Control | Keys.Shift)) != Keys.None;
            if (!selectionModifier && filesDataGridView.Rows[hit.RowIndex].Selected)
                pendingFileDragIndexes = GetSelectedFileIndexes();

            fileDragStartPoint = e.Location;
            fileDragArmed = true;
        }

        private async void filesDataGridView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!fileDragArmed || preparingFileDrag || e.Button != MouseButtons.Left) return;

            Size dragSize = SystemInformation.DragSize;
            Rectangle dragBox = new Rectangle(
                fileDragStartPoint.X - dragSize.Width / 2,
                fileDragStartPoint.Y - dragSize.Height / 2,
                dragSize.Width,
                dragSize.Height);

            if (dragBox.Contains(e.Location)) return;

            fileDragArmed = false;
            int[] indexes = pendingFileDragIndexes;
            pendingFileDragIndexes = null;
            RestoreSelectedFileIndexes(indexes);
            await BeginSelectedFilesDragAsync(indexes);
        }

        private async Task BeginSelectedFilesDragAsync(int[] indexes = null)
        {
            if (indexes == null) indexes = GetSelectedFileIndexes();
            if (indexes.Length == 0 || (ttarch == null && ttarch2 == null)) return;

            var arc1 = ttarch;
            var arc2 = ttarch2;
            var selected1 = arc1 != null && shownTtarchFiles != null
                ? indexes.Where(i => i >= 0 && i < shownTtarchFiles.Length).Select(i => shownTtarchFiles[i]).ToArray()
                : null;
            var selected2 = arc2 != null && shownTtarch2Files != null
                ? indexes.Where(i => i >= 0 && i < shownTtarch2Files.Length).Select(i => shownTtarch2Files[i]).ToArray()
                : null;

            if ((selected1 == null || selected1.Length == 0) && (selected2 == null || selected2.Length == 0)) return;

            byte[] archiveKey = (byte[])MainMenu.gamelist[gameListCB.SelectedIndex].key.Clone();
            bool decryptLua = decryptLuaCB.Checked;
            string tempRoot = Path.Combine(DragTempRoot, Guid.NewGuid().ToString("N"));

            preparingFileDrag = true;
            UseWaitCursor = true;

            DragExtractionResult extraction;
            try
            {
                Directory.CreateDirectory(tempRoot);
                extraction = await Task.Run(() => arc1 != null
                    ? ExtractTtarchFilesForDrag(arc1, selected1, archiveKey, decryptLua, tempRoot)
                    : ExtractTtarch2FilesForDrag(arc2, selected2, archiveKey, decryptLua, tempRoot));
            }
            catch (Exception ex)
            {
                extraction = new DragExtractionResult();
                extraction.Errors.Add(ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                UseWaitCursor = false;
                preparingFileDrag = false;
            }

            if (extraction.Files.Count > 0 && Control.MouseButtons.HasFlag(MouseButtons.Left))
            {
                var data = new DataObject();
                data.SetData(DataFormats.FileDrop, true, extraction.Files.ToArray());
                DragDropEffects effect = filesDataGridView.DoDragDrop(data, DragDropEffects.Copy);
                ScheduleDragFolderCleanup(tempRoot, effect == DragDropEffects.None ? 0 : 120000);
            }
            else
            {
                ScheduleDragFolderCleanup(tempRoot, 0);
            }

            if (extraction.Errors.Count > 0)
            {
                MessageBox.Show(
                    Loc.T("ArchiveUnpacker.msgFailedExtractList", "The following files could not be extracted:") + "\n\n" + string.Join("\n", extraction.Errors),
                    Loc.T("ArchiveUnpacker.titleFilesNotExtracted", "Files not extracted"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private DragExtractionResult ExtractTtarchFilesForDrag(
            ClassesStructs.TtarchClass archive,
            ClassesStructs.TtarchClass.ttarchFiles[] files,
            byte[] archiveKey,
            bool decryptLua,
            string tempRoot)
        {
            var result = new DragExtractionResult();
            SetMinimum(0);
            SetMaximum(files.Length);

            using (FileStream fs = OpenArchiveReadStream(archive.filePath))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int chunkSize = archive.chunkSize * 1024;

                for (int i = 0; i < files.Length; i++)
                {
                    string name = files[i].fileName;
                    try
                    {
                        byte[] bytes = getTtarchFile(archive, files[i], archiveKey, chunkSize, br);
                        if (bytes == null) throw new InvalidDataException("Could not read the archive entry.");

                        if (decryptLua && name.EndsWith(".lenc", StringComparison.OrdinalIgnoreCase))
                        {
                            name = name.Substring(0, name.Length - 4) + "lua";
                            bytes = Methods.decryptLua(bytes, archiveKey, archive.version);
                        }

                        result.Files.Add(WriteDragFile(tempRoot, name, bytes));
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(name + " (" + ex.GetType().Name + ": " + ex.Message + ")");
                    }

                    Progress(i + 1);
                }
            }

            return result;
        }

        private DragExtractionResult ExtractTtarch2FilesForDrag(
            ClassesStructs.Ttarch2Class archive,
            ClassesStructs.Ttarch2Class.Ttarch2files[] files,
            byte[] archiveKey,
            bool decryptLua,
            string tempRoot)
        {
            var result = new DragExtractionResult();
            SetMinimum(0);
            SetMaximum(files.Length);

            using (FileStream fs = OpenArchiveReadStream(archive.fileName))
            using (BinaryReader br = new BinaryReader(fs))
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string name = files[i].fileName;
                    try
                    {
                        byte[] bytes = getTtarch2File(archive, files[i], archiveKey, br);
                        if (bytes == null) throw new InvalidDataException("Could not read the archive entry.");

                        if (decryptLua && (name.EndsWith(".lenc", StringComparison.OrdinalIgnoreCase)
                            || name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (name.EndsWith(".lenc", StringComparison.OrdinalIgnoreCase))
                                name = name.Substring(0, name.Length - 4) + "lua";
                            bytes = Methods.decryptLua(bytes, archiveKey, 7);
                        }

                        result.Files.Add(WriteDragFile(tempRoot, name, bytes));
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(name + " (" + ex.GetType().Name + ": " + ex.Message + ")");
                    }

                    Progress(i + 1);
                }
            }

            return result;
        }

        private static string WriteDragFile(string tempRoot, string archiveName, byte[] bytes)
        {
            string root = Path.GetFullPath(tempRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string relative = (archiveName ?? "").Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            string outputPath = Path.GetFullPath(Path.Combine(root, relative));

            if (!outputPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Archive entry points outside the extraction folder.");

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(ToLongPath(directory));
            File.WriteAllBytes(ToLongPath(outputPath), bytes);
            return outputPath;
        }

        private static async void ScheduleDragFolderCleanup(string path, int delayMilliseconds)
        {
            if (delayMilliseconds > 0) await Task.Delay(delayMilliseconds);
            try { if (Directory.Exists(path)) Directory.Delete(ToLongPath(path), true); }
            catch { }
        }

        private static void CleanupStaleDragFolders()
        {
            try
            {
                if (!Directory.Exists(DragTempRoot)) return;

                foreach (string directory in Directory.GetDirectories(DragTempRoot))
                {
                    try
                    {
                        if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow.AddDays(-1))
                            Directory.Delete(ToLongPath(directory), true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        #endregion

        #region Preview panel

        //Bumped on every preview request; a finished background build only gets displayed when its
        //sequence still matches, so rapid selection changes never show a stale preview.
        private volatile int previewSeq; //read by preview worker threads to notice they were superseded

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
                int luaGame = -1;
                bool tryOtherKeys = !useCustomKeyCB.Checked;
                int selectedGame = gameListCB.SelectedIndex;

                result = await Task.Run(() =>
                {
                    byte[] bytes = ReadPreviewBytes(arc1, file1, arc2, file2, archKey);
                    if (bytes == null) return null;

                    byte[] previewKey = archKey;
                    if (tryOtherKeys && IsLuaFileName(fileName) && Methods.isLuaEncrypted(bytes) && !LuaDecryptsWith(bytes, archKey, luaVersion))
                    {
                        luaGame = FindLuaGameIndex(bytes, luaVersion, selectedGame);
                        if (luaGame >= 0) previewKey = MainMenu.gamelist[luaGame].key;
                    }

                    return PreviewBuilder.Build(fileName, bytes, previewKey, luaVersion, () => seq != previewSeq);
                });

                if (luaGame >= 0 && luaGame != gameListCB.SelectedIndex && seq == previewSeq) gameListCB.SelectedIndex = luaGame;
            }
            catch
            {
                result = null;
            }

            if (seq != previewSeq)
            {
                if (result != null && result.Image != null) result.Image.Dispose();
                if (result != null && result.Pages != null) foreach (var page in result.Pages) if (page != result.Image) page.Dispose();
                return;
            }

            ShowPreviewResult(result, fileName, fileSize);
        }

        private static byte[] ReadPreviewBytes(ClassesStructs.TtarchClass arc1, ClassesStructs.TtarchClass.ttarchFiles file1,
            ClassesStructs.Ttarch2Class arc2, ClassesStructs.Ttarch2Class.Ttarch2files file2, byte[] archKey)
        {
            if (arc1 != null)
            {
                using (FileStream fs = OpenArchiveReadStream(arc1.filePath))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    return getTtarchFile(arc1, file1, archKey, arc1.chunkSize * 1024, br);
                }
            }

            using (FileStream fs = OpenArchiveReadStream(arc2.fileName))
            using (BinaryReader br = new BinaryReader(fs))
            {
                return getTtarch2File(arc2, file2, archKey, br);
            }
        }

        //Used by the --preview-all CLI: builds the preview of every shown file synchronously,
        //exactly like the panel does, without going through grid selection.
        //Used by the --formats CLI: extension counts plus the raw bytes of the first file of each.
        internal Dictionary<string, KeyValuePair<int, byte[]>> SampleFormatsForTest()
        {
            var result = new Dictionary<string, KeyValuePair<int, byte[]>>(StringComparer.OrdinalIgnoreCase);
            if ((ttarch == null || shownTtarchFiles == null) && (ttarch2 == null || shownTtarch2Files == null)) return result;

            byte[] archKey = MainMenu.gamelist[gameListCB.SelectedIndex].key;
            int count = ttarch != null ? shownTtarchFiles.Length : shownTtarch2Files.Length;

            for (int i = 0; i < count; i++)
            {
                string name = ttarch != null ? shownTtarchFiles[i].fileName : shownTtarch2Files[i].fileName;
                string ext = Methods.GetExtension(name).ToLower();
                KeyValuePair<int, byte[]> entry;

                if (result.TryGetValue(ext, out entry))
                {
                    result[ext] = new KeyValuePair<int, byte[]>(entry.Key + 1, entry.Value);
                    continue;
                }

                byte[] bytes = null;
                try
                {
                    bytes = ReadPreviewBytes(ttarch, ttarch != null ? shownTtarchFiles[i] : default(ClassesStructs.TtarchClass.ttarchFiles),
                        ttarch2, ttarch2 != null ? shownTtarch2Files[i] : default(ClassesStructs.Ttarch2Class.Ttarch2files), archKey);
                }
                catch { }

                result[ext] = new KeyValuePair<int, byte[]>(1, bytes);
            }

            return result;
        }

        internal void BuildAllPreviewsForTest(string extFilter, Action<string, byte[], PreviewBuilder.PreviewResult> onResult, int limit = 0)
        {
            int done = 0;

            if ((ttarch == null || shownTtarchFiles == null) && (ttarch2 == null || shownTtarch2Files == null)) return; //archive did not open

            byte[] archKey = MainMenu.gamelist[gameListCB.SelectedIndex].key;
            int count = ttarch != null ? shownTtarchFiles.Length : shownTtarch2Files.Length;

            for (int i = 0; i < count; i++)
            {
                string name = ttarch != null ? shownTtarchFiles[i].fileName : shownTtarch2Files[i].fileName;
                if (extFilter != null && !name.EndsWith(extFilter, StringComparison.OrdinalIgnoreCase)) continue;
                if (limit > 0 && done++ >= limit) break;

                PreviewBuilder.PreviewResult res;
                byte[] bytes = null;

                //Names the culprit if a native decoder crash kills the sweep.
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "ttg_preview_current.txt"), name);

                try
                {
                    bytes = ReadPreviewBytes(ttarch, ttarch != null ? shownTtarchFiles[i] : default(ClassesStructs.TtarchClass.ttarchFiles),
                        ttarch2, ttarch2 != null ? shownTtarch2Files[i] : default(ClassesStructs.Ttarch2Class.Ttarch2files), archKey);
                    res = bytes == null ? null : PreviewBuilder.Build(name, bytes, archKey, ttarch != null ? ttarch.version : 7);
                }
                catch (Exception ex)
                {
                    res = new PreviewBuilder.PreviewResult { Kind = PreviewBuilder.PreviewKind.None, Info = "EXCEPTION " + ex.Message };
                }

                onResult(name, bytes, res);
            }
        }

        private void ShowPreviewMessage(string message)
        {
            SwapPreviewImage(null, null);
            HideAudioPlayer();
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
                //Say why when the builder knows (e.g. "FSB5 codec XMA not supported"), first line only.
                string why = result != null && !string.IsNullOrEmpty(result.Info) ? result.Info.Split('\n')[0].Trim() : "";
                ShowPreviewMessage(why.Length > 0 && why.Length <= 140 ? baseInfo + "  |  " + why : "");
                return;
            }

            if (result.Kind != PreviewBuilder.PreviewKind.Audio) HideAudioPlayer();

            if (result.Kind == PreviewBuilder.PreviewKind.Audio)
            {
                SwapPreviewImage(null, null);
                previewTextBox.Visible = false;
                previewTextBox.Text = "";
                previewPictureBox.Visible = false;
                EnsureAudioPlayer();
                previewAudio.LoadAudio(result.Audio.Pcm, result.Audio.SampleRate, result.Audio.Channels);
                previewAudio.Visible = true;
                previewAudio.BringToFront();
            }
            else if (result.Kind == PreviewBuilder.PreviewKind.Image)
            {
                SwapPreviewImage(result.Image, result.Pages);
                previewTextBox.Visible = false;
                previewTextBox.Text = "";
                previewPictureBox.Visible = true;
                previewPictureBox.BringToFront();
            }
            else
            {
                SwapPreviewImage(null, null);
                previewTextBox.Visible = true;
                previewPictureBox.Visible = false;
                previewTextBox.BringToFront(); //the footer panel stays at the back, so it still docks first
                previewTextBox.Text = result.Text ?? "";
                previewTextBox.SelectionStart = 0;
                previewTextBox.SelectionLength = 0;
                previewTextBox.ScrollToCaret();
                previewTextBox.Refresh();
            }

            previewInfoLabel.Text = string.IsNullOrEmpty(result.Info) ? baseInfo : baseInfo + "  |  " + result.Info;
        }

        private void previewTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.LinkText)) return;

            try
            {
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch
            {
                // Keep the preview passive if Windows cannot resolve the link.
            }
        }

        private int[] GetSelectedFileIndexes()
        {
            var indexes = new List<int>();

            foreach (DataGridViewRow row in filesDataGridView.SelectedRows)
            {
                object cellValue = row.Cells[0].Value;
                int number;

                if (cellValue != null && int.TryParse(cellValue.ToString(), out number) && number > 0)
                    indexes.Add(number - 1);
            }

            return indexes.Distinct().OrderBy(i => i).ToArray();
        }

        private void RestoreSelectedFileIndexes(int[] indexes)
        {
            if (indexes == null || indexes.Length == 0) return;

            var wanted = new HashSet<int>(indexes);
            filesDataGridView.ClearSelection();

            foreach (DataGridViewRow row in filesDataGridView.Rows)
            {
                object cellValue = row.Cells[0].Value;
                int number;
                if (cellValue != null && int.TryParse(cellValue.ToString(), out number) && wanted.Contains(number - 1))
                    row.Selected = true;
            }
        }

        //The image is painted by hand (not through PictureBox.Image) so transparent pixels sit on a
        //checkerboard, like image editors show them, instead of the panel color.
        private Bitmap previewImage;
        private List<Bitmap> previewPages;
        private int previewPageIndex;
        private Panel previewFooterPanel;
        private Button previewPrevButton;
        private Button previewNextButton;
        private Label previewPageLabel;
        private TextureBrush checkerBrush;
        private AudioPlayerPanel previewAudio;

        //The info label used to be docked straight into the panel, and the BringToFront calls made
        //the Fill controls dock first, so the label was painted over the bottom of the image. It
        //now lives in a footer panel that is docked before (behind) the image and text boxes.
        private void SetupPreviewPanel()
        {
            Control host = previewInfoLabel.Parent;

            previewFooterPanel = new Panel { Dock = DockStyle.Bottom, Height = 24 };
            previewPrevButton = new Button { Text = "◀", Width = 28, Dock = DockStyle.Right, FlatStyle = FlatStyle.System };
            previewNextButton = new Button { Text = "▶", Width = 28, Dock = DockStyle.Right, FlatStyle = FlatStyle.System };
            previewPageLabel = new Label { AutoSize = false, Width = 110, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleCenter };
            previewPrevButton.Click += (s, e) => ShowPreviewPage(previewPageIndex - 1);
            previewNextButton.Click += (s, e) => ShowPreviewPage(previewPageIndex + 1);

            host.Controls.Remove(previewInfoLabel);
            previewInfoLabel.Dock = DockStyle.Fill;
            previewInfoLabel.AutoEllipsis = true;

            //Docking runs from the back of the z-order: Fill (info) must be added first so the
            //right-docked page controls claim their space before it.
            previewFooterPanel.Controls.Add(previewInfoLabel);
            previewFooterPanel.Controls.Add(previewPrevButton);
            previewFooterPanel.Controls.Add(previewPageLabel);
            previewFooterPanel.Controls.Add(previewNextButton);
            host.Controls.Add(previewFooterPanel);
            previewFooterPanel.SendToBack();
            SetPageControlsVisible(false);


            previewPictureBox.Image = null;
            //Create the RichTextBox handle now. Created lazily by the first "Visible = true" in
            //ShowPreviewResult, the text assigned right after it was dropped and the first text
            //preview of the window stayed blank.
            IntPtr textBoxHandle = previewTextBox.Handle;
            previewPictureBox.Paint += previewPictureBox_Paint;
            previewPictureBox.Resize += (s, e) => previewPictureBox.Invalidate();

            Bitmap tile = new Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(tile))
            {
                //Mid-gray squares like Paint.NET: white glyphs and textures stay readable on both.
                g.Clear(Color.FromArgb(191, 191, 191));
                using (var dark = new SolidBrush(Color.FromArgb(143, 143, 143)))
                {
                    g.FillRectangle(dark, 0, 0, 8, 8);
                    g.FillRectangle(dark, 8, 8, 8, 8);
                }
            }
            checkerBrush = new TextureBrush(tile);
        }

        //Created on first use, when the window already exists: built in the constructor (before
        //the form handle), its child windows ended up on WinForms' parking window and the player
        //stayed blank.
        private void EnsureAudioPlayer()
        {
            if (previewAudio != null) return;

            previewAudio = new AudioPlayerPanel { Dock = DockStyle.Fill };
            previewFooterPanel.Parent.Controls.Add(previewAudio);
            previewAudio.BringToFront();
            previewFooterPanel.SendToBack();
        }

        private void HideAudioPlayer()
        {
            if (previewAudio == null) return;
            previewAudio.Unload();
            previewAudio.Visible = false;
        }

        private void SetPageControlsVisible(bool visible)
        {
            previewPrevButton.Visible = visible;
            previewNextButton.Visible = visible;
            previewPageLabel.Visible = visible;
        }

        private void SwapPreviewImage(Bitmap image, List<Bitmap> pages)
        {
            var oldImage = previewImage;
            var oldPages = previewPages;

            previewImage = image;
            previewPages = pages != null && pages.Count > 1 ? pages : null;
            previewPageIndex = 0;

            if (oldPages != null)
            {
                foreach (Bitmap p in oldPages) if (p != image) p.Dispose();
            }
            else if (oldImage != null && oldImage != image)
            {
                oldImage.Dispose();
            }

            //A single-page list is not kept; drop its extra references.
            if (pages != null && previewPages == null)
            {
                foreach (Bitmap p in pages) if (p != image) p.Dispose();
            }

            UpdatePageControls();
            previewPictureBox.Invalidate();
        }

        private void ShowPreviewPage(int index)
        {
            if (previewPages == null || index < 0 || index >= previewPages.Count) return;

            previewPageIndex = index;
            previewImage = previewPages[index];
            UpdatePageControls();

            //Keep the "W x H" part of the info line in sync with the page shown.
            previewInfoLabel.Text = System.Text.RegularExpressions.Regex.Replace(previewInfoLabel.Text, @"\|  \d+ x \d+", "|  " + previewImage.Width + " x " + previewImage.Height);

            previewPictureBox.Invalidate();
        }

        private void UpdatePageControls()
        {
            bool multi = previewPages != null;
            SetPageControlsVisible(multi);
            if (!multi) return;

            previewPageLabel.Text = string.Format(Loc.T("ArchiveUnpacker.previewPage", "Page {0} of {1}"), previewPageIndex + 1, previewPages.Count);
            previewPrevButton.Enabled = previewPageIndex > 0;
            previewNextButton.Enabled = previewPageIndex < previewPages.Count - 1;
        }

        //Small images (font atlases, icons) look blurry when scaled up, so they are drawn 1:1 and
        //centered; only images bigger than the panel are shrunk to fit, keeping the aspect ratio.
        private void previewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            Bitmap img = previewImage;
            if (img == null) return;

            Size area = previewPictureBox.ClientSize;
            if (area.Width <= 0 || area.Height <= 0) return;

            double scale = Math.Min(1.0, Math.Min((double)area.Width / img.Width, (double)area.Height / img.Height));
            int w = Math.Max(1, (int)Math.Round(img.Width * scale));
            int h = Math.Max(1, (int)Math.Round(img.Height * scale));
            var dest = new Rectangle((area.Width - w) / 2, (area.Height - h) / 2, w, h);

            checkerBrush.ResetTransform();
            checkerBrush.TranslateTransform(dest.X, dest.Y);
            e.Graphics.FillRectangle(checkerBrush, dest);

            e.Graphics.InterpolationMode = scale < 1.0 ? System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic : System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.DrawImage(img, dest);
        }

        #endregion
    }

    internal sealed class DragAwareDataGridView : DataGridView
    {
        public event MouseEventHandler PreviewMouseDown;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            MouseEventHandler handler = PreviewMouseDown;
            if (handler != null) handler(this, e);
            base.OnMouseDown(e);
        }
    }
}
