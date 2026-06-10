using System;
using System.Data;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class WarehouseDAL
    {
        /// <summary>جلب كل المخازن النشطة</summary>
        public static DataTable GetAll(bool activeOnly = true)
        {
            string sql = activeOnly
                ? "SELECT WarehouseID, WarehouseName, Location FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseID"
                : "SELECT WarehouseID, WarehouseName, Location FROM Warehouses ORDER BY WarehouseID";
            return DbHelper.Query(sql);
        }

        /// <summary>البحث عن منتج بالباركود أو رقم القطعة أو اسم المنتج</summary>
        public static DataRow GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            string trimmed = barcode.Trim();
            var dt = DbHelper.Query(
                @"SELECT TOP 1 ProductID, ProductName, SalePrice, PurchasePrice, ProductCode, Barcode, PartNumber,
                          ISNULL(MinStockLimit, 0) AS MinStockLimit
                  FROM Products
                  WHERE IsActive = 1
                    AND (ProductCode = @bc OR Barcode = @bc OR PartNumber = @bc OR ProductName LIKE @like)
                  ORDER BY CASE WHEN ProductCode = @bc THEN 0
                                WHEN Barcode = @bc THEN 1
                                WHEN PartNumber = @bc THEN 2
                                ELSE 3 END",
                DbHelper.P("@bc", trimmed),
                DbHelper.P("@like", "%" + trimmed + "%"));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }
}
