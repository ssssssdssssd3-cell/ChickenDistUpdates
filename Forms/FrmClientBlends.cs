using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة إدارة توليفات العملاء — تتيح حفظ التركيبة الخاصة لكل عميل
    /// (المقادير، نوع البن، درجة الطحن، التحويج) وإدراجها في فاتورة البيع
    /// </summary>
    public class FrmClientBlends : Form
    {
        // ── بيانات العميل ──
        public int ClientID { get; }
        public string ClientName { get; }

        // ── نتيجة الاختيار للإدراج في الفاتورة ──
        public string SelectedBlendName { get; private set; }
        public string SelectedRecipeDetails { get; private set; }
        public string SelectedGrindType { get; private set; }
        public string SelectedRoastLevel { get; private set; }
        public int? SelectedBaseProductID { get; private set; }
        public string SelectedBaseProductName { get; private set; }
        public int? SelectedTargetWeightGrams { get; private set; }
        public bool BlendInserted { get; private set; } = false;

        // ── عناصر الواجهة ──
        private DataGridView dgBlends;
        private TextBox txtBlendName, txtGrindType, txtRoastLevel, txtRecipeDetails, txtNotes;
        private NumericUpDown numWeight;
        private Button btnBaseProduct, btnSave, btnDelete, btnInsertToInvoice, btnNew;
        private Label lblBaseProduct;
        private CheckBox chkIsDefault;
        private int _editingBlendID = 0;
        private int? _selectedBaseProductID = null;
        private string _selectedBaseProductName = "";

        public FrmClientBlends(int clientID, string clientName)
        {
            ClientID = clientID;
            ClientName = clientName;
            InitUI();
            LoadBlends();
        }

        private void InitUI()
        {
            this.Text = $"☕ توليفات العميل: {ClientName}";
            this.Size = new Size(920, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── عنوان ──
            var pnlTop = Theme.MakeTitleBar($"☕ إدارة توليفات العميل: {ClientName}",
                "أضف أو عدّل التوليفات والمقادير المخصصة لهذا العميل، ثم ادرجها في الفاتورة مباشرة");
            this.Controls.Add(pnlTop);

            // ── قائمة التوليفات (يسار) ──
            var lblListTitle = new Label
            {
                Text = "📋 قائمة التوليفات المحفوظة:",
                Location = new Point(490, 80),
                Size = new Size(400, 22),
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            this.Controls.Add(lblListTitle);

            dgBlends = new DataGridView
            {
                Location = new Point(490, 106),
                Size = new Size(400, 500),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            dgBlends.DefaultCellStyle.BackColor = Theme.BgCard;
            dgBlends.DefaultCellStyle.ForeColor = Theme.TextMain;
            dgBlends.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            dgBlends.DefaultCellStyle.SelectionForeColor = Color.White;
            dgBlends.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dgBlends.Columns.Add(new DataGridViewTextBoxColumn { Name = "BlendID", Visible = false });
            dgBlends.Columns.Add(new DataGridViewTextBoxColumn { Name = "BlendName", HeaderText = "اسم التوليفة", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgBlends.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetWeight", HeaderText = "الوزن (جم)", Width = 75 });
            dgBlends.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrindType", HeaderText = "الطحن", Width = 80 });
            dgBlends.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsDefault", HeaderText = "مفضلة", Width = 55 });

            dgBlends.SelectionChanged += DgBlends_SelectionChanged;
            dgBlends.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) InsertSelectedBlendToInvoice();
            };
            this.Controls.Add(dgBlends);

            // ── أزرار القائمة ──
            btnInsertToInvoice = Theme.MakeButton("✅ إدراج في الفاتورة (F5)", 490, 610, 240, 36, Theme.Success);
            btnInsertToInvoice.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnInsertToInvoice.Click += (s, e) => InsertSelectedBlendToInvoice();
            this.Controls.Add(btnInsertToInvoice);

            var btnClose = Theme.MakeButton("❌ إغلاق", 742, 610, 148, 36, Color.FromArgb(100, 110, 120));
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            // ══════════════════════════════════════════════
            // ── نموذج التحرير (يمين) ──
            // ══════════════════════════════════════════════
            int lx = 20, fy = 80;
            int fw = 450, fh = 30;

            var lblFormTitle = new Label
            {
                Text = "✏️ إضافة / تعديل توليفة:",
                Location = new Point(lx, fy),
                Size = new Size(fw, 22),
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            this.Controls.Add(lblFormTitle);
            fy += 30;

            // اسم التوليفة
            AddLabel("اسم التوليفة:", lx, fy);
            txtBlendName = MakeTxt(lx, fy + 22, fw, fh);
            this.Controls.Add(txtBlendName);
            fy += 60;

            // الصنف الأساسي المباع
            AddLabel("الصنف الأساسي في الفاتورة:", lx, fy);
            lblBaseProduct = new Label
            {
                Location = new Point(lx, fy + 22),
                Size = new Size(fw - 120, fh),
                ForeColor = Color.White,
                BackColor = Theme.BgInput,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontMain,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "-- اختر الصنف --"
            };
            this.Controls.Add(lblBaseProduct);
            btnBaseProduct = Theme.MakeButton("🔍 اختيار", lx + fw - 115, fy + 22, 110, fh, Theme.Accent);
            btnBaseProduct.Click += BtnBaseProduct_Click;
            this.Controls.Add(btnBaseProduct);
            fy += 60;

            // الوزن الإجمالي ونوع الطحن في سطر واحد
            AddLabel("الوزن الإجمالي (جم):", lx, fy);
            AddLabel("درجة الطحن:", lx + 200, fy);
            numWeight = new NumericUpDown
            {
                Location = new Point(lx, fy + 22),
                Size = new Size(180, fh),
                Minimum = 0,
                Maximum = 5000,
                Value = 250,
                Increment = 50,
                DecimalPlaces = 0,
                BackColor = Theme.BgInput,
                ForeColor = Color.White,
                Font = Theme.FontMain
            };
            this.Controls.Add(numWeight);
            txtGrindType = MakeTxt(lx + 200, fy + 22, fw - 200, fh);
            this.Controls.Add(txtGrindType);
            fy += 60;

            // درجة التحويج
            AddLabel("درجة التحويج / التحميص:", lx, fy);
            txtRoastLevel = MakeTxt(lx, fy + 22, fw, fh);
            this.Controls.Add(txtRoastLevel);
            fy += 60;

            // تفاصيل المقادير — أهم حقل
            AddLabel("☕ مقادير التوليفة (كل مكوّن في سطر — جم ونوع):", lx, fy);
            txtRecipeDetails = new TextBox
            {
                Location = new Point(lx, fy + 22),
                Size = new Size(fw, 110),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Theme.BgInput,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            this.Controls.Add(txtRecipeDetails);
            fy += 120;

            // ملاحظات
            AddLabel("ملاحظات إضافية:", lx, fy);
            txtNotes = MakeTxt(lx, fy + 22, fw, fh);
            this.Controls.Add(txtNotes);
            fy += 58;

            // التوليفة المفضلة
            chkIsDefault = new CheckBox
            {
                Text = "⭐ تعيين كالتوليفة المفضلة لهذا العميل",
                Location = new Point(lx, fy),
                Size = new Size(fw, 26),
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            this.Controls.Add(chkIsDefault);
            fy += 38;

            // أزرار النموذج
            btnNew = Theme.MakeButton("➕ جديد", lx, fy, 100, 36, Theme.Primary);
            btnNew.Click += (s, e) => ClearForm();
            this.Controls.Add(btnNew);

            btnSave = Theme.MakeButton("💾 حفظ التوليفة", lx + 108, fy, 170, 36, Theme.Success);
            btnSave.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnDelete = Theme.MakeButton("🗑 حذف", lx + 286, fy, 100, 36, Color.FromArgb(192, 57, 43));
            btnDelete.Click += BtnDelete_Click;
            btnDelete.Enabled = false;
            this.Controls.Add(btnDelete);

            // F5 اختصار
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) InsertSelectedBlendToInvoice();
                if (e.KeyCode == Keys.Escape) this.Close();
            };
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(460, 20),
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9f)
            };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox MakeTxt(int x, int y, int w, int h)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Theme.BgInput,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontMain
            };
        }

        // ── تحميل قائمة التوليفات ──
        private void LoadBlends()
        {
            dgBlends.Rows.Clear();
            try
            {
                var dt = DbHelper.Query(
                    "SELECT BlendID, BlendName, ISNULL(TargetWeightGrams,0) AS TargetWeightGrams, " +
                    "ISNULL(GrindType,N'') AS GrindType, IsDefault " +
                    "FROM ClientBlends WHERE ClientID=@cid ORDER BY IsDefault DESC, BlendName",
                    DbHelper.P("@cid", ClientID));

                foreach (DataRow r in dt.Rows)
                {
                    int idx = dgBlends.Rows.Add(
                        r["BlendID"],
                        r["BlendName"].ToString(),
                        Convert.ToInt32(r["TargetWeightGrams"]) > 0
                            ? $"{r["TargetWeightGrams"]} جم" : "-",
                        r["GrindType"].ToString(),
                        Convert.ToBoolean(r["IsDefault"])
                    );
                    if (Convert.ToBoolean(r["IsDefault"]))
                        dgBlends.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(35, 70, 35);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmClientBlends.LoadBlends", ex);
            }
        }

        // ── تحديد توليفة من القائمة → تحميلها في النموذج ──
        private void DgBlends_SelectionChanged(object sender, EventArgs e)
        {
            if (dgBlends.SelectedRows.Count == 0) return;
            var row = dgBlends.SelectedRows[0];
            int blendID = Convert.ToInt32(row.Cells["BlendID"].Value);
            LoadBlendIntoForm(blendID);
        }

        private void LoadBlendIntoForm(int blendID)
        {
            try
            {
                var dt = DbHelper.Query(
                    "SELECT b.*, ISNULL(p.ProductName,N'') AS ProductName " +
                    "FROM ClientBlends b " +
                    "LEFT JOIN Products p ON b.BaseProductID = p.ProductID " +
                    "WHERE b.BlendID=@id",
                    DbHelper.P("@id", blendID));

                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                _editingBlendID = blendID;
                txtBlendName.Text = r["BlendName"].ToString();
                numWeight.Value = r["TargetWeightGrams"] != DBNull.Value ? Convert.ToDecimal(r["TargetWeightGrams"]) : 0;
                txtGrindType.Text = r["GrindType"]?.ToString() ?? "";
                txtRoastLevel.Text = r["RoastLevel"]?.ToString() ?? "";
                txtRecipeDetails.Text = r["RecipeDetails"]?.ToString() ?? "";
                txtNotes.Text = r["Notes"]?.ToString() ?? "";
                chkIsDefault.Checked = Convert.ToBoolean(r["IsDefault"]);

                _selectedBaseProductID = r["BaseProductID"] != DBNull.Value ? (int?)Convert.ToInt32(r["BaseProductID"]) : null;
                _selectedBaseProductName = r["ProductName"].ToString();
                lblBaseProduct.Text = string.IsNullOrEmpty(_selectedBaseProductName)
                    ? "-- لم يُحدد صنف --" : _selectedBaseProductName;

                btnDelete.Enabled = true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmClientBlends.LoadBlendIntoForm", ex);
            }
        }

        // ── اختيار الصنف الأساسي ──
        private void BtnBaseProduct_Click(object sender, EventArgs e)
        {
            try
            {
                using var frm = new FrmProductSearch(warehouseID: null, isPurchaseMode: false, defaultShowZeroStock: false);
                if (frm.ShowDialog() == DialogResult.OK && frm.SelectedProductID > 0)
                {
                    _selectedBaseProductID = frm.SelectedProductID;
                    var row = DbHelper.Query("SELECT ProductName FROM Products WHERE ProductID=@id", DbHelper.P("@id", frm.SelectedProductID));
                    _selectedBaseProductName = row.Rows.Count > 0 ? row.Rows[0]["ProductName"].ToString() : "";
                    lblBaseProduct.Text = string.IsNullOrEmpty(_selectedBaseProductName) ? "-- لم يُحدد صنف --" : _selectedBaseProductName;
                }
            }
            catch
            {
                // فالبك: إدخال رقم الصنف يدوياً إذا لم تتوفر شاشة البحث
                using var dlg = new FrmInputDialog("رقم الصنف", "أدخل رقم/كود الصنف الأساسي:");
                if (dlg.ShowDialog() == DialogResult.OK && int.TryParse(dlg.Value, out int pid) && pid > 0)
                {
                    var row = DbHelper.Query("SELECT ProductName FROM Products WHERE ProductID=@id", DbHelper.P("@id", pid));
                    if (row.Rows.Count > 0)
                    {
                        _selectedBaseProductID = pid;
                        _selectedBaseProductName = row.Rows[0]["ProductName"].ToString();
                        lblBaseProduct.Text = _selectedBaseProductName;
                    }
                }
            }
        }

        // ── حفظ التوليفة ──
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBlendName.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم التوليفة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBlendName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRecipeDetails.Text))
            {
                MessageBox.Show("الرجاء إدخال تفاصيل مقادير التوليفة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRecipeDetails.Focus();
                return;
            }

            try
            {
                // إذا تعيّنت مفضلة، اشطب المفضلة الحالية
                if (chkIsDefault.Checked)
                {
                    DbHelper.Execute("UPDATE ClientBlends SET IsDefault=0 WHERE ClientID=@cid",
                        DbHelper.P("@cid", ClientID));
                }

                if (_editingBlendID == 0)
                {
                    // إضافة جديدة
                    DbHelper.Execute(
                        "INSERT INTO ClientBlends (ClientID,BlendName,BaseProductID,TargetWeightGrams,GrindType,RoastLevel,RecipeDetails,IsDefault,Notes,CreatedAt,UpdatedAt) " +
                        "VALUES (@cid,@bn,@bpid,@wt,@gt,@rl,@rd,@isd,@notes,GETDATE(),GETDATE())",
                        DbHelper.P("@cid", ClientID),
                        DbHelper.P("@bn", txtBlendName.Text.Trim()),
                        DbHelper.P("@bpid", _selectedBaseProductID.HasValue ? (object)_selectedBaseProductID.Value : DBNull.Value),
                        DbHelper.P("@wt", numWeight.Value > 0 ? (object)(int)numWeight.Value : DBNull.Value),
                        DbHelper.P("@gt", string.IsNullOrWhiteSpace(txtGrindType.Text) ? DBNull.Value : (object)txtGrindType.Text.Trim()),
                        DbHelper.P("@rl", string.IsNullOrWhiteSpace(txtRoastLevel.Text) ? DBNull.Value : (object)txtRoastLevel.Text.Trim()),
                        DbHelper.P("@rd", txtRecipeDetails.Text.Trim()),
                        DbHelper.P("@isd", chkIsDefault.Checked),
                        DbHelper.P("@notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? DBNull.Value : (object)txtNotes.Text.Trim())
                    );
                }
                else
                {
                    // تعديل
                    DbHelper.Execute(
                        "UPDATE ClientBlends SET BlendName=@bn,BaseProductID=@bpid,TargetWeightGrams=@wt,GrindType=@gt,RoastLevel=@rl,RecipeDetails=@rd,IsDefault=@isd,Notes=@notes,UpdatedAt=GETDATE() " +
                        "WHERE BlendID=@id",
                        DbHelper.P("@bn", txtBlendName.Text.Trim()),
                        DbHelper.P("@bpid", _selectedBaseProductID.HasValue ? (object)_selectedBaseProductID.Value : DBNull.Value),
                        DbHelper.P("@wt", numWeight.Value > 0 ? (object)(int)numWeight.Value : DBNull.Value),
                        DbHelper.P("@gt", string.IsNullOrWhiteSpace(txtGrindType.Text) ? DBNull.Value : (object)txtGrindType.Text.Trim()),
                        DbHelper.P("@rl", string.IsNullOrWhiteSpace(txtRoastLevel.Text) ? DBNull.Value : (object)txtRoastLevel.Text.Trim()),
                        DbHelper.P("@rd", txtRecipeDetails.Text.Trim()),
                        DbHelper.P("@isd", chkIsDefault.Checked),
                        DbHelper.P("@notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? DBNull.Value : (object)txtNotes.Text.Trim()),
                        DbHelper.P("@id", _editingBlendID)
                    );
                }

                LoadBlends();
                ClearForm();
                MessageBox.Show("✅ تم حفظ التوليفة بنجاح!", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmClientBlends.BtnSave_Click", ex);
                MessageBox.Show("حدث خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── حذف توليفة ──
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_editingBlendID == 0) return;
            var ans = MessageBox.Show("هل تريد حذف هذه التوليفة نهائياً؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            if (ans != DialogResult.Yes) return;
            try
            {
                DbHelper.Execute("DELETE FROM ClientBlends WHERE BlendID=@id", DbHelper.P("@id", _editingBlendID));
                LoadBlends();
                ClearForm();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmClientBlends.BtnDelete_Click", ex);
                MessageBox.Show("حدث خطأ أثناء الحذف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── إدراج التوليفة المختارة في الفاتورة ──
        private void InsertSelectedBlendToInvoice()
        {
            if (dgBlends.SelectedRows.Count == 0)
            {
                MessageBox.Show("الرجاء تحديد توليفة من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int blendID = Convert.ToInt32(dgBlends.SelectedRows[0].Cells["BlendID"].Value);

            try
            {
                var dt = DbHelper.Query(
                    "SELECT b.*, ISNULL(p.ProductName,N'') AS ProductName " +
                    "FROM ClientBlends b " +
                    "LEFT JOIN Products p ON b.BaseProductID = p.ProductID " +
                    "WHERE b.BlendID=@id",
                    DbHelper.P("@id", blendID));

                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                SelectedBlendName = r["BlendName"].ToString();
                SelectedRecipeDetails = r["RecipeDetails"]?.ToString() ?? "";
                SelectedGrindType = r["GrindType"]?.ToString() ?? "";
                SelectedRoastLevel = r["RoastLevel"]?.ToString() ?? "";
                SelectedBaseProductID = r["BaseProductID"] != DBNull.Value ? (int?)Convert.ToInt32(r["BaseProductID"]) : null;
                SelectedBaseProductName = r["ProductName"].ToString();
                SelectedTargetWeightGrams = r["TargetWeightGrams"] != DBNull.Value ? (int?)Convert.ToInt32(r["TargetWeightGrams"]) : null;
                BlendInserted = true;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmClientBlends.InsertSelectedBlendToInvoice", ex);
            }
        }

        // ── تفريغ النموذج لإضافة جديدة ──
        private void ClearForm()
        {
            _editingBlendID = 0;
            _selectedBaseProductID = null;
            _selectedBaseProductName = "";
            txtBlendName.Text = "";
            txtGrindType.Text = "";
            txtRoastLevel.Text = "";
            txtRecipeDetails.Text = "";
            txtNotes.Text = "";
            numWeight.Value = 250;
            chkIsDefault.Checked = false;
            lblBaseProduct.Text = "-- لم يُحدد صنف --";
            btnDelete.Enabled = false;
            dgBlends.ClearSelection();
            txtBlendName.Focus();
        }

        /// <summary>
        /// الحصول على التوليفة المفضلة للعميل مباشرةً (إذا وجدت) دون فتح الشاشة
        /// </summary>
        public static (bool found, string blendName, string recipe, string grindType, string roastLevel, int? productID, string productName, int? weightGrams)
            GetDefaultBlend(int clientID)
        {
            try
            {
                var dt = DbHelper.Query(
                    "SELECT TOP 1 b.BlendName, b.RecipeDetails, b.GrindType, b.RoastLevel, " +
                    "b.BaseProductID, b.TargetWeightGrams, ISNULL(p.ProductName,N'') AS ProductName " +
                    "FROM ClientBlends b " +
                    "LEFT JOIN Products p ON b.BaseProductID = p.ProductID " +
                    "WHERE b.ClientID=@cid AND b.IsDefault=1",
                    DbHelper.P("@cid", clientID));

                if (dt.Rows.Count == 0)
                    return (false, "", "", "", "", null, "", null);

                var r = dt.Rows[0];
                return (
                    true,
                    r["BlendName"].ToString(),
                    r["RecipeDetails"]?.ToString() ?? "",
                    r["GrindType"]?.ToString() ?? "",
                    r["RoastLevel"]?.ToString() ?? "",
                    r["BaseProductID"] != DBNull.Value ? (int?)Convert.ToInt32(r["BaseProductID"]) : null,
                    r["ProductName"].ToString(),
                    r["TargetWeightGrams"] != DBNull.Value ? (int?)Convert.ToInt32(r["TargetWeightGrams"]) : null
                );
            }
            catch { return (false, "", "", "", "", null, "", null); }
        }

        /// <summary>
        /// عدد التوليفات المحفوظة للعميل (لأغراض Badge وشارة الزر)
        /// </summary>
        public static int GetBlendCount(int clientID)
        {
            try
            {
                var v = DbHelper.Scalar("SELECT COUNT(*) FROM ClientBlends WHERE ClientID=@cid",
                    DbHelper.P("@cid", clientID));
                return Convert.ToInt32(v);
            }
            catch { return 0; }
        }
    }

    /// <summary>
    /// نافذة إدخال نص بسيطة (Fallback لاختيار الصنف يدوياً)
    /// </summary>
    internal class FrmInputDialog : Form
    {
        private TextBox txtVal;
        public string Value => txtVal.Text.Trim();

        public FrmInputDialog(string title, string prompt)
        {
            this.Text = title;
            this.Size = new Size(350, 145);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var lbl = new Label { Text = prompt, Location = new Point(10, 12), Size = new Size(310, 20), ForeColor = Theme.TextMain };
            txtVal = new TextBox { Location = new Point(10, 36), Size = new Size(310, 26), BackColor = Theme.BgInput, ForeColor = Color.White };
            var btnOk = new Button { Text = "موافق", Location = new Point(10, 72), Size = new Size(80, 30), DialogResult = DialogResult.OK, BackColor = Theme.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "إلغاء", Location = new Point(100, 72), Size = new Size(80, 30), DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(100, 110, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            this.Controls.AddRange(new Control[] { lbl, txtVal, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
