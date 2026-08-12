using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>كشف حساب العميل التفصيلي والمالي وكشف مسحوبات الأصناف</summary>
    public class FrmClientStatement : Form
    {
        private int _clientID;
        private string _clientName;
        private int _initialTab;

        private TabControl tabMain;
        private TabPage tabFinancial;
        private TabPage tabItemized;

        // Shared Filter Controls
        private ComboBox cmbClientSelector;
        private bool _isLoadingCombo = false;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad, btnPrint;

        // Financial Tab Controls
        private DataGridView dgStatement;
        private Label lblDebit, lblCredit, lblBalance;
        private DataTable _dt;
        private decimal _totalSales = 0;
        private decimal _totalReturns = 0;
        private decimal _totalPayments = 0;
        private decimal _totalClientPurchases = 0;
        private decimal _runBalance = 0;

        // Itemized Tab Controls
        private DataGridView dgItemized;
        private DataTable _dtItemized;
        private TextBox txtItemSearch;
        private Label lblItemizedCount, lblItemizedTotalQty, lblItemizedTotalAmount;
        private Button btnPrintItemized, btnWhatsAppItemized, btnExportItemizedExcel;

        public FrmClientStatement() : this(0, "")
        {
        }

        public FrmClientStatement(int clientID, string clientName, int initialTab = 0)
        {
            _clientID = clientID;
            _clientName = clientName;
            _initialTab = initialTab;
            InitUI();
            LoadClientsCombo();
            LoadStatement();
            LoadItemizedStatement();

            if (_initialTab == 1 && tabMain != null && tabMain.TabPages.Count > 1)
            {
                tabMain.SelectedIndex = 1;
            }
        }

        private void InitUI()
        {
            this.Text = "كشف حساب تفصيلي - " + (!string.IsNullOrEmpty(_clientName) ? _clientName : "اختر العميل");
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Shared Date & Client Filter Bar (Top)
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = false
            };

            pnlFilter.Controls.Add(new Label { Text = "👤 العميل:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 6, 0, 0) });
            cmbClientSelector = new ComboBox
            {
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(2, 2, 0, 0)
            };
            cmbClientSelector.SelectedIndexChanged += CmbClientSelector_SelectedIndexChanged;
            pnlFilter.Controls.Add(cmbClientSelector);

            pnlFilter.Controls.Add(new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(12, 6, 0, 0) });
            dtpFrom = new DateTimePicker
            {
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0),
                Margin = new Padding(2, 2, 0, 0)
            };
            dtpFrom.ValueChanged += (s, e) => RefreshAllData();
            pnlFilter.Controls.Add(dtpFrom);

            pnlFilter.Controls.Add(new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(10, 6, 0, 0) });
            dtpTo = new DateTimePicker
            {
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy/MM/dd   hh:mm tt",
                Value = DateTime.Now,
                Margin = new Padding(2, 2, 0, 0)
            };
            dtpTo.ValueChanged += (s, e) => RefreshAllData();
            pnlFilter.Controls.Add(dtpTo);

            btnLoad = Theme.MakeButton("🔄 تحديث العرض", Theme.Accent);
            btnLoad.Size = new Size(115, 30);
            btnLoad.Margin = new Padding(12, 0, 0, 0);
            btnLoad.Click += (s, e) => RefreshAllData();
            pnlFilter.Controls.Add(btnLoad);

            var btnCollect = Theme.MakeButton("💵 تحصيل نقدية", Theme.Success);
            btnCollect.Size = new Size(125, 30);
            btnCollect.Margin = new Padding(8, 0, 0, 0);
            btnCollect.Click += (s, e) =>
            {
                if (_clientID <= 0)
                {
                    MessageBox.Show("اختر عميلاً أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var dlg = new Form
                {
                    Width = 380, Height = 230,
                    Text = "تحصيل نقدية من العميل",
                    StartPosition = FormStartPosition.CenterParent,
                    RightToLeft = RightToLeft.Yes, RightToLeftLayout = true,
                    BackColor = Theme.BgCard, Font = Theme.FontMain
                };
                var lbl = new Label { Text = $"👤 العميل: {_clientName}\n💰 أدخل المبلغ المحصل (ج.م):", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(15, 15) };
                var nud = new NumericUpDown { Location = new Point(15, 55), Width = 330, Minimum = 0.01m, Maximum = 9999999m, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 12f) };
                var txtNotes = new TextBox { Location = new Point(15, 95), Width = 330, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
                var btnSave = Theme.MakeButton("✅ حفظ وإصدار سند", 185, 135, 160, 36, Theme.Success);
                var btnCancel = Theme.MakeButton("❌ إلغاء", 15, 135, 150, 36, Theme.Danger);
                btnSave.Click += (s2, e2) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
                btnCancel.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
                dlg.Controls.AddRange(new Control[] { lbl, nud, txtNotes, btnSave, btnCancel });

                if (dlg.ShowDialog(this) == DialogResult.OK && nud.Value > 0)
                {
                    ClientDAL.AddPayment(_clientID, nud.Value, txtNotes.Text.Trim());
                    new FrmPrintClientPayment(_clientID, nud.Value, txtNotes.Text.Trim(), null, _clientName);
                    RefreshAllData();
                }
            };
            pnlFilter.Controls.Add(btnCollect);

            // TabControl Setup
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontMain
            };

            tabFinancial = new TabPage("📑 كشف الحساب المالي (الماليات والعمليات)") { BackColor = Theme.BgMain };
            tabItemized = new TabPage("📦 كشف حساب أصناف العميل (المسحوبات التفصيلية)") { BackColor = Theme.BgMain };

            BuildFinancialTab(tabFinancial);
            BuildItemizedTab(tabItemized);

            tabMain.TabPages.Add(tabFinancial);
            tabMain.TabPages.Add(tabItemized);

            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblMain.Controls.Add(pnlFilter, 0, 0);
            tblMain.Controls.Add(tabMain, 0, 1);

            this.Controls.Add(tblMain);
            Theme.ApplyFormRTL(this);
        }

        private void LoadClientsCombo()
        {
            try
            {
                _isLoadingCombo = true;
                DataTable dt = ClientDAL.GetAll();
                if (dt != null)
                {
                    if (!dt.Columns.Contains("ClientDisplayInfo"))
                    {
                        dt.Columns.Add("ClientDisplayInfo", typeof(string));
                        foreach (DataRow r in dt.Rows)
                        {
                            string code = r.Table.Columns.Contains("ClientCode") ? r["ClientCode"].ToString() : "";
                            string phone = r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value ? r["Phone"].ToString() : "";
                            r["ClientDisplayInfo"] = string.IsNullOrEmpty(phone) ? $"{r["ClientName"]} (كود: {code})" : $"{r["ClientName"]}  |  📱 {phone}  |  (كود: {code})";
                        }
                    }
                    cmbClientSelector.DataSource = dt;
                    cmbClientSelector.DisplayMember = "ClientDisplayInfo";
                    cmbClientSelector.ValueMember = "ClientID";
                }

                if (_clientID > 0)
                {
                    cmbClientSelector.SelectedValue = _clientID;
                }
                else if (dt != null && dt.Rows.Count > 0)
                {
                    _clientID = Convert.ToInt32(dt.Rows[0]["ClientID"]);
                    _clientName = dt.Rows[0]["ClientName"].ToString();
                    this.Text = "كشف حساب تفصيلي - " + _clientName;
                    cmbClientSelector.SelectedValue = _clientID;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadClientsCombo failed", ex);
            }
            finally
            {
                _isLoadingCombo = false;
            }
        }

        private void CmbClientSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoadingCombo) return;
            if (cmbClientSelector.SelectedValue != null && cmbClientSelector.SelectedValue != DBNull.Value)
            {
                if (int.TryParse(cmbClientSelector.SelectedValue.ToString(), out int cid) && cid > 0)
                {
                    if (cid != _clientID)
                    {
                        _clientID = cid;
                        _clientName = cmbClientSelector.Text;
                        this.Text = "كشف حساب تفصيلي - " + _clientName;
                        RefreshAllData();
                    }
                }
            }
        }

        private void RefreshAllData()
        {
            LoadStatement();
            LoadItemizedStatement();
        }

        // =========================================================================
        // TAB 1: FINANCIAL STATEMENT (كشف الحساب المالي)
        // =========================================================================
        private void BuildFinancialTab(TabPage page)
        {
            var pnlTopBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 4, 8, 4)
            };

            btnPrint = Theme.MakeButton("🖨️ طباعة كشف الحساب المالي", Theme.Primary);
            btnPrint.Size = new Size(190, 28);
            btnPrint.Click += BtnPrint_Click;
            pnlTopBar.Controls.Add(btnPrint);

            dgStatement = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ والوقت", FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "النوع", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Debit", HeaderText = "مدين", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Credit", HeaderText = "دائن", FillWeight = 40 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الجاري", FillWeight = 55 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "القائم بالعمل", FillWeight = 50 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "تفاصيل الأصناف والبيان المالي للحساب", FillWeight = 170 });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransTypeRaw", Visible = false });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefID", Visible = false });
            dgStatement.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseNotes", Visible = false });

            var btnCol = new DataGridViewButtonColumn
            {
                Name = "BtnView",
                HeaderText = "عرض",
                Text = "👁️",
                UseColumnTextForButtonValue = true,
                FillWeight = 25
            };
            dgStatement.Columns.Add(btnCol);

            dgStatement.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgStatement.Columns[e.ColumnIndex].Name == "BtnView")
                {
                    var row = dgStatement.Rows[e.RowIndex];
                    if (row.Cells["BtnView"] is DataGridViewButtonCell)
                    {
                        string typeRaw = row.Cells["TransTypeRaw"].Value?.ToString();
                        int refID = row.Cells["RefID"].Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;

                        if ((typeRaw == "Sale" || typeRaw == "Return") && refID > 0)
                        {
                            var frm = new FrmStatementItemsInfo(typeRaw, refID);
                            frm.ShowDialog();
                        }
                    }
                }
            };

            var pnlFoot = new Panel { Dock = DockStyle.Fill, Height = 46, BackColor = Theme.BgCard, Padding = new Padding(8) };
            lblBalance = new Label { Text = "الصافي: 0", ForeColor = Color.FromArgb(10, 60, 140), Location = new Point(680, 12), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            lblCredit = new Label { Text = "إجمالي مرتجع: 0 | إجمالي توريد: 0", ForeColor = Color.FromArgb(15, 120, 50), Location = new Point(250, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblDebit = new Label { Text = "إجمالي مديونية: 0", ForeColor = Color.FromArgb(180, 20, 20), Location = new Point(20, 12), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlFoot.Controls.AddRange(new Control[] { lblDebit, lblCredit, lblBalance });

            var tblFin = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblFin.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            tblFin.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblFin.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            tblFin.Controls.Add(pnlTopBar, 0, 0);
            tblFin.Controls.Add(dgStatement, 0, 1);
            tblFin.Controls.Add(pnlFoot, 0, 2);

            page.Controls.Add(tblFin);
        }

        private void LoadStatement()
        {
            if (dgStatement == null || _clientID <= 0) return;
            _dt = ClientDAL.GetStatement(_clientID, dtpFrom.Value, dtpTo.Value);
            dgStatement.Rows.Clear();
            decimal prevBalance = ClientDAL.GetPreviousBalance(_clientID, dtpFrom.Value);
            _runBalance = prevBalance;
            _totalSales = 0;
            _totalReturns = 0;
            _totalPayments = 0;
            _totalClientPurchases = 0;

            if (prevBalance != 0)
            {
                dgStatement.Rows.Add("", "رصيد افتتاحي سابق", "", "", prevBalance.ToString("N2") + " ج", "---", "رصيد ما قبل " + dtpFrom.Value.ToString("dd/MM/yyyy"), "", 0, "");
            }

            foreach (DataRow r in _dt.Rows)
            {
                decimal deb = Convert.ToDecimal(r["Debit"]);
                decimal cred = Convert.ToDecimal(r["Credit"]);
                _runBalance = _runBalance + deb - cred;

                string typeStr = r["TransType"].ToString();
                int refID = r["RefID"] != DBNull.Value ? Convert.ToInt32(r["RefID"]) : 0;
                string baseNotes = r["Notes"].ToString();
                string detailedNotes = baseNotes;

                if (typeStr == "Sale" && refID > 0)
                {
                    _totalSales += deb;
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, si.Quantity, p.Unit
                        FROM SaleItems si
                        JOIN Products p ON si.ProductID = p.ProductID
                        WHERE si.SaleID = @id", DbHelper.P("@id", refID));
                    
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["Unit"]})");
                        }
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
                }
                else if (typeStr == "Return" && refID > 0)
                {
                    _totalReturns += cred;
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, ri.Quantity, p.Unit
                        FROM ReturnItems ri
                        JOIN Products p ON ri.ProductID = p.ProductID
                        WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
                    
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["Unit"]})");
                        }
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
                }
                else if (typeStr == "Payment")
                {
                    _totalPayments += cred;
                }
                else if (typeStr == "ClientPurchase" && refID > 0)
                {
                    _totalClientPurchases += cred;
                    var dtItems = DbHelper.Query(@"
                        SELECT p.ProductName, pi2.Quantity, pi2.UnitName
                        FROM PurchaseItems pi2
                        JOIN Products p ON pi2.ProductID = p.ProductID
                        WHERE pi2.PurchaseID = @id", DbHelper.P("@id", refID));
                    if (dtItems.Rows.Count > 0)
                    {
                        var itemsList = new List<string>();
                        foreach (DataRow itemRow in dtItems.Rows)
                            itemsList.Add($"{itemRow["ProductName"]} ({Convert.ToDecimal(itemRow["Quantity"]):N0} {itemRow["UnitName"]})");
                        detailedNotes += " [" + string.Join("، ", itemsList) + "]";
                    }
                }
                else if (typeStr == "Opening")
                {
                    _totalSales += deb;
                }

                string createdBy = r.Table.Columns.Contains("CreatedByName") && r["CreatedByName"] != DBNull.Value ? r["CreatedByName"].ToString() : "---";
                var rowIdx = dgStatement.Rows.Add(
                    Convert.ToDateTime(r["TransDate"]).ToString("dd/MM/yyyy HH:mm"),
                    TransTypeName(typeStr),
                    deb > 0 ? deb.ToString("N2") : "",
                    cred > 0 ? cred.ToString("N2") : "",
                    _runBalance.ToString("N2") + " ج",
                    createdBy,
                    detailedNotes,
                    typeStr,
                    refID,
                    baseNotes);

                if ((typeStr != "Sale" && typeStr != "Return") || refID <= 0)
                {
                    dgStatement.Rows[rowIdx].Cells["BtnView"] = new DataGridViewTextBoxCell { Value = "" };
                }

                var rowStyle = dgStatement.Rows[rowIdx].DefaultCellStyle;
                if (typeStr == "Sale")
                {
                    rowStyle.BackColor = Color.FromArgb(240, 244, 255);
                    rowStyle.ForeColor = Color.FromArgb(10, 50, 130);
                }
                else if (typeStr == "Payment")
                {
                    rowStyle.BackColor = Color.FromArgb(235, 250, 240);
                    rowStyle.ForeColor = Color.FromArgb(15, 120, 50);
                }
                else if (typeStr == "Return")
                {
                    rowStyle.BackColor = Color.FromArgb(255, 240, 240);
                    rowStyle.ForeColor = Color.FromArgb(180, 20, 20);
                }
                else if (typeStr == "ClientPurchase")
                {
                    rowStyle.BackColor = Color.FromArgb(245, 238, 255);
                    rowStyle.ForeColor = Color.FromArgb(90, 20, 140);
                }
                else
                {
                    rowStyle.BackColor = Color.FromArgb(250, 250, 250);
                    rowStyle.ForeColor = Color.FromArgb(30, 40, 50);
                }
            }

            lblDebit.Text = $"إجمالي مديونية: {_totalSales:N2} ج";
            lblCredit.Text = $"إجمالي مرتجع: {_totalReturns:N2} ج  |  إجمالي توريد: {_totalPayments:N2} ج" +
                             (_totalClientPurchases > 0 ? $"  |  شراء من عميل: {_totalClientPurchases:N2} ج" : "");
            lblBalance.Text = $"الصافي: {_runBalance:N2} ج";
        }

        private string TransTypeName(string t)
        {
            switch (t)
            {
                case "Sale": return "فاتورة بيع";
                case "Return": return "مرتجع";
                case "Payment": return "تحصيل";
                case "Opening": return "رصيد افتتاحي";
                case "Discount": return "تسوية خصم";
                case "Addition": return "تسوية إضافة";
                case "ClientPurchase": return "📦 شراء من عميل";
                default: return t;
            }
        }

        // =========================================================================
        // TAB 2: ITEMIZED CLIENT PRODUCT SALES (كشف مسحوبات الأصناف التفصيلي)
        // =========================================================================
        private void BuildItemizedTab(TabPage page)
        {
            var pnlTopBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(8, 5, 8, 5)
            };

            pnlTopBar.Controls.Add(new Label { Text = "🔍 تصفية الأصناف:", AutoSize = true, ForeColor = Theme.TextSearchLabel, Font = Theme.FontBold, Margin = new Padding(5, 5, 0, 0) });
            txtItemSearch = new TextBox { Width = 200, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            txtItemSearch.TextChanged += (s, e) => FilterAndDisplayItemized();
            pnlTopBar.Controls.Add(txtItemSearch);

            btnPrintItemized = Theme.MakeButton("🖨️ طباعة كشف أصناف العميل", Color.FromArgb(30, 90, 160));
            btnPrintItemized.Size = new Size(185, 28);
            btnPrintItemized.Margin = new Padding(15, 0, 0, 0);
            btnPrintItemized.Click += BtnPrintItemized_Click;
            pnlTopBar.Controls.Add(btnPrintItemized);

            btnWhatsAppItemized = Theme.MakeButton("📲 إرسال الكشف واتساب", Color.FromArgb(37, 211, 102));
            btnWhatsAppItemized.Size = new Size(170, 28);
            btnWhatsAppItemized.Margin = new Padding(10, 0, 0, 0);
            btnWhatsAppItemized.Click += BtnWhatsAppItemized_Click;
            pnlTopBar.Controls.Add(btnWhatsAppItemized);

            btnExportItemizedExcel = Theme.MakeButton("📥 تصدير إكسيل", Color.FromArgb(0, 102, 204));
            btnExportItemizedExcel.Size = new Size(130, 28);
            btnExportItemizedExcel.Margin = new Padding(10, 0, 0, 0);
            btnExportItemizedExcel.Click += (s, e) => ExportGridToCsv(dgItemized, $"كشف_مسحوبات_أصناف_{_clientName}");
            pnlTopBar.Controls.Add(btnExportItemizedExcel);

            dgItemized = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 40 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 110 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 35 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldQty", HeaderText = "إجمالي المبيعات", FillWeight = 45 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnQty", HeaderText = "إجمالي المرتجع", FillWeight = 45 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetQty", HeaderText = "صافي الكمية المسحوبة", FillWeight = 55 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvgPrice", HeaderText = "متوسط السعر", FillWeight = 45 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetTotal", HeaderText = "صافي المبلغ (ج)", FillWeight = 55 });
            dgItemized.Columns.Add(new DataGridViewTextBoxColumn { Name = "SharePct", HeaderText = "نسبة المساهمة", FillWeight = 40 });

            var pnlFoot = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 44,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 8)
            };
            lblItemizedCount = new Label { Text = "عدد الأصناف: 0 صنف", AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold, Margin = new Padding(10, 4, 0, 0) };
            lblItemizedTotalQty = new Label { Text = "إجمالي مسحوبات الكميات: 0.00", AutoSize = true, ForeColor = Color.FromArgb(15, 120, 50), Font = Theme.FontBold, Margin = new Padding(30, 4, 0, 0) };
            lblItemizedTotalAmount = new Label { Text = "إجمالي قيمة مبيعات الأصناف: 0.00 ج", AutoSize = true, ForeColor = Color.FromArgb(20, 70, 150), Font = Theme.FontBold, Margin = new Padding(30, 4, 0, 0) };
            pnlFoot.Controls.AddRange(new Control[] { lblItemizedCount, lblItemizedTotalQty, lblItemizedTotalAmount });

            var tblItm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblItm.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            tblItm.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblItm.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            tblItm.Controls.Add(pnlTopBar, 0, 0);
            tblItm.Controls.Add(dgItemized, 0, 1);
            tblItm.Controls.Add(pnlFoot, 0, 2);

            page.Controls.Add(tblItm);
        }

        private void LoadItemizedStatement()
        {
            if (_clientID <= 0) return;
            _dtItemized = ReportDAL.GetClientItemizedStatement(dtpFrom.Value, dtpTo.Value, _clientID);
            FilterAndDisplayItemized();
        }

        private void FilterAndDisplayItemized()
        {
            if (dgItemized == null) return;
            dgItemized.Rows.Clear();
            if (_dtItemized == null) return;

            string filter = txtItemSearch != null ? txtItemSearch.Text.Trim() : "";

            decimal grandTotalVal = 0m;
            decimal grandTotalQty = 0m;
            foreach (DataRow r in _dtItemized.Rows)
            {
                grandTotalVal += Convert.ToDecimal(r["صافي المبلغ"]);
                grandTotalQty += Convert.ToDecimal(r["صافي الكمية"]);
            }

            int count = 0;
            foreach (DataRow r in _dtItemized.Rows)
            {
                string code = r["كود الصنف"]?.ToString() ?? "";
                string name = r["اسم الصنف"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!code.ToLower().Contains(filter.ToLower()) && !name.ToLower().Contains(filter.ToLower()))
                        continue;
                }

                string unit = r["الوحدة"]?.ToString() ?? "قطعة";
                decimal sold = Convert.ToDecimal(r["إجمالي المبيعات"]);
                decimal ret = Convert.ToDecimal(r["إجمالي المرتجع"]);
                decimal netQty = Convert.ToDecimal(r["صافي الكمية"]);
                decimal avgPrice = Convert.ToDecimal(r["متوسط السعر"]);
                decimal netVal = Convert.ToDecimal(r["صافي المبلغ"]);

                double sharePct = grandTotalVal > 0 ? (double)(netVal / grandTotalVal * 100m) : 0.0;

                dgItemized.Rows.Add(
                    code,
                    name,
                    unit,
                    sold.ToString("N2"),
                    ret.ToString("N2"),
                    netQty.ToString("N2"),
                    avgPrice.ToString("N2") + " ج",
                    netVal.ToString("N2") + " ج",
                    sharePct.ToString("F1") + "%"
                );

                count++;
            }

            if (lblItemizedCount != null) lblItemizedCount.Text = $"عدد الأصناف: {count} صنف";
            if (lblItemizedTotalQty != null) lblItemizedTotalQty.Text = $"إجمالي مسحوبات الكميات: {grandTotalQty:N2}";
            if (lblItemizedTotalAmount != null) lblItemizedTotalAmount.Text = $"إجمالي مبيعات الأصناف: {grandTotalVal:N2} ج";
        }

        private void ExportGridToCsv(DataGridView dg, string fileName)
        {
            if (dg == null || dg.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            using (var sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = fileName + ".csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new System.Text.StringBuilder();
                    var headers = new List<string>();
                    foreach (DataGridViewColumn col in dg.Columns)
                        if (col.Visible) headers.Add(col.HeaderText);
                    sb.AppendLine(string.Join(",", headers));

                    foreach (DataGridViewRow row in dg.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var cells = new List<string>();
                        foreach (DataGridViewColumn col in dg.Columns)
                        {
                            if (col.Visible)
                            {
                                string val = row.Cells[col.Index].Value?.ToString() ?? "";
                                cells.Add($"\"{val.Replace("\"", "\"\"")}\"");
                            }
                        }
                        sb.AppendLine(string.Join(",", cells));
                    }
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show("✅ تم تصدير الملف بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnPrintItemized_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            int currentRowIndex = 0;
            int pageNumber = 0;

            pd.BeginPrint += (s, ev) =>
            {
                currentRowIndex = 0;
                pageNumber = 0;
            };

            pd.PrintPage += (s, ev) =>
            {
                pageNumber++;
                var g = ev.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var titleFont = new Font("Arial", 14, FontStyle.Bold);
                var subTitleFont = new Font("Arial", 9, FontStyle.Bold);
                var headerFont = new Font("Arial", 9, FontStyle.Bold);
                var dataFont = new Font("Arial", 8.5f, FontStyle.Regular);
                var boldDataFont = new Font("Arial", 8.5f, FontStyle.Bold);

                var headerBgBrush = new SolidBrush(Color.FromArgb(15, 45, 90));
                var borderPen = new Pen(Color.FromArgb(15, 45, 90), 1.5f);

                int y = 25;
                int leftMargin = 20;
                int rightMargin = 805;
                int tableWidth = rightMargin - leftMargin;

                // Title Header Block
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 250)), leftMargin, y, tableWidth, 45);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 45);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                g.DrawString($"كشف حساب أصناف مسحوبات العميل: {_clientName}", titleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 4, tableWidth, 22), sfCenter);
                g.DrawString($"الفترة من: {dtpFrom.Value:yyyy/MM/dd HH:mm}  إلى: {dtpTo.Value:yyyy/MM/dd HH:mm}   |   تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", subTitleFont, Brushes.DimGray, new RectangleF(leftMargin, y + 25, tableWidth, 16), sfCenter);
                y += 55;

                int[] xCols = { 20, 90, 310, 370, 440, 510, 590, 680, 805 };
                string[] headers = { "كود الصنف", "اسم الصنف", "الوحدة", "المبيعات", "المرتجع", "صافي الكمية", "متوسط السعر", "صافي المبلغ" };

                g.FillRectangle(headerBgBrush, leftMargin, y, tableWidth, 26);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 26);

                for (int i = 0; i < headers.Length; i++)
                {
                    float cx = xCols[i];
                    float cw = xCols[i + 1] - xCols[i];
                    g.DrawString(headers[i], headerFont, Brushes.White, new RectangleF(cx, y, cw, 26), sfCenter);
                    if (i > 0) g.DrawLine(Pens.White, xCols[i], y, xCols[i], y + 26);
                }
                y += 26;

                ev.HasMorePages = false;
                while (currentRowIndex < dgItemized.Rows.Count)
                {
                    var row = dgItemized.Rows[currentRowIndex];
                    if (y + 24 > ev.PageBounds.Height - 80)
                    {
                        ev.HasMorePages = true;
                        return;
                    }

                    if (currentRowIndex % 2 == 1)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 254)), leftMargin, y, tableWidth, 24);
                    }

                    string code = row.Cells[0].Value?.ToString() ?? "";
                    string name = row.Cells[1].Value?.ToString() ?? "";
                    string unit = row.Cells[2].Value?.ToString() ?? "";
                    string sold = row.Cells[3].Value?.ToString() ?? "";
                    string ret = row.Cells[4].Value?.ToString() ?? "";
                    string netQty = row.Cells[5].Value?.ToString() ?? "";
                    string avgP = row.Cells[6].Value?.ToString() ?? "";
                    string netVal = row.Cells[7].Value?.ToString() ?? "";

                    g.DrawString(code, dataFont, Brushes.Black, new RectangleF(xCols[0], y, xCols[1] - xCols[0], 24), sfCenter);
                    g.DrawString(name, boldDataFont, Brushes.DarkSlateGray, new RectangleF(xCols[1] + 5, y + 2, xCols[2] - xCols[1] - 10, 20), sfRight);
                    g.DrawString(unit, dataFont, Brushes.Black, new RectangleF(xCols[2], y, xCols[3] - xCols[2], 24), sfCenter);
                    g.DrawString(sold, dataFont, Brushes.Black, new RectangleF(xCols[3], y, xCols[4] - xCols[3], 24), sfCenter);
                    g.DrawString(ret, dataFont, Brushes.DarkRed, new RectangleF(xCols[4], y, xCols[5] - xCols[4], 24), sfCenter);
                    g.DrawString(netQty, boldDataFont, Brushes.DarkGreen, new RectangleF(xCols[5], y, xCols[6] - xCols[5], 24), sfCenter);
                    g.DrawString(avgP, dataFont, Brushes.Black, new RectangleF(xCols[6], y, xCols[7] - xCols[6], 24), sfCenter);
                    g.DrawString(netVal, boldDataFont, Brushes.DarkBlue, new RectangleF(xCols[7], y, xCols[8] - xCols[7], 24), sfCenter);

                    g.DrawLine(Pens.LightGray, leftMargin, y + 24, rightMargin, y + 24);
                    y += 24;
                    currentRowIndex++;
                }

                g.DrawLine(borderPen, leftMargin, y + 5, rightMargin, y + 5);
                g.DrawString(lblItemizedCount.Text + "   |   " + lblItemizedTotalQty.Text + "   |   " + lblItemizedTotalAmount.Text,
                    subTitleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 10, tableWidth, 22), sfCenter);
            };

            var preview = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 750 };
            preview.ShowDialog(this);
        }

        private void BtnWhatsAppItemized_Click(object sender, EventArgs e)
        {
            if (dgItemized == null || dgItemized.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف لعرضها وتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var dlg = new Form
            {
                Width = 420,
                Height = 190,
                Text = "إرسال كشف حساب أصناف العميل واتساب",
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain
            };
            var lbl = new Label { Text = "📱 أدخل رقم الواتساب (مثال: 01012345678):", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(10, 15) };
            var txt = new TextBox { Location = new Point(10, 42), Width = 380, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };
            var btnSend = Theme.MakeButton("✅ إرسال", 230, 90, 150, 36, Color.FromArgb(37, 211, 102));
            var btnCancel = Theme.MakeButton("❌ إلغاء", 60, 90, 150, 36, Color.FromArgb(180, 60, 60));
            btnSend.Click += (s2, e2) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
            btnCancel.Click += (s2, e2) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
            dlg.Controls.AddRange(new Control[] { lbl, txt, btnSend, btnCancel });

            if (dlg.ShowDialog() != DialogResult.OK) return;
            string phone = txt.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone)) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"📋 *كشف حساب أصناف مسحوبات العميل*");
            sb.AppendLine($"🏢 {AppConfig.CompanyName}");
            sb.AppendLine($"👤 العميل: {_clientName}");
            sb.AppendLine($"📅 الفترة: من {dtpFrom.Value:yyyy/MM/dd} إلى {dtpTo.Value:yyyy/MM/dd}");
            sb.AppendLine("──────────────────────");

            foreach (DataGridViewRow row in dgItemized.Rows)
            {
                string name = row.Cells[1].Value?.ToString() ?? "";
                string unit = row.Cells[2].Value?.ToString() ?? "";
                string netQty = row.Cells[5].Value?.ToString() ?? "0";
                string netVal = row.Cells[7].Value?.ToString() ?? "0";

                sb.AppendLine($"• {name}");
                sb.AppendLine($"  الكمية: {netQty} {unit} | القيمة: {netVal}");
            }

            sb.AppendLine("──────────────────────");
            sb.AppendLine($"📊 {lblItemizedCount.Text}");
            sb.AppendLine($"📦 {lblItemizedTotalQty.Text}");
            sb.AppendLine($"💰 {lblItemizedTotalAmount.Text}");
            sb.AppendLine("──────────────────────");

            WhatsAppSender.OpenWhatsApp(phone, sb.ToString());
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var pd = new PrintDocument();
            AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            int currentRowIndex = 0;
            int pageNumber = 0;

            pd.BeginPrint += (s, ev) =>
            {
                currentRowIndex = 0;
                pageNumber = 0;
            };

            pd.PrintPage += (s, ev) =>
            {
                pageNumber++;
                var g = ev.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var titleFont  = new Font("Arial", 14, FontStyle.Bold);
                var subTitleFont = new Font("Arial", 9, FontStyle.Bold);
                var headerFont = new Font("Arial", 9, FontStyle.Bold);
                var dataFont   = new Font("Arial", 8.5f, FontStyle.Regular);
                var boldDataFont = new Font("Arial", 8.5f, FontStyle.Bold);
                var itemFont   = new Font("Arial", 8f, FontStyle.Regular);
                var itemHeaderFont = new Font("Arial", 8f, FontStyle.Bold);

                var headerBgBrush = new SolidBrush(Color.FromArgb(15, 45, 90));
                var gridPen = new Pen(Color.FromArgb(180, 190, 205), 1f);
                var borderPen = new Pen(Color.FromArgb(15, 45, 90), 1.5f);
                var subGridPen = new Pen(Color.FromArgb(200, 210, 225), 1f);

                int y = 25;
                int leftMargin = 20;
                int rightMargin = 805;
                int tableWidth = rightMargin - leftMargin;

                // Title Block
                g.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 250)), leftMargin, y, tableWidth, 45);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 45);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sfRight  = new StringFormat { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                g.DrawString($"كشف حساب العميل التفصيلي: {_clientName}", titleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 4, tableWidth, 22), sfCenter);
                g.DrawString($"الفترة من: {dtpFrom.Value:yyyy/MM/dd HH:mm}  إلى: {dtpTo.Value:yyyy/MM/dd HH:mm}   |   تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", subTitleFont, Brushes.DimGray, new RectangleF(leftMargin, y + 25, tableWidth, 16), sfCenter);
                y += 55;

                int[] xCols = { 20, 135, 220, 295, 370, 465, 805 };
                string[] headers = { "التاريخ والوقت", "النوع", "مدين", "دائن", "الرصيد الجاري", "البيان التفصيلي والأصناف" };

                int headerY = y;
                g.FillRectangle(headerBgBrush, leftMargin, y, tableWidth, 26);
                g.DrawRectangle(borderPen, leftMargin, y, tableWidth, 26);

                for (int i = 0; i < headers.Length; i++)
                {
                    float cx = xCols[i];
                    float cw = xCols[i + 1] - xCols[i];
                    g.DrawString(headers[i], headerFont, Brushes.White, new RectangleF(cx, y, cw, 26), sfCenter);
                    if (i > 0)
                        g.DrawLine(Pens.White, xCols[i], y, xCols[i], y + 26);
                }
                y += 26;

                ev.HasMorePages = false;
                while (currentRowIndex < dgStatement.Rows.Count)
                {
                    var row = dgStatement.Rows[currentRowIndex];
                    string typeRaw = row.Cells["TransTypeRaw"].Value?.ToString();
                    int refID = row.Cells["RefID"].Value != null ? Convert.ToInt32(row.Cells["RefID"].Value) : 0;

                    DataTable dtItems = null;
                    if ((typeRaw == "Sale" || typeRaw == "Return" || typeRaw == "ClientPurchase") && refID > 0)
                    {
                        if (typeRaw == "Sale")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, si.Quantity, ISNULL(si.UnitName, p.Unit) AS Unit, si.UnitPrice, (si.Quantity * si.UnitPrice) AS Total
                                FROM SaleItems si
                                JOIN Products p ON si.ProductID = p.ProductID
                                WHERE si.SaleID = @id", DbHelper.P("@id", refID));
                        }
                        else if (typeRaw == "Return")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, ri.Quantity, ISNULL(ri.UnitName, p.Unit) AS Unit, ri.UnitPrice, (ri.Quantity * ri.UnitPrice) AS Total
                                FROM ReturnItems ri
                                JOIN Products p ON ri.ProductID = p.ProductID
                                WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
                        }
                        else if (typeRaw == "ClientPurchase")
                        {
                            dtItems = DbHelper.Query(@"
                                SELECT p.ProductName, pi2.Quantity, ISNULL(pi2.UnitName, p.Unit) AS Unit, pi2.UnitPrice, (pi2.Quantity * pi2.UnitPrice) AS Total
                                FROM PurchaseItems pi2
                                JOIN Products p ON pi2.ProductID = p.ProductID
                                WHERE pi2.PurchaseID = @id", DbHelper.P("@id", refID));
                        }
                    }

                    int itemsCount = dtItems != null ? dtItems.Rows.Count : 0;
                    int rowHeight = 22 + (itemsCount > 0 ? (18 + itemsCount * 17) : 0);

                    if (y + rowHeight > ev.PageBounds.Height - 90)
                    {
                        ev.HasMorePages = true;
                        return;
                    }

                    if (currentRowIndex % 2 == 1 && itemsCount == 0)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 254)), leftMargin, y, tableWidth, 22);
                    }

                    string dateStr = row.Cells["TransDate"].Value?.ToString() ?? "";
                    string typeStr = row.Cells["TransType"].Value?.ToString() ?? "";
                    string debStr  = row.Cells["Debit"].Value?.ToString() ?? "";
                    string credStr = row.Cells["Credit"].Value?.ToString() ?? "";
                    string balStr  = row.Cells["Balance"].Value?.ToString() ?? "";
                    
                    string baseNotes = row.Cells["BaseNotes"].Value?.ToString() ?? "";
                    string createdBy = row.Cells["CreatedByName"].Value?.ToString();
                    if (!string.IsNullOrEmpty(createdBy) && createdBy != "---")
                        baseNotes += $" (بواسطة: {createdBy})";

                    g.DrawString(dateStr, dataFont, Brushes.Black, new RectangleF(xCols[0], y, xCols[1] - xCols[0], 22), sfCenter);
                    g.DrawString(typeStr, boldDataFont, Brushes.DarkSlateGray, new RectangleF(xCols[1], y, xCols[2] - xCols[1], 22), sfCenter);
                    g.DrawString(debStr,  boldDataFont, Brushes.DarkRed, new RectangleF(xCols[2], y, xCols[3] - xCols[2], 22), sfCenter);
                    g.DrawString(credStr, boldDataFont, Brushes.DarkGreen, new RectangleF(xCols[3], y, xCols[4] - xCols[3], 22), sfCenter);
                    g.DrawString(balStr,  boldDataFont, Brushes.DarkBlue, new RectangleF(xCols[4], y, xCols[5] - xCols[4], 22), sfCenter);
                    g.DrawString(baseNotes, dataFont, Brushes.Black, new RectangleF(xCols[5] + 5, y + 2, xCols[6] - xCols[5] - 10, 20), sfRight);

                    y += 22;

                    if (itemsCount > 0)
                    {
                        int subLeft = xCols[0] + 15;
                        int subWidth = tableWidth - 30;
                        int subHeaderY = y;

                        g.FillRectangle(new SolidBrush(Color.FromArgb(235, 240, 250)), subLeft, subHeaderY, subWidth, 18);
                        g.DrawRectangle(subGridPen, subLeft, subHeaderY, subWidth, 18);

                        float[] subCols = { subLeft, subLeft + 280, subLeft + 350, subLeft + 420, subLeft + 500, subLeft + subWidth };
                        string[] subHeaders = { "اسم الصنف", "الكمية", "الوحدة", "السعر", "الإجمالي" };

                        for (int k = 0; k < subHeaders.Length; k++)
                        {
                            g.DrawString(subHeaders[k], itemHeaderFont, Brushes.DarkSlateGray, new RectangleF(subCols[k], subHeaderY, subCols[k + 1] - subCols[k], 18), sfCenter);
                            if (k > 0) g.DrawLine(subGridPen, subCols[k], subHeaderY, subCols[k], subHeaderY + 18);
                        }
                        y += 18;

                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            string pName = itemRow["ProductName"].ToString();
                            string pQty  = Convert.ToDecimal(itemRow["Quantity"]).ToString("N2");
                            string pUnit = itemRow["Unit"].ToString();
                            string pPrice = Convert.ToDecimal(itemRow["UnitPrice"]).ToString("N2");
                            string pTot  = Convert.ToDecimal(itemRow["Total"]).ToString("N2");

                            g.FillRectangle(new SolidBrush(Color.FromArgb(252, 253, 255)), subLeft, y, subWidth, 17);
                            g.DrawRectangle(subGridPen, subLeft, y, subWidth, 17);

                            g.DrawString(pName, itemFont, Brushes.Black, new RectangleF(subCols[0] + 5, y, subCols[1] - subCols[0] - 10, 17), sfRight);
                            g.DrawString(pQty, itemFont, Brushes.Black, new RectangleF(subCols[1], y, subCols[2] - subCols[1], 17), sfCenter);
                            g.DrawString(pUnit, itemFont, Brushes.Black, new RectangleF(subCols[2], y, subCols[3] - subCols[2], 17), sfCenter);
                            g.DrawString(pPrice, itemFont, Brushes.Black, new RectangleF(subCols[3], y, subCols[4] - subCols[3], 17), sfCenter);
                            g.DrawString(pTot, itemFont, Brushes.DarkBlue, new RectangleF(subCols[4], y, subCols[5] - subCols[4], 17), sfCenter);

                            for (int k = 1; k < subCols.Length - 1; k++)
                                g.DrawLine(subGridPen, subCols[k], y, subCols[k], y + 17);

                            y += 17;
                        }
                    }

                    g.DrawLine(gridPen, leftMargin, y, rightMargin, y);
                    currentRowIndex++;
                }

                g.DrawLine(borderPen, leftMargin, y + 5, rightMargin, y + 5);
                g.DrawString(lblDebit.Text + "   |   " + lblCredit.Text + "   |   " + lblBalance.Text,
                    subTitleFont, Brushes.DarkBlue, new RectangleF(leftMargin, y + 10, tableWidth, 22), sfCenter);
            };

            var preview = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 750 };
            preview.ShowDialog(this);
        }
    }

    /// <summary>نافذة فرعية لعرض تفاصيل الأصناف المضمنة في الفاتورة المحددة</summary>
    public class FrmStatementItemsInfo : Form
    {
        private DataGridView dgItems;
        private Label lblTitle, lblTotal;
        private Button btnClose;

        public FrmStatementItemsInfo(string transType, int refID)
        {
            InitUI(transType, refID);
        }

        private void InitUI(string transType, int refID)
        {
            string titleText = transType == "Sale" ? $"تفاصيل أصناف فاتورة البيع رقم: #{refID}" : $"تفاصيل أصناف المرتجع رقم: #{refID}";
            this.Text = titleText;
            this.Size = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            lblTitle = new Label
            {
                Text = titleText,
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            dgItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف" });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 35 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "السعر", FillWeight = 45 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "الإجمالي", FillWeight = 50 });
            this.Controls.Add(dgItems);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            
            lblTotal = new Label
            {
                Text = "إجمالي العملية: 0.00 ج",
                Dock = DockStyle.Right,
                Width = 250,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlBottom.Controls.Add(lblTotal);

            btnClose = Theme.MakeButton("إغلاق", 10, 10, 100, 32, Color.FromArgb(90, 90, 90));
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlBottom);

            lblTitle.BringToFront();
            pnlBottom.BringToFront();
            dgItems.BringToFront();

            LoadItems(transType, refID);
        }

        private void LoadItems(string transType, int refID)
        {
            dgItems.Rows.Clear();
            DataTable dt = null;

            if (transType == "Sale")
            {
                dt = DbHelper.Query(@"
                    SELECT p.ProductName, si.Quantity, p.Unit, si.UnitPrice, (si.Quantity * si.UnitPrice) AS Total
                    FROM SaleItems si
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE si.SaleID = @id", DbHelper.P("@id", refID));
            }
            else if (transType == "Return")
            {
                dt = DbHelper.Query(@"
                    SELECT p.ProductName, ri.Quantity, p.Unit, ri.UnitPrice, (ri.Quantity * ri.UnitPrice) AS Total
                    FROM ReturnItems ri
                    JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE ri.ReturnID = @id", DbHelper.P("@id", refID));
            }

            if (dt == null) return;

            decimal totalSum = 0;
            foreach (DataRow r in dt.Rows)
            {
                decimal qty = Convert.ToDecimal(r["Quantity"]);
                decimal price = Convert.ToDecimal(r["UnitPrice"]);
                decimal tot = Convert.ToDecimal(r["Total"]);
                totalSum += tot;

                dgItems.Rows.Add(r["ProductName"], qty.ToString("N0"), r["Unit"], price.ToString("N2"), tot.ToString("N2") + " ج");
            }

            lblTotal.Text = $"إجمالي العملية: {totalSum:N2} ج";
        }
    }
}
