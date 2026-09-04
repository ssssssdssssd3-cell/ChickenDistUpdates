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
                @"SELECT ti.ProductID, p.ProductName, ti.Quantity, p.Unit
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
                // التحقق من توافر الرصيد لكل صنف في المخزن المرسل
                foreach (var item in items)
                {
                    decimal availableStock = InventoryDAL.GetProductStock(item.ProductID, fromWarehouseID);
                    if (availableStock < item.Quantity)
                    {
                        throw new Exception($"رصيد الصنف '{item.ProductName}' لا يكفي للتحويل.\nالرصيد المتاح في المخزن المصدر: {availableStock:N3}\nالكمية المطلوبة: {item.Quantity:N3}");
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

                // إدراج البنود
                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO WarehouseTransferItems (TransferID, ProductID, Quantity)
                          VALUES (@tid, @pid, @qty)",
                        DbHelper.P("@tid", transferID),
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity));
                }
            });

            return returnedID;
        }
    }
}
