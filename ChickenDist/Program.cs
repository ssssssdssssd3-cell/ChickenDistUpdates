using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ChickenDist.Forms;

namespace ChickenDist
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // ===== تحميل QRCoder.dll من داخل الـ EXE نفسه (لازم قبل أي شيء) =====
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name != null && args.Name.StartsWith("QRCoder", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var asm = typeof(Program).Assembly;
                        // اسم المورد المضمّن
                        string resName = "ChickenDist.QRCoder.dll";
                        using (var stream = asm.GetManifestResourceStream(resName))
                        {
                            if (stream != null)
                            {
                                byte[] data = new byte[stream.Length];
                                stream.Read(data, 0, data.Length);
                                return System.Reflection.Assembly.Load(data);
                            }
                        }
                    }
                    catch { }
                }
                return null;
            };

            // ===== إعداد اللغة العربية RTL على مستوى التطبيق كله =====
            var arCulture = new CultureInfo("ar-EG");
            Thread.CurrentThread.CurrentCulture   = arCulture;
            Thread.CurrentThread.CurrentUICulture = arCulture;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // تطبيق RTL على كل الشاشات التي ستُفتح
            Application.AddMessageFilter(new RtlMessageFilter());

            // تحويل مفتاح Enter إلى Tab للتنقل بين الخانات تلقائياً
            Application.AddMessageFilter(new EnterKeyFilter());

            // تفعيل التمرير بعجلة الماوس لأي عنصر تحت المؤشر (بدون الحاجة للنقر أولاً)
            Application.AddMessageFilter(new MouseWheelFilter());

            // ===== فحص اتصال قاعدة البيانات قبل أي شيء =====
            if (!CheckDatabaseConnection())
                return;

            // ===== فحص ترخيص البرنامج =====
            if (!ChickenDist.Core.LicenseManager.CheckLicense())
                return;

            // Ensure database schema is up-to-date
            ChickenDist.Core.DbHelper.EnsureDatabaseSchema();

            // التحقق من إصدار البرنامج وقاعدة البيانات لمنع الأيقونات القديمة
            if (!ChickenDist.Core.DbHelper.CheckAndEnforceVersion(ChickenDist.Core.UpdateManager.CurrentVersion))
            {
                return; // إنهاء التطبيق فوراً
            }

            // استخراج صفحة المندوب من الموارد المضمنة
            ExtractDriverSalesHtml();

            // Show Login
            var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK)
                return;

            // فحص النسخ الاحتياطي — يعرض تحذير إذا مضى أكثر من 24 ساعة
            ChickenDist.Core.BackupManager.CheckAndWarnIfOverdue();

            // Open Main
            Application.Run(new FrmMain());
        }

        /// <summary>
        /// يتحقق من أن SQL Server يعمل وقاعدة البيانات موجودة.
        /// يعرض رسالة واضحة ويُرجع false لو الاتصال فشل.
        /// </summary>
        private static bool CheckDatabaseConnection()
        {
            try
            {
                string connStr = ChickenDist.Core.DbHelper.GetConnectionStringForCheck();
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                }
                return true;
            }
            catch (SqlException ex)
            {
                string msg;
                // الكود 2 / 53 / -1 = SQL Server واقف أو الخادم غير موجود
                // الكود 4060 = قاعدة البيانات غير موجودة
                if (ex.Number == 2 || ex.Number == 53 || ex.Number == -1)
                {
                    msg = "⚠️ تعذّر الاتصال بقاعدة البيانات!\n\n" +
                          "السبب المرجّح: خدمة SQL Server متوقفة.\n\n" +
                          "الحل:\n" +
                          "1) افتح قائمة ابدأ واكتب: Services\n" +
                          "2) ابحث عن (SQL Server (SQLEXPRESS أو (MSSQLSERVER)\n" +
                          "3) انقر بالزر الأيمن واختر Start\n" +
                          "4) أعد تشغيل البرنامج\n\n" +
                          $"تفاصيل الخطأ: {ex.Message}";
                }
                else if (ex.Number == 4060)
                {
                    msg = "⚠️ قاعدة البيانات (ChickenDist) غير موجودة!\n\n" +
                          "يرجى التواصل مع مدير النظام لإنشاء قاعدة البيانات.\n\n" +
                          $"تفاصيل الخطأ: {ex.Message}";
                }
                else
                {
                    msg = $"⚠️ خطأ في الاتصال بقاعدة البيانات:\n\n{ex.Message}";
                }

                MessageBox.Show(msg, "خطأ في الاتصال",
                    MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"⚠️ خطأ غير متوقع عند بدء التشغيل:\n\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                return false;
            }
        }

        private static void ExtractDriverSalesHtml()
        {
            try
            {
                string formsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Forms");
                if (!System.IO.Directory.Exists(formsDir))
                {
                    System.IO.Directory.CreateDirectory(formsDir);
                }
                string path = System.IO.Path.Combine(formsDir, "driver_sales.html");

                var assembly = typeof(Program).Assembly;
                string resourceName = "ChickenDist.Forms.driver_sales.html";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var fileStream = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to extract driver_sales.html: " + ex.Message);
            }
        }
    }

    /// <summary>يضمن RTL لكل الـ MessageBox وDialogs</summary>
    internal class RtlMessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m) => false;
    }

    /// <summary>
    /// مرشح رسائل عالمي لتحويل مفتاح Enter إلى Tab
    /// لتسهيل التنقل بين خانات الإدخال في كافة شاشات البرنامج
    /// </summary>
    internal class EnterKeyFilter : IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
            {
                Keys keyCode = (Keys)(int)m.WParam & Keys.KeyCode;
                if (keyCode == Keys.Enter)
                {
                    Control ctrl = Control.FromHandle(m.HWnd);
                    if (ctrl != null)
                    {
                        Control activeControl = GetActiveControl(ctrl);
                        if (activeControl != null)
                        {
                            // استثناء الأزرار، جداول البيانات، النصوص متعددة الأسطر، وحقول الباركود/الاسكنر
                            if (activeControl is Button || 
                                activeControl is DataGridView || 
                                (activeControl is TextBox txt && txt.Multiline) ||
                                (activeControl.Name != null && activeControl.Name.IndexOf("Barcode", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                return false;
                            }

                            // استثناء أي عنصر يقع داخل جدول بيانات (مثل خلايا التعديل)
                            Control parent = activeControl.Parent;
                            while (parent != null)
                            {
                                if (parent is DataGridView)
                                    return false;
                                parent = parent.Parent;
                            }

                            // الانتقال للعنصر التالي
                            var form = activeControl.FindForm();
                            if (form != null)
                            {
                                // استخدام SelectNextControl للانتقال الطبيعي
                                form.SelectNextControl(activeControl, true, true, true, true);
                                return true; // إلغاء معالجة المفتاح الافتراضية لمنع صوت التنبيه (Beep)
                            }
                        }
                    }
                }
            }
            return false;
        }

        private Control GetActiveControl(Control c)
        {
            if (c == null) return null;
            var container = c as IContainerControl;
            if (container != null && container.ActiveControl != null)
            {
                return GetActiveControl(container.ActiveControl);
            }
            return c;
        }
    }

    /// <summary>
    /// مرشح عالمي لتمرير بكرة الماوس (Scroll Wheel) لأي عنصر تحت مؤشر الماوس
    /// يحل مشكلة ضرورة النقر على العنصر أولاً لتفعيل التمرير
    /// </summary>
    internal class MouseWheelFilter : IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                // احصل على العنصر تحت مؤشر الماوس
                var pt = new System.Drawing.Point(
                    (int)(short)((uint)m.LParam & 0xFFFF),         // X
                    (int)(short)(((uint)m.LParam >> 16) & 0xFFFF)  // Y
                );

                var ctrl = Control.FromHandle(m.HWnd);
                if (ctrl == null) return false;

                // ابحث عن العنصر تحت المؤشر
                var target = ctrl.TopLevelControl?.GetChildAtPoint(
                    ctrl.TopLevelControl.PointToClient(pt),
                    GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Disabled);

                if (target == null) target = ctrl;

                // إذا كان العنصر المستهدف مختلفاً عن مستقبل الرسالة الأصلي، أعد التوجيه
                if (target.Handle != m.HWnd)
                {
                    // أرسل رسالة التمرير للعنصر الصحيح
                    Win32.SendMessage(target.Handle, WM_MOUSEWHEEL, m.WParam, m.LParam);
                    return true; // منع المعالجة الافتراضية
                }
            }
            return false;
        }
    }

    /// <summary>استدعاءات Win32 API</summary>
    internal static class Win32
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}
