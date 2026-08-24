using System;
using System.Collections.Generic;
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
            int? defaultSafeID, string allowedSafeIDs, bool canSellCash, bool canSellCredit, bool canSellDriverLoad, bool canSellInstallment,
            bool canEditShippingCharge = true, bool canSelectDriver = true, bool canSellVisa = true,
            decimal salary = 0, decimal dailyWorkHours = 8, decimal hourlyRate = 0, decimal commissionRate = 0, decimal targetAmount = 0,
            string jobTitle = null, DateTime? hireDate = null, string nationalID = null)
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
                    "INSERT INTO Employees(EmpName,UserName,Password,Role,Phone,IsDriver,IsActive,DefaultSafeID,AllowedSafeIDs,CanSellCash,CanSellCredit,CanSellDriverLoad,CanSellInstallment,CanEditShippingCharge,CanSelectDriver,CanSellVisa,Salary,DailyWorkHours,HourlyRate,SalesCommissionRate,TargetAmount,JobTitle,HireDate,NationalID) " +
                    "VALUES(@n,@u,@p,@r,@ph,@dr,@a,@dsid,@asids,@csc,@ccr,@cdl,@cins,@cesc,@csd,@csv,@sal,@dwh,@hrate,@crate,@target,@jtitle,@hdate,@nid)",
                    DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", hashedPassword),
                    DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive),
                    DbHelper.P("@dsid", defaultSafeID.HasValue ? (object)defaultSafeID.Value : (object)DBNull.Value),
                    DbHelper.P("@asids", string.IsNullOrEmpty(allowedSafeIDs) ? (object)DBNull.Value : (object)allowedSafeIDs),
                    DbHelper.P("@csc", canSellCash), DbHelper.P("@ccr", canSellCredit),
                    DbHelper.P("@cdl", canSellDriverLoad), DbHelper.P("@cins", canSellInstallment),
                    DbHelper.P("@cesc", canEditShippingCharge), DbHelper.P("@csd", canSelectDriver),
                    DbHelper.P("@csv", canSellVisa),
                    DbHelper.P("@sal", salary),
                    DbHelper.P("@dwh", dailyWorkHours),
                    DbHelper.P("@hrate", hourlyRate),
                    DbHelper.P("@crate", commissionRate),
                    DbHelper.P("@target", targetAmount),
                    DbHelper.P("@jtitle", (object)jobTitle ?? DBNull.Value),
                    DbHelper.P("@hdate", hireDate.HasValue ? (object)hireDate.Value : DBNull.Value),
                    DbHelper.P("@nid", (object)nationalID ?? DBNull.Value));
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
                    DbHelper.P("@csd", canSelectDriver),
                    DbHelper.P("@csv", canSellVisa),
                    DbHelper.P("@sal", salary),
                    DbHelper.P("@dwh", dailyWorkHours),
                    DbHelper.P("@hrate", hourlyRate),
                    DbHelper.P("@crate", commissionRate),
                    DbHelper.P("@target", targetAmount),
                    DbHelper.P("@jtitle", (object)jobTitle ?? DBNull.Value),
                    DbHelper.P("@hdate", hireDate.HasValue ? (object)hireDate.Value : DBNull.Value),
                    DbHelper.P("@nid", (object)nationalID ?? DBNull.Value),
                    DbHelper.P("@id", id)
                };

                string updateSql = "UPDATE Employees SET EmpName=@n,UserName=@u,Role=@r,Phone=@ph,IsDriver=@dr,IsActive=@a," +
                                   "DefaultSafeID=@dsid,AllowedSafeIDs=@asids,CanSellCash=@csc,CanSellCredit=@ccr,CanSellDriverLoad=@cdl,CanSellInstallment=@cins,CanEditShippingCharge=@cesc,CanSelectDriver=@csd,CanSellVisa=@csv," +
                                   "Salary=@sal,DailyWorkHours=@dwh,HourlyRate=@hrate,SalesCommissionRate=@crate,TargetAmount=@target,JobTitle=@jtitle,HireDate=@hdate,NationalID=@nid";

                if (hashedPassword != null)
                {
                    updateSql += ",Password=@p";
                    prmsList.Add(DbHelper.P("@p", hashedPassword));
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
            DbHelper.EnsurePermissionsColumns();
            try
            {
                return DbHelper.Query(@"
                    SELECT ScreenName, CanAccess, 
                           COALESCE(CanAdd, 1) AS CanAdd, 
                           COALESCE(CanEdit, 1) AS CanEdit, 
                           COALESCE(CanDelete, 1) AS CanDelete, 
                           CanEditPrice, 
                           COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, 
                           COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, 
                           COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, 
                           COALESCE(CanViewCost, 0) AS CanViewCost, 
                           COALESCE(CanOrderColumns, 0) AS CanOrderColumns,
                           COALESCE(CanViewDetails, 1) AS CanViewDetails,
                           COALESCE(CanViewBalance, 1) AS CanViewBalance,
                           COALESCE(CanChangeSafe, 1) AS CanChangeSafe,
                           COALESCE(CanViewSalesTotals, 1) AS CanViewSalesTotals,
                           COALESCE(CanViewQuickItems, 1) AS CanViewQuickItems
                    FROM Permissions WHERE EmpID=@id", DbHelper.P("@id", empID));
            }
            catch
            {
                DbHelper.EnsurePermissionsColumns();
                return DbHelper.Query(@"
                    SELECT ScreenName, CanAccess, 
                           COALESCE(CanAdd, 1) AS CanAdd, 
                           COALESCE(CanEdit, 1) AS CanEdit, 
                           COALESCE(CanDelete, 1) AS CanDelete, 
                           CanEditPrice, 
                           COALESCE(CanEditSalesInvoice, 0) AS CanEditSalesInvoice, 
                           COALESCE(CanDeleteSalesInvoice, 0) AS CanDeleteSalesInvoice, 
                           COALESCE(CanCopySalesInvoice, 0) AS CanCopySalesInvoice, 
                           COALESCE(CanViewCost, 0) AS CanViewCost, 
                           COALESCE(CanOrderColumns, 0) AS CanOrderColumns,
                           COALESCE(CanViewDetails, 1) AS CanViewDetails,
                           COALESCE(CanViewBalance, 1) AS CanViewBalance,
                           COALESCE(CanChangeSafe, 1) AS CanChangeSafe,
                           COALESCE(CanViewSalesTotals, 1) AS CanViewSalesTotals,
                           COALESCE(CanViewQuickItems, 1) AS CanViewQuickItems
                    FROM Permissions WHERE EmpID=@id", DbHelper.P("@id", empID));
            }
        }

        public static void SavePermissions(int empID, string screen, 
            bool canAccess, bool canAdd, bool canEdit, bool canDelete,
            bool canEditPrice, bool canEditSalesInvoice, bool canDeleteSalesInvoice, 
            bool canCopySalesInvoice, bool canViewCost, bool canOrderColumns,
            bool canViewDetails, bool canViewBalance, bool canChangeSafe, bool canViewSalesTotals,
            bool canViewQuickItems = true)
        {
            DbHelper.EnsurePermissionsColumns();
            var exists = DbHelper.Scalar("SELECT COUNT(*) FROM Permissions WHERE EmpID=@e AND ScreenName=@s",
                DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            if (Convert.ToInt32(exists) > 0)
                DbHelper.Execute(@"
                    UPDATE Permissions SET 
                        CanAccess=@a, CanAdd=@add, CanEdit=@ed, CanDelete=@del,
                        CanEditPrice=@ep, CanEditSalesInvoice=@cesi, CanDeleteSalesInvoice=@cdsi, 
                        CanCopySalesInvoice=@ccsi, CanViewCost=@cvc, CanOrderColumns=@coc,
                        CanViewDetails=@cvd, CanViewBalance=@cvb, CanChangeSafe=@ccs,
                        CanViewSalesTotals=@cvst, CanViewQuickItems=@cvqi
                    WHERE EmpID=@e AND ScreenName=@s",
                    DbHelper.P("@a", canAccess), DbHelper.P("@add", canAdd), DbHelper.P("@ed", canEdit), DbHelper.P("@del", canDelete),
                    DbHelper.P("@ep", canEditPrice), DbHelper.P("@cesi", canEditSalesInvoice), DbHelper.P("@cdsi", canDeleteSalesInvoice), 
                    DbHelper.P("@ccsi", canCopySalesInvoice), DbHelper.P("@cvc", canViewCost), DbHelper.P("@coc", canOrderColumns),
                    DbHelper.P("@cvd", canViewDetails), DbHelper.P("@cvb", canViewBalance), DbHelper.P("@ccs", canChangeSafe),
                    DbHelper.P("@cvst", canViewSalesTotals), DbHelper.P("@cvqi", canViewQuickItems),
                    DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            else
                DbHelper.Execute(@"
                    INSERT INTO Permissions(EmpID, ScreenName, CanAccess, CanAdd, CanEdit, CanDelete, CanEditPrice, CanEditSalesInvoice, CanDeleteSalesInvoice, CanCopySalesInvoice, CanViewCost, CanOrderColumns, CanViewDetails, CanViewBalance, CanChangeSafe, CanViewSalesTotals, CanViewQuickItems) 
                    VALUES(@e, @s, @a, @add, @ed, @del, @ep, @cesi, @cdsi, @ccsi, @cvc, @coc, @cvd, @cvb, @ccs, @cvst, @cvqi)",
                    DbHelper.P("@e", empID), DbHelper.P("@s", screen), DbHelper.P("@a", canAccess), DbHelper.P("@add", canAdd), DbHelper.P("@ed", canEdit), DbHelper.P("@del", canDelete),
                    DbHelper.P("@ep", canEditPrice), DbHelper.P("@cesi", canEditSalesInvoice), DbHelper.P("@cdsi", canDeleteSalesInvoice), 
                    DbHelper.P("@ccsi", canCopySalesInvoice), DbHelper.P("@cvc", canViewCost), DbHelper.P("@coc", canOrderColumns),
                    DbHelper.P("@cvd", canViewDetails), DbHelper.P("@cvb", canViewBalance), DbHelper.P("@ccs", canChangeSafe),
                    DbHelper.P("@cvst", canViewSalesTotals), DbHelper.P("@cvqi", canViewQuickItems));
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

                    if (amtOut > 0)
                    {
                        int accId = Session.GetDefaultSafeID();
                        AccountDAL.EnsureSufficientCashTrans(trans, accId, amtOut, transType == "Advance" ? "صرف سلفة للموظف" : "صرف راتب للموظف");
                    }

                    if (amtIn > 0 || amtOut > 0)
                    {
                        int accId = Session.GetDefaultSafeID();
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO CashBox(TransDate, TransType, AmountIn, AmountOut, RefID, Notes, CreatedBy, AccountID)
                              VALUES(@date, @cashType, @amtIn, @amtOut, @ref, @notes, @by, @accId)",
                            DbHelper.P("@date", date),
                            DbHelper.P("@cashType", cashType),
                            DbHelper.P("@amtIn", amtIn),
                            DbHelper.P("@amtOut", amtOut),
                            DbHelper.P("@ref", transID),
                            DbHelper.P("@notes", notes),
                            DbHelper.P("@by", Session.EmpID),
                            DbHelper.P("@accId", accId));
                    }
                }
            });

            return transID;
        }

        public static bool DeleteTransaction(int transID)
        {
            bool success = false;
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

                success = true;
            });
            return success;
        }
    }

    // =================== Product DAL ===================
    public static class ProductDAL
    {
        public static string GetNextProductCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ProductID), 0) FROM Products");
            int maxId = (result != DBNull.Value && result != null) ? Convert.ToInt32(result) : 0;

            try
            {
                var objMaxCode = DbHelper.Scalar("SELECT MAX(CAST(ProductCode AS INT)) FROM Products WHERE ISNUMERIC(ProductCode) = 1");
                if (objMaxCode != DBNull.Value && objMaxCode != null && int.TryParse(objMaxCode.ToString(), out int maxCodeNum))
                {
                    if (maxCodeNum > maxId) maxId = maxCodeNum;
                }
            }
            catch { }

            int nextId = maxId < 1000 ? 1001 : maxId + 1;
            return nextId.ToString("D8");
        }

        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = activeOnly
                ? @"SELECT p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.EnglishName, p.Unit, p.SalePrice, p.PurchasePrice, 
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.PendingQtyThreshold, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ProducerCompany, p.ShelfLocation, p.InternationalCode, p.ProductSize, p.Color,
                           COALESCE(p.WholesalePrice, 0) AS WholesalePrice, COALESCE(p.SemiWholesalePrice, 0) AS SemiWholesalePrice, p.PrintLocalBarcode,
                           COALESCE(p.IsService, 0) AS IsService, COALESCE(p.IsQuickItem, 0) AS IsQuickItem,
                           p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                           p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                           p.Unit3Factor, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays, p.DefaultSaleUnit
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive=1 ORDER BY p.ProductName"
                : @"SELECT p.ProductID, p.ProductCode, p.PartNumber, p.ProductName, p.EnglishName, p.Unit, p.SalePrice, p.PurchasePrice, 
                           p.MinStockLimit, p.Description, p.PendingSalePrice, p.PendingQtyThreshold, p.CategoryID, c.CategoryName, p.CarModel, p.Brand, p.ProducerCompany, p.ShelfLocation, p.IsActive, p.InternationalCode, p.ProductSize, p.Color,
                           COALESCE(p.WholesalePrice, 0) AS WholesalePrice, COALESCE(p.SemiWholesalePrice, 0) AS SemiWholesalePrice, p.PrintLocalBarcode,
                           COALESCE(p.IsService, 0) AS IsService, COALESCE(p.IsQuickItem, 0) AS IsQuickItem,
                           p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                           p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                           p.Unit3Factor, COALESCE(p.HasExpiry, 0) AS HasExpiry, p.DefaultExpiryDays, p.DefaultSaleUnit
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

        public static DataRow GetByBarcodeOrScaleCode(string scannedCode, out decimal parsedWeight)
        {
            parsedWeight = 1m;
            if (string.IsNullOrWhiteSpace(scannedCode)) return null;

            scannedCode = scannedCode.Trim();
            int.TryParse(scannedCode, out int scannedInt);
            string scannedPadded = scannedInt > 0 ? scannedInt.ToString("D8") : scannedCode;
            string scannedTrimmed = scannedCode.TrimStart('0');
            if (string.IsNullOrEmpty(scannedTrimmed)) scannedTrimmed = "0";

            // 1. FIRST Priority: Direct Exact Full Barcode / Code / PartNumber Lookup
            // If the scanned barcode matches ProductCode, InternationalCode, Unit1Barcode, Unit2Barcode, PartNumber, or ScalePLU directly
            var dtDirect = DbHelper.Query(@"
                SELECT TOP 1 p.*, c.CategoryName 
                FROM Products p 
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID 
                WHERE p.IsActive = 1 AND (
                    p.ProductCode = @code OR p.ProductCode = @scannedPadded OR p.ProductCode = @scannedTrimmed OR
                    p.InternationalCode = @code OR ',' + p.InternationalCode + ',' LIKE '%,' + @code + ',%' OR
                    p.Unit1Barcode = @code OR ',' + p.Unit1Barcode + ',' LIKE '%,' + @code + ',%' OR
                    p.Unit2Barcode = @code OR ',' + p.Unit2Barcode + ',' LIKE '%,' + @code + ',%' OR
                    p.PartNumber = @code OR
                    p.ScalePLU = @code OR p.ScalePLU = @scannedPadded OR p.ScalePLU = @scannedTrimmed OR
                    (@scannedInt > 0 AND p.ProductID = @scannedInt) OR
                    (ISNUMERIC(p.ProductCode) = 1 AND CAST(p.ProductCode AS INT) = @scannedInt)
                )
                ORDER BY CASE 
                    WHEN (p.ProductCode = @code OR p.InternationalCode = @code OR p.Unit1Barcode = @code OR p.Unit2Barcode = @code OR p.PartNumber = @code) THEN 0
                    WHEN (p.ScalePLU = @code) THEN 1
                    ELSE 2
                END",
                DbHelper.P("@code", scannedCode),
                DbHelper.P("@scannedPadded", scannedPadded),
                DbHelper.P("@scannedTrimmed", scannedTrimmed),
                DbHelper.P("@scannedInt", scannedInt));

            if (dtDirect.Rows.Count > 0)
            {
                parsedWeight = 1m;
                return dtDirect.Rows[0];
            }

            // 2. SECOND Priority: Scale Barcode Parsing (e.g. 9900168000724 -> PLU = 00168, Weight = 0.072)
            var scaleRes = BarcodeParser.Parse(scannedCode);

            // Fallback parsing for 13-digit scale barcodes starting with 99, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 9
            if (!scaleRes.IsScaleBarcode && scannedCode.Length == 13 && (scannedCode.StartsWith("99") || scannedCode.StartsWith("20") || scannedCode.StartsWith("21") || scannedCode.StartsWith("22") || scannedCode.StartsWith("27") || scannedCode.StartsWith("9")))
            {
                string p = (scannedCode.StartsWith("99") || scannedCode.StartsWith("20") || scannedCode.StartsWith("21") || scannedCode.StartsWith("22") || scannedCode.StartsWith("27")) ? scannedCode.Substring(0, 2) : "9";
                int codeLen = 5;
                int weightLen = 5;
                if (scannedCode.Length >= p.Length + codeLen + weightLen)
                {
                    scaleRes.IsScaleBarcode = true;
                    scaleRes.MatchedPrefix = p;
                    scaleRes.ItemCode = scannedCode.Substring(p.Length, codeLen);
                    scaleRes.TrimmedItemCode = scaleRes.ItemCode.TrimStart('0');
                    if (string.IsNullOrEmpty(scaleRes.TrimmedItemCode)) scaleRes.TrimmedItemCode = "0";

                    string weightStr = scannedCode.Substring(p.Length + codeLen, weightLen);
                    if (decimal.TryParse(weightStr, out decimal w))
                    {
                        scaleRes.WeightOrPrice = w / 1000m;
                    }
                }
            }

            if (scaleRes.IsScaleBarcode && !string.IsNullOrEmpty(scaleRes.ItemCode))
            {
                parsedWeight = scaleRes.WeightOrPrice > 0 ? scaleRes.WeightOrPrice : 1m;
                string itemCode = scaleRes.ItemCode;
                string trimmed = scaleRes.TrimmedItemCode;
                int.TryParse(trimmed, out int itemCodeInt);
                string padded = itemCodeInt > 0 ? itemCodeInt.ToString("D8") : itemCode;

                var dtScale = DbHelper.Query(@"
                    SELECT TOP 1 p.*, c.CategoryName 
                    FROM Products p 
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID 
                    WHERE p.IsActive = 1 AND (
                        p.ScalePLU = @c OR p.ScalePLU = @trimmed OR p.ScalePLU = @padded OR (@intVal > 0 AND ISNUMERIC(p.ScalePLU) = 1 AND CAST(p.ScalePLU AS INT) = @intVal) OR
                        p.ProductCode = @c OR p.ProductCode = @trimmed OR p.ProductCode = @padded OR (ISNUMERIC(p.ProductCode) = 1 AND CAST(p.ProductCode AS INT) = @intVal) OR
                        p.InternationalCode = @c OR p.InternationalCode = @trimmed OR ',' + p.InternationalCode + ',' LIKE '%,' + @c + ',%' OR
                        p.Unit1Barcode = @c OR p.Unit1Barcode = @trimmed OR ',' + p.Unit1Barcode + ',' LIKE '%,' + @c + ',%' OR
                        p.Unit2Barcode = @c OR p.Unit2Barcode = @trimmed OR ',' + p.Unit2Barcode + ',' LIKE '%,' + @c + ',%' OR
                        p.PartNumber = @c OR p.PartNumber = @trimmed OR
                        (@intVal > 0 AND p.ProductID = @intVal)
                    )
                    ORDER BY CASE WHEN (p.ScalePLU = @c OR p.ScalePLU = @trimmed OR p.ScalePLU = @padded OR (@intVal > 0 AND ISNUMERIC(p.ScalePLU) = 1 AND CAST(p.ScalePLU AS INT) = @intVal)) THEN 0 ELSE 1 END",
                    DbHelper.P("@c", itemCode),
                    DbHelper.P("@trimmed", trimmed),
                    DbHelper.P("@padded", padded),
                    DbHelper.P("@intVal", itemCodeInt));

                if (dtScale.Rows.Count > 0)
                {
                    return dtScale.Rows[0];
                }
            }

            return null;
        }

        public static int Save(int id, string code, string name, string unit, decimal price, bool active, decimal purchasePrice, decimal minStockLimit, string description,
            string partNumber, int? categoryID, string carModel, string brand, string shelfLocation, decimal wholesalePrice = 0, decimal semiWholesalePrice = 0, string internationalCode = null, bool printLocalBarcode = true, bool isService = false,
            string unit1Name = null, string unit1Barcode = null, decimal? unit1SalePrice = null, decimal? unit1PurchasePrice = null,
            string unit2Name = null, decimal? unit2Factor = null, string unit2Barcode = null, decimal? unit2SalePrice = null, decimal? unit2PurchasePrice = null,
            decimal? unit3Factor = null, bool isQuickItem = false, string producerCompany = null, bool hasExpiry = false, int? defaultExpiryDays = null, string defaultSaleUnit = null, string productSize = null, string color = null, string englishName = null, string scalePLU = null)
        {
            DbHelper.EnsureScalePLUColumnExists();

            if (!string.IsNullOrWhiteSpace(scalePLU))
            {
                string cleanPLU = scalePLU.Trim();
                string trimmedPLU = cleanPLU.TrimStart('0');
                if (string.IsNullOrEmpty(trimmedPLU)) trimmedPLU = "0";

                int.TryParse(trimmedPLU, out int pluInt);

                var dtCheck = DbHelper.Query(@"
                    SELECT TOP 1 ProductName 
                    FROM Products 
                    WHERE IsActive = 1 
                      AND ProductID <> @id 
                      AND (
                          ScalePLU = @c OR 
                          ScalePLU = @trimmed OR
                          (@pluInt > 0 AND ISNUMERIC(ScalePLU) = 1 AND CAST(ScalePLU AS INT) = @pluInt)
                      )",
                    DbHelper.P("@id", id),
                    DbHelper.P("@c", cleanPLU),
                    DbHelper.P("@trimmed", trimmedPLU),
                    DbHelper.P("@pluInt", pluInt));

                if (dtCheck.Rows.Count > 0)
                {
                    string existingName = dtCheck.Rows[0]["ProductName"].ToString();
                    throw new Exception($"⚠️ كود الميزان (PLU) [{cleanPLU}] مخصص بالفعل لصنف آخر: [{existingName}]!\nلا يمكن تكرار نفس كود الميزان لصنفين مختلفين.");
                }
            }
            if (id == 0)
                return DbHelper.ExecuteInsert(
                    @"INSERT INTO Products(ProductCode,ProductName,Unit,SalePrice,IsActive,PurchasePrice,MinStockLimit,Description,PartNumber,CategoryID,CarModel,Brand,ShelfLocation,WholesalePrice,SemiWholesalePrice,InternationalCode,PrintLocalBarcode,IsService,IsQuickItem,
                                           Unit1Name,Unit1Barcode,Unit1SalePrice,Unit1PurchasePrice,Unit2Name,Unit2Factor,Unit2Barcode,Unit2SalePrice,Unit2PurchasePrice,Unit3Factor,ProducerCompany,HasExpiry,DefaultExpiryDays,DefaultSaleUnit,ProductSize,Color,EnglishName,ScalePLU) 
                      VALUES(@c,@n,@u,@p,@a,@pp,@msl,@d,@pn,@cat,@cm,@b,@sl,@wp,@swp,@ic,@plb,@srv,@qi,
                             @u1n,@u1b,@u1sp,@u1pp,@u2n,@u2f,@u2b,@u2sp,@u2pp,@u3f,@comp,@hexp,@expd,@dsu,@psize,@clr,@enName,@splu)",
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
                    DbHelper.P("@comp", producerCompany),
                    DbHelper.P("@hexp", hasExpiry),
                    DbHelper.P("@expd", defaultExpiryDays.HasValue ? (object)defaultExpiryDays.Value : DBNull.Value),
                    DbHelper.P("@dsu", string.IsNullOrEmpty(defaultSaleUnit) ? (object)DBNull.Value : defaultSaleUnit),
                    DbHelper.P("@psize", string.IsNullOrEmpty(productSize) ? (object)DBNull.Value : productSize),
                    DbHelper.P("@clr", string.IsNullOrEmpty(color) ? (object)DBNull.Value : color),
                    DbHelper.P("@enName", string.IsNullOrEmpty(englishName) ? (object)DBNull.Value : englishName),
                    DbHelper.P("@splu", string.IsNullOrWhiteSpace(scalePLU) ? (object)DBNull.Value : scalePLU.Trim()));
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
                          Unit3Factor=@u3f,ProducerCompany=@comp,HasExpiry=@hexp,DefaultExpiryDays=@expd,DefaultSaleUnit=@dsu,ProductSize=@psize,Color=@clr,EnglishName=@enName,ScalePLU=@splu,
                          PendingSalePrice=NULL, PendingQtyThreshold=NULL, PendingPriceSourceRefID=NULL
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
                    DbHelper.P("@comp", producerCompany),
                    DbHelper.P("@hexp", hasExpiry),
                    DbHelper.P("@expd", defaultExpiryDays.HasValue ? (object)defaultExpiryDays.Value : DBNull.Value),
                    DbHelper.P("@dsu", string.IsNullOrEmpty(defaultSaleUnit) ? (object)DBNull.Value : defaultSaleUnit),
                    DbHelper.P("@psize", string.IsNullOrEmpty(productSize) ? (object)DBNull.Value : productSize),
                    DbHelper.P("@clr", string.IsNullOrEmpty(color) ? (object)DBNull.Value : color),
                    DbHelper.P("@enName", string.IsNullOrEmpty(englishName) ? (object)DBNull.Value : englishName),
                    DbHelper.P("@splu", string.IsNullOrWhiteSpace(scalePLU) ? (object)DBNull.Value : scalePLU.Trim()),
                    DbHelper.P("@id", id));

                if (Math.Abs(price - oldPrice) > 0.005m)
                {
                    DbHelper.Execute(
                        @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                          VALUES (@pid, @old, @new, 'ProductCard', NULL, @uid, N'تعديل سعر البيع من كارت الصنف')",
                        DbHelper.P("@pid", id), DbHelper.P("@old", oldPrice), DbHelper.P("@new", price), DbHelper.P("@uid", Session.EmpID));
                }

                ProductCache.Invalidate();
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

        public static void UpdateMinStockLimit(int productID, decimal minStockLimit)
        {
            DbHelper.Execute("UPDATE Products SET MinStockLimit=@msl WHERE ProductID=@id",
                DbHelper.P("@msl", minStockLimit),
                DbHelper.P("@id", productID));
            ProductCache.Invalidate();
        }

        public static int BulkUpdateMinStockLimit(Dictionary<int, decimal> updates)
        {
            if (updates == null || updates.Count == 0) return 0;
            int count = 0;
            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (var kvp in updates)
                {
                    DbHelper.ExecuteTrans(trans,
                        "UPDATE Products SET MinStockLimit=@msl WHERE ProductID=@id",
                        DbHelper.P("@msl", kvp.Value),
                        DbHelper.P("@id", kvp.Key));
                    count++;
                }
            });
            ProductCache.Invalidate();
            return count;
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Products SET IsActive=0 WHERE ProductID=@id", DbHelper.P("@id", id));
            ProductCache.Invalidate();
        }

        /// <summary>
        /// بحث عن صنف عن طريق الباركود أو كود الصنف أو رقم القطعة (PartNumber).
        /// يُستخدم للقراءة السريعة بجهاز السكنر.
        /// </summary>
        public static DataTable FindByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return new DataTable();

            DataRow dr = GetByBarcodeOrScaleCode(code, out decimal weight);
            if (dr == null) return new DataTable();

            DataTable dt = dr.Table.Clone();
            if (!dt.Columns.Contains("MatchedUnit")) dt.Columns.Add("MatchedUnit", typeof(int));
            if (!dt.Columns.Contains("ParsedWeight")) dt.Columns.Add("ParsedWeight", typeof(decimal));

            DataRow newRow = dt.NewRow();
            foreach (DataColumn col in dr.Table.Columns)
            {
                if (dt.Columns.Contains(col.ColumnName))
                    newRow[col.ColumnName] = dr[col.ColumnName];
            }
            newRow["MatchedUnit"] = dr.Table.Columns.Contains("MatchedUnit") ? dr["MatchedUnit"] : 3;
            newRow["ParsedWeight"] = weight;
            dt.Rows.Add(newRow);

            return dt;
        }

        /// <summary>
        /// تسجيل سعر بيع مقترح كـ"سعر معلق" بناءً على الكمية الحالية بالمخزون.
        /// يتفعّل تلقائياً عندما يصل المخزون إلى الحد المحدد أو أقل.
        /// </summary>
        /// <param name="productID">الصنف</param>
        /// <param name="pendingPrice">السعر الجديد المقترح</param>
        /// <param name="costPrice">تكلفة الشراء الجديدة</param>
        /// <param name="applyNow">true = طبّق فوراً | false = علّق حتى نفاد المخزون القديم</param>
        public static void SetPendingPrice(int productID, decimal pendingPrice, decimal costPrice, bool applyNow = true, int? purchaseID = null, string unitName = null)
        {
            var dtProd = DbHelper.Query("SELECT Unit, Unit1Name, Unit1SalePrice, Unit2Name, Unit2SalePrice, SalePrice FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
            string saleCol = "SalePrice";
            if (dtProd.Rows.Count > 0)
            {
                var row = dtProd.Rows[0];
                string u1Name = row["Unit1Name"] != DBNull.Value ? row["Unit1Name"].ToString() : "";
                string u2Name = row["Unit2Name"] != DBNull.Value ? row["Unit2Name"].ToString() : "";
                if (!string.IsNullOrEmpty(unitName) && unitName == u2Name)
                {
                    saleCol = "Unit2SalePrice";
                }
                else if (!string.IsNullOrEmpty(unitName) && unitName == u1Name)
                {
                    saleCol = "Unit1SalePrice";
                }
            }

            var val = DbHelper.Scalar($"SELECT {saleCol} FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
            decimal currentSalePrice = val != DBNull.Value && val != null ? Convert.ToDecimal(val) : 0m;

            if (applyNow)
            {
                // طبّق فوراً على الكل — امسح أي سعر معلق سابق
                DbHelper.Execute(
                    $@"UPDATE Products
                      SET {saleCol}            = @sp,
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
                    SetPendingPrice(productID, pendingPrice, costPrice, applyNow: true, purchaseID: purchaseID, unitName: unitName);
                    return;
                }

                DbHelper.Execute(
                    $@"UPDATE Products
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
                $"صنف رقم ({productID}) | العمود: {saleCol} | السعر الجديد: {pendingPrice:N2} ج | التكلفة: {costPrice:N2} ج | التفعيل فوري: {(applyNow ? "نعم" : "معلق لحين نفاذ الكمية")}");
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

                AppLogger.Audit("تفعيل سعر معلق", $"صنف رقم ({productID}) | السعر الجديد المفعل: {newPrice:N2} ج");
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
                    $"صنف رقم ({productID}) | المخزون المتبقي: {currentStock:N2} | الحد المطلوب للتفعيل: {threshold:N2}");
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
                                       AND s.IsPosted IN (0, 1)
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
        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Suppliers WHERE SupplierID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static decimal GetBalance(int supplierID)
        {
            return GetPreviousBalance(supplierID, DateTime.Now.AddDays(1));
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
        public static string AddSupplierPayment(int supplierID, decimal amount, string notes, int? safeAccountID = null)
        {
            string payCode = "";
            DbHelper.RunInTransaction((con, trans) =>
            {
                int accId = safeAccountID.HasValue && safeAccountID.Value > 0 ? safeAccountID.Value : Session.GetDefaultSafeID();

                // توليد كود القيد التسلسلي SPY-XXXX
                var nextResult = DbHelper.ScalarTrans(trans,
                    "SELECT COALESCE(MAX(TransID), 0) + 1 FROM SupplierTransactions");
                int nextNum = nextResult != null ? Convert.ToInt32(nextResult) : 1;
                payCode = "SPY-" + nextNum.ToString("D4");

                // التحقق من رصيد الخزنة قبل الصرف ومنع السحب على المكشوف
                AccountDAL.EnsureSufficientCashTrans(trans, accId, amount, "سداد دفعة المورد");

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
                    "INSERT INTO CashBox(TransDate,TransType,AmountOut,Notes,CreatedBy,AccountID) " +
                    "VALUES(GETDATE(),'SupplierPayment',@amt,@n,@by,@accId)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", payCode + " - صرف للمورد - " + notes),
                    DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@accId", accId));
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
                           COALESCE(c.DefaultPaymentType, N'Any') AS DefaultPaymentType,
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
                  COALESCE(c.DefaultPaymentType, N'Any') AS DefaultPaymentType,
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

        public static decimal GetBalance(int clientID)
        {
            return GetClientBalance(clientID);
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

        public static int Save(int id, string code, string name, string phone, string phone2, string address, decimal opening, bool active, int? driverID, decimal maxCreditLimit, string notes, string defaultPriceTier = "قطاعي", int openingCrates = 0, string defaultPaymentType = "Any")
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Clients(ClientCode,ClientName,Phone,Phone2,Address,OpeningBalance,IsActive,DriverID,MaxCreditLimit,Notes,DefaultPriceTier,OpeningCrates,DefaultPaymentType) VALUES(@c,@n,@ph,@ph2,@a,@ob,@act,@dr,@mcl,@notes,@dpt,@oc,@dptype)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes), DbHelper.P("@dpt", defaultPriceTier),
                    DbHelper.P("@oc", openingCrates), DbHelper.P("@dptype", string.IsNullOrEmpty(defaultPaymentType) ? "Any" : defaultPaymentType));
                return newID;
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Clients SET ClientCode=@c,ClientName=@n,Phone=@ph,Phone2=@ph2,Address=@a,IsActive=@act,DriverID=@dr,MaxCreditLimit=@mcl,Notes=@notes,DefaultPriceTier=@dpt,OpeningCrates=@oc,DefaultPaymentType=@dptype WHERE ClientID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes), DbHelper.P("@dpt", defaultPriceTier),
                    DbHelper.P("@oc", openingCrates), DbHelper.P("@dptype", string.IsNullOrEmpty(defaultPaymentType) ? "Any" : defaultPaymentType),
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

    // =================== Shift DAL ===================
    public static class ShiftDAL
    {
        public static DataTable GetShiftsReport(DateTime from, DateTime to)
        {
            DbHelper.EnsureShiftSchema();
            DateTime f = from;
            DateTime t = to;
            if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

            string sql = @"
                SELECT 
                    s.ShiftID,
                    ISNULL(sa.AccountName, N'الدرج الرئيسي') AS SafeName,
                    eOpen.EmpName AS OpenedByName,
                    s.OpenTime,
                    eClose.EmpName AS ClosedByName,
                    s.CloseTime,
                    s.OpeningCash,
                    CASE 
                        WHEN s.Status = 'Closed' THEN s.CashSales 
                        ELSE ISNULL((SELECT SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) 
                    END AS CashSales,
                    CASE 
                        WHEN s.Status = 'Closed' THEN s.VisaSales 
                        ELSE ISNULL((SELECT SUM(CASE WHEN SaleType = 'Visa' THEN ISNULL(VisaPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(VisaPaid, 0) ELSE 0 END) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) 
                    END AS VisaSales,
                    CASE 
                        WHEN s.Status = 'Closed' THEN s.TotalSales 
                        ELSE ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) 
                    END AS TotalSales,
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(s.OpenTime AS DATE) AND IsPosted = 1), 0) AS CalendarSales,
                    CASE 
                        WHEN s.Status = 'Closed' THEN s.ExpectedCash 
                        ELSE (s.OpeningCash + 
                              ISNULL((SELECT SUM(CASE WHEN SaleType = 'Cash' THEN ISNULL(CashPaid, TotalAmount) WHEN SaleType = 'Mixed' THEN ISNULL(CashPaid, 0) ELSE 0 END) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) -
                              ISNULL((SELECT SUM(sr.TotalAmount) FROM SalesReturns sr JOIN Sales sl ON sr.SaleID = sl.SaleID WHERE (sl.ShiftID = s.ShiftID OR (sl.ShiftID IS NULL AND sl.SaleDate >= s.OpenTime))), 0) -
                              ISNULL((SELECT SUM(AmountOut) FROM CashBox WHERE TransDate >= s.OpenTime AND (AccountID = s.SafeAccountID OR AccountID = 1 OR AccountID IS NULL) AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')), 0) +
                              ISNULL((SELECT SUM(AmountIn) FROM CashBox WHERE TransDate >= s.OpenTime AND (AccountID = s.SafeAccountID OR AccountID = 1 OR AccountID IS NULL) AND TransType NOT IN ('Sale', 'SaleIncome', 'SaleReturn', 'Return', 'ShiftCloseOut', 'ShiftCloseIn', 'ShiftClose', 'ShiftDeficit', 'ShiftSurplus', 'ShiftOpen')), 0)
                             )
                    END AS ExpectedCash,
                    s.ActualCash,
                    s.Difference,
                    CASE WHEN s.Status = 'Closed' THEN N'مغلقة' ELSE N'مفتوحة 🟢' END AS StatusArabic,
                    s.Notes
                FROM Shifts s
                LEFT JOIN Employees eOpen ON s.OpenedBy = eOpen.EmpID
                LEFT JOIN Employees eClose ON s.ClosedBy = eClose.EmpID
                LEFT JOIN SafeAccounts sa ON s.SafeAccountID = sa.AccountID
                WHERE s.OpenTime BETWEEN @f AND @t
                ORDER BY s.ShiftID DESC";
            return DbHelper.Query(sql, DbHelper.P("@f", f), DbHelper.P("@t", t));
        }

        public static DataTable GetShiftVsCalendarComparison(DateTime from, DateTime to)
        {
            DbHelper.EnsureShiftSchema();
            DateTime f = from;
            DateTime t = to;
            if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

            string sql = @"
                SELECT 
                    s.ShiftID,
                    CASE WHEN s.Status = 'Closed' THEN N'مغلقة' ELSE N'مفتوحة 🟢' END AS StatusArabic,
                    s.OpenTime,
                    ISNULL(s.CloseTime, GETDATE()) AS CloseTime,
                    CASE 
                        WHEN s.Status = 'Closed' THEN s.TotalSales 
                        ELSE ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) 
                    END AS ShiftSales,
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(s.OpenTime AS DATE) AND IsPosted = 1), 0) AS CalendarSales,
                    (
                        (CASE WHEN s.Status = 'Closed' THEN s.TotalSales ELSE ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE (ShiftID = s.ShiftID OR (ShiftID IS NULL AND SaleDate >= s.OpenTime)) AND IsPosted = 1), 0) END) -
                        ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(s.OpenTime AS DATE) AND IsPosted = 1), 0)
                    ) AS Difference,
                    N'مبيعات الوردية تحسب الفواتير من وقت الفتح إلى الإغلاق، بينما مبيعات اليوم التقويمي تحسب الفواتير من 12 ص إلى 11:59 م' AS Explanation
                FROM Shifts s
                WHERE s.OpenTime BETWEEN @f AND @t
                ORDER BY s.ShiftID DESC";
            return DbHelper.Query(sql, DbHelper.P("@f", f), DbHelper.P("@t", t));
        }

        public static int? GetActiveShiftID()
        {
            try
            {
                DbHelper.EnsureShiftSchema();
                object o = DbHelper.Scalar("SELECT TOP 1 ShiftID FROM Shifts WHERE Status = 'Open' ORDER BY ShiftID DESC");
                if (o != null && o != DBNull.Value)
                {
                    int sid = Convert.ToInt32(o);
                    Session.CurrentShiftID = sid;
                    // Auto-heal any orphan invoices created today that were saved without a ShiftID
                    try { DbHelper.Execute("UPDATE Sales SET ShiftID = @sid WHERE ShiftID IS NULL AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)", DbHelper.P("@sid", sid)); } catch {}
                    return sid;
                }
            }
            catch {}
            return null;
        }

        /// <summary>
        /// يضمن وجود وردية نشطة ومفتوحة تلقائياً، وإذا لم تكن هناك وردية مفتوحة يقوم بفتح وردية جديدة فوراً برصيد الخزينة الفعلي.
        /// </summary>
        public static int EnsureActiveShift(int empID = 0, int? safeAccountID = null, decimal? customOpeningCash = null)
        {
            try
            {
                DbHelper.EnsureShiftSchema();

                // 1. التحقق من وجود وردية مفتوحة حالياً
                object o = DbHelper.Scalar("SELECT TOP 1 ShiftID FROM Shifts WHERE Status = 'Open' ORDER BY ShiftID DESC");
                if (o != null && o != DBNull.Value)
                {
                    int sid = Convert.ToInt32(o);
                    Session.CurrentShiftID = sid;
                    try { DbHelper.Execute("UPDATE Sales SET ShiftID = @sid WHERE ShiftID IS NULL AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)", DbHelper.P("@sid", sid)); } catch {}
                    return sid;
                }

                // 2. لا توجد وردية مفتوحة -> فتح وردية جديدة تلقائياً فوراً
                if (empID <= 0) empID = Session.EmpID > 0 ? Session.EmpID : 1;
                int safeID = safeAccountID.HasValue && safeAccountID.Value > 0 
                    ? safeAccountID.Value 
                    : (Session.DefaultSafeID ?? Session.GetDefaultSafeID());

                decimal openingCash = 0m;
                if (customOpeningCash.HasValue)
                {
                    openingCash = Math.Max(0m, customOpeningCash.Value);
                }
                else
                {
                    try
                    {
                        decimal liveBal = AccountDAL.GetCashBalance(safeID);
                        openingCash = Math.Max(0m, liveBal);
                    }
                    catch
                    {
                        openingCash = 0m;
                    }
                }

                string stationName = Environment.MachineName;
                string cashierName = Session.EmpName ?? "كاشير";
                string branchName = "الفرع الرئيسي";

                int newShiftID = DbHelper.ExecuteInsert(
                    @"INSERT INTO Shifts (ShiftDate, OpenTime, OpenedBy, OpeningCash, SafeAccountID, Status, Notes, POSStationName, BranchName, CashierName, ApprovalStatus)
                      VALUES (CAST(GETDATE() AS DATE), GETDATE(), @emp, @cash, @safe, 'Open', @notes, @pos, @branch, @cashier, 'Open')",
                    DbHelper.P("@emp", empID),
                    DbHelper.P("@cash", openingCash),
                    DbHelper.P("@safe", safeID > 0 ? (object)safeID : DBNull.Value),
                    DbHelper.P("@notes", "فتح وردية تلقائي"),
                    DbHelper.P("@pos", stationName),
                    DbHelper.P("@branch", branchName),
                    DbHelper.P("@cashier", cashierName));

                if (newShiftID > 0)
                {
                    Session.CurrentShiftID = newShiftID;
                    if (safeID > 0) Session.DefaultSafeID = safeID;

                    // تسجيل قيد إثبات فتح الوردية
                    DbHelper.Execute(
                        @"INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, AccountID, Notes, CreatedBy, RefID, ShiftID)
                          VALUES (GETDATE(), 'ShiftOpen', 0, 0, @acc, @notes, @uid, @ref, @ref)",
                        DbHelper.P("@acc", safeID > 0 ? safeID : 1),
                        DbHelper.P("@notes", $"فتح وردية عمل جديدة #{newShiftID} تلقائياً ({cashierName} - {stationName}) - رصيد افتتاحي: {openingCash:N2} ج"),
                        DbHelper.P("@uid", empID),
                        DbHelper.P("@ref", newShiftID));

                    try { DbHelper.Execute("UPDATE Sales SET ShiftID = @sid WHERE ShiftID IS NULL AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)", DbHelper.P("@sid", newShiftID)); } catch {}

                    return newShiftID;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShiftDAL.EnsureActiveShift", ex);
            }
            return 0;
        }

        /// <summary>
        /// اعتماد إغلاق الوردية من قبل المدير / المحاسب وقفل أي تعديل لاحق
        /// </summary>
        public static bool ApproveShift(int shiftID, int managerEmpID, string managerName, string notes = "")
        {
            try
            {
                DbHelper.EnsureShiftSchema();
                DbHelper.Execute(@"
                    UPDATE Shifts
                    SET ApprovalStatus = 'Approved',
                        ApprovedBy = @mgrId,
                        ApprovedByName = @mgrName,
                        ApprovalTime = GETDATE(),
                        ApprovalNotes = @notes
                    WHERE ShiftID = @sid",
                    DbHelper.P("@sid", shiftID),
                    DbHelper.P("@mgrId", managerEmpID),
                    DbHelper.P("@mgrName", managerName ?? "المدير العام"),
                    DbHelper.P("@notes", notes ?? "تم اعتماد تقفيل الوردية"));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShiftDAL.ApproveShift", ex);
                return false;
            }
        }

        /// <summary>
        /// رفض تقفيل الوردية للمراجعة وإعادة التدقيق
        /// </summary>
        public static bool RejectShift(int shiftID, int managerEmpID, string managerName, string reason)
        {
            try
            {
                DbHelper.EnsureShiftSchema();
                DbHelper.Execute(@"
                    UPDATE Shifts
                    SET ApprovalStatus = 'Rejected',
                        ApprovedBy = @mgrId,
                        ApprovedByName = @mgrName,
                        ApprovalTime = GETDATE(),
                        ApprovalNotes = @notes
                    WHERE ShiftID = @sid",
                    DbHelper.P("@sid", shiftID),
                    DbHelper.P("@mgrId", managerEmpID),
                    DbHelper.P("@mgrName", managerName ?? "المدير العام"),
                    DbHelper.P("@notes", reason ?? "تم رفض تقفيل الوردية للمراجعة"));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShiftDAL.RejectShift", ex);
                return false;
            }
        }
    }

    // =================== Account DAL ===================
    public static class AccountDAL
    {
        public static DataTable GetActiveSafeAccounts()
        {
            return DbHelper.Query("SELECT AccountID, AccountName, AccountType, AccountNumber, OpeningBalance FROM SafeAccounts WHERE IsActive = 1 ORDER BY AccountID");
        }

        /// <summary>
        /// استرجاع الخزن / الأدراج المسموح بها للمستخدم الحالي فقط.
        /// </summary>
        public static DataTable GetAllowedSafeAccounts()
        {
            DataTable dt = GetActiveSafeAccounts();
            if (Session.IsAdmin) return dt;

            var allowed = Session.GetAllowedSafeIDSet();
            if (allowed == null) return dt;

            DataTable filtered = dt.Clone();
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["AccountID"]);
                if (allowed.Contains(id))
                {
                    filtered.ImportRow(r);
                }
            }

            if (filtered.Rows.Count == 0 && dt.Rows.Count > 0)
            {
                int defId = Session.GetDefaultSafeID();
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["AccountID"]) == defId)
                    {
                        filtered.ImportRow(r);
                        break;
                    }
                }
                if (filtered.Rows.Count == 0)
                {
                    filtered.ImportRow(dt.Rows[0]);
                }
            }

            return filtered;
        }

        public static DataTable GetActiveVisaAccounts()
        {
            try
            {
                var dt = DbHelper.Query("SELECT AccountID, AccountName, AccountType, AccountNumber, OpeningBalance FROM SafeAccounts WHERE IsActive = 1 AND AccountType IN ('Visa', 'Bank') ORDER BY AccountID");
                if (dt.Rows.Count == 0)
                {
                    DbHelper.Execute(@"
                        IF NOT EXISTS (SELECT 1 FROM SafeAccounts WHERE AccountType = 'Visa')
                        BEGIN
                            INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                            VALUES (N'ماكينة فيزا 1', N'Visa', N'VISA-01', 0.00, 1);
                        END");
                    dt = DbHelper.Query("SELECT AccountID, AccountName, AccountType, AccountNumber, OpeningBalance FROM SafeAccounts WHERE IsActive = 1 AND AccountType IN ('Visa', 'Bank') ORDER BY AccountID");
                }
                return dt;
            }
            catch
            {
                return new DataTable();
            }
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

        public static decimal GetCashBalance(int? accountID = null, DateTime? upToDate = null)
        {
            if (accountID.HasValue && accountID.Value > 0)
            {
                var openingObj = DbHelper.Scalar("SELECT OpeningBalance FROM SafeAccounts WHERE AccountID = @accId", DbHelper.P("@accId", accountID.Value));
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                string sql = "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox WHERE (AccountID = @accId" + (accountID.Value == 1 ? " OR AccountID IS NULL)" : ")");
                if (upToDate.HasValue)
                {
                    sql += " AND CAST(TransDate AS DATE) <= @to";
                    var result = DbHelper.Scalar(sql, DbHelper.P("@accId", accountID.Value), DbHelper.P("@to", upToDate.Value.Date));
                    return opening + (result == null ? 0 : Convert.ToDecimal(result));
                }
                else
                {
                    var result = DbHelper.Scalar(sql, DbHelper.P("@accId", accountID.Value));
                    return opening + (result == null ? 0 : Convert.ToDecimal(result));
                }
            }
            else
            {
                var openingObj = DbHelper.Scalar("SELECT SUM(OpeningBalance) FROM SafeAccounts");
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                string sql = "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox";
                if (upToDate.HasValue)
                {
                    sql += " WHERE CAST(TransDate AS DATE) <= @to";
                    var result = DbHelper.Scalar(sql, DbHelper.P("@to", upToDate.Value.Date));
                    return opening + (result == null ? 0 : Convert.ToDecimal(result));
                }
                else
                {
                    var result = DbHelper.Scalar(sql);
                    return opening + (result == null ? 0 : Convert.ToDecimal(result));
                }
            }
        }

        public static decimal GetCashBalanceTrans(System.Data.SqlClient.SqlTransaction trans, int? accountID = null)
        {
            if (accountID.HasValue && accountID.Value > 0)
            {
                var openingObj = DbHelper.ScalarTrans(trans, "SELECT OpeningBalance FROM SafeAccounts WHERE AccountID = @accId", DbHelper.P("@accId", accountID.Value));
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                string sql = "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox WHERE (AccountID = @accId" + (accountID.Value == 1 ? " OR AccountID IS NULL)" : ")");
                var result = DbHelper.ScalarTrans(trans, sql, DbHelper.P("@accId", accountID.Value));
                return opening + (result == null ? 0 : Convert.ToDecimal(result));
            }
            else
            {
                var openingObj = DbHelper.ScalarTrans(trans, "SELECT SUM(OpeningBalance) FROM SafeAccounts");
                decimal opening = openingObj != DBNull.Value && openingObj != null ? Convert.ToDecimal(openingObj) : 0m;
                
                string sql = "SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox";
                var result = DbHelper.ScalarTrans(trans, sql);
                return opening + (result == null ? 0 : Convert.ToDecimal(result));
            }
        }

        public static void EnsureSufficientCashTrans(System.Data.SqlClient.SqlTransaction trans, int accountID, decimal requiredAmount, string operationName = "الصرف")
        {
            if (requiredAmount <= 0) return;
            decimal currentBalance = GetCashBalanceTrans(trans, accountID);
            if (currentBalance < requiredAmount)
            {
                string accName = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", accountID))?.ToString() ?? "الخزنة / الدرج";
                throw new Exception($"⛔ غير مسموح بالصرف على المكشوف أو تحويل الحساب لرصيد سالب!\nالرصيد المتاح حالياً في [{accName}] هو ({currentBalance:N2} ج) فقط، بينما المبلغ المطلوب لـ ({operationName}) هو ({requiredAmount:N2} ج).\nالعملية مرفوضة نهائياً.");
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
            int targetAccID = safeAccountID.HasValue && safeAccountID.Value > 0 ? safeAccountID.Value : Session.GetDefaultSafeID();

            // ضبط التوقيت والتاريخ لضمان تسجيل الساعات والدقائق الفعلية إذا اختار المستخدم اليوم الحالي
            if (date.TimeOfDay == TimeSpan.Zero || date.Date == DateTime.Today)
            {
                date = date.Date.Add(DateTime.Now.TimeOfDay);
            }

            int? currentShiftID = Session.CurrentShiftID > 0 ? (int?)Session.CurrentShiftID : null;

            if (id == 0)
            {
                int newID = -1;
                DbHelper.RunInTransaction((con, trans) =>
                {
                    EnsureSufficientCashTrans(trans, targetAccID, amount, "تسجيل المصروف");

                    newID = DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO Expenses(ExpenseDate,ExpenseType,Amount,Notes,SupplierID,VehicleID,CreatedBy,SafeAccountID,ShiftID) VALUES(@d,@t,@a,@n,@s,@v,@by,@accId,@sid)",
                        DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                        DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@accId", targetAccID), DbHelper.P("@sid", currentShiftID));
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy,AccountID,ShiftID) VALUES(@d,'Expense',@a,@ref,@n,@by,@accId,@sid)",
                        DbHelper.P("@d", date), DbHelper.P("@a", amount), DbHelper.P("@ref", newID),
                        DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@accId", targetAccID), DbHelper.P("@sid", currentShiftID));
                });
                return newID;
            }

            DbHelper.RunInTransaction((con, trans) =>
            {
                var oldAmountObj = DbHelper.ScalarTrans(trans, "SELECT Amount FROM Expenses WHERE ExpenseID=@id", DbHelper.P("@id", id));
                decimal oldAmount = oldAmountObj != null ? Convert.ToDecimal(oldAmountObj) : 0;

                decimal diff = amount - oldAmount;
                if (diff > 0)
                {
                    EnsureSufficientCashTrans(trans, targetAccID, diff, "تعديل المصروف بالزيادة");
                }

                DbHelper.ExecuteTrans(trans,
                    "UPDATE Expenses SET ExpenseDate=@d,ExpenseType=@t,Amount=@a,Notes=@n,SupplierID=@s,VehicleID=@v,SafeAccountID=@accId,ShiftID=ISNULL(ShiftID,@sid) WHERE ExpenseID=@id",
                    DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                    DbHelper.P("@n", notes), DbHelper.P("@s", supplierID), DbHelper.P("@v", vehicleID), DbHelper.P("@accId", targetAccID), DbHelper.P("@sid", currentShiftID), DbHelper.P("@id", id));

                DbHelper.ExecuteTrans(trans,
                    "UPDATE CashBox SET TransDate=@d, AmountOut=@a, Notes=@n, AccountID=@accId, ShiftID=ISNULL(ShiftID,@sid) WHERE RefID=@ref AND TransType='Expense'",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@accId", targetAccID), DbHelper.P("@sid", currentShiftID), DbHelper.P("@ref", id));
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
                EnsureSufficientCashTrans(trans, sourceAccountID, amount, "تحويل النقدية");
                
                string srcName = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", sourceAccountID))?.ToString();
                string destName = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID=@id", DbHelper.P("@id", destAccountID))?.ToString();

                // Outflow from source
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransDate, TransType, AmountOut, Notes, CreatedBy, AccountID) VALUES(GETDATE(), 'Transfer', @amt, @notes, @uid, @src)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@notes", $"تحويل صادر إلى {destName} | " + notes),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@src", sourceAccountID));
                
                // Inflow to destination
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransDate, TransType, AmountIn, Notes, CreatedBy, AccountID) VALUES(GETDATE(), 'Transfer', @amt, @notes, @uid, @dest)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@notes", $"تحويل وارد من {srcName} | " + notes),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@dest", destAccountID));
            });
        }
    }
}

