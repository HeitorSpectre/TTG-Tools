using System.ComponentModel;
using System.Windows.Forms;

namespace TTG_Tools
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }
    }
}
