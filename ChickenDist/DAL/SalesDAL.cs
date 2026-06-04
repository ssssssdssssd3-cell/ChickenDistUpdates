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
            return GetAll(from, to, null, null);
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? clientID, string productSearch)
        {
            string productFilter = string.IsNullOrWhiteSpace(productSearch) ? null : productSearch.Trim();
            return DbHelper.Query(
                @"SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType,
                         ISNULL(c.ClientName,N'---') AS ClientName,
                         ISNULL(e.EmpName,N'---') AS DriverName,
                         s.TotalAmount, s.Notes,
                         ISNULL((
                             SELECT SUM(ri.Quantity * ri.UnitPrice)
                             FROM SalesReturns r
                             JOIN ReturnItems ri ON r.ReturnID = ri.ReturnID
                             WHERE r.SaleID = s.SaleID
                         ), 0) AS ReturnAmount
                  FROM Sales s
                  LEFT JOIN Clients c ON s.ClientID = c.ClientID
                  LEFT JOIN Employees e ON s.DriverID = e.EmpID
                  WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    AND (@clientID IS NULL OR s.ClientID = @clientID)
                    AND (@product IS NULL OR EXISTS (
                        SELECT 1 FROM SaleItems si2
                        JOIN Products pr ON si2.ProductID = pr.ProductID
                        WHERE si2.SaleID = s.SaleID
                          AND (pr.ProductName LIKE N'%' + @product + N'%'
                            OR pr.ProductCode LIKE N'%' + @product + N'%')
                    ))
                  ORDER BY s.SaleDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@clientID", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                DbHelper.P("@product", (object)productFilter ?? DBNull.Value));
        }

        public static DataTable GetItems(int saleID)
        {
            return DbHelper.Query(
                @"SELECT si.ItemID, si.ProductID, p.ProductName, si.Quantity, si.UnitPrice, si.TotalPrice,
                          COALESCE(si.DiscountPct, 0) AS DiscountPct, COALESCE(si.DiscountAmt, 0) AS DiscountAmt,
                          COALESCE(si.PriceTier, N'قطاعي') AS PriceTier
                  FROM SaleItems si JOIN Products p ON si.ProductID=p.ProductID
                  WHERE si.SaleID=@id",
                DbHelper.P("@id", saleID));
        }

        public static int SaveSale(int saleType, int? clientID, int? driverID, decimal total, string notes,
            List<SaleItemDTO> items, decimal discountAmount = 0m, decimal discountPct = 0m, bool isDraft = false, int? warehouseID = null, string priceTier = "قطاعي")
        {
            int returnedSaleID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                string typeStr = saleType == 0 ? "Credit" : saleType == 1 ? "DriverLoad" : "Cash";
                var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                string code = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                int targetWarehouse = warehouseID ?? 1;

                int saleID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO Sales(SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,Notes,CreatedBy,DiscountAmount,DiscountPct,IsPosted,WarehouseID,PriceTier,LastModifiedDate) VALUES(@code,@dt,@typ,@cid,@did,@tot,@n,@by,@discAmt,@discPct,@ip,@wid,@pt,GETDATE())",
                    DbHelper.P("@code", code), DbHelper.P("@dt", DateTime.Now), DbHelper.P("@typ", typeStr),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@did", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@discAmt", discountAmount), DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@ip", !isDraft), DbHelper.P("@wid", targetWarehouse), DbHelper.P("@pt", priceTier));

                if (saleID <= 0) throw new Exception("فشل في استخراج رقم الفاتورة الجديد.");
                returnedSaleID = saleID;

                // تسجيل حركة إنشاء جديدة في الـ Audit
                DbHelper.ExecuteTrans(trans,
                    @"INSERT INTO SalesAudit(SaleID, UserID, EditDate, OldTotal, NewTotal, Notes, MachineName, ActionType) 
                      VALUES(@sid, @uid, GETDATE(), 0, @newTot, @auditNotes, @mach, 'CREATE')",
                    DbHelper.P("@sid", saleID),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@newTot", total),
                    DbHelper.P("@auditNotes", string.IsNullOrEmpty(notes) ? "إنشاء فاتورة جديدة" : "إنشاء: " + notes),
                    DbHelper.P("@mach", Environment.MachineName));

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier) VALUES(@sid,@pid,@qty,@up,@tp,@dpct,@damt,@pt)",
                        DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice), DbHelper.P("@dpct", item.DiscountPct),
                        DbHelper.P("@damt", item.DiscountAmt), DbHelper.P("@pt", item.PriceTier ?? priceTier));
                }

                if (!isDraft)
                {
                    // آجل: أضف للحساب
                    if (typeStr == "Credit" && clientID.HasValue)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "فاتورة بيع " + code),
                            DbHelper.P("@by", Session.EmpID));
                    }

                    // نقدي: أضف للخزنة
                    if (typeStr == "Cash")
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy) VALUES('SaleIncome',@amt,@ref,@n,@by)",
                            DbHelper.P("@amt", total), DbHelper.P("@ref", saleID),
                            DbHelper.P("@n", "بيع نقدي " + code), DbHelper.P("@by", Session.EmpID));
                    }

                    // تحميل مندوب: أنشئ سجل حمولة
                    if (typeStr == "DriverLoad" && driverID.HasValue)
                    {
                        int loadID = DbHelper.ExecuteInsertTrans(trans,
                            "INSERT INTO DriverLoads(LoadDate,DriverID,SaleID,IsClosed,WarehouseID) VALUES(@dt,@did,@sid,0,@wid)",
                            DbHelper.P("@dt", DateTime.Now), DbHelper.P("@did", driverID.Value), DbHelper.P("@sid", saleID),
                            DbHelper.P("@wid", targetWarehouse));

                        foreach (var item in items)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO DriverLoadItems(LoadID,ProductID,LoadedQty,UnitPrice) VALUES(@lid,@pid,@qty,@up)",
                                DbHelper.P("@lid", loadID), DbHelper.P("@pid", item.ProductID),
                                DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice));
                        }
                    }
                }
            });

            if (returnedSaleID > 0 && !isDraft)
            {
                foreach (var item in items)
                {
                    try
                    {
                        ProductDAL.CheckAndActivatePendingPrice(item.ProductID);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("CheckAndActivatePendingPrice failed inside SalesDAL.SaveSale", ex, $"ProductID: {item.ProductID}");
                    }
                }
            }

            return returnedSaleID;
        }

        public static DataTable GetDraftSales()
        {
            return DbHelper.Query(
                @"SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType,
                         ISNULL(c.ClientName,N'---') AS ClientName,
                         ISNULL(e.EmpName,N'---') AS DriverName,
                         s.TotalAmount, s.Notes, s.ClientID, s.DriverID,
                         COALESCE(s.DiscountAmount, 0) AS DiscountAmount,
                         COALESCE(s.DiscountPct, 0) AS DiscountPct
                  FROM Sales s
                  LEFT JOIN Clients c ON s.ClientID = c.ClientID
                  LEFT JOIN Employees e ON s.DriverID = e.EmpID
                  WHERE s.IsPosted = 0
                  ORDER BY s.SaleDate DESC");
        }

        public static bool DeleteDraftSale(int saleID)
        {
            try
            {
                DbHelper.Execute("DELETE FROM Sales WHERE SaleID=@id AND IsPosted=0", DbHelper.P("@id", saleID));
                AppLogger.Audit("حذف مسودة فاتورة", $"SaleID: {saleID}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"فشل حذف مسودة الفاتورة رقم {saleID} — الفاتورة قد تظل معلقة في قاعدة البيانات", ex, "SaleDAL.DeleteDraftSale");
                System.Windows.Forms.MessageBox.Show(
                    $"فشل حذف المسودة:\n{ex.Message}\nيرجى مراجعة سجل الأخطاء.",
                    "خطأ في الحذف", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
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
            // FIX: كل عمليات الحذف داخل Transaction واحد لضمان الاتساق
            // إذا فشلت أي خطوة يتم التراجع عن الكل تلقائياً
            bool success = false;

            // 1. استرجاع تفاصيل الفاتورة قبل بدء الـ Transaction
            var dt = DbHelper.Query("SELECT SaleType, TotalAmount, Notes FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (dt.Rows.Count == 0) return false;

            string typeStr = dt.Rows[0]["SaleType"].ToString();
            decimal oldTotal = Convert.ToDecimal(dt.Rows[0]["TotalAmount"]);
            string oldNotes = dt.Rows[0]["Notes"].ToString();

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 2. إدخال سجل الحذف في SalesAudit
                int auditID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO SalesAudit(SaleID, UserID, EditDate, OldTotal, NewTotal, Notes, MachineName, ActionType) 
                      VALUES(@sid, @uid, GETDATE(), @oldTot, 0, @auditNotes, @mach, 'DELETE')",
                    DbHelper.P("@sid", saleID),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@oldTot", oldTotal),
                    DbHelper.P("@auditNotes", "حذف الفاتورة: " + oldNotes),
                    DbHelper.P("@mach", Environment.MachineName));

                if (auditID <= 0) throw new Exception("فشل في إنشاء سجل أرشفة الحذف.");

                // 3. نسخ البنود المحذوفة إلى SaleItemsHistory
                var dtOldItems = DbHelper.QueryTrans(trans, "SELECT ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, PriceTier FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", saleID));
                foreach (DataRow r in dtOldItems.Rows)
                 {
                     DbHelper.ExecuteTrans(trans,
                         @"INSERT INTO SaleItemsHistory(AuditID, SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, PriceTier)
                           VALUES(@aid, @sid, @pid, @qty, @up, @tp, @dpct, @damt, @pt)",
                         DbHelper.P("@aid", auditID),
                         DbHelper.P("@sid", saleID),
                         DbHelper.P("@pid", r["ProductID"]),
                         DbHelper.P("@qty", r["Quantity"]),
                         DbHelper.P("@up", r["UnitPrice"]),
                         DbHelper.P("@tp", r["TotalPrice"]),
                         DbHelper.P("@dpct", r["DiscountPct"]),
                         DbHelper.P("@damt", r["DiscountAmt"]),
                         DbHelper.P("@pt", r["PriceTier"] == DBNull.Value ? "قطاعي" : r["PriceTier"]));
                 }

                // 4. عكس حركات حساب العميل إذا كان بيع آجل
                if (typeStr == "Credit")
                {
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM ClientTransactions WHERE TransType='Sale' AND RefID=@id",
                        DbHelper.P("@id", saleID));
                }

                // 5. عكس حركات الخزينة إذا كان بيع نقدي
                if (typeStr == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM CashBox WHERE TransType='SaleIncome' AND RefID=@id",
                        DbHelper.P("@id", saleID));
                }

                // 6. حذف حمولات المناديب غير المغلقة المرتبطة بالفاتورة
                if (typeStr == "DriverLoad")
                {
                    var loadData = DbHelper.QueryTrans(trans,
                        "SELECT LoadID FROM DriverLoads WHERE SaleID=@id",
                        DbHelper.P("@id", saleID));
                    if (loadData.Rows.Count > 0)
                    {
                        int loadID = Convert.ToInt32(loadData.Rows[0]["LoadID"]);
                        DbHelper.ExecuteTrans(trans,
                            "DELETE FROM DriverLoadItems WHERE LoadID=@lid",
                            DbHelper.P("@lid", loadID));
                        DbHelper.ExecuteTrans(trans,
                            "DELETE FROM DriverLoads WHERE LoadID=@lid",
                            DbHelper.P("@lid", loadID));
                    }
                }

                // 7. حذف الفاتورة نفسها (سوف تحذف الأصناف تلقائياً بسبب CASCADE)
                int rows = DbHelper.ExecuteTrans(trans,
                    "DELETE FROM Sales WHERE SaleID=@id",
                    DbHelper.P("@id", saleID));

                if (rows <= 0)
                    throw new Exception($"لم يتم حذف الفاتورة رقم {saleID} — قد تكون محذوفة مسبقاً.");

                success = true;
            });

            return success;
        }

        public static bool CanEditSale(int saleID, out string reason)
        {
            reason = "";

            // 1. تحقق من وجود مرتجعات
            var returnsCount = DbHelper.Scalar("SELECT COUNT(*) FROM SalesReturns WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (returnsCount != null && Convert.ToInt32(returnsCount) > 0)
            {
                reason = "لا يمكن تعديل الفاتورة لوجود مرتجع مبيعات مرتبط بها.";
                return false;
            }

            // 2. تحقق من وجود حمولة مندوب مغلقة
            var loadData = DbHelper.Query("SELECT LoadID, IsClosed FROM DriverLoads WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (loadData.Rows.Count > 0)
            {
                bool isClosed = Convert.ToBoolean(loadData.Rows[0]["IsClosed"]);
                if (isClosed)
                {
                    reason = "لا يمكن تعديل الفاتورة لأنها حمولة مندوب مغلقة (تم تقفيلها وتسليم العهدة بالفعل).";
                    return false;
                }
            }

            return true;
        }

        public static bool UpdateSale(int saleID, int saleType, int? clientID, int? driverID, decimal total, string notes,
            List<SaleItemDTO> items, decimal discountAmount = 0m, decimal discountPct = 0m, bool isDraft = false, int? warehouseID = null, string priceTier = "قطاعي",
            DateTime? loadedLastModified = null)
        {
            bool success = false;

            // 1. جلب البيانات القديمة لأرشفة الفاتورة والتحقق من التعديل المتزامن
            var dtOldSale = DbHelper.Query("SELECT TotalAmount, Notes, SaleType, LastModifiedDate FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
            if (dtOldSale.Rows.Count == 0) return false;

            // التحقق من التعديل المتزامن (Concurrency Check)
            if (loadedLastModified.HasValue && dtOldSale.Rows[0]["LastModifiedDate"] != DBNull.Value)
            {
                DateTime dbLastModified = Convert.ToDateTime(dtOldSale.Rows[0]["LastModifiedDate"]);
                if (Math.Abs((dbLastModified - loadedLastModified.Value).TotalSeconds) > 1.5) // سماحية 1.5 ثانية للفروق البسيطة
                {
                    throw new Exception("CONCURRENCY_ERROR: تم تعديل هذه الفاتورة بواسطة مستخدم آخر أثناء قيامك بالعمل عليها. يرجى إلغاء العملية وإعادة فتح الفاتورة للحصول على أحدث البيانات.");
                }
            }

            decimal oldTotal = Convert.ToDecimal(dtOldSale.Rows[0]["TotalAmount"]);
            string oldTypeStr = dtOldSale.Rows[0]["SaleType"].ToString();

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 2. إدخال سجل التعديل في SalesAudit
                int auditID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO SalesAudit(SaleID, UserID, EditDate, OldTotal, NewTotal, Notes, MachineName, ActionType) 
                      VALUES(@sid, @uid, GETDATE(), @oldTot, @newTot, @notes, @mach, 'EDIT')",
                    DbHelper.P("@sid", saleID),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@oldTot", oldTotal),
                    DbHelper.P("@newTot", total),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@mach", Environment.MachineName));

                if (auditID <= 0) throw new Exception("فشل في إنشاء سجل أرشفة التعديل.");

                // 3. نسخ البنود القديمة إلى SaleItemsHistory
                var dtOldItems = DbHelper.QueryTrans(trans, "SELECT ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, PriceTier FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", saleID));
                foreach (DataRow r in dtOldItems.Rows)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO SaleItemsHistory(AuditID, SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, PriceTier)
                          VALUES(@aid, @sid, @pid, @qty, @up, @tp, @dpct, @damt, @pt)",
                        DbHelper.P("@aid", auditID),
                        DbHelper.P("@sid", saleID),
                        DbHelper.P("@pid", r["ProductID"]),
                        DbHelper.P("@qty", r["Quantity"]),
                        DbHelper.P("@up", r["UnitPrice"]),
                        DbHelper.P("@tp", r["TotalPrice"]),
                        DbHelper.P("@dpct", r["DiscountPct"]),
                        DbHelper.P("@damt", r["DiscountAmt"]),
                        DbHelper.P("@pt", r["PriceTier"] == DBNull.Value ? "قطاعي" : r["PriceTier"]));
                }

                // 4. عكس الحركات المالية السابقة (العملاء، الخزينة، المندوب)
                if (oldTypeStr == "Credit")
                {
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM ClientTransactions WHERE TransType='Sale' AND RefID=@id",
                        DbHelper.P("@id", saleID));
                }
                else if (oldTypeStr == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM CashBox WHERE TransType='SaleIncome' AND RefID=@id",
                        DbHelper.P("@id", saleID));
                }
                else if (oldTypeStr == "DriverLoad")
                {
                    var loadData = DbHelper.QueryTrans(trans, "SELECT LoadID FROM DriverLoads WHERE SaleID=@id", DbHelper.P("@id", saleID));
                    if (loadData.Rows.Count > 0)
                    {
                        int loadID = Convert.ToInt32(loadData.Rows[0]["LoadID"]);
                        DbHelper.ExecuteTrans(trans, "DELETE FROM DriverLoadItems WHERE LoadID=@lid", DbHelper.P("@lid", loadID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM DriverLoads WHERE LoadID=@lid", DbHelper.P("@lid", loadID));
                    }
                }

                // 5. حذف البنود الحالية
                DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID=@id", DbHelper.P("@id", saleID));

                // 6. تحديث رأس الفاتورة
                string typeStr = saleType == 0 ? "Credit" : saleType == 1 ? "DriverLoad" : "Cash";
                int targetWarehouse = warehouseID ?? 1;

                DbHelper.ExecuteTrans(trans,
                    @"UPDATE Sales 
                      SET SaleType=@typ, ClientID=@cid, DriverID=@did, TotalAmount=@tot, Notes=@n, 
                          DiscountAmount=@discAmt, DiscountPct=@discPct, IsPosted=@ip, WarehouseID=@wid, PriceTier=@pt,
                          LastModifiedDate=GETDATE()
                      WHERE SaleID=@id",
                    DbHelper.P("@typ", typeStr),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@did", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@tot", total),
                    DbHelper.P("@n", notes),
                    DbHelper.P("@discAmt", discountAmount),
                    DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@ip", !isDraft),
                    DbHelper.P("@wid", targetWarehouse),
                    DbHelper.P("@pt", priceTier),
                    DbHelper.P("@id", saleID));

                // 7. إدخال البنود الجديدة
                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier) 
                          VALUES(@sid,@pid,@qty,@up,@tp,@dpct,@damt,@pt)",
                        DbHelper.P("@sid", saleID), 
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), 
                        DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice), 
                        DbHelper.P("@dpct", item.DiscountPct),
                        DbHelper.P("@damt", item.DiscountAmt),
                        DbHelper.P("@pt", item.PriceTier ?? priceTier));
                }

                // 8. إنشاء الحركات المالية الجديدة
                if (!isDraft)
                {
                    string code = saleID.ToString();
                    var dtCode = DbHelper.QueryTrans(trans, "SELECT SaleCode FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
                    if (dtCode.Rows.Count > 0) code = dtCode.Rows[0]["SaleCode"].ToString();

                    if (typeStr == "Credit" && clientID.HasValue)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "تعديل فاتورة بيع " + code),
                            DbHelper.P("@by", Session.EmpID));
                    }
                    else if (typeStr == "Cash")
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy) VALUES('SaleIncome',@amt,@ref,@n,@by)",
                            DbHelper.P("@amt", total), DbHelper.P("@ref", saleID),
                            DbHelper.P("@n", "تعديل بيع نقدي " + code), DbHelper.P("@by", Session.EmpID));
                    }
                    else if (typeStr == "DriverLoad" && driverID.HasValue)
                    {
                        int loadID = DbHelper.ExecuteInsertTrans(trans,
                            "INSERT INTO DriverLoads(LoadDate,DriverID,SaleID,IsClosed,WarehouseID) VALUES(@dt,@did,@sid,0,@wid)",
                            DbHelper.P("@dt", DateTime.Now), DbHelper.P("@did", driverID.Value), DbHelper.P("@sid", saleID),
                            DbHelper.P("@wid", targetWarehouse));

                        foreach (var item in items)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO DriverLoadItems(LoadID,ProductID,LoadedQty,UnitPrice) VALUES(@lid,@pid,@qty,@up)",
                                DbHelper.P("@lid", loadID), DbHelper.P("@pid", item.ProductID),
                                DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice));
                        }
                    }
                }

                success = true;
            });

            return success;
        }
    }


    public class SaleItemDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal StockQty { get; set; } = 0m;
        public decimal MinStockLimit { get; set; } = 0m;
        /// <summary>نسبة الخصم % على الصنف</summary>
        public decimal DiscountPct { get; set; } = 0m;
        /// <summary>قيمة الخصم بالجنيه على الصنف</summary>
        public decimal DiscountAmt
        {
            get
            {
                // FIX: إذا أُدخلت قيمة يدوية (_discountPctOverride=false) نُعيدها مباشرةً
                // حتى لو كانت DiscountPct == 0 — الكود القديم كان يُعيد 0 متجاهلاً _discountAmtOverride
                if (!_discountPctOverride)
                    return _discountAmtOverride;
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
        public string PriceTier { get; set; } = "قطاعي";
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

            // ===== FIX: كل عمليات التقفيل الأساسية في Transaction واحد =====
            // لو حصل أي خطأ في المنتصف يتم التراجع عن كل العمليات تلقائياً
            int hvID = -1;
            DateTime closedAt = DateTime.Now;

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. تسجيل التقفيل
                hvID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO DriverHandovers(HandoverDate,LoadID,DriverID,TotalLoaded,TotalReturned,TotalDead,TotalExtra,TotalDeficit,Notes,CreatedBy)
                      VALUES(@dt,@lid,@did,@tl,@tr,@td,@te,@tdf,@n,@by)",
                    DbHelper.P("@dt", closedAt), DbHelper.P("@lid", loadID), DbHelper.P("@did", driverID),
                    DbHelper.P("@tl", totLoaded), DbHelper.P("@tr", totRet), DbHelper.P("@td", totDead),
                    DbHelper.P("@te", totExtra), DbHelper.P("@tdf", totDef),
                    DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

                if (hvID <= 0) throw new Exception("فشل في إنشاء سجل التقفيل.");

                // 2. تفاصيل أصناف التقفيل
                foreach (var i in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO HandoverItems(HandoverID,ProductID,LoadedQty,ReturnedQty,DeadQty,ExtraQty,DeficitQty)
                          VALUES(@hid,@pid,@lq,@rq,@dq,@eq,@dfq)",
                        DbHelper.P("@hid", hvID), DbHelper.P("@pid", i.ProductID),
                        DbHelper.P("@lq", i.LoadedQty), DbHelper.P("@rq", i.ReturnedQty),
                        DbHelper.P("@dq", i.DeadQty), DbHelper.P("@eq", i.ExtraQty),
                        DbHelper.P("@dfq", i.DeficitQty));
                }

                // 3. إغلاق الحمولة
                DbHelper.ExecuteTrans(trans,
                    "UPDATE DriverLoads SET IsClosed=1, ClosedAt=@dt WHERE LoadID=@lid",
                    DbHelper.P("@dt", closedAt), DbHelper.P("@lid", loadID));

                // 4. قيد الخزنة (النقدية المحصّلة) — جزء من نفس الـ Transaction
                if (cashCollected > 0)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy) VALUES(@dt,'DriverHandover',@amt,@ref,@n,@by)",
                        DbHelper.P("@dt", closedAt), DbHelper.P("@amt", cashCollected),
                        DbHelper.P("@ref", loadID),
                        DbHelper.P("@n", $"تحصيل تقفيل حمولة ({loadID}) — مبيعات ({totalSoldValue:N2})"),
                        DbHelper.P("@by", Session.EmpID));
                }
            });
            // ===== نهاية Transaction الأساسي =====
            // قيد الخزنة (cashCollected) تم داخل Transaction الأساسي أعلاه بشكل آمن

            AppLogger.Audit("تقفيل حمولة مندوب", $"LoadID: {loadID}, DriverID: {driverID}, HandoverID: {hvID}, كاش محصل: {cashCollected:N2}");

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

        /// <summary>تسوية العجز المالي عند التقفيل — 3 خيارات: سلفة، مصروف شركة، خصم</summary>
        public static void SettleDeficit(int driverID, int loadID, decimal deficitValue, string settlementType, string notes)
        {
            // settlementType: "Advance" = سلفة على المندوب | "CompanyExpense" = مصروف شركة | "Deduction" = خصم
            DbHelper.RunInTransaction((con, trans) =>
            {
                if (settlementType == "Advance" || settlementType == "Deduction")
                {
                    // تسجيل مديونية/خصم على المندوب في EmployeeTransactions
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO EmployeeTransactions(EmpID, TransType, Debit, RefID, Notes, CreatedBy)
                          VALUES(@eid, @type, @amt, @ref, @n, @by)",
                        DbHelper.P("@eid", driverID),
                        DbHelper.P("@type", settlementType == "Advance" ? "DeficitCharge" : "Deduction"),
                        DbHelper.P("@amt", deficitValue),
                        DbHelper.P("@ref", loadID),
                        DbHelper.P("@n", notes),
                        DbHelper.P("@by", Session.EmpID));
                }
                else if (settlementType == "CompanyExpense")
                {
                    // تحميل العجز على الشركة كمصروف تشغيلي
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO Expenses(ExpenseDate, ExpenseType, Amount, Notes, CreatedBy)
                          VALUES(GETDATE(), N'عجز حمولة مندوب', @amt, @n, @by)",
                        DbHelper.P("@amt", deficitValue),
                        DbHelper.P("@n", notes),
                        DbHelper.P("@by", Session.EmpID));
                }
            });

            AppLogger.Audit("تسوية عجز حمولة", $"DriverID:{driverID} LoadID:{loadID} Value:{deficitValue:N2} Type:{settlementType}");
        }

        /// <summary>كشف التحصيل اليومي للمندوب — قائمة عملاء بديونهم لإرسالها عبر واتساب</summary>
        public static DataTable GetDriverCollectionList(int driverID, DateTime date)
        {
            return DbHelper.Query(
                @"SELECT
                    c.ClientName,
                    ISNULL(c.Phone, N'---') AS Phone,
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                    ISNULL((
                        SELECT SUM(TotalAmount)
                        FROM Sales
                        WHERE DriverID = @did AND ClientID = c.ClientID
                          AND CAST(SaleDate AS DATE) = @dt AND IsPosted = 1
                          AND SaleType IN ('Cash','Credit')
                    ), 0) AS TodaySales
                  FROM Clients c
                  LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                  WHERE c.DriverID = @did AND c.IsActive = 1
                    AND ISNULL(cb.Balance, c.OpeningBalance) > 0
                  ORDER BY Balance DESC",
                DbHelper.P("@did", driverID),
                DbHelper.P("@dt", date.Date));
        }

        /// <summary>بيانات لوحة أداء ومنافسة المناديب</summary>
        public static DataTable GetLeaderboard(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT
                    e.EmpID,
                    e.EmpName                                          AS DriverName,
                    ISNULL(e.Phone, N'---')                             AS Phone,
                    -- إجمالي المبيعات
                    ISNULL((
                        SELECT SUM(s.TotalAmount)
                        FROM Sales s
                        WHERE s.DriverID = e.EmpID AND s.IsPosted = 1
                          AND s.SaleType IN ('Cash','Credit')
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS TotalSales,
                    -- النقدية المحصلة (مبيعات نقدية + ما حُصّل عند التقفيل)
                    ISNULL((
                        SELECT SUM(s.TotalAmount)
                        FROM Sales s
                        WHERE s.DriverID = e.EmpID AND s.IsPosted = 1
                          AND s.SaleType = 'Cash'
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS CashSales,
                    -- إجمالي النافق
                    ISNULL((
                        SELECT SUM(dh.TotalDead)
                        FROM DriverHandovers dh
                        WHERE dh.DriverID = e.EmpID
                          AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS TotalDead,
                    -- إجمالي العجز (بعد قاعدة النافق = عجز)
                    ISNULL((
                        SELECT SUM(dh.TotalDeficit)
                        FROM DriverHandovers dh
                        WHERE dh.DriverID = e.EmpID
                          AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS TotalDeficit,
                    -- عدد الحمولات المقفلة
                    ISNULL((
                        SELECT COUNT(*)
                        FROM DriverHandovers dh
                        WHERE dh.DriverID = e.EmpID
                          AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS HandoverCount,
                    -- رصيد المديونية على المندوب
                    ISNULL((
                        SELECT SUM(et.Debit) - SUM(et.Credit)
                        FROM EmployeeTransactions et
                        WHERE et.EmpID = e.EmpID
                    ), 0) AS DebtBalance
                  FROM Employees e
                  WHERE e.IsDriver = 1 AND e.IsActive = 1
                  ORDER BY TotalSales DESC",
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date));
        }

        /// <summary>رصيد مديونية مندوب معين</summary>
        public static decimal GetEmployeeBalance(int empID)
        {
            var r = DbHelper.Scalar(
                "SELECT ISNULL(SUM(Debit),0)-ISNULL(SUM(Credit),0) FROM EmployeeTransactions WHERE EmpID=@id",
                DbHelper.P("@id", empID));
            return r != null ? Convert.ToDecimal(r) : 0;
        }

        // =====================================================================
        //  تصدير بيانات الجوال (data.json) — قائمة العملاء والأصناف والمناديب
        // =====================================================================
        /// <summary>
        /// يُولّد كائن JSON للمندوب يحتوي على قوائم العملاء والأصناف والمناديب النشطين.
        /// يُستخدم بواسطة FrmDriverHandover لتصدير ملف data.json للجوال.
        /// </summary>
        public static string BuildDriverExportJson()
        {
            // جلب العملاء النشطين مع DriverID
            var clients = DbHelper.Query(
                "SELECT ClientID, ClientName, ISNULL(Phone,'') AS Phone, DriverID FROM Clients WHERE IsActive=1 ORDER BY ClientName");

            // جلب الأصناف النشطة
            var products = DbHelper.Query(
                "SELECT ProductID, ProductName, SalePrice, ISNULL(Unit,'وحدة') AS Unit FROM Products WHERE IsActive=1 ORDER BY ProductName");

            // جلب المناديب النشطين
            var drivers = DbHelper.Query(
                "SELECT EmpID, EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 ORDER BY EmpName");

            // جلب الحمولات المفتوحة
            var loads = DbHelper.Query(@"
                SELECT dl.DriverID, dli.ProductID, SUM(dli.LoadedQty) AS LoadedQty
                FROM DriverLoads dl
                JOIN DriverLoadItems dli ON dl.LoadID = dli.LoadID
                WHERE dl.IsClosed = 0
                GROUP BY dl.DriverID, dli.ProductID");

            var sb = new System.Text.StringBuilder();
            sb.Append("{");

            // clients array
            sb.Append("\"clients\":[");
            bool firstC = true;
            foreach (System.Data.DataRow r in clients.Rows)
            {
                if (!firstC) sb.Append(",");
                string driverVal = r["DriverID"] == DBNull.Value ? "null" : r["DriverID"].ToString();
                sb.AppendFormat("{{\"id\":{0},\"name\":\"{1}\",\"phone\":\"{2}\",\"driverId\":{3}}}",
                    r["ClientID"],
                    EscapeJson(r["ClientName"].ToString()),
                    EscapeJson(r["Phone"].ToString()),
                    driverVal);
                firstC = false;
            }
            sb.Append("],");

            // products array
            sb.Append("\"products\":[");
            bool firstP = true;
            foreach (System.Data.DataRow r in products.Rows)
            {
                if (!firstP) sb.Append(",");
                sb.AppendFormat("{{\"id\":{0},\"name\":\"{1}\",\"price\":{2},\"unit\":\"{3}\"}}",
                    r["ProductID"],
                    EscapeJson(r["ProductName"].ToString()),
                    Convert.ToDecimal(r["SalePrice"]).ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                    EscapeJson(r["Unit"].ToString()));
                firstP = false;
            }
            sb.Append("],");

            // loads array
            sb.Append("\"loads\":[");
            bool firstL = true;
            foreach (System.Data.DataRow r in loads.Rows)
            {
                if (!firstL) sb.Append(",");
                sb.AppendFormat("{{\"driverId\":{0},\"productId\":{1},\"qty\":{2}}}",
                    r["DriverID"],
                    r["ProductID"],
                    Convert.ToDecimal(r["LoadedQty"]).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                firstL = false;
            }
            sb.Append("],");

            // drivers array
            sb.Append("\"drivers\":[");
            bool firstD = true;
            foreach (System.Data.DataRow r in drivers.Rows)
            {
                if (!firstD) sb.Append(",");
                sb.AppendFormat("{{\"id\":{0},\"name\":\"{1}\"}}",
                    r["EmpID"],
                    EscapeJson(r["EmpName"].ToString()));
                firstD = false;
            }
            sb.Append("]");

            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r", "").Replace("\n", " ");
        }

        // =====================================================================
        //  استيراد فاتورة واحدة من CSV (يُستدعى من FrmImportPreview)
        // =====================================================================
        /// <summary>
        /// يحفظ مجموعة بنود (تمثل فاتورة واحدة) كفاتورة رسمية في السيستم.
        /// يستخدم نفس SaveSale الرئيسي تماماً.
        /// </summary>
        /// <param name="clientID">رقم العميل (0 = عميل غير محدد)</param>
        /// <param name="driverID">رقم المندوب</param>
        /// <param name="paymentType">Cash أو Credit</param>
        /// <param name="saleDate">تاريخ البيع من الـ CSV</param>
        /// <param name="notes">ملاحظات الفاتورة</param>
        /// <param name="items">بنود الفاتورة</param>
        /// <returns>SaleID الجديد أو -1 عند الفشل</returns>
        public static int ImportDriverSaleRow(int clientID, int driverID, string paymentType,
            DateTime saleDate, string notes, List<SaleItemDTO> items)
        {
            if (items == null || items.Count == 0) return -1;
            decimal total = 0;
            foreach (var it in items) total += it.Quantity * it.UnitPrice;

            int returnedID = -1;
            DbHelper.RunInTransaction((con, trans) =>
            {
                var nextResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                string code = nextResult?.ToString() ?? "1";

                int saleID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO Sales(SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,Notes,CreatedBy,DiscountAmount,DiscountPct,IsPosted) " +
                    "VALUES(@code,@dt,@typ,@cid,@did,@tot,@n,@by,0,0,1)",
                    DbHelper.P("@code", code),
                    DbHelper.P("@dt", saleDate),
                    DbHelper.P("@typ", paymentType == "Cash" ? "Cash" : "Credit"),
                    DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                    DbHelper.P("@did", driverID > 0 ? (object)driverID : DBNull.Value),
                    DbHelper.P("@tot", total),
                    DbHelper.P("@n", notes ?? "استيراد مبيعات مندوب"),
                    DbHelper.P("@by", Session.EmpID));

                if (saleID <= 0) throw new Exception("فشل في إنشاء الفاتورة المستوردة.");
                returnedID = saleID;

                foreach (var it in items)
                {
                    decimal lineTotal = it.Quantity * it.UnitPrice;
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt) " +
                        "VALUES(@sid,@pid,@qty,@up,@tot,0,0)",
                        DbHelper.P("@sid", saleID),
                        DbHelper.P("@pid", it.ProductID),
                        DbHelper.P("@qty", it.Quantity),
                        DbHelper.P("@up", it.UnitPrice),
                        DbHelper.P("@tot", lineTotal));
                }

                // قيد العميل (Credit) إن كانت آجل
                if (paymentType != "Cash" && clientID > 0)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ClientTransactions(ClientID,TransDate,TransType,Debit,RefID,Notes,CreatedBy) " +
                        "VALUES(@cid,@dt,'Sale',@amt,@ref,@n,@by)",
                        DbHelper.P("@cid", clientID),
                        DbHelper.P("@dt", saleDate),
                        DbHelper.P("@amt", total),
                        DbHelper.P("@ref", saleID),
                        DbHelper.P("@n", "استيراد مبيعات مندوب — فاتورة #" + code),
                        DbHelper.P("@by", Session.EmpID));
                }
                // قيد خزنة (Cash)
                else if (paymentType == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy) " +
                        "VALUES(@dt,'DriverSaleImport',@amt,@ref,@n,@by)",
                        DbHelper.P("@dt", saleDate),
                        DbHelper.P("@amt", total),
                        DbHelper.P("@ref", saleID),
                        DbHelper.P("@n", "استيراد نقدي مندوب — فاتورة #" + code),
                        DbHelper.P("@by", Session.EmpID));
                }
            });

            AppLogger.Audit("استيراد فاتورة مندوب", $"SaleID:{returnedID} Client:{clientID} Driver:{driverID} Total:{total:N2}");
            return returnedID;
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

        // ✅ قاعدة: النافق = عجز — لا يخصم النافق من المتوقع، فكل نافق يحاسب عليه المندوب
        // المتوقع = المحمل - المرتجع فقط (النافق لا يُعفي المندوب)
        // عجز الكمية = المتوقع - المباع (إذا كان المتوقع > المباع)
        public decimal DeficitQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty; // لا نطرح DeadQty
                return expected > SoldQty ? expected - SoldQty : 0;
            }
        }

        public decimal ExtraQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty; // لا نطرح DeadQty
                return SoldQty > expected ? SoldQty - expected : 0;
            }
        }

        /// <summary>القيمة المالية للعجز (كمية العجز × سعر الوحدة)</summary>
        public decimal DeficitValue => DeficitQty * UnitPrice;

        /// <summary>القيمة المالية للنافق (كمية النافق × سعر الوحدة)</summary>
        public decimal DeadValue => DeadQty * UnitPrice;
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
            int returnedRetID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                var dtSale = DbHelper.Query("SELECT SaleType, ClientID FROM Sales WHERE SaleID=@sid", DbHelper.P("@sid", saleID));
                string saleType = dtSale.Rows.Count > 0 ? dtSale.Rows[0]["SaleType"].ToString() : "Credit";

                // استخدم clientID من الفاتورة الأصلية إذا لم يُحدَّد في الشاشة
                if (!clientID.HasValue && dtSale.Rows.Count > 0 && dtSale.Rows[0]["ClientID"] != DBNull.Value)
                    clientID = Convert.ToInt32(dtSale.Rows[0]["ClientID"]);

                int retID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO SalesReturns(ReturnDate,SaleID,ClientID,TotalAmount,Notes,CreatedBy) VALUES(@dt,@sid,@cid,@tot,@n,@by)",
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@sid", saleID > 0 ? (object)saleID : DBNull.Value),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

                if (retID <= 0) throw new Exception("فشل إنشاء سجل المرتجع.");

                returnedRetID = retID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice) VALUES(@rid,@pid,@qty,@up,@tp)",
                        DbHelper.P("@rid", retID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice));
                }

                // المنطق المحاسبي السليم:
                // بيع نقدي → رد نقدي من الخزنة (AmountOut)
                // بيع آجل أو حمولة مندوب → تخفيض دين العميل (Credit في ClientTransactions)
                if (saleType == "Cash")
                {
                    // تسجيل خروج نقدية من الخزنة (رد مبلغ المرتجع للعميل)
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy) VALUES(@dt,'ReturnOutcome',@amt,@ref,@n,@by)",
                        DbHelper.P("@dt", DateTime.Now),
                        DbHelper.P("@amt", total), DbHelper.P("@ref", retID),
                        DbHelper.P("@n", "مرتجع بيع للفاتورة رقم " + saleID),
                        DbHelper.P("@by", Session.EmpID));
                }
                else if (clientID.HasValue)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'Return',@amt,@ref,@n,@by)",
                        DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                        DbHelper.P("@ref", retID), DbHelper.P("@n", "مرتجع بيع للفاتورة رقم " + saleID),
                        DbHelper.P("@by", Session.EmpID));
                }
            });

            return returnedRetID;
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
            // FIX: استخدام CTE بدلاً من subqueries متكررة (3x) لنفس ReturnedQty
            return DbHelper.Query(
                @";WITH ReturnTotals AS (
                    SELECT ri.ProductID,
                           SUM(ri.Quantity)   AS ReturnedQty,
                           SUM(ri.TotalPrice) AS ReturnedAmount
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    GROUP BY ri.ProductID
                )
                SELECT 
                    p.ProductName,
                    p.Unit,
                    AVG(si.UnitPrice)                                 AS AvgPrice,
                    SUM(si.Quantity)                                   AS TotalQty,
                    SUM(si.TotalPrice)                                 AS TotalAmount,
                    ISNULL(rt.ReturnedQty, 0)                         AS ReturnedQty,
                    ISNULL(rt.ReturnedAmount, 0)                       AS ReturnedAmount,
                    SUM(si.Quantity)   - ISNULL(rt.ReturnedQty, 0)   AS NetQty,
                    SUM(si.TotalPrice) - ISNULL(rt.ReturnedAmount, 0) AS NetAmount
                FROM SaleItems si
                JOIN Sales s    ON si.SaleID    = s.SaleID
                JOIN Products p ON si.ProductID = p.ProductID
                LEFT JOIN ReturnTotals rt ON rt.ProductID = p.ProductID
                WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t AND s.IsPosted = 1
                GROUP BY p.ProductID, p.ProductName, p.Unit, rt.ReturnedQty, rt.ReturnedAmount
                ORDER BY TotalQty DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

                /// <summary>كميات مبيعات كل عميل لكل صنف في يوم معين (للتقرير اليومي المحوري) — مخصوماً منها المرتجعات</summary>
        public static DataTable GetDailyClientProductSales(DateTime date)
        {
            return DbHelper.Query(
                @"SELECT
                    c.ClientID,
                    c.ClientName,
                    t.ProductID,
                    SUM(t.Qty) AS TotalQty,
                    MAX(t.UnitPrice) AS UnitPrice
                  FROM (
                      SELECT s.ClientID, si.ProductID, si.Quantity AS Qty, si.UnitPrice
                      FROM SaleItems si
                      JOIN Sales s ON si.SaleID = s.SaleID
                      WHERE CAST(s.SaleDate AS DATE) = @date
                        AND s.IsPosted = 1
                        AND s.SaleType IN ('Cash','Credit')
                      
                      UNION ALL
                      
                      SELECT sr.ClientID, ri.ProductID, -ri.Quantity AS Qty, ri.UnitPrice
                      FROM ReturnItems ri
                      JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                      WHERE CAST(sr.ReturnDate AS DATE) = @date
                  ) t
                  JOIN Clients c ON t.ClientID = c.ClientID
                  GROUP BY c.ClientID, c.ClientName, t.ProductID
                  ORDER BY c.ClientName",
                DbHelper.P("@date", date.Date));
        }

        /// <summary>إجمالي الفاتورة وآخر توريد والمديونية لكل عميل في يوم معين — مخصوماً منها المرتجعات</summary>
        public static DataTable GetDailyClientTotals(DateTime date)
        {
            return DbHelper.Query(
                @"SELECT
                    c.ClientID,
                    c.ClientName,
                    SUM(t.Amt) AS TotalInvoice,
                    ISNULL((
                        SELECT TOP 1 ct.Credit
                        FROM ClientTransactions ct
                        WHERE ct.ClientID = c.ClientID
                          AND ct.TransType = 'Payment'
                        ORDER BY ct.TransDate DESC
                    ), 0) AS LastPayment,
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                  FROM (
                      SELECT ClientID, TotalAmount AS Amt
                      FROM Sales
                      WHERE CAST(SaleDate AS DATE) = @date
                        AND IsPosted = 1
                        AND SaleType IN ('Cash','Credit')

                      UNION ALL

                      SELECT ClientID, -TotalAmount AS Amt
                      FROM SalesReturns
                      WHERE CAST(ReturnDate AS DATE) = @date
                  ) t
                  JOIN Clients c ON t.ClientID = c.ClientID
                  LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
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
            // FIX: استخدام CTE لحساب SoldQty, ReturnedQty, DriverReturnQty مرة واحدة
            // الكود القديم كان يُعيد حساب نفس الـ subqueries 3 مرات في NetSoldQty و CurrentStock
            // مما يُسبّب ضغطاً كبيراً على قاعدة البيانات في التقارير الكبيرة
            return DbHelper.Query(@"
                ;WITH
                SalesPeriod AS (
                    SELECT si.ProductID,
                           SUM(si.Quantity)   AS TotalQty,
                           SUM(si.TotalPrice) AS TotalAmt,
                           SUM(CASE WHEN s.SaleType='Cash'       THEN si.Quantity ELSE 0 END) AS CashQty,
                           SUM(CASE WHEN s.SaleType='Credit'     THEN si.Quantity ELSE 0 END) AS CreditQty,
                           SUM(CASE WHEN s.SaleType='DriverLoad' THEN si.Quantity ELSE 0 END) AS DriverLoadQty
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    WHERE s.IsPosted = 1
                      AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    GROUP BY si.ProductID
                ),
                ReturnsPeriod AS (
                    SELECT ri.ProductID,
                           SUM(ri.Quantity) AS ReturnedQty
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    GROUP BY ri.ProductID
                ),
                DriverReturnsPeriod AS (
                    SELECT hi.ProductID,
                           SUM(hi.ReturnedQty) AS DriverReturnQty
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    WHERE CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                      AND hi.ReturnedQty > 0
                    GROUP BY hi.ProductID
                ),
                StockSinceAdj AS (
                    SELECT p2.ProductID,
                           ISNULL(adj2.ActualQty, 0)
                           + ISNULL((SELECT SUM(ri2.Quantity) FROM ReturnItems ri2
                                      JOIN SalesReturns sr2 ON ri2.ReturnID=sr2.ReturnID
                                      WHERE ri2.ProductID=p2.ProductID
                                        AND (adj2.AdjDate IS NULL OR sr2.ReturnDate > adj2.AdjDate)), 0)
                           + ISNULL((SELECT SUM(hi2.ReturnedQty) FROM HandoverItems hi2
                                      JOIN DriverHandovers dh2 ON hi2.HandoverID=dh2.HandoverID
                                      WHERE hi2.ProductID=p2.ProductID
                                        AND (adj2.AdjDate IS NULL OR dh2.HandoverDate > adj2.AdjDate)), 0)
                           -- Incoming: Purchases since adjustment
                           + ISNULL((SELECT SUM(pi2.Quantity) FROM PurchaseItems pi2
                                      JOIN Purchases pu2 ON pi2.PurchaseID = pu2.PurchaseID
                                      WHERE pi2.ProductID = p2.ProductID
                                        AND pu2.IsPosted = 1
                                        AND (adj2.AdjDate IS NULL OR pu2.PurchaseDate > adj2.AdjDate)), 0)
                           -- Outgoing: Purchase Returns since adjustment
                           - ISNULL((SELECT SUM(pri2.Quantity) FROM PurchaseReturnItems pri2
                                      JOIN PurchaseReturns pr2 ON pri2.ReturnID = pr2.ReturnID
                                      WHERE pri2.ProductID = p2.ProductID
                                        AND (adj2.AdjDate IS NULL OR pr2.ReturnDate > adj2.AdjDate)), 0)
                           -- Outgoing: Warehouse Sales & Driver Loads (prevent double counting driver road sales)
                           - ISNULL((SELECT SUM(si2.Quantity) FROM SaleItems si2
                                      JOIN Sales s2 ON si2.SaleID=s2.SaleID
                                      WHERE si2.ProductID=p2.ProductID
                                        AND s2.IsPosted = 1
                                        AND (s2.SaleType = 'DriverLoad' OR (s2.SaleType IN ('Cash', 'Credit') AND s2.DriverID IS NULL))
                                        AND (adj2.AdjDate IS NULL OR s2.SaleDate > adj2.AdjDate)), 0)
                           AS CurrentStock
                    FROM Products p2
                    OUTER APPLY (
                        SELECT TOP 1 sa2.AdjDate, sa2.ActualQty
                        FROM StockAdjustments sa2
                        WHERE sa2.ProductID = p2.ProductID
                        ORDER BY sa2.AdjDate DESC
                    ) adj2
                    WHERE p2.IsActive = 1
                )
                SELECT
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,

                    ISNULL((
                        SELECT TOP 1 sa.ActualQty
                        FROM StockAdjustments sa
                        WHERE sa.ProductID = p.ProductID
                          AND sa.AdjDate <= DATEADD(DAY, 1, @t)
                        ORDER BY sa.AdjDate DESC
                    ), 0)                                                   AS LastAdjQty,

                    ISNULL(sp.TotalQty,        0)                          AS SoldQty,
                    ISNULL(sp.CashQty,         0)                          AS CashQty,
                    ISNULL(sp.CreditQty,       0)                          AS CreditQty,
                    ISNULL(sp.DriverLoadQty,   0)                          AS DriverLoadQty,
                    ISNULL(rp.ReturnedQty,     0)                          AS ReturnedQty,
                    ISNULL(drp.DriverReturnQty,0)                          AS DriverReturnQty,

                    -- صافي المبيعات: مرة حساب واحدة من الـ CTEs بدلاً من subqueries مكررة
                    ISNULL(sp.TotalQty, 0)
                    - ISNULL(rp.ReturnedQty, 0)
                    - ISNULL(drp.DriverReturnQty, 0)                       AS NetSoldQty,

                    ISNULL(sp.TotalAmt,        0)                          AS TotalSalesAmt,

                    -- الرصيد الكتابي الحالي من الـ CTE
                    ISNULL(sca.CurrentStock,   0)                          AS CurrentStock

                FROM Products p
                LEFT JOIN SalesPeriod        sp  ON sp.ProductID  = p.ProductID
                LEFT JOIN ReturnsPeriod      rp  ON rp.ProductID  = p.ProductID
                LEFT JOIN DriverReturnsPeriod drp ON drp.ProductID = p.ProductID
                LEFT JOIN StockSinceAdj      sca ON sca.ProductID = p.ProductID
                WHERE p.IsActive = 1
                ORDER BY p.ProductName",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }
    }
}

