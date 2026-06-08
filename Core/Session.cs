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
                    _perms[s] = new PermInfo { CanAccess = true, CanEditPrice = true, CanShowCostProfit = true };
                return;
            }

            var dt = DbHelper.Query(
                "SELECT ScreenName, CanAccess, CanEditPrice, COALESCE(CanShowCostProfit, 0) AS CanShowCostProfit FROM Permissions WHERE EmpID=@id",
                DbHelper.P("@id", empID));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                _perms[row["ScreenName"].ToString()] = new PermInfo
                {
                    CanAccess    = (bool)row["CanAccess"],
                    CanEditPrice = (bool)row["CanEditPrice"],
                    CanShowCostProfit = row.Table.Columns.Contains("CanShowCostProfit") && row["CanShowCostProfit"] != DBNull.Value && Convert.ToBoolean(row["CanShowCostProfit"])
                };
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

        public static bool CanShowCostProfit(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanShowCostProfit;
        }

        public static void Clear()
        {
            EmpID = 0; EmpName = ""; UserName = ""; Role = ""; IsDriver = false;
            _perms.Clear();
        }

        public static readonly string[] AllScreens = {
            "Sales", "DriverHandover", "Clients", "Products",
            "CashBox", "Expenses", "Reports", "Employees", "Returns",
            "Suppliers", "Purchases"
        };
    }

    public class PermInfo
    {
        public bool CanAccess { get; set; }
        public bool CanEditPrice { get; set; }
        public bool CanShowCostProfit { get; set; }
    }
}
