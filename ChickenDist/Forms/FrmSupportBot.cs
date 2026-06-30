using System;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSupportBot : Form
    {
        private FlowLayoutPanel pnlChat;
        private TextBox txtInput;
        private Button btnSend;
        private Panel pnlInputArea;
        private FlowLayoutPanel pnlChips;

        public FrmSupportBot()
        {
            InitUI();
            AddBotMessage("أهلاً بك يا فندم في الدعم الفني الذكي للبرنامج! 🤖\nأنا هنا عشان أجاوبك بالبلدي وبأبسط طريقة على أي حاجة عاوز تعملها.\nتقدر تسألني عن (الخصم، الصيانة، مصفوفة الملابس، تتبع IMEI، الواتساب، الباركود) أو تضغط على الأسئلة السريعة تحت.");
        }

        private void InitUI()
        {
            this.Text = "🤖 مساعد الدعم الفني الذكي (أوفلاين)";
            this.Size = new Size(550, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTitle = Theme.MakeTitleBar("🤖 المساعد الذكي للدعم الفني", "اسأل أي سؤال وهرد عليك فوراً بالعامية لشرح طريقة استخدام البرنامج");
            this.Controls.Add(pnlTitle);

            // 1. Chat History Panel
            pnlChat = new FlowLayoutPanel
            {
                Location = new Point(15, 75),
                Size = new Size(505, 410),
                AutoScroll = true,
                BackColor = Color.FromArgb(26, 32, 44),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlChat);

            // 2. Quick Action Chips
            pnlChips = new FlowLayoutPanel
            {
                Location = new Point(15, 495),
                Size = new Size(505, 75),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(2)
            };
            this.Controls.Add(pnlChips);

            AddChip("💰 ازاي أعمل خصم؟", "ازاى اعمل خصم على الفاتورة");
            AddChip("👕 مصفوفة مقاسات الملابس", "شرح مصفوفة الملابس والالوان");
            AddChip("📱 تتبع IMEI الموبايلات", "شغل الـ imei والسيريال");
            AddChip("🛠️ نظام تشغيل الصيانة", "ازاي اشغل شاشة الصيانة والباركود");
            AddChip("💬 مشاركة الفاتورة واتساب", "ازاي ابعت فاتورة واتساب للعميل");
            AddChip("🏷️ طباعة الباركود", "طريقة طباعة باركود صنف");

            // 3. Input Area
            pnlInputArea = new Panel
            {
                Location = new Point(15, 575),
                Size = new Size(505, 40),
                BackColor = Color.Transparent
            };
            this.Controls.Add(pnlInputArea);

            txtInput = new TextBox
            {
                Location = new Point(100, 5),
                Size = new Size(400, 30),
                Font = Theme.FontNormal,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendUserMessage(); e.SuppressKeyPress = true; } };
            pnlInputArea.Controls.Add(txtInput);

            btnSend = Theme.MakeButton("🚀 إرسال", 5, 3, 90, 32, Theme.Accent);
            btnSend.Click += (s, e) => SendUserMessage();
            pnlInputArea.Controls.Add(btnSend);
        }

        private void AddChip(string text, string question)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextMain,
                BackColor = Color.FromArgb(45, 55, 72),
                Cursor = Cursors.Hand,
                Font = new Font(Theme.FontMain.FontFamily, 8.5f),
                Margin = new Padding(3),
                Height = 26
            };
            btn.FlatAppearance.BorderColor = Theme.BorderColor;
            btn.Click += (s, e) => {
                txtInput.Text = question;
                SendUserMessage();
            };
            pnlChips.Controls.Add(btn);
        }

        private void SendUserMessage()
        {
            string userText = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            AddUserMessage(userText);
            txtInput.Clear();

            // Bot Response (instantly)
            string botResponse = GetBotResponse(userText);
            AddBotMessage(botResponse);
        }

        private void AddUserMessage(string text)
        {
            CreateBubble(text, Color.FromArgb(30, 120, 180), Color.White, FlowLayoutPanelRightToLeft.Yes);
        }

        private void AddBotMessage(string text)
        {
            CreateBubble(text, Color.FromArgb(45, 55, 72), Color.White, FlowLayoutPanelRightToLeft.No);
        }

        private void CreateBubble(string text, Color bg, Color fg, FlowLayoutPanelRightToLeft rtl)
        {
            var pnl = new Panel
            {
                Width = pnlChat.Width - 40,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 0, 5)
            };

            var lbl = new Label
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                Font = Theme.FontNormal,
                Padding = new Padding(10),
                AutoSize = true,
                MaximumSize = new Size(350, 1000)
            };

            // Rounded/bubble emulation
            pnl.Height = lbl.PreferredHeight + 10;
            pnl.Controls.Add(lbl);

            if (rtl == FlowLayoutPanelRightToLeft.Yes)
            {
                lbl.Location = new Point(pnl.Width - lbl.PreferredWidth - 10, 5);
            }
            else
            {
                lbl.Location = new Point(10, 5);
            }

            pnlChat.Controls.Add(pnl);
            pnlChat.ScrollControlIntoView(pnl);
        }

        private string GetBotResponse(string query)
        {
            query = query.ToLower();

            if (query.Contains("خصم") || query.Contains("تخفيض") || query.Contains("انزل") || query.Contains("اقلل") || query.Contains("ارخص"))
            {
                return "يا فندم عشان تعمل خصم، وأنت بتسجل الفاتورة في شاشة البيع هتلاقي خانة اسمها 'الخصم' تحت الإجمالي. اكتب فيها القيمة بالجنيه أو اختار نسبة مئوية (%). كمان تقدر تعمل خصم خاص بصنف معين جوه الفاتورة مباشرة في عمود الخصم.";
            }

            if (query.Contains("ملابس") || query.Contains("مقاس") || query.Contains("لون") || query.Contains("الوان") || query.Contains("مصفوفة") || query.Contains("خامة"))
            {
                return "لو شغال ملابس، ضغطنا لك زرار سحري اسمه '📦 مصفوفة الملابس' في شاشة الأصناف. اكتب اسم الموديل الأساسي واكتب الألوان والمقاسات اللي عندك واضغط توليد. البرنامج هيعملك لكل التركيبات دي أصناف بباركودات مستقلة في ثانية واحدة!";
            }

            if (query.Contains("سيريال") || query.Contains("imei") || query.Contains("تتبع") || query.Contains("موبايل") || query.Contains("الهواتف"))
            {
                return "تتبع الـ IMEI بيشتغل لو نشاطك هو الهواتف. وأنت بتبيع، هيظهرلك عمود اسمه IMEI في جدول الأصناف، اكتب فيه السيريال نمبر بتاع الجهاز عشان يتحفظ في الفاتورة ويطبع عليها تلقائياً لضمان حقك وحق العميل.";
            }

            if (query.Contains("واتساب") || query.Contains("ارسال") || query.Contains("شات") || query.Contains("رسالة"))
            {
                return "تقدر تبعت الفاتورة للعميل على الواتساب بضغطة زرار! في شاشة الـ POS بعد ما تحفظ الفاتورة، اضغط على زرار '💬 واتساب' الأخضر تحت، البرنامج هيسحب رقم العميل ويفتحلك الواتساب بالرسالة مكتوبة ومنسقة وجاهزة للإرسال علطول.";
            }

            if (query.Contains("صيانة") || query.Contains("تذكرة") || query.Contains("تصليح") || query.Contains("جهاز") || query.Contains("عطل") || query.Contains("ايصال"))
            {
                return "عشان تسجل جهاز للصيانة، ادخل شاشة الصيانة واضغط 'إضافة تذكرة صيانة جديدة'، واكتب بيانات العميل والجهاز وتكلفة قطع الغيار والمصنعية وفترة الضمان واطبع إيصال الاستلام للعميل. الإيصال فيه باركود، لو العميل رجعلك، امسح الباركود بجهاز السكنر وهيفتحلك تذكرته فوراً!";
            }

            if (query.Contains("باركود") || query.Contains("اطبع باركود") || query.Contains("ملصق") || query.Contains("ستيكر"))
            {
                return "لطباعة باركود صنف، ادخل شاشة الأصناف وحدد الصنف اللي عاوزه، واضغط على زرار '🏷️ طباعة الباركود' تحت، وحدد مقاس الاستيكر واطبع علطول على برنتر الباركود.";
            }

            if (query.Contains("طباعة") || query.Contains("طابعة") || query.Contains("برنتر") || query.Contains("مش بيطبع"))
            {
                return "تأكد أولاً إن الطابعة متعرفة على الكمبيوتر ومتوصلة وسليمة. بعد كده ادخل شاشة الإعدادات في البرنامج وتأكد إنك مختار الطابعة الصح في خانة 'طابعة الفواتير' أو 'طابعة A4'.";
            }

            return "يا فندم للاسف مش فاهم السؤال ده كويس بالبلدي. 😅\nممكن تسألني عن (الخصم، الصيانة، مصفوفة الملابس، تتبع IMEI، الواتساب، الباركود) أو تضغط على الأسئلة السريعة تحت وهشرحلك بالتفصيل.";
        }
    }

    public enum FlowLayoutPanelRightToLeft
    {
        No,
        Yes
    }
}
