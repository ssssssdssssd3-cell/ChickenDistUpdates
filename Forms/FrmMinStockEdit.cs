using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmMinStockEdit : Form
    {
        private DataGridView dgProducts;
        private TextBox txtSearch;
        private ComboBox cboCategory;
        private CheckBox chkBelowMinOnly;
        private Button btnFilter, btnResetFilter, btnApplyBulk, btnSave, btnClose;
        private NumericUpDown nudBulkValue;
        private Label lblCountInfo, lblModifiedCount;
        private Timer _searchTimer;
        private DataTable _dtProducts;
        private HashSet<int> _modifiedProductIDs = new HashSet<int>();

        public FrmMinStockEdit()
        {
            _searchTimer = new Timer { Interval = 250 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); ApplyFilter(); };

            InitUI();
            LoadCategories();
            LoadData();
        }

        private void InitUI()
        {
            this.Text = "🎯 إدارة وتعديل حد طلب الأصناف";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Title Bar
            var titleBar = Theme.MakeTitleBar("🎯 تعديل حد طلب الأصناف (النواقص والحد الأدنى)",
                "شاشة سريعة لعرض وتعديل حد الطلب للأصناف وتصفية الأصناف التي يقل رصيدها عن الحد الأدنى");
            this.Controls.Add(titleBar);

            // Filter Header Panel
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10)
            };

            Label lblSearch = new Label
            {
                Text = "🔍 بحث سريع:",
                AutoSize = true,
                Location = new Point(15, 20),
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };

            txtSearch = new TextBox
            {
                Location = new Point(105, 17),
                Width = 220,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontNormal
            };
            txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            Label lblCat = new Label
            {
                Text = "📂 التصنيف:",
                AutoSize = true,
                Location = new Point(345, 20),
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };

            cboCategory = new ComboBox
            {
                Location = new Point(430, 17),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontNormal
            };
            cboCategory.SelectedIndexChanged += (s, e) => ApplyFilter();

            chkBelowMinOnly = new CheckBox
            {
                Text = "⚠️ الأصناف أقل من حد الطلب فقط (النواقص)",
                AutoSize = true,
                Location = new Point(630, 19),
                ForeColor = Color.FromArgb(220, 53, 69),
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            chkBelowMinOnly.CheckedChanged += (s, e) => ApplyFilter();

            btnFilter = Theme.MakeButton("تصفية", Theme.Primary);
            btnFilter.Size = new Size(80, 32);
            btnFilter.Location = new Point(940, 15);
            btnFilter.Click += (s, e) => ApplyFilter();

            btnResetFilter = Theme.MakeButton("إعادة ضبط", Color.FromArgb(108, 117, 125));
            btnResetFilter.Size = new Size(90, 32);
            btnResetFilter.Location = new Point(1030, 15);
            btnResetFilter.Click += (s, e) => {
                txtSearch.Clear();
                if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
                chkBelowMinOnly.Checked = false;
                ApplyFilter();
            };

            pnlFilter.Controls.AddRange(new Control[] {
                lblSearch, txtSearch, lblCat, cboCategory, chkBelowMinOnly, btnFilter, btnResetFilter
            });
            this.Controls.Add(pnlFilter);

            // Bulk Tools Bar
            Panel pnlBulk = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(12, 8, 12, 8)
            };

            Label lblBulkTitle = new Label
            {
                Text = "⚡ تعديل جماعي للحد الأدنى القائم:",
                AutoSize = true,
                Location = new Point(15, 14),
                Font = Theme.FontBold,
                ForeColor = Theme.TextMain
            };

            nudBulkValue = new NumericUpDown
            {
                Location = new Point(230, 12),
                Width = 100,
                Minimum = 0,
                Maximum = 999999,
                DecimalPlaces = 2,
                Value = 10,
                Font = Theme.FontNormal
            };

            btnApplyBulk = Theme.MakeButton("تطبيق القيمة على الظاهر بالجدول", Color.FromArgb(13, 202, 240));
            btnApplyBulk.ForeColor = Color.Black;
            btnApplyBulk.Size = new Size(220, 32);
            btnApplyBulk.Location = new Point(345, 9);
            btnApplyBulk.Click += (s, e) => ApplyBulkLimit();

            lblCountInfo = new Label
            {
                Text = "عدد الأصناف المعروضة: 0",
                AutoSize = true,
                Location = new Point(585, 14),
                Font = Theme.FontNormal,
                ForeColor = Theme.TextSub
            };

            lblModifiedCount = new Label
            {
                Text = "الأصناف المعدلة غير المحفوظة: 0",
                AutoSize = true,
                Location = new Point(780, 14),
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(255, 102, 0)
            };

            pnlBulk.Controls.AddRange(new Control[] {
                lblBulkTitle, nudBulkValue, btnApplyBulk, lblCountInfo, lblModifiedCount
            });
            this.Controls.Add(pnlBulk);

            // Grid View
            dgProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                RowHeadersVisible = false
            };
            Theme.StyleGrid(dgProducts);

            SetupGridColumns();
            dgProducts.CellValueChanged += DgProducts_CellValueChanged;
            dgProducts.CellFormatting += DgProducts_CellFormatting;

            this.Controls.Add(dgProducts);

            // Bottom Footer Bar
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            btnSave = Theme.MakeButton("💾 حفظ كافة التغييرات", Theme.Success);
            btnSave.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSave.Size = new Size(180, 40);
            btnSave.Location = new Point(15, 10);
            btnSave.Click += (s, e) => SaveChanges();

            btnClose = Theme.MakeButton("إغلاق الشاشة", Color.FromArgb(108, 117, 125));
            btnClose.Size = new Size(120, 40);
            btnClose.Location = new Point(205, 10);
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnClose });
            this.Controls.Add(pnlFooter);
        }

        private void SetupGridColumns()
        {
            dgProducts.Columns.Clear();

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductID",
                DataPropertyName = "ProductID",
                Visible = false
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductCode",
                DataPropertyName = "ProductCode",
                HeaderText = "كود الصنف",
                Width = 110,
                ReadOnly = true
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProductName",
                DataPropertyName = "ProductName",
                HeaderText = "اسم الصنف",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CategoryName",
                DataPropertyName = "CategoryName",
                HeaderText = "التصنيف",
                Width = 140,
                ReadOnly = true
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Unit",
                DataPropertyName = "Unit",
                HeaderText = "الوحدة",
                Width = 90,
                ReadOnly = true
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BookQty",
                DataPropertyName = "BookQty",
                HeaderText = "الرصيد الحالي",
                Width = 120,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Format = "N2",
                    Font = Theme.FontBold
                }
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MinStockLimit",
                DataPropertyName = "MinStockLimit",
                HeaderText = "حد الطلب (الحد الأدنى) ✏️",
                Width = 170,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Format = "N2",
                    BackColor = Color.FromArgb(255, 250, 235),
                    ForeColor = Color.DarkBlue,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                }
            });

            dgProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StockStatus",
                HeaderText = "حالة الرصيد",
                Width = 130,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = Theme.FontBold
                }
            });
        }

        private void LoadCategories()
        {
            try
            {
                DataTable dtCat = CategoryDAL.GetAll();
                DataTable dtCombo = new DataTable();
                dtCombo.Columns.Add("CategoryID", typeof(int));
                dtCombo.Columns.Add("CategoryName", typeof(string));

                dtCombo.Rows.Add(0, "--- كل التصنيفات ---");
                foreach (DataRow r in dtCat.Rows)
                {
                    dtCombo.Rows.Add(r["CategoryID"], r["CategoryName"]);
                }

                cboCategory.DataSource = dtCombo;
                cboCategory.DisplayMember = "CategoryName";
                cboCategory.ValueMember = "CategoryID";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Err loading categories in FrmMinStockEdit", ex);
            }
        }

        private void LoadData()
        {
            try
            {
                _dtProducts = InventoryDAL.GetStock(warehouseID: null, searchTerm: "", belowMinOnly: false, hideZeroStock: false, expiryOnly: false, categoryID: null, maxRows: 2000);
                if (_dtProducts != null)
                {
                    if (!_dtProducts.Columns.Contains("OriginalMinStockLimit"))
                    {
                        _dtProducts.Columns.Add("OriginalMinStockLimit", typeof(decimal));
                        foreach (DataRow row in _dtProducts.Rows)
                        {
                            decimal minLimit = row["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(row["MinStockLimit"]) : 0m;
                            row["OriginalMinStockLimit"] = minLimit;
                        }
                    }
                }
                _modifiedProductIDs.Clear();
                UpdateModifiedCountLabel();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل بيانات الأصناف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_dtProducts == null) return;

            string filter = "1=1";
            string search = txtSearch.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(search))
            {
                filter += $" AND (ProductName LIKE '%{search}%' OR ProductCode LIKE '%{search}%' OR PartNumber LIKE '%{search}%')";
            }

            if (cboCategory.SelectedValue != null && Convert.ToInt32(cboCategory.SelectedValue) > 0)
            {
                filter += $" AND CategoryID = {cboCategory.SelectedValue}";
            }

            if (chkBelowMinOnly.Checked)
            {
                filter += " AND (MinStockLimit > 0 AND BookQty <= MinStockLimit)";
            }

            DataView dv = new DataView(_dtProducts)
            {
                RowFilter = filter,
                Sort = "ProductName ASC"
            };

            dgProducts.DataSource = dv;
            lblCountInfo.Text = $"عدد الأصناف المعروضة: {dv.Count} من أصل {_dtProducts.Rows.Count}";
        }

        private void DgProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (dgProducts.Columns[e.ColumnIndex].Name == "MinStockLimit")
            {
                DataGridViewRow row = dgProducts.Rows[e.RowIndex];
                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                decimal newVal = Convert.ToDecimal(row.Cells["MinStockLimit"].Value ?? 0);
                
                // Find matching row in _dtProducts
                DataRow[] foundRows = _dtProducts.Select($"ProductID = {pid}");
                if (foundRows.Length > 0)
                {
                    decimal origVal = Convert.ToDecimal(foundRows[0]["OriginalMinStockLimit"]);
                    if (Math.Abs(newVal - origVal) > 0.001m)
                    {
                        _modifiedProductIDs.Add(pid);
                    }
                    else
                    {
                        _modifiedProductIDs.Remove(pid);
                    }
                }

                UpdateModifiedCountLabel();
                dgProducts.InvalidateRow(e.RowIndex);
            }
        }

        private void DgProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgProducts.Rows.Count) return;

            DataGridViewRow row = dgProducts.Rows[e.RowIndex];
            decimal bookQty = Convert.ToDecimal(row.Cells["BookQty"].Value ?? 0);
            decimal minStock = Convert.ToDecimal(row.Cells["MinStockLimit"].Value ?? 0);
            int pid = Convert.ToInt32(row.Cells["ProductID"].Value);

            if (dgProducts.Columns[e.ColumnIndex].Name == "StockStatus")
            {
                if (minStock > 0 && bookQty <= minStock)
                {
                    e.Value = "⚠️ نقص في الرصيد";
                    e.CellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                }
                else if (minStock > 0)
                {
                    e.Value = "✅ كافي";
                    e.CellStyle.ForeColor = Color.FromArgb(25, 135, 84);
                }
                else
                {
                    e.Value = "غير محدد";
                    e.CellStyle.ForeColor = Color.Gray;
                }
            }

            // Highlight under-stocked rows subtly
            if (minStock > 0 && bookQty <= minStock)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 238, 238);
            }
            else
            {
                row.DefaultCellStyle.BackColor = _modifiedProductIDs.Contains(pid) ? Color.FromArgb(235, 247, 255) : Color.White;
            }
        }

        private void ApplyBulkLimit()
        {
            decimal bulkVal = nudBulkValue.Value;
            if (dgProducts.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد أصناف معروضة حالياً بالجدول للتعديل الجماعي!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = MessageBox.Show($"هل أنت تأكد من تطبيق حد طلب بقيمة ({bulkVal:N2}) على جميع الأصناف المعروضة حالياً بالقائمة عدد ({dgProducts.Rows.Count}) صنف؟",
                "تأكيد التعديل الجماعي", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialog == DialogResult.Yes)
            {
                dgProducts.SuspendLayout();
                foreach (DataGridViewRow row in dgProducts.Rows)
                {
                    row.Cells["MinStockLimit"].Value = bulkVal;
                }
                dgProducts.ResumeLayout();
                dgProducts.Refresh();
                MessageBox.Show("تم تطبيق حد الطلب بنجاح على الأصناف المعروضة. يرجى الضغط على (حفظ كافة التغييرات) لاعتماد الحفظ بقاعدة البيانات.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateModifiedCountLabel()
        {
            lblModifiedCount.Text = $"الأصناف المعدلة غير المحفوظة: {_modifiedProductIDs.Count}";
            lblModifiedCount.ForeColor = _modifiedProductIDs.Count > 0 ? Color.FromArgb(255, 102, 0) : Theme.TextSub;
        }

        private void SaveChanges()
        {
            if (_modifiedProductIDs.Count == 0)
            {
                MessageBox.Show("لم تقم بتعديل حد الطلب لأي صنف!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Dictionary<int, decimal> updates = new Dictionary<int, decimal>();
                foreach (int pid in _modifiedProductIDs)
                {
                    DataRow[] found = _dtProducts.Select($"ProductID = {pid}");
                    if (found.Length > 0)
                    {
                        decimal newLimit = Convert.ToDecimal(found[0]["MinStockLimit"]);
                        updates[pid] = newLimit;
                    }
                }

                int savedCount = ProductDAL.BulkUpdateMinStockLimit(updates);
                MessageBox.Show($"تم حفظ تحديث حد الطلب بنجاح لعدد ({savedCount}) صنف!", "تم الحفظ بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh original values
                foreach (int pid in _modifiedProductIDs)
                {
                    DataRow[] found = _dtProducts.Select($"ProductID = {pid}");
                    if (found.Length > 0)
                    {
                        found[0]["OriginalMinStockLimit"] = found[0]["MinStockLimit"];
                    }
                }
                _modifiedProductIDs.Clear();
                UpdateModifiedCountLabel();
                dgProducts.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء حفظ التغييرات: " + ex.Message, "خطأ بالحفظ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
