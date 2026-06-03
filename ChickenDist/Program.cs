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

            // ===== فحص اتصال قاعدة البيانات قبل أي شيء =====
            if (!CheckDatabaseConnection())
                return;

            // Ensure database schema is up-to-date
            ChickenDist.Core.DbHelper.EnsureDatabaseSchema();

            // استخراج صفحة المندوب من الموارد المضمنة
            ExtractDriverSalesHtml();

            // Show Login
            var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK)
                return;

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
                            // استثناء الأزرار، جداول البيانات، والنصوص متعددة الأسطر
                            if (activeControl is Button || 
                                activeControl is DataGridView || 
                                (activeControl is TextBox txt && txt.Multiline))
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
}
