using System;
using System.Collections.Generic;
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
        private Panel pnlBottomContainer;

        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public FrmSupportBot()
        {
            InitUI();
            AddBotMessage("أهلاً بك يا فندم في الدعم الفني الذكي للبرنامج! 🤖\nأنا هنا عشان أجاوبك بالبلدي وبأبسط طريقة على أي حاجة عاوز تعملها.\nتقدر تسألني عن (الخصم، الصيانة، مصفوفة الملابس، تتبع IMEI، الواتساب، الباركود، الحسابات) أو تضغط على الأسئلة السريعة تحت.");
        }

        private void InitUI()
        {
            this.Text = "🤖 مساعد الدعم الفني الذكي (أوفلاين)";
            this.Size = new Size(1024, 768);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            var pnlTitle = Theme.MakeTitleBar("🤖 المساعد الذكي للدعم الفني", "اسأل أي سؤال وهرد عليك فوراً بالعامية لشرح طريقة استخدام البرنامج");
            pnlTitle.Dock = DockStyle.Top;
            this.Controls.Add(pnlTitle);

            // ── الحاوية السفلية لمنع تداخل أزرار الشرح ومربع الإدخال ──
            // NOTE: يجب إضافة Bottom controls أولاً قبل Fill في WinForms
            pnlBottomContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.Transparent
            };

            // 3. Input Area inside Bottom Container
            pnlInputArea = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlBottomContainer.Controls.Add(pnlInputArea);

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

            // 2. Quick Action Chips inside Bottom Container
            pnlChips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 5, 10, 5),
                AutoScroll = true
            };
            pnlBottomContainer.Controls.Add(pnlChips);

            // 1. Chat History Panel — يُضاف بعد كل Bottom controls حتى يملأ المساحة المتبقية بشكل صحيح
            pnlChat = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(26, 32, 44),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(15)
            };

            // ترتيب الإضافة المهم: Bottom أولاً ثم Fill
            this.Controls.Add(pnlBottomContainer);
            this.Controls.Add(pnlChat);
            pnlChat.BringToFront();

            // ── كروت شرح الشاشات بألوان مميزة وتفاعلية ──
            AddChip("🖥️ شرح شاشة البيع السريع", "شرح شاشة البيع السريع", Color.FromArgb(40, 167, 69)); // أخضر
            AddChip("🛒 شرح شاشة المشتريات", "شرح شاشة المشتريات", Color.FromArgb(0, 123, 255)); // أزرق
            AddChip("⚖️ شرح شاشة جرد الأسعار", "شرح شاشة جرد الأسعار", Color.FromArgb(253, 126, 20)); // برتقالي
            AddChip("📊 شرح شاشة الموقف المالي", "شرح شاشة الموقف المالي", Color.FromArgb(23, 162, 184)); // سماوي
            AddChip("👥 شرح شاشة العملاء", "شرح شاشة العملاء", Color.FromArgb(111, 66, 193)); // بنفسجي
            AddChip("💰 شرح شاشة الخزنة والدرج", "شرح شاشة الخزنة والدرج", Color.FromArgb(220, 53, 69)); // أحمر
            AddChip("⚖️ شرح مديول الحسابات والقوائم", "شرح مديول الحسابات والقوائم المالية", Color.FromArgb(230, 80, 80)); // لون أحمر طوبي للحسابات

            if (AppConfig.BusinessType == "Mobiles")
            {
                AddChip("🔧 شرح شاشة الصيانة", "شرح شاشة الصيانة", Color.FromArgb(13, 148, 136)); // تركواز صيانة
            }
            else if (AppConfig.BusinessType == "Clothing")
            {
                AddChip("👕 شرح مصفوفة الملابس", "شرح مصفوفة الملابس والالوان", Color.FromArgb(232, 62, 140)); // وردي
            }
            else
            {
                AddChip("🚚 شرح حركة الحمولات", "شرح حركة الحمولات والسيارات", Color.FromArgb(108, 117, 125)); // رمادي
            }

            // ── كروت الأسئلة العامة ──
            AddChip("💰 ازاي أعمل خصم؟", "ازاى اعمل خصم على الفاتورة");
            AddChip("💬 مشاركة الفاتورة واتساب", "ازاي ابعت فاتورة واتساب للعميل");
            AddChip("🏷️ طباعة الباركود", "طريقة طباعة باركود صنف");
            AddChip("📈 حساب متوسط التكلفة", "ازاي بيتم حساب التكلفة ومتوسط التكلفة للأصناف");
            AddChip("💵 تعديل سعر البيع", "ازاي اقدر اغير سعر البيع لصنف");
            AddChip("⌨️ اختصارات كيبورد السريعة", "اختصارات لوحة المفاتيح والزراير السريعة", Color.FromArgb(70, 80, 95));

            // ── التعديل التلقائي لتوزيع فقاعات الدردشة عند تغيير حجم الشاشة أو التكبير ──
            this.SizeChanged += (s, e) => {
                int newWidth = pnlChat.ClientSize.Width - 30;
                if (newWidth < 200) return;
                pnlChat.SuspendLayout();
                foreach (Control ctrl in pnlChat.Controls)
                {
                    if (ctrl is Panel pnl)
                    {
                        pnl.Width = newWidth;
                        foreach (Control child in pnl.Controls)
                        {
                            if (child is Label lbl)
                            {
                                lbl.MaximumSize = new Size((int)(newWidth * 0.75), 0);
                            }
                        }
                    }
                }
                pnlChat.ResumeLayout();
            };
        }

        private void AddChip(string text, string question, Color? bgColor = null)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = bgColor ?? Color.FromArgb(45, 55, 72),
                Cursor = Cursors.Hand,
                Font = new Font(Theme.FontMain.FontFamily, 8.5f, FontStyle.Bold),
                Margin = new Padding(3),
                Height = 26
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.CreateControl();
            btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            btn.SizeChanged += (s, e) => {
                btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 8, 8));
            };

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
            int chatWidth = pnlChat.ClientSize.Width - 30;
            if (chatWidth < 200) chatWidth = 460;

            var pnl = new Panel
            {
                Width = chatWidth,
                Height = 0,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 5, 10, 5)
            };

            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size((int)(chatWidth * 0.75), 0),
                Font = Theme.FontNormal,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = (rtl == FlowLayoutPanelRightToLeft.Yes) ? Color.FromArgb(13, 110, 253) : Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Dock = (rtl == FlowLayoutPanelRightToLeft.Yes) ? DockStyle.Right : DockStyle.Left
            };

            lbl.CreateControl();
            lbl.SizeChanged += (s, e) => {
                if (lbl.Width > 0 && lbl.Height > 0)
                {
                    lbl.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, lbl.Width, lbl.Height, 12, 12));
                }
            };

            pnl.Controls.Add(lbl);
            pnlChat.Controls.Add(pnl);
            pnlChat.ScrollControlIntoView(pnl);
        }

        private string GetBotResponse(string query)
        {
            query = query.ToLower();

            if (query.Contains("شرح شاشة البيع") || query.Contains("شاشة البيع") || query.Contains("شاشه البيع"))
            {
                return "🖥️ **شاشة البيع السريع (POS)**:\n" +
                       "تستخدم لبيع المنتجات بشكل سريع ومباشر للزبائن.\n" +
                       "1. امسح باركود الصنف بجهاز السكنر أو ابحث عنه بالاسم.\n" +
                       "2. اختر العميل من قائمة العملاء إذا كانت الفاتورة آجل، وسيتم قيد المديونية عليه تلقائياً.\n" +
                       "3. لتفعيل استرداد النقاط، اضغط على 'استرداد نقاط' وسيتم خصم المبلغ من الفاتورة.\n" +
                       "4. اكتب المبلغ المدفوع واضغط على (إتمام البيع - F5) لحفظ الفاتورة وطباعة الإيصال.";
            }

            if (query.Contains("شرح شاشة المشتريات") || query.Contains("شرح شاشة الشراء") || query.Contains("شاشة المشتريات"))
            {
                return "🛒 **شاشة المشتريات**:\n" +
                       "لتسجيل بضاعة جديدة قادمة من الموردين وزيادة المخزن.\n" +
                       "1. حدد المورد وتاريخ الفاتورة.\n" +
                       "2. أدخل الأصناف والكميات وأسعار الشراء (التكلفة).\n" +
                       "3. يمكنك تعديل 'سعر البيع المقترح' مباشرة لتحديث أسعار الأصناف تلقائياً.\n" +
                       "4. بعد الحفظ، يتم زيادة أرصدة المخازن وتسجيل الديون للمورد في حسابه.";
            }

            if (query.Contains("شرح شاشة جرد الأسعار") || query.Contains("شرح شاشة الجرد") || query.Contains("شاشة الجرد"))
            {
                return "⚖️ **شاشة جرد وتعديل الأسعار**:\n" +
                       "تتيح لك جرد مخزونك الفعلي وتعديل أسعار البيع والشراء مباشرة في نفس الجدول بمرونة عالية.\n" +
                       "1. اختر المستودع لعرض الأصناف المتوفرة فيه.\n" +
                       "2. اكتب الرصيد الفعلي المجدود في عمود 'الرصيد الفعلي' وسيقوم البرنامج بحساب الفارق تلقائياً.\n" +
                       "3. يمكنك تعديل أسعار الشراء وأسعار البيع مباشرة لأي صنف في الجدول.\n" +
                       "4. اضغط على حفظ ليقوم البرنامج بعمل تسويات الأرصدة وتحديث أسعار البيع والشراء وتوثيق تعديل الأسعار في سجل التغييرات.";
            }

            if (query.Contains("شرح شاشة الموقف المالي") || query.Contains("الموقف المالي") || query.Contains("الموقف المالى"))
            {
                return "📊 **شاشة الموقف المالي للمكان (ميزانية عمومية مصغرة)**:\n" +
                       "تعرض لك تحليلاً بيانياً شاملاً للحالة المالية الحالية للمكان:\n" +
                       "1. **إجمالي النقدية**: مجموع الأموال المتاحة في كافة الخزائن والحسابات البنكية.\n" +
                       "2. **قيمة المخزون**: تكلفة بضاعة المخزن الحالية (بالشراء) والقيمة المتوقعة لبيعها، مع حساب الأرباح المتوقعة منها.\n" +
                       "3. **العملاء والموردين**: إجمالي ديون العملاء لنا، ومستحقات الموردين علينا.\n" +
                       "4. **صافي رأس المال الفعلي**: رأس مالك التشغيلي الحالي بالمعادلة المالية.";
            }

            if (query.Contains("شرح الحسابات") || query.Contains("التقارير المالية") || query.Contains("القوائم المالية") || query.Contains("مديول الحسابات") || query.Contains("شرح مديول الحسابات"))
            {
                return "⚖️ **شاشة ضبط الحسابات والقوائم المالية العامة**:\n" +
                       "هذا المديول يتيح لك إدارة كاملة للحسابات الدفترية وإصدار القوائم المالية:\n" +
                       "1. **شاشة ضبط الحسابات الدفترية**: لتسجيل الحسابات الافتتاحية للمكان وتعديل الأرصدة يدوياً لضمان تطابق الدفاتر مع الواقع.\n" +
                       "2. **قائمة الدخل والربحية**: تعرض لك بالتفصيل إجمالي المبيعات، وتكلفة البضاعة المباعة (COGS)، والخصومات، والمصروفات التشغيلية المفصلة، للوصول لـ 'صافي الربح قبل وبعد الضريبة' بدقة متناهية.\n" +
                       "3. **الميزانية العمومية**: توضح الوضع المالي للمؤسسة مقسماً إلى (الأصول المتداولة والثابتة) في الجانب المدين، و(الخصوم وحقوق الملكية) في الجانب الدائن بشكل متوازن ومحترف.\n" +
                       "4. **ملخص الموقف والمؤشرات**: لوحة قيادة تعرض لك ملخصات بيانية سريعة وإحصائيات دقيقة لرأس المال والتدفقات النقدية.";
            }

            if (query.Contains("اختصارات") || query.Contains("كيبورد") || query.Contains("لوحة المفاتيح") || query.Contains("مفاتيح") || query.Contains("زرار") || query.Contains("أزرار") || query.Contains("سريعة"))
            {
                return "⌨️ **اختصارات لوحة المفاتيح السريعة بالنظام**:\n\n" +
                       "🟢 **شاشة المبيعات (الفاتورة العادية)**:\n" +
                       "• `[F2]` : فاتورة جديدة (تصفير الحقول للبدء).\n" +
                       "• `[F3]` : فتح شاشة البحث السريع عن الأصناف.\n" +
                       "• `[F5]` : حفظ الفاتورة الحالية.\n" +
                       "• `[F9]` : طباعة الفاتورة الحالية.\n" +
                       "• `[F12]` : التركيز على مربع اختيار الصنف.\n" +
                       "• `[Ctrl + D]` : فتح درج الكاشير يدوياً بدون طباعة.\n" +
                       "• `[Ctrl + 1]` : تغيير وحدة الصنف الحالي للوحدة الكبرى (الأساسية).\n" +
                       "• `[Ctrl + 2]` : تغيير وحدة الصنف الحالي للوحدة المتوسطة.\n" +
                       "• `[Ctrl + 3]` : تغيير وحدة الصنف الحالي للوحدة الصغرى.\n" +
                       "• `[Insert]` : إضافة سطر إدخال باركود جديد يدوي بالجدول.\n\n" +
                       "🔵 **شاشة البيع السريع (الكاشير POS)**:\n" +
                       "• `[F2]` : فاتورة جديدة.\n" +
                       "• `[F5]` : إتمام البيع والدفع السريع.\n" +
                       "• `[F6]` : إعادة طباعة آخر فاتورة تم بيعها.\n" +
                       "• `[F12]` : التركيز التلقائي وتصفير مربع الباركود.\n" +
                       "• `[Ctrl + D]` : فتح درج الكاشير يدوياً بدون طباعة.\n" +
                       "• `[Esc]` : إلغاء الفاتورة الحالية (أو إغلاق الشاشة إذا كانت فارغة).";
            }

            if (query.Contains("شرح شاشة العملاء") || query.Contains("شاشة العملاء"))
            {
                return "👥 **شاشة العملاء**:\n" +
                       "تستخدم لإدارة حسابات الزبائن والتحصيلات النقدية.\n" +
                       "1. يمكنك إضافة عملاء جدد وتحديد سقف الائتمان والأرصدة الافتتاحية.\n" +
                       "2. اضغط على 'كشف حساب' لعرض قائمة تفصيلية بكافة المشتريات والمدفوعات والمتبقي على العميل.\n" +
                       "3. يمكنك تسجيل الدفعات المقبوضة نقداً من العميل لتنزيلها من حسابه فوراً.";
            }

            if (query.Contains("شرح شاشة الخزنة") || query.Contains("شاشة الخزنة") || query.Contains("الخزنة والدرج"))
            {
                return "💰 **شاشة الخزنة والدرج**:\n" +
                       "لمراقبة حركة الأموال النقدية والبنكية الواردة والصادرة:\n" +
                       "1. يعرض لك العمليات والمدفوعات والمقبوضات اليومية وتوقيتاتها ومن سجلها.\n" +
                       "2. عمليات البيع السريع وتذاكر الصيانة المستلمة تدخل تلقائياً كوارد في الدرج.\n" +
                       "3. عمليات الشراء من الموردين ودفع المصاريف تسجل كصادر نقدية لضبط الرصيد الفعلي للدرج بالقرش.";
            }

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

            if (query.Contains("شاشات التوزيع") || query.Contains("شرح التوزيع") || query.Contains("شرح الحمولات") || query.Contains("حركة الحمولات"))
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
