using System;
using System.Data;
using System.Threading;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// كاش مركزي لبيانات الأصناف - يحمّل مرة واحدة ويُعاد استخدامه في كل الشاشات
    /// لتجنب استعلام قاعدة البيانات عن 7000+ صنف في كل مرة تفتح فيها شاشة
    /// </summary>
    public static class ProductCache
    {
        private static DataTable _activeProducts;
        private static DataTable _allProducts;
        private static readonly object _lock = new object();
        private static bool _isLoading = false;

        /// <summary>
        /// الحصول على الأصناف النشطة فقط (مع كاش)
        /// </summary>
        public static DataTable GetActive()
        {
            if (_activeProducts != null) return _activeProducts;
            lock (_lock)
            {
                if (_activeProducts != null) return _activeProducts;
                _activeProducts = ProductDAL.GetAll(activeOnly: true);
                return _activeProducts;
            }
        }

        /// <summary>
        /// الحصول على جميع الأصناف (مع كاش)
        /// </summary>
        public static DataTable GetAll()
        {
            if (_allProducts != null) return _allProducts;
            lock (_lock)
            {
                if (_allProducts != null) return _allProducts;
                _allProducts = ProductDAL.GetAll(activeOnly: false);
                return _allProducts;
            }
        }

        /// <summary>
        /// تحميل الكاش في الخلفية عند بدء التطبيق
        /// </summary>
        public static void PreWarm()
        {
            if (_isLoading || _activeProducts != null) return;
            _isLoading = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    GetActive();
                    GetAll();
                }
                catch { }
                finally
                {
                    _isLoading = false;
                }
            });
        }

        /// <summary>
        /// إبطال الكاش (يتم استدعاؤه بعد حفظ/حذف/استيراد صنف)
        /// </summary>
        public static void Invalidate()
        {
            lock (_lock)
            {
                _activeProducts = null;
                _allProducts = null;
            }
        }

        /// <summary>
        /// إبطال الكاش ثم إعادة تحميله فوراً في الخلفية
        /// </summary>
        public static void Refresh()
        {
            Invalidate();
            PreWarm();
        }
    }
}
