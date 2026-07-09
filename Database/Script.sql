USE master
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ChickenDist')
BEGIN
    ALTER DATABASE ChickenDist SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ChickenDist;
END
GO

CREATE DATABASE ChickenDist
    COLLATE Arabic_CI_AS
GO

ALTER DATABASE ChickenDist SET RECOVERY SIMPLE
GO

USE ChickenDist
GO

-- ========================================
-- الموظفين والصلاحيات
-- ========================================
CREATE TABLE Employees (
    EmpID     INT IDENTITY(1,1) PRIMARY KEY,
    EmpName   NVARCHAR(100) NOT NULL,
    UserName  NVARCHAR(50)  NOT NULL UNIQUE,
    Password  NVARCHAR(100) NOT NULL,
    Role      NVARCHAR(20)  NOT NULL DEFAULT 'User',  -- Admin / Driver / User
    Phone     NVARCHAR(20),
    IsActive  BIT DEFAULT 1,
    IsDriver  BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
)

CREATE TABLE Permissions (
    PermID       INT IDENTITY(1,1) PRIMARY KEY,
    EmpID        INT NOT NULL REFERENCES Employees(EmpID) ON DELETE CASCADE,
    ScreenName   NVARCHAR(60) NOT NULL,
    CanAccess    BIT DEFAULT 0,
    CanEditPrice BIT DEFAULT 0
)

-- ========================================
-- الأصناف
-- ========================================
CREATE TABLE Products (
    ProductID   INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode NVARCHAR(20),
    ProductName NVARCHAR(100) NOT NULL,
    Unit        NVARCHAR(20)  DEFAULT N'كتكوت',
    SalePrice   DECIMAL(10,2) DEFAULT 0,
    IsActive    BIT DEFAULT 1,
    PrintLocalBarcode BIT NOT NULL DEFAULT 1,
    IsQuickItem BIT NOT NULL DEFAULT 0
)

-- ========================================
-- العملاء
-- ========================================
CREATE TABLE Clients (
    ClientID       INT IDENTITY(1,1) PRIMARY KEY,
    ClientCode     NVARCHAR(20),
    ClientName     NVARCHAR(100) NOT NULL,
    Phone          NVARCHAR(20),
    Address        NVARCHAR(200),
    OpeningBalance DECIMAL(10,2) DEFAULT 0,
    IsActive       BIT DEFAULT 1,
    CreatedAt      DATETIME DEFAULT GETDATE()
)

-- ========================================
-- المبيعات
-- ========================================
CREATE TABLE Sales (
    SaleID      INT IDENTITY(1,1) PRIMARY KEY,
    SaleCode    NVARCHAR(20),
    SaleDate    DATETIME DEFAULT GETDATE(),
    SaleType    NVARCHAR(20) NOT NULL,  -- Credit / DriverLoad / Cash
    ClientID    INT REFERENCES Clients(ClientID),
    DriverID    INT REFERENCES Employees(EmpID),
    TotalAmount DECIMAL(10,2) DEFAULT 0,
    Notes       NVARCHAR(500),
    CreatedBy   INT REFERENCES Employees(EmpID),
    IsPosted    BIT DEFAULT 1
)

CREATE TABLE SaleItems (
    ItemID      INT IDENTITY(1,1) PRIMARY KEY,
    SaleID      INT NOT NULL REFERENCES Sales(SaleID) ON DELETE CASCADE,
    ProductID   INT NOT NULL REFERENCES Products(ProductID),
    Quantity    DECIMAL(10,3),
    UnitPrice   DECIMAL(10,2),
    TotalPrice  DECIMAL(10,2)
)

-- ========================================
-- تحميل المندوب
-- ========================================
CREATE TABLE DriverLoads (
    LoadID    INT IDENTITY(1,1) PRIMARY KEY,
    LoadDate  DATETIME DEFAULT GETDATE(),
    DriverID  INT NOT NULL REFERENCES Employees(EmpID),
    SaleID    INT REFERENCES Sales(SaleID),
    IsClosed  BIT DEFAULT 0,
    ClosedAt  DATETIME,
    Notes     NVARCHAR(500)
)

