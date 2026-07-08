using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace TTG_Tools
{
    /// <summary>
    /// Applies translations to Designer-built forms without touching each control by hand.
    ///
    /// The convention is: key = "<FormTypeName>.<ControlName>". <see cref="Localize"/> walks the
    /// whole control tree, replaces each named control's <c>Text</c> when a translation exists
    /// (otherwise the English Designer text is kept), and then — for non-English languages — runs a
    /// layout "reflow" so the interface ADAPTS to the (usually longer) translated text: controls
    /// grow to fit their text at full font size, controls to their right are pushed over to keep the
    /// original spacing, and the group boxes / window grow to contain everything. Nothing is ever
    /// shrunk or squeezed, and the English layout is left exactly as designed.
    /// </summary>
    internal static class Localizer
    {
        // Extra pixels added to measured text so it isn't cramped against the control border.
        private const int ButtonPad = 16;
        private const int CheckRadioPad = 22; // box glyph + gap
        private const int LabelPad = 4;

        /// <summary>Localizes a whole form (title + every named child control), then reflows it.</summary>
        public static void Localize(Control root)
        {
            if (root == null) return;

            string formKey = root.GetType().Name;

            //Map each control to its Designer FIELD name. Some forms (e.g. LuaEditor) never set the
            //controls' .Name property, so keying by .Name alone misses them; the field name (which
            //the Designer always declares, like "private Label lblGame;") is what our keys use.
            var names = BuildNameMap(root);

            //English is the baseline the forms were designed for: just apply text, never reflow.
            if (IsEnglish())
            {
                string t;
                if (Localization.TryGet(formKey + ".$this", out t)) root.Text = t;
                SetTexts(root, formKey, names);
                return;
            }

            //Snapshot the ENGLISH bounds BEFORE the (longer) translated text is applied, so the
            //reflow knows the original widths and gaps to reproduce.
            var orig = new Dictionary<Control, Rectangle>();
            CaptureBounds(root, orig);

            string title;
            if (Localization.TryGet(formKey + ".$this", out title)) root.Text = title;
            SetTexts(root, formKey, names);

            Reflow(root, orig);
        }

        private static bool IsEnglish()
        {
            string code = Localization.ActiveLanguageCode;
            return string.IsNullOrEmpty(code) || code == "en";
        }

        #region Text application
        //Maps every Control declared as a field on the form (recursively up the type hierarchy) to
        //its field name. The Designer declares all controls — even deeply nested ones — as fields.
        private static Dictionary<Control, string> BuildNameMap(Control root)
        {
            var map = new Dictionary<Control, string>();
            for (Type t = root.GetType(); t != null && t != typeof(Form) && t != typeof(object); t = t.BaseType)
            {
                foreach (FieldInfo fi in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!typeof(Control).IsAssignableFrom(fi.FieldType)) continue;
                    Control ctl = fi.GetValue(root) as Control;
                    if (ctl != null && !map.ContainsKey(ctl))
                        map[ctl] = fi.Name;
                }
            }
            return map;
        }

        private static string KeyName(Control c, Dictionary<Control, string> names)
        {
            string n;
            if (names != null && names.TryGetValue(c, out n)) return n;
            return c.Name;
        }

        private static void SetTexts(Control parent, string formKey, Dictionary<Control, string> names)
        {
            foreach (Control c in parent.Controls)
            {
                string name = KeyName(c, names);
                if (!string.IsNullOrEmpty(name))
                {
                    string translated;
                    if (Localization.TryGet(formKey + "." + name, out translated))
                        c.Text = translated;
                }

                ToolStrip strip = c as ToolStrip;
                if (strip != null)
                    LocalizeToolStripItems(strip.Items, formKey);

                if (c.Controls.Count > 0)
                    SetTexts(c, formKey, names);
            }
        }

        private static void LocalizeToolStripItems(ToolStripItemCollection items, string formKey)
        {
            foreach (ToolStripItem item in items)
            {
                if (!string.IsNullOrEmpty(item.Name))
                {
                    string translated;
                    if (Localization.TryGet(formKey + "." + item.Name, out translated))
                        item.Text = translated;
                }

                ToolStripDropDownItem dropDown = item as ToolStripDropDownItem;
                if (dropDown != null && dropDown.HasDropDownItems)
                    LocalizeToolStripItems(dropDown.DropDownItems, formKey);
            }
        }
        #endregion

        #region Reflow (grow-to-fit layout)
        /// <summary>Grows controls/containers/window so the translated text fits, without shrinking font.</summary>
        private static void Reflow(Control root, Dictionary<Control, Rectangle> orig)
        {
            int origClientW = root.ClientSize.Width;
            int origClientH = root.ClientSize.Height;
            int engMaxRight, engMaxBottom;
            Extent(root, orig, true, out engMaxRight, out engMaxBottom);
            int rightMargin = origClientW - engMaxRight; if (rightMargin < 0) rightMargin = 8;
            int bottomMargin = origClientH - engMaxBottom; if (bottomMargin < 0) bottomMargin = 8;

            ReflowContainer(root, orig);

            //Grow the window itself to contain everything, keeping the original margins.
            int maxRight, maxBottom;
            Extent(root, orig, false, out maxRight, out maxBottom);
            int newW = maxRight + rightMargin;
            int newH = maxBottom + bottomMargin;
            int finalW = newW > origClientW ? newW : origClientW;
            int finalH = newH > origClientH ? newH : origClientH;

            //A docked top/bottom panel is forced back to the old form width by WinForms layout.
            //If translated children inside it need a few more pixels, carry that overflow to the
            //window itself so controls such as Lua Editor's "new engine" checkbox stay visible.
            foreach (Control child in root.Controls)
            {
                if (!child.Visible) continue;
                ScrollableControl scrollable = child as ScrollableControl;
                if (child.Dock == DockStyle.None || (scrollable != null && scrollable.AutoScroll)) continue;
                int contentRight = 0;
                foreach (Control grandChild in child.Controls)
                {
                    if (!grandChild.Visible) continue;
                    if (grandChild.Visible && grandChild.Right > contentRight) contentRight = grandChild.Right;
                }
                int overflow = contentRight - child.ClientSize.Width;
                if (overflow > 0 && origClientW + overflow > finalW)
                    finalW = origClientW + overflow;
            }

            root.ClientSize = new Size(finalW, finalH);

            //Content areas that originally spanned to the right/bottom edge (log boxes, progress
            //bars, grids, text areas) should keep spanning after the window grows, so they don't
            //leave a blank strip. Controls already anchored to that edge are stretched by WinForms.
            StretchFillers(root, orig, origClientW, origClientH, finalW - origClientW, finalH - origClientH);
        }

        //Extends "filler" controls (the big log/progress/grid areas) by the amount the window grew,
        //so their original right/bottom margin is preserved instead of leaving a blank gap.
        private static void StretchFillers(Control container, Dictionary<Control, Rectangle> orig,
            int origClientW, int origClientH, int growW, int growH)
        {
            foreach (Control c in container.Controls)
            {
                Rectangle o;
                if (!orig.TryGetValue(c, out o)) continue;

                bool isFiller = c is ListBox || c is TextBoxBase || c is ProgressBar
                                || c is DataGridView || c is ListView;
                if (isFiller)
                {
                    //Right edge: stretch only if it was near the original right edge and isn't
                    //already pinned there by a Right anchor (WinForms handles anchored ones).
                    if (growW > 0 && o.Right >= origClientW - 20 && (c.Anchor & AnchorStyles.Right) == 0)
                        c.Width = c.Width + growW;

                    if (growH > 0 && o.Bottom >= origClientH - 20 && (c.Anchor & AnchorStyles.Bottom) == 0)
                        c.Height = c.Height + growH;
                }
            }
        }

        private static void CaptureBounds(Control c, Dictionary<Control, Rectangle> orig)
        {
            foreach (Control child in c.Controls)
            {
                if (!child.Visible) continue;
                orig[child] = child.Bounds;
                if (child.Controls.Count > 0)
                    CaptureBounds(child, orig);
            }
        }

        private static void ReflowContainer(Control container, Dictionary<Control, Rectangle> orig)
        {
            //1. Reflow nested containers first (bottom-up), then grow each to fit its content.
            foreach (Control c in container.Controls)
            {
                if (!c.Visible) continue;
                if (IsContainer(c))
                {
                    ReflowContainer(c, orig);
                    GrowContainer(c, orig);
                }
            }

            //2. Widen text controls to fit their translated text (full font, grow only).
            foreach (Control c in container.Controls)
            {
                if (!c.Visible) continue;
                if (IsGrowText(c))
                {
                    //Keep AutoSize controls dynamic. Several labels receive their final text only
                    //after a file is loaded (archive information, sort warning, etc.); disabling
                    //AutoSize here would make that later text clip even when the initial translation
                    //fit perfectly.
                    int pref = MeasureWidth(c);
                    if (pref > c.Width)
                        c.Width = pref;
                }
            }

            //2b. Buttons stacked in a column (same original Left) get a uniform width = the widest
            //one, so a grid of menu buttons stays aligned instead of looking ragged.
            UnifyButtonColumns(container, orig);

            //3. Push right-hand neighbours over so nothing overlaps, keeping the English gaps.
            PushApart(container, orig);
        }

        //For every pair of siblings that overlap vertically, makes sure the right one keeps at least
        //its original horizontal gap from the (possibly grown) left one. Controls only move right, so
        //a few relaxation passes converge. Uses ENGLISH bounds for ordering/gaps, current bounds for
        //the actual push.
        private static void PushApart(Control container, Dictionary<Control, Rectangle> orig)
        {
            var kids = new List<Control>();
            foreach (Control c in container.Controls) kids.Add(c);
            kids.RemoveAll(c => !c.Visible);
            if (kids.Count < 2) return;

            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                for (int i = 0; i < kids.Count; i++)
                {
                    for (int j = 0; j < kids.Count; j++)
                    {
                        if (i == j) continue;
                        Control a = kids[i], b = kids[j];

                        //a must be the one originally on the LEFT; b the one on the right we push.
                        if (EngLeft(a, orig) >= EngLeft(b, orig)) continue;
                        if (IsRightAnchored(b)) continue;
                        if (!VerticalOverlap(a, b, orig)) continue;

                        int gap = EngLeft(b, orig) - EngRight(a, orig);
                        if (gap < 2) gap = 2;

                        int required = a.Right + gap;
                        if (b.Left < required) { b.Left = required; changed = true; }
                    }
                }
                if (!changed) break;
            }
        }

        //Gives every button that starts at the same original X (a visual column) the same width —
        //the widest button in that column — so button grids stay aligned after growth.
        private static void UnifyButtonColumns(Control container, Dictionary<Control, Rectangle> orig)
        {
            var columns = new Dictionary<string, List<Button>>();
            foreach (Control c in container.Controls)
            {
                Button b = c as Button;
                if (b == null) continue;
                int left = EngLeft(b, orig);
                Rectangle original = orig.ContainsKey(b) ? orig[b] : b.Bounds;
                //Buttons can share a left edge while belonging to different grids (for example,
                //Lua Editor's 390px main action above a 190px manual action). Grouping those
                //together made the narrow row 390px wide and pushed its neighbours off-screen.
                string key = (left / 8) + ":" + (original.Width / 8);
                List<Button> list;
                if (!columns.TryGetValue(key, out list)) { list = new List<Button>(); columns[key] = list; }
                list.Add(b);
            }

            foreach (List<Button> col in columns.Values)
            {
                if (col.Count < 2) continue;
                int maxW = 0;
                foreach (Button b in col) if (b.Width > maxW) maxW = b.Width;
                foreach (Button b in col) b.Width = maxW;
            }
        }

        //Grows a group box / panel so it contains all its (moved/grown) children, keeping the same
        //inner padding it had originally.
        private static void GrowContainer(Control container, Dictionary<Control, Rectangle> orig)
        {
            TabControl tabs = container as TabControl;
            if (tabs != null)
            {
                int requiredPageWidth = 0;
                int requiredPageHeight = 0;
                int pageClientWidth = 0;
                int pageClientHeight = 0;

                foreach (TabPage page in tabs.TabPages)
                {
                    pageClientWidth = Math.Max(pageClientWidth, page.ClientSize.Width);
                    pageClientHeight = Math.Max(pageClientHeight, page.ClientSize.Height);
                    foreach (Control child in page.Controls)
                    {
                        if (child.Right + 12 > requiredPageWidth) requiredPageWidth = child.Right + 12;
                        if (child.Bottom + 12 > requiredPageHeight) requiredPageHeight = child.Bottom + 12;
                    }
                }

                int chromeWidth = Math.Max(0, tabs.Width - pageClientWidth);
                int chromeHeight = Math.Max(0, tabs.Height - pageClientHeight);
                int tabWidth = requiredPageWidth + chromeWidth;
                int tabHeight = requiredPageHeight + chromeHeight;
                tabs.Size = new Size(
                    tabWidth > tabs.Width ? tabWidth : tabs.Width,
                    tabHeight > tabs.Height ? tabHeight : tabs.Height);
                return;
            }

            Rectangle o;
            if (!orig.TryGetValue(container, out o)) o = container.Bounds;

            int engRight, engBottom;
            Extent(container, orig, true, out engRight, out engBottom);
            int padR = o.Width - engRight; if (padR < 4) padR = 8;
            int padB = o.Height - engBottom; if (padB < 4) padB = 8;

            int maxRight, maxBottom;
            Extent(container, orig, false, out maxRight, out maxBottom);

            int newW = maxRight + padR;
            int newH = maxBottom + padB;
            container.Size = new Size(
                newW > container.Width ? newW : container.Width,
                newH > container.Height ? newH : container.Height);
        }

        private static void Extent(Control container, Dictionary<Control, Rectangle> orig,
            bool english, out int maxRight, out int maxBottom)
        {
            maxRight = 0; maxBottom = 0;
            foreach (Control c in container.Controls)
            {
                if (!c.Visible) continue;
                Rectangle b = english && orig.ContainsKey(c) ? orig[c] : c.Bounds;
                if (b.Right > maxRight) maxRight = b.Right;
                if (b.Bottom > maxBottom) maxBottom = b.Bottom;
            }
        }
        #endregion

        #region helpers
        private static bool IsContainer(Control c)
        {
            return c is GroupBox || c is Panel || c is TabControl || c is TabPage;
        }

        private static bool IsGrowText(Control c)
        {
            Label l = c as Label;
            if (l != null && l.MaximumSize.Width > 0) return false; // meant to wrap — leave it
            return c is Button || c is CheckBox || c is RadioButton || c is Label || c is LinkLabel;
        }

        private static bool IsRightAnchored(Control c)
        {
            return (c.Anchor & AnchorStyles.Right) != 0 && (c.Anchor & AnchorStyles.Left) == 0;
        }

        private static bool VerticalOverlap(Control a, Control b, Dictionary<Control, Rectangle> orig)
        {
            Rectangle ra = orig.ContainsKey(a) ? orig[a] : a.Bounds;
            Rectangle rb = orig.ContainsKey(b) ? orig[b] : b.Bounds;
            return ra.Top < rb.Bottom && rb.Top < ra.Bottom;
        }

        private static int MeasureWidth(Control c)
        {
            int text = TextRenderer.MeasureText(c.Text ?? "", c.Font).Width;
            if (c is CheckBox || c is RadioButton) return text + CheckRadioPad;
            if (c is Button) return text + ButtonPad;
            return text + LabelPad;
        }

        private static int EngLeft(Control c, Dictionary<Control, Rectangle> orig)
        {
            Rectangle r; return orig.TryGetValue(c, out r) ? r.Left : c.Left;
        }
        private static int EngRight(Control c, Dictionary<Control, Rectangle> orig)
        {
            Rectangle r; return orig.TryGetValue(c, out r) ? r.Right : c.Right;
        }
        #endregion

        /// <summary>Reflows a code-built form (e.g. SettingsForm) after its translated text is set.</summary>
        public static void AutoFit(Control root)
        {
            if (root == null || IsEnglish()) return;
            var orig = new Dictionary<Control, Rectangle>();
            CaptureBounds(root, orig);
            Reflow(root, orig);
        }

        /// <summary>Localizes the column headers of a <see cref="ListView"/> (not part of the tree).</summary>
        public static void LocalizeColumns(ListView lv, string formKey)
        {
            if (lv == null || string.IsNullOrEmpty(lv.Name)) return;
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                string translated;
                if (Localization.TryGet(formKey + "." + lv.Name + ".col" + i, out translated))
                    lv.Columns[i].Text = translated;
            }
        }
    }
}
