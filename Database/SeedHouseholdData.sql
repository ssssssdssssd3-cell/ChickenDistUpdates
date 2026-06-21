USE ChickenDist;
GO

-- 1. Disable constraints to clear data safely
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";

-- 2. Clear tables
DELETE FROM ClientCratesTransactions;
DELETE FROM InstallmentAuditLog;
DELETE FROM InstallmentPayments;
DELETE FROM InstallmentSchedules;
DELETE FROM InstallmentContracts;
DELETE FROM ProductStock;
DELETE FROM SafeAccounts;
DELETE FROM Vehicles;
DELETE FROM WarehouseTransferItems;
DELETE FROM WarehouseTransfers;
DELETE FROM SaleItemsHistory;
DELETE FROM SalesAudit;
DELETE FROM HandoverItems;
DELETE FROM DriverHandovers;
DELETE FROM DriverLoadItems;
DELETE FROM DriverLoads;
DELETE FROM SaleItems;
DELETE FROM Sales;
DELETE FROM ReturnItems;
DELETE FROM SalesReturns;
DELETE FROM PurchaseItems;
DELETE FROM Purchases;
DELETE FROM PurchaseReturnItems;
DELETE FROM PurchaseReturns;
DELETE FROM WastageLossItems;
DELETE FROM WastageLoss;
DELETE FROM StockAdjustments;
DELETE FROM ClientTransactions;
DELETE FROM Clients;
DELETE FROM SupplierTransactions;
DELETE FROM Suppliers;
DELETE FROM CashBox;
DELETE FROM Expenses;
DELETE FROM EmployeeTransactions;
DELETE FROM PriceChangesLog;
DELETE FROM Products;
DELETE FROM Categories;

-- Keep Admin Employee but delete others
DELETE FROM Employees WHERE UserName <> '1';

-- Keep Main Warehouse but delete others
DELETE FROM Warehouses WHERE WarehouseID <> 1;

-- 3. Reset identities
DBCC CHECKIDENT ('Products', RESEED, 0);
DBCC CHECKIDENT ('Categories', RESEED, 0);
DBCC CHECKIDENT ('Clients', RESEED, 0);
DBCC CHECKIDENT ('Suppliers', RESEED, 0);
DBCC CHECKIDENT ('Sales', RESEED, 0);
DBCC CHECKIDENT ('Purchases', RESEED, 0);
DBCC CHECKIDENT ('Expenses', RESEED, 0);
DBCC CHECKIDENT ('DriverLoads', RESEED, 0);
DBCC CHECKIDENT ('DriverHandovers', RESEED, 0);
DBCC CHECKIDENT ('SalesAudit', RESEED, 0);
DBCC CHECKIDENT ('WastageLoss', RESEED, 0);
DBCC CHECKIDENT ('WarehouseTransfers', RESEED, 0);
DBCC CHECKIDENT ('InstallmentContracts', RESEED, 0);
DBCC CHECKIDENT ('InstallmentPayments', RESEED, 0);
DBCC CHECKIDENT ('InstallmentSchedules', RESEED, 0);
DBCC CHECKIDENT ('InstallmentAuditLog', RESEED, 0);
DBCC CHECKIDENT ('SafeAccounts', RESEED, 0);
DBCC CHECKIDENT ('Vehicles', RESEED, 0);
DBCC CHECKIDENT ('ClientCratesTransactions', RESEED, 0);
DBCC CHECKIDENT ('Warehouses', RESEED, 1);

-- 4. Re-enable constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";
GO

-- 5. Insert Categories
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryID, CategoryName, IsActive) VALUES 
(1, N'أدوات منزلية', 1),
(2, N'أجهزة كهربائية', 1),
(3, N'أدوات كهربائية وتأسيس', 1),
(4, N'إضاءة ولمبات', 1),
(5, N'أدوات ومعدات يدوية', 1);
SET IDENTITY_INSERT Categories OFF;
GO

