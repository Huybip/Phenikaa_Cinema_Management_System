using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public enum SeatStatus
    {
        Available,
        Held,
        Booked,
        Selected // Cho UI
    }

    public class SeatDto
    {
        public int SeatId { get; set; }
        public int RoomId { get; set; }
        public string RowLabel { get; set; }
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } // 'Standard', 'VIP', 'Couple'
        public decimal ExtraPrice { get; set; }
        public decimal CalculatedPrice { get; set; }
        public bool IsActive { get; set; }
        public SeatStatus Status { get; set; }

        public string SeatCode => $"{RowLabel}{SeatNumber}";
        public bool IsVip => string.Equals(SeatType, "VIP", StringComparison.OrdinalIgnoreCase);
        public bool IsCouple => string.Equals(SeatType, "Couple", StringComparison.OrdinalIgnoreCase);
        public bool CanSelect => IsActive && Status == SeatStatus.Available;
        public bool IsAvailable => CanSelect;
        public decimal Price
        {
            get => CalculatedPrice;
            set => CalculatedPrice = value;
        }
    }

    public class SeatService
    {
        public List<SeatDto> GetSeatsForShowtime(int showtimeId, int roomId, decimal basePrice)
        {
            // Tự động giải phóng các ghế giữ đã hết hạn trước khi tải sơ đồ
            try
            {
                DatabaseHelper.ExecuteNonQuery("EXEC dbo.sp_ExpireBookingHolds");
            }
            catch { }

            string sql = @"
                SELECT s.SeatId, s.RoomId, s.RowLabel, s.SeatNumber, s.SeatType, s.ExtraPrice, s.IsActive,
                       bs.ReservationStatus, bs.IsActiveReservation
                FROM dbo.Seats s
                LEFT JOIN dbo.BookingSeats bs
                    ON bs.SeatId = s.SeatId
                   AND bs.ShowtimeId = @ShowtimeId
                   AND bs.IsActiveReservation = 1
                WHERE s.RoomId = @RoomId
                  AND s.IsActive = 1
                ORDER BY s.RowLabel, s.SeatNumber";

            var pShowtimeId = new SqlParameter("@ShowtimeId", SqlDbType.Int) { Value = showtimeId };
            var pRoomId = new SqlParameter("@RoomId", SqlDbType.Int) { Value = roomId };

            var table = DatabaseHelper.ExecuteQuery(sql, pShowtimeId, pRoomId);
            var list = new List<SeatDto>();

            foreach (DataRow row in table.Rows)
            {
                var dto = new SeatDto
                {
                    SeatId = Convert.ToInt32(row["SeatId"]),
                    RoomId = Convert.ToInt32(row["RoomId"]),
                    RowLabel = row["RowLabel"].ToString().Trim(),
                    SeatNumber = Convert.ToInt32(row["SeatNumber"]),
                    SeatType = row["SeatType"].ToString().Trim(),
                    ExtraPrice = Convert.ToDecimal(row["ExtraPrice"]),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                };

                dto.CalculatedPrice = basePrice + dto.ExtraPrice;

                // Xác định trạng thái
                if (row["IsActiveReservation"] != DBNull.Value && Convert.ToBoolean(row["IsActiveReservation"]))
                {
                    string resStatus = row["ReservationStatus"] != DBNull.Value ? row["ReservationStatus"].ToString().Trim() : "";
                    if (string.Equals(resStatus, "Booked", StringComparison.OrdinalIgnoreCase))
                    {
                        dto.Status = SeatStatus.Booked;
                    }
                    else if (string.Equals(resStatus, "Held", StringComparison.OrdinalIgnoreCase))
                    {
                        dto.Status = SeatStatus.Held;
                    }
                    else
                    {
                        dto.Status = SeatStatus.Available;
                    }
                }
                else
                {
                    dto.Status = SeatStatus.Available;
                }

                list.Add(dto);
            }

            return list;
        }
    }
}
