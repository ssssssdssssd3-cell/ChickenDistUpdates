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

        public static int? DefaultSafeID { get; set; }
        public static string AllowedSafeIDs { get; set; }
        public static bool CanSellCash { get; set; }
        public static bool CanSellCredit { get; set; }
        public static bool CanSellDriverLoad { get; set; }
        public static bool CanSellInstallment { get; set; }
        public static bool CanEditShippingCharge { get; set; }

        /// <summary>معرف الوردية الحالية المفتوحة (null = لا توجد وردية مفتوحة)</summary>
        public static int? CurrentShiftID { get; set; }


        // قائمة الشاشات المسموح بها
        private static Dictionary<string, PermInfo> _perms = new Dictionary<string, PermInfo>();

        public static void LoadPermissions(int empID)
        {
            _perms.Clear();
            if (Role == "Admin")
            {
                // المدير لديه كل الصلاحيات
                foreach (var s in AllScreens)
                    _perms[s] = new PermInfo { CanAccess = true, CanEditPrice = true, CanEditSalesInvoice = true, CanDeleteSalesInvoice = true, CanCopySalesInvoice = true, CanViewCost = true };
                return;
            }

            var dt = DbHelper.Query(
                "SELECT ScreenName, CanAccess, CanEditPrice, COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, COALESCE(CanViewCost, 0) AS CanViewCost FROM Permissions WHERE EmpID=@id",
                DbHelper.P("@id", empID));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    _perms[row["ScreenName"].ToString()] = new PermInfo
                    {
                        CanAccess    = row["CanAccess"]    != DBNull.Value && Convert.ToBoolean(row["CanAccess"]),
                        CanEditPrice = row["CanEditPrice"] != DBNull.Value && Convert.ToBoolean(row["CanEditPrice"]),
                        CanEditSalesInvoice = row["CanEditSalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanEditSalesInvoice"]),
                        CanDeleteSalesInvoice = row.Table.Columns.Contains("CanDeleteSalesInvoice") && row["CanDeleteSalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanDeleteSalesInvoice"]),
                        CanCopySalesInvoice = row.Table.Columns.Contains("CanCopySalesInvoice") && row["CanCopySalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanCopySalesInvoice"]),
                        CanViewCost  = row["CanViewCost"]  != DBNull.Value && Convert.ToBoolean(row["CanViewCost"])
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

        public static bool CanEditSalesInvoice(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanEditSalesInvoice;
        }

        public static bool CanDeleteSalesInvoice(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanDeleteSalesInvoice;
        }

        public static bool CanCopySalesInvoice(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanCopySalesInvoice;
        }

        public static bool CanViewCost(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanViewCost;
        }

        public static void Clear()
        {
            EmpID = 0; EmpName = ""; UserName = ""; Role = ""; IsDriver = false;
            DefaultSafeID = null; AllowedSafeIDs = "";
            CanSellCash = true; CanSellCredit = true; CanSellDriverLoad = true; CanSellInstallment = true;
            CanEditShippingCharge = true;
            CurrentShiftID = null;
            _perms.Clear();
        }


        public static readonly string[] AllScreens = {
            "Sales", "Returns", "Installments", "SalesList", "SalesAudit", "AccountantPortal",
            "Clients", "InactiveClients", "Vehicles",
            "Purchases", "PurchaseReturn", "PurchasesList",
            "Suppliers", "SupplierStatement", "SupplierPayment", "SupplierAdjustment",
            "Products", "Categories", "ImportProducts", "Warehouses", "Inventory", "Wastage",
            "WarehouseTransfer", "WarehouseTransfersList", "PriceChanges", "BulkPrintBarcodes",
            "CashBox", "Reports", "DailyClosing", "Employees", "EmployeeTransactions",
            "DriverHandover", "DriverPortal", "ImportPreview", "DriversMonitor", "DriverCustody", "DriverLeaderboard",
            "Settings", "BotManager",
            "POS", "ShiftClose",
            "DashTreasury", "DashSales", "DashLoads", "DashBelowMin"
        };
    }

    public class PermInfo
    {
        public bool CanAccess { get; set; }
        public bool CanEditPrice { get; set; }
        public bool CanEditSalesInvoice { get; set; }
        public bool CanDeleteSalesInvoice { get; set; }
        public bool CanCopySalesInvoice { get; set; }
        public bool CanViewCost { get; set; }
    }
}