-- 6. Insert Products (Household & Electrical Tools)
-- Columns: ProductCode, ProductName, Unit, SalePrice, PurchasePrice, MinStockLimit, Description, PartNumber, CategoryID, Brand, ShelfLocation, WholesalePrice, SemiWholesalePrice, IsActive
INSERT INTO Products 
(ProductCode, ProductName, Unit, SalePrice, PurchasePrice, MinStockLimit, Description, PartNumber, CategoryID, Brand, ShelfLocation, WholesalePrice, SemiWholesalePrice, IsActive) 
VALUES 
-- أدوات منزلية (CategoryID = 1)
('P101', N'طقم حلل سيراميك 10 قطع', N'طقم', 2200.00, 1800.00, 5.00, N'طقم حلل سيراميك تركي أصلي مقاوم للالتصاق 10 قطع', 'TR-CER-10', 1, N'Korkmaz', N'A1-1', 2000.00, 2100.00, 1),
('P102', N'طقم تيفال 12 قطعة', N'طقم', 2700.00, 2200.00, 5.00, N'طقم تيفال سافلون مقاوم للحرارة وسهل التنظيف 12 قطعة', 'SF-TEF-12', 1, N'Savlon', N'A1-2', 2450.00, 2550.00, 1),
('P103', N'طقم معالق وشوك 24 قطعة', N'طقم', 480.00, 350.00, 10.00, N'طقم أدوات مائدة ستانلس ستيل عالي الجودة 24 قطعة', 'SS-CUT-24', 1, N'Tramontina', N'A2-1', 400.00, 440.00, 1),
('P104', N'صينية عشاء ستانلس كبير', N'قطعة', 180.00, 120.00, 15.00, N'صينية تقديم ستانلس ستيل مقاس كبير مقاوم للصدأ', 'SS-TRAY-LG', 1, N'Al-Ahram', N'A2-2', 140.00, 160.00, 1),
('P105', N'مقص مطبخ ستانلس متعدد الاستخدام', N'قطعة', 45.00, 25.00, 20.00, N'مقص مطبخ ستانلس بمقبض مريح لتقطيع الدواجن واللحوم', 'KT-SCIS-01', 1, N'Zyliss', N'A3-1', 32.00, 38.00, 1),
('P106', N'ترمس ماء وحفظ حرارة 1 لتر', N'قطعة', 220.00, 150.00, 12.00, N'ترمس لحفظ السوائل الساخنة والباردة سعة 1 لتر', 'TM-THERM-1L', 1, N'Tiger', N'A3-2', 180.00, 200.00, 1),
('P107', N'طقم كاسات زجاج 6 قطع', N'طقم', 130.00, 80.00, 15.00, N'طقم كاسات عصير زجاجي شفاف 6 قطع بتصميم عصري', 'GL-CUP-6', 1, N'Pasabahce', N'A4-1', 100.00, 115.00, 1),

-- أجهزة كهربائية (CategoryID = 2)
('P201', N'خلاط توشيبا بالدورق 600 وات', N'جهاز', 1100.00, 850.00, 4.00, N'خلاط كهربائي توشيبا مع مطحنتين ودورق سعة 1.5 لتر', 'TB-BL-600', 2, N'Toshiba', N'B1-1', 950.00, 1000.00, 1),
('P202', N'مروحة عمود تورنيدو 16 بوصة بالريموت', N'جهاز', 1550.00, 1200.00, 6.00, N'مروحة عمودية تورنيدو 16 بوصة بـ 3 سرعات وريموت كنترول', 'TR-FAN-16R', 2, N'Tornado', N'B1-2', 1350.00, 1450.00, 1),
('P203', N'مكواة بخار تورنيدو 2000 وات', N'جهاز', 850.00, 650.00, 8.00, N'مكواة بخار تورنيدو بقاعدة سيراميك ونظام تنظيف ذاتي', 'TR-IR-2000', 2, N'Tornado', N'B2-1', 720.00, 780.00, 1),
('P204', N'غلاية مياه ستانلس 1.5 لتر', N'جهاز', 220.00, 140.00, 10.00, N'كاتيل كهربائي ستانلس سريع الغليان مع فصل تلقائي', 'KT-ST-1.5', 2, N'MediaTech', N'B2-2', 170.00, 190.00, 1),
('P205', N'كبة لحم وخضروات 400 وات', N'جهاز', 650.00, 480.00, 5.00, N'فرامة لحم وبصل وخضروات بسلاح ستانلس حاد', 'KT-CHOP-400', 2, N'Kenwood', N'B3-1', 540.00, 590.00, 1),
('P206', N'ميكروويف شارب 20 لتر ديجيتال', N'جهاز', 4600.00, 3800.00, 2.00, N'فرن ميكروويف شارب 800 وات ديجيتال بلون فضي', 'SH-MW-20L', 2, N'Sharp', N'B3-2', 4100.00, 4350.00, 1),
('P207', N'سخان مياه كهربائي أوليمبيك 50 لتر', N'جهاز', 2900.00, 2400.00, 3.00, N'سخان مياه كهربائي خزان سعة 50 لتر بمؤشر حرارة', 'OL-WH-50L', 2, N'Olympic', N'B4-1', 2600.00, 2750.00, 1),

