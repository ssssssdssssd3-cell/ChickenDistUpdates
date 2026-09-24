using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    /// <summary>
    /// DTO لبيانات بون الخصم
    /// </summary>
    public class DiscountVoucherDTO
    {
        public int VoucherID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string DiscountType { get; set; }   // "Percent" أو "Amount"
        public decimal DiscountValue { get; set; }  // النسبة أو القيمة
        public int? MaxUses { get; set; }           // null = بلا حد
        public int UsedCount { get; set; }
        public DateTime? ExpiryDate { get; set; }   // null = لا تنتهي
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsValid(out string reason)
        {
            reason = "";
            if (!IsActive)
            {
                reason = "بون الخصم غير مفعّل";
                return false;
            }
            if (ExpiryDate.HasValue && DateTime.Today > ExpiryDate.Value.Date)
            {
                reason = $"بون الخصم انتهت صلاحيته في {ExpiryDate.Value:yyyy/MM/dd}";
                return false;
            }
            if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
            {
                reason = $"بون الخصم وصل الحد الأقصى لعدد الاستخدامات ({MaxUses.Value})";
                return false;
            }
            return true;
        }

        /// <summary>
        /// يحسب مبلغ الخصم بناءً على إجمالي الفاتورة
        /// </summary>
        public decimal CalculateDiscount(decimal invoiceTotal)
        {
            if (DiscountType == "Percent")
                return Math.Round(invoiceTotal * DiscountValue / 100m, 2);
            else
                return Math.Min(DiscountValue, invoiceTotal); // خصم قيمة لا يتجاوز الإجمالي
        }
    }

    public static class DiscountVouchersDAL
    {
        /// <summary>
        /// يُنشئ جدول DiscountVouchers لو مش موجود (Auto-migration)
        /// </summary>
        public static void EnsureSchema()
        {
            try
            {
                DbHelper.Execute(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='DiscountVouchers')
                    BEGIN
                        CREATE TABLE DiscountVouchers (
                            VoucherID     INT IDENTITY(1,1) PRIMARY KEY,
                            Code          NVARCHAR(50)  NOT NULL UNIQUE,
                            Description   NVARCHAR(200) NULL,
                            DiscountType  NVARCHAR(10)  NOT NULL DEFAULT N'Percent',  -- Percent / Amount
                            DiscountValue DECIMAL(18,2) NOT NULL DEFAULT 0,
                            MaxUses       INT NULL,           -- NULL = غير محدود
                            UsedCount     INT NOT NULL DEFAULT 0,
                            ExpiryDate    DATE NULL,          -- NULL = لا تنتهي
                            IsActive      BIT NOT NULL DEFAULT 1,
                            CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END
                ");
            }
            catch (Exception ex)
            {
                AppLogger.Error("EnsureDiscountVouchersSchema failed", ex, "DiscountVouchersDAL");
            }
        }

        /// <summary>
        /// يجلب كل البونات
        /// </summary>
        public static DataTable GetAll()
        {
            EnsureSchema();
            return DbHelper.Query(@"
                SELECT VoucherID, Code, Description, DiscountType, DiscountValue,
                       MaxUses, UsedCount, ExpiryDate, IsActive, CreatedAt
                FROM DiscountVouchers
                ORDER BY CreatedAt DESC
            ");
        }

        /// <summary>
        /// يبحث عن بون بالكود ويرجّع DTO أو null
        /// </summary>
        public static DiscountVoucherDTO GetByCode(string code)
        {
            EnsureSchema();
            var dt = DbHelper.Query(
                @"SELECT VoucherID, Code, Description, DiscountType, DiscountValue,
                         MaxUses, UsedCount, ExpiryDate, IsActive, CreatedAt
                  FROM DiscountVouchers WHERE Code = @code",
                DbHelper.P("@code", code.Trim().ToUpper()));

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return RowToDTO(row);
        }

        /// <summary>
        /// يجلب بون بـ ID
        /// </summary>
        public static DiscountVoucherDTO GetByID(int voucherID)
        {
            EnsureSchema();
            var dt = DbHelper.Query(
                @"SELECT VoucherID, Code, Description, DiscountType, DiscountValue,
                         MaxUses, UsedCount, ExpiryDate, IsActive, CreatedAt
                  FROM DiscountVouchers WHERE VoucherID = @id",
                DbHelper.P("@id", voucherID));

            if (dt.Rows.Count == 0) return null;
            return RowToDTO(dt.Rows[0]);
        }

        private static DiscountVoucherDTO RowToDTO(DataRow row)
        {
            return new DiscountVoucherDTO
            {
                VoucherID    = Convert.ToInt32(row["VoucherID"]),
                Code         = row["Code"].ToString(),
                Description  = row["Description"]?.ToString() ?? "",
                DiscountType = row["DiscountType"].ToString(),
                DiscountValue= Convert.ToDecimal(row["DiscountValue"]),
                MaxUses      = row["MaxUses"] != DBNull.Value ? (int?)Convert.ToInt32(row["MaxUses"]) : null,
                UsedCount    = Convert.ToInt32(row["UsedCount"]),
                ExpiryDate   = row["ExpiryDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ExpiryDate"]) : null,
                IsActive     = Convert.ToBoolean(row["IsActive"]),
                CreatedAt    = Convert.ToDateTime(row["CreatedAt"])
            };
        }

        /// <summary>
        /// يحفظ بون جديد أو يعدّل موجوداً
        /// </summary>
        public static int Save(DiscountVoucherDTO dto)
        {
            EnsureSchema();
            string code = dto.Code.Trim().ToUpper();

            if (dto.VoucherID <= 0)
            {
                // جديد
                var result = DbHelper.Scalar(
                    @"INSERT INTO DiscountVouchers (Code, Description, DiscountType, DiscountValue, MaxUses, ExpiryDate, IsActive)
                      VALUES (@code, @desc, @type, @val, @max, @exp, @active);
                      SELECT SCOPE_IDENTITY();",
                    DbHelper.P("@code",   code),
                    DbHelper.P("@desc",   dto.Description ?? ""),
                    DbHelper.P("@type",   dto.DiscountType),
                    DbHelper.P("@val",    dto.DiscountValue),
                    DbHelper.P("@max",    dto.MaxUses.HasValue ? (object)dto.MaxUses.Value : DBNull.Value),
                    DbHelper.P("@exp",    dto.ExpiryDate.HasValue ? (object)dto.ExpiryDate.Value.Date : DBNull.Value),
                    DbHelper.P("@active", dto.IsActive ? 1 : 0));
                return result != null ? Convert.ToInt32(result) : 0;
            }
            else
            {
                // تعديل
                DbHelper.Execute(
                    @"UPDATE DiscountVouchers SET
                        Code=@code, Description=@desc, DiscountType=@type, DiscountValue=@val,
                        MaxUses=@max, ExpiryDate=@exp, IsActive=@active
                      WHERE VoucherID=@id",
                    DbHelper.P("@code",   code),
                    DbHelper.P("@desc",   dto.Description ?? ""),
                    DbHelper.P("@type",   dto.DiscountType),
                    DbHelper.P("@val",    dto.DiscountValue),
                    DbHelper.P("@max",    dto.MaxUses.HasValue ? (object)dto.MaxUses.Value : DBNull.Value),
                    DbHelper.P("@exp",    dto.ExpiryDate.HasValue ? (object)dto.ExpiryDate.Value.Date : DBNull.Value),
                    DbHelper.P("@active", dto.IsActive ? 1 : 0),
                    DbHelper.P("@id",     dto.VoucherID));
                return dto.VoucherID;
            }
        }

        /// <summary>
        /// يزيد عداد الاستخدام بعد تطبيق البون ناجح
        /// </summary>
        public static void IncrementUsage(int voucherID)
        {
            DbHelper.Execute(
                "UPDATE DiscountVouchers SET UsedCount = UsedCount + 1 WHERE VoucherID = @id",
                DbHelper.P("@id", voucherID));
        }

        /// <summary>
        /// يحذف بون
        /// </summary>
        public static void Delete(int voucherID)
        {
            DbHelper.Execute(
                "DELETE FROM DiscountVouchers WHERE VoucherID = @id",
                DbHelper.P("@id", voucherID));
        }
    }
}
