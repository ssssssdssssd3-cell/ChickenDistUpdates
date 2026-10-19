using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    public partial class FrmProductBenefits : Form
    {
        private TextBox txtSearch;
        private ComboBox cboCategory;
        private DataGridView dgvProducts;
        private Label lblSelectedProduct;
        private TextBox txtDescription;
        private Button btnSaveDesc;
        private Button btnSyncOnline;
        private Button btnClose;
        private Label lblStatus;
        private FlowLayoutPanel flpQuickPhrases;

        private DataTable _dtProducts;
        private int _currentProductId = 0;
        private string _currentProductName = "";

        public FrmProductBenefits()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        private void InitializeComponent()
        {
            this.Text = "📝 فوائد ومكونات ووصف الأصناف للمتجر والمنيو الإلكتروني";
            this.Size = new Size(1020, 700);
            this.MinimumSize = new Size(920, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9.5f);
            this.ForeColor = Color.White;

            // 1. Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitle = new Label
            {
                Text = "✨ إدارة فوائد ومكونات ووصف الأصناف للمتجر والمنيو أونلاين",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblSubtitle = new Label
            {
                Text = "اكتب فوائد ومكونات ومواصفات كل صنف ليراها الزبائن عند الضغط على زر '✨ فوائد ومكونات الصنف' في المتجر والمنيو أونلاين.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            // 2. Toolbar (Search & Filter)
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(12, 8, 12, 8)
            };

            var lblCat = new Label
            {
                Text = "القسم:",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 45,
                TextAlign = ContentAlignment.MiddleRight
            };

            cboCategory = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboCategory.SelectedIndexChanged += (s, e) => FilterProducts();

            var lblSearch = new Label
            {
                Text = "🔍 بحث بالاسم أو الكود:",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 140,
                TextAlign = ContentAlignment.MiddleRight
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => FilterProducts();

            pnlToolbar.Controls.Add(txtSearch);
            pnlToolbar.Controls.Add(lblSearch);
            pnlToolbar.Controls.Add(cboCategory);
            pnlToolbar.Controls.Add(lblCat);

            // 3. Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 8, 16, 8)
            };

            btnSyncOnline = new Button
            {
                Text = "☁️ مزامنة التعديلات مع المتجر أونلاين الآن",
                Size = new Size(260, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnSyncOnline.FlatAppearance.BorderSize = 0;
            btnSyncOnline.Click += async (s, e) =>
            {
                btnSyncOnline.Enabled = false;
                lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
                lblStatus.Text = "⏳ جاري مزامنة الكتالوج مع السحابة...";
                try
                {
                    bool ok = await CloudSyncService.SyncStoreCatalogToFirebaseAsync();
                    if (ok)
                    {
                        lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
                        lblStatus.Text = "✅ تم رفع وتحديث فوائد الأصناف في المتجر أونلاين بنجاح!";
                        MessageBox.Show("تم رفع وتحديث فوائد ووصف الأصناف في المتجر الإلكتروني بنجاح ✅", "تمت المزامنة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblStatus.ForeColor = Color.FromArgb(244, 63, 94);
                        lblStatus.Text = "⚠️ تعذر المزامنة أونلاين، تأكد من الإنترنت.";
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.ForeColor = Color.FromArgb(244, 63, 94);
                    lblStatus.Text = "خطأ: " + ex.Message;
                }
                finally
                {
                    btnSyncOnline.Enabled = true;
                }
            };

            btnClose = new Button
            {
                Text = "إغلاق",
                Size = new Size(100, 38),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(52, 211, 153),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlFooter.Controls.Add(btnSyncOnline);
            pnlFooter.Controls.Add(lblStatus);
            pnlFooter.Controls.Add(btnClose);

            // 4. Main Body (Split 50% / 50%)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            // Left: Grid
            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(30, 41, 59)
            };
            dgvProducts.DefaultCellStyle.BackColor = Color.FromArgb(24, 33, 53);
            dgvProducts.DefaultCellStyle.ForeColor = Color.White;
            dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            dgvProducts.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvProducts.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(203, 213, 225);
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.SelectionChanged += (s, e) => OnProductSelected();

            split.Panel1.Controls.Add(dgvProducts);
            split.Panel1.Padding = new Padding(8);
            split.Panel1.BackColor = Color.FromArgb(15, 23, 42);

            // Right: Editor Panel
            var pnlEditor = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(24, 33, 53)
            };

            lblSelectedProduct = new Label
            {
                Text = "اختر صنفاً من القائمة لكتابة فوائده ومكوناته",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblDescTitle = new Label
            {
                Text = "نص فوائد ومكونات ومواصفات الصنف (يظهر للزبون في نافذة تفاصيل الصنف بالمتجر):",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Quick phrases panel
            var pnlPhrases = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.FromArgb(24, 33, 53)
            };

            var lblQuick = new Label
            {
                Text = "💡 عبارات وفوائد مقترحة سريعة (اضغط للإضافة):",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36),
                TextAlign = ContentAlignment.MiddleRight
            };

            flpQuickPhrases = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(2)
            };

            string[] phrases = new string[]
            {
                "✨ طازج ومحضر يومياً بأعلى معايير الجودة.",
                "🌿 خالي تماماً من المواد الحافظة والزيوت المهدرجة.",
                "🔥 يُقدم ساخناً وطازجاً فور استلام الطلب.",
                "☕ قهوة مختصة 100% بن أرابيكا فاخر بنكهة ساحرة.",
                "💪 غني بالبروتين والعناصر الغذائية المفيدة للجسم.",
                "🍯 محلى طبيعياً بالعسل وخالي من السكر المكرر.",
                "🥗 مكونات طبيعية 100% منتقاة بعناية فائقة.",
                "❄️ يُحفظ في درجة حرارة باردة للحفاظ على النكهة والقوام."
            };

            foreach (var phrase in phrases)
            {
                var btnChip = new Button
                {
                    Text = phrase,
                    AutoSize = true,
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3)
                };
                btnChip.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                btnChip.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtDescription.Text))
                        txtDescription.Text = phrase;
                    else
                        txtDescription.Text += Environment.NewLine + phrase;
                    txtDescription.SelectionStart = txtDescription.Text.Length;
                    txtDescription.Focus();
                };
                flpQuickPhrases.Controls.Add(btnChip);
            }

            pnlPhrases.Controls.Add(flpQuickPhrases);
            pnlPhrases.Controls.Add(lblQuick);

            // Save Desc Button panel
            var pnlSaveDesc = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.FromArgb(24, 33, 53)
            };

            btnSaveDesc = new Button
            {
                Text = "💾 حفظ وصف وفوائد هذا الصنف",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveDesc.FlatAppearance.BorderSize = 0;
            btnSaveDesc.Click += (s, e) => SaveCurrentProductDescription();

            pnlSaveDesc.Controls.Add(btnSaveDesc);

            pnlEditor.Controls.Add(txtDescription);
            pnlEditor.Controls.Add(pnlPhrases);
            pnlEditor.Controls.Add(pnlSaveDesc);
            pnlEditor.Controls.Add(lblDescTitle);
            pnlEditor.Controls.Add(lblSelectedProduct);

            split.Panel2.Controls.Add(pnlEditor);
            split.Panel2.Padding = new Padding(8);
            split.Panel2.BackColor = Color.FromArgb(15, 23, 42);

            this.Controls.Add(split);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private void LoadCategories()
        {
            try
            {
                cboCategory.Items.Clear();
                cboCategory.Items.Add("جميع الأقسام");

                DataTable dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WITH (NOLOCK) WHERE ISNULL(IsActive, 1) = 1 ORDER BY CategoryName ASC");
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        cboCategory.Items.Add(r["CategoryName"]?.ToString() ?? "");
                    }
                }
                cboCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadProducts()
        {
            try
            {
                DbHelper.EnsureProductImagesColumnsExist();

                string sql = @"
                    SELECT 
                        p.ProductID,
                        p.ProductCode,
                        p.ProductName,
                        c.CategoryName,
                        ISNULL(p.RetailPrice, 0) AS RetailPrice,
                        ISNULL(p.OnlineDescription, '') AS OnlineDescription,
                        CASE WHEN ISNULL(LTRIM(RTRIM(p.OnlineDescription)), '') <> '' THEN N'✅ مسجل' ELSE N'⚪ فارغ' END AS DescStatus
                    FROM Products p WITH (NOLOCK)
                    LEFT JOIN Categories c WITH (NOLOCK) ON p.CategoryID = c.CategoryID
                    WHERE ISNULL(p.IsActive, 1) = 1
                    ORDER BY c.CategoryName ASC, p.ProductName ASC";

                _dtProducts = DbHelper.Query(sql);
                FilterProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الأصناف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterProducts()
        {
            if (_dtProducts == null) return;

            string search = txtSearch.Text.Trim().Replace("'", "''");
            string cat = cboCategory.SelectedItem?.ToString() ?? "جميع الأقسام";

            string filter = "1=1";
            if (!string.IsNullOrEmpty(search))
            {
                filter += $" AND (ProductName LIKE '%{search}%' OR ProductCode LIKE '%{search}%')";
            }
            if (!string.IsNullOrEmpty(cat) && cat != "جميع الأقسام")
            {
                filter += $" AND CategoryName = '{cat.Replace("'", "''")}'";
            }

            var dv = new DataView(_dtProducts)
            {
                RowFilter = filter
            };

            dgvProducts.DataSource = dv;

            if (dgvProducts.Columns["ProductID"] != null)
                dgvProducts.Columns["ProductID"].Visible = false;

            if (dgvProducts.Columns["OnlineDescription"] != null)
                dgvProducts.Columns["OnlineDescription"].Visible = false;

            if (dgvProducts.Columns["ProductCode"] != null)
            {
                dgvProducts.Columns["ProductCode"].HeaderText = "الكود";
                dgvProducts.Columns["ProductCode"].Width = 85;
            }

            if (dgvProducts.Columns["ProductName"] != null)
            {
                dgvProducts.Columns["ProductName"].HeaderText = "اسم الصنف";
                dgvProducts.Columns["ProductName"].FillWeight = 160;
            }

            if (dgvProducts.Columns["CategoryName"] != null)
            {
                dgvProducts.Columns["CategoryName"].HeaderText = "القسم";
                dgvProducts.Columns["CategoryName"].Width = 110;
            }

            if (dgvProducts.Columns["RetailPrice"] != null)
            {
                dgvProducts.Columns["RetailPrice"].HeaderText = "السعر";
                dgvProducts.Columns["RetailPrice"].Width = 80;
                dgvProducts.Columns["RetailPrice"].DefaultCellStyle.Format = "N2";
            }

            if (dgvProducts.Columns["DescStatus"] != null)
            {
                dgvProducts.Columns["DescStatus"].HeaderText = "حالة الفوائد";
                dgvProducts.Columns["DescStatus"].Width = 95;
            }

            OnProductSelected();
        }

        private void OnProductSelected()
        {
            if (dgvProducts.CurrentRow != null && dgvProducts.CurrentRow.DataBoundItem is DataRowView drv)
            {
                _currentProductId = Convert.ToInt32(drv["ProductID"]);
                _currentProductName = drv["ProductName"]?.ToString() ?? "";
                string desc = drv["OnlineDescription"]?.ToString() ?? "";

                lblSelectedProduct.Text = $"📌 صنف: {_currentProductName}";
                txtDescription.Text = desc;
                btnSaveDesc.Enabled = true;
            }
            else
            {
                _currentProductId = 0;
                _currentProductName = "";
                lblSelectedProduct.Text = "اختر صنفاً من القائمة لتعديل فوائده ومكوناته";
                txtDescription.Text = "";
                btnSaveDesc.Enabled = false;
            }
        }

        private void SaveCurrentProductDescription()
        {
            if (_currentProductId <= 0) return;

            string desc = txtDescription.Text.Trim();
            try
            {
                DbHelper.Execute(
                    "UPDATE Products SET OnlineDescription = @desc WHERE ProductID = @id",
                    DbHelper.P("@desc", string.IsNullOrEmpty(desc) ? (object)DBNull.Value : desc),
                    DbHelper.P("@id", _currentProductId)
                );

                // Update in-memory DataTable
                if (_dtProducts != null)
                {
                    DataRow[] found = _dtProducts.Select($"ProductID = {_currentProductId}");
                    if (found.Length > 0)
                    {
                        found[0]["OnlineDescription"] = desc;
                        found[0]["DescStatus"] = string.IsNullOrWhiteSpace(desc) ? "⚪ فارغ" : "✅ مسجل";
                    }
                }

                lblStatus.ForeColor = Color.FromArgb(52, 211, 153);
                lblStatus.Text = $"✅ تم حفظ وصف وفوائد صنف [{_currentProductName}] بنجاح!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل حفظ الوصف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
