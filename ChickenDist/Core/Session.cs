using System;
using System.Collections.Generic;

namespace ChickenDist.Core
{
    /// <summary>بيانات المستخدم الحالي في الجلسة</summary>
    public static class Session
    {
        public static int EmpID { get; set; }
        public static string EmpName { get; set; }
        public static string UserName { get; set; }
        public static string Role { get; set; }
        public static bool IsDriver { get; set; }

        // قائمة الشاشات المسموح بها
        private static Dictionary<string, PermInfo> _perms = new Dictionary<string, PermInfo>();

        public static void LoadPermissions(int empID)
        {
            _perms.Clear();
            if (Role == "Admin")
            {
                // المدير لديه كل الصلاحيات
                foreach (var s in AllScreens)
                    _perms[s] = new PermInfo { CanAccess = true, CanEditPrice = true };
                return;
            }

            var dt = DbHelper.Query(
                "SELECT ScreenName, CanAccess, CanEditPrice FROM Permissions WHERE EmpID=@id",
                DbHelper.P("@id", empID));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                // FIX: استخدام Convert.ToBoolean بدلاً من (bool)cast المباشر
                // الـ cast المباشر يرمي InvalidCastException لو كانت القيمة null أو نوع غلط
                try
                {
                    _perms[row["ScreenName"].ToString()] = new PermInfo
                    {
                        CanAccess    = row["CanAccess"]    != DBNull.Value && Convert.ToBoolean(row["CanAccess"]),
                        CanEditPrice = row["CanEditPrice"] != DBNull.Value && Convert.ToBoolean(row["CanEditPrice"])
                    };
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ بدلاً من إسقاط البرنامج
                    AppLogger.Error($"خطأ في قراءة صلاحيات الشاشة: {row["ScreenName"]}", ex, "Session.LoadPermissions");
                }
            }
        }

        public static bool CanAccess(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanAccess;
        }

        public static bool CanEditPrice(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanEditPrice;
        }

        public static void Clear()
        {
            EmpID = 0; EmpName = ""; UserName = ""; Role = ""; IsDriver = false;
            _perms.Clear();
        }

        public static readonly string[] AllScreens = {
            "Sales", "DriverHandover", "DriverSales", "ImportPreview", "Clients", "Products",
            "Vehicles", "CashBox", "Expenses", "Reports", "Employees", "Returns",
            "Suppliers", "Purchases", "Inventory"
        };
    }

    public class PermInfo
    {
        public bool CanAccess { get; set; }
        public bool CanEditPrice { get; set; }
    }
}
