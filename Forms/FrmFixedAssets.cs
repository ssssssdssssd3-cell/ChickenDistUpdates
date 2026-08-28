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
    /// <summary>
    /// مديول إدارة الأصول الثابتة، محرك الإهلاك الآلي، والصيانة والبيع والتخريد
    /// </summary>
    public class FrmFixedAssets : Form
    {
        private Label lblTotalCostVal, lblTotalAccumVal, lblTotalBookVal, lblActiveCountVal;
        private TabControl tabMain;
        private TabPage tabRegistry, tabDepreciation, tabOperations, tabCategories;

        // Registry Controls
        private DataGridView dgAssets;
        private TextBox txtSearchAsset;
        private ComboBox cboFilterCat, cboFilterStatus;
        private Button btnAddAsset, btnEditAsset, btnDeleteAsset, btnAssetMaint, btnAssetSale, btnAssetScrap, btnPrintAssetCard, btnPrintAllAssets;

        // Depreciation Controls
        private DateTimePicker dtpDepPeriod;
        private Button btnPreviewDep, btnPostDep, btnPrintDepReport;
        private DataGridView dgDepPreview;
        private List<DepreciationPreviewItem> _currentDepList = new List<DepreciationPreviewItem>();

        // Operations Controls
        private DataGridView dgOperations;
        private ComboBox cboOpAssetFilter;
        private Button btnRefreshOps;

        // Categories Controls
        private DataGridView dgCategories;
        private TextBox txtCatName, txtCatNotes;
        private NumericUpDown nudCatRate;
        private ComboBox cboCatMethod;
        private Button btnSaveCat, btnDeleteCat;
        private int _selectedCatID = 0;

        public FrmFixedAssets()
        {
            InitUI();
            LoadCategoriesCombo();
            RefreshDashboard();
            LoadAssetsGrid();
            LoadCategoriesGrid();
        }

        private void InitUI()
        {
            this.Text = "🏢 إدارة الأصول الثابتة وحساب الإهلاك الدوري";
            this.Size = new Size(1180, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. Dashboard Metrics Panel
            var pnlMetrics = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Theme.BgMain,
                Padding = new Padding(10, 6, 10, 6)
            };

            pnlMetrics.Controls.Add(MakeMetricCard("💰 إجمالي تكلفة الأصول", out lblTotalCostVal, Theme.Primary));
            pnlMetrics.Controls.Add(MakeMetricCard("📉 مجمع الإهلاك المتراكم", out lblTotalAccumVal, Theme.Danger));
            pnlMetrics.Controls.Add(MakeMetricCard("🏛️ صافي القيمة الدفترية الحالية", out lblTotalBookVal, Theme.Success));
            pnlMetrics.Controls.Add(MakeMetricCard("📦 عدد الأصول النشطة", out lblActiveCountVal, Color.FromArgb(52, 152, 219)));

            var btnGuide = new Button
            {
                Text = "📖 شرح واستخدام الشاشة",
                Width = 175,
                Height = 56,
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnGuide.FlatAppearance.BorderSize = 0;
            btnGuide.Click += (s, e) => ShowAssetGuideDialog();
            pnlMetrics.Controls.Add(btnGuide);

            // 2. TabControl
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontMain
            };

            tabRegistry = new TabPage("📋 سجل ودليل الأصول الثابتة") { BackColor = Theme.BgMain };
            tabDepreciation = new TabPage("⚡ محرك احتساب وقيد الإهلاك الآلي") { BackColor = Theme.BgMain };
            tabOperations = new TabPage("🔧 سجل العمليات والصيانة والتخريد") { BackColor = Theme.BgMain };
            tabCategories = new TabPage("⚙️ تصنيفات الأصول ونسب الإهلاك") { BackColor = Theme.BgMain };

            BuildRegistryTab(tabRegistry);
            BuildDepreciationTab(tabDepreciation);
            BuildOperationsTab(tabOperations);
            BuildCategoriesTab(tabCategories);

            tabMain.TabPages.AddRange(new TabPage[] { tabRegistry, tabDepreciation, tabOperations, tabCategories });
            Theme.StyleTabControl(tabMain);

            this.Controls.Add(tabMain);
            this.Controls.Add(pnlMetrics);
        }

        private Panel MakeMetricCard(string title, out Label valLabel, Color barColor)
        {
            var pnl = new Panel
            {
                Width = 265,
                Height = 56,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 10, 0)
            };

            var bar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 5,
                BackColor = barColor
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub,
                Location = new Point(5, 5),
                AutoSize = true
            };

            valLabel = new Label
            {
                Text = "0.00 ج",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Location = new Point(5, 26),
                AutoSize = true
            };

            pnl.Controls.AddRange(new Control[] { bar, lblT, valLabel });
            return pnl;
        }

        private void RefreshDashboard()
        {
            var summary = FixedAssetsDAL.GetSummaryMetrics();
            lblTotalCostVal.Text = $"{summary.totalCost:N2} ج";
            lblTotalAccumVal.Text = $"{summary.totalAccumulated:N2} ج";
            lblTotalBookVal.Text = $"{summary.totalBookValue:N2} ج";
            lblActiveCountVal.Text = $"{summary.activeCount} أصل";
        }

        // ══════════════════════════════════════════════════
        // تبويب 1: سجل ودليل الأصول الثابتة
        // ══════════════════════════════════════════════════
        private void BuildRegistryTab(TabPage tab)
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            pnlTop.Controls.Add(new Label { Text = "🔍 بحث:", AutoSize = true, Margin = new Padding(3, 8, 0, 0), Font = Theme.FontBold });
            txtSearchAsset = new TextBox { Width = 160, Margin = new Padding(3, 4, 10, 0) };
            txtSearchAsset.TextChanged += (s, e) => LoadAssetsGrid();
            pnlTop.Controls.Add(txtSearchAsset);

            pnlTop.Controls.Add(new Label { Text = "التصنيف:", AutoSize = true, Margin = new Padding(3, 8, 0, 0), Font = Theme.FontBold });
            cboFilterCat = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 10, 0) };
            cboFilterCat.SelectedIndexChanged += (s, e) => LoadAssetsGrid();
            pnlTop.Controls.Add(cboFilterCat);

            pnlTop.Controls.Add(new Label { Text = "الحالة:", AutoSize = true, Margin = new Padding(3, 8, 0, 0), Font = Theme.FontBold });
            cboFilterStatus = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 4, 10, 0) };
            cboFilterStatus.Items.AddRange(new object[] { "All", "Active", "Sold", "Scrapped", "Maintenance" });
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (s, e) => LoadAssetsGrid();
            pnlTop.Controls.Add(cboFilterStatus);

            btnAddAsset = Theme.MakeButton("➕ إضافة أصل جديد", 0, 0, 140, 32, Theme.Primary);
            btnAddAsset.Click += (s, e) => OpenAssetEditor(0);
            pnlTop.Controls.Add(btnAddAsset);

            btnPrintAllAssets = Theme.MakeButton("🖨️ طباعة سجل الأصول", 0, 0, 145, 32, Theme.Secondary);
            btnPrintAllAssets.Click += BtnPrintAllAssets_Click;
            pnlTop.Controls.Add(btnPrintAllAssets);

            // أزرار العمليات السريعة السفلية
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 7, 10, 7),
                RightToLeft = RightToLeft.Yes
            };

            btnEditAsset = Theme.MakeButton("✏️ تعديل الأصل", 0, 0, 120, 32, Theme.Accent);
            btnEditAsset.Click += (s, e) => { if (GetSelectedAssetID() > 0) OpenAssetEditor(GetSelectedAssetID()); };

            btnAssetMaint = Theme.MakeButton("🔧 تسجيل صيانة", 0, 0, 125, 32, Color.FromArgb(202, 138, 4));
            btnAssetMaint.Click += BtnAssetMaint_Click;

            btnAssetSale = Theme.MakeButton("💵 بيع الأصل", 0, 0, 110, 32, Theme.Success);
            btnAssetSale.Click += BtnAssetSale_Click;

            btnAssetScrap = Theme.MakeButton("♻️ تخريد الأصل", 0, 0, 115, 32, Theme.Danger);
            btnAssetScrap.Click += BtnAssetScrap_Click;

            btnPrintAssetCard = Theme.MakeButton("📄 بطاقة الأصل", 0, 0, 120, 32, Color.FromArgb(70, 70, 70));
            btnPrintAssetCard.Click += BtnPrintAssetCard_Click;

            btnDeleteAsset = Theme.MakeButton("🗑️ حذف", 0, 0, 90, 32, Color.FromArgb(153, 27, 27));
            btnDeleteAsset.Click += BtnDeleteAsset_Click;

            pnlBottom.Controls.AddRange(new Control[] { btnEditAsset, btnAssetMaint, btnAssetSale, btnAssetScrap, btnPrintAssetCard, btnDeleteAsset });

            dgAssets = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgAssets.DoubleClick += (s, e) => { if (GetSelectedAssetID() > 0) OpenAssetEditor(GetSelectedAssetID()); };

            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssetID", Visible = false });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssetCode", HeaderText = "كود الأصل", FillWeight = 40 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssetName", HeaderText = "اسم الأصل الثابت", FillWeight = 90 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 50 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate", HeaderText = "تاريخ الشراء", FillWeight = 45 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCost", HeaderText = "تكلفة الشراء", FillWeight = 45 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAccumulatedDepreciation", HeaderText = "مجمع الإهلاك", FillWeight = 45 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentBookValue", HeaderText = "القيمة الدفترية", FillWeight = 45 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "الموقع / القسم", FillWeight = 50 });
            dgAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", FillWeight = 35 });

            tab.Controls.Add(dgAssets);
            tab.Controls.Add(pnlBottom);
            tab.Controls.Add(pnlTop);
        }

        private int GetSelectedAssetID()
        {
            if (dgAssets.SelectedRows.Count > 0)
            {
                return Convert.ToInt32(dgAssets.SelectedRows[0].Cells["AssetID"].Value);
            }
            return 0;
        }

        private void LoadCategoriesCombo()
        {
            var dt = FixedAssetsDAL.GetAllCategories();
            cboFilterCat.Items.Clear();
            cboFilterCat.Items.Add(new ComboItem(0, "-- كل التصنيفات --"));
            foreach (DataRow r in dt.Rows)
            {
                cboFilterCat.Items.Add(new ComboItem(Convert.ToInt32(r["CategoryID"]), r["CategoryName"].ToString()));
            }
            cboFilterCat.DisplayMember = "Text";
            cboFilterCat.SelectedIndex = 0;
        }

        private void LoadAssetsGrid()
        {
            string status = cboFilterStatus?.SelectedItem?.ToString() ?? "All";
            int catID = (cboFilterCat?.SelectedItem is ComboItem ci) ? ci.ID : 0;
            string q = txtSearchAsset?.Text.Trim();

            DataTable dt = FixedAssetsDAL.GetAllAssets(status, catID, q);
            dgAssets.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["AssetID"]);
                string code = r["AssetCode"].ToString();
                string name = r["AssetName"].ToString();
                string catName = r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "عام";
                DateTime pDate = Convert.ToDateTime(r["PurchaseDate"]);
                decimal cost = Convert.ToDecimal(r["PurchaseCost"]);
                decimal accum = Convert.ToDecimal(r["TotalAccumulatedDepreciation"]);
                decimal bookVal = Convert.ToDecimal(r["CurrentBookValue"]);
                string loc = r["Location"] != DBNull.Value ? r["Location"].ToString() : "";
                string st = r["Status"].ToString();

                int rowIndex = dgAssets.Rows.Add(id, code, name, catName, pDate.ToString("yyyy/MM/dd"),
                    cost.ToString("N2"), accum.ToString("N2"), bookVal.ToString("N2"), loc, st);

                if (st == "Sold") dgAssets.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkGray;
                else if (st == "Scrapped") dgAssets.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Firebrick;
            }

            RefreshDashboard();
        }

        private void OpenAssetEditor(int assetID)
        {
            using (var dlg = new Form())
            {
                dlg.Text = assetID > 0 ? "✏️ تعديل كارت الأصل الثابت" : "➕ إضافة أصل ثابت جديد";
                dlg.Size = new Size(580, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes;
                dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain;
                dlg.Font = Theme.FontMain;

                int y = 15;
                // كود واسم الأصل
                dlg.Controls.Add(new Label { Text = "كود الأصل:", Location = new Point(460, y), AutoSize = true });
                var txtCode = new TextBox { Location = new Point(320, y + 20), Width = 220, Text = FixedAssetsDAL.GenerateAssetCode() };
                dlg.Controls.Add(txtCode);

                dlg.Controls.Add(new Label { Text = "اسم الأصل الثابت (*):", Location = new Point(190, y), AutoSize = true, Font = Theme.FontBold });
                var txtName = new TextBox { Location = new Point(20, y + 20), Width = 280, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
                dlg.Controls.Add(txtName);
                y += 55;

                // التصنيف والموقع
                dlg.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(480, y), AutoSize = true });
                var cboCat = new ComboBox { Location = new Point(300, y + 20), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
                var dtCats = FixedAssetsDAL.GetAllCategories();
                foreach (DataRow cr in dtCats.Rows)
                    cboCat.Items.Add(new ComboItem(Convert.ToInt32(cr["CategoryID"]), cr["CategoryName"].ToString()));
                cboCat.DisplayMember = "Text";
                if (cboCat.Items.Count > 0) cboCat.SelectedIndex = 0;
                dlg.Controls.Add(cboCat);

                dlg.Controls.Add(new Label { Text = "الموقع / القسم:", Location = new Point(190, y), AutoSize = true });
                var txtLocation = new TextBox { Location = new Point(20, y + 20), Width = 260 };
                dlg.Controls.Add(txtLocation);
                y += 55;

                // تاريخ الشراء وتكلفة الشراء
                dlg.Controls.Add(new Label { Text = "تاريخ الشراء:", Location = new Point(460, y), AutoSize = true });
                var dtpPurchase = new DateTimePicker { Location = new Point(320, y + 20), Width = 220, Format = DateTimePickerFormat.Short };
                dlg.Controls.Add(dtpPurchase);

                dlg.Controls.Add(new Label { Text = "تكلفة الشراء الأصلية (ج):", Location = new Point(160, y), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Primary });
                var txtCost = new TextBox { Location = new Point(20, y + 20), Width = 280, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "0.00", TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtCost);
                y += 55;

                // قيمة الخردة والعمر الإنتاجي
                dlg.Controls.Add(new Label { Text = "قيمة الخردة التقديرية (ج):", Location = new Point(400, y), AutoSize = true });
                var txtSalvage = new TextBox { Location = new Point(320, y + 20), Width = 220, Text = "0.00", TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtSalvage);

                dlg.Controls.Add(new Label { Text = "العمر الإنتاجي (بالشهور):", Location = new Point(160, y), AutoSize = true });
                var nudLife = new NumericUpDown { Location = new Point(20, y + 20), Width = 280, Minimum = 1, Maximum = 1200, Value = 60, Font = new Font("Segoe UI", 10.5f), TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(nudLife);
                y += 55;

                // طريقة الإهلاك ونسبة الإهلاك
                dlg.Controls.Add(new Label { Text = "طريقة الإهلاك:", Location = new Point(460, y), AutoSize = true });
                var cboMethod = new ComboBox { Location = new Point(320, y + 20), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
                cboMethod.Items.AddRange(new object[] { "StraightLine (قسط ثابت)", "ReducingBalance (قسط متناقص)" });
                cboMethod.SelectedIndex = 0;
                dlg.Controls.Add(cboMethod);

                dlg.Controls.Add(new Label { Text = "نسبة الإهلاك السنوية (%):", Location = new Point(160, y), AutoSize = true });
                var txtRate = new TextBox { Location = new Point(20, y + 20), Width = 280, Text = "10.0", TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtRate);
                y += 55;

                // الحالة والملاحظات
                dlg.Controls.Add(new Label { Text = "الحالة:", Location = new Point(480, y), AutoSize = true });
                var cboStatus = new ComboBox { Location = new Point(320, y + 20), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
                cboStatus.Items.AddRange(new object[] { "Active", "Maintenance", "Sold", "Scrapped" });
                cboStatus.SelectedIndex = 0;
                dlg.Controls.Add(cboStatus);

                dlg.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(220, y), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, y + 20), Width = 280, Height = 45, Multiline = true };
                dlg.Controls.Add(txtNotes);
                y += 75;

                // ملء البيانات في حالة التعديل
                if (assetID > 0)
                {
                    var row = FixedAssetsDAL.GetAssetByID(assetID);
                    if (row != null)
                    {
                        txtCode.Text = row["AssetCode"].ToString();
                        txtName.Text = row["AssetName"].ToString();
                        if (row["CategoryID"] != DBNull.Value)
                        {
                            int cid = Convert.ToInt32(row["CategoryID"]);
                            for (int i = 0; i < cboCat.Items.Count; i++)
                                if (cboCat.Items[i] is ComboItem ci && ci.ID == cid) { cboCat.SelectedIndex = i; break; }
                        }
                        txtLocation.Text = row["Location"] != DBNull.Value ? row["Location"].ToString() : "";
                        dtpPurchase.Value = Convert.ToDateTime(row["PurchaseDate"]);
                        txtCost.Text = Convert.ToDecimal(row["PurchaseCost"]).ToString("F2");
                        txtSalvage.Text = Convert.ToDecimal(row["SalvageValue"]).ToString("F2");
                        nudLife.Value = Convert.ToInt32(row["UsefulLifeMonths"]);
                        txtRate.Text = Convert.ToDecimal(row["DepreciationRate"]).ToString("F1");
                        cboStatus.SelectedItem = row["Status"].ToString();
                        txtNotes.Text = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "";
                    }
                }

                // زر الحفظ والإلغاء
                var btnSave = Theme.MakeButton("💾 حفظ بيانات الأصل", 290, y, 250, 38, Theme.Success);
                var btnCancel = Theme.MakeButton("إلغاء", 20, y, 120, 38, Color.FromArgb(100, 116, 139));
                btnCancel.Click += (s, e) => dlg.Close();

                btnSave.Click += (s, e) =>
                {
                    try
                    {
                        decimal.TryParse(txtCost.Text.Trim(), out decimal cost);
                        decimal.TryParse(txtSalvage.Text.Trim(), out decimal salvage);
                        decimal.TryParse(txtRate.Text.Trim(), out decimal rate);
                        int life = (int)nudLife.Value;
                        int? catID = (cboCat.SelectedItem is ComboItem ci && ci.ID > 0) ? (int?)ci.ID : null;
                        string method = cboMethod.SelectedIndex == 0 ? "StraightLine" : "ReducingBalance";

                        FixedAssetsDAL.SaveAsset(assetID, txtCode.Text.Trim(), txtName.Text.Trim(), catID,
                            dtpPurchase.Value, cost, salvage, life, rate, method, txtLocation.Text.Trim(),
                            null, cboStatus.SelectedItem?.ToString(), txtNotes.Text.Trim(), Session.EmpID);

                        MessageBox.Show("✅ تم حفظ الأصل الثابت بنجاح.", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                dlg.Controls.AddRange(new Control[] { btnSave, btnCancel });

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadAssetsGrid();
                }
            }
        }

        private void BtnAssetMaint_Click(object sender, EventArgs e)
        {
            int assetID = GetSelectedAssetID();
            if (assetID <= 0) return;

            var row = FixedAssetsDAL.GetAssetByID(assetID);
            if (row == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = $"🔧 تسجيل مصروف صيانة للأصل [{row["AssetName"]}]";
                dlg.Size = new Size(420, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                dlg.Controls.Add(new Label { Text = "تكلفة الصيانة (ج.م):", Location = new Point(280, 20), AutoSize = true });
                var txtCost = new TextBox { Location = new Point(20, 42), Width = 360, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "0.00" };
                dlg.Controls.Add(txtCost);

                dlg.Controls.Add(new Label { Text = "الخزينة المخصوم منها:", Location = new Point(260, 80), AutoSize = true });
                var cboSafe = new ComboBox { Location = new Point(20, 102), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
                var dtSafes = AccountDAL.GetActiveSafeAccounts();
                if (dtSafes == null || dtSafes.Rows.Count == 0)
                {
                    dtSafes = DbHelper.Query("SELECT AccountID, AccountName FROM SafeAccounts");
                }
                if (dtSafes != null && dtSafes.Rows.Count > 0)
                {
                    foreach (DataRow sr in dtSafes.Rows)
                    {
                        int accId = Convert.ToInt32(sr["AccountID"]);
                        string accName = sr["AccountName"].ToString();
                        decimal bal = AccountDAL.GetCashBalance(accId);
                        cboSafe.Items.Add(new ComboItem(accId, $"{accName}  [الرصيد: {bal:N2} ج]"));
                    }
                }
                else
                {
                    cboSafe.Items.Add(new ComboItem(1, "الخزينة الرئيسية"));
                }
                cboSafe.DisplayMember = "Text";
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
                dlg.Controls.Add(cboSafe);

                dlg.Controls.Add(new Label { Text = "تفاصيل وبيان الصيانة:", Location = new Point(260, 140), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, 162), Width = 360, Height = 45, Multiline = true };
                dlg.Controls.Add(txtNotes);

                var btnSave = Theme.MakeButton("✅ تسجيل وخصم المصروف", 190, 225, 190, 36, Theme.Success);
                btnSave.Click += (s, ev) =>
                {
                    decimal.TryParse(txtCost.Text.Trim(), out decimal cost);
                    int safeID = (cboSafe.SelectedItem is ComboItem ci && ci.ID > 0) ? ci.ID : 1;
                    FixedAssetsDAL.RecordMaintenance(assetID, cost, txtNotes.Text.Trim(), safeID, Session.EmpID);
                    MessageBox.Show("✅ تم تسجيل عملية الصيانة والتأثير على الخزينة بنجاح.", "تمت العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAssetsGrid();
            }
        }

        private void BtnAssetSale_Click(object sender, EventArgs e)
        {
            int assetID = GetSelectedAssetID();
            if (assetID <= 0) return;

            var row = FixedAssetsDAL.GetAssetByID(assetID);
            if (row == null) return;

            decimal bookVal = Convert.ToDecimal(row["CurrentBookValue"]);

            using (var dlg = new Form())
            {
                dlg.Text = $"💵 بيع الأصل الثابت [{row["AssetName"]}]";
                dlg.Size = new Size(420, 340);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                dlg.Controls.Add(new Label { Text = $"القيمة الدفترية الحالية: {bookVal:N2} ج", Location = new Point(20, 15), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.Primary });

                dlg.Controls.Add(new Label { Text = "سعر البيع الفعلي (ج.م):", Location = new Point(260, 45), AutoSize = true });
                var txtPrice = new TextBox { Location = new Point(20, 68), Width = 360, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = bookVal.ToString("F2") };
                dlg.Controls.Add(txtPrice);

                dlg.Controls.Add(new Label { Text = "الخزينة المورد إليها:", Location = new Point(270, 105), AutoSize = true });
                var cboSafe = new ComboBox { Location = new Point(20, 128), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
                var dtSafes = AccountDAL.GetActiveSafeAccounts();
                if (dtSafes == null || dtSafes.Rows.Count == 0)
                {
                    dtSafes = DbHelper.Query("SELECT AccountID, AccountName FROM SafeAccounts");
                }
                if (dtSafes != null && dtSafes.Rows.Count > 0)
                {
                    foreach (DataRow sr in dtSafes.Rows)
                    {
                        int accId = Convert.ToInt32(sr["AccountID"]);
                        string accName = sr["AccountName"].ToString();
                        decimal bal = AccountDAL.GetCashBalance(accId);
                        cboSafe.Items.Add(new ComboItem(accId, $"{accName}  [الرصيد: {bal:N2} ج]"));
                    }
                }
                else
                {
                    cboSafe.Items.Add(new ComboItem(1, "الخزينة الرئيسية"));
                }
                cboSafe.DisplayMember = "Text";
                if (cboSafe.Items.Count > 0) cboSafe.SelectedIndex = 0;
                dlg.Controls.Add(cboSafe);

                dlg.Controls.Add(new Label { Text = "ملاحظات البيع وبيانات المشتري:", Location = new Point(210, 165), AutoSize = true });
                var txtNotes = new TextBox { Location = new Point(20, 188), Width = 360, Height = 45, Multiline = true };
                dlg.Controls.Add(txtNotes);

                var btnSave = Theme.MakeButton("✅ إتمام البيع وتوريد النقدية", 180, 250, 200, 36, Theme.Success);
                btnSave.Click += (s, ev) =>
                {
                    decimal.TryParse(txtPrice.Text.Trim(), out decimal price);
                    int safeID = (cboSafe.SelectedItem is ComboItem ci) ? ci.ID : 1;
                    FixedAssetsDAL.RecordSale(assetID, price, txtNotes.Text.Trim(), safeID, Session.EmpID);
                    MessageBox.Show("✅ تم تسجيل بيع الأصل وإيداع القيمة بالخزينة بنجاح.", "تم البيع", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAssetsGrid();
            }
        }

        private void BtnAssetScrap_Click(object sender, EventArgs e)
        {
            int assetID = GetSelectedAssetID();
            if (assetID <= 0) return;

            var row = FixedAssetsDAL.GetAssetByID(assetID);
            if (row == null) return;

            if (MessageBox.Show($"هل أنت متأكد من تخريد الأصل [{row["AssetName"]}] وتحويله لخردة وتصفية قيمته الدفترية؟", "تأكيد التخريد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                FixedAssetsDAL.RecordScrap(assetID, 0m, "تخريد الأصل لانتهاء العمر أو التلف", null, Session.EmpID);
                MessageBox.Show("✅ تم تخريد الأصل بنجاح.", "تم التخريد", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAssetsGrid();
            }
        }

        private void BtnDeleteAsset_Click(object sender, EventArgs e)
        {
            int assetID = GetSelectedAssetID();
            if (assetID <= 0) return;

            if (MessageBox.Show("هل أنت متأكد من حذف هذا الأصل وسجل إهلاكاته نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                FixedAssetsDAL.DeleteAsset(assetID);
                LoadAssetsGrid();
            }
        }

        // ══════════════════════════════════════════════════
        // تبويب 2: محرك احتساب وقيد الإهلاك الآلي
        // ══════════════════════════════════════════════════
        private void BuildDepreciationTab(TabPage tab)
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            pnlTop.Controls.Add(new Label { Text = "📅 شهر الإهلاك:", AutoSize = true, Margin = new Padding(3, 7, 0, 0), Font = Theme.FontBold });
            dtpDepPeriod = new DateTimePicker
            {
                Width = 140,
                CustomFormat = "yyyy-MM",
                Format = DateTimePickerFormat.Custom,
                Margin = new Padding(3, 3, 15, 0)
            };
            pnlTop.Controls.Add(dtpDepPeriod);

            btnPreviewDep = Theme.MakeButton("⚡ فحص واحتساب أقساط الإهلاك", 0, 0, 210, 32, Theme.Primary);
            btnPreviewDep.Click += (s, e) => RunDepreciationPreview();
            pnlTop.Controls.Add(btnPreviewDep);

            btnPostDep = Theme.MakeButton("💾 اعتماد وقيد إهلاك الشهر في الحسابات", 0, 0, 250, 32, Theme.Success);
            btnPostDep.Click += BtnPostDep_Click;
            pnlTop.Controls.Add(btnPostDep);

            btnPrintDepReport = Theme.MakeButton("🖨️ طباعة كشف إهلاك الشهر", 0, 0, 180, 32, Theme.Secondary);
            btnPrintDepReport.Click += BtnPrintDepReport_Click;
            pnlTop.Controls.Add(btnPrintDepReport);

            dgDepPreview = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssetCode", HeaderText = "كود الأصل", FillWeight = 40 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssetName", HeaderText = "اسم الأصل", FillWeight = 90 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 50 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCost", HeaderText = "تكلفة الشراء", FillWeight = 45 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentBookValue", HeaderText = "القيمة الحالية", FillWeight = 45 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "MonthlyDepreciation", HeaderText = "قسط الشهر (ج)", FillWeight = 45 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "BookValueAfter", HeaderText = "الدفترية بعد القيد", FillWeight = 50 });
            dgDepPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusNote", HeaderText = "الحالة / ملاحظات", FillWeight = 70 });

            tab.Controls.Add(dgDepPreview);
            tab.Controls.Add(pnlTop);
        }

        private void RunDepreciationPreview()
        {
            string month = dtpDepPeriod.Value.ToString("yyyy-MM");
            _currentDepList = FixedAssetsDAL.PreviewMonthlyDepreciation(month);
            dgDepPreview.Rows.Clear();

            decimal totalMonthlyDep = 0m;
            int eligibleCount = 0;

            foreach (var item in _currentDepList)
            {
                int r = dgDepPreview.Rows.Add(
                    item.AssetCode,
                    item.AssetName,
                    item.CategoryName,
                    item.PurchaseCost.ToString("N2"),
                    item.CurrentBookValue.ToString("N2"),
                    item.MonthlyDepreciation.ToString("N2"),
                    item.BookValueAfter.ToString("N2"),
                    item.Note
                );

                if (!item.IsEligible)
                {
                    dgDepPreview.Rows[r].DefaultCellStyle.ForeColor = Color.DarkGray;
                }
                else
                {
                    dgDepPreview.Rows[r].DefaultCellStyle.ForeColor = Color.DarkGreen;
                    dgDepPreview.Rows[r].DefaultCellStyle.Font = new Font(Theme.FontMain, FontStyle.Bold);
                    totalMonthlyDep += item.MonthlyDepreciation;
                    eligibleCount++;
                }
            }

            MessageBox.Show($"تم فحص واحتساب إهلاك شهر [{month}]:\n• عدد الأصول المستحقة: {eligibleCount}\n• إجمالي قسط الإهلاك المستحق: {totalMonthlyDep:N2} ج", "نتيجة الفحص", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPostDep_Click(object sender, EventArgs e)
        {
            string month = dtpDepPeriod.Value.ToString("yyyy-MM");
            if (_currentDepList.Count == 0)
            {
                RunDepreciationPreview();
            }

            if (MessageBox.Show($"هل ترغب في اعتماد وترحيل قيود إهلاك شهر [{month}] لكافة الأصول المستحقة وتحديث القيمة الدفترية ومجمع الإهلاك؟", "تأكيد الاعتماد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int count = FixedAssetsDAL.PostMonthlyDepreciation(month, _currentDepList, Session.EmpID);
                MessageBox.Show($"✅ تم اعتماد وترحيل إهلاك [{count}] أصل بنجاح.", "تم الترحيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAssetsGrid();
                RunDepreciationPreview();
            }
        }

        // ══════════════════════════════════════════════════
        // تبويب 3: سجل العمليات والصيانة
        // ══════════════════════════════════════════════════
        private void BuildOperationsTab(TabPage tab)
        {
            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            pnlTop.Controls.Add(new Label { Text = "فلترة الأصل:", AutoSize = true, Margin = new Padding(3, 6, 0, 0), Font = Theme.FontBold });
            cboOpAssetFilter = new ComboBox { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cboOpAssetFilter.SelectedIndexChanged += (s, e) => LoadOperationsGrid();
            pnlTop.Controls.Add(cboOpAssetFilter);

            btnRefreshOps = Theme.MakeButton("🔄 تحديث السجل", 0, 0, 130, 30, Theme.Primary);
            btnRefreshOps.Click += (s, e) => { LoadOpAssetsCombo(); LoadOperationsGrid(); };
            pnlTop.Controls.Add(btnRefreshOps);

            dgOperations = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpDate", HeaderText = "التاريخ والوقت", FillWeight = 50 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpType", HeaderText = "نوع العملية", FillWeight = 40 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "المبلغ (ج)", FillWeight = 40 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "SafeName", HeaderText = "الخزينة", FillWeight = 50 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "GainLoss", HeaderText = "أرباح/خسائر رأسمالية", FillWeight = 45 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان والتفاصيل", FillWeight = 110 });
            dgOperations.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "المستخدم", FillWeight = 40 });

            tab.Controls.Add(dgOperations);
            tab.Controls.Add(pnlTop);
        }

        private void LoadOpAssetsCombo()
        {
            var dt = FixedAssetsDAL.GetAllAssets();
            cboOpAssetFilter.Items.Clear();
            cboOpAssetFilter.Items.Add(new ComboItem(0, "-- كل الأصول --"));
            foreach (DataRow r in dt.Rows)
            {
                cboOpAssetFilter.Items.Add(new ComboItem(Convert.ToInt32(r["AssetID"]), $"{r["AssetCode"]} - {r["AssetName"]}"));
            }
            cboOpAssetFilter.DisplayMember = "Text";
            cboOpAssetFilter.SelectedIndex = 0;
        }

        private void LoadOperationsGrid()
        {
            int assetID = (cboOpAssetFilter?.SelectedItem is ComboItem ci) ? ci.ID : 0;
            DataTable dt = FixedAssetsDAL.GetAssetOperations(assetID);
            dgOperations.Rows.Clear();

            foreach (DataRow r in dt.Rows)
            {
                DateTime dtOp = Convert.ToDateTime(r["OpDate"]);
                string type = r["OpType"].ToString();
                decimal amt = Convert.ToDecimal(r["Amount"]);
                string safe = r["SafeName"] != DBNull.Value ? r["SafeName"].ToString() : "—";
                decimal gl = Convert.ToDecimal(r["GainLossAmount"]);
                string notes = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : "";
                string user = r["UserName"] != DBNull.Value ? r["UserName"].ToString() : "";

                dgOperations.Rows.Add(dtOp.ToString("yyyy/MM/dd hh:mm tt"), type, amt.ToString("N2"), safe, gl != 0 ? gl.ToString("N2") : "—", notes, user);
            }
        }

        // ══════════════════════════════════════════════════
        // تبويب 4: تصنيفات الأصول
        // ══════════════════════════════════════════════════
        private void BuildCategoriesTab(TabPage tab)
        {
            var pnlLeft = new Panel { Dock = DockStyle.Right, Width = 380, BackColor = Theme.BgCard, Padding = new Padding(15) };
            int y = 15;

            pnlLeft.Controls.Add(new Label { Text = "اسم التصنيف (*):", Location = new Point(250, y), AutoSize = true, Font = Theme.FontBold });
            txtCatName = new TextBox { Location = new Point(20, y + 22), Width = 340 };
            pnlLeft.Controls.Add(txtCatName);
            y += 55;

            pnlLeft.Controls.Add(new Label { Text = "نسبة الإهلاك السنوية الافتراضية (%):", Location = new Point(140, y), AutoSize = true });
            nudCatRate = new NumericUpDown { Location = new Point(20, y + 22), Width = 340, DecimalPlaces = 2, Value = 10m };
            pnlLeft.Controls.Add(nudCatRate);
            y += 55;

            pnlLeft.Controls.Add(new Label { Text = "طريقة الإهلاك:", Location = new Point(270, y), AutoSize = true });
            cboCatMethod = new ComboBox { Location = new Point(20, y + 22), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCatMethod.Items.AddRange(new object[] { "StraightLine", "ReducingBalance" });
            cboCatMethod.SelectedIndex = 0;
            pnlLeft.Controls.Add(cboCatMethod);
            y += 55;

            pnlLeft.Controls.Add(new Label { Text = "ملاحظات:", Location = new Point(290, y), AutoSize = true });
            txtCatNotes = new TextBox { Location = new Point(20, y + 22), Width = 340, Height = 60, Multiline = true };
            pnlLeft.Controls.Add(txtCatNotes);
            y += 90;

            btnSaveCat = Theme.MakeButton("💾 حفظ التصنيف", 20, y, 340, 36, Theme.Success);
            btnSaveCat.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCatName.Text)) return;
                FixedAssetsDAL.SaveCategory(_selectedCatID, txtCatName.Text.Trim(), nudCatRate.Value, cboCatMethod.SelectedItem.ToString(), txtCatNotes.Text.Trim());
                _selectedCatID = 0;
                txtCatName.Clear();
                txtCatNotes.Clear();
                LoadCategoriesGrid();
                LoadCategoriesCombo();
            };
            pnlLeft.Controls.Add(btnSaveCat);
            y += 44;

            btnDeleteCat = Theme.MakeButton("🗑️ حذف التصنيف المحدد", 20, y, 340, 32, Theme.Danger);
            btnDeleteCat.Click += (s, e) =>
            {
                if (dgCategories.SelectedRows.Count > 0)
                {
                    int id = Convert.ToInt32(dgCategories.SelectedRows[0].Cells["CategoryID"].Value);
                    try
                    {
                        FixedAssetsDAL.DeleteCategory(id);
                        LoadCategoriesGrid();
                        LoadCategoriesCombo();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };
            pnlLeft.Controls.Add(btnDeleteCat);

            dgCategories = new DataGridView
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
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgCategories.SelectionChanged += (s, e) =>
            {
                if (dgCategories.SelectedRows.Count > 0)
                {
                    _selectedCatID = Convert.ToInt32(dgCategories.SelectedRows[0].Cells["CategoryID"].Value);
                    txtCatName.Text = dgCategories.SelectedRows[0].Cells["CategoryName"].Value.ToString();
                    nudCatRate.Value = Convert.ToDecimal(dgCategories.SelectedRows[0].Cells["DefaultDepreciationRate"].Value);
                    cboCatMethod.SelectedItem = dgCategories.SelectedRows[0].Cells["DepreciationMethod"].Value.ToString();
                    txtCatNotes.Text = dgCategories.SelectedRows[0].Cells["Notes"].Value?.ToString() ?? "";
                }
            };

            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryID", Visible = false });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "اسم التصنيف", FillWeight = 80 });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "DefaultDepreciationRate", HeaderText = "نسبة الإهلاك %", FillWeight = 40 });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "DepreciationMethod", HeaderText = "طريقة الإهلاك", FillWeight = 50 });
            dgCategories.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 80 });

            tab.Controls.Add(dgCategories);
            tab.Controls.Add(pnlLeft);
        }

        private void LoadCategoriesGrid()
        {
            var dt = FixedAssetsDAL.GetAllCategories();
            dgCategories.Rows.Clear();
            foreach (DataRow r in dt.Rows)
            {
                dgCategories.Rows.Add(r["CategoryID"], r["CategoryName"], r["DefaultDepreciationRate"], r["DepreciationMethod"], r["Notes"]);
            }
        }

        // ══════════════════════════════════════════════════
        // تقارير الطباعة
        // ══════════════════════════════════════════════════
        private void BtnPrintAllAssets_Click(object sender, EventArgs e)
        {
            var dt = FixedAssetsDAL.GetAllAssets();
            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString("سجل ودليل الأصول الثابتة ومجمع الإهلاك", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 35;
                g.DrawString($"تاريخ التقرير: {DateTime.Now:yyyy/MM/dd hh:mm tt}", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 100, y));
                y += 35;

                float[] colWidths = { 70, 160, 100, 80, 80, 80, 70 };
                string[] headers = { "الكود", "اسم الأصل", "التصنيف", "تكلفة الشراء", "مجمع الإهلاك", "الدفترية", "الحالة" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                decimal totCost = 0m, totAccum = 0m, totBook = 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (y > ev.PageBounds.Height - 80) break;
                    x = 30;
                    decimal cost = Convert.ToDecimal(r["PurchaseCost"]);
                    decimal accum = Convert.ToDecimal(r["TotalAccumulatedDepreciation"]);
                    decimal book = Convert.ToDecimal(r["CurrentBookValue"]);

                    totCost += cost; totAccum += accum; totBook += book;

                    string[] vals = {
                        r["AssetCode"].ToString(),
                        r["AssetName"].ToString(),
                        r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "",
                        $"{cost:N0}",
                        $"{accum:N0}",
                        $"{book:N0}",
                        r["Status"].ToString()
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 20);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 2);
                        x += colWidths[i];
                    }
                    y += 20;
                }

                y += 10;
                g.DrawString($"الإجماليات: تكلفة الأصول: {totCost:N2} ج  |  مجمع الإهلاك: {totAccum:N2} ج  |  صافي القيمة الدفترية: {totBook:N2} ج", fontHeader, Brushes.DarkBlue, 30, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPrintAssetCard_Click(object sender, EventArgs e)
        {
            int assetID = GetSelectedAssetID();
            if (assetID <= 0) return;

            var r = FixedAssetsDAL.GetAssetByID(assetID);
            if (r == null) return;

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 40;
                var fontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
                var fontSub = new Font("Segoe UI", 11f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 10f);

                g.DrawRectangle(Pens.DarkSlateBlue, 30, 20, ev.PageBounds.Width - 60, ev.PageBounds.Height - 40);

                g.DrawString("بطاقة تعريف وسجل أصل ثابت", fontTitle, Brushes.DarkSlateBlue, 40, y);
                y += 40;

                g.DrawString($"كود الأصل: {r["AssetCode"]}   |   اسم الأصل: {r["AssetName"]}", fontSub, Brushes.Black, 40, y);
                y += 30;
                g.DrawString($"التصنيف: {r["CategoryName"]}   |   الموقع: {r["Location"]}", fontBody, Brushes.Black, 40, y);
                y += 25;
                g.DrawString($"تاريخ الشراء: {Convert.ToDateTime(r["PurchaseDate"]):yyyy/MM/dd}   |   تكلفة الشراء: {Convert.ToDecimal(r["PurchaseCost"]):N2} ج", fontBody, Brushes.Black, 40, y);
                y += 25;
                g.DrawString($"مجمع الإهلاك: {Convert.ToDecimal(r["TotalAccumulatedDepreciation"]):N2} ج   |   القيمة الدفترية الحالية: {Convert.ToDecimal(r["CurrentBookValue"]):N2} ج", fontSub, Brushes.DarkGreen, 40, y);
                y += 25;
                g.DrawString($"طريقة الإهلاك: {r["DepreciationMethod"]}   |   النسبة السنوية: {r["DepreciationRate"]}%   |   الحالة: {r["Status"]}", fontBody, Brushes.Black, 40, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 800, Height = 600 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPrintDepReport_Click(object sender, EventArgs e)
        {
            string month = dtpDepPeriod.Value.ToString("yyyy-MM");
            if (_currentDepList.Count == 0) RunDepreciationPreview();

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 9f);

                g.DrawString($"كشف إهلاك الأصول الثابتة لشهر [{month}]", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 160, y));
                y += 35;

                float[] colWidths = { 70, 160, 100, 90, 80, 80, 90 };
                string[] headers = { "الكود", "اسم الأصل", "التصنيف", "الدفترية قبل", "قسط الشهر", "الدفترية بعد", "الحالة" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 24);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 24);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 4, y + 3);
                    x += colWidths[i];
                }
                y += 24;

                decimal totMonth = 0m;
                foreach (var item in _currentDepList)
                {
                    if (y > ev.PageBounds.Height - 80) break;
                    x = 30;
                    if (item.IsEligible) totMonth += item.MonthlyDepreciation;

                    string[] vals = {
                        item.AssetCode,
                        item.AssetName,
                        item.CategoryName,
                        $"{item.CurrentBookValue:N0}",
                        $"{item.MonthlyDepreciation:N2}",
                        $"{item.BookValueAfter:N0}",
                        item.Note
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 20);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 4, y + 2);
                        x += colWidths[i];
                    }
                    y += 20;
                }

                y += 10;
                g.DrawString($"إجمالي قسط إهلاك الشهر: {totMonth:N2} ج", fontHeader, Brushes.DarkGreen, 30, y);
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }

        private void ShowAssetGuideDialog()
        {
            new FrmGuideModal("دليل وشرح إدارة الأصول الثابتة وحساب الإهلاك الدوري", rtb =>
            {
                FrmGuideModal.AppendHeader1(rtb, "🏢 ما هي الأصول الثابتة في النظام؟");
                FrmGuideModal.AppendParagraph(rtb, "الأصول الثابتة هي كافة الممتلكات والمعدات المعمرة التي تشتريها المنشأة لغرض تشغيل النشاط واستمرار العمل والإنتاج (وليس لغرض إعادة بيعها كبضاعة تجارية مباشرة). مثل: السيارات، وسائل النقل، الآلات والمعدات، خطوط الإنتاج، أجهزة الكمبيوتر، الطابعات، الأثاث والمكاتب، الثلاجات والمكيفات، والمباني والإنشاءات.");

                FrmGuideModal.AppendHeader1(rtb, "📋 1. التبويب الأول: سجل ودليل الأصول الثابتة");
                FrmGuideModal.AppendStep(rtb, "1", "إضافة أصل جديد (زر ➕ إضافة أصل):", 
                    "اضغط على زر إضافة أصل، أدخل كود واسم الأصل وتصنيفه، تاريخ الشراء، تكلفة الشراء، القيمة التخريدية المتوقعة (سعر الخردة التقديري بعد انتهاء عمره الإنتاجي)، نسبة الإهلاك السنوية، ومكان الأصل والمسؤول عنه. يمكنك تحديد طريقة السداد (نقداً من الخزينة، أو من حساب بنكي، أو كأصل قائم من بداية النشاط).");
                FrmGuideModal.AppendStep(rtb, "2", "صيانة أصل (زر 🔧 صيانة أصل):", 
                    "لتسجيل أي مبالغ تم إنفاقها على صيانة أو إصلاح أو تغيير قطع غيار للأصل مع خصم القيمة من الخزينة المختارة وتسجيلها في كشف حركات الأصل ومصاريف المنشأة.");
                FrmGuideModal.AppendStep(rtb, "3", "بيع أصل (زر 💰 بيع أصل):", 
                    "عند الاستغناء عن أصل قديم وبيعه، يقوم النظام تلقائياً بمقارنة سعر البيع مع صافي القيمة الدفترية للأصل وقت البيع، واحتساب أرباح أو خسائر رأسمالية ناتجة عن البيع وتوريد القيمة للخزينة.");
                FrmGuideModal.AppendStep(rtb, "4", "تخريد أصل (زر 🗑️ تخريد):", 
                    "في حال تلف الأصل تماماً وعدم صلاحيته للعمل، يتم استبعاده وإثبات قيمته الدفترية المتبقية كخسارة تخريد رسمية وإيقاف إهلاكه.");
                FrmGuideModal.AppendStep(rtb, "5", "طباعة بطاقة الأصل (زر 🖨️):", 
                    "لطباعة تقرير شامل ومفصل لتاريخ الأصل وتكلفته ومجمع إهلاكه وصياناته السابقة.");

                FrmGuideModal.AppendHeader1(rtb, "⚡ 2. التبويب الثاني: محرك احتساب وقيد الإهلاك الآلي");
                FrmGuideModal.AppendParagraph(rtb, "الإهلاك هو الانخفاض التدريجي لقيمة الأصل نتيجة التشغيل والتقادم بمرور الزمن. يقوم النظام بحساب هذا القسط آلياً دون الحاجة لحسابات يدوية معقدة.");
                FrmGuideModal.AppendStep(rtb, "1", "اختيار الفترة الشهرية:", 
                    "حدد التاريخ حتى نهاية الشهر المراد احتساب إهلاكه (مثلاً شهر 08/2026).");
                FrmGuideModal.AppendStep(rtb, "2", "معاينة الإهلاك (زر ⚡ معاينة الإهلاك):", 
                    "يقوم النظام بفحص كل أصل نشط وحساب نصيبه من الإهلاك الشهري بدقة بالغة وفق المعادلة المعتمدة، وعرض التكلفة ومجمع الإهلاك السابق والقسط المستحق وصافي القيمة الدفترية المتبقية.");
                FrmGuideModal.AppendStep(rtb, "3", "قيد وترحيل الإهلاك (زر 💾 قيد وترحيل الإهلاك):", 
                    "عند الضغط على ترحيل، يتم تثبيت الإهلاك للأصول رسمياً، وتخفيض صافي قيمتها الدفترية، وزيادة مجمع الإهلاك المتراكم، وتسجيل مصروف الإهلاك ضمن التكاليف الدورية للمنشأة.");

                FrmGuideModal.AppendHeader1(rtb, "🔧 3. التبويب الثالث: سجل العمليات والصيانة والتخريد");
                FrmGuideModal.AppendParagraph(rtb, "يعرض هذا التبويب أرشيفاً كاملاً وموثقاً لكافة العمليات التي تمت على كل أصل (مصاريف الصيانة والإصلاح، عمليات البيع، التخريد، وحركات الإهلاك) مع إمكانية الفلترة باسم الأصل وتصدير البيانات.");

                FrmGuideModal.AppendHeader1(rtb, "⚙️ 4. التبويب الرابع: تصنيفات الأصول ونسب الإهلاك");
                FrmGuideModal.AppendParagraph(rtb, "تحديد المجموعات الرئيسية للأصول ونسب الإهلاك السنوية الافتراضية، مثل: سيارات ونقل (20%)، أجهزة كمبيوتر وطابعات (25%)، أثاث ومكاتب (10%)، آلات وخطوط إنتاج (15%)، مباني وإنشاءات (5%).");

                FrmGuideModal.AppendTip(rtb, "صافي القيمة الدفترية = تكلفة شراء الأصل - مجمع الإهلاك المتراكم. وعند وصول القيمة الدفترية إلى القيمة التخريدية يتوقف الإهلاك تلقائياً.");
            }).ShowDialog();
        }
    }
}
