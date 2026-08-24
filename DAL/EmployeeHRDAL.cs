using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class EmployeeHRDAL
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // 1. الحضور والانصراف (Attendance & Departure)
        // ═══════════════════════════════════════════════════════════════════════════

        public static DataTable GetDailyAttendanceGrid(DateTime date)
        {
            string sql = @"
                SELECT e.EmpID, e.EmpName, e.Role, e.JobTitle, e.DailyWorkHours,
                       ea.AttendanceID,
                       ea.AttendDate,
                       ea.CheckInTime,
                       ea.CheckOutTime,
                       COALESCE(ea.Status, N'حاضر') AS Status,
                       COALESCE(ea.WorkHours, e.DailyWorkHours) AS WorkHours,
                       COALESCE(ea.OvertimeHours, 0) AS OvertimeHours,
                       COALESCE(ea.LateMinutes, 0) AS LateMinutes,
                       ea.Notes
                FROM Employees e
                LEFT JOIN EmployeeAttendance ea ON e.EmpID = ea.EmpID AND CAST(ea.AttendDate AS DATE) = @dt
                WHERE e.IsActive = 1
                ORDER BY e.EmpName ASC";

            return DbHelper.Query(sql, DbHelper.P("@dt", date.Date));
        }

        public static DataTable GetAttendanceReport(int empID, DateTime from, DateTime to, string statusFilter = "الكل")
        {
            string sql = @"
                SELECT ea.AttendanceID, ea.EmpID, e.EmpName, e.Role, ea.AttendDate,
                       ea.CheckInTime, ea.CheckOutTime, ea.Status, ea.WorkHours,
                       ea.OvertimeHours, ea.LateMinutes, ea.Notes,
                       creator.EmpName AS CreatedByName
                FROM EmployeeAttendance ea
                JOIN Employees e ON ea.EmpID = e.EmpID
                LEFT JOIN Employees creator ON ea.CreatedBy = creator.EmpID
                WHERE CAST(ea.AttendDate AS DATE) BETWEEN @from AND @to";

            var prms = new List<SqlParameter>
            {
                DbHelper.P("@from", from.Date),
                DbHelper.P("@to", to.Date)
            };

            if (empID > 0)
            {
                sql += " AND ea.EmpID = @empID";
                prms.Add(DbHelper.P("@empID", empID));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "الكل")
            {
                sql += " AND ea.Status = @status";
                prms.Add(DbHelper.P("@status", statusFilter));
            }

            sql += " ORDER BY ea.AttendDate DESC, e.EmpName ASC";
            return DbHelper.Query(sql, prms.ToArray());
        }

        public static bool SaveAttendance(int empID, DateTime attendDate, DateTime? checkIn, DateTime? checkOut,
            string status, decimal workHours, decimal overtime, int lateMinutes, string notes)
        {
            try
            {
                string sql = @"
                    IF EXISTS (SELECT 1 FROM EmployeeAttendance WHERE EmpID = @empID AND CAST(AttendDate AS DATE) = @dt)
                    BEGIN
                        UPDATE EmployeeAttendance
                        SET CheckInTime = @in, CheckOutTime = @out, Status = @st,
                            WorkHours = @wh, OvertimeHours = @ot, LateMinutes = @lm, Notes = @notes
                        WHERE EmpID = @empID AND CAST(AttendDate AS DATE) = @dt;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO EmployeeAttendance (EmpID, AttendDate, CheckInTime, CheckOutTime, Status, WorkHours, OvertimeHours, LateMinutes, Notes, CreatedBy)
                        VALUES (@empID, @dt, @in, @out, @st, @wh, @ot, @lm, @notes, @uid);
                    END";

                int res = DbHelper.Execute(sql,
                    DbHelper.P("@empID", empID),
                    DbHelper.P("@dt", attendDate.Date),
                    DbHelper.P("@in", (object)checkIn ?? DBNull.Value),
                    DbHelper.P("@out", (object)checkOut ?? DBNull.Value),
                    DbHelper.P("@st", status ?? "حاضر"),
                    DbHelper.P("@wh", workHours),
                    DbHelper.P("@ot", overtime),
                    DbHelper.P("@lm", lateMinutes),
                    DbHelper.P("@notes", notes ?? ""),
                    DbHelper.P("@uid", Session.EmpID));

                return res > 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("EmployeeHRDAL.SaveAttendance", ex);
                return false;
            }
        }

        public static bool QuickCheckIn(int empID, DateTime now)
        {
            return SaveAttendance(empID, now.Date, now, null, "حاضر", 8, 0, 0, "تسجيل حضور سريع");
        }

        public static bool QuickCheckOut(int empID, DateTime now)
        {
            var dt = DbHelper.Query("SELECT TOP 1 AttendanceID, CheckInTime, WorkHours FROM EmployeeAttendance WHERE EmpID = @empID AND CAST(AttendDate AS DATE) = @dt",
                DbHelper.P("@empID", empID), DbHelper.P("@dt", now.Date));

            DateTime? inTime = null;
            decimal hours = 8;
            decimal overtime = 0;
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["CheckInTime"] != DBNull.Value)
                {
                    inTime = Convert.ToDateTime(dt.Rows[0]["CheckInTime"]);
                    var diff = now - inTime.Value;
                    hours = Math.Round((decimal)diff.TotalHours, 2);
                    if (hours > 8) overtime = hours - 8;
                }
            }

            return SaveAttendance(empID, now.Date, inTime, now, "حاضر", hours, overtime, 0, "تسجيل انصراف سريع");
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 2. البدلات والمكافآت والخصومات والسلف (Salary Items)
        // ═══════════════════════════════════════════════════════════════════════════

        public static DataTable GetSalaryItems(int empID, DateTime from, DateTime to, string itemType = "الكل", bool? isSettled = null)
        {
            string sql = @"
                SELECT esi.ItemID, esi.EmpID, e.EmpName, esi.ItemDate, esi.ItemType,
                       esi.Amount, esi.Reason, esi.IsSettled, esi.PayrollMonth,
                       creator.EmpName AS CreatedByName
                FROM EmployeeSalaryItems esi
                JOIN Employees e ON esi.EmpID = e.EmpID
                LEFT JOIN Employees creator ON esi.CreatedBy = creator.EmpID
                WHERE CAST(esi.ItemDate AS DATE) BETWEEN @from AND @to";

            var prms = new List<SqlParameter>
            {
                DbHelper.P("@from", from.Date),
                DbHelper.P("@to", to.Date)
            };

            if (empID > 0)
            {
                sql += " AND esi.EmpID = @empID";
                prms.Add(DbHelper.P("@empID", empID));
            }

            if (!string.IsNullOrEmpty(itemType) && itemType != "الكل")
            {
                sql += " AND esi.ItemType = @itemType";
                prms.Add(DbHelper.P("@itemType", itemType));
            }

            if (isSettled.HasValue)
            {
                sql += " AND esi.IsSettled = @settled";
                prms.Add(DbHelper.P("@settled", isSettled.Value));
            }

            sql += " ORDER BY esi.ItemDate DESC, esi.ItemID DESC";
            return DbHelper.Query(sql, prms.ToArray());
        }

        public static int SaveSalaryItem(int empID, DateTime date, string itemType, decimal amount, string reason, string payrollMonth, bool affectCash = false)
        {
            int itemId = -1;
            DbHelper.RunInTransaction((con, trans) =>
            {
                itemId = DbHelper.ExecuteInsertTrans(trans, @"
                    INSERT INTO EmployeeSalaryItems (EmpID, ItemDate, ItemType, Amount, Reason, IsSettled, PayrollMonth, CreatedBy)
                    VALUES (@empID, @dt, @type, @amt, @reason, 0, @pMonth, @uid)",
                    DbHelper.P("@empID", empID),
                    DbHelper.P("@dt", date),
                    DbHelper.P("@type", itemType),
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@reason", reason ?? ""),
                    DbHelper.P("@pMonth", payrollMonth ?? date.ToString("yyyy-MM")),
                    DbHelper.P("@uid", Session.EmpID));

                // إذا كانت سلفة أو صرف نقدي فوري يؤثر على الخزينة وحساب الموظف
                if (affectCash && amount > 0)
                {
                    decimal debit = (itemType == "سلفة" || itemType == "خصم / جزاء") ? amount : 0;
                    decimal credit = (itemType == "مكافأة" || itemType == "بدل" || itemType == "إضافي") ? amount : 0;

                    // قيد في حساب الموظف
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO EmployeeTransactions (EmpID, TransDate, TransType, Debit, Credit, Notes, CreatedBy)
                        VALUES (@empID, @dt, @type, @debit, @credit, @notes, @uid)",
                        DbHelper.P("@empID", empID),
                        DbHelper.P("@dt", date),
                        DbHelper.P("@type", itemType),
                        DbHelper.P("@debit", debit),
                        DbHelper.P("@credit", credit),
                        DbHelper.P("@notes", reason ?? itemType),
                        DbHelper.P("@uid", Session.EmpID));

                    // قيد في الخزينة إذا كانت سلفة نقدية منصرفة
                    if (itemType == "سلفة")
                    {
                        int safeId = Session.GetDefaultSafeID();
                        AccountDAL.EnsureSufficientCashTrans(trans, safeId, amount, "صرف سلفة موظف");
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, RefID, Notes, CreatedBy, AccountID)
                            VALUES (@dt, 'EmpAdvance', 0, @amt, @ref, @notes, @uid, @accId)",
                            DbHelper.P("@dt", date),
                            DbHelper.P("@amt", amount),
                            DbHelper.P("@ref", itemId),
                            DbHelper.P("@notes", "سلفة للموظف: " + (reason ?? "")),
                            DbHelper.P("@uid", Session.EmpID),
                            DbHelper.P("@accId", safeId));
                    }
                }
            });
            return itemId;
        }

        public static bool DeleteSalaryItem(int itemID)
        {
            try
            {
                int r = DbHelper.Execute("DELETE FROM EmployeeSalaryItems WHERE ItemID = @id", DbHelper.P("@id", itemID));
                return r > 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("EmployeeHRDAL.DeleteSalaryItem", ex);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 3. نظام العمولات وشرائح البيع وعمولات الأصناف
        // ═══════════════════════════════════════════════════════════════════════════

        public static DataTable GetProductCommissions(int empID)
        {
            string sql = @"
                SELECT epc.RuleID, epc.EmpID, e.EmpName, epc.ProductID,
                       p.ProductCode, p.ProductName, p.Unit, p.SalePrice,
                       epc.CommissionType, epc.CommissionValue, epc.IsActive, epc.Notes
                FROM EmployeeProductCommissions epc
                JOIN Employees e ON epc.EmpID = e.EmpID
                JOIN Products p ON epc.ProductID = p.ProductID
                WHERE epc.EmpID = @empID
                ORDER BY p.ProductName ASC";

            return DbHelper.Query(sql, DbHelper.P("@empID", empID));
        }

        public static bool SaveProductCommission(int ruleID, int empID, int productID, string commType, decimal commVal, string notes)
        {
            try
            {
                string sql = @"
                    IF EXISTS (SELECT 1 FROM EmployeeProductCommissions WHERE RuleID = @rid)
                    BEGIN
                        UPDATE EmployeeProductCommissions
                        SET EmpID = @eid, ProductID = @pid, CommissionType = @ctype,
                            CommissionValue = @cval, Notes = @notes, IsActive = 1
                        WHERE RuleID = @rid;
                    END
                    ELSE IF EXISTS (SELECT 1 FROM EmployeeProductCommissions WHERE EmpID = @eid AND ProductID = @pid)
                    BEGIN
                        UPDATE EmployeeProductCommissions
                        SET CommissionType = @ctype, CommissionValue = @cval, Notes = @notes, IsActive = 1
                        WHERE EmpID = @eid AND ProductID = @pid;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO EmployeeProductCommissions (EmpID, ProductID, CommissionType, CommissionValue, IsActive, Notes)
                        VALUES (@eid, @pid, @ctype, @cval, 1, @notes);
                    END";

                int res = DbHelper.Execute(sql,
                    DbHelper.P("@rid", ruleID),
                    DbHelper.P("@eid", empID),
                    DbHelper.P("@pid", productID),
                    DbHelper.P("@ctype", commType ?? "Fixed"),
                    DbHelper.P("@cval", commVal),
                    DbHelper.P("@notes", notes ?? ""));

                return res > 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("EmployeeHRDAL.SaveProductCommission", ex);
                return false;
            }
        }

        public static bool DeleteProductCommission(int ruleID)
        {
            return DbHelper.Execute("DELETE FROM EmployeeProductCommissions WHERE RuleID = @rid", DbHelper.P("@rid", ruleID)) > 0;
        }

        public static DataTable GetCommissionTiers(int empID)
        {
            string sql = @"
                SELECT TierID, EmpID, MinTarget, MaxTarget, CommissionRate, BonusAmount, IsActive
                FROM EmployeeCommissionTiers
                WHERE EmpID = @empID OR EmpID IS NULL
                ORDER BY MinTarget ASC";

            return DbHelper.Query(sql, DbHelper.P("@empID", empID));
        }

        public static bool SaveCommissionTier(int tierID, int? empID, decimal minTarget, decimal maxTarget, decimal rate, decimal bonus)
        {
            try
            {
                string sql = @"
                    IF EXISTS (SELECT 1 FROM EmployeeCommissionTiers WHERE TierID = @tid)
                    BEGIN
                        UPDATE EmployeeCommissionTiers
                        SET EmpID = @eid, MinTarget = @min, MaxTarget = @max, CommissionRate = @rate, BonusAmount = @bonus, IsActive = 1
                        WHERE TierID = @tid;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO EmployeeCommissionTiers (EmpID, MinTarget, MaxTarget, CommissionRate, BonusAmount, IsActive)
                        VALUES (@eid, @min, @max, @rate, @bonus, 1);
                    END";

                return DbHelper.Execute(sql,
                    DbHelper.P("@tid", tierID),
                    DbHelper.P("@eid", empID.HasValue && empID.Value > 0 ? (object)empID.Value : DBNull.Value),
                    DbHelper.P("@min", minTarget),
                    DbHelper.P("@max", maxTarget),
                    DbHelper.P("@rate", rate),
                    DbHelper.P("@bonus", bonus)) > 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("EmployeeHRDAL.SaveCommissionTier", ex);
                return false;
            }
        }

        public static bool DeleteCommissionTier(int tierID)
        {
            return DbHelper.Execute("DELETE FROM EmployeeCommissionTiers WHERE TierID = @tid", DbHelper.P("@tid", tierID)) > 0;
        }

        /// <summary>
        /// احتساب العمولات التفصيلية للموظف أو المندوب لفترة زمنية محددة من واقع الفواتير والأصناف
        /// </summary>
        public static (DataTable DetailsTable, decimal TotalCommission, decimal TotalSalesAmount) CalculateCommissions(int empID, DateTime from, DateTime to)
        {
            var dtResult = new DataTable();
            dtResult.Columns.Add("SaleID", typeof(int));
            dtResult.Columns.Add("SaleCode", typeof(string));
            dtResult.Columns.Add("SaleDate", typeof(string));
            dtResult.Columns.Add("ClientName", typeof(string));
            dtResult.Columns.Add("ProductName", typeof(string));
            dtResult.Columns.Add("Quantity", typeof(decimal));
            dtResult.Columns.Add("UnitPrice", typeof(decimal));
            dtResult.Columns.Add("TotalPrice", typeof(decimal));
            dtResult.Columns.Add("CommissionRule", typeof(string));
            dtResult.Columns.Add("CommissionAmount", typeof(decimal));

            decimal totalCommission = 0m;
            decimal totalSalesAmount = 0m;

            try
            {
                // قراءة بيانات الموظف
                var dtEmp = DbHelper.Query("SELECT EmpID, EmpName, SalesCommissionRate, TargetAmount FROM Employees WHERE EmpID = @eid", DbHelper.P("@eid", empID));
                decimal generalRate = 0m;
                if (dtEmp.Rows.Count > 0 && dtEmp.Rows[0]["SalesCommissionRate"] != DBNull.Value)
                {
                    generalRate = Convert.ToDecimal(dtEmp.Rows[0]["SalesCommissionRate"]);
                }

                // قراءة عمولات الأصناف المخصصة للموظف
                var dtProdRules = DbHelper.Query("SELECT ProductID, CommissionType, CommissionValue FROM EmployeeProductCommissions WHERE EmpID = @eid AND IsActive = 1", DbHelper.P("@eid", empID));
                var productRules = new Dictionary<int, (string Type, decimal Value)>();
                foreach (DataRow r in dtProdRules.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    string type = r["CommissionType"]?.ToString() ?? "Fixed";
                    decimal val = Convert.ToDecimal(r["CommissionValue"]);
                    productRules[pid] = (type, val);
                }

                // قراءة بنود الفواتير المعتمدة للموظف أو المندوب
                string sqlSales = @"
                    SELECT s.SaleID, s.SaleCode, s.SaleDate, s.TotalAmount AS InvoiceTotal,
                           COALESCE(c.ClientName, N'عميل نقدي') AS ClientName,
                           si.ProductID, p.ProductName,
                           si.Quantity * COALESCE(si.Factor, 1.0) AS Qty,
                           si.UnitPrice,
                           si.TotalPrice
                    FROM Sales s
                    JOIN SaleItems si ON s.SaleID = si.SaleID
                    JOIN Products p ON si.ProductID = p.ProductID
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    WHERE (s.CreatedBy = @eid OR s.DriverID = @eid)
                      AND s.IsPosted = 1
                      AND CAST(s.SaleDate AS DATE) BETWEEN @from AND @to
                    ORDER BY s.SaleDate ASC, s.SaleID ASC";

                var dtSales = DbHelper.Query(sqlSales,
                    DbHelper.P("@eid", empID),
                    DbHelper.P("@from", from.Date),
                    DbHelper.P("@to", to.Date));

                var countedSales = new HashSet<int>();

                foreach (DataRow r in dtSales.Rows)
                {
                    int sid = Convert.ToInt32(r["SaleID"]);
                    string code = r["SaleCode"]?.ToString() ?? "";
                    string dtStr = Convert.ToDateTime(r["SaleDate"]).ToString("yyyy-MM-dd");
                    string cName = r["ClientName"]?.ToString() ?? "";
                    int pid = Convert.ToInt32(r["ProductID"]);
                    string pName = r["ProductName"]?.ToString() ?? "";
                    decimal qty = Convert.ToDecimal(r["Qty"]);
                    decimal price = Convert.ToDecimal(r["UnitPrice"]);
                    decimal itemTotal = Convert.ToDecimal(r["TotalPrice"]);

                    if (!countedSales.Contains(sid))
                    {
                        countedSales.Add(sid);
                        totalSalesAmount += Convert.ToDecimal(r["InvoiceTotal"]);
                    }

                    decimal comm = 0m;
                    string ruleDesc = "";

                    // 1. فحص هل هناك عمولة خاصة بالصنف
                    if (productRules.TryGetValue(pid, out var rule))
                    {
                        if (rule.Type == "Fixed")
                        {
                            comm = qty * rule.Value;
                            ruleDesc = $"عمولة صنف ثابتة ({rule.Value:N2} ج × {qty:N2})";
                        }
                        else // Percentage
                        {
                            comm = (itemTotal * rule.Value) / 100m;
                            ruleDesc = $"عمولة صنف نسبية ({rule.Value:N1}% من {itemTotal:N2} ج)";
                        }
                    }
                    else if (generalRate > 0)
                    {
                        // 2. إذا لم يكن هناك عمولة خاصة للصنف، تطبق العمولة العامة للموظف
                        comm = (itemTotal * generalRate) / 100m;
                        ruleDesc = $"عمولة عامة ({generalRate:N1}% من {itemTotal:N2} ج)";
                    }

                    totalCommission += comm;
                    dtResult.Rows.Add(sid, code, dtStr, cName, pName, qty, price, itemTotal, ruleDesc, comm);
                }

                // 3. فحص هل تنطبق شرائح مبيعات إضافية (Tiered / Bonus)
                var dtTiers = GetCommissionTiers(empID);
                foreach (DataRow tr in dtTiers.Rows)
                {
                    decimal min = Convert.ToDecimal(tr["MinTarget"]);
                    decimal max = Convert.ToDecimal(tr["MaxTarget"]);
                    decimal bonus = Convert.ToDecimal(tr["BonusAmount"]);
                    if (totalSalesAmount >= min && (max == 0 || totalSalesAmount <= max))
                    {
                        if (bonus > 0)
                        {
                            totalCommission += bonus;
                            dtResult.Rows.Add(0, "---", to.ToString("yyyy-MM-dd"), "---", "🎁 مكافأة تحقيق شريحة وتارجت المبيعات", 1, bonus, bonus, $"تحقيق مبيعات ({totalSalesAmount:N2} ج)", bonus);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("EmployeeHRDAL.CalculateCommissions", ex);
            }

            return (dtResult, totalCommission, totalSalesAmount);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // 4. مسيرات الرواتب الشهرية والاحتساب التلقائي (Monthly Payroll)
        // ═══════════════════════════════════════════════════════════════════════════

        public static DataTable GetMonthlyPayrollSummary(string monthYear)
        {
            string sql = @"
                SELECT emp.EmpID, emp.EmpName, emp.JobTitle, emp.Role,
                       COALESCE(p.PayrollID, 0) AS PayrollID,
                       COALESCE(p.MonthYear, @my) AS MonthYear,
                       COALESCE(p.BasicSalary, emp.Salary) AS BasicSalary,
                       COALESCE(p.TotalAllowances, 0) AS TotalAllowances,
                       COALESCE(p.TotalBonuses, 0) AS TotalBonuses,
                       COALESCE(p.TotalCommissions, 0) AS TotalCommissions,
                       COALESCE(p.OvertimeAmount, 0) AS OvertimeAmount,
                       COALESCE(p.TotalDeductions, 0) AS TotalDeductions,
                       COALESCE(p.AbsenceDeductions, 0) AS AbsenceDeductions,
                       COALESCE(p.AdvancesDeductions, 0) AS AdvancesDeductions,
                       COALESCE(p.NetSalary, 0) AS NetSalary,
                       COALESCE(p.IsPaid, 0) AS IsPaid,
                       p.PaymentDate,
                       p.Notes
                FROM Employees emp
                LEFT JOIN EmployeeMonthlyPayroll p ON emp.EmpID = p.EmpID AND p.MonthYear = @my
                WHERE emp.IsActive = 1
                ORDER BY emp.EmpName ASC";

            return DbHelper.Query(sql, DbHelper.P("@my", monthYear));
        }

        /// <summary>
        /// احتساب وتوليد مسير رواتب الشهر تلقائياً لكافة الموظفين
        /// </summary>
        public static void GenerateMonthlyPayroll(string monthYear)
        {
            DateTime startMonth = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime endMonth = startMonth.AddMonths(1).AddDays(-1);

            var dtEmps = DbHelper.Query("SELECT EmpID, EmpName, Salary, DailyWorkHours, HourlyRate FROM Employees WHERE IsActive = 1");

            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (DataRow er in dtEmps.Rows)
                {
                    int empId = Convert.ToInt32(er["EmpID"]);
                    decimal basicSalary = er["Salary"] != DBNull.Value ? Convert.ToDecimal(er["Salary"]) : 0m;
                    decimal hourlyRate = er["HourlyRate"] != DBNull.Value && Convert.ToDecimal(er["HourlyRate"]) > 0
                        ? Convert.ToDecimal(er["HourlyRate"])
                        : (basicSalary > 0 ? (basicSalary / 30m / 8m) : 0m);

                    // 1. احتساب البدلات والمكافآت والخصومات والسلف من EmployeeSalaryItems
                    var dtItems = DbHelper.QueryTrans(trans, @"
                        SELECT ItemType, SUM(Amount) AS TotalAmt
                        FROM EmployeeSalaryItems
                        WHERE EmpID = @eid
                          AND (PayrollMonth = @my OR (CAST(ItemDate AS DATE) BETWEEN @from AND @to AND IsSettled = 0))
                        GROUP BY ItemType",
                        DbHelper.P("@eid", empId),
                        DbHelper.P("@my", monthYear),
                        DbHelper.P("@from", startMonth),
                        DbHelper.P("@to", endMonth));

                    decimal allowances = 0m, bonuses = 0m, deductions = 0m, advances = 0m;
                    foreach (DataRow ir in dtItems.Rows)
                    {
                        string type = ir["ItemType"]?.ToString() ?? "";
                        decimal amt = Convert.ToDecimal(ir["TotalAmt"]);
                        if (type == "بدل") allowances += amt;
                        else if (type == "مكافأة") bonuses += amt;
                        else if (type == "خصم / جزاء") deductions += amt;
                        else if (type == "سلفة") advances += amt;
                    }

                    // 2. احتساب الغياب والتأخير والإضافي من الحضور والانصراف
                    var dtAtt = DbHelper.QueryTrans(trans, @"
                        SELECT Status, SUM(OvertimeHours) AS TotOT, SUM(LateMinutes) AS TotLate, COUNT(*) AS DaysCount
                        FROM EmployeeAttendance
                        WHERE EmpID = @eid AND CAST(AttendDate AS DATE) BETWEEN @from AND @to
                        GROUP BY Status",
                        DbHelper.P("@eid", empId),
                        DbHelper.P("@from", startMonth),
                        DbHelper.P("@to", endMonth));

                    decimal overtimeAmt = 0m;
                    decimal absenceDeductions = 0m;
                    decimal dayRate = basicSalary > 0 ? (basicSalary / 30m) : 0m;

                    foreach (DataRow ar in dtAtt.Rows)
                    {
                        string st = ar["Status"]?.ToString() ?? "";
                        int days = Convert.ToInt32(ar["DaysCount"]);
                        decimal ot = ar["TotOT"] != DBNull.Value ? Convert.ToDecimal(ar["TotOT"]) : 0m;
                        int lateMins = ar["TotLate"] != DBNull.Value ? Convert.ToInt32(ar["TotLate"]) : 0;

                        if (st == "غائب") absenceDeductions += (days * dayRate);
                        else if (st == "نصف يوم") absenceDeductions += (days * dayRate * 0.5m);

                        overtimeAmt += (ot * hourlyRate * 1.5m);
                        if (lateMins >= 60)
                        {
                            decimal lateHours = lateMins / 60m;
                            deductions += (lateHours * hourlyRate);
                        }
                    }

                    // 3. احتساب عمولات المبيعات التلقائية
                    var commRes = CalculateCommissions(empId, startMonth, endMonth);
                    decimal commissions = commRes.TotalCommission;

                    // 4. صافي الراتب
                    decimal net = (basicSalary + allowances + bonuses + commissions + overtimeAmt) - (deductions + absenceDeductions + advances);
                    if (net < 0) net = 0;

                    // 5. الحفظ في مسير الرواتب
                    DbHelper.ExecuteTrans(trans, @"
                        IF EXISTS (SELECT 1 FROM EmployeeMonthlyPayroll WHERE EmpID = @eid AND MonthYear = @my)
                        BEGIN
                            UPDATE EmployeeMonthlyPayroll
                            SET BasicSalary = @bs, TotalAllowances = @al, TotalBonuses = @bn,
                                TotalCommissions = @cm, OvertimeAmount = @ot, TotalDeductions = @ded,
                                AbsenceDeductions = @abs, AdvancesDeductions = @adv, NetSalary = @net
                            WHERE EmpID = @eid AND MonthYear = @my AND IsPaid = 0;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO EmployeeMonthlyPayroll
                                (EmpID, MonthYear, BasicSalary, TotalAllowances, TotalBonuses, TotalCommissions,
                                 OvertimeAmount, TotalDeductions, AbsenceDeductions, AdvancesDeductions, NetSalary, IsPaid, CreatedAt)
                            VALUES
                                (@eid, @my, @bs, @al, @bn, @cm, @ot, @ded, @abs, @adv, @net, 0, GETDATE());
                        END",
                        DbHelper.P("@eid", empId),
                        DbHelper.P("@my", monthYear),
                        DbHelper.P("@bs", basicSalary),
                        DbHelper.P("@al", allowances),
                        DbHelper.P("@bn", bonuses),
                        DbHelper.P("@cm", commissions),
                        DbHelper.P("@ot", overtimeAmt),
                        DbHelper.P("@ded", deductions),
                        DbHelper.P("@abs", absenceDeductions),
                        DbHelper.P("@adv", advances),
                        DbHelper.P("@net", net));
                }
            });
        }

        public static bool PaySalary(int payrollID, int safeID, string notes)
        {
            bool success = false;
            DbHelper.RunInTransaction((con, trans) =>
            {
                var dt = DbHelper.QueryTrans(trans, "SELECT EmpID, MonthYear, NetSalary, IsPaid FROM EmployeeMonthlyPayroll WHERE PayrollID = @pid", DbHelper.P("@pid", payrollID));
                if (dt.Rows.Count == 0) throw new Exception("مسير الراتب غير موجود.");
                if (Convert.ToBoolean(dt.Rows[0]["IsPaid"])) throw new Exception("تم صرف هذا الراتب مسبقاً.");

                int empId = Convert.ToInt32(dt.Rows[0]["EmpID"]);
                string mYear = dt.Rows[0]["MonthYear"].ToString();
                decimal netSalary = Convert.ToDecimal(dt.Rows[0]["NetSalary"]);

                if (netSalary > 0)
                {
                    AccountDAL.EnsureSufficientCashTrans(trans, safeID, netSalary, $"صرف صافي راتب شهر {mYear}");
                }

                // 1. تحديث مسير الرواتب
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE EmployeeMonthlyPayroll
                    SET IsPaid = 1, PaymentDate = GETDATE(), PaidFromSafeID = @sid, Notes = @notes, ApprovedBy = @uid
                    WHERE PayrollID = @pid",
                    DbHelper.P("@pid", payrollID),
                    DbHelper.P("@sid", safeID),
                    DbHelper.P("@notes", notes ?? ""),
                    DbHelper.P("@uid", Session.EmpID));

                // 2. تسجيل قيد في حساب الموظف
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO EmployeeTransactions (EmpID, TransDate, TransType, Debit, Credit, Notes, CreatedBy)
                    VALUES (@eid, GETDATE(), N'صرف راتب شهري', @amt, 0, @notes, @uid)",
                    DbHelper.P("@eid", empId),
                    DbHelper.P("@amt", netSalary),
                    DbHelper.P("@notes", "صرف راتب شهر " + mYear + " " + (notes ?? "")),
                    DbHelper.P("@uid", Session.EmpID));

                // 3. تسجيل حركة في الخزينة
                if (netSalary > 0)
                {
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransDate, TransType, AmountIn, AmountOut, RefID, Notes, CreatedBy, AccountID)
                        VALUES (GETDATE(), 'EmpPaymentOut', 0, @amt, @ref, @notes, @uid, @accId)",
                        DbHelper.P("@amt", netSalary),
                        DbHelper.P("@ref", payrollID),
                        DbHelper.P("@notes", "صرف صافي راتب شهر " + mYear + " للموظف ID:" + empId),
                        DbHelper.P("@uid", Session.EmpID),
                        DbHelper.P("@accId", safeID));
                }

                // 4. تسوية البنود الفردية لهذا الشهر
                DbHelper.ExecuteTrans(trans, "UPDATE EmployeeSalaryItems SET IsSettled = 1 WHERE EmpID = @eid AND PayrollMonth = @my",
                    DbHelper.P("@eid", empId), DbHelper.P("@my", mYear));

                success = true;
            });
            return success;
        }
    }
}
