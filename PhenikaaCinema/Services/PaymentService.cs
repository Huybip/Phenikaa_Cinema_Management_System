using System;
using System.Data;
using System.Data.SqlClient;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string TransactionCode { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public DateTime? PaidAtVn => PaidAtUtc.HasValue ? DateTimeHelper.ToVietnamTime(PaidAtUtc.Value) : (DateTime?)null;
        public string FormattedAmount => CurrencyHelper.FormatVnd(Amount);
    }

    public class PaymentService
    {
        private readonly BookingService _bookingService = new BookingService();

        public PaymentConfirmResult ProcessMockPayment(int bookingId)
        {
            // Kiểm tra trạng thái booking trước khi gọi SP
            var booking = _bookingService.GetBookingById(bookingId);
            if (booking == null)
            {
                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Không tìm thấy thông tin đơn đặt vé."
                };
            }

            if (booking.Status == "Confirmed")
            {
                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Đơn đặt vé này đã được thanh toán và xác nhận trước đó."
                };
            }

            if (booking.Status == "Cancelled")
            {
                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Đơn đặt vé này đã bị hủy, không thể tiếp tục thanh toán."
                };
            }

            if (booking.Status == "Expired" || (booking.HoldExpiresAtUtc.HasValue && booking.HoldExpiresAtUtc.Value <= DateTime.UtcNow))
            {
                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Thời gian giữ ghế cho đơn đặt vé này đã hết hạn. Ghế đã được giải phóng."
                };
            }

            // Gọi sp_ConfirmMockPayment
            return _bookingService.ConfirmMockPayment(bookingId);
        }

        public PaymentDto GetPaymentByBookingId(int bookingId)
        {
            string sql = @"
                SELECT TOP 1 PaymentId, BookingId, PaymentMethod, Amount, Status, TransactionCode, PaidAtUtc, CreatedAtUtc
                FROM dbo.Payments
                WHERE BookingId = @BookingId
                ORDER BY PaymentId DESC";

            var pId = new SqlParameter("@BookingId", SqlDbType.Int) { Value = bookingId };
            var table = DatabaseHelper.ExecuteQuery(sql, pId);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new PaymentDto
            {
                PaymentId = Convert.ToInt32(row["PaymentId"]),
                BookingId = Convert.ToInt32(row["BookingId"]),
                PaymentMethod = row["PaymentMethod"].ToString(),
                Amount = Convert.ToDecimal(row["Amount"]),
                Status = row["Status"].ToString(),
                TransactionCode = row["TransactionCode"] != DBNull.Value ? row["TransactionCode"].ToString() : null,
                PaidAtUtc = row["PaidAtUtc"] != DBNull.Value ? Convert.ToDateTime(row["PaidAtUtc"]) : (DateTime?)null,
                CreatedAtUtc = Convert.ToDateTime(row["CreatedAtUtc"])
            };
        }
    }
}