-- أدوات كهربائية وتأسيس (CategoryID = 3)
('P301', N'لفة سلك سويدي معزول 2 مم', N'لفة', 2200.00, 1900.00, 8.00, N'لفة سلك نحاس معزول السويدي الأصلي 2 مم طول 100 متر', 'SW-WIRE-2MM', 3, N'El Sewedy', N'C1-1', 2000.00, 2100.00, 1),
('P302', N'لفة سلك سويدي معزول 4 مم', N'لفة', 3900.00, 3400.00, 5.00, N'لفة سلك نحاس معزول السويدي الأصلي 4 مم طول 100 متر', 'SW-WIRE-4MM', 3, N'El Sewedy', N'C1-2', 3600.00, 3750.00, 1),
('P303', N'شريط لحام كهرباء (شكرتون)', N'علبة', 10.00, 5.00, 50.00, N'شريط لاصق عازل للكهرباء عالي الجودة للربط والتثبيت', 'IN-TAPE-01', 3, N'3M', N'C2-1', 7.00, 8.50, 1),
('P304', N'مفتاح أوتوماتيك شنايدر 16 أمبير', N'قطعة', 130.00, 90.00, 12.00, N'قاطع تيار أوتوماتيك شنايدر أحادي 16 أمبير لحماية اللوحة', 'SCH-CB-16A', 3, N'Schneider', N'C2-2', 105.00, 115.00, 1),
('P305', N'لقمة مفتاح كهرباء فينوس', N'قطعة', 25.00, 15.00, 40.00, N'لقمة مفتاح إنارة كهربائي فينوس عالية الجودة', 'VN-SW-MOD', 3, N'Venus', N'C3-1', 18.00, 21.00, 1),
('P306', N'بريزة كهرباء فينوس', N'قطعة', 27.00, 16.00, 40.00, N'بريزة (فيشة) كهرباء فينوس للمخارج الجدارية', 'VN-SO-MOD', 3, N'Venus', N'C3-2', 20.00, 23.00, 1),
('P307', N'شاسيه كهرباء فينوس معدن', N'قطعة', 15.00, 8.00, 50.00, N'شاسيه معدني لتركيب لقم مفاتيح وبرايز فينوس في العلبة', 'VN-FRAME-M', 3, N'Venus', N'C4-1', 10.50, 12.00, 1),
('P308', N'وش مفتاح فينوس أبيض فخم', N'قطعة', 20.00, 12.00, 50.00, N'وش مفاتيح خارجي فينوس بلاستيك بلون أبيض فخم', 'VN-COV-WH', 3, N'Venus', N'C4-2', 15.00, 17.00, 1),
('P309', N'مشترك كهربائي 4 عيون بالفيلتر', N'قطعة', 160.00, 110.00, 15.00, N'مشترك كهرباء 4 مخارج مع زر تشغيل وحماية من الارتفاع المفاجئ', 'VN-EXT-4W', 3, N'Venus', N'C5-1', 130.00, 145.00, 1),

