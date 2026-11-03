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
            Task opening = (Task)open.Invoke(form, new object[] { archivePath });
            Pump(() => opening.IsCompleted, 180000);

            DataGridView grid = (DataGridView)GetField(form, "filesDataGridView");
            Label info = (Label)GetField(form, "previewInfoLabel");

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
            }

            File.WriteAllLines(Path.Combine(outDir, "log.txt"), log.ToArray());
            form.Close();
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