CREATE TABLE DriverLoadItems (
    LoadItemID INT IDENTITY(1,1) PRIMARY KEY,
    LoadID     INT NOT NULL REFERENCES DriverLoads(LoadID) ON DELETE CASCADE,
    ProductID  INT NOT NULL REFERENCES Products(ProductID),
    LoadedQty  DECIMAL(10,3),
    UnitPrice  DECIMAL(10,2)
)

-- ========================================
-- اتسلام عهدة المندوب
-- ========================================
CREATE TABLE DriverHandovers (
    HandoverID    INT IDENTITY(1,1) PRIMARY KEY,
    HandoverDate  DATETIME DEFAULT GETDATE(),
    LoadID        INT NOT NULL REFERENCES DriverLoads(LoadID),
    DriverID      INT NOT NULL REFERENCES Employees(EmpID),
    TotalLoaded   DECIMAL(10,3) DEFAULT 0,
    TotalReturned DECIMAL(10,3) DEFAULT 0,
    TotalDead     DECIMAL(10,3) DEFAULT 0,
    TotalExtra    DECIMAL(10,3) DEFAULT 0,
    TotalDeficit  DECIMAL(10,3) DEFAULT 0,
    Notes         NVARCHAR(500),
    CreatedBy     INT REFERENCES Employees(EmpID)
)

CREATE TABLE HandoverItems (
    HItemID     INT IDENTITY(1,1) PRIMARY KEY,
    HandoverID  INT NOT NULL REFERENCES DriverHandovers(HandoverID) ON DELETE CASCADE,
    ProductID   INT NOT NULL REFERENCES Products(ProductID),
    LoadedQty   DECIMAL(10,3) DEFAULT 0,
    ReturnedQty DECIMAL(10,3) DEFAULT 0,  -- مرتجع
    DeadQty     DECIMAL(10,3) DEFAULT 0,  -- نافق
    ExtraQty    DECIMAL(10,3) DEFAULT 0,  -- زيادة (قديمة)
    DeficitQty  DECIMAL(10,3) DEFAULT 0   -- عجز
)

-- ========================================
-- حركات حساب العميل
-- ========================================
CREATE TABLE ClientTransactions (
    TransID    INT IDENTITY(1,1) PRIMARY KEY,
    TransDate  DATETIME DEFAULT GETDATE(),
    ClientID   INT NOT NULL REFERENCES Clients(ClientID),
    TransType  NVARCHAR(30),  -- Sale / Return / Payment / Opening
    Debit      DECIMAL(10,2) DEFAULT 0,  -- مدين (على العميل)
    Credit     DECIMAL(10,2) DEFAULT 0,  -- دائن (للعميل)
    RefID      INT,
    Notes      NVARCHAR(500),
    CreatedBy  INT REFERENCES Employees(EmpID)
)

-- ========================================
-- الخزنة
-- ========================================
CREATE TABLE CashBox (
    CashID    INT IDENTITY(1,1) PRIMARY KEY,
    TransDate DATETIME DEFAULT GETDATE(),
    TransType NVARCHAR(30),  -- SaleIncome / ClientPayment / Expense / Opening
    AmountIn  DECIMAL(10,2) DEFAULT 0,
    AmountOut DECIMAL(10,2) DEFAULT 0,
    RefID     INT,
    Notes     NVARCHAR(500),
    CreatedBy INT REFERENCES Employees(EmpID)
)

-- ========================================
-- المصروفات
-- ========================================
CREATE TABLE Expenses (
    ExpenseID   INT IDENTITY(1,1) PRIMARY KEY,
    ExpenseDate DATETIME DEFAULT GETDATE(),
    ExpenseType NVARCHAR(50),
    Amount      DECIMAL(10,2),
    Notes       NVARCHAR(500),
    CreatedBy   INT REFERENCES Employees(EmpID)
)

-- ========================================
-- مرتجع البيع
-- ========================================
CREATE TABLE SalesReturns (
    ReturnID    INT IDENTITY(1,1) PRIMARY KEY,
    ReturnDate  DATETIME DEFAULT GETDATE(),
    SaleID      INT REFERENCES Sales(SaleID),
    ClientID    INT REFERENCES Clients(ClientID),
    TotalAmount DECIMAL(10,2),
    Notes       NVARCHAR(500),
    CreatedBy   INT REFERENCES Employees(EmpID)
)

