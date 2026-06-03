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
            // First check for local activation file that can override connection string
            try
            {
                string actPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activation.txt");
                if (System.IO.File.Exists(actPath))
                {
                    var lines = System.IO.File.ReadAllLines(actPath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                        var idx = line.IndexOf('=');
                        if (idx <= 0) continue;
                        var key = line.Substring(0, idx).Trim();
                        var val = line.Substring(idx + 1).Trim();
                        if (key.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val))
                        {
                            return val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // non-fatal: show debug info
                System.Diagnostics.Debug.WriteLine("Activation file read failed: " + ex.Message);
            }

            try
            {
                var connSetting = ConfigurationManager.ConnectionStrings["MainDB"];
                if (connSetting != null && !string.IsNullOrEmpty(connSetting.ConnectionString))
                {
                    return connSetting.ConnectionString;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل قراءة إعدادات الاتصال من App.config:\n" + ex.Message, "خطأ في الإعدادات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                // FIX: توسيع عمود Password ليستوعب الـ hash (PBKDF2 يحتاج ~80 حرف)
                string sqlPasswordUpgrade = @"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE object_id = OBJECT_ID('Employees') AND name = 'Password'
                      AND max_length < 400
                )
                BEGIN
                    ALTER TABLE Employees ALTER COLUMN Password NVARCHAR(200) NOT NULL;
                END";
                Execute(sqlPasswordUpgrade);

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

                // Add Purchases and Suppliers schema
                string sqlPurchases = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
                BEGIN
                    CREATE TABLE Suppliers (
                        SupplierID       INT IDENTITY(1,1) PRIMARY KEY,
                        SupplierCode     NVARCHAR(20),
                        SupplierName     NVARCHAR(100) NOT NULL,
                        Phone            NVARCHAR(20),
                        Address          NVARCHAR(200),
                        OpeningBalance   DECIMAL(10,2) DEFAULT 0,
                        IsActive         BIT DEFAULT 1,
                        CreatedAt        DATETIME DEFAULT GETDATE()
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplierTransactions')
                BEGIN
                    CREATE TABLE SupplierTransactions (
                        TransID    INT IDENTITY(1,1) PRIMARY KEY,
                        TransDate  DATETIME DEFAULT GETDATE(),
                        SupplierID INT NOT NULL REFERENCES Suppliers(SupplierID),
                        TransType  NVARCHAR(30),
                        Debit      DECIMAL(10,2) DEFAULT 0,
                        Credit     DECIMAL(10,2) DEFAULT 0,
                        RefID      INT,
                        Notes      NVARCHAR(500),
                        CreatedBy  INT REFERENCES Employees(EmpID)
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
                BEGIN
                    CREATE TABLE Purchases (
                        PurchaseID      INT IDENTITY(1,1) PRIMARY KEY,
                        PurchaseCode    NVARCHAR(20),
                        PurchaseDate    DATETIME DEFAULT GETDATE(),
                        PurchaseType    NVARCHAR(20) NOT NULL,
                        SupplierID      INT REFERENCES Suppliers(SupplierID),
                        TotalAmount     DECIMAL(10,2) DEFAULT 0,
                        DiscountAmount  DECIMAL(10,2) DEFAULT 0,
                        DiscountPct     DECIMAL(5,2) DEFAULT 0,
                        Notes           NVARCHAR(500),
                        CreatedBy       INT REFERENCES Employees(EmpID),
                        IsPosted        BIT DEFAULT 1
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseItems')
                BEGIN
                    CREATE TABLE PurchaseItems (
                        ItemID      INT IDENTITY(1,1) PRIMARY KEY,
                        PurchaseID  INT NOT NULL REFERENCES Purchases(PurchaseID) ON DELETE CASCADE,
                        ProductID   INT NOT NULL REFERENCES Products(ProductID),
                        Quantity    DECIMAL(10,3),
                        UnitPrice   DECIMAL(10,2),
                        TotalPrice  DECIMAL(10,2),
                        DiscountPct DECIMAL(5,2) DEFAULT 0,
                        DiscountAmt DECIMAL(10,2) DEFAULT 0
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SupplierBalance')
                BEGIN
                    EXEC('CREATE VIEW vw_SupplierBalance AS
                    SELECT
                        s.SupplierID,
                        s.SupplierName,
                        s.Phone,
                        s.OpeningBalance,
                        ISNULL(SUM(st.Debit),0)  AS TotalDebit,
                        ISNULL(SUM(st.Credit),0) AS TotalCredit,
                        s.OpeningBalance + ISNULL(SUM(st.Credit),0) - ISNULL(SUM(st.Debit),0) AS Balance
                    FROM Suppliers s
                    LEFT JOIN SupplierTransactions st ON s.SupplierID = st.SupplierID
                    GROUP BY s.SupplierID, s.SupplierName, s.Phone, s.OpeningBalance')
                END";
                Execute(sqlPurchases);

                // Add SupplierID to Expenses (optional link to Suppliers)
                string sqlExpensesSupplier = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SupplierID')
                BEGIN
                    ALTER TABLE Expenses ADD SupplierID INT NULL;
                    ALTER TABLE Expenses ADD CONSTRAINT FK_Expenses_Suppliers FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID);
                END";
                Execute(sqlExpensesSupplier);

                string sqlVehicles = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vehicles')
                BEGIN
                    CREATE TABLE Vehicles (
                        VehicleID INT IDENTITY(1,1) PRIMARY KEY,
                        VehicleType NVARCHAR(100),
                        VehicleName NVARCHAR(100),
                        LicensePlate NVARCHAR(50),
                        Notes NVARCHAR(500),
                        IsActive BIT DEFAULT 1,
                        CreatedAt DATETIME DEFAULT GETDATE()
                    );
                END";
                Execute(sqlVehicles);

                string sqlExpensesVehicle = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'VehicleID')
                BEGIN
                    ALTER TABLE Expenses ADD VehicleID INT NULL;
                    ALTER TABLE Expenses ADD CONSTRAINT FK_Expenses_Vehicles FOREIGN KEY (VehicleID) REFERENCES Vehicles(VehicleID);
                END";
                Execute(sqlExpensesVehicle);

                // جداول مرتجع المشتريات
                string sqlPurchaseReturns = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseReturns')
                BEGIN
                    CREATE TABLE PurchaseReturns (
                        ReturnID    INT IDENTITY(1,1) PRIMARY KEY,
                        ReturnDate  DATETIME DEFAULT GETDATE(),
                        PurchaseID  INT REFERENCES Purchases(PurchaseID),
                        SupplierID  INT REFERENCES Suppliers(SupplierID),
                        TotalAmount DECIMAL(10,2) DEFAULT 0,
                        Notes       NVARCHAR(500),
                        CreatedBy   INT REFERENCES Employees(EmpID)
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseReturnItems')
                BEGIN
                    CREATE TABLE PurchaseReturnItems (
                        RItemID    INT IDENTITY(1,1) PRIMARY KEY,
                        ReturnID   INT NOT NULL REFERENCES PurchaseReturns(ReturnID) ON DELETE CASCADE,
                        ProductID  INT NOT NULL REFERENCES Products(ProductID),
                        Quantity   DECIMAL(10,3),
                        UnitPrice  DECIMAL(10,2),
                        TotalPrice DECIMAL(10,2)
                    );
                END";
                Execute(sqlPurchaseReturns);

                // إضافة حقلي ضريبة المشتريات للجدول Purchases
                string sqlPurchasesTax = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'TaxPct')
                BEGIN
                    ALTER TABLE Purchases ADD TaxPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'TaxAmount')
                BEGIN
                    ALTER TABLE Purchases ADD TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
                END";
                Execute(sqlPurchasesTax);

                // ===== جدول حركات الموظفين (عجز، سلفة، تسوية) =====
                string sqlEmployeeTransactions = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeTransactions')
                BEGIN
                    CREATE TABLE EmployeeTransactions (
                        TransID    INT IDENTITY(1,1) PRIMARY KEY,
                        TransDate  DATETIME DEFAULT GETDATE(),
                        EmpID      INT NOT NULL REFERENCES Employees(EmpID),
                        TransType  NVARCHAR(30) NOT NULL,
                        Debit      DECIMAL(10,2) NOT NULL DEFAULT 0,
                        Credit     DECIMAL(10,2) NOT NULL DEFAULT 0,
                        RefID      INT NULL,
                        Notes      NVARCHAR(500) NULL,
                        CreatedBy  INT REFERENCES Employees(EmpID)
                    );
                END";
                Execute(sqlEmployeeTransactions);

                // ===== عرض أرصدة الموظفين =====
                string sqlEmployeeBalanceView = @"
                IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_EmployeeBalance')
                BEGIN
                    EXEC('CREATE VIEW vw_EmployeeBalance AS
                    SELECT
                        e.EmpID,
                        e.EmpName,
                        e.Phone,
                        ISNULL(SUM(et.Debit),0)  AS TotalDebit,
                        ISNULL(SUM(et.Credit),0) AS TotalCredit,
                        ISNULL(SUM(et.Debit),0) - ISNULL(SUM(et.Credit),0) AS Balance
                    FROM Employees e
                    LEFT JOIN EmployeeTransactions et ON e.EmpID = et.EmpID
                    GROUP BY e.EmpID, e.EmpName, e.Phone')
                END";
                Execute(sqlEmployeeBalanceView);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تطبيق ترحيلات قاعدة البيانات:\n" + ex.Message, "خطأ في التهيئة", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
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
                // تسجيل التفاصيل الكاملة في السجل وإظهار رسالة عامة للمستخدم
                AppLogger.Error("DbHelper.Query failed", ex, sql.Length > 80 ? sql.Substring(0, 80) : sql);
                MessageBox.Show("حدث خطأ أثناء قراءة البيانات.\nيرجى مراجعة المسؤول أو ملف app.log للتفاصيل.",
                    "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AppLogger.Error("DbHelper.Execute failed", ex, sql.Length > 80 ? sql.Substring(0, 80) : sql);
                MessageBox.Show("حدث خطأ أثناء تنفيذ العملية.\nيرجى مراجعة المسؤول أو ملف app.log للتفاصيل.",
                    "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AppLogger.Error("DbHelper.ExecuteInsert failed", ex, sql.Length > 80 ? sql.Substring(0, 80) : sql);
                MessageBox.Show("حدث خطأ أثناء حفظ البيانات.\nيرجى مراجعة المسؤول أو ملف app.log للتفاصيل.",
                    "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AppLogger.Error("DbHelper.Scalar failed", ex, sql.Length > 80 ? sql.Substring(0, 80) : sql);
                MessageBox.Show("حدث خطأ أثناء استرجاع البيانات.\nيرجى مراجعة المسؤول أو ملف app.log للتفاصيل.",
                    "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static void RunInTransaction(Action<SqlConnection, SqlTransaction> action)
        {
            using (var con = GetConnection())
            {
                con.Open();
                using (var trans = con.BeginTransaction())
                {
                    try
                    {
                        action(con, trans);
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        AppLogger.Error("RunInTransaction: تم التراجع عن العملية", ex, "DbHelper.RunInTransaction");
                        MessageBox.Show("حدث خطأ وتم التراجع عن العملية بأمان.\n\n" + ex.Message,
                            "خطأ في العملية", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw;
                    }
                }
            }
        }

        public static int ExecuteTrans(SqlTransaction trans, string sql, params SqlParameter[] prms)
        {
            using (var cmd = new SqlCommand(sql, trans.Connection, trans))
            {
                if (prms != null) cmd.Parameters.AddRange(prms);
                return cmd.ExecuteNonQuery();
            }
        }

        public static int ExecuteInsertTrans(SqlTransaction trans, string sql, params SqlParameter[] prms)
        {
            using (var cmd = new SqlCommand(sql + "; SELECT SCOPE_IDENTITY();", trans.Connection, trans))
            {
                if (prms != null) cmd.Parameters.AddRange(prms);
                var result = cmd.ExecuteScalar();
                return result == null ? -1 : Convert.ToInt32(result);
            }
        }

        public static object ScalarTrans(SqlTransaction trans, string sql, params SqlParameter[] prms)
        {
            using (var cmd = new SqlCommand(sql, trans.Connection, trans))
            {
                if (prms != null) cmd.Parameters.AddRange(prms);
                return cmd.ExecuteScalar();
            }
        }
    }
}
