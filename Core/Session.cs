using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    /// <summary>بيانات المستخدم الحالي في الجلسة</summary>
    public static class Session
    {
        public static int EmpID { get; set; }
        public static string EmpName { get; set; }
        public static string UserName { get; set; }
        public static string Role { get; set; }
        public static bool IsAdmin => !string.IsNullOrWhiteSpace(Role) && 
            (string.Equals(Role.Trim(), "Admin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(Role.Trim(), "مدير", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(Role.Trim(), "مدير النظام", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(Role.Trim(), "administrator", StringComparison.OrdinalIgnoreCase));
        public static bool IsDriver { get; set; }

        public static int? DefaultSafeID { get; set; }
        public static int GetDefaultSafeID()
        {
            if (DefaultSafeID.HasValue && DefaultSafeID.Value > 0)
                return DefaultSafeID.Value;

            try
            {
                object safeIdObj = DbHelper.Scalar("SELECT TOP 1 AccountID FROM SafeAccounts WHERE IsActive = 1 ORDER BY AccountID");
                if (safeIdObj != null && safeIdObj != DBNull.Value)
                {
                    return Convert.ToInt32(safeIdObj);
                }
            }
            catch (Exception ex) { AppLogger.Error("Session.GetDefaultSafeID", ex); }
            return 1;
        }

        public static string AllowedSafeIDs { get; set; }

        /// <summary>
        /// الحصول على معرّف الدرج/الخزنة المسموح بها للموظف الحالي.
        /// </summary>
        public static int GetPrimaryAllowedSafeID()
        {
            if (DefaultSafeID.HasValue && DefaultSafeID.Value > 0)
                return DefaultSafeID.Value;

            var set = GetAllowedSafeIDSet();
            if (set != null && set.Count > 0)
            {
                foreach (var id in set) return id;
            }

            return GetDefaultSafeID();
        }

        /// <summary>
        /// الحصول على مجموعة معرّفات الخزن / الأدراج المسموح بها للمستخدم الحالي.
        /// للأدمن: يعيد null (جميع الخزن مسموح بها بدون أي قيود).
        /// للموظف: يعيد قائمة الأدراج المخصصة له فقط.
        /// </summary>
        public static HashSet<int> GetAllowedSafeIDSet()
        {
            if (IsAdmin) return null;

            var set = new HashSet<int>();
            if (DefaultSafeID.HasValue && DefaultSafeID.Value > 0)
            {
                set.Add(DefaultSafeID.Value);
            }

            if (!string.IsNullOrWhiteSpace(AllowedSafeIDs))
            {
                var parts = AllowedSafeIDs.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p.Trim(), out int id) && id > 0)
                        set.Add(id);
                }
            }

            // إذا لم يكن محدداً له أي درج محدد وهو ليس أدمن، يُلزم بدرج النظام الافتراضي فقط
            if (set.Count == 0 && !IsAdmin)
            {
                int defId = GetDefaultSafeID();
                if (defId > 0) set.Add(defId);
            }

            return set;
        }

        /// <summary>
        /// التحقق هل الخزنة أو الدرج مسموح للمستخدم الحالي بالتعامل عليه ورؤية حركاته؟
        /// </summary>
        public static bool IsSafeAllowed(int safeId)
        {
            if (IsAdmin) return true;
            if (safeId <= 0) return false;
            var set = GetAllowedSafeIDSet();
            if (set == null) return true;
            return set.Contains(safeId);
        }
        public static bool CanSellCash { get; set; }
        public static bool CanSellCredit { get; set; }
        public static bool CanSellVisa { get; set; }
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
            if (IsAdmin)
            {
                // المدير لديه كل الصلاحيات للشاشات العادية
                foreach (var s in AllScreens)
                {
                    // شاشات الأصول والشركاء حساسة ولا تظهر لأي حساب إلا بعد تفعيلها صراحة في شاشة الصلاحيات
                    bool defaultAccess = (s != "FixedAssets" && s != "Shareholders");
                    _perms[s] = new PermInfo { CanAccess = defaultAccess, CanAdd = true, CanEdit = true, CanDelete = true, CanEditPrice = true, CanEditSalesInvoice = true, CanDeleteSalesInvoice = true, CanCopySalesInvoice = true, CanViewCost = true, CanOrderColumns = true, CanViewDetails = true, CanViewBalance = true, CanChangeSafe = true, CanViewSalesTotals = true, CanViewQuickItems = true };
                }

                // قراءة الصلاحيات المخصصة لحساب المدير (لتفعيل الأصول والشركاء عند اختيارها له)
                try
                {
                    var dtAdmin = DbHelper.Query("SELECT ScreenName, CanAccess FROM Permissions WHERE EmpID=@id AND ScreenName IN ('FixedAssets', 'Shareholders')", DbHelper.P("@id", empID));
                    foreach (System.Data.DataRow row in dtAdmin.Rows)
                    {
                        string sName = row["ScreenName"]?.ToString();
                        if (!string.IsNullOrEmpty(sName) && _perms.ContainsKey(sName))
                        {
                            _perms[sName].CanAccess = row["CanAccess"] != DBNull.Value && Convert.ToBoolean(row["CanAccess"]);
                        }
                    }
                }
                catch { }

                return;
            }

            var dt = DbHelper.Query(
                "SELECT ScreenName, CanAccess, COALESCE(CanAdd, 1) AS CanAdd, COALESCE(CanEdit, 1) AS CanEdit, COALESCE(CanDelete, 1) AS CanDelete, CanEditPrice, COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, COALESCE(CanViewCost, 0) AS CanViewCost, COALESCE(CanOrderColumns, 0) AS CanOrderColumns, COALESCE(CanViewDetails, 1) AS CanViewDetails, COALESCE(CanViewBalance, 1) AS CanViewBalance, COALESCE(CanChangeSafe, 1) AS CanChangeSafe, COALESCE(CanViewSalesTotals, 1) AS CanViewSalesTotals, COALESCE(CanViewQuickItems, 1) AS CanViewQuickItems FROM Permissions WHERE EmpID=@id",
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
                        CanChangeSafe = row.Table.Columns.Contains("CanChangeSafe") && row["CanChangeSafe"] != DBNull.Value ? Convert.ToBoolean(row["CanChangeSafe"]) : true,
                        CanViewSalesTotals = row.Table.Columns.Contains("CanViewSalesTotals") && row["CanViewSalesTotals"] != DBNull.Value ? Convert.ToBoolean(row["CanViewSalesTotals"]) : true,
                        CanViewQuickItems = row.Table.Columns.Contains("CanViewQuickItems") && row["CanViewQuickItems"] != DBNull.Value ? Convert.ToBoolean(row["CanViewQuickItems"]) : true
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
            if (string.IsNullOrEmpty(screen)) return true;

            // Handle comma-separated screen checks (e.g., "Reports,Financials")
            if (screen.Contains(","))
            {
                string[] parts = screen.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (!CanAccess(p.Trim())) return false;
                }
                return true;
            }

            // شاشات الأصول والشركاء: حساسة جداً ولا تظهر لأي حساب إلا بعد تفعيلها صراحة في شاشة الصلاحيات
            if (screen == "FixedAssets" || screen == "Shareholders")
            {
                if (_perms.ContainsKey(screen)) return _perms[screen].CanAccess;
                return false;
            }

            if (IsAdmin) return true;

            if (_perms.ContainsKey(screen) && _perms[screen].CanAccess) return true;

            // Synonym mapping for backwards-compatibility or UI mismatches
            if (screen == "DriverSales" && _perms.ContainsKey("DriverPortal") && _perms["DriverPortal"].CanAccess) return true;
            if (screen == "DriverPortal" && _perms.ContainsKey("DriverSales") && _perms["DriverSales"].CanAccess) return true;
            if (screen == "FinancialPosition" && _perms.ContainsKey("Reports") && _perms["Reports"].CanAccess) return true;
            if (screen == "RepFinancials" && _perms.ContainsKey("Financials") && _perms["Financials"].CanAccess) return true;
            if (screen == "Financials" && _perms.ContainsKey("Reports") && _perms["Reports"].CanAccess) return true;
            if (screen == "ScheduleClientDebt" && _perms.ContainsKey("Installments") && _perms["Installments"].CanAccess) return true;
            if (screen == "DuplicateCodesResolver" && _perms.ContainsKey("Products") && _perms["Products"].CanAccess) return true;
            if (screen == "MaintenanceCard" && _perms.ContainsKey("Maintenance") && _perms["Maintenance"].CanAccess) return true;
            if (screen == "QuickDetails" && (_perms.ContainsKey("DashSales") && _perms["DashSales"].CanAccess || _perms.ContainsKey("Reports") && _perms["Reports"].CanAccess)) return true;
            if (screen == "SupportBot" && _perms.ContainsKey("BotManager") && _perms["BotManager"].CanAccess) return true;
            if (screen == "InventorySessions" && _perms.ContainsKey("Inventory") && _perms["Inventory"].CanAccess) return true;

            // Fallback for detailed reports: If a specific report key (starting with Rep) is not configured in _perms, fall back to "Reports"
            if (screen.StartsWith("Rep") || screen.StartsWith("Reports_"))
            {
                if (_perms.ContainsKey(screen)) return _perms[screen].CanAccess;
                if (_perms.ContainsKey("Reports")) return _perms["Reports"].CanAccess;
            }

            return false;
        }

        public static bool CanEditPrice(string screen = "Sales")
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanEditPrice;
        }

        public static bool CanEditSalesInvoice(string screen = "Sales")
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanEditSalesInvoice;
        }

        public static bool CanDeleteSalesInvoice(string screen = "Sales")
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanDeleteSalesInvoice;
        }

        public static bool CanCopySalesInvoice(string screen = "Sales")
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanCopySalesInvoice;
        }

        /// <summary>
        /// هل يحق للمستخدم رؤية سعر التكلفة وهوامش الربح؟
        /// إذا كان الموظف مقفولاً عليه الدخول على كارت الصنف أو شاشة الأصناف، أو ليس لديه صلاحية رؤية التكلفة، تُحجب التكلفة نهائياً في كل مكان
        /// </summary>
        public static bool CanViewCost(string screen = "Sales")
        {
            if (IsAdmin) return true;

            // إذا كان الموظف مقفولاً عليه الدخول على كارت الصنف أو شاشة الأصناف، تُحجب التكلفة تماماً في كل البرنامج
            if (!CanAccess("ProductCard") || !CanAccess("Products")) return false;

            if (_perms.ContainsKey(screen) && _perms[screen].CanViewCost) return true;
            if (screen == "PriceQuote" && _perms.ContainsKey("Sales") && _perms["Sales"].CanViewCost) return true;
            if (screen == "Sales" && _perms.ContainsKey("Sales") && _perms["Sales"].CanViewCost) return true;
            if (screen == "POS" && _perms.ContainsKey("Sales") && _perms["Sales"].CanViewCost) return true;
            if (screen == "Products" && _perms.ContainsKey("Products") && _perms["Products"].CanViewCost) return true;
            if (screen == "ProductCard" && _perms.ContainsKey("Products") && _perms["Products"].CanViewCost) return true;
            if (screen == "Inventory" && _perms.ContainsKey("Inventory") && _perms["Inventory"].CanViewCost) return true;
            if (screen == "ShortageNotebook" && (_perms.ContainsKey("ShortageNotebook") && _perms["ShortageNotebook"].CanViewCost || _perms.ContainsKey("Inventory") && _perms["Inventory"].CanViewCost)) return true;
            if (screen == "Warehouses" && (_perms.ContainsKey("Warehouses") && _perms["Warehouses"].CanViewCost || _perms.ContainsKey("Products") && _perms["Products"].CanViewCost)) return true;
            if (screen == "Wastage" && (_perms.ContainsKey("Wastage") && _perms["Wastage"].CanViewCost || _perms.ContainsKey("Inventory") && _perms["Inventory"].CanViewCost)) return true;
            if (screen == "Reports" && (_perms.ContainsKey("Reports") && _perms["Reports"].CanViewCost || _perms.ContainsKey("Sales") && _perms["Sales"].CanViewCost)) return true;

            return false;
        }

        public static bool CanViewAnyCost()
        {
            if (IsAdmin) return true;
            if (!CanAccess("ProductCard") || !CanAccess("Products")) return false;
            return CanViewCost("Sales") || CanViewCost("Products") || CanViewCost("Inventory") || CanViewCost("Reports");
        }

        public static bool CanAdd(string screen)
        {
            if (IsAdmin) return true;
            if (string.IsNullOrEmpty(screen)) return true;

            if (_perms.ContainsKey(screen) && _perms[screen].CanAdd) return true;

            if (screen == "Returns" || screen == "PurchaseReturn")
            {
                if (_perms.ContainsKey(screen))
                    return _perms[screen].CanAdd;
                return CanAccess(screen);
            }

            if (screen == "ReceiptVoucher")
            {
                if (_perms.ContainsKey("ReceiptVoucher") && _perms["ReceiptVoucher"].CanAccess) return true;
                if (_perms.ContainsKey("CashBox") && (_perms["CashBox"].CanAdd || _perms["CashBox"].CanAccess)) return true;
                if (_perms.ContainsKey("DailyAccounts") && (_perms["DailyAccounts"].CanAdd || _perms["DailyAccounts"].CanAccess)) return true;
                if (_perms.ContainsKey("SafeAccounts") && (_perms["SafeAccounts"].CanAdd || _perms["SafeAccounts"].CanAccess)) return true;
            }
            if (screen == "CashBox")
            {
                if (_perms.ContainsKey("ReceiptVoucher") && (_perms["ReceiptVoucher"].CanAdd || _perms["ReceiptVoucher"].CanAccess)) return true;
                if (_perms.ContainsKey("DailyAccounts") && (_perms["DailyAccounts"].CanAdd || _perms["DailyAccounts"].CanAccess)) return true;
                if (_perms.ContainsKey("SafeAccounts") && (_perms["SafeAccounts"].CanAdd || _perms["SafeAccounts"].CanAccess)) return true;
            }

            return false;
        }

        /// <summary>
        /// هل يحق للموظف عمل مرتجع لجميع فواتير المبيعات (حتى فواتير الموظفين الآخرين) أم مبيعاته هو فقط؟
        /// </summary>
        public static bool CanReturnAllSales()
        {
            if (IsAdmin) return true;
            if (_perms.ContainsKey("Returns"))
                return _perms["Returns"].CanEditSalesInvoice;
            return false;
        }

        public static bool CanEdit(string screen)
        {
            if (IsAdmin) return true;
            if (string.IsNullOrEmpty(screen)) return true;

            if (_perms.ContainsKey(screen) && _perms[screen].CanEdit) return true;

            if (screen == "ReceiptVoucher" || screen == "CashBox")
            {
                if (_perms.ContainsKey("ReceiptVoucher") && _perms["ReceiptVoucher"].CanAccess) return true;
                if (_perms.ContainsKey("CashBox") && (_perms["CashBox"].CanEdit || _perms["CashBox"].CanAccess)) return true;
                if (_perms.ContainsKey("DailyAccounts") && (_perms["DailyAccounts"].CanEdit || _perms["DailyAccounts"].CanAccess)) return true;
            }

            return false;
        }

        public static bool CanDelete(string screen)
        {
            if (IsAdmin) return true;
            if (string.IsNullOrEmpty(screen)) return true;

            return _perms.ContainsKey(screen) && _perms[screen].CanDelete;
        }

        public static bool CanViewDetails(string screen)
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanViewDetails;
        }

        public static bool CanViewShiftDetails()
        {
            if (IsAdmin) return true;
            if (_perms.ContainsKey("ShiftClose")) return _perms["ShiftClose"].CanViewDetails;
            if (_perms.ContainsKey("ShiftsHistory")) return _perms["ShiftsHistory"].CanViewDetails;
            if (_perms.ContainsKey("POS")) return _perms["POS"].CanViewDetails;
            if (_perms.ContainsKey("Sales")) return _perms["Sales"].CanViewDetails;
            if (_perms.ContainsKey("DailyClosing")) return _perms["DailyClosing"].CanViewDetails;
            return false;
        }

        public static bool CanViewBalance(string screen = "CashBox")
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanViewBalance;
        }

        public static bool CanChangeSafe(string screen = "ReceiptVoucher")
        {
            if (IsAdmin) return true;

            if (!string.IsNullOrEmpty(screen) && _perms.ContainsKey(screen))
                return _perms[screen].CanChangeSafe;

            if (_perms.ContainsKey("ReceiptVoucher")) return _perms["ReceiptVoucher"].CanChangeSafe;
            if (_perms.ContainsKey("CashBox")) return _perms["CashBox"].CanChangeSafe;
            if (_perms.ContainsKey("DailyAccounts")) return _perms["DailyAccounts"].CanChangeSafe;
            if (_perms.ContainsKey("POS")) return _perms["POS"].CanChangeSafe;
            if (_perms.ContainsKey("Sales")) return _perms["Sales"].CanChangeSafe;

            return false;
        }

        public static bool CanOrderColumns(string screen)
        {
            if (IsAdmin) return true;
            return _perms.ContainsKey(screen) && _perms[screen].CanOrderColumns;
        }

        /// <summary>
        /// يتحقق مما إذا كانت كلمة المرور تخص أحد المسؤولين (Role='مدير' أو 'Admin' أو الماستر باسورد)
        /// </summary>
        public static bool VerifyAdminPassword(string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword)) return false;

            // كلمة المرور الرئيسية للطوارئ
            if (inputPassword == "Pro@soft2026" || inputPassword == "admin2026") return true;

            try
            {
                // فحص جميع المديرين النشطين في قاعدة البيانات
                var dtAdmins = DbHelper.Query(
                    "SELECT Password FROM Employees WHERE (Role = N'مدير' OR Role = 'Admin' OR Role = 'Administrator' OR Role = N'مدير النظام') AND IsActive = 1");

                foreach (DataRow row in dtAdmins.Rows)
                {
                    string stored = row["Password"]?.ToString();
                    if (!string.IsNullOrEmpty(stored) && PasswordHelper.Verify(inputPassword, stored))
                        return true;
                }

                // فحص المستخدم الحالي إذا كان مديراً
                if (IsAdmin && EmpID > 0)
                {
                    var dt = DbHelper.Query("SELECT Password FROM Employees WHERE EmpID = @id", DbHelper.P("@id", EmpID));
                    if (dt.Rows.Count > 0)
                    {
                        string stored = dt.Rows[0]["Password"]?.ToString();
                        if (!string.IsNullOrEmpty(stored) && PasswordHelper.Verify(inputPassword, stored))
                            return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("خطأ في التحقق من كلمة مرور المسؤول", ex, "Session.VerifyAdminPassword");
            }

            return false;
        }

        /// <summary>
        /// يفتح نافذة أنيقة لطلب وتأكيد كلمة مرور المسؤول للمتابعة
        /// </summary>
        public static bool PromptAdminPassword(IWin32Window owner = null, string reason = "")
        {
            using (var passForm = new Form())
            {
                passForm.Text = "🔒 تأكيد بصلاحية المسؤول (Admin)";
                passForm.Size = new Size(380, 210);
                passForm.StartPosition = FormStartPosition.CenterParent;
                passForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passForm.MaximizeBox = false;
                passForm.MinimizeBox = false;
                passForm.RightToLeft = RightToLeft.Yes;
                passForm.RightToLeftLayout = true;
                passForm.BackColor = Color.FromArgb(24, 28, 38);
                passForm.Font = new Font("Segoe UI", 10f);

                var pnlHeader = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 45,
                    BackColor = Color.FromArgb(35, 40, 55),
                    Padding = new Padding(12, 10, 12, 10)
                };
                var lblHeader = new Label
                {
                    Text = string.IsNullOrWhiteSpace(reason) ? "🔒 يرجى إدخال كلمة مرور المسؤول:" : $"🔒 {reason}:",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(250, 204, 21), // Gold
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlHeader.Controls.Add(lblHeader);

                var pnlBody = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20, 15, 20, 10),
                    BackColor = Color.Transparent
                };

                var lblDesc = new Label
                {
                    Text = "كلمة مرور المدير:",
                    Dock = DockStyle.Top,
                    Height = 24,
                    ForeColor = Color.FromArgb(220, 225, 235),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
                };

                var txtPass = new TextBox
                {
                    Dock = DockStyle.Top,
                    PasswordChar = '●',
                    Height = 32,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    BackColor = Color.FromArgb(40, 45, 60),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    RightToLeft = RightToLeft.No,
                    TextAlign = HorizontalAlignment.Center
                };

                pnlBody.Controls.Add(txtPass);
                pnlBody.Controls.Add(lblDesc);

                var pnlFooter = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = Color.FromArgb(18, 22, 30),
                    Padding = new Padding(12, 8, 12, 8)
                };

                var btnOk = new Button
                {
                    Text = "✅ تأكيد",
                    Size = new Size(100, 34),
                    Dock = DockStyle.Left,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(16, 185, 129),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                btnOk.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text = "❌ إلغاء",
                    Size = new Size(90, 34),
                    Dock = DockStyle.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.FlatAppearance.BorderSize = 0;

                pnlFooter.Controls.Add(btnOk);
                pnlFooter.Controls.Add(btnCancel);

                passForm.Controls.Add(pnlBody);
                passForm.Controls.Add(pnlHeader);
                passForm.Controls.Add(pnlFooter);
                passForm.AcceptButton = btnOk;
                passForm.CancelButton = btnCancel;

                passForm.Shown += (s, e) => txtPass.Focus();

                if (passForm.ShowDialog(owner) == DialogResult.OK)
                {
                    string input = txtPass.Text.Trim();
                    if (VerifyAdminPassword(input))
                    {
                        return true;
                    }
                    MessageBox.Show(owner, "❌ كلمة المرور غير صحيحة! لا يمكنك تعديل أو تخصيص ترتيب الأعمدة.", "خطأ في التحقق", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        public static bool CanViewSalesTotals(string screen = "SalesList")
        {
            if (IsAdmin || (Role != null && (Role.Contains("مدير") || Role.Contains("Admin")))) return true;
            if (_perms.ContainsKey(screen)) return _perms[screen].CanViewSalesTotals;
            return true;
        }

        public static bool CanViewQuickItems(string screen = "Sales")
        {
            if (IsAdmin) return true;
            if (_perms.ContainsKey(screen)) return _perms[screen].CanViewQuickItems;
            return true;
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
            CanSellCash = true; CanSellCredit = true; CanSellVisa = true; CanSellDriverLoad = true; CanSellInstallment = true;
            CanEditShippingCharge = true;
            CanSelectDriver = true;
            CurrentShiftID = null;
            _perms.Clear();
        }


        public static readonly string[] AllScreens = {
            // Sales & POS
            "Sales", "POS", "PriceQuote", "Returns", "Installments", "Reservations", "ClearanceOffers", "SalesList", "SalesAudit", "AccountantPortal", "ProductSearch", "Maintenance",
            "Clients", "ClientStatement", "InactiveClients", "Vehicles",
            
            // Purchases & Suppliers
            "Purchases", "PurchaseReturn", "PurchasesList",
            "Suppliers", "SupplierStatement", "SupplierPayment", "SupplierAdjustment",
            
            // Inventory & Products
            "Products", "ProductCard", "Categories", "Units", "ImportProducts", "Warehouses", "Inventory", "MinStockEdit", "ShortageNotebook", "InventoryVarianceReport", "Wastage",
            "WarehouseTransfer", "WarehouseTransfersList", "PriceChanges", "PricePoster", "ProductMovement", "BulkPrintBarcodes", "ClothingMatrix", "ModelLookup", "MultiBarcodes",
            
            // Finance, Safes & Shifts
            "CashBox", "SafeAccounts", "ActualBalances", "DailyAccounts", "ReceiptVoucher", "FinancialPosition", "Reports", "Financials", "DailyClosing", "ShiftClose", "ShiftsHistory",
            
            // Assets & Shareholders (🏢 الأصول والشركاء)
            "FixedAssets", "Shareholders",
            
            // Detailed Reports
            "RepDailySales", "RepSalesByPeriod", "RepDetailedSales", "RepDetailedSaleItems", "RepSalesByProduct", "RepSalesByCategory", "RepSalesByClient", "RepSalesByUser",
            "RepSalesByPayment", "RepSalesDiscounts", "RepDetailedReturns", "RepSalesProfit", "RepStagnantProducts",
            "RepPurchases", "RepDailyPurchases", "RepPurchasesByPeriod", "RepDetailedPurchases", "RepDetailedPurchaseItems", "RepPurchasesBySupplier", "RepPurchasesByProduct", "RepPurchasesByCategory", "RepPurchaseReturns", "RepSupplierPayments", "RepPurchasePrices", "RepCreditPurchases",
            "RepStores", "RepProductQtyDetail", "RepWastageLoss", "RepInventoryValuation", "RepSupplierItemActivity", "RepExpiryReport", "RepInventoryVariance",
            "RepClients", "RepClientBalances", "RepDebtAging", "RepClientProductSales",
            "RepSuppliers",
            "RepDrivers", "RepSalesByDriver", "RepHandovers",
            "RepFinancials", "RepDailyClosing", "RepIncomeStatement", "RepFinancialSummary", "RepShiftComparison",

            // Drivers & Administration
            "DriverHandover", "DriverPortal", "DriverSales", "ImportPreview", "DriversMonitor", "DriverCustody", "DriverLeaderboard",
            "Employees", "EmployeeTransactions",
            "Settings", "BotManager", "CloudSync", "LookupManager", "EditInvoiceDate",
            
            // Dashboards
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
        public bool CanChangeSafe { get; set; } = false;
        public bool CanViewSalesTotals { get; set; } = true;
        public bool CanViewQuickItems { get; set; } = true;
    }
}
