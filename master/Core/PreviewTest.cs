using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTG_Tools
{
    // Headless smoke test for the Archive Unpacker preview panel:
    //   "TTG Tools.exe" --preview-test <archive> <outputDir> [ext1,ext2,...]
    // Opens the archive in a real ArchiveUnpacker window, selects the first file of each requested
    // extension, waits for the preview to build, and saves PNG screenshots of the form plus a
    // log.txt. Lets the preview pipeline be validated end to end without manual clicking, in the
    // same spirit as the --extract-ttarch / --pack-ttarch CLI hooks.
    internal static class PreviewTest
    {
        //Direct PreviewBuilder run on loose files (no archive):
        //  "TTG Tools.exe" --preview-file <fileOrFolder> <outputDir>
        //Saves each preview as PNG/TXT next to a log line describing the result kind.
        public static int RunFiles(string[] args)
        {
            string input = args[1];
            string outDir = args[2];

            Directory.CreateDirectory(outDir);
            InitEnvironment();

            var files = new List<string>();
            if (Directory.Exists(input)) files.AddRange(Directory.GetFiles(input, "*.*", SearchOption.AllDirectories));
            else files.Add(input);

            byte[] key = MainMenu.gamelist[MainMenu.settings.encKeyIndex].key;
            var log = new List<string>();

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);

                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    var res = PreviewBuilder.Build(name, data, key, 7);

                    string line = name + " -> " + (res == null ? "null" : res.Kind.ToString());

                    if (res != null && res.Image != null)
                    {
                        string png = Path.Combine(outDir, name + ".png");
                        res.Image.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                        line += " (" + res.Image.Width + "x" + res.Image.Height + ")";
                        res.Image.Dispose();
                    }
                    else if (res != null && res.Audio != null)
                    {
                        using (var w = new NAudio.Wave.WaveFileWriter(Path.Combine(outDir, name + ".wav"), new NAudio.Wave.WaveFormat(res.Audio.SampleRate, 16, res.Audio.Channels)))
                            w.Write(res.Audio.Pcm, 0, res.Audio.Pcm.Length);
                        line += " (" + res.Audio.Pcm.Length + " PCM bytes)";
                    }
                    else if (res != null && res.Text != null)
                    {
                        File.WriteAllText(Path.Combine(outDir, name + ".txt"), res.Text);
                        line += " (" + res.Text.Length + " chars)";
                    }

                    if (res != null && !string.IsNullOrEmpty(res.Info)) line += " [" + res.Info + "]";
                    log.Add(line);
                }
                catch (Exception ex)
                {
                    log.Add(name + " -> EXCEPTION " + ex.Message);
                }
            }

            File.WriteAllLines(Path.Combine(outDir, "log.txt"), log.ToArray());
            return 0;
        }

        private static void InitEnvironment()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Headless runs must not stop on the "unhandled exception, Continue?" dialog.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "ttg_test_exception.txt"),
                    "thread " + Thread.CurrentThread.ManagedThreadId + " " + Thread.CurrentThread.GetApartmentState() + Environment.NewLine + e.Exception + Environment.NewLine);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            };

            Settings cfg = Settings.LoadConfigOrNull();
            if (cfg != null) MainMenu.settings = cfg;
            Localization.Init(cfg != null ? cfg.interfaceLanguage : null);

            MainMenu menu = new MainMenu();
            MethodInfo load = typeof(MainMenu).GetMethod("MainMenu_Load", BindingFlags.Instance | BindingFlags.NonPublic);
            load.Invoke(menu, new object[] { menu, EventArgs.Empty });

            if (MainMenu.settings.encKeyIndex < 0 || MainMenu.settings.encKeyIndex >= MainMenu.gamelist.Count)
            {
                MainMenu.settings.encKeyIndex = 0;
            }
        }

        public static int Run(string[] args)
        {
            string archivePath = args[1];
            string outDir = args[2];
            string[] exts = args.Length >= 4 ? args[3].Split(',') : new string[0];

            Directory.CreateDirectory(outDir);

            //The encryption key list is populated inside MainMenu_Load, so InitEnvironment fires
            //it on a hidden instance instead of duplicating a few hundred keys here.
            InitEnvironment();

            var log = new List<string>();

            ArchiveUnpacker form = new ArchiveUnpacker();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Show();

            MethodInfo open = typeof(ArchiveUnpacker).GetMethod("OpenArchiveFile", BindingFlags.Instance | BindingFlags.NonPublic);
            DataGridView grid = (DataGridView)GetField(form, "filesDataGridView");
            Label info = (Label)GetField(form, "previewInfoLabel");

            //"a.ttarch|b.ttarch": opens them one after another in the same window, like dropping a
            //second archive on top of the first, and logs how long each switch took.
            string[] sequence = archivePath.Split('|');
            for (int a = 0; a < sequence.Length - 1; a++)
            {
                var swPrev = System.Diagnostics.Stopwatch.StartNew();
                Task prev = (Task)open.Invoke(form, new object[] { sequence[a] });
                Pump(() => prev.IsCompleted, 180000);
                log.Add("Opened first: " + sequence[a] + " -> " + form.Text + " | rows " + grid.RowCount + " | " + swPrev.ElapsedMilliseconds + " ms");

                //Select a row so a preview is in flight when the next archive arrives.
                if (grid.RowCount > 0) { grid.CurrentCell = grid[1, 0]; grid.Rows[0].Selected = true; }
                Pump(() => false, 300);
            }
            archivePath = sequence[sequence.Length - 1];

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Task opening = (Task)open.Invoke(form, new object[] { archivePath });
            Pump(() => opening.IsCompleted, 180000);
            log.Add("Open time: " + sw.ElapsedMilliseconds + " ms (completed=" + opening.IsCompleted + ")");

            log.Add("Archive: " + archivePath);
            log.Add("Title:   " + form.Text);
            log.Add("Rows:    " + grid.RowCount);

            Capture(form, Path.Combine(outDir, "00_opened.png"));

            string loadingText = Loc.T("ArchiveUnpacker.previewLoading", "Loading preview...");
            int shot = 1;

            foreach (string extRaw in exts)
            {
                string ext = extRaw.Trim().ToLower();
                if (ext.Length == 0) continue;

                int row = FindRow(grid, ext);

                if (row < 0)
                {
                    log.Add(ext + ": NOT FOUND in archive");
                    continue;
                }

                grid.ClearSelection();
                Application.DoEvents();
                grid.CurrentCell = grid[1, row];
                grid.Rows[row].Selected = true;
                if (row < grid.RowCount) grid.FirstDisplayedScrollingRowIndex = row;

                //UpdatePreview sets the loading text synchronously during the selection event, so
                //from here we only need to pump until the background build replaces it.
                Pump(() => info.Text != loadingText, 90000);
                Pump(() => false, 500); //settle for the final paint

                string name = Convert.ToString(grid[1, row].Value);
                log.Add(ext + ": row " + (row + 1) + " (" + name + ") -> " + info.Text);
                Capture(form, Path.Combine(outDir, string.Format("{0:D2}_{1}.png", shot++, ext.TrimStart('.'))));

                //Audio previews: press play and capture the player mid-playback.
                var player = (AudioPlayerPanel)GetField(form, "previewAudio");
                if (player != null && player.Visible)
                {
                    player.TogglePlay();
                    Pump(() => false, 1500);
                    Capture(form, Path.Combine(outDir, string.Format("{0:D2}_{1}_playing.png", shot++, ext.TrimStart('.'))));
                    player.Stop();
                }

                //Multi-page previews (Wii): step through the remaining pages with the ▶ button.
                Button next = (Button)GetField(form, "previewNextButton");
                for (int page = 2; next.Visible && next.Enabled && page <= 8; page++)
                {
                    next.PerformClick();
                    Pump(() => false, 300);
                    log.Add("    page " + page + " -> " + info.Text);
                    Capture(form, Path.Combine(outDir, string.Format("{0:D2}_{1}_page{2}.png", shot++, ext.TrimStart('.'), page)));
                }
            }

            File.WriteAllLines(Path.Combine(outDir, "log.txt"), log.ToArray());
            form.Close();
            return 0;
        }

        //Previews EVERY file of one extension inside an archive (no screenshots, just the
        //decoded bitmaps), to sweep a whole game dump for failing textures:
        //  "TTG Tools.exe" --preview-all <archive> <outputDir> [ext] [save 0|1|2=also raw files]
        public static int RunAll(string[] args)
        {
            string archivePath = args[1];
            string outDir = args[2];
            string ext = args.Length >= 4 ? args[3] : ".d3dtx";
            bool save = args.Length < 5 || args[4] != "0";
            bool dumpRaw = args.Length >= 5 && args[4] == "2";
            int limit = args.Length >= 6 ? int.Parse(args[5]) : 0;

            Directory.CreateDirectory(outDir);
            InitEnvironment();

            ArchiveUnpacker form = new ArchiveUnpacker();
            form.Show();

            MethodInfo open = typeof(ArchiveUnpacker).GetMethod("OpenArchiveFile", BindingFlags.Instance | BindingFlags.NonPublic);
            Task opening = (Task)open.Invoke(form, new object[] { archivePath });
            Pump(() => opening.IsCompleted, 180000);

            var log = new List<string>();
            int ok = 0, fail = 0;
            log.Add("Archive: " + archivePath + " | " + form.Text);

            form.BuildAllPreviewsForTest(ext, (name, bytes, res) =>
            {
                string line = name + " -> ";
                if (dumpRaw && bytes != null) File.WriteAllBytes(Path.Combine(outDir, Path.GetFileName(name.Replace('/', '\\'))), bytes);

                if (res == null || res.Kind == PreviewBuilder.PreviewKind.None)
                {
                    fail++;
                    line += "NONE" + (res != null && res.Info != null ? " " + res.Info : "");
                }
                else
                {
                    ok++;
                    line += res.Kind + (res.Info != null ? " [" + res.Info + "]" : "");
                    if (res.Pages != null) line += " pages=" + res.Pages.Count;

                    if (res.Image != null)
                    {
                        if (save) res.Image.Save(Path.Combine(outDir, Path.GetFileName(name.Replace('/', '\\')) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        if (save && res.Pages != null)
                            for (int p = 1; p < res.Pages.Count; p++)
                                res.Pages[p].Save(Path.Combine(outDir, Path.GetFileName(name.Replace('/', '\\')) + "_page" + (p + 1) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        res.Image.Dispose();
                    }
                }

                log.Add(line);
            }, limit);

            log.Add("OK " + ok + " / FAIL " + fail);
            File.WriteAllLines(Path.Combine(outDir, "log.txt"), log.ToArray());
            Console.WriteLine("OK " + ok + " / FAIL " + fail);
            form.Close();
            return 0;
        }

        //"TTG Tools.exe" --formats <archive> <outDir>: extension counts + one raw sample of each.
        public static int RunFormats(string[] args)
        {
            Directory.CreateDirectory(args[2]);
            InitEnvironment();

            ArchiveUnpacker form = new ArchiveUnpacker();
            form.Show();
            MethodInfo open = typeof(ArchiveUnpacker).GetMethod("OpenArchiveFile", BindingFlags.Instance | BindingFlags.NonPublic);
            Task opening = (Task)open.Invoke(form, new object[] { args[1] });
            Pump(() => opening.IsCompleted, 180000);

            var log = new List<string>();
            foreach (var kv in form.SampleFormatsForTest())
            {
                log.Add(kv.Key + " " + kv.Value.Key);
                if (kv.Value.Value != null) File.WriteAllBytes(Path.Combine(args[2], "sample" + kv.Key), kv.Value.Value);
            }

            File.WriteAllLines(Path.Combine(args[2], "formats.txt"), log.ToArray());
            form.Close();
            return 0;
        }

                //--audio-ui-test <audioFile> <png>: the player alone in a window, captured after a short play.
        public static int RunAudioUi(string[] args)
        {
            InitEnvironment();
            byte[] data = File.ReadAllBytes(args[1]);
            string err;
            var audio = AudioDecoder.Decode(data, Path.GetExtension(args[1]).ToLower(), null, out err);
            var f = new Form { Width = 520, Height = 360, StartPosition = FormStartPosition.Manual, Location = new Point(0, 0), TopMost = true };
            var p = new AudioPlayerPanel { Dock = DockStyle.Fill };
            f.Controls.Add(p);
            f.Show();
            p.LoadAudio(audio.Pcm, audio.SampleRate, audio.Channels);
            p.TogglePlay();
            Pump(() => false, 1500);
            Capture(f, args[2]);
            p.Stop();
            f.Close();
            return 0;
        }

                public static int RunOpenAll(string[] args)
        {
            InitEnvironment();
            ArchiveUnpacker.OpenAllCli(args[1], args[2]);
            return 0;
        }


        private static object GetField(object obj, string name)
        {
            FieldInfo fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return fi.GetValue(obj);
        }

        //Picks the LARGEST file of the extension: stub files of a few bytes make a poor preview
        //smoke test, while the biggest one exercises the real decoder.
        private static int FindRow(DataGridView grid, string ext)
        {
            //A full file name ("font.font") selects that exact file instead of the largest one.
            if (!ext.StartsWith("."))
            {
                for (int i = 0; i < grid.RowCount; i++)
                    if (string.Equals(Convert.ToString(grid[1, i].Value), ext, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }

            int best = -1;
            long bestSize = -1;

            for (int i = 0; i < grid.RowCount; i++)
            {
                if (Convert.ToString(grid[2, i].Value) != ext) continue;

                long size;
                long.TryParse(Convert.ToString(grid[3, i].Value), out size);

                if (size > bestSize)
                {
                    bestSize = size;
                    best = i;
                }
            }

            return best;
        }

        private static void Pump(Func<bool> done, int timeoutMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Application.DoEvents();
                if (done()) return;
                Thread.Sleep(50);
            }
        }

        //Real screen pixels via CopyFromScreen: DrawToBitmap (WM_PRINT) renders native TextBox
        //content blank, which would make the text previews look broken in the captures.
        private static void Capture(Form form, string path)
        {
            form.TopMost = true;
            form.BringToFront();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();

            Rectangle b = form.Bounds;

            using (Bitmap bmp = new Bitmap(b.Width, b.Height))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(b.Location, Point.Empty, b.Size);
                }

                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
