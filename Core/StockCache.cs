using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// كاش فائق السرعة لبيانات أرصدة المخزون في الذاكرة (In-Memory Stock Cache)
    /// يمنع تكرار استعلامات تجميع الحركات المعقدة (10 جداول) عند فتح شاشات البيع والبحث
    /// مما يجعل شاشة البحث وشاشة البيع تفتح في أجزاء من الثانية (0 لاج) حتى بعد فترات الخمول
    /// </summary>
    public static class StockCache
    {
        private static readonly ConcurrentDictionary<int, Dictionary<int, decimal>> _warehouseStockCache = new ConcurrentDictionary<int, Dictionary<int, decimal>>();
        private static Dictionary<int, decimal> _globalStockCache = null;
        private static readonly ConcurrentDictionary<int, DateTime> _warehouseLastUpdated = new ConcurrentDictionary<int, DateTime>();
        private static DateTime _globalLastUpdated = DateTime.MinValue;
        private static readonly object _syncLock = new object();
        
        // صلاحية الكاش في الذاكرة (دقيقتان) لتوفير أقصى سرعة واستجابة فورية
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

        /// <summary>
        /// جلب ملخص الأرصدة للمخزن المحدد (أو لجميع المخازن إذا كان null) من الكاش أو من قاعدة البيانات
        /// </summary>
        public static Dictionary<int, decimal> GetStockSummary(int? warehouseID, bool forceRefresh = false)
        {
            int key = warehouseID ?? -1;
            DateTime now = DateTime.Now;

            if (!forceRefresh)
            {
                if (warehouseID.HasValue)
                {
                    if (_warehouseStockCache.TryGetValue(key, out var cached) && 
                        _warehouseLastUpdated.TryGetValue(key, out var lastTime) && 
                        (now - lastTime) < CacheTtl && cached != null && cached.Count > 0)
                    {
                        return cached;
                    }
                }
                else
                {
                    if (_globalStockCache != null && (now - _globalLastUpdated) < CacheTtl && _globalStockCache.Count > 0)
                    {
                        return _globalStockCache;
                    }
                }
            }

            lock (_syncLock)
            {
                // فحص مزدوج داخل الـ lock
                if (!forceRefresh)
                {
                    if (warehouseID.HasValue)
                    {
                        if (_warehouseStockCache.TryGetValue(key, out var cached) && 
                            _warehouseLastUpdated.TryGetValue(key, out var lastTime) && 
                            (now - lastTime) < CacheTtl && cached != null && cached.Count > 0)
                        {
                            return cached;
                        }
                    }
                    else
                    {
                        if (_globalStockCache != null && (now - _globalLastUpdated) < CacheTtl && _globalStockCache.Count > 0)
                        {
                            return _globalStockCache;
                        }
                    }
                }

                try
                {
                    var fresh = InventoryDAL.GetStockSummary(warehouseID);
                    if (warehouseID.HasValue)
                    {
                        _warehouseStockCache[key] = fresh;
                        _warehouseLastUpdated[key] = DateTime.Now;
                    }
                    else
                    {
                        _globalStockCache = fresh;
                        _globalLastUpdated = DateTime.Now;
                    }
                    return fresh;
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"StockCache.GetStockSummary failed: {ex.Message}", "StockCache");
                    // في حال الخطأ نعيد الكاش القديم إن وجد بدلاً من انهيار الشاشة
                    if (warehouseID.HasValue && _warehouseStockCache.TryGetValue(key, out var fallback))
                        return fallback;
                    if (!warehouseID.HasValue && _globalStockCache != null)
                        return _globalStockCache;
                    return new Dictionary<int, decimal>();
                }
            }
        }

        /// <summary>
        /// تفريغ الكاش وإعادة تحميله عند حدوث حركة بيع أو شراء أو تسوية مخزنية
        /// </summary>
        public static void Invalidate(int? warehouseID = null)
        {
            if (warehouseID.HasValue)
            {
                _warehouseLastUpdated.TryRemove(warehouseID.Value, out _);
                _warehouseStockCache.TryRemove(warehouseID.Value, out _);
            }
            else
            {
                _warehouseStockCache.Clear();
                _warehouseLastUpdated.Clear();
                _globalStockCache = null;
                _globalLastUpdated = DateTime.MinValue;
            }
        }

        /// <summary>
        /// تسخين وتحميل الأرصدة في الخلفية في بداية تشغيل البرنامج أو بعد المعاملات الكبرى
        /// </summary>
        public static void PreWarm(int? defaultWarehouseID = null)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (defaultWarehouseID.HasValue && defaultWarehouseID.Value > 0)
                    {
                        GetStockSummary(defaultWarehouseID.Value, forceRefresh: true);
                    }
                    GetStockSummary(null, forceRefresh: true);
                }
                catch { }
            });
        }
    }
}
