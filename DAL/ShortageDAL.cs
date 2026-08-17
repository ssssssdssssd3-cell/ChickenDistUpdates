using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class ZeroStockItemDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinStockLimit { get; set; }
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int? SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string Brand { get; set; }
    }

    public static class ShortageDAL
    {
        public static void EnsureTable()
        {
            try
            {
                DbHelper.Execute(@"
                IF OBJECT_ID('ShortageNotebook', 'U') IS NULL
                BEGIN
                    CREATE TABLE ShortageNotebook (
                        ShortageID INT IDENTITY(1,1) PRIMARY KEY,
                        ProductID INT NULL,
                        ProductName NVARCHAR(255) NOT NULL,
                        ProductCode NVARCHAR(100) NULL,
                        CurrentStock DECIMAL(18,3) NOT NULL DEFAULT 0,
                        MinStockLimit DECIMAL(18,3) NOT NULL DEFAULT 0,
                        RequestedQty DECIMAL(18,3) NOT NULL DEFAULT 1,
                        SupplierID INT NULL,
                        SupplierName NVARCHAR(255) NULL,
                        CategoryID INT NULL,
                        CategoryName NVARCHAR(255) NULL,
                        Brand NVARCHAR(255) NULL,
                        Notes NVARCHAR(500) NULL,
                        Status NVARCHAR(50) NOT NULL DEFAULT N'جديد',
                        Source NVARCHAR(50) NOT NULL DEFAULT N'يدوي',
                        CreatedBy INT NULL,
                        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
                    );
                    CREATE INDEX IX_ShortageNotebook_Status ON ShortageNotebook(Status);
                    CREATE INDEX IX_ShortageNotebook_ProductID ON ShortageNotebook(ProductID);
                END
                ELSE
                BEGIN
                    IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShortageNotebook') AND name = 'SupplierID')
                        ALTER TABLE ShortageNotebook ADD SupplierID INT NULL, SupplierName NVARCHAR(255) NULL;
                    IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShortageNotebook') AND name = 'CategoryID')
                        ALTER TABLE ShortageNotebook ADD CategoryID INT NULL, CategoryName NVARCHAR(255) NULL;
                    IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShortageNotebook') AND name = 'Brand')
                        ALTER TABLE ShortageNotebook ADD Brand NVARCHAR(255) NULL;
                    IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShortageNotebook') AND name = 'RequestedQty')
                        ALTER TABLE ShortageNotebook ADD RequestedQty DECIMAL(18,3) NOT NULL DEFAULT 1;
                END");
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShortageDAL.EnsureTable", ex);
            }
        }

        public static bool AddOrUpdateShortage(
            int? productID,
            string productName,
            decimal requestedQty,
            decimal currentStock,
            decimal minStockLimit,
            string notes,
            string source,
            string status = "جديد",
            int? supplierID = null,
            string supplierName = null,
            int? categoryID = null,
            string categoryName = null,
            string brand = null)
        {
            EnsureTable();
            try
            {
                if (productID.HasValue && productID.Value > 0)
                {
                    // Fetch product details if not passed
                    if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(categoryName) || string.IsNullOrEmpty(supplierName))
                    {
                        var dtProd = DbHelper.Query(@"
                            SELECT p.ProductName, p.ProductCode, p.CategoryID, c.CategoryName, 
                                   COALESCE(p.Brand, p.ProducerCompany) AS Brand, p.MinStockLimit,
                                   lastSup.SupplierID, lastSup.SupplierName
                            FROM Products p
                            LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                            OUTER APPLY (
                                SELECT TOP 1 pu.SupplierID, sup.SupplierName
                                FROM PurchaseItems pi
                                INNER JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                                LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                                WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1
                                ORDER BY pu.PurchaseDate DESC, pu.PurchaseID DESC
                            ) lastSup
                            WHERE p.ProductID = @pid",
                            DbHelper.P("@pid", productID.Value));

                        if (dtProd.Rows.Count > 0)
                        {
                            var r = dtProd.Rows[0];
                            if (string.IsNullOrEmpty(productName)) productName = r["ProductName"].ToString();
                            if (!categoryID.HasValue && r["CategoryID"] != DBNull.Value) categoryID = Convert.ToInt32(r["CategoryID"]);
                            if (string.IsNullOrEmpty(categoryName) && r["CategoryName"] != DBNull.Value) categoryName = r["CategoryName"].ToString();
                            if (string.IsNullOrEmpty(brand) && r["Brand"] != DBNull.Value) brand = r["Brand"].ToString();
                            if (minStockLimit <= 0 && r["MinStockLimit"] != DBNull.Value) minStockLimit = Convert.ToDecimal(r["MinStockLimit"]);
                            if (!supplierID.HasValue && r["SupplierID"] != DBNull.Value) supplierID = Convert.ToInt32(r["SupplierID"]);
                            if (string.IsNullOrEmpty(supplierName) && r["SupplierName"] != DBNull.Value) supplierName = r["SupplierName"].ToString();
                        }
                    }

                    // Check if already in ShortageNotebook with active status (جديد / تم الطلب)
                    var dtActive = DbHelper.Query(@"
                        SELECT TOP 1 ShortageID, RequestedQty 
                        FROM ShortageNotebook 
                        WHERE ProductID = @pid AND Status IN (N'جديد', N'تم الطلب')",
                        DbHelper.P("@pid", productID.Value));

                    if (dtActive.Rows.Count > 0)
                    {
                        int sId = Convert.ToInt32(dtActive.Rows[0]["ShortageID"]);
                        decimal existingReq = Convert.ToDecimal(dtActive.Rows[0]["RequestedQty"]);
                        decimal newReq = requestedQty > 0 ? requestedQty : Math.Max(existingReq, 1);

                        DbHelper.Execute(@"
                            UPDATE ShortageNotebook 
                            SET CurrentStock = @stock,
                                MinStockLimit = @min,
                                RequestedQty = @req,
                                SupplierID = COALESCE(@supId, SupplierID),
                                SupplierName = COALESCE(@supName, SupplierName),
                                CategoryID = COALESCE(@catId, CategoryID),
                                CategoryName = COALESCE(@catName, CategoryName),
                                Brand = COALESCE(@b, Brand),
                                Notes = CASE WHEN @notes IS NOT NULL AND LTRIM(RTRIM(@notes)) <> '' THEN @notes ELSE Notes END
                            WHERE ShortageID = @sid",
                            DbHelper.P("@stock", currentStock),
                            DbHelper.P("@min", minStockLimit),
                            DbHelper.P("@req", newReq),
                            DbHelper.P("@supId", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                            DbHelper.P("@supName", string.IsNullOrEmpty(supplierName) ? (object)DBNull.Value : supplierName),
                            DbHelper.P("@catId", categoryID.HasValue ? (object)categoryID.Value : DBNull.Value),
                            DbHelper.P("@catName", string.IsNullOrEmpty(categoryName) ? (object)DBNull.Value : categoryName),
                            DbHelper.P("@b", string.IsNullOrEmpty(brand) ? (object)DBNull.Value : brand),
                            DbHelper.P("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes),
                            DbHelper.P("@sid", sId));
                        return true;
                    }
                }

                // Insert New Shortage
                DbHelper.Execute(@"
                    INSERT INTO ShortageNotebook (ProductID, ProductName, CurrentStock, MinStockLimit, RequestedQty, SupplierID, SupplierName, CategoryID, CategoryName, Brand, Notes, Status, Source, CreatedBy)
                    VALUES (@pid, @pname, @stock, @min, @req, @supId, @supName, @catId, @catName, @b, @notes, @status, @source, @by)",
                    DbHelper.P("@pid", productID.HasValue && productID.Value > 0 ? (object)productID.Value : DBNull.Value),
                    DbHelper.P("@pname", productName),
                    DbHelper.P("@stock", currentStock),
                    DbHelper.P("@min", minStockLimit),
                    DbHelper.P("@req", requestedQty <= 0 ? 1 : requestedQty),
                    DbHelper.P("@supId", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@supName", string.IsNullOrEmpty(supplierName) ? (object)DBNull.Value : supplierName),
                    DbHelper.P("@catId", categoryID.HasValue ? (object)categoryID.Value : DBNull.Value),
                    DbHelper.P("@catName", string.IsNullOrEmpty(categoryName) ? (object)DBNull.Value : categoryName),
                    DbHelper.P("@b", string.IsNullOrEmpty(brand) ? (object)DBNull.Value : brand),
                    DbHelper.P("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes),
                    DbHelper.P("@status", string.IsNullOrEmpty(status) ? "جديد" : status),
                    DbHelper.P("@source", string.IsNullOrEmpty(source) ? "يدوي" : source),
                    DbHelper.P("@by", Session.EmpID));

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShortageDAL.AddOrUpdateShortage", ex);
                return false;
            }
        }

        public static bool UpdateRequestedQty(int shortageID, int? productID, decimal newQty)
        {
            EnsureTable();
            try
            {
                decimal qty = newQty <= 0 ? 1 : newQty;
                if (shortageID > 0)
                {
                    DbHelper.Execute("UPDATE ShortageNotebook SET RequestedQty = @q WHERE ShortageID = @sid",
                        DbHelper.P("@q", qty),
                        DbHelper.P("@sid", shortageID));
                    return true;
                }
                else if (productID.HasValue && productID.Value > 0)
                {
                    // Update if existing active shortage
                    var count = DbHelper.Scalar("SELECT COUNT(1) FROM ShortageNotebook WHERE ProductID = @pid AND Status IN (N'جديد', N'تم الطلب')",
                        DbHelper.P("@pid", productID.Value));

                    if (Convert.ToInt32(count) > 0)
                    {
                        DbHelper.Execute("UPDATE ShortageNotebook SET RequestedQty = @q WHERE ProductID = @pid AND Status IN (N'جديد', N'تم الطلب')",
                            DbHelper.P("@q", qty),
                            DbHelper.P("@pid", productID.Value));
                        return true;
                    }
                    else
                    {
                        return AddOrUpdateShortage(
                            productID: productID.Value,
                            productName: "",
                            requestedQty: qty,
                            currentStock: 0,
                            minStockLimit: 0,
                            notes: "تم تعديل الكمية المطلوبة يدوياً",
                            source: "كشكول النواقص",
                            status: "جديد"
                        );
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShortageDAL.UpdateRequestedQty", ex);
                return false;
            }
        }

        /// <summary>
        /// فحص الأصناف بعد المشتريات:
        /// إذا زاد رصيد الصنف عن حد الطلب (وكان رصيده > 0) يتم تحويل حالته في كشكول النواقص إلى "تم التوفير" لإزالته من النواقص النشطة تلقائياً
        /// </summary>
        public static void ProcessStockReplenishmentAfterPurchase(IEnumerable<int> productIDs)
        {
            EnsureTable();
            if (productIDs == null) return;

            HashSet<int> processed = new HashSet<int>();
            foreach (int pid in productIDs)
            {
                if (pid <= 0 || processed.Contains(pid)) continue;
                processed.Add(pid);

                try
                {
                    var dt = DbHelper.Query(@"
                        SELECT p.MinStockLimit, ISNULL(stk.TotalStock, 0) AS CurrentStock
                        FROM Products p
                        OUTER APPLY (
                            SELECT SUM(Quantity) AS TotalStock 
                            FROM ProductBatches 
                            WHERE ProductID = p.ProductID
                        ) stk
                        WHERE p.ProductID = @pid",
                        DbHelper.P("@pid", pid));

                    if (dt.Rows.Count > 0)
                    {
                        decimal currentStock = Convert.ToDecimal(dt.Rows[0]["CurrentStock"]);
                        decimal minLimit = dt.Rows[0]["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["MinStockLimit"]) : 0m;

                        if (currentStock > minLimit && currentStock > 0)
                        {
                            DbHelper.Execute(@"
                                UPDATE ShortageNotebook 
                                SET Status = N'تم التوفير',
                                    CurrentStock = @stock,
                                    Notes = N'تم توفير الصنف وتوريده بالمشتريات (رصيد حالي: ' + CAST(@stock AS NVARCHAR(20)) + N')'
                                WHERE ProductID = @pid AND Status IN (N'جديد', N'تم الطلب')",
                                DbHelper.P("@stock", currentStock),
                                DbHelper.P("@pid", pid));
                        }
                        else
                        {
                            DbHelper.Execute(@"
                                UPDATE ShortageNotebook
                                SET CurrentStock = @stock
                                WHERE ProductID = @pid AND Status IN (N'جديد', N'تم الطلب')",
                                DbHelper.P("@stock", currentStock),
                                DbHelper.P("@pid", pid));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ShortageDAL.ProcessStockReplenishmentAfterPurchase", ex);
                }
            }
        }

        /// <summary>
        /// فحص الأصناف بعد عمليات البيع أو الخصم:
        /// 1) الأصناف التي وصلت حد الطلب تدخل تلقائياً في كشكول النواقص
        /// 2) ترجع قائمة الأصناف التي وصل رصيدها إلى 0 لتنبيه المستخدم
        /// </summary>
        public static List<ZeroStockItemDTO> ProcessStockChangesAfterSale(IEnumerable<int> productIDs)
        {
            List<ZeroStockItemDTO> zeroStockItems = new List<ZeroStockItemDTO>();
            EnsureTable();

            if (productIDs == null) return zeroStockItems;

            HashSet<int> processed = new HashSet<int>();

            foreach (int pid in productIDs)
            {
                if (pid <= 0 || processed.Contains(pid)) continue;
                processed.Add(pid);

                try
                {
                    var dt = DbHelper.Query(@"
                        SELECT p.ProductID, p.ProductCode, p.ProductName, p.MinStockLimit, p.CategoryID, c.CategoryName,
                               COALESCE(p.Brand, p.ProducerCompany) AS Brand,
                               ISNULL(stk.TotalStock, 0) AS CurrentStock,
                               lastSup.SupplierID, lastSup.SupplierName
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        OUTER APPLY (
                            SELECT SUM(pb.Quantity) AS TotalStock
                            FROM ProductBatches pb
                            WHERE pb.ProductID = p.ProductID
                        ) stk
                        OUTER APPLY (
                            SELECT TOP 1 pu.SupplierID, sup.SupplierName
                            FROM PurchaseItems pi
                            INNER JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                            LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                            WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1
                            ORDER BY pu.PurchaseDate DESC, pu.PurchaseID DESC
                        ) lastSup
                        WHERE p.ProductID = @pid AND p.IsActive = 1",
                        DbHelper.P("@pid", pid));

                    if (dt.Rows.Count > 0)
                    {
                        var r = dt.Rows[0];
                        decimal currentStock = Convert.ToDecimal(r["CurrentStock"]);
                        decimal minStockLimit = r["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(r["MinStockLimit"]) : 0m;
                        string pName = r["ProductName"].ToString();
                        string pCode = r["ProductCode"] != DBNull.Value ? r["ProductCode"].ToString() : "";
                        int? catId = r["CategoryID"] != DBNull.Value ? (int?)Convert.ToInt32(r["CategoryID"]) : null;
                        string catName = r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : null;
                        int? supId = r["SupplierID"] != DBNull.Value ? (int?)Convert.ToInt32(r["SupplierID"]) : null;
                        string supName = r["SupplierName"] != DBNull.Value ? r["SupplierName"].ToString() : null;
                        string brand = r["Brand"] != DBNull.Value ? r["Brand"].ToString() : null;

                        // 1) إذا وصل أو نزل عن حد الطلب (وكان حد الطلب محدد > 0) -> يدخل نواقص تلقائي
                        if (minStockLimit > 0 && currentStock <= minStockLimit)
                        {
                            decimal deficit = minStockLimit - currentStock;
                            if (deficit <= 0) deficit = 1;

                            AddOrUpdateShortage(
                                productID: pid,
                                productName: pName,
                                requestedQty: deficit,
                                currentStock: currentStock,
                                minStockLimit: minStockLimit,
                                notes: currentStock <= 0 ? "نفد المخزون بالكامل (رصيد 0)" : "وصل إلى حد الطلب الأدنى",
                                source: currentStock <= 0 ? "آلي (نفاد المخزون)" : "آلي (حد الطلب)",
                                status: "جديد",
                                supplierID: supId,
                                supplierName: supName,
                                categoryID: catId,
                                categoryName: catName,
                                brand: brand
                            );
                        }

                        // 2) إذا وصل الرصيد إلى 0 أو أقل -> نجمعه للتنبيه التفاعلي
                        if (currentStock <= 0)
                        {
                            zeroStockItems.Add(new ZeroStockItemDTO
                            {
                                ProductID = pid,
                                ProductName = pName,
                                ProductCode = pCode,
                                CurrentStock = currentStock,
                                MinStockLimit = minStockLimit,
                                CategoryID = catId,
                                CategoryName = catName,
                                SupplierID = supId,
                                SupplierName = supName,
                                Brand = brand
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ShortageDAL.ProcessStockChangesAfterSale", ex);
                }
            }

            return zeroStockItems;
        }

        public static void PromptZeroStockDialog(Form parent, List<ZeroStockItemDTO> zeroItems)
        {
            if (zeroItems == null || zeroItems.Count == 0) return;

            try
            {
                if (zeroItems.Count == 1)
                {
                    var item = zeroItems[0];
                    string msg = $"⚠️ تنبيه نفاد المخزون:\n\nالصنف: [{item.ProductName}]\nأصبح رصيده في المخزن (0) بعد هذه العملية!\n\nهل ترغب في إضافته وتأكيده في كشكول النواقص لتوريده؟";
                    DialogResult res = MessageBox.Show(parent, msg, "نفاد المخزون (رصيد 0)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    if (res == DialogResult.Yes)
                    {
                        AddOrUpdateShortage(
                            productID: item.ProductID,
                            productName: item.ProductName,
                            requestedQty: item.MinStockLimit > 0 ? item.MinStockLimit : 1,
                            currentStock: item.CurrentStock,
                            minStockLimit: item.MinStockLimit,
                            notes: "تمت الإضافة بتأكيد المستخدم عند نفاد الرصيد (0)",
                            source: "آلي (رصيد صفر)",
                            status: "جديد",
                            supplierID: item.SupplierID,
                            supplierName: item.SupplierName,
                            categoryID: item.CategoryID,
                            categoryName: item.CategoryName,
                            brand: item.Brand
                        );
                    }
                }
                else
                {
                    string names = string.Join("\n • ", zeroItems.ConvertAll(x => x.ProductName));
                    string msg = $"⚠️ تنبيه نفاد المخزون:\n\nالأصناف التالية أصبحت رصيدها (0) في المخزن:\n • {names}\n\nهل ترغب في إضافة هذه الأصناف بالكامل إلى كشكول النواقص؟";
                    DialogResult res = MessageBox.Show(parent, msg, "أصناف نفدت من المخزون", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    if (res == DialogResult.Yes)
                    {
                        foreach (var item in zeroItems)
                        {
                            AddOrUpdateShortage(
                                productID: item.ProductID,
                                productName: item.ProductName,
                                requestedQty: item.MinStockLimit > 0 ? item.MinStockLimit : 1,
                                currentStock: item.CurrentStock,
                                minStockLimit: item.MinStockLimit,
                                notes: "تمت الإضافة بتأكيد المستخدم عند نفاد الرصيد (0)",
                                source: "آلي (رصيد صفر)",
                                status: "جديد",
                                supplierID: item.SupplierID,
                                supplierName: item.SupplierName,
                                categoryID: item.CategoryID,
                                categoryName: item.CategoryName,
                                brand: item.Brand
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShortageDAL.PromptZeroStockDialog", ex);
            }
        }

        public static DataTable GetComprehensiveShortages(
            string searchTerm = "",
            int? supplierID = null,
            int? categoryID = null,
            string brand = null,
            string stockCondition = "ALL", // "ALL", "BELOW_MIN", "ZERO_ONLY", "BETWEEN_ZERO_AND_MIN"
            string statusFilter = "الكل",
            int maxRows = 5000)
        {
            EnsureTable();
            List<SqlParameter> prms = new List<SqlParameter>();

            string sql = @"
                SELECT 
                    p.ProductID,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.CategoryID,
                    COALESCE(c.CategoryName, N'عام / غير محدد') AS CategoryName,
                    COALESCE(p.Brand, p.ProducerCompany, N'-') AS Brand,
                    p.MinStockLimit,
                    p.SalePrice,
                    p.PurchasePrice,
                    ISNULL(stk.TotalStock, 0) AS CurrentStock,
                    CASE 
                        WHEN sn.RequestedQty IS NOT NULL AND sn.RequestedQty > 0 THEN sn.RequestedQty
                        WHEN p.MinStockLimit > ISNULL(stk.TotalStock, 0) THEN (p.MinStockLimit - ISNULL(stk.TotalStock, 0))
                        ELSE 1.000 
                    END AS DeficitQty,
                    COALESCE(lastSup.SupplierID, sn.SupplierID) AS SupplierID,
                    COALESCE(lastSup.SupplierName, sn.SupplierName, N'---') AS SupplierName,
                    COALESCE(sn.Status, CASE WHEN ISNULL(stk.TotalStock, 0) <= 0 THEN N'جديد' ELSE N'تحت الحد' END) AS Status,
                    COALESCE(sn.Source, CASE WHEN ISNULL(stk.TotalStock, 0) <= 0 THEN N'آلي (رصيد صفر)' ELSE N'آلي (حد الطلب)' END) AS Source,
                    COALESCE(sn.Notes, N'-') AS Notes,
                    COALESCE(sn.CreatedDate, GETDATE()) AS CreatedDate,
                    COALESCE(sn.ShortageID, 0) AS ShortageID
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                OUTER APPLY (
                    SELECT SUM(pb.Quantity) AS TotalStock
                    FROM ProductBatches pb
                    WHERE pb.ProductID = p.ProductID
                ) stk
                OUTER APPLY (
                    SELECT TOP 1 pu.SupplierID, sup.SupplierName
                    FROM PurchaseItems pi
                    INNER JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                    LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                    WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1
                    ORDER BY pu.PurchaseDate DESC, pu.PurchaseID DESC
                ) lastSup
                LEFT JOIN (
                    SELECT s1.ProductID, s1.SupplierID, s1.SupplierName, s1.Status, s1.Source, s1.Notes, s1.CreatedDate, s1.ShortageID, s1.RequestedQty
                    FROM ShortageNotebook s1
                    INNER JOIN (
                        SELECT ProductID, MAX(ShortageID) AS MaxID
                        FROM ShortageNotebook
                        WHERE ProductID IS NOT NULL
                        GROUP BY ProductID
                    ) s2 ON s1.ShortageID = s2.MaxID
                ) sn ON p.ProductID = sn.ProductID
                WHERE p.IsActive = 1 ";

            // شروط نوع النواقص والمخزون
            if (stockCondition == "ZERO_ONLY")
            {
                sql += " AND ISNULL(stk.TotalStock, 0) <= 0 ";
            }
            else if (stockCondition == "BELOW_MIN")
            {
                sql += " AND ( (p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit) OR ISNULL(stk.TotalStock, 0) <= 0 ) ";
            }
            else if (stockCondition == "BETWEEN_ZERO_AND_MIN")
            {
                sql += " AND ( p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit ) ";
            }
            else
            {
                // ALL: إما تحت حد الطلب أو رصيد صفر أو مسجل في كشكول النواقص
                sql += " AND ( (p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit) OR ISNULL(stk.TotalStock, 0) <= 0 OR sn.ShortageID IS NOT NULL ) ";
            }

            // فلتر البحث النصي
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term OR p.PartNumber LIKE @term OR p.Brand LIKE @term OR p.ProducerCompany LIKE @term OR sn.Notes LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + searchTerm.Trim() + "%"));
            }

            // فلتر المورد
            if (supplierID.HasValue && supplierID.Value > 0)
            {
                sql += " AND (lastSup.SupplierID = @supId OR sn.SupplierID = @supId) ";
                prms.Add(DbHelper.P("@supId", supplierID.Value));
            }

            // فلتر التصنيف / القسم
            if (categoryID.HasValue && categoryID.Value > 0)
            {
                sql += " AND p.CategoryID = @catId ";
                prms.Add(DbHelper.P("@catId", categoryID.Value));
            }

            // فلتر الشركة المنتجة / الماركة
            if (!string.IsNullOrWhiteSpace(brand) && brand != "-- كل الشركات / الماركات --")
            {
                sql += " AND (p.Brand = @brand OR p.ProducerCompany = @brand) ";
                prms.Add(DbHelper.P("@brand", brand.Trim()));
            }

            // فلتر الحالة
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "الكل")
            {
                sql += " AND (sn.Status = @st OR (@st = N'جديد' AND sn.Status IS NULL)) ";
                prms.Add(DbHelper.P("@st", statusFilter));
            }

            sql += " ORDER BY ISNULL(stk.TotalStock, 0) ASC, p.ProductName ASC ";

            return DbHelper.Query(sql, prms.ToArray());
        }

        public static List<string> GetAvailableBrands()
        {
            List<string> list = new List<string>();
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT DISTINCT LTRIM(RTRIM(Brand)) AS Brand
                    FROM Products
                    WHERE Brand IS NOT NULL AND LTRIM(RTRIM(Brand)) <> ''
                    UNION
                    SELECT DISTINCT LTRIM(RTRIM(ProducerCompany)) AS Brand
                    FROM Products
                    WHERE ProducerCompany IS NOT NULL AND LTRIM(RTRIM(ProducerCompany)) <> ''
                    ORDER BY Brand");

                foreach (DataRow r in dt.Rows)
                {
                    string b = r["Brand"].ToString();
                    if (!string.IsNullOrWhiteSpace(b) && !list.Contains(b))
                    {
                        list.Add(b);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShortageDAL.GetAvailableBrands", ex);
            }
            return list;
        }
    }
}
