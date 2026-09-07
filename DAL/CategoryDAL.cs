using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class CategoryDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = activeOnly 
                ? "SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName"
                : "SELECT CategoryID, CategoryName, IsActive FROM Categories ORDER BY CategoryName";
            return DbHelper.Query(sql);
        }

        public static DataRow GetByID(int id)
        {
            DataTable dt = DbHelper.Query("SELECT * FROM Categories WHERE CategoryID = @id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string name, bool isActive = true)
        {
            if (id == 0)
            {
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Categories (CategoryName, IsActive) VALUES (@name, @act)",
                    DbHelper.P("@name", name), DbHelper.P("@act", isActive));
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Categories SET CategoryName = @name, IsActive = @act WHERE CategoryID = @id",
                    DbHelper.P("@name", name), DbHelper.P("@act", isActive), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Categories SET IsActive = 0 WHERE CategoryID = @id", DbHelper.P("@id", id));
        }

        public static DataTable GetAllForStore()
        {
            return DbHelper.Query(@"
                SELECT CategoryID, CategoryName, IsActive, 
                       ISNULL(ShowInOnlineStore, 1) AS ShowInOnlineStore,
                       (SELECT COUNT(*) FROM Products p WHERE p.CategoryID = Categories.CategoryID AND p.IsActive = 1) AS ProductsCount
                FROM Categories 
                WHERE IsActive = 1 
                ORDER BY CategoryName ASC");
        }

        public static void SetStoreVisibility(int categoryID, bool show)
        {
            DbHelper.Execute(
                "UPDATE Categories SET ShowInOnlineStore = @show WHERE CategoryID = @id",
                DbHelper.P("@show", show), DbHelper.P("@id", categoryID));
        }

        public static void SetAllStoreVisibility(bool show)
        {
            DbHelper.Execute("UPDATE Categories SET ShowInOnlineStore = @show", DbHelper.P("@show", show));
        }
    }
}
