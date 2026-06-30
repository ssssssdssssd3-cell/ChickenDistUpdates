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

        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public FrmSupportBot()
        {
            InitUI();
            AddBotMessage("أهلاً بك يا فندم في الدعم الفني الذكي للبرنامج! 🤖\nأنا هنا عشان أجاوبك بالبلدي وبأبسط طريقة على أي حاجة عاوز تعملها.\nتقدر تسألني عن (الخصم، الصيانة، مصفوفة الملابس، تتبع IMEI، الواتساب، الباركود) أو تضغط على الأسئلة السريعة تحت.");
        }

        private void InitUI()
        {
            this.Text = "🤖 مساعد الدعم الفني الذكي (أوفلاين)";
            this.Size = new Size(580, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTitle = Theme.MakeTitleBar("🤖 المساعد الذكي للدعم الفني", "اسأل أي سؤال وهرد عليك فوراً بالعامية لشرح طريقة استخدام البرنامج");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // 3. Input Area
            pnlInputArea = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(pnlInputArea);

            btnSend = Theme.MakeButton("🚀 إرسال", Theme.Accent);
            btnSend.Width = 90;
            btnSend.Dock = DockStyle.Left;
            btnSend.Click += (s, e) => SendUserMessage();
            pnlInputArea.Controls.Add(btnSend);

            txtInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontNormal,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendUserMessage(); e.SuppressKeyPress = true; } };
            pnlInputArea.Controls.Add(txtInput);

            // 2. Quick Action Chips
            pnlChips = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 5, 10, 5),
                AutoScroll = true
            };
            this.Controls.Add(pnlChips);

            // 1. Chat History Panel
            pnlChat = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(26, 32, 44),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlChat);

            pnlChat.SendToBack();
            pnlTitle.BringToFront();
            pnlInputArea.BringToFront();
            pnlChips.BringToFront();

            // إضافة الأسئلة السريعة (Chips) حسب نوع النشاط المختار
            AddChip("💰 ازاي أعمل خصم؟", "ازاى اعمل خصم على الفاتورة");
            AddChip("💬 مشاركة الفاتورة واتساب", "ازاي ابعت فاتورة واتساب للعميل");
            AddChip("🏷️ طباعة الباركود", "طريقة طباعة باركود صنف");
            AddChip("📈 حساب متوسط التكلفة", "ازاي بيتم حساب التكلفة ومتوسط التكلفة للأصناف");
            AddChip("💵 تعديل سعر البيع", "ازاي اقدر اغير سعر البيع لصنف");

            if (AppConfig.BusinessType == "Mobiles")
            {
                AddChip("🔧 شاشة الصيانة والباركود", "ازاي اشغل شاشة الصيانة والباركود");
                AddChip("📱 تتبع IMEI وسيريال الموبايل", "شغل الـ imei والسيريال");
                AddChip("📋 شرح شاشات الموبايل", "شرح شاشات الصيانة والأجهزة للموبايلات");
            }
            else if (AppConfig.BusinessType == "Clothing")
            {
                AddChip("👕 مصفوفة مقاسات وألوان الملابس", "شرح مصفوفة الملابس والالوان");
                AddChip("📋 شرح شاشات الملابس", "شرح شاشات مصفوفة المقاسات والملابس");
            }
            else
            {
                AddChip("🚚 حركة الحمولات والسيارات", "ازاي اسجل حمولة مندوب وحركة السيارات");
                AddChip("📦 جرد المخزن الفعلي", "ازاي اعمل جرد للمخزن والتسويات");
                AddChip("📋 شرح شاشات التوزيع", "شرح شاشات الحمولات والسيارات وجرد المخازن");
            }
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

            AddChatMessage(userText, FlowLayoutPanelRightToLeft.Yes);
            txtInput.Clear();

            string response = GetBotResponse(userText);
            AddBotMessage(response);
        }

        private void AddBotMessage(string text)
        {
            AddChatMessage(text, FlowLayoutPanelRightToLeft.No);
        }

        private void AddChatMessage(string text, FlowLayoutPanelRightToLeft rtl)
        {
            var pnl = new FlowLayoutPanel
            {
                Width = 460,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(5),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Font = Theme.FontNormal,
                Padding = new Padding(10),
                BackColor = (rtl == FlowLayoutPanelRightToLeft.Yes) ? Color.FromArgb(13, 110, 253) : Color.FromArgb(45, 55, 72),
                ForeColor = Color.White
            };

            // Round corners style simulation
            lbl.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, lbl.Width, lbl.Height, 10, 10));
            lbl.SizeChanged += (s, e) => {
                lbl.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, lbl.Width, lbl.Height, 10, 10));
            };

            pnl.Controls.Add(lbl);

            if (rtl == FlowLayoutPanelRightToLeft.Yes)
            {
                pnl.FlowDirection = FlowDirection.RightToLeft;
            }
            else
            {
                pnl.FlowDirection = FlowDirection.LeftToRight;
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

            if (query.Contains("تكلفة") || query.Contains("متوسط") || query.Contains("حساب التكلفة"))
            {
                return "يا فندم، البرنامج بيحسب تكلفة الأصناف بطريقة 'متوسط التكلفة المتحرك' (Moving Average Cost). يعني لما تشتري بضاعة بسعر جديد في شاشة المشتريات، البرنامج بيجمع (كمية المخزن الحالية × تكلفتها القديمة) + (الكمية الجديدة × سعر الشراء الجديد) ويقسمهم على إجمالي الكمية عشان يطلع متوسط تكلفة جديد للصنف تلقائياً بدون تدخل منك، وده بيضمنلك حساب أرباح دقيق جداً.";
            }

            if (query.Contains("سعر البيع") || query.Contains("تغير سعر") || query.Contains("اضبط السعر") || query.Contains("تعديل السعر"))
            {
                return "لتعديل سعر البيع لأي صنف: ادخل شاشة 'الأصناف'، واضغط مرتين على الصنف اللي عاوز تعدله عشان يفتحلك كارت الصنف. هتلاقي خانة 'سعر البيع'، اكتب السعر الجديد واضغط حفظ. كمان تقدر تعدل سعر البيع مباشرة أثناء تسجيل فاتورة مشتريات جديدة في عمود 'سعر البيع المقترح' وهيحدث سعر الصنف تلقائياً.";
            }

            if (query.Contains("شاشات الموبايل") || query.Contains("شرح الموبايل"))
            {
                return "شاشات الموبايل بتشمل:\n1. **ورشة الصيانة**: لتسجيل أجهزة العملاء وعيوبها وتكلفة تصليحها ومتابعة حالتها (قيد الإصلاح/جاهز/تم التسليم).\n2. **كارت الصنف**: بيسمحلك تدخل الـ IMEI لكل جهاز لتتبعه بدقة في البيع والشراء والضمان.";
            }

            if (query.Contains("شاشات الملابس") || query.Contains("شرح الملابس"))
            {
                return "شاشات الملابس بتشمل:\n1. **مصفوفة الملابس**: شاشة سحرية بتنشئلك كل المقاسات والألوان لموديل معين بضغطة واحدة وبتطبع باركودات مستقلة لكل قطعة.\n2. **الاستبدال والاسترجاع**: مطبوعة تلقائياً في أسفل إيصالات البيع لتنظيم العمل مع الزباين.";
            }

            if (query.Contains("شاشات التوزيع") || query.Contains("شرح التوزيع") || query.Contains("شرح الحمولات"))
            {
                return "شاشات التوزيع بتشمل:\n1. **حركة السيارات**: لتسجيل حمولات المناديب، وجرد السيارات عند عودتهم.\n2. **جرد المخزن**: لعمل تسويات وجرد دوري للكميات الفعلية وحساب الفوارق.\n3. **تحصيل الخزنة**: لإثبات مبالغ التسليم والتحصيل اليومي.";
            }

            return "يا فندم للاسف مش فاهم السؤال ده كويس بالبلدي. 😅\nممكن تسألني عن (الخصم، الصيانة، مصفوفة الملابس، تتبع IMEI، متوسط التكلفة، تعديل سعر البيع، أو شرح الشاشات) أو تضغط على الأسئلة السريعة تحت وهشرحلك بالتفصيل.";
        }
    }

    public enum FlowLayoutPanelRightToLeft
    {
        No,
        Yes
    }
}
