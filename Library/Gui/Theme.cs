  //\\   OmenMon: Hardware Monitoring & Control Utility
 //  \\  Copyright © 2023 Piotr Szczepański * License: GPL3
     //  https://omenmon.github.io/
//
// Modern dark theme (custom build): palette, card-style group boxes,
// dark window chrome, owner-drawn combo boxes and a dark tray menu.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OmenMon.Library {

    // Theme palette and helpers
    public static class Theme {

#region Palette
        public static readonly Color Back       = Color.FromArgb(0x14, 0x15, 0x19); // Window background
        public static readonly Color Card       = Color.FromArgb(0x1E, 0x20, 0x26); // Card surface
        public static readonly Color CardBorder = Color.FromArgb(0x2E, 0x31, 0x3A); // Card outline
        public static readonly Color Input      = Color.FromArgb(0x2A, 0x2D, 0x35); // Input field surface
        public static readonly Color InputHover = Color.FromArgb(0x36, 0x3A, 0x44); // Hovered input surface
        public static readonly Color Text       = Color.FromArgb(0xE8, 0xE9, 0xED); // Primary text
        public static readonly Color SubText    = Color.FromArgb(0x9A, 0xA0, 0xAC); // Secondary text
        public static readonly Color Disabled   = Color.FromArgb(0x5C, 0x61, 0x6C); // Disabled text
        public static readonly Color Accent     = Color.FromArgb(0xFF, 0x46, 0x55); // Omen-style red
        public static readonly Color Accent2    = Color.FromArgb(0x3A, 0xD1, 0xE0); // Cyan
        public static readonly Color KbdOutline = Color.FromArgb(0x6B, 0x71, 0x7E); // Keyboard drawing lines
        public static readonly Color KbdOff     = Color.FromArgb(0x33, 0x36, 0x3F); // Keys with backlight off

        public static readonly Font UiFont      = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static readonly Font TitleFont   = new Font("Segoe UI Semibold", 8.25f, FontStyle.Regular);
#endregion

#region Window Chrome
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hwnd);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        // Applies a dark title bar, rounded corners and matching caption color
        public static void ApplyWindowChrome(Form form) {
            try {
                IntPtr h = form.Handle;
                int on = 1, round = 2;
                int caption = ColorTranslator.ToWin32(Back);
                int border = ColorTranslator.ToWin32(CardBorder);
                int text = ColorTranslator.ToWin32(Text);
                DwmSetWindowAttribute(h, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, 4);
                DwmSetWindowAttribute(h, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, 4);
                DwmSetWindowAttribute(h, DWMWA_CAPTION_COLOR, ref caption, 4);
                DwmSetWindowAttribute(h, DWMWA_BORDER_COLOR, ref border, 4);
                DwmSetWindowAttribute(h, DWMWA_TEXT_COLOR, ref text, 4);
            } catch { } // Older Windows versions: silently keep the default chrome
        }
#endregion

