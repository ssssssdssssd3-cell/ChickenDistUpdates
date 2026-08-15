using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmWhatsAppPreviewDialog : Form
    {
        private ComboBox cboTemplates;
        private Panel pnlCenter;
        private PictureBox picPreview;
        private RichTextBox txtPreview;
        private Bitmap currentBitmap;
        private string currentText;
        private DataRow saleRow;
        private DataTable saleItems;
        private decimal prevBalance;
        private decimal lastPaymentAmt;
        private DateTime lastPaymentDate;
        private decimal todayPayments;
        private decimal todayReturns;
        private decimal actualCurrentBalance;

        public FrmWhatsAppPreviewDialog(string initialTemplate = null, int? saleID = null)
        {
            InitializeDialog(initialTemplate, saleID);
        }

        private void InitializeDialog(string initialTemplate, int? saleID)
        {
            this.Text = "📱 معاينة قوالب وكروت الواتساب - Pro System";
            this.Size = new Size(760, 840);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(650, 600);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Load data (real or sample)
            LoadSaleData(saleID);

            // ── TOP CONTROL PANEL ─────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgHeader,
                Padding = new Padding(15, 12, 15, 10)
            };

            var lblTpl = new Label
            {
                Text = "اختر نموذج الواتساب للمعاينة:",
                AutoSize = true,
                Location = new Point(480, 20),
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };

            cboTemplates = new ComboBox
            {
                Location = new Point(140, 16),
                Width = 330,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };

            cboTemplates.Items.AddRange(new object[]
            {
                "🖼️ كارت الفاتورة الكلاسيكي الملكي (Royal Navy Card)",
                "🖼️ كارت الفاتورة المودرن الفحمي (Modern Charcoal Card)",
                "🖼️ كارت الفاتورة الشبكي التجاري (Commercial Grid Card)",
                "🖼️ كارت الفاتورة الزمردي الأنيق (Emerald Green Card)",
                "🖼️ كارت الفاتورة الذهبي للشركات (Corporate Gold Card)",
                "💬 النموذج التفصيلي الشامل (رسالة نصية تفصيلية)",
                "💬 النموذج السريع الموجز (رسالة نصية سريعة)",
                "💬 نموذج كشف الحساب والمالية (رسالة نصية مالية)"
            });

            int selIdx = 0;
            string tplToMatch = !string.IsNullOrWhiteSpace(initialTemplate) ? initialTemplate : AppConfig.WhatsAppInvoiceTemplate;
            selIdx = tplToMatch switch
            {
                "ImageCardModern" => 1,
                "ImageCardCommercial" => 2,
                "ImageCardEmerald" => 3,
                "ImageCardGold" => 4,
                "Detailed" => 5,
                "Summary" => 6,
                "Financial" => 7,
                _ => 0
            };
            cboTemplates.SelectedIndex = selIdx;

            cboTemplates.SelectedIndexChanged += (s, e) => RenderSelectedTemplate();

            var btnRefresh = Theme.MakeButton("🔄 تحديث", 20, 15, 100, 32, Theme.Primary);
            btnRefresh.Click += (s, e) => RenderSelectedTemplate();

            pnlTop.Controls.Add(lblTpl);
            pnlTop.Controls.Add(cboTemplates);
            pnlTop.Controls.Add(btnRefresh);

            // ── CENTER PREVIEW PANEL ──────────────────────────
            pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20)
            };

            picPreview = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(20, 20),
                BackColor = Color.Transparent,
                Visible = true
            };

            txtPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.None,
                RightToLeft = RightToLeft.Yes,
                ReadOnly = true,
                Visible = false
            };

            pnlCenter.Controls.Add(picPreview);
            pnlCenter.Controls.Add(txtPreview);

            // ── BOTTOM ACTION BAR ─────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgSearchPanel,
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnCopy = Theme.MakeButton("📋 نسخ للحافظة", Color.FromArgb(37, 211, 102));
            btnCopy.Size = new Size(160, 40);
            btnCopy.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCopy.Dock = DockStyle.Left;
            btnCopy.Click += (s, e) =>
            {
                if (cboTemplates.SelectedIndex < 5 && currentBitmap != null)
                {
                    Clipboard.SetImage(currentBitmap);
                    MessageBox.Show("✅ تم نسخ صورة كارت الفاتورة للحافظة بنجاح!\nيمكنك الآن لصقها في أي محادثة واتساب (Ctrl + V).", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (!string.IsNullOrWhiteSpace(currentText))
                {
                    Clipboard.SetText(currentText);
                    MessageBox.Show("✅ تم نسخ نص الفاتورة للحافظة بنجاح!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnSaveImage = Theme.MakeButton("💾 حفظ الصورة كملف", Color.FromArgb(18, 140, 126));
            btnSaveImage.Size = new Size(160, 40);
            btnSaveImage.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSaveImage.Dock = DockStyle.Left;
            btnSaveImage.Margin = new Padding(8, 0, 0, 0);
            btnSaveImage.Click += (s, e) =>
            {
                if (currentBitmap != null)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg";
                        sfd.FileName = $"Invoice_Card_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        if (sfd.ShowDialog(this) == DialogResult.OK)
                        {
                            currentBitmap.Save(sfd.FileName, sfd.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ? ImageFormat.Jpeg : ImageFormat.Png);
                            MessageBox.Show("✅ تم حفظ صورة كارت الفاتورة بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            };

            var btnSetDefault = Theme.MakeButton("⚙️ تعيين كافتراضي", Color.FromArgb(70, 80, 100));
            btnSetDefault.Size = new Size(150, 40);
            btnSetDefault.Dock = DockStyle.Left;
            btnSetDefault.Margin = new Padding(8, 0, 0, 0);
            btnSetDefault.Click += (s, e) =>
            {
                string key = GetSelectedTemplateKey();
                AppConfig.WhatsAppInvoiceTemplate = key;
                MessageBox.Show($"✅ تم تعيين هذا النموذج كنموذج افتراضي لفواتير الواتساب بنجاح!\n(الرمز: {key})", "تم التعيين", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var btnClose = Theme.MakeButton("إغلاق", Color.FromArgb(100, 100, 110));
            btnClose.Size = new Size(90, 40);
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnCopy);
            pnlFooter.Controls.Add(btnSaveImage);
            pnlFooter.Controls.Add(btnSetDefault);
            pnlFooter.Controls.Add(btnClose);

            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlFooter);

            Theme.ApplyFormRTL(this);
            RenderSelectedTemplate();
        }

        private void LoadSaleData(int? saleID)
        {
            try
            {
                if (saleID.HasValue && saleID.Value > 0)
                {
                    DataTable dt = DbHelper.Query("SELECT s.*, c.ClientName, c.Phone FROM Sales s LEFT JOIN Clients c ON s.ClientID=c.ClientID WHERE s.SaleID=@id", DbHelper.P("@id", saleID.Value));
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        saleRow = dt.Rows[0];
                        saleItems = SaleDAL.GetItems(saleID.Value);
                        int clientID = saleRow["ClientID"] != DBNull.Value ? Convert.ToInt32(saleRow["ClientID"]) : 0;
                        if (clientID > 0)
                        {
                            prevBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, saleID.Value);
                            decimal netVal = Convert.ToDecimal(saleRow["TotalAmount"]);
                            bool isCredit = saleRow["SaleType"].ToString() == "Credit";
                            decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : netVal;
                            actualCurrentBalance = prevBalance + (isCredit ? netVal : (netVal - cashPaid));
                        }
                        return;
                    }
                }
                else
                {
                    // Try to fetch last real sale for realistic preview
                    DataTable dtLast = DbHelper.Query("SELECT TOP 1 s.*, c.ClientName, c.Phone FROM Sales s LEFT JOIN Clients c ON s.ClientID=c.ClientID ORDER BY s.SaleID DESC");
                    if (dtLast != null && dtLast.Rows.Count > 0)
                    {
                        saleRow = dtLast.Rows[0];
                        int lastId = Convert.ToInt32(saleRow["SaleID"]);
                        saleItems = SaleDAL.GetItems(lastId);
                        int clientID = saleRow["ClientID"] != DBNull.Value ? Convert.ToInt32(saleRow["ClientID"]) : 0;
                        if (clientID > 0)
                        {
                            prevBalance = ClientDAL.GetPreviousBalanceBeforeSale(clientID, lastId);
                            decimal netVal = Convert.ToDecimal(saleRow["TotalAmount"]);
                            bool isCredit = saleRow["SaleType"].ToString() == "Credit";
                            decimal cashPaid = saleRow["CashPaid"] != DBNull.Value ? Convert.ToDecimal(saleRow["CashPaid"]) : netVal;
                            actualCurrentBalance = prevBalance + (isCredit ? netVal : (netVal - cashPaid));
                        }
                        return;
                    }
                }
            }
            catch { }

            // Mock Data Fallback
            DataTable dtMock = new DataTable();
            dtMock.Columns.Add("SaleID", typeof(int));
            dtMock.Columns.Add("SaleCode", typeof(string));
            dtMock.Columns.Add("SaleDate", typeof(DateTime));
            dtMock.Columns.Add("SaleType", typeof(string));
            dtMock.Columns.Add("ClientID", typeof(int));
            dtMock.Columns.Add("ClientName", typeof(string));
            dtMock.Columns.Add("Phone", typeof(string));
            dtMock.Columns.Add("TotalAmount", typeof(decimal));
            dtMock.Columns.Add("DiscountAmount", typeof(decimal));
            dtMock.Columns.Add("DiscountPct", typeof(decimal));
            dtMock.Columns.Add("CashPaid", typeof(decimal));
            dtMock.Columns.Add("Notes", typeof(string));

            DataRow r = dtMock.NewRow();
            r["SaleID"] = 101;
            r["SaleCode"] = "INV-2026-088";
            r["SaleDate"] = DateTime.Now;
            r["SaleType"] = "Credit";
            r["ClientID"] = 1;
            r["ClientName"] = "معرض الأمل للتجارة والتوزيع";
            r["Phone"] = "01070909181";
            r["TotalAmount"] = 24850.00m;
            r["DiscountAmount"] = 350.00m;
            r["DiscountPct"] = 0m;
            r["CashPaid"] = 10000.00m;
            r["Notes"] = "تسليم المخزن الرئيسي - بضاعة معتمدة بالضمان";
            dtMock.Rows.Add(r);
            saleRow = r;

            DataTable items = new DataTable();
            items.Columns.Add("ProductCode", typeof(string));
            items.Columns.Add("ProductName", typeof(string));
            items.Columns.Add("UnitName", typeof(string));
            items.Columns.Add("Quantity", typeof(decimal));
            items.Columns.Add("UnitPrice", typeof(decimal));
            items.Columns.Add("DiscountAmt", typeof(decimal));
            items.Columns.Add("TotalPrice", typeof(decimal));
            items.Columns.Add("Notes", typeof(string));

            items.Rows.Add("TV-55-4K", "شاشة 55 بوصة سمارت 4K Ultra HD", "جهاز", 2m, 8500.00m, 200.00m, 16800.00m, "ضمان سنتين");
            items.Rows.Add("WM-8KG-INV", "غسالة أوتوماتيك 8 كيلو انفرتر ديجيتال", "جهاز", 1m, 6200.00m, 150.00m, 6050.00m, "إيطالي أصلي");
            items.Rows.Add("IR-TEF-22", "مكواة بخار تيفال سيراميك 2200W", "قطعة", 3m, 450.00m, 0m, 1350.00m, "");
            items.Rows.Add("MX-MUL-60", "خلاط ومطحنة مولينكس 600W", "طقم", 2m, 325.00m, 0m, 650.00m, "");
            saleItems = items;

            prevBalance = 15000.00m;
            lastPaymentAmt = 5000.00m;
            lastPaymentDate = DateTime.Now.AddDays(-3);
            todayPayments = 5000.00m;
            todayReturns = 0m;
            actualCurrentBalance = 29850.00m;
        }

        private string GetSelectedTemplateKey()
        {
            return cboTemplates.SelectedIndex switch
            {
                1 => "ImageCardModern",
                2 => "ImageCardCommercial",
                3 => "ImageCardEmerald",
                4 => "ImageCardGold",
                5 => "Detailed",
                6 => "Summary",
                7 => "Financial",
                _ => "ImageCardNavy"
            };
        }

        private void RenderSelectedTemplate()
        {
            int idx = cboTemplates.SelectedIndex;
            if (idx < 5)
            {
                // Image Card
                txtPreview.Visible = false;
                picPreview.Visible = true;

                string tplKey = GetSelectedTemplateKey();
                currentBitmap = ReceiptImageGenerator.GenerateSaleReceiptImage(saleRow, saleItems, prevBalance, lastPaymentAmt, lastPaymentDate, todayPayments, todayReturns, actualCurrentBalance, tplKey);

                picPreview.Image = currentBitmap;

                // Center picture box in scrollable panel
                if (currentBitmap != null)
                {
                    int padX = Math.Max(20, (pnlCenter.ClientSize.Width - currentBitmap.Width) / 2);
                    picPreview.Location = new Point(padX, 20);
                }
            }
            else
            {
                // Text Message
                picPreview.Visible = false;
                txtPreview.Visible = true;

                currentText = idx switch
                {
                    6 => BuildSampleSummaryText(),
                    7 => BuildSampleFinancialText(),
                    _ => BuildSampleDetailedText()
                };

                txtPreview.Text = currentText;
            }
        }

        private string BuildSampleDetailedText()
        {
            string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🧾 *فاتورة مبيعات - {comp}*");
            sb.AppendLine($"رقم الفاتورة: #{saleRow["SaleCode"]}");
            sb.AppendLine($"التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):yyyy/MM/dd hh:mm tt}");
            sb.AppendLine($"العميل: {saleRow["ClientName"]}");
            sb.AppendLine("━━━━━━━━━━━━━━━━");
            sb.AppendLine("📦 *الأصناف والمسحوبات:*");
            if (saleItems != null)
            {
                foreach (DataRow r in saleItems.Rows)
                {
                    sb.AppendLine($"• {r["ProductName"]} × {Convert.ToDecimal(r["Quantity"]):0.##} = {Convert.ToDecimal(r["TotalPrice"]):N2} ج.م");
                }
            }
            sb.AppendLine("━━━━━━━━━━━━━━━━");
            sb.AppendLine($"💰 *إجمالي الفاتورة:* {Convert.ToDecimal(saleRow["TotalAmount"]):N2} ج.م");
            sb.AppendLine($"💵 *المدفوع:* {Convert.ToDecimal(saleRow["CashPaid"]):N2} ج.م");
            sb.AppendLine($"⚖️ *الرصيد السابق:* {prevBalance:N2} ج.م");
            sb.AppendLine($"🔴 *الرصيد الحالي المستحق:* {actualCurrentBalance:N2} ج.م");
            sb.AppendLine("🙏 شكراً لتعاملكم معنا!");
            return sb.ToString();
        }

        private string BuildSampleSummaryText()
        {
            string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🧾 *فاتورة مبيعات سريعة - {comp}*");
            sb.AppendLine($"العميل: {saleRow["ClientName"]} | #{saleRow["SaleCode"]}");
            sb.AppendLine($"💰 إجمالي الفاتورة: {Convert.ToDecimal(saleRow["TotalAmount"]):N2} ج.م");
            sb.AppendLine($"⚖️ الرصيد الحالي: {actualCurrentBalance:N2} ج.م");
            sb.AppendLine("🙏 شكراً لتعاملكم معنا!");
            return sb.ToString();
        }

        private string BuildSampleFinancialText()
        {
            string comp = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المؤسسة العامة";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"📊 *كشف حساب وموقف مالي - {comp}*");
            sb.AppendLine($"العميل: {saleRow["ClientName"]}");
            sb.AppendLine($"التاريخ: {Convert.ToDateTime(saleRow["SaleDate"]):yyyy/MM/dd}");
            sb.AppendLine("━━━━━━━━━━━━━━━━");
            sb.AppendLine($"• الرصيد السابق: {prevBalance:N2} ج.م");
            sb.AppendLine($"• قيمة الفاتورة الحالية: {Convert.ToDecimal(saleRow["TotalAmount"]):N2} ج.م");
            sb.AppendLine($"• المدفوع: {Convert.ToDecimal(saleRow["CashPaid"]):N2} ج.م");
            sb.AppendLine($"🔴 *الرصيد النهائي المطلوب:* {actualCurrentBalance:N2} ج.م");
            sb.AppendLine("━━━━━━━━━━━━━━━━");
            sb.AppendLine("🙏 شاكرين ومقدرين حسن تعاونكم!");
            return sb.ToString();
        }
    }
}
