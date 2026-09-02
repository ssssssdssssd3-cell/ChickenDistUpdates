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

            // فحص تطابق إصدار البرنامج الحالي مع قاعدة البيانات المحدثة
            if (!ChickenDist.Core.DbHelper.CheckAndEnforceVersion(ChickenDist.Core.UpdateManager.CurrentVersion))
            {
                return;
            }

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

            // بدء خدمة المزامنة السحابية اللحظية مع تطبيق المالك بالخلفية
            ChickenDist.Services.CloudSyncService.StartAutoBackgroundSync();

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

    /// <summary>يضمن تطبيق نظام RTL الكامل لكل شاشات ونوافذ البرنامج ومربعات الحوار تلقائياً</summary>
    internal class RtlMessageFilter : IMessageFilter
    {
        private static readonly System.Collections.Generic.HashSet<IntPtr> _processedHandles = new System.Collections.Generic.HashSet<IntPtr>();

        public bool PreFilterMessage(ref Message m)
        {
            // 0x0018 = WM_SHOWWINDOW, 0x0006 = WM_ACTIVATE
            if ((m.Msg == 0x0018 && m.WParam != IntPtr.Zero) || m.Msg == 0x0006)
            {
                if (!_processedHandles.Contains(m.HWnd))
                {
                    _processedHandles.Add(m.HWnd);
                    try
                    {
                        var ctrl = Control.FromHandle(m.HWnd);
                        if (ctrl is Form form)
                        {
                            ChickenDist.Core.Theme.ApplyFormRTL(form);
                        }
                    }
                    catch { }
                }
            }
            return false;
        }
    }
}
