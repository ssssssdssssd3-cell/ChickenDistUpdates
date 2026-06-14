using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;

namespace ChickenDist.Core
{
    public static class DbHelper
    {
        private static string _connStr = GetInitialConnectionString();

        private static string GetInitialConnectionString()
        {
            return GetConnectionStringFromIni();
        }

        private static string GetConnectionStringFromIni()
        {
            string iniPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
            if (!System.IO.File.Exists(iniPath))
            {
                try
                {
                    System.IO.File.WriteAllText(iniPath, 
                        "; ChickenDist Configuration\r\n" +
                        "[Database]\r\n" +
                        "Server=.\r\n" +
                        "Database=ChickenDist\r\n" +
                        "IntegratedSecurity=True\r\n" +
                        "User=\r\n" +
                        "Password=\r\n" +
                        "[General]\r\n" +
                        "Installed=True\r\n", Encoding.Unicode);
                }
                catch {}
            }

            try
            {
                string server = ReadIniDirect(iniPath, "Database", "Server", ".");
                string db = ReadIniDirect(iniPath, "Database", "Database", "ChickenDist");
                string intSec = ReadIniDirect(iniPath, "Database", "IntegratedSecurity", "True");
                string user = ReadIniDirect(iniPath, "Database", "User", "");
                string pass = ReadIniDirect(iniPath, "Database", "Password", "");

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = db,
                    IntegratedSecurity = intSec.Equals("True", StringComparison.OrdinalIgnoreCase),
                    TrustServerCertificate = true,
                    ConnectTimeout = 30
                };

                if (!builder.IntegratedSecurity)
                {
                    builder.UserID = user;
                    builder.Password = pass;
                }

                return builder.ToString();
            }
            catch
            {
                return "Data Source=.;Initial Catalog=ChickenDist;Integrated Security=True;Connect Timeout=30;";
            }
        }

