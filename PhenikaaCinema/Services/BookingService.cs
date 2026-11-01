using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace PhenikaaCinema.Services
{
    public class BookingHoldResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime HoldExpiresAtUtc { get; set; }
        public string Status { get; set; }

        public DateTime HoldExpiresAtVn => DateTimeHelper.ToVietnamTime(HoldExpiresAtUtc);
    }

    public class PaymentConfirmResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string BookingStatus { get; set; }
        public int PaymentId { get; set; }
        public string TransactionCode { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; }
        public List<TicketIssuedDto> Tickets { get; set; } = new List<TicketIssuedDto>();
    }

    public class TicketIssuedDto
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }
        public int SeatId { get; set; }
        public string SeatName { get; set; }
    }

    public class BookingDetailDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public int? CustomerUserId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; }
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; }
        public string RoomName { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public DateTime BookingTimeUtc { get; set; }
        public DateTime? HoldExpiresAtUtc { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string SeatNames { get; set; }
        public int TicketCount { get; set; }

        public DateTime StartTimeVn => DateTimeHelper.ToVietnamTime(StartTimeUtc);
        public DateTime BookingTimeVn => DateTimeHelper.ToVietnamTime(BookingTimeUtc);
        public DateTime? HoldExpiresAtVn => HoldExpiresAtUtc.HasValue ? DateTimeHelper.ToVietnamTime(HoldExpiresAtUtc.Value) : (DateTime?)null;
        public string FormattedTotal => CurrencyHelper.FormatVnd(FinalAmount ?? TotalAmount);
        public bool CanPay => Status == "PendingPayment" && HoldExpiresAtUtc.HasValue && HoldExpiresAtUtc.Value > DateTime.UtcNow;
        public bool CanCancel => Status == "PendingPayment";
    }

    public class BookingService
    {
        public BookingHoldResult CreateBookingHold(int showtimeId, int createdByUserId, int? customerUserId, IEnumerable<int> seatIds, int holdMinutes = 10)
        {
            var seatList = seatIds?.ToList() ?? new List<int>();
            if (seatList.Count == 0)
            {
                return new BookingHoldResult
                {
                    Success = false,
                    Message = "Vui lòng chọn ít nhất một ghế để đặt."
                };
            }

            string seatIdsStr = string.Join(",", seatList);

            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_CreateBookingHold", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ShowtimeId", SqlDbType.Int).Value = showtimeId;
                        cmd.Parameters.Add("@CreatedByUserId", SqlDbType.Int).Value = createdByUserId;
                        cmd.Parameters.Add("@CustomerUserId", SqlDbType.Int).Value = customerUserId.HasValue ? (object)customerUserId.Value : DBNull.Value;
                        cmd.Parameters.Add("@SeatIds", SqlDbType.NVarChar, -1).Value = seatIdsStr;
                        cmd.Parameters.Add("@HoldMinutes", SqlDbType.Int).Value = holdMinutes;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BookingHoldResult
                                {
                                    Success = true,
                                    BookingId = Convert.ToInt32(reader["BookingId"]),
                                    BookingCode = reader["BookingCode"].ToString(),
                                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                    HoldExpiresAtUtc = Convert.ToDateTime(reader["HoldExpiresAtUtc"]),
                                    Status = reader["Status"].ToString(),
                                    Message = "Giữ ghế thành công! Vui lòng hoàn tất thanh toán trước khi hết hạn."
                                };
                            }
                        }
                    }
                }

                return new BookingHoldResult
                {
                    Success = false,
                    Message = "Không nhận được phản hồi từ hệ thống giữ ghế."
                };
            }
            catch (SqlException ex)
            {
                string friendlyMessage;
                switch (ex.Number)
                {
                    case 50001:
                        friendlyMessage = "Thời gian giữ ghế không hợp lệ.";
                        break;
                    case 50002:
                    case 50004:
                        friendlyMessage = "Danh sách ghế đặt không hợp lệ.";
                        break;
                    case 50005:
                        friendlyMessage = "Suất chiếu không còn khả dụng hoặc đã bắt đầu.";
                        break;
                    case 50006:
                        friendlyMessage = "Một hoặc nhiều ghế đã chọn không hợp lệ trong phòng chiếu.";
                        break;
                    case 50007:
                        friendlyMessage = "Một hoặc nhiều ghế bạn chọn vừa được người khác giữ hoặc đã bán. Vui lòng chọn ghế khác.";
                        break;
                    default:
                        friendlyMessage = "Lỗi đặt ghế: " + ex.Message;
                        break;
                }

                return new BookingHoldResult
                {
                    Success = false,
                    Message = friendlyMessage
                };
            }
            catch (Exception ex)
            {
                return new BookingHoldResult
                {
                    Success = false,
                    Message = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message
                };
            }
        }

        public PaymentConfirmResult ConfirmMockPayment(int bookingId)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_ConfirmMockPayment", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;

                        var result = new PaymentConfirmResult();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            bool hasRead = false;
                            while (reader.Read())
                            {
                                if (!hasRead)
                                {
                                    result.BookingId = Convert.ToInt32(reader["BookingId"]);
                                    result.BookingCode = reader["BookingCode"].ToString();
                                    result.BookingStatus = reader["BookingStatus"].ToString();
                                    result.PaymentId = Convert.ToInt32(reader["PaymentId"]);
                                    result.TransactionCode = reader["TransactionCode"].ToString();
                                    result.PaidAmount = Convert.ToDecimal(reader["PaidAmount"]);
                                    result.PaymentStatus = reader["PaymentStatus"].ToString();
                                    hasRead = true;
                                }

                                result.Tickets.Add(new TicketIssuedDto
                                {
                                    TicketId = Convert.ToInt32(reader["TicketId"]),
                                    TicketCode = reader["TicketCode"].ToString(),
                                    SeatId = Convert.ToInt32(reader["SeatId"])
                                });
                            }

                            if (hasRead)
                            {
                                result.Success = true;
                                result.Message = "Thanh toán mô phỏng thành công! Đã phát hành vé xem phim.";
                                return result;
                            }
                        }
                    }
                }

                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Không thể xác nhận thanh toán."
                };
            }
            catch (SqlException ex)
            {
                string friendlyMessage;
                switch (ex.Number)
                {
                    case 50101:
                        friendlyMessage = "Đơn đặt vé không tồn tại, đã hết hạn giữ chỗ hoặc không ở trạng thái chờ thanh toán.";
                        break;
                    case 50102:
                        friendlyMessage = "Không tìm thấy ghế nào đang được giữ cho đơn đặt vé này.";
                        break;
                    case 50103:
                        friendlyMessage = "Suất chiếu không còn khả dụng hoặc đã bắt đầu chiếu.";
                        break;
                    default:
                        friendlyMessage = "Lỗi thanh toán: " + ex.Message;
                        break;
                }

                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = friendlyMessage
                };
            }
            catch (Exception ex)
            {
                return new PaymentConfirmResult
                {
                    Success = false,
                    Message = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message
                };
            }
        }

        public bool CancelPendingBooking(int bookingId, out string message)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_CancelPendingBooking", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                message = "Đã hủy đơn đặt vé và giải phóng ghế thành công.";
                                return true;
                            }
                        }
                    }
                }
                message = "Không thể hủy đơn đặt vé.";
                return false;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50201)
                {
                    message = "Đơn đặt vé không tồn tại hoặc không ở trạng thái chờ thanh toán.";
                }
                else
                {
                    message = "Lỗi khi hủy đơn đặt vé: " + ex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                message = "Lỗi cơ sở dữ liệu: " + ex.Message;
                return false;
            }
        }

        public void ExpireBookingHolds()
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery("EXEC dbo.sp_ExpireBookingHolds");
            }
            catch { }
        }

        public List<BookingDetailDto> GetBookingsByCustomer(int customerUserId)
        {
            ExpireBookingHolds();

            string sql = @"
                SELECT b.BookingId, b.BookingCode, b.CustomerUserId, u.FullName AS CustomerName,
                       u.Email AS CustomerEmail, u.Phone AS CustomerPhone,
                       b.CreatedByUserId, creator.FullName AS CreatedByName,
                       b.ShowtimeId, m.Title AS MovieTitle, r.RoomName,
                       st.StartTimeUtc, st.EndTimeUtc, b.BookingTimeUtc, b.HoldExpiresAtUtc,
                       b.Status, b.TotalAmount, b.DiscountAmount, b.FinalAmount,
                       (SELECT STUFF((
                            SELECT ', ' + s.RowLabel + CAST(s.SeatNumber AS NVARCHAR(10))
                            FROM dbo.BookingSeats bs
                            INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                            WHERE bs.BookingId = b.BookingId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS SeatNames,
                       (SELECT COUNT(1)
                        FROM dbo.BookingSeats bs
                        INNER JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                        WHERE bs.BookingId = b.BookingId) AS TicketCount
                FROM dbo.Bookings b
                LEFT JOIN dbo.Users u ON b.CustomerUserId = u.UserId
                INNER JOIN dbo.Users creator ON b.CreatedByUserId = creator.UserId
                INNER JOIN dbo.Showtimes st ON b.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                WHERE b.CustomerUserId = @CustomerUserId
                ORDER BY b.BookingId DESC";

            var pUserId = new SqlParameter("@CustomerUserId", SqlDbType.Int) { Value = customerUserId };
            return MapBookings(DatabaseHelper.ExecuteQuery(sql, pUserId));
        }

        public List<BookingDetailDto> GetAllBookings(string keyword = null, string status = null)
        {
            ExpireBookingHolds();

            keyword = (keyword ?? "").Trim();
            string sql = @"
                SELECT b.BookingId, b.BookingCode, b.CustomerUserId, u.FullName AS CustomerName,
                       u.Email AS CustomerEmail, u.Phone AS CustomerPhone,
                       b.CreatedByUserId, creator.FullName AS CreatedByName,
                       b.ShowtimeId, m.Title AS MovieTitle, r.RoomName,
                       st.StartTimeUtc, st.EndTimeUtc, b.BookingTimeUtc, b.HoldExpiresAtUtc,
                       b.Status, b.TotalAmount, b.DiscountAmount, b.FinalAmount,
                       (SELECT STUFF((
                            SELECT ', ' + s.RowLabel + CAST(s.SeatNumber AS NVARCHAR(10))
                            FROM dbo.BookingSeats bs
                            INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                            WHERE bs.BookingId = b.BookingId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS SeatNames,
                       (SELECT COUNT(1)
                        FROM dbo.BookingSeats bs
                        INNER JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                        WHERE bs.BookingId = b.BookingId) AS TicketCount
                FROM dbo.Bookings b
                LEFT JOIN dbo.Users u ON b.CustomerUserId = u.UserId
                INNER JOIN dbo.Users creator ON b.CreatedByUserId = creator.UserId
                INNER JOIN dbo.Showtimes st ON b.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                WHERE (@Keyword = '' OR b.BookingCode LIKE '%' + @Keyword + '%' OR u.FullName LIKE '%' + @Keyword + '%' OR u.Phone LIKE '%' + @Keyword + '%')
                  AND (@Status IS NULL OR b.Status = @Status)
                ORDER BY b.BookingId DESC";

            var pKeyword = new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = keyword };
            var pStatus = new SqlParameter("@Status", SqlDbType.NVarChar, 20) { Value = string.IsNullOrEmpty(status) ? (object)DBNull.Value : status };

            return MapBookings(DatabaseHelper.ExecuteQuery(sql, pKeyword, pStatus));
        }

        public BookingDetailDto GetBookingById(int bookingId)
        {
            ExpireBookingHolds();

            string sql = @"
                SELECT b.BookingId, b.BookingCode, b.CustomerUserId, u.FullName AS CustomerName,
                       u.Email AS CustomerEmail, u.Phone AS CustomerPhone,
                       b.CreatedByUserId, creator.FullName AS CreatedByName,
                       b.ShowtimeId, m.Title AS MovieTitle, r.RoomName,
                       st.StartTimeUtc, st.EndTimeUtc, b.BookingTimeUtc, b.HoldExpiresAtUtc,
                       b.Status, b.TotalAmount, b.DiscountAmount, b.FinalAmount,
                       (SELECT STUFF((
                            SELECT ', ' + s.RowLabel + CAST(s.SeatNumber AS NVARCHAR(10))
                            FROM dbo.BookingSeats bs
                            INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                            WHERE bs.BookingId = b.BookingId
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')) AS SeatNames,
                       (SELECT COUNT(1)
                        FROM dbo.BookingSeats bs
                        INNER JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                        WHERE bs.BookingId = b.BookingId) AS TicketCount
                FROM dbo.Bookings b
                LEFT JOIN dbo.Users u ON b.CustomerUserId = u.UserId
                INNER JOIN dbo.Users creator ON b.CreatedByUserId = creator.UserId
                INNER JOIN dbo.Showtimes st ON b.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                WHERE b.BookingId = @BookingId";

            var pId = new SqlParameter("@BookingId", SqlDbType.Int) { Value = bookingId };
            var table = DatabaseHelper.ExecuteQuery(sql, pId);
            if (table.Rows.Count == 0) return null;
            return MapBookingRow(table.Rows[0]);
        }

        private List<BookingDetailDto> MapBookings(DataTable table)
        {
            var list = new List<BookingDetailDto>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(MapBookingRow(row));
            }
            return list;
        }

        private BookingDetailDto MapBookingRow(DataRow row)
        {
            return new BookingDetailDto
            {
                BookingId = Convert.ToInt32(row["BookingId"]),
                BookingCode = row["BookingCode"].ToString(),
                CustomerUserId = row["CustomerUserId"] != DBNull.Value ? Convert.ToInt32(row["CustomerUserId"]) : (int?)null,
                CustomerName = row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "Khách vãng lai",
                CustomerEmail = row["CustomerEmail"] != DBNull.Value ? row["CustomerEmail"].ToString() : "",
                CustomerPhone = row["CustomerPhone"] != DBNull.Value ? row["CustomerPhone"].ToString() : "",
                CreatedByUserId = Convert.ToInt32(row["CreatedByUserId"]),
                CreatedByName = row["CreatedByName"].ToString(),
                ShowtimeId = Convert.ToInt32(row["ShowtimeId"]),
                MovieTitle = row["MovieTitle"].ToString(),
                RoomName = row["RoomName"].ToString(),
                StartTimeUtc = Convert.ToDateTime(row["StartTimeUtc"]),
                EndTimeUtc = Convert.ToDateTime(row["EndTimeUtc"]),
                BookingTimeUtc = Convert.ToDateTime(row["BookingTimeUtc"]),
                HoldExpiresAtUtc = row["HoldExpiresAtUtc"] != DBNull.Value ? Convert.ToDateTime(row["HoldExpiresAtUtc"]) : (DateTime?)null,
                Status = row["Status"].ToString(),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                DiscountAmount = Convert.ToDecimal(row["DiscountAmount"]),
                FinalAmount = row["FinalAmount"] != DBNull.Value ? Convert.ToDecimal(row["FinalAmount"]) : (decimal?)null,
                SeatNames = row["SeatNames"] != DBNull.Value ? row["SeatNames"].ToString() : "",
                TicketCount = Convert.ToInt32(row["TicketCount"])
            };
        }
    }
}
