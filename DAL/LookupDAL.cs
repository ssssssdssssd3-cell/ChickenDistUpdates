using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class LookupDAL
    {
        public static DataTable GetAll(string tableName, string orderCol)
        {
            try
            {
                return DbHelper.Query($"SELECT * FROM {tableName} ORDER BY {orderCol}");
            }
            catch
            {
                return DbHelper.Query($"SELECT * FROM {tableName}");
            }
        }

        public static int Save(string tableName, string idCol, string codeCol, string nameCol, string prefix, int id, string name)
        {
            if (id == 0)
            {
                // Check if name already exists
                var existing = DbHelper.Scalar($"SELECT {idCol} FROM {tableName} WHERE {nameCol} = @name", DbHelper.P("@name", name));
                if (existing != null && existing != DBNull.Value)
                {
                    return Convert.ToInt32(existing);
                }

                // Generate sequential code
                var maxIdVal = DbHelper.Scalar($"SELECT COALESCE(MAX({idCol}), 0) + 1 FROM {tableName}");
                int nextId = maxIdVal != null ? Convert.ToInt32(maxIdVal) : 1;
                string code = $"{prefix}-{nextId:D4}";

                try
                {
                    var colExists = DbHelper.Scalar($"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('{tableName}') AND name = '{codeCol}'");
                    if (Convert.ToInt32(colExists) > 0)
                    {
                        return DbHelper.ExecuteInsert(
                            $"INSERT INTO {tableName} ({codeCol}, {nameCol}) VALUES (@code, @name)",
                            DbHelper.P("@code", code), DbHelper.P("@name", name));
                    }
                    else
                    {
                        return DbHelper.ExecuteInsert(
                            $"INSERT INTO {tableName} ({nameCol}) VALUES (@name)",
                            DbHelper.P("@name", name));
                    }
                }
                catch
                {
                    return DbHelper.ExecuteInsert(
                        $"INSERT INTO {tableName} ({nameCol}) VALUES (@name)",
                        DbHelper.P("@name", name));
                }
            }
            else
            {
                DbHelper.Execute(
                    $"UPDATE {tableName} SET {nameCol} = @name WHERE {idCol} = @id",
                    DbHelper.P("@name", name), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(string tableName, string idCol, int id)
        {
            DbHelper.Execute($"DELETE FROM {tableName} WHERE {idCol} = @id", DbHelper.P("@id", id));
        }
    }
}
