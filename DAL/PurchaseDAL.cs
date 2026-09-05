using System;
using System.Data;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    /// <summary>بيانات صنف في فاتورة مشتريات — TotalPrice محسوب تلقائياً</summary>
    public class PurchaseItemDTO
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        /// <summary>نسبة خصم الصنف % (0 = لا خصم)</summary>
        public decimal DiscountPct { get; set; } = 0m;
        /// <summary>قيمة خصم الصنف — تُستخدم فقط إذا كانت DiscountPct = 0</summary>
        public decimal DiscountAmt { get; set; } = 0m;
        /// <summary>سعر البيع المقترح</summary>
        public decimal? SuggestedSalePrice { get; set; } = null;
        public string UnitName { get; set; } = null;
        public decimal Factor { get; set; } = 1.0m;
        public DateTime? ExpiryDate { get; set; } = null;
        public string IMEI { get; set; } = "";

        /// <summary>صافي سعر شراء الوحدة بعد الخصم (تكلفة الوحدة الصافية)</summary>
        public decimal NetUnitPrice
        {
            get
            {
                if (Quantity > 0)
                    return Math.Round(TotalPrice / Quantity, 4);
                return UnitPrice;
            }
        }

        /// <summary>صافي قيمة الصنف بعد خصم الصنف</summary>
        public decimal TotalPrice
        {
            get
            {
                decimal gross = Quantity * UnitPrice;
                if (DiscountAmt > 0m)
                    return Math.Round(Math.Max(0m, gross - DiscountAmt), 2);
                return Math.Round(gross, 2);
            }
        }
    }

    public static class PurchaseDAL
    {
        static PurchaseDAL()
        {
            try
            {
                DbHelper.EnsurePurchaseColumnsExist();
            }
            catch { }
        }

        // ─── قراءة الفواتير المؤكدة ──────────────────────────────────────────────
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return GetAll(from, to, null, null);
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? supplierID, string productSearch)
        {
            string productFilter = string.IsNullOrWhiteSpace(productSearch) ? null : productSearch.Trim();
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, ISNULL(p.SupplierInvoiceNo, N'') AS SupplierInvoiceNo, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, ISNULL(c.ClientName, N'---')) AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID, p.ClientID, p.PurchaseSource,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct,   0) AS DiscountPct,
                         COALESCE(p.TaxPct,        0) AS TaxPct,
                         COALESCE(p.TaxAmount,     0) AS TaxAmount,
                         COALESCE(p.ShippingCost,  0) AS ShippingCost,
                         ISNULL(p.ShippingOn, N'Company') AS ShippingOn,
                         ISNULL((
                             SELECT SUM(pri.Quantity * pri.UnitPrice)
                             FROM PurchaseReturnItems pri
                             JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                             WHERE pr.PurchaseID = p.PurchaseID
                         ), 0) AS ReturnAmount,
                         ISNULL((
                             SELECT SUM(pi.Quantity * pi.UnitPrice)
                             FROM PurchaseItems pi
                             WHERE pi.PurchaseID = p.PurchaseID
                         ), p.TotalAmount + COALESCE(p.DiscountAmount, 0)) AS SubTotal
                  FROM Purchases p
                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                  LEFT JOIN Clients c ON p.ClientID = c.ClientID
                  WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                    AND p.IsPosted = 1
                    AND (@supplierID IS NULL OR p.SupplierID = @supplierID OR p.ClientID = @supplierID)
                    AND (@product IS NULL OR (
                        p.PurchaseCode LIKE N'%' + @product + N'%' OR
                        p.SupplierInvoiceNo LIKE N'%' + @product + N'%' OR
                        EXISTS (
                            SELECT 1 FROM PurchaseItems pi2
                            JOIN Products pr ON pi2.ProductID = pr.ProductID
                            WHERE pi2.PurchaseID = p.PurchaseID
                              AND (pr.ProductName LIKE N'%' + @product + N'%'
                                OR pr.ProductCode LIKE N'%' + @product + N'%')
                        )
                    ))
                  ORDER BY p.PurchaseDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@supplierID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                DbHelper.P("@product", (object)productFilter ?? DBNull.Value));
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? warehouseID)
        {
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, ISNULL(p.SupplierInvoiceNo, N'') AS SupplierInvoiceNo, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct,   0) AS DiscountPct,
                         COALESCE(p.TaxPct,        0) AS TaxPct,
                         COALESCE(p.TaxAmount,     0) AS TaxAmount,
                         COALESCE(p.ShippingCost,  0) AS ShippingCost,
                         ISNULL(p.ShippingOn, N'Company') AS ShippingOn,
                         ISNULL((
                             SELECT SUM(pi.Quantity * pi.UnitPrice)
                             FROM PurchaseItems pi
                             WHERE pi.PurchaseID = p.PurchaseID
                         ), p.TotalAmount + COALESCE(p.DiscountAmount, 0)) AS SubTotal,
                         w.WarehouseName
                  FROM Purchases p
                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                  LEFT JOIN Warehouses w ON p.WarehouseID = w.WarehouseID
                  WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                    AND p.IsPosted = 1
                    AND (@warehouseID IS NULL OR p.WarehouseID = @warehouseID)
                  ORDER BY p.PurchaseDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        // ─── أصناف فاتورة معينة ──────────────────────────────────────────────────
        public static DataTable GetItems(int purchaseID)
        {
             return DbHelper.Query(
                 @"SELECT pi.ProductID, pr.ProductCode, pr.ProductName, pi.Quantity, pi.UnitPrice, pi.TotalPrice,
                           COALESCE(pi.DiscountPct, 0) AS DiscountPct,
                           COALESCE(pi.DiscountAmt, 0) AS DiscountAmt,
                           pi.SuggestedSalePrice,
                           pi.UnitName, COALESCE(pi.Factor, 1.0) AS Factor, pi.ExpiryDate
                    FROM PurchaseItems pi
                    JOIN Products pr ON pi.ProductID = pr.ProductID
                    WHERE pi.PurchaseID = @id",
                DbHelper.P("@id", purchaseID));
        }

        // ─── الفواتير المعلقة (مسودات) ───────────────────────────────────────────
        public static DataTable GetDraftPurchases()
        {
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, ISNULL(p.SupplierInvoiceNo, N'') AS SupplierInvoiceNo, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID, p.WarehouseID,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct,   0) AS DiscountPct,
                         COALESCE(p.TaxPct,        0) AS TaxPct,
                         COALESCE(p.TaxAmount,     0) AS TaxAmount,
                         COALESCE(p.ShippingCost,  0) AS ShippingCost,
                         ISNULL(p.ShippingOn, N'Company') AS ShippingOn
                  FROM Purchases p
                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                  WHERE p.IsPosted = 0
                  ORDER BY p.PurchaseDate DESC");
        }

        // ─── حذف مسودة فاتورة مشتريات ───────────────────────────────────────────
        public static void DeleteDraftPurchase(int purchaseID)
        {
            DbHelper.Execute(
                "DELETE FROM Purchases WHERE PurchaseID = @id AND IsPosted = 0",
                DbHelper.P("@id", purchaseID));
        }

        // ─── حفظ فاتورة مشتريات (جديدة أو مسودة) ────────────────────────────────
        /// <param name="isDraft">true = تعليق (مسودة)، false = حفظ نهائي</param>
        public static int SavePurchase(
            string purchaseType, int? supplierID, decimal total, string notes,
            List<PurchaseItemDTO> items,
            decimal discountAmount = 0m, decimal discountPct = 0m,
            decimal taxPct = 0m, decimal taxAmount = 0m,
            bool isDraft = false, int? warehouseID = 1,
            string supplierInvoiceNo = "",
            decimal shippingCost = 0m, string shippingOn = "Company",
            int? clientID = null, string purchaseSource = "Supplier")
        {
            int returnedID = -1;
            DbHelper.EnsurePurchaseColumnsExist();

            DbHelper.RunInTransaction((con, trans) =>
            {
                var nextResult = DbHelper.ScalarTrans(trans,
                    "SELECT COALESCE(MAX(PurchaseID), 0) + 1 FROM Purchases");
                string code = nextResult != null ? nextResult.ToString() : "1";

                // فحص رصيد الخزنة للمشتريات النقدية المؤكدة فقط ومنع الرصيد السالب
                if (purchaseType == "Cash" && !isDraft)
                {
                    int accId = Session.GetDefaultSafeID();
                    AccountDAL.EnsureSufficientCashTrans(trans, accId, total, "سداد فاتورة مشتريات نقدية");
                }

                int purchaseID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO Purchases
                        (PurchaseCode, SupplierInvoiceNo, PurchaseDate, PurchaseType, SupplierID, ClientID, PurchaseSource,
                         TotalAmount, DiscountAmount, DiscountPct, TaxPct, TaxAmount,
                         Notes, CreatedBy, IsPosted, WarehouseID, ShippingCost, ShippingOn)
                      VALUES
                        (@code, @sinv, @dt, @typ, @sid, @cid, @psrc,
                         @tot, @discAmt, @discPct, @taxPct, @taxAmt,
                         @n, @by, @ip, @wid, @shdost, @shon)",
                    DbHelper.P("@code",    code),
                    DbHelper.P("@sinv",    (object)supplierInvoiceNo ?? DBNull.Value),
                    DbHelper.P("@dt",      DateTime.Now),
                    DbHelper.P("@typ",     purchaseType),
                    DbHelper.P("@sid",     supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@cid",     clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@psrc",    purchaseSource ?? "Supplier"),
                    DbHelper.P("@tot",     total),
                    DbHelper.P("@discAmt", discountAmount),
                    DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@taxPct",  taxPct),
                    DbHelper.P("@taxAmt",  taxAmount),
                    DbHelper.P("@n",       notes),
                    DbHelper.P("@by",      Session.EmpID),
                    DbHelper.P("@ip",      isDraft ? 0 : 1),
                    DbHelper.P("@wid",     warehouseID.HasValue ? (object)warehouseID.Value : 1),
                    DbHelper.P("@shdost",  shippingCost),
                    DbHelper.P("@shon",    shippingOn ?? "Company"));

                if (purchaseID <= 0)
                    throw new Exception("فشل في استخراج رقم فاتورة المشتريات الجديدة.");

                returnedID = purchaseID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO PurchaseItems
                            (PurchaseID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, SuggestedSalePrice, UnitName, Factor, ExpiryDate, IMEI)
                          VALUES (@pid, @prodid, @qty, @up, @tp, @dpct, @damt, @ssp, @un, @fac, @exp, @imei)",
                        DbHelper.P("@pid",    purchaseID),
                        DbHelper.P("@prodid", item.ProductID),
                        DbHelper.P("@qty",    item.Quantity),
                        DbHelper.P("@up",     item.UnitPrice),
                        DbHelper.P("@tp",     item.TotalPrice),
                        DbHelper.P("@dpct",   item.DiscountPct),
                        DbHelper.P("@damt",   item.DiscountAmt),
                        DbHelper.P("@ssp",    item.SuggestedSalePrice.HasValue ? (object)item.SuggestedSalePrice.Value : DBNull.Value),
                        DbHelper.P("@un",     item.UnitName),
                        DbHelper.P("@fac",    item.Factor),
                        DbHelper.P("@exp",    item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value.Date : DBNull.Value),
                        DbHelper.P("@imei",   string.IsNullOrWhiteSpace(item.IMEI) ? DBNull.Value : (object)item.IMEI.Trim()));

                    if (!isDraft)
                    {
                        if (item.UnitPrice > 0)
                        {
                            decimal costPerBaseUnit = item.UnitPrice / (item.Factor > 0 ? item.Factor : 1.0m);
                            DbHelper.ExecuteTrans(trans,
                                @"UPDATE Products 
                                  SET PurchasePrice = @pp, 
                                      CostPrice = @cp 
                                  WHERE ProductID = @pid",
                                DbHelper.P("@pp", item.UnitPrice),
                                DbHelper.P("@cp", costPerBaseUnit),
                                DbHelper.P("@pid", item.ProductID));
                        }

                        if (item.ExpiryDate.HasValue)
                    {
                        decimal factor = item.Factor > 0 ? item.Factor : 1.0m;
                        decimal baseQty = item.Quantity * factor;
                        int wid = warehouseID ?? 1;

                        var existingBatchId = DbHelper.ScalarTrans(trans,
                            "SELECT BatchID FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid AND ExpiryDate=@exp",
                            DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@wid", wid),
                            DbHelper.P("@exp", item.ExpiryDate.Value.Date));

                        if (existingBatchId != null && existingBatchId != DBNull.Value)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET Quantity = Quantity + @qty WHERE BatchID = @bid",
                                DbHelper.P("@qty", baseQty),
                                DbHelper.P("@bid", Convert.ToInt32(existingBatchId)));
                        }
                        else
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ProductBatches(ProductID, WarehouseID, Quantity, ExpiryDate, PurchaseID) VALUES (@pid, @wid, @qty, @exp, @purid)",
                                DbHelper.P("@pid", item.ProductID),
                                DbHelper.P("@wid", wid),
                                DbHelper.P("@qty", baseQty),
                                DbHelper.P("@exp", item.ExpiryDate.Value.Date),
                                DbHelper.P("@purid", purchaseID));
                        }
                    }
                }
            }

                // ── القيود المحاسبية (للفواتير المؤكدة فقط) ─────────────────────
                if (!isDraft)
                {
                    if (purchaseSource == "Client" || clientID.HasValue)
                    {
                        if (purchaseType == "Credit" && clientID.HasValue)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID, TransType, Credit, RefID, Notes, CreatedBy)" +
                                " VALUES(@cid, 'ClientPurchase', @amt, @ref, @n, @by)",
                                DbHelper.P("@cid", clientID.Value),
                                DbHelper.P("@amt", total),
                                DbHelper.P("@ref", purchaseID),
                                DbHelper.P("@n",   "فاتورة شراء من عميل آجل " + code),
                                DbHelper.P("@by",  Session.EmpID));
                        }
                        else if (purchaseType == "Cash")
                        {
                            int accId = Session.GetDefaultSafeID();
                            AccountDAL.EnsureSufficientCashTrans(trans, accId, total, "فاتورة شراء نقدي من عميل");

                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO CashBox(TransDate, TransType, AmountOut, RefID, Notes, CreatedBy, AccountID)" +
                                " VALUES(@dt, 'ClientPurchaseCash', @amt, @ref, @n, @by, @accId)",
                                DbHelper.P("@dt",  DateTime.Now),
                                DbHelper.P("@amt", total),
                                DbHelper.P("@ref", purchaseID),
                                DbHelper.P("@n",   "فاتورة شراء من عميل نقدي " + code),
                                DbHelper.P("@by",  Session.EmpID),
                                DbHelper.P("@accId", accId));
                        }
                    }
                    else
                    {
                        // آجل → أضف دائناً في حساب المورد
                        if (purchaseType == "Credit" && supplierID.HasValue)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO SupplierTransactions(SupplierID,TransType,Credit,RefID,Notes,CreatedBy)" +
                                " VALUES(@sid,'Purchase',@amt,@ref,@n,@by)",
                                DbHelper.P("@sid", supplierID.Value),
                                DbHelper.P("@amt", total),
                                DbHelper.P("@ref", purchaseID),
                                DbHelper.P("@n",   "فاتورة مشتريات " + code),
                                DbHelper.P("@by",  Session.EmpID));
                        }

                        // نقدي → اخصم من الخزنة
                        if (purchaseType == "Cash")
                        {
                            int accId = Session.GetDefaultSafeID();
                            AccountDAL.EnsureSufficientCashTrans(trans, accId, total, "فاتورة مشتريات نقدية");

                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy,AccountID)" +
                                " VALUES(@dt,'PurchaseExpense',@amt,@ref,@n,@by,@accId)",
                                DbHelper.P("@dt",  DateTime.Now),
                                DbHelper.P("@amt", total),
                                DbHelper.P("@ref", purchaseID),
                                DbHelper.P("@n",   "مشتريات نقدية " + code),
                                DbHelper.P("@by",  Session.EmpID),
                                DbHelper.P("@accId", accId));
                        }
                    }
                }
            });

            if (returnedID > 0 && !isDraft)
            {
                try
                {
                    List<int> purchasedPids = items != null ? items.ConvertAll(x => x.ProductID) : new List<int>();
                    ShortageDAL.ProcessStockReplenishmentAfterPurchase(purchasedPids);
                }
                catch { }
            }

            return returnedID;
        }

        public static bool CanDeletePurchase(int purchaseID, out string reason)
        {
            reason = "";
            var returnsCount = DbHelper.Scalar("SELECT COUNT(*) FROM PurchaseReturns WHERE PurchaseID = @id", DbHelper.P("@id", purchaseID));
            if (Convert.ToInt32(returnsCount) > 0)
            {
                reason = "لا يمكن حذف أو تعديل الفاتورة لوجود مرتجع مشتريات مرتبط بها.";
                return false;
            }

            var dtItems = PurchaseDAL.GetItems(purchaseID);
            foreach (DataRow iRow in dtItems.Rows)
            {
                int productID = Convert.ToInt32(iRow["ProductID"]);
                decimal qty = Convert.ToDecimal(iRow["Quantity"]);
                decimal factor = Convert.ToDecimal(iRow["Factor"]);
                decimal baseQty = qty * factor;
                DateTime? expDate = iRow["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(iRow["ExpiryDate"]);

                if (expDate.HasValue)
                {
                    var batchQtyObj = DbHelper.Scalar("SELECT Quantity FROM ProductBatches WHERE ProductID=@pid AND ExpiryDate=@exp",
                        DbHelper.P("@pid", productID), DbHelper.P("@exp", expDate.Value.Date));
                    decimal currentBatchQty = batchQtyObj != null && batchQtyObj != DBNull.Value ? Convert.ToDecimal(batchQtyObj) : 0m;
                    if (currentBatchQty < baseQty)
                    {
                        reason = $"لا يمكن حذف أو تعديل الفاتورة لأن الصنف \"{iRow["ProductName"]}\" تم بيع أجزاء منه (الكمية المتبقية بالصلاحية {currentBatchQty} أقل من المشتراة {baseQty}).";
                        return false;
                    }
                }
                else
                {
                    decimal currentStock = InventoryDAL.GetProductStock(productID);
                    if (currentStock < baseQty)
                    {
                        reason = $"لا يمكن حذف أو تعديل الفاتورة لأن الصنف \"{iRow["ProductName"]}\" تم بيع أجزاء منه (الرصيد الحالي {currentStock} أقل من المشتراة {baseQty}).";
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool DeletePurchase(int purchaseID)
        {
            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    var dtPurchase = DbHelper.QueryTrans(trans, "SELECT PurchaseType, TotalAmount, SupplierID, IsPosted, WarehouseID FROM Purchases WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                    if (dtPurchase.Rows.Count == 0) throw new Exception("الفاتورة غير موجودة.");
                    var pRow = dtPurchase.Rows[0];
                    bool isPosted = Convert.ToInt32(pRow["IsPosted"]) == 1;
                    string type = pRow["PurchaseType"].ToString();
                    decimal total = Convert.ToDecimal(pRow["TotalAmount"]);
                    int? supplierID = pRow["SupplierID"] == DBNull.Value ? (int?)null : Convert.ToInt32(pRow["SupplierID"]);
                    int wid = pRow["WarehouseID"] == DBNull.Value ? 1 : Convert.ToInt32(pRow["WarehouseID"]);

                    if (isPosted)
                    {
                        var dtItems = DbHelper.QueryTrans(trans, "SELECT ProductID, Quantity, Factor, ExpiryDate FROM PurchaseItems WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                        foreach (DataRow iRow in dtItems.Rows)
                        {
                            int productID = Convert.ToInt32(iRow["ProductID"]);
                            decimal qty = Convert.ToDecimal(iRow["Quantity"]);
                            decimal factor = Convert.ToDecimal(iRow["Factor"]);
                            decimal baseQty = qty * factor;
                            DateTime? expDate = iRow["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(iRow["ExpiryDate"]);

                            if (expDate.HasValue)
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE ProductID=@pid AND WarehouseID=@wid AND ExpiryDate=@exp",
                                    DbHelper.P("@q", baseQty), DbHelper.P("@pid", productID), DbHelper.P("@wid", wid), DbHelper.P("@exp", expDate.Value.Date));
                            }
                        }
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SupplierTransactions WHERE RefID=@id AND TransType='Purchase'", DbHelper.P("@id", purchaseID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM ClientTransactions WHERE RefID=@id AND TransType='ClientPurchase'", DbHelper.P("@id", purchaseID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM CashBox WHERE RefID=@id AND (TransType='PurchaseExpense' OR TransType='ClientPurchaseCash')", DbHelper.P("@id", purchaseID));
                    }
                    DbHelper.ExecuteTrans(trans, "DELETE FROM PurchaseItems WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                    DbHelper.ExecuteTrans(trans, "DELETE FROM Purchases WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                });
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting purchase {purchaseID}", ex);
                return false;
            }
        }

        public static bool UpdatePurchase(int purchaseID, string purchaseType, int? supplierID, decimal total, string notes, List<PurchaseItemDTO> items,
            decimal discountAmount, decimal discountPct, decimal taxPct, decimal taxAmt, int? warehouseID, string supplierInvoiceNo = "",
            decimal shippingCost = 0m, string shippingOn = "Company")
        {
            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    var dtOldItems = DbHelper.QueryTrans(trans, "SELECT ProductID, Quantity, Factor, ExpiryDate FROM PurchaseItems WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                    int wid = warehouseID ?? 1;
                    foreach (DataRow iRow in dtOldItems.Rows)
                    {
                        int productID = Convert.ToInt32(iRow["ProductID"]);
                        decimal qty = Convert.ToDecimal(iRow["Quantity"]);
                        decimal factor = Convert.ToDecimal(iRow["Factor"]);
                        decimal baseQty = qty * factor;
                        DateTime? expDate = iRow["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(iRow["ExpiryDate"]);

                        if (expDate.HasValue)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE ProductID=@pid AND WarehouseID=@wid AND ExpiryDate=@exp",
                                DbHelper.P("@q", baseQty), DbHelper.P("@pid", productID), DbHelper.P("@wid", wid), DbHelper.P("@exp", expDate.Value.Date));
                        }
                    }

                    DbHelper.ExecuteTrans(trans, "DELETE FROM PurchaseItems WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                    DbHelper.ExecuteTrans(trans, "DELETE FROM SupplierTransactions WHERE RefID=@id AND TransType='Purchase'", DbHelper.P("@id", purchaseID));
                    DbHelper.ExecuteTrans(trans, "DELETE FROM CashBox WHERE RefID=@id AND TransType='PurchaseExpense'", DbHelper.P("@id", purchaseID));

                    DbHelper.ExecuteTrans(trans,
                        @"UPDATE Purchases 
                          SET PurchaseType=@typ, SupplierID=@sid, TotalAmount=@tot, Notes=@n, 
                              DiscountAmount=@discAmt, DiscountPct=@discPct, TaxPct=@taxPct, TaxAmount=@taxAmt,
                              WarehouseID=@wid, SupplierInvoiceNo=@sinv,
                              ShippingCost=@shdost, ShippingOn=@shon
                          WHERE PurchaseID=@id",
                        DbHelper.P("@typ",     purchaseType),
                        DbHelper.P("@sid",     supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                        DbHelper.P("@tot",     total),
                        DbHelper.P("@n",       notes),
                        DbHelper.P("@discAmt", discountAmount),
                        DbHelper.P("@discPct", discountPct),
                        DbHelper.P("@taxPct",  taxPct),
                        DbHelper.P("@taxAmt",  taxAmt),
                        DbHelper.P("@wid",     wid),
                        DbHelper.P("@sinv",    (object)supplierInvoiceNo ?? DBNull.Value),
                        DbHelper.P("@shdost",  shippingCost),
                        DbHelper.P("@shon",    shippingOn ?? "Company"),
                        DbHelper.P("@id",      purchaseID));

                    foreach (var item in items)
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO PurchaseItems
                                (PurchaseID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, SuggestedSalePrice, UnitName, Factor, ExpiryDate)
                              VALUES (@pid, @prodid, @qty, @up, @tp, @dpct, @damt, @ssp, @un, @fac, @exp)",
                            DbHelper.P("@pid",    purchaseID),
                            DbHelper.P("@prodid", item.ProductID),
                            DbHelper.P("@qty",    item.Quantity),
                            DbHelper.P("@up",     item.UnitPrice),
                            DbHelper.P("@tp",     item.TotalPrice),
                            DbHelper.P("@dpct",   item.DiscountPct),
                            DbHelper.P("@damt",   item.DiscountAmt),
                            DbHelper.P("@ssp",    item.SuggestedSalePrice.HasValue ? (object)item.SuggestedSalePrice.Value : DBNull.Value),
                            DbHelper.P("@un",     item.UnitName),
                            DbHelper.P("@fac",    item.Factor),
                            DbHelper.P("@exp",    item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value.Date : DBNull.Value));

                        if (item.ExpiryDate.HasValue)
                        {
                            decimal factor = item.Factor > 0 ? item.Factor : 1.0m;
                            decimal baseQty = item.Quantity * factor;

                            var existingBatchId = DbHelper.ScalarTrans(trans,
                                "SELECT BatchID FROM ProductBatches WHERE ProductID=@pid AND WarehouseID=@wid AND ExpiryDate=@exp",
                                DbHelper.P("@pid", item.ProductID),
                                DbHelper.P("@wid", wid),
                                DbHelper.P("@exp", item.ExpiryDate.Value.Date));

                            if (existingBatchId != null && existingBatchId != DBNull.Value)
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "UPDATE ProductBatches SET Quantity = Quantity + @qty WHERE BatchID = @bid",
                                    DbHelper.P("@qty", baseQty),
                                    DbHelper.P("@bid", Convert.ToInt32(existingBatchId)));
                            }
                            else
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO ProductBatches(ProductID, WarehouseID, Quantity, ExpiryDate, PurchaseID) VALUES (@pid, @wid, @qty, @exp, @purid)",
                                    DbHelper.P("@pid", item.ProductID),
                                    DbHelper.P("@wid", wid),
                                    DbHelper.P("@qty", baseQty),
                                    DbHelper.P("@exp", item.ExpiryDate.Value.Date),
                                    DbHelper.P("@purid", purchaseID));
                            }
                        }
                    }

                    var pCodeObj = DbHelper.ScalarTrans(trans, "SELECT PurchaseCode FROM Purchases WHERE PurchaseID=@id", DbHelper.P("@id", purchaseID));
                    string code = pCodeObj?.ToString() ?? purchaseID.ToString();

                    if (purchaseType == "Credit" && supplierID.HasValue)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO SupplierTransactions(SupplierID,TransType,Credit,RefID,Notes,CreatedBy)" +
                            " VALUES(@sid,'Purchase',@amt,@ref,@n,@by)",
                            DbHelper.P("@sid", supplierID.Value),
                            DbHelper.P("@amt", total),
                            DbHelper.P("@ref", purchaseID),
                            DbHelper.P("@n",   "تعديل فاتورة مشتريات " + code),
                            DbHelper.P("@by",  Session.EmpID));
                    }

                    if (purchaseType == "Cash")
                    {
                        int accId = Session.GetDefaultSafeID();
                        AccountDAL.EnsureSufficientCashTrans(trans, accId, total, "تعديل فاتورة مشتريات نقدية");

                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy,AccountID)" +
                            " VALUES(GETDATE(),'PurchaseExpense',@amt,@ref,@n,@by,@accId)",
                            DbHelper.P("@amt", total),
                            DbHelper.P("@ref", purchaseID),
                            DbHelper.P("@n",   "تعديل مشتريات نقدية " + code),
                            DbHelper.P("@by",  Session.EmpID),
                            DbHelper.P("@accId", accId));
                    }
                });

                try
                {
                    List<int> purchasedPids = items != null ? items.ConvertAll(x => x.ProductID) : new List<int>();
                    ShortageDAL.ProcessStockReplenishmentAfterPurchase(purchasedPids);
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating purchase {purchaseID}", ex);
                return false;
            }
        }

        public static List<string> GetAvailableSerialsForProduct(int productID)
        {
            var list = new List<string>();
            if (productID <= 0) return list;

            string sql = @"
                SELECT DISTINCT LTRIM(RTRIM(pItem.IMEI)) AS IMEI
                FROM PurchaseItems pItem
                JOIN Purchases p ON pItem.PurchaseID = p.PurchaseID
                WHERE pItem.ProductID = @pid 
                  AND pItem.IMEI IS NOT NULL 
                  AND LTRIM(RTRIM(pItem.IMEI)) <> '' 
                  AND p.IsPosted = 1
                  AND LTRIM(RTRIM(pItem.IMEI)) NOT IN (
                      SELECT LTRIM(RTRIM(sItem.IMEI))
                      FROM SaleItems sItem
                      JOIN Sales s ON sItem.SaleID = s.SaleID
                      WHERE sItem.ProductID = @pid 
                        AND sItem.IMEI IS NOT NULL 
                        AND LTRIM(RTRIM(sItem.IMEI)) <> '' 
                        AND s.IsPosted = 1
                  )
                ORDER BY IMEI ASC";

            DataTable dt = DbHelper.Query(sql, DbHelper.P("@pid", productID));
            foreach (DataRow r in dt.Rows)
            {
                string imei = r["IMEI"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(imei) && !list.Contains(imei))
                {
                    list.Add(imei);
                }
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // مجموعة تقارير المشتريات الشاملة (11 تقرير تفصيلي متكامل)
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>1. تقرير المشتريات اليومية الشامل</summary>
        public static DataTable GetDailyPurchasesSummary(DateTime from, DateTime to, int? supplierID = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    CAST(p.PurchaseDate AS DATE) AS PurchaseDay,
                    COUNT(DISTINCT p.PurchaseID) AS InvoiceCount,
                    ISNULL(SUM(p.TotalAmount + ISNULL(p.DiscountAmount, 0) - ISNULL(p.TaxAmount, 0) - ISNULL(p.ShippingCost, 0)), 0) AS GrossPurchases,
                    ISNULL(SUM(p.DiscountAmount), 0) AS TotalDiscounts,
                    ISNULL(SUM(p.TaxAmount), 0) AS TotalTax,
                    ISNULL(SUM(p.ShippingCost), 0) AS TotalShipping,
                    ISNULL(SUM(p.TotalAmount), 0) AS TotalPurchases,
                    ISNULL((SELECT SUM(pr.TotalAmount) FROM PurchaseReturns pr WHERE CAST(pr.ReturnDate AS DATE) = CAST(p.PurchaseDate AS DATE) AND (@supID IS NULL OR pr.SupplierID = @supID)), 0) AS TotalReturns,
                    (ISNULL(SUM(p.TotalAmount), 0) - ISNULL((SELECT SUM(pr.TotalAmount) FROM PurchaseReturns pr WHERE CAST(pr.ReturnDate AS DATE) = CAST(p.PurchaseDate AS DATE) AND (@supID IS NULL OR pr.SupplierID = @supID)), 0)) AS NetPurchases
                FROM Purchases p
                WHERE p.IsPosted = 1
                  AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                  AND (@supID IS NULL OR p.SupplierID = @supID OR p.ClientID = @supID)
                GROUP BY CAST(p.PurchaseDate AS DATE)
                ORDER BY PurchaseDay DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value));
        }

        /// <summary>2. تقرير المشتريات خلال فترة (يومي / أسبوعي / شهري ومقارنة الفترات)</summary>
        public static DataTable GetPurchasesByPeriod(DateTime from, DateTime to, string periodType = "Daily", int? supplierID = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    CASE 
                        WHEN @pType = 'Monthly' THEN SUBSTRING(CONVERT(VARCHAR(10), p.PurchaseDate, 120), 1, 7)
                        WHEN @pType = 'Weekly' THEN N'أسبوع ' + CAST(DATEPART(week, p.PurchaseDate) AS NVARCHAR) + N' (' + CONVERT(VARCHAR(10), DATEADD(day, 1-DATEPART(weekday, p.PurchaseDate), p.PurchaseDate), 120) + N')'
                        ELSE CONVERT(VARCHAR(10), p.PurchaseDate, 120)
                    END AS PeriodName,
                    COUNT(DISTINCT p.PurchaseID) AS InvoiceCount,
                    ISNULL(SUM(CASE WHEN p.PurchaseType = 'Cash' THEN p.TotalAmount ELSE 0 END), 0) AS CashPurchases,
                    ISNULL(SUM(CASE WHEN p.PurchaseType != 'Cash' THEN p.TotalAmount ELSE 0 END), 0) AS CreditPurchases,
                    ISNULL(SUM(p.DiscountAmount), 0) AS TotalDiscounts,
                    ISNULL(SUM(p.TaxAmount), 0) AS TotalTax,
                    ISNULL(SUM(p.TotalAmount), 0) AS TotalPurchases
                FROM Purchases p
                WHERE p.IsPosted = 1
                  AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                  AND (@supID IS NULL OR p.SupplierID = @supID OR p.ClientID = @supID)
                GROUP BY 
                    CASE 
                        WHEN @pType = 'Monthly' THEN SUBSTRING(CONVERT(VARCHAR(10), p.PurchaseDate, 120), 1, 7)
                        WHEN @pType = 'Weekly' THEN N'أسبوع ' + CAST(DATEPART(week, p.PurchaseDate) AS NVARCHAR) + N' (' + CONVERT(VARCHAR(10), DATEADD(day, 1-DATEPART(weekday, p.PurchaseDate), p.PurchaseDate), 120) + N')'
                        ELSE CONVERT(VARCHAR(10), p.PurchaseDate, 120)
                    END
                ORDER BY MIN(p.PurchaseDate) DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@pType", periodType),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value));
        }

        /// <summary>3. تقرير تفاصيل المشتريات وسطور الفواتير (Line-by-line Items)</summary>
        public static DataTable GetDetailedPurchaseItems(DateTime from, DateTime to, int? supplierID = null, string keyword = null)
        {
            DateTime f = from;
            DateTime t = to;
            if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

            return DbHelper.Query(
                @"SELECT 
                    p.PurchaseCode AS PurchaseCode,
                    ISNULL(p.SupplierInvoiceNo, N'-') AS SupplierInvoiceNo,
                    p.PurchaseDate AS PurchaseDate,
                    ISNULL(s.SupplierName, ISNULL(c.ClientName, N'مورد عام')) AS SupplierName,
                    COALESCE(pr.ProductCode, pr.PartNumber, CAST(pr.ProductID AS NVARCHAR)) AS ProductCode,
                    pr.ProductName AS ProductName,
                    ISNULL(cat.CategoryName, N'عام') AS CategoryName,
                    pi.Quantity AS Quantity,
                    COALESCE(pi.UnitName, pr.Unit, N'قطعة') AS UnitName,
                    pi.UnitPrice AS UnitPrice,
                    ISNULL(pi.DiscountAmt, 0) AS DiscountAmt,
                    pi.TotalPrice AS TotalPrice,
                    CASE p.PurchaseType
                        WHEN 'Cash' THEN N'نقدي'
                        WHEN 'Credit' THEN N'آجل'
                        ELSE p.PurchaseType
                    END AS PurchaseTypeArabic,
                    ISNULL(e.EmpName, N'---') AS CreatedByName,
                    ISNULL(w.WarehouseName, N'الرئيسي') AS WarehouseName,
                    COALESCE(NULLIF(pi.Notes, N''), p.Notes, N'') AS Notes
                FROM PurchaseItems pi
                JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                JOIN Products pr ON pi.ProductID = pr.ProductID
                LEFT JOIN Categories cat ON pr.CategoryID = cat.CategoryID
                LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                LEFT JOIN Clients c ON p.ClientID = c.ClientID
                LEFT JOIN Employees e ON p.CreatedBy = e.EmpID
                LEFT JOIN Warehouses w ON p.WarehouseID = w.WarehouseID
                WHERE p.IsPosted = 1
                  AND p.PurchaseDate BETWEEN @f AND @t
                  AND (@supID IS NULL OR p.SupplierID = @supID OR p.ClientID = @supID)
                  AND (@kw IS NULL OR pr.ProductName LIKE N'%' + @kw + N'%' OR pr.ProductCode LIKE N'%' + @kw + N'%' OR s.SupplierName LIKE N'%' + @kw + N'%' OR p.PurchaseCode LIKE N'%' + @kw + N'%' OR p.SupplierInvoiceNo LIKE N'%' + @kw + N'%')
                ORDER BY p.PurchaseDate DESC, p.PurchaseID DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                DbHelper.P("@kw", string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim()));
        }

        /// <summary>4. تقرير المشتريات حسب المورد</summary>
        public static DataTable GetPurchasesBySupplier(DateTime from, DateTime to)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    ISNULL(s.SupplierName, N'مورد عام') AS SupplierName,
                    ISNULL(s.Phone, N'---') AS Phone,
                    COUNT(DISTINCT p.PurchaseID) AS InvoiceCount,
                    ISNULL(SUM(p.TotalAmount), 0) AS TotalPurchases,
                    ISNULL((SELECT SUM(pr.TotalAmount) FROM PurchaseReturns pr WHERE pr.SupplierID = s.SupplierID AND CAST(pr.ReturnDate AS DATE) BETWEEN @f AND @t), 0) AS TotalReturns,
                    (ISNULL(SUM(p.TotalAmount), 0) - ISNULL((SELECT SUM(pr.TotalAmount) FROM PurchaseReturns pr WHERE pr.SupplierID = s.SupplierID AND CAST(pr.ReturnDate AS DATE) BETWEEN @f AND @t), 0)) AS NetPurchases,
                    ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID AND st.TransType = 'Payment' AND CAST(st.TransDate AS DATE) BETWEEN @f AND @t), 0) AS TotalPaid,
                    (s.OpeningBalance + 
                     ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                     ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)
                    ) AS CurrentBalance
                FROM Suppliers s
                LEFT JOIN Purchases p ON s.SupplierID = p.SupplierID AND p.IsPosted = 1 AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                GROUP BY s.SupplierID, s.SupplierName, s.Phone, s.OpeningBalance
                HAVING (ISNULL(SUM(p.TotalAmount), 0) > 0 OR EXISTS (SELECT 1 FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID AND CAST(st.TransDate AS DATE) BETWEEN @f AND @t))
                ORDER BY TotalPurchases DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t));
        }

        /// <summary>5. تقرير المشتريات حسب الصنف وتكلفة الشراء ومتوسط الأسعار</summary>
        public static DataTable GetPurchasesByProduct(DateTime from, DateTime to, int? supplierID = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    pr.ProductCode,
                    pr.ProductName,
                    pr.Unit,
                    ISNULL(cat.CategoryName, N'عام') AS CategoryName,
                    SUM(pi.Quantity) AS TotalQtyPurchased,
                    SUM(pi.TotalPrice) AS TotalCost,
                    AVG(pi.UnitPrice) AS AvgPurchasePrice,
                    (SELECT TOP 1 pi2.UnitPrice FROM PurchaseItems pi2 JOIN Purchases p2 ON pi2.PurchaseID = p2.PurchaseID WHERE pi2.ProductID = pr.ProductID AND p2.IsPosted = 1 ORDER BY p2.PurchaseDate DESC) AS LastPurchasePrice,
                    MIN(pi.UnitPrice) AS MinPurchasePrice,
                    MAX(pi.UnitPrice) AS MaxPurchasePrice
                FROM PurchaseItems pi
                JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                JOIN Products pr ON pi.ProductID = pr.ProductID
                LEFT JOIN Categories cat ON pr.CategoryID = cat.CategoryID
                WHERE p.IsPosted = 1
                  AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                  AND (@supID IS NULL OR p.SupplierID = @supID OR p.ClientID = @supID)
                GROUP BY pr.ProductID, pr.ProductCode, pr.ProductName, pr.Unit, cat.CategoryName
                ORDER BY TotalCost DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value));
        }

        /// <summary>6. تقرير المشتريات حسب المجموعة / القسم</summary>
        public static DataTable GetPurchasesByCategory(DateTime from, DateTime to)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    ISNULL(cat.CategoryName, N'بدون قسم / عام') AS CategoryName,
                    COUNT(DISTINCT pi.ProductID) AS DistinctProductsCount,
                    SUM(pi.Quantity) AS TotalQtyPurchased,
                    ISNULL(SUM(pi.DiscountAmt), 0) AS TotalDiscounts,
                    SUM(pi.TotalPrice) AS TotalPurchasesAmount,
                    COUNT(DISTINCT p.PurchaseID) AS InvoicesCount
                FROM PurchaseItems pi
                JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                JOIN Products pr ON pi.ProductID = pr.ProductID
                LEFT JOIN Categories cat ON pr.CategoryID = cat.CategoryID
                WHERE p.IsPosted = 1
                  AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                GROUP BY ISNULL(cat.CategoryName, N'بدون قسم / عام')
                ORDER BY TotalPurchasesAmount DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t));
        }

        /// <summary>7. تقرير مرتجعات المشتريات التفصيلي</summary>
        public static DataTable GetDetailedPurchaseReturns(DateTime from, DateTime to, int? supplierID = null, string keyword = null)
        {
            DateTime f = from;
            DateTime t = to;
            if (t.TimeOfDay == TimeSpan.Zero) t = t.Date.AddDays(1).AddTicks(-1);

            return DbHelper.Query(
                @"SELECT 
                    pr.ReturnDate AS ReturnDate,
                    CAST(pr.ReturnID AS NVARCHAR) AS ReturnCode,
                    ISNULL(p.PurchaseCode, N'مرتجع عام') AS OriginalPurchaseCode,
                    ISNULL(s.SupplierName, N'مورد عام') AS SupplierName,
                    COALESCE(prod.ProductCode, prod.PartNumber, CAST(prod.ProductID AS NVARCHAR)) AS ProductCode,
                    prod.ProductName AS ProductName,
                    pri.Quantity AS ReturnedQty,
                    COALESCE(pri.UnitName, prod.Unit, N'قطعة') AS UnitName,
                    pri.UnitPrice AS UnitPrice,
                    pri.TotalPrice AS TotalReturnAmount,
                    ISNULL(e.EmpName, N'---') AS CreatedByName,
                    ISNULL(pr.Notes, N'---') AS Notes
                FROM PurchaseReturnItems pri
                JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                JOIN Products prod ON pri.ProductID = prod.ProductID
                LEFT JOIN Purchases p ON pr.PurchaseID = p.PurchaseID
                LEFT JOIN Suppliers s ON pr.SupplierID = s.SupplierID
                LEFT JOIN Employees e ON pr.CreatedBy = e.EmpID
                WHERE pr.ReturnDate BETWEEN @f AND @t
                  AND (@supID IS NULL OR pr.SupplierID = @supID)
                  AND (@kw IS NULL OR prod.ProductName LIKE N'%' + @kw + N'%' OR s.SupplierName LIKE N'%' + @kw + N'%' OR p.PurchaseCode LIKE N'%' + @kw + N'%')
                ORDER BY pr.ReturnDate DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                DbHelper.P("@kw", string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim()));
        }

        /// <summary>8. تقرير المدفوعات للموردين والتسويات</summary>
        public static DataTable GetSupplierPaymentsReport(DateTime from, DateTime to, int? supplierID = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    s.SupplierName,
                    ISNULL(s.Phone, N'---') AS Phone,
                    ISNULL(pur.TotalPurchases, 0) AS TotalPurchases,
                    ISNULL(pay.TotalPaid, 0) AS TotalPaid,
                    (s.OpeningBalance + 
                     ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                     ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0)
                    ) AS CurrentBalance,
                    lp.LastPaymentAmount,
                    lp.LastPaymentDate
                FROM Suppliers s
                LEFT JOIN (
                    SELECT SupplierID, SUM(TotalAmount) AS TotalPurchases
                    FROM Purchases
                    WHERE IsPosted = 1 AND CAST(PurchaseDate AS DATE) BETWEEN @f AND @t
                    GROUP BY SupplierID
                ) pur ON s.SupplierID = pur.SupplierID
                LEFT JOIN (
                    SELECT SupplierID, SUM(Debit) AS TotalPaid
                    FROM SupplierTransactions
                    WHERE TransType = 'Payment' AND CAST(TransDate AS DATE) BETWEEN @f AND @t
                    GROUP BY SupplierID
                ) pay ON s.SupplierID = pay.SupplierID
                OUTER APPLY (
                    SELECT TOP 1 Debit AS LastPaymentAmount, TransDate AS LastPaymentDate
                    FROM SupplierTransactions
                    WHERE SupplierID = s.SupplierID AND TransType = 'Payment'
                    ORDER BY TransDate DESC, TransID DESC
                ) lp
                WHERE (@supID IS NULL OR s.SupplierID = @supID)
                  AND (ISNULL(pur.TotalPurchases, 0) > 0 OR ISNULL(pay.TotalPaid, 0) > 0 OR lp.LastPaymentAmount IS NOT NULL)
                ORDER BY TotalPaid DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value));
        }

        /// <summary>9. كشف حساب المورد الشامل (Ledger Statement)</summary>
        public static DataTable GetSupplierStatement(int supplierID, DateTime from, DateTime to)
        {
            DateTime f = from.Date;
            DateTime t = to.Date.AddDays(1).AddTicks(-1);

            return DbHelper.Query(
                @"SELECT 
                    st.TransID,
                    st.TransDate,
                    st.TransType,
                    st.Debit,
                    st.Credit,
                    st.RefID,
                    st.Notes,
                    ISNULL(e.EmpName, N'---') AS CreatedByName
                FROM SupplierTransactions st
                LEFT JOIN Employees e ON st.CreatedBy = e.EmpID
                WHERE st.SupplierID = @supID
                  AND st.TransDate BETWEEN @f AND @t
                ORDER BY st.TransDate ASC, st.TransID ASC",
                DbHelper.P("@supID", supplierID),
                DbHelper.P("@f", f), DbHelper.P("@t", t));
        }

        /// <summary>10. تقرير أسعار الشراء وتغير الأسعار لمراقبة التكلفة</summary>
        public static DataTable GetPurchasePricesTracking(DateTime from, DateTime to, string keyword = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @";WITH RankedPurchases AS (
                    SELECT 
                        pi.ProductID,
                        p.SupplierID,
                        pi.UnitPrice,
                        p.PurchaseDate,
                        ROW_NUMBER() OVER(PARTITION BY pi.ProductID ORDER BY p.PurchaseDate DESC, p.PurchaseID DESC) AS rn
                    FROM PurchaseItems pi
                    JOIN Purchases p ON pi.PurchaseID = p.PurchaseID
                    WHERE p.IsPosted = 1
                      AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                )
                SELECT 
                    pr.ProductCode,
                    pr.ProductName,
                    ISNULL(s.SupplierName, N'---') AS SupplierName,
                    cur.UnitPrice AS LastPrice,
                    prev.UnitPrice AS PreviousPrice,
                    CASE 
                        WHEN prev.UnitPrice > 0 THEN ROUND(((cur.UnitPrice - prev.UnitPrice) / prev.UnitPrice) * 100, 2)
                        ELSE 0 
                    END AS ChangePercentage,
                    cur.PurchaseDate AS LastPurchaseDate
                FROM RankedPurchases cur
                JOIN Products pr ON cur.ProductID = pr.ProductID
                LEFT JOIN Suppliers s ON cur.SupplierID = s.SupplierID
                LEFT JOIN RankedPurchases prev ON cur.ProductID = prev.ProductID AND prev.rn = 2
                WHERE cur.rn = 1
                  AND (@kw IS NULL OR pr.ProductName LIKE N'%' + @kw + N'%' OR pr.ProductCode LIKE N'%' + @kw + N'%' OR s.SupplierName LIKE N'%' + @kw + N'%')
                ORDER BY cur.PurchaseDate DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@kw", string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim()));
        }

        /// <summary>11. تقرير المشتريات الآجلة والمديونيات المستحقة</summary>
        public static DataTable GetCreditPurchasesReport(DateTime from, DateTime to, int? supplierID = null)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            return DbHelper.Query(
                @"SELECT 
                    p.PurchaseCode,
                    p.PurchaseDate,
                    ISNULL(p.SupplierInvoiceNo, N'-') AS SupplierInvoiceNo,
                    ISNULL(s.SupplierName, N'مورد عام') AS SupplierName,
                    ISNULL(s.Phone, N'---') AS Phone,
                    p.TotalAmount AS TotalInvoiceAmount,
                    ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.RefID = p.PurchaseID AND st.TransType = 'Payment'), 0) AS PaidAmount,
                    (p.TotalAmount - ISNULL((SELECT SUM(st.Debit) FROM SupplierTransactions st WHERE st.RefID = p.PurchaseID AND st.TransType = 'Payment'), 0)) AS RemainingAmount,
                    (s.OpeningBalance + 
                     ISNULL((SELECT SUM(st.Credit) FROM SupplierTransactions st WHERE st.SupplierID = s.SupplierID), 0) - 
                LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                WHERE p.IsPosted = 1
                  AND p.PurchaseType = 'Credit'
                  AND CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                  AND (@supID IS NULL OR p.SupplierID = @supID)
                ORDER BY p.PurchaseDate DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@supID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value));
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    /// <summary>مرتجع مشتريات — القيد المحاسبي السليم</summary>
    public static class PurchaseReturnDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT pr.ReturnID, pr.ReturnDate,
                         ISNULL(p.PurchaseCode, N'---') AS PurchaseCode,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         pr.TotalAmount, pr.Notes
                  FROM PurchaseReturns pr
                  LEFT JOIN Purchases p  ON pr.PurchaseID  = p.PurchaseID
                  LEFT JOIN Suppliers s  ON pr.SupplierID  = s.SupplierID
                  WHERE CAST(pr.ReturnDate AS DATE) BETWEEN @f AND @t
                    AND (@warehouseID IS NULL OR pr.WarehouseID = @warehouseID)
                  ORDER BY pr.ReturnDate DESC",
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetItems(int returnID)
        {
            return DbHelper.Query(
                @"SELECT pri.ProductID, p.ProductName, pri.Quantity, pri.UnitPrice, pri.TotalPrice,
                         pri.UnitName, COALESCE(pri.Factor, 1.0) AS Factor
                  FROM PurchaseReturnItems pri
                  JOIN Products p ON pri.ProductID = p.ProductID
                  WHERE pri.ReturnID = @id",
                DbHelper.P("@id", returnID));
        }

        /// <summary>
        /// حفظ مرتجع مشتريات (مع ربط الخزينة والوردية وطريقة الدفع)
        /// - شراء نقدي → يُعاد المبلغ للخزنة (AmountIn)
        /// - شراء فيزا/بنك → يُعاد لحساب البنك/الفيزا
        /// - شراء آجل  → يُقلَّل ما يستحقه المورد (Debit في SupplierTransactions)
        /// </summary>
        public static int SavePurchaseReturn(int purchaseID, int? supplierID, decimal total,
            string notes, List<PurchaseItemDTO> items, int? warehouseID = null, string returnType = "Credit", int? shiftID = null, int? targetAccountID = null)
        {
            int returnedRetID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                string purType = returnType;
                int whID = warehouseID ?? 1;
                bool isClientPurchase = false;
                int? clientID = null;

                if (purchaseID > 0)
                {
                    var dtPur = DbHelper.QueryTrans(trans,
                        "SELECT PurchaseType, SupplierID, ClientID, PurchaseSource, WarehouseID FROM Purchases WHERE PurchaseID=@pid",
                        DbHelper.P("@pid", purchaseID));
                    if (dtPur.Rows.Count > 0)
                    {
                        if (string.IsNullOrEmpty(returnType) || returnType == "Credit")
                            purType = dtPur.Rows[0]["PurchaseType"].ToString();
                        string pSrc = dtPur.Rows[0]["PurchaseSource"]?.ToString() ?? "";
                        if (pSrc == "Client") isClientPurchase = true;

                        if (dtPur.Rows[0]["WarehouseID"] != DBNull.Value)
                            whID = Convert.ToInt32(dtPur.Rows[0]["WarehouseID"]);
                        if (!supplierID.HasValue && dtPur.Rows[0]["SupplierID"] != DBNull.Value)
                            supplierID = Convert.ToInt32(dtPur.Rows[0]["SupplierID"]);
                        if (dtPur.Rows[0]["ClientID"] != DBNull.Value)
                            clientID = Convert.ToInt32(dtPur.Rows[0]["ClientID"]);
                    }
                }

                int? sID = shiftID ?? Session.CurrentShiftID;
                if (!sID.HasValue || sID.Value <= 0)
                {
                    try { sID = ShiftDAL.GetActiveShiftID(); } catch { }
                }

                string pType = "Credit";
                if (purType != null && (purType.Contains("Cash") || purType.Contains("نقدي"))) pType = "Cash";
                else if (purType != null && (purType.Contains("Visa") || purType.Contains("Bank") || purType.Contains("فيزا") || purType.Contains("بنك"))) pType = "Visa";
                else pType = "Credit";

                // تسجيل المرتجع
                int retID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO PurchaseReturns(ReturnDate,PurchaseID,SupplierID,TotalAmount,Notes,CreatedBy,WarehouseID,PaymentType,ShiftID)" +
                    " VALUES(@dt,@pid,@sid,@tot,@n,@by,@wid,@ptyp,@shid)",
                    DbHelper.P("@dt",   DateTime.Now),
                    DbHelper.P("@pid",  purchaseID > 0 ? (object)purchaseID : DBNull.Value),
                    DbHelper.P("@sid",  supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@tot",  total),
                    DbHelper.P("@n",    notes),
                    DbHelper.P("@by",   Session.EmpID),
                    DbHelper.P("@wid",  whID),
                    DbHelper.P("@ptyp", pType),
                    DbHelper.P("@shid", sID.HasValue && sID.Value > 0 ? (object)sID.Value : DBNull.Value));

                if (retID <= 0) throw new Exception("فشل إنشاء سجل مرتجع الشراء.");
                returnedRetID = retID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO PurchaseReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice,UnitName,Factor)" +
                        " VALUES(@rid,@pid,@qty,@up,@tp,@un,@fac)",
                        DbHelper.P("@rid", retID),
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity),
                        DbHelper.P("@up",  item.UnitPrice),
                        DbHelper.P("@tp",  item.TotalPrice),
                        DbHelper.P("@un",  item.UnitName),
                        DbHelper.P("@fac", item.Factor));

                    decimal baseQty = item.Quantity * (item.Factor > 0 ? item.Factor : 1m);
                    var targetBatchObj = DbHelper.ScalarTrans(trans,
                        "SELECT TOP 1 BatchID FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity >= @qty ORDER BY ExpiryDate ASC, BatchID ASC",
                        DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", whID), DbHelper.P("@qty", baseQty));

                    if (targetBatchObj == null || targetBatchObj == DBNull.Value)
                    {
                        targetBatchObj = DbHelper.ScalarTrans(trans,
                            "SELECT TOP 1 BatchID FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid ORDER BY BatchID DESC",
                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", whID));
                    }

                    if (targetBatchObj != null && targetBatchObj != DBNull.Value)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "UPDATE ProductBatches SET Quantity = CASE WHEN Quantity - @qty < 0 THEN 0 ELSE Quantity - @qty END WHERE BatchID = @bid",
                            DbHelper.P("@qty", baseQty), DbHelper.P("@bid", Convert.ToInt32(targetBatchObj)));
                    }
                }

                // القيد المحاسبي السليم
                if (pType == "Cash")
                {
                    int accId = targetAccountID ?? Session.GetDefaultSafeID();
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy,AccountID,ShiftID)" +
                        " VALUES(@dt,'PurchaseReturn',@amt,@ref,@n,@by,@accId,@shid)",
                        DbHelper.P("@dt",    DateTime.Now),
                        DbHelper.P("@amt",   total),
                        DbHelper.P("@ref",   retID),
                        DbHelper.P("@n",     purchaseID > 0 ? ("مرتجع شراء نقدي — فاتورة رقم " + purchaseID) : ("مرتجع شراء عام نقدي " + (notes ?? ""))),
                        DbHelper.P("@by",    Session.EmpID),
                        DbHelper.P("@accId", accId),
                        DbHelper.P("@shid",  sID.HasValue && sID.Value > 0 ? (object)sID.Value : DBNull.Value));
                }
                else if (pType == "Visa")
                {
                    int visaAcc = targetAccountID ?? Session.GetDefaultSafeID();
                    try
                    {
                        var dtVisa = DbHelper.QueryTrans(trans, "SELECT TOP 1 AccountID FROM SafeAccounts WHERE IsActive=1 AND AccountType IN ('Visa','Bank') ORDER BY AccountID");
                        if (dtVisa.Rows.Count > 0 && targetAccountID == null)
                            visaAcc = Convert.ToInt32(dtVisa.Rows[0]["AccountID"]);
                    }
                    catch { }

                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy,AccountID,ShiftID)" +
                        " VALUES(@dt,'PurchaseReturn',@amt,@ref,@n,@by,@accId,@shid)",
                        DbHelper.P("@dt",    DateTime.Now),
                        DbHelper.P("@amt",   total),
                        DbHelper.P("@ref",   retID),
                        DbHelper.P("@n",     purchaseID > 0 ? ("مرتجع شراء بنكي/فيزا — فاتورة رقم " + purchaseID) : ("مرتجع شراء عام بنكي/فيزا " + (notes ?? ""))),
                        DbHelper.P("@by",    Session.EmpID),
                        DbHelper.P("@accId", visaAcc),
                        DbHelper.P("@shid",  sID.HasValue && sID.Value > 0 ? (object)sID.Value : DBNull.Value));
                }
                else if (isClientPurchase && clientID.HasValue)
                {
                    // مرتجع شراء من عميل -> Debit في ClientTransactions (يُقلل دائنية العميل علينا)
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy)" +
                        " VALUES(@cid,'Return',@amt,@ref,@n,@by)",
                        DbHelper.P("@cid", clientID.Value),
                        DbHelper.P("@amt", total),
                        DbHelper.P("@ref", retID),
                        DbHelper.P("@n",   purchaseID > 0 ? ("مرتجع شراء من عميل — فاتورة رقم " + purchaseID) : ("مرتجع شراء عام من عميل " + (notes ?? ""))),
                        DbHelper.P("@by",  Session.EmpID));
                }
                else if (supplierID.HasValue)
                {
                    // Debit في حساب المورد = يُقلّل ما يستحقه (يُقلّل الدين علينا)
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SupplierTransactions(SupplierID,TransType,Debit,RefID,Notes,CreatedBy)" +
                        " VALUES(@sid,'PurchaseReturn',@amt,@ref,@n,@by)",
                        DbHelper.P("@sid", supplierID.Value),
                        DbHelper.P("@amt", total),
                        DbHelper.P("@ref", retID),
                        DbHelper.P("@n",   purchaseID > 0 ? ("مرتجع شراء — فاتورة رقم " + purchaseID) : ("مرتجع شراء عام آجل " + (notes ?? ""))),
                        DbHelper.P("@by",  Session.EmpID));
                }
            });

            return returnedRetID;
        }
    }
}
