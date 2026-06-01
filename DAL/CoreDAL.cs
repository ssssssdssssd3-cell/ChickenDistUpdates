using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    // =================== Employee DAL ===================
    public static class EmployeeDAL
    {
        public static DataTable GetAll()
        {
            return DbHelper.Query("SELECT EmpID,EmpName,UserName,Role,Phone,IsActive,IsDriver FROM Employees ORDER BY EmpName");
        }

        public static DataTable GetDrivers()
        {
            return DbHelper.Query("SELECT EmpID,EmpName FROM Employees WHERE IsDriver=1 AND IsActive=1 ORDER BY EmpName");
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Employees WHERE EmpID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static DataRow Login(string username, string password)
        {
            var dt = DbHelper.Query(
                "SELECT * FROM Employees WHERE UserName=@u AND Password=@p AND IsActive=1",
                DbHelper.P("@u", username), DbHelper.P("@p", password));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string name, string username, string password, string role, string phone, bool isDriver, bool isActive)
        {
            if (id == 0)
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Employees(EmpName,UserName,Password,Role,Phone,IsDriver,IsActive) VALUES(@n,@u,@p,@r,@ph,@dr,@a)",
                    DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", password),
                    DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive));
            else
            {
                DbHelper.Execute(
                    "UPDATE Employees SET EmpName=@n,UserName=@u,Role=@r,Phone=@ph,IsDriver=@dr,IsActive=@a" +
                    (string.IsNullOrWhiteSpace(password) ? "" : ",Password=@p") + " WHERE EmpID=@id",
                    string.IsNullOrWhiteSpace(password)
                        ? new[] { DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive), DbHelper.P("@id", id) }
                        : new[] { DbHelper.P("@n", name), DbHelper.P("@u", username), DbHelper.P("@p", password), DbHelper.P("@r", role), DbHelper.P("@ph", phone), DbHelper.P("@dr", isDriver), DbHelper.P("@a", isActive), DbHelper.P("@id", id) });
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Employees SET IsActive=0 WHERE EmpID=@id", DbHelper.P("@id", id));
        }

        // Permissions
        public static DataTable GetPermissions(int empID)
        {
            return DbHelper.Query("SELECT ScreenName,CanAccess,CanEditPrice FROM Permissions WHERE EmpID=@id", DbHelper.P("@id", empID));
        }

        public static void SavePermissions(int empID, string screen, bool canAccess, bool canEditPrice)
        {
            var exists = DbHelper.Scalar("SELECT COUNT(*) FROM Permissions WHERE EmpID=@e AND ScreenName=@s",
                DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            if (Convert.ToInt32(exists) > 0)
                DbHelper.Execute("UPDATE Permissions SET CanAccess=@a,CanEditPrice=@ep WHERE EmpID=@e AND ScreenName=@s",
                    DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice), DbHelper.P("@e", empID), DbHelper.P("@s", screen));
            else
                DbHelper.Execute("INSERT INTO Permissions(EmpID,ScreenName,CanAccess,CanEditPrice) VALUES(@e,@s,@a,@ep)",
                    DbHelper.P("@e", empID), DbHelper.P("@s", screen), DbHelper.P("@a", canAccess), DbHelper.P("@ep", canEditPrice));
        }
    }

    // =================== Product DAL ===================
    public static class ProductDAL
    {
        public static string GetNextProductCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ProductID), 0) + 1 FROM Products");
            return result != null ? result.ToString() : "1";
        }

        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = activeOnly
                ? "SELECT ProductID,ProductCode,ProductName,Unit,SalePrice,PurchasePrice,MinStockLimit,Description FROM Products WHERE IsActive=1 ORDER BY ProductName"
                : "SELECT ProductID,ProductCode,ProductName,Unit,SalePrice,PurchasePrice,MinStockLimit,Description,IsActive FROM Products ORDER BY ProductName";
            return DbHelper.Query(sql);
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Products WHERE ProductID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Save(int id, string code, string name, string unit, decimal price, bool active, decimal purchasePrice, decimal minStockLimit, string description)
        {
            if (id == 0)
                return DbHelper.ExecuteInsert(
                    "INSERT INTO Products(ProductCode,ProductName,Unit,SalePrice,IsActive,PurchasePrice,MinStockLimit,Description) VALUES(@c,@n,@u,@p,@a,@pp,@msl,@d)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description));
            else
            {
                DbHelper.Execute("UPDATE Products SET ProductCode=@c,ProductName=@n,Unit=@u,SalePrice=@p,IsActive=@a,PurchasePrice=@pp,MinStockLimit=@msl,Description=@d WHERE ProductID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@u", unit), DbHelper.P("@p", price), DbHelper.P("@a", active),
                    DbHelper.P("@pp", purchasePrice), DbHelper.P("@msl", minStockLimit), DbHelper.P("@d", description), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Products SET IsActive=0 WHERE ProductID=@id", DbHelper.P("@id", id));
        }
    }

    // =================== Supplier DAL ===================
    public static class SupplierDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = @"SELECT s.SupplierID, s.SupplierCode, s.SupplierName, s.Phone, s.Address,
                           s.OpeningBalance, s.IsActive, ISNULL(sb.Balance, s.OpeningBalance) AS Balance
                           FROM Suppliers s
                           LEFT JOIN vw_SupplierBalance sb ON s.SupplierID = sb.SupplierID
                           " + (activeOnly ? "WHERE s.IsActive=1" : "") + " ORDER BY s.SupplierName";
            return DbHelper.Query(sql);
        }

        public static string GetNextSupplierCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(SupplierID), 0) + 1 FROM Suppliers");
            return result != null ? result.ToString() : "1";
        }

        public static int Save(int id, string code, string name, string phone, string address, decimal opening, bool active)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Suppliers(SupplierCode,SupplierName,Phone,Address,OpeningBalance,IsActive) VALUES(@c,@n,@ph,@a,@ob,@act)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active));
                return newID;
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Suppliers SET SupplierCode=@c,SupplierName=@n,Phone=@ph,Address=@a,IsActive=@act WHERE SupplierID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone),
                    DbHelper.P("@a", address), DbHelper.P("@act", active), DbHelper.P("@id", id));
                return id;
            }
        }

        public static void Delete(int id)
        {
            DbHelper.Execute("UPDATE Suppliers SET IsActive=0 WHERE SupplierID=@id", DbHelper.P("@id", id));
        }

        /// <summary>
        /// صرف نقدي للمورد - يسجل في حساب المورد (Debit يقلل المديونية) وفي الخزنة (AmountOut)
        /// بكود قيد تلقائي مثل SPY-0001
        /// </summary>
        public static string AddSupplierPayment(int supplierID, decimal amount, string notes)
        {
            string payCode = "";
            DbHelper.RunInTransaction((con, trans) =>
            {
                // توليد كود القيد التسلسلي SPY-XXXX
                var nextResult = DbHelper.ScalarTrans(trans,
                    "SELECT COALESCE(MAX(TransID), 0) + 1 FROM SupplierTransactions");
                int nextNum = nextResult != null ? Convert.ToInt32(nextResult) : 1;
                payCode = "SPY-" + nextNum.ToString("D4");

                // التحقق من رصيد الخزنة قبل الصرف
                var cashResult = DbHelper.ScalarTrans(trans,
                    "SELECT ISNULL(SUM(AmountIn),0) - ISNULL(SUM(AmountOut),0) FROM CashBox");
                decimal cashBalance = cashResult != null ? Convert.ToDecimal(cashResult) : 0;
                if (cashBalance < amount)
                    throw new Exception(
                        $"رصيد الخزنة ({cashBalance:N2} ج) لا يكفي للصرف ({amount:N2} ج).\nيرجى تحصيل نقدية أولاً.");

                // تسجيل في حساب المورد: Debit يقلل المديونية (صرفنا له)
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO SupplierTransactions(SupplierID,TransType,Debit,Notes,CreatedBy) " +
                    "VALUES(@sid,'Payment',@amt,@n,@by)",
                    DbHelper.P("@sid", supplierID),
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", payCode + " - " + notes),
                    DbHelper.P("@by", Session.EmpID));

                // تسجيل الخصم من الخزنة
                DbHelper.ExecuteTrans(trans,
                    "INSERT INTO CashBox(TransType,AmountOut,Notes,CreatedBy) " +
                    "VALUES('SupplierPayment',@amt,@n,@by)",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@n", payCode + " - صرف للمورد - " + notes),
                    DbHelper.P("@by", Session.EmpID));
            });
            return payCode;
        }

        /// <summary>كشف حساب المورد في فترة زمنية</summary>
        public static DataTable GetStatement(int supplierID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT TransDate, TransType, Debit, Credit, Notes
                  FROM SupplierTransactions
                  WHERE SupplierID=@id AND CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@id", supplierID),
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date));
        }
    }

    // =================== Client DAL ===================
    public static class ClientDAL
    {
        public static DataTable GetAll(bool activeOnly = false)
        {
            string sql = @"SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.Address,
                           c.OpeningBalance, c.IsActive, c.DriverID, c.MaxCreditLimit, c.Notes,
                           ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                           FROM Clients c
                           LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                           " + (activeOnly ? "WHERE c.IsActive=1" : "") + " ORDER BY c.ClientName";
            return DbHelper.Query(sql);
        }

        public static DataTable Search(string term)
        {
            return DbHelper.Query(
                @"SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.Address, c.DriverID, c.MaxCreditLimit, c.Notes,
                  ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                  FROM Clients c LEFT JOIN vw_ClientBalance cb ON c.ClientID=cb.ClientID
                  WHERE c.ClientName LIKE @t OR c.Phone LIKE @t OR c.ClientCode LIKE @t OR c.Phone2 LIKE @t",
                DbHelper.P("@t", "%" + term + "%"));
        }

        public static DataRow GetByID(int id)
        {
            var dt = DbHelper.Query("SELECT * FROM Clients WHERE ClientID=@id", DbHelper.P("@id", id));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static decimal GetClientBalance(int clientID)
        {
            var dt = DbHelper.Query(@"
                SELECT ISNULL(cb.Balance, c.OpeningBalance) AS Balance
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                WHERE c.ClientID = @id", DbHelper.P("@id", clientID));
            return dt.Rows.Count > 0 ? Convert.ToDecimal(dt.Rows[0]["Balance"]) : 0;
        }

        public class ClientFinancialStatus
        {
            public decimal Balance { get; set; }
            public decimal MaxCreditLimit { get; set; }
            public decimal OldDebt30 { get; set; }
        }

        public static ClientFinancialStatus GetFinancialStatus(int clientID)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    ISNULL(cb.Balance, c.OpeningBalance) AS Balance,
                    ISNULL(c.MaxCreditLimit, 0) AS MaxCreditLimit,
                    ISNULL((SELECT SUM(Debit) FROM ClientTransactions WHERE ClientID=@id AND TransDate >= DATEADD(day, -30, GETDATE())), 0) AS RecentDebit
                FROM Clients c
                LEFT JOIN vw_ClientBalance cb ON c.ClientID = cb.ClientID
                WHERE c.ClientID = @id", DbHelper.P("@id", clientID));

            if (dt.Rows.Count > 0)
            {
                decimal bal = Convert.ToDecimal(dt.Rows[0]["Balance"]);
                decimal recentDebit = Convert.ToDecimal(dt.Rows[0]["RecentDebit"]);
                return new ClientFinancialStatus
                {
                    Balance = bal,
                    MaxCreditLimit = Convert.ToDecimal(dt.Rows[0]["MaxCreditLimit"]),
                    OldDebt30 = bal > recentDebit ? bal - recentDebit : 0
                };
            }
            return new ClientFinancialStatus();
        }

        public static string GetNextClientCode()
        {
            var result = DbHelper.Scalar("SELECT COALESCE(MAX(ClientID), 0) + 1 FROM Clients");
            return result != null ? result.ToString() : "1";
        }

        public static int Save(int id, string code, string name, string phone, string phone2, string address, decimal opening, bool active, int? driverID, decimal maxCreditLimit, string notes)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Clients(ClientCode,ClientName,Phone,Phone2,Address,OpeningBalance,IsActive,DriverID,MaxCreditLimit,Notes) VALUES(@c,@n,@ph,@ph2,@a,@ob,@act,@dr,@mcl,@notes)",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@ob", opening), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes));
                return newID;
            }
            else
            {
                DbHelper.Execute(
                    "UPDATE Clients SET ClientCode=@c,ClientName=@n,Phone=@ph,Phone2=@ph2,Address=@a,IsActive=@act,DriverID=@dr,MaxCreditLimit=@mcl,Notes=@notes WHERE ClientID=@id",
                    DbHelper.P("@c", code), DbHelper.P("@n", name), DbHelper.P("@ph", phone), DbHelper.P("@ph2", phone2),
                    DbHelper.P("@a", address), DbHelper.P("@act", active),
                    DbHelper.P("@dr", driverID.HasValue ? (object)driverID.Value : DBNull.Value),
                    DbHelper.P("@mcl", maxCreditLimit), DbHelper.P("@notes", notes),
                    DbHelper.P("@id", id));
                return id;
            }
        }

        public static DataTable GetStatement(int clientID, DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT TransDate, TransType, Debit, Credit, Notes, RefID
                  FROM ClientTransactions
                  WHERE ClientID=@id AND CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@id", clientID), DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static decimal GetPreviousBalance(int clientID, DateTime beforeDate)
        {
            var dt = DbHelper.Query(@"
                SELECT 
                    c.OpeningBalance + 
                    ISNULL((SELECT SUM(Debit) - SUM(Credit) FROM ClientTransactions WHERE ClientID=@id AND CAST(TransDate AS DATE) < @dt), 0) AS PrevBal
                FROM Clients c WHERE c.ClientID=@id", 
                DbHelper.P("@id", clientID), DbHelper.P("@dt", beforeDate.Date));
            if (dt.Rows.Count > 0 && dt.Rows[0]["PrevBal"] != DBNull.Value)
                return Convert.ToDecimal(dt.Rows[0]["PrevBal"]);
            return 0;
        }

        public static void AddPayment(int clientID, decimal amount, string notes)
        {
            DbHelper.Execute(
                "INSERT INTO ClientTransactions(ClientID,TransType,Credit,Notes,CreatedBy) VALUES(@id,'Payment',@amt,@n,@by)",
                DbHelper.P("@id", clientID), DbHelper.P("@amt", amount),
                DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));

            DbHelper.Execute(
                "INSERT INTO CashBox(TransType,AmountIn,Notes,CreatedBy) VALUES('ClientPayment',@amt,@n,@by)",
                DbHelper.P("@amt", amount),
                DbHelper.P("@n", "تحصيل من عميل - " + notes),
                DbHelper.P("@by", Session.EmpID));
        }
    }

    // =================== Account DAL ===================
    public static class AccountDAL
    {
        public static DataTable GetCashBox(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT CashID, TransDate, TransType, AmountIn, AmountOut,
                  (AmountIn - AmountOut) AS Net, Notes
                  FROM CashBox
                  WHERE CAST(TransDate AS DATE) BETWEEN @f AND @t
                  ORDER BY TransDate",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static decimal GetCashBalance()
        {
            var result = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn)-SUM(AmountOut),0) FROM CashBox");
            return result == null ? 0 : Convert.ToDecimal(result);
        }

        public static void SaveCashReceipt(int? clientID, decimal amount, DateTime date, string notes)
        {
            if (clientID.HasValue)
            {
                ClientDAL.AddPayment(clientID.Value, amount, notes);
            }
            else
            {
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransDate,TransType,AmountIn,Notes,CreatedBy) VALUES(@d,'Deposit',@a,@n,@by)",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount),
                    DbHelper.P("@n", "توريد نقدية - " + notes), DbHelper.P("@by", Session.EmpID));
            }
        }

        public static DataTable GetExpenses(DateTime from, DateTime to)
        {
            return DbHelper.Query(
                @"SELECT ExpenseID, ExpenseDate, ExpenseType, Amount, Notes
                  FROM Expenses
                  WHERE CAST(ExpenseDate AS DATE) BETWEEN @f AND @t
                  ORDER BY ExpenseDate",
                DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date));
        }

        public static int SaveExpense(int id, DateTime date, string type, decimal amount, string notes)
        {
            if (id == 0)
            {
                int newID = DbHelper.ExecuteInsert(
                    "INSERT INTO Expenses(ExpenseDate,ExpenseType,Amount,Notes,CreatedBy) VALUES(@d,@t,@a,@n,@by)",
                    DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                    DbHelper.P("@n", notes), DbHelper.P("@by", Session.EmpID));
                DbHelper.Execute(
                    "INSERT INTO CashBox(TransDate,TransType,AmountOut,RefID,Notes,CreatedBy) VALUES(@d,'Expense',@a,@ref,@n,@by)",
                    DbHelper.P("@d", date), DbHelper.P("@a", amount), DbHelper.P("@ref", newID),
                    DbHelper.P("@n", "مصروف: " + type), DbHelper.P("@by", Session.EmpID));
                return newID;
            }
            DbHelper.Execute("UPDATE Expenses SET ExpenseDate=@d,ExpenseType=@t,Amount=@a,Notes=@n WHERE ExpenseID=@id",
                DbHelper.P("@d", date), DbHelper.P("@t", type), DbHelper.P("@a", amount),
                DbHelper.P("@n", notes), DbHelper.P("@id", id));
            return id;
        }

        public static void DeleteExpense(int id)
        {
            DbHelper.Execute("DELETE FROM Expenses WHERE ExpenseID=@id", DbHelper.P("@id", id));
        }
    }
}