CREATE TABLE ReturnItems (
    RItemID    INT IDENTITY(1,1) PRIMARY KEY,
    ReturnID   INT NOT NULL REFERENCES SalesReturns(ReturnID) ON DELETE CASCADE,
    ProductID  INT NOT NULL REFERENCES Products(ProductID),
    Quantity   DECIMAL(10,3),
    UnitPrice  DECIMAL(10,2),
    TotalPrice DECIMAL(10,2)
)

-- ========================================
-- المخازن
-- ========================================
CREATE TABLE Warehouses (
    WarehouseID INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseName NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NULL,
    Notes NVARCHAR(500) NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
)

-- ========================================
-- كميات الأصناف لكل مخزن
-- ========================================
CREATE TABLE ProductStock (
    StockID     INT IDENTITY(1,1) PRIMARY KEY,
    ProductID   INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
    WarehouseID INT NOT NULL REFERENCES Warehouses(WarehouseID),
    Quantity    DECIMAL(10,3) NOT NULL DEFAULT 0,
    LastUpdated DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_ProductStock UNIQUE (ProductID, WarehouseID)
)

-- ========================================
-- تسويات وتعديلات كميات الأصناف
-- ========================================
CREATE TABLE StockAdjustments (
    AdjID INT IDENTITY(1,1) PRIMARY KEY,
    AdjDate DATETIME DEFAULT GETDATE(),
    ProductID INT NOT NULL REFERENCES Products(ProductID) ON DELETE CASCADE,
    WarehouseID INT NULL REFERENCES Warehouses(WarehouseID),
    BookQty DECIMAL(10,3) NOT NULL,
    ActualQty DECIMAL(10,3) NOT NULL,
    Notes NVARCHAR(500),
    CreatedBy INT REFERENCES Employees(EmpID),
    UnitName NVARCHAR(50) NULL,
    Factor DECIMAL(10,3) NULL
)

-- ========================================
-- بيانات ابتدائية
-- ========================================
-- مدير النظام الافتراضي (كلمة المرور: 1)
INSERT INTO Employees (EmpName, UserName, Password, Role, IsActive)
VALUES (N'مدير النظام', '1', '1', 'Admin', 1)

-- المخزن الرئيسي الافتراضي
INSERT INTO Warehouses (WarehouseName, Location, Notes, IsActive)
VALUES (N'المخزن الرئيسي', N'المقر الرئيسي', N'المخزن الأساسي للنظام', 1)

-- صنف تجريبي
INSERT INTO Products (ProductCode, ProductName, Unit, SalePrice)
VALUES ('P001', N'كتكوت بياض', N'كتكوت', 15.00),
       ('P002', N'كتكوت لحم', N'كتكوت', 12.00),
       ('P003', N'كتكوت ملون', N'كتكوت', 10.00)

GO

-- ========================================
-- Views مفيدة
-- ========================================
CREATE VIEW vw_ClientBalance AS
SELECT
    c.ClientID,
    c.ClientName,
    c.Phone,
    c.OpeningBalance,
    ISNULL(SUM(ct.Debit),0)  AS TotalDebit,
    ISNULL(SUM(ct.Credit),0) AS TotalCredit,
    c.OpeningBalance + ISNULL(SUM(ct.Debit),0) - ISNULL(SUM(ct.Credit),0) AS Balance
FROM Clients c
LEFT JOIN ClientTransactions ct ON c.ClientID = ct.ClientID
GROUP BY c.ClientID, c.ClientName, c.Phone, c.OpeningBalance
GO

CREATE VIEW vw_CashBalance AS
SELECT
    ISNULL(SUM(AmountIn),0)  AS TotalIn,
    ISNULL(SUM(AmountOut),0) AS TotalOut,
    ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) AS Balance
FROM CashBox
GO

PRINT N'تم إنشاء قاعدة البيانات بنجاح!'
GO
