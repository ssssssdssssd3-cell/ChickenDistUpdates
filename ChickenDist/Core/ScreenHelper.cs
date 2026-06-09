using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>
    /// مساعد الشاشة - يضمن أن جميع الفورمات تعمل بشكل احترافي على دقة 1366×768
    /// </summary>
    public static class ScreenHelper
    {
        // الدقة المرجعية التي صُمم عليها البرنامج
        private const int RefWidth  = 1366;
        private const int RefHeight = 768;

        /// <summary>الدقة الفعلية للشاشة الرئيسية</summary>
        public static int ScreenW => Screen.PrimaryScreen.WorkingArea.Width;
        public static int ScreenH => Screen.PrimaryScreen.WorkingArea.Height;

        /// <summary>هل الشاشة صغيرة (أقل من 1400 عرض)</summary>
        public static bool IsSmallScreen => ScreenW < 1400;

        /// <summary>نسبة التوسع/التقليص الأفقي</summary>
        public static float ScaleX => (float)ScreenW / RefWidth;

        /// <summary>نسبة التوسع/التقليص العمودي</summary>
        public static float ScaleY => (float)ScreenH / RefHeight;

        /// <summary>
        /// يُطبق الإعدادات المناسبة على أي فورم ليعمل بشكل صحيح على أي دقة.
        /// يجب استدعاؤه في بداية كل InitializeComponent/InitUI.
        /// </summary>
        public static void FitForm(Form form, bool maximize = true)
        {
            if (maximize)
            {
                form.WindowState  = FormWindowState.Maximized;
                form.MinimumSize  = new Size(1024, 600);
            }
            form.StartPosition        = FormStartPosition.CenterScreen;
            form.AutoScaleMode        = AutoScaleMode.Dpi;
            form.AutoScaleDimensions  = new SizeF(96F, 96F);
        }

        /// <summary>
        /// يُطبق الإعدادات المناسبة على فورم حوار (Dialog) صغير.
        /// يضمن أن الحوار لا يتجاوز حدود الشاشة.
        /// </summary>
        public static void FitDialog(Form form, int preferredWidth, int preferredHeight)
        {
            int safeW = Math.Min(preferredWidth,  ScreenW - 40);
            int safeH = Math.Min(preferredHeight, ScreenH - 80);
            form.Size          = new Size(safeW, safeH);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimumSize   = new Size(Math.Min(preferredWidth, 400), Math.Min(preferredHeight, 300));
            form.AutoScaleMode = AutoScaleMode.Dpi;
        }

        /// <summary>
        /// يحسب ارتفاع Panel الهيدر المناسب بناءً على حجم الشاشة.
        /// يُرجع ارتفاعاً أصغر على الشاشات الصغيرة.
        /// </summary>
        public static int HeaderHeight(int normal, int compact)
        {
            return IsSmallScreen ? compact : normal;
        }

        /// <summary>
        /// يُحوّل مقياساً من الدقة المرجعية للدقة الفعلية (للأبعاد الأفقية)
        /// </summary>
        public static int ScaleWidth(int value)
        {
            return (int)(value * ScaleX);
        }

        /// <summary>
        /// يُحوّل مقياساً من الدقة المرجعية للدقة الفعلية (للأبعاد العمودية)
        /// </summary>
        public static int ScaleHeight(int value)
        {
            return (int)(value * ScaleY);
        }

        /// <summary>
        /// يُطبق خصائص AutoScroll و Anchor على جميع الـ Panels الثابتة في فورم
        /// لضمان ظهور scrollbar عند الحاجة على الشاشات الصغيرة
        /// </summary>
        public static void EnableScrollIfNeeded(Panel panel)
        {
            if (IsSmallScreen)
                panel.AutoScroll = true;
        }

        /// <summary>
        /// يعيد حساب حجم الخط ليناسب الشاشة الصغيرة
        /// </summary>
        public static Font ScaleFont(Font font)
        {
            if (!IsSmallScreen) return font;
            float newSize = Math.Max(7.5f, font.Size * 0.9f);
            return new Font(font.FontFamily, newSize, font.Style);
        }
    }
}
