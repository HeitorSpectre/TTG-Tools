using System;
using System.Drawing;
using System.Windows.Forms;

namespace TTG_Tools
{
    internal static class AppIcon
    {
        private static Icon cachedIcon;

        private static Icon Current
        {
            get
            {
                if (cachedIcon == null)
                {
                    cachedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }

                return cachedIcon;
            }
        }

        public static void Apply(Form form)
        {
            if (form == null) return;

            try
            {
                form.Icon = Current;
            }
            catch
            {
            }
        }

        public static void Apply(NotifyIcon notifyIcon)
        {
            if (notifyIcon == null) return;

            try
            {
                notifyIcon.Icon = Current;
            }
            catch
            {
            }
        }
    }
}
