using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تقرير سجل أجهزة البرنامج ومواعيد الفتح والإغلاق.
    /// تسجل دخول وخروج كل جهاز بدقة مع حماية ومراقبة التوقيت الشاذ وتصحيحه.
    /// </summary>
    public class FrmDeviceSessions : Form
    {
        // ─── أدوات الفلترة ───
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cmbMachine, cmbStatus;
        private TextBox txtSearch;
        private Button btnRefresh, btnDeleteSelected, btnCleanAnomalies, btnExportExcel, btnPrint, btnWhatsApp;
        private Button btnToday, btnYesterday, btnThisWeek, btnThisMonth, btnAllDates;

        // ─── بطاقات المؤشرات (KPIs) ───
        private Label lblTotalSessionsVal, lblActiveDevicesVal, lblDistinctMachinesVal, lblTotalHoursVal;

        // ─── شبكة البيانات ───
        private DataGridView dgvSessions;
        private ContextMenuStrip ctxMenu;

        private DataTable _dtRaw;
        private DataView _dvFiltered;

        public FrmDeviceSessions()
        {
            if (!Session.IsAdmin && !Session.CanAccess("DeviceSessions") && !Session.CanAccess("Settings"))
            {
                MessageBox.Show("غير مصرح لك بعرض سجل أجهزة البرنامج.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitUI();
            LoadFilterData();
            LoadSessions();
        }

        private void InitUI()
        {
            this.Text = "سجل أجهزة البرنامج ومواعيد الفتح والإغلاق (حماية التوقيت)";
            this.Size = new Size(1220, 780);
            this.MinimumSize = new Size(980, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. رأس الشاشة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "سجل أجهزة البرنامج ومواعيد الفتح والإغلاق",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblSub = new Label
            {
                Text = "مراقبة توقيت دخول وخروج كل جهاز ولاب توب، وتتبع ساعات العمل، وتصحيح التواريخ الشاذة لتأمين التقفيلات",
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 22
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. بطاقات الإحصائيات (KPI Cards)
            var pnlKPIs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 72,
                ColumnCount = 4,
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Theme.BgMain
            };
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            lblTotalSessionsVal = new Label();
            lblActiveDevicesVal = new Label();
            lblDistinctMachinesVal = new Label();
            lblTotalHoursVal = new Label();

            pnlKPIs.Controls.Add(CreateKPICard("إجمالي الجلسات", lblTotalSessionsVal, Color.FromArgb(59, 130, 246)), 0, 0);
            pnlKPIs.Controls.Add(CreateKPICard("أجهزة متصلة الآن", lblActiveDevicesVal, Color.FromArgb(16, 185, 129)), 1, 0);
            pnlKPIs.Controls.Add(CreateKPICard("عدد الأجهزة المختلفة", lblDistinctMachinesVal, Color.FromArgb(139, 92, 246)), 2, 0);
            pnlKPIs.Controls.Add(CreateKPICard("إجمالي ساعات التشغيل", lblTotalHoursVal, Color.FromArgb(245, 158, 11)), 3, 0);

            // 3. شريط الفلتر
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 52),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = true
            };

            // تاريخ من / إلى
            dtpFrom = new DateTimePicker
            {
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            dtpFrom.ValueChanged += (s, e) => ApplyLocalFilter();

            dtpTo = new DateTimePicker
            {
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpTo.ValueChanged += (s, e) => ApplyLocalFilter();

            pnlFilter.Controls.Add(MakeFilterItem("من تاريخ:", dtpFrom));
            pnlFilter.Controls.Add(MakeFilterItem("إلى تاريخ:", dtpTo));

            // أزرار الفترات السريعة
            btnToday = CreateQuickDateButton("اليوم", () => SetDateRange(DateTime.Today, DateTime.Today));
            btnYesterday = CreateQuickDateButton("أمس", () => SetDateRange(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1)));
            btnThisWeek = CreateQuickDateButton("الأسبوع", () => SetDateRange(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today));
            btnThisMonth = CreateQuickDateButton("الشهر", () => SetDateRange(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today));
            btnAllDates = CreateQuickDateButton("الكل", () => SetDateRange(new DateTime(2024, 1, 1), DateTime.Today.AddDays(1)));

            pnlFilter.Controls.Add(btnToday);
            pnlFilter.Controls.Add(btnYesterday);
            pnlFilter.Controls.Add(btnThisWeek);
            pnlFilter.Controls.Add(btnThisMonth);
            pnlFilter.Controls.Add(btnAllDates);

            // فلتر الجهاز
            cmbMachine = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMachine.SelectedIndexChanged += (s, e) => ApplyLocalFilter();
            pnlFilter.Controls.Add(MakeFilterItem("الجهاز :", cmbMachine));

            // فلتر الحالة
            cmbStatus = new ComboBox { Width = 115, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "الكل", "متصل الآن", "تم الخروج" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.SelectedIndexChanged += (s, e) => ApplyLocalFilter();
            pnlFilter.Controls.Add(MakeFilterItem("الحالة :", cmbStatus));

            // البحث السريع
            txtSearch = new TextBox { Width = 140, Height = 26 };
            txtSearch.TextChanged += (s, e) => ApplyLocalFilter();
            pnlFilter.Controls.Add(MakeFilterItem("بحث:", txtSearch));

            // 4. شريط الأزرار والإجراءات
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 4, 10, 8),
                WrapContents = true
            };

            btnRefresh = CreateActionButton("تحديث السجل", Color.FromArgb(37, 99, 235), (s, e) => LoadSessions());
            btnDeleteSelected = CreateActionButton("حذف الجلسة المحددة (لتصحيح الوقت)", Color.FromArgb(220, 38, 38), (s, e) => DeleteSelectedSession());
            btnCleanAnomalies = CreateActionButton("تنظيف التواريخ الشاذة", Color.FromArgb(180, 83, 9), (s, e) => CleanAnomalies());
            btnExportExcel = CreateActionButton("تصدير إكسيل", Color.FromArgb(5, 150, 105), (s, e) => ExportToExcel());
            btnPrint = CreateActionButton("طباعة", Color.FromArgb(75, 85, 99), (s, e) => PrintReport());
            btnWhatsApp = CreateActionButton("مشاركة واتساب", Color.FromArgb(37, 211, 102), (s, e) => ShareWhatsApp());

            pnlActions.Controls.Add(btnRefresh);
            pnlActions.Controls.Add(btnDeleteSelected);
            pnlActions.Controls.Add(btnCleanAnomalies);
            pnlActions.Controls.Add(btnExportExcel);
            pnlActions.Controls.Add(btnPrint);
            pnlActions.Controls.Add(btnWhatsApp);

            // 5. شبكة البيانات DataGridView
            dgvSessions = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RightToLeft = RightToLeft.Yes,
                RowTemplate = { Height = 34 }
            };

            // تفعيل السلاسة ومنع الوميض
            EnableDoubleBuffer(dgvSessions);

            // تنسيق مظهر الشبكة
            dgvSessions.EnableHeadersVisualStyles = false;
            dgvSessions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvSessions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSessions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvSessions.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSessions.ColumnHeadersHeight = 38;

            dgvSessions.DefaultCellStyle.Font = new Font("Segoe UI", 9.2f);
            dgvSessions.DefaultCellStyle.ForeColor = Theme.TextMain;
            dgvSessions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvSessions.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);

            SetupColumns();

            dgvSessions.CellFormatting += DgvSessions_CellFormatting;

            // قائمة السياق (Right-Click Context Menu)
            ctxMenu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = Theme.FontMain };
            ctxMenu.Items.Add("حذف هذه الجلسة (لتصحيح أي خطأ تاريخ)", null, (s, e) => DeleteSelectedSession());
            ctxMenu.Items.Add("نسخ اسم الجهاز", null, (s, e) => CopyMachineName());
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add("تحديث البيانات", null, (s, e) => LoadSessions());
            dgvSessions.ContextMenuStrip = ctxMenu;

            // تجميع عناصر النموذج
            this.Controls.Add(dgvSessions);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlKPIs);
            this.Controls.Add(pnlHeader);
        }

        private void SetupColumns()
        {
            dgvSessions.Columns.Clear();

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SessionID",
                HeaderText = "#",
                Width = 60,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MachineName",
                HeaderText = "اسم الجهاز",
                Width = 145,
                DefaultCellStyle = { Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EmpName",
                HeaderText = "الموظف",
                Width = 135,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "UserName",
                HeaderText = "اسم الدخول",
                Width = 100,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LoginTime",
                HeaderText = "وقت فتح البرنامج",
                Width = 150,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy-MM-dd   hh:mm tt" }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LogoutDisplay",
                HeaderText = "وقت إغلاق البرنامج",
                Width = 150,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DurationDisplay",
                HeaderText = "مدة التشغيل",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LocalMachineTime",
                HeaderText = "ساعة جهاز اللاب",
                Width = 145,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy-MM-dd   hh:mm tt" }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StatusArabic",
                HeaderText = "الحالة",
                Width = 105,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AppVersion",
                HeaderText = "الإصدار",
                Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IPAddress",
                HeaderText = "عنوان IP",
                Width = 115,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvSessions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Notes",
                HeaderText = "ملاحظات",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
        }

        private void DgvSessions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvSessions.Rows.Count) return;
            var row = dgvSessions.Rows[e.RowIndex];
            var drv = row.DataBoundItem as DataRowView;
            if (drv == null) return;

            string status = drv["StatusArabic"]?.ToString() ?? "";
            DateTime loginDt = drv["LoginTime"] != DBNull.Value ? Convert.ToDateTime(drv["LoginTime"]) : DateTime.MinValue;

            // 1. تمييز الجلسات ذات التاريخ المستقبلي الشاذ باللون الأحمر التحذيري
            if (loginDt > DateTime.Now.AddDays(1))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                return;
            }

            // 2. تمييز الأجهزة المتصلة الآن باللون الأخضر الناعم
            if (status.Contains("متصل الآن"))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(6, 95, 70);
            }
            else
            {
                row.DefaultCellStyle.BackColor = (e.RowIndex % 2 == 0) ? Color.White : Color.FromArgb(248, 250, 252);
                row.DefaultCellStyle.ForeColor = Theme.TextMain;
            }
        }

        private void LoadFilterData()
        {
            try
            {
                cmbMachine.Items.Clear();
                cmbMachine.Items.Add("الكل");
                var machines = DeviceSessionManager.GetAllMachineNames();
                foreach (var m in machines)
                {
                    cmbMachine.Items.Add(m);
                }
                cmbMachine.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmDeviceSessions.LoadFilterData", ex);
            }
        }

        public void LoadSessions()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                _dtRaw = DeviceSessionManager.GetSessionsReport();

                // إضافة أعمدة منسقة للعرض
                if (!_dtRaw.Columns.Contains("LogoutDisplay"))
                    _dtRaw.Columns.Add("LogoutDisplay", typeof(string));

                if (!_dtRaw.Columns.Contains("DurationDisplay"))
                    _dtRaw.Columns.Add("DurationDisplay", typeof(string));

                foreach (DataRow r in _dtRaw.Rows)
                {
                    // وقت الخروج المنسق
                    if (r["LogoutTime"] != DBNull.Value)
                    {
                        DateTime outDt = Convert.ToDateTime(r["LogoutTime"]);
                        r["LogoutDisplay"] = outDt.ToString("yyyy-MM-dd   hh:mm tt");
                    }
                    else
                    {
                        string st = r["StatusArabic"]?.ToString() ?? "";
                        if (st.Contains("متصل الآن"))
                            r["LogoutDisplay"] = "متصل الآن";
                        else
                            r["LogoutDisplay"] = "لم يُسجل خروج";
                    }

                    // مدة الجلسة المنسقة
                    int mins = 0;
                    if (r["DurationMinutes"] != DBNull.Value)
                        mins = Convert.ToInt32(r["DurationMinutes"]);

                    r["DurationDisplay"] = FormatDuration(mins);
                }

                _dvFiltered = _dtRaw.DefaultView;
                ApplyLocalFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب سجل الأجهزة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error("FrmDeviceSessions.LoadSessions", ex);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void ApplyLocalFilter()
        {
            if (_dtRaw == null || _dvFiltered == null) return;

            try
            {
                var sb = new StringBuilder("1=1");

                // تصفية التاريخ
                DateTime f = dtpFrom.Value.Date;
                DateTime t = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);
                sb.AppendFormat(" AND LoginTime >= #{0:yyyy-MM-dd}# AND LoginTime <= #{1:yyyy-MM-dd 23:59:59}#", f, t);

                // تصفية الجهاز
                if (cmbMachine.SelectedIndex > 0 && cmbMachine.SelectedItem != null)
                {
                    string m = cmbMachine.SelectedItem.ToString().Replace("'", "''");
                    sb.AppendFormat(" AND MachineName = '{0}'", m);
                }

                // تصفية الحالة
                if (cmbStatus.SelectedIndex == 1) // متصل الآن
                {
                    sb.Append(" AND StatusArabic LIKE '%متصل الآن%'");
                }
                else if (cmbStatus.SelectedIndex == 2) // تم الخروج
                {
                    sb.Append(" AND NOT (StatusArabic LIKE '%متصل الآن%')");
                }

                // البحث النصي
                string query = txtSearch.Text?.Trim().Replace("'", "''") ?? "";
                if (!string.IsNullOrEmpty(query))
                {
                    sb.AppendFormat(" AND (MachineName LIKE '%{0}%' OR EmpName LIKE '%{0}%' OR UserName LIKE '%{0}%' OR IPAddress LIKE '%{0}%' OR Notes LIKE '%{0}%')", query);
                }

                _dvFiltered.RowFilter = sb.ToString();
                dgvSessions.DataSource = _dvFiltered;

                UpdateKPICards();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmDeviceSessions.ApplyLocalFilter", ex);
            }
        }

        private void UpdateKPICards()
        {
            if (_dvFiltered == null) return;

            int totalSessions = _dvFiltered.Count;
            int activeDevices = 0;
            var machines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long totalMinutes = 0;

            foreach (DataRowView drv in _dvFiltered)
            {
                string st = drv["StatusArabic"]?.ToString() ?? "";
                if (st.Contains("متصل الآن")) activeDevices++;

                string m = drv["MachineName"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(m)) machines.Add(m);

                if (drv["DurationMinutes"] != DBNull.Value)
                {
                    long mCount = Convert.ToInt64(drv["DurationMinutes"]);
                    if (mCount > 0) totalMinutes += mCount;
                }
            }

            lblTotalSessionsVal.Text = totalSessions.ToString("N0");
            lblActiveDevicesVal.Text = activeDevices.ToString("N0");
            lblDistinctMachinesVal.Text = machines.Count.ToString("N0");

            long hours = totalMinutes / 60;
            long remMins = totalMinutes % 60;
            lblTotalHoursVal.Text = $"{hours} س و {remMins} د";
        }

        private void SetDateRange(DateTime from, DateTime to)
        {
            dtpFrom.Value = from;
            dtpTo.Value = to;
            ApplyLocalFilter();
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return "أقل من دقيقة";
            if (minutes < 60) return $"{minutes} دقيقة";
            int h = minutes / 60;
            int m = minutes % 60;
            return m > 0 ? $"{h} س {m} د" : $"{h} ساعة";
        }

        // ══════════════════════════════════════════════
        //  الإجراءات: حذف جلسة وتصحيح الوقت
        // ══════════════════════════════════════════════

        private void DeleteSelectedSession()
        {
            if (dgvSessions.CurrentRow == null)
            {
                MessageBox.Show("يرجى تحديد الجلسة المطلوب حذفها أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = dgvSessions.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int sessionId = Convert.ToInt32(drv["SessionID"]);
            string machine = drv["MachineName"]?.ToString() ?? "";
            string emp = drv["EmpName"]?.ToString() ?? "";
            string loginStr = drv["LoginTime"]?.ToString() ?? "";

            var confirm = MessageBox.Show(
                $"هل أنت متأكد من حذف جلسة الجهاز:\n\nالجهاز: [{machine}]\nالموظف: [{emp}]\nتوقيت الفتح: [{loginStr}]\n\nفائدة هذا الإجراء: حذف أي توقيت خاطئ أو مستقبلي سُجل عن طريق الخطأ لتمكين الجهاز من الدخول بتوقيته الصحيح دون تعليق.",
                "تأكيد حذف جلسة لتصحيح التوقيت",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

            try
            {
                bool ok = DeviceSessionManager.DeleteSession(sessionId);
                if (ok)
                {
                    MessageBox.Show("تم حذف الجلسة بنجاح! يمكن الآن ضبط ساعة الجهاز والدخول مجدداً.", "تم بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSessions();
                }
                else
                {
                    MessageBox.Show("فشل حذف الجلسة من قاعدة البيانات.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحذف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanAnomalies()
        {
            var confirm = MessageBox.Show(
                "هل تريد فحص وحذف جميع الجلسات المسجلة بتواريخ مستقبلية شاذة بنقرة واحدة؟\n\nسيقوم البرنامج بحذف أي جلسة مسجلة بعد تاريخ الغد لإزالة أي شذوذ زمني مسجل في الشبكة.",
                "تأكيد تنظيف التواريخ الشاذة",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int count = DeviceSessionManager.CleanFutureAnomalies();
                MessageBox.Show($"تم تنظيف السجلات وحذف ({count}) سجل زمني شاذ.", "تم التنظيف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSessions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تنظيف السجلات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyMachineName()
        {
            if (dgvSessions.CurrentRow == null) return;
            var drv = dgvSessions.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;
            string mach = drv["MachineName"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(mach))
            {
                Clipboard.SetText(mach);
                MessageBox.Show($"تم نسخ اسم الجهاز: {mach}", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ══════════════════════════════════════════════
        //  تصدير إكسيل وطباعة ومشاركة واتساب
        // ══════════════════════════════════════════════

        private void ExportToExcel()
        {
            if (dgvSessions.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "تصدير سجل أجهزة البرنامج إلى Excel";
                dlg.FileName = $"DeviceSessions_{DateTime.Now:yyyyMMdd_HHmm}.xls";
                dlg.Filter = "Excel Files (*.xls)|*.xls|All Files (*.*)|*.*";
                dlg.DefaultExt = "xls";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FrmReports.ExportDataGridViewToXls(dgvSessions, dlg.FileName, "سجل مواعيد أجهزة البرنامج", AppConfig.CompanyName);

                        var res = MessageBox.Show("تم تصدير التقرير إلى إكسيل بنجاح!\nهل تريد فتح الملف الآن؟", "تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل التصدير:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void PrintReport()
        {
            if (dgvSessions.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                pd.DefaultPageSettings.Landscape = true;
                pd.DefaultPageSettings.Margins = new Margins(25, 25, 30, 30);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);

                int rowIndex = 0;
                int pageNum = 0;

                pd.PrintPage += (s, ev) =>
                {
                    pageNum++;
                    RenderPrintPage(ev.Graphics, ev.MarginBounds, ref rowIndex, pageNum);
                    ev.HasMorePages = (rowIndex < dgvSessions.Rows.Count);
                };

                using (var dlg = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 720, StartPosition = FormStartPosition.CenterScreen })
                {
                    ((Form)dlg).Text = "معاينة طباعة سجل أجهزة البرنامج ومواعيد الفتح والإغلاق";
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إعداد الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderPrintPage(Graphics g, Rectangle bounds, ref int rowIndex, int pageNum)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var fTitle = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var fSub = new Font("Segoe UI", 9.5f))
            using (var fHeader = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fRow = new Font("Segoe UI", 8.5f))
            using (var pBorder = new Pen(Color.FromArgb(203, 213, 225), 1))
            using (var bHeader = new SolidBrush(Color.FromArgb(241, 245, 249)))
            using (var bText = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                int y = bounds.Top;

                // رأس الصفحة الأولى فقط
                if (pageNum == 1)
                {
                    string title = $"تقرير سجل أجهزة البرنامج ومواعيد الفتح والإغلاق - {AppConfig.CompanyName}";
                    g.DrawString(title, fTitle, bText, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 28;

                    string sub = $"الفترة من: {dtpFrom.Value:yyyy/MM/dd} إلى: {dtpTo.Value:yyyy/MM/dd}  |  تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}  |  إجمالي الجلسات: {_dvFiltered.Count}";
                    g.DrawString(sub, fSub, Brushes.Gray, bounds.Right, y, new StringFormat { Alignment = StringAlignment.Far });
                    y += 26;
                }

                // رسم ترويسة الجدول
                int[] colWidths = { 45, 140, 130, 95, 140, 140, 95, 95, 100 };
                string[] headers = { "#", "الجهاز", "الموظف", "اسم الدخول", "وقت الفتح", "وقت الإغلاق", "المدة", "الحالة", "عنوان IP" };

                int x = bounds.Right;
                int rowH = 26;

                for (int i = 0; i < headers.Length; i++)
                {
                    int w = colWidths[i];
                    Rectangle r = new Rectangle(x - w, y, w, rowH);
                    g.FillRectangle(bHeader, r);
                    g.DrawRectangle(pBorder, r);
                    g.DrawString(headers[i], fHeader, bText, r, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    x -= w;
                }
                y += rowH;

                // رسم صفوف البيانات
                while (rowIndex < dgvSessions.Rows.Count)
                {
                    if (y + rowH > bounds.Bottom - 30)
                    {
                        break; // صفحة جديدة
                    }

                    var row = dgvSessions.Rows[rowIndex];
                    var drv = row.DataBoundItem as DataRowView;

                    x = bounds.Right;

                    string id = drv["SessionID"]?.ToString() ?? "";
                    string mach = drv["MachineName"]?.ToString() ?? "";
                    string emp = drv["EmpName"]?.ToString() ?? "";
                    string usr = drv["UserName"]?.ToString() ?? "";
                    string login = drv["LoginTime"] != DBNull.Value ? Convert.ToDateTime(drv["LoginTime"]).ToString("yyyy/MM/dd hh:mm tt") : "";
                    string logout = drv["LogoutDisplay"]?.ToString() ?? "";
                    string dur = drv["DurationDisplay"]?.ToString() ?? "";
                    string st = drv["StatusArabic"]?.ToString() ?? "";
                    string ip = drv["IPAddress"]?.ToString() ?? "";

                    string[] vals = { id, mach, emp, usr, login, logout, dur, st, ip };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        int w = colWidths[i];
                        Rectangle r = new Rectangle(x - w, y, w, rowH);
                        if (rowIndex % 2 == 1)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), r);
                        }
                        g.DrawRectangle(pBorder, r);
                        g.DrawString(vals[i], fRow, bText, r, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                        x -= w;
                    }

                    y += rowH;
                    rowIndex++;
                }

                // رقم الصفحة في التذييل
                string footer = $"صفحة {pageNum}";
                g.DrawString(footer, fSub, Brushes.Gray, bounds.Left, bounds.Bottom - 20, new StringFormat { Alignment = StringAlignment.Near });
            }
        }

        private void ShareWhatsApp()
        {
            if (_dvFiltered == null || _dvFiltered.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للمشاركة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"*تقرير أجهزة البرنامج ومواعيد الفتح والإغلاق*");
                sb.AppendLine($"المؤسسة: {AppConfig.CompanyName}");
                sb.AppendLine($"التاريخ: {DateTime.Now:yyyy/MM/dd hh:mm tt}");
                sb.AppendLine($"الأجهزة المتصلة الآن: {lblActiveDevicesVal.Text}");
                sb.AppendLine($"إجمالي الأجهزة المختلفة: {lblDistinctMachinesVal.Text}");
                sb.AppendLine($"إجمالي ساعات التشغيل: {lblTotalHoursVal.Text}");
                sb.AppendLine("────────────────────");
                sb.AppendLine("*الأجهزة النشطة حالياً:*");

                int count = 0;
                foreach (DataRowView drv in _dvFiltered)
                {
                    string st = drv["StatusArabic"]?.ToString() ?? "";
                    if (st.Contains("متصل الآن"))
                    {
                        count++;
                        sb.AppendLine($"• جهاز: {drv["MachineName"]} | الموظف: {drv["EmpName"]} | فتح: {Convert.ToDateTime(drv["LoginTime"]):hh:mm tt}");
                        if (count >= 15) break;
                    }
                }

                if (count == 0)
                {
                    sb.AppendLine("لا توجد أجهزة متصلة حالياً.");
                }

                string url = "https://api.whatsapp.com/send?text=" + Uri.EscapeDataString(sb.ToString());
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل فتح واتساب:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════
        //  أدوات مساعدة لتصميم الواجهة
        // ══════════════════════════════════════════════

        private Panel CreateKPICard(string title, Label lblVal, Color accentColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(4),
                Padding = new Padding(10, 6, 10, 6)
            };

            var bar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 4,
                BackColor = accentColor
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblVal.Text = "0";
            lblVal.Dock = DockStyle.Fill;
            lblVal.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblVal.ForeColor = accentColor;
            lblVal.TextAlign = ContentAlignment.MiddleRight;

            pnl.Controls.Add(lblVal);
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(bar);

            return pnl;
        }

        private Panel MakeFilterItem(string label, Control ctrl)
        {
            var pnl = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 2, 4, 2),
                Padding = new Padding(0),
                WrapContents = false
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(2, 6, 2, 0)
            };

            ctrl.Margin = new Padding(1, 2, 3, 2);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(ctrl);
            return pnl;
        }

        private Button CreateQuickDateButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f),
                Margin = new Padding(2, 2, 2, 2)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Button CreateActionButton(string text, Color bg, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                Margin = new Padding(3, 2, 3, 2),
                Padding = new Padding(8, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private static void EnableDoubleBuffer(DataGridView dgv)
        {
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                    null, dgv, new object[] { true });
            }
            catch { }
        }
    }
}
