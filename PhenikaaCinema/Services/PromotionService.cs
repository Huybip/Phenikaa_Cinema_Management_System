using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; }
        public string Description { get; set; }
        public string DiscountType { get; set; } // 'Percentage' or 'Fixed'
        public decimal DiscountValue { get; set; }
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public int? MaxUsage { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }

        public string FormattedDiscount => DiscountType == "Percentage" ? $"{DiscountValue:N0}%" : CurrencyHelper.FormatVnd(DiscountValue);
    }

    public class PromotionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        public bool IsValid => Success;
    }

    public class PromotionService
    {
        public List<PromotionDto> GetActivePromotions()
        {
            string sql = @"
                SELECT PromotionId, PromotionCode, Description, DiscountType, DiscountValue,
                       StartAtUtc, EndAtUtc, MaxUsage, UsedCount, IsActive
                FROM dbo.Promotions
                WHERE IsActive = 1
                  AND StartAtUtc <= SYSUTCDATETIME()
                  AND EndAtUtc >= SYSUTCDATETIME()
                  AND (MaxUsage IS NULL OR UsedCount < MaxUsage)
                ORDER BY DiscountValue DESC";

            var table = DatabaseHelper.ExecuteQuery(sql);
            var list = new List<PromotionDto>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new PromotionDto
                {
                    PromotionId = Convert.ToInt32(row["PromotionId"]),
                    PromotionCode = row["PromotionCode"].ToString(),
                    Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "",
                    DiscountType = row["DiscountType"].ToString(),
                    DiscountValue = Convert.ToDecimal(row["DiscountValue"]),
                    StartAtUtc = Convert.ToDateTime(row["StartAtUtc"]),
                    EndAtUtc = Convert.ToDateTime(row["EndAtUtc"]),
                    MaxUsage = row["MaxUsage"] != DBNull.Value ? Convert.ToInt32(row["MaxUsage"]) : (int?)null,
                    UsedCount = Convert.ToInt32(row["UsedCount"]),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }

            return list;
        }

        public PromotionResult ValidateAndCalculateDiscount(string promoCode, decimal totalAmount)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return new PromotionResult { Success = false, Message = "Vui lòng nhập mã khuyến mãi." };
            }

            string sql = @"
                SELECT PromotionId, PromotionCode, DiscountType, DiscountValue,
                       StartAtUtc, EndAtUtc, MaxUsage, UsedCount, IsActive
                FROM dbo.Promotions
                WHERE UPPER(PromotionCode) = UPPER(@Code)";

            var pCode = new SqlParameter("@Code", SqlDbType.NVarChar, 30) { Value = promoCode.Trim() };
            var table = DatabaseHelper.ExecuteQuery(sql, pCode);

            if (table.Rows.Count == 0)
            {
                return new PromotionResult { Success = false, Message = "Mã khuyến mãi không tồn tại." };
            }

            DataRow row = table.Rows[0];
            bool isActive = Convert.ToBoolean(row["IsActive"]);
            DateTime startAt = Convert.ToDateTime(row["StartAtUtc"]);
            DateTime endAt = Convert.ToDateTime(row["EndAtUtc"]);
            int? maxUsage = row["MaxUsage"] != DBNull.Value ? Convert.ToInt32(row["MaxUsage"]) : (int?)null;
            int usedCount = Convert.ToInt32(row["UsedCount"]);
            DateTime now = DateTime.UtcNow;

            if (!isActive)
                return new PromotionResult { Success = false, Message = "Mã khuyến mãi đã ngưng áp dụng." };

            if (now < startAt)
                return new PromotionResult { Success = false, Message = "Mã khuyến mãi chưa đến thời gian áp dụng." };

            if (now > endAt)
                return new PromotionResult { Success = false, Message = "Mã khuyến mãi đã hết hạn sử dụng." };

            if (maxUsage.HasValue && usedCount >= maxUsage.Value)
                return new PromotionResult { Success = false, Message = "Mã khuyến mãi đã hết lượt sử dụng." };

            string discountType = row["DiscountType"].ToString();
            decimal discountValue = Convert.ToDecimal(row["DiscountValue"]);
            decimal discountAmount = 0;

            if (discountType == "Percentage")
            {
                discountAmount = totalAmount * (discountValue / 100m);
            }
            else
            {
                discountAmount = discountValue;
            }

            if (discountAmount > totalAmount)
                discountAmount = totalAmount;

            decimal finalAmount = totalAmount - discountAmount;

            return new PromotionResult
            {
                Success = true,
                PromotionId = Convert.ToInt32(row["PromotionId"]),
                PromotionCode = row["PromotionCode"].ToString(),
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                Message = $"Áp dụng mã thành công! Giảm {CurrencyHelper.FormatVnd(discountAmount)}."
            };
        }
    }
}
