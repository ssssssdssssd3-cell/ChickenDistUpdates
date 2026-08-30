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
    /// شاشة ذكية وشاملة لفحص واكتشاف ومعالجة الأكواد والباركودات المكررة ودمج الأصناف
    /// </summary>
    public class FrmDuplicateCodesResolver : Form
    {
        private ComboBox cboFilterType;
        private TextBox txtSearch;
        private Button btnScan, btnAutoFixCodes, btnAutoFixBarcodes, btnMergeSelected, btnEditCode, btnPrintReport, btnClose;
        private DataGridView dgDuplicates;
        private Label lblStats;

        private Color[] _groupColors = new Color[]
        {
            Color.FromArgb(254, 242, 242), // أحمر خفيف
            Color.FromArgb(254, 249, 195), // أصفر خفيف
            Color.FromArgb(238, 242, 255), // أزرق خفيف
            Color.FromArgb(243, 232, 255), // بنفسجي خفيف
            Color.FromArgb(236, 253, 245)  // أخضر خفيف
        };

        public FrmDuplicateCodesResolver()
        {
            InitUI();
            RunScan();
        }

        private void InitUI()
        {
            this.Text = "🔍 أداة فحص ومعالجة وتصحيح الأكواد والباركودات المكررة";
            this.Size = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. شريط العنوان العلوي
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = "🔍 أداة فحص ومعالجة وتصحيح الأكواد والباركودات المكررة في قاعدة البيانات",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 8)
            };

            var lblSub = new Label
            {
                Text = "اكتشاف الأصناف التي تحتوي على نفس الكود أو الباركود أو كود الميزان وحلها تلقائياً أو دمج الأصناف بضغطة زر واحدة.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(15, 33)
            };

            pnlTitle.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // 2. شريط الفلترة والبحث
            var pnlFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = false,
                RightToLeft = RightToLeft.Yes
            };

            pnlFilters.Controls.Add(new Label { Text = "نوع التكرار:", AutoSize = true, Margin = new Padding(3, 7, 0, 0), Font = Theme.FontBold });
            cboFilterType = new ComboBox
            {
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 3, 15, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboFilterType.Items.AddRange(new object[]
            {
                "⚡ كل الأكواد والباركودات المكررة",
                "كود الصنف الأساسي (ProductCode)",
                "باركود الوحدات (Unit Barcodes)",
                "كود الميزان الإلكتروني (ScalePLU)",
                "اسم الصنف المكرر تماماً (ProductName)"
            });
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += (s, e) => RunScan();
            pnlFilters.Controls.Add(cboFilterType);

            pnlFilters.Controls.Add(new Label { Text = "بحث:", AutoSize = true, Margin = new Padding(3, 7, 0, 0), Font = Theme.FontBold });
            txtSearch = new TextBox
            {
                Width = 180,
                Margin = new Padding(3, 3, 15, 0),
                Font = new Font("Segoe UI", 9.5f)
            };
            txtSearch.TextChanged += (s, e) => RunScan();
            pnlFilters.Controls.Add(txtSearch);

            btnScan = Theme.MakeButton("🔄 إعادة الفحص", 0, 0, 120, 30, Theme.Primary);
            btnScan.Click += (s, e) => RunScan();
            pnlFilters.Controls.Add(btnScan);

            lblStats = new Label
            {
                Text = "جاري الفحص...",
                AutoSize = true,
                Margin = new Padding(20, 7, 0, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28)
            };
            pnlFilters.Controls.Add(lblStats);

            // 3. شريط الأزرار والإجراءات السفلية
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 9, 10, 9),
                RightToLeft = RightToLeft.Yes
            };

            btnAutoFixCodes = Theme.MakeButton("⚡ حل تلقائي ذكي لجميع الأكواد المكررة", 0, 0, 260, 35, Theme.Success);
            btnAutoFixCodes.Click += BtnAutoFixCodes_Click;

            btnAutoFixBarcodes = Theme.MakeButton("🏷️ حل وتفريغ الباركودات المكررة", 0, 0, 220, 35, Color.FromArgb(202, 138, 4));
            btnAutoFixBarcodes.Click += BtnAutoFixBarcodes_Click;

            btnMergeSelected = Theme.MakeButton("🔀 دمج صنفين مكررين", 0, 0, 160, 35, Color.FromArgb(79, 70, 229));
            btnMergeSelected.Click += BtnMergeSelected_Click;

            btnEditCode = Theme.MakeButton("✏️ تعديل كود الصنف", 0, 0, 140, 35, Theme.Accent);
            btnEditCode.Click += BtnEditCode_Click;

            btnPrintReport = Theme.MakeButton("🖨️ طباعة تقرير التكرار", 0, 0, 160, 35, Theme.Secondary);
            btnPrintReport.Click += BtnPrintReport_Click;

            btnClose = Theme.MakeButton("إغلاق", 0, 0, 90, 35, Color.FromArgb(100, 116, 139));
            btnClose.Click += (s, e) => this.Close();

            pnlActions.Controls.AddRange(new Control[] { btnAutoFixCodes, btnAutoFixBarcodes, btnMergeSelected, btnEditCode, btnPrintReport, btnClose });

            // 4. جدول عرض الأصناف المكررة
            dgDuplicates = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 28 }
            };

            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", HeaderText = "ID", FillWeight = 30 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 50 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف", FillWeight = 110 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit1Barcode", HeaderText = "باركود 1", FillWeight = 50 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScalePLU", HeaderText = "كود ميزان", FillWeight = 35 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoryName", HeaderText = "التصنيف", FillWeight = 50 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع", FillWeight = 40 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "رصيد المخزن", FillWeight = 45 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalesCount", HeaderText = "فواتير البيع", FillWeight = 40 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasesCount", HeaderText = "فواتير الشراء", FillWeight = 40 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusTrans", HeaderText = "حالة الحركة", FillWeight = 55 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "DuplicateReason", HeaderText = "سبب التكرار", FillWeight = 80 });
            dgDuplicates.Columns.Add(new DataGridViewTextBoxColumn { Name = "GroupKey", Visible = false });

            this.Controls.Add(dgDuplicates);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFilters);
            this.Controls.Add(pnlTitle);
        }

        private string GetSelectedFilterMode()
        {
            switch (cboFilterType.SelectedIndex)
            {
                case 1: return "ProductCode";
                case 2: return "Barcode";
                case 3: return "ScalePLU";
                case 4: return "ProductName";
                default: return "All";
            }
        }

        private void RunScan()
        {
            try
            {
                string mode = GetSelectedFilterMode();
                string q = txtSearch?.Text?.Trim();

                DataTable dt = ProductDuplicateDAL.GetDuplicateProductsReport(mode, q);
                dgDuplicates.Rows.Clear();

                if (dt.Rows.Count == 0)
                {
                    lblStats.Text = "✅ لا توجد أي أكواد أو باركودات مكررة مطابقة في قاعدة البيانات!";
                    lblStats.ForeColor = Color.DarkGreen;
                    btnAutoFixCodes.Enabled = false;
                    btnAutoFixBarcodes.Enabled = false;
                    btnMergeSelected.Enabled = false;
                    return;
                }

                btnAutoFixCodes.Enabled = true;
                btnAutoFixBarcodes.Enabled = true;
                btnMergeSelected.Enabled = true;

                // حساب عدد المجموعات
                var groupsCount = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string lastGroupKey = "";
                int colorIdx = 0;

                foreach (DataRow r in dt.Rows)
                {
                    string groupKey = r["GroupKey"].ToString().Trim();
                    groupsCount.Add(groupKey);

                    if (groupKey != lastGroupKey)
                    {
                        colorIdx = (colorIdx + 1) % _groupColors.Length;
                        lastGroupKey = groupKey;
                    }

                    int hasT = r.Table.Columns.Contains("HasTransactions") ? Convert.ToInt32(r["HasTransactions"]) : 0;
                    int totalT = r.Table.Columns.Contains("TotalTransactions") ? Convert.ToInt32(r["TotalTransactions"]) : 0;
                    string statusText = hasT == 1 ? $"🔒 له حركات ({totalT})" : "⚡ بدون حركات";

                    int rowIdx = dgDuplicates.Rows.Add(
                        r["ProductID"],
                        r["ProductCode"],
                        r["ProductName"],
                        r["Unit1Barcode"] != DBNull.Value ? r["Unit1Barcode"].ToString() : "—",
                        r["ScalePLU"] != DBNull.Value ? r["ScalePLU"].ToString() : "—",
                        r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "عام",
                        Convert.ToDecimal(r["SalePrice"]).ToString("N2"),
                        Convert.ToDecimal(r["CurrentStock"]).ToString("N2"),
                        r["SalesCount"],
                        r["PurchasesCount"],
                        statusText,
                        r["DuplicateReason"],
                        groupKey
                    );

                    dgDuplicates.Rows[rowIdx].DefaultCellStyle.BackColor = _groupColors[colorIdx];
                }

                int withTransCount = 0;
                if (dt.Columns.Contains("HasTransactions"))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (Convert.ToInt32(dr["HasTransactions"]) == 1) withTransCount++;
                    }
                }
                int withoutTransCount = dt.Rows.Count - withTransCount;

                lblStats.Text = $"⚠️ تم اكتشاف [{dt.Rows.Count}] صنف مكرر: [{withTransCount}] محمي (له حركات/رصيد) و [{withoutTransCount}] قابل للتعديل (بدون أي حركات)!";
                lblStats.ForeColor = Color.FromArgb(185, 28, 28);
            }
            catch (Exception ex)
            {
                lblStats.Text = "❌ خطأ أثناء فحص وتجميع الأكواد المكررة: " + ex.Message;
                lblStats.ForeColor = Color.Red;
            }
        }

        private void BtnAutoFixCodes_Click(object sender, EventArgs e)
        {
            string msg = "⚡ هل ترغب في إعادة ترقيم وتصحيح جميع الأكواد المكررة تلقائياً؟\n\n" +
                         "📌 آلية الترقيم والتصحيح الذكية المعتمدة:\n" +
                         "1️⃣ سيتم الاحتفاظ بالكود الأصلي للصنف الأساسي (الأكثر حركات ورصيداً على البرنامج).\n" +
                         "2️⃣ الأصناف المكررة الأخرى (الأقل حركات) سيتم إعادة ترقيمها بأكواد تسلسلية جديدة فريدة تلي آخر كود في قائمة الأصناف.\n" +
                         "3️⃣ سيتم تعديل وتحديث الكود الجديد في جميع حركات ومبيعات ومشتريات وتقارير الصنف تلقائياً لتعمل بكفاءة 100% وبدون أي تعارض.\n\n" +
                         "هل تريد البدء في تصحيح وإعادة ترقيم الأكواد المكررة الآن؟";

            if (MessageBox.Show(msg, "تأكيد تصحيح وإعادة ترقيم الأكواد المكررة", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var result = ProductDuplicateDAL.AutoFixDuplicateProductCodes("ProductCode", onlyModifyZeroTransactions: false);
                MessageBox.Show($"✅ تمت معالجة وإعادة ترقيم [{result.totalFixed}] صنف مكرر بنجاح!\n\n" +
                                $"• تم إبقاء الكود الأصلي للأصناف الأساسية الأكثر نشاطاً.\n" +
                                $"• تم توليد أكواد تسلسلية جديدة للأصناف الأقل حركات وتحديث كافة حركاتها وتقاريرها.", 
                                "تمت المعالجة بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RunScan();
            }
        }

        private void BtnAutoFixBarcodes_Click(object sender, EventArgs e)
        {
            string msg = "🏷️ هل ترغب في معالجة وتفريغ الباركودات المكررة؟\n\n" +
                         "• سيتم إبقاء الباركود على الصنف الأساسي.\n" +
                         "• سيتم تفريغ الباركود المكرر من الأصناف الثانوية لمنع تضارب قارئ الباركود (السكنر).";

            if (MessageBox.Show(msg, "تأكيد معالجة الباركودات", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var result = ProductDuplicateDAL.AutoFixDuplicateBarcodes();
                MessageBox.Show($"✅ تمت معالجة وتصحيح [{result.totalFixed}] باركود مكرر بنجاح.", "تمت المعالجة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RunScan();
            }
        }

        private void BtnMergeSelected_Click(object sender, EventArgs e)
        {
            if (dgDuplicates.SelectedRows.Count != 2)
            {
                MessageBox.Show("يرجى تحديد صفين (صنفين) بالضبط من الجدول لإجراء الدمج بينهما.\n(حدد الصنفين باستخدام زر Ctrl مع النقر).", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row1 = dgDuplicates.SelectedRows[0];
            var row2 = dgDuplicates.SelectedRows[1];

            int id1 = Convert.ToInt32(row1.Cells["ProductID"].Value);
            string name1 = row1.Cells["ProductName"].Value.ToString();
            int sales1 = Convert.ToInt32(row1.Cells["SalesCount"].Value);

            int id2 = Convert.ToInt32(row2.Cells["ProductID"].Value);
            string name2 = row2.Cells["ProductName"].Value.ToString();
            int sales2 = Convert.ToInt32(row2.Cells["SalesCount"].Value);

            // تحديد الصنف الأساسي والصنف المكرر
            int targetID = (sales1 >= sales2) ? id1 : id2;
            string targetName = (sales1 >= sales2) ? name1 : name2;

            int sourceID = (sales1 >= sales2) ? id2 : id1;
            string sourceName = (sales1 >= sales2) ? name2 : name1;

            string confirm = $"هل أنت متأكد من دمج الصنف المكرر:\n" +
                             $"[ID: {sourceID} - {sourceName}]\n\n" +
                             $"إلى الصنف الأساسي المستمر:\n" +
                             $"[ID: {targetID} - {targetName}]؟\n\n" +
                             $"⚡ سيتم ترحيل كافة كميات المخزون وفواتير البيع والشراء والمردودات إلى الصنف الأساسي وإلغاء الصنف المكرر.";

            if (MessageBox.Show(confirm, "تأكيد دمج الصنفين", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (ProductDuplicateDAL.MergeDuplicateProducts(targetID, sourceID, out string err))
                {
                    MessageBox.Show("✅ تم دمج الصنفين بنجاح وترحيل كافة الحركات والمخزون.", "تم الدمج", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RunScan();
                }
                else
                {
                    MessageBox.Show("حدث خطأ أثناء الدمج:\n" + err, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnEditCode_Click(object sender, EventArgs e)
        {
            if (dgDuplicates.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار الصنف المراد تعديل كوده أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selRow = dgDuplicates.SelectedRows[0];
            int productID = Convert.ToInt32(selRow.Cells["ProductID"].Value);
            string currentCode = selRow.Cells["ProductCode"].Value?.ToString() ?? "";
            string name = selRow.Cells["ProductName"].Value.ToString();

            using (var dlg = new Form())
            {
                dlg.Text = $"✏️ تعديل كود الصنف [{name}]";
                dlg.Size = new Size(380, 220);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.RightToLeft = RightToLeft.Yes; dlg.RightToLeftLayout = true;
                dlg.BackColor = Theme.BgMain; dlg.Font = Theme.FontMain;

                dlg.Controls.Add(new Label { Text = "أدخل كود الصنف الجديد (فريد):", Location = new Point(160, 20), AutoSize = true, Font = Theme.FontBold });
                var txtNew = new TextBox { Location = new Point(20, 50), Width = 320, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = currentCode, TextAlign = HorizontalAlignment.Center };
                dlg.Controls.Add(txtNew);

                var btnSave = Theme.MakeButton("💾 حفظ وتحديث الكود", 170, 110, 170, 36, Theme.Success);
                btnSave.Click += (s, ev) =>
                {
                    if (ProductDuplicateDAL.UpdateProductCodeDirect(productID, txtNew.Text.Trim(), out string err))
                    {
                        MessageBox.Show("✅ تم تحديث كود الصنف بنجاح.", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    else
                    {
                        MessageBox.Show(err, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                var btnCancel = Theme.MakeButton("إلغاء", 20, 110, 100, 36, Color.FromArgb(100, 116, 139));
                btnCancel.Click += (s, ev) => dlg.Close();

                dlg.Controls.AddRange(new Control[] { btnSave, btnCancel });

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RunScan();
                }
            }
        }

        private void BtnPrintReport_Click(object sender, EventArgs e)
        {
            if (dgDuplicates.Rows.Count == 0) return;

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                float y = 30;
                var fontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
                var fontHeader = new Font("Segoe UI", 9f, FontStyle.Bold);
                var fontBody = new Font("Segoe UI", 8.5f);

                g.DrawString("تقرير فحص الأكواد والباركودات المكررة للأصناف", fontTitle, Brushes.DarkSlateBlue, new PointF(ev.PageBounds.Width / 2 - 180, y));
                y += 30;
                g.DrawString($"تاريخ التقرير: {DateTime.Now:yyyy/MM/dd hh:mm tt}   |   إجمالي المكرر: {dgDuplicates.Rows.Count} صنف", fontBody, Brushes.Gray, new PointF(ev.PageBounds.Width / 2 - 140, y));
                y += 30;

                float[] colWidths = { 40, 70, 170, 80, 80, 80, 140 };
                string[] headers = { "ID", "الكود", "اسم الصنف", "سعر البيع", "الرصيد", "المبيعات", "سبب التكرار" };

                float x = 30;
                for (int i = 0; i < headers.Length; i++)
                {
                    g.FillRectangle(Brushes.LightSteelBlue, x, y, colWidths[i], 22);
                    g.DrawRectangle(Pens.SlateGray, x, y, colWidths[i], 22);
                    g.DrawString(headers[i], fontHeader, Brushes.Black, x + 3, y + 3);
                    x += colWidths[i];
                }
                y += 22;

                foreach (DataGridViewRow r in dgDuplicates.Rows)
                {
                    if (y > ev.PageBounds.Height - 60) break;
                    x = 30;
                    string[] vals = {
                        r.Cells["ProductID"].Value.ToString(),
                        r.Cells["ProductCode"].Value.ToString(),
                        r.Cells["ProductName"].Value.ToString(),
                        r.Cells["SalePrice"].Value.ToString(),
                        r.Cells["CurrentStock"].Value.ToString(),
                        r.Cells["SalesCount"].Value.ToString(),
                        r.Cells["DuplicateReason"].Value.ToString()
                    };

                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawRectangle(Pens.LightGray, x, y, colWidths[i], 20);
                        g.DrawString(vals[i], fontBody, Brushes.Black, x + 3, y + 2);
                        x += colWidths[i];
                    }
                    y += 20;
                }
            };

            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700 })
            {
                dlg.ShowDialog(this);
            }
        }
    }
}
