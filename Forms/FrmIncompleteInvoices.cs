using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة استرجاع الفواتير وعمليات الجرد غير المكتملة بسبب فصل الجهاز المفاجئ أو انقطاع الكهرباء
    /// </summary>
    public class FrmIncompleteInvoices : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboType;
        private TextBox txtSearch;
        private Button btnSearch, btnRestore, btnDelete, btnPrint;

        private DataGridView dgDrafts;
        private DataGridView dgItems;
        private Label lblDraftsCount, lblItemsTitle;

        private DataTable _dtDrafts;
        private string _initialType = "";
        
        // Property for caller to receive the selected restored draft
        public DataRow SelectedDraftRow { get; private set; }
        public string SelectedDraftType { get; private set; }
        public string SelectedDraftJson { get; private set; }
        public int SelectedDraftID { get; private set; }
        public bool IsRestored { get; private set; } = false;

        public FrmIncompleteInvoices(string initialType = "")
        {
            _initialType = initialType;
            InitUI();
            LoadDrafts();
        }

        private void InitUI()
        {
            this.Text = "📂 فواتير وعمليات غير مكتملة (المحفوظة تلقائياً قبل انقطاع الكهرباء أو إغلاق الجهاز)";
            this.Size = new Size(1200, 720);
            this.MinimumSize = new Size(950, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── 1. Header Bar ──
            var pnlTitle = Theme.MakeTitleBar("📂 فواتير وعمليات غير مكتملة", "يعرض النظام تلقائياً كافة فواتير البيع والمشتريات وجلسات الجرد التي تم حفظها لحظياً ولم تكتمل بسبب فصل الجهاز أو انقطاع الكهرباء.");

            // ── 2. Top Filter Bar ──
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 8, 10, 8),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            pnlFilter.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(5, 8, 2, 0) });
            dtpFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            dtpTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlFilter.Controls.Add(dtpTo);

            pnlFilter.Controls.Add(new Label { Text = "نوع العملية:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            cboType = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, FlatStyle = FlatStyle.Flat };
            cboType.Items.AddRange(new object[] { "--- كل العمليات ---", "🛒 فواتير مبيعات", "🖥️ نقطة بيع POS", "📦 فواتير مشتريات", "📋 عمليات جرد مخزني" });
            
            if (_initialType == "Sale") cboType.SelectedIndex = 1;
            else if (_initialType == "POS") cboType.SelectedIndex = 2;
            else if (_initialType == "Purchase") cboType.SelectedIndex = 3;
            else if (_initialType == "Inventory") cboType.SelectedIndex = 4;
            else cboType.SelectedIndex = 0;

            pnlFilter.Controls.Add(cboType);

            pnlFilter.Controls.Add(new Label { Text = "بحث:", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(12, 8, 2, 0) });
            txtSearch = new TextBox { Width = 150, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadDrafts(); };
            pnlFilter.Controls.Add(txtSearch);

            btnSearch = Theme.MakeButton("🔍 بحث", Theme.Primary);
            btnSearch.Size = new Size(95, 32);
            btnSearch.Click += (s, e) => LoadDrafts();
            pnlFilter.Controls.Add(btnSearch);

            btnRestore = Theme.MakeButton("📂 استرجاع وتحميل الفاتورة", Color.FromArgb(39, 174, 96));
            btnRestore.Size = new Size(200, 32);
            btnRestore.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnRestore.Click += BtnRestore_Click;
            pnlFilter.Controls.Add(btnRestore);

            btnDelete = Theme.MakeButton("🗑️ حذف المسودة", Color.FromArgb(192, 57, 43));
            btnDelete.Size = new Size(130, 32);
            btnDelete.Click += BtnDelete_Click;
            pnlFilter.Controls.Add(btnDelete);

            // ── 3. SplitContainer ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250,
                BackColor = Color.FromArgb(210, 215, 225),
                Panel1 = { BackColor = Theme.BgMain },
                Panel2 = { BackColor = Theme.BgMain }
            };

            // Upper Panel: Master Drafts List
            var pnlUpperHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgCard, Padding = new Padding(10, 5, 10, 0) };
            lblDraftsCount = new Label { Text = "📋 قائمة الفواتير والعمليات غير المكتملة:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Accent };
            pnlUpperHeader.Controls.Add(lblDraftsCount);

            dgDrafts = MakeGrid();
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "DraftID",     Visible = false });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "DraftType",   HeaderText = "نوع العملية",        FillWeight = 55 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "UpdatedAt",   HeaderText = "تاريخ ووقت الحفظ",    FillWeight = 65 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetName",  HeaderText = "العميل / المورد / المخزن", FillWeight = 90 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvoiceType", HeaderText = "نوع الفاتورة",      FillWeight = 45 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemCount",   HeaderText = "عدد الأصناف",        FillWeight = 40 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAmount", HeaderText = "إجمالي المبلغ",     FillWeight = 45 });
            dgDrafts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy",   HeaderText = "المستخدم",           FillWeight = 50 });
            Theme.AdjustGridHeaders(dgDrafts);

            dgDrafts.SelectionChanged += DgDrafts_SelectionChanged;
            dgDrafts.DoubleClick += (s, e) => BtnRestore_Click(null, null);

            split.Panel1.Controls.Add(dgDrafts);
            split.Panel1.Controls.Add(pnlUpperHeader);

            // Lower Panel: Detail Items List
            var pnlLowerHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgCard, Padding = new Padding(10, 5, 10, 0) };
            lblItemsTitle = new Label { Text = "🔍 محتويات وأصناف العملية المحددة:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Primary };
            pnlLowerHeader.Controls.Add(lblItemsTitle);

            dgItems = MakeGrid();
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الباركود / الكود", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف",       FillWeight = 100 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",        HeaderText = "الوحدة",          FillWeight = 35 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity",    HeaderText = "الكمية / الفعلي", FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice",   HeaderText = "السعر / الدفتري", FillWeight = 40 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineTotal",   HeaderText = "الإجمالي / الفارق",FillWeight = 45 });
            Theme.AdjustGridHeaders(dgItems);

            split.Panel2.Controls.Add(dgItems);
            split.Panel2.Controls.Add(pnlLowerHeader);

            this.Controls.Add(split);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlTitle);
        }

        private DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(220, 225, 230),
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain, Alignment = DataGridViewContentAlignment.MiddleCenter },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(246, 248, 250), ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain, Alignment = DataGridViewContentAlignment.MiddleCenter },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleCenter },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Theme.EnableDoubleBuffer(g);
            return g;
        }

        public void LoadDrafts()
        {
            dgDrafts.Rows.Clear();
            dgItems.Rows.Clear();

            string draftType = "ALL";
            if (cboType.SelectedIndex == 1) draftType = "Sale";
            else if (cboType.SelectedIndex == 2) draftType = "POS";
            else if (cboType.SelectedIndex == 3) draftType = "Purchase";
            else if (cboType.SelectedIndex == 4) draftType = "Inventory";

            _dtDrafts = DraftManager.GetIncompleteDrafts(draftType, dtpFrom.Value, dtpTo.Value, txtSearch.Text.Trim());

            foreach (DataRow r in _dtDrafts.Rows)
            {
                string rawType = r["DraftType"].ToString();
                string displayType = rawType switch
                {
                    "Sale" => "🛒 فاتورة بيع",
                    "POS" => "🖥️ نقطة بيع POS",
                    "Purchase" => "📦 فاتورة شراء",
                    "Inventory" => "📋 جرد مخزن",
                    _ => rawType
                };

                DateTime dt = Convert.ToDateTime(r["UpdatedAt"]);
                string target = r["TargetName"]?.ToString() ?? "غير محدد";
                string invType = r["InvoiceType"]?.ToString() ?? "";
                int count = Convert.ToInt32(r["ItemCount"]);
                decimal total = Convert.ToDecimal(r["TotalAmount"]);
                string user = r["CreatedBy"]?.ToString() ?? "";

                int ri = dgDrafts.Rows.Add(
                    r["DraftID"],
                    displayType,
                    dt.ToString("dd/MM/yyyy HH:mm:ss"),
                    target,
                    invType,
                    count.ToString("N0"),
                    total > 0 ? total.ToString("N2") : "-",
                    user
                );
            }

            lblDraftsCount.Text = $"📋 قائمة الفواتير والعمليات غير المكتملة: ({dgDrafts.Rows.Count:N0} عملية غير مكتملة)";

            if (dgDrafts.Rows.Count > 0 && dgDrafts.SelectedRows.Count == 0)
            {
                dgDrafts.Rows[0].Selected = true;
            }
        }

        private void DgDrafts_SelectionChanged(object sender, EventArgs e)
        {
            dgItems.Rows.Clear();
            if (dgDrafts.SelectedRows.Count == 0 || _dtDrafts == null || _dtDrafts.Rows.Count == 0) return;

            int draftId = Convert.ToInt32(dgDrafts.SelectedRows[0].Cells["DraftID"].Value);
            var rows = _dtDrafts.Select($"DraftID = {draftId}");
            if (rows.Length == 0) return;

            var r = rows[0];
            string draftType = r["DraftType"].ToString();
            string json = r["DraftData"].ToString();

            lblItemsTitle.Text = $"🔍 محتويات [{r["TargetName"]}] ({r["ItemCount"]} صنف):";

            if (draftType == "Sale" || draftType == "POS")
            {
                var data = DraftManager.Deserialize<SaleDraftData>(json);
                if (data?.Items != null)
                {
                    foreach (var itm in data.Items)
                    {
                        dgItems.Rows.Add(itm.ProductCode, itm.ProductName, itm.Unit, itm.Quantity.ToString("N3"), itm.UnitPrice.ToString("N2"), itm.LineTotal.ToString("N2"));
                    }
                }
            }
            else if (draftType == "Purchase")
            {
                var data = DraftManager.Deserialize<PurchaseDraftData>(json);
                if (data?.Items != null)
                {
                    foreach (var itm in data.Items)
                    {
                        dgItems.Rows.Add(itm.ProductCode, itm.ProductName, itm.Unit, itm.Quantity.ToString("N3"), itm.UnitPrice.ToString("N2"), itm.LineTotal.ToString("N2"));
                    }
                }
            }
            else if (draftType == "Inventory")
            {
                var data = DraftManager.Deserialize<InventoryDraftData>(json);
                if (data?.ItemsDetails != null && data.ItemsDetails.Count > 0)
                {
                    foreach (var itm in data.ItemsDetails)
                    {
                        dgItems.Rows.Add(itm.ProductCode, itm.ProductName, itm.Unit, itm.ActualQty.ToString("N3"), itm.BookQty.ToString("N3"), itm.DiffQty.ToString("N3"));
                    }
                }
                else if (data?.EnteredActualQty != null)
                {
                    foreach (var kvp in data.EnteredActualQty)
                    {
                        dgItems.Rows.Add($"ID #{kvp.Key}", "صنف مجرود", "-", kvp.Value.ToString("N3"), "-", "-");
                    }
                }
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (dgDrafts.SelectedRows.Count == 0 || _dtDrafts == null)
            {
                MessageBox.Show("من فضلك حدد الفاتورة أو العملية المراد استرجاعها أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int draftId = Convert.ToInt32(dgDrafts.SelectedRows[0].Cells["DraftID"].Value);
            var rows = _dtDrafts.Select($"DraftID = {draftId}");
            if (rows.Length == 0) return;

            var r = rows[0];
            SelectedDraftRow = r;
            SelectedDraftID = draftId;
            SelectedDraftType = r["DraftType"].ToString();
            SelectedDraftJson = r["DraftData"].ToString();
            IsRestored = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgDrafts.SelectedRows.Count == 0 || _dtDrafts == null)
            {
                MessageBox.Show("من فضلك حدد المسودة المراد حذفها أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("هل أنت متأكد من رغبتك في حذف هذه المسودة نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int draftId = Convert.ToInt32(dgDrafts.SelectedRows[0].Cells["DraftID"].Value);
                var rows = _dtDrafts.Select($"DraftID = {draftId}");
                if (rows.Length > 0)
                {
                    string draftKey = rows[0]["DraftKey"].ToString();
                    DraftManager.DeleteDraft(draftKey);
                    LoadDrafts();
                }
            }
        }
    }
}
