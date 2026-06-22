using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class UnitDAL
    {
        public static DataTable GetAll()
        {
            return DbHelper.Query("SELECT UnitID, UnitName FROM Units ORDER BY UnitName");
        }

        public static int Save(int id, string name)
        {
            if (id == 0)
            {
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Units (UnitName) VALUES (@name)",
                    DbHelper.P("@name", name));
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Units SET UnitName = @name WHERE UnitID = @id",
                    DbHelper.P("@name", name), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("DELETE FROM Units WHERE UnitID = @id", DbHelper.P("@id", id));
        }
    }
}
