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
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        /// <summary>نسبة خصم الصنف % (0 = لا خصم)</summary>
        public decimal DiscountPct { get; set; } = 0m;
        /// <summary>قيمة خصم الصنف — تُستخدم فقط إذا كانت DiscountPct = 0</summary>
        public decimal DiscountAmt { get; set; } = 0m;

        /// <summary>صافي قيمة الصنف بعد خصم الصنف</summary>
        public decimal TotalPrice
        {
            get
            {
                decimal gross = Quantity * UnitPrice;
                if (DiscountPct > 0m)
                    return Math.Round(gross * (1m - DiscountPct / 100m), 2);
                if (DiscountAmt > 0m)
                    return Math.Round(Math.Max(0m, gross - DiscountAmt), 2);
                return Math.Round(gross, 2);
            }
        }
    }

    public static class PurchaseDAL
    {
        // ─── قراءة الفواتير المؤكدة ──────────────────────────────────────────────
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return GetAll(from, to, null, null);
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? supplierID, string productSearch)
        {
            string productFilter = string.IsNullOrWhiteSpace(productSearch) ? null : productSearch.Trim();
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct,   0) AS DiscountPct,
                         COALESCE(p.TaxPct,        0) AS TaxPct,
                         COALESCE(p.TaxAmount,     0) AS TaxAmount,
                         ISNULL((
                             SELECT SUM(pri.Quantity * pri.UnitPrice)
                             FROM PurchaseReturnItems pri
                             JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                             WHERE pr.PurchaseID = p.PurchaseID
                         ), 0) AS ReturnAmount
                  FROM Purchases p
                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                  WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t
                    AND p.IsPosted = 1
                    AND (@supplierID IS NULL OR p.SupplierID = @supplierID)
                    AND (@product IS NULL OR EXISTS (
                        SELECT 1 FROM PurchaseItems pi2
                        JOIN Products pr ON pi2.ProductID = pr.ProductID
                        WHERE pi2.PurchaseID = p.PurchaseID
                          AND (pr.ProductName LIKE N'%' + @product + N'%'
                            OR pr.ProductCode LIKE N'%' + @product + N'%')
                    ))
                  ORDER BY p.PurchaseDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@supplierID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                DbHelper.P("@product", (object)productFilter ?? DBNull.Value));
        }

        // ─── أصناف فاتورة معينة ──────────────────────────────────────────────────
        public static DataTable GetItems(int purchaseID)
        {
            return DbHelper.Query(
                @"SELECT pi.ProductID, pr.ProductName, pi.Quantity, pi.UnitPrice, pi.TotalPrice,
                         COALESCE(pi.DiscountPct, 0) AS DiscountPct,
                         COALESCE(pi.DiscountAmt, 0) AS DiscountAmt
                  FROM PurchaseItems pi
                  JOIN Products pr ON pi.ProductID = pr.ProductID
                  WHERE pi.PurchaseID = @id",
                DbHelper.P("@id", purchaseID));
        }

        // ─── الفواتير المعلقة (مسودات) ───────────────────────────────────────────
        public static DataTable GetDraftPurchases()
        {
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct,   0) AS DiscountPct,
                         COALESCE(p.TaxPct,        0) AS TaxPct,
                         COALESCE(p.TaxAmount,     0) AS TaxAmount
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
            bool isDraft = false)
        {
            int returnedID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                var nextResult = DbHelper.ScalarTrans(trans,
                    "SELECT COALESCE(MAX(PurchaseID), 0) + 1 FROM Purchases");
                string code = "PUR-" + (nextResult != null ? nextResult.ToString() : "1");

                // فحص رصيد الخزنة للمشتريات النقدية المؤكدة فقط
                if (purchaseType == "Cash" && !isDraft)
                {
                    var cashResult = DbHelper.ScalarTrans(trans,
                        "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                    decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                    if (cashBalance < total)
                        throw new Exception(
                            $"رصيد الخزنة ({cashBalance:N2} ج) لا يكفي لهذه الفاتورة ({total:N2} ج).\n" +
                            "يرجى تحصيل نقدية أولاً أو تسجيلها كفاتورة آجلة.");
                }

                int purchaseID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO Purchases
                        (PurchaseCode, PurchaseDate, PurchaseType, SupplierID,
                         TotalAmount, DiscountAmount, DiscountPct, TaxPct, TaxAmount,
                         Notes, CreatedBy, IsPosted)
                      VALUES
                        (@code, @dt, @typ, @sid,
                         @tot, @discAmt, @discPct, @taxPct, @taxAmt,
                         @n, @by, @ip)",
                    DbHelper.P("@code",    code),
                    DbHelper.P("@dt",      DateTime.Now),
                    DbHelper.P("@typ",     purchaseType),
                    DbHelper.P("@sid",     supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@tot",     total),
                    DbHelper.P("@discAmt", discountAmount),
                    DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@taxPct",  taxPct),
                    DbHelper.P("@taxAmt",  taxAmount),
                    DbHelper.P("@n",       notes),
                    DbHelper.P("@by",      Session.EmpID),
                    DbHelper.P("@ip",      isDraft ? 0 : 1));

                if (purchaseID <= 0)
                    throw new Exception("فشل في استخراج رقم فاتورة المشتريات الجديدة.");

                returnedID = purchaseID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO PurchaseItems
                            (PurchaseID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt)
                          VALUES (@pid, @prodid, @qty, @up, @tp, @dpct, @damt)",
                        DbHelper.P("@pid",    purchaseID),
                        DbHelper.P("@prodid", item.ProductID),
                        DbHelper.P("@qty",    item.Quantity),
                        DbHelper.P("@up",     item.UnitPrice),
                        DbHelper.P("@tp",     item.TotalPrice),
                        DbHelper.P("@dpct",   item.DiscountPct),
                        DbHelper.P("@damt",   item.DiscountAmt));
                }

                // ── القيود المحاسبية (للفواتير المؤكدة فقط) ─────────────────────
                if (!isDraft)
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
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy)" +
                            " VALUES(@dt,'PurchaseExpense',@amt,@ref,@n,@by)",
                            DbHelper.P("@dt",  DateTime.Now),
                            DbHelper.P("@amt", total),
                            DbHelper.P("@ref", purchaseID),
                            DbHelper.P("@n",   "مشتريات نقدية " + code),
                            DbHelper.P("@by",  Session.EmpID));
                    }
                }
            });

            return returnedID;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    /// <summary>مرتجع مشتريات — القيد المحاسبي السليم</summary>
    public static class PurchaseReturnDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to)
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
                  ORDER BY pr.ReturnDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable GetItems(int returnID)
        {
            return DbHelper.Query(
                @"SELECT pri.ProductID, p.ProductName, pri.Quantity, pri.UnitPrice, pri.TotalPrice
                  FROM PurchaseReturnItems pri
                  JOIN Products p ON pri.ProductID = p.ProductID
                  WHERE pri.ReturnID = @id",
                DbHelper.P("@id", returnID));
        }

        /// <summary>
        /// حفظ مرتجع شراء مع القيد المحاسبي الصحيح:
        /// - شراء نقدي → يُعاد المبلغ للخزنة (AmountIn)
        /// - شراء آجل  → يُقلَّل ما يستحقه المورد (Debit في SupplierTransactions)
        /// </summary>
        public static int SavePurchaseReturn(int purchaseID, int? supplierID, decimal total,
            string notes, List<PurchaseItemDTO> items)
        {
            int returnedRetID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                // جلب نوع الفاتورة الأصلية والمورد منها إذا لم يُحدَّد
                var dtPur = DbHelper.Query(
                    "SELECT PurchaseType, SupplierID FROM Purchases WHERE PurchaseID=@pid",
                    DbHelper.P("@pid", purchaseID));
                string purType = dtPur.Rows.Count > 0 ? dtPur.Rows[0]["PurchaseType"].ToString() : "Credit";

                if (!supplierID.HasValue && dtPur.Rows.Count > 0 && dtPur.Rows[0]["SupplierID"] != DBNull.Value)
                    supplierID = Convert.ToInt32(dtPur.Rows[0]["SupplierID"]);

                // تسجيل المرتجع
                int retID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO PurchaseReturns(ReturnDate,PurchaseID,SupplierID,TotalAmount,Notes,CreatedBy)" +
                    " VALUES(@dt,@pid,@sid,@tot,@n,@by)",
                    DbHelper.P("@dt",  DateTime.Now),
                    DbHelper.P("@pid", purchaseID > 0 ? (object)purchaseID : DBNull.Value),
                    DbHelper.P("@sid", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@tot", total),
                    DbHelper.P("@n",   notes),
                    DbHelper.P("@by",  Session.EmpID));

                if (retID <= 0) throw new Exception("فشل إنشاء سجل مرتجع الشراء.");
                returnedRetID = retID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO PurchaseReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice)" +
                        " VALUES(@rid,@pid,@qty,@up,@tp)",
                        DbHelper.P("@rid", retID),
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity),
                        DbHelper.P("@up",  item.UnitPrice),
                        DbHelper.P("@tp",  item.TotalPrice));
                }

                // القيد المحاسبي السليم
                if (purType == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy)" +
                        " VALUES(@dt,'PurchaseReturn',@amt,@ref,@n,@by)",
                        DbHelper.P("@dt",  DateTime.Now),
                        DbHelper.P("@amt", total),
                        DbHelper.P("@ref", retID),
                        DbHelper.P("@n",   "مرتجع شراء نقدي — فاتورة رقم " + purchaseID),
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
                        DbHelper.P("@n",   "مرتجع شراء — فاتورة رقم " + purchaseID),
                        DbHelper.P("@by",  Session.EmpID));
                }
            });

            return returnedRetID;
        }
    }
}
