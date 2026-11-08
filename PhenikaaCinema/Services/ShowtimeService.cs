using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Services
{
    public class ShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public int MovieDurationMinutes { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int RoomCapacity { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; }

        public DateTime StartTimeVn => DateTimeHelper.ToVietnamTime(StartTimeUtc);
        public DateTime EndTimeVn => DateTimeHelper.ToVietnamTime(EndTimeUtc);
        public string FormattedTimeRange => $"{StartTimeVn:HH:mm} ~ {EndTimeVn:HH:mm}";
        public string FormattedDate => StartTimeVn.ToString("dd/MM/yyyy");
        public string FormattedPrice => CurrencyHelper.FormatVnd(BasePrice);
        public bool IsAvailable => Status == "Scheduled" && StartTimeUtc > DateTime.UtcNow;

        /// <summary>Hiển thị đầy đủ: Tên phim — dd/MM/yyyy HH:mm ~ HH:mm — Phòng (dùng cho ComboBox/DataGrid binding)</summary>
        public string FormattedShowtime => $"{MovieTitle}  |  {StartTimeVn:dd/MM/yyyy  HH:mm} ~ {EndTimeVn:HH:mm}  |  {RoomName}";
    }

    public class ShowtimeService
    {
        public List<ShowtimeDto> GetAvailableShowtimes(int? movieId = null, DateTime? date = null)
        {
            // Lấy tất cả suất chiếu Scheduled từ 00:00 hôm nay trở đi (không lọc đã qua giờ, để hiển thị đủ suất trong ngày)
            string sql = @"
                SELECT st.ShowtimeId, st.MovieId, m.Title AS MovieTitle, m.DurationMinutes,
                       st.RoomId, r.RoomName, r.Capacity AS RoomCapacity,
                       st.StartTimeUtc, st.EndTimeUtc, st.BasePrice, st.Status
                FROM dbo.Showtimes st
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                WHERE st.Status = 'Scheduled'
                  AND st.StartTimeUtc >= CAST(GETUTCDATE() AS DATE)
                  AND (@MovieId IS NULL OR st.MovieId = @MovieId)
                ORDER BY st.StartTimeUtc ASC";

            var pMovieId = new SqlParameter("@MovieId", SqlDbType.Int)
            {
                Value = movieId.HasValue ? (object)movieId.Value : DBNull.Value
            };

            var table = DatabaseHelper.ExecuteQuery(sql, pMovieId);
            var list = new List<ShowtimeDto>();

            foreach (DataRow row in table.Rows)
            {
                var dto = MapShowtimeRow(row);
                if (date.HasValue)
                {
                    if (dto.StartTimeVn.Date != date.Value.Date)
                        continue;
                }
                list.Add(dto);
            }

            return list;
        }

        public List<ShowtimeDto> GetShowtimesByMovie(int movieId)
        {
            return GetAvailableShowtimes(movieId);
        }

        public ShowtimeDto GetShowtimeById(int showtimeId)
        {
            string sql = @"
                SELECT st.ShowtimeId, st.MovieId, m.Title AS MovieTitle, m.DurationMinutes,
                       st.RoomId, r.RoomName, r.Capacity AS RoomCapacity,
                       st.StartTimeUtc, st.EndTimeUtc, st.BasePrice, st.Status
                FROM dbo.Showtimes st
                INNER JOIN dbo.Movies m ON st.MovieId = m.MovieId
                INNER JOIN dbo.Rooms r ON st.RoomId = r.RoomId
                WHERE st.ShowtimeId = @ShowtimeId";

            var pId = new SqlParameter("@ShowtimeId", SqlDbType.Int) { Value = showtimeId };
            var table = DatabaseHelper.ExecuteQuery(sql, pId);
            if (table.Rows.Count == 0) return null;
            return MapShowtimeRow(table.Rows[0]);
        }

        private ShowtimeDto MapShowtimeRow(DataRow row)
        {
            return new ShowtimeDto
            {
                ShowtimeId = Convert.ToInt32(row["ShowtimeId"]),
                MovieId = Convert.ToInt32(row["MovieId"]),
                MovieTitle = row["MovieTitle"].ToString(),
                MovieDurationMinutes = row["DurationMinutes"] != DBNull.Value ? Convert.ToInt32(row["DurationMinutes"]) : 0,
                RoomId = Convert.ToInt32(row["RoomId"]),
                RoomName = row["RoomName"].ToString(),
                RoomCapacity = row["RoomCapacity"] != DBNull.Value ? Convert.ToInt32(row["RoomCapacity"]) : 0,
                StartTimeUtc = Convert.ToDateTime(row["StartTimeUtc"]),
                EndTimeUtc = Convert.ToDateTime(row["EndTimeUtc"]),
                BasePrice = Convert.ToDecimal(row["BasePrice"]),
                Status = row["Status"].ToString()
            };
        }
    }
}
