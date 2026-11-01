using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalConfirmedBookings { get; set; }
        public int TotalMoviesShown { get; set; }

        public string FormattedRevenue => CurrencyHelper.FormatVnd(TotalRevenue);
    }

    public class MovieRevenueDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public int TicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        public string FormattedRevenue => CurrencyHelper.FormatVnd(TotalRevenue);
    }

    public class ReportService
    {
        public RevenueSummaryDto GetRevenueSummary(DateTime? fromDateUtc = null, DateTime? toDateUtc = null)
        {
            string sql = @"
                SELECT 
                    ISNULL(SUM(p.Amount), 0) AS TotalRevenue,
                    COUNT(DISTINCT t.TicketId) AS TotalTicketsSold,
                    COUNT(DISTINCT b.BookingId) AS TotalConfirmedBookings,
                    COUNT(DISTINCT st.MovieId) AS TotalMoviesShown
                FROM dbo.Payments p
                INNER JOIN dbo.Bookings b ON p.BookingId = b.BookingId
                INNER JOIN dbo.Showtimes st ON b.ShowtimeId = st.ShowtimeId
                LEFT JOIN dbo.BookingSeats bs ON bs.BookingId = b.BookingId AND bs.IsActiveReservation = 1 AND bs.ReservationStatus = 'Booked'
                LEFT JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                WHERE p.Status = 'Success'
                  AND b.Status = 'Confirmed'
                  AND (@FromDate IS NULL OR p.PaidAtUtc >= @FromDate)
                  AND (@ToDate IS NULL OR p.PaidAtUtc <= @ToDate)";

            var pFrom = new SqlParameter("@FromDate", SqlDbType.DateTime2)
            {
                Value = fromDateUtc.HasValue ? (object)fromDateUtc.Value : DBNull.Value
            };
            var pTo = new SqlParameter("@ToDate", SqlDbType.DateTime2)
            {
                Value = toDateUtc.HasValue ? (object)toDateUtc.Value : DBNull.Value
            };

            var table = DatabaseHelper.ExecuteQuery(sql, pFrom, pTo);
            if (table.Rows.Count == 0) return new RevenueSummaryDto();

            DataRow row = table.Rows[0];
            return new RevenueSummaryDto
            {
                TotalRevenue = Convert.ToDecimal(row["TotalRevenue"]),
                TotalTicketsSold = Convert.ToInt32(row["TotalTicketsSold"]),
                TotalConfirmedBookings = Convert.ToInt32(row["TotalConfirmedBookings"]),
                TotalMoviesShown = Convert.ToInt32(row["TotalMoviesShown"])
            };
        }

        public List<MovieRevenueDto> GetRevenueByMovie(DateTime? fromDateUtc = null, DateTime? toDateUtc = null)
        {
            string sql = @"
                SELECT 
                    m.MovieId,
                    m.Title,
                    COUNT(DISTINCT t.TicketId) AS TicketsSold,
                    ISNULL(SUM(p.Amount), 0) AS TotalRevenue
                FROM dbo.Movies m
                INNER JOIN dbo.Showtimes st ON st.MovieId = m.MovieId
                INNER JOIN dbo.Bookings b ON b.ShowtimeId = st.ShowtimeId AND b.Status = 'Confirmed'
                INNER JOIN dbo.Payments p ON p.BookingId = b.BookingId AND p.Status = 'Success'
                LEFT JOIN dbo.BookingSeats bs ON bs.BookingId = b.BookingId AND bs.IsActiveReservation = 1 AND bs.ReservationStatus = 'Booked'
                LEFT JOIN dbo.Tickets t ON t.BookingSeatId = bs.BookingSeatId
                WHERE (@FromDate IS NULL OR p.PaidAtUtc >= @FromDate)
                  AND (@ToDate IS NULL OR p.PaidAtUtc <= @ToDate)
                GROUP BY m.MovieId, m.Title
                ORDER BY TotalRevenue DESC";

            var pFrom = new SqlParameter("@FromDate", SqlDbType.DateTime2)
            {
                Value = fromDateUtc.HasValue ? (object)fromDateUtc.Value : DBNull.Value
            };
            var pTo = new SqlParameter("@ToDate", SqlDbType.DateTime2)
            {
                Value = toDateUtc.HasValue ? (object)toDateUtc.Value : DBNull.Value
            };

            var table = DatabaseHelper.ExecuteQuery(sql, pFrom, pTo);
            var list = new List<MovieRevenueDto>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new MovieRevenueDto
                {
                    MovieId = Convert.ToInt32(row["MovieId"]),
                    Title = row["Title"].ToString(),
                    TicketsSold = Convert.ToInt32(row["TicketsSold"]),
                    TotalRevenue = Convert.ToDecimal(row["TotalRevenue"])
                });
            }

            return list;
        }
    }
}
