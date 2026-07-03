using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    public partial class LuaEditor : Form
    {
        private string currentLoadedFile;
        private byte[] currentLoadedRaw;
        private bool currentLoadedWasEncrypted;
        private bool currentLoadedWasCompiled;
        private bool isReloading;

        private System.Windows.Forms.Timer highlightTimer;
        private bool suppressHighlight;
        private const int HighlightMaxSize = 200_000;
        private bool fullHighlightEnabled = true;

        public LuaEditor()
        {
            InitializeComponent();

            highlightTimer = new System.Windows.Forms.Timer { Interval = 350 };
            highlightTimer.Tick += (s, e) => { highlightTimer.Stop(); HighlightCurrentLine(); };

            rtbEditor.TextChanged += (s, e) =>
            {
                gutter.Invalidate();
                if (suppressHighlight) return;
                highlightTimer.Stop();
                highlightTimer.Start();
            };
            rtbEditor.VScroll += (s, e) =>
            {
                gutter.Invalidate();
                if (((CodeRichTextBox)rtbEditor).ShowWhitespace) rtbEditor.Invalidate();
            };
            rtbEditor.Resize += (s, e) => gutter.Invalidate();
            rtbEditor.FontChanged += (s, e) => gutter.Invalidate();
            gutter.Paint += Gutter_Paint;

            btnToggleWhitespace.Click += BtnToggleWhitespace_Click;
            editorToolbar.Resize += (s, e) =>
            {
                btnToggleWhitespace.Left = editorToolbar.ClientSize.Width - btnToggleWhitespace.Width - 8;
                btnToggleWhitespace.Top = 6;
            };
            this.Shown += (s, e) =>
            {
                btnToggleWhitespace.Left = editorToolbar.ClientSize.Width - btnToggleWhitespace.Width - 8;
                btnToggleWhitespace.Top = 6;
                btnToggleWhitespace.BringToFront();
            };

            WireDragDrop();
        }

        private void WireDragDrop()
        {
            Control[] editorTargets = { tabEditor, editorContainer, editorToolbar, rtbEditor, gutter, lblLoaded };
            foreach (var c in editorTargets)
            {
                c.AllowDrop = true;
                c.DragEnter += EditorDragEnter;
                c.DragDrop += EditorDragDrop;
            }

            Control[] batchTargets = { tabBatch, txtBatchPath };
            foreach (var c in batchTargets)
            {
                c.AllowDrop = true;
                c.DragEnter += BatchDragEnter;
                c.DragDrop += BatchDragDrop;
            }
        }

        private static string[] GetDroppedPaths(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            return e.Data.GetData(DataFormats.FileDrop) as string[];
        }

        private void EditorDragEnter(object sender, DragEventArgs e)
        {
            var paths = GetDroppedPaths(e);
            e.Effect = (paths != null && paths.Length == 1 && File.Exists(paths[0]))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void EditorDragDrop(object sender, DragEventArgs e)
        {
            var paths = GetDroppedPaths(e);
            if (paths == null || paths.Length != 1 || !File.Exists(paths[0])) return;
            string path = paths[0];
            Task.Run(() =>
            {
                byte[] raw;
                try { raw = File.ReadAllBytes(path); }
                catch (Exception ex) { Log("ERROR: " + ex.Message); return; }
                currentLoadedFile = path;
                currentLoadedRaw = raw;
                LoadBytesIntoEditor(raw, path, true);
            });
        }

        private void BatchDragEnter(object sender, DragEventArgs e)
        {
            var paths = GetDroppedPaths(e);
            e.Effect = (paths != null && paths.Length == 1 && Directory.Exists(paths[0]))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void BatchDragDrop(object sender, DragEventArgs e)
        {
            var paths = GetDroppedPaths(e);
            if (paths == null || paths.Length != 1 || !Directory.Exists(paths[0])) return;
            txtBatchPath.Text = paths[0];
            Log("Batch folder set to: " + paths[0]);
        }

        private void BtnToggleWhitespace_Click(object sender, EventArgs e)
        {
            var rtb = rtbEditor as CodeRichTextBox;
            if (rtb == null) return;
            rtb.ShowWhitespace = !rtb.ShowWhitespace;
            btnToggleWhitespace.BackColor = rtb.ShowWhitespace
                ? System.Drawing.Color.FromArgb(0, 122, 204)
                : System.Drawing.Color.FromArgb(62, 62, 66);
            rtb.Invalidate();
        }

        private enum LuaVer { LuaP = 0, LuaQ = 1, LuaR = 2 }

        private LuaVer SelectedVersion
        {
            get { return (LuaVer)Math.Max(0, comboLuaVersion.SelectedIndex); }
        }

        private string VersionFolderName
        {
            get
            {
                switch (SelectedVersion)
                {
                    case LuaVer.LuaP: return "LuaP Files";
                    case LuaVer.LuaQ: return "LuaQ Files";
                    default: return "LuaR Files";
                }
            }
        }

        private int BlowfishVersionNumber
        {
            get { return comboEncMethod.SelectedIndex == 1 ? 7 : 2; }
        }

        private byte[] CurrentGameKey
        {
            get
            {
                int idx = comboGame.SelectedIndex;
                if (idx < 0 || idx >= MainMenu.gamelist.Count) return null;
                return MainMenu.gamelist[idx].key;
            }
        }

        private string ToolsVersionDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LencTools", VersionFolderName); }
        }

        private static string ToolsDirForVersion(int verIdx)
        {
            string folder = (verIdx == 0) ? "LuaP Files" : (verIdx == 1 ? "LuaQ Files" : "LuaR Files");
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LencTools", folder);
        }

        private static int? DetectLuaVersionFromBytes(byte[] compiledBytes)
        {
            if (compiledBytes == null || compiledBytes.Length < 5) return null;
            if (!IsCompiledLua(compiledBytes)) return null;
            switch (compiledBytes[4])
            {
                case 0x50: return 0;
                case 0x51: return 1;
                case 0x52: return 2;
                default: return null;
            }
        }

        private void LuaEditor_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < MainMenu.gamelist.Count; i++)
                comboGame.Items.Add(i + ". " + MainMenu.gamelist[i].gamename);

            comboLuaVersion.Items.AddRange(new object[]
            {
                "LuaP (Lua 5.0)",
                "LuaQ (Lua 5.1)",
                "LuaR (Lua 5.2)"
            });

            int savedGame = MainMenu.settings.encKeyIndex;
            if (savedGame < 0 || savedGame >= comboGame.Items.Count) savedGame = 0;
            if (comboGame.Items.Count > 0) comboGame.SelectedIndex = savedGame;

            int savedVer = MainMenu.settings.luaVersionIndex;
            if (savedVer < 0 || savedVer > 2) savedVer = 1;
            comboLuaVersion.SelectedIndex = savedVer;

            chkNewEngine.Checked = MainMenu.settings.encNewLua;

            comboWorkflowMode.Items.AddRange(new object[]
            {
                "Encrypted .lenc  (Telltale classic)",
                "Encrypted .lua",
                "Compiled .lua  (no encryption)",
                "Compiled .lenc  (no encryption)"
            });
            int savedMode = MainMenu.settings.workflowMode;
            if (savedMode < 0 || savedMode > 3) savedMode = 0;
            comboWorkflowMode.SelectedIndex = savedMode;

            comboEncMethod.Items.AddRange(new object[]
            {
                "Versions 2-6  (old engine)",
                "Versions 7-9  (new engine)"
            });
            int savedEnc = MainMenu.settings.versionEnc;
            if (savedEnc < 0 || savedEnc > 1) savedEnc = 0;
            comboEncMethod.SelectedIndex = savedEnc;

            comboLuaVersion.SelectedIndexChanged += comboLuaVersion_SelectedIndexChanged;
            chkNewEngine.CheckedChanged += chkNewEngine_CheckedChanged;
            comboWorkflowMode.SelectedIndexChanged += comboWorkflowMode_SelectedIndexChanged;
            comboEncMethod.SelectedIndexChanged += comboEncMethod_SelectedIndexChanged;
        }

        private void comboEncMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboEncMethod.SelectedIndex < 0) return;
            MainMenu.settings.versionEnc = comboEncMethod.SelectedIndex;
            PersistSettings();
            ReloadCurrentFile();
        }


        private void comboGame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboGame.SelectedIndex < 0) return;
            MainMenu.settings.encKeyIndex = comboGame.SelectedIndex;
            PersistSettings();
            ReloadCurrentFile();
        }

        private void comboLuaVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboLuaVersion.SelectedIndex < 0) return;
            MainMenu.settings.luaVersionIndex = comboLuaVersion.SelectedIndex;
            PersistSettings();
            if (!isReloading) ReloadCurrentFile();
        }

        private void chkNewEngine_CheckedChanged(object sender, EventArgs e)
        {
            MainMenu.settings.encNewLua = chkNewEngine.Checked;
            PersistSettings();
            ReloadCurrentFile();
        }

        private void PersistSettings()
        {
            try { Settings.SaveConfig(MainMenu.settings); }
            catch (Exception ex) { Log("Failed to save config: " + ex.Message); }
        }

        private void LuaEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            PersistSettings();
        }

        private void ReloadCurrentFile()
        {
            if (currentLoadedRaw == null || isReloading) return;
            byte[] raw = currentLoadedRaw;
            string path = currentLoadedFile;
            Task.Run(() => LoadBytesIntoEditor(raw, path, false));
        }

        private void AutoDetectLuaVersion(byte[] compiledBytes)
        {
            if (compiledBytes == null || compiledBytes.Length < 5) return;
            if (!IsCompiledLua(compiledBytes)) return;
            int detected;
            switch (compiledBytes[4])
            {
                case 0x50: detected = 0; break;
                case 0x51: detected = 1; break;
                case 0x52: detected = 2; break;
                default: return;
            }
            if (detected != comboLuaVersion.SelectedIndex)
            {
                BeginInvoke((Action)(() =>
                {
                    comboLuaVersion.SelectedIndex = detected;
                    Log("Auto-detected Lua version: " + comboLuaVersion.Items[detected]);
                }));
            }
        }

        #region Code editor (gutter + syntax highlight)
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        private const int WM_USER = 0x400;
        private const int EM_GETSCROLLPOS = WM_USER + 221;
        private const int EM_SETSCROLLPOS = WM_USER + 222;

        private static readonly string[] LuaKeywords = {
            "and","break","do","else","elseif","end","false","for","function","goto",
            "if","in","local","nil","not","or","repeat","return","then","true","until","while"
        };
        private static readonly string[] LuaBuiltins = {
            "self","_G","_ENV","print","require","pairs","ipairs","tostring","tonumber",
            "type","setmetatable","getmetatable","rawget","rawset","rawequal","next",
            "select","unpack","table","string","math","io","os","coroutine","debug","package"
        };

        private static readonly Color ColorKeyword = Color.FromArgb(86, 156, 214);
        private static readonly Color ColorBuiltin = Color.FromArgb(78, 201, 176);
        private static readonly Color ColorString = Color.FromArgb(206, 145, 120);
        private static readonly Color ColorNumber = Color.FromArgb(181, 206, 168);
        private static readonly Color ColorComment = Color.FromArgb(106, 153, 85);
        private static readonly Color ColorDefault = Color.FromArgb(220, 220, 220);
        private static readonly Color ColorFunctionDef = Color.FromArgb(220, 220, 170);

        private static readonly Regex RxToken = new Regex(
            @"(?<comment>--\[\[[\s\S]*?\]\]|--[^\n]*)" +
            @"|(?<string>""(?:\\.|[^""\\\n])*""|'(?:\\.|[^'\\\n])*'|\[\[[\s\S]*?\]\])" +
            @"|(?<number>\b0[xX][0-9a-fA-F]+\b|\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)" +
            @"|(?<ident>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        private void ApplySyntaxHighlight()
        {
            if (!fullHighlightEnabled) return;
            HighlightRange(0, rtbEditor.TextLength, rtbEditor.Text);
        }

        private void HighlightCurrentLine()
        {
            if (rtbEditor.TextLength == 0) return;
            int caret = rtbEditor.SelectionStart;
            int line = rtbEditor.GetLineFromCharIndex(caret);
            int start = rtbEditor.GetFirstCharIndexFromLine(line);
            if (start < 0) return;
            int totalLines = rtbEditor.GetLineFromCharIndex(rtbEditor.TextLength);
            int end = (line + 1 <= totalLines)
                ? rtbEditor.GetFirstCharIndexFromLine(line + 1)
                : rtbEditor.TextLength;
            int length = end - start;
            if (length <= 0) return;
            string text = rtbEditor.Text;
            if (start + length > text.Length) length = text.Length - start;
            HighlightRange(start, length, text);
        }

        private void HighlightRange(int rangeStart, int rangeLength, string fullText)
        {
            if (rangeLength <= 0) return;

            IntPtr handle = rtbEditor.Handle;
            int savedStart = rtbEditor.SelectionStart;
            int savedLen = rtbEditor.SelectionLength;
            IntPtr scrollPos = Marshal.AllocCoTaskMem(8);
            SendMessage(handle, EM_GETSCROLLPOS, IntPtr.Zero, scrollPos);
            SendMessage(handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            suppressHighlight = true;
            try
            {
                string segment = fullText.Substring(rangeStart, rangeLength);
                rtbEditor.Select(rangeStart, rangeLength);
                rtbEditor.SelectionColor = ColorDefault;

                foreach (Match m in RxToken.Matches(segment))
                {
                    Color? c = null;
                    if (m.Groups["comment"].Success) c = ColorComment;
                    else if (m.Groups["string"].Success) c = ColorString;
                    else if (m.Groups["number"].Success) c = ColorNumber;
                    else if (m.Groups["ident"].Success)
                    {
                        string id = m.Value;
                        if (Array.IndexOf(LuaKeywords, id) >= 0) c = ColorKeyword;
                        else if (Array.IndexOf(LuaBuiltins, id) >= 0) c = ColorBuiltin;
                        else
                        {
                            int prev = m.Index - 1;
                            while (prev >= 0 && char.IsWhiteSpace(segment[prev])) prev--;
                            if (prev >= 7 && segment.Substring(prev - 7, 8) == "function") c = ColorFunctionDef;
                        }
                    }
                    if (c.HasValue)
                    {
                        rtbEditor.Select(rangeStart + m.Index, m.Length);
                        rtbEditor.SelectionColor = c.Value;
                    }
                }

                rtbEditor.Select(savedStart, savedLen);
            }
            finally
            {
                suppressHighlight = false;
                SendMessage(handle, EM_SETSCROLLPOS, IntPtr.Zero, scrollPos);
                Marshal.FreeCoTaskMem(scrollPos);
                SendMessage(handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                rtbEditor.Invalidate();
                gutter.Invalidate();
            }
        }

        private void Gutter_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(gutter.BackColor);
            using (var pen = new Pen(Color.FromArgb(60, 60, 60)))
                g.DrawLine(pen, gutter.Width - 1, 0, gutter.Width - 1, gutter.Height);

            if (rtbEditor.TextLength == 0)
            {
                using (var brushEmpty = new SolidBrush(Color.FromArgb(110, 110, 110)))
                    g.DrawString("1", rtbEditor.Font, brushEmpty, gutter.Width - 18, 2);
                return;
            }

            int firstChar = rtbEditor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = rtbEditor.GetLineFromCharIndex(firstChar);
            int lastChar = rtbEditor.GetCharIndexFromPosition(new Point(0, gutter.Height - 1));
            int lastLine = rtbEditor.GetLineFromCharIndex(lastChar) + 1;
            int totalLines = rtbEditor.GetLineFromCharIndex(rtbEditor.TextLength);
            if (lastLine > totalLines) lastLine = totalLines;

            int currentLine = rtbEditor.GetLineFromCharIndex(rtbEditor.SelectionStart);
            int fontH = rtbEditor.Font.Height;

            using (var brush = new SolidBrush(Color.FromArgb(133, 133, 133)))
            using (var currBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Far })
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    int charIdx = rtbEditor.GetFirstCharIndexFromLine(i);
                    if (charIdx < 0) continue;
                    Point p = rtbEditor.GetPositionFromCharIndex(charIdx);
                    if (p.Y > gutter.Height) break;
                    string num = (i + 1).ToString();
                    var rect = new RectangleF(0, p.Y, gutter.Width - 6, fontH + 2);
                    g.DrawString(num, rtbEditor.Font, i == currentLine ? currBrush : brush, rect, sf);
                }
            }
        }
        #endregion

        #region Logging
        private void Log(string msg)
        {
            if (txtLog.InvokeRequired) { txtLog.BeginInvoke((Action)(() => Log(msg))); return; }
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine);
        }

        private void SetBusy(bool busy)
        {
            if (InvokeRequired) { BeginInvoke((Action)(() => SetBusy(busy))); return; }
            progressBar.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            tabs.Enabled = !busy;
            panelTop.Enabled = !busy;
        }
        #endregion

        #region Core operations
        private byte[] DecryptBytes(byte[] data)
        {
            byte[] key = CurrentGameKey;
            if (key == null) throw new InvalidOperationException("Select a game first.");
            byte[] copy = (byte[])data.Clone();
            return Methods.decryptLua(copy, key, BlowfishVersionNumber);
        }

        private byte[] EncryptBytes(byte[] data)
        {
            byte[] key = CurrentGameKey;
            if (key == null) throw new InvalidOperationException("Select a game first.");
            byte[] copy = (byte[])data.Clone();
            return Methods.encryptLua(copy, key, chkNewEngine.Checked, BlowfishVersionNumber);
        }

        private static bool IsCompiledLua(byte[] data)
        {
            if (data == null || data.Length < 4) return false;
            return data[0] == 0x1B && data[1] == 0x4C && data[2] == 0x75 && data[3] == 0x61;
        }

        private string RunProcess(string exe, string args, string workingDir, bool captureStdout, out int exitCode, out string stdout)
        {
            stdout = null;
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir ?? Environment.CurrentDirectory
            };
            if (captureStdout) psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var p = Process.Start(psi))
            {
                var sbOut = new StringBuilder();
                var sbErr = new StringBuilder();
                p.OutputDataReceived += (s, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                exitCode = p.ExitCode;
                if (captureStdout) stdout = sbOut.ToString();
                return sbErr.ToString();
            }
        }

        private byte[] DecompileToLuaSource(byte[] compiledBytes)
        {
            return DecompileToLuaSource(compiledBytes, (int)SelectedVersion);
        }

        private byte[] DecompileToLuaSource(byte[] compiledBytes, int versionIndex)
        {
            string dir = ToolsDirForVersion(versionIndex);
            string jar = Path.Combine(dir, "unluac.jar");
            if (!File.Exists(jar)) throw new FileNotFoundException("unluac.jar not found in " + dir);

            string tempDir = Path.Combine(Path.GetTempPath(), "TTGLuaEditor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string inFile = Path.Combine(tempDir, "in.luac");
                File.WriteAllBytes(inFile, compiledBytes);

                int code;
                string stdout;
                string err = RunProcess("java", "-jar \"" + jar + "\" \"" + inFile + "\"", tempDir, true, out code, out stdout);
                if (code != 0) throw new Exception("unluac failed (exit " + code + "): " + err);
                return Encoding.UTF8.GetBytes(stdout ?? string.Empty);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private byte[] CompileLuaSource(byte[] sourceBytes)
        {
            string dir = ToolsVersionDir;
            string luac = Path.Combine(dir, "luac.exe");
            if (!File.Exists(luac)) throw new FileNotFoundException("luac.exe not found in " + dir);

            string tempDir = Path.Combine(Path.GetTempPath(), "TTGLuaEditor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string inFile = Path.Combine(tempDir, "src.lua");
                File.WriteAllBytes(inFile, sourceBytes);

                foreach (string f in Directory.GetFiles(dir, "*.exe").Concat(Directory.GetFiles(dir, "*.dll")))
                    File.Copy(f, Path.Combine(tempDir, Path.GetFileName(f)), true);

                int code;
                string stdout;
                string err = RunProcess(Path.Combine(tempDir, "luac.exe"), "\"" + inFile + "\"", tempDir, false, out code, out stdout);
                if (code != 0) throw new Exception("luac failed (exit " + code + "): " + err);

                string outFile = Path.Combine(tempDir, "luac.out");
                if (!File.Exists(outFile)) throw new FileNotFoundException("luac.out was not produced. stderr: " + err);
                return File.ReadAllBytes(outFile);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        #endregion

        #region Editor tab
        private void btnLoadEdit_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Lua related|*.lenc;*.luac;*.lua;*.*|All files|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                string path = ofd.FileName;
                Task.Run(() =>
                {
                    byte[] raw;
                    try { raw = File.ReadAllBytes(path); }
                    catch (Exception ex) { Log("ERROR: " + ex.Message); return; }
                    currentLoadedFile = path;
                    currentLoadedRaw = raw;
                    LoadBytesIntoEditor(raw, path, true);
                });
            }
        }

        private void LoadBytesIntoEditor(byte[] raw, string path, bool autoDetect)
        {
            try
            {
                isReloading = !autoDetect;
                SetBusy(true);
                Log((autoDetect ? "Loading " : "Reloading ") + path);

                byte[] working = (byte[])raw.Clone();
                bool wasEncrypted = false;
                bool wasCompiled = false;
                bool isText = LooksLikePlainText(working);

                if (!isText && Methods.isLuaEncrypted(working))
                {
                    Log("Encrypted Lua detected. Decrypting...");
                    working = DecryptBytes(working);
                    wasEncrypted = true;
                }

                string source;
                if (IsCompiledLua(working))
                {
                    if (autoDetect) AutoDetectLuaVersion(working);
                    Log("Compiled Lua detected. Decompiling...");
                    byte[] src = DecompileToLuaSource(working);
                    source = Encoding.UTF8.GetString(src);
                    wasCompiled = true;
                }
                else
                {
                    Log("Plain Lua source.");
                    source = Encoding.UTF8.GetString(working);
                }

                currentLoadedWasEncrypted = wasEncrypted;
                currentLoadedWasCompiled = wasCompiled;

                BeginInvoke((Action)(() =>
                {
                    suppressHighlight = true;
                    rtbEditor.Text = source;
                    ((CodeRichTextBox)rtbEditor).ApplyTabStops();
                    suppressHighlight = false;
                    fullHighlightEnabled = source.Length <= HighlightMaxSize;
                    if (fullHighlightEnabled)
                    {
                        ApplySyntaxHighlight();
                    }
                    else
                    {
                        Log("Large file (" + (source.Length / 1024) + " KB) - syntax highlight limited to the line being edited for performance.");
                    }
                    lblLoaded.Text = Path.GetFileName(path) +
                        "  (" + (wasEncrypted ? "encrypted" : "plain") +
                        " / " + (wasCompiled ? "compiled" : "source") + ")";
                }));
                Log("Loaded.");
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
            finally { SetBusy(false); isReloading = false; }
        }

        private void btnSaveRepack_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentLoadedFile)) { MessageBox.Show("Load a file first."); return; }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Same as original|*.*|Encrypted .lenc|*.lenc|Source .lua|*.lua";
                sfd.FileName = Path.GetFileNameWithoutExtension(currentLoadedFile) + Path.GetExtension(currentLoadedFile);
                sfd.InitialDirectory = Path.GetDirectoryName(currentLoadedFile);
                if (sfd.ShowDialog() != DialogResult.OK) return;

                string outPath = sfd.FileName;
                int filterIdx = sfd.FilterIndex;
                string source = rtbEditor.Text;

                Task.Run(() =>
                {
                    try
                    {
                        SetBusy(true);
                        bool produceCompiled, produceEncrypted;
                        switch (filterIdx)
                        {
                            case 1: produceCompiled = currentLoadedWasCompiled; produceEncrypted = currentLoadedWasEncrypted; break;
                            case 2: produceCompiled = true; produceEncrypted = true; break;
                            default: produceCompiled = false; produceEncrypted = false; break;
                        }

                        byte[] data = Encoding.UTF8.GetBytes(source);
                        if (produceCompiled) { Log("Compiling..."); data = CompileLuaSource(data); }
                        if (produceEncrypted) { Log("Encrypting..."); data = EncryptBytes(data); }
                        File.WriteAllBytes(outPath, data);
                        Log("Saved: " + outPath);
                    }
                    catch (Exception ex) { Log("ERROR: " + ex.Message); }
                    finally { SetBusy(false); }
                });
            }
        }
        #endregion

        #region Single-file tab
        private string PickInput(string filter)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = filter;
                return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
            }
        }

        private string PickOutput(string filter, string suggested)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = filter;
                sfd.FileName = suggested;
                return sfd.ShowDialog() == DialogResult.OK ? sfd.FileName : null;
            }
        }

        private void btnSingleExtract_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Game file|*.lenc;*.lua;*.*"); if (inp == null) return;
            string ext = Path.GetExtension(inp);
            string outp = PickOutput("Editable text|*" + ext, Path.GetFileNameWithoutExtension(inp) + ext);
            if (outp == null) return;
            int mode = comboWorkflowMode.SelectedIndex;
            Task.Run(() =>
            {
                try
                {
                    SetBusy(true);
                    byte[] raw = File.ReadAllBytes(inp);
                    byte[] data;
                    switch (mode)
                    {
                        case 0:
                        case 1: data = DecryptAndDecompile(raw); break;
                        default: data = AutoDecompile(raw); break;
                    }
                    File.WriteAllBytes(outp, data);
                    Log("Extract -> " + outp);
                }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void btnSingleRepack_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Editable text|*.lua;*.lenc;*.*"); if (inp == null) return;
            string ext = Path.GetExtension(inp);
            string outp = PickOutput("Game file|*" + ext, Path.GetFileNameWithoutExtension(inp) + ext);
            if (outp == null) return;
            int mode = comboWorkflowMode.SelectedIndex;
            Task.Run(() =>
            {
                try
                {
                    SetBusy(true);
                    byte[] raw = File.ReadAllBytes(inp);
                    byte[] compiled = CompileLuaSource(raw);
                    byte[] data = (mode == 0 || mode == 1) ? EncryptBytes(compiled) : compiled;
                    File.WriteAllBytes(outp, data);
                    Log("Repack -> " + outp);
                }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Encrypted Lua|*.lenc;*.*"); if (inp == null) return;
            string outp = PickOutput("Lua (compiled binary)|*.lua", Path.GetFileNameWithoutExtension(inp) + ".lua"); if (outp == null) return;
            Task.Run(() =>
            {
                try { SetBusy(true); File.WriteAllBytes(outp, DecryptBytes(File.ReadAllBytes(inp))); Log("Decrypted -> " + outp); }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Lua (compiled binary)|*.lua;*.luac;*.*"); if (inp == null) return;
            string outp = PickOutput("Encrypted Lua|*.lenc", Path.GetFileNameWithoutExtension(inp) + ".lenc"); if (outp == null) return;
            Task.Run(() =>
            {
                try { SetBusy(true); File.WriteAllBytes(outp, EncryptBytes(File.ReadAllBytes(inp))); Log("Encrypted -> " + outp); }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void btnDecompile_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Lua (compiled binary)|*.lua;*.luac;*.*"); if (inp == null) return;
            string outp = PickOutput("Lua source|*.lua", Path.GetFileNameWithoutExtension(inp) + ".lua"); if (outp == null) return;
            Task.Run(() =>
            {
                try { SetBusy(true); File.WriteAllBytes(outp, AutoDecompile(File.ReadAllBytes(inp))); Log("Decompiled -> " + outp); }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            string inp = PickInput("Lua source|*.lua;*.*"); if (inp == null) return;
            string outp = PickOutput("Lua (compiled binary)|*.lua", Path.GetFileNameWithoutExtension(inp) + ".lua"); if (outp == null) return;
            Task.Run(() =>
            {
                try { SetBusy(true); File.WriteAllBytes(outp, CompileLuaSource(File.ReadAllBytes(inp))); Log("Compiled -> " + outp); }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }
        #endregion

        #region Batch tab
        private void btnBrowseBatch_Click(object sender, EventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true,
                Title = "Select folder"
            };
            if (Directory.Exists(txtBatchPath.Text)) dlg.InitialDirectory = txtBatchPath.Text;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok) txtBatchPath.Text = dlg.FileName;
        }

        private string[] EnumFiles(string dir, string pattern)
        {
            return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
        }

        private static bool LooksLikePlainText(byte[] data)
        {
            if (data == null || data.Length < 4) return true;
            if (data[0] == 0x1B) return false; // \x1bLua / \x1bLEn / \x1bLEo
            int n = Math.Min(512, data.Length);
            int asciiText = 0;
            for (int i = 0; i < n; i++)
            {
                byte b = data[i];
                if (b == 0) return false; // null byte → binary
                if (b == 0x09 || b == 0x0A || b == 0x0D || (b >= 0x20 && b <= 0x7E))
                    asciiText++;
            }
            return asciiText >= n * 0.95;
        }

        private void RunBatch(string pattern, string outExt, Func<byte[], byte[]> op, string opName,
            bool skipText = false, bool skipBinary = false)
        {
            string dir = txtBatchPath.Text;
            if (!Directory.Exists(dir)) { MessageBox.Show("Pick a valid folder."); return; }
            Task.Run(() =>
            {
                try
                {
                    SetBusy(true);
                    batchLastDetectedVersion = null;
                    var files = EnumFiles(dir, pattern);
                    Log(opName + ": " + files.Length + " files found.");
                    int ok = 0, fail = 0, skip = 0;
                    foreach (var f in files)
                    {
                        try
                        {
                            byte[] raw = File.ReadAllBytes(f);
                            bool isText = LooksLikePlainText(raw);
                            if (skipText && isText) { skip++; continue; }
                            if (skipBinary && !isText) { skip++; continue; }

                            byte[] data = op(raw);
                            string outPath = Path.Combine(Path.GetDirectoryName(f),
                                Path.GetFileNameWithoutExtension(f) + outExt);
                            File.WriteAllBytes(outPath, data);
                            ok++;
                        }
                        catch (Exception ex) { Log("  FAIL " + Path.GetFileName(f) + ": " + ex.Message); fail++; }
                    }
                    Log(opName + " done. OK=" + ok + " FAIL=" + fail + (skip > 0 ? " SKIP=" + skip : ""));

                    int? detected = batchLastDetectedVersion;
                    if (detected.HasValue && detected.Value != comboLuaVersion.SelectedIndex)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            comboLuaVersion.SelectedIndex = detected.Value;
                            Log("Lua version updated to " + comboLuaVersion.Items[detected.Value] + " based on batch detection.");
                        }));
                    }
                }
                catch (Exception ex) { Log("ERROR: " + ex.Message); }
                finally { SetBusy(false); }
            });
        }

        private int? batchLastDetectedVersion;

        private byte[] DecryptAndDecompile(byte[] raw)
        {
            // Step 1: decrypt if needed.
            byte[] decrypted = (!LooksLikePlainText(raw) && Methods.isLuaEncrypted(raw)) ? DecryptBytes(raw) : raw;

            // Step 2: if the decrypted payload is already plain Lua source
            // (some games ship .lua/.lenc that are ONLY encrypted, not
            // compiled), skip the decompile step - unluac would just bail
            // with "not a valid Lua file".
            if (LooksLikePlainText(decrypted)) return decrypted;

            // Step 3: compiled Lua → decompile with the detected/picked version.
            int? detected = DetectLuaVersionFromBytes(decrypted);
            if (detected.HasValue) batchLastDetectedVersion = detected;
            int verIdx = detected ?? (int)SelectedVersion;
            return DecompileToLuaSource(decrypted, verIdx);
        }

        private byte[] AutoDecompile(byte[] raw)
        {
            // Already plain text? Nothing to decompile.
            if (LooksLikePlainText(raw)) return raw;

            int? detected = DetectLuaVersionFromBytes(raw);
            if (detected.HasValue) batchLastDetectedVersion = detected;
            int verIdx = detected ?? (int)SelectedVersion;
            return DecompileToLuaSource(raw, verIdx);
        }

        private void btnBatchDecrypt_Click(object sender, EventArgs e) { RunBatch("*.lenc", ".lua", DecryptBytes, "Batch decrypt"); }
        private void btnBatchEncrypt_Click(object sender, EventArgs e) { RunBatch("*.lua", ".lenc", EncryptBytes, "Batch encrypt", skipText: true); }
        private void btnBatchDecompile_Click(object sender, EventArgs e) { RunBatch("*.lua", ".lua", AutoDecompile, "Batch decompile", skipText: true); }
        private void btnBatchCompile_Click(object sender, EventArgs e) { RunBatch("*.lua", ".lua", CompileLuaSource, "Batch compile", skipBinary: true); }

        // Workflow modes for SMART EXTRACT / SMART REPACK
        //   0 = .lenc encrypted (Telltale classic)
        //   1 = .lua  encrypted
        //   2 = .lua  plain compiled
        //   3 = .lenc plain compiled
        private void btnSmartExtract_Click(object sender, EventArgs e)
        {
            switch (comboWorkflowMode.SelectedIndex)
            {
                case 0: RunBatch("*.lenc", ".lenc", DecryptAndDecompile, "Extract (.lenc encrypted)", skipText: true); break;
                case 1: RunBatch("*.lua",  ".lua",  DecryptAndDecompile, "Extract (.lua encrypted)",  skipText: true); break;
                case 2: RunBatch("*.lua",  ".lua",  AutoDecompile,       "Extract (.lua compiled)",   skipText: true); break;
                case 3: RunBatch("*.lenc", ".lenc", AutoDecompile,       "Extract (.lenc compiled)",  skipText: true); break;
            }
        }

        private void btnSmartRepack_Click(object sender, EventArgs e)
        {
            switch (comboWorkflowMode.SelectedIndex)
            {
                case 0: RunBatch("*.lenc", ".lenc", b => EncryptBytes(CompileLuaSource(b)), "Repack (.lenc encrypted)", skipBinary: true); break;
                case 1: RunBatch("*.lua",  ".lua",  b => EncryptBytes(CompileLuaSource(b)), "Repack (.lua encrypted)",  skipBinary: true); break;
                case 2: RunBatch("*.lua",  ".lua",  CompileLuaSource,                       "Repack (.lua compiled)",   skipBinary: true); break;
                case 3: RunBatch("*.lenc", ".lenc", CompileLuaSource,                       "Repack (.lenc compiled)",  skipBinary: true); break;
            }
        }

        private void comboWorkflowMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboWorkflowMode.SelectedIndex < 0) return;
            MainMenu.settings.workflowMode = comboWorkflowMode.SelectedIndex;
            PersistSettings();
        }

        #endregion
    }
}
