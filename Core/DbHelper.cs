using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class DbHelper
    {
        private static string _connStr = GetInitialConnectionString();

        private static string GetInitialConnectionString()
        {
            try
            {
                var connSetting = ConfigurationManager.ConnectionStrings["MainDB"];
                if (connSetting != null && !string.IsNullOrEmpty(connSetting.ConnectionString))
                {
                    return connSetting.ConnectionString;
                }
            }
            catch { }
            return "Data Source=.;Initial Catalog=ChickenDist;Integrated Security=True;Connect Timeout=30;";
        }

        public static void SetConnectionString(string connStr)
        {
            _connStr = connStr;
        }

        public static void EnsureDatabaseSchema()
        {
            try
            {
                string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockAdjustments')
                BEGIN
                    CREATE TABLE StockAdjustments (
                        AdjID INT IDENTITY(1,1) PRIMARY KEY,
                        AdjDate DATETIME DEFAULT GETDATE(),
                        ProductID INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
                        BookQty DECIMAL(10,3) NOT NULL,
                        ActualQty DECIMAL(10,3) NOT NULL,
                        Notes NVARCHAR(500),
                        CreatedBy INT REFERENCES Employees(EmpID)
                    );
                END";
                Execute(sql);

                // Add DriverID migration to Clients table if not exists
                string sqlClientsDriver = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'DriverID')
                BEGIN
                    ALTER TABLE Clients ADD DriverID INT NULL FOREIGN KEY REFERENCES Employees(EmpID);
                END";
                Execute(sqlClientsDriver);

                // Add Phone2, MaxCreditLimit, Notes to Clients
                string sqlClientsExtra = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'Phone2')
                BEGIN
                    ALTER TABLE Clients ADD Phone2 NVARCHAR(20) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'MaxCreditLimit')
                BEGIN
                    ALTER TABLE Clients ADD MaxCreditLimit DECIMAL(10,2) DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'Notes')
                BEGIN
                    ALTER TABLE Clients ADD Notes NVARCHAR(500) NULL;
                END";
                Execute(sqlClientsExtra);

                // Add PurchasePrice, MinStockLimit, Description to Products
                string sqlProductsExtra = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PurchasePrice')
                BEGIN
                    ALTER TABLE Products ADD PurchasePrice DECIMAL(10,2) DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'MinStockLimit')
                BEGIN
                    ALTER TABLE Products ADD MinStockLimit DECIMAL(10,3) DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Description')
                BEGIN
                    ALTER TABLE Products ADD Description NVARCHAR(500) NULL;
                END";
                Execute(sqlProductsExtra);
                // Add Discount fields to Sales and SaleItems
                string sqlSalesDiscount = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DiscountAmount')
                BEGIN
                    ALTER TABLE Sales ADD DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DiscountPct')
                BEGIN
                    ALTER TABLE Sales ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                END";
                Execute(sqlSalesDiscount);

                string sqlSaleItemsDiscount = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'DiscountPct')
                BEGIN
                    ALTER TABLE SaleItems ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'DiscountAmt')
                BEGIN
                    ALTER TABLE SaleItems ADD DiscountAmt DECIMAL(10,2) NOT NULL DEFAULT 0;
                END";
                Execute(sqlSaleItemsDiscount);
            }
            catch {}
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connStr);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        /// <summary>تنفيذ استعلام وإرجاع DataTable</summary>
        public static DataTable Query(string sql, params SqlParameter[] prms)
        {
            var dt = new DataTable();
            try
            {
                using (var con = GetConnection())
                using (var cmd = new SqlCommand(sql, con))
                {
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    con.Open();
                    using (var da = new SqlDataAdapter(cmd))
                        da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في قاعدة البيانات:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }

        /// <summary>تنفيذ أمر (INSERT/UPDATE/DELETE) وإرجاع عدد الصفوف المتأثرة</summary>
        public static int Execute(string sql, params SqlParameter[] prms)
        {
            try
            {
                using (var con = GetConnection())
                using (var cmd = new SqlCommand(sql, con))
                {
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    con.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في قاعدة البيانات:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        /// <summary>تنفيذ INSERT وإرجاع الـ ID الجديد</summary>
        public static int ExecuteInsert(string sql, params SqlParameter[] prms)
        {
            try
            {
                using (var con = GetConnection())
                using (var cmd = new SqlCommand(sql + "; SELECT SCOPE_IDENTITY();", con))
                {
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null ? -1 : Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في قاعدة البيانات:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        /// <summary>إرجاع قيمة واحدة</summary>
        public static object Scalar(string sql, params SqlParameter[] prms)
        {
            try
            {
                using (var con = GetConnection())
                using (var cmd = new SqlCommand(sql, con))
                {
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    con.Open();
                    return cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في قاعدة البيانات:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }
    }
}
