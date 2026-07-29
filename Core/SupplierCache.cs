using System;
using System.Data;
using System.Threading;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// كاش مركزي لقائمة الموردين لتجنب طلب قاعدة البيانات عبر الشبكة عند فتح أي شاشة
    /// </summary>
    public static class SupplierCache
    {
        private static DataTable _activeSuppliers;
        private static DataTable _allSuppliers;
        private static readonly object _lock = new object();

        public static DataTable GetActive()
        {
            if (_activeSuppliers != null) return _activeSuppliers;
            lock (_lock)
            {
                if (_activeSuppliers != null) return _activeSuppliers;
                _activeSuppliers = SupplierDAL.GetAll(activeOnly: true);
                return _activeSuppliers;
            }
        }

        public static DataTable GetAll()
        {
            if (_allSuppliers != null) return _allSuppliers;
            lock (_lock)
            {
                if (_allSuppliers != null) return _allSuppliers;
                _allSuppliers = SupplierDAL.GetAll(activeOnly: false);
                return _allSuppliers;
            }
        }

        public static void PreWarm()
        {
            if (_activeSuppliers != null) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    GetActive();
                }
                catch { }
            });
        }

        public static void Invalidate()
        {
            lock (_lock)
            {
                _activeSuppliers = null;
                _allSuppliers = null;
            }
        }

        public static void Refresh()
        {
            Invalidate();
            PreWarm();
        }
    }
}
