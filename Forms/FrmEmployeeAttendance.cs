using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة الحضور والانصراف الشاملة وإدارة دوام الموظفين
    /// </summary>
    public class FrmEmployeeAttendance : Form
    {
        private DateTimePicker dtpAttendDay;
        private DataGridView dgDailyAttendance;
        private Button btnSaveDaily, btnCheckInAll, btnRefresh;

        // تبويب التقارير
        private DateTimePicker dtpRepFrom, dtpRepTo;
        private ComboBox cboRepEmp, cboRepStatus;
        private DataGridView dgRep;
        private Button btnLoadRep, btnPrintRep;
        private Label lblTotalDays, lblTotalLate, lblTotalOvertime;

        private TabControl tabControl;

        public FrmEmployeeAttendance()
        {
            InitUI();
            LoadDailyData();
            LoadReportEmployees();
        }

        private void InitUI()
        {
            this.Text = "🕒 مديول الحضور والانصراف وإدارة الدوام";
            this.Size = new Size(1180, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // شريط العنوان العلوي
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "🕒 إدارة الحضور والانصراف وساعات العمل والغياب",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12)
            };
            pnlTop.Controls.Add(lblTitle);

            // التبويبات
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            var tabDaily = new TabPage("📅 تسجيل الحضور والانصراف اليومي");
            var tabReports = new TabPage("📊 تقرير الحضور والغياب والتأخيرات");

            BuildDailyTab(tabDaily);
            BuildReportsTab(tabReports);

            tabControl.TabPages.Add(tabDaily);
            tabControl.TabPages.Add(tabReports);
            Theme.StyleTabControl(tabControl);

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlTop);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الأول: التسجيل اليومي
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildDailyTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;
            var pnlToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblDate = new Label { Text = "📅 تاريخ اليوم:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            dtpAttendDay = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 140,
                Font = new Font("Segoe UI", 10.5f)
            };
            dtpAttendDay.ValueChanged += (s, e) => LoadDailyData();

            btnRefresh = Theme.MakeButton("🔄 تحديث", 0, 0, 95, 34, Theme.Secondary);
            btnRefresh.Click += (s, e) => LoadDailyData();

            btnCheckInAll = Theme.MakeButton("⚡ تحضير الكل وفق الورديات", 0, 0, 180, 34, Color.FromArgb(16, 149, 193));
            btnCheckInAll.Click += (s, e) =>
            {
                foreach (DataGridViewRow row in dgDailyAttendance.Rows)
                {
                    row.Cells["Status"].Value = "حاضر";
                    if (row.Tag is ShiftInfo sInfo)
                    {
                        if (row.Cells["CheckInTime"].Value == null || string.IsNullOrWhiteSpace(row.Cells["CheckInTime"].Value.ToString()))
                        {
                            row.Cells["CheckInTime"].Value = FormatDisplayTime(sInfo.StartTime);
                        }
                        if (row.Cells["CheckOutTime"].Value == null || string.IsNullOrWhiteSpace(row.Cells["CheckOutTime"].Value.ToString()))
                        {
                            row.Cells["CheckOutTime"].Value = FormatDisplayTime(sInfo.EndTime);
                        }
                    }
                    RecalculateRowMetrics(row);
                }
            };

            var btnAutoCalc = Theme.MakeButton("⚡ احتساب التأخيرات وساعات العمل", 0, 0, 220, 34, Color.FromArgb(217, 119, 6));
            btnAutoCalc.Click += (s, e) =>
            {
                dgDailyAttendance.EndEdit();
                foreach (DataGridViewRow row in dgDailyAttendance.Rows)
                {
                    RecalculateRowMetrics(row);
                }
                MessageBox.Show("✅ تم احتساب دقائق التأخير والانصراف وساعات العمل والإضافي لجميع الموظفين وفق وردياتهم الرسمية.", "تم الاحتساب", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnSaveDaily = Theme.MakeButton("💾 حفظ واعتماد يومية الحضور", 0, 0, 210, 34, Theme.Success);
            btnSaveDaily.Click += BtnSaveDaily_Click;

            pnlToolbar.Controls.AddRange(new Control[] { lblDate, dtpAttendDay, btnRefresh, btnCheckInAll, btnAutoCalc, btnSaveDaily });

            // الجدول اليومي
            dgDailyAttendance = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Color.FromArgb(224, 242, 254), SelectionForeColor = Color.Black },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 }
            };

            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpID", Visible = false });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "اسم الموظف", ReadOnly = true, FillWeight = 105 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobTitle", HeaderText = "الوظيفة / الدور", ReadOnly = true, FillWeight = 75 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShiftSchedule", HeaderText = "الدوام المقرر", ReadOnly = true, FillWeight = 85 });

            var colStatus = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "حالة الحضور",
                FillWeight = 75
            };
            colStatus.Items.AddRange(new object[] { "حاضر", "متأخر", "غائب", "إجازة", "نصف يوم", "مأمورية" });
            dgDailyAttendance.Columns.Add(colStatus);

            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckInTime", HeaderText = "وقت الحضور", FillWeight = 70 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckOutTime", HeaderText = "وقت الانصراف", FillWeight = 70 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "LateMinutes", HeaderText = "تأخير (د)", FillWeight = 55 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "EarlyLeaveMinutes", HeaderText = "خروج مبكر (د)", FillWeight = 60 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkHours", HeaderText = "ساعات العمل", FillWeight = 60 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "OvertimeHours", HeaderText = "إضافي (س)", FillWeight = 55 });
            dgDailyAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 110 });

            // أزرار سريعة
            var btnColIn = new DataGridViewButtonColumn { Name = "BtnIn", HeaderText = "", Text = "🟢 حضور", UseColumnTextForButtonValue = true, FillWeight = 55 };
            var btnColOut = new DataGridViewButtonColumn { Name = "BtnOut", HeaderText = "", Text = "🔴 انصراف", UseColumnTextForButtonValue = true, FillWeight = 55 };
            dgDailyAttendance.Columns.Add(btnColIn);
            dgDailyAttendance.Columns.Add(btnColOut);

            dgDailyAttendance.CellClick += DgDailyAttendance_CellClick;
            dgDailyAttendance.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    RecalculateRowMetrics(dgDailyAttendance.Rows[e.RowIndex]);
                }
            };

            tab.Controls.Add(dgDailyAttendance);
            tab.Controls.Add(pnlToolbar);
        }

        private class ShiftInfo
        {
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public int GraceMinutes { get; set; }
            public decimal DailyHours { get; set; }
        }

        private string FormatDisplayTime(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return "";
            if (DateTime.TryParse(timeStr, out DateTime dt)) return dt.ToString("hh:mm tt");
            if (TimeSpan.TryParse(timeStr, out TimeSpan ts)) return DateTime.Today.Add(ts).ToString("hh:mm tt");
            return timeStr;
        }

        private void RecalculateRowMetrics(DataGridViewRow row)
        {
            if (row == null || !(row.Tag is ShiftInfo sInfo)) return;
            DateTime targetDate = dtpAttendDay.Value.Date;

            string inStr = row.Cells["CheckInTime"].Value?.ToString();
            string outStr = row.Cells["CheckOutTime"].Value?.ToString();

            DateTime? inTime = null;
            DateTime? outTime = null;

            if (!string.IsNullOrWhiteSpace(inStr) && DateTime.TryParse(inStr, out DateTime dtIn))
            {
                inTime = targetDate.Add(dtIn.TimeOfDay);
            }
            if (!string.IsNullOrWhiteSpace(outStr) && DateTime.TryParse(outStr, out DateTime dtOut))
            {
                outTime = targetDate.Add(dtOut.TimeOfDay);
            }

            var metrics = EmployeeHRDAL.CalculateAttendanceMetrics(sInfo.StartTime, sInfo.EndTime, sInfo.DailyHours, sInfo.GraceMinutes, inTime, outTime);

            row.Cells["LateMinutes"].Value = metrics.lateMinutes.ToString();
            row.Cells["EarlyLeaveMinutes"].Value = metrics.earlyLeaveMinutes.ToString();
            row.Cells["WorkHours"].Value = metrics.workHours.ToString("N2");
            row.Cells["OvertimeHours"].Value = metrics.overtimeHours.ToString("N2");

            string currentStatus = row.Cells["Status"].Value?.ToString() ?? "حاضر";
            if (currentStatus == "حاضر" && metrics.lateMinutes > sInfo.GraceMinutes)
            {
                row.Cells["Status"].Value = "متأخر";
                currentStatus = "متأخر";
            }

            ApplyRowStyling(row, currentStatus, metrics.lateMinutes);
        }

        private void ApplyRowStyling(DataGridViewRow row, string status, int lateMinutes)
        {
            if (status == "غائب") row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
            else if (status == "إجازة") row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
            else if (status == "نصف يوم") row.DefaultCellStyle.BackColor = Color.FromArgb(224, 231, 255);
            else if (status == "متأخر" || lateMinutes > 0) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 237, 213);
            else row.DefaultCellStyle.BackColor = Theme.BgCard;
        }

        private void LoadDailyData()
        {
            var dt = EmployeeHRDAL.GetDailyAttendanceGrid(dtpAttendDay.Value);
            dgDailyAttendance.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                int empId = Convert.ToInt32(r["EmpID"]);
                string name = r["EmpName"]?.ToString() ?? "";
                string job = r["JobTitle"]?.ToString();
                if (string.IsNullOrWhiteSpace(job)) job = r["Role"]?.ToString() ?? "";

                string schedStart = r["WorkStartTime"]?.ToString() ?? "09:00";
                string schedEnd = r["WorkEndTime"]?.ToString() ?? "17:00";
                int grace = Convert.ToInt32(r["GracePeriodMinutes"]);
                decimal dwh = Convert.ToDecimal(r["DailyWorkHours"]);

                string shiftText = $"{FormatDisplayTime(schedStart)} - {FormatDisplayTime(schedEnd)}";

                string st = r["Status"]?.ToString() ?? "حاضر";
                string inTime = r["CheckInTime"] != DBNull.Value ? Convert.ToDateTime(r["CheckInTime"]).ToString("hh:mm tt") : "";
                string outTime = r["CheckOutTime"] != DBNull.Value ? Convert.ToDateTime(r["CheckOutTime"]).ToString("hh:mm tt") : "";
                decimal wh = Convert.ToDecimal(r["WorkHours"]);
                decimal ot = Convert.ToDecimal(r["OvertimeHours"]);
                int lm = Convert.ToInt32(r["LateMinutes"]);
                int elm = r.Table.Columns.Contains("EarlyLeaveMinutes") && r["EarlyLeaveMinutes"] != DBNull.Value ? Convert.ToInt32(r["EarlyLeaveMinutes"]) : 0;
                string notes = r["Notes"]?.ToString() ?? "";

                int rowIdx = dgDailyAttendance.Rows.Add(empId, name, job, shiftText, st, inTime, outTime, lm.ToString(), elm.ToString(), wh.ToString("N2"), ot.ToString("N2"), notes);

                var row = dgDailyAttendance.Rows[rowIdx];
                row.Tag = new ShiftInfo
                {
                    StartTime = schedStart,
                    EndTime = schedEnd,
                    GraceMinutes = grace,
                    DailyHours = dwh
                };

                ApplyRowStyling(row, st, lm);
            }
        }

        private void DgDailyAttendance_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var r = dgDailyAttendance.Rows[e.RowIndex];
            int empId = Convert.ToInt32(r.Cells["EmpID"].Value);

            if (dgDailyAttendance.Columns[e.ColumnIndex].Name == "BtnIn")
            {
                EmployeeHRDAL.QuickCheckIn(empId, DateTime.Now);
                LoadDailyData();
            }
            else if (dgDailyAttendance.Columns[e.ColumnIndex].Name == "BtnOut")
            {
                EmployeeHRDAL.QuickCheckOut(empId, DateTime.Now);
                LoadDailyData();
            }
        }

        private void BtnSaveDaily_Click(object sender, EventArgs e)
        {
            dgDailyAttendance.EndEdit();
            int savedCount = 0;
            DateTime targetDate = dtpAttendDay.Value.Date;

            foreach (DataGridViewRow r in dgDailyAttendance.Rows)
            {
                int empId = Convert.ToInt32(r.Cells["EmpID"].Value);
                string st = r.Cells["Status"].Value?.ToString() ?? "حاضر";
                string inStr = r.Cells["CheckInTime"].Value?.ToString();
                string outStr = r.Cells["CheckOutTime"].Value?.ToString();

                DateTime? checkIn = null;
                DateTime? checkOut = null;
                if (!string.IsNullOrWhiteSpace(inStr) && DateTime.TryParse(inStr, out DateTime inDt))
                {
                    checkIn = targetDate.Add(inDt.TimeOfDay);
                }
                if (!string.IsNullOrWhiteSpace(outStr) && DateTime.TryParse(outStr, out DateTime outDt))
                {
                    checkOut = targetDate.Add(outDt.TimeOfDay);
                }

                decimal.TryParse(r.Cells["WorkHours"].Value?.ToString(), out decimal wh);
                decimal.TryParse(r.Cells["OvertimeHours"].Value?.ToString(), out decimal ot);
                int.TryParse(r.Cells["LateMinutes"].Value?.ToString(), out int lm);
                int.TryParse(r.Cells["EarlyLeaveMinutes"].Value?.ToString(), out int elm);
                string notes = r.Cells["Notes"].Value?.ToString() ?? "";

                if (EmployeeHRDAL.SaveAttendance(empId, targetDate, checkIn, checkOut, st, wh, ot, lm, notes, elm))
                {
                    savedCount++;
                }
            }

            MessageBox.Show($"✅ تم حفظ بيانات الحضور والانصراف لـ ({savedCount}) موظف بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadDailyData();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // التبويب الثاني: تقارير الحضور والغياب
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildReportsTab(TabPage tab)
        {
            tab.BackColor = Theme.BgMain;

            var pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes
            };

            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(5, 7, 0, 0) };
            dtpRepFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Width = 120 };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0) };
            dtpRepTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 120 };

            var lblEmp = new Label { Text = "الموظف:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0) };
            cboRepEmp = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Font = new Font("Segoe UI", 9.5f) };

            var lblStatus = new Label { Text = "الحالة:", AutoSize = true, ForeColor = Theme.TextMain, Margin = new Padding(10, 7, 0, 0) };
            cboRepStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = new Font("Segoe UI", 9.5f) };
            cboRepStatus.Items.AddRange(new object[] { "الكل", "حاضر", "غائب", "إجازة", "نصف يوم", "مأمورية" });
            cboRepStatus.SelectedIndex = 0;

            btnLoadRep = Theme.MakeButton("🔍 عرض التقرير", 0, 0, 130, 34, Theme.Primary);
            btnLoadRep.Click += (s, e) => LoadReportData();

            btnPrintRep = Theme.MakeButton("🖨️ طباعة", 0, 0, 100, 34, Theme.Secondary);
            btnPrintRep.Click += BtnPrintRep_Click;

            pnlFilters.Controls.AddRange(new Control[] { lblFrom, dtpRepFrom, lblTo, dtpRepTo, lblEmp, cboRepEmp, lblStatus, cboRepStatus, btnLoadRep, btnPrintRep });

            // شريط الإحصائيات أسفل التقرير
            var pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 8, 15, 8),
                RightToLeft = RightToLeft.Yes
            };

            lblTotalDays = new Label { Text = "📊 إجمالي الأيام: 0", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.TextMain, Margin = new Padding(10, 0, 25, 0) };
            lblTotalLate = new Label { Text = "⏰ إجمالي التأخير: 0 دقيقة", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.DarkRed, Margin = new Padding(10, 0, 25, 0) };
            lblTotalOvertime = new Label { Text = "➕ إجمالي الإضافي: 0 ساعة", AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(10, 0, 25, 0) };

            pnlSummary.Controls.AddRange(new Control[] { lblTotalDays, lblTotalLate, lblTotalOvertime });

            // جدول التقرير
            dgRep = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "AttendDate", HeaderText = "التاريخ", FillWeight = 60 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "EmpName", HeaderText = "اسم الموظف", FillWeight = 100 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 50 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckInTime", HeaderText = "وقت الحضور", FillWeight = 60 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckOutTime", HeaderText = "وقت الانصراف", FillWeight = 60 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkHours", HeaderText = "ساعات العمل", FillWeight = 50 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "OvertimeHours", HeaderText = "إضافي", FillWeight = 45 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "LateMinutes", HeaderText = "تأخير (دقيقة)", FillWeight = 50 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "الملاحظات", FillWeight = 110 });
            dgRep.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "المسجل", FillWeight = 60 });

            tab.Controls.Add(dgRep);
            tab.Controls.Add(pnlSummary);
            tab.Controls.Add(pnlFilters);
        }

        private void LoadReportEmployees()
        {
            var dt = EmployeeDAL.GetAll();
            cboRepEmp.Items.Clear();
            cboRepEmp.Items.Add(new ComboItem(0, "-- كل الموظفين --"));
            foreach (DataRow r in dt.Rows)
            {
                cboRepEmp.Items.Add(new ComboItem((int)r["EmpID"], r["EmpName"].ToString()));
            }
            cboRepEmp.DisplayMember = "Text";
            cboRepEmp.SelectedIndex = 0;
        }

        private void LoadReportData()
        {
            int empId = (cboRepEmp.SelectedItem is ComboItem ci) ? ci.ID : 0;
            string st = cboRepStatus.SelectedItem?.ToString() ?? "الكل";

            var dt = EmployeeHRDAL.GetAttendanceReport(empId, dtpRepFrom.Value, dtpRepTo.Value, st);
            dgRep.Rows.Clear();

            int totalLate = 0;
            decimal totalOvertime = 0;

            foreach (DataRow r in dt.Rows)
            {
                string dtStr = Convert.ToDateTime(r["AttendDate"]).ToString("yyyy-MM-dd");
                string name = r["EmpName"]?.ToString() ?? "";
                string status = r["Status"]?.ToString() ?? "";
                string inTime = r["CheckInTime"] != DBNull.Value ? Convert.ToDateTime(r["CheckInTime"]).ToString("hh:mm tt") : "—";
                string outTime = r["CheckOutTime"] != DBNull.Value ? Convert.ToDateTime(r["CheckOutTime"]).ToString("hh:mm tt") : "—";
                decimal wh = Convert.ToDecimal(r["WorkHours"]);
                decimal ot = Convert.ToDecimal(r["OvertimeHours"]);
                int lm = Convert.ToInt32(r["LateMinutes"]);
                string notes = r["Notes"]?.ToString() ?? "";
                string creator = r["CreatedByName"]?.ToString() ?? "";

                totalLate += lm;
                totalOvertime += ot;

                int rowIdx = dgRep.Rows.Add(dtStr, name, status, inTime, outTime, wh.ToString("N2"), ot.ToString("N2"), lm.ToString(), notes, creator);
                var row = dgRep.Rows[rowIdx];
                if (status == "غائب") row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                else if (status == "إجازة") row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
            }

            lblTotalDays.Text = $"📊 إجمالي السجلات: {dgRep.Rows.Count:N0}";
            lblTotalLate.Text = $"⏰ إجمالي التأخير: {totalLate:N0} دقيقة";
            lblTotalOvertime.Text = $"➕ إجمالي الإضافي: {totalOvertime:N2} ساعة";
        }

        private void BtnPrintRep_Click(object sender, EventArgs e)
        {
            if (dgRep.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument doc = new PrintDocument();
            doc.DefaultPageSettings.Landscape = true;
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 10f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString("تقرير الحضور والانصراف والدوام", fontTitle, Brushes.Black, new PointF(ev.PageBounds.Width / 2 - 130, y));
                y += 35;
                g.DrawString($"الفترة من: {dtpRepFrom.Value:yyyy/MM/dd}  إلى: {dtpRepTo.Value:yyyy/MM/dd}", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 110, y));
                y += 35;

                // رسم الجدول
                float[] colWidths = { 90, 160, 80, 80, 80, 70, 70, 70, 160 };
                string[] headers = { "التاريخ", "الموظف", "الحالة", "حضور", "انصراف", "ساعات", "إضافي", "تأخير", "ملاحظات" };

                float x = 40;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightGray, x, y, colWidths[i], 26);
                    g.DrawRectangle(Pens.Gray, x, y, colWidths[i], 26);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 5, y + 4);
                    x += colWidths[i];
                }
                y += 26;

                foreach (DataGridViewRow r in dgRep.Rows)
                {
                    if (y > ev.PageBounds.Height - 60) break;
                    x = 40;
                    string[] vals = {
                        r.Cells["AttendDate"].Value?.ToString() ?? "",
                        r.Cells["EmpName"].Value?.ToString() ?? "",
                        r.Cells["Status"].Value?.ToString() ?? "",
                        r.Cells["CheckInTime"].Value?.ToString() ?? "",
                        r.Cells["CheckOutTime"].Value?.ToString() ?? "",
                        r.Cells["WorkHours"].Value?.ToString() ?? "",
                        r.Cells["OvertimeHours"].Value?.ToString() ?? "",
                        r.Cells["LateMinutes"].Value?.ToString() ?? "",
                        r.Cells["Notes"].Value?.ToString() ?? ""
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 24);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 3, y + 4);
                        x += colWidths[i];
                    }
                    y += 24;
                }
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 950, Height = 650 })
            {
                dlg.ShowDialog(this);
            }
        }
    }
}
