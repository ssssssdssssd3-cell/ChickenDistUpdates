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
            // ===== 1. تحميل وتضمين مكتبة QRCoder.dll من داخل الـ EXE تلقائياً =====
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name != null && args.Name.StartsWith("QRCoder", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var asm = typeof(Program).Assembly;
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

            // استخراج QRCoder.dll في مجلد البرنامج تلقائياً إن لم تكن موجودة لضمان أقصى توافقية
            try
            {
                string targetDll = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QRCoder.dll");
                if (!System.IO.File.Exists(targetDll))
                {
                    var asm = typeof(Program).Assembly;
                    using (var stream = asm.GetManifestResourceStream("ChickenDist.QRCoder.dll"))
                    {
                        if (stream != null)
                        {
                            byte[] data = new byte[stream.Length];
                            stream.Read(data, 0, data.Length);
                            System.IO.File.WriteAllBytes(targetDll, data);
                        }
                    }
                }
            }
            catch { }

            // ===== إعداد اللغة العربية RTL على مستوى التطبيق كله =====
            var arCulture = new CultureInfo("ar-EG");
            Thread.CurrentThread.CurrentCulture   = arCulture;
            Thread.CurrentThread.CurrentUICulture = arCulture;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // تطبيق RTL على كل الشاشات التي ستُفتح
            Application.AddMessageFilter(new RtlMessageFilter());

            // ===== فحص الاتصال بقاعدة البيانات مرة واحدة قبل أي عملية =====
            // هذا يمنع ظهور رسائل خطأ متعددة عند فشل الاتصال بـ SQL Server
            if (!ChickenDist.Core.DbHelper.TryTestConnection(out string dbConnError))
            {
                MessageBox.Show(
                    "تعذّر الاتصال بقاعدة البيانات.\n\nيرجى التأكد من:\n• تشغيل خدمة SQL Server\n• صحة إعدادات ملف Settings.ini\n• الاتصال بالشبكة (للأجهزة الفرعية)\n\nيُرجى التواصل مع الدعم الفني.",
                    "خطأ الاتصال بقاعدة البيانات",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // فحص تطابق إصدار البرنامج الحالي مع قاعدة البيانات المحدثة
            if (!ChickenDist.Core.DbHelper.CheckAndEnforceVersion(ChickenDist.Core.UpdateManager.CurrentVersion))
            {
                return;
            }

            // Ensure database schema is up-to-date
            ChickenDist.Core.DbHelper.EnsureDatabaseSchema();

            // معالجة وتحديث أي تكاليف مفقودة في فواتير المبيعات السابقة تلقائياً في الخلفية
            System.Threading.Tasks.Task.Run(() => ChickenDist.DAL.SaleDAL.BackfillMissingCostPrices());

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
            try { Environment.Exit(0); } catch { }
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
