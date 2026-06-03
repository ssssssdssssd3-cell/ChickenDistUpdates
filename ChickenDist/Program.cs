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

            // ===== فحص اتصال قاعدة البيانات قبل أي شيء =====
            if (!CheckDatabaseConnection())
                return;

            // Ensure database schema is up-to-date
            ChickenDist.Core.DbHelper.EnsureDatabaseSchema();

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
    }

    /// <summary>يضمن RTL لكل الـ MessageBox وDialogs</summary>
    internal class RtlMessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m) => false;
    }
}
