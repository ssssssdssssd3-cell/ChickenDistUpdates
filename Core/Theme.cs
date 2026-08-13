using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>ألوان وأنماط النظام</summary>
    public static class Theme
    {
        public static Image GetCompanyLogo()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                    {
                        return new Bitmap(img);
                    }
                }
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("ChickenDist.pro_soft_logo.png"))
                {
                    if (stream != null)
                    {
                        using (var tempImg = Image.FromStream(stream))
                        {
                            return new Bitmap(tempImg);
                        }
                    }
                }
            }
            catch { }
            return CreateDefaultLogoBitmap(128);
        }

        public static Bitmap CreateDefaultLogoBitmap(int size = 128)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                Rectangle rect = new Rectangle(4, 4, size - 8, size - 8);
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(20, 30, 55), Color.FromArgb(13, 110, 253), 45f))
                {
                    g.FillEllipse(bgBrush, rect);
                }

                using (Pen goldPen = new Pen(Color.FromArgb(243, 198, 35), 3.5f))
                {
                    g.DrawEllipse(goldPen, rect);
                }

                Rectangle innerRect = new Rectangle(14, 14, size - 28, size - 28);
                using (Pen innerPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.5f))
                {
                    g.DrawEllipse(innerPen, innerRect);
                }

                using (Font fontEmoji = new Font("Segoe UI Emoji", size * 0.42f, FontStyle.Bold))
                using (Brush goldBrush = new SolidBrush(Color.FromArgb(243, 198, 35)))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("⚡", fontEmoji, goldBrush, new RectangleF(0, -size * 0.02f, size, size), sf);
                }
            }
            return bmp;
        }
        // الألوان الرئيسية - زاهية وواضحة جداً وعالية التباين (Vibrant & High-Contrast Modern Palette)
        public static Color Primary    = Color.FromArgb(13, 110, 253);   // Royal Blue زرقاء زاهية
        public static Color Secondary  = Color.FromArgb(108, 117, 125);  // Slate Gray رمادي متناسق
        public static Color Accent     = Color.FromArgb(253, 126, 20);   // Vibrant Orange برتقالي زاهي
        public static Color AccentDark = Color.FromArgb(217, 83, 79);
        public static Color Success    = Color.FromArgb(25, 135, 84);    // Green خضراء زاهية
        public static Color Danger     = Color.FromArgb(220, 53, 69);    // Red حمراء زاهية
        public static Color BgLight    = Color.FromArgb(245, 247, 250);
        public static Color BgWhite    = Color.White;
        public static Color TextDark   = Color.FromArgb(33, 37, 41);
        public static Color TextGray   = Color.FromArgb(108, 117, 125);
        public static Color Sidebar    = Color.FromArgb(248, 249, 250);
        public static Color SidebarBtn = Color.FromArgb(25, 13, 110, 253);

        // ألوان النمط المختار ديناميكياً
        public static Color BgMain => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(245, 247, 250),
            "Slate" => Color.FromArgb(226, 232, 240),
            _       => Color.FromArgb(40, 44, 52) // Dark
        };

        public static Color BgCard => AppConfig.AppTheme switch
        {
            "Light" => Color.White,
            "Slate" => Color.FromArgb(241, 245, 249),
            _       => Color.FromArgb(50, 54, 64) // Dark
        };

        // ألوان ونظام شريط البحث والفلترة المميز والمريح للعين
        public static Color BgSearchPanel => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(24, 43, 73),   // كحلي فخم ومميز زاهي وواضح جداً
            "Slate" => Color.FromArgb(30, 41, 59),
            _       => Color.FromArgb(24, 38, 62)    // رمادي داكن مائل للأزرق
        };

        public static Color BorderSearchPanel => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(41, 65, 105),
            "Slate" => Color.FromArgb(50, 75, 115),
            _       => Color.FromArgb(65, 78, 102)
        };

        public static Color TextSearchLabel => Color.FromArgb(255, 220, 110); // ذهبي عالي التباين والأناقة

        public static Color BgInput => Color.White; // خانات الإدخال بلون أبيض ناصع ومريح
        public static Color TextInput => Color.FromArgb(33, 37, 41); // لون خط خانات الإدخال - غامق دائماً للقراءة على الخلفية البيضاء

        public static void StyleSearchHeaderPanel(Panel pnl)
        {
            if (pnl == null) return;
            pnl.BackColor = BgSearchPanel;
            pnl.Paint += (s, e) =>
            {
                using (Pen p = new Pen(BorderSearchPanel, 1.5f))
                {
                    e.Graphics.DrawLine(p, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
                }
            };
        }

        public static Color TextMain => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(45, 55, 72),
            "Slate" => Color.FromArgb(15, 23, 42),
            _       => Color.FromArgb(235, 240, 245) // Dark
        };

        public static Color TextSub => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(100, 110, 125),
            "Slate" => Color.FromArgb(71, 85, 105),
            _       => Color.FromArgb(160, 172, 190) // Dark
        };

        public static Color BorderColor => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(218, 224, 233),
            "Slate" => Color.FromArgb(203, 213, 225),
            _       => Color.FromArgb(68, 74, 88) // Dark
        };

        public static Color BgHeader => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(45, 55, 72),
            "Slate" => Color.FromArgb(45, 55, 72),
            _       => Color.FromArgb(30, 34, 42) // Dark
        };

        public static Color BgNavBar => AppConfig.AppTheme switch
        {
            "Light" => Color.FromArgb(240, 244, 248),
            "Slate" => Color.FromArgb(226, 232, 240),
            _       => Color.FromArgb(35, 39, 48) // Dark
        };

        // الخطوط
        public static Font FontMain   = new Font("Segoe UI", 9.5f);
        public static Font FontTitle  = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static Font FontHeader = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static Font FontNormal = new Font("Segoe UI", 10f);
        public static Font FontBold   = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static Font FontSmall  = new Font("Segoe UI", 9f);
        public static Font FontArabic = new Font("Segoe UI", 10f);

        public static void EnableDoubleBuffering(this Control control)
        {
            if (control == null || SystemInformation.TerminalServerSession) return;
            try
            {
                var pi = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pi?.SetValue(control, true, null);
            }
            catch { }
        }

        public static void ApplyRTL(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                c.EnableDoubleBuffering();
                c.RightToLeft = RightToLeft.Yes;

                if (c is FlowLayoutPanel flp)
                {
                    flp.FlowDirection = FlowDirection.RightToLeft;
                }
                else if (c is StatusStrip ss)
                {
                    ss.RightToLeft = RightToLeft.Yes;
                }
                else if (c is ToolStrip ts)
                {
                    ts.RightToLeft = RightToLeft.Yes;
                }
                else if (c is MenuStrip ms)
                {
                    ms.RightToLeft = RightToLeft.Yes;
                }

                if (c is TextBox tb)
                {
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.ForeColor = TextDark;
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
                    cb.ForeColor = TextDark;
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
                    nud.ForeColor = TextDark;
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
                    tc.DrawMode = TabDrawMode.OwnerDrawFixed;
                    tc.SizeMode = TabSizeMode.Fixed;
                    tc.ItemSize = new Size(165, 32);
                    tc.DrawItem -= TabControl_DrawItem;
                    tc.DrawItem += TabControl_DrawItem;
                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.RightToLeft = RightToLeft.Yes;
                        tp.BackColor = BgCard;
                        ApplyRTL(tp.Controls);
                    }
                }
                else if (c is DataGridView dgv)
                {
                    StyleGridHeader(dgv);
                }
                else if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel || c is GroupBox)
                {
                    string n = c.Name?.ToLower() ?? "";
                    string t = c.Tag?.ToString()?.ToLower() ?? "";
                    if (n.StartsWith("pnlfilter") || n.StartsWith("pnlsearch") || n.StartsWith("pnlinfo") ||
                        n.StartsWith("pnlf") || n.StartsWith("pnltop") || n.StartsWith("pnlheader") ||
                        n.StartsWith("grpfilter") || n.StartsWith("grpsearch") || t.Contains("search") || t.Contains("filter") ||
                        c.Dock == DockStyle.Top)
                    {
                        StyleSearchPanel(c);
                    }
                }

                if (c.HasChildren)
                    ApplyRTL(c.Controls);
            }
        }

        public static void StyleSearchPanel(Control container)
        {
            if (container == null) return;

            container.BackColor = BgSearchPanel;
            container.RightToLeft = RightToLeft.Yes;
            if (container is FlowLayoutPanel flpContainer)
            {
                flpContainer.FlowDirection = FlowDirection.RightToLeft;
            }

            foreach (Control c in container.Controls)
            {
                c.RightToLeft = RightToLeft.Yes;
                if (c is FlowLayoutPanel flpChild)
                {
                    flpChild.FlowDirection = FlowDirection.RightToLeft;
                }

                if (c is Label lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    if (lbl.ForeColor != Color.Red && lbl.ForeColor != Color.Green && lbl.ForeColor != Color.OrangeRed)
                    {
                        lbl.ForeColor = Color.FromArgb(255, 220, 110);
                    }
                    lbl.Font = FontBold;
                }
                else if (c is TextBox tb)
                {
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.BackColor = Color.White;
                    tb.ForeColor = Color.FromArgb(15, 23, 42);
                    tb.Font = FontBold;
                }
                else if (c is ComboBox cb)
                {
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.BackColor = Color.White;
                    cb.ForeColor = Color.FromArgb(15, 23, 42);
                    cb.Font = FontBold;
                }
                else if (c is DateTimePicker dtp)
                {
                    dtp.Font = FontBold;
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BorderStyle = BorderStyle.FixedSingle;
                    nud.BackColor = Color.White;
                    nud.ForeColor = Color.FromArgb(15, 23, 42);
                    nud.Font = FontBold;
                }
                else if (c is CheckBox chk)
                {
                    chk.BackColor = Color.Transparent;
                    chk.ForeColor = Color.White;
                    chk.Font = FontBold;
                }
                else if (c is RadioButton rb)
                {
                    rb.BackColor = Color.Transparent;
                    rb.ForeColor = Color.White;
                    rb.Font = FontBold;
                }
                else if (c is Button btn)
                {
                    if (btn.BackColor == BgCard || btn.BackColor == SystemColors.Control || btn.BackColor == Color.Empty || btn.BackColor == Color.White)
                    {
                        btn.BackColor = Accent;
                        btn.ForeColor = Color.White;
                    }
                }

                if (c.HasChildren)
                {
                    StyleSearchPanel(c);
                }
            }
        }

        public static void ApplyGridTheme(DataGridView grid)
        {
            StyleGridHeader(grid);
        }

        private static readonly Font _gridHeaderFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        public static void StyleGridHeader(DataGridView grid)
        {
            if (grid == null) return;
            grid.EnableDoubleBuffering();
            grid.ColumnHeadersVisible = true;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            if (grid.ColumnHeadersHeight < 28) grid.ColumnHeadersHeight = 28;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = _gridHeaderFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.CellPainting -= HeaderCellPainting;
            grid.CellPainting += HeaderCellPainting;
        }

        private static void HeaderCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= 0 && sender is DataGridView grid)
            {
                e.PaintBackground(e.CellBounds, true);
                using (var b = new LinearGradientBrush(e.CellBounds, Color.FromArgb(41, 60, 88), Color.FromArgb(24, 38, 60), 90f))
                {
                    e.Graphics.FillRectangle(b, e.CellBounds);
                }
                using (var pen = new Pen(Color.FromArgb(70, 90, 120)))
                {
                    e.Graphics.DrawRectangle(pen, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
                }
                string headerText = grid.Columns[e.ColumnIndex].HeaderText;
                TextRenderer.DrawText(e.Graphics, headerText, _gridHeaderFont,
                    e.CellBounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                e.Handled = true;
            }
        }

        /// <summary>يطبق RTL كاملاً على الـ Form بكل محتوياته</summary>
        public static void ApplyFormRTL(Form form)
        {
            if (form == null) return;
            form.EnableDoubleBuffering();
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

        /// <summary>تصميم زر مع موقع وحجم محددين</summary>
        public static Button MakeButton(string text, Color backColor, Point location, Size size)
        {
            var btn = MakeButton(text, backColor);
            btn.Location = location;
            btn.Size = size;
            return btn;
        }

        /// <summary>رسم إطار الكارت</summary>
        public static void DrawCardBorder(Graphics g, Control ctrl)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(BorderColor, 1.5f))
            {
                g.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
            }
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
            grid.DefaultCellStyle.ForeColor = TextDark;
            grid.DefaultCellStyle.BackColor = BgWhite;
            grid.DefaultCellStyle.SelectionBackColor = Primary;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersHeight = 28;
            grid.RowTemplate.Height = 24;
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

        /// <summary>لوحة عنوان الشاشة (مدمجة وموفرة للمساحة واليمين 100%)</summary>
        public static Panel MakeTitleBar(string title, string subtitle = "")
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = string.IsNullOrEmpty(subtitle) ? 30 : 42,
                BackColor = BgCard,
                Padding = new Padding(12, 3, 12, 3),
                RightToLeft = RightToLeft.Yes
            };

            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    ForeColor = TextSub,
                    Dock = DockStyle.Top,
                    Height = 18,
                    RightToLeft = RightToLeft.Yes,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panel.Controls.Add(sub);
            }

            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextMain,
                Dock = DockStyle.Top,
                Height = 22,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(lbl);

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

        public static void EnableDoubleBuffer(Control control)
        {
            try
            {
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.SetProperty,
                    null, control, new object[] { true });
            }
            catch { }
        }

        public static void AdjustGridHeaders(DataGridView grid)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name == "PartNumber")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Mobiles"   => "كود الموديل",
                        "Clothing"  => "كود الموديل",
                        "SpareParts" => "رقم القطعة",
                        _           => "كود الصنف"
                    };
                }
                else if (col.Name == "CarModel")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Mobiles"   => "التوافق",
                        _           => "الموديل"
                    };
                }
                else if (col.Name == "Brand")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Mobiles"   => "الماركة",
                        _           => "الماركة"
                    };
                }
                else if (col.Name == "ShelfLocation")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Clothing"  => "مكان العرض",
                        _           => "الرف"
                    };
                }
                else if (col.Name == "ProducerCompany" || col.Name == "ProducerName")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Mobiles"   => "الشركة المصنعة",
                        "Clothing"  => "الخامة",
                        _           => "الشركة المنتجة"
                    };
                }
                else if (col.Name == "ProductName")
                {
                    col.HeaderText = AppConfig.BusinessType switch
                    {
                        "Mobiles"   => "الجهاز / الصنف",
                        "Clothing"  => "القطعة / الصنف",
                        _           => "اسم الصنف"
                    };
                }
            }

            // ضبط الأعمدة المخصصة لكل نشاط لتجنب التكرار
            bool isClothing = AppConfig.BusinessType == "Clothing";

            // في نشاط الملابس: أخفِ CarModel/Brand (كانا يعرضان المقاس/اللون القديم)
            // وأظهر ProductSize/Color الجديدين المنفصلين
            if (grid.Columns.Contains("CarModel"))
                grid.Columns["CarModel"].Visible = !isClothing;
            if (grid.Columns.Contains("Brand"))
                grid.Columns["Brand"].Visible = !isClothing;
            if (grid.Columns.Contains("ProductSize"))
                grid.Columns["ProductSize"].Visible = isClothing;
            if (grid.Columns.Contains("Color"))
                grid.Columns["Color"].Visible = isClothing;
        }

        private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (sender is TabControl tc && e.Index >= 0 && e.Index < tc.TabPages.Count)
                {
                    TabPage tp = tc.TabPages[e.Index];
                    Rectangle tabRect = tc.GetTabRect(e.Index);
                    bool isSelected = tc.SelectedIndex == e.Index;

                    Color backColor = isSelected ? Primary : Color.FromArgb(55, 65, 81);
                    Color foreColor = isSelected ? Color.White : Color.FromArgb(200, 205, 215);

                    if (AppConfig.AppTheme == "Light")
                    {
                        backColor = isSelected ? Primary : Color.FromArgb(230, 235, 245);
                        foreColor = isSelected ? Color.White : Color.FromArgb(70, 80, 95);
                    }
                    else if (AppConfig.AppTheme == "Slate")
                    {
                        backColor = isSelected ? Accent : Color.FromArgb(203, 213, 225);
                        foreColor = isSelected ? Color.White : Color.FromArgb(50, 60, 75);
                    }

                    using (var brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, tabRect);
                    }

                    if (isSelected)
                    {
                        using (var pen = new Pen(Accent, 3f))
                        {
                            e.Graphics.DrawLine(pen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
                        }
                    }
                    else
                    {
                        using (var pen = new Pen(BorderColor, 1f))
                        {
                            e.Graphics.DrawRectangle(pen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
                        }
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        tp.Text,
                        new Font(FontMain.FontFamily, 9.5f, FontStyle.Bold),
                        tabRect,
                        foreColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                    );
                }
            }
            catch { }
        }

        /// <summary>إظهار رسائل تنبيه بتنسيق RTL ومحاذاة عربية صحيحة دائماً</summary>
        public static DialogResult ShowMsg(string text, string title = "تنبيه", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            return MessageBox.Show(text, title, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }
    }
}
