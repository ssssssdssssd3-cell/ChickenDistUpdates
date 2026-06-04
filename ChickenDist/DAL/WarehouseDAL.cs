using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class WarehouseDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = activeOnly
                ? "SELECT WarehouseID, WarehouseName, Location, Notes FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName"
                : "SELECT WarehouseID, WarehouseName, Location, Notes, IsActive FROM Warehouses ORDER BY WarehouseName";
            return DbHelper.Query(sql);
        }

        public static DataRow GetByID(int id)
        {
            DataTable dt = DbHelper.Query("SELECT * FROM Warehouses WHERE WarehouseID = @id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string name, string location, string notes, bool isActive = true)
        {
            if (id == 0)
            {
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Warehouses (WarehouseName, Location, Notes, IsActive) VALUES (@name, @loc, @notes, @act)",
                    DbHelper.P("@name", name), DbHelper.P("@loc", location), DbHelper.P("@notes", notes), DbHelper.P("@act", isActive));
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Warehouses SET WarehouseName = @name, Location = @loc, Notes = @notes, IsActive = @act WHERE WarehouseID = @id",
                    DbHelper.P("@name", name), DbHelper.P("@loc", location), DbHelper.P("@notes", notes), DbHelper.P("@act", isActive), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            // لا يمكن حذف المخزن الرئيسي الافتراضي (ID = 1)
            if (id == 1) return;
            DbHelper.Execute("UPDATE Warehouses SET IsActive = 0 WHERE WarehouseID = @id", DbHelper.P("@id", id));
        }
    }
}
