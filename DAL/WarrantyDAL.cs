using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class WarrantyTicket
    {
        public int TicketID { get; set; }
        public string TicketCode { get; set; }
        public DateTime ReceiveDate { get; set; } = DateTime.Now;
        public int? ClientID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public int? ProductID { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string SerialNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Accessories { get; set; }
        public string DeviceCondition { get; set; }
        public string ProblemDescription { get; set; }
        public string VendorName { get; set; }
        public DateTime? SentToVendorDate { get; set; }
        public string VendorTrackingNo { get; set; }
        public DateTime? ReceivedFromVendorDate { get; set; }
        public string VendorReport { get; set; }
        public string ResolutionType { get; set; }
        public DateTime? DeliveredToCustomerDate { get; set; }
        public string Status { get; set; } = "مستلم بالمحل";
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    public class WarrantyStats
    {
        public int TotalCount { get; set; }
        public int AtShopCount { get; set; }
        public int AtVendorCount { get; set; }
        public int ReadyCount { get; set; }
        public int DeliveredCount { get; set; }
    }

    public static class WarrantyDAL
    {
        public static void EnsureTable()
        {
            try
            {
                DbHelper.Execute(@"
                IF OBJECT_ID('WarrantyTickets', 'U') IS NULL
                BEGIN
                    CREATE TABLE WarrantyTickets (
                        TicketID INT IDENTITY(1,1) PRIMARY KEY,
                        TicketCode NVARCHAR(50) NULL,
                        ReceiveDate DATETIME NOT NULL DEFAULT GETDATE(),
                        ClientID INT NULL,
                        CustomerName NVARCHAR(150) NOT NULL,
                        CustomerPhone NVARCHAR(50) NULL,
                        CustomerAddress NVARCHAR(250) NULL,
                        ProductID INT NULL,
                        ProductName NVARCHAR(200) NOT NULL,
                        Brand NVARCHAR(100) NULL,
                        SerialNumber NVARCHAR(100) NULL,
                        InvoiceNumber NVARCHAR(50) NULL,
                        InvoiceDate DATETIME NULL,
                        Accessories NVARCHAR(300) NULL,
                        DeviceCondition NVARCHAR(300) NULL,
                        ProblemDescription NVARCHAR(1000) NULL,
                        VendorName NVARCHAR(150) NULL,
                        SentToVendorDate DATETIME NULL,
                        VendorTrackingNo NVARCHAR(100) NULL,
                        ReceivedFromVendorDate DATETIME NULL,
                        VendorReport NVARCHAR(1000) NULL,
                        ResolutionType NVARCHAR(50) NULL,
                        DeliveredToCustomerDate DATETIME NULL,
                        Status NVARCHAR(50) NOT NULL DEFAULT N'مستلم بالمحل',
                        Notes NVARCHAR(1000) NULL,
                        CreatedBy INT NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME NULL
                    );
                    CREATE INDEX IX_WarrantyTickets_Status ON WarrantyTickets(Status);
                    CREATE INDEX IX_WarrantyTickets_Phone ON WarrantyTickets(CustomerPhone);
                    CREATE INDEX IX_WarrantyTickets_Serial ON WarrantyTickets(SerialNumber);
                END");
            }
            catch (Exception ex)
            {
                AppLogger.Error("WarrantyDAL.EnsureTable", ex);
            }
        }

        public static string GetNextTicketCode()
        {
            EnsureTable();
            try
            {
                var maxObj = DbHelper.Scalar("SELECT ISNULL(MAX(TicketID), 0) + 1 FROM WarrantyTickets");
                int nextId = maxObj != null ? Convert.ToInt32(maxObj) : 1;
                return "WR-" + nextId.ToString("D5");
            }
            catch
            {
                return "WR-00001";
            }
        }

        public static DataTable GetAll(string search = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            EnsureTable();
            var pList = new List<SqlParameter>();
            string sql = "SELECT * FROM WarrantyTickets WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (CustomerName LIKE @s OR CustomerPhone LIKE @s OR ProductName LIKE @s OR SerialNumber LIKE @s OR TicketCode LIKE @s OR InvoiceNumber LIKE @s OR VendorName LIKE @s) ";
                pList.Add(DbHelper.P("@s", "%" + search.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                sql += " AND Status = @st ";
                pList.Add(DbHelper.P("@st", status.Trim()));
            }

            if (fromDate.HasValue)
            {
                sql += " AND ReceiveDate >= @from ";
                pList.Add(DbHelper.P("@from", fromDate.Value.Date));
            }

            if (toDate.HasValue)
            {
                sql += " AND ReceiveDate <= @to ";
                pList.Add(DbHelper.P("@to", toDate.Value.Date.AddDays(1).AddSeconds(-1)));
            }

            sql += " ORDER BY TicketID DESC";
            return DbHelper.Query(sql, pList.ToArray());
        }

        public static WarrantyTicket GetByID(int ticketID)
        {
            EnsureTable();
            var dt = DbHelper.Query("SELECT * FROM WarrantyTickets WHERE TicketID = @id", DbHelper.P("@id", ticketID));
            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];

            return new WarrantyTicket
            {
                TicketID = Convert.ToInt32(r["TicketID"]),
                TicketCode = r["TicketCode"]?.ToString(),
                ReceiveDate = r["ReceiveDate"] != DBNull.Value ? Convert.ToDateTime(r["ReceiveDate"]) : DateTime.Now,
                ClientID = r["ClientID"] != DBNull.Value ? (int?)Convert.ToInt32(r["ClientID"]) : null,
                CustomerName = r["CustomerName"]?.ToString(),
                CustomerPhone = r["CustomerPhone"]?.ToString(),
                CustomerAddress = r["CustomerAddress"]?.ToString(),
                ProductID = r["ProductID"] != DBNull.Value ? (int?)Convert.ToInt32(r["ProductID"]) : null,
                ProductName = r["ProductName"]?.ToString(),
                Brand = r["Brand"]?.ToString(),
                SerialNumber = r["SerialNumber"]?.ToString(),
                InvoiceNumber = r["InvoiceNumber"]?.ToString(),
                InvoiceDate = r["InvoiceDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["InvoiceDate"]) : null,
                Accessories = r["Accessories"]?.ToString(),
                DeviceCondition = r["DeviceCondition"]?.ToString(),
                ProblemDescription = r["ProblemDescription"]?.ToString(),
                VendorName = r["VendorName"]?.ToString(),
                SentToVendorDate = r["SentToVendorDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["SentToVendorDate"]) : null,
                VendorTrackingNo = r["VendorTrackingNo"]?.ToString(),
                ReceivedFromVendorDate = r["ReceivedFromVendorDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["ReceivedFromVendorDate"]) : null,
                VendorReport = r["VendorReport"]?.ToString(),
                ResolutionType = r["ResolutionType"]?.ToString(),
                DeliveredToCustomerDate = r["DeliveredToCustomerDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["DeliveredToCustomerDate"]) : null,
                Status = r["Status"]?.ToString() ?? "مستلم بالمحل",
                Notes = r["Notes"]?.ToString(),
                CreatedBy = r["CreatedBy"] != DBNull.Value ? (int?)Convert.ToInt32(r["CreatedBy"]) : null,
                CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
            };
        }

        public static int Save(WarrantyTicket ticket)
        {
            EnsureTable();
            if (string.IsNullOrWhiteSpace(ticket.TicketCode))
            {
                ticket.TicketCode = GetNextTicketCode();
            }

            if (ticket.TicketID <= 0)
            {
                string insertSql = @"
                INSERT INTO WarrantyTickets
                (TicketCode, ReceiveDate, ClientID, CustomerName, CustomerPhone, CustomerAddress,
                 ProductID, ProductName, Brand, SerialNumber, InvoiceNumber, InvoiceDate,
                 Accessories, DeviceCondition, ProblemDescription, VendorName, SentToVendorDate,
                 VendorTrackingNo, ReceivedFromVendorDate, VendorReport, ResolutionType,
                 DeliveredToCustomerDate, Status, Notes, CreatedBy, CreatedAt)
                VALUES
                (@code, @recDate, @cid, @cname, @phone, @addr,
                 @pid, @pname, @brand, @sn, @invNo, @invDate,
                 @acc, @cond, @prob, @vname, @sentDate,
                 @vtrack, @recvDate, @vrep, @resol,
                 @delivDate, @status, @notes, @by, GETDATE());
                SELECT SCOPE_IDENTITY();";

                var idObj = DbHelper.Scalar(insertSql,
                    DbHelper.P("@code", ticket.TicketCode),
                    DbHelper.P("@recDate", ticket.ReceiveDate),
                    DbHelper.P("@cid", ticket.ClientID.HasValue ? (object)ticket.ClientID.Value : DBNull.Value),
                    DbHelper.P("@cname", ticket.CustomerName ?? ""),
                    DbHelper.P("@phone", ticket.CustomerPhone ?? ""),
                    DbHelper.P("@addr", ticket.CustomerAddress ?? ""),
                    DbHelper.P("@pid", ticket.ProductID.HasValue ? (object)ticket.ProductID.Value : DBNull.Value),
                    DbHelper.P("@pname", ticket.ProductName ?? ""),
                    DbHelper.P("@brand", ticket.Brand ?? ""),
                    DbHelper.P("@sn", ticket.SerialNumber ?? ""),
                    DbHelper.P("@invNo", ticket.InvoiceNumber ?? ""),
                    DbHelper.P("@invDate", ticket.InvoiceDate.HasValue ? (object)ticket.InvoiceDate.Value : DBNull.Value),
                    DbHelper.P("@acc", ticket.Accessories ?? ""),
                    DbHelper.P("@cond", ticket.DeviceCondition ?? ""),
                    DbHelper.P("@prob", ticket.ProblemDescription ?? ""),
                    DbHelper.P("@vname", ticket.VendorName ?? ""),
                    DbHelper.P("@sentDate", ticket.SentToVendorDate.HasValue ? (object)ticket.SentToVendorDate.Value : DBNull.Value),
                    DbHelper.P("@vtrack", ticket.VendorTrackingNo ?? ""),
                    DbHelper.P("@recvDate", ticket.ReceivedFromVendorDate.HasValue ? (object)ticket.ReceivedFromVendorDate.Value : DBNull.Value),
                    DbHelper.P("@vrep", ticket.VendorReport ?? ""),
                    DbHelper.P("@resol", ticket.ResolutionType ?? ""),
                    DbHelper.P("@delivDate", ticket.DeliveredToCustomerDate.HasValue ? (object)ticket.DeliveredToCustomerDate.Value : DBNull.Value),
                    DbHelper.P("@status", ticket.Status ?? "مستلم بالمحل"),
                    DbHelper.P("@notes", ticket.Notes ?? ""),
                    DbHelper.P("@by", Session.EmpID > 0 ? (object)Session.EmpID : DBNull.Value)
                );
                return Convert.ToInt32(idObj);
            }
            else
            {
                string updateSql = @"
                UPDATE WarrantyTickets SET
                    ReceiveDate = @recDate,
                    ClientID = @cid,
                    CustomerName = @cname,
                    CustomerPhone = @phone,
                    CustomerAddress = @addr,
                    ProductID = @pid,
                    ProductName = @pname,
                    Brand = @brand,
                    SerialNumber = @sn,
                    InvoiceNumber = @invNo,
                    InvoiceDate = @invDate,
                    Accessories = @acc,
                    DeviceCondition = @cond,
                    ProblemDescription = @prob,
                    VendorName = @vname,
                    SentToVendorDate = @sentDate,
                    VendorTrackingNo = @vtrack,
                    ReceivedFromVendorDate = @recvDate,
                    VendorReport = @vrep,
                    ResolutionType = @resol,
                    DeliveredToCustomerDate = @delivDate,
                    Status = @status,
                    Notes = @notes,
                    UpdatedAt = GETDATE()
                WHERE TicketID = @id";

                DbHelper.Execute(updateSql,
                    DbHelper.P("@id", ticket.TicketID),
                    DbHelper.P("@recDate", ticket.ReceiveDate),
                    DbHelper.P("@cid", ticket.ClientID.HasValue ? (object)ticket.ClientID.Value : DBNull.Value),
                    DbHelper.P("@cname", ticket.CustomerName ?? ""),
                    DbHelper.P("@phone", ticket.CustomerPhone ?? ""),
                    DbHelper.P("@addr", ticket.CustomerAddress ?? ""),
                    DbHelper.P("@pid", ticket.ProductID.HasValue ? (object)ticket.ProductID.Value : DBNull.Value),
                    DbHelper.P("@pname", ticket.ProductName ?? ""),
                    DbHelper.P("@brand", ticket.Brand ?? ""),
                    DbHelper.P("@sn", ticket.SerialNumber ?? ""),
                    DbHelper.P("@invNo", ticket.InvoiceNumber ?? ""),
                    DbHelper.P("@invDate", ticket.InvoiceDate.HasValue ? (object)ticket.InvoiceDate.Value : DBNull.Value),
                    DbHelper.P("@acc", ticket.Accessories ?? ""),
                    DbHelper.P("@cond", ticket.DeviceCondition ?? ""),
                    DbHelper.P("@prob", ticket.ProblemDescription ?? ""),
                    DbHelper.P("@vname", ticket.VendorName ?? ""),
                    DbHelper.P("@sentDate", ticket.SentToVendorDate.HasValue ? (object)ticket.SentToVendorDate.Value : DBNull.Value),
                    DbHelper.P("@vtrack", ticket.VendorTrackingNo ?? ""),
                    DbHelper.P("@recvDate", ticket.ReceivedFromVendorDate.HasValue ? (object)ticket.ReceivedFromVendorDate.Value : DBNull.Value),
                    DbHelper.P("@vrep", ticket.VendorReport ?? ""),
                    DbHelper.P("@resol", ticket.ResolutionType ?? ""),
                    DbHelper.P("@delivDate", ticket.DeliveredToCustomerDate.HasValue ? (object)ticket.DeliveredToCustomerDate.Value : DBNull.Value),
                    DbHelper.P("@status", ticket.Status ?? "مستلم بالمحل"),
                    DbHelper.P("@notes", ticket.Notes ?? "")
                );
                return ticket.TicketID;
            }
        }

        public static void UpdateStatus(int ticketID, string newStatus, string vendorName = null, string trackingNo = null, string vendorReport = null, string resolutionType = null, DateTime? deliveredDate = null)
        {
            EnsureTable();
            string sql = "UPDATE WarrantyTickets SET Status = @st, UpdatedAt = GETDATE() ";
            var pList = new List<SqlParameter> { DbHelper.P("@st", newStatus), DbHelper.P("@id", ticketID) };

            if (!string.IsNullOrEmpty(vendorName))
            {
                sql += ", VendorName = @vn ";
                pList.Add(DbHelper.P("@vn", vendorName));
            }
            if (!string.IsNullOrEmpty(trackingNo))
            {
                sql += ", VendorTrackingNo = @vt ";
                pList.Add(DbHelper.P("@vt", trackingNo));
            }
            if (newStatus == "مرسل للتوكيل")
            {
                sql += ", SentToVendorDate = ISNULL(SentToVendorDate, GETDATE()) ";
            }
            if (newStatus.Contains("جاهز") || newStatus.Contains("مستلم من التوكيل"))
            {
                sql += ", ReceivedFromVendorDate = ISNULL(ReceivedFromVendorDate, GETDATE()) ";
            }
            if (!string.IsNullOrEmpty(vendorReport))
            {
                sql += ", VendorReport = @vr ";
                pList.Add(DbHelper.P("@vr", vendorReport));
            }
            if (!string.IsNullOrEmpty(resolutionType))
            {
                sql += ", ResolutionType = @res ";
                pList.Add(DbHelper.P("@res", resolutionType));
            }
            if (deliveredDate.HasValue)
            {
                sql += ", DeliveredToCustomerDate = @dd ";
                pList.Add(DbHelper.P("@dd", deliveredDate.Value));
            }
            else if (newStatus.Contains("تم التسليم"))
            {
                sql += ", DeliveredToCustomerDate = ISNULL(DeliveredToCustomerDate, GETDATE()) ";
            }

            sql += " WHERE TicketID = @id";
            DbHelper.Execute(sql, pList.ToArray());
        }

        public static void Delete(int ticketID)
        {
            EnsureTable();
            DbHelper.Execute("DELETE FROM WarrantyTickets WHERE TicketID = @id", DbHelper.P("@id", ticketID));
        }

        public static WarrantyStats GetStats()
        {
            EnsureTable();
            var stats = new WarrantyStats();
            try
            {
                string sql = @"
                SELECT 
                    COUNT(*) AS TotalCount,
                    SUM(CASE WHEN Status = N'مستلم بالمحل' THEN 1 ELSE 0 END) AS AtShopCount,
                    SUM(CASE WHEN Status = N'مرسل للتوكيل' THEN 1 ELSE 0 END) AS AtVendorCount,
                    SUM(CASE WHEN Status LIKE N'%جاهز%' OR Status LIKE N'%مستلم من التوكيل%' THEN 1 ELSE 0 END) AS ReadyCount,
                    SUM(CASE WHEN Status LIKE N'%تم التسليم%' THEN 1 ELSE 0 END) AS DeliveredCount
                FROM WarrantyTickets";

                var dt = DbHelper.Query(sql);
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    stats.TotalCount = r["TotalCount"] != DBNull.Value ? Convert.ToInt32(r["TotalCount"]) : 0;
                    stats.AtShopCount = r["AtShopCount"] != DBNull.Value ? Convert.ToInt32(r["AtShopCount"]) : 0;
                    stats.AtVendorCount = r["AtVendorCount"] != DBNull.Value ? Convert.ToInt32(r["AtVendorCount"]) : 0;
                    stats.ReadyCount = r["ReadyCount"] != DBNull.Value ? Convert.ToInt32(r["ReadyCount"]) : 0;
                    stats.DeliveredCount = r["DeliveredCount"] != DBNull.Value ? Convert.ToInt32(r["DeliveredCount"]) : 0;
                }
            }
            catch { }
            return stats;
        }

        public static DataTable SearchSalesByQuery(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return new DataTable();
            q = q.Trim();
            string sql = @"
            SELECT TOP 20 
                s.SaleID, s.SaleCode, s.SaleDate, s.ClientName, s.Phone,
                si.ProductID, si.ProductName, si.Quantity, si.UnitPrice, si.IMEI
            FROM Sales s
            INNER JOIN SaleItems si ON s.SaleID = si.SaleID
            WHERE s.SaleCode = @exact 
               OR s.Phone LIKE @like 
               OR s.ClientName LIKE @like 
               OR si.ProductName LIKE @like 
               OR si.IMEI = @exact
            ORDER BY s.SaleID DESC";

            return DbHelper.Query(sql, 
                DbHelper.P("@exact", q), 
                DbHelper.P("@like", "%" + q + "%"));
        }

        public static DataTable GetClientPurchasedItems(int? clientID, string customerPhone = null, string customerName = null)
        {
            try
            {
                string sql = @"
                SELECT TOP 100
                    s.SaleID,
                    s.SaleCode,
                    s.SaleDate,
                    si.ProductID,
                    si.ProductName,
                    ISNULL(p.Brand, N'') AS Brand,
                    ISNULL(si.IMEI, N'') AS IMEI,
                    si.Quantity,
                    si.UnitPrice,
                    ISNULL(s.ClientName, N'') AS ClientName,
                    ISNULL(s.Phone, N'') AS Phone
                FROM SaleItems si
                INNER JOIN Sales s ON si.SaleID = s.SaleID
                LEFT JOIN Products p ON si.ProductID = p.ProductID
                WHERE 1 = 1 ";

                var pList = new List<SqlParameter>();
                if (clientID.HasValue && clientID.Value > 0)
                {
                    sql += " AND s.ClientID = @cid ";
                    pList.Add(DbHelper.P("@cid", clientID.Value));
                }
                else if (!string.IsNullOrWhiteSpace(customerPhone))
                {
                    sql += " AND (s.Phone = @phone OR s.Phone LIKE @phoneLike) ";
                    pList.Add(DbHelper.P("@phone", customerPhone.Trim()));
                    pList.Add(DbHelper.P("@phoneLike", "%" + customerPhone.Trim() + "%"));
                }
                else if (!string.IsNullOrWhiteSpace(customerName))
                {
                    sql += " AND s.ClientName LIKE @name ";
                    pList.Add(DbHelper.P("@name", "%" + customerName.Trim() + "%"));
                }
                else
                {
                    sql += " AND 1 = 0 ";
                }

                sql += " ORDER BY s.SaleDate DESC, s.SaleID DESC";
                return DbHelper.Query(sql, pList.ToArray());
            }
            catch (Exception ex)
            {
                AppLogger.Error("WarrantyDAL.GetClientPurchasedItems", ex);
                return new DataTable();
            }
        }
    }
}
