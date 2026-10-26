using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChickenDist.DAL;

namespace ChickenDist.Core
{
    /// <summary>
    /// مدير جلسات وأجهزة البرنامج: يسجل مواعيد فتح وإغلاق البرنامج على كل جهاز/لاب توب بدقة،
    /// ويفحص الفروقات الزمنية لضمان سلامة التقفيلات ومنع التواريخ الخاطئة.
    /// </summary>
    public static class DeviceSessionManager
    {
        private static int? _currentSessionID;
        private static System.Threading.Timer _heartbeatTimer;
        private static TimeSpan _serverOffset = TimeSpan.Zero;
        private static bool _offsetCalculated = false;
        private static readonly object _lock = new object();

        public static int? CurrentSessionID => _currentSessionID;

        /// <summary>
        /// الحصول على التوقيت الحقيقي للسيرفر المركزي دائماً
        /// </summary>
        public static DateTime GetServerDateTime()
        {
            if (!_offsetCalculated)
            {
                lock (_lock)
                {
                    if (!_offsetCalculated)
                    {
                        try
                        {
                            var obj = DbHelper.Scalar("SELECT GETDATE()");
                            if (obj != null && obj != DBNull.Value && DateTime.TryParse(obj.ToString(), out DateTime srvDt))
                            {
                                _serverOffset = srvDt - DateTime.Now;
                                _offsetCalculated = true;
                            }
                        }
                        catch { }
                    }
                }
            }
            return DateTime.Now.Add(_serverOffset);
        }

