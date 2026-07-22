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
        public static bool CanSelectDriver { get; set; }

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
                    _perms[s] = new PermInfo { CanAccess = true, CanAdd = true, CanEdit = true, CanDelete = true, CanEditPrice = true, CanEditSalesInvoice = true, CanDeleteSalesInvoice = true, CanCopySalesInvoice = true, CanViewCost = true, CanOrderColumns = true, CanViewDetails = true, CanViewBalance = true, CanChangeSafe = true };
                return;
            }

            var dt = DbHelper.Query(
                "SELECT ScreenName, CanAccess, COALESCE(CanAdd, 1) AS CanAdd, COALESCE(CanEdit, 1) AS CanEdit, COALESCE(CanDelete, 1) AS CanDelete, CanEditPrice, COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, COALESCE(CanViewCost, 0) AS CanViewCost, COALESCE(CanOrderColumns, 0) AS CanOrderColumns, COALESCE(CanViewDetails, 1) AS CanViewDetails, COALESCE(CanViewBalance, 1) AS CanViewBalance, COALESCE(CanChangeSafe, 1) AS CanChangeSafe FROM Permissions WHERE EmpID=@id",
                DbHelper.P("@id", empID));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    _perms[row["ScreenName"].ToString()] = new PermInfo
                    {
                        CanAccess    = row["CanAccess"]    != DBNull.Value && Convert.ToBoolean(row["CanAccess"]),
                        CanAdd       = row.Table.Columns.Contains("CanAdd") && row["CanAdd"] != DBNull.Value ? Convert.ToBoolean(row["CanAdd"]) : true,
                        CanEdit      = row.Table.Columns.Contains("CanEdit") && row["CanEdit"] != DBNull.Value ? Convert.ToBoolean(row["CanEdit"]) : true,
                        CanDelete    = row.Table.Columns.Contains("CanDelete") && row["CanDelete"] != DBNull.Value ? Convert.ToBoolean(row["CanDelete"]) : true,
                        CanEditPrice = row["CanEditPrice"] != DBNull.Value && Convert.ToBoolean(row["CanEditPrice"]),
                        CanEditSalesInvoice = row["CanEditSalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanEditSalesInvoice"]),
                        CanDeleteSalesInvoice = row.Table.Columns.Contains("CanDeleteSalesInvoice") && row["CanDeleteSalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanDeleteSalesInvoice"]),
                        CanCopySalesInvoice = row.Table.Columns.Contains("CanCopySalesInvoice") && row["CanCopySalesInvoice"] != DBNull.Value && Convert.ToBoolean(row["CanCopySalesInvoice"]),
                        CanViewCost  = row["CanViewCost"]  != DBNull.Value && Convert.ToBoolean(row["CanViewCost"]),
                        CanOrderColumns = row.Table.Columns.Contains("CanOrderColumns") && row["CanOrderColumns"] != DBNull.Value && Convert.ToBoolean(row["CanOrderColumns"]),
                        CanViewDetails = row.Table.Columns.Contains("CanViewDetails") && row["CanViewDetails"] != DBNull.Value ? Convert.ToBoolean(row["CanViewDetails"]) : true,
                        CanViewBalance = row.Table.Columns.Contains("CanViewBalance") && row["CanViewBalance"] != DBNull.Value ? Convert.ToBoolean(row["CanViewBalance"]) : true,
                        CanChangeSafe = row.Table.Columns.Contains("CanChangeSafe") && row["CanChangeSafe"] != DBNull.Value ? Convert.ToBoolean(row["CanChangeSafe"]) : true
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

        public static bool CanAdd(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanAdd;
        }

        public static bool CanEdit(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanEdit;
        }

        public static bool CanDelete(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanDelete;
        }

        public static bool CanViewDetails(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanViewDetails;
        }

        public static bool CanViewBalance(string screen = "CashBox")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanViewBalance;
        }

        public static bool CanChangeSafe(string screen = "Sales")
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanChangeSafe;
        }

        public static bool CanOrderColumns(string screen)
        {
            if (Role == "Admin") return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanOrderColumns;
        }

        public static void SaveColumnOrder(System.Windows.Forms.DataGridView dgv, string gridKey)
        {
            try
            {
                var list = new List<string>();
                foreach (System.Windows.Forms.DataGridViewColumn col in dgv.Columns)
                {
                    list.Add($"{col.Name}:{col.DisplayIndex}");
                }
                string val = string.Join(",", list);
                AppConfig.Set($"GridCols_{gridKey}_Emp_{EmpID}", val);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error saving column order", ex, "Session.SaveColumnOrder");
            }
        }

        public static void LoadColumnOrder(System.Windows.Forms.DataGridView dgv, string gridKey)
        {
            try
            {
                string val = AppConfig.Get($"GridCols_{gridKey}_Emp_{EmpID}", "");
                if (string.IsNullOrEmpty(val)) return;

                var parts = val.Split(',');
                var dict = new Dictionary<string, int>();
                foreach (var part in parts)
                {
                    var pair = part.Split(':');
                    if (pair.Length == 2 && int.TryParse(pair[1], out int idx))
                    {
                        dict[pair[0]] = idx;
                    }
                }

                var sortedCols = new List<System.Windows.Forms.DataGridViewColumn>();
                foreach (System.Windows.Forms.DataGridViewColumn col in dgv.Columns)
                {
                    if (dict.ContainsKey(col.Name))
                    {
                        sortedCols.Add(col);
                    }
                }
                sortedCols.Sort((a, b) => dict[a.Name].CompareTo(dict[b.Name]));

                foreach (var col in sortedCols)
                {
                    col.DisplayIndex = dict[col.Name];
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error loading column order", ex, "Session.LoadColumnOrder");
            }
        }

        public static void Clear()
        {
            EmpID = 0; EmpName = ""; UserName = ""; Role = ""; IsDriver = false;
            DefaultSafeID = null; AllowedSafeIDs = "";
            CanSellCash = true; CanSellCredit = true; CanSellDriverLoad = true; CanSellInstallment = true;
            CanEditShippingCharge = true;
            CanSelectDriver = true;
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
        public bool CanAdd { get; set; } = true;
        public bool CanEdit { get; set; } = true;
        public bool CanDelete { get; set; } = true;
        public bool CanEditPrice { get; set; }
        public bool CanEditSalesInvoice { get; set; }
        public bool CanDeleteSalesInvoice { get; set; }
        public bool CanCopySalesInvoice { get; set; }
        public bool CanViewCost { get; set; }
        public bool CanOrderColumns { get; set; }
        public bool CanViewDetails { get; set; } = true;
        public bool CanViewBalance { get; set; } = true;
        public bool CanChangeSafe { get; set; } = true;
    }
}