#region Control Styling
        // Recursively applies the theme to a form or control tree
        public static void Apply(Control root) {

            if(root is Form form) {
                form.BackColor = Back;
                form.ForeColor = Text;
                if(form.IsHandleCreated)
                    ApplyWindowChrome(form);
                else
                    form.HandleCreated += (s, e) => ApplyWindowChrome((Form) s);
            }

            foreach(Control c in root.Controls)
                ApplyControl(c);

        }

        // Applies the theme to a single control and its children
        private static void ApplyControl(Control c) {

            // Only replace the font where the default font is in use
            // (keeps the custom digital figure font intact)
            if(c.Font.Name == Control.DefaultFont.Name)
                c.Font = c is GroupBox ? TitleFont : UiFont;

            switch(c) {

                case GroupBox g:
                    g.BackColor = Card;
                    g.ForeColor = Text;
                    break;

                case ButtonBase b when b is Button:
                    b.FlatStyle = FlatStyle.Flat;
                    b.BackColor = Input;
                    b.ForeColor = Text;
                    b.FlatAppearance.BorderColor = CardBorder;
                    b.FlatAppearance.MouseOverBackColor = InputHover;
                    b.FlatAppearance.MouseDownBackColor = Accent;
                    b.Cursor = Cursors.Hand;
                    break;

                case RadioButton r:
                    r.FlatStyle = FlatStyle.Flat;
                    r.ForeColor = Text;
                    r.FlatAppearance.CheckedBackColor = Accent;
                    r.Cursor = Cursors.Hand;
                    break;

                case CheckBox k:
                    k.FlatStyle = FlatStyle.Flat;
                    k.ForeColor = Text;
                    k.FlatAppearance.CheckedBackColor = Accent;
                    k.Cursor = Cursors.Hand;
                    break;

                case ComboBox cb:
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.BackColor = Input;
                    cb.ForeColor = Text;
                    cb.DrawMode = DrawMode.OwnerDrawFixed;
                    cb.ItemHeight = 17;
                    cb.DrawItem -= DrawComboItem;
                    cb.DrawItem += DrawComboItem;
                    cb.Cursor = Cursors.Hand;
                    if(cb.IsHandleCreated)
                        SetWindowTheme(cb.Handle, "DarkMode_CFD", null);
                    else
                        cb.HandleCreated += (s, e) => SetWindowTheme(((Control) s).Handle, "DarkMode_CFD", null);
                    break;

                case RichTextBox rt:
                    // A disabled rich-text box ignores its background color,
                    // so keep it enabled but read-only and caret-free
                    rt.Enabled = true;
                    rt.ReadOnly = true;
                    rt.TabStop = false;
                    rt.BackColor = Card;
                    rt.ForeColor = Text;
                    rt.GotFocus += (s, e) => HideCaret(((Control) s).Handle);
                    rt.MouseDown += (s, e) => HideCaret(((Control) s).Handle);
                    rt.SelectionChanged += (s, e) => HideCaret(((Control) s).Handle);
                    break;

                case TextBox t:
                    t.BackColor = Input;
                    t.ForeColor = Text;
                    t.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case TrackBar tb:
                    tb.BackColor = Card;
                    break;

                case ProgressBarEx p:
                    // Gradient colors are kept; only the track behind them changes
                    break;

                case Label l:
                    l.ForeColor = l.Font.Name == UiFont.Name ? SubText : Text;
                    break;

                case PictureBox pb:
                    pb.BackColor = Card;
                    break;

            }

            foreach(Control child in c.Controls)
                ApplyControl(child);

        }

        // Owner-draws a combo box item (and the closed edit portion) in theme colors
        private static void DrawComboItem(object sender, DrawItemEventArgs e) {

            ComboBox cb = (ComboBox) sender;
            bool selected = (e.State & DrawItemState.Selected) != 0
                && (e.State & DrawItemState.ComboBoxEdit) == 0;

            using(SolidBrush bg = new SolidBrush(selected ? Accent : Input))
                e.Graphics.FillRectangle(bg, e.Bounds);

            if(e.Index >= 0) {
                string text = cb.GetItemText(cb.Items[e.Index]);
                Color fg = !cb.Enabled ? Disabled : selected ? Color.White : Text;
                TextRenderer.DrawText(e.Graphics, text, cb.Font,
                    Rectangle.Inflate(e.Bounds, -3, 0), fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }

        }

        // Builds a rounded-rectangle path
        public static GraphicsPath RoundRect(RectangleF r, float radius) {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
#endregion

    }

    // Group box rendered as a rounded card with a small accent title
    public class CardGroup : GroupBox {

        public CardGroup() {
            this.SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e) {

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Paint the area outside the rounded corners in the parent color
            g.Clear(this.Parent != null ? this.Parent.BackColor : Theme.Back);

            RectangleF r = new RectangleF(0.5f, 0.5f, this.Width - 1.5f, this.Height - 1.5f);
            using(GraphicsPath path = Theme.RoundRect(r, 8))
            using(SolidBrush fill = new SolidBrush(this.BackColor))
            using(Pen border = new Pen(Theme.CardBorder)) {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            // Accent tick and title text
            using(SolidBrush tick = new SolidBrush(Theme.Accent))
                g.FillRectangle(tick, 8, 5, 3, 10);
            TextRenderer.DrawText(g, this.Text.Replace("&&", "&").ToUpperInvariant(),
                Theme.TitleFont, new Point(13, 2), Theme.SubText, TextFormatFlags.NoPrefix);

        }

    }

    // Combo box whose closed state is fully painted in theme colors
    // (the stock control paints light-gray arrow buttons and disabled backgrounds)
    public class ThemedComboBox : ComboBox {

        private const int WM_PAINT = 0x000F;

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            if(m.Msg == WM_PAINT)
                using(Graphics g = Graphics.FromHwnd(this.Handle))
                    PaintThemed(g);
        }

        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e) {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        private void PaintThemed(Graphics g) {

            Rectangle r = this.ClientRectangle;
            bool hot = this.Enabled && (this.Focused || this.ClientRectangle.Contains(PointToClient(Cursor.Position)));

            using(SolidBrush bg = new SolidBrush(this.Enabled ? (hot ? Theme.InputHover : Theme.Input) : Theme.Card))
                g.FillRectangle(bg, r);
            using(Pen border = new Pen(Theme.CardBorder))
                g.DrawRectangle(border, r.X, r.Y, r.Width - 1, r.Height - 1);

            // Chevron arrow
            int cx = r.Right - 10, cy = r.Height / 2;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using(Pen arrow = new Pen(this.Enabled ? Theme.SubText : Theme.Disabled, 1.6f))
                g.DrawLines(arrow, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });

            // Current item text
            string text = this.SelectedIndex >= 0 ? GetItemText(this.SelectedItem) : "";
            TextRenderer.DrawText(g, text, this.Font,
                new Rectangle(r.X + 4, r.Y, r.Width - 24, r.Height),
                this.Enabled ? Theme.Text : Theme.Disabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }

    }

    // Dark color table for the tray menu
    public class ThemeMenuColors : ProfessionalColorTable {
        public override Color ToolStripDropDownBackground => Theme.Card;
        public override Color ImageMarginGradientBegin => Theme.Card;
        public override Color ImageMarginGradientMiddle => Theme.Card;
        public override Color ImageMarginGradientEnd => Theme.Card;
        public override Color MenuBorder => Theme.CardBorder;
        public override Color MenuItemBorder => Theme.InputHover;
        public override Color MenuItemSelected => Theme.InputHover;
        public override Color MenuItemSelectedGradientBegin => Theme.InputHover;
        public override Color MenuItemSelectedGradientEnd => Theme.InputHover;
        public override Color MenuItemPressedGradientBegin => Theme.Input;
        public override Color MenuItemPressedGradientEnd => Theme.Input;
        public override Color SeparatorDark => Theme.CardBorder;
        public override Color SeparatorLight => Theme.CardBorder;
        public override Color CheckBackground => Theme.Accent;
        public override Color CheckSelectedBackground => Theme.Accent;
        public override Color CheckPressedBackground => Theme.Accent;
        public override Color ButtonSelectedBorder => Theme.Accent;
    }

}
