using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class InstallmentScheduleDTO
    {
        public int ScheduleID { get; set; }
        public Guid ScheduleGUID { get; set; }
        public int ContractID { get; set; }
        public int InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; }
    }

    public static class InstallmentDAL
    {
        public static void AddAuditLogTrans(SqlTransaction trans, string action, int contractID, string oldValue, string newValue)
        {
            try
            {
                DbHelper.ExecuteTrans(trans,
                    @"INSERT INTO InstallmentAuditLog (Action, ContractID, UserID, LogDate, MachineName, OldValue, NewValue)
                      VALUES (@act, @cid, @uid, @dt, @mach, @old, @new)",
                    DbHelper.P("@act", action),
                    DbHelper.P("@cid", contractID),
                    DbHelper.P("@uid", Session.EmpID > 0 ? (object)Session.EmpID : DBNull.Value),
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@mach", Environment.MachineName),
                    DbHelper.P("@old", oldValue),
                    DbHelper.P("@new", newValue));
            }
            catch { /* Ignore logging errors to not block transaction */ }
        }

        public static void AddAuditLog(string action, int contractID, string oldValue, string newValue)
        {
            try
            {
                DbHelper.Execute(
                    @"INSERT INTO InstallmentAuditLog (Action, ContractID, UserID, LogDate, MachineName, OldValue, NewValue)
                      VALUES (@act, @cid, @uid, @dt, @mach, @old, @new)",
                    DbHelper.P("@act", action),
                    DbHelper.P("@cid", contractID),
                    DbHelper.P("@uid", Session.EmpID > 0 ? (object)Session.EmpID : DBNull.Value),
                    DbHelper.P("@dt", DateTime.Now),
                    DbHelper.P("@mach", Environment.MachineName),
                    DbHelper.P("@old", oldValue),
                    DbHelper.P("@new", newValue));
            }
            catch { }
        }

        private static List<SqlParameter> BuildParams(int? customerID, string status, string code)
        {
            var prms = new List<SqlParameter>();
            if (customerID.HasValue && customerID.Value > 0)
            {
                prms.Add(DbHelper.P("@custID", customerID.Value));
            }
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                prms.Add(DbHelper.P("@status", status));
            }
            if (!string.IsNullOrEmpty(code))
            {
                prms.Add(DbHelper.P("@code", "%" + code + "%"));
            }
            return prms;
        }

        public static DataTable GetContracts(int? customerID, string status, string code, int pageIndex, int pageSize, out int totalCount)
        {
            string filter = " WHERE 1=1 ";
            if (customerID.HasValue && customerID.Value > 0)
            {
                filter += " AND ic.CustomerID = @custID ";
            }
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filter += " AND ic.Status = @status ";
            }
            if (!string.IsNullOrEmpty(code))
            {
                filter += " AND (ic.ContractCode LIKE @code OR c.ClientName LIKE @code) ";
            }

            // Count total
            string countSql = @"SELECT COUNT(*) FROM InstallmentContracts ic 
                                JOIN Clients c ON ic.CustomerID = c.ClientID" + filter;
            
            var prmsCount = BuildParams(customerID, status, code);
            var countResult = DbHelper.Scalar(countSql, prmsCount.ToArray());
            totalCount = countResult != null ? Convert.ToInt32(countResult) : 0;

            // Query with pagination
            int startRow = (pageIndex - 1) * pageSize;
            int endRow = startRow + pageSize;
            string querySql = $@"
                SELECT * FROM (
                    SELECT ic.ContractID, ic.ContractGUID, ic.ContractCode, ic.BranchID, ic.InvoiceID,
                           ic.CustomerID, c.ClientName AS CustomerName, ic.SaleType, ic.ContractAmount,
                           ic.DownPayment, ic.FinancedAmount, ic.InstallmentCount, ic.InstallmentValue,
                           ic.StartDate, ic.Status, ic.Notes, ic.CreatedDate,
                           (SELECT TOP 1 SaleCode FROM Sales WHERE SaleID = ic.InvoiceID) AS InvoiceCode,
                           ROW_NUMBER() OVER (ORDER BY ic.ContractID DESC) AS RowNum
                    FROM InstallmentContracts ic
                    JOIN Clients c ON ic.CustomerID = c.ClientID
                    {filter}
                ) AS RowConstrainedResult
                WHERE RowNum > @startRow AND RowNum <= @endRow
                ORDER BY RowNum";

            var prmsQuery = BuildParams(customerID, status, code);
            prmsQuery.Add(DbHelper.P("@startRow", startRow));
            prmsQuery.Add(DbHelper.P("@endRow", endRow));

            return DbHelper.Query(querySql, prmsQuery.ToArray());
        }

        public static DataTable GetContractSchedule(int contractID)
        {
            return DbHelper.Query(
                @"SELECT ScheduleID, ScheduleGUID, ContractID, InstallmentNo, DueDate, Amount, PaidAmount, RemainingAmount, PaidDate, Status 
                  FROM InstallmentSchedules 
                  WHERE ContractID = @cid 
                  ORDER BY InstallmentNo",
                DbHelper.P("@cid", contractID));
        }

        public static DataTable GetContractPayments(int contractID)
        {
            return DbHelper.Query(
                @"SELECT ip.PaymentID, ip.PaymentGUID, ip.PaymentDate, ip.Amount, ip.PaymentMethod, ip.SafeID, ip.Notes, ip.ScheduleID,
                         e.EmpName AS UserName, s.InstallmentNo
                  FROM InstallmentPayments ip
                  LEFT JOIN Employees e ON ip.UserID = e.EmpID
                  LEFT JOIN InstallmentSchedules s ON ip.ScheduleID = s.ScheduleID
                  WHERE ip.ContractID = @cid
                  ORDER BY ip.PaymentDate DESC",
                DbHelper.P("@cid", contractID));
        }

        public static bool HasPaymentsCollected(int contractID)
        {
            // التحقق من وجود دفعات بخلاف الدفعة المقدمة المسجلة عند التأسيس
            var count = DbHelper.Scalar("SELECT COUNT(*) FROM InstallmentPayments WHERE ContractID = @cid", DbHelper.P("@cid", contractID));
            return count != null && Convert.ToInt32(count) > 0;
        }

        public static bool CollectPayment(int contractID, int branchID, decimal amount, string paymentMethod, int safeID, string notes)
        {
            if (amount <= 0) throw new Exception("يجب أن يكون مبلغ التحصيل أكبر من الصفر.");

            bool success = false;
            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. استرجاع بيانات العقد
                var dtContract = DbHelper.Query("SELECT CustomerID, ContractCode, ContractAmount, Status FROM InstallmentContracts WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                if (dtContract.Rows.Count == 0) throw new Exception("العقد غير موجود.");

                int customerID = Convert.ToInt32(dtContract.Rows[0]["CustomerID"]);
                string contractCode = dtContract.Rows[0]["ContractCode"].ToString();
                string oldStatus = dtContract.Rows[0]["Status"].ToString();

                if (oldStatus == "Cancelled") throw new Exception("لا يمكن تحصيل أقساط على عقد ملغي.");
                if (oldStatus == "Completed") throw new Exception("العقد مسدد بالكامل بالفعل.");

                // 2. استرجاع الأقساط غير المسددة بالكامل
                var dtSchedule = DbHelper.Query(
                    "SELECT ScheduleID, InstallmentNo, Amount, PaidAmount, RemainingAmount FROM InstallmentSchedules WHERE ContractID=@cid AND Status <> 'Paid' ORDER BY InstallmentNo",
                    DbHelper.P("@cid", contractID));

                decimal remainingToDistribute = amount;
                List<string> paymentDetails = new List<string>();

                foreach (DataRow row in dtSchedule.Rows)
                {
                    if (remainingToDistribute <= 0) break;

                    int scheduleID = Convert.ToInt32(row["ScheduleID"]);
                    int instNo = Convert.ToInt32(row["InstallmentNo"]);
                    decimal remAmt = Convert.ToDecimal(row["RemainingAmount"]);

                    decimal payForThis = Math.Min(remainingToDistribute, remAmt);
                    decimal newPaidAmount = Convert.ToDecimal(row["PaidAmount"]) + payForThis;
                    decimal newRemainingAmount = remAmt - payForThis;
                    string newStatus = newRemainingAmount == 0 ? "Paid" : "Partially Paid";

                    // تحديث القسط
                    DbHelper.ExecuteTrans(trans,
                        @"UPDATE InstallmentSchedules 
                          SET PaidAmount=@pa, RemainingAmount=@ra, Status=@st, PaidDate=@pd 
                          WHERE ScheduleID=@sid",
                        DbHelper.P("@pa", newPaidAmount),
                        DbHelper.P("@ra", newRemainingAmount),
                        DbHelper.P("@st", newStatus),
                        DbHelper.P("@pd", newStatus == "Paid" ? (object)DateTime.Now : DBNull.Value),
                        DbHelper.P("@sid", scheduleID));

                    // تسجيل الدفعة
                    DbHelper.ExecuteTrans(trans,
                        @"INSERT INTO InstallmentPayments (ContractID, ScheduleID, BranchID, PaymentDate, Amount, PaymentMethod, SafeID, UserID, Notes)
                          VALUES (@cid, @sid, @bid, @pd, @amt, @pm, @safe, @uid, @notes)",
                        DbHelper.P("@cid", contractID),
                        DbHelper.P("@sid", scheduleID),
                        DbHelper.P("@bid", branchID),
                        DbHelper.P("@pd", DateTime.Now),
                        DbHelper.P("@amt", payForThis),
                        DbHelper.P("@pm", paymentMethod),
                        DbHelper.P("@safe", safeID),
                        DbHelper.P("@uid", Session.EmpID > 0 ? (object)Session.EmpID : DBNull.Value),
                        DbHelper.P("@notes", $"تحصيل قسط رقم {instNo} للعقد {contractCode}. " + notes));

                    paymentDetails.Add($"قسط {instNo} ({payForThis:N2} ج)");
                    remainingToDistribute -= payForThis;
                }

                if (remainingToDistribute > 0)
                {
                    // إذا كان هناك فائض سداد زائد ليس له أقساط مستحقة (العميل سدد أكثر من إجمالي العقد المتبقي)
                    throw new Exception($"تم دفع مبلغ زائد بقيمة {remainingToDistribute:N2} ج يتجاوز مديونية العقد بالكامل!");
                }

                // 3. التحقق وتحديث حالة العقد
                var totalPaidObj = DbHelper.ScalarTrans(trans, "SELECT SUM(PaidAmount) FROM InstallmentSchedules WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                var totalAmountObj = DbHelper.ScalarTrans(trans, "SELECT ContractAmount FROM InstallmentContracts WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                
                decimal totalPaid = totalPaidObj != null ? Convert.ToDecimal(totalPaidObj) : 0m;
                decimal totalAmount = totalAmountObj != null ? Convert.ToDecimal(totalAmountObj) : 0m;

                if (totalPaid >= totalAmount)
                {
                    DbHelper.ExecuteTrans(trans, "UPDATE InstallmentContracts SET Status='Completed' WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                    AddAuditLogTrans(trans, "Complete", contractID, oldStatus, "Completed");
                }

                // 4. الربط المحاسبي (توريد للخزنة وخصم من حساب العميل)
                string payDesc = $"سداد أقساط العقد {contractCode}: " + string.Join("، ", paymentDetails);
                if (!string.IsNullOrEmpty(notes)) payDesc += " | " + notes;

                // قيد العميل (Credit دائن)
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO ClientTransactions(ClientID, TransType, Credit, RefID, Notes, CreatedBy, TransDate) VALUES(@cid, 'Payment', @amt, @ref, @notes, @uid, @dt)",
                    DbHelper.P("@cid", customerID),
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@ref", contractID),
                    DbHelper.P("@notes", payDesc),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@dt", DateTime.Now));

                // قيد الخزنة (AmountIn مدين)
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType, AmountIn, RefID, Notes, CreatedBy, TransDate) VALUES('ClientPayment', @amt, @ref, @notes, @uid, @dt)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@ref", contractID),
                    DbHelper.P("@notes", $"تحصيل أقساط العقد {contractCode} للعميل ID:{customerID}"),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@dt", DateTime.Now));

                AddAuditLogTrans(trans, "Collect", contractID, "", $"تحصيل مبلغ: {amount:N2} ج");
                success = true;
            });

            return success;
        }

        public static bool RescheduleInstallments(int contractID, List<InstallmentScheduleDTO> newSchedule)
        {
            // التحقق من وجود دفعات
            if (HasPaymentsCollected(contractID))
            {
                // إذا تم تحصيل دفعات، يمنع تعديل أي قيمة مالية، ولكن يسمح فقط بتعديل التواريخ للأقساط غير المسددة
                var dtSchedule = DbHelper.Query("SELECT ScheduleID, Status, Amount FROM InstallmentSchedules WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                foreach (var ns in newSchedule)
                {
                    foreach (DataRow row in dtSchedule.Rows)
                    {
                        if (Convert.ToInt32(row["ScheduleID"]) == ns.ScheduleID)
                        {
                            decimal dbAmount = Convert.ToDecimal(row["Amount"]);
                            string dbStatus = row["Status"].ToString();
                            if (dbAmount != ns.Amount)
                            {
                                throw new Exception("يمنع تعديل مبالغ الأقساط بعد تحصيل أي دفعة. يُسمح فقط بتأجيل/تغيير التواريخ للأقساط غير المدفوعة.");
                            }
                            if (dbStatus == "Paid" && ns.DueDate.Date != Convert.ToDateTime(row["DueDate"]).Date)
                            {
                                throw new Exception("يمنع تعديل تواريخ الأقساط المسددة بالفعل.");
                            }
                        }
                    }
                }
            }

            bool success = false;
            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (var ns in newSchedule)
                {
                    DbHelper.ExecuteTrans(trans,
                        "UPDATE InstallmentSchedules SET DueDate=@dt, Notes=@notes WHERE ScheduleID=@sid AND Status <> 'Paid'",
                        DbHelper.P("@dt", ns.DueDate),
                        DbHelper.P("@notes", "إعادة جدولة إدارية"),
                        DbHelper.P("@sid", ns.ScheduleID));
                }

                AddAuditLogTrans(trans, "Reschedule", contractID, "", "إعادة جدولة التواريخ للأقساط المتبقية");
                success = true;
            });

            return success;
        }

        public static bool CancelContract(int contractID, string reason)
        {
            // يمنع إلغاء العقد نهائياً إذا كانت به أقساط مسددة بخلاف المقدم
            if (HasPaymentsCollected(contractID))
            {
                throw new Exception("يمنع إلغاء أو حذف هذا العقد لوجود تحصيلات مسجلة عليه. يرجى عمل تسوية مالية أو مرتجع مبيعات بدلاً من الإلغاء.");
            }

            bool success = false;
            DbHelper.RunInTransaction((con, trans) =>
            {
                // استرجاع العميل والفاتورة
                var dt = DbHelper.Query("SELECT CustomerID, InvoiceID, ContractAmount, DownPayment, Status FROM InstallmentContracts WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
                if (dt.Rows.Count == 0) throw new Exception("العقد غير موجود.");

                int customerID = Convert.ToInt32(dt.Rows[0]["CustomerID"]);
                object invIDObj = dt.Rows[0]["InvoiceID"];
                decimal totalAmount = Convert.ToDecimal(dt.Rows[0]["ContractAmount"]);
                decimal downPayment = Convert.ToDecimal(dt.Rows[0]["DownPayment"]);
                string oldStatus = dt.Rows[0]["Status"].ToString();

                if (oldStatus == "Cancelled") throw new Exception("العقد ملغي بالفعل.");

                // 1. تحديث حالة العقد إلى ملغي
                DbHelper.ExecuteTrans(trans, "UPDATE InstallmentContracts SET Status='Cancelled', Notes=@n WHERE ContractID=@cid", 
                    DbHelper.P("@n", $"تم الإلغاء بسبب: {reason}"), DbHelper.P("@cid", contractID));

                // 2. تحديث جدول الأقساط
                DbHelper.ExecuteTrans(trans, "UPDATE InstallmentSchedules SET Status='Cancelled' WHERE ContractID=@cid", DbHelper.P("@cid", contractID));

                // 3. قيود عكسية محاسبية
                // عكس قيد الفاتورة الأصلي (المديونية بالكامل)
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO ClientTransactions(ClientID, TransType, Credit, RefID, Notes, CreatedBy, TransDate) VALUES(@cid, 'Adjustment', @amt, @ref, @notes, @uid, @dt)",
                    DbHelper.P("@cid", customerID),
                    DbHelper.P("@amt", totalAmount),
                    DbHelper.P("@ref", contractID),
                    DbHelper.P("@notes", $"قيد عكسي لإلغاء عقد التقسيط: {reason}"),
                    DbHelper.P("@uid", Session.EmpID),
                    DbHelper.P("@dt", DateTime.Now));

                // إذا كان هناك دفعة مقدمة، نعكس قيد الخزنة والعميل
                if (downPayment > 0)
                {
                    // عكس توريد الخزنة (صرف AmountOut)
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO CashBox(TransType, AmountOut, RefID, Notes, CreatedBy, TransDate) VALUES('Adjustment', @amt, @ref, @notes, @uid, @dt)",
                        DbHelper.P("@amt", downPayment),
                        DbHelper.P("@ref", contractID),
                        DbHelper.P("@notes", $"رد مقدم عقد التقسيط الملغي"),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@dt", DateTime.Now));

                    // عكس قيد سداد العميل (مدين Debit)
                    DbHelper.ExecuteTrans(trans,
                        "INSERT INTO ClientTransactions(ClientID, TransType, Debit, RefID, Notes, CreatedBy, TransDate) VALUES(@cid, 'Adjustment', @amt, @ref, @notes, @uid, @dt)",
                        DbHelper.P("@cid", customerID),
                        DbHelper.P("@amt", downPayment),
                        DbHelper.P("@ref", contractID),
                        DbHelper.P("@notes", $"رد دفعة مقدمة لعقد تقسيط ملغي"),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@dt", DateTime.Now));
                }

                AddAuditLogTrans(trans, "Cancel", contractID, oldStatus, "Cancelled");
                success = true;
            });

            return success;
        }

        public static void HandleSalesReturn(SqlTransaction trans, int invoiceID, decimal returnAmount)
        {
            // البحث عن العقد المرتبط بالفاتورة
            var dtContract = DbHelper.Query("SELECT ContractID, ContractAmount, FinancedAmount, DownPayment FROM InstallmentContracts WHERE InvoiceID = @iid AND Status = 'Active'",
                DbHelper.P("@iid", invoiceID));

            if (dtContract.Rows.Count == 0) return; // ليس بيع بالتقسيط نشط

            int contractID = Convert.ToInt32(dtContract.Rows[0]["ContractID"]);
            decimal contractAmount = Convert.ToDecimal(dtContract.Rows[0]["ContractAmount"]);
            decimal downPayment = Convert.ToDecimal(dtContract.Rows[0]["DownPayment"]);

            // التحقق من الرصيد المتبقي الإجمالي للعقد
            var paidObj = DbHelper.ScalarTrans(trans, "SELECT SUM(PaidAmount) FROM InstallmentSchedules WHERE ContractID = @cid", DbHelper.P("@cid", contractID));
            decimal totalPaid = paidObj != null ? Convert.ToDecimal(paidObj) : 0m;
            decimal remainingBalance = contractAmount - totalPaid;

            if (returnAmount > remainingBalance)
            {
                throw new Exception($"خطأ محاسبي: قيمة المرتجع ({returnAmount:N2} ج) أكبر من الرصيد المتبقي القائم للعقد ({remainingBalance:N2} ج)، ويمنع المرتجع الذي يسبب رصيداً سالباً للعقد!");
            }

            // تحديث العقد بالقيمة الجديدة
            decimal newContractAmount = contractAmount - returnAmount;
            decimal newFinancedAmount = newContractAmount - downPayment;

            DbHelper.ExecuteTrans(trans,
                "UPDATE InstallmentContracts SET ContractAmount = @ca, FinancedAmount = @fa WHERE ContractID = @cid",
                DbHelper.P("@ca", newContractAmount),
                DbHelper.P("@fa", newFinancedAmount),
                DbHelper.P("@cid", contractID));

            // تعديل الأقساط تنازلياً بدءاً من الأخير
            var dtSchedule = DbHelper.Query(
                "SELECT ScheduleID, Amount, PaidAmount, RemainingAmount FROM InstallmentSchedules WHERE ContractID = @cid ORDER BY InstallmentNo DESC",
                DbHelper.P("@cid", contractID));

            decimal remainingToReduce = returnAmount;
            foreach (DataRow row in dtSchedule.Rows)
            {
                if (remainingToReduce <= 0) break;

                int scheduleID = Convert.ToInt32(row["ScheduleID"]);
                decimal amount = Convert.ToDecimal(row["Amount"]);
                decimal paid = Convert.ToDecimal(row["PaidAmount"]);
                decimal rem = Convert.ToDecimal(row["RemainingAmount"]);

                // المبلغ المتاح للخصم من هذا القسط هو قيمة القسط المتبقي غير المسدد
                decimal availableToReduce = rem;
                if (availableToReduce <= 0) continue; // مسدد بالكامل

                decimal reduceThis = Math.Min(remainingToReduce, availableToReduce);
                decimal newAmount = amount - reduceThis;
                decimal newRemaining = rem - reduceThis;
                string newStatus = newRemaining == 0 ? "Paid" : "Pending";

                DbHelper.ExecuteTrans(trans,
                    "UPDATE InstallmentSchedules SET Amount=@a, RemainingAmount=@ra, Status=@st WHERE ScheduleID=@sid",
                    DbHelper.P("@a", newAmount),
                    DbHelper.P("@ra", newRemaining),
                    DbHelper.P("@st", newStatus),
                    DbHelper.P("@sid", scheduleID));

                remainingToReduce -= reduceThis;
            }

            // في حال تم تصفير الدين تماماً
            if (newContractAmount <= totalPaid)
            {
                DbHelper.ExecuteTrans(trans, "UPDATE InstallmentContracts SET Status='Completed' WHERE ContractID=@cid", DbHelper.P("@cid", contractID));
            }

            AddAuditLogTrans(trans, "Return", contractID, "", $"تطبيق مرتجع بقيمة: {returnAmount:N2} ج");
        }

        public static DataTable GetDashboardData(int branchID)
        {
            var dt = new DataTable();
            dt.Columns.Add("ActiveContracts", typeof(int));
            dt.Columns.Add("TotalRemaining", typeof(decimal));
            dt.Columns.Add("DueToday", typeof(decimal));
            dt.Columns.Add("Overdue", typeof(decimal));
            dt.Columns.Add("CollectedToday", typeof(decimal));

            int activeCount = 0;
            decimal totalRemaining = 0m;
            decimal dueToday = 0m;
            decimal overdue = 0m;
            decimal collectedToday = 0m;

            try
            {
                // 1. العقود النشطة
                var count = DbHelper.Scalar("SELECT COUNT(*) FROM InstallmentContracts WHERE Status = 'Active' AND BranchID=@bid", DbHelper.P("@bid", branchID));
                activeCount = count != null ? Convert.ToInt32(count) : 0;

                // 2. إجمالي المتبقي للتحصيل
                var rem = DbHelper.Scalar(@"
                    SELECT SUM(s.RemainingAmount) 
                    FROM InstallmentSchedules s
                    JOIN InstallmentContracts c ON s.ContractID = c.ContractID
                    WHERE c.Status = 'Active' AND c.BranchID=@bid", DbHelper.P("@bid", branchID));
                totalRemaining = rem != DBNull.Value && rem != null ? Convert.ToDecimal(rem) : 0m;

                // 3. أقساط اليوم
                var today = DbHelper.Scalar(@"
                    SELECT SUM(s.RemainingAmount) 
                    FROM InstallmentSchedules s
                    JOIN InstallmentContracts c ON s.ContractID = c.ContractID
                    WHERE c.Status = 'Active' AND c.BranchID=@bid AND CAST(s.DueDate AS DATE) = CAST(GETDATE() AS DATE)", DbHelper.P("@bid", branchID));
                dueToday = today != DBNull.Value && today != null ? Convert.ToDecimal(today) : 0m;

                // 4. الأقساط المتأخرة
                var ov = DbHelper.Scalar(@"
                    SELECT SUM(s.RemainingAmount) 
                    FROM InstallmentSchedules s
                    JOIN InstallmentContracts c ON s.ContractID = c.ContractID
                    WHERE c.Status = 'Active' AND c.BranchID=@bid AND CAST(s.DueDate AS DATE) < CAST(GETDATE() AS DATE) AND s.Status <> 'Paid'", DbHelper.P("@bid", branchID));
                overdue = ov != DBNull.Value && ov != null ? Convert.ToDecimal(ov) : 0m;

                // 5. تحصيلات اليوم
                var col = DbHelper.Scalar(@"
                    SELECT SUM(Amount) 
                    FROM InstallmentPayments 
                    WHERE BranchID=@bid AND CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)", DbHelper.P("@bid", branchID));
                collectedToday = col != DBNull.Value && col != null ? Convert.ToDecimal(col) : 0m;
            }
            catch { }

            dt.Rows.Add(activeCount, totalRemaining, dueToday, overdue, collectedToday);
            return dt;
        }

        public static DataTable GetTop10Debtors(int branchID)
        {
            return DbHelper.Query(@"
                SELECT TOP 10 c.ClientName, SUM(s.RemainingAmount) AS DebtAmount
                FROM InstallmentSchedules s
                JOIN InstallmentContracts ic ON s.ContractID = ic.ContractID
                JOIN Clients c ON ic.CustomerID = c.ClientID
                WHERE ic.Status = 'Active' AND ic.BranchID=@bid
                GROUP BY c.ClientName
                ORDER BY DebtAmount DESC",
                DbHelper.P("@bid", branchID));
        }

        public static DataTable GetAuditLogs(int contractID)
        {
            return DbHelper.Query(@"
                SELECT al.LogID, al.Action, al.LogDate, al.MachineName, al.OldValue, al.NewValue, e.EmpName AS UserName
                FROM InstallmentAuditLog al
                LEFT JOIN Employees e ON al.UserID = e.EmpID
                WHERE al.ContractID = @cid
                ORDER BY al.LogDate DESC",
                DbHelper.P("@cid", contractID));
        }
    }
}
