-- Migration: إضافة عمود IsService لجدول Products
-- تشغيل مرة واحدة فقط على قاعدة البيانات
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsService')
BEGIN
    ALTER TABLE Products ADD IsService BIT NOT NULL DEFAULT 0;
    PRINT 'IsService column added successfully.';
END
ELSE
BEGIN
    PRINT 'IsService column already exists.';
END
