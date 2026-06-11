-- ============================================================
-- سكريبت إصلاح الأعمدة الناقصة في قاعدة البيانات
-- ChickenDist - Fix Missing Columns Script
-- تاريخ: 2026-06-11  |  الإصدار: v1.6.6
-- تشغيل مرة واحدة فقط على قاعدة البيانات المثبتة
-- ============================================================

USE ChickenDist;
GO

PRINT N'--- بدء إصلاح الأعمدة الناقصة ---';

-- ============================================================
-- 1. أعمدة الخصم في جدول Sales
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DiscountAmount')
BEGIN
    ALTER TABLE Sales ADD DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود DiscountAmount إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.DiscountAmount موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DiscountPct')
BEGIN
    ALTER TABLE Sales ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود DiscountPct إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.DiscountPct موجود مسبقاً';

-- ============================================================
-- 2. أعمدة الخصم في جدول SaleItems  ← المشكلة الرئيسية
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'DiscountPct')
BEGIN
    ALTER TABLE SaleItems ADD DiscountPct DECIMAL(5,2) NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود DiscountPct إلى جدول SaleItems';
END
ELSE PRINT N'✓ SaleItems.DiscountPct موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'DiscountAmt')
BEGIN
    ALTER TABLE SaleItems ADD DiscountAmt DECIMAL(10,2) NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود DiscountAmt إلى جدول SaleItems';
END
ELSE PRINT N'✓ SaleItems.DiscountAmt موجود مسبقاً';

-- ============================================================
-- 3. عمود CloudID في Sales
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CloudID')
BEGIN
    ALTER TABLE Sales ADD CloudID BIGINT NULL;
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_CloudID' AND object_id = OBJECT_ID('Sales'))
        CREATE INDEX IX_Sales_CloudID ON Sales(CloudID);
    PRINT N'✔ تمت إضافة عمود CloudID إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.CloudID موجود مسبقاً';

-- ============================================================
-- 4. أعمدة شرائح الأسعار في جدول Sales و SaleItems
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PriceTier')
BEGIN
    ALTER TABLE Sales ADD PriceTier NVARCHAR(20) DEFAULT N'قطاعي';
    PRINT N'✔ تمت إضافة عمود PriceTier إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.PriceTier موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'PriceTier')
BEGIN
    ALTER TABLE SaleItems ADD PriceTier NVARCHAR(20) DEFAULT N'قطاعي';
    PRINT N'✔ تمت إضافة عمود PriceTier إلى جدول SaleItems';
END
ELSE PRINT N'✓ SaleItems.PriceTier موجود مسبقاً';

-- ============================================================
-- 5. LastModifiedDate في Sales
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'LastModifiedDate')
BEGIN
    ALTER TABLE Sales ADD LastModifiedDate DATETIME NULL;
    EXEC('UPDATE Sales SET LastModifiedDate = GETDATE() WHERE LastModifiedDate IS NULL');
    PRINT N'✔ تمت إضافة عمود LastModifiedDate إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.LastModifiedDate موجود مسبقاً';

-- ============================================================
-- 6. أعمدة المخزن WarehouseID
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Warehouses')
BEGIN
    CREATE TABLE Warehouses (
        WarehouseID   INT IDENTITY(1,1) PRIMARY KEY,
        WarehouseName NVARCHAR(100) NOT NULL,
        Location      NVARCHAR(200) NULL,
        Notes         NVARCHAR(500) NULL,
        IsActive      BIT DEFAULT 1,
        CreatedAt     DATETIME DEFAULT GETDATE()
    );
    INSERT INTO Warehouses(WarehouseName, Location, Notes, IsActive)
    VALUES(N'المخزن الرئيسي', N'المقر الرئيسي', N'المخزن الأساسي للنظام', 1);
    PRINT N'✔ تم إنشاء جدول Warehouses وإضافة المخزن الرئيسي';
END
ELSE PRINT N'✓ جدول Warehouses موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'WarehouseID')
BEGIN
    ALTER TABLE Sales ADD WarehouseID INT NULL REFERENCES Warehouses(WarehouseID);
    EXEC('UPDATE Sales SET WarehouseID = 1 WHERE WarehouseID IS NULL');
    PRINT N'✔ تمت إضافة عمود WarehouseID إلى جدول Sales';
END
ELSE PRINT N'✓ Sales.WarehouseID موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SalesReturns') AND name = 'WarehouseID')
BEGIN
    ALTER TABLE SalesReturns ADD WarehouseID INT NULL REFERENCES Warehouses(WarehouseID);
    EXEC('UPDATE SalesReturns SET WarehouseID = 1 WHERE WarehouseID IS NULL');
    PRINT N'✔ تمت إضافة عمود WarehouseID إلى جدول SalesReturns';