        /// <summary>
        /// إنشاء وتحديث هيكل جدول جلسات الأجهزة
        /// </summary>
        public static void EnsureSchema()
        {
            try
            {
                DbHelper.Execute(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DeviceSessions')
                    BEGIN
                        CREATE TABLE DeviceSessions (
                            SessionID INT IDENTITY(1,1) PRIMARY KEY,
                            MachineName NVARCHAR(100) NOT NULL,
                            EmpID INT NOT NULL,
                            EmpName NVARCHAR(100) NOT NULL,
                            UserName NVARCHAR(100) NULL,
                            LoginTime DATETIME NOT NULL,
                            LogoutTime DATETIME NULL,
                            ServerTimeAtLogin DATETIME NOT NULL DEFAULT GETDATE(),
                            LastHeartbeat DATETIME NOT NULL DEFAULT GETDATE(),
                            LocalMachineTime DATETIME NULL,
                            AppVersion NVARCHAR(50) NULL,
                            IPAddress NVARCHAR(50) NULL,
                            Status NVARCHAR(30) NOT NULL DEFAULT 'Active',
                            Notes NVARCHAR(250) NULL
                        );
                        CREATE INDEX IX_DeviceSessions_Login ON DeviceSessions(LoginTime DESC);
                        CREATE INDEX IX_DeviceSessions_Machine ON DeviceSessions(MachineName, Status);
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DeviceSessions') AND name = 'LocalMachineTime')
                            ALTER TABLE DeviceSessions ADD LocalMachineTime DATETIME NULL;
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DeviceSessions') AND name = 'ServerTimeAtLogin')
                            ALTER TABLE DeviceSessions ADD ServerTimeAtLogin DATETIME NOT NULL DEFAULT GETDATE();
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DeviceSessions') AND name = 'LastHeartbeat')
                            ALTER TABLE DeviceSessions ADD LastHeartbeat DATETIME NOT NULL DEFAULT GETDATE();
                    END");
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeviceSessionManager.EnsureSchema", ex);
            }
        }

        /// <summary>
        /// تسجيل بداية جلسة عمل جديدة عند فتح البرنامج
        /// </summary>
        public static void StartSession(int empID, string empName, string userName = "")
        {
            try
            {
                EnsureSchema();
                string machine = Environment.MachineName;
                string ip = GetLocalIPAddress();
                string ver = UpdateManager.CurrentVersion;
                DateTime localNow = DateTime.Now;

                // إغلاق أي جلسات قديمة معلقة لهذا الجهاز لم يتم إغلاقها سابقاً بشكل سليم
                try
                {
                    DbHelper.Execute(@"
                        UPDATE DeviceSessions
                        SET Status = 'Closed',
                            LogoutTime = LastHeartbeat,
                            Notes = N'إغلاق آلي عند فتح جلسة جديدة'
                        WHERE MachineName = @mach AND Status = 'Active'",
                        DbHelper.P("@mach", machine));
                }
                catch { }

                object sIdObj = DbHelper.Scalar(@"
                    INSERT INTO DeviceSessions 
                        (MachineName, EmpID, EmpName, UserName, LoginTime, ServerTimeAtLogin, LastHeartbeat, LocalMachineTime, AppVersion, IPAddress, Status)
                    VALUES 
                        (@mach, @emp, @empName, @user, GETDATE(), GETDATE(), GETDATE(), @localNow, @ver, @ip, 'Active');
                    SELECT SCOPE_IDENTITY();",
                    DbHelper.P("@mach", machine),
                    DbHelper.P("@emp", empID),
                    DbHelper.P("@empName", string.IsNullOrWhiteSpace(empName) ? "موظف" : empName.Trim()),
                    DbHelper.P("@user", userName ?? ""),
                    DbHelper.P("@localNow", localNow),
                    DbHelper.P("@ver", ver ?? ""),
                    DbHelper.P("@ip", ip ?? ""));

                if (sIdObj != null && sIdObj != DBNull.Value)
                {
                    _currentSessionID = Convert.ToInt32(sIdObj);
                    Session.CurrentDeviceSessionID = _currentSessionID;
                }

                StartHeartbeatTimer();
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeviceSessionManager.StartSession", ex);
            }
        }

        /// <summary>
        /// تسجيل وقت الخروج وإغلاق الجلسة
        /// </summary>
        public static void EndCurrentSession()
        {
            try
            {
                StopHeartbeatTimer();
                if (!_currentSessionID.HasValue || _currentSessionID.Value <= 0) return;

                DbHelper.Execute(@"
                    UPDATE DeviceSessions
                    SET LogoutTime = GETDATE(),
                        LastHeartbeat = GETDATE(),
                        Status = 'Closed'
                    WHERE SessionID = @sid",
                    DbHelper.P("@sid", _currentSessionID.Value));

                _currentSessionID = null;
                Session.CurrentDeviceSessionID = null;
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeviceSessionManager.EndCurrentSession", ex);
            }
        }

        private static void StartHeartbeatTimer()
        {
            try
            {
                StopHeartbeatTimer();
                _heartbeatTimer = new System.Threading.Timer(_ =>
                {
                    if (_currentSessionID.HasValue && _currentSessionID.Value > 0)
                    {
                        try
                        {
                            DbHelper.Execute("UPDATE DeviceSessions SET LastHeartbeat = GETDATE() WHERE SessionID = @sid",
                                DbHelper.P("@sid", _currentSessionID.Value));
                        }
                        catch { }
                    }
                }, null, 60000, 90000);
            }
            catch { }
        }

        private static void StopHeartbeatTimer()
        {
            try
            {
                if (_heartbeatTimer != null)
                {
                    _heartbeatTimer.Dispose();
                    _heartbeatTimer = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// جلب سجل الجلسات ومواعيد فتح وغلق البرنامج مع تصفية احترافية
        /// </summary>
        public static DataTable GetSessionsReport(DateTime? from = null, DateTime? to = null, string machineName = null, int? empID = null, string status = null)
        {
            EnsureSchema();
            string sql = @"
                SELECT 
                    s.SessionID,
                    s.MachineName,
                    s.EmpName,
                    s.UserName,
                    s.LoginTime,
                    s.LogoutTime,
                    s.ServerTimeAtLogin,
                    s.LastHeartbeat,
                    s.LocalMachineTime,
                    CASE 
                        WHEN s.Status = 'Active' AND DATEDIFF(minute, s.LastHeartbeat, GETDATE()) <= 5 THEN N'متصل الآن'
                        WHEN s.Status = 'Active' THEN N'معلق'
                        ELSE N'تم الخروج'
                    END AS StatusArabic,
                    s.Status,
                    s.AppVersion,
                    s.IPAddress,
                    DATEDIFF(minute, s.LoginTime, ISNULL(s.LogoutTime, s.LastHeartbeat)) AS DurationMinutes,
                    s.Notes
                FROM DeviceSessions s
                WHERE 1=1 ";

            var prms = new List<SqlParameter>();

            if (from.HasValue)
            {
                sql += " AND CAST(s.LoginTime AS DATE) >= @from";
                prms.Add(DbHelper.P("@from", from.Value.Date));
            }
            if (to.HasValue)
            {
                sql += " AND CAST(s.LoginTime AS DATE) <= @to";
                prms.Add(DbHelper.P("@to", to.Value.Date));
            }
            if (!string.IsNullOrWhiteSpace(machineName) && machineName != "الكل")
            {
                sql += " AND s.MachineName = @mach";
                prms.Add(DbHelper.P("@mach", machineName.Trim()));
            }
            if (empID.HasValue && empID.Value > 0)
            {
                sql += " AND s.EmpID = @emp";
                prms.Add(DbHelper.P("@emp", empID.Value));
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                if (status == "Active")
                    sql += " AND s.Status = 'Active' AND DATEDIFF(minute, s.LastHeartbeat, GETDATE()) <= 5";
                else if (status == "Closed")
                    sql += " AND (s.Status = 'Closed' OR DATEDIFF(minute, s.LastHeartbeat, GETDATE()) > 5)";
            }

            sql += " ORDER BY s.SessionID DESC";

            return DbHelper.Query(sql, prms.ToArray());
        }

        /// <summary>
        /// حذف جلسة محددة يدوياً من الأدمن (لتصحيح أي خطأ تاريخ)
        /// </summary>
        public static bool DeleteSession(int sessionID)
        {
            try
            {
                DbHelper.Execute("DELETE FROM DeviceSessions WHERE SessionID = @sid", DbHelper.P("@sid", sessionID));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeviceSessionManager.DeleteSession", ex);
                return false;
            }
        }

        /// <summary>
        /// تنظيف وحذف جميع الجلسات المسجلة بتواريخ مستقبلية خاطئة بنقرة واحدة
        /// </summary>
        public static int CleanFutureAnomalies()
        {
            try
            {
                EnsureSchema();
                return DbHelper.Execute("DELETE FROM DeviceSessions WHERE LoginTime > DATEADD(day, 1, GETDATE())");
            }
            catch (Exception ex)
            {
                AppLogger.Error("DeviceSessionManager.CleanFutureAnomalies", ex);
                return 0;
            }
        }

        /// <summary>
        /// فحص الشذوذ الزمني ومعرفة الجهاز المتسبب فيه إن وجد
        /// </summary>
        public static bool CheckForTimeAnomalies(out string anomalyInfo)
        {
            anomalyInfo = "";
            try
            {
                EnsureSchema();
                // 1. فحص وجود جلسات مستقبلية شاذة
                DataTable dtFuture = DbHelper.Query(@"
                    SELECT TOP 1 MachineName, EmpName, LoginTime 
                    FROM DeviceSessions 
                    WHERE LoginTime > DATEADD(day, 1, GETDATE()) 
                    ORDER BY LoginTime DESC");

                if (dtFuture.Rows.Count > 0)
                {
                    DataRow r = dtFuture.Rows[0];
                    string mach = r["MachineName"].ToString();
                    string emp = r["EmpName"].ToString();
                    DateTime dt = Convert.ToDateTime(r["LoginTime"]);
                    anomalyInfo = $"تم رصد تسجيل دخول بتاريخ مستقبلي خاطئ ({dt:yyyy-MM-dd}) مسجل من جهاز [{mach}] بواسطة [{emp}]";
                    return true;
                }

                // 2. فحص وجود فواتير مبيعات مستقبلية شاذة
                object maxSaleObj = DbHelper.Scalar("SELECT TOP 1 SaleDate FROM Sales WHERE SaleDate > DATEADD(day, 1, GETDATE()) ORDER BY SaleDate DESC");
                if (maxSaleObj != null && maxSaleObj != DBNull.Value)
                {
                    DateTime maxSale = Convert.ToDateTime(maxSaleObj);
                    anomalyInfo = $"تم رصد فاتورة بيع مسجلة بتاريخ مستقبلي ({maxSale:yyyy-MM-dd})";
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static List<string> GetAllMachineNames()
        {
            var list = new List<string>();
            try
            {
                EnsureSchema();
                var dt = DbHelper.Query("SELECT DISTINCT MachineName FROM DeviceSessions WHERE MachineName IS NOT NULL AND LTRIM(RTRIM(MachineName)) <> '' ORDER BY MachineName");
                foreach (DataRow r in dt.Rows)
                {
                    list.Add(r["MachineName"].ToString());
                }
            }
            catch { }
            return list;
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
