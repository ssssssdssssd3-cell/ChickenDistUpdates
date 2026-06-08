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

                // Add CanShowCostProfit migration to Permissions table if not exists
                string sqlPermissionsExtra = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanShowCostProfit')
                BEGIN
                    ALTER TABLE Permissions ADD CanShowCostProfit BIT NOT NULL DEFAULT 0;
                END";
                Execute(sqlPermissionsExtra);

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

                // ===== Warehouses & Barcode & Categories =====
                string sqlWarehouses = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Warehouses')
                BEGIN
                    CREATE TABLE Warehouses (
                        WarehouseID   INT IDENTITY(1,1) PRIMARY KEY,
                        WarehouseName NVARCHAR(100) NOT NULL,
                        Location      NVARCHAR(200),
                        IsActive      BIT DEFAULT 1,
                        CreatedAt     DATETIME DEFAULT GETDATE()
                    );
                    INSERT INTO Warehouses(WarehouseName, Location) VALUES(N'المخزن الرئيسي', N'المقر الرئيسي');
                END";
                Execute(sqlWarehouses);

                string sqlCategories = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                BEGIN
                    CREATE TABLE Categories (
                        CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
                        CategoryName NVARCHAR(100) NOT NULL,
                        IsActive     BIT DEFAULT 1
                    );
                END";
                Execute(sqlCategories);

                // Add Barcode & spare-parts fields to Products
                string sqlProductsBarcode = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Barcode')
                BEGIN
                    ALTER TABLE Products ADD Barcode NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PartNumber')
                BEGIN
                    ALTER TABLE Products ADD PartNumber NVARCHAR(50) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CategoryID')
                BEGIN
                    ALTER TABLE Products ADD CategoryID INT NULL REFERENCES Categories(CategoryID);
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CarModel')
                BEGIN
                    ALTER TABLE Products ADD CarModel NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Brand')
                BEGIN
                    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ShelfLocation')
                BEGIN
                    ALTER TABLE Products ADD ShelfLocation NVARCHAR(50) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'InternationalBarcode')
                BEGIN
                    ALTER TABLE Products ADD InternationalBarcode NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'HasBarcode')
                BEGIN
                    ALTER TABLE Products ADD HasBarcode BIT DEFAULT 1;
                END";
                Execute(sqlProductsBarcode);

                // Add WarehouseID to Purchases and Sales
                string sqlWarehouseIDs = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE Purchases ADD WarehouseID INT NULL REFERENCES Warehouses(WarehouseID);
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE Sales ADD WarehouseID INT NULL REFERENCES Warehouses(WarehouseID);
                END";
                Execute(sqlWarehouseIDs);

                // ===== Indexes لتسريع الاستعلامات =====
                string sqlIndexes = @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate' AND object_id = OBJECT_ID('Sales'))
                    CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_ClientID' AND object_id = OBJECT_ID('Sales'))
                    CREATE INDEX IX_Sales_ClientID ON Sales(ClientID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_DriverID' AND object_id = OBJECT_ID('Sales'))
                    CREATE INDEX IX_Sales_DriverID ON Sales(DriverID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_SaleID' AND object_id = OBJECT_ID('SaleItems'))
                    CREATE INDEX IX_SaleItems_SaleID ON SaleItems(SaleID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_ProductID' AND object_id = OBJECT_ID('SaleItems'))
                    CREATE INDEX IX_SaleItems_ProductID ON SaleItems(ProductID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientTransactions_ClientID' AND object_id = OBJECT_ID('ClientTransactions'))
                    CREATE INDEX IX_ClientTransactions_ClientID ON ClientTransactions(ClientID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientTransactions_TransDate' AND object_id = OBJECT_ID('ClientTransactions'))
                    CREATE INDEX IX_ClientTransactions_TransDate ON ClientTransactions(TransDate);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CashBox_TransDate' AND object_id = OBJECT_ID('CashBox'))
                    CREATE INDEX IX_CashBox_TransDate ON CashBox(TransDate);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DriverLoads_DriverID' AND object_id = OBJECT_ID('DriverLoads'))
                    CREATE INDEX IX_DriverLoads_DriverID ON DriverLoads(DriverID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HandoverItems_HandoverID' AND object_id = OBJECT_ID('HandoverItems'))
                    CREATE INDEX IX_HandoverItems_HandoverID ON HandoverItems(HandoverID);";
                Execute(sqlIndexes);

                // ===== UNIQUE constraint: منع تكرار تقفيل نفس الحمولة =====
                string sqlUniqueHandover = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.indexes 
                    WHERE name = 'UQ_DriverHandovers_LoadID' AND object_id = OBJECT_ID('DriverHandovers')
                )
                BEGIN
                    -- نحذف التكرارات الموجودة إن وجدت قبل إضافة القيد
                    WITH CTE AS (
                        SELECT HandoverID,
                               ROW_NUMBER() OVER (PARTITION BY LoadID ORDER BY HandoverID DESC) AS rn
                        FROM DriverHandovers
                    )
                    DELETE FROM CTE WHERE rn > 1;

                    ALTER TABLE DriverHandovers
                        ADD CONSTRAINT UQ_DriverHandovers_LoadID UNIQUE (LoadID);
                END";
                Execute(sqlUniqueHandover);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ - لا نكتم الأخطاء بصمت
                System.Diagnostics.Debug.WriteLine("[EnsureDatabaseSchema ERROR] " + ex.Message);
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema_errors.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.Message}{Environment.NewLine}");
                }
                catch { /* لو ما قدرناش نكتب الـ log، خلّي البرنامج يشتغل بدونه */ }
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
                        MessageBox.Show("خطأ في قاعدة البيانات وتم التراجع عن العملية بأمان:\n" + ex.Message, "خطأ خطير", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
