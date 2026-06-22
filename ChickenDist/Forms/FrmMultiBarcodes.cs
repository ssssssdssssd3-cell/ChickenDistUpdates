using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة تعديل الباركودات المتعددة لصنف (مفصولة بفاصلة)
    /// </summary>
    public class FrmMultiBarcodes : Form
    {
        private TextBox txtBarcodes;
        private Button btnOK;
        private Button btnCancel;
        private Label lblHint;
        private int _productID;

        /// <summary>الباركودات المُدخلة بعد الموافقة</summary>
        public string ResultBarcodes { get; private set; }

        public FrmMultiBarcodes(string currentBarcodes, int productID)
        {
            _productID = productID;
            ResultBarcodes = currentBarcodes ?? "";
            InitUI(currentBarcodes);
        }

        private void InitUI(string currentBarcodes)
        {
            Text = "تعديل الباركودات المتعددة";
            Size = new Size(480, 240);
            StartPosition = FormStartPosition.CenterParent;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            lblHint = new Label
            {
                Text = "أدخل الباركودات مفصولة بفاصلة (,) أو فاصلة منقوطة (;):",
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(5)
            };

            txtBarcodes = new TextBox
            {
                Text = currentBarcodes ?? "",
                Dock = DockStyle.Top,
                Height = 80,
                Multiline = true,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 11f)
            };

            var pnlBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 8, 8, 8)
            };

            btnOK = Theme.MakeButton("✔ موافق", 0, 0, 100, 32, Theme.Accent);
            btnCancel = Theme.MakeButton("✖ إلغاء", 0, 0, 100, 32, Theme.BgInput);
            btnOK.Click += (s, e) =>
            {
                ResultBarcodes = txtBarcodes.Text.Trim();
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
            Controls.Add(txtBarcodes);
            Controls.Add(lblHint);
        }
    }
}
