using System;
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

            // Ensure database schema is up-to-date
            ChickenDist.Core.DbHelper.EnsureDatabaseSchema();

            // Ensure MobileApp folder exists for owner
            ChickenDist.Services.CloudSyncService.EnsureMobileAppFolderExists();

            // فحص تاريخ وساعة الويندوز للتأكد من سلامة التقارير
            if (!ChickenDist.Core.DbHelper.ValidateSystemDate(out string dateWarning))
            {
                MessageBox.Show(dateWarning, "⚠️ تنبيه تاريخ الويندوز", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Pre-warm product, client, and supplier caches asynchronously so opening screens is instant over LAN
            ChickenDist.Core.ProductCache.PreWarm();
            ChickenDist.Core.ClientCache.PreWarm();
            ChickenDist.Core.SupplierCache.PreWarm();

            // التحقق من تفعيل ترخيص البرنامج
            if (!ChickenDist.Core.LicenseManager.CheckLicense())
            {
                return;
            }

            // Show Login
            var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK)
                return;

            // Open Main
            Application.Run(new FrmMain());
        }
    }

    /// <summary>يضمن RTL لكل الـ MessageBox وDialogs</summary>
    internal class RtlMessageFilter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m) => false;
    }
}
