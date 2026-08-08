using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class SaleDAL
    {
        public static decimal? GetLastPriceForClient(int productID, int clientID)
        {
            if (productID <= 0 || clientID <= 0) return null;
            object res = DbHelper.Scalar(@"
                SELECT TOP 1 si.UnitPrice 
                FROM SaleItems si 
                JOIN Sales s ON si.SaleID = s.SaleID 
                WHERE s.ClientID = @cid AND si.ProductID = @pid AND s.IsPosted = 1 
                ORDER BY s.SaleDate DESC, s.SaleID DESC",
                DbHelper.P("@cid", clientID), DbHelper.P("@pid", productID));

            if (res != null && res != DBNull.Value && decimal.TryParse(res.ToString(), out decimal price))
                return price;
            return null;
        }

        public static DataTable GetAll(DateTime from, DateTime to)
        {
            return GetAll(from, to, null, null, null);
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? warehouseID)
        {
            return GetAll(from, to, null, null, warehouseID);
        }

        public static DataTable GetAll(DateTime from, DateTime to, int? clientID, string productSearch, int? warehouseID = null)
        {
            string productFilter = string.IsNullOrWhiteSpace(productSearch) ? null : productSearch.Trim();
            DateTime f = from;
            DateTime t = to;
            if (t.TimeOfDay == TimeSpan.Zero)
            {
                t = t.Date.AddDays(1).AddTicks(-1);
            }

            return DbHelper.Query(
                @"SELECT s.SaleID, s.SaleCode, s.SaleDate, s.SaleType,
                         ISNULL(c.ClientName,N'---') AS ClientName,
                         ISNULL(e.EmpName,N'---') AS DriverName,
                         s.TotalAmount, s.Notes,
                         ISNULL(creator.EmpName, N'---') AS CreatedByName,
                         ISNULL(s.ShippingCharge, 0.0) AS ShippingCharge,
                         ISNULL(ret.ReturnAmount, 0) AS ReturnAmount,
                         ISNULL(costs.TotalCost, 0) AS TotalCost,
                         (s.TotalAmount - ISNULL(costs.TotalCost, 0)) AS NetProfit
                  FROM Sales s
                  LEFT JOIN Clients c ON s.ClientID = c.ClientID
                  LEFT JOIN Employees e ON s.DriverID = e.EmpID
                  LEFT JOIN Employees creator ON s.CreatedBy = creator.EmpID
                  LEFT JOIN (
                      SELECT r.SaleID, SUM(ri.Quantity * ri.UnitPrice) AS ReturnAmount
                      FROM SalesReturns r
                      JOIN ReturnItems ri ON r.ReturnID = ri.ReturnID
                      GROUP BY r.SaleID
                  ) ret ON ret.SaleID = s.SaleID
                  LEFT JOIN (
                      SELECT si.SaleID,
                             SUM(si.Quantity * ISNULL(si.Factor, 1.0) *
                                 COALESCE(NULLIF(p.Unit1PurchasePrice, 0),
                                 ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0),
                                 NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))) AS TotalCost
                      FROM SaleItems si
                      JOIN Products p ON si.ProductID = p.ProductID
                      GROUP BY si.SaleID
                  ) costs ON costs.SaleID = s.SaleID
                  WHERE s.SaleDate BETWEEN @f AND @t
                    AND (@clientID IS NULL OR s.ClientID = @clientID)
                    AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                    AND (@product IS NULL OR EXISTS (
                        SELECT 1 FROM SaleItems si2
                        JOIN Products pr ON si2.ProductID = pr.ProductID
                        WHERE si2.SaleID = s.SaleID
                        AND (pr.ProductName LIKE N'%' + @product + N'%'
                          OR pr.ProductCode LIKE N'%' + @product + N'%')
                    ))
                  ORDER BY s.SaleDate DESC",
                DbHelper.P("@f", f), DbHelper.P("@t", t),
                DbHelper.P("@clientID", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                DbHelper.P("@product", (object)productFilter ?? DBNull.Value),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetItems(int saleID)
        {
            return DbHelper.Query(
                @"SELECT si.ItemID, si.ProductID, p.ProductName, si.Quantity, si.Quantity AS SoldQty, si.UnitPrice, si.TotalPrice,
                          COALESCE(si.DiscountPct, 0) AS DiscountPct, COALESCE(si.DiscountAmt, 0) AS DiscountAmt,
                          COALESCE(si.PriceTier, N'قطاعي') AS PriceTier,
                          COALESCE(p.PurchasePrice, 0) AS PurchasePrice,
                          p.PartNumber, p.CarModel, p.Brand, p.ShelfLocation,
                          p.Unit AS BaseUnitName, p.Unit1Name, p.Unit1SalePrice, p.Unit2Name, p.Unit2Factor, p.Unit2SalePrice, p.Unit3Factor,
                          ISNULL(ret.PrevReturnedQty, 0.0) AS PrevReturnedQty,
                          si.UnitName, COALESCE(si.Factor, 1.0) AS Factor, si.IMEI, si.KitchenNotes
                  FROM SaleItems si 
                  JOIN Products p ON si.ProductID = p.ProductID
                  LEFT JOIN (
                      SELECT ri.ProductID, ISNULL(SUM(ri.Quantity), 0.0) AS PrevReturnedQty
                      FROM SalesReturns sr
                      JOIN ReturnItems ri ON sr.ReturnID = ri.ReturnID
                      WHERE sr.SaleID = @id
                      GROUP BY ri.ProductID
                  ) ret ON ret.ProductID = si.ProductID
                  WHERE si.SaleID = @id",
                DbHelper.P("@id", saleID));
        }

        public static int SaveSale(int saleType, int? clientID, int? driverID, decimal total, string notes,
            List<SaleItemDTO> items, decimal discountAmount = 0m, decimal discountPct = 0m, bool isDraft = false, int? warehouseID = null, string priceTier = "قطاعي",
            decimal downPayment = 0m, int installmentCount = 1, string installmentPeriod = "Monthly", DateTime? startDate = null, List<InstallmentScheduleDTO> schedule = null, int branchID = 1, int? safeAccountID = null, decimal? cashPaid = null,
            int cratesOut = 0, int cratesIn = 0, decimal shippingCharge = 0m, string orderType = null, string tableNumber = null)
        {
            int? activeShiftID = ShiftDAL.GetActiveShiftID();
            if (!activeShiftID.HasValue && !isDraft)
            {
                throw new InvalidOperationException("⚠️ عفوًا: لا يمكن حفظ الفاتورة بدون وجود وردية (شيفت) مفتوحة حالياً!\nيرجى فتح وردية جديدة أولاً لتسجيل الفاتورة وحساب النقدية والدرج.");
            }

            int returnedSaleID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                string typeStr = saleType == 0 ? "Credit" : saleType == 1 ? "DriverLoad" : saleType == 3 ? "Installment" : "Cash";
                var nextSaleResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                string code = nextSaleResult != null ? nextSaleResult.ToString() : "1";
                int targetWarehouse = warehouseID ?? 1;

                int saleID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO Sales(SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,Notes,CreatedBy,DiscountAmount,DiscountPct,IsPosted,WarehouseID,PriceTier,CashPaid,CratesOut,CratesIn,LastModifiedDate,ShippingCharge,OrderType,TableNumber,ShiftID) VALUES(@code,@dt,@typ,@cid,@did,@tot,@n,@by,@discAmt,@discPct,@ip,@wid,@pt,@cp,@co,@ci,GETDATE(),@shipping,@ot,@tn,@sid)",
                    DbHelper.P("@code", code), DbHelper.P("@dt", DateTime.Now), DbHelper.P("@typ", typeStr),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@did", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@discAmt", discountAmount), DbHelper.P("@discPct", discountPct),
                    DbHelper.P("@ip", !isDraft), DbHelper.P("@wid", targetWarehouse), DbHelper.P("@pt", priceTier),
                    DbHelper.P("@cp", cashPaid.HasValue ? (object)cashPaid.Value : DBNull.Value),
                    DbHelper.P("@co", cratesOut), DbHelper.P("@ci", cratesIn), DbHelper.P("@shipping", shippingCharge),
                    DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                    DbHelper.P("@tn", string.IsNullOrEmpty(tableNumber) ? DBNull.Value : (object)tableNumber),
                    DbHelper.P("@sid", activeShiftID.HasValue ? (object)activeShiftID.Value : DBNull.Value));

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
                        "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,IMEI,KitchenNotes) VALUES(@sid,@pid,@qty,@up,@tp,@dpct,@damt,@pt,@un,@fac,@exp,@bid,@imei,@kn)",
                        DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice), DbHelper.P("@dpct", item.DiscountPct),
                        DbHelper.P("@damt", item.DiscountAmt), DbHelper.P("@pt", item.PriceTier ?? priceTier),
                        DbHelper.P("@un", item.UnitName), DbHelper.P("@fac", item.Factor),
                        DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                        DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                        DbHelper.P("@imei", string.IsNullOrEmpty(item.IMEI) ? DBNull.Value : (object)item.IMEI),
                        DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes));

                    // Deduct from ProductBatches table
                    if (!isDraft)
                    {
                        if (item.BatchID.HasValue)
                        {
                            decimal baseQty = item.Quantity * item.Factor;
                            DbHelper.ExecuteTrans(trans,
                                "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                DbHelper.P("@q", baseQty), DbHelper.P("@bid", item.BatchID.Value));
                        }
                        else
                        {
                            var hasExpObj = DbHelper.ScalarTrans(trans, "SELECT HasExpiry FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", item.ProductID));
                            if (hasExpObj != null && hasExpObj != DBNull.Value && Convert.ToBoolean(hasExpObj))
                            {
                                decimal remainingQty = item.Quantity * item.Factor;
                                var batchesDt = DbHelper.QueryTrans(trans, 
                                    "SELECT BatchID, Quantity FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid AND Quantity > 0 ORDER BY ExpiryDate ASC, BatchID ASC",
                                    DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", targetWarehouse));
                                foreach (DataRow bRow in batchesDt.Rows)
                                {
                                    int bId = Convert.ToInt32(bRow["BatchID"]);
                                    decimal bQty = Convert.ToDecimal(bRow["Quantity"]);
                                    decimal toDeduct = Math.Min(remainingQty, bQty);
                                    if (toDeduct > 0)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", toDeduct), DbHelper.P("@bid", bId));
                                        remainingQty -= toDeduct;
                                        if (remainingQty <= 0) break;
                                    }
                                }
                                if (remainingQty > 0)
                                {
                                    var oldestBatchId = DbHelper.ScalarTrans(trans, "SELECT TOP 1 BatchID FROM ProductBatches WHERE ProductID = @pid AND WarehouseID = @wid ORDER BY ExpiryDate ASC, BatchID ASC", DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", targetWarehouse));
                                    if (oldestBatchId != null && oldestBatchId != DBNull.Value)
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "UPDATE ProductBatches SET Quantity = Quantity - @q WHERE BatchID = @bid",
                                            DbHelper.P("@q", remainingQty), DbHelper.P("@bid", oldestBatchId));
                                    }
                                    else
                                    {
                                        DbHelper.ExecuteTrans(trans,
                                            "INSERT INTO ProductBatches (ProductID, WarehouseID, Quantity, ExpiryDate) VALUES (@pid, @wid, -@q, @exp)",
                                            DbHelper.P("@pid", item.ProductID), DbHelper.P("@wid", targetWarehouse), DbHelper.P("@q", remainingQty), DbHelper.P("@exp", DateTime.Today.AddDays(30)));
                                    }
                                }
                            }
                        }
                    }

                    // check if price is different from product base price to log it in PriceChangesLog
                    if (!isDraft)
                    {
                        var basePriceObj = DbHelper.ScalarTrans(trans, "SELECT SalePrice FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", item.ProductID));
                        if (basePriceObj != null && basePriceObj != DBNull.Value)
                        {
                            decimal basePrice = Convert.ToDecimal(basePriceObj);
                            if (Math.Abs(item.UnitPrice - basePrice) > 0.005m)
                            {
                                string changeNotes = $"بيع بسعر مختلف في فاتورة البيع #{code}";
                                DbHelper.ExecuteTrans(trans,
                                    @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                                      VALUES (@pid, @old, @new, 'SalesInvoice', @ref, @uid, @notes)",
                                    DbHelper.P("@pid", item.ProductID), DbHelper.P("@old", basePrice), DbHelper.P("@new", item.UnitPrice),
                                    DbHelper.P("@ref", saleID), DbHelper.P("@uid", Session.EmpID), DbHelper.P("@notes", changeNotes));
                            }
                        }

                        // Atomic update to subtract from PendingQtyThreshold if sold at old SalePrice
                        DbHelper.ExecuteTrans(trans,
                            @"UPDATE Products
                              SET PendingQtyThreshold = CASE 
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingQtyThreshold - @qty
                                  END,
                                  SalePrice = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN PendingSalePrice
                                  ELSE SalePrice
                                  END,
                                  PendingSalePrice = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingSalePrice
                                  END,
                                  PendingPriceSourceRefID = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingPriceSourceRefID
                                  END
                              WHERE ProductID = @pid 
                                AND PendingSalePrice IS NOT NULL 
                                AND PendingQtyThreshold > 0 
                                AND @qty > 0
                                AND ABS(SalePrice - @up) < 0.005",
                            DbHelper.P("@qty", item.Quantity),
                            DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@up", item.UnitPrice));
                    }
                }

                if (!isDraft)
                {
                    // حركات الأقفاص
                    if (clientID.HasValue && (cratesOut > 0 || cratesIn > 0))
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientCratesTransactions(ClientID,CratesOut,CratesIn,RefSaleID,Notes,CreatedBy) VALUES(@cid,@co,@ci,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@co", cratesOut), DbHelper.P("@ci", cratesIn),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "حركة فوارغ فاتورة مبيعات " + code),
                            DbHelper.P("@by", Session.EmpID));
                    }

                    // آجل: أضف للحساب
                    if (typeStr == "Credit" && clientID.HasValue)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "فاتورة بيع " + code),
                            DbHelper.P("@by", Session.EmpID));
                    }

                    // تقسيط: أضف للحساب بالكامل (مدين: العملاء بالتقسيط، دائن: المبيعات)
                    if (typeStr == "Installment" && clientID.HasValue)
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "فاتورة بيع بالتقسيط " + code),
                            DbHelper.P("@by", Session.EmpID));

                        // إنشاء عقد التقسيط والأقساط المرتبطة به
                        var nextContractResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(ContractID), 0) + 1 FROM InstallmentContracts");
                        int nextCId = nextContractResult != null ? Convert.ToInt32(nextContractResult) : 1;
                        string contractCode = "INST-" + nextCId.ToString("D4");

                        decimal financedAmount = total - downPayment;
                        decimal instValue = installmentCount > 0 ? Math.Round(financedAmount / installmentCount, 2) : financedAmount;

                        int contractID = DbHelper.ExecuteInsertTrans(trans,
                            @"INSERT INTO InstallmentContracts (ContractCode, BranchID, InvoiceID, CustomerID, SaleType, ContractAmount, DownPayment, FinancedAmount, InstallmentCount, InstallmentValue, StartDate, Status, Notes, CreatedBy, CreatedDate)
                              VALUES (@cc, @bid, @inv, @cust, 'Installment', @tot, @dp, @fa, @ic, @iv, @sd, 'Active', @notes, @uid, @cd)",
                            DbHelper.P("@cc", contractCode),
                            DbHelper.P("@bid", branchID),
                            DbHelper.P("@inv", saleID),
                            DbHelper.P("@cust", clientID.Value),
                            DbHelper.P("@tot", total),
                            DbHelper.P("@dp", downPayment),
                            DbHelper.P("@fa", financedAmount),
                            DbHelper.P("@ic", installmentCount),
                            DbHelper.P("@iv", instValue),
                            DbHelper.P("@sd", startDate ?? DateTime.Today),
                            DbHelper.P("@notes", notes),
                            DbHelper.P("@uid", Session.EmpID),
                            DbHelper.P("@cd", DateTime.Now));

                        if (schedule != null)
                        {
                            foreach (var s in schedule)
                            {
                                DbHelper.ExecuteTrans(trans,
                                    @"INSERT INTO InstallmentSchedules (ContractID, InstallmentNo, DueDate, Amount, PaidAmount, RemainingAmount, Status)
                                      VALUES (@cid, @no, @dt, @amt, 0, @amt, 'Pending')",
                                    DbHelper.P("@cid", contractID),
                                    DbHelper.P("@no", s.InstallmentNo),
                                    DbHelper.P("@dt", s.DueDate),
                                    DbHelper.P("@amt", s.Amount));
                            }
                        }

                        // إذا تم دفع مقدم، نسجل الحركات المالية (مدين: الصندوق/الخزنة، دائن: العملاء بالتقسيط)
                        if (downPayment > 0)
                        {
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID, TransType, Credit, RefID, Notes, CreatedBy) VALUES(@cid, 'Payment', @amt, @ref, @notes, @uid)",
                                DbHelper.P("@cid", clientID.Value),
                                DbHelper.P("@amt", downPayment),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@notes", $"دفعة مقدمة لعقد التقسيط {contractCode}"),
                                DbHelper.P("@uid", Session.EmpID));

                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO CashBox(TransType, AmountIn, RefID, Notes, CreatedBy, AccountID) VALUES('ClientPayment', @amt, @ref, @notes, @uid, @accId)",
                                DbHelper.P("@amt", downPayment),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@notes", $"مقدم عقد التقسيط {contractCode} - فاتورة {code}"),
                                DbHelper.P("@uid", Session.EmpID),
                                DbHelper.P("@accId", safeAccountID.HasValue ? (object)safeAccountID.Value : DBNull.Value));
                        }

                        InstallmentDAL.AddAuditLogTrans(trans, "Create", contractID, "", $"إنشاء عقد التقسيط بقيمة: {total:N2} ج");
                    }

                    // نقدي: أضف للخزنة وسجل في حساب العميل دائماً
                    if (typeStr == "Cash")
                    {
                        decimal actualPaid = cashPaid ?? total;
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy,AccountID) VALUES('SaleIncome',@amt,@ref,@n,@by,@accId)",
                            DbHelper.P("@amt", actualPaid), DbHelper.P("@ref", saleID),
                            DbHelper.P("@n", "بيع نقدي " + code), DbHelper.P("@by", Session.EmpID),
                            DbHelper.P("@accId", safeAccountID.HasValue ? (object)safeAccountID.Value : DBNull.Value));

                        // تسجيل الفاتورة وسداد العميل دائماً في ClientTransactions (يظهر في كشف الحساب)
                        if (clientID.HasValue)
                        {
                            // مدين: قيمة الفاتورة كاملة
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                                DbHelper.P("@ref", saleID), DbHelper.P("@n", "فاتورة بيع نقدي " + code),
                                DbHelper.P("@by", Session.EmpID));
                            // دائن: المبلغ المسدد فعلاً
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'Payment',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", actualPaid),
                                DbHelper.P("@ref", saleID), DbHelper.P("@n", "سداد فاتورة بيع نقدي " + code),
                                DbHelper.P("@by", Session.EmpID));
                        }
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
                // Run price threshold checks asynchronously in the background to free the UI thread instantly
                System.Threading.Tasks.Task.Run(() =>
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
                });
            }

            try { System.Threading.Tasks.Task.Run(() => Services.CloudSyncService.PushLiveStatsToFirestoreAsync()); } catch {}

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
                AppLogger.Audit("حذف مسودة فاتورة", $"مسودة فاتورة رقم (#{saleID})");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"فشل حذف مسودة الفاتورة رقم {saleID} — الفاتورة قد تظل معلقة في قاعدة البيانات", ex, "SaleDAL.DeleteDraftSale");
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

            // 3. التحقق من وجود عقد تقسيط به تحصيلات مسجلة
            var dtContract = DbHelper.Query("SELECT ContractID FROM InstallmentContracts WHERE InvoiceID=@id AND Status <> 'Cancelled'", DbHelper.P("@id", saleID));
            if (dtContract.Rows.Count > 0)
            {
                int contractID = Convert.ToInt32(dtContract.Rows[0]["ContractID"]);
                if (InstallmentDAL.HasPaymentsCollected(contractID))
                {
                    reason = "لا يمكن حذف الفاتورة لوجود أقساط مسددة أو دفعات مسجلة على عقد التقسيط المرتبط بها. يجب عمل مرتجع مبيعات أو تسوية مالية.";
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
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                    decimal up = Convert.ToDecimal(r["UnitPrice"]);

                    DbHelper.ExecuteTrans(trans,
                        @"UPDATE Products
                          SET PendingQtyThreshold = PendingQtyThreshold + @qty
                          WHERE ProductID = @pid 
                            AND PendingSalePrice IS NOT NULL 
                            AND PendingQtyThreshold > 0
                            AND ABS(SalePrice - @up) < 0.005",
                        DbHelper.P("@qty", qty),
                        DbHelper.P("@pid", pid),
                        DbHelper.P("@up", up));

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

                // 4. عكس حركات حساب العميل إذا كان بيع آجل أو تقسيط
                if (typeStr == "Credit" || typeStr == "Installment")
                {
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM ClientTransactions WHERE RefID=@id",
                        DbHelper.P("@id", saleID));
                }

                // 4a. حذف عقد التقسيط المرتبط بالفاتورة والجدولة والدفعات بالكامل
                if (typeStr == "Installment")
                {
                    var dtContractDel = DbHelper.QueryTrans(trans, "SELECT ContractID FROM InstallmentContracts WHERE InvoiceID=@id", DbHelper.P("@id", saleID));
                    if (dtContractDel.Rows.Count > 0)
                    {
                        int contractID = Convert.ToInt32(dtContractDel.Rows[0]["ContractID"]);
                        DbHelper.ExecuteTrans(trans, "DELETE FROM InstallmentSchedules WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM InstallmentPayments WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM InstallmentContracts WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                    }

                    // عكس حركة الخزينة للدفعة المقدمة
                    DbHelper.ExecuteTrans(trans,
                        "DELETE FROM CashBox WHERE RefID=@id",
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
            DateTime? loadedLastModified = null, int? safeAccountID = null, decimal? cashPaid = null,
            int cratesOut = 0, int cratesIn = 0, decimal shippingCharge = 0m, string orderType = null, string tableNumber = null)
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
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                    decimal up = Convert.ToDecimal(r["UnitPrice"]);

                    if (!isDraft)
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"UPDATE Products
                              SET PendingQtyThreshold = PendingQtyThreshold + @qty
                              WHERE ProductID = @pid 
                                AND PendingSalePrice IS NOT NULL 
                                AND PendingQtyThreshold > 0
                                AND ABS(SalePrice - @up) < 0.005",
                            DbHelper.P("@qty", qty),
                            DbHelper.P("@pid", pid),
                            DbHelper.P("@up", up));
                    }

                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO SaleItemsHistory(AuditID, SaleID, ProductID, Quantity, UnitPrice, TotalPrice, DiscountPct, DiscountAmt, PriceTier)
                          VALUES(@aid, @sid, @pid, @qty, @up, @tp, @dpct, @damt, @pt)",
                        DbHelper.P("@aid", auditID),
                        DbHelper.P("@sid", saleID),
                        DbHelper.P("@pid", pid),
                        DbHelper.P("@qty", qty),
                        DbHelper.P("@up", up),
                        DbHelper.P("@tp", Convert.ToDecimal(r["TotalPrice"])),
                        DbHelper.P("@dpct", Convert.ToDecimal(r["DiscountPct"])),
                        DbHelper.P("@damt", Convert.ToDecimal(r["DiscountAmt"])),
                        DbHelper.P("@pt", r["PriceTier"] == DBNull.Value ? "قطاعي" : r["PriceTier"]));
                }

                // 4. عكس الحركات المالية والفوارغ السابقة (العملاء، الخزينة، المندوب، الأقفاص)
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM ClientCratesTransactions WHERE RefSaleID=@id",
                    DbHelper.P("@id", saleID));
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM ClientTransactions WHERE RefID=@id AND TransType IN ('Sale', 'Payment')",
                    DbHelper.P("@id", saleID));
                DbHelper.ExecuteTrans(trans,
                    "DELETE FROM CashBox WHERE RefID=@id AND TransType IN ('SaleIncome', 'ClientPayment')",
                    DbHelper.P("@id", saleID));
                if (oldTypeStr == "DriverLoad")
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

                string code = saleID.ToString();
                var dtCode = DbHelper.QueryTrans(trans, "SELECT SaleCode FROM Sales WHERE SaleID=@id", DbHelper.P("@id", saleID));
                if (dtCode.Rows.Count > 0) code = dtCode.Rows[0]["SaleCode"].ToString();

                // 6. تحديث رأس الفاتورة
                string typeStr = saleType == 0 ? "Credit" : saleType == 1 ? "DriverLoad" : "Cash";
                int targetWarehouse = warehouseID ?? 1;

                DbHelper.ExecuteTrans(trans,
                    @"UPDATE Sales 
                      SET SaleType=@typ, ClientID=@cid, DriverID=@did, TotalAmount=@tot, Notes=@n, 
                          DiscountAmount=@discAmt, DiscountPct=@discPct, IsPosted=@ip, WarehouseID=@wid, PriceTier=@pt,
                          CashPaid=@cp, CratesOut=@co, CratesIn=@ci, LastModifiedDate=GETDATE(), ShippingCharge=@shipping,
                          OrderType=@ot, TableNumber=@tn
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
                    DbHelper.P("@cp", cashPaid.HasValue ? (object)cashPaid.Value : DBNull.Value),
                    DbHelper.P("@co", cratesOut),
                    DbHelper.P("@ci", cratesIn),
                    DbHelper.P("@shipping", shippingCharge),
                    DbHelper.P("@ot", string.IsNullOrEmpty(orderType) ? DBNull.Value : (object)orderType),
                    DbHelper.P("@tn", string.IsNullOrEmpty(tableNumber) ? DBNull.Value : (object)tableNumber),
                    DbHelper.P("@id", saleID));

                // 7. إدخال البنود الجديدة
                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,PriceTier,UnitName,Factor,ExpiryDate,BatchID,IMEI,KitchenNotes) 
                          VALUES(@sid,@pid,@qty,@up,@tp,@dpct,@damt,@pt,@un,@fac,@exp,@bid,@imei,@kn)",
                        DbHelper.P("@sid", saleID), 
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), 
                        DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice), 
                        DbHelper.P("@dpct", item.DiscountPct),
                        DbHelper.P("@damt", item.DiscountAmt),
                        DbHelper.P("@pt", item.PriceTier ?? priceTier),
                        DbHelper.P("@un", item.UnitName),
                        DbHelper.P("@fac", item.Factor),
                        DbHelper.P("@exp", item.ExpiryDate.HasValue ? (object)item.ExpiryDate.Value : DBNull.Value),
                        DbHelper.P("@bid", item.BatchID.HasValue ? (object)item.BatchID.Value : DBNull.Value),
                        DbHelper.P("@imei", string.IsNullOrEmpty(item.IMEI) ? DBNull.Value : (object)item.IMEI),
                        DbHelper.P("@kn", string.IsNullOrEmpty(item.KitchenNotes) ? DBNull.Value : (object)item.KitchenNotes));

                    // check if price is different from product base price to log it in PriceChangesLog
                    if (!isDraft)
                    {
                        var basePriceObj = DbHelper.ScalarTrans(trans, "SELECT SalePrice FROM Products WHERE ProductID = @pid", DbHelper.P("@pid", item.ProductID));
                        if (basePriceObj != null && basePriceObj != DBNull.Value)
                        {
                            decimal basePrice = Convert.ToDecimal(basePriceObj);
                            if (Math.Abs(item.UnitPrice - basePrice) > 0.005m)
                            {
                                string changeNotes = $"بيع بسعر مختلف في فاتورة البيع #{code}";
                                DbHelper.ExecuteTrans(trans,
                                    @"INSERT INTO PriceChangesLog (ProductID, OldPrice, NewPrice, ChangeSource, SourceRefID, UserID, Notes)
                                      VALUES (@pid, @old, @new, 'SalesInvoice', @ref, @uid, @notes)",
                                    DbHelper.P("@pid", item.ProductID), DbHelper.P("@old", basePrice), DbHelper.P("@new", item.UnitPrice),
                                    DbHelper.P("@ref", saleID), DbHelper.P("@uid", Session.EmpID), DbHelper.P("@notes", changeNotes));
                            }
                        }

                        // Atomic update to subtract from PendingQtyThreshold if sold at old SalePrice
                        DbHelper.ExecuteTrans(trans,
                            @"UPDATE Products
                              SET PendingQtyThreshold = CASE 
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingQtyThreshold - @qty
                                  END,
                                  SalePrice = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN PendingSalePrice
                                  ELSE SalePrice
                                  END,
                                  PendingSalePrice = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingSalePrice
                                  END,
                                  PendingPriceSourceRefID = CASE
                                  WHEN PendingQtyThreshold - @qty <= 0 THEN NULL
                                  ELSE PendingPriceSourceRefID
                                  END
                              WHERE ProductID = @pid 
                                AND PendingSalePrice IS NOT NULL 
                                AND PendingQtyThreshold > 0 
                                AND @qty > 0
                                AND ABS(SalePrice - @up) < 0.005",
                            DbHelper.P("@qty", item.Quantity),
                            DbHelper.P("@pid", item.ProductID),
                            DbHelper.P("@up", item.UnitPrice));
                    }
                }

                // 8. إنشاء الحركات المالية والفوارغ الجديدة
                if (!isDraft)
                {
                    if (clientID.HasValue && (cratesOut > 0 || cratesIn > 0))
                    {
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO ClientCratesTransactions(ClientID,CratesOut,CratesIn,RefSaleID,Notes,CreatedBy) VALUES(@cid,@co,@ci,@ref,@n,@by)",
                            DbHelper.P("@cid", clientID.Value), DbHelper.P("@co", cratesOut), DbHelper.P("@ci", cratesIn),
                            DbHelper.P("@ref", saleID), DbHelper.P("@n", "تعديل حركة فوارغ فاتورة مبيعات " + code),
                            DbHelper.P("@by", Session.EmpID));
                    }

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
                        decimal actualPaid = cashPaid ?? total;
                        DbHelper.ExecuteTrans(trans,
                            "INSERT INTO CashBox(TransType,AmountIn,RefID,Notes,CreatedBy,AccountID) VALUES('SaleIncome',@amt,@ref,@n,@by,@accId)",
                            DbHelper.P("@amt", actualPaid), DbHelper.P("@ref", saleID),
                            DbHelper.P("@n", "تعديل بيع نقدي " + code), DbHelper.P("@by", Session.EmpID),
                            DbHelper.P("@accId", safeAccountID.HasValue ? (object)safeAccountID.Value : DBNull.Value));

                        // تسجيل الفاتورة وسداد العميل دائماً في ClientTransactions (يظهر في كشف الحساب)
                        if (clientID.HasValue)
                        {
                            // مدين: قيمة الفاتورة كاملة
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'Sale',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                                DbHelper.P("@ref", saleID), DbHelper.P("@n", "تعديل فاتورة بيع نقدي " + code),
                                DbHelper.P("@by", Session.EmpID));
                            // دائن: المبلغ المسدد فعلاً
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'Payment',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", actualPaid),
                                DbHelper.P("@ref", saleID), DbHelper.P("@n", "سداد تعديل فاتورة بيع نقدي " + code),
                                DbHelper.P("@by", Session.EmpID));
                        }
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
        public string KitchenNotes { get; set; } = "";
        public string IMEI { get; set; } = "";
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; } = 0m;
        public DateTime? ExpiryDate { get; set; } = null;
        public int? BatchID { get; set; } = null;
        public decimal StockQty { get; set; } = 0m;
        public decimal MinStockLimit { get; set; } = 0m;
        public string PartNumber { get; set; } = "";
        public string CarModel { get; set; } = "";
        public string Brand { get; set; } = "";
        public string ProductSize { get; set; } = "";
        public string Color { get; set; } = "";
        public string ShelfLocation { get; set; } = "";
        public string ProductCode { get; set; } = "";
        /// <summary>صنف خدمة — يُباع بالسالب بدون فحص المخزون</summary>
        public bool IsService { get; set; } = false;
        public string UnitName { get; set; } = null;
        public decimal Factor { get; set; } = 1.0m;
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
                               AND s.SaleType IN ('Cash', 'Credit', 'Installment')
                               AND s.SaleDate >= dl.LoadDate
                               AND si.ProductID = p.ProductID
                         ), 0) AS SoldQty
                  FROM DriverLoadItems dli
                  JOIN Products p ON dli.ProductID=p.ProductID
                  WHERE dli.LoadID=@lid",
                DbHelper.P("@lid", loadID));
        }

        public static int SaveHandover(int loadID, int driverID,
            List<HandoverItemDTO> items, string notes, decimal cashCollected,
            string settlementType = null, decimal deficitValue = 0m, string settlementNotes = null,
            string deadQtyHandling = "None")
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
                    @"INSERT INTO DriverHandovers(HandoverDate,LoadID,DriverID,TotalLoaded,TotalReturned,TotalDead,TotalExtra,TotalDeficit,Notes,CreatedBy,DeadQtyHandling)
                      VALUES(@dt,@lid,@did,@tl,@tr,@td,@te,@tdf,@n,@by,@dqh)",
                    DbHelper.P("@dt", closedAt), DbHelper.P("@lid", loadID), DbHelper.P("@did", driverID),
                    DbHelper.P("@tl", totLoaded), DbHelper.P("@tr", totRet), DbHelper.P("@td", totDead),
                    DbHelper.P("@te", totExtra), DbHelper.P("@tdf", totDef),
                    DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@dqh", deadQtyHandling));

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

                // 5. تسوية العجز المالي المباشرة داخل الـ Transaction لضمان سلامة البيانات
                if (deficitValue > 0.009m && !string.IsNullOrEmpty(settlementType) && settlementType != "Skip")
                {
                    if (settlementType == "Advance" || settlementType == "Deduction")
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO EmployeeTransactions(EmpID, TransType, Debit, RefID, Notes, CreatedBy)
                              VALUES(@eid, @type, @amt, @ref, @n, @by)",
                            DbHelper.P("@eid", driverID),
                            DbHelper.P("@type", settlementType == "Advance" ? "DeficitCharge" : "Deduction"),
                            DbHelper.P("@amt", deficitValue),
                            DbHelper.P("@ref", loadID),
                            DbHelper.P("@n", settlementNotes),
                            DbHelper.P("@by", Session.EmpID));
                    }
                    else if (settlementType == "CompanyExpense")
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO Expenses(ExpenseDate, ExpenseType, Amount, Notes, CreatedBy)
                              VALUES(GETDATE(), N'عجز حمولة مندوب', @amt, @n, @by)",
                            DbHelper.P("@amt", deficitValue),
                            DbHelper.P("@n", settlementNotes),
                            DbHelper.P("@by", Session.EmpID));
                    }
                }

                // 6. تسجيل قيمة النافق كمصروف للشركة في حال تم اختيار ذلك
                if (deadQtyHandling == "Company" && totDead > 0)
                {
                    decimal totalDeadValue = 0;
                    foreach (var i in items)
                    {
                        totalDeadValue += (i.DeadQty * i.UnitPrice);
                    }

                    if (totalDeadValue > 0)
                    {
                        string driverName = DbHelper.ScalarTrans(trans, "SELECT EmpName FROM Employees WHERE EmpID=@did", DbHelper.P("@did", driverID))?.ToString() ?? "";
                        DbHelper.ExecuteTrans(trans,
                            @"INSERT INTO Expenses(ExpenseDate, ExpenseType, Amount, Notes, CreatedBy)
                              VALUES(GETDATE(), N'نافق حمولة مندوب', @amt, @n, @by)",
                            DbHelper.P("@amt", totalDeadValue),
                            DbHelper.P("@n", $"نافق حمولة المندوب ({driverName}) - حمولة #{loadID} - جهة التحمل: الشركة"),
                            DbHelper.P("@by", Session.EmpID));
                    }
                }
            });
            // ===== نهاية Transaction الأساسي =====

            AppLogger.Audit("تقفيل حمولة مندوب", $"حمولة رقم (#{loadID}) | مندوب رقم ({driverID}) | تسليم عهدة (#{hvID}) | المبلغ المحصل: {cashCollected:N2} ج | نوع تسوية العجز: {settlementType}");

            return hvID;
        }

        public static DataTable GetHandovers(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT dh.HandoverID, dh.HandoverDate, e.EmpName AS DriverName,
                          dh.TotalLoaded, dh.TotalReturned, dh.TotalDead, dh.TotalExtra, dh.TotalDeficit,
                          dh.Notes, creator.EmpName AS CreatedBy
                  FROM DriverHandovers dh
                  JOIN Employees e ON dh.DriverID = e.EmpID
                  JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                  LEFT JOIN Employees creator ON dh.CreatedBy = creator.EmpID
                  WHERE CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                    AND (@warehouseID IS NULL OR dl.WarehouseID = @warehouseID)
                  ORDER BY dh.HandoverDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
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
                          AND s2.SaleType IN ('Cash','Credit','Installment')
                          AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                          AND s2.IsPosted = 1
                    ), 0)                                   AS SoldQty,
                    -- قيمة المبيعات
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType IN ('Cash','Credit','Installment')
                          AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                          AND s2.IsPosted = 1
                    ), 0)                                   AS SoldValue,
                    -- منها نقدي (محصل فعلاً)
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType = 'Cash'
                          AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                          AND s2.IsPosted = 1
                    ), 0)                                   AS CashCollected,
                    -- منها آجل (غير محصل)
                    ISNULL((
                        SELECT SUM(s2.TotalAmount)
                        FROM Sales s2
                        WHERE s2.DriverID = dl.DriverID
                          AND s2.SaleType IN ('Credit','Installment')
                          AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                          AND s2.IsPosted = 1
                    ), 0)                                   AS CreditSold,
                    -- الكميات المرتجعة من العملاء في نفس الفترة
                    ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                        JOIN Sales s2 ON sr.SaleID = s2.SaleID
                        WHERE s2.DriverID = dl.DriverID
                          AND CAST(sr.ReturnDate AS DATE) >= CAST(dl.LoadDate AS DATE)
                    ), 0)                                   AS ReturnedQty,
                    -- المتبقي بعهدته (محمل - مباع + مرتجع)
                    ISNULL((
                        SELECT SUM(dli.LoadedQty)
                        FROM DriverLoadItems dli WHERE dli.LoadID = dl.LoadID
                    ), 0)
                    - ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si JOIN Sales s2 ON si.SaleID=s2.SaleID
                        WHERE s2.DriverID=dl.DriverID AND s2.SaleType IN('Cash','Credit','Installment')
                          AND CAST(s2.SaleDate AS DATE) >= CAST(dl.LoadDate AS DATE) AND s2.IsPosted=1
                    ), 0)
                    + ISNULL((
                        SELECT SUM(ri.Quantity)
                        FROM ReturnItems ri
                        JOIN SalesReturns sr ON ri.ReturnID=sr.ReturnID
                        JOIN Sales s2 ON sr.SaleID=s2.SaleID
                        WHERE s2.DriverID=dl.DriverID AND CAST(sr.ReturnDate AS DATE) >= CAST(dl.LoadDate AS DATE)
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

            AppLogger.Audit("تسوية عجز حمولة", $"مندوب رقم ({driverID}) | حمولة رقم (#{loadID}) | قيمة العجز: {deficitValue:N2} ج | طريقة التسوية: {settlementType}");
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
                          AND SaleType IN ('Cash','Credit','Installment')
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
                          AND s.SaleType IN ('Cash','Credit','Installment')
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
        public static string BuildDriverExportJson(int? driverID = null)
        {
            // ── بيانات المالك (KPIs) ──
            string storeName = "";
            try {
                var dtStore = DbHelper.Query("SELECT TOP 1 SettingValue FROM AppSettings WHERE SettingKey = 'StoreName'");
                if (dtStore.Rows.Count > 0) storeName = dtStore.Rows[0][0]?.ToString() ?? "";
            } catch {}

            decimal todaySalesTotal = 0, todayCashSales = 0, todayCreditSales = 0, todayNetProfit = 0;
            decimal cashboxBalance = 0, clientDebts = 0, supplierDebts = 0, todayPurchases = 0;
            int lowStockCount = 0;
            try {
                object o;
                o = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                if (o != null && o != DBNull.Value) todaySalesTotal = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND ISNULL(SaleType,'') = 'Cash'");
                if (o != null && o != DBNull.Value) todayCashSales = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND ISNULL(SaleType,'') <> 'Cash'");
                if (o != null && o != DBNull.Value) todayCreditSales = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(si.TotalPrice - (si.Quantity * ISNULL(p.PurchasePrice, 0))), 0) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID JOIN Products p ON si.ProductID = p.ProductID WHERE CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                if (o != null && o != DBNull.Value) todayNetProfit = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(AmountIn,0) - ISNULL(AmountOut,0)), 0) FROM CashBox");
                if (o != null && o != DBNull.Value) cashboxBalance = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Clients WHERE Balance > 0");
                if (o != null && o != DBNull.Value) clientDebts = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(Balance),0) FROM Suppliers WHERE Balance > 0");
                if (o != null && o != DBNull.Value) supplierDebts = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Purchases WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                if (o != null && o != DBNull.Value) todayPurchases = Convert.ToDecimal(o);

                o = DbHelper.Scalar("SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND Quantity <= ISNULL(MinQuantity, 5)");
                if (o != null && o != DBNull.Value) lowStockCount = Convert.ToInt32(o);
            } catch {}

            // ── بيانات المندوبين ──
            System.Data.DataTable clients;
            if (driverID.HasValue && driverID.Value > 0)
            {
                clients = DbHelper.Query(
                    "SELECT ClientID, ClientName, ISNULL(Phone,'') AS Phone, DriverID FROM Clients WHERE IsActive=1 AND DriverID = @did ORDER BY ClientName",
                    DbHelper.P("@did", driverID.Value));
            }
            else
            {
                clients = DbHelper.Query(
                    "SELECT ClientID, ClientName, ISNULL(Phone,'') AS Phone, DriverID FROM Clients WHERE IsActive=1 ORDER BY ClientName");
            }

            var products = DbHelper.Query(
                "SELECT ProductID, ProductName, SalePrice, ISNULL(Unit,'وحدة') AS Unit FROM Products WHERE IsActive=1 ORDER BY ProductName");

            System.Data.DataTable drivers;
            if (driverID.HasValue && driverID.Value > 0)
            {
                drivers = DbHelper.Query(
                    "SELECT EmpID, EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 AND EmpID = @did ORDER BY EmpName",
                    DbHelper.P("@did", driverID.Value));
            }
            else
            {
                drivers = DbHelper.Query(
                    "SELECT EmpID, EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 ORDER BY EmpName");
            }

            System.Data.DataTable loads;
            if (driverID.HasValue && driverID.Value > 0)
            {
                loads = DbHelper.Query(@"
                    SELECT dl.DriverID, dli.ProductID, SUM(dli.LoadedQty) AS LoadedQty
                    FROM DriverLoads dl
                    JOIN DriverLoadItems dli ON dl.LoadID = dli.LoadID
                    WHERE dl.IsClosed = 0 AND dl.DriverID = @did
                    GROUP BY dl.DriverID, dli.ProductID",
                    DbHelper.P("@did", driverID.Value));
            }
            else
            {
                loads = DbHelper.Query(@"
                    SELECT dl.DriverID, dli.ProductID, SUM(dli.LoadedQty) AS LoadedQty
                    FROM DriverLoads dl
                    JOIN DriverLoadItems dli ON dl.LoadID = dli.LoadID
                    WHERE dl.IsClosed = 0
                    GROUP BY dl.DriverID, dli.ProductID");
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("{");

            // ── KPIs الرئيسية للمالك ──
            sb.AppendFormat("\"StoreName\":\"{0}\",", EscapeJson(storeName));
            sb.AppendFormat("\"SyncTime\":\"{0}\",", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendFormat("\"TodaySalesTotal\":{0},", todaySalesTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"TodayCashSales\":{0},", todayCashSales.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"TodayCreditSales\":{0},", todayCreditSales.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"TodayNetProfit\":{0},", todayNetProfit.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"CashboxBalance\":{0},", cashboxBalance.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"ClientDebts\":{0},", clientDebts.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"SupplierDebts\":{0},", supplierDebts.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"TodayPurchases\":{0},", todayPurchases.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat("\"LowStockCount\":{0},", lowStockCount);

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

            if (driverID.HasValue && driverID.Value > 0 && drivers.Rows.Count > 0)
            {
                sb.AppendFormat("\"targetDriverId\":{0},\"targetDriverName\":\"{1}\",",
                    drivers.Rows[0]["EmpID"],
                    EscapeJson(drivers.Rows[0]["EmpName"].ToString()));
            }

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
            DateTime saleDate, string notes, List<SaleItemDTO> items, long? cloudID = null)
        {
            if (items == null || items.Count == 0) return -1;
            decimal total = 0;
            foreach (var it in items) total += it.Quantity * it.UnitPrice;

            int returnedID = -1;
            DbHelper.RunInTransaction((con, trans) =>
            {
                // Resolve warehouse from driver's open load
                int targetWarehouse = 1;
                object activeLoadWh = DbHelper.ScalarTrans(trans,
                    "SELECT TOP 1 WarehouseID FROM DriverLoads WHERE DriverID = @did AND IsClosed = 0 ORDER BY LoadDate DESC",
                    DbHelper.P("@did", driverID));
                if (activeLoadWh != null && activeLoadWh != DBNull.Value)
                {
                    targetWarehouse = Convert.ToInt32(activeLoadWh);
                }

                // Check for existing cloud import (idempotency)
                if (cloudID.HasValue && cloudID.Value > 0)
                {
                    object existing = DbHelper.ScalarTrans(trans, "SELECT SaleID FROM Sales WHERE CloudID = @cloudID", DbHelper.P("@cloudID", cloudID.Value));
                    if (existing != null && existing != DBNull.Value)
                    {
                        int saleID = Convert.ToInt32(existing);
                        
                        // Delete old transactions and items so we can overwrite/update them
                        DbHelper.ExecuteTrans(trans, "DELETE FROM SaleItems WHERE SaleID = @sid", DbHelper.P("@sid", saleID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM ClientTransactions WHERE RefID = @sid AND TransType = 'Sale'", DbHelper.P("@sid", saleID));
                        DbHelper.ExecuteTrans(trans, "DELETE FROM CashBox WHERE RefID = @sid AND TransType = 'DriverSaleImport'", DbHelper.P("@sid", saleID));
                        
                        // Update main invoice header
                        DbHelper.ExecuteTrans(trans,
                            "UPDATE Sales SET SaleDate=@dt, SaleType=@typ, ClientID=@cid, DriverID=@did, TotalAmount=@tot, Notes=@n, WarehouseID=@wid, LastModifiedDate=GETDATE() WHERE SaleID=@sid",
                            DbHelper.P("@dt", saleDate),
                            DbHelper.P("@typ", paymentType == "Cash" ? "Cash" : "Credit"),
                            DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                            DbHelper.P("@did", driverID > 0 ? (object)driverID : DBNull.Value),
                            DbHelper.P("@tot", total),
                            DbHelper.P("@n", notes ?? "استيراد مبيعات مندوب (محدث)"),
                            DbHelper.P("@wid", targetWarehouse),
                            DbHelper.P("@sid", saleID));

                        returnedID = saleID;
                    }
                }

                string code = "1";
                if (returnedID <= 0)
                {
                    var nextResult = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID), 0) + 1 FROM Sales");
                    code = nextResult?.ToString() ?? "1";

                    int saleID = DbHelper.ExecuteInsertTrans(trans,
                        "INSERT INTO Sales(SaleCode,SaleDate,SaleType,ClientID,DriverID,TotalAmount,Notes,CreatedBy,DiscountAmount,DiscountPct,IsPosted,WarehouseID,CloudID,LastModifiedDate) " +
                        "VALUES(@code,@dt,@typ,@cid,@did,@tot,@n,@by,0,0,1,@wid,@cloud,GETDATE())",
                        DbHelper.P("@code", code),
                        DbHelper.P("@dt", saleDate),
                        DbHelper.P("@typ", paymentType == "Cash" ? "Cash" : "Credit"),
                        DbHelper.P("@cid", clientID > 0 ? (object)clientID : DBNull.Value),
                        DbHelper.P("@did", driverID > 0 ? (object)driverID : DBNull.Value),
                        DbHelper.P("@tot", total),
                        DbHelper.P("@n", notes ?? "استيراد مبيعات مندوب"),
                        DbHelper.P("@by", Session.EmpID),
                        DbHelper.P("@wid", targetWarehouse),
                        DbHelper.P("@cloud", cloudID.HasValue ? (object)cloudID.Value : DBNull.Value));

                    if (saleID <= 0) throw new Exception("فشل في إنشاء الفاتورة المستوردة.");
                    returnedID = saleID;
                }
                else
                {
                    // If we updated, let's load the existing code for audit/logging or transactions
                    object existingCode = DbHelper.ScalarTrans(trans, "SELECT SaleCode FROM Sales WHERE SaleID = @sid", DbHelper.P("@sid", returnedID));
                    if (existingCode != null && existingCode != DBNull.Value) code = existingCode.ToString();
                }

                foreach (var it in items)
                {
                    decimal lineTotal = it.Quantity * it.UnitPrice;
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,DiscountPct,DiscountAmt,UnitName,Factor) " +
                        "VALUES(@sid,@pid,@qty,@up,@tot,0,0,@un,@fac)",
                        DbHelper.P("@sid", returnedID),
                        DbHelper.P("@pid", it.ProductID),
                        DbHelper.P("@qty", it.Quantity),
                        DbHelper.P("@up", it.UnitPrice),
                        DbHelper.P("@tot", lineTotal),
                        DbHelper.P("@un", it.UnitName),
                        DbHelper.P("@fac", it.Factor));
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
                        DbHelper.P("@ref", returnedID),
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
                        DbHelper.P("@ref", returnedID),
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
        public string DeadQtyHandling { get; set; } = "Driver"; // "Driver", "Company", "None"

        public decimal DeficitQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty;
                if (DeadQtyHandling == "Company" || DeadQtyHandling == "None")
                {
                    expected -= DeadQty;
                }
                return expected > SoldQty ? expected - SoldQty : 0;
            }
        }

        public decimal ExtraQty
        {
            get
            {
                decimal expected = LoadedQty - ReturnedQty;
                if (DeadQtyHandling == "Company" || DeadQtyHandling == "None")
                {
                    expected -= DeadQty;
                }
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
        public static DataTable GetAll(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT sr.ReturnID, sr.ReturnDate,
                          ISNULL(s.SaleCode, N'مرتجع عام') AS SaleCode,
                          ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                          sr.TotalAmount, sr.Notes
                  FROM SalesReturns sr
                  LEFT JOIN Sales s ON sr.SaleID=s.SaleID
                  LEFT JOIN Clients c ON sr.ClientID=c.ClientID
                  WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                    AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)
                  ORDER BY sr.ReturnDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static int SaveReturn(int saleID, int? clientID, decimal total, string notes, List<SaleItemDTO> items, int? warehouseID = null, string returnType = "Credit")
        {
            int returnedRetID = -1;

            DbHelper.RunInTransaction((con, trans) =>
            {
                string saleType = returnType;
                int whID = warehouseID ?? 1;

                if (saleID > 0)
                {
                    var dtSale = DbHelper.QueryTrans(trans, "SELECT SaleType, ClientID, WarehouseID FROM Sales WHERE SaleID=@sid", DbHelper.P("@sid", saleID));
                    if (dtSale.Rows.Count > 0)
                    {
                        saleType = dtSale.Rows[0]["SaleType"].ToString();
                        if (dtSale.Rows[0]["WarehouseID"] != DBNull.Value)
                            whID = Convert.ToInt32(dtSale.Rows[0]["WarehouseID"]);
                        if (!clientID.HasValue && dtSale.Rows[0]["ClientID"] != DBNull.Value)
                            clientID = Convert.ToInt32(dtSale.Rows[0]["ClientID"]);
                    }
                }

                int retID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO SalesReturns(ReturnDate,SaleID,ClientID,TotalAmount,Notes,CreatedBy,WarehouseID,ReturnType) VALUES(@dt,@sid,@cid,@tot,@n,@by,@wid,@rtyp)",
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@sid", saleID > 0 ? (object)saleID : DBNull.Value),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@tot", total), DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@wid", whID),
                    DbHelper.P("@rtyp", saleID > 0 ? "InvoiceReturn" : "GeneralReturn"));

                if (retID <= 0) throw new Exception("فشل إنشاء سجل المرتجع.");

                returnedRetID = retID;

                foreach (var item in items)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice,UnitName,Factor) VALUES(@rid,@pid,@qty,@up,@tp,@un,@fac)",
                        DbHelper.P("@rid", retID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice),
                        DbHelper.P("@un", item.UnitName),
                        DbHelper.P("@fac", item.Factor));
                }

                // المنطق المحاسبي السليم:
                // بيع نقدي → رد نقدي من الخزنة (AmountOut)
                // بيع آجل أو حمولة مندوب → تخفيض دين العميل (Credit في ClientTransactions)
                if (saleType == "Cash")
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy) VALUES(@dt,'ReturnOutcome',@amt,@ref,@n,@by)",
                        DbHelper.P("@dt", DateTime.Now),
                        DbHelper.P("@amt", total), DbHelper.P("@ref", retID),
                        DbHelper.P("@n", saleID > 0 ? ("مرتجع بيع للفاتورة رقم " + saleID) : ("مرتجع بيع عام نقدي " + (notes ?? ""))),
                        DbHelper.P("@by", Session.EmpID));
                }
                else if (clientID.HasValue)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'Return',@amt,@ref,@n,@by)",
                        DbHelper.P("@cid", clientID.Value), DbHelper.P("@amt", total),
                        DbHelper.P("@ref", retID), DbHelper.P("@n", saleID > 0 ? ("مرتجع بيع للفاتورة رقم " + saleID) : ("مرتجع بيع عام آجل " + (notes ?? ""))),
                        DbHelper.P("@by", Session.EmpID));
                }

                // معالجة مرتجع التقسيط
                if (saleID > 0 && saleType == "Installment")
                {
                    InstallmentDAL.HandleSalesReturn(trans, saleID, total);
                }
            });

            return returnedRetID;
        }

        /// <summary>
        /// استبدال أصناف: إرجاع بضاعة واستلام بضاعة جديدة في نفس الحركة وتسوية الفرق مالياً ومخزنياً
        /// </summary>
        public static bool SaveItemExchange(int? clientID, int warehouseID, List<SaleItemDTO> returnedItems, List<SaleItemDTO> newItems, string paymentType, string notes)
        {
            if (returnedItems == null || returnedItems.Count == 0)
                throw new Exception("يجب تحديد صنف واحد على الأقل للمرتجع!");
            if (newItems == null || newItems.Count == 0)
                throw new Exception("يجب تحديد صنف جديد واحد على الأقل للبديل!");

            decimal totalReturned = 0m;
            foreach (var item in returnedItems) totalReturned += item.TotalPrice;

            decimal totalNew = 0m;
            foreach (var item in newItems) totalNew += item.TotalPrice;

            decimal netDiff = totalNew - totalReturned;

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. تسجيل حركة المرتجع
                int retID = DbHelper.ExecuteInsertTrans(trans,
                    "INSERT INTO SalesReturns(ReturnDate,SaleID,ClientID,TotalAmount,Notes,CreatedBy,WarehouseID,ReturnType) VALUES(@dt,NULL,@cid,@tot,@n,@by,@wid,N'ExchangeReturn')",
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@tot", totalReturned),
                    DbHelper.P("@n", "استبدال أصناف (مرتجع) - " + notes),
                    DbHelper.P("@by", Session.EmpID),
                    DbHelper.P("@wid", warehouseID));

                foreach (var item in returnedItems)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ReturnItems(ReturnID,ProductID,Quantity,UnitPrice,TotalPrice,UnitName,Factor) VALUES(@rid,@pid,@qty,@up,@tp,@un,@fac)",
                        DbHelper.P("@rid", retID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice),
                        DbHelper.P("@un", item.UnitName),
                        DbHelper.P("@fac", item.Factor));
                }

                // 2. تسجيل حركة الصرف الجديدة (فاتورة بيع بديل)
                var nextSaleCodeObj = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(SaleID),0)+1 FROM Sales");
                string saleCode = "EXC-" + (nextSaleCodeObj ?? "1");

                int saleID = DbHelper.ExecuteInsertTrans(trans,
                    @"INSERT INTO Sales (SaleCode, SaleDate, SaleType, ClientID, WarehouseID, TotalAmount, DiscountAmount, DiscountPct, TaxPct, TaxAmount, ShippingCharge, CashPaid, Notes, CreatedBy, IsPosted)
                      VALUES (@code, GETDATE(), @stype, @cid, @wid, @tot, 0, 0, 0, 0, 0, @cash, @n, @by, 1)",
                    DbHelper.P("@code", saleCode),
                    DbHelper.P("@stype", paymentType),
                    DbHelper.P("@cid", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                    DbHelper.P("@wid", warehouseID),
                    DbHelper.P("@tot", totalNew),
                    DbHelper.P("@cash", paymentType == "Cash" ? totalNew : 0m),
                    DbHelper.P("@n", "استبدال أصناف (صرف بديل) - " + notes),
                    DbHelper.P("@by", Session.EmpID));

                foreach (var item in newItems)
                {
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO SaleItems(SaleID,ProductID,Quantity,UnitPrice,TotalPrice,UnitName,Factor) VALUES(@sid,@pid,@qty,@up,@tp,@un,@fac)",
                        DbHelper.P("@sid", saleID), DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@qty", item.Quantity), DbHelper.P("@up", item.UnitPrice),
                        DbHelper.P("@tp", item.TotalPrice),
                        DbHelper.P("@un", item.UnitName),
                        DbHelper.P("@fac", item.Factor));
                }

                // 3. التسوية المالية للفرق الصافي
                if (netDiff != 0m)
                {
                    if (paymentType == "Cash")
                    {
                        if (netDiff > 0)
                        {
                            // العميل دفع الفارق نقداً (إيراد للخزنة)
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO CashBox(TransDate,TransType,AmountIn,RefID,Notes,CreatedBy) VALUES(@dt,'ExchangeDiffIn',@amt,@ref,@n,@by)",
                                DbHelper.P("@dt", DateTime.Now),
                                DbHelper.P("@amt", netDiff),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@n", "تحصيل فارق استبدال أصناف (عميل)"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                        else
                        {
                            // تم إرجاع الفارق للعميل نقداً (خروج من الخزنة)
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy) VALUES(@dt,'ExchangeDiffOut',@amt,@ref,@n,@by)",
                                DbHelper.P("@dt", DateTime.Now),
                                DbHelper.P("@amt", Math.Abs(netDiff)),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@n", "رد فارق استبدال أصناف للعميل نقداً"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                    }
                    else if (clientID.HasValue)
                    {
                        if (netDiff > 0)
                        {
                            // إضافة فرق المدينية على حساب العميل (Debit)
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Debit,RefID,Notes,CreatedBy) VALUES(@cid,'ExchangeDiff',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value),
                                DbHelper.P("@amt", netDiff),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@n", "فارق استبدال أصناف (زيادة مديونية)"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                        else
                        {
                            // خصم الفارق من حساب العميل (Credit)
                            DbHelper.ExecuteTrans(trans,
                                "INSERT INTO ClientTransactions(ClientID,TransType,Credit,RefID,Notes,CreatedBy) VALUES(@cid,'ExchangeDiff',@amt,@ref,@n,@by)",
                                DbHelper.P("@cid", clientID.Value),
                                DbHelper.P("@amt", Math.Abs(netDiff)),
                                DbHelper.P("@ref", saleID),
                                DbHelper.P("@n", "فارق استبدال أصناف (خصم مديونية)"),
                                DbHelper.P("@by", Session.EmpID));
                        }
                    }
                }
            });

            return true;
        }
    }

    public static class ReportDAL
    {
        public static DataTable SalesByDay(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @";WITH SaleCosts AS (
                    SELECT s.SaleID,
                           s.SaleDate,
                           s.SaleType,
                           s.TotalAmount,
                           s.WarehouseID,
                           ISNULL(SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) AS SaleCost
                    FROM Sales s
                    LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                    LEFT JOIN Products p ON si.ProductID = p.ProductID
                    WHERE s.IsPosted = 1
                    GROUP BY s.SaleID, s.SaleDate, s.SaleType, s.TotalAmount, s.WarehouseID
                )
                SELECT 
                    CAST(SaleDate AS DATE) AS SaleDay,
                    COUNT(*) AS Count,
                    SUM(CASE WHEN SaleType = 'Cash' THEN TotalAmount ELSE 0 END) AS CashTotal,
                    SUM(CASE WHEN SaleType = 'Credit' OR SaleType = 'Installment' THEN TotalAmount ELSE 0 END) AS CreditTotal,
                    SUM(CASE WHEN SaleType = 'DriverLoad' THEN TotalAmount ELSE 0 END) AS LoadTotal,
                    SUM(TotalAmount) AS Total,
                    SUM(SaleCost) AS TotalCost,
                    SUM(TotalAmount) - SUM(SaleCost) AS NetProfit
                FROM SaleCosts
                WHERE CAST(SaleDate AS DATE) BETWEEN @f AND @t
                  AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)
                GROUP BY CAST(SaleDate AS DATE)
                ORDER BY SaleDay",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable SalesByDriver(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @";WITH SaleCosts AS (
                    SELECT s.SaleID,
                           s.DriverID,
                           s.SaleDate,
                           s.SaleType,
                           s.TotalAmount,
                           s.WarehouseID,
                           ISNULL(SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) AS SaleCost
                    FROM Sales s
                    LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                    LEFT JOIN Products p ON si.ProductID = p.ProductID
                    WHERE s.IsPosted = 1
                    GROUP BY s.SaleID, s.DriverID, s.SaleDate, s.SaleType, s.TotalAmount, s.WarehouseID
                )
                SELECT 
                    ISNULL(e.EmpName, N'مبيعات مباشرة') AS DriverName,
                    COUNT(s.SaleID) AS Count,
                    SUM(CASE WHEN s.SaleType = 'Cash' THEN s.TotalAmount ELSE 0 END) AS CashTotal,
                    SUM(CASE WHEN s.SaleType = 'Credit' OR s.SaleType = 'Installment' THEN s.TotalAmount ELSE 0 END) AS CreditTotal,
                    SUM(s.TotalAmount) AS Total,
                    SUM(s.SaleCost) AS TotalCost,
                    SUM(s.TotalAmount) - SUM(s.SaleCost) AS NetProfit
                FROM SaleCosts s 
                LEFT JOIN Employees e ON s.DriverID = e.EmpID
                WHERE CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                  AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                GROUP BY e.EmpName
                ORDER BY Total DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable SalesByClient(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @";WITH ClientSaleCosts AS (
                    SELECT s.ClientID,
                           SUM(s.TotalAmount) AS TotalSales,
                           SUM(CASE WHEN s.SaleType = 'Cash' THEN s.TotalAmount ELSE 0 END) AS CashTotal,
                           SUM(CASE WHEN s.SaleType = 'Credit' OR s.SaleType = 'Installment' THEN s.TotalAmount ELSE 0 END) AS CreditTotal,
                           ISNULL(SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) AS SalesCost,
                           COUNT(DISTINCT s.SaleID) AS SaleCount
                    FROM Sales s
                    LEFT JOIN SaleItems si ON s.SaleID = si.SaleID
                    LEFT JOIN Products p ON si.ProductID = p.ProductID
                    WHERE s.IsPosted = 1
                      AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                    GROUP BY s.ClientID
                ),
                ClientReturnCosts AS (
                    SELECT sr.ClientID,
                           SUM(sr.TotalAmount) AS TotalReturns,
                           ISNULL(SUM(ri.Quantity * ISNULL(ri.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))), 0) AS ReturnsCost
                    FROM SalesReturns sr
                    LEFT JOIN ReturnItems ri ON sr.ReturnID = ri.ReturnID
                    LEFT JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)
                    GROUP BY sr.ClientID
                )
                SELECT 
                    c.ClientName,
                    ISNULL(c.Phone, N'---') AS Phone,
                    ISNULL(sc.SaleCount, 0) AS Count,
                    ISNULL(sc.CashTotal, 0) AS CashTotal,
                    ISNULL(sc.CreditTotal, 0) AS CreditTotal,
                    ISNULL(rc.TotalReturns, 0) AS ReturnsTotal,
                    ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID AND ct.TransType = 'Payment' AND CAST(ct.TransDate AS DATE) BETWEEN @f AND @t), 0) AS PaidTotal,
                    ISNULL(sc.TotalSales, 0) AS Total,
                    (c.OpeningBalance + 
                     ISNULL((SELECT SUM(ct.Debit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) - 
                     ISNULL((SELECT SUM(ct.Credit) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)
                    ) AS CurrentBalance,
                    (ISNULL(sc.SalesCost, 0) - ISNULL(rc.ReturnsCost, 0)) AS TotalCost,
                    (ISNULL(sc.TotalSales, 0) - ISNULL(rc.TotalReturns, 0)) - (ISNULL(sc.SalesCost, 0) - ISNULL(rc.ReturnsCost, 0)) AS NetProfit
                FROM Clients c
                LEFT JOIN ClientSaleCosts sc ON c.ClientID = sc.ClientID
                LEFT JOIN ClientReturnCosts rc ON c.ClientID = rc.ClientID
                WHERE (sc.TotalSales > 0 OR rc.TotalReturns > 0 OR EXISTS (SELECT 1 FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID AND ct.TransType = 'Payment' AND CAST(ct.TransDate AS DATE) BETWEEN @f AND @t))
                ORDER BY Total DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable SalesByProduct(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @";WITH SaleTotals AS (
                    SELECT si.ProductID,
                           AVG(si.UnitPrice) AS AvgPrice,
                           SUM(si.Quantity) AS TotalQty,
                           SUM(si.TotalPrice) AS TotalAmount,
                           SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))) AS TotalCost
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    JOIN Products p ON si.ProductID = p.ProductID
                    WHERE s.IsPosted = 1
                      AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                    GROUP BY si.ProductID
                ),
                ReturnTotals AS (
                    SELECT ri.ProductID,
                           SUM(ri.Quantity) AS ReturnedQty,
                           SUM(ri.TotalPrice) AS ReturnedAmount,
                           SUM(ri.Quantity * ISNULL(ri.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))) AS ReturnedCost
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    JOIN Products p ON ri.ProductID = p.ProductID
                    WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)
                    GROUP BY ri.ProductID
                ),
                StockTotals AS (
                    SELECT ProductID, ISNULL(SUM(Quantity), 0.0) AS CurrentStock
                    FROM ProductStock
                    WHERE (@warehouseID IS NULL OR WarehouseID = @warehouseID)
                    GROUP BY ProductID
                )
                SELECT 
                    p.ProductName,
                    p.Unit,
                    ISNULL(stk.CurrentStock, 0.0) AS CurrentStock,
                    ISNULL(st.AvgPrice, 0.0) AS AvgPrice,
                    ISNULL(st.TotalQty, 0.0) AS TotalQty,
                    ISNULL(st.TotalAmount, 0.0) AS TotalAmount,
                    ISNULL(rt.ReturnedQty, 0.0) AS ReturnedQty,
                    ISNULL(rt.ReturnedAmount, 0.0) AS ReturnedAmount,
                    (ISNULL(st.TotalQty, 0.0) - ISNULL(rt.ReturnedQty, 0.0)) AS NetQty,
                    (ISNULL(st.TotalAmount, 0.0) - ISNULL(rt.ReturnedAmount, 0.0)) AS NetAmount,
                    (ISNULL(st.TotalCost, 0.0) - ISNULL(rt.ReturnedCost, 0.0)) AS TotalCost,
                    ((ISNULL(st.TotalAmount, 0.0) - ISNULL(rt.ReturnedAmount, 0.0)) - (ISNULL(st.TotalCost, 0.0) - ISNULL(rt.ReturnedCost, 0.0))) AS NetProfit
                FROM Products p
                LEFT JOIN SaleTotals st ON p.ProductID = st.ProductID
                LEFT JOIN ReturnTotals rt ON p.ProductID = rt.ProductID
                LEFT JOIN StockTotals stk ON p.ProductID = stk.ProductID
                WHERE (st.TotalQty > 0 OR rt.ReturnedQty > 0)
                ORDER BY NetQty DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetInventoryValuation(int? warehouseID = null)
        {
            return DbHelper.Query(@"
                SELECT 
                    v.ProductCode,
                    v.ProductName,
                    v.Unit,
                    p.PurchasePrice,
                    v.SalePrice,
                    SUM(v.CurrentQty) AS CurrentStock,
                    SUM(v.CurrentQty * (ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit2Factor * p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), 1.0))) AS StockValue,
                    SUM(v.CurrentQty * (ISNULL(v.SalePrice, 0.0) / COALESCE(NULLIF(p.Unit2Factor * p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), 1.0))) AS StockSaleValue,
                    SUM(v.CurrentQty * ((ISNULL(v.SalePrice, 0.0) - ISNULL(p.PurchasePrice, 0.0)) / COALESCE(NULLIF(p.Unit2Factor * p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), 1.0))) AS ExpectedProfit
                FROM vw_CurrentStockByWarehouse v
                JOIN Products p ON v.ProductID = p.ProductID
                WHERE (@warehouseID IS NULL OR v.WarehouseID = @warehouseID)
                GROUP BY v.ProductCode, v.ProductName, v.Unit, p.PurchasePrice, v.SalePrice
                ORDER BY v.ProductName",
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetIncomeStatement(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(@"
                SELECT 
                    -- 1. المبيعات
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND CAST(SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS GrossSales,
                    ISNULL((SELECT SUM(TotalAmount) FROM SalesReturns WHERE CAST(ReturnDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS SalesReturns,
                    
                    -- 2. تكلفة المبيعات
                    ISNULL((SELECT SUM(si.Quantity * ISNULL(si.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID JOIN Products p ON si.ProductID = p.ProductID WHERE s.IsPosted = 1 AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)), 0) AS GrossCOGS,
                    ISNULL((SELECT SUM(ri.Quantity * ISNULL(ri.Factor, 1.0) * COALESCE(NULLIF(p.Unit1PurchasePrice, 0), ISNULL(p.PurchasePrice, 0.0) / COALESCE(NULLIF(p.Unit3Factor * p.Unit2Factor, 0), NULLIF(p.Unit3Factor, 0), NULLIF(p.Unit2Factor, 0), 1.0))) FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID JOIN Products p ON ri.ProductID = p.ProductID WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)), 0) AS ReturnsCOGS,
                    
                    -- 3. المصروفات
                    ISNULL((SELECT SUM(Amount) FROM Expenses WHERE CAST(ExpenseDate AS DATE) BETWEEN @f AND @t), 0) AS GeneralExpenses,
                    
                    -- 4. الهالك والنافق
                    -- أ. هالك مخزن
                    ISNULL((SELECT SUM(wi.TotalCost) FROM WastageLossItems wi JOIN WastageLoss w ON wi.WastageID = w.WastageID WHERE CAST(w.WastageDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR w.WarehouseID = @warehouseID)), 0) AS WarehouseWastage,
                    -- ب. نافق مندوب
                    ISNULL((SELECT SUM(hi.DeadQty * dli.UnitPrice) FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID JOIN DriverLoadItems dli ON dh.LoadID = dli.LoadID AND hi.ProductID = dli.ProductID WHERE hi.DeadQty > 0 AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR (SELECT dl.WarehouseID FROM DriverLoads dl WHERE dl.LoadID = dh.LoadID) = @warehouseID)), 0) AS DriverWastage",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        /// <summary>كميات مبيعات كل عميل لكل صنف في يوم معين (للتقرير اليومي المحوري) — مخصوماً منها المرتجعات</summary>
        public static DataTable GetDailyClientProductSales(DateTime date, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT
                    ISNULL(c.ClientID, 0) AS ClientID,
                    ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                    t.ProductID,
                    SUM(t.Qty) AS TotalQty,
                    MAX(t.UnitPrice) AS UnitPrice
                  FROM (
                      SELECT s.ClientID, si.ProductID, si.Quantity AS Qty, si.UnitPrice
                      FROM SaleItems si
                      JOIN Sales s ON si.SaleID = s.SaleID
                      WHERE CAST(s.SaleDate AS DATE) = @date
                        AND s.IsPosted = 1
                        AND s.SaleType IN ('Cash','Credit','Installment')
                        AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                      
                      UNION ALL
                      
                      SELECT sr.ClientID, ri.ProductID, -ri.Quantity AS Qty, ri.UnitPrice
                      FROM ReturnItems ri
                      JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                      WHERE CAST(sr.ReturnDate AS DATE) = @date
                        AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)
                  ) t
                  LEFT JOIN Clients c ON ISNULL(t.ClientID, 0) = c.ClientID
                  GROUP BY ISNULL(c.ClientID, 0), ISNULL(c.ClientName, N'عميل نقدي / عام'), t.ProductID
                  ORDER BY ClientName",
                DbHelper.P("@date", date.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        /// <summary>إجمالي الفاتورة وآخر توريد والمديونية لكل عميل في يوم معين — مخصوماً منها المرتجعات</summary>
        public static DataTable GetDailyClientTotals(DateTime date, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT
                    ISNULL(c.ClientID, 0) AS ClientID,
                    ISNULL(c.ClientName, N'عميل نقدي / عام') AS ClientName,
                    SUM(t.Amt) AS TotalInvoice,
                    ISNULL((
                        SELECT TOP 1 ct.Credit
                        FROM ClientTransactions ct
                        WHERE ct.ClientID = ISNULL(c.ClientID, 0)
                          AND ct.TransType = 'Payment'
                        ORDER BY ct.TransDate DESC
                    ), 0) AS LastPayment,
                    ISNULL(cb.Balance, ISNULL(c.OpeningBalance, 0)) AS Balance
                  FROM (
                      SELECT ClientID, TotalAmount AS Amt
                      FROM Sales
                      WHERE CAST(SaleDate AS DATE) = @date
                        AND IsPosted = 1
                        AND SaleType IN ('Cash','Credit','Installment')
                        AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)

                      UNION ALL

                      SELECT ClientID, -TotalAmount AS Amt
                      FROM SalesReturns
                      WHERE CAST(ReturnDate AS DATE) = @date
                        AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)
                  ) t
                  LEFT JOIN Clients c ON ISNULL(t.ClientID, 0) = c.ClientID
                  LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                  GROUP BY ISNULL(c.ClientID, 0), ISNULL(c.ClientName, N'عميل نقدي / عام'), c.OpeningBalance, cb.Balance
                  ORDER BY ClientName",
                DbHelper.P("@date", date.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetFinancialSummary(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT 
                    -- Total Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND CAST(SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS TotalSales,
                    -- Cash Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType='Cash' AND CAST(SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS CashSales,
                    -- Credit Sales (including Installment)
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType IN ('Credit', 'Installment') AND CAST(SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS CreditSales,
                    -- Driver Loads Sales
                    ISNULL((SELECT SUM(TotalAmount) FROM Sales WHERE IsPosted=1 AND SaleType='DriverLoad' AND CAST(SaleDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS DriverLoadsSales,
                    -- Returns
                    ISNULL((SELECT SUM(TotalAmount) FROM SalesReturns WHERE CAST(ReturnDate AS DATE) BETWEEN @f AND @t AND (@warehouseID IS NULL OR WarehouseID = @warehouseID)), 0) AS TotalReturns,
                    -- Client Payments
                    ISNULL((SELECT SUM(Credit) FROM ClientTransactions WHERE TransType='Payment' AND CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS ClientPayments,
                    -- Expenses
                    ISNULL((SELECT SUM(Amount) FROM Expenses WHERE CAST(ExpenseDate AS DATE) BETWEEN @f AND @t), 0) AS TotalExpenses,
                    -- Cashbox Inflow (Cash Sales + Payments)
                    ISNULL((SELECT SUM(AmountIn) FROM CashBox WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS CashInflow,
                    -- Cashbox Outflow (Expenses + Handover returned or other outflows)
                    ISNULL((SELECT SUM(AmountOut) FROM CashBox WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t), 0) AS CashOutflow",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
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

        public static DataTable DebtAgingReport(DateTime fromDate, DateTime toDate, int? driverID = null, decimal minBalance = 0, int minDays = 0, string searchText = "", string addressText = "", string priceTier = "")
        {
            var parameters = new List<SqlParameter>
            {
                DbHelper.P("@FromDate", fromDate.Date)
            };

            string sql = @";WITH LastPayment AS (
                    SELECT ClientID, TransDate AS LastPaymentDate, Credit AS LastPaymentAmount,
                           ROW_NUMBER() OVER (PARTITION BY ClientID ORDER BY TransDate DESC, TransID DESC) AS rn
                    FROM ClientTransactions
                    WHERE TransType = 'Payment'
                ),
                LastInvoice AS (
                    SELECT ClientID, TransDate AS LastInvoiceDate, Debit AS LastInvoiceAmount,
                           ROW_NUMBER() OVER (PARTITION BY ClientID ORDER BY TransDate DESC, TransID DESC) AS rn
                    FROM ClientTransactions
                    WHERE TransType = 'Sale'
                )
                SELECT 
                    c.ClientCode,
                    c.ClientName,
                    ISNULL(c.Phone, N'---') AS Phone,
                    ISNULL(c.Address, N'---') AS Address,
                    COALESCE(c.DefaultPriceTier, N'تجزئة') AS DefaultPriceTier,
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                    lp.LastPaymentDate,
                    ISNULL(lp.LastPaymentAmount, 0) AS LastPaymentAmount,
                    li.LastInvoiceDate,
                    ISNULL(li.LastInvoiceAmount, 0) AS LastInvoiceAmount,
                    DATEDIFF(day, COALESCE(lp.LastPaymentDate, li.LastInvoiceDate, c.CreatedAt), GETDATE()) AS DaysSinceLastPayment
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                LEFT JOIN LastPayment lp ON c.ClientID = lp.ClientID AND lp.rn = 1
                LEFT JOIN LastInvoice li ON c.ClientID = li.ClientID AND li.rn = 1
                WHERE ISNULL(cb.Balance, c.OpeningBalance) >= @MinBalance ";

            parameters.Add(DbHelper.P("@MinBalance", minBalance > 0 ? minBalance : 0.01m));

            if (driverID.HasValue && driverID.Value > 0)
            {
                sql += " AND c.DriverID = @DriverID ";
                parameters.Add(DbHelper.P("@DriverID", driverID.Value));
            }

            if (minDays > 0)
            {
                sql += " AND DATEDIFF(day, COALESCE(lp.LastPaymentDate, li.LastInvoiceDate, c.CreatedAt), GETDATE()) >= @MinDays ";
                parameters.Add(DbHelper.P("@MinDays", minDays));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (c.ClientName LIKE @SearchText OR c.ClientCode LIKE @SearchText OR c.Phone LIKE @SearchText OR c.Phone2 LIKE @SearchText) ";
                parameters.Add(DbHelper.P("@SearchText", "%" + searchText + "%"));
            }

            if (!string.IsNullOrWhiteSpace(addressText))
            {
                sql += " AND c.Address LIKE @AddressText ";
                parameters.Add(DbHelper.P("@AddressText", "%" + addressText + "%"));
            }

            if (!string.IsNullOrWhiteSpace(priceTier) && priceTier != "كل الفئات")
            {
                sql += " AND COALESCE(c.DefaultPriceTier, N'تجزئة') = @PriceTier ";
                parameters.Add(DbHelper.P("@PriceTier", priceTier));
            }

            sql += " AND (lp.LastPaymentDate IS NULL OR lp.LastPaymentDate < @FromDate) ";
            sql += " ORDER BY Balance DESC, c.ClientName";

            return DbHelper.Query(sql, parameters.ToArray());
        }

        /// <summary>تقرير كميات الأصناف التفصيلي للفترة المحددة</summary>
        public static DataTable GetProductQtyDetail(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(@"
                ;WITH
                SalesPeriod AS (
                    SELECT si.ProductID,
                           SUM(si.Quantity)   AS TotalQty,
                           SUM(si.TotalPrice) AS TotalAmt,
                           SUM(CASE WHEN s.SaleType='Cash'       THEN si.Quantity ELSE 0 END) AS CashQty,
                           SUM(CASE WHEN s.SaleType='Credit' OR s.SaleType='Installment' THEN si.Quantity ELSE 0 END) AS CreditQty,
                           SUM(CASE WHEN s.SaleType='DriverLoad' THEN si.Quantity ELSE 0 END) AS DriverLoadQty
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    WHERE s.IsPosted = 1
                      AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR s.WarehouseID = @warehouseID)
                    GROUP BY si.ProductID
                ),
                ReturnsPeriod AS (
                    SELECT ri.ProductID,
                           SUM(ri.Quantity) AS ReturnedQty
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    WHERE CAST(sr.ReturnDate AS DATE) BETWEEN @f AND @t
                      AND (@warehouseID IS NULL OR sr.WarehouseID = @warehouseID)
                    GROUP BY ri.ProductID
                ),
                DriverReturnsPeriod AS (
                    SELECT hi.ProductID,
                           SUM(hi.ReturnedQty) AS DriverReturnQty
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                    WHERE CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                      AND hi.ReturnedQty > 0
                      AND (@warehouseID IS NULL OR dl.WarehouseID = @warehouseID)
                    GROUP BY hi.ProductID
                ),
                StockTotals AS (
                    SELECT ProductID, ISNULL(SUM(Quantity), 0.0) AS CurrentStock
                    FROM ProductStock
                    WHERE (@warehouseID IS NULL OR WarehouseID = @warehouseID)
                    GROUP BY ProductID
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
                          AND (@warehouseID IS NULL OR sa.WarehouseID = @warehouseID)
                        ORDER BY sa.AdjDate DESC
                    ), 0)                                                   AS LastAdjQty,

                    ISNULL(sp.TotalQty,        0)                          AS SoldQty,
                    ISNULL(sp.CashQty,         0)                          AS CashQty,
                    ISNULL(sp.CreditQty,       0)                          AS CreditQty,
                    ISNULL(sp.DriverLoadQty,   0)                          AS DriverLoadQty,
                    ISNULL(rp.ReturnedQty,     0)                          AS ReturnedQty,
                    ISNULL(drp.DriverReturnQty,0)                          AS DriverReturnQty,

                    ISNULL(sp.TotalQty, 0)
                    - ISNULL(rp.ReturnedQty, 0)
                    - ISNULL(drp.DriverReturnQty, 0)                       AS NetSoldQty,

                    ISNULL(sp.TotalAmt,        0)                          AS TotalSalesAmt,

                    ISNULL(stk.CurrentStock,   0)                          AS CurrentStock

                FROM Products p
                LEFT JOIN SalesPeriod        sp  ON sp.ProductID  = p.ProductID
                LEFT JOIN ReturnsPeriod      rp  ON rp.ProductID  = p.ProductID
                LEFT JOIN DriverReturnsPeriod drp ON drp.ProductID = p.ProductID
                LEFT JOIN StockTotals        stk ON stk.ProductID = p.ProductID
                WHERE p.IsActive = 1
                ORDER BY p.ProductName",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable WastageLossReport(DateTime from, DateTime to, int? warehouseID = null)
        {
            return DbHelper.Query(
                @"SELECT 
                    w.WastageDate AS TransDate,
                    N'هالك مخزن (' + wh.WarehouseName + ')' AS SourceType,
                    p.ProductName,
                    wi.Quantity,
                    wi.CostPrice AS UnitPrice,
                    wi.TotalCost,
                    ISNULL(e.EmpName, N'---') AS ResponsibleParty,
                    w.Notes
                  FROM WastageLoss w
                  JOIN WastageLossItems wi ON w.WastageID = wi.WastageID
                  JOIN Products p ON wi.ProductID = p.ProductID
                  JOIN Warehouses wh ON w.WarehouseID = wh.WarehouseID
                  LEFT JOIN Employees e ON w.ResponsibleDriverID = e.EmpID
                  WHERE CAST(w.WastageDate AS DATE) BETWEEN @f AND @t
                    AND (@warehouseID IS NULL OR w.WarehouseID = @warehouseID)

                  UNION ALL

                  SELECT 
                    dh.HandoverDate AS TransDate,
                    N'نافق مندوب (ح#' + CAST(dh.LoadID AS NVARCHAR) + N')' AS SourceType,
                    p.ProductName,
                    hi.DeadQty AS Quantity,
                    dli.UnitPrice AS UnitPrice,
                    (hi.DeadQty * dli.UnitPrice) AS TotalCost,
                    e.EmpName AS ResponsibleParty,
                    N'جهة التحمل: ' + 
                      CASE dh.DeadQtyHandling 
                        WHEN 'Driver' THEN N'المندوب (عجز)'
                        WHEN 'Company' THEN N'الشركة (مصروف)'
                        WHEN 'None' THEN N'خصم فقط'
                        ELSE N'غير محدد'
                      END + ISNULL(N' - ' + dh.Notes, N'') AS Notes
                  FROM DriverHandovers dh
                  JOIN HandoverItems hi ON dh.HandoverID = hi.HandoverID
                  JOIN Products p ON hi.ProductID = p.ProductID
                  JOIN Employees e ON dh.DriverID = e.EmpID
                  JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                  JOIN DriverLoadItems dli ON dh.LoadID = dli.LoadID AND hi.ProductID = dli.ProductID
                  WHERE hi.DeadQty > 0
                    AND CAST(dh.HandoverDate AS DATE) BETWEEN @f AND @t
                    AND (@warehouseID IS NULL OR dl.WarehouseID = @warehouseID)

                  ORDER BY TransDate DESC",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value));
        }

        public static DataTable GetClientProductSalesReport(DateTime from, DateTime to, int? clientID, int? productID, string saleType, int? warehouseID = null)
        {
            string query = @"
                SELECT 
                    s.SaleCode AS [رقم الفاتورة],
                    s.SaleDate AS [تاريخ الفاتورة],
                    c.ClientName AS [العميل],
                    p.ProductName AS [الصنف],
                    ISNULL(si.IMEI, N'-') AS [السيريال],
                    si.Quantity AS [الكمية],
                    si.UnitPrice AS [سعر الوحدة],
                    si.TotalPrice AS [الصافي],
                    CASE s.SaleType
                        WHEN 'Cash' THEN N'نقدي'
                        WHEN 'Credit' THEN N'آجل'
                        WHEN 'Installment' THEN N'تقسيط'
                        WHEN 'DriverLoad' THEN N'حملة مندوب'
                        ELSE s.SaleType
                    END AS [نوع البيع]
                FROM SaleItems si
                JOIN Sales s ON si.SaleID = s.SaleID
                JOIN Products p ON si.ProductID = p.ProductID
                LEFT JOIN Clients c ON s.ClientID = c.ClientID
                WHERE s.IsPosted = 1
                  AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t";

            if (clientID.HasValue && clientID.Value > 0)
            {
                query += " AND s.ClientID = @clientID";
            }
            if (productID.HasValue && productID.Value > 0)
            {
                query += " AND si.ProductID = @productID";
            }
            if (warehouseID.HasValue && warehouseID.Value > 0)
            {
                query += " AND s.WarehouseID = @warehouseID";
            }
            if (!string.IsNullOrEmpty(saleType) && saleType != "الكل")
            {
                query += " AND s.SaleType = @saleType";
            }

            query += " ORDER BY s.SaleDate DESC, s.SaleID DESC";

            return DbHelper.Query(query,
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date),
                DbHelper.P("@clientID", clientID.HasValue ? (object)clientID.Value : DBNull.Value),
                DbHelper.P("@productID", productID.HasValue ? (object)productID.Value : DBNull.Value),
                DbHelper.P("@warehouseID", warehouseID.HasValue ? (object)warehouseID.Value : DBNull.Value),
                DbHelper.P("@saleType", !string.IsNullOrEmpty(saleType) ? (object)saleType : DBNull.Value));
        }

        public static DataTable GetSupplierItemActivityReport(DateTime from, DateTime to, int? supplierID, string producerCompany, string searchTerm)
        {
            string query = @"
                SELECT 
                    p.ProductName AS [الصنف],
                    p.ProducerCompany AS [الشركة المنتجة],
                    -- Current Stock
                    ISNULL((
                        SELECT SUM(v.CurrentQty)
                        FROM vw_CurrentStockByWarehouse v
                        WHERE v.ProductID = p.ProductID
                    ), 0) AS [المخزون الحالي],
                    -- Quantity sold in period
                    ISNULL((
                        SELECT SUM(si.Quantity)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS [الكمية المباعة],
                    -- Total sales value in period
                    ISNULL((
                        SELECT SUM(si.TotalPrice)
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS [قيمة المبيعات],
                    -- Quantity purchased in period
                    ISNULL((
                        SELECT SUM(pi.Quantity)
                        FROM PurchaseItems pi
                        JOIN Purchases pur ON pi.PurchaseID = pur.PurchaseID
                        WHERE pi.ProductID = p.ProductID
                          AND pur.IsPosted = 1
                          AND CAST(pur.PurchaseDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS [الكمية المشتراة],
                    -- Total purchases value in period
                    ISNULL((
                        SELECT SUM(pi.TotalPrice)
                        FROM PurchaseItems pi
                        JOIN Purchases pur ON pi.PurchaseID = pur.PurchaseID
                        WHERE pi.ProductID = p.ProductID
                          AND pur.IsPosted = 1
                          AND CAST(pur.PurchaseDate AS DATE) BETWEEN @f AND @t
                    ), 0) AS [قيمة المشتريات],
                    -- Status
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM SaleItems si
                        JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID
                          AND s.IsPosted = 1
                          AND CAST(s.SaleDate AS DATE) BETWEEN @f AND @t
                    ) THEN N'نشط' ELSE N'راكد' END AS [الحالة]
                FROM Products p
                WHERE p.IsActive = 1";

            if (supplierID.HasValue && supplierID.Value > 0)
            {
                query += @" AND EXISTS (
                    SELECT 1 
                    FROM PurchaseItems pi2 
                    JOIN Purchases pur ON pi2.PurchaseID = pur.PurchaseID 
                    WHERE pi2.ProductID = p.ProductID AND pur.SupplierID = @supplierID AND pur.IsPosted = 1
                )";
            }

            if (!string.IsNullOrEmpty(producerCompany) && producerCompany != "الكل")
            {
                query += " AND p.ProducerCompany = @producerCompany";
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (p.ProductName LIKE @search OR p.ProductCode LIKE @search OR p.InternationalCode LIKE @search)";
            }

            query += " ORDER BY [الكمية المباعة] DESC, p.ProductName ASC";

            return DbHelper.Query(query,
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date),
                DbHelper.P("@supplierID", supplierID.HasValue ? (object)supplierID.Value : DBNull.Value),
                DbHelper.P("@producerCompany", !string.IsNullOrEmpty(producerCompany) ? (object)producerCompany : DBNull.Value),
                DbHelper.P("@search", !string.IsNullOrEmpty(searchTerm) ? (object)("%" + searchTerm + "%") : DBNull.Value));
        }

        public static decimal? GetLastPriceForClient(int productID, int clientID)
        {
            if (productID <= 0 || clientID <= 0) return null;
            object res = DbHelper.Scalar(@"
                SELECT TOP 1 si.UnitPrice 
                FROM SaleItems si 
                JOIN Sales s ON si.SaleID = s.SaleID 
                WHERE s.ClientID = @cid AND si.ProductID = @pid AND s.IsPosted = 1 
                ORDER BY s.SaleDate DESC, s.SaleID DESC",
                DbHelper.P("@cid", clientID), DbHelper.P("@pid", productID));

            if (res != null && res != DBNull.Value && decimal.TryParse(res.ToString(), out decimal price))
                return price;
            return null;
        }
    }
}

