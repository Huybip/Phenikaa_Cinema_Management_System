using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class TicketDto
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; }
        public int BookingSeatId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; }
        public string RoomName { get; set; }
        public string RowLabel { get; set; }
        public int SeatNumber { get; set; }
        public string SeatType { get; set; }
        public decimal SeatPrice { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public bool IsUsed { get; set; }

        public string SeatCode => $"{RowLabel}{SeatNumber}";
        public DateTime StartTimeVn => DateTimeHelper.ToVietnamTime(StartTimeUtc);
        public DateTime IssuedAtVn => DateTimeHelper.ToVietnamTime(IssuedAtUtc);
        public string FormattedShowtime => $"{StartTimeVn:dd/MM/yyyy HH:mm}";
        public string FormattedPrice => CurrencyHelper.FormatVnd(SeatPrice);
        public string StatusText => IsUsed ? "Đã soát vé" : "Hợp lệ (Chưa soát)";
    }

    public class TicketService
    {
        public List<TicketDto> GetTicketsByBookingId(int bookingId)
        {
            string sql = @"
                SELECT t.TicketId, t.TicketCode, t.IssuedAtUtc, t.IsUsed,
                       bs.BookingSeatId, bs.BookingId, bs.SeatPrice,
                       b.BookingCode,
                       st.ShowtimeId, st.StartTimeUtc, st.EndTimeUtc,
                       m.Title AS MovieTitle, r.RoomName,
                       s.RowLabel, s.SeatNumber, s.SeatType
                FROM dbo.Tickets t
                INNER JOIN dbo.BookingSeats bs ON t.BookingSeatId = bs.BookingSeatId
                INNER JOIN dbo.Bookings b ON bs.BookingId = b.BookingId
                INNER JOIN dbo.Showtimes st ON bs.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                WHERE bs.BookingId = @BookingId
                ORDER BY s.RowLabel, s.SeatNumber";

            var pId = new SqlParameter("@BookingId", SqlDbType.Int) { Value = bookingId };
            return MapTickets(DatabaseHelper.ExecuteQuery(sql, pId));
        }

        public TicketDto GetTicketByCode(string ticketCode)
        {
            if (string.IsNullOrWhiteSpace(ticketCode)) return null;

            string sql = @"
                SELECT t.TicketId, t.TicketCode, t.IssuedAtUtc, t.IsUsed,
                       bs.BookingSeatId, bs.BookingId, bs.SeatPrice,
                       b.BookingCode,
                       st.ShowtimeId, st.StartTimeUtc, st.EndTimeUtc,
                       m.Title AS MovieTitle, r.RoomName,
                       s.RowLabel, s.SeatNumber, s.SeatType
                FROM dbo.Tickets t
                INNER JOIN dbo.BookingSeats bs ON t.BookingSeatId = bs.BookingSeatId
                INNER JOIN dbo.Bookings b ON bs.BookingId = b.BookingId
                INNER JOIN dbo.Showtimes st ON bs.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                WHERE t.TicketCode = @TicketCode";

            var pCode = new SqlParameter("@TicketCode", SqlDbType.NVarChar, 50) { Value = ticketCode.Trim() };
            var table = DatabaseHelper.ExecuteQuery(sql, pCode);
            if (table.Rows.Count == 0) return null;
            return MapTicketRow(table.Rows[0]);
        }

        public List<TicketDto> GetTicketsByCustomer(int customerUserId)
        {
            string sql = @"
                SELECT t.TicketId, t.TicketCode, t.IssuedAtUtc, t.IsUsed,
                       bs.BookingSeatId, bs.BookingId, bs.SeatPrice,
                       b.BookingCode,
                       st.ShowtimeId, st.StartTimeUtc, st.EndTimeUtc,
                       m.Title AS MovieTitle, r.RoomName,
                       s.RowLabel, s.SeatNumber, s.SeatType
                FROM dbo.Tickets t
                INNER JOIN dbo.BookingSeats bs ON t.BookingSeatId = bs.BookingSeatId
                INNER JOIN dbo.Bookings b ON bs.BookingId = b.BookingId
                INNER JOIN dbo.Showtimes st ON bs.ShowtimeId = st.ShowtimeId
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId
                WHERE b.CustomerUserId = @CustomerUserId
                ORDER BY t.TicketId DESC";

            var pId = new SqlParameter("@CustomerUserId", SqlDbType.Int) { Value = customerUserId };
            return MapTickets(DatabaseHelper.ExecuteQuery(sql, pId));
        }

        public bool CheckInTicket(string ticketCode, out string message)
        {
            if (string.IsNullOrWhiteSpace(ticketCode))
            {
                message = "Mã vé không được để trống.";
                return false;
            }

            var ticket = GetTicketByCode(ticketCode);
            if (ticket == null)
            {
                message = $"Không tìm thấy vé với mã: {ticketCode}";
                return false;
            }

            if (ticket.IsUsed)
            {
                message = $"Vé {ticket.TicketCode} đã được soát vé trước đó!";
                return false;
            }

            string updateSql = "UPDATE dbo.Tickets SET IsUsed = 1 WHERE TicketCode = @TicketCode";
            var pCode = new SqlParameter("@TicketCode", SqlDbType.NVarChar, 50) { Value = ticketCode.Trim() };
            int rows = DatabaseHelper.ExecuteNonQuery(updateSql, pCode);
            if (rows > 0)
            {
                message = $"Soát vé thành công cho ghế {ticket.SeatCode} - Phim {ticket.MovieTitle}!";
                return true;
            }

            message = "Có lỗi xảy ra khi cập nhật trạng thái vé.";
            return false;
        }

        private List<TicketDto> MapTickets(DataTable table)
        {
            var list = new List<TicketDto>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(MapTicketRow(row));
            }
            return list;
        }

        private TicketDto MapTicketRow(DataRow row)
        {
            return new TicketDto
            {
                TicketId = Convert.ToInt32(row["TicketId"]),
                TicketCode = row["TicketCode"].ToString(),
                BookingSeatId = Convert.ToInt32(row["BookingSeatId"]),
                BookingId = Convert.ToInt32(row["BookingId"]),
                BookingCode = row["BookingCode"].ToString(),
                ShowtimeId = Convert.ToInt32(row["ShowtimeId"]),
                MovieTitle = row["MovieTitle"].ToString(),
                RoomName = row["RoomName"].ToString(),
                RowLabel = row["RowLabel"].ToString().Trim(),
                SeatNumber = Convert.ToInt32(row["SeatNumber"]),
                SeatType = row["SeatType"].ToString().Trim(),
                SeatPrice = Convert.ToDecimal(row["SeatPrice"]),
                StartTimeUtc = Convert.ToDateTime(row["StartTimeUtc"]),
                EndTimeUtc = Convert.ToDateTime(row["EndTimeUtc"]),
                IssuedAtUtc = Convert.ToDateTime(row["IssuedAtUtc"]),
                IsUsed = Convert.ToBoolean(row["IsUsed"])
            };
        }
    }
}
