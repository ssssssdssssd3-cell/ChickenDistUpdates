using System;
using System.Data;
using System.Threading;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// كاش مركزي لقائمة العملاء لتجنب طلب قاعدة البيانات عبر الشبكة عند فتح أي شاشة
    /// </summary>
    public static class ClientCache
    {
        private static DataTable _activeClients;
        private static DataTable _allClients;
        private static readonly object _lock = new object();

        public static DataTable GetActive()
        {
            if (_activeClients != null) return _activeClients;
            lock (_lock)
            {
                if (_activeClients != null) return _activeClients;
                _activeClients = ClientDAL.GetAll(activeOnly: true);
                return _activeClients;
            }
        }

        public static DataTable GetAll()
        {
            if (_allClients != null) return _allClients;
            lock (_lock)
            {
                if (_allClients != null) return _allClients;
                _allClients = ClientDAL.GetAll(activeOnly: false);
                return _allClients;
            }
        }

        public static void PreWarm()
        {
            if (_activeClients != null) return;
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
                _activeClients = null;
                _allClients = null;
            }
        }

        public static void Refresh()
        {
            Invalidate();
            PreWarm();
        }
    }
}
