using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class SaleDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType,
                         ISNULL(c.ClientName,N'---') AS ClientName,
                         ISNULL(e.EmpName,N'---') AS DriverName,
                         s.TotalAmount, s.Notes
                  FROM Sales s
                  LEFT JOIN Clients c ON s.ClientID = c.ClientID
                  LEFT JOIN Employees e ON s.DriverID = e.EmpID
                  WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                  ORDER BY s.SaleDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable GetItems(int saleID)
        {
            return DbHelper.Query(
                @"SELECT si.ItemID, si.ProductID, p.ProductName, si.Quantity, si.UnitPrice, si.TotalPrice,
                         COALESCE(si.DiscountPct, 0) AS DiscountPct, COALESCE(si.DiscountAmt, 0) AS DiscountAmt
                  FROM SaleItems si JOIN Products p ON si.ProductID=p.ProductID
                  WHERE si.SaleID=@id",
                DbHelper.P("@id", saleID));
        }

        public static int SaveSale(int saleType, int? clientID, int? driverID, decimal total, string notes,
            List<SaleItemDTO> items, decimal discountAmount = 0m, decimal discountPct = 0m)
        {
            string typeStr = saleType == 0 ? "Credit" : saleType == 1 ? "DriverLoad" : "Cash";
            var nextSaleResult = DbHelper.Scalar("SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
            string code = nextSaleResult != null ? nextSaleResult.ToString() : "1";

            int saleID = DbHelper.ExecuteInsert(
                "INSERT INTO Sales(SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,Notes,CreatedBy,DiscountAmount,DiscountPct) VALUES(@code,@dt,@typ,@cid,@did,@tot,@n,@by,@discAmt,@discPct)",
                DbHelper.P("@code", code), DbHelper.P("@dt", DateTime.Now), DbHelper.P("@typ", typeStr),
                DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                DbHelper.P("@did", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                DbHelper.P("@discAmt", discountAmount), DbHelper.P("@discPct", discountPct));

            if (saleID <= 0) return -1;

            foreach (var item in items)
            {
                DbHelper.Execute(
                    "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt) VALUES(@sid,@pid,@qty,@up,@tp,@dpct,@damt)",
                    DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                    DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                    DbHelper.P("@tp", item.TotalPrice), DbHelper.P("@dpct", item.DiscountPct),
                    DbHelper.P("@damt", item.DiscountAmt));
            }

            // آجل: أضف للحساب
            if (typeStr == "Credit" && clientID.HasValue)
            {
                DbHelper.Execute(
                    "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                    DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                    DbHelper.P("@ref", saleID), DbHelper.P("@n", "فاتورة بيع " + code),
                    DbHelper.P("@by", Session.EmpID));
            }

            // نقدي: أضف للخزنة
            if (typeStr == "Cash")
            {
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy) VALUES('SaleIncome',@amt,@ref,@n,@by)",
                    DbHelper.P("@amt", total), DbHelper.P("@ref", saleID),
                    DbHelper.P("@n", "بيع نقدي " + code), DbHelper.P("@by", Session.EmpID));
            }

            // تحميل مندوب: أنشئ سجل حمولة
            if (typeStr == "DriverLoad" && driverID.HasValue)
            {
                int loadID = DbHelper.ExecuteInsert(
                    "INSERT INTO DriverLoads(LoadDate,DriverID,SaleID,IsClosed) VALUES(@dt,@did,@sid,0)",
                    DbHelper.P("@dt", DateTime.Now), DbHelper.P("@did", driverID.Value), DbHelper.P("@sid", saleID));

                foreach (var item in items)
                {
                    DbHelper.Execute(
                        "INSERT INTO DriverLoadItems(LoadID,ProductID,LoadedQty,UnitPrice) VALUES(@lid,@pid,@qty,@up)",
                        DbHelper.P("@lid", loadID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice));
                }
            }

            return saleID;
        }

        public static bool CanDeleteSale(int saleID, out string reason)
        {
            reason = "";

            // 1. تحقق من وجود مرتجعات
            var returnsCount = DbHelper.Scalar("SELECT COUNT(*) FROM SalesReturns WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (returnsCount != null && Convert.ToInt32(returnsCount) > 0)
            {
                reason = "لا يمكن حذف الفاتورة لوجود مرتجع مبيعات مرتبط بها.";
                return false;
            }

            // 2. تحقق من وجود حمولة مندوب مغلقة
            var loadData = DbHelper.Query("SELECT LoadID, IsClosed FROM DriverLoads WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (loadData.Rows.Count > 0)
            {
                bool isClosed = Convert.ToBoolean(loadData.Rows[0]["IsClosed"]);
                if (isClosed)
                {
                    reason = "لا يمكن حذف الفاتورة لأنها حمولة مندوب مغلقة (تم تقفيلها وتسليم العهدة بالفعل).";
                    return false;
                }
            }

            return true;
        }

        public static bool DeleteSale(int saleID)
        {
            // 1. استرجاع تفاصيل الفاتورة لمعرفة نوعها
            var dt = DbHelper.Query("SELECT SaleType FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (dt.Rows.Count == 0) return false;

            string typeStr = dt.Rows[0]["SaleType"].ToString();

            // 2. عكس حركات حساب العميل إذا كان بيع آجل
            if (typeStr == "Credit")
            {
                DbHelper.Execute("DELETE FROM ClientTransactions WHERE TransType='Sale' AND RefID=@id", DbHelper.P("@id", saleID));
            }

            // 3. عكس حركات الخزينة إذا كان بيع نقدي
            if (typeStr == "Cash")
            {
                DbHelper.Execute("DELETE FROM CashBox WHERE TransType='SaleIncome' AND RefID=@id", DbHelper.P("@id", saleID));
            }

            // 4. حذف حمولات المناديب غير المغلقة المرتبطة بالفاتورة
            if (typeStr == "DriverLoad")
            {
                var loadData = DbHelper.Query("SELECT LoadID FROM DriverLoads WHERE SaleID=@id", DbHelper.P("@id", saleID));
                if (loadData.Rows.Count > 0)
                {
                    int loadID = Convert.ToInt32(loadData.Rows[0]["LoadID"]);
                    DbHelper.Execute("DELETE FROM DriverLoadItems WHERE LoadID=@lid", DbHelper.P("@lid", loadID));
                    DbHelper.Execute("DELETE FROM DriverLoads WHERE LoadID=@lid", DbHelper.P("@lid", loadID));
                }
            }

            // 5. حذف الفاتورة نفسها (سوف تحذف الأصناف تلقائياً بسبب CASCADE)
            int rows = DbHelper.Execute("DELETE FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
            return rows > 0;
        }
    }


    public class SaleItemDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal StockQty { get; set; } = 0m;
        /// <summary>نسبة الخصم % على الصنف</summary>
        public decimal DiscountPct { get; set; } = 0m;
        /// <summary>قيمة الخصم بالجنيه على الصنف</summary>
        public decimal DiscountAmt
        {
            get
            {
                if (DiscountPct > 0)
                    return Math.Round(Quantity * UnitPrice * DiscountPct / 100m, 2);
                return 0m;
            }
            set
            {
                // عند الادخال اليدوي نحفظ القيمة مباشرة
                _discountAmtOverride = value;
                _discountPctOverride = false;
            }
        }
        private decimal _discountAmtOverride = 0m;
        private bool _discountPctOverride = true;
        public decimal TotalPrice
        {
            get
            {
                decimal gross = Quantity * UnitPrice;
                if (_discountPctOverride)
                    return Math.Round(gross - (gross * DiscountPct / 100m), 2);
                else
                    return Math.Round(gross - _discountAmtOverride, 2);
            }
        }
    }

    public static class DriverDAL
    {
        public static DataTable GetOpenLoads(int? driverID = null, DateTime? from = null, DateTime? to = null)
        {
            string filter = driverID.HasValue ? "AND dl.DriverID=@did" : "";
            var pList = new List<SqlParameter>();
            if (driverID.HasValue) pList.Add(DbHelper.P("@did", driverID.Value));
            
            if (from.HasValue && to.HasValue)
            {
                filter += " AND CAST(dl.LoadDate AS DATE) BETWEEN @f AND @t";
                pList.Add(DbHelper.P("@f", from.Value.Date));
                pList.Add(DbHelper.P("@t", to.Value.Date));
            }

            return DbHelper.Query(
                @"SELECT dl.LoadID, dl.LoadDate, e.EmpName AS DriverName,
                         s.SaleCode, s.TotalAmount
                  FROM DriverLoads dl
                  JOIN Employees e ON dl.DriverID = e.EmpID
                  JOIN Sales s ON dl.SaleID = s.SaleID
                  WHERE dl.IsClosed=0 " + filter + " ORDER BY dl.LoadDate DESC",
                pList.ToArray());
        }

        public static DataTable GetLoadItems(int loadID)
        {
            return DbHelper.Query(
                @"SELECT dli.LoadItemID, p.ProductID, p.ProductName, p.Unit,
                         dli.LoadedQty, dli.UnitPrice,
                         ISNULL((
                             SELECT SUM(si.Quantity)
                             FROM SaleItems si
                             JOIN Sales s ON si.SaleID = s.SaleID
                             JOIN DriverLoads dl ON dl.LoadID = @lid
                             WHERE s.DriverID = dl.DriverID
                               AND s.SaleType IN ('Cash', 'Credit')
                               AND s.SaleDate >= dl.LoadDate
                               AND si.ProductID = p.ProductID
                         ), 0) AS SoldQty
                  FROM DriverLoadItems dli
                  JOIN Products p ON dli.ProductID=p.ProductID
                  WHERE dli.LoadID=@lid",
                DbHelper.P("@lid", loadID));
        }

        public static int SaveHandover(int loadID, int driverID,
            List<HandoverItemDTO> items, string notes, decimal cashCollected)
        {
            decimal totLoaded = 0, totRet = 0, totDead = 0, totExtra = 0, totDef = 0;
            decimal totalSoldValue = 0;

            foreach (var i in items)
            {
                totLoaded += i.LoadedQty;
                totRet += i.ReturnedQty;
                totDead += i.DeadQty;
                totExtra += i.ExtraQty;
                totDef += i.DeficitQty;
                totalSoldValue += (i.SoldQty * i.UnitPrice);
            }

            // 1. تسجيل التقفيل
            int hvID = DbHelper.ExecuteInsert(
                @"INSERT INTO DriverHandovers(HandoverDate,LoadID,DriverID,TotalLoaded,TotalReturned,TotalDead,TotalExtra,TotalDeficit,Notes,CreatedBy)
                  VALUES(@dt,@lid,@did,@tl,@tr,@td,@te,@tdf,@n,@by)",
                DbHelper.P("@dt", DateTime.Now), DbHelper.P("@lid", loadID), DbHelper.P("@did", driverID),
                DbHelper.P("@tl", totLoaded), DbHelper.P("@tr", totRet), DbHelper.P("@td", totDead),
                DbHelper.P("@te", totExtra), DbHelper.P("@tdf", totDef),
                DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

            foreach (var i in items)
            {
                DbHelper.Execute(
                    @"INSERT INTO HandoverItems(HandoverID,ProductID,LoadedQty,ReturnedQty,DeadQty,ExtraQty,DeficitQty)
                      VALUES(@hid,@pid,@lq,@rq,@dq,@eq,@dfq)",
                    DbHelper.P("@hid", hvID), DbHelper.P("@pid", i.ProductID),
                    DbHelper.P("@lq", i.LoadedQty), DbHelper.P("@rq", i.ReturnedQty),
                    DbHelper.P("@dq", i.DeadQty), DbHelper.P("@eq", i.ExtraQty),
                    DbHelper.P("@dfq", i.DeficitQty));
            }

            // إغلاق الحمولة
            DbHelper.Execute("UPDATE DriverLoads SET IsClosed=1, ClosedAt=@dt WHERE LoadID=@lid",
                DbHelper.P("@dt", DateTime.Now), DbHelper.P("@lid", loadID));

            // 2. إذا كان هناك كميات مباعة، ننشئ فاتورة مبيعات نقدية مجمعة باسم المندوب
            // حتى تظهر في تقارير المبيعات اليومية، ولكن بدون خصم من المخزن مجدداً (لأننا صلحنا دالة المخزن)
            if (totalSoldValue > 0)
            {
                var saleItemsDto = new List<SaleItemDTO>();
                foreach (var i in items)
                {
                    if (i.SoldQty > 0)
                    {
                        saleItemsDto.Add(new SaleItemDTO
                        {
                            ProductID = i.ProductID,
                            Quantity = i.SoldQty,
                            UnitPrice = i.UnitPrice
                        });
                    }
                }
                
                // استخدام نفس دالة الحفظ، مع إرسال DriverID وعدم إرسال ClientID
                SaleDAL.SaveSale(2, null, driverID, totalSoldValue, "مبيعات مقفلة من حمولة رقم " + loadID, saleItemsDto);
                
                // ولكن SaveSale عندما يتم تمرير 2 (Cash) يضيف totalSoldValue للخزنة
                // في حالتنا نحن نحتاج لإضافة cashCollected فقط للخزنة، وليس totalSoldValue
                // لذا سنحذف القيمة المضافة افتراضيا من SaveSale ونضيف القيمة الفعلية المحصلة
                
                // البحث عن آخر فاتورة بيع نقدي لنفس المندوب للتو
                var lastSaleIdRes = DbHelper.Scalar("SELECT MAX(SaleID) FROM Sales WHERE DriverID=@did AND SaleType='Cash'", DbHelper.P("@did", driverID));
                if (lastSaleIdRes != null && lastSaleIdRes != DBNull.Value)
                {
                    int lastSaleId = Convert.ToInt32(lastSaleIdRes);
                    
                    // حذف القيد الافتراضي من الخزنة
                    DbHelper.Execute("DELETE FROM CashBox WHERE TransType='SaleIncome' AND RefID=@ref", DbHelper.P("@ref", lastSaleId));
                    
                    // إدراج المبلغ الفعلي المحصل
                    if (cashCollected > 0)
                    {
                        DbHelper.Execute(
                            "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy) VALUES('DriverHandover',@amt,@ref,@n,@by)",
                            DbHelper.P("@amt", cashCollected), DbHelper.P("@ref", loadID),
                            DbHelper.P("@n", $"تحصيل تقفيل حمولة ({loadID}) - مبيعات ({totalSoldValue:N2})"), DbHelper.P("@by", Session.EmpID));
                    }
                }
            }
            else if (cashCollected > 0)
            {
                // إذا لم تكن هناك مبيعات (نادرة) ولكن سلم كاش
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy) VALUES('DriverHandover',@amt,@ref,@n,@by)",
                    DbHelper.P("@amt", cashCollected), DbHelper.P("@ref", loadID),
                    DbHelper.P("@n", $"تحصيل تقفيل حمولة ({loadID})"), DbHelper.P("@by", Session.EmpID));
            }

            return hvID;
        }

        public static DataTable GetHandovers(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT dh.HandoverID, dh.HandoverDate, e.EmpName AS DriverName,
                          dh.TotalLoaded, dh.TotalReturned, dh.TotalDead, dh.TotalExtra, dh.TotalDeficit,
                          dh.Notes, creator.EmpName AS CreatedBy
                  FROM DriverHandovers dh
                  JOIN Employees e ON dh.DriverID = e.EmpID
                  LEFT JOIN Employees creator ON dh.CreatedBy = creator.EmpID
                  WHERE CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                  ORDER BY dh.HandoverDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        /// <summary>ملخص عهدة المناديب الحاليين (الحمولات المفتوحة فقط)</summary>
        public static DataTable GetDriversCustodySummary()
        {
            return DbHelper.Query(@"
                SELECT
                    e.EmpName                               AS DriverName,
                    dl.LoadID,
                    dl.LoadDate,
                    s.SaleCode                              AS LoadCode,
                    -- إجمالي الكميات المحملة
                    ISNULL((
                        SELECT SUM(dli.LoadedQty)
                        FROM DriverLoadItems dli
                        WHERE dli.LoadID = dl.LoadID
                    ), 0)                                   AS TotalLoadedQty,
                    -- إجمالي قيمة الحمولة
                    s.TotalAmount                           AS LoadedValue,
                    -- الكميات المباعة منذ تاريخ الحمولة
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s2 ON si.SaleID = s2.SaleID
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType IN ('Cash','Credit')
                          AND s2.SaleDate >= dl.LoadDate
                          AND s2.IsPosted = 1
                    ), 0)                                   AS SoldQty,
                    -- قيمة المبيعات
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType IN ('Cash','Credit')
                          AND s2.SaleDate >= dl.LoadDate
                          AND s2.IsPosted = 1
                    ), 0)                                   AS SoldValue,
                    -- منها نقدي (محصل فعلاً)
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType = 'Cash'
                          AND s2.SaleDate >= dl.LoadDate
                          AND s2.IsPosted = 1
                    ), 0)                                   AS CashCollected,
                    -- منها آجل (غير محصل)
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType = 'Credit'
                          AND s2.SaleDate >= dl.LoadDate
                          AND s2.IsPosted = 1
                    ), 0)                                   AS CreditSold,
                    -- الكميات المرتجعة من العملاء في نفس الفترة
                    ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                        JOIN Sales s2 ON sr.SaleID = s2.SaleID
                        WHERE s2.DriverID = dl.DriverID
                          AND sr.ReturnDate >= dl.LoadDate
                    ), 0)                                   AS ReturnedQty,
                    -- المتبقي بعهدته (محمل - مباع + مرتجع)
                    ISNULL((
                        SELECT SUM(dli.LoadedQty)
                        FROM DriverLoadItems dli WHERE dli.LoadID = dl.LoadID
                    ), 0)
                    - ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si JOIN Sales s2 ON si.SaleID=s2.SaleID
                        WHERE s2.DriverID=dl.DriverID AND s2.SaleType IN('Cash','Credit')
                          AND s2.SaleDate >= dl.LoadDate AND s2.IsPosted=1
                    ), 0)
                    + ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID=sr.ReturnID
                        JOIN Sales s2 ON sr.SaleID=s2.SaleID
                        WHERE s2.DriverID=dl.DriverID AND sr.ReturnDate >= dl.LoadDate
                    ), 0)                                   AS RemainingQty
                FROM DriverLoads dl
                JOIN Employees e ON dl.DriverID = e.EmpID
                JOIN Sales s ON dl.SaleID = s.SaleID
                WHERE dl.IsClosed = 0
                ORDER BY e.EmpName, dl.LoadDate DESC");
        }
    }

    public class HandoverItemDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal LoadedQty { get; set; }
        public decimal SoldQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal DeadQty { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal DeficitQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty - DeadQty;
                return expected > SoldQty ? expected - SoldQty : 0;
            }
        }

        public decimal ExtraQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty - DeadQty;
                return SoldQty > expected ? SoldQty - expected : 0;
            }
        }
    }


    public static class ReturnDAL
    {
        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT sr.ReturnID, sr.ReturnDate, s.SaleCode,
                          ISNULL(c.ClientName,N'---') AS ClientName, sr.TotalAmount, sr.Notes
                  FROM SalesReturns sr
                  LEFT JOIN Sales s ON sr.SaleID=s.SaleID
                  LEFT JOIN Clients c ON sr.ClientID=c.ClientID
                  WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                  ORDER BY sr.ReturnDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static int SaveReturn(int saleID, int? clientID, decimal total, string notes, List<SaleItemDTO> items)
        {
            var dtSale = DbHelper.Query("SELECT SaleType FROM Sales WHERE SaleID=@sid", DbHelper.P("@sid", saleID));
            string saleType = dtSale.Rows.Count > 0 ? dtSale.Rows[0]["SaleType"].ToString() : "Credit";

            int retID = DbHelper.ExecuteInsert(
                "INSERT INTO SalesReturns(ReturnDate,SaleID,ClientID,TotalAmount,Notes,CreatedBy) VALUES(@dt,@sid,@cid,@tot,@n,@by)",
                DbHelper.P("@dt", DateTime.Now),
                DbHelper.P("@sid", saleID > 0 ? (object)saleID : DBNull.Value),
                DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

            foreach (var item in items)
                DbHelper.Execute(
                    "INSERT INTO ReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice) VALUES(@rid,@pid,@qty,@up,@tp)",
                    DbHelper.P("@rid", retID), DbHelper.P("@pid", item.ProductID),
                    DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                    DbHelper.P("@tp", item.TotalPrice));

            if (saleType == "Cash")
            {
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransType,AmountOut,RefID,Notes,CreatedBy) VALUES('ReturnOutcome',@amt,@ref,@n,@by)",
                    DbHelper.P("@amt", total), DbHelper.P("@ref", retID),
                    DbHelper.P("@n", "مرتجع بيع نقدي لفاتورة رقم " + saleID),
                    DbHelper.P("@by", Session.EmpID));
            }
            else if (clientID.HasValue)
            {
                DbHelper.Execute(
                    "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'Return',@amt,@ref,@n,@by)",
                    DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                    DbHelper.P("@ref", retID), DbHelper.P("@n", "مرتجع بيع لفاتورة رقم " + saleID),
                    DbHelper.P("@by", Session.EmpID));
            }

            return retID;
        }
    }

    public static class ReportDAL
    {
        public static DataTable SalesByDay(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT 
                    CAST(SaleDate AS DATE) AS SaleDay,
                    COUNT(*) AS Count,
                    SUM(CASE WHEN SaleType = 'Cash' THEN TotalAmount ELSE 0 END) AS CashTotal,
                    SUM(CASE WHEN SaleType = 'Credit' THEN TotalAmount ELSE 0 END) AS CreditTotal,
                    SUM(CASE WHEN SaleType = 'DriverLoad' THEN TotalAmount ELSE 0 END) AS LoadTotal,
                    SUM(TotalAmount) AS Total
                  FROM Sales
                  WHERE CAST(SaleDate AS DATE) BETWEEN @f AND @t AND IsPosted=1
                  GROUP BY CAST(SaleDate AS DATE)
                  ORDER BY SaleDay",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable SalesByDriver(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT 
                    ISNULL(e.EmpName, N'مبيعات مباشرة') AS DriverName,
                    COUNT(s.SaleID) AS Count,
                    SUM(CASE WHEN s.SaleType = 'Cash' THEN s.TotalAmount ELSE 0 END) AS CashTotal,
                    SUM(CASE WHEN s.SaleType = 'Credit' THEN s.TotalAmount ELSE 0 END) AS CreditTotal,
                    SUM(s.TotalAmount) AS Total
                  FROM Sales s 
                  LEFT JOIN Employees e ON s.DriverID = e.EmpID
                  WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t AND s.IsPosted=1
                  GROUP BY e.EmpName
                  ORDER BY Total DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable SalesByClient(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT 
                    c.ClientName,
                    ISNULL(c.Phone, N'---') AS Phone,
                    COUNT(DISTINCT s.SaleID) AS Count,
                    SUM(CASE WHEN s.SaleType = 'Cash' THEN s.TotalAmount ELSE 0 END) AS CashTotal,
                    SUM(CASE WHEN s.SaleType = 'Credit' THEN s.TotalAmount ELSE 0 END) AS CreditTotal,
                    SUM(s.TotalAmount) AS Total,
                    ISNULL((SELECT SUM(sr.TotalAmount) FROM SalesReturns sr WHERE sr.ClientID = c.ClientID AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t), 0) AS ReturnsTotal,
                    ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID AND ct.TransType = 'Payment' AND CAST(ct.TransDate AS DATE) BETWEEN @f AND @t), 0) AS PaidTotal,
                    (c.OpeningBalance + 
                     ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                     ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)
                    ) AS CurrentBalance
                  FROM Sales s 
                  JOIN Clients c ON s.ClientID = c.ClientID
                  WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t AND s.IsPosted=1
                  GROUP BY c.ClientID, c.ClientName, c.Phone, c.OpeningBalance
                  ORDER BY Total DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable SalesByProduct(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT 
                    p.ProductName,
                    p.Unit,
                    AVG(si.UnitPrice) AS AvgPrice,
                    SUM(si.Quantity) AS TotalQty,
                    SUM(si.TotalPrice) AS TotalAmount,
                    ISNULL((
                        SELECT SUM(ri.Quantity) 
                        FROM ReturnItems ri 
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                        WHERE ri.ProductID = p.ProductID AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS ReturnedQty,
                    ISNULL((
                        SELECT SUM(ri.TotalPrice) 
                        FROM ReturnItems ri 
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                        WHERE ri.ProductID = p.ProductID AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS ReturnedAmount,
                    -- Net outcomes
                    (SUM(si.Quantity) - ISNULL((
                        SELECT SUM(ri.Quantity) 
                        FROM ReturnItems ri 
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                        WHERE ri.ProductID = p.ProductID AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0)) AS NetQty,
                    (SUM(si.TotalPrice) - ISNULL((
                        SELECT SUM(ri.TotalPrice) 
                        FROM ReturnItems ri 
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                        WHERE ri.ProductID = p.ProductID AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0)) AS NetAmount
                  FROM SaleItems si
                  JOIN Sales s ON si.SaleID=s.SaleID
                  JOIN Products p ON si.ProductID=p.ProductID
                  WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t AND s.IsPosted=1
                  GROUP BY p.ProductID, p.ProductName, p.Unit
                  ORDER BY TotalQty DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        /// <summary>كميات مبيعات كل عميل لكل صنف في يوم معين (للتقرير اليومي المحوري)</summary>
        public static DataTable GetDailyClientProductSales(DateTime date)
        {
            return DbHelper.Query(
                @"SELECT
                    c.ClientID,
                    c.ClientName,
                    si.ProductID,
                    SUM(si.Quantity) AS TotalQty,
                    MAX(si.UnitPrice) AS UnitPrice
                  FROM SaleItems si
                  JOIN Sales s   ON si.SaleID  = s.SaleID
                  JOIN Clients c ON s.ClientID = c.ClientID
                  WHERE CAST(s.SaleDate AS DATE) = @date
                    AND s.IsPosted = 1
                    AND s.SaleType IN ('Cash','Credit')
                  GROUP BY c.ClientID, c.ClientName, si.ProductID
                  ORDER BY c.ClientName",
                DbHelper.P("@date", date.Date));
        }

        /// <summary>إجمالي الفاتورة وآخر توريد والمديونية لكل عميل في يوم معين</summary>
        public static DataTable GetDailyClientTotals(DateTime date)
        {
            return DbHelper.Query(
                @"SELECT
                    c.ClientID,
                    c.ClientName,
                    SUM(s.TotalAmount) AS TotalInvoice,
                    ISNULL((
                        SELECT TOP 1 ct.Credit
                        FROM ClientTransactions ct
                        WHERE ct.ClientID = c.ClientID
                          AND ct.TransType = 'Payment'
                        ORDER BY ct.TransDate DESC
                    ), 0) AS LastPayment,
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                  FROM Sales s
                  JOIN Clients c ON s.ClientID = c.ClientID
                  LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                  WHERE CAST(s.SaleDate AS DATE) = @date
                    AND s.IsPosted = 1
                    AND s.SaleType IN ('Cash','Credit')
                  GROUP BY c.ClientID, c.ClientName, c.OpeningBalance, cb.Balance
                  ORDER BY c.ClientName",
                DbHelper.P("@date", date.Date));
        }

        public static DataTable GetFinancialSummary(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT 
                    -- Total Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND CAST(SaleDate AS DATE) BETWEEN @f AND @t), 0) AS TotalSales,
                    -- Cash Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType='Cash' AND CAST(SaleDate AS DATE) BETWEEN @f AND @t), 0) AS CashSales,
                    -- Credit Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType='Credit' AND CAST(SaleDate AS DATE) BETWEEN @f AND @t), 0) AS CreditSales,
                    -- Driver Loads Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType='DriverLoad' AND CAST(SaleDate AS DATE) BETWEEN @f AND @t), 0) AS DriverLoadsSales,
                    -- Returns
                    ISNULL((SELECT SUM(TotalAmount) FROM SalesReturns WHERE CAST(ReturnDate AS DATE) BETWEEN @f AND @t), 0) AS TotalReturns,
                    -- Client Payments
                    ISNULL((SELECT SUM(Credit) FROM ClientTransactions WHERE TransType='Payment' AND CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS ClientPayments,
                    -- Expenses
                    ISNULL((SELECT SUM(Amount) FROM Expenses WHERE CAST(ExpenseDate AS DATE) BETWEEN @f AND @t), 0) AS TotalExpenses,
                    -- Cashbox Inflow (Cash Sales + Payments)
                    ISNULL((SELECT SUM(AmountIn) FROM CashBox WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS CashInflow,
                    -- Cashbox Outflow (Expenses + Handover returned or other outflows)
                    ISNULL((SELECT SUM(AmountOut) FROM CashBox WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS CashOutflow",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static DataTable ClientsReport()
        {
            return DbHelper.Query(@"
                SELECT 
                    c.ClientCode,
                    c.ClientName,
                    ISNULL(c.Phone, N'---') AS Phone,
                    ISNULL(c.Phone2, N'---') AS Phone2,
                    ISNULL(c.Address, N'---') AS Address,
                    ISNULL(e.EmpName, N'---') AS DriverName,
                    ISNULL(c.MaxCreditLimit, 0) AS MaxCreditLimit,
                    c.OpeningBalance,
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                    ISNULL(c.Notes, N'---') AS Notes
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                LEFT JOIN Employees e ON c.DriverID = e.EmpID
                ORDER BY c.ClientName");
        }

        /// <summary>تقرير كميات الأصناف التفصيلي للفترة المحددة</summary>
        public static DataTable GetProductQtyDetail(DateTime from, DateTime to)
        {
            return DbHelper.Query(@"
                SELECT
                    p.ProductCode                                       AS ProductCode,
                    p.ProductName                                       AS ProductName,
                    p.Unit                                              AS Unit,
                    p.SalePrice                                         AS SalePrice,

                    -- رصيد آخر تسوية جردية قبل / خلال الفترة (أو 0 إن لم توجد)
                    ISNULL((
                        SELECT TOP 1 sa.ActualQty
                        FROM StockAdjustments sa
                        WHERE sa.ProductID = p.ProductID
                          AND sa.AdjDate <= DATEADD(DAY, 1, @t)
                        ORDER BY sa.AdjDate DESC
                    ), 0)                                               AS LastAdjQty,

                    -- إجمالي كميات المبيعات (جميع الأنواع) في الفترة
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS SoldQty,

                    -- كمية مبيعات نقدية
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.SaleType = 'Cash' AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS CashQty,

                    -- كمية مبيعات آجلة
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.SaleType = 'Credit' AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS CreditQty,

                    -- كمية حمولات المناديب
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.SaleType = 'DriverLoad' AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS DriverLoadQty,

                    -- مرتجعات المبيعات في الفترة
                    ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                        WHERE ri.ProductID = p.ProductID
                          AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS ReturnedQty,

                    -- مرتجعات حمولات المناديب في الفترة
                    ISNULL((
                        SELECT SUM(hi.ReturnedQty)
                        FROM HandoverItems hi
                        JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                        WHERE hi.ProductID = p.ProductID
                          AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                          AND hi.ReturnedQty > 0
                    ), 0)                                               AS DriverReturnQty,

                    -- صافي الكميات المباعة (مبيعات - مرتجعات مبيعات - مرتجعات مناديب)
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si JOIN Sales s ON si.SaleID=s.SaleID
                        WHERE si.ProductID=p.ProductID AND s.IsPosted=1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)
                    - ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID=sr.ReturnID
                        WHERE ri.ProductID=p.ProductID
                          AND CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    ), 0)
                    - ISNULL((
                        SELECT SUM(hi.ReturnedQty)
                        FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID=dh.HandoverID
                        WHERE hi.ProductID=p.ProductID
                          AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                          AND hi.ReturnedQty > 0
                    ), 0)                                               AS NetSoldQty,

                    -- إجمالي قيمة المبيعات في الفترة
                    ISNULL((
                        SELECT SUM(si.TotalPrice)
                        FROM SaleItems si JOIN Sales s ON si.SaleID=s.SaleID
                        WHERE si.ProductID=p.ProductID AND s.IsPosted=1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0)                                               AS TotalSalesAmt,

                    -- الرصيد الكتابي الحالي
                    ISNULL(adj.ActualQty, 0)
                    + ISNULL((SELECT SUM(ri.Quantity) FROM ReturnItems ri
                               JOIN SalesReturns sr ON ri.ReturnID=sr.ReturnID
                               WHERE ri.ProductID=p.ProductID
                                 AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0)
                    + ISNULL((SELECT SUM(hi.ReturnedQty) FROM HandoverItems hi
                               JOIN DriverHandovers dh ON hi.HandoverID=dh.HandoverID
                               WHERE hi.ProductID=p.ProductID
                                 AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0)
                    - ISNULL((SELECT SUM(si.Quantity) FROM SaleItems si
                               JOIN Sales s ON si.SaleID=s.SaleID
                               WHERE si.ProductID=p.ProductID
                                 AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0)
                                                                        AS CurrentStock

                FROM Products p
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty
                    FROM StockAdjustments sa
                    WHERE sa.ProductID = p.ProductID
                    ORDER BY sa.AdjDate DESC
                ) adj
                WHERE p.IsActive = 1
                ORDER BY p.ProductName",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }
    }
}

