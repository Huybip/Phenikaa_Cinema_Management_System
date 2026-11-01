using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public class DayRevenueDto
    {
        public DateTime Date { get; set; }
        public string DateLabel => Date.ToString("dd/MM");
        public decimal Revenue { get; set; }
        public int TicketsCount { get; set; }
    }

    public class RoomOccupancyDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public double OccupancyRate => TotalSeats > 0 ? Math.Round((double)BookedSeats / TotalSeats * 100, 1) : 0;
    }

    public class StatisticsService
    {
        public List<DayRevenueDto> GetLast7DaysRevenue()
        {
            var list = new List<DayRevenueDto>();
            try
            {
                // Truy vấn doanh thu theo 7 ngày gần nhất
                string sql = @"
                    SELECT CAST(p.PaidAtUtc AS DATE) AS PayDate,
                           ISNULL(SUM(p.Amount), 0) AS Revenue,
                           COUNT(DISTINCT t.TicketId) AS TicketsCount
                    FROM dbo.Payments p
                    INNER JOIN dbo.Bookings b ON p.BookingId = b.BookingId
                    LEFT JOIN dbo.BookingSeats bs ON bs.BookingId = b.BookingId AND bs.IsActiveReservation = 1
                    LEFT JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                    WHERE p.Status = 'Success'
                      AND p.PaidAtUtc >= DATEADD(DAY, -7, GETUTCDATE())
                    GROUP BY CAST(p.PaidAtUtc AS DATE)
                    ORDER BY PayDate ASC";

                var dt = DatabaseHelper.ExecuteQuery(sql);
                var dict = new Dictionary<DateTime, (decimal Rev, int Tix)>();
                foreach (DataRow row in dt.Rows)
                {
                    if (row["PayDate"] != DBNull.Value)
                    {
                        var d = Convert.ToDateTime(row["PayDate"]).Date;
                        dict[d] = (Convert.ToDecimal(row["Revenue"]), Convert.ToInt32(row["TicketsCount"]));
                    }
                }

                // Điền đủ 7 ngày kể cả ngày chưa có doanh thu để biểu đồ vẽ mượt mà
                DateTime today = DateTime.UtcNow.Date;
                for (int i = 6; i >= 0; i--)
                {
                    DateTime d = today.AddDays(-i);
                    decimal rev = dict.ContainsKey(d) ? dict[d].Rev : 0;
                    int tix = dict.ContainsKey(d) ? dict[d].Tix : 0;
                    list.Add(new DayRevenueDto
                    {
                        Date = d,
                        Revenue = rev,
                        TicketsCount = tix
                    });
                }
            }
            catch
            {
                // Fallback nếu có lỗi mạng
                DateTime today = DateTime.UtcNow.Date;
                for (int i = 6; i >= 0; i--)
                {
                    list.Add(new DayRevenueDto { Date = today.AddDays(-i), Revenue = 0, TicketsCount = 0 });
                }
            }
            return list;
        }

        public List<RoomOccupancyDto> GetRoomOccupancies()
        {
            var list = new List<RoomOccupancyDto>();
            try
            {
                string sql = @"
                    SELECT r.RoomId, r.RoomName,
                           (SELECT COUNT(*) FROM dbo.Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 1) AS TotalSeats,
                           (SELECT COUNT(DISTINCT bs.SeatId) 
                            FROM dbo.BookingSeats bs 
                            INNER JOIN dbo.Seats s ON bs.SeatId = s.SeatId 
                            WHERE s.RoomId = r.RoomId AND bs.IsActiveReservation = 1 AND bs.ReservationStatus = 'Booked') AS BookedSeats
                    FROM dbo.Rooms r
                    WHERE r.IsActive = 1";

                var dt = DatabaseHelper.ExecuteQuery(sql);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new RoomOccupancyDto
                    {
                        RoomId = Convert.ToInt32(row["RoomId"]),
                        RoomName = row["RoomName"].ToString(),
                        TotalSeats = Convert.ToInt32(row["TotalSeats"]),
                        BookedSeats = Convert.ToInt32(row["BookedSeats"])
                    });
                }
            }
            catch
            {
            }
            return list;
        }
    }
}

