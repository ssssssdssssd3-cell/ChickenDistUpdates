using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ChickenDist.Core
{
    /// <summary>ألوان وأنماط النظام</summary>
    public static class Theme
    {
        // الألوان الرئيسية
        public static Color Primary    = Color.FromArgb(26, 43, 75);    // Navy
        public static Color Accent     = Color.FromArgb(243, 156, 18);  // Gold
        public static Color AccentDark = Color.FromArgb(211, 128, 0);
        public static Color Success    = Color.FromArgb(39, 174, 96);
        public static Color Danger     = Color.FromArgb(231, 76, 60);
        public static Color BgLight    = Color.FromArgb(245, 247, 250);
        public static Color BgWhite    = Color.White;
        public static Color TextDark   = Color.FromArgb(30, 39, 46);
        public static Color TextGray   = Color.FromArgb(127, 140, 141);
        public static Color Sidebar    = Color.FromArgb(43, 54, 76);
        public static Color SidebarBtn = Color.FromArgb(15, 255, 255, 255);

        // ألوان النمط الداكن (للشاشات الداخلية)
        public static Color BgMain      = Color.FromArgb(28, 33, 45);
        public static Color BgCard      = Color.FromArgb(36, 42, 58);
        public static Color BgInput     = Color.FromArgb(48, 55, 72);
        public static Color TextMain    = Color.FromArgb(220, 225, 235);
        public static Color TextSub     = Color.FromArgb(150, 160, 180);
        public static Color BorderColor = Color.FromArgb(55, 65, 85);

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
            
            // تحسين تخطيط العناصر للتناسق مع الشاشات والريزليوشن المختلف تلقائياً
            OptimizeFormLayout(form);

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
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 30;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
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
                Height = ScreenHelper.IsSmallScreen ? 60 : 70,
                BackColor = BgCard,
                Padding = new Padding(20, 0, 0, 0)
            };
            var lbl = new Label
            {
                Text = title,
                Font = FontTitle,
                ForeColor = TextMain,
                AutoSize = false,
                Height = 40,
                Top = ScreenHelper.IsSmallScreen ? 4 : 8,
                Left = 15,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lbl.Width = panel.Width - 30;
            panel.Controls.Add(lbl);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = TextSub,
                    AutoSize = false,
                    Height = 20,
                    Top = ScreenHelper.IsSmallScreen ? 36 : 46,
                    Left = 15,
                    RightToLeft = RightToLeft.Yes,
                    TextAlign = ContentAlignment.MiddleRight,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                sub.Width = panel.Width - 30;
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

        /// <summary>يُحسّن تخطيط الفورم والمكونات لتناسب الشاشات الصغيرة وتكون متجاوبة</summary>
        public static void OptimizeFormLayout(Control container)
        {
            if (container == null) return;

            // 1. تحسين المكونات الفرعية أولاً
            foreach (Control child in container.Controls)
            {
                if (child.HasChildren)
                {
                    OptimizeFormLayout(child);
                }
            }

            // 2. إذا كانت الحاوية لوحة تفاصيل أو حاوية إدخال
            if ((container is Panel || container.GetType().Name == "SplitterPanel") && !(container is TableLayoutPanel) && !(container is FlowLayoutPanel))
            {
                Panel panel = (Panel)container;

                // تحسين الألواح الفرعية (مثل MakeField في الموردين) لتتمدد
                var childPanels = new List<Panel>();
                foreach (Control c in panel.Controls)
                {
                    if (c is Panel subPanel && !(subPanel is TableLayoutPanel) && !(subPanel is FlowLayoutPanel) && subPanel.Width >= 250 && subPanel.Width <= 350)
                    {
                        childPanels.Add(subPanel);
                    }
                }

                foreach (var subPanel in childPanels)
                {
                    subPanel.Width = panel.Width - subPanel.Left - 15;
                    subPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    OptimizeRowLayout(subPanel);
                }

                // تحسين ترتيب المكونات المباشرة على نفس السطر
                OptimizeRowLayout(panel);
            }
        }

        private static void OptimizeRowLayout(Control parent)
        {
            if (parent == null || parent.Controls.Count == 0) return;

            // تجميع العناصر غير المترابطة بالـ Docking
            var controls = new List<Control>();
            foreach (Control c in parent.Controls)
            {
                if (c.Dock == DockStyle.None && !(c is Panel && c.Anchor == (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)))
                {
                    controls.Add(c);
                }
            }

            // تجميع المكونات التي تقع على نفس السطر (بفارق 8 بكسل)
            var rows = new List<List<Control>>();
            foreach (var ctrl in controls)
            {
                bool found = false;
                foreach (var row in rows)
                {
                    if (Math.Abs(row[0].Top - ctrl.Top) <= 8)
                    {
                        row.Add(ctrl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    rows.Add(new List<Control> { ctrl });
                }
            }

            // ترتيب الأسطر من الأعلى إلى الأسفل للحفاظ على تسلسل التبويب
            rows.Sort((a, b) => a[0].Top.CompareTo(b[0].Top));

            // فرز الحقول (ملصق ومدخل) عن باقي العناصر
            var fieldRows = new List<KeyValuePair<Label, Control>>();
            var otherControls = new List<Control>();

            foreach (var row in rows)
            {
                Label lbl = row.Count >= 1 && row[0] is Label ? (Label)row[0] : (row.Count >= 2 && row[1] is Label ? (Label)row[1] : null);
                Control input = row.Count >= 1 && !(row[0] is Label) ? row[0] : (row.Count >= 2 && !(row[1] is Label) ? row[1] : null);

                if (lbl != null && input != null && (input is TextBox || input is ComboBox || input is NumericUpDown || input is DateTimePicker))
                {
                    fieldRows.Add(new KeyValuePair<Label, Control>(lbl, input));
                }
                else
                {
                    foreach (var c in row)
                    {
                        otherControls.Add(c);
                    }
                }
            }

            // إذا كان عدد الحقول أكثر من 5، نقسمها لعمودين لتفادي التمرير العمودي الطويل
            if (fieldRows.Count > 5)
            {
                int n = fieldRows.Count;
                int rowsPerCol = (n + 1) / 2;
                int rowHeight = 44; // ارتفاع مناسب للعنوان والمدخل مع الفراغ

                for (int i = 0; i < n; i++)
                {
                    var pair = fieldRows[i];
                    Label lbl = pair.Key;
                    Control input = pair.Value;

                    int col = i < rowsPerCol ? 0 : 1; // 0 = العمود الأيمن (لغة عربية)، 1 = العمود الأيسر
                    int rowIdx = i < rowsPerCol ? i : i - rowsPerCol;

                    int y = 10 + rowIdx * rowHeight;

                    if (col == 0) // العمود الأيمن
                    {
                        lbl.Top = y;
                        lbl.Left = parent.Width - lbl.Width - 10;
                        lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                        input.Top = y + 16;
                        input.Left = parent.Width / 2 + 5;
                        input.Width = parent.Width / 2 - 15;
                        input.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    }
                    else // العمود الأيسر
                    {
                        lbl.Top = y;
                        lbl.Left = parent.Width / 2 - lbl.Width - 10;
                        lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                        input.Top = y + 16;
                        input.Left = 10;
                        input.Width = parent.Width / 2 - 15;
                        input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    }
                }

                // وضع الأزرار وعناصر التحكم الأخرى بالأسفل تحت الأعمدة
                int columnsBottomY = 15 + rowsPerCol * rowHeight + 10;

                // إعادة تجميع عناصر التحكم غير المدخلة وترتيبها عمودياً
                var otherRows = new List<List<Control>>();
                foreach (var ctrl in otherControls)
                {
                    bool found = false;
                    foreach (var r in otherRows)
                    {
                        if (Math.Abs(r[0].Top - ctrl.Top) <= 8)
                        {
                            r.Add(ctrl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        otherRows.Add(new List<Control> { ctrl });
                    }
                }
                otherRows.Sort((a, b) => a[0].Top.CompareTo(b[0].Top));

                int currentY = columnsBottomY;
                foreach (var r in otherRows)
                {
                    int maxH = 0;
                    bool allButtons = true;
                    foreach (var c in r)
                    {
                        if (!(c is Button)) allButtons = false;
                        if (c.Height > maxH) maxH = c.Height;
                    }

                    if (allButtons && r.Count >= 2)
                    {
                        // ترتيب الأزرار المتجاورة بشكل متناسق في اليمين
                        int totalW = 0;
                        foreach (var btn in r) totalW += btn.Width;
                        int spacing = 8;
                        int btnX = parent.Width - 15;

                        r.Sort((a, b) => b.Left.CompareTo(a.Left));
                        foreach (var btn in r)
                        {
                            btn.Top = currentY;
                            btn.Left = btnX - btn.Width;
                            btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                            btnX -= (btn.Width + spacing);
                        }
                    }
                    else
                    {
                        // العناصر الفردية (الـ CheckBox أو الأزرار العريضة المفردة)
                        foreach (var c in r)
                        {
                            c.Top = currentY;
                            if (c is CheckBox || c is Label)
                            {
                                c.Left = parent.Width - c.Width - 15;
                                c.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                            }
                            else
                            {
                                c.Left = 15;
                                c.Width = parent.Width - 30;
                                c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                            }
                        }
                    }
                    currentY += maxH + 10;
                }
            }
            else
            {
                // تخطيط العمود الفردي القياسي للمدخلات القليلة
                foreach (var row in rows)
                {
                    if (row.Count == 2)
                    {
                        Label lbl = row[0] is Label ? (Label)row[0] : (row[1] is Label ? (Label)row[1] : null);
                        Control input = row[0] is Label ? row[1] : (row[1] is Label ? row[0] : null);

                        if (lbl != null && input != null && (input is TextBox || input is ComboBox || input is NumericUpDown || input is DateTimePicker))
                        {
                            if (input.Left < lbl.Left)
                            {
                                lbl.Left = parent.Width - lbl.Width - 15;
                                lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                                input.Left = 15;
                                input.Width = lbl.Left - 15 - 10;
                                input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                            }
                        }
                    }
                    else if (row.Count > 0)
                    {
                        bool allButtons = true;
                        foreach (var c in row)
                        {
                            if (!(c is Button)) { allButtons = false; break; }
                        }
                        if (allButtons)
                        {
                            foreach (var btn in row)
                            {
                                btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                            }
                        }
                    }
                }
            }
        }
    }
}
