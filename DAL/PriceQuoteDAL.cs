using System;
using System.Collections.Generic;
using System.Data;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class PriceQuoteDAL
    {
        public static void EnsureSchema()
        {
            try
            {
                DbHelper.Execute(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='PriceQuotes')
                    BEGIN
                        CREATE TABLE PriceQuotes (
                            QuoteID INT IDENTITY(1,1) PRIMARY KEY,
                            QuoteCode NVARCHAR(50) NOT NULL,
                            QuoteDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ClientID INT NULL,
                            ClientName NVARCHAR(200) NULL,
                            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            DiscountPct DECIMAL(18,2) NOT NULL DEFAULT 0,
                            Notes NVARCHAR(MAX) NULL,
                            CreatedBy INT NOT NULL,
                            WarehouseID INT NULL,
                            PriceTier NVARCHAR(50) DEFAULT N'قطاعي',
                            Status NVARCHAR(20) DEFAULT N'Pending',
                            ConvertedSaleID INT NULL,
                            LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='PriceQuoteItems')
                    BEGIN
                        CREATE TABLE PriceQuoteItems (
                            QuoteItemID INT IDENTITY(1,1) PRIMARY KEY,
                            QuoteID INT NOT NULL,
                            ProductID INT NOT NULL,
                            ProductName NVARCHAR(250) NULL,
                            Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
                            UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                            TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                            DiscountPct DECIMAL(18,2) NOT NULL DEFAULT 0,
                            DiscountAmt DECIMAL(18,2) NOT NULL DEFAULT 0,
                            UnitName NVARCHAR(50) NULL,
                            Factor DECIMAL(18,4) NOT NULL DEFAULT 1,
                            ShelfLocation NVARCHAR(100) NULL
                        );
                    END
                ");
            }
            catch (Exception ex)
            {
                AppLogger.Error("EnsurePriceQuoteSchema failed", ex, "PriceQuoteDAL");
            }
        }

        public static int SaveQuote(int? clientID, string clientName, decimal total, decimal discAmt, decimal discPct,
            string notes, List<SaleItemDTO> items, int? warehouseID, string priceTier, int? quoteID = null)
        {
            EnsureSchema();
            int returnedID = 0;

            DbHelper.RunInTransaction((con, trans) =>
            {
                if (quoteID.HasValue && quoteID.Value > 0)
                {
                    returnedID = quoteID.Value;
                    DbHelper.ExecuteTrans(trans,
                        @"UPDATE PriceQuotes 
                          SET ClientID=@cid, ClientName=@cname, TotalAmount=@tot, DiscountAmount=@damt, DiscountPct=@dpct,
                              Notes=@n, WarehouseID=@wid, PriceTier=@pt, LastModifiedDate=GETDATE()
                          WHERE QuoteID=@qid",
                        DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                        DbHelper.P("@cname", string.IsNullOrEmpty(clientName) ? DBNull.Value : (object)clientName),
                        DbHelper.P("@tot", total), DbHelper.P("@damt", discAmt), DbHelper.P("@dpct", discPct),
                        DbHelper.P("@n", notes), DbHelper.P("@wid", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value),
                        DbHelper.P("@pt", priceTier ?? "قطاعي"), DbHelper.P("@qid", returnedID));

                    DbHelper.ExecuteTrans(trans, "DELETE FROM PriceQuoteItems WHERE QuoteID=@qid", DbHelper.P("@qid", returnedID));
                }
                else
                {
                    var nextCodeRes = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(QuoteID), 0) + 1 FROM PriceQuotes");
                    string code = "Q-" + (nextCodeRes != null ? nextCodeRes.ToString() : "1");

                    returnedID = DbHelper.ExecuteInsertTrans(trans,
                        @"INSERT INTO PriceQuotes(QuoteCode, QuoteDate, ClientID, ClientName, TotalAmount, DiscountAmount, DiscountPct, Notes, CreatedBy, WarehouseID, PriceTier, Status)
                          VALUES(@code, GETDATE(), @cid, @cname, @tot, @damt, @dpct, @n, @by, @wid, @pt, 'Pending')",
                        DbHelper.P("@code", code),
                        DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                        DbHelper.P("@cname", string.IsNullOrEmpty(clientName) ? DBNull.Value : (object)clientName),
                        DbHelper.P("@tot", total), DbHelper.P("@damt", discAmt), DbHelper.P("@dpct", discPct),
                        DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@wid", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value),
                        DbHelper.P("@pt", priceTier ?? "قطاعي"));
                }

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO PriceQuoteItems(QuoteID, ProductID, ProductName, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, UnitName, Factor, ShelfLocation)
                          VALUES(@qid, @pid, @pname, @qty, @up, @tp, @dpct, @damt, @un, @fac, @loc)",
                        DbHelper.P("@qid", returnedID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@pname", item.ProductName), DbHelper.P("@qty", item.Quantity),
                        DbHelper.P("@up", item.UnitPrice), DbHelper.P("@tp", item.TotalPrice),
                        DbHelper.P("@dpct", item.DiscountPct), DbHelper.P("@damt", item.DiscountAmt),
                        DbHelper.P("@un", item.UnitName), DbHelper.P("@fac", item.Factor),
                        DbHelper.P("@loc", string.IsNullOrEmpty(item.ShelfLocation) ? DBNull.Value : (object)item.ShelfLocation));
                }
            });

            return returnedID;
        }

        public static DataTable GetPendingQuotes()
        {
            EnsureSchema();
            return DbHelper.Query(@"
                SELECT q.QuoteID, q.QuoteCode, q.QuoteDate, q.ClientID, q.ClientName,
                       COALESCE(c.ClientName, q.ClientName, N'عميل بدون اسم') AS DisplayClient,
                       q.TotalAmount, q.DiscountAmount, q.DiscountPct, q.Notes, q.WarehouseID, q.PriceTier,
                       COALESCE(e.EmpName, N'---') AS CreatedByName,
                       (SELECT COUNT(*) FROM PriceQuoteItems qi WHERE qi.QuoteID = q.QuoteID) AS ItemCount
                FROM PriceQuotes q
                LEFT JOIN Clients c ON q.ClientID = c.ClientID
                LEFT JOIN Employees e ON q.CreatedBy = e.EmpID
                WHERE q.Status = 'Pending'
                ORDER BY q.QuoteID DESC");
        }

        public static DataRow GetQuoteHeader(int quoteID)
        {
            EnsureSchema();
            var dt = DbHelper.Query(@"
                SELECT q.QuoteID, q.QuoteCode, q.QuoteDate, q.ClientID, q.ClientName,
                       COALESCE(c.ClientName, q.ClientName) AS DisplayClient,
                       q.TotalAmount, q.DiscountAmount, q.DiscountPct, q.Notes, q.WarehouseID, q.PriceTier, q.Status
                FROM PriceQuotes q
                LEFT JOIN Clients c ON q.ClientID = c.ClientID
                WHERE q.QuoteID = @id", DbHelper.P("@id", quoteID));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static DataTable GetQuoteItems(int quoteID)
        {
            EnsureSchema();
            return DbHelper.Query(@"
                SELECT qi.QuoteItemID, qi.QuoteID, qi.ProductID, qi.ProductName, qi.Quantity, qi.UnitPrice,
                       qi.TotalPrice, qi.DiscountPct, qi.DiscountAmt, qi.UnitName, qi.Factor, qi.ShelfLocation,
                       p.ProductCode, p.PartNumber, COALESCE(qi.ShelfLocation, p.ShelfLocation, N'') AS ProductShelfLocation
                FROM PriceQuoteItems qi
                LEFT JOIN Products p ON qi.ProductID = p.ProductID
                WHERE qi.QuoteID = @id", DbHelper.P("@id", quoteID));
        }

        public static void MarkAsConverted(int quoteID, int saleID)
        {
            EnsureSchema();
            DbHelper.Execute(@"
                UPDATE PriceQuotes 
                SET Status = 'Converted', ConvertedSaleID = @sid, LastModifiedDate = GETDATE()
                WHERE QuoteID = @qid", DbHelper.P("@qid", quoteID), DbHelper.P("@sid", saleID));
        }

        public static void DeleteQuote(int quoteID)
        {
            EnsureSchema();
            DbHelper.Execute("DELETE FROM PriceQuoteItems WHERE QuoteID=@qid", DbHelper.P("@qid", quoteID));
            DbHelper.Execute("DELETE FROM PriceQuotes WHERE QuoteID=@qid", DbHelper.P("@qid", quoteID));
        }
    }
}
