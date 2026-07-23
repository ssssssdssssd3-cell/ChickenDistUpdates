using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmClearanceOffers : Form
    {
        private TextBox txtSearch;
        private ComboBox cboCategoryFilter;
        private CheckBox chkOffersOnly;
        private NumericUpDown nudBatchDiscPct;
        private NumericUpDown nudBatchDiscAmt;
        private Button btnApplyBatchPct;
        private Button btnApplyBatchAmt;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private DataGridView dgProducts;
        private Button btnSaveOffers;
        private Button btnResetOffers;
        private Label lblSummary;

        private DataTable _dtProducts;

        public FrmClearanceOffers()
        {
            InitUI();
            LoadCategories();
            LoadProducts();
        }

        private void InitUI()
        {
            this.Text = "🏷️ إدارة الأوكازيون والتصفيات والعروض";
            this.Size = new Size(1150, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ===== 1. Title Bar =====
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.Primary,
                Padding = new Padding(15, 10, 15, 10)
            };
            var lblTitle = new Label
            {
                Text = "🏷️ شاشة الأوكازيون والتخفيضات العروض (تطبيق الخصم ونسب الخفض)",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12)
            };
            pnlHeader.Controls.Add(lblTitle);

            // ===== 2. Filter & Bulk Action Panel =====
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            // Row 1: Search & Filter
            pnlTop.Controls.Add(new Label { Text = "🔍 بحث سريـع:", Location = new Point(1020, 15), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            txtSearch = new TextBox { Location = new Point(800, 12), Width = 210, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = Theme.FontMain };
            txtSearch.TextChanged += (s, e) => FilterGrid();
            pnlTop.Controls.Add(txtSearch);

            pnlTop.Controls.Add(new Label { Text = "التصنيف:", Location = new Point(730, 15), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold });
            cboCategoryFilter = new ComboBox { Location = new Point(560, 12), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.BgInput, ForeColor = Theme.TextMain, Font = Theme.FontMain };
            cboCategoryFilter.SelectedIndexChanged += (s, e) => FilterGrid();
            pnlTop.Controls.Add(cboCategoryFilter);

            chkOffersOnly = new CheckBox
            {
                Text = "🎯 عرض أصناف الأوكازيون النشطة فقط",
                Location = new Point(280, 13),
                Width = 260,
                ForeColor = Theme.Accent,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                AutoSize = false
            };
            chkOffersOnly.CheckedChanged += (s, e) => FilterGrid();
            pnlTop.Controls.Add(chkOffersOnly);

            btnSelectAll = Theme.MakeButton("☑️ تحديد الكل", 140, 10, 120, 30, Color.FromArgb(70, 80, 95));
            btnSelectAll.Click += (s, e) => SetAllCheckboxes(true);
            btnDeselectAll = Theme.MakeButton("⬛ إلغاء الكل", 10, 10, 125, 30, Color.FromArgb(70, 80, 95));
            btnDeselectAll.Click += (s, e) => SetAllCheckboxes(false);
            pnlTop.Controls.AddRange(new Control[] { btnSelectAll, btnDeselectAll });

            // Row 2: Bulk Discount Apply Controls
            var grpBulk = new Panel
            {
                Location = new Point(10, 52),
                Size = new Size(1110, 52),
                BackColor = Color.FromArgb(240, 243, 246),
                BorderStyle = BorderStyle.FixedSingle
            };

            grpBulk.Controls.Add(new Label { Text = "⚡ تطبيق خصم جماعي للمحدد:", Location = new Point(880, 14), AutoSize = true, ForeColor = Theme.Primary, Font = new Font(Theme.FontMain, FontStyle.Bold) });

            grpBulk.Controls.Add(new Label { Text = "نسبة الخصم %:", Location = new Point(780, 14), AutoSize = true, ForeColor = Theme.TextMain });
            nudBatchDiscPct = new NumericUpDown { Location = new Point(700, 12), Width = 75, Minimum = 0, Maximum = 99, DecimalPlaces = 1, Value = 10, Font = Theme.FontBold, TextAlign = HorizontalAlignment.Center };
            btnApplyBatchPct = Theme.MakeButton("تطبيق %", 595, 9, 95, 32, Theme.Primary);
            btnApplyBatchPct.Click += BtnApplyBatchPct_Click;
            grpBulk.Controls.AddRange(new Control[] { nudBatchDiscPct, btnApplyBatchPct });

            grpBulk.Controls.Add(new Label { Text = "أو خصم قيمة (ج):", Location = new Point(480, 14), AutoSize = true, ForeColor = Theme.TextMain });
            nudBatchDiscAmt = new NumericUpDown { Location = new Point(390, 12), Width = 85, Minimum = 0, Maximum = 99999, DecimalPlaces = 2, Value = 5, Font = Theme.FontBold, TextAlign = HorizontalAlignment.Center };
            btnApplyBatchAmt = Theme.MakeButton("تطبيق مبلغ", 280, 9, 100, 32, Theme.Primary);
            btnApplyBatchAmt.Click += BtnApplyBatchAmt_Click;
            grpBulk.Controls.AddRange(new Control[] { nudBatchDiscAmt, btnApplyBatchAmt });

            btnResetOffers = Theme.MakeButton("🔄 إلغاء الأوكازيون واستعادة السعر الأصلي", 10, 9, 255, 32, Color.FromArgb(180, 50, 50));
            btnResetOffers.Click += BtnResetOffers_Click;
            grpBulk.Controls.Add(btnResetOffers);

            pnlTop.Controls.Add(grpBulk);

            // ===== 3. DataGridView Grid =====
            dgProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Color.FromArgb(226, 232, 240),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var colCheck = new DataGridViewCheckBoxColumn { Name = "colSelect", HeaderText = "اختيار 🏷️", FillWeight = 40 };
            var colID = new DataGridViewTextBoxColumn { Name = "colProductID", Visible = false };
            var colCode = new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "كود الصنف", ReadOnly = true, FillWeight = 60 };
            var colName = new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الصنف", ReadOnly = true, FillWeight = 160 };
            var colCategory = new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "التصنيف", ReadOnly = true, FillWeight = 80 };
            var colOriginalPrice = new DataGridViewTextBoxColumn { Name = "colOriginalPrice", HeaderText = "السعر الأصلي (ج)", ReadOnly = true, FillWeight = 85 };
            var colDiscountPct = new DataGridViewTextBoxColumn { Name = "colDiscountPct", HeaderText = "نسبة الخصم %", FillWeight = 75 };
            var colDiscountAmt = new DataGridViewTextBoxColumn { Name = "colDiscountAmt", HeaderText = "خصم مبلغ (ج)", FillWeight = 75 };
            var colOfferPrice = new DataGridViewTextBoxColumn { Name = "colOfferPrice", HeaderText = "سعر الأوكازيون (ج)", FillWeight = 95 };
            var colStatus = new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "الحالة", ReadOnly = true, FillWeight = 70 };

            dgProducts.Columns.AddRange(new DataGridViewColumn[] {
                colCheck, colID, colCode, colName, colCategory, colOriginalPrice, colDiscountPct, colDiscountAmt, colOfferPrice, colStatus
            });

            dgProducts.CellValueChanged += DgProducts_CellValueChanged;
            dgProducts.CurrentCellDirtyStateChanged += (s, e) => {
                if (dgProducts.IsCurrentCellDirty) dgProducts.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // ===== 4. Footer Panel =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblSummary = new Label
            {
                Text = "إجمالي أصناف الأوكازيون: 0 صنف",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Location = new Point(450, 18),
                AutoSize = true
            };

            btnSaveOffers = Theme.MakeButton("💾 حفظ وتطبيق أسعار الأوكازيون", 15, 10, 260, 40, Theme.Accent);
            btnSaveOffers.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnSaveOffers.Click += BtnSaveOffers_Click;

            pnlFooter.Controls.AddRange(new Control[] { btnSaveOffers, lblSummary });

            // Layout assembly in Docking Z-Order
            this.Controls.Add(dgProducts);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlHeader);

            pnlHeader.SendToBack();
            pnlTop.SendToBack();
            pnlFooter.SendToBack();
            dgProducts.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadCategories()
        {
            try
            {
                cboCategoryFilter.Items.Clear();
                cboCategoryFilter.Items.Add("-- كل التصنيفات --");
                DataTable dt = CategoryDAL.GetAll(true);
                foreach (DataRow r in dt.Rows)
                {
                    cboCategoryFilter.Items.Add(r["CategoryName"].ToString());
                }
                cboCategoryFilter.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadProducts()
        {
            try
            {
                string sql = @"
                    SELECT p.ProductID, p.ProductCode, p.ProductName, 
                           COALESCE(c.CategoryName, '---') AS CategoryName,
                           p.SalePrice, p.OriginalPrice, p.DiscountPct, p.IsOffer
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1
                    ORDER BY p.ProductName";

                _dtProducts = DbHelper.Query(sql);
                PopulateGrid();
            }
            catch (Exception ex)
            {
                AppLogger.Error("LoadProducts in FrmClearanceOffers failed", ex);
            }
        }

        private void PopulateGrid()
        {
            dgProducts.Rows.Clear();
            if (_dtProducts == null) return;

            string search = txtSearch?.Text?.Trim().ToLower() ?? "";
            string catFilter = cboCategoryFilter?.SelectedItem?.ToString() ?? "-- كل التصنيفات --";
            bool offersOnly = chkOffersOnly != null && chkOffersOnly.Checked;

            int offerCount = 0;

            dgProducts.SuspendLayout();
            foreach (DataRow r in _dtProducts.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                string code = r["ProductCode"]?.ToString() ?? "";
                string name = r["ProductName"]?.ToString() ?? "";
                string cat = r["CategoryName"]?.ToString() ?? "";
                decimal currentPrice = Convert.ToDecimal(r["SalePrice"]);
                decimal originalPrice = r["OriginalPrice"] != DBNull.Value && Convert.ToDecimal(r["OriginalPrice"]) > 0 
                                        ? Convert.ToDecimal(r["OriginalPrice"]) : currentPrice;
                decimal discPct = r["DiscountPct"] != DBNull.Value ? Convert.ToDecimal(r["DiscountPct"]) : 0m;
                bool isOffer = r["IsOffer"] != DBNull.Value && Convert.ToBoolean(r["IsOffer"]);

                // Filter logic
                if (!string.IsNullOrEmpty(search) && !name.ToLower().Contains(search) && !code.ToLower().Contains(search))
                    continue;

                if (catFilter != "-- كل التصنيفات --" && cat != catFilter)
                    continue;

                if (offersOnly && !isOffer)
                    continue;

                if (isOffer) offerCount++;

                decimal discAmt = Math.Round(originalPrice * (discPct / 100m), 2);
                decimal offerPrice = isOffer ? currentPrice : Math.Max(0, originalPrice - discAmt);

                int ri = dgProducts.Rows.Add(
                    isOffer,
                    pid,
                    code,
                    name,
                    cat,
                    originalPrice.ToString("N2"),
                    discPct > 0 ? discPct.ToString("N1") : "0.0",
                    discAmt > 0 ? discAmt.ToString("N2") : "0.00",
                    offerPrice.ToString("N2"),
                    isOffer ? "🏷️ أوكازيون" : "عادي"
                );

                if (isOffer)
                {
                    dgProducts.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                    dgProducts.Rows[ri].Cells["colStatus"].Style.ForeColor = Color.DarkGreen;
                    dgProducts.Rows[ri].Cells["colStatus"].Style.Font = new Font(Theme.FontMain, FontStyle.Bold);
                }
            }
            dgProducts.ResumeLayout();

            lblSummary.Text = $"إجمالي الأصناف المعروضة: {dgProducts.Rows.Count} | الأصناف في الأوكازيون حالياً: {offerCount} صنف 🏷️";
        }

        private void FilterGrid()
        {
            PopulateGrid();
        }

        private void SetAllCheckboxes(bool value)
        {
            foreach (DataGridViewRow row in dgProducts.Rows)
            {
                row.Cells["colSelect"].Value = value;
            }
        }

        private void BtnApplyBatchPct_Click(object sender, EventArgs e)
        {
            decimal pct = nudBatchDiscPct.Value;
            int count = 0;
            foreach (DataGridViewRow row in dgProducts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["colSelect"].Value))
                {
                    decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                    decimal discAmt = Math.Round(origPrice * (pct / 100m), 2);
                    decimal finalPrice = Math.Max(0, origPrice - discAmt);

                    row.Cells["colDiscountPct"].Value = pct.ToString("N1");
                    row.Cells["colDiscountAmt"].Value = discAmt.ToString("N2");
                    row.Cells["colOfferPrice"].Value = finalPrice.ToString("N2");
                    count++;
                }
            }

            if (count == 0)
            {
                MessageBox.Show("يرجى تحديد الأصناف المراد تطبيق الخصم عليها أولاً بوضع علامة (✓) في خانة الاختيار.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnApplyBatchAmt_Click(object sender, EventArgs e)
        {
            decimal amt = nudBatchDiscAmt.Value;
            int count = 0;
            foreach (DataGridViewRow row in dgProducts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["colSelect"].Value))
                {
                    decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                    decimal finalPrice = Math.Max(0, origPrice - amt);
                    decimal pct = origPrice > 0 ? Math.Round(((origPrice - finalPrice) / origPrice) * 100m, 1) : 0m;

                    row.Cells["colDiscountPct"].Value = pct.ToString("N1");
                    row.Cells["colDiscountAmt"].Value = amt.ToString("N2");
                    row.Cells["colOfferPrice"].Value = finalPrice.ToString("N2");
                    count++;
                }
            }

            if (count == 0)
            {
                MessageBox.Show("يرجى تحديد الأصناف المراد تطبيق الخصم عليها أولاً بوضع علامة (✓) في خانة الاختيار.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DgProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgProducts.Rows[e.RowIndex];
            string colName = dgProducts.Columns[e.ColumnIndex].Name;

            if (colName == "colDiscountPct")
            {
                decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                if (decimal.TryParse(row.Cells["colDiscountPct"].Value?.ToString(), out decimal pct))
                {
                    decimal discAmt = Math.Round(origPrice * (pct / 100m), 2);
                    decimal finalPrice = Math.Max(0, origPrice - discAmt);
                    row.Cells["colDiscountAmt"].Value = discAmt.ToString("N2");
                    row.Cells["colOfferPrice"].Value = finalPrice.ToString("N2");
                }
            }
            else if (colName == "colDiscountAmt")
            {
                decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                if (decimal.TryParse(row.Cells["colDiscountAmt"].Value?.ToString(), out decimal amt))
                {
                    decimal finalPrice = Math.Max(0, origPrice - amt);
                    decimal pct = origPrice > 0 ? Math.Round(((origPrice - finalPrice) / origPrice) * 100m, 1) : 0m;
                    row.Cells["colDiscountPct"].Value = pct.ToString("N1");
                    row.Cells["colOfferPrice"].Value = finalPrice.ToString("N2");
                }
            }
            else if (colName == "colOfferPrice")
            {
                decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                if (decimal.TryParse(row.Cells["colOfferPrice"].Value?.ToString(), out decimal finalPrice))
                {
                    decimal amt = Math.Max(0, origPrice - finalPrice);
                    decimal pct = origPrice > 0 ? Math.Round((amt / origPrice) * 100m, 1) : 0m;
                    row.Cells["colDiscountPct"].Value = pct.ToString("N1");
                    row.Cells["colDiscountAmt"].Value = amt.ToString("N2");
                }
            }
        }

        private void BtnSaveOffers_Click(object sender, EventArgs e)
        {
            int updatedCount = 0;
            try
            {
                foreach (DataGridViewRow row in dgProducts.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["colSelect"].Value);
                    int pid = Convert.ToInt32(row.Cells["colProductID"].Value);
                    decimal origPrice = Convert.ToDecimal(row.Cells["colOriginalPrice"].Value);
                    decimal offerPrice = Convert.ToDecimal(row.Cells["colOfferPrice"].Value);
                    decimal discPct = Convert.ToDecimal(row.Cells["colDiscountPct"].Value);

                    if (isSelected && offerPrice < origPrice)
                    {
                        DbHelper.Execute(@"
                            UPDATE Products 
                            SET OriginalPrice = COALESCE(OriginalPrice, SalePrice),
                                SalePrice = @offerPrice,
                                DiscountPct = @discPct,
                                IsOffer = 1
                            WHERE ProductID = @pid",
                            DbHelper.P("@offerPrice", offerPrice),
                            DbHelper.P("@discPct", discPct),
                            DbHelper.P("@pid", pid));
                        updatedCount++;
                    }
                }

                MessageBox.Show($"✅ تم تفعيل وتطبيـق الأوكازيون بنجاح لـ ({updatedCount}) صنف!", "تم التفعيل", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProducts();
            }
            catch (Exception ex)
            {
                AppLogger.Error("BtnSaveOffers_Click failed", ex);
                MessageBox.Show("❌ حدث خطأ أثناء حفظ عروض الأوكازيون: " + ex.Message);
            }
        }

        private void BtnResetOffers_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من إيقاف الأوكازيون واستعادة الأسعار الأصلية السابقة للأصناف المحددة؟", "تأكيد إلغاء الأوكازيون", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int resetCount = 0;
            try
            {
                foreach (DataGridViewRow row in dgProducts.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["colSelect"].Value);
                    int pid = Convert.ToInt32(row.Cells["colProductID"].Value);

                    if (isSelected)
                    {
                        DbHelper.Execute(@"
                            UPDATE Products 
                            SET SalePrice = COALESCE(OriginalPrice, SalePrice),
                                DiscountPct = 0,
                                IsOffer = 0
                            WHERE ProductID = @pid",
                            DbHelper.P("@pid", pid));
                        resetCount++;
                    }
                }

                MessageBox.Show($"✅ تم إلغاء الأوكازيون وإعادة الأسعار الأصلية لـ ({resetCount}) صنف!", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProducts();
            }
            catch (Exception ex)
            {
                AppLogger.Error("BtnResetOffers_Click failed", ex);
                MessageBox.Show("❌ حدث خطأ أثناء إيقاف عروض الأوكازيون: " + ex.Message);
            }
        }
    }
}
