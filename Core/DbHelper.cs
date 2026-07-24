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

        public static DateTime? ParseExpiryInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim().Replace("/", "").Replace("-", "").Replace(".", "");
            if (input.Length == 3 || input.Length == 4)
            {
                if (int.TryParse(input, out int num))
                {
                    string padded = input.PadLeft(4, '0');
                    int month = int.Parse(padded.Substring(0, 2));
                    int yearShort = int.Parse(padded.Substring(2, 2));
                    int year = 2000 + yearShort;
                    if (month >= 1 && month <= 12)
                    {
                        return new DateTime(year, month, 1);
                    }
                }
            }
            if (DateTime.TryParse(input, out DateTime parsed))
                return parsed;
            return null;
        }

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

        private const string SchemaVersionKey = "SchemaVersion";
        private const int CurrentSchemaVersion = 28;

        public static void EnsurePurchaseColumnsExist()
        {
            SafeMigrate("Purchases.SupplierInvoiceNo", @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'SupplierInvoiceNo')
            BEGIN
                ALTER TABLE Purchases ADD SupplierInvoiceNo NVARCHAR(100) NULL;
            END");

            SafeMigrate("Purchases.ShippingCost", @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'ShippingCost')
            BEGIN
                ALTER TABLE Purchases ADD ShippingCost DECIMAL(18,2) NULL CONSTRAINT DF_Purchases_ShippingCost DEFAULT 0;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'ShippingOn')
            BEGIN
                ALTER TABLE Purchases ADD ShippingOn NVARCHAR(20) NULL CONSTRAINT DF_Purchases_ShippingOn DEFAULT 'Company';
            END");

            SafeMigrate("ProductSizes.Table", @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductSizes')
            BEGIN
                CREATE TABLE ProductSizes (
                    SizeID   INT IDENTITY(1,1) PRIMARY KEY,
                    SizeCode NVARCHAR(20),
                    SizeName NVARCHAR(100) NOT NULL
                );
            END");

            SafeMigrate("Products.ProductSize", @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ProductSize')
            BEGIN
                ALTER TABLE Products ADD ProductSize NVARCHAR(100) NULL;
            END");

            SafeMigrate("Products.OfferColumns", @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'OriginalPrice')
            BEGIN
                ALTER TABLE Products ADD OriginalPrice DECIMAL(18,2) NULL;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'DiscountPct')
            BEGIN
                ALTER TABLE Products ADD DiscountPct DECIMAL(5,2) NULL CONSTRAINT DF_Products_DiscountPct DEFAULT 0;
            END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsOffer')
            BEGIN
                ALTER TABLE Products ADD IsOffer BIT NULL CONSTRAINT DF_Products_IsOffer DEFAULT 0;
            END");

            SafeMigrate("InstallmentSchedules.Notes", @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InstallmentSchedules') AND name = 'Notes')
            BEGIN
                ALTER TABLE InstallmentSchedules ADD Notes NVARCHAR(500) NULL;
            END");
        }

        public static void EnsureDatabaseSchema()
        {
            EnsurePurchaseColumnsExist();

            try
            {
                // Bypass heavy schema inspection if already initialized to the latest version
                string cachedVer = AppConfig.Get(SchemaVersionKey, "0");
                if (int.TryParse(cachedVer, out int parsedVer) && parsedVer >= CurrentSchemaVersion)
                {
                    // Double check critical columns to handle database restore cases
                    try
                    {
                        var colExists = Scalar("SELECT COL_LENGTH('Products', 'DefaultSaleUnit')");
                        var sinvExists = Scalar("SELECT COL_LENGTH('Purchases', 'SupplierInvoiceNo')");
                        var shipExists = Scalar("SELECT COL_LENGTH('Purchases', 'ShippingCost')");
                        if (colExists != null && colExists != DBNull.Value &&
                            sinvExists != null && sinvExists != DBNull.Value &&
                            shipExists != null && shipExists != DBNull.Value)
                        {
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // كل SafeMigrate مستقلة: فشل أي خطوة لا يوقف الباقي
            try
            {
                SafeMigrate("SafeAccounts.DefaultDrawer", @"
                IF EXISTS (SELECT 1 FROM SafeAccounts WHERE AccountID = 1 AND AccountName = N'الخزينة الرئيسية')
                BEGIN
                    UPDATE SafeAccounts SET AccountName = N'درج نقدي' WHERE AccountID = 1;
                END
                IF NOT EXISTS (SELECT 1 FROM SafeAccounts WHERE AccountName = N'درج نقدي')
                BEGIN
                    INSERT INTO SafeAccounts (AccountName, AccountType, AccountNumber, OpeningBalance, IsActive)
                    VALUES (N'درج نقدي', N'Safe', N'Auto-Drawer', 0.00, 1);
                END");

                SafeMigrate("CustomerReservations", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerReservations')
                BEGIN
                    CREATE TABLE CustomerReservations (
                        ReservationID INT IDENTITY(1,1) PRIMARY KEY,
                        ReservationNumber NVARCHAR(50) NOT NULL,
                        ClientID INT NULL REFERENCES Clients(ClientID),
                        ClientName NVARCHAR(150) NOT NULL,
                        ClientPhone NVARCHAR(50) NULL,
                        ProductID INT NULL REFERENCES Products(ProductID),
                        ProductName NVARCHAR(200) NOT NULL,
                        ProductCode NVARCHAR(100) NULL,
                        Quantity DECIMAL(18,2) DEFAULT 1,
                        UnitPrice DECIMAL(18,2) DEFAULT 0,
                        TotalAmount DECIMAL(18,2) DEFAULT 0,
                        DepositAmount DECIMAL(18,2) DEFAULT 0,
                        RemainingAmount DECIMAL(18,2) DEFAULT 0,
                        ReservationDate DATETIME DEFAULT GETDATE(),
                        ExpectedDate DATETIME NULL,
                        Status NVARCHAR(50) DEFAULT N'قيد الانتظار',
                        Notes NVARCHAR(500) NULL,
                        CreatedBy NVARCHAR(100) NULL,
                        SaleID INT NULL REFERENCES Sales(SaleID)
                    );
                END");

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

                // Add IsService to Products
                SafeMigrate("Products.IsService", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsService')
                BEGIN
                    ALTER TABLE Products ADD IsService BIT NOT NULL DEFAULT 0;
                END");

                // Add IsQuickItem to Products
                SafeMigrate("Products.IsQuickItem", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsQuickItem')
                BEGIN
                    ALTER TABLE Products ADD IsQuickItem BIT NOT NULL DEFAULT 0;
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

                SafeMigrate("DriverHandovers.DeadQtyHandling", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DriverHandovers') AND name = 'DeadQtyHandling')
                BEGIN
                    ALTER TABLE DriverHandovers ADD DeadQtyHandling NVARCHAR(50) NULL;
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

                SafeMigrate("Sales.CashPaid", @"
                IF OBJECT_ID('Sales', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Sales', 'CashPaid') IS NULL
                    BEGIN
                        ALTER TABLE Sales ADD CashPaid DECIMAL(10,2) NULL;
                    END
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

                SafeMigrate("PurchaseItems.ExpiryDate", @"
                IF OBJECT_ID('PurchaseItems', 'U') IS NOT NULL AND COL_LENGTH('PurchaseItems', 'ExpiryDate') IS NULL
                BEGIN
                    ALTER TABLE PurchaseItems ADD ExpiryDate DATE NULL;
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
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ProducerCompany')
                BEGIN
                    ALTER TABLE Products ADD ProducerCompany NVARCHAR(200) NULL;
                END";
                Execute(sqlProductsPartsFields);

                // 4.1. إضافة حقل طباعة الباركود المحلي PrintLocalBarcode لجدول الأصناف Products
                string sqlProductsPrintBarcodeField = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PrintLocalBarcode')
                BEGIN
                    ALTER TABLE Products ADD PrintLocalBarcode BIT NOT NULL DEFAULT 1;
                END";
                Execute(sqlProductsPrintBarcodeField);

                // 5. إضافة معرف المخزن WarehouseID ورقم فاتورة المورد لجداول الحركات
                string sqlAddSupplierInvoiceNo = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'SupplierInvoiceNo')
                BEGIN
                    ALTER TABLE Purchases ADD SupplierInvoiceNo NVARCHAR(100) NULL;
                END";
                Execute(sqlAddSupplierInvoiceNo);

                string sqlAddShipping = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'ShippingCost')
                BEGIN
                    ALTER TABLE Purchases ADD ShippingCost DECIMAL(18,2) NULL CONSTRAINT DF_Purchases_ShippingCost DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Purchases') AND name = 'ShippingOn')
                BEGIN
                    ALTER TABLE Purchases ADD ShippingOn NVARCHAR(20) NULL CONSTRAINT DF_Purchases_ShippingOn DEFAULT 'Company';
                END";
                Execute(sqlAddShipping);

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
                
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanOrderColumns')
                BEGIN
                    ALTER TABLE Permissions ADD CanOrderColumns BIT DEFAULT 0;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanAdd')
                BEGIN
                    ALTER TABLE Permissions ADD CanAdd BIT DEFAULT 1;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanEdit')
                BEGIN
                    ALTER TABLE Permissions ADD CanEdit BIT DEFAULT 1;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanDelete')
                BEGIN
                    ALTER TABLE Permissions ADD CanDelete BIT DEFAULT 1;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewDetails')
                BEGIN
                    ALTER TABLE Permissions ADD CanViewDetails BIT DEFAULT 1;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewBalance')
                BEGIN
                    ALTER TABLE Permissions ADD CanViewBalance BIT DEFAULT 1;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanChangeSafe')
                BEGIN
                    ALTER TABLE Permissions ADD CanChangeSafe BIT DEFAULT 1;
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

                // ===== ميزة الوحدات المتعددة (كرتونة، علبة، قطعة) =====
                SafeMigrate("Products.MultiUnits", @"
                IF OBJECT_ID('Products', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Products', 'Unit1Name') IS NULL ALTER TABLE Products ADD Unit1Name NVARCHAR(50) NULL;
                    IF COL_LENGTH('Products', 'Unit1Barcode') IS NULL ALTER TABLE Products ADD Unit1Barcode NVARCHAR(50) NULL;
                    IF COL_LENGTH('Products', 'Unit1SalePrice') IS NULL ALTER TABLE Products ADD Unit1SalePrice DECIMAL(10,2) NULL;
                    IF COL_LENGTH('Products', 'Unit1PurchasePrice') IS NULL ALTER TABLE Products ADD Unit1PurchasePrice DECIMAL(10,2) NULL;
                    
                    IF COL_LENGTH('Products', 'Unit2Name') IS NULL ALTER TABLE Products ADD Unit2Name NVARCHAR(50) NULL;
                    IF COL_LENGTH('Products', 'Unit2Factor') IS NULL ALTER TABLE Products ADD Unit2Factor DECIMAL(10,3) NULL;
                    IF COL_LENGTH('Products', 'Unit2Barcode') IS NULL ALTER TABLE Products ADD Unit2Barcode NVARCHAR(50) NULL;
                    IF COL_LENGTH('Products', 'Unit2SalePrice') IS NULL ALTER TABLE Products ADD Unit2SalePrice DECIMAL(10,2) NULL;
                    IF COL_LENGTH('Products', 'Unit2PurchasePrice') IS NULL ALTER TABLE Products ADD Unit2PurchasePrice DECIMAL(10,2) NULL;
                    
                    IF COL_LENGTH('Products', 'Unit3Factor') IS NULL ALTER TABLE Products ADD Unit3Factor DECIMAL(10,3) NULL;
                END");

                SafeMigrate("TransactionItems.MultiUnits", @"
                IF OBJECT_ID('SaleItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('SaleItems', 'UnitName') IS NULL ALTER TABLE SaleItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('SaleItems', 'Factor') IS NULL ALTER TABLE SaleItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('SaleItemsHistory', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('SaleItemsHistory', 'UnitName') IS NULL ALTER TABLE SaleItemsHistory ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('SaleItemsHistory', 'Factor') IS NULL ALTER TABLE SaleItemsHistory ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('PurchaseItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('PurchaseItems', 'UnitName') IS NULL ALTER TABLE PurchaseItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('PurchaseItems', 'Factor') IS NULL ALTER TABLE PurchaseItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('ReturnItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('ReturnItems', 'UnitName') IS NULL ALTER TABLE ReturnItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('ReturnItems', 'Factor') IS NULL ALTER TABLE ReturnItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('PurchaseReturnItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('PurchaseReturnItems', 'UnitName') IS NULL ALTER TABLE PurchaseReturnItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('PurchaseReturnItems', 'Factor') IS NULL ALTER TABLE PurchaseReturnItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('WarehouseTransferItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('WarehouseTransferItems', 'UnitName') IS NULL ALTER TABLE WarehouseTransferItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('WarehouseTransferItems', 'Factor') IS NULL ALTER TABLE WarehouseTransferItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('WastageLossItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('WastageLossItems', 'UnitName') IS NULL ALTER TABLE WastageLossItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('WastageLossItems', 'Factor') IS NULL ALTER TABLE WastageLossItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('DriverLoadItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('DriverLoadItems', 'UnitName') IS NULL ALTER TABLE DriverLoadItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('DriverLoadItems', 'Factor') IS NULL ALTER TABLE DriverLoadItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END
                IF OBJECT_ID('HandoverItems', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('HandoverItems', 'UnitName') IS NULL ALTER TABLE HandoverItems ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('HandoverItems', 'Factor') IS NULL ALTER TABLE HandoverItems ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END");

                SafeMigrate("StockAdjustments.MultiUnits", @"
                IF OBJECT_ID('StockAdjustments', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('StockAdjustments', 'UnitName') IS NULL ALTER TABLE StockAdjustments ADD UnitName NVARCHAR(50) NULL;
                    IF COL_LENGTH('StockAdjustments', 'Factor') IS NULL ALTER TABLE StockAdjustments ADD Factor DECIMAL(10,3) DEFAULT 1.0;
                END");

                SafeMigrate("UnitsTable", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Units')
                BEGIN
                    CREATE TABLE Units (
                        UnitID INT IDENTITY(1,1) PRIMARY KEY,
                        UnitName NVARCHAR(50) UNIQUE NOT NULL
                    );
                END
                IF NOT EXISTS (SELECT * FROM Units)
                BEGIN
                    INSERT INTO Units (UnitName) VALUES 
                    (N'قطعة'),
                    (N'علبة'),
                    (N'كرتونة'),
                    (N'كيس'),
                    (N'كيلو'),
                    (N'متر'),
                    (N'جوز');
                END");

                // ===== عرض الكميات الفعلية الحالية لكل صنف في كل مخزن =====
                SafeMigrate("vw_CurrentStockByWarehouse.Drop_v3",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CurrentStockByWarehouse') DROP VIEW vw_CurrentStockByWarehouse;");
                SafeMigrate("vw_CurrentStockByWarehouse.Create_MultiUnits_v3", @"
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
                    ISNULL(adj.ActualQty * COALESCE(adj.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)), 0)
                    + ISNULL((SELECT SUM(ri.Quantity * ISNULL(ri.Factor, 0)) FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID WHERE ri.ProductID = p.ProductID AND sr.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0)
                    + COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ri.Quantity) FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID WHERE ri.ProductID = p.ProductID AND ri.Factor IS NULL AND sr.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(hi.ReturnedQty * ISNULL(hi.Factor, 0)) FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID JOIN DriverLoads dl ON dh.LoadID = dl.LoadID WHERE hi.ProductID = p.ProductID AND dl.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0)
                    + COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(hi.ReturnedQty) FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID JOIN DriverLoads dl ON dh.LoadID = dl.LoadID WHERE hi.ProductID = p.ProductID AND hi.Factor IS NULL AND dl.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(pi.Quantity * ISNULL(pi.Factor, 0)) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1 AND pu.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate)), 0)
                    + COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(pi.Quantity) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pi.Factor IS NULL AND pu.IsPosted = 1 AND pu.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(ti.Quantity * ISNULL(ti.Factor, 0)) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND t.IsPosted = 1 AND t.ToWarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)), 0)
                    + COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ti.Quantity) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND ti.Factor IS NULL AND t.IsPosted = 1 AND t.ToWarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(pri.Quantity * ISNULL(pri.Factor, 0)) FROM PurchaseReturnItems pri JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID WHERE pri.ProductID = p.ProductID AND pr.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate)), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(pri.Quantity) FROM PurchaseReturnItems pri JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID WHERE pri.ProductID = p.ProductID AND pri.Factor IS NULL AND pr.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(si.Quantity * ISNULL(si.Factor, 0)) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND s.IsPosted = 1 AND s.WarehouseID = w.WarehouseID AND (s.SaleType = ''DriverLoad'' OR (s.SaleType IN (''Cash'', ''Credit'', ''Installment'') AND s.DriverID IS NULL)) AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(si.Quantity) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND si.Factor IS NULL AND s.IsPosted = 1 AND s.WarehouseID = w.WarehouseID AND (s.SaleType = ''DriverLoad'' OR (s.SaleType IN (''Cash'', ''Credit'', ''Installment'') AND s.DriverID IS NULL)) AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(ti2.Quantity * ISNULL(ti2.Factor, 0)) FROM WarehouseTransferItems ti2 JOIN WarehouseTransfers t2 ON ti2.TransferID = t2.TransferID WHERE ti2.ProductID = p.ProductID AND t2.IsPosted = 1 AND t2.FromWarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR t2.TransferDate > adj.AdjDate)), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ti2.Quantity) FROM WarehouseTransferItems ti2 JOIN WarehouseTransfers t2 ON ti2.TransferID = t2.TransferID WHERE ti2.ProductID = p.ProductID AND ti2.Factor IS NULL AND t2.IsPosted = 1 AND t2.FromWarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR t2.TransferDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(wli.Quantity * ISNULL(wli.Factor, 0)) FROM WastageLossItems wli JOIN WastageLoss wl ON wli.WastageID = wl.WastageID WHERE wli.ProductID = p.ProductID AND wl.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR wl.WastageDate > adj.AdjDate)), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(wli.Quantity) FROM WastageLossItems wli JOIN WastageLoss wl ON wli.WastageID = wl.WastageID WHERE wli.ProductID = p.ProductID AND wli.Factor IS NULL AND wl.WarehouseID = w.WarehouseID AND (adj.AdjDate IS NULL OR wl.WastageDate > adj.AdjDate)), 0)
                    AS CurrentQty,
                    adj.AdjDate AS LastAdjDate
                FROM Products p
                CROSS JOIN Warehouses w
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty, sa.Factor
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
                Execute(@"UPDATE Employees SET PlainPassword = NULL WHERE PlainPassword IS NOT NULL");

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

                // Crate tracking migrations
                SafeMigrate("Clients.OpeningCrates", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'OpeningCrates')
                BEGIN
                    ALTER TABLE Clients ADD OpeningCrates INT NOT NULL DEFAULT 0;
                END");

                SafeMigrate("Sales.Crates", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CratesOut')
                BEGIN
                    ALTER TABLE Sales ADD CratesOut INT NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CratesIn')
                BEGIN
                    ALTER TABLE Sales ADD CratesIn INT NOT NULL DEFAULT 0;
                END");

                SafeMigrate("ClientCratesTransactions", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClientCratesTransactions')
                BEGIN
                    CREATE TABLE ClientCratesTransactions (
                        CrateTransID INT IDENTITY(1,1) PRIMARY KEY,
                        ClientID INT NOT NULL REFERENCES Clients(ClientID) ON DELETE CASCADE,
                        TransDate DATETIME DEFAULT GETDATE(),
                        CratesOut INT DEFAULT 0,
                        CratesIn INT DEFAULT 0,
                        RefSaleID INT NULL REFERENCES Sales(SaleID) ON DELETE SET NULL,
                        Notes NVARCHAR(500),
                        CreatedBy INT NULL REFERENCES Employees(EmpID)
                    );
                END");

                SafeMigrate("vw_ClientCratesBalance.Drop",
                    "IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ClientCratesBalance') DROP VIEW vw_ClientCratesBalance;");
                SafeMigrate("vw_ClientCratesBalance.Create", @"
                EXEC('CREATE VIEW vw_ClientCratesBalance AS
                SELECT
                    c.ClientID,
                    c.OpeningCrates,
                    ISNULL(SUM(cct.CratesOut),0) AS TotalCratesOut,
                    ISNULL(SUM(cct.CratesIn),0) AS TotalCratesIn,
                    c.OpeningCrates + ISNULL(SUM(cct.CratesOut),0) - ISNULL(SUM(cct.CratesIn),0) AS CratesBalance
                FROM Clients c
                LEFT JOIN ClientCratesTransactions cct ON c.ClientID = cct.ClientID
                GROUP BY c.ClientID, c.OpeningCrates');");

                // ===== أعمدة صلاحيات الخزائن وطرق البيع للموظفين =====
                SafeMigrate("Employees.PermissionsAndSafes", @"
                IF OBJECT_ID('Employees', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Employees', 'DefaultSafeID') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD DefaultSafeID INT NULL;
                    END
                    IF COL_LENGTH('Employees', 'AllowedSafeIDs') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD AllowedSafeIDs VARCHAR(255) NULL;
                    END
                    IF COL_LENGTH('Employees', 'CanSellCash') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanSellCash BIT NOT NULL DEFAULT 1;
                    END
                    IF COL_LENGTH('Employees', 'CanSellCredit') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanSellCredit BIT NOT NULL DEFAULT 1;
                    END
                    IF COL_LENGTH('Employees', 'CanSellDriverLoad') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanSellDriverLoad BIT NOT NULL DEFAULT 1;
                    END
                    IF COL_LENGTH('Employees', 'CanSellInstallment') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanSellInstallment BIT NOT NULL DEFAULT 1;
                    END
                END");


                SafeMigrate("Sales.ShippingCharge", @"
                IF OBJECT_ID('Sales', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Sales', 'ShippingCharge') IS NULL
                    BEGIN
                        ALTER TABLE Sales ADD ShippingCharge DECIMAL(10,2) NOT NULL DEFAULT 0.0;
                    END
                END");

                SafeMigrate("Employees.CanEditShippingCharge", @"
                IF OBJECT_ID('Employees', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Employees', 'CanEditShippingCharge') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanEditShippingCharge BIT NOT NULL DEFAULT 1;
                    END
                END");

                SafeMigrate("Employees.CanSelectDriver", @"
                IF OBJECT_ID('Employees', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Employees', 'CanSelectDriver') IS NULL
                    BEGIN
                        ALTER TABLE Employees ADD CanSelectDriver BIT NOT NULL DEFAULT 1;
                    END
                END");

                // ===== ميزات السوبر ماركت =====

                // 1. إدارة الورديات (Shifts)
                SafeMigrate("Shifts", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Shifts')
                BEGIN
                    CREATE TABLE Shifts (
                        ShiftID       INT IDENTITY(1,1) PRIMARY KEY,
                        ShiftDate     DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
                        OpenTime      DATETIME NOT NULL DEFAULT GETDATE(),
                        CloseTime     DATETIME NULL,
                        OpenedBy      INT NOT NULL REFERENCES Employees(EmpID),
                        ClosedBy      INT NULL REFERENCES Employees(EmpID),
                        OpeningCash   DECIMAL(10,2) NOT NULL DEFAULT 0,
                        TotalSales    DECIMAL(10,2) NOT NULL DEFAULT 0,
                        TotalReturns  DECIMAL(10,2) NOT NULL DEFAULT 0,
                        CashSales     DECIMAL(10,2) NOT NULL DEFAULT 0,
                        VisaSales     DECIMAL(10,2) NOT NULL DEFAULT 0,
                        OtherSales    DECIMAL(10,2) NOT NULL DEFAULT 0,
                        ExpectedCash  DECIMAL(10,2) NOT NULL DEFAULT 0,
                        ActualCash    DECIMAL(10,2) NULL,
                        Difference    DECIMAL(10,2) NULL,
                        Notes         NVARCHAR(500) NULL,
                        Status        NVARCHAR(20) NOT NULL DEFAULT 'Open'
                    );
                END");

                SafeMigrate("Sales.ShiftID", @"
                IF OBJECT_ID('Sales','U') IS NOT NULL AND COL_LENGTH('Sales','ShiftID') IS NULL
                BEGIN
                    ALTER TABLE Sales ADD ShiftID INT NULL REFERENCES Shifts(ShiftID);
                END");

                // 2. دفعات الأصناف مع تاريخ الانتهاء (ProductBatches)
                SafeMigrate("ProductBatches", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='ProductBatches')
                BEGIN
                    CREATE TABLE ProductBatches (
                        BatchID       INT IDENTITY(1,1) PRIMARY KEY,
                        ProductID     INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
                        WarehouseID   INT NOT NULL REFERENCES Warehouses(WarehouseID),
                        BatchNumber   NVARCHAR(50) NULL,
                        Quantity      DECIMAL(10,3) NOT NULL DEFAULT 0,
                        ExpiryDate    DATE NULL,
                        PurchaseID    INT NULL REFERENCES Purchases(PurchaseID) ON DELETE SET NULL,
                        CreatedAt     DATETIME DEFAULT GETDATE()
                    );
                END");

                SafeMigrate("Products.DefaultExpiryDays", @"
                IF OBJECT_ID('Products','U') IS NOT NULL AND COL_LENGTH('Products','DefaultExpiryDays') IS NULL
                BEGIN
                    ALTER TABLE Products ADD DefaultExpiryDays INT NULL;
                END");

                SafeMigrate("Products.HasExpiry", @"
                IF OBJECT_ID('Products','U') IS NOT NULL AND COL_LENGTH('Products','HasExpiry') IS NULL
                BEGIN
                    ALTER TABLE Products ADD HasExpiry BIT NOT NULL DEFAULT 0;
                END");

                SafeMigrate("SaleItems.ExpiryDateAndBatch", @"
                IF OBJECT_ID('SaleItems','U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('SaleItems','ExpiryDate') IS NULL ALTER TABLE SaleItems ADD ExpiryDate DATE NULL;
                    IF COL_LENGTH('SaleItems','BatchID') IS NULL ALTER TABLE SaleItems ADD BatchID INT NULL;
                END");

                // 3. نظام نقاط الولاء (Loyalty)
                SafeMigrate("Clients.LoyaltyPoints", @"
                IF OBJECT_ID('Clients','U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Clients','LoyaltyPoints') IS NULL
                        ALTER TABLE Clients ADD LoyaltyPoints DECIMAL(10,2) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('Clients','TotalPointsEarned') IS NULL
                        ALTER TABLE Clients ADD TotalPointsEarned DECIMAL(10,2) NOT NULL DEFAULT 0;
                END");

                SafeMigrate("LoyaltyTransactions", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='LoyaltyTransactions')
                BEGIN
                    CREATE TABLE LoyaltyTransactions (
                        TransID    INT IDENTITY(1,1) PRIMARY KEY,
                        ClientID   INT NOT NULL REFERENCES Clients(ClientID) ON DELETE CASCADE,
                        TransDate  DATETIME DEFAULT GETDATE(),
                        TransType  NVARCHAR(20) NOT NULL,
                        Points     DECIMAL(10,2) NOT NULL,
                        RefSaleID  INT NULL REFERENCES Sales(SaleID) ON DELETE SET NULL,
                        Notes      NVARCHAR(200) NULL,
                        CreatedBy  INT NULL REFERENCES Employees(EmpID)
                    );
                END");

                SafeMigrate("ExpenseTypes", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseTypes')
                BEGIN
                    CREATE TABLE ExpenseTypes (
                        ExpenseTypeID   INT IDENTITY(1,1) PRIMARY KEY,
                        ExpenseTypeCode NVARCHAR(20) NOT NULL UNIQUE,
                        ExpenseTypeName NVARCHAR(100) NOT NULL UNIQUE
                    );
                    INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES
                    ('EXP-0001', N'رواتب'),
                    ('EXP-0002', N'وقود'),
                    ('EXP-0003', N'صيانة'),
                    ('EXP-0004', N'مصروف إداري'),
                    ('EXP-0005', N'مواد تغليف'),
                    ('EXP-0006', N'نقل'),
                    ('EXP-0007', N'أخرى');
                END");

                SafeMigrate("Brands", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Brands')
                BEGIN
                    CREATE TABLE Brands (
                        BrandID INT IDENTITY(1,1) PRIMARY KEY,
                        BrandCode NVARCHAR(50) NOT NULL UNIQUE,
                        BrandName NVARCHAR(100) NOT NULL UNIQUE
                    );
                END");

                SafeMigrate("Brands.Migration", @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Brands')
                BEGIN
                    INSERT INTO Brands (BrandCode, BrandName)
                    SELECT 
                        'BRD-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY p.Brand) + COALESCE((SELECT MAX(BrandID) FROM Brands), 0) AS NVARCHAR(50)), 4),
                        p.Brand
                    FROM (SELECT DISTINCT Brand FROM Products WHERE Brand IS NOT NULL AND Brand <> '') p
                    WHERE p.Brand NOT IN (SELECT BrandName FROM Brands);
                END");

                SafeMigrate("CarModels", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CarModels')
                BEGIN
                    CREATE TABLE CarModels (
                        CarModelID INT IDENTITY(1,1) PRIMARY KEY,
                        CarModelCode NVARCHAR(50) NOT NULL UNIQUE,
                        CarModelName NVARCHAR(200) NOT NULL UNIQUE
                    );
                END");

                SafeMigrate("CarModels.Migration", @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CarModels')
                BEGIN
                    INSERT INTO CarModels (CarModelCode, CarModelName)
                    SELECT 
                        'MDL-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY p.CarModel) + COALESCE((SELECT MAX(CarModelID) FROM CarModels), 0) AS NVARCHAR(50)), 4),
                        p.CarModel
                    FROM (SELECT DISTINCT CarModel FROM Products WHERE CarModel IS NOT NULL AND CarModel <> '') p
                    WHERE p.CarModel NOT IN (SELECT CarModelName FROM CarModels);
                END");

                SafeMigrate("ShelfLocations", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShelfLocations')
                BEGIN
                    CREATE TABLE ShelfLocations (
                        ShelfID INT IDENTITY(1,1) PRIMARY KEY,
                        ShelfCode NVARCHAR(50) NOT NULL UNIQUE,
                        ShelfName NVARCHAR(100) NOT NULL UNIQUE
                    );
                END");

                SafeMigrate("ShelfLocations.Migration", @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ShelfLocations')
                BEGIN
                    INSERT INTO ShelfLocations (ShelfCode, ShelfName)
                    SELECT 
                        'SHF-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY p.ShelfLocation) + COALESCE((SELECT MAX(ShelfID) FROM ShelfLocations), 0) AS NVARCHAR(50)), 4),
                        p.ShelfLocation
                    FROM (SELECT DISTINCT ShelfLocation FROM Products WHERE ShelfLocation IS NOT NULL AND ShelfLocation <> '') p
                    WHERE p.ShelfLocation NOT IN (SELECT ShelfName FROM ShelfLocations);
                END");

                SafeMigrate("ProducerCompanies", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProducerCompanies')
                BEGIN
                    CREATE TABLE ProducerCompanies (
                        ProducerID INT IDENTITY(1,1) PRIMARY KEY,
                        ProducerCode NVARCHAR(50) NOT NULL UNIQUE,
                        ProducerName NVARCHAR(200) NOT NULL UNIQUE
                    );
                END");

                SafeMigrate("ProducerCompanies.Migration", @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProducerCompanies')
                BEGIN
                    INSERT INTO ProducerCompanies (ProducerCode, ProducerName)
                    SELECT 
                        'PRD-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY p.ProducerCompany) + COALESCE((SELECT MAX(ProducerID) FROM ProducerCompanies), 0) AS NVARCHAR(50)), 4),
                        p.ProducerCompany
                    FROM (SELECT DISTINCT ProducerCompany FROM Products WHERE ProducerCompany IS NOT NULL AND ProducerCompany <> '') p
                    WHERE p.ProducerCompany NOT IN (SELECT ProducerName FROM ProducerCompanies);
                END");

                SafeMigrate("MaintenanceTickets", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaintenanceTickets')
                BEGIN
                    CREATE TABLE MaintenanceTickets (
                        TicketID INT IDENTITY(1,1) PRIMARY KEY,
                        CustomerName NVARCHAR(100) NOT NULL,
                        CustomerPhone NVARCHAR(50) NULL,
                        DeviceModel NVARCHAR(100) NOT NULL,
                        DeviceSerial NVARCHAR(100) NULL,
                        Problem NVARCHAR(500) NULL,
                        Cost DECIMAL(18, 2) NOT NULL DEFAULT 0,
                        Status NVARCHAR(50) NOT NULL DEFAULT N'قيد الإصلاح',
                        Notes NVARCHAR(500) NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END");

                SafeMigrate("SaleItems.IMEI", @"
                IF OBJECT_ID('SaleItems','U') IS NOT NULL AND COL_LENGTH('SaleItems','IMEI') IS NULL
                BEGIN
                    ALTER TABLE SaleItems ADD IMEI NVARCHAR(100) NULL;
                END");

                SafeMigrate("MaintenanceTickets.MobileShopFields", @"
                IF OBJECT_ID('MaintenanceTickets','U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('MaintenanceTickets','PartsCost') IS NULL ALTER TABLE MaintenanceTickets ADD PartsCost DECIMAL(18, 2) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('MaintenanceTickets','LaborCost') IS NULL ALTER TABLE MaintenanceTickets ADD LaborCost DECIMAL(18, 2) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('MaintenanceTickets','WarrantyPeriod') IS NULL ALTER TABLE MaintenanceTickets ADD WarrantyPeriod NVARCHAR(100) NULL;
                END");

                SafeMigrate("MaintenanceTickets.PrepaidAmount", @"
                IF OBJECT_ID('MaintenanceTickets','U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('MaintenanceTickets','PrepaidAmount') IS NULL ALTER TABLE MaintenanceTickets ADD PrepaidAmount DECIMAL(18, 2) NOT NULL DEFAULT 0;
                END");

                // ===== 4. \u0646\u0638\u0627\u0645 \u0627\u0644\u0623\u0633\u062a\u0627\u0630 \u0627\u0644\u0639\u0627\u0645 \u0627\u0644\u0645\u0632\u062f\u0648\u062c \u0627\u0644\u0645\u0648\u062d\u062f (General Ledger) =====
                SafeMigrate("JournalEntries", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalEntries')
                BEGIN
                    CREATE TABLE JournalEntries (
                        EntryID     INT IDENTITY(1,1) PRIMARY KEY,
                        EntryDate   DATETIME NOT NULL DEFAULT GETDATE(),
                        Description NVARCHAR(500) NULL,
                        SourceType  NVARCHAR(50) NOT NULL,
                        SourceRefID INT NOT NULL,
                        CreatedBy   INT NULL
                    );
                END");

                SafeMigrate("JournalDetails", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalDetails')
                BEGIN
                    CREATE TABLE JournalDetails (
                        DetailID    INT IDENTITY(1,1) PRIMARY KEY,
                        EntryID     INT NOT NULL FOREIGN KEY REFERENCES JournalEntries(EntryID) ON DELETE CASCADE,
                        AccountName NVARCHAR(100) NOT NULL,
                        Debit       DECIMAL(18,2) NOT NULL DEFAULT 0,
                        Credit      DECIMAL(18,2) NOT NULL DEFAULT 0
                    );
                END");

                SafeMigrate("JournalEntries.Index", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_JournalEntries_Source' AND object_id = OBJECT_ID('JournalEntries'))
                BEGIN
                    CREATE INDEX IX_JournalEntries_Source ON JournalEntries(SourceType, SourceRefID);
                END");

                // \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0641\u0647\u0627\u0631\u0633 \u0627\u0644\u0645\u062d\u0633\u0646\u0629 \u0644\u062a\u0633\u0631\u064a\u0639 \u0627\u0644\u0627\u0633\u062a\u0639\u0644\u0627\u0645\u0627\u062a \u0648\u0625\u0644\u063a\u0627\u0621 \u0628\u0637\u0621 \u062c\u0631\u062f \u0627\u0644\u0645\u062e\u0627\u0632\u0646 \u0646\u0647\u0627\u0626\u064a\u0627\u064b
                SafeMigrate("OptimizingIndexes", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_Opt' AND object_id = OBJECT_ID('Sales'))
                    CREATE INDEX IX_Sales_Opt ON Sales(WarehouseID, IsPosted, SaleDate) INCLUDE (SaleID, SaleType, ClientID, DriverID, TotalAmount);
                
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_Opt' AND object_id = OBJECT_ID('SaleItems'))
                    CREATE INDEX IX_SaleItems_Opt ON SaleItems(ProductID, SaleID) INCLUDE (Quantity, Factor);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchases_Opt' AND object_id = OBJECT_ID('Purchases'))
                    CREATE INDEX IX_Purchases_Opt ON Purchases(WarehouseID, IsPosted, PurchaseDate) INCLUDE (PurchaseID);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseItems_Opt' AND object_id = OBJECT_ID('PurchaseItems'))
                    CREATE INDEX IX_PurchaseItems_Opt ON PurchaseItems(ProductID, PurchaseID) INCLUDE (Quantity, Factor);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesReturns_Opt' AND object_id = OBJECT_ID('SalesReturns'))
                    CREATE INDEX IX_SalesReturns_Opt ON SalesReturns(WarehouseID, ReturnDate) INCLUDE (ReturnID);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ReturnItems_Opt' AND object_id = OBJECT_ID('ReturnItems'))
                    CREATE INDEX IX_ReturnItems_Opt ON ReturnItems(ProductID, ReturnID) INCLUDE (Quantity, Factor);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientTransactions_Opt' AND object_id = OBJECT_ID('ClientTransactions'))
                    CREATE INDEX IX_ClientTransactions_Opt ON ClientTransactions(ClientID, TransDate) INCLUDE (Debit, Credit);

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CashBox_Opt' AND object_id = OBJECT_ID('CashBox'))
                    CREATE INDEX IX_CashBox_Opt ON CashBox(AccountID, TransDate) INCLUDE (AmountIn, AmountOut);
                ");

                SafeMigrate("JournalDetails.ExpandAccountName", @"
                IF OBJECT_ID('JournalDetails', 'U') IS NOT NULL
                BEGIN
                    -- زيادة حجم العمود لتفادي مشاكل اقتطاع أسماء الحسابات أو الملاحظات الطويلة
                    ALTER TABLE JournalDetails ALTER COLUMN AccountName NVARCHAR(500) NOT NULL;
                END");

                // الإجراء مزامنة وتوليد القيود المحاسبية المزدوجة التاريخية
                SafeMigrate("SyncingGeneralLedger", @"
                DELETE FROM JournalEntries;
                
                INSERT INTO JournalEntries (EntryDate, Description, SourceType, SourceRefID, CreatedBy)
                SELECT SaleDate, LEFT(N'فاتورة مبيعات رقم ' + SaleCode, 490), 'Sale', SaleID, CreatedBy FROM Sales WHERE IsPosted = 1;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(sa.AccountName, 490), s.TotalAmount, 0 
                FROM JournalEntries je 
                JOIN Sales s ON je.SourceRefID = s.SaleID AND je.SourceType = 'Sale'
                JOIN SafeAccounts sa ON sa.AccountID = 1
                WHERE s.SaleType = 'Cash';

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, N'حساب المبيعات', 0, s.TotalAmount 
                FROM JournalEntries je 
                JOIN Sales s ON je.SourceRefID = s.SaleID AND je.SourceType = 'Sale'
                WHERE s.SaleType = 'Cash';

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(N'العميل: ' + c.ClientName, 490), s.TotalAmount, 0 
                FROM JournalEntries je 
                JOIN Sales s ON je.SourceRefID = s.SaleID AND je.SourceType = 'Sale'
                JOIN Clients c ON s.ClientID = c.ClientID
                WHERE s.SaleType IN ('Credit', 'Installment');

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, N'حساب المبيعات', 0, s.TotalAmount 
                FROM JournalEntries je 
                JOIN Sales s ON je.SourceRefID = s.SaleID AND je.SourceType = 'Sale'
                WHERE s.SaleType IN ('Credit', 'Installment');

                INSERT INTO JournalEntries (EntryDate, Description, SourceType, SourceRefID, CreatedBy)
                SELECT PurchaseDate, LEFT(N'فاتورة مشتريات رقم ' + PurchaseCode, 490), 'Purchase', PurchaseID, CreatedBy FROM Purchases WHERE IsPosted = 1;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, N'حساب المشتريات', p.TotalAmount, 0 
                FROM JournalEntries je 
                JOIN Purchases p ON je.SourceRefID = p.PurchaseID AND je.SourceType = 'Purchase'
                WHERE p.PurchaseType = 'Cash';

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(sa.AccountName, 490), 0, p.TotalAmount 
                FROM JournalEntries je 
                JOIN Purchases p ON je.SourceRefID = p.PurchaseID AND je.SourceType = 'Purchase'
                JOIN SafeAccounts sa ON sa.AccountID = 1
                WHERE p.PurchaseType = 'Cash';

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, N'حساب المشتريات', p.TotalAmount, 0 
                FROM JournalEntries je 
                JOIN Purchases p ON je.SourceRefID = p.PurchaseID AND je.SourceType = 'Purchase'
                WHERE p.PurchaseType = 'Credit';

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(N'المورد: ' + sup.SupplierName, 490), 0, p.TotalAmount 
                FROM JournalEntries je 
                JOIN Purchases p ON je.SourceRefID = p.PurchaseID AND je.SourceType = 'Purchase'
                JOIN Suppliers sup ON p.SupplierID = sup.SupplierID
                WHERE p.PurchaseType = 'Credit';

                INSERT INTO JournalEntries (EntryDate, Description, SourceType, SourceRefID, CreatedBy)
                SELECT TransDate, LEFT(Notes, 490), 'CashBox', CashID, CreatedBy FROM CashBox 
                WHERE TransType NOT IN ('SaleIncome', 'PurchaseExpense');

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(sa.AccountName, 490), cb.AmountIn, 0 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                WHERE cb.TransType = 'ClientPayment' AND cb.AmountIn > 0;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(COALESCE(N'العميل: ' + c.ClientName, N'حساب العملاء'), 490), 0, cb.AmountIn 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                LEFT JOIN ClientTransactions ct ON cb.RefID = ct.RefID AND ct.TransType = 'Payment'
                LEFT JOIN Clients c ON ct.ClientID = c.ClientID
                WHERE cb.TransType = 'ClientPayment' AND cb.AmountIn > 0;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, LEFT(COALESCE(N'المورد: ' + s.SupplierName, N'حساب الموردين'), 490), cb.AmountOut, 0 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                LEFT JOIN SupplierTransactions st ON cb.RefID = st.RefID AND st.TransType = 'Payment'
                LEFT JOIN Suppliers s ON st.SupplierID = s.SupplierID
                WHERE cb.TransType = 'SupplierPayment' AND cb.AmountOut > 0;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, sa.AccountName, 0, cb.AmountOut 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                WHERE cb.TransType = 'SupplierPayment' AND cb.AmountOut > 0;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, N'\u062d\u0633\u0627\u0628 \u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a: ' + COALESCE(e.Notes, N'\u0639\u0627\u0645'), cb.AmountOut, 0 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                LEFT JOIN Expenses e ON cb.RefID = e.ExpenseID
                WHERE cb.TransType = 'Expense' AND cb.AmountOut > 0;

                INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                SELECT je.EntryID, sa.AccountName, 0, cb.AmountOut 
                FROM JournalEntries je 
                JOIN CashBox cb ON je.SourceRefID = cb.CashID AND je.SourceType = 'CashBox'
                JOIN SafeAccounts sa ON cb.AccountID = sa.AccountID
                WHERE cb.TransType = 'Expense' AND cb.AmountOut > 0;
                ");

                // ===== ميزة زيادة حجم الباركود للوحدات المتعددة لتكفي 10 أكواد دولية =====
                SafeMigrate("Products.ExpandBarcodes", @"
                IF OBJECT_ID('Products', 'U') IS NOT NULL
                BEGIN
                    -- تعديل عمود Unit1Barcode و Unit2Barcode ليكون 500 حرف لتسهيل إدخال وتخزين 10 أكواد دولية
                    ALTER TABLE Products ALTER COLUMN Unit1Barcode NVARCHAR(500) NULL;
                    ALTER TABLE Products ALTER COLUMN Unit2Barcode NVARCHAR(500) NULL;
                    -- زيادة حجم الباركود الأساسي أيضاً للاحتياط
                    ALTER TABLE Products ALTER COLUMN ProductCode NVARCHAR(500) NULL;
                END");

                // ===== ميزة تحديد وحدة البيع الافتراضية للمنتج =====
                SafeMigrate("Products.DefaultSaleUnit", @"
                IF OBJECT_ID('Products', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Products', 'DefaultSaleUnit') IS NULL
                    BEGIN
                        ALTER TABLE Products ADD DefaultSaleUnit NVARCHAR(50) NULL;
                    END
                END");

                // ===== 5. نظام التسويات المحاسبية والميزانية العمومية وقائمة الدخل =====
                SafeMigrate("Accounting.AdjustmentsTable", @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountingAdjustments')
                BEGIN
                    CREATE TABLE AccountingAdjustments (
                        AccountKey   NVARCHAR(100) PRIMARY KEY,
                        AccountValue DECIMAL(18,2) NOT NULL DEFAULT 0
                    );
                    
                    INSERT INTO AccountingAdjustments (AccountKey, AccountValue) VALUES
                    ('Land', 0),
                    ('Buildings', 0),
                    ('Machinery', 0),
                    ('Vehicles', 0),
                    ('Furniture', 0),
                    ('Computers', 0),
                    ('Investments', 0),
                    ('Intangibles', 0),
                    ('AccumulatedDepreciation', 0),
                    ('NotesReceivable', 0),
                    ('PrepaidExpenses', 0),
                    ('AccruedRevenues', 0),
                    ('CustodiesAdvances', 0),
                    ('NotesPayable', 0),
                    ('ShortTermLoans', 0),
                    ('AccruedTax', 0),
                    ('AccruedInsurance', 0),
                    ('AccruedExpenses', 0),
                    ('DeferredRevenues', 0),
                    ('LongTermLoans', 0),
                    ('LongTermLiabilities', 0),
                    ('Capital', 0),
                    ('LegalReserve', 0),
                    ('GeneralReserve', 0),
                    ('RetainedEarnings', 0),
                    ('Drawings', 0),
                    ('GainOnAssetSale', 0),
                    ('InterestEarned', 0),
                    ('FXGain', 0),
                    ('OtherRevenues', 0),
                    ('InterestPaid', 0),
                    ('FXLoss', 0),
                    ('LossOnAssetSale', 0),
                    ('FinesPenalties', 0),
                    ('OtherExpenses', 0),
                    ('IncomeTax', 0);
                END");

                SafeMigrate("ExpenseTypes.SeedProfessional", @"
                IF OBJECT_ID('ExpenseTypes', 'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الرواتب والأجور')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0010', N'الرواتب والأجور');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'التأمينات')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0011', N'التأمينات');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الإيجارات')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0012', N'الإيجارات');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الكهرباء والمياه')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0013', N'الكهرباء والمياه');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الاتصالات والإنترنت')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0014', N'الاتصالات والإنترنت');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الوقود والمحروقات')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0015', N'الوقود والمحروقات');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الصيانة')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0016', N'الصيانة');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'النقل والشحن')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0017', N'النقل والشحن');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'التسويق والإعلان')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0018', N'التسويق والإعلان');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الضيافة')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0019', N'الضيافة');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الأدوات المكتبية')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0020', N'الأدوات المكتبية');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'الإهلاك')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0021', N'الإهلاك');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'المصروفات البنكية')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0022', N'المصروفات البنكية');
                    IF NOT EXISTS (SELECT * FROM ExpenseTypes WHERE ExpenseTypeName = N'مصروفات متنوعة')
                        INSERT INTO ExpenseTypes (ExpenseTypeCode, ExpenseTypeName) VALUES ('EXP-0023', N'مصروفات متنوعة');
                END");

                // ===== v24: Performance Indexes =====
                SafeMigrate("IX_Sales_SaleDate", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate' AND object_id = OBJECT_ID('Sales'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Sales_SaleDate ON Sales(SaleDate DESC);
                END");

                SafeMigrate("IX_Sales_ClientID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_ClientID' AND object_id = OBJECT_ID('Sales'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Sales_ClientID ON Sales(ClientID) INCLUDE (SaleDate, TotalAmount, SaleType);
                END");

                SafeMigrate("IX_SaleItems_SaleID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_SaleID' AND object_id = OBJECT_ID('SaleItems'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SaleItems_SaleID ON SaleItems(SaleID) INCLUDE (ProductID, Quantity, UnitPrice, TotalPrice, Factor);
                END");

                SafeMigrate("IX_SaleItems_ProductID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_ProductID' AND object_id = OBJECT_ID('SaleItems'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SaleItems_ProductID ON SaleItems(ProductID);
                END");

                SafeMigrate("IX_ClientTrans_ClientID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientTrans_ClientID' AND object_id = OBJECT_ID('ClientTransactions'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_ClientTrans_ClientID ON ClientTransactions(ClientID) INCLUDE (Debit, Credit, TransType);
                END");

                SafeMigrate("IX_ClientTrans_TransDate", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientTrans_TransDate' AND object_id = OBJECT_ID('ClientTransactions'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_ClientTrans_TransDate ON ClientTransactions(TransDate DESC);
                END");

                SafeMigrate("IX_SupplierTrans_SupplierID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierTrans_SupplierID' AND object_id = OBJECT_ID('SupplierTransactions'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SupplierTrans_SupplierID ON SupplierTransactions(SupplierID) INCLUDE (Debit, Credit, TransType);
                END");

                SafeMigrate("IX_SupplierTrans_TransDate", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierTrans_TransDate' AND object_id = OBJECT_ID('SupplierTransactions'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SupplierTrans_TransDate ON SupplierTransactions(TransDate DESC);
                END");

                SafeMigrate("IX_SalesReturns_SaleID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesReturns_SaleID' AND object_id = OBJECT_ID('SalesReturns'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_SalesReturns_SaleID ON SalesReturns(SaleID);
                END");

                SafeMigrate("IX_ReturnItems_ReturnID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ReturnItems_ReturnID' AND object_id = OBJECT_ID('ReturnItems'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_ReturnItems_ReturnID ON ReturnItems(ReturnID) INCLUDE (Quantity, UnitPrice);
                END");

                SafeMigrate("IX_Purchases_SupplierID", @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchases_SupplierID' AND object_id = OBJECT_ID('Purchases'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Purchases_SupplierID ON Purchases(SupplierID) INCLUDE (PurchaseDate, TotalAmount);
                END");

                // ── Restaurant System Migrations ──
                SafeMigrate("Sales.RestaurantFields", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'OrderType')
                BEGIN
                    ALTER TABLE Sales ADD OrderType NVARCHAR(50) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TableNumber')
                BEGIN
                    ALTER TABLE Sales ADD TableNumber NVARCHAR(20) NULL;
                END");

                SafeMigrate("SaleItems.KitchenNotes", @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'KitchenNotes')
                BEGIN
                    ALTER TABLE SaleItems ADD KitchenNotes NVARCHAR(200) NULL;
                END");

                // Save version number so we don't repeat inspection on next startup
                try
                {
                    AppConfig.Set(SchemaVersionKey, CurrentSchemaVersion.ToString());
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.Error("EnsureDatabaseSchema overall process failed", ex);
                MessageBox.Show("فشل تطبيق بعض ترحيلات قاعدة البيانات:\n" + ex.Message, "تنبيه في التهيئة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // لا نعيد رمي الاستثناء حتى لا يتعطل تشغيل التطبيق بالكامل في حال وجود أخطاء طفيفة
            }
        }

        public static void EnsurePermissionsColumns()
        {
            try
            {
                Execute(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanAdd')
                        ALTER TABLE Permissions ADD CanAdd BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanEdit')
                        ALTER TABLE Permissions ADD CanEdit BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanDelete')
                        ALTER TABLE Permissions ADD CanDelete BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewDetails')
                        ALTER TABLE Permissions ADD CanViewDetails BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewBalance')
                        ALTER TABLE Permissions ADD CanViewBalance BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanChangeSafe')
                        ALTER TABLE Permissions ADD CanChangeSafe BIT DEFAULT 1;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewSalesTotals')
                        ALTER TABLE Permissions ADD CanViewSalesTotals BIT DEFAULT 1;
                ");
            }
            catch (Exception ex)
            {
                AppLogger.Error("EnsurePermissionsColumns failed", ex);
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
