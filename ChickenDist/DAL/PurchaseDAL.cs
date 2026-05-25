using System;
using System.Data;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class PurchaseItemDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountPct { get; set; }
        public decimal DiscountAmt { get; set; }
    }

    public static class PurchaseDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT p.PurchaseID, p.PurchaseCode, p.PurchaseDate, p.PurchaseType,
                         ISNULL(s.SupplierName, N'---') AS SupplierName,
                         p.TotalAmount, p.Notes, p.SupplierID,
                         COALESCE(p.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(p.DiscountPct, 0) AS DiscountPct
                  FROM Purchases p
                  LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                  WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @f AND @t AND p.IsPosted = 1
                  ORDER BY p.PurchaseDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable GetItems(int purchaseID)
        {
            return DbHelper.Query(
                @"SELECT pi.ProductID, pr.ProductName, pi.Quantity, pi.UnitPrice, pi.TotalPrice,
                         COALESCE(pi.DiscountPct, 0) AS DiscountPct, COALESCE(pi.DiscountAmt, 0) AS DiscountAmt
                  FROM PurchaseItems pi JOIN Products pr ON pi.ProductID = pr.ProductID
                  WHERE pi.PurchaseID = @id",
                DbHelper.P("@id", purchaseID));
        }

        public static int SavePurchase(string purchaseType, int? supplierID, decimal total, string notes,
            List<PurchaseItemDTO> items, decimal discountAmount = 0m, decimal discountPct = 0m)
        {
            int returnedID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                var nextResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(PurchaseID), 0) + 1 FROM Purchases");
                string code = "PUR-" + (nextResult != null ? nextResult.ToString() : "1");

                // إذا كان نقدي، تحقق من رصيد الخزنة
                if (purchaseType == "Cash")
                {
                    var cashResult = DbHelper.ScalarTrans(trans, 
                        "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                    decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                    if (cashBalance < total)
                    {
                        throw new Exception($"رصيد الخزنة ({cashBalance:N2} ج) لا يكفي لهذه الفاتورة ({total:N2} ج).\nيرجى تحصيل نقدية أولاً أو تسجيلها كفاتورة آجلة.");
                    }
                }

                int purchaseID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO Purchases(PurchaseCode,PurchaseDate,PurchaseType,SupplierID,TotalAmount,DiscountAmount,DiscountPct,Notes,CreatedBy,IsPosted) VALUES(@code,@dt,@typ,@sid,@tot,@discAmt,@discPct,@n,@by,1)",
                    DbHelper.P("@code", code), DbHelper.P("@dt", DateTime.Now), DbHelper.P("@typ", purchaseType),
                    DbHelper.P("@sid", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                    DbHelper.P("@tot", total), DbHelper.P("@discAmt", discountAmount), DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

                if (purchaseID <= 0) throw new Exception("فشل في استخراج رقم فاتورة المشتريات الجديدة.");
                returnedID = purchaseID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO PurchaseItems(PurchaseID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt) VALUES(@pid,@prodid,@qty,@up,@tp,@dpct,@damt)",
                        DbHelper.P("@pid", purchaseID), DbHelper.P("@prodid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice), DbHelper.P("@dpct", item.DiscountPct),
                        DbHelper.P("@damt", item.DiscountAmt));
                }

                // آجل: أضف لحساب المورد (Credit = مستحق للمورد)
                if (purchaseType == "Credit" && supplierID.HasValue)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SupplierTransactions(SupplierID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@sid,'Purchase',@amt,@ref,@n,@by)",
                        DbHelper.P("@sid", supplierID.Value), DbHelper.P("@amt", total),
                        DbHelper.P("@ref", purchaseID), DbHelper.P("@n", "فاتورة مشتريات " + code),
                        DbHelper.P("@by", Session.EmpID));
                }

                // نقدي: اخصم من الخزنة
                if (purchaseType == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransType,AmountOut,RefID,Notes,CreatedBy) VALUES('PurchaseExpense',@amt,@ref,@n,@by)",
                        DbHelper.P("@amt", total), DbHelper.P("@ref", purchaseID),
                        DbHelper.P("@n", "مشتريات نقدية " + code), DbHelper.P("@by", Session.EmpID));
                }
            });

            return returnedID;
        }
    }
}