END
ELSE PRINT N'✓ SalesReturns.WarehouseID موجود مسبقاً';

-- ============================================================
-- 7. أعمدة Clients الإضافية
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'CurrentDebt')
BEGIN
    ALTER TABLE Clients ADD CurrentDebt DECIMAL(12,2) NOT NULL DEFAULT 0;
    EXEC('UPDATE c SET c.CurrentDebt = c.OpeningBalance
              + ISNULL((SELECT SUM(ct.Debit) - SUM(ct.Credit)
                        FROM ClientTransactions ct
                        WHERE ct.ClientID = c.ClientID), 0)
          FROM Clients c');
    PRINT N'✔ تمت إضافة عمود CurrentDebt إلى جدول Clients';
END
ELSE PRINT N'✓ Clients.CurrentDebt موجود مسبقاً';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'DefaultPriceTier')
BEGIN
    ALTER TABLE Clients ADD DefaultPriceTier NVARCHAR(20) DEFAULT N'قطاعي';
    PRINT N'✔ تمت إضافة عمود DefaultPriceTier إلى جدول Clients';
END
ELSE PRINT N'✓ Clients.DefaultPriceTier موجود مسبقاً';

-- ============================================================
-- 8. أعمدة Products الإضافية
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'WholesalePrice')
BEGIN
    ALTER TABLE Products ADD WholesalePrice DECIMAL(10,2) DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود WholesalePrice إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SemiWholesalePrice')
BEGIN
    ALTER TABLE Products ADD SemiWholesalePrice DECIMAL(10,2) DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود SemiWholesalePrice إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PartNumber')
BEGIN
    ALTER TABLE Products ADD PartNumber NVARCHAR(100) NULL;
    PRINT N'✔ تمت إضافة عمود PartNumber إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CarModel')
BEGIN
    ALTER TABLE Products ADD CarModel NVARCHAR(200) NULL;
    PRINT N'✔ تمت إضافة عمود CarModel إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Brand')
BEGIN
    ALTER TABLE Products ADD Brand NVARCHAR(100) NULL;
    PRINT N'✔ تمت إضافة عمود Brand إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ShelfLocation')
BEGIN
    ALTER TABLE Products ADD ShelfLocation NVARCHAR(100) NULL;
    PRINT N'✔ تمت إضافة عمود ShelfLocation إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CostPrice')
BEGIN
    ALTER TABLE Products ADD CostPrice DECIMAL(10,3) NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CostPrice إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingSalePrice')
BEGIN
    ALTER TABLE Products ADD PendingSalePrice DECIMAL(10,3) NULL;
    PRINT N'✔ تمت إضافة عمود PendingSalePrice إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingQtyThreshold')
BEGIN
    ALTER TABLE Products ADD PendingQtyThreshold DECIMAL(10,3) NULL;
    PRINT N'✔ تمت إضافة عمود PendingQtyThreshold إلى جدول Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'PendingPriceSourceRefID')
BEGIN
    ALTER TABLE Products ADD PendingPriceSourceRefID INT NULL;
    PRINT N'✔ تمت إضافة عمود PendingPriceSourceRefID إلى جدول Products';
END

-- ============================================================
-- 9. أعمدة الصلاحيات Permissions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanShowCostProfit')
BEGIN
    ALTER TABLE Permissions ADD CanShowCostProfit BIT NOT NULL DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CanShowCostProfit إلى جدول Permissions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanEditSalesInvoice')
BEGIN
    ALTER TABLE Permissions ADD CanEditSalesInvoice BIT DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CanEditSalesInvoice إلى جدول Permissions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanViewCost')
BEGIN
    ALTER TABLE Permissions ADD CanViewCost BIT DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CanViewCost إلى جدول Permissions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanDeleteSalesInvoice')
BEGIN
    ALTER TABLE Permissions ADD CanDeleteSalesInvoice BIT DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CanDeleteSalesInvoice إلى جدول Permissions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'CanCopySalesInvoice')
BEGIN
    ALTER TABLE Permissions ADD CanCopySalesInvoice BIT DEFAULT 0;
    PRINT N'✔ تمت إضافة عمود CanCopySalesInvoice إلى جدول Permissions';
END

