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

        public static int Save(int id, string name, string username, string password, string role, string phone, bool isDriver, bool isActive,
            int? defaultSafeID, string allowedSafeIDs, bool canSellCash, bool canSellCredit, bool canSellDriverLoad, bool canSellInstallment, bool canEditShippingCharge = true)
        {
            // التحقق من عدم تكرار اسم المستخدم
            if (!string.IsNullOrWhiteSpace(username))
            {
                object exists = DbHelper.Scalar(
                    "SELECT TOP 1 EmpID FROM Employees WHERE UserName = @u AND EmpID <> @id",
                    DbHelper.P("@u", username.Trim()),
                    DbHelper.P("@id", id));
                if (exists != null)
                {
                    throw new Exception("اسم المستخدم هذا مسجل بالفعل لموظف آخر. يرجى اختيار اسم مستخدم مختلف.");
                }
            }

            // تشفير كلمة المرور قبل الحفظ (فقط إذا أُدخلت كلمة مرور جديدة)
            string hashedPassword = string.IsNullOrWhiteSpace(password)
                ? null
                : PasswordHelper.Hash(password);

            if (id == 0)
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Employees(EmpName,UserName,Password,PlainPassword,Role,Phone,IsDriver,IsActive,DefaultSafeID,AllowedSafeIDs,CanSellCash,CanSellCredit,CanSellDriverLoad,CanSellInstallment,CanEditShippingCharge) " +
                    "VALUES(@n,@u,@p,@pp,@r,@ph,@dr,@a,@dsid,@asids,@csc,@ccr,@cdl,@cins,@cesc)",
                    DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", hashedPassword),
                    DbHelper.P("@pp", string.IsNullOrWhiteSpace(password) ? (object)DBNull.Value : (object)password),
                    DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive),
                    DbHelper.P("@dsid", defaultSafeID.HasValue ? (object)defaultSafeID.Value : (object)DBNull.Value),
                    DbHelper.P("@asids", string.IsNullOrEmpty(allowedSafeIDs) ? (object)DBNull.Value : (object)allowedSafeIDs),
                    DbHelper.P("@csc", canSellCash), DbHelper.P("@ccr", canSellCredit),
                    DbHelper.P("@cdl", canSellDriverLoad), DbHelper.P("@cins", canSellInstallment),
                    DbHelper.P("@cesc", canEditShippingCharge));
            else
            {
                var prmsList = new System.Collections.Generic.List<SqlParameter>
                {
                    DbHelper.P("@n", name),
                    DbHelper.P("@u", username),
                    DbHelper.P("@r", role),
                    DbHelper.P("@ph", phone),
                    DbHelper.P("@dr", isDriver),
                    DbHelper.P("@a", isActive),
                    DbHelper.P("@dsid", defaultSafeID.HasValue ? (object)defaultSafeID.Value : (object)DBNull.Value),
                    DbHelper.P("@asids", string.IsNullOrEmpty(allowedSafeIDs) ? (object)DBNull.Value : (object)allowedSafeIDs),
                    DbHelper.P("@csc", canSellCash),
                    DbHelper.P("@ccr", canSellCredit),
                    DbHelper.P("@cdl", canSellDriverLoad),
                    DbHelper.P("@cins", canSellInstallment),
                    DbHelper.P("@cesc", canEditShippingCharge),
                    DbHelper.P("@id", id)
                };

                string updateSql = "UPDATE Employees SET EmpName=@n,UserName=@u,Role=@r,Phone=@ph,IsDriver=@dr,IsActive=@a," +
                                   "DefaultSafeID=@dsid,AllowedSafeIDs=@asids,CanSellCash=@csc,CanSellCredit=@ccr,CanSellDriverLoad=@cdl,CanSellInstallment=@cins,CanEditShippingCharge=@cesc";

                if (hashedPassword != null)
                {
                    updateSql += ",Password=@p,PlainPassword=@pp";
                    prmsList.Add(DbHelper.P("@p", hashedPassword));
                    prmsList.Add(DbHelper.P("@pp", string.IsNullOrWhiteSpace(password) ? (object)DBNull.Value : (object)password));
                }

                updateSql += " WHERE EmpID=@id";

                DbHelper.Execute(updateSql, prmsList.ToArray());
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
            return DbHelper.Query("SELECT ScreenName, CanAccess, CanEditPrice, COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, COALESCE(CanViewCost, 0) AS CanViewCost FROM Permissions WHERE EmpID=@id", DbHelper.P("@id", empID));
        }

        public static void SavePermissions(int empID, string screen, bool canAccess, bool canEditPrice, bool canEditSalesInvoice, bool canDeleteSalesInvoice, bool canCopySalesInvoice, bool canViewCost)
        {
            var exists = DbHelper.Scalar("SELECT COUNT(*) FROM Permissions WHERE EmpID=@e AND ScreenName=@s",
                DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            if (Convert.ToInt32(exists) > 0)
                DbHelper.Execute("UPDATE Permissions SET CanAccess=@a,CanEditPrice=@ep,CanEditSalesInvoice=@cesi,CanDeleteSalesInvoice=@cdsi,CanCopySalesInvoice=@ccsi,CanViewCost=@cvc WHERE EmpID=@e AND ScreenName=@s",
                    DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice), DbHelper.P("@cesi", canEditSalesInvoice), DbHelper.P("@cdsi", canDeleteSalesInvoice), DbHelper.P("@ccsi", canCopySalesInvoice), DbHelper.P("@cvc", canViewCost), DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            else
                DbHelper.Execute("INSERT INTO Permissions(EmpID,ScreenName,CanAccess,CanEditPrice,CanEditSalesInvoice,CanDeleteSalesInvoice,CanCopySalesInvoice,CanViewCost) VALUES(@e,@s,@a,@ep,@cesi,@cdsi,@ccsi,@cvc)",
                    DbHelper.P("@e", empID), DbHelper.P("@s", screen), DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice), DbHelper.P("@cesi", canEditSalesInvoice), DbHelper.P("@cdsi", canDeleteSalesInvoice), DbHelper.P("@ccsi", canCopySalesInvoice), DbHelper.P("@cvc", canViewCost));
        }

        public static DataTable GetTransactions(int empID, DateTime from, DateTime to, string typeFilter)
        {
            string sql = @"
                SELECT et.TransID, et.TransDate, et.TransType, et.Debit, et.Credit, et.RefID, et.Notes,
                       creator.EmpName AS CreatedByName
                FROM EmployeeTransactions et
                LEFT JOIN Employees creator ON et.CreatedBy = creator.EmpID
                WHERE et.EmpID = @empID
                  AND CAST(et.TransDate AS DATE) BETWEEN @from AND @to";
            
            System.Collections.Generic.List<SqlParameter> pList = new System.Collections.Generic.List<SqlParameter> {
                DbHelper.P("@empID", empID),
                DbHelper.P("@from", from.Date),
                DbHelper.P("@to", to.Date)
            };

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
            {
                sql += " AND et.TransType = @type";
                pList.Add(DbHelper.P("@type", typeFilter));
            }

            sql += " ORDER BY et.TransDate DESC, et.TransID DESC";
            return DbHelper.Query(sql, pList.ToArray());
        }

        public static decimal GetBalance(int empID)
        {
            var result = DbHelper.Scalar(
                "SELECT Balance FROM vw_EmployeeBalance WHERE EmpID = @id",
                DbHelper.P("@id", empID));
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
        }

        public static int SaveTransaction(int empID, DateTime date, string transType, decimal debit, decimal credit, string notes, bool affectCash)
        {
            int transID = -1;
            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. Insert into EmployeeTransactions
                transID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO EmployeeTransactions(EmpID, TransDate, TransType, Debit, Credit, Notes, CreatedBy)
                      VALUES(@empID, @date, @type, @debit, @credit, @notes, @by)",
                    DbHelper.P("@empID", empID),
                    DbHelper.P("@date", date),
                    DbHelper.P("@type", transType),
                    DbHelper.P("@debit", debit),
                    DbHelper.P("@credit", credit),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@by", Session.EmpID));

                if (transID <= 0) throw new Exception("فشل في حفظ حركة الموظف.");

                // 2. If it affects cash, insert into CashBox
                if (affectCash)
                {
                    decimal amtIn = 0;
                    decimal amtOut = 0;
                    string cashType = "";

                    if (debit > 0)
                    {
                        amtOut = debit;
                        cashType = transType == "Advance" ? "EmpAdvance" : "EmpPaymentOut";
                    }
                    else if (credit > 0)
                    {
                        amtIn = credit;
                        cashType = "EmpPaymentIn";
                    }

                    if (amtIn > 0 || amtOut > 0)
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO CashBox(TransDate, TransType, AmountIn, AmountOut, RefID, Notes, CreatedBy)
                              VALUES(@date, @cashType, @amtIn, @amtOut, @ref, @notes, @by)",
                            DbHelper.P("@date", date),
                            DbHelper.P("@cashType", cashType),
                            DbHelper.P("@amtIn", amtIn),
                            DbHelper.P("@amtOut", amtOut),
                            DbHelper.P("@ref", transID),
                            DbHelper.P("@notes", notes),
                            DbHelper.P("@by", Session.EmpID));
                    }
                }
            });

            return transID;
        }

        public static void DeleteTransaction(int transID)
        {
            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. Delete from CashBox first
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM CashBox WHERE RefID=@id AND TransType IN ('EmpAdvance', 'EmpPaymentOut', 'EmpPaymentIn')",
                    DbHelper.P("@id", transID));

                // 2. Delete from EmployeeTransactions
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM EmployeeTransactions WHERE TransID=@id",
                    DbHelper.P("@id", transID));
            });
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
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.PendingQtyThreshold, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ShelfLocation, p.InternationalCode,
                           COALESCE(p.WholesalePrice, 0) AS WholesalePrice, COALESCE(p.SemiWholesalePrice, 0) AS SemiWholesalePrice, p.PrintLocalBarcode,
                           COALESCE(p.IsService, 0) AS IsService, COALESCE(p.IsQuickItem, 0) AS IsQuickItem,
                           p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                           p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                           p.Unit3Factor
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive=1 ORDER BY p.ProductName"
                : @"SELECT p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.Unit, p.SalePrice, p.PurchasePrice, 
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.PendingQtyThreshold, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ShelfLocation, p.IsActive, p.InternationalCode,
                           COALESCE(p.WholesalePrice, 0) AS WholesalePrice, COALESCE(p.SemiWholesalePrice, 0) AS SemiWholesalePrice, p.PrintLocalBarcode,
                           COALESCE(p.IsService, 0) AS IsService, COALESCE(p.IsQuickItem, 0) AS IsQuickItem,
                           p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                           p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                           p.Unit3Factor
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
            string partNumber, int? categoryID, string carModel, string brand, string shelfLocation, decimal wholesalePrice = 0, decimal semiWholesalePrice = 0, string internationalCode = null, bool printLocalBarcode = true, bool isService = false,
            string unit1Name = null, string unit1Barcode = null, decimal? unit1SalePrice = null, decimal? unit1PurchasePrice = null,
            string unit2Name = null, decimal? unit2Factor = null, string unit2Barcode = null, decimal? unit2SalePrice = null, decimal? unit2PurchasePrice = null,
            decimal? unit3Factor = null, bool isQuickItem = false)
        {
            if (id == 0)
                return DbHelper.ExecuteInsert(
                    @"INSERT INTO Products(ProductCode,ProductName,Unit,SalePrice,IsActive,PurchasePrice,MinStockLimit,Description,PartNumber,CategoryID,CarModel,Brand,ShelfLocation,WholesalePrice,SemiWholesalePrice,InternationalCode,PrintLocalBarcode,IsService,IsQuickItem,
                                           Unit1Name,Unit1Barcode,Unit1SalePrice,Unit1PurchasePrice,Unit2Name,Unit2Factor,Unit2Barcode,Unit2SalePrice,Unit2PurchasePrice,Unit3Factor) 
                      VALUES(@c,@n,@u,@p,@a,@pp,@msl,@d,@pn,@cat,@cm,@b,@sl,@wp,@swp,@ic,@plb,@srv,@qi,
                             @u1n,@u1b,@u1sp,@u1pp,@u2n,@u2f,@u2b,@u2sp,@u2pp,@u3f)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description),
                    DbHelper.P("@pn", partNumber), DbHelper.P("@cat", categoryID), DbHelper.P("@cm", carModel), DbHelper.P("@b", brand), DbHelper.P("@sl", shelfLocation),
                    DbHelper.P("@wp", wholesalePrice), DbHelper.P("@swp", semiWholesalePrice),
                    DbHelper.P("@ic", internationalCode),
                    DbHelper.P("@plb", printLocalBarcode),
                    DbHelper.P("@srv", isService),
                    DbHelper.P("@qi", isQuickItem),
                    DbHelper.P("@u1n", unit1Name), DbHelper.P("@u1b", unit1Barcode), DbHelper.P("@u1sp", unit1SalePrice ?? (object)DBNull.Value), DbHelper.P("@u1pp", unit1PurchasePrice ?? (object)DBNull.Value),
                    DbHelper.P("@u2n", unit2Name), DbHelper.P("@u2f", unit2Factor ?? (object)DBNull.Value), DbHelper.P("@u2b", unit2Barcode), DbHelper.P("@u2sp", unit2SalePrice ?? (object)DBNull.Value), DbHelper.P("@u2pp", unit2PurchasePrice ?? (object)DBNull.Value),
                    DbHelper.P("@u3f", unit3Factor ?? (object)DBNull.Value));
            else
            {
                decimal oldPrice = 0m;
                var oldPriceVal = DbHelper.Scalar("SELECT SalePrice FROM Products WHERE ProductID=@id", DbHelper.P("@id", id));
                if (oldPriceVal != null && oldPriceVal != DBNull.Value)
                {
                    oldPrice = Convert.ToDecimal(oldPriceVal);
                }

                DbHelper.Execute(
                    @"UPDATE Products 
                      SET ProductCode=@c,ProductName=@n,Unit=@u,SalePrice=@p,IsActive=@a,PurchasePrice=@pp,MinStockLimit=@msl,Description=@d,
                          PartNumber=@pn,CategoryID=@cat,CarModel=@cm,Brand=@b,ShelfLocation=@sl,WholesalePrice=@wp,SemiWholesalePrice=@swp,InternationalCode=@ic,PrintLocalBarcode=@plb,IsService=@srv,IsQuickItem=@qi,
                          Unit1Name=@u1n,Unit1Barcode=@u1b,Unit1SalePrice=@u1sp,Unit1PurchasePrice=@u1pp,
                          Unit2Name=@u2n,Unit2Factor=@u2f,Unit2Barcode=@u2b,Unit2SalePrice=@u2sp,Unit2PurchasePrice=@u2pp,
                          Unit3Factor=@u3f 
                      WHERE ProductID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description),
                    DbHelper.P("@pn", partNumber), DbHelper.P("@cat", categoryID), DbHelper.P("@cm", carModel), DbHelper.P("@b", brand), DbHelper.P("@sl", shelfLocation),
                    DbHelper.P("@wp", wholesalePrice), DbHelper.P("@swp", semiWholesalePrice),
                    DbHelper.P("@ic", internationalCode),
                    DbHelper.P("@plb", printLocalBarcode),
                    DbHelper.P("@srv", isService),
                    DbHelper.P("@qi", isQuickItem),
                    DbHelper.P("@u1n", unit1Name), DbHelper.P("@u1b", unit1Barcode), DbHelper.P("@u1sp", unit1SalePrice ?? (object)DBNull.Value), DbHelper.P("@u1pp", unit1PurchasePrice ?? (object)DBNull.Value),
                    DbHelper.P("@u2n", unit2Name), DbHelper.P("@u2f", unit2Factor ?? (object)DBNull.Value), DbHelper.P("@u2b", unit2Barcode), DbHelper.P("@u2sp", unit2SalePrice ?? (object)DBNull.Value), DbHelper.P("@u2pp", unit2PurchasePrice ?? (object)DBNull.Value),
                    DbHelper.P("@u3f", unit3Factor ?? (object)DBNull.Value),
                    DbHelper.P("@id", id));

                if (Math.Abs(price - oldPrice) > 0.005m)
                {
                    DbHelper.Execute(
                        @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                          VALUES (@pid, @old, @new, 'ProductCard', NULL, @uid, N'تعديل سعر البيع من كارت الصنف')",
                        DbHelper.P("@pid", id), DbHelper.P("@old", oldPrice), DbHelper.P("@new", price), DbHelper.P("@uid", Session.EmpID));
                }

                return id;
            }
        }

        public static DataTable GetQuickItems()
        {
            string sql = @"SELECT p.ProductID, p.ProductCode, p.ProductName, p.SalePrice, COALESCE(p.IsService, 0) AS IsService
                           FROM Products p
                           WHERE p.IsActive = 1 AND p.IsQuickItem = 1
                           ORDER BY p.ProductName";
            return DbHelper.Query(sql);
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
                         p.SalePrice, p.PurchasePrice, p.MinStockLimit, p.InternationalCode,
                         p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                         p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                         p.Unit3Factor,
                         CASE 
                             WHEN p.Unit1Barcode = @code OR ',' + p.Unit1Barcode + ',' LIKE '%,' + @code + ',%' THEN 1
                             WHEN p.Unit2Barcode = @code OR ',' + p.Unit2Barcode + ',' LIKE '%,' + @code + ',%' THEN 2
                             ELSE 3 -- الوحدة الكبرى (الافتراضية) للصنف المتطابق بالكود الصغير أو الدولي
                          END AS MatchedUnit
                  FROM Products p
                  WHERE p.IsActive = 1
                    AND (p.ProductCode = @code OR p.PartNumber = @code OR p.InternationalCode = @code OR ',' + p.InternationalCode + ',' LIKE '%,' + @code + ',%'
                         OR p.Unit1Barcode = @code OR ',' + p.Unit1Barcode + ',' LIKE '%,' + @code + ',%'
                         OR p.Unit2Barcode = @code OR ',' + p.Unit2Barcode + ',' LIKE '%,' + @code + ',%')",
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
        public static void SetPendingPrice(int productID, decimal pendingPrice, decimal costPrice, bool applyNow, int? purchaseID = null)
        {
            var val = DbHelper.Scalar("SELECT SalePrice FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
            decimal currentSalePrice = val != DBNull.Value && val != null ? Convert.ToDecimal(val) : 0m;

            if (applyNow)
            {
                // طبّق فوراً على الكل — امسح أي سعر معلق سابق
                DbHelper.Execute(
                    @"UPDATE Products
                      SET SalePrice            = @sp,
                          CostPrice            = @cp,
                          PurchasePrice        = @cp,
                          PendingSalePrice     = NULL,
                          PendingQtyThreshold  = NULL,
                          PendingPriceSourceRefID = NULL
                      WHERE ProductID = @id",
                    DbHelper.P("@sp", pendingPrice),
                    DbHelper.P("@cp", costPrice),
                    DbHelper.P("@id", productID));

                if (Math.Abs(pendingPrice - currentSalePrice) > 0.005m)
                {
                    string notes = purchaseID.HasValue ? "تحديث سعر بيع فوري من فاتورة الشراء #" + purchaseID.Value : "تحديث سعر بيع فوري";
                    DbHelper.Execute(
                        @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                          VALUES (@pid, @old, @new, 'PurchaseInvoice', @ref, @uid, @notes)",
                        DbHelper.P("@pid", productID), DbHelper.P("@old", currentSalePrice), DbHelper.P("@new", pendingPrice),
                        DbHelper.P("@ref", purchaseID.HasValue ? (object)purchaseID.Value : DBNull.Value), DbHelper.P("@uid", Session.EmpID), DbHelper.P("@notes", notes));
                }
            }
            else
            {
                // احسب المخزون الحالي (والذي تم تحديثه بعد حفظ المشتريات)
                decimal currentStock = InventoryDAL.GetProductStock(productID);
                decimal purchasedQty = 0m;

                if (purchaseID.HasValue)
                {
                    var pqtyVal = DbHelper.Scalar(
                        "SELECT Quantity FROM PurchaseItems WHERE PurchaseID = @ref AND ProductID = @pid",
                        DbHelper.P("@ref", purchaseID.Value),
                        DbHelper.P("@pid", productID));
                    if (pqtyVal != DBNull.Value && pqtyVal != null)
                    {
                        purchasedQty = Convert.ToDecimal(pqtyVal);
                    }
                }

                // المخزون القديم هو المخزون الحالي مطروحاً منه الكمية التي تم شراؤها في هذه الفاتورة
                decimal oldStock = Math.Max(0m, currentStock - purchasedQty);

                if (oldStock <= 0m)
                {
                    // إذا لم يكن هناك مخزون قديم، نطبق السعر الجديد فوراً للكل
                    SetPendingPrice(productID, pendingPrice, costPrice, applyNow: true, purchaseID: purchaseID);
                    return;
                }

                DbHelper.Execute(
                    @"UPDATE Products
                      SET CostPrice            = @cp,
                          PurchasePrice        = @cp,
                          PendingSalePrice     = @psp,
                          PendingQtyThreshold  = @pqt,
                          PendingPriceSourceRefID = @pref
                      WHERE ProductID = @id",
                    DbHelper.P("@cp", costPrice),
                    DbHelper.P("@psp", pendingPrice),
                    DbHelper.P("@pqt", oldStock),
                    DbHelper.P("@pref", purchaseID.HasValue ? (object)purchaseID.Value : DBNull.Value),
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
            var dt = DbHelper.Query("SELECT SalePrice, PendingSalePrice, PendingPriceSourceRefID FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
            if (dt.Rows.Count > 0 && dt.Rows[0]["PendingSalePrice"] != DBNull.Value)
            {
                decimal oldPrice = Convert.ToDecimal(dt.Rows[0]["SalePrice"]);
                decimal newPrice = Convert.ToDecimal(dt.Rows[0]["PendingSalePrice"]);
                object refIdVal = dt.Rows[0]["PendingPriceSourceRefID"];
                int? purchaseID = refIdVal != DBNull.Value ? (int?)Convert.ToInt32(refIdVal) : null;

                DbHelper.Execute(
                    @"UPDATE Products
                      SET SalePrice           = PendingSalePrice,
                          PendingSalePrice    = NULL,
                          PendingQtyThreshold = NULL,
                          PendingPriceSourceRefID = NULL
                      WHERE ProductID = @id AND PendingSalePrice IS NOT NULL",
                    DbHelper.P("@id", productID));

                string notes = purchaseID.HasValue ? "تفعيل سعر معلق تلقائي من فاتورة الشراء #" + purchaseID.Value : "تفعيل سعر معلق تلقائي";
                DbHelper.Execute(
                    @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                      VALUES (@pid, @old, @new, 'PurchaseInvoice', @ref, @uid, @notes)",
                    DbHelper.P("@pid", productID), DbHelper.P("@old", oldPrice), DbHelper.P("@new", newPrice),
                    DbHelper.P("@ref", purchaseID.HasValue ? (object)purchaseID.Value : DBNull.Value), DbHelper.P("@uid", Session.EmpID), DbHelper.P("@notes", notes));

                AppLogger.Audit("تفعيل سعر معلق", $"ProductID:{productID} Price:{newPrice}");
            }
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

        public static bool IsCodeExists(string code, int currentProductID)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var res = DbHelper.Scalar("SELECT COUNT(1) FROM Products WHERE ProductCode = @c AND ProductID != @id", 
                DbHelper.P("@c", code.Trim()), DbHelper.P("@id", currentProductID));
            return Convert.ToInt32(res) > 0;
        }

        public static bool IsPartNumberExists(string partNumber, int currentProductID)
        {
            if (string.IsNullOrWhiteSpace(partNumber)) return false;
            var res = DbHelper.Scalar("SELECT COUNT(1) FROM Products WHERE PartNumber = @pn AND ProductID != @id", 
                DbHelper.P("@pn", partNumber.Trim()), DbHelper.P("@id", currentProductID));
            return Convert.ToInt32(res) > 0;
        }

        public static bool IsNameExists(string name, int currentProductID)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var res = DbHelper.Scalar("SELECT COUNT(1) FROM Products WHERE ProductName = @n AND ProductID != @id", 
                DbHelper.P("@n", name.Trim()), DbHelper.P("@id", currentProductID));
            return Convert.ToInt32(res) > 0;
        }

        public static string GetOwnerOfInternationalBarcode(string barcode, int currentProductID)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            var res = DbHelper.Scalar(
                @"SELECT TOP 1 ProductCode FROM Products 
                  WHERE ProductID != @id AND (
                      ProductCode = @bc OR 
                      InternationalCode = @bc OR 
                      ',' + InternationalCode + ',' LIKE '%,' + @bc + ',%' OR
                      Unit1Barcode = @bc OR
                      ',' + Unit1Barcode + ',' LIKE '%,' + @bc + ',%' OR
                      Unit2Barcode = @bc OR
                      ',' + Unit2Barcode + ',' LIKE '%,' + @bc + ',%'
                  )", 
                DbHelper.P("@bc", barcode.Trim()), DbHelper.P("@id", currentProductID));
            return res != null && res != DBNull.Value ? res.ToString() : null;
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


        // ─── فحص التكرار ───
        /// <summary>هل يوجد مورد آخر بنفس الاسم (لمنع التكرار)</summary>
        public static bool IsDuplicateName(string name, int currentID = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var res = DbHelper.Scalar(
                "SELECT COUNT(1) FROM Suppliers WHERE SupplierName = @n AND SupplierID != @id",
                DbHelper.P("@n", name.Trim()), DbHelper.P("@id", currentID));
            return Convert.ToInt32(res) > 0;
        }

        /// <summary>هل يوجد مورد آخر بنفس رقم الهاتف (لمنع التكرار)</summary>
        public static bool IsDuplicatePhone(string phone, int currentID = 0)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var res = DbHelper.Scalar(
                "SELECT COUNT(1) FROM Suppliers WHERE Phone = @ph AND SupplierID != @id",
                DbHelper.P("@ph", phone.Trim()), DbHelper.P("@id", currentID));
            return Convert.ToInt32(res) > 0;
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

        public static void AddAdjustment(int supplierID, decimal amount, bool isDiscount, string notes)
        {
            decimal debit = isDiscount ? amount : 0m;
            decimal credit = isDiscount ? 0m : amount;
            string transType = isDiscount ? "Discount" : "Addition";

            DbHelper.Execute(
                "INSERT INTO SupplierTransactions(SupplierID,TransType,Debit,Credit,Notes,CreatedBy) VALUES(@id,@type,@debit,@credit,@notes,@by)",
                DbHelper.P("@id", supplierID),
                DbHelper.P("@type", transType),
                DbHelper.P("@debit", debit),
                DbHelper.P("@credit", credit),
                DbHelper.P("@notes", notes),
                DbHelper.P("@by", Session.EmpID));
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
                           COALESCE(c.DefaultPriceTier, N'قطاعي') AS DefaultPriceTier,
                           ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                           COALESCE(c.OpeningCrates, 0) AS OpeningCrates,
                           ISNULL(ccb.CratesBalance, COALESCE(c.OpeningCrates, 0)) AS CratesBalance
                           FROM Clients c
                           LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                           LEFT JOIN vw_ClientCratesBalance ccb ON c.ClientID = ccb.ClientID
                           " + (activeOnly ? "WHERE c.IsActive=1" : "") + " ORDER BY c.ClientName";
            return DbHelper.Query(sql);
        }

        public static DataTable Search(string term)
        {
            return DbHelper.Query(
                @"SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.Address, c.DriverID, c.MaxCreditLimit, c.Notes,
                  COALESCE(c.DefaultPriceTier, N'قطاعي') AS DefaultPriceTier,
                  ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                  COALESCE(c.OpeningCrates, 0) AS OpeningCrates,
                  ISNULL(ccb.CratesBalance, COALESCE(c.OpeningCrates, 0)) AS CratesBalance
                  FROM Clients c 
                  LEFT JOIN vw_ClientBalance cb ON c.ClientID=cb.ClientID
                  LEFT JOIN vw_ClientCratesBalance ccb ON c.ClientID=ccb.ClientID
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

        public static int GetClientCratesBalance(int clientID)
        {
            var dt = DbHelper.Query(@"
                SELECT ISNULL(ccb.CratesBalance, c.OpeningCrates) AS CratesBalance
                FROM Clients c
                LEFT JOIN vw_ClientCratesBalance ccb ON c.ClientID = ccb.ClientID
                WHERE c.ClientID = @id", DbHelper.P("@id", clientID));
            return dt.Rows.Count > 0 && dt.Rows[0]["CratesBalance"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["CratesBalance"]) : 0;
        }

        public static DataTable GetCratesStatement(int clientID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT cct.TransDate, 
                         CASE WHEN cct.RefSaleID IS NOT NULL THEN N'فاتورة بيع #' + CAST(cct.RefSaleID AS NVARCHAR(20)) ELSE N'حركة أقفاص يدوي' END AS TransType,
                         cct.CratesOut, cct.CratesIn, cct.Notes, cct.RefSaleID,
                         ISNULL(e.EmpName, N'---') AS CreatedByName
                  FROM ClientCratesTransactions cct
                  LEFT JOIN Employees e ON cct.CreatedBy = e.EmpID
                  WHERE cct.ClientID=@id AND CAST(cct.TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY cct.TransDate",
                DbHelper.P("@id", clientID), DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static int GetPreviousCratesBalance(int clientID, DateTime beforeDate)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    c.OpeningCrates + 
                    ISNULL((SELECT SUM(CratesOut) - SUM(CratesIn) FROM ClientCratesTransactions WHERE ClientID=@id AND CAST(TransDate AS DATE) < @dt), 0) AS PrevBal
                FROM Clients c WHERE c.ClientID=@id", 
                DbHelper.P("@id", clientID), DbHelper.P("@dt", beforeDate.Date));
            if (dt.Rows.Count > 0 && dt.Rows[0]["PrevBal"] != DBNull.Value)
                return Convert.ToInt32(dt.Rows[0]["PrevBal"]);
            return 0;
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


        // ─── فحص التكرار ───
        /// <summary>هل يوجد عميل آخر بنفس الاسم (لمنع التكرار)</summary>
        public static bool IsDuplicateName(string name, int currentID = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var res = DbHelper.Scalar(
                "SELECT COUNT(1) FROM Clients WHERE ClientName = @n AND ClientID != @id",
                DbHelper.P("@n", name.Trim()), DbHelper.P("@id", currentID));
            return Convert.ToInt32(res) > 0;
        }

        /// <summary>هل يوجد عميل آخر بنفس رقم الهاتف (لمنع التكرار)</summary>
        public static bool IsDuplicatePhone(string phone, int currentID = 0)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var res = DbHelper.Scalar(
                "SELECT COUNT(1) FROM Clients WHERE (Phone = @ph OR Phone2 = @ph) AND ClientID != @id",
                DbHelper.P("@ph", phone.Trim()), DbHelper.P("@id", currentID));
            return Convert.ToInt32(res) > 0;
        }

        public static string GetNextClientCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ClientID), 0) + 1 FROM Clients");
            return result != null ? result.ToString() : "1";
        }

        public static int Save(int id, string code, string name, string phone, string phone2, string address, decimal opening, bool active, int? driverID, decimal maxCreditLimit, string notes, string defaultPriceTier = "قطاعي", int openingCrates = 0)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Clients(ClientCode,ClientName,Phone,Phone2,Address,OpeningBalance,IsActive,DriverID,MaxCreditLimit,Notes,DefaultPriceTier,OpeningCrates) VALUES(@c,@n,@ph,@ph2,@a,@ob,@act,@dr,@mcl,@notes,@dpt,@oc)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes), DbHelper.P("@dpt", defaultPriceTier),
                    DbHelper.P("@oc", openingCrates));
                return newID;
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Clients SET ClientCode=@c,ClientName=@n,Phone=@ph,Phone2=@ph2,Address=@a,IsActive=@act,DriverID=@dr,MaxCreditLimit=@mcl,Notes=@notes,DefaultPriceTier=@dpt,OpeningCrates=@oc WHERE ClientID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes), DbHelper.P("@dpt", defaultPriceTier),
                    DbHelper.P("@oc", openingCrates),
                    DbHelper.P("@id", id));
                return id;
            }
        }

        public static DataTable GetStatement(int clientID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT ct.TransDate, ct.TransType, ct.Debit, ct.Credit, ct.Notes, ct.RefID,
                         ISNULL(e.EmpName, N'---') AS CreatedByName
                  FROM ClientTransactions ct
                  LEFT JOIN Employees e ON ct.CreatedBy = e.EmpID
                  WHERE ct.ClientID=@id AND CAST(ct.TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY ct.TransDate",
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

        public static decimal GetPreviousBalanceBeforeSale(int clientID, int saleID)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    c.OpeningBalance + 
                    ISNULL((
                        SELECT SUM(ct.Debit) - SUM(ct.Credit) 
                        FROM ClientTransactions ct
                        WHERE ct.ClientID = @cid 
                          AND ct.TransID < ISNULL((SELECT TOP 1 t.TransID FROM ClientTransactions t WHERE t.ClientID = @cid AND t.TransType = 'Sale' AND t.RefID = @sid ORDER BY t.TransID DESC), 999999999)
                    ), 0) AS PrevBal
                FROM Clients c WHERE c.ClientID = @cid", 
                DbHelper.P("@cid", clientID), DbHelper.P("@sid", saleID));
            if (dt.Rows.Count > 0 && dt.Rows[0]["PrevBal"] != DBNull.Value)
                return Convert.ToDecimal(dt.Rows[0]["PrevBal"]);
            return 0;
        }

        public static void AddPayment(int clientID, decimal amount, string notes, int? safeAccountID = null)
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
                    "INSERT INTO CashBox(TransType,AmountIn,Notes,CreatedBy,AccountID) VALUES('ClientPayment',@amt,@n,@by,@accId)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", "تحصيل من عميل - " + notes),
                    DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@accId", safeAccountID.HasValue ? (object)safeAccountID.Value : DBNull.Value));
            });
        }

        public static void AddAdjustment(int clientID, decimal amount, bool isDiscount, string notes)
        {
            decimal debit = isDiscount ? 0m : amount;
            decimal credit = isDiscount ? amount : 0m;
            string transType = isDiscount ? "Discount" : "Addition";

            DbHelper.Execute(
                "INSERT INTO ClientTransactions(ClientID,TransType,Debit,Credit,Notes,CreatedBy) VALUES(@id,@type,@debit,@credit,@notes,@by)",
                DbHelper.P("@id", clientID),
                DbHelper.P("@type", transType),
                DbHelper.P("@debit", debit),
                DbHelper.P("@credit", credit),
                DbHelper.P("@notes", notes),
                DbHelper.P("@by", Session.EmpID));
        }
    }

    // =================== Account DAL ===================
    public static class AccountDAL
    {
        public static DataTable GetActiveSafeAccounts()
        {
            return DbHelper.Query("SELECT AccountID, AccountName, AccountType, AccountNumber, OpeningBalance FROM SafeAccounts WHERE IsActive = 1 ORDER BY AccountID");
        }

        public static DataTable GetCashBox(DateTime from, DateTime to, int? accountID = null)
        {
            string sql = @"SELECT cb.CashID, cb.TransDate, cb.TransType, cb.AmountIn, cb.AmountOut,
                          (cb.AmountIn - cb.AmountOut) AS Net, cb.Notes, sa.AccountName
                          FROM CashBox cb
                          LEFT JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                          WHERE CAST(cb.TransDate AS DATE) BETWEEN @f AND @t";
            
            if (accountID.HasValue && accountID.Value > 0)
            {
                sql += " AND cb.AccountID = @accId ORDER BY cb.TransDate";
                return DbHelper.Query(sql,
                    DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                    DbHelper.P("@accId", accountID.Value));
            }
            
            sql += " ORDER BY cb.TransDate";
            return DbHelper.Query(sql, DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static decimal GetCashBalance(int? accountID = null)
        {
            if (accountID.HasValue && accountID.Value > 0)
            {
                var openingObj = DbHelper.Scalar("SELECT OpeningBalance FROM SafeAccounts WHERE AccountID = @accId", DbHelper.P("@accId", accountID.Value));
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                var result = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox WHERE AccountID = @accId", DbHelper.P("@accId", accountID.Value));
                return opening + (result == null ? 0 : Convert.ToDecimal(result));
            }
            else
            {
                var openingObj = DbHelper.Scalar("SELECT SUM(OpeningBalance) FROM SafeAccounts");
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                var result = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox");
                return opening + (result == null ? 0 : Convert.ToDecimal(result));
            }
        }

        public static void SaveCashReceipt(int? clientID, decimal amount, DateTime date, string notes, int? safeAccountID = null)
        {
            if (clientID.HasValue)
            {
                ClientDAL.AddPayment(clientID.Value, amount, notes, safeAccountID);
            }
            else
            {
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransDate,TransType,AmountIn,Notes,CreatedBy,AccountID) VALUES(@d,'Deposit',@a,@n,@by,@accId)",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "توريد نقدية - " + notes), DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@accId", safeAccountID.HasValue ? (object)safeAccountID.Value : DBNull.Value));
            }
        }

        public static DataTable GetExpenses(DateTime from, DateTime to, int? vehicleID = null, string vehicleType = null)
        {
            var sql = @"SELECT e.ExpenseID, e.ExpenseDate, e.ExpenseType, e.Amount, e.Notes,
                  e.SupplierID, s.SupplierName,
                  e.VehicleID, v.VehicleName, v.VehicleType,
                  e.SafeAccountID
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
                sql += " AND v.VehicleType = @vtype ORDER BY e.ExpenseDate";
                return DbHelper.Query(sql,
                    DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                    DbHelper.P("@vtype", vehicleType));
            }

            sql += " ORDER BY e.ExpenseDate";
            return DbHelper.Query(sql, DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static int SaveExpense(int id, DateTime date, string type, decimal amount, string notes, int? supplierID = null, int? vehicleID = null, int? safeAccountID = null)
        {
            if (id == 0)
            {
                int newID = -1;
                DbHelper.RunInTransaction((con, trans) =>
                {
                    var cashResult = DbHelper.ScalarTrans(trans, 
                        "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox WHERE AccountID = @accId",
                        DbHelper.P("@accId", safeAccountID ?? 1));
                    decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                    if (cashBalance < amount)
                    {
                        throw new Exception($"رصيد الحساب المختار ({cashBalance:N2} ج) لا يكفي لتسجيل هذا المصروف بقيمة ({amount:N2} ج)!");
                    }

                    newID = DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO Expenses(ExpenseDate,ExpenseType,Amount,Notes,SupplierID,VehicleID,CreatedBy,SafeAccountID) VALUES(@d,@t,@a,@n,@s,@v,@by,@accId)",
                        DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                        DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@accId", safeAccountID ?? 1));
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy,AccountID) VALUES(@d,'Expense',@a,@ref,@n,@by,@accId)",
                        DbHelper.P("@d", date), DbHelper.P("@a", amount), DbHelper.P("@ref", newID),
                        DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@accId", safeAccountID ?? 1));
                });
                return newID;
            }

            DbHelper.RunInTransaction((con, trans) =>
            {
                var oldAmountObj = DbHelper.ScalarTrans(trans, "SELECT Amount FROM Expenses WHERE ExpenseID=@id", DbHelper.P("@id", id));
                decimal oldAmount = oldAmountObj != null ? Convert.ToDecimal(oldAmountObj) : 0;

                var cashResult = DbHelper.ScalarTrans(trans, 
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox WHERE AccountID = @accId",
                    DbHelper.P("@accId", safeAccountID ?? 1));
                decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;

                decimal diff = amount - oldAmount;
                if (diff > 0 && cashBalance < diff)
                {
                    throw new Exception($"رصيد الحساب المختار ({cashBalance:N2} ج) لا يكفي لتعديل قيمة المصروف بزيادة قدرها ({diff:N2} ج)!");
                }

                DbHelper.ExecuteTrans(trans,
                    "UPDATE Expenses SET ExpenseDate=@d,ExpenseType=@t,Amount=@a,Notes=@n,SupplierID=@s,VehicleID=@v,SafeAccountID=@accId WHERE ExpenseID=@id",
                    DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                    DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@accId", safeAccountID ?? 1), DbHelper.P("@id", id));

                DbHelper.ExecuteTrans(trans,
                    "UPDATE CashBox SET TransDate=@d, AmountOut=@a, Notes=@n, AccountID=@accId WHERE RefID=@ref AND TransType='Expense'",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@accId", safeAccountID ?? 1), DbHelper.P("@ref", id));
            });
            return id;
        }

        public static void DeleteExpense(int id)
        {
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

        public static void TransferFunds(int sourceAccountID, int destAccountID, decimal amount, string notes)
        {
            if (sourceAccountID == destAccountID)
            {
                throw new Exception("لا يمكن تحويل نقدية إلى نفس الحساب المختار كأصل.");
            }

            DbHelper.RunInTransaction((con, trans) =>
            {
                // Check balance of source account
                decimal sourceBalance = GetCashBalance(sourceAccountID);
                if (sourceBalance < amount)
                {
                    throw new Exception($"رصيد الحساب المصدر ({sourceBalance:N2} ج) لا يكفي لإتمام عملية التحويل بقيمة ({amount:N2} ج)!");
                }
                
                string srcName = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", sourceAccountID))?.ToString();
                string destName = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", destAccountID))?.ToString();

                // Outflow from source
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType, AmountOut, Notes, CreatedBy, AccountID) VALUES('Transfer', @amt, @notes, @uid, @src)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@notes", $"تحويل صادر إلى {destName} | " + notes),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@src", sourceAccountID));
                
                // Inflow to destination
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType, AmountIn, Notes, CreatedBy, AccountID) VALUES('Transfer', @amt, @notes, @uid, @dest)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@notes", $"تحويل وارد من {srcName} | " + notes),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@dest", destAccountID));
            });
        }
    }
}

