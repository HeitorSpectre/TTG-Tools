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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(FirstTime ? (Form)new FormSettings() : new MainMenu());

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
