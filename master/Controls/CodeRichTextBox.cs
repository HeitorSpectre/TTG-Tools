using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TTG_Tools
{
    [ToolboxItem(true)]
    [DesignTimeVisible(true)]
    public class CodeRichTextBox : RichTextBox
    {
        private bool _showWhitespace;
        public bool ShowWhitespace
        {
            get { return _showWhitespace; }
            set { _showWhitespace = value; Invalidate(); }
        }

        public Color WhitespaceColor { get; set; } = Color.FromArgb(80, 80, 80);
        public int TabSize { get; set; } = 4;

        private int _charWidth;
        private int _lineHeight;
        private string _cachedText;
        private int[] _lineStarts;

        public CodeRichTextBox()
        {
            this.TextChanged += (s, e) => { _cachedText = null; _lineStarts = null; };
            this.FontChanged += (s, e) => { _charWidth = 0; _lineHeight = 0; };
        }

        public void ApplyTabStops()
        {
            try
            {
                using (var g = CreateGraphics())
                {
                    int cw = TextRenderer.MeasureText(g, "M", Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                    if (cw < 1) cw = 1;
                    int tabPixels = cw * TabSize;
                    int[] tabs = new int[32];
                    for (int i = 0; i < 32; i++) tabs[i] = tabPixels * (i + 1);
                    int s = SelectionStart, l = SelectionLength;
                    SelectAll();
                    SelectionTabs = tabs;
                    Select(s, l);
                }
            }
            catch { }
        }

        private void EnsureMetrics(System.Drawing.Graphics g)
        {
            if (_charWidth == 0)
            {
                var sz = TextRenderer.MeasureText(g, "M", Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                _charWidth = Math.Max(1, sz.Width);
                _lineHeight = sz.Height;
            }
        }

        private void EnsureCache()
        {
            if (_cachedText != null) return;
            _cachedText = Text;
            int count = 1;
            for (int i = 0; i < _cachedText.Length; i++) if (_cachedText[i] == '\n') count++;
            _lineStarts = new int[count];
            _lineStarts[0] = 0;
            int idx = 1;
            for (int i = 0; i < _cachedText.Length; i++)
                if (_cachedText[i] == '\n') _lineStarts[idx++] = i + 1;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x000F && _showWhitespace) // WM_PAINT
            {
                try
                {
                    using (var g = System.Drawing.Graphics.FromHwnd(Handle))
                        DrawWhitespaceMarkers(g);
                }
                catch { }
            }
        }

        private void DrawWhitespaceMarkers(System.Drawing.Graphics g)
        {
            if (TextLength == 0) return;
            EnsureMetrics(g);
            EnsureCache();

            int firstChar = GetCharIndexFromPosition(new Point(0, 0));
            int lastChar = GetCharIndexFromPosition(new Point(0, ClientSize.Height));
            int firstLine = GetLineFromCharIndex(firstChar);
            int lastLine = GetLineFromCharIndex(lastChar);
            if (firstLine < 0) firstLine = 0;
            if (lastLine >= _lineStarts.Length) lastLine = _lineStarts.Length - 1;

            Point basePoint = GetPositionFromCharIndex(_lineStarts[firstLine]);
            int xBase = basePoint.X;
            int yStart = basePoint.Y;
            int lh = _lineHeight;
            int tabSize = TabSize;
            int cw = _charWidth;

            using (var brush = new SolidBrush(WhitespaceColor))
            using (var pen = new Pen(WhitespaceColor))
            {
                for (int li = firstLine; li <= lastLine; li++)
                {
                    int lineStart = _lineStarts[li];
                    int lineEnd = (li + 1 < _lineStarts.Length) ? _lineStarts[li + 1] - 1 : _cachedText.Length;
                    int y = yStart + (li - firstLine) * lh;
                    if (y > ClientSize.Height) break;
                    int yMid = y + lh / 2;

                    int col = 0;
                    for (int j = lineStart; j < lineEnd; j++)
                    {
                        char ch = _cachedText[j];
                        if (ch == '\r') continue;
                        if (ch == ' ')
                        {
                            int x = xBase + col * cw + cw / 2 - 1;
                            g.FillRectangle(brush, x, yMid - 1, 2, 2);
                            col++;
                        }
                        else if (ch == '\t')
                        {
                            int nextCol = ((col / tabSize) + 1) * tabSize;
                            int xStart = xBase + col * cw + 2;
                            int xEnd = xBase + nextCol * cw - 3;
                            if (xEnd > xStart)
                            {
                                g.DrawLine(pen, xStart, yMid, xEnd, yMid);
                                g.DrawLine(pen, xEnd - 3, yMid - 3, xEnd, yMid);
                                g.DrawLine(pen, xEnd - 3, yMid + 3, xEnd, yMid);
                            }
                            col = nextCol;
                        }
                        else
                        {
                            col++;
                        }
                    }
                }
            }
        }
    }
}
