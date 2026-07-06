using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TTG_Tools
{
    static class Program
    {
        public static bool FirstTime = true;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "--extract-ttarch")
            {
                // --extract-ttarch <archive> <outputDir>
                string archive = args[1];
                string outDir = args[2];
                byte[] key = { 0x96, 0xCA, 0x99, 0xA0, 0x85, 0xCF, 0x98, 0x8A, 0xE4, 0xDB, 0xE2, 0xCD, 0xA6, 0x96, 0x83, 0x88, 0xC0, 0x8B, 0x99, 0xE3, 0x9E, 0xD8, 0x9B, 0xB6, 0xD7, 0x90, 0xDC, 0xBE, 0xAD, 0x9D, 0x91, 0x65, 0xB6, 0xA6, 0x9E, 0xBB, 0xC2, 0xC6, 0x9E, 0xB3, 0xE7, 0xE3, 0xE5, 0xD5, 0xAB, 0x63, 0x82, 0xA0, 0x9C, 0xC4, 0x92, 0x9F, 0xD1, 0xD5, 0xA4 };
                Settings.EnsureConfigAvailable();
                ArchiveUnpacker.ExtractTtarchCli(archive, outDir, key);
                return 0;
            }
            if (args.Length >= 8 && args[0] == "--pack-ttarch")
            {
                // --pack-ttarch <inputDir> <outputFile> <gameIndex> <version> <compress 0|1> <encrypt 0|1> <dontEncLua 0|1> [<compressAlgo 0|1>]
                string inputDir = args[1];
                string outputFile = args[2];
                int gameIndex = int.Parse(args[3]);
                int version = int.Parse(args[4]);
                bool compress = args[5] == "1";
                bool encrypt = args[6] == "1";
                bool dontEncLua = args[7] == "1";
                int compressAlgo = args.Length >= 9 ? int.Parse(args[8]) : 0;

                Settings.EnsureConfigAvailable();
                // Hardcoded WG Ep1 key for CLI test (gameIndex 21).
                byte[] key = { 0x96, 0xCA, 0x99, 0xA0, 0x85, 0xCF, 0x98, 0x8A, 0xE4, 0xDB, 0xE2, 0xCD, 0xA6, 0x96, 0x83, 0x88, 0xC0, 0x8B, 0x99, 0xE3, 0x9E, 0xD8, 0x9B, 0xB6, 0xD7, 0x90, 0xDC, 0xBE, 0xAD, 0x9D, 0x91, 0x65, 0xB6, 0xA6, 0x9E, 0xBB, 0xC2, 0xC6, 0x9E, 0xB3, 0xE7, 0xE3, 0xE5, 0xD5, 0xAB, 0x63, 0x82, 0xA0, 0x9C, 0xC4, 0x92, 0x9F, 0xD1, 0xD5, 0xA4 };
                Console.WriteLine("CLI pack: " + inputDir + " -> " + outputFile + " v" + version + " game=" + gameIndex);
                ArchivePacker.PackTtarchCli(inputDir, outputFile, key, version, compress, encrypt, dontEncLua, compressAlgo);
                Console.WriteLine("CLI pack DONE");
                return 0;
            }

            // Issue #81: forms are laid out at 9pt Tahoma. Without an explicit
            // default, .NET Framework asks the OS, which on non-English Windows
            // (Chinese, Japanese, Korean...) returns a CJK fallback whose glyph
            // metrics are wider, so labels overflow their panels. Pin a Latin
            // font before any form is constructed so every Form/Control inherits it.
            ApplyDefaultFont();

            string xmlPath = Settings.EnsureConfigAvailable();
            if (File.Exists(xmlPath))
            {
                FirstTime = false;
            }

            // Load the saved config early so the interface language is known before any form is
            // constructed. MainMenu_Load re-reads config.xml afterwards (harmless); we only need
            // the language here. On first run there is no config, so the UI stays English.
            Settings earlyConfig = Settings.LoadConfigOrNull();
            if (earlyConfig != null)
            {
                MainMenu.settings = earlyConfig;
                //A recoverable backup/profile is still an existing configuration even when the
                //primary config.xml was missing or damaged.
                FirstTime = false;
            }
            Localization.Init(earlyConfig != null ? earlyConfig.interfaceLanguage : null);

            // On the very first run (no config yet) create default Input/Output folders next
            // to the executable and pre-select them in the first-run settings screen. Users
            // who already have a saved config are never touched.
            if (FirstTime)
            {
                Settings.EnsureDefaultFolders(MainMenu.settings);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(FirstTime ? (Form)new SettingsForm() : new MainMenu());

            return 0;
        }

        // .NET Framework 4.8 has no Application.SetDefaultFont, so we override
        // Control.defaultFont (the lazy backing field of Control.DefaultFont)
        // via reflection. Every Form built after this point inherits the font
        // we set; controls that don't override it pick it up automatically.
        private static void ApplyDefaultFont()
        {
            try
            {
                Font font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
                FieldInfo fi = typeof(Control).GetField(
                    "defaultFont",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (fi != null)
                {
                    fi.SetValue(null, font);
                }
            }
            catch
            {
                // If the runtime renamed the field this fallback is harmless;
                // forms simply revert to the OS default and the cosmetic bug
                // returns instead of crashing the app.
            }
        }
    }
}