-- إضاءة ولمبات (CategoryID = 4)
('P401', N'لمبة ليد فينوس 9 وات أبيض', N'قطعة', 48.00, 30.00, 100.00, N'لمبة ليد فينوس 9 وات موفرة للطاقة إضاءة بيضاء', 'VN-LED-9W', 4, N'Venus', N'D1-1', 36.00, 42.00, 1),
('P402', N'لمبة ليد فينوس 12 وات أبيض', N'قطعة', 60.00, 38.00, 100.00, N'لمبة ليد فينوس 12 وات موفرة للطاقة إضاءة بيضاء', 'VN-LED-12W', 4, N'Venus', N'D1-2', 46.00, 52.00, 1),
('P403', N'لمبة ليد فينوس 15 وات أصفر', N'قطعة', 75.00, 48.00, 50.00, N'لمبة ليد فينوس 15 وات موفرة للطاقة إضاءة صفراء دافئة', 'VN-LED-15Y', 4, N'Venus', N'D2-1', 58.00, 66.00, 1),
('P404', N'كشاف ليد طوارئ شحن 60 ليد', N'قطعة', 320.00, 220.00, 10.00, N'كشاف طوارئ ليد قابل للشحن يدوم حتى 6 ساعات تشغيل متواصل', 'EM-LIGHT-60', 4, N'Generic', N'D2-2', 260.00, 290.00, 1),
('P405', N'سبوت لايت ليد 7 وات متحرك', N'قطعة', 45.00, 28.00, 30.00, N'سبوت لايت ليد غاطس 7 وات إضاءة مركزة متحرك', 'SP-LED-7W', 4, N'Philips', N'D3-1', 34.00, 39.00, 1),
('P406', N'لمبة فينوس ليد شمعة 4 وات', N'قطعة', 40.00, 24.00, 40.00, N'لمبة ليد شكل شمعة سن رفيع E14 مناسبة للنجف 4 وات', 'VN-LED-CAN', 4, N'Venus', N'D3-2', 30.00, 35.00, 1),

-- أدوات ومعدات يدوية (CategoryID = 5)
('P501', N'مفك مقاسين صليبة وعادة مغناطيسي', N'قطعة', 35.00, 20.00, 20.00, N'مفك ذو وجهين (عادة وصليبة) بمقبض عازل وسن مغناطيسي', 'TL-SCW-DBL', 5, N'Total', N'E1-1', 25.00, 30.00, 1),
('P502', N'بنسة يدوية (زردية) 8 بوصة عازل', N'قطعة', 120.00, 85.00, 15.00, N'بنسة يدوية عازلة للكهرباء للقطع والشد مقاس 8 بوصة', 'TL-PLR-8', 5, N'Total', N'E1-2', 95.00, 105.00, 1),
('P503', N'مفتاح إنجليزي مقاس 10 بوصة للسباكة', N'قطعة', 190.00, 130.00, 10.00, N'مفتاح أنابيب إنجليزي شديد التحمل مقاس 10 بوصة للسباكة والربط', 'TL-WNC-10', 5, N'Total', N'E2-1', 150.00, 170.00, 1),
('P504', N'جاكوش يدوي بمقبض فايبر 500 جرام', N'قطعة', 140.00, 90.00, 10.00, N'شاكوش يدوي رأس حديد صلب ومقبض فايبر مانع للاهتزاز 500 جرام', 'TL-HAM-500', 5, N'Total', N'E2-2', 110.00, 125.00, 1),
('P505', N'شريط قياس (شريط متري) 5 متر', N'قطعة', 65.00, 40.00, 15.00, N'متر قياس سحب ذاتي وقفل مغناطيسي بطول 5 أمتار', 'TL-TAPE-5M', 5, N'Total', N'E3-1', 48.00, 56.00, 1),
('P506', N'طقم مفاتيح ألن مسدس 9 قطع', N'طقم', 150.00, 95.00, 8.00, N'طقم مفاتيح ألن مسدسة المقاسات الكروم شديد الصلابة 9 قطع', 'TL-ALLEN-9', 5, N'Total', N'E3-2', 115.00, 130.00, 1);
GO

PRINT N'تم تفريغ الجداول وتغذية قاعدة البيانات بنجاح!';
GO
