using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تعديل الباركودات المتعددة لصنف (5 خانات منفصلة)
    /// </summary>
    public class FrmMultiBarcodes : Form
    {
        private TextBox txtBarcode1;
        private TextBox txtBarcode2;
        private TextBox txtBarcode3;
        private TextBox txtBarcode4;
        private TextBox txtBarcode5;
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
            Text = "تعديل الباركودات المتعددة (حتى 5 باركودات)";
            Size = new Size(460, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            int labelWidth = 120;
            int textBoxWidth = 260;
            int startY = 20;
            int spacing = 35;

            // 1
            AddBarcodeField("الباركود الدولي 1:", startY, out txtBarcode1);
            startY += spacing;

            // 2
            AddBarcodeField("الباركود الدولي 2:", startY, out txtBarcode2);
            startY += spacing;

            // 3
            AddBarcodeField("الباركود الدولي 3:", startY, out txtBarcode3);
            startY += spacing;

            // 4
            AddBarcodeField("الباركود الدولي 4:", startY, out txtBarcode4);
            startY += spacing;

            // 5
            AddBarcodeField("الباركود الدولي 5:", startY, out txtBarcode5);
            startY += spacing;

            // Populate values
            string[] codes = (currentBarcodes ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (codes.Length > 0) txtBarcode1.Text = codes[0];
            if (codes.Length > 1) txtBarcode2.Text = codes[1];
            if (codes.Length > 2) txtBarcode3.Text = codes[2];
            if (codes.Length > 3) txtBarcode4.Text = codes[3];
            if (codes.Length > 4) txtBarcode5.Text = codes[4];

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

        private void AddBarcodeField(string labelText, int y, out TextBox txt)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 3),
                Width = 120,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextMain
            };

            txt = new TextBox
            {
                Location = new Point(145, y),
                Width = 260,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };

            Controls.Add(lbl);
            Controls.Add(txt);
        }
    }
}