        private static string ReadIniDirect(string filePath, string section, string key, string defaultValue)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return defaultValue;
                string currentSection = "";
                foreach (var line in System.IO.File.ReadAllLines(filePath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith(";")) continue;
                    if (t.StartsWith("[") && t.EndsWith("]"))
                    {
                        currentSection = t.Substring(1, t.Length - 2).Trim();
                        continue;
                    }
                    if (currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = t.IndexOf('=');
                        if (idx > 0)
                        {
                            string k = t.Substring(0, idx).Trim();
                            string v = t.Substring(idx + 1).Trim();
                            if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                            {
                                return v;
                            }
                        }
                    }
                }
            }
            catch {}
            return defaultValue;
        }

        public static void SetConnectionString(string connStr)
        {
            _connStr = connStr;
        }

        /// <summary>
        /// يُرجع نسخة من الاتصال بفاصل زمني صغير (5 ثواني) لاختبار الاتصال سريعاً عند بدء التشغيل.
        /// </summary>
        public static string GetConnectionStringForCheck()
        {
            // نستخدم الاتصال الحالي ونغيّر الفاصل إلى 5 ثواني فقط
            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(_connStr);
            builder.ConnectTimeout = 5;
            return builder.ConnectionString;
        }

        // مساعد: ينفّذ خطوة ترحيل واحدة ويسجّل الأخطاء بدون إيقاف باقي الخطوات
        private static void SafeMigrate(string stepName, string sql, params SqlParameter[] prms)
        {
            try
            {
                using (var con = GetConnection())
                using (var cmd = new SqlCommand(sql, con))
                {
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"EnsureDatabaseSchema[{stepName}] failed", ex, stepName);
                try
                {
                    string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema_errors.log");
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STEP={stepName} | {ex.Message}{Environment.NewLine}");
                }
                catch { }
                // لا نعيد الرمي — الخطوة الفاشلة معزولة ولا تؤثر على ما بعدها
            }
        }

        public static void EnsureDatabaseSchema()
        {
            // كل SafeMigrate مستقلة: فشل أي خطوة لا يوقف الباقي
            try
            {
                SafeMigrate("StockAdjustments", @"
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
                END");


                // FIX: توسيع عمود Password ليستوعب الـ hash (PBKDF2 يحتاج ~80 حرف)
                SafeMigrate("Employees.Password", @"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE object_id = OBJECT_ID('Employees') AND name = 'Password'
                      AND max_length < 400
                )
                BEGIN
                    ALTER TABLE Employees ALTER COLUMN Password NVARCHAR(200) NOT NULL;
                END");

                // Add DriverID migration to Clients table if not exists
                SafeMigrate("Clients.DriverID", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'DriverID')
                BEGIN
                    ALTER TABLE Clients ADD DriverID INT NULL FOREIGN KEY REFERENCES Employees(EmpID);
                END");

                // Add Phone2, MaxCreditLimit, Notes to Clients
                SafeMigrate("Clients.Extra", @"
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
                END");

                // Add PurchasePrice, MinStockLimit, Description to Products
                SafeMigrate("Products.Extra", @"
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
                END");

                // Add InternationalCode to Products
                SafeMigrate("Products.InternationalCode", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'InternationalCode')
                BEGIN
                    ALTER TABLE Products ADD InternationalCode NVARCHAR(100) NULL;
                END");

                // Add WastageLoss and WastageLossItems tables
                SafeMigrate("WastageLoss", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WastageLoss')
                BEGIN
                    CREATE TABLE WastageLoss (
                        WastageID INT IDENTITY(1,1) PRIMARY KEY,
                        WastageDate DATETIME DEFAULT GETDATE(),
                        WarehouseID INT NULL REFERENCES Warehouses(WarehouseID),
                        ResponsibleDriverID INT NULL REFERENCES Employees(EmpID),
                        TotalCost DECIMAL(12,2) DEFAULT 0,
                        Notes NVARCHAR(500),
                        CreatedBy INT NULL REFERENCES Employees(EmpID)
                    );
                END");

                SafeMigrate("WastageLossItems", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WastageLossItems')
                BEGIN
                    CREATE TABLE WastageLossItems (
                        ItemID INT IDENTITY(1,1) PRIMARY KEY,
                        WastageID INT NOT NULL REFERENCES WastageLoss(WastageID) ON DELETE CASCADE,
                        ProductID INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
                        Quantity DECIMAL(10,3) NOT NULL,
                        CostPrice DECIMAL(10,2) NOT NULL,
                        TotalCost DECIMAL(12,2) NOT NULL
                    );
                END");

                // Add Discount fields to Sales
                SafeMigrate("Sales.Discount", @"
                IF OBJECT_ID('Sales', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Sales', 'DiscountAmount') IS NULL
                    BEGIN
                        ALTER TABLE Sales ADD DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
                    END
                    IF COL_LENGTH('Sales', 'DiscountPct') IS NULL
                    BEGIN
                        ALTER TABLE Sales ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                    END
                END");

                // Add CloudID to Sales table for idempotency check on Cloud Import
                SafeMigrate("Sales.CloudID", @"
                IF OBJECT_ID('Sales', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Sales', 'CloudID') IS NULL
                    BEGIN
                        ALTER TABLE Sales ADD CloudID BIGINT NULL;
                    END
                END
                IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_CloudID' AND object_id = OBJECT_ID('Sales'))
                BEGIN
                    CREATE INDEX IX_Sales_CloudID ON Sales(CloudID);
                END");

                // *** الأعمدة الحرجة: DiscountPct و DiscountAmt في SaleItems ***
                SafeMigrate("SaleItems.Discount", @"
                IF OBJECT_ID('SaleItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('SaleItems', 'DiscountPct') IS NULL
                    BEGIN
                        ALTER TABLE SaleItems ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                    END
                    IF COL_LENGTH('SaleItems', 'DiscountAmt') IS NULL
                    BEGIN
                        ALTER TABLE SaleItems ADD DiscountAmt DECIMAL(10,2) NOT NULL DEFAULT 0;
                    END
                END");

                // *** ضمان إضافي (v2) لإضافة DiscountPct/DiscountAmt في حال فشل الخطوة الأولى ***
                SafeMigrate("SaleItems.DiscountV2", @"
                IF OBJECT_ID('SaleItems', 'U') IS NOT NULL AND COL_LENGTH('SaleItems', 'DiscountPct') IS NULL
                BEGIN
                    ALTER TABLE SaleItems ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                END");
                SafeMigrate("SaleItems.DiscountAmtV2", @"
                IF OBJECT_ID('SaleItems', 'U') IS NOT NULL AND COL_LENGTH('SaleItems', 'DiscountAmt') IS NULL
                BEGIN
                    ALTER TABLE SaleItems ADD DiscountAmt DECIMAL(10,2) NOT NULL DEFAULT 0;
                END");

                // *** ضمان إضافي للخصومات في جداول المشتريات (Purchases & PurchaseItems) ***
                SafeMigrate("Purchases.DiscountSafety", @"
                IF OBJECT_ID('Purchases', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Purchases', 'DiscountAmount') IS NULL
                    BEGIN
                        ALTER TABLE Purchases ADD DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
                    END
                    IF COL_LENGTH('Purchases', 'DiscountPct') IS NULL
                    BEGIN
                        ALTER TABLE Purchases ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                    END
                END");

                SafeMigrate("PurchaseItems.DiscountSafety", @"
                IF OBJECT_ID('PurchaseItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('PurchaseItems', 'DiscountPct') IS NULL
                    BEGIN
                        ALTER TABLE PurchaseItems ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
                    END
                    IF COL_LENGTH('PurchaseItems', 'DiscountAmt') IS NULL
                    BEGIN
                        ALTER TABLE PurchaseItems ADD DiscountAmt DECIMAL(10,2) NOT NULL DEFAULT 0;
                    END
                END");

                // Add Purchases and Suppliers schema (tables only)
                SafeMigrate("Purchases.Tables", @"
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
                END");

                // *** فصل DROP VIEW / CREATE VIEW إلى استدعاء مستقل لتفادي خطأ الدفعة ***
                SafeMigrate("vw_SupplierBalance.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SupplierBalance') DROP VIEW vw_SupplierBalance;");
                SafeMigrate("vw_SupplierBalance.Create", @"
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
                GROUP BY s.SupplierID, s.SupplierName, s.Phone, s.OpeningBalance');");

                // Add SupplierID to Expenses (optional link to Suppliers)
                SafeMigrate("Expenses.SupplierID", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SupplierID')
                BEGIN
                    ALTER TABLE Expenses ADD SupplierID INT NULL;
                    ALTER TABLE Expenses ADD CONSTRAINT FK_Expenses_Suppliers FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID);
                END");

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
                SafeMigrate("vw_EmployeeBalance.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_EmployeeBalance') DROP VIEW vw_EmployeeBalance;");
                SafeMigrate("vw_EmployeeBalance.Create", @"
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
                GROUP BY e.EmpID, e.EmpName, e.Phone');");

                // ===== أعمدة السعر المعلق وهامش الربح على Products =====
                string sqlProductPricing = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CostPrice')
                BEGIN
                    ALTER TABLE Products ADD CostPrice DECIMAL(10,3) NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingSalePrice')
                BEGIN
                    ALTER TABLE Products ADD PendingSalePrice DECIMAL(10,3) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingQtyThreshold')
                BEGIN
                    -- الكمية القديمة المتبقية عند تسجيل السعر المعلق
                    -- عندما يصل المخزون إلى هذا الرقم أو أقل → يتفعل السعر الجديد
                    ALTER TABLE Products ADD PendingQtyThreshold DECIMAL(10,3) NULL;
                END";
                Execute(sqlProductPricing);

                // ===== عمود سعر البيع المقترح في بنود الشراء =====
                string sqlPurchaseItemsSalePrice = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseItems') AND name = 'SuggestedSalePrice')
                BEGIN
                    ALTER TABLE PurchaseItems ADD SuggestedSalePrice DECIMAL(10,3) NULL;
                END";
                Execute(sqlPurchaseItemsSalePrice);

                // ===== جدول الإصدار لعدم تشغيل إصدارات قديمة =====
                // 1. إنشاء الجدول بالأعمدة الأساسية إن لم يكن موجوداً
                string sqlVersionTableCreate = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'version')
                BEGIN
                    CREATE TABLE [version] (
                        [version] NVARCHAR(50) NOT NULL
                    );
                    INSERT INTO [version] ([version]) VALUES ('1.0.0');
                END";
                Execute(sqlVersionTableCreate);

                // 2. إضافة عمود وقت التحديث بشكل منفصل لتفادي خطأ التجميع في SQL Server
                string sqlVersionTableAlter = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('version') AND name = 'UpdatedAt')
                BEGIN
                    ALTER TABLE [version] ADD [UpdatedAt] DATETIME NULL;
                END";
                Execute(sqlVersionTableAlter);

                // --- ترحيلات نظام المخازن وقطع الغيار ---

                // 1. تكبير طول كود الصنف ليستوعب الباركود الطويل
                string sqlUpgradeProductCode = @"
                IF EXISTS (
                    SELECT * FROM sys.columns
                    WHERE object_id = OBJECT_ID('Products') AND name = 'ProductCode'
                      AND max_length < 100
                )
                BEGIN
                    ALTER TABLE Products ALTER COLUMN ProductCode NVARCHAR(50) NULL;
                END";
                Execute(sqlUpgradeProductCode);

                // 2. إنشاء جدول المخازن Warehouses
                string sqlWarehousesTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Warehouses')
                BEGIN
                    CREATE TABLE Warehouses (
                        WarehouseID INT IDENTITY(1,1) PRIMARY KEY,
                        WarehouseName NVARCHAR(100) NOT NULL,
                        Location NVARCHAR(200) NULL,
                        Notes NVARCHAR(500) NULL,
                        IsActive BIT DEFAULT 1,
                        CreatedAt DATETIME DEFAULT GETDATE()
                    );
                    -- إدخال المخزن الرئيسي الافتراضي
                    INSERT INTO Warehouses (WarehouseName, Location, Notes, IsActive)
                    VALUES (N'المخزن الرئيسي', N'المقر الرئيسي', N'المخزن الأساسي للنظام', 1);
                END";
                Execute(sqlWarehousesTable);

                // 3. إنشاء جدول التصنيفات Categories لقطع الغيار
                string sqlCategoriesTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                BEGIN
                    CREATE TABLE Categories (
                        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
                        CategoryName NVARCHAR(100) NOT NULL,
                        IsActive BIT DEFAULT 1
                    );
                END";
                Execute(sqlCategoriesTable);

                // 4. إضافة حقول قطع الغيار لجدول الأصناف Products
                string sqlProductsPartsFields = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PartNumber')
                BEGIN
                    ALTER TABLE Products ADD PartNumber NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CategoryID')
                BEGIN
                    ALTER TABLE Products ADD CategoryID INT NULL FOREIGN KEY REFERENCES Categories(CategoryID);
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CarModel')
                BEGIN
                    ALTER TABLE Products ADD CarModel NVARCHAR(200) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Brand')
                BEGIN
                    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ShelfLocation')
                BEGIN
                    ALTER TABLE Products ADD ShelfLocation NVARCHAR(100) NULL;
                END";
                Execute(sqlProductsPartsFields);

                // 5. إضافة معرف المخزن WarehouseID لجداول الحركات وربطه
                string sqlAddWarehouseID = @"
                -- Purchases
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE Purchases ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE Purchases SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END
                -- Sales
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE Sales ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE Sales SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END
                -- SalesReturns
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SalesReturns') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE SalesReturns ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE SalesReturns SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END
                -- PurchaseReturns
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseReturns') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE PurchaseReturns ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE PurchaseReturns SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END
                -- StockAdjustments
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StockAdjustments') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE StockAdjustments ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE StockAdjustments SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END
                -- DriverLoads
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DriverLoads') AND name = 'WarehouseID')
                BEGIN
                    ALTER TABLE DriverLoads ADD WarehouseID INT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID);
                    EXEC('UPDATE DriverLoads SET WarehouseID = 1 WHERE WarehouseID IS NULL');
                END";
                Execute(sqlAddWarehouseID);

                // 6. إنشاء جداول التحويلات بين المخازن WarehouseTransfers & WarehouseTransferItems
                string sqlTransfersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WarehouseTransfers')
                BEGIN
                    CREATE TABLE WarehouseTransfers (
                        TransferID INT IDENTITY(1,1) PRIMARY KEY,
                        TransferCode NVARCHAR(20) NOT NULL,
                        TransferDate DATETIME DEFAULT GETDATE(),
                        FromWarehouseID INT NOT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID),
                        ToWarehouseID INT NOT NULL FOREIGN KEY REFERENCES Warehouses(WarehouseID),
                        Notes NVARCHAR(500) NULL,
                        CreatedBy INT REFERENCES Employees(EmpID),
                        IsPosted BIT DEFAULT 1
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WarehouseTransferItems')
                BEGIN
                    CREATE TABLE WarehouseTransferItems (
                        ItemID INT IDENTITY(1,1) PRIMARY KEY,
                        TransferID INT NOT NULL FOREIGN KEY REFERENCES WarehouseTransfers(TransferID) ON DELETE CASCADE,
                        ProductID INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID),
                        Quantity DECIMAL(10,3) NOT NULL
                    );
                END";
                Execute(sqlTransfersTable);

                // 7. ترحيلات تعديل الفواتير وشرائح الأسعار والأرشفة
                string sqlInvoiceEditingAndTiers = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanEditSalesInvoice')
                BEGIN
                    ALTER TABLE Permissions ADD CanEditSalesInvoice BIT DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewCost')
                BEGIN
                    ALTER TABLE Permissions ADD CanViewCost BIT DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanDeleteSalesInvoice')
                BEGIN
                    ALTER TABLE Permissions ADD CanDeleteSalesInvoice BIT DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanCopySalesInvoice')
                BEGIN
                    ALTER TABLE Permissions ADD CanCopySalesInvoice BIT DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'WholesalePrice')
                BEGIN
                    ALTER TABLE Products ADD WholesalePrice DECIMAL(10,2) DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SemiWholesalePrice')
                BEGIN
                    ALTER TABLE Products ADD SemiWholesalePrice DECIMAL(10,2) DEFAULT 0;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PriceTier')
                BEGIN
                    ALTER TABLE Sales ADD PriceTier NVARCHAR(20) DEFAULT N'قطاعي';
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'PriceTier')
                BEGIN
                    ALTER TABLE SaleItems ADD PriceTier NVARCHAR(20) DEFAULT N'قطاعي';
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'DefaultPriceTier')
                BEGIN
                    ALTER TABLE Clients ADD DefaultPriceTier NVARCHAR(20) DEFAULT N'قطاعي';
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesAudit')
                BEGIN
                    CREATE TABLE SalesAudit (
                        AuditID INT IDENTITY(1,1) PRIMARY KEY,
                        SaleID INT NOT NULL,
                        UserID INT NOT NULL REFERENCES Employees(EmpID),
                        EditDate DATETIME DEFAULT GETDATE(),
                        OldTotal DECIMAL(10,2) NOT NULL,
                        NewTotal DECIMAL(10,2) NOT NULL,
                        Notes NVARCHAR(500) NULL
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleItemsHistory')
                BEGIN
                    CREATE TABLE SaleItemsHistory (
                        HistoryID INT IDENTITY(1,1) PRIMARY KEY,
                        AuditID INT NOT NULL REFERENCES SalesAudit(AuditID) ON DELETE CASCADE,
                        SaleID INT NOT NULL,
                        ProductID INT NOT NULL REFERENCES Products(ProductID),
                        Quantity DECIMAL(10,3) NOT NULL,
                        UnitPrice DECIMAL(10,2) NOT NULL,
                        TotalPrice DECIMAL(10,2) NOT NULL,
                        DiscountPct DECIMAL(5,2) DEFAULT 0,
                        DiscountAmt DECIMAL(10,2) DEFAULT 0,
                        PriceTier NVARCHAR(20) NULL
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SalesAudit') AND name = 'MachineName')
                BEGIN
                    ALTER TABLE SalesAudit ADD MachineName NVARCHAR(100) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SalesAudit') AND name = 'ActionType')
                BEGIN
                    ALTER TABLE SalesAudit ADD ActionType NVARCHAR(50) NOT NULL DEFAULT 'EDIT';
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'LastModifiedDate')
                BEGIN
                    ALTER TABLE Sales ADD LastModifiedDate DATETIME NULL;
                END
                EXEC('UPDATE Sales SET LastModifiedDate = GETDATE() WHERE LastModifiedDate IS NULL');";
                Execute(sqlInvoiceEditingAndTiers);

                // ===== جدول رصيد الأصناف لكل مخزن (ProductStock) =====
                string sqlProductStock = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductStock')
                BEGIN
                    CREATE TABLE ProductStock (
                        StockID     INT IDENTITY(1,1) PRIMARY KEY,
                        ProductID   INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
                        WarehouseID INT NOT NULL REFERENCES Warehouses(WarehouseID),
                        Quantity    DECIMAL(10,3) NOT NULL DEFAULT 0,
                        LastUpdated DATETIME DEFAULT GETDATE(),
                        CONSTRAINT UQ_ProductStock UNIQUE (ProductID, WarehouseID)
                    );
                END";
                Execute(sqlProductStock);

                // ===== عرض رصيد العملاء vw_ClientBalance =====
                SafeMigrate("vw_ClientBalance.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ClientBalance') DROP VIEW vw_ClientBalance;");
                SafeMigrate("vw_ClientBalance.Create", @"
                EXEC('CREATE VIEW vw_ClientBalance AS
                SELECT
                    c.ClientID,
                    c.ClientName,
                    c.Phone,
                    c.OpeningBalance,
                    ISNULL(SUM(CASE WHEN ct.TransType IN (''Sale'',''DriverLoad'') THEN ct.Debit ELSE 0 END), 0) AS TotalSales,
                    ISNULL(SUM(CASE WHEN ct.TransType = ''Payment'' THEN ct.Credit ELSE 0 END), 0) AS TotalPayments,
                    c.OpeningBalance
                        + ISNULL(SUM(ct.Debit), 0)
                        - ISNULL(SUM(ct.Credit), 0) AS Balance
                FROM Clients c
                LEFT JOIN ClientTransactions ct ON c.ClientID = ct.ClientID
                GROUP BY c.ClientID, c.ClientName, c.Phone, c.OpeningBalance');");

                // ===== عرض رصيد الخزنة vw_CashBalance =====
                SafeMigrate("vw_CashBalance.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CashBalance') DROP VIEW vw_CashBalance;");
                SafeMigrate("vw_CashBalance.Create", @"
                EXEC('CREATE VIEW vw_CashBalance AS
                SELECT
                    ISNULL(SUM(AmountIn),0)  AS TotalIn,
                    ISNULL(SUM(AmountOut),0) AS TotalOut,
                    ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) AS Balance
                FROM CashBox');");

                // ===== عرض مخزون الأصناف vw_ProductStock =====
                SafeMigrate("vw_ProductStock.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ProductStock') DROP VIEW vw_ProductStock;");
                SafeMigrate("vw_ProductStock.Create", @"
                EXEC('CREATE VIEW vw_ProductStock AS
                SELECT
                    p.ProductID,
                    p.ProductCode,
                    p.ProductName,
                    p.PartNumber,
                    p.MinStockLimit,
                    w.WarehouseID,
                    w.WarehouseName,
                    ISNULL(ps.Quantity, 0) AS StockQty,
                    CASE WHEN ISNULL(ps.Quantity, 0) <= p.MinStockLimit THEN 1 ELSE 0 END AS IsBelowMin
                FROM Products p
                CROSS JOIN Warehouses w
                LEFT JOIN ProductStock ps ON ps.ProductID = p.ProductID AND ps.WarehouseID = w.WarehouseID
                WHERE p.IsActive = 1 AND w.IsActive = 1');");

                // ===== عمود المديونية الحالية في جدول العملاء =====
                string sqlClientDebtCol = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'CurrentDebt')
                BEGIN
                    ALTER TABLE Clients ADD CurrentDebt DECIMAL(12,2) NOT NULL DEFAULT 0;
                    -- تعبئة أولية بالأرصدة الموجودة فعلاً (استخدام EXEC لتفادي خطأ التحقق من وجود العمود وقت تصريف الدفعة)
                    EXEC('UPDATE c
                          SET c.CurrentDebt = c.OpeningBalance
                              + ISNULL((SELECT SUM(ct.Debit) - SUM(ct.Credit)
                                        FROM ClientTransactions ct
                                        WHERE ct.ClientID = c.ClientID), 0)
                          FROM Clients c');
                END";
                Execute(sqlClientDebtCol);

                // تريغر يُحدّث CurrentDebt تلقائياً عند أي إدراج/تعديل/حذف في ClientTransactions
                Execute(@"IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_UpdateClientCurrentDebt')
                              DROP TRIGGER tr_UpdateClientCurrentDebt;");
                Execute(@"EXEC('CREATE TRIGGER tr_UpdateClientCurrentDebt
                ON ClientTransactions
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    ;WITH affected AS (
                        SELECT ClientID FROM inserted
                        UNION
                        SELECT ClientID FROM deleted
                    )
                    UPDATE c
                    SET c.CurrentDebt = c.OpeningBalance
                        + ISNULL((SELECT SUM(ct.Debit) - SUM(ct.Credit)
                                  FROM ClientTransactions ct
                                  WHERE ct.ClientID = c.ClientID), 0)
                    FROM Clients c
                    WHERE c.ClientID IN (SELECT ClientID FROM affected);
                END')");

                // ===== عرض سجل جميع حركات الأصناف vw_AllStockMovements =====
                SafeMigrate("vw_AllStockMovements.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AllStockMovements') DROP VIEW vw_AllStockMovements;");
                SafeMigrate("vw_AllStockMovements.Create", @"
                EXEC('CREATE VIEW vw_AllStockMovements AS

                -- 1. مبيعات (صادر)
                SELECT
                    s.SaleDate       AS MovDate,
                    CASE s.SaleType
                        WHEN ''Cash''       THEN N''بيع نقدي''
                        WHEN ''Credit''     THEN N''بيع آجل''
                        WHEN ''Installment'' THEN N''تقسيط شرعي''
                        ELSE                     N''تحميل مندوب''
                    END              AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    w.WarehouseName,
                    s.SaleCode       AS RefCode,
                    ISNULL(c.ClientName, e.EmpName) AS PersonName,
                    0.000            AS QtyIn,
                    si.Quantity      AS QtyOut,
                    s.Notes
                FROM SaleItems si
                JOIN Sales      s  ON si.SaleID     = s.SaleID
                JOIN Products   p  ON si.ProductID  = p.ProductID
                JOIN Warehouses w  ON s.WarehouseID = w.WarehouseID
                LEFT JOIN Clients   c ON s.ClientID = c.ClientID
                LEFT JOIN Employees e ON s.DriverID = e.EmpID
                WHERE s.IsPosted = 1

                UNION ALL

                -- 2. مرتجع مبيعات (وارد)
                SELECT
                    sr.ReturnDate    AS MovDate,
                    N''مرتجع مبيعات'' AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    w.WarehouseName,
                    ISNULL(sl.SaleCode, N''---'') AS RefCode,
                    ISNULL(c.ClientName,   N''---'') AS PersonName,
                    ri.Quantity      AS QtyIn,
                    0.000            AS QtyOut,
                    sr.Notes
                FROM ReturnItems ri
                JOIN SalesReturns sr ON ri.ReturnID    = sr.ReturnID
                JOIN Products     p  ON ri.ProductID   = p.ProductID
                JOIN Warehouses   w  ON sr.WarehouseID = w.WarehouseID
                LEFT JOIN Sales   sl ON sr.SaleID      = sl.SaleID
                LEFT JOIN Clients c  ON sr.ClientID    = c.ClientID

                UNION ALL

                -- 3. مشتريات (وارد)
                SELECT
                    pu.PurchaseDate  AS MovDate,
                    N''شراء''          AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    w.WarehouseName,
                    pu.PurchaseCode  AS RefCode,
                    ISNULL(sup.SupplierName, N''---'') AS PersonName,
                    pi.Quantity      AS QtyIn,
                    0.000            AS QtyOut,
                    pu.Notes
                FROM PurchaseItems pi
                JOIN Purchases  pu  ON pi.PurchaseID  = pu.PurchaseID
                JOIN Products   p   ON pi.ProductID   = p.ProductID
                JOIN Warehouses w   ON pu.WarehouseID = w.WarehouseID
                LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                WHERE pu.IsPosted = 1

                UNION ALL

                -- 4. مرتجع مشتريات (صادر)
                SELECT
                    pr.ReturnDate    AS MovDate,
                    N''مرتجع مشتريات'' AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    w.WarehouseName,
                    N''مرتجع #'' + CAST(pr.ReturnID AS NVARCHAR(20)) AS RefCode,
                    ISNULL(sup.SupplierName, N''---'') AS PersonName,
                    0.000            AS QtyIn,
                    pri.Quantity     AS QtyOut,
                    pr.Notes
                FROM PurchaseReturnItems pri
                JOIN PurchaseReturns pr ON pri.ReturnID    = pr.ReturnID
                JOIN Products        p  ON pri.ProductID   = p.ProductID
                JOIN Warehouses      w  ON pr.WarehouseID  = w.WarehouseID
                LEFT JOIN Suppliers sup ON pr.SupplierID   = sup.SupplierID

                UNION ALL

                -- 5. تسويات جردية
                SELECT
                    sa.AdjDate       AS MovDate,
                    N''تسوية جردية''   AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    w.WarehouseName,
                    N''تسوية #'' + CAST(sa.AdjID AS NVARCHAR(20)) AS RefCode,
                    ISNULL(e.EmpName, N''---'') AS PersonName,
                    CASE WHEN (sa.ActualQty - sa.BookQty) > 0 THEN (sa.ActualQty - sa.BookQty) ELSE 0 END AS QtyIn,
                    CASE WHEN (sa.ActualQty - sa.BookQty) < 0 THEN ABS(sa.ActualQty - sa.BookQty) ELSE 0 END AS QtyOut,
                    sa.Notes
                FROM StockAdjustments sa
                JOIN Products   p  ON sa.ProductID   = p.ProductID
                JOIN Warehouses w  ON sa.WarehouseID = w.WarehouseID
                LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID

                UNION ALL

                -- 6. تحويلات وارد (الى المخزن)
                SELECT
                    t.TransferDate   AS MovDate,
                    N''تحويل وارد''    AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    wTo.WarehouseName,
                    t.TransferCode   AS RefCode,
                    N''من: '' + wFrom.WarehouseName AS PersonName,
                    ti.Quantity      AS QtyIn,
                    0.000            AS QtyOut,
                    t.Notes
                FROM WarehouseTransferItems ti
                JOIN WarehouseTransfers t  ON ti.TransferID       = t.TransferID
                JOIN Products           p  ON ti.ProductID        = p.ProductID
                JOIN Warehouses      wFrom ON t.FromWarehouseID   = wFrom.WarehouseID
                JOIN Warehouses        wTo ON t.ToWarehouseID     = wTo.WarehouseID
                WHERE t.IsPosted = 1

                UNION ALL

                -- 7. تحويلات صادر (من المخزن)
                SELECT
                    t.TransferDate   AS MovDate,
                    N''تحويل صادر''   AS ChangeType,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    wFrom.WarehouseName,
                    t.TransferCode   AS RefCode,
                    N''إلى: '' + wTo.WarehouseName AS PersonName,
                    0.000            AS QtyIn,
                    ti.Quantity      AS QtyOut,
                    t.Notes
                FROM WarehouseTransferItems ti
                JOIN WarehouseTransfers t  ON ti.TransferID       = t.TransferID
                JOIN Products           p  ON ti.ProductID        = p.ProductID
                JOIN Warehouses      wFrom ON t.FromWarehouseID   = wFrom.WarehouseID
                JOIN Warehouses        wTo ON t.ToWarehouseID     = wTo.WarehouseID
                WHERE t.IsPosted = 1');");

                // ===== عرض الكميات الفعلية الحالية لكل صنف في كل مخزن =====
                SafeMigrate("vw_CurrentStockByWarehouse.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CurrentStockByWarehouse') DROP VIEW vw_CurrentStockByWarehouse;");
                SafeMigrate("vw_CurrentStockByWarehouse.Create", @"
                EXEC('CREATE VIEW vw_CurrentStockByWarehouse AS
                SELECT
                    p.ProductID,
                    p.ProductCode,
                    p.PartNumber,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,
                    p.MinStockLimit,
                    w.WarehouseID,
                    w.WarehouseName,
                    ISNULL(adj.ActualQty, 0)
                    + ISNULL((SELECT SUM(ri.Quantity)
                              FROM ReturnItems ri
                              JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                              WHERE ri.ProductID = p.ProductID
                                AND sr.WarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(hi.ReturnedQty)
                              FROM HandoverItems hi
                              JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                              JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                              WHERE hi.ProductID = p.ProductID
                                AND dl.WarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(pi.Quantity)
                              FROM PurchaseItems pi
                              JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                              WHERE pi.ProductID = p.ProductID
                                AND pu.IsPosted = 1
                                AND pu.WarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(ti.Quantity)
                              FROM WarehouseTransferItems ti
                              JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                              WHERE ti.ProductID = p.ProductID
                                AND t.IsPosted = 1
                                AND t.ToWarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(pri.Quantity)
                              FROM PurchaseReturnItems pri
                              JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                              WHERE pri.ProductID = p.ProductID
                                AND pr.WarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(si.Quantity)
                              FROM SaleItems si
                              JOIN Sales s ON si.SaleID = s.SaleID
                              WHERE si.ProductID = p.ProductID
                                AND s.IsPosted = 1
                                AND s.WarehouseID = w.WarehouseID
                                AND (s.SaleType = ''DriverLoad'' OR (s.SaleType IN (''Cash'', ''Credit'', ''Installment'') AND s.DriverID IS NULL))
                                AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(ti2.Quantity)
                              FROM WarehouseTransferItems ti2
                              JOIN WarehouseTransfers t2 ON ti2.TransferID = t2.TransferID
                              WHERE ti2.ProductID = p.ProductID
                                AND t2.IsPosted = 1
                                AND t2.FromWarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR t2.TransferDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(wli.Quantity)
                              FROM WastageLossItems wli
                              JOIN WastageLoss wl ON wli.WastageID = wl.WastageID
                              WHERE wli.ProductID = p.ProductID
                                AND wl.WarehouseID = w.WarehouseID
                                AND (adj.AdjDate IS NULL OR wl.WastageDate > adj.AdjDate)), 0)
                    AS CurrentQty,
                    adj.AdjDate AS LastAdjDate
                FROM Products p
                CROSS JOIN Warehouses w
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty
                    FROM StockAdjustments sa
                    WHERE sa.ProductID = p.ProductID
                      AND sa.WarehouseID = w.WarehouseID
                    ORDER BY sa.AdjDate DESC
                ) adj
                WHERE p.IsActive = 1 AND w.IsActive = 1');");

                // ===== عمود كلمة المرور الأصلية للموظفين (للمراجعة الإدارية فقط) =====
                Execute(@"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'PlainPassword')
                BEGIN
                    ALTER TABLE Employees ADD PlainPassword NVARCHAR(200) NULL;
                END");

                // ===== Installment Module Migration =====
                string sqlInstallments = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentContracts')
                BEGIN
                    CREATE TABLE InstallmentContracts (
                        ContractID         INT IDENTITY(1,1) PRIMARY KEY,
                        ContractGUID       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() UNIQUE,
                        ContractCode       NVARCHAR(50) NULL,
                        BranchID           INT NOT NULL DEFAULT 1,
                        InvoiceID          INT NULL REFERENCES Sales(SaleID) ON DELETE SET NULL,
                        CustomerID         INT NOT NULL REFERENCES Clients(ClientID),
                        SaleType           NVARCHAR(20) NOT NULL DEFAULT 'Installment',
                        ContractAmount     DECIMAL(10,2) NOT NULL,
                        DownPayment        DECIMAL(10,2) NOT NULL DEFAULT 0,
                        FinancedAmount     DECIMAL(10,2) NOT NULL,
                        InstallmentCount   INT NOT NULL DEFAULT 1,
                        InstallmentValue   DECIMAL(10,2) NOT NULL,
                        StartDate          DATETIME NOT NULL,
                        Status             NVARCHAR(20) NOT NULL DEFAULT 'Active',
                        Notes              NVARCHAR(500) NULL,
                        CreatedBy          INT NULL REFERENCES Employees(EmpID),
                        CreatedDate        DATETIME NOT NULL DEFAULT GETDATE(),
                        LastModifiedBy     INT NULL REFERENCES Employees(EmpID),
                        LastModifiedDate   DATETIME NULL
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InstallmentContracts') AND name = 'ContractCode')
                    BEGIN
                        ALTER TABLE InstallmentContracts ADD ContractCode NVARCHAR(50) NULL;
                    END
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentSchedules')
                BEGIN
                    CREATE TABLE InstallmentSchedules (
                        ScheduleID         INT IDENTITY(1,1) PRIMARY KEY,
                        ScheduleGUID       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() UNIQUE,
                        ContractID         INT NOT NULL REFERENCES InstallmentContracts(ContractID) ON DELETE CASCADE,
                        InstallmentNo      INT NOT NULL,
                        DueDate            DATETIME NOT NULL,
                        Amount             DECIMAL(10,2) NOT NULL,
                        PaidAmount         DECIMAL(10,2) NOT NULL DEFAULT 0,
                        RemainingAmount    DECIMAL(10,2) NOT NULL,
                        PaidDate           DATETIME NULL,
                        Status             NVARCHAR(20) NOT NULL DEFAULT 'Pending'
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentPayments')
                BEGIN
                    CREATE TABLE InstallmentPayments (
                        PaymentID          INT IDENTITY(1,1) PRIMARY KEY,
                        PaymentGUID        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() UNIQUE,
                        ContractID         INT NOT NULL REFERENCES InstallmentContracts(ContractID),
                        ScheduleID         INT NOT NULL REFERENCES InstallmentSchedules(ScheduleID),
                        BranchID           INT NOT NULL DEFAULT 1,
                        PaymentDate        DATETIME NOT NULL DEFAULT GETDATE(),
                        Amount             DECIMAL(10,2) NOT NULL,
                        PaymentMethod      NVARCHAR(20) NOT NULL DEFAULT 'Cash',
                        SafeID             INT NOT NULL DEFAULT 1,
                        UserID             INT NULL REFERENCES Employees(EmpID),
                        Notes              NVARCHAR(500) NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentAuditLog')
                BEGIN
                    CREATE TABLE InstallmentAuditLog (
                        LogID              INT IDENTITY(1,1) PRIMARY KEY,
                        Action             NVARCHAR(50) NOT NULL,
                        ContractID         INT NOT NULL,
                        UserID             INT NULL REFERENCES Employees(EmpID),
                        LogDate            DATETIME NOT NULL DEFAULT GETDATE(),
                        MachineName        NVARCHAR(100) NOT NULL,
                        OldValue           NVARCHAR(MAX) NULL,
                        NewValue           NVARCHAR(MAX) NULL
                    );
                END

                -- Indexes for Installment tables
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentContracts_CustomerID' AND object_id = OBJECT_ID('InstallmentContracts'))
                    CREATE INDEX IX_InstallmentContracts_CustomerID ON InstallmentContracts(CustomerID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentContracts_InvoiceID' AND object_id = OBJECT_ID('InstallmentContracts'))
                    CREATE INDEX IX_InstallmentContracts_InvoiceID ON InstallmentContracts(InvoiceID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentContracts_BranchID' AND object_id = OBJECT_ID('InstallmentContracts'))
                    CREATE INDEX IX_InstallmentContracts_BranchID ON InstallmentContracts(BranchID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentSchedules_ContractID' AND object_id = OBJECT_ID('InstallmentSchedules'))
                    CREATE INDEX IX_InstallmentSchedules_ContractID ON InstallmentSchedules(ContractID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentSchedules_DueDate_Status' AND object_id = OBJECT_ID('InstallmentSchedules'))
                    CREATE INDEX IX_InstallmentSchedules_DueDate_Status ON InstallmentSchedules(DueDate, Status);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentPayments_ContractID' AND object_id = OBJECT_ID('InstallmentPayments'))
                    CREATE INDEX IX_InstallmentPayments_ContractID ON InstallmentPayments(ContractID);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InstallmentPayments_PaymentDate' AND object_id = OBJECT_ID('InstallmentPayments'))
                    CREATE INDEX IX_InstallmentPayments_PaymentDate ON InstallmentPayments(PaymentDate);
                ";
                Execute(sqlInstallments);

                // ===== Detailed Safes & Bank Accounts Migration =====
                string sqlSafeAccounts = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SafeAccounts')
                BEGIN
                    CREATE TABLE SafeAccounts (
                        AccountID      INT IDENTITY(1,1) PRIMARY KEY,
                        AccountName    NVARCHAR(100) NOT NULL UNIQUE,
                        AccountType    NVARCHAR(20) NOT NULL, -- 'Cash', 'Bank', 'Visa'
                        AccountNumber  NVARCHAR(50) NULL,
                        OpeningBalance DECIMAL(12,2) NOT NULL DEFAULT 0,
                        IsActive       BIT NOT NULL DEFAULT 1,
                        CreatedAt      DATETIME NOT NULL DEFAULT GETDATE()
                    );
                    -- Insert default accounts
                    INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                    VALUES (N'الخزينة الرئيسية', 'Cash', NULL, 0.00, 1);
                    
                    INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                    VALUES (N'حساب البنك الأهلي', 'Bank', N'1234567890', 0.00, 1);
                    
                    INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                    VALUES (N'فيزا المحل', 'Visa', NULL, 0.00, 1);
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CashBox') AND name = 'AccountID')
                BEGIN
                    ALTER TABLE CashBox ADD AccountID INT NULL;
                    ALTER TABLE CashBox ADD CONSTRAINT FK_CashBox_SafeAccounts FOREIGN KEY (AccountID) REFERENCES SafeAccounts(AccountID);
                    EXEC('UPDATE CashBox SET AccountID = 1 WHERE AccountID IS NULL');
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SafeAccountID')
                BEGIN
                    ALTER TABLE Expenses ADD SafeAccountID INT NULL;
                    ALTER TABLE Expenses ADD CONSTRAINT FK_Expenses_SafeAccounts FOREIGN KEY (SafeAccountID) REFERENCES SafeAccounts(AccountID);
                    EXEC('UPDATE Expenses SET SafeAccountID = 1 WHERE SafeAccountID IS NULL');
                END

                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_InstallmentPayments_SafeAccounts') AND EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentPayments')
                BEGIN
                    ALTER TABLE InstallmentPayments ADD CONSTRAINT FK_InstallmentPayments_SafeAccounts FOREIGN KEY (SafeID) REFERENCES SafeAccounts(AccountID);
                END
                ";
                Execute(sqlSafeAccounts);

                SafeMigrate("vw_SafeAccountBalances.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SafeAccountBalances') DROP VIEW vw_SafeAccountBalances;");
                SafeMigrate("vw_SafeAccountBalances.Create", @"
                EXEC('CREATE VIEW vw_SafeAccountBalances AS
                SELECT
                    sa.AccountID,
                    sa.AccountName,
                    sa.AccountType,
                    sa.AccountNumber,
                    sa.OpeningBalance,
                    ISNULL(SUM(cb.AmountIn),0) AS TotalIn,
                    ISNULL(SUM(cb.AmountOut),0) AS TotalOut,
                    sa.OpeningBalance + ISNULL(SUM(cb.AmountIn),0) - ISNULL(SUM(cb.AmountOut),0) AS Balance
                FROM SafeAccounts sa
                LEFT JOIN CashBox cb ON sa.AccountID = cb.AccountID
                WHERE sa.IsActive = 1
                GROUP BY sa.AccountID, sa.AccountName, sa.AccountType, sa.AccountNumber, sa.OpeningBalance');");

                // ===== Price Change History & Pending Price Reference Migration =====
                string sqlPriceChangesSchema = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PriceChangesLog')
                BEGIN
                    CREATE TABLE PriceChangesLog (
                        LogID         INT IDENTITY(1,1) PRIMARY KEY,
                        ProductID     INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID) ON DELETE CASCADE,
                        OldPrice      DECIMAL(10,2) NOT NULL,
                        NewPrice      DECIMAL(10,2) NOT NULL,
                        ChangeSource  NVARCHAR(50) NOT NULL,
                        SourceRefID   INT NULL,
                        ChangeDate    DATETIME NOT NULL DEFAULT GETDATE(),
                        UserID        INT NULL FOREIGN KEY REFERENCES Employees(EmpID),
                        Notes         NVARCHAR(500) NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingPriceSourceRefID')
                BEGIN
                    ALTER TABLE Products ADD PendingPriceSourceRefID INT NULL;
                END";
                Execute(sqlPriceChangesSchema);
            }
            catch (Exception ex)
            {
                AppLogger.Error("EnsureDatabaseSchema overall process failed", ex);
                MessageBox.Show("فشل تطبيق بعض ترحيلات قاعدة البيانات:\n" + ex.Message, "تنبيه في التهيئة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // لا نعيد رمي الاستثناء حتى لا يتعطل تشغيل التطبيق بالكامل في حال وجود أخطاء طفيفة
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

        /// <summary>
        /// تنفيذ SELECT داخل Transaction قائمة وإرجاع DataTable.
        /// تُستخدم لقراءة بيانات قبل تعديلها ضمن نفس الـ Transaction.
        /// </summary>
        public static DataTable QueryTrans(SqlTransaction trans, string sql, params SqlParameter[] prms)
        {
            using (var cmd = new SqlCommand(sql, trans.Connection, trans))
            {
                if (prms != null) cmd.Parameters.AddRange(prms);
                var dt = new DataTable();
                using (var adapter = new SqlDataAdapter(cmd))
                    adapter.Fill(dt);
                return dt;
            }
        }

        /// <summary>
        /// يتحقق من توافق إصدار التطبيق الحالي مع إصدار قاعدة البيانات.
        /// ويمنع التشغيل إذا كانت قاعدة البيانات قد تم ترقيتها بإصدار أحدث.
        /// </summary>
        public static bool CheckAndEnforceVersion(string currentAppVersion)
        {
            try
            {
                // قراءة الإصدار الحالي من جدول version
                object dbVerObj = Scalar("SELECT TOP 1 [version] FROM [version]");
                if (dbVerObj == null || dbVerObj == DBNull.Value)
                {
                    // إذا كان الجدول فارغاً لسبب ما، نضع الإصدار الحالي ونسمح بالدخول
                    Execute("DELETE FROM [version]; INSERT INTO [version] ([version], [UpdatedAt]) VALUES (@ver, GETDATE())", P("@ver", currentAppVersion));
                    return true;
                }

                string dbVersionStr = dbVerObj.ToString().Trim();
                Version appVer = new Version(currentAppVersion);
                Version dbVer = new Version(dbVersionStr);

                if (appVer >= dbVer)
                {
                    // إذا كان إصدار البرنامج الحالي أحدث، نقوم بتحديث رقم الإصدار في قاعدة البيانات
                    if (appVer > dbVer)
                    {
                        Execute("DELETE FROM [version]; INSERT INTO [version] ([version], [UpdatedAt]) VALUES (@ver, GETDATE())", P("@ver", currentAppVersion));
                    }
                    return true;
                }
                else
                {
                    // إذا كان إصدار البرنامج الحالي أقدم من قاعدة البيانات
                    string errorMsg = $"⚠️ هذا الإصدار من البرنامج قديم جداً وغير متوافق مع قاعدة البيانات الحالية المحدثة.\n\n" +
                                      $"إصدار البرنامج الحالي: {currentAppVersion}\n" +
                                      $"إصدار قاعدة البيانات المحدث: {dbVersionStr}\n\n" +
                                      $"لقد تم تحديث البرنامج سابقاً. يرجى فتح البرنامج من الأيقونة الجديدة المحدثة (في مجلد Updates أو الاختصار الجديد).";

                    MessageBox.Show(errorMsg, "تنبيه توافق الإصدار", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CheckAndEnforceVersion failed", ex, "DbHelper");
                // في حالة حدوث خطأ غير متوقع في المقارنة، نسمح بالمرور منعاً لتعطيل العمل
                return true;
            }
        }
    }
}
