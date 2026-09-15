using System;
using System.Data;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class TransferItemDTO
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvailableStock { get; set; }
        public string Unit { get; set; }
        public decimal Factor { get; set; } = 1.0m;
        public decimal TotalBaseQty => Quantity * (Factor > 0 ? Factor : 1.0m);
    }

    public static class TransferDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT t.TransferID, t.TransferCode, t.TransferDate,
                         wFrom.WarehouseName AS FromWarehouse,
                         wTo.WarehouseName AS ToWarehouse,
                         t.Notes, e.EmpName AS CreatedBy
                  FROM WarehouseTransfers t
                  JOIN Warehouses wFrom ON t.FromWarehouseID = wFrom.WarehouseID
                  JOIN Warehouses wTo ON t.ToWarehouseID = wTo.WarehouseID
                  LEFT JOIN Employees e ON t.CreatedBy = e.EmpID
                  WHERE CAST(t.TransferDate AS DATE) BETWEEN @f AND @t AND t.IsPosted = 1
                  ORDER BY t.TransferDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable GetItems(int transferID)
        {
            return DbHelper.Query(
                @"SELECT ti.ProductID, p.ProductCode, p.ProductName, ti.Quantity, 
                         ISNULL(ti.UnitName, p.Unit) AS Unit,
                         ISNULL(ti.Factor, 1.0) AS Factor
                  FROM WarehouseTransferItems ti
                  JOIN Products p ON ti.ProductID = p.ProductID
                  WHERE ti.TransferID = @id",
                DbHelper.P("@id", transferID));
        }

        public static int SaveTransfer(int fromWarehouseID, int toWarehouseID, string notes, List<TransferItemDTO> items)
        {
            if (fromWarehouseID == toWarehouseID)
            {
                throw new Exception("لا يمكن التحويل لنفس المخزن!");
            }

            int returnedID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                // التحقق من توافر الرصيد لكل صنف في المخزن المرسل بناءً على المعامل الفعلي
                foreach (var item in items)
                {
                    decimal availableStockSmallest = InventoryDAL.GetProductStock(item.ProductID, fromWarehouseID);
                    decimal factor = item.Factor > 0 ? item.Factor : 1.0m;
                    decimal requiredSmallest = item.Quantity * factor;
                    if (availableStockSmallest < requiredSmallest)
                    {
                        decimal maxInUnit = factor > 0 ? Math.Floor(availableStockSmallest / factor * 1000m) / 1000m : availableStockSmallest;
                        throw new Exception($"رصيد الصنف '{item.ProductName}' لا يكفي للتحويل.\nالرصيد المتاح بالمخزن المصدر: {maxInUnit:G29} {item.Unit} (أي {availableStockSmallest:G29} بالوحدة الصغرى)\nالكمية المطلوبة للتحويل: {item.Quantity:G29} {item.Unit} (أي {requiredSmallest:G29} بالوحدة الصغرى)");
                    }
                }

                // استخراج الكود التالي للتحويل
                var nextResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(TransferID), 0) + 1 FROM WarehouseTransfers");
                string code = "TRF-" + (nextResult != null ? nextResult.ToString() : "1");

                // إدراج رأس التحويل
                int transferID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO WarehouseTransfers (TransferCode, TransferDate, FromWarehouseID, ToWarehouseID, Notes, CreatedBy, IsPosted)
                      VALUES (@code, @dt, @from, @to, @n, @by, 1)",
                    DbHelper.P("@code", code),
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@from", fromWarehouseID),
                    DbHelper.P("@to", toWarehouseID),
                    DbHelper.P("@n", notes),
                    DbHelper.P("@by", Session.EmpID));

                if (transferID <= 0) throw new Exception("فشل في استخراج رقم التحويل المخزني الجديد.");
                returnedID = transferID;

                // إدراج البنود مع اسم الوحدة ومعامل التحويل
                foreach (var item in items)
                {
                    decimal factor = item.Factor > 0 ? item.Factor : 1.0m;
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO WarehouseTransferItems (TransferID, ProductID, Quantity, UnitName, Factor)
                          VALUES (@tid, @pid, @qty, @unit, @factor)",
                        DbHelper.P("@tid", transferID),
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity),
                        DbHelper.P("@unit", string.IsNullOrWhiteSpace(item.Unit) ? "وحدة" : item.Unit),
                        DbHelper.P("@factor", factor));
                }
            });

            return returnedID;
        }
    }
}
