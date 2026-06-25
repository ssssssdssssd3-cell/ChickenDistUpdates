using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>ألوان وأنماط النظام</summary>
    public static class Theme
    {
        // الألوان الرئيسية - هادئة ومريحة جداً للعين (Eye-Comfort Soft Palette)
        public static Color Primary    = Color.FromArgb(45, 55, 72);    // Slate Blue / Navy هادئ
        public static Color Accent     = Color.FromArgb(212, 163, 89);  // Sand Gold / ذهبي رملي ناعم غير متوهج
        public static Color AccentDark = Color.FromArgb(180, 135, 20);
        public static Color Success    = Color.FromArgb(46, 117, 89);   // Sage Green / أخضر هادئ
        public static Color Danger     = Color.FromArgb(186, 73, 73);   // Terracotta Red / أحمر طوبي هادئ
        public static Color BgLight    = Color.FromArgb(245, 247, 250); // رمادي-أزرق فاتح وناعم
        public static Color BgWhite    = Color.White;
        public static Color TextDark   = Color.FromArgb(45, 55, 72);
        public static Color TextGray   = Color.FromArgb(115, 125, 140);
        public static Color Sidebar    = Color.FromArgb(240, 244, 248);
        public static Color SidebarBtn = Color.FromArgb(25, 45, 55, 72);

        // ألوان النمط الفاتح المريح للعين
        public static Color BgMain      = Color.FromArgb(245, 247, 250); // خلفية النوافذ
        public static Color BgCard      = Color.FromArgb(255, 255, 255); // خلفية البطاقات والألواح
        public static Color BgInput     = Color.FromArgb(255, 255, 255); // خلفية حقول الإدخال
        public static Color TextMain    = Color.FromArgb(45, 55, 72);     // النص الرئيسي (داكن وواضح)
        public static Color TextSub     = Color.FromArgb(100, 110, 125);  // النص الفرعي
        public static Color BorderColor = Color.FromArgb(218, 224, 233);  // حدود خفيفة وناعمة

        // ألوان اللوحات الرئيسية (رأس الصفحة والنابار)
        public static Color BgHeader    = Color.FromArgb(45, 55, 72);     // رأس الصفحة (كحلي هادئ)
        public static Color BgNavBar    = Color.FromArgb(240, 244, 248);  // شريط الأزرار (رمادي فاتح)

        // الخطوط
        public static Font FontMain   = new Font("Segoe UI", 9.5f);
        public static Font FontTitle  = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static Font FontHeader = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static Font FontNormal = new Font("Segoe UI", 10f);
        public static Font FontBold   = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static Font FontSmall  = new Font("Segoe UI", 9f);
        public static Font FontArabic = new Font("Segoe UI", 10f);

        public static void ApplyRTL(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                // Force RTL on all controls
                c.RightToLeft = RightToLeft.Yes;

                if (c is TextBox tb)
                {
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.ForeColor = TextMain;
                    tb.BackColor = BgInput;

                    // Auto select all text on focus
                    tb.Enter += (s, e) => {
                        tb.BeginInvoke((MethodInvoker)delegate {
                            tb.SelectAll();
                        });
                    };

                    // Move focus on Enter
                    tb.KeyDown += (s, e) => {
                        if (e.KeyCode == Keys.Enter)
                        {
                            if (tb.Multiline) return;
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            tb.FindForm()?.SelectNextControl(tb, true, true, true, true);
                        }
                    };
                }
                else if (c is ComboBox cb)
                {
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.ForeColor = TextMain;
                    cb.BackColor = BgInput;

                    // Move focus on Enter
                    cb.KeyDown += (s, e) => {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            cb.FindForm()?.SelectNextControl(cb, true, true, true, true);
                        }
                    };
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BorderStyle = BorderStyle.FixedSingle;
                    nud.ForeColor = TextMain;
                    nud.BackColor = BgInput;
                    foreach (Control child in nud.Controls)
                    {
                        if (child.GetType().Name == "UpDownButtons")
                        {
                            child.Visible = false;
                            child.Width = 0;
                        }

                        // Attach select all and Enter key navigation to the internal TextBox editor
                        if (child is TextBox || child.GetType().Name.Contains("Edit"))
                        {
                            child.Enter += (s, ev) => {
                                nud.BeginInvoke((MethodInvoker)delegate {
                                    nud.Select(0, nud.Text.Length);
                                });
                            };
                            child.KeyDown += (s, ev) => {
                                if (ev.KeyCode == Keys.Enter)
                                {
                                    ev.Handled = true;
                                    ev.SuppressKeyPress = true;
                                    nud.FindForm()?.SelectNextControl(nud, true, true, true, true);
                                }
                            };
                        }
                    }

                    // Also attach to parent NumericUpDown
                    nud.Enter += (s, e) => {
                        nud.BeginInvoke((MethodInvoker)delegate {
                            nud.Select(0, nud.Text.Length);
                        });
                    };

                    nud.KeyDown += (s, e) => {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            nud.FindForm()?.SelectNextControl(nud, true, true, true, true);
                        }
                    };
                }
                else if (c is DateTimePicker dtp)
                {
                    dtp.RightToLeftLayout = true;

                    // Move focus on Enter
                    dtp.KeyDown += (s, e) => {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            dtp.FindForm()?.SelectNextControl(dtp, true, true, true, true);
                        }
                    };
                }
                else if (c is SplitContainer sc)
                {
                    sc.RightToLeft = RightToLeft.Yes;
                    ApplyRTL(sc.Panel1.Controls);
                    ApplyRTL(sc.Panel2.Controls);
                }
                else if (c is TabControl tc)
                {
                    tc.RightToLeft = RightToLeft.Yes;
                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.RightToLeft = RightToLeft.Yes;
                        ApplyRTL(tp.Controls);
                    }
                }

                if (c.HasChildren)
                    ApplyRTL(c.Controls);
            }
        }

        /// <summary>يطبق RTL كاملاً على الـ Form بكل محتوياته</summary>
        public static void ApplyFormRTL(Form form)
        {
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            ApplyRTL(form.Controls);

            // Add form-level key listener for Enter as Tab navigation
            form.KeyPreview = true;
            form.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    Control activeCtrl = form.ActiveControl;
                    if (activeCtrl != null)
                    {
                        if (activeCtrl is DataGridView) return;
                        if (activeCtrl is TextBox tb && tb.Multiline) return;
                        if (activeCtrl is Button) return;

                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        form.SelectNextControl(activeCtrl, true, true, true, true);
                    }
                }
            };
        }

        private static Color GetHoverColor(Color color)
        {
            int r = Math.Min(255, (int)(color.R * 1.15));
            int g = Math.Min(255, (int)(color.G * 1.15));
            int b = Math.Min(255, (int)(color.B * 1.15));
            return Color.FromArgb(r, g, b);
        }

        private static Color GetDownColor(Color color)
        {
            int r = (int)(color.R * 0.85);
            int g = (int)(color.G * 0.85);
            int b = (int)(color.B * 0.85);
            return Color.FromArgb(r, g, b);
        }

        /// <summary>تصميم زر رئيسي (بدون إحداثيات)</summary>
        public static Button MakeButton(string text, Color backColor = default)
        {
            if (backColor == default) backColor = Accent;
            var btn = new Button
            {
                Text = text,
                Font = FontBold,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 38,
                Cursor = Cursors.Hand,
                RightToLeft = RightToLeft.Yes
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = GetHoverColor(backColor);
            btn.FlatAppearance.MouseDownBackColor = GetDownColor(backColor);
            return btn;
        }

        /// <summary>تصميم زر مع إحداثيات وأبعاد محددة</summary>
        public static Button MakeButton(string text, int x, int y, int width, int height, Color backColor)
        {
            var btn = MakeButton(text, backColor);
            btn.Location = new System.Drawing.Point(x, y);
            btn.Size = new System.Drawing.Size(width, height);
            return btn;
        }

        /// <summary>تصميم Label عنوان</summary>
        public static Label MakeLabel(string text, Font font = null)
        {
            return new Label
            {
                Text = text,
                Font = font ?? FontNormal,
                ForeColor = TextDark,
                AutoSize = true,
                RightToLeft = RightToLeft.Yes
            };
        }

        /// <summary>تصميم TextBox موحد</summary>
        public static TextBox MakeTextBox(int width = 200)
        {
            return new TextBox
            {
                Width = width,
                Height = 30,
                Font = FontNormal,
                RightToLeft = RightToLeft.Yes,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        /// <summary>تصميم DataGridView موحد</summary>
        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = BgWhite;
            grid.BorderStyle = BorderStyle.None;
            grid.DefaultCellStyle.Font = FontSmall;
            grid.DefaultCellStyle.SelectionBackColor = Primary;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 30;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(220, 220, 220);
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false;
            grid.RightToLeft = RightToLeft.Yes;
        }

        /// <summary>لوحة عنوان الشاشة</summary>
        public static Panel MakeTitleBar(string title, string subtitle = "")
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BgCard,
                Padding = new Padding(20, 0, 0, 0)
            };
            var lbl = new Label
            {
                Text = title,
                Font = FontTitle,
                ForeColor = TextMain,
                AutoSize = false,
                Width = 600,
                Height = 40,
                Top = 8,
                Left = 15,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(lbl);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = TextSub,
                    AutoSize = false,
                    Width = 600,
                    Height = 22,
                    Top = 46,
                    Left = 15,
                    RightToLeft = RightToLeft.Yes,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panel.Controls.Add(sub);
            }

            // رسم خط فاصل متدرج فخم جداً (Modern Gradient Separator) في أسفل اللوحة
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(new Point(0, panel.Height - 3), new Point(panel.Width, panel.Height - 3), Accent, Primary))
                using (var pen = new Pen(brush, 2f))
                {
                    g.DrawLine(pen, 0, panel.Height - 2, panel.Width, panel.Height - 2);
                }
            };

            return panel;
        }
    }
}
