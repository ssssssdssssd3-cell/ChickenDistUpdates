using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تعديل الباركودات المتعددة لصنف (10 خانات منفصلة)
    /// </summary>
    public class FrmMultiBarcodes : Form
    {
        private TextBox txtBarcode1;
        private TextBox txtBarcode2;
        private TextBox txtBarcode3;
        private TextBox txtBarcode4;
        private TextBox txtBarcode5;
        private TextBox txtBarcode6;
        private TextBox txtBarcode7;
        private TextBox txtBarcode8;
        private TextBox txtBarcode9;
        private TextBox txtBarcode10;
        private Button btnOK;
        private Button btnCancel;
        private int _productID;

        /// <summary>الباركودات المُدخلة مدمجة بفاصلة بعد الموافقة</summary>
        public string ResultBarcodes { get; private set; }

        public FrmMultiBarcodes(string currentBarcodes, int productID)
        {
            _productID = productID;
            ResultBarcodes = currentBarcodes ?? "";
            InitUI(currentBarcodes);
        }

        private void InitUI(string currentBarcodes)
        {
            Text = "تعديل الباركودات المتعددة (حتى 10 باركودات)";
            Size = new Size(520, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            int col1X = 15;
            int col2X = 260;
            int startY = 20;
            int spacing = 35;

            // Column 1
            AddBarcodeField("باركود دولي 1:", col1X, startY, out txtBarcode1);
            AddBarcodeField("باركود دولي 6:", col2X, startY, out txtBarcode6);
            startY += spacing;

            AddBarcodeField("باركود دولي 2:", col1X, startY, out txtBarcode2);
            AddBarcodeField("باركود دولي 7:", col2X, startY, out txtBarcode7);
            startY += spacing;

            AddBarcodeField("باركود دولي 3:", col1X, startY, out txtBarcode3);
            AddBarcodeField("باركود دولي 8:", col2X, startY, out txtBarcode8);
            startY += spacing;

            AddBarcodeField("باركود دولي 4:", col1X, startY, out txtBarcode4);
            AddBarcodeField("باركود دولي 9:", col2X, startY, out txtBarcode9);
            startY += spacing;

            AddBarcodeField("باركود دولي 5:", col1X, startY, out txtBarcode5);
            AddBarcodeField("باركود دولي 10:", col2X, startY, out txtBarcode10);

            // Populate values
            string[] codes = (currentBarcodes ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (codes.Length > 0) txtBarcode1.Text = codes[0];
            if (codes.Length > 1) txtBarcode2.Text = codes[1];
            if (codes.Length > 2) txtBarcode3.Text = codes[2];
            if (codes.Length > 3) txtBarcode4.Text = codes[3];
            if (codes.Length > 4) txtBarcode5.Text = codes[4];
            if (codes.Length > 5) txtBarcode6.Text = codes[5];
            if (codes.Length > 6) txtBarcode7.Text = codes[6];
            if (codes.Length > 7) txtBarcode8.Text = codes[7];
            if (codes.Length > 8) txtBarcode9.Text = codes[8];
            if (codes.Length > 9) txtBarcode10.Text = codes[9];

            var pnlBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 8, 8, 8)
            };

            btnOK = Theme.MakeButton("✔ موافق", 0, 0, 100, 32, Theme.Accent);
            btnCancel = Theme.MakeButton("✖ إلغاء", 0, 0, 100, 32, Theme.BgInput);

            btnOK.Click += (s, e) =>
            {
                var list = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(txtBarcode1.Text)) list.Add(txtBarcode1.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode2.Text)) list.Add(txtBarcode2.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode3.Text)) list.Add(txtBarcode3.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode4.Text)) list.Add(txtBarcode4.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode5.Text)) list.Add(txtBarcode5.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode6.Text)) list.Add(txtBarcode6.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode7.Text)) list.Add(txtBarcode7.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode8.Text)) list.Add(txtBarcode8.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode9.Text)) list.Add(txtBarcode9.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtBarcode10.Text)) list.Add(txtBarcode10.Text.Trim());

                ResultBarcodes = string.Join(",", list);
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnOK);

            Controls.Add(pnlBtns);
        }

        private void AddBarcodeField(string labelText, int x, int y, out TextBox txt)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(x, y + 3),
                Width = 90,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextMain
            };

            txt = new TextBox
            {
                Location = new Point(x + 95, y),
                Width = 135,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            Controls.Add(lbl);
            Controls.Add(txt);
        }
    }
}
