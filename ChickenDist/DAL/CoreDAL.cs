using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    // =================== Employee DAL ===================
    public static class EmployeeDAL
    {
        public static DataTable GetAll()
        {
            return DbHelper.Query("SELECT EmpID,EmpName,UserName,Role,Phone,IsActive,IsDriver FROM Employees ORDER BY EmpName");
        }

        public static DataTable GetDrivers()
        {
            return DbHelper.Query("SELECT EmpID,EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 ORDER BY EmpName");
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Employees WHERE EmpID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static DataRow Login(string username, string password)
        {
            // نجلب الصف بالاسم فقط ثم نتحقق من كلمة المرور بـ PasswordHelper
            // (يدعم كلمات المرور القديمة plain-text والجديدة المُشفَّرة تلقائياً)
            var dt = DbHelper.Query(
                "SELECT * FROM Employees WHERE UserName=@u AND IsActive=1",
                DbHelper.P("@u", username));
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            string stored = row["Password"].ToString();

            if (!PasswordHelper.Verify(password, stored))
                return null;

            // ترقية تلقائية: إذا كانت كلمة المرور plain text نحوّلها لـ hash
            if (PasswordHelper.NeedsUpgrade(stored))
            {
                string hashed = PasswordHelper.Hash(password);
                DbHelper.Execute(
                    "UPDATE Employees SET Password=@p WHERE EmpID=@id",
                    DbHelper.P("@p", hashed),
                    DbHelper.P("@id", Convert.ToInt32(row["EmpID"])));
            }

            return row;
        }

        public static int Save(int id, string name, string username, string password, string role, string phone, bool isDriver, bool isActive)
        {
            // تشفير كلمة المرور قبل الحفظ (فقط إذا أُدخلت كلمة مرور جديدة)
            string hashedPassword = string.IsNullOrWhiteSpace(password)
                ? null
                : PasswordHelper.Hash(password);

            if (id == 0)
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Employees(EmpName,UserName,Password,Role,Phone,IsDriver,IsActive) VALUES(@n,@u,@p,@r,@ph,@dr,@a)",
                    DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", hashedPassword),
                    DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive));
            else
            {
                DbHelper.Execute(
                    "UPDATE Employees SET EmpName=@n,UserName=@u,Role=@r,Phone=@ph,IsDriver=@dr,IsActive=@a" +
                    (hashedPassword == null ? "" : ",Password=@p") + " WHERE EmpID=@id",
                    hashedPassword == null
                        ? new[] { DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive), DbHelper.P("@id", id) }
                        : new[] { DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", hashedPassword), DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive), DbHelper.P("@id", id) });
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Employees SET IsActive=0 WHERE EmpID=@id", DbHelper.P("@id", id));
        }

        // Permissions
        public static DataTable GetPermissions(int empID)
        {
            return DbHelper.Query("SELECT ScreenName,CanAccess,CanEditPrice FROM Permissions WHERE EmpID=@id", DbHelper.P("@id", empID));
        }

        public static void SavePermissions(int empID, string screen, bool canAccess, bool canEditPrice)
        {
            var exists = DbHelper.Scalar("SELECT COUNT(*) FROM Permissions WHERE EmpID=@e AND ScreenName=@s",
                DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            if (Convert.ToInt32(exists) > 0)
                DbHelper.Execute("UPDATE Permissions SET CanAccess=@a,CanEditPrice=@ep WHERE EmpID=@e AND ScreenName=@s",
                    DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice), DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            else
                DbHelper.Execute("INSERT INTO Permissions(EmpID,ScreenName,CanAccess,CanEditPrice) VALUES(@e,@s,@a,@ep)",
                    DbHelper.P("@e", empID), DbHelper.P("@s", screen), DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice));
        }
    }

    // =================== Product DAL ===================
    public static class ProductDAL
    {
        public static string GetNextProductCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ProductID), 0) + 1 FROM Products");
            return result != null ? result.ToString() : "1";
        }

        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = activeOnly
                ? @"SELECT p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ShelfLocation 
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive=1 ORDER BY p.ProductName"
                : @"SELECT p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ShelfLocation, p.IsActive 
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    ORDER BY p.ProductName";
            return DbHelper.Query(sql);
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query(
                @"SELECT p.*, c.CategoryName 
                  FROM Products p 
                  LEFT JOIN Categories c ON p.CategoryID = c.CategoryID 
                  WHERE p.ProductID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string code, string name, string unit, decimal price, bool active, decimal purchasePrice, decimal minStockLimit, string description,
            string partNumber, int? categoryID, string carModel, string brand, string shelfLocation)
        {
            if (id == 0)
                return DbHelper.ExecuteInsert(
                    @"INSERT INTO Products(ProductCode,ProductName,Unit,SalePrice,IsActive,PurchasePrice,MinStockLimit,Description,PartNumber,CategoryID,CarModel,Brand,ShelfLocation) 
                      VALUES(@c,@n,@u,@p,@a,@pp,@msl,@d,@pn,@cat,@cm,@b,@sl)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description),
                    DbHelper.P("@pn", partNumber), DbHelper.P("@cat", categoryID), DbHelper.P("@cm", carModel), DbHelper.P("@b", brand), DbHelper.P("@sl", shelfLocation));
            else
            {
                DbHelper.Execute(
                    @"UPDATE Products 
                      SET ProductCode=@c,ProductName=@n,Unit=@u,SalePrice=@p,IsActive=@a,PurchasePrice=@pp,MinStockLimit=@msl,Description=@d,
                          PartNumber=@pn,CategoryID=@cat,CarModel=@cm,Brand=@b,ShelfLocation=@sl 
                      WHERE ProductID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description),
                    DbHelper.P("@pn", partNumber), DbHelper.P("@cat", categoryID), DbHelper.P("@cm", carModel), DbHelper.P("@b", brand), DbHelper.P("@sl", shelfLocation),
                    DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Products SET IsActive=0 WHERE ProductID=@id", DbHelper.P("@id", id));
        }

        /// <summary>
        /// بحث عن صنف عن طريق الباركود أو كود الصنف أو رقم القطعة (PartNumber).
        /// يُستخدم للقراءة السريعة بجهاز السكنر.
        /// </summary>
        public static DataTable FindByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return new DataTable();
            return DbHelper.Query(
                @"SELECT TOP 1 p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.Unit, 
                         p.SalePrice, p.PurchasePrice, p.MinStockLimit
                  FROM Products p
                  WHERE p.IsActive = 1
                    AND (p.ProductCode = @code OR p.PartNumber = @code)",
                DbHelper.P("@code", code));
        }

        /// <summary>
        /// تسجيل سعر بيع مقترح كـ"سعر معلق" بناءً على الكمية الحالية بالمخزون.
        /// يتفعّل تلقائياً عندما يصل المخزون إلى الحد المحدد أو أقل.
        /// </summary>
        /// <param name="productID">الصنف</param>
        /// <param name="pendingPrice">السعر الجديد المقترح</param>
        /// <param name="costPrice">تكلفة الشراء الجديدة</param>
        /// <param name="applyNow">true = طبّق فوراً | false = علّق حتى نفاد المخزون القديم</param>
        public static void SetPendingPrice(int productID, decimal pendingPrice, decimal costPrice, bool applyNow)
        {
            if (applyNow)
            {
                // طبّق فوراً على الكل — امسح أي سعر معلق سابق
                DbHelper.Execute(
                    @"UPDATE Products
                      SET SalePrice            = @sp,
                          CostPrice            = @cp,
                          PurchasePrice        = @cp,
                          PendingSalePrice     = NULL,
                          PendingQtyThreshold  = NULL
                      WHERE ProductID = @id",
                    DbHelper.P("@sp", pendingPrice),
                    DbHelper.P("@cp", costPrice),
                    DbHelper.P("@id", productID));
            }
            else
            {
                // احسب المخزون الحالي كـ Threshold للتفعيل التلقائي لاحقاً
                decimal currentStock = InventoryDAL.GetProductStock(productID);

                DbHelper.Execute(
                    @"UPDATE Products
                      SET CostPrice            = @cp,
                          PurchasePrice        = @cp,
                          PendingSalePrice     = @psp,
                          PendingQtyThreshold  = @pqt
                      WHERE ProductID = @id",
                    DbHelper.P("@cp", costPrice),
                    DbHelper.P("@psp", pendingPrice),
                    DbHelper.P("@pqt", currentStock),
                    DbHelper.P("@id", productID));
            }

            AppLogger.Audit("تحديث سعر الصنف",
                $"ProductID:{productID} NewPrice:{pendingPrice:N3} Cost:{costPrice:N3} ApplyNow:{applyNow}");
        }

        /// <summary>
        /// تفعيل السعر المعلق يدوياً (زر "فعّل السعر الجديد" من شاشة الأصناف).
        /// </summary>
        public static void ActivatePendingPrice(int productID)
        {
            DbHelper.Execute(
                @"UPDATE Products
                  SET SalePrice           = PendingSalePrice,
                      PendingSalePrice    = NULL,
                      PendingQtyThreshold = NULL
                  WHERE ProductID = @id AND PendingSalePrice IS NOT NULL",
                DbHelper.P("@id", productID));

            AppLogger.Audit("تفعيل سعر معلق", $"ProductID:{productID}");
        }

        /// <summary>
        /// يُستدعى تلقائياً بعد كل عملية بيع للتحقق هل يجب تفعيل السعر المعلق.
        /// إذا أصبح المخزون ≤ PendingQtyThreshold → يُفعَّل السعر الجديد.
        /// </summary>
        public static void CheckAndActivatePendingPrice(int productID)
        {
            // جلب الصنف لمعرفة هل عنده سعر معلق
            var dt = DbHelper.Query(
                "SELECT PendingSalePrice, PendingQtyThreshold FROM Products WHERE ProductID=@id AND PendingSalePrice IS NOT NULL",
                DbHelper.P("@id", productID));

            if (dt.Rows.Count == 0) return; // لا يوجد سعر معلق

            decimal threshold = Convert.ToDecimal(dt.Rows[0]["PendingQtyThreshold"]);

            // احسب المخزون الحالي
            decimal currentStock = InventoryDAL.GetProductStock(productID);

            // إذا المخزون وصل للحد أو أقل → فعّل السعر الجديد
            if (currentStock <= 0 || currentStock <= threshold)
            {
                ActivatePendingPrice(productID);
                AppLogger.Audit("تفعيل سعر معلق تلقائي",
                    $"ProductID:{productID} Stock:{currentStock:N2} Threshold:{threshold:N2}");
            }
        }

        /// <summary>
        /// تقرير هامش الربح — مقارنة سعر البيع بالتكلفة لكل صنف.
        /// </summary>
        public static DataTable GetProfitMarginReport(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.CostPrice,
                    p.SalePrice,
                    CASE WHEN p.CostPrice > 0
                         THEN ROUND((p.SalePrice - p.CostPrice) / p.CostPrice * 100, 2)
                         ELSE 0 END                                     AS MarginPct,
                    p.SalePrice - p.CostPrice                           AS ProfitPerUnit,
                    ISNULL(SUM(si.Quantity), 0)                         AS TotalQtySold,
                    ISNULL(SUM(si.Quantity * p.CostPrice), 0)           AS TotalCost,
                    ISNULL(SUM(si.TotalPrice), 0)                       AS TotalRevenue,
                    ISNULL(SUM(si.TotalPrice), 0)
                        - ISNULL(SUM(si.Quantity * p.CostPrice), 0)     AS TotalProfit,
                    CASE WHEN p.PendingSalePrice IS NOT NULL
                         THEN p.PendingSalePrice ELSE NULL END           AS PendingSalePrice,
                    CASE WHEN p.PendingQtyThreshold IS NOT NULL
                         THEN p.PendingQtyThreshold ELSE NULL END        AS PendingQtyThreshold
                  FROM Products p
                  LEFT JOIN SaleItems si ON si.ProductID = p.ProductID
                  LEFT JOIN Sales s      ON si.SaleID = s.SaleID
                                       AND s.IsPosted = 1
                                       AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                                       AND s.SaleType IN ('Cash','Credit','DriverLoad')
                  WHERE p.IsActive = 1
                  GROUP BY p.ProductID, p.ProductCode, p.ProductName, p.Unit,
                           p.CostPrice, p.SalePrice, p.PendingSalePrice, p.PendingQtyThreshold
                  ORDER BY TotalProfit DESC",
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date));
        }
    }

    // =================== Supplier DAL ===================
    public static class SupplierDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = @"SELECT s.SupplierID, s.SupplierCode, s.SupplierName, s.Phone, s.Address,
                           s.OpeningBalance, s.IsActive, ISNULL(sb.Balance, s.OpeningBalance) AS Balance
                           FROM Suppliers s
                           LEFT JOIN vw_SupplierBalance sb ON s.SupplierID = sb.SupplierID
                           " + (activeOnly ? "WHERE s.IsActive=1" : "") + " ORDER BY s.SupplierName";
            return DbHelper.Query(sql);
        }

        public static string GetNextSupplierCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(SupplierID), 0) + 1 FROM Suppliers");
            return result != null ? result.ToString() : "1";
        }

        public static int Save(int id, string code, string name, string phone, string address, decimal opening, bool active)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Suppliers(SupplierCode,SupplierName,Phone,Address,OpeningBalance,IsActive) VALUES(@c,@n,@ph,@a,@ob,@act)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active));
                return newID;
            }
            else
            {
                // FIX: إضافة OpeningBalance للـ UPDATE — كان مفقوداً فيتجاهل تغيير الرصيد الافتتاحي
                DbHelper.Execute(
                    "UPDATE Suppliers SET SupplierCode=@c,SupplierName=@n,Phone=@ph,Address=@a,OpeningBalance=@ob,IsActive=@act WHERE SupplierID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Suppliers SET IsActive=0 WHERE SupplierID=@id", DbHelper.P("@id", id));
        }

        /// <summary>
        /// صرف نقدي للمورد - يسجل في حساب المورد (Debit يقلل المديونية) وفي الخزنة (AmountOut)
        /// بكود قيد تلقائي مثل SPY-0001
        /// </summary>
        public static string AddSupplierPayment(int supplierID, decimal amount, string notes)
        {
            string payCode = "";
            DbHelper.RunInTransaction((con, trans) =>
            {
                // توليد كود القيد التسلسلي SPY-XXXX
                var nextResult = DbHelper.ScalarTrans(trans,
                    "SELECT COALESCE(MAX(TransID), 0) + 1 FROM SupplierTransactions");
                int nextNum = nextResult != null ? Convert.ToInt32(nextResult) : 1;
                payCode = "SPY-" + nextNum.ToString("D4");

                // التحقق من رصيد الخزنة قبل الصرف
                var cashResult = DbHelper.ScalarTrans(trans,
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                if (cashBalance < amount)
                    throw new Exception(
                        $"رصيد الخزنة ({cashBalance:N2} ج) لا يكفي للصرف ({amount:N2} ج).\nيرجى تحصيل نقدية أولاً.");

                // تسجيل في حساب المورد: Debit يقلل المديونية (دفعنا له)
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO SupplierTransactions(SupplierID,TransType,Debit,Notes,CreatedBy) " +
                    "VALUES(@sid,'Payment',@amt,@n,@by)",
                    DbHelper.P("@sid", supplierID),
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", payCode + " - " + notes),
                    DbHelper.P("@by", Session.EmpID));

                // خصم من الخزنة
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType,AmountOut,Notes,CreatedBy) " +
                    "VALUES('SupplierPayment',@amt,@n,@by)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", payCode + " - صرف للمورد - " + notes),
                    DbHelper.P("@by", Session.EmpID));
            });
            return payCode;
        }

        /// <summary>كشف حساب المورد في فترة زمنية</summary>
        public static DataTable GetStatement(int supplierID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT TransDate, TransType, ISNULL(Debit,0) AS Debit, ISNULL(Credit,0) AS Credit,
                         ISNULL(RefID,0) AS RefID, Notes
                  FROM SupplierTransactions
                  WHERE SupplierID=@id AND CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@id", supplierID),
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date));
        }

        /// <summary>رصيد المورد قبل تاريخ معين (للرصيد الافتتاحي في الكشف)</summary>
        public static decimal GetPreviousBalance(int supplierID, DateTime before)
        {
            // الرصيد = الرصيد الافتتاحي + مجموع Credit (مشتريات) - مجموع Debit (مدفوعات)
            var openResult = DbHelper.Scalar(
                "SELECT ISNULL(OpeningBalance,0) FROM Suppliers WHERE SupplierID=@id",
                DbHelper.P("@id", supplierID));
            decimal opening = openResult != null ? Convert.ToDecimal(openResult) : 0;

            var txResult = DbHelper.Scalar(
                @"SELECT ISNULL(SUM(Credit),0) - ISNULL(SUM(Debit),0)
                  FROM SupplierTransactions
                  WHERE SupplierID=@id AND CAST(TransDate AS DATE) < @d",
                DbHelper.P("@id", supplierID),
                DbHelper.P("@d", before.Date));
            decimal txBalance = txResult != null ? Convert.ToDecimal(txResult) : 0;

            return opening + txBalance;
        }
    }

    public static class VehicleDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = @"SELECT VehicleID, VehicleType, VehicleName, LicensePlate, Notes, IsActive
                           FROM Vehicles" + (activeOnly ? " WHERE IsActive=1" : "") + " ORDER BY VehicleType, VehicleName";
            return DbHelper.Query(sql);
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Vehicles WHERE VehicleID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string type, string name, string licensePlate, string notes, bool active)
        {
            if (id == 0)
            {
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Vehicles(VehicleType,VehicleName,LicensePlate,Notes,IsActive) VALUES(@t,@n,@lp,@no,@act)",
                    DbHelper.P("@t", type), DbHelper.P("@n", name), DbHelper.P("@lp", licensePlate), DbHelper.P("@no", notes), DbHelper.P("@act", active));
            }
            DbHelper.Execute(
                "UPDATE Vehicles SET VehicleType=@t,VehicleName=@n,LicensePlate=@lp,Notes=@no,IsActive=@act WHERE VehicleID=@id",
                DbHelper.P("@t", type), DbHelper.P("@n", name), DbHelper.P("@lp", licensePlate), DbHelper.P("@no", notes), DbHelper.P("@act", active), DbHelper.P("@id", id));
            return id;
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Vehicles SET IsActive=0 WHERE VehicleID=@id", DbHelper.P("@id", id));
        }
    }

    // =================== Client DAL ===================
    public static class ClientDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = @"SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.Address,
                           c.OpeningBalance, c.IsActive, c.DriverID, c.MaxCreditLimit, c.Notes,
                           ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                           FROM Clients c
                           LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                           " + (activeOnly ? "WHERE c.IsActive=1" : "") + " ORDER BY c.ClientName";
            return DbHelper.Query(sql);
        }

        public static DataTable Search(string term)
        {
            return DbHelper.Query(
                @"SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.Address, c.DriverID, c.MaxCreditLimit, c.Notes,
                  ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                  FROM Clients c LEFT JOIN vw_ClientBalance cb ON c.ClientID=cb.ClientID
                  WHERE c.ClientName LIKE @t OR c.Phone LIKE @t OR c.ClientCode LIKE @t OR c.Phone2 LIKE @t",
                DbHelper.P("@t", "%" + term + "%"));
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Clients WHERE ClientID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static decimal GetClientBalance(int clientID)
        {
            var dt = DbHelper.Query(@"
                SELECT ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                WHERE c.ClientID = @id", DbHelper.P("@id", clientID));
            return dt.Rows.Count > 0 ? Convert.ToDecimal(dt.Rows[0]["Balance"]) : 0;
        }

        public class ClientFinancialStatus
        {
            public decimal Balance { get; set; }
            public decimal MaxCreditLimit { get; set; }
            public decimal OldDebt30 { get; set; }
        }

        public static ClientFinancialStatus GetFinancialStatus(int clientID)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                    ISNULL(c.OpeningBalance, 0) AS OpeningBalance,
                    ISNULL(c.MaxCreditLimit, 0) AS MaxCreditLimit,
                    ISNULL((SELECT SUM(Debit) FROM ClientTransactions WHERE ClientID=@id AND TransDate >= DATEADD(day, -30, GETDATE())), 0) AS RecentDebit
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                WHERE c.ClientID = @id", DbHelper.P("@id", clientID));

            if (dt.Rows.Count > 0)
            {
                decimal bal = Convert.ToDecimal(dt.Rows[0]["Balance"]);
                decimal openingBal = Convert.ToDecimal(dt.Rows[0]["OpeningBalance"]);
                decimal recentDebit = Convert.ToDecimal(dt.Rows[0]["RecentDebit"]);
                decimal transBalance = bal - openingBal;
                return new ClientFinancialStatus
                {
                    Balance = bal,
                    MaxCreditLimit = Convert.ToDecimal(dt.Rows[0]["MaxCreditLimit"]),
                    OldDebt30 = transBalance > recentDebit ? transBalance - recentDebit : 0
                };
            }
            return new ClientFinancialStatus();
        }

        public static string GetNextClientCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ClientID), 0) + 1 FROM Clients");
            return result != null ? result.ToString() : "1";
        }

        public static int Save(int id, string code, string name, string phone, string phone2, string address, decimal opening, bool active, int? driverID, decimal maxCreditLimit, string notes)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Clients(ClientCode,ClientName,Phone,Phone2,Address,OpeningBalance,IsActive,DriverID,MaxCreditLimit,Notes) VALUES(@c,@n,@ph,@ph2,@a,@ob,@act,@dr,@mcl,@notes)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes));
                return newID;
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Clients SET ClientCode=@c,ClientName=@n,Phone=@ph,Phone2=@ph2,Address=@a,IsActive=@act,DriverID=@dr,MaxCreditLimit=@mcl,Notes=@notes WHERE ClientID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes),
                    DbHelper.P("@id", id));
                return id;
            }
        }

        public static DataTable GetStatement(int clientID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT TransDate, TransType, Debit, Credit, Notes, RefID
                  FROM ClientTransactions
                  WHERE ClientID=@id AND CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@id", clientID), DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static decimal GetPreviousBalance(int clientID, DateTime beforeDate)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    c.OpeningBalance + 
                    ISNULL((SELECT SUM(Debit) - SUM(Credit) FROM ClientTransactions WHERE ClientID=@id AND CAST(TransDate AS DATE) < @dt), 0) AS PrevBal
                FROM Clients c WHERE c.ClientID=@id", 
                DbHelper.P("@id", clientID), DbHelper.P("@dt", beforeDate.Date));
            if (dt.Rows.Count > 0 && dt.Rows[0]["PrevBal"] != DBNull.Value)
                return Convert.ToDecimal(dt.Rows[0]["PrevBal"]);
            return 0;
        }

        public static void AddPayment(int clientID, decimal amount, string notes)
        {
            // FIX: القيدان (حساب العميل + الخزنة) داخل Transaction واحد
            // إذا فشل أي منهما يتم التراجع عن الآخر لمنع الفجوة المالية الصامتة
            DbHelper.RunInTransaction((con, trans) =>
            {
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO ClientTransactions(ClientID,TransType,Credit,Notes,CreatedBy) VALUES(@id,'Payment',@amt,@n,@by)",
                    DbHelper.P("@id", clientID), DbHelper.P("@amt", amount),
                    DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType,AmountIn,Notes,CreatedBy) VALUES('ClientPayment',@amt,@n,@by)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", "تحصيل من عميل - " + notes),
                    DbHelper.P("@by", Session.EmpID));
            });
        }
    }

    // =================== Account DAL ===================
    public static class AccountDAL
    {
        public static DataTable GetCashBox(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT CashID, TransDate, TransType, AmountIn, AmountOut,
                  (AmountIn - AmountOut) AS Net, Notes
                  FROM CashBox
                  WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static decimal GetCashBalance()
        {
            var result = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox");
            return result == null ? 0 : Convert.ToDecimal(result);
        }

        public static void SaveCashReceipt(int? clientID, decimal amount, DateTime date, string notes)
        {
            if (clientID.HasValue)
            {
                ClientDAL.AddPayment(clientID.Value, amount, notes);
            }
            else
            {
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransDate,TransType,AmountIn,Notes,CreatedBy) VALUES(@d,'Deposit',@a,@n,@by)",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "توريد نقدية - " + notes), DbHelper.P("@by", Session.EmpID));
            }
        }

        public static DataTable GetExpenses(DateTime from, DateTime to, int? vehicleID = null, string vehicleType = null)
        {
            var sql = @"SELECT e.ExpenseID, e.ExpenseDate, e.ExpenseType, e.Amount, e.Notes,
                  e.SupplierID, s.SupplierName,
                  e.VehicleID, v.VehicleName, v.VehicleType
                  FROM Expenses e
                  LEFT JOIN Suppliers s ON e.SupplierID = s.SupplierID
                  LEFT JOIN Vehicles v ON e.VehicleID = v.VehicleID
                  WHERE CAST(e.ExpenseDate AS DATE) BETWEEN @f AND @t";

            if (vehicleID.HasValue)
            {
                sql += " AND e.VehicleID = @vid ORDER BY e.ExpenseDate";
                return DbHelper.Query(sql,
                    DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                    DbHelper.P("@vid", vehicleID.Value));
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                // FIX: كان يُمرَّر @vtype حتى عندما لا يكون في الـ SQL → SqlException
                sql += " AND v.VehicleType = @vtype ORDER BY e.ExpenseDate";
                return DbHelper.Query(sql,
                    DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                    DbHelper.P("@vtype", vehicleType));
            }

            sql += " ORDER BY e.ExpenseDate";
            return DbHelper.Query(sql, DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static int SaveExpense(int id, DateTime date, string type, decimal amount, string notes, int? supplierID = null, int? vehicleID = null)
        {
            if (id == 0)
            {
                // إضافة جديدة: Expense + CashBox في Transaction واحد
                int newID = -1;
                DbHelper.RunInTransaction((con, trans) =>
                {
                    var cashResult = DbHelper.ScalarTrans(trans, 
                        "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                    decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                    if (cashBalance < amount)
                    {
                        throw new Exception($"رصيد الخزنة الحالي ({cashBalance:N2} ج) لا يكفي لتسجيل هذا المصروف بقيمة ({amount:N2} ج)!");
                    }

                    newID = DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO Expenses(ExpenseDate,ExpenseType,Amount,Notes,SupplierID,VehicleID,CreatedBy) VALUES(@d,@t,@a,@n,@s,@v,@by)",
                        DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                        DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@by", Session.EmpID));
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy) VALUES(@d,'Expense',@a,@ref,@n,@by)",
                        DbHelper.P("@d", date), DbHelper.P("@a", amount), DbHelper.P("@ref", newID),
                        DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@by", Session.EmpID));
                });
                return newID;
            }

            // FIX: تعديل — يُحدَّث Expenses وقيد الخزنة معاً في Transaction واحد ويتحقق من رصيد الخزنة
            DbHelper.RunInTransaction((con, trans) =>
            {
                var oldAmountObj = DbHelper.ScalarTrans(trans, "SELECT Amount FROM Expenses WHERE ExpenseID=@id", DbHelper.P("@id", id));
                decimal oldAmount = oldAmountObj != null ? Convert.ToDecimal(oldAmountObj) : 0;

                var cashResult = DbHelper.ScalarTrans(trans, 
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;

                decimal diff = amount - oldAmount;
                if (diff > 0 && cashBalance < diff)
                {
                    throw new Exception($"رصيد الخزنة الحالي ({cashBalance:N2} ج) لا يكفي لتعديل قيمة المصروف بزيادة قدرها ({diff:N2} ج)!");
                }

                DbHelper.ExecuteTrans(trans,
                    "UPDATE Expenses SET ExpenseDate=@d,ExpenseType=@t,Amount=@a,Notes=@n,SupplierID=@s,VehicleID=@v WHERE ExpenseID=@id",
                    DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                    DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@id", id));

                DbHelper.ExecuteTrans(trans,
                    "UPDATE CashBox SET TransDate=@d, AmountOut=@a, Notes=@n WHERE RefID=@ref AND TransType='Expense'",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@ref", id));
            });
            return id;
        }

        public static void DeleteExpense(int id)
        {
            // FIX: حذف Expense + قيد الخزنة معاً في Transaction
            // الكود القديم كان يحذف Expenses فقط ويبقي قيد الخزنة
            DbHelper.RunInTransaction((con, trans) =>
            {
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM CashBox WHERE RefID=@id AND TransType='Expense'",
                    DbHelper.P("@id", id));
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM Expenses WHERE ExpenseID=@id",
                    DbHelper.P("@id", id));
            });
        }
    }
}

