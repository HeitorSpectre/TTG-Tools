using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TTG_Tools
{
    /// <summary>
    /// Shared logic used by both settings screens (FormSettings and AutoDePackerSettings).
    /// The two forms keep their own layouts/views, but the duplicated behaviour
    /// (folder picker, unicode-mode radios and their load/save) lives here so there is
    /// a single source of truth. All values are read from / written to the global
    /// <see cref="MainMenu.settings"/>.
    /// </summary>
    internal static class SettingsShared
    {
        /// <summary>Opens a folder picker and returns the chosen path (or the current one if cancelled).</summary>
        public static string PickFolder(string currentPath)
        {
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true,
                InitialDirectory = Directory.Exists(currentPath) ? currentPath : Application.StartupPath
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return folderDialog.FileName;
            }

            return currentPath;
        }

        /// <summary>
        /// Keeps the four unicode-mode radio buttons mutually exclusive. They live in
        /// different group boxes, so WinForms doesn't auto-exclude them; <paramref name="updating"/>
        /// is a per-form re-entrancy guard that prevents the cascade of CheckedChanged events.
        /// </summary>
        public static void HandleUnicodeModeChanged(object sender, ref bool updating,
            RadioButton normal, RadioButton nonNormal, RadioButton newBttF, RadioButton twdSwitch)
        {
            if (updating) return;

            RadioButton selected = sender as RadioButton;
            if (selected == null || !selected.Checked) return;

            updating = true;
            try
            {
                if (selected != normal) normal.Checked = false;
                if (selected != nonNormal) nonNormal.Checked = false;
                if (selected != newBttF) newBttF.Checked = false;
                if (selected != twdSwitch) twdSwitch.Checked = false;
            }
            finally
            {
                updating = false;
            }
        }

        /// <summary>Sets the unicode-mode radios from the saved settings.</summary>
        public static void LoadUnicodeMode(RadioButton normal, RadioButton nonNormal, RadioButton newBttF, RadioButton twdSwitch)
        {
            switch (MainMenu.settings.unicodeSettings)
            {
                case 1:
                    nonNormal.Checked = true;
                    break;

                case 2:
                    newBttF.Checked = true;
                    break;

                default:
                    normal.Checked = true;
                    break;
            }

            twdSwitch.Checked = MainMenu.settings.supportTwdNintendoSwitch;
        }

        /// <summary>Writes the unicode-mode selection back into the settings.</summary>
        public static void SaveUnicodeMode(RadioButton nonNormal, RadioButton newBttF, RadioButton twdSwitch)
        {
            if (newBttF.Checked || twdSwitch.Checked) MainMenu.settings.unicodeSettings = 2;
            else if (nonNormal.Checked) MainMenu.settings.unicodeSettings = 1;
            else MainMenu.settings.unicodeSettings = 0;

            MainMenu.settings.supportTwdNintendoSwitch = twdSwitch.Checked;
        }

        /// <summary>
        /// Minimal single-line text prompt (WinForms has no built-in InputBox). Returns the
        /// entered text, or null if the user cancelled. Used for naming/renaming profiles.
        /// </summary>
        public static string PromptText(IWin32Window owner, string title, string label, string defaultValue)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(324, 112);
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.ShowInTaskbar = false;

                Label l = new Label { Left = 12, Top = 14, Width = 300, Text = label };
                TextBox tb = new TextBox { Left = 12, Top = 36, Width = 300, Text = defaultValue ?? "" };
                Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 156, Top = 72, Width = 72 };
                Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 240, Top = 72, Width = 72 };

                f.Controls.Add(l);
                f.Controls.Add(tb);
                f.Controls.Add(ok);
                f.Controls.Add(cancel);
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                tb.SelectAll();
                return f.ShowDialog(owner) == DialogResult.OK ? tb.Text : null;
            }
        }
    }
}
