using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPrintInstallmentPayment
    {
        private int _contractID;
        private decimal _collectedAmount;
        private string _printFormat;
        private bool _showPreview;

        private string _customerName = "---";
        private string _contractCode = "---";
        private string _paymentMethod = "نقدي";
        private int _safeID = 0;
        private string _safeName = "---";
        private string _notes = "";
        
        private DateTime? _nextDueDate = null;
        private decimal _remainingBalance = 0;

        public FrmPrintInstallmentPayment(int contractID, decimal collectedAmount, string paymentMethod = "نقدي", int safeID = 0, string notes = "", string format = null, bool showPreview = false)
        {
            _contractID = contractID;
            _collectedAmount = collectedAmount;
            _paymentMethod = paymentMethod;
            _safeID = safeID;
            _notes = notes;
            _printFormat = format ?? AppConfig.DefaultInvoiceFormat;
            if (string.IsNullOrEmpty(_printFormat))
                _printFormat = "Receipt";
            _showPreview = showPreview;

            LoadData();
            DoPrint();
        }

        private void LoadData()
        {
            try
            {
                // 1. Get client and contract details
                var dtContract = DbHelper.Query(@"
                    SELECT ic.ContractCode, c.ClientName AS CustomerName
                    FROM InstallmentContracts ic
                    JOIN Clients c ON ic.CustomerID = c.ClientID
                    WHERE ic.ContractID = @cid", DbHelper.P("@cid", _contractID));
                if (dtContract.Rows.Count > 0)
                {
                    _contractCode = dtContract.Rows[0]["ContractCode"].ToString();
                    _customerName = dtContract.Rows[0]["CustomerName"].ToString();
                }

                // 2. Get next installment due date
                var nextDueObj = DbHelper.Scalar(@"
                    SELECT TOP 1 DueDate FROM InstallmentSchedules 
                    WHERE ContractID = @cid AND Status <> 'Paid' AND RemainingAmount > 0 
                    ORDER BY InstallmentNo", DbHelper.P("@cid", _contractID));
                if (nextDueObj != null && nextDueObj != DBNull.Value)
                {
                    _nextDueDate = Convert.ToDateTime(nextDueObj);
                }

                // 3. Get remaining balance on the contract
                var remObj = DbHelper.Scalar(@"
                    SELECT SUM(RemainingAmount) FROM InstallmentSchedules 
                    WHERE ContractID = @cid", DbHelper.P("@cid", _contractID));
                if (remObj != null && remObj != DBNull.Value)
                {
                    _remainingBalance = Convert.ToDecimal(remObj);
                }

                // 4. Get safe name
                if (_safeID > 0)
                {
                    var safeObj = DbHelper.Scalar("SELECT AccountName FROM SafeAccounts WHERE AccountID = @sid", DbHelper.P("@sid", _safeID));
                    if (safeObj != null && safeObj != DBNull.Value)
                    {
                        _safeName = safeObj.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل بيانات طباعة السند: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoPrint()
        {
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            bool isReceipt = string.Equals(_printFormat, "Receipt", StringComparison.OrdinalIgnoreCase);

            if (isReceipt)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 300, 800);
                pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
                AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
                pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
            }

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 12, FontStyle.Bold);
                var bold = new Font("Arial", 9.5f, FontStyle.Bold);
                var normal = new Font("Arial", 9f);
                var small = new Font("Arial", 8f);

                int pageW = e.PageBounds.Width;
                int lMargin = isReceipt ? 12 : 20;
                int rMargin = isReceipt ? 12 : 20;
                int printableW = pageW - lMargin - rMargin;
                int y = isReceipt ? 30 : 25;

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };
                var left = new StringFormat { Alignment = StringAlignment.Near };

                // Draw Logo
                DrawShopLogo(g, pageW, ref y, isReceipt);

                // Shop / Company Name
                g.DrawString(AppConfig.CompanyName, boldBig, Brushes.Black, new RectangleF(lMargin, y, printableW, 22), center);
                y += 22;

                g.DrawString("إيصال تحصيل قسط تقسيط", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), center);
                y += 22;

                g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y);
                y += 10;

                // Receipt Info (RTL)
                g.DrawString($"تاريخ التحصيل: {DateTime.Now:dd/MM/yyyy hh:mm tt}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 18;
                g.DrawString($"رقم العقد: {_contractCode}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 18;
                g.DrawString($"العميل: {_customerName}", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 22;

                g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y);
                y += 10;

                // Payment Details
                g.DrawString($"المبلغ المستلم: {_collectedAmount:N2} جنيه", boldBig, Brushes.DarkGreen, new RectangleF(lMargin, y, printableW, 22), right);
                y += 24;

                string payMethodText = _paymentMethod;
                if (!string.IsNullOrEmpty(_safeName) && _safeName != "---")
                    payMethodText += $" ({_safeName})";
                
                g.DrawString($"طريقة الدفع: {payMethodText}", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 18;

                if (!string.IsNullOrEmpty(_notes))
                {
                    g.DrawString($"ملاحظات: {_notes}", small, Brushes.DimGray, new RectangleF(lMargin, y, printableW, 30), right);
                    y += 25;
                }

                g.DrawLine(Pens.LightGray, lMargin, y, pageW - rMargin, y);
                y += 10;

                // Next Installment & Balance
                string nextDueText = _nextDueDate.HasValue 
                    ? _nextDueDate.Value.ToString("yyyy-MM-dd") 
                    : "لا يوجد (تم سداد العقد بالكامل)";
                
                g.DrawString($"تاريخ القسط التالي: {nextDueText}", bold, Brushes.DarkBlue, new RectangleF(lMargin, y, printableW, 18), right);
                y += 20;

                g.DrawString($"إجمالي المتبقي على العقد: {_remainingBalance:N2} جنيه", normal, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), right);
                y += 22;

                g.DrawLine(Pens.Black, lMargin, y, pageW - rMargin, y);
                y += 10;

                // Footer
                g.DrawString("شكراً لتعاملكم معنا", bold, Brushes.Black, new RectangleF(lMargin, y, printableW, 18), center);
                y += 18;
                g.DrawString("تمت الطباعة بواسطة Pro Soft", small, Brushes.Gray, new RectangleF(lMargin, y, printableW, 14), center);
            };

            if (_showPreview)
            {
                var preview = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = isReceipt ? 400 : 650,
                    Height = 700,
                    Text = "معاينة إيصال السداد"
                };
                preview.ShowDialog();
            }
            else
            {
                AppConfig.PrintInBackground(pd);
            }
        }

        private void DrawShopLogo(Graphics g, int pageW, ref int y, bool isReceipt)
        {
            if (!AppConfig.PrintShopLogo || string.IsNullOrEmpty(AppConfig.ShopLogoPath))
                return;

            try
            {
                if (System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                    {
                        int logoW = isReceipt ? 70 : 90;
                        int logoH = (int)((double)img.Height / img.Width * logoW);
                        int logoX = (pageW - logoW) / 2;
                        g.DrawImage(img, logoX, y, logoW, logoH);
                        y += logoH + 8;
                    }
                }
            }
            catch { }
        }
    }
}
