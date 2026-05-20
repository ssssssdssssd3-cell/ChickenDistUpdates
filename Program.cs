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