-- ============================================================
-- 10. Employees.PlainPassword
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'PlainPassword')
BEGIN
    ALTER TABLE Employees ADD PlainPassword NVARCHAR(200) NULL;
    PRINT N'✔ تمت إضافة عمود PlainPassword إلى جدول Employees';
END

-- ============================================================
-- 11. جدول التقسيط InstallmentContracts
-- ============================================================
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
    PRINT N'✔ تم إنشاء جدول InstallmentContracts';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InstallmentContracts') AND name = 'ContractCode')
    BEGIN
        ALTER TABLE InstallmentContracts ADD ContractCode NVARCHAR(50) NULL;
        PRINT N'✔ تمت إضافة عمود ContractCode إلى InstallmentContracts';
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
    PRINT N'✔ تم إنشاء جدول InstallmentSchedules';
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
    PRINT N'✔ تم إنشاء جدول InstallmentPayments';
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
    PRINT N'✔ تم إنشاء جدول InstallmentAuditLog';
END

-- ============================================================
-- 12. حسابات الخزينة SafeAccounts
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SafeAccounts')
BEGIN
    CREATE TABLE SafeAccounts (
        AccountID      INT IDENTITY(1,1) PRIMARY KEY,
        AccountName    NVARCHAR(100) NOT NULL UNIQUE,
        AccountType    NVARCHAR(20) NOT NULL,
        AccountNumber  NVARCHAR(50) NULL,
        OpeningBalance DECIMAL(12,2) NOT NULL DEFAULT 0,
        IsActive       BIT NOT NULL DEFAULT 1,
        CreatedAt      DATETIME NOT NULL DEFAULT GETDATE()
    );
    INSERT INTO SafeAccounts (AccountName, AccountType, OpeningBalance, IsActive)
    VALUES (N'الخزينة الرئيسية', 'Cash', 0.00, 1);
    PRINT N'✔ تم إنشاء جدول SafeAccounts';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CashBox') AND name = 'AccountID')
BEGIN
    ALTER TABLE CashBox ADD AccountID INT NULL;
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SafeAccounts')
    BEGIN
        ALTER TABLE CashBox ADD CONSTRAINT FK_CashBox_SafeAccounts 
            FOREIGN KEY (AccountID) REFERENCES SafeAccounts(AccountID);
        EXEC('UPDATE CashBox SET AccountID = 1 WHERE AccountID IS NULL');
    END
    PRINT N'✔ تمت إضافة عمود AccountID إلى جدول CashBox';
END

-- ============================================================
-- 13. إعادة إنشاء View رصيد العملاء المحدّث
-- ============================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ClientBalance')
    DROP VIEW vw_ClientBalance;
GO

CREATE VIEW vw_ClientBalance AS
SELECT
    c.ClientID,
    c.ClientName,
    c.Phone,
    c.OpeningBalance,
    ISNULL(SUM(ct.Debit),0)   AS TotalDebit,
    ISNULL(SUM(ct.Credit),0)  AS TotalCredit,
    c.OpeningBalance + ISNULL(SUM(ct.Debit),0) - ISNULL(SUM(ct.Credit),0) AS Balance
FROM Clients c
LEFT JOIN ClientTransactions ct ON c.ClientID = ct.ClientID
GROUP BY c.ClientID, c.ClientName, c.Phone, c.OpeningBalance;
GO

PRINT N'✔ تم إعادة إنشاء vw_ClientBalance';

-- ============================================================
-- 14. إعادة إنشاء View رصيد الخزينة
-- ============================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CashBalance')
    DROP VIEW vw_CashBalance;
GO

CREATE VIEW vw_CashBalance AS
SELECT
    ISNULL(SUM(AmountIn),0)  AS TotalIn,
    ISNULL(SUM(AmountOut),0) AS TotalOut,
    ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) AS Balance
FROM CashBox;
GO

PRINT N'✔ تم إعادة إنشاء vw_CashBalance';

-- ============================================================
-- 15. جدول الإصدار
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'version')
BEGIN
    CREATE TABLE [version] ([version] NVARCHAR(50) NOT NULL);
    INSERT INTO [version] ([version]) VALUES ('1.0.0');
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('version') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [version] ADD [UpdatedAt] DATETIME NULL;
END

DELETE FROM [version];
INSERT INTO [version] ([version], [UpdatedAt]) VALUES ('1.6.6', GETDATE());
PRINT N'✔ تم تحديث رقم الإصدار إلى 1.6.6';
GO

PRINT N'';
PRINT N'=== تم تطبيق جميع الإصلاحات بنجاح! ===';
PRINT N'يمكنك الآن تشغيل البرنامج بشكل طبيعي.';
GO
