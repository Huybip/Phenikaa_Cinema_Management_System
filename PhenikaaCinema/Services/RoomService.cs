using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoomService
    {
        public List<RoomDto> GetAllRooms(bool activeOnly = true)
        {
            string sql = @"
                SELECT RoomId, RoomName, Capacity, IsActive
                FROM dbo.Rooms
                WHERE (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY RoomName";

            var pActive = new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly };
            var table = DatabaseHelper.ExecuteQuery(sql, pActive);

            var list = new List<RoomDto>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(new RoomDto
                {
                    RoomId = Convert.ToInt32(row["RoomId"]),
                    RoomName = row["RoomName"].ToString(),
                    Capacity = Convert.ToInt32(row["Capacity"]),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }
            return list;
        }
    }
}
