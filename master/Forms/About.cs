using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TTG_Tools
{
    public partial class About : Form
    {
        // Names written as markdown [Name](url) become clickable links on the Name.
        private static readonly Regex MarkdownLink = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        // Bare URLs (e.g. the GitHub lines) are made clickable as-is.
        private static readonly Regex BareUrl = new Regex(@"https?://[^\s]+", RegexOptions.Compiled);

        public About()
        {
            InitializeComponent();
            AppIcon.Apply(this);
            Localizer.Localize(this);
            BuildReadOnlyPanel();
        }

        // Replaces the editable RichTextBox with a read-only, non-selectable scrollable panel
        // of Labels (plain text) and LinkLabels (embedded clickable links).
        private void BuildReadOnlyPanel()
        {
            string text = richTextBox1.Text ?? string.Empty;

            // Reuse the RichTextBox slot so the layout stays the same, but hide the box itself.
            Panel host = new Panel
            {
                Bounds = richTextBox1.Bounds,
                Anchor = richTextBox1.Anchor,
                AutoScroll = true,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D
            };
            richTextBox1.Visible = false;
            Controls.Add(host);
            host.BringToFront();

            int contentWidth = host.ClientSize.Width - 6;

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(6, 6, 6, 6)
            };
            host.Controls.Add(flow);

            string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            StringBuilder plain = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                LinkLabel link = TryBuildLinkLabel(lines[i], contentWidth);

                if (link != null)
                {
                    FlushPlain(flow, plain, contentWidth);
                    flow.Controls.Add(link);
                }
                else
                {
                    plain.Append(lines[i]);
                    plain.Append('\n');
                }
            }

            FlushPlain(flow, plain, contentWidth);
        }

        private void FlushPlain(FlowLayoutPanel flow, StringBuilder plain, int contentWidth)
        {
            if (plain.Length == 0) return;

            flow.Controls.Add(new Label
            {
                Text = plain.ToString().TrimEnd('\n'),
                AutoSize = true,
                UseMnemonic = false, // so '&' in game names (Sam & Max) shows literally
                MaximumSize = new Size(contentWidth, 0),
                Margin = new Padding(0)
            });

            plain.Clear();
        }

        // Builds a LinkLabel for a line that contains links, or returns null for plain text.
        private LinkLabel TryBuildLinkLabel(string line, int contentWidth)
        {
            var spans = new List<KeyValuePair<int, KeyValuePair<int, string>>>(); // start -> (length, url)

            if (MarkdownLink.IsMatch(line))
            {
                StringBuilder display = new StringBuilder();
                int pos = 0;
                foreach (Match m in MarkdownLink.Matches(line))
                {
                    display.Append(line.Substring(pos, m.Index - pos));
                    string name = m.Groups[1].Value;
                    string url = m.Groups[2].Value;
                    int start = display.Length;
                    display.Append(name);
                    spans.Add(new KeyValuePair<int, KeyValuePair<int, string>>(start, new KeyValuePair<int, string>(name.Length, url)));
                    pos = m.Index + m.Length;
                }
                display.Append(line.Substring(pos));
                return MakeLinkLabel(display.ToString(), spans, contentWidth);
            }

            Match bare = BareUrl.Match(line);
            if (bare.Success)
            {
                spans.Add(new KeyValuePair<int, KeyValuePair<int, string>>(bare.Index, new KeyValuePair<int, string>(bare.Length, bare.Value)));
                return MakeLinkLabel(line, spans, contentWidth);
            }

            return null;
        }

        private LinkLabel MakeLinkLabel(string text, List<KeyValuePair<int, KeyValuePair<int, string>>> spans, int contentWidth)
        {
            LinkLabel label = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                UseMnemonic = false,
                MaximumSize = new Size(contentWidth, 0),
                Margin = new Padding(0),
                LinkBehavior = LinkBehavior.HoverUnderline
            };

            foreach (var span in spans)
            {
                label.Links.Add(span.Key, span.Value.Key, span.Value.Value);
            }

            label.LinkClicked += (s, e) =>
            {
                string url = e.Link.LinkData as string;
                if (string.IsNullOrEmpty(url)) return;
                try { Process.Start(url); }
                catch { /* ignore if no browser/handler is available */ }
            };

            return label;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
