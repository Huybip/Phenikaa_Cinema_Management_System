using System;
using System.Data;
using System.Data.SqlClient;

namespace PhenikaaCinema.Services
{
    public class CustomerProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public DateTime CreatedAtVn => DateTimeHelper.ToVietnamTime(CreatedAtUtc);
        public string FormattedCreatedDate => CreatedAtVn.ToString("dd/MM/yyyy");
    }

    public class CustomerService
    {
        public CustomerProfileDto GetProfile(int userId)
        {
            string sql = @"
                SELECT UserId, FullName, Email, Phone, CreatedAtUtc
                FROM dbo.Users
                WHERE UserId = @UserId AND IsActive = 1";

            var pId = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
            var table = DatabaseHelper.ExecuteQuery(sql, pId);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new CustomerProfileDto
            {
                UserId = Convert.ToInt32(row["UserId"]),
                FullName = row["FullName"].ToString(),
                Email = row["Email"].ToString(),
                Phone = row["Phone"] != DBNull.Value ? row["Phone"].ToString() : "",
                CreatedAtUtc = Convert.ToDateTime(row["CreatedAtUtc"])
            };
        }

        public bool UpdateProfile(int userId, string fullName, string phone, out string message)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                message = "Họ tên không được để trống.";
                return false;
            }

            if (!ValidationHelper.IsValidPhoneNumber(phone))
            {
                message = "Số điện thoại không hợp lệ.";
                return false;
            }

            try
            {
                string sql = @"
                    UPDATE dbo.Users
                    SET FullName = @FullName,
                        Phone = @Phone,
                        UpdatedAtUtc = SYSUTCDATETIME()
                    WHERE UserId = @UserId";

                var pId = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
                var pName = new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = fullName.Trim() };
                var pPhone = new SqlParameter("@Phone", SqlDbType.NVarChar, 25)
                {
                    Value = string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim()
                };

                int rows = DatabaseHelper.ExecuteNonQuery(sql, pId, pName, pPhone);
                if (rows > 0)
                {
                    message = "Cập nhật thông tin thành công!";
                    return true;
                }
                message = "Không tìm thấy tài khoản người dùng.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Lỗi khi cập nhật thông tin: " + ex.Message;
                return false;
            }
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword, out string message)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                message = "Mật khẩu mới phải có ít nhất 8 ký tự.";
                return false;
            }

            try
            {
                string sqlGet = "SELECT PasswordHash FROM dbo.Users WHERE UserId = @UserId";
                var pId = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
                object currentHashObj = DatabaseHelper.ExecuteScalar(sqlGet, pId);

                if (currentHashObj == null || currentHashObj == DBNull.Value)
                {
                    message = "Tài khoản không tồn tại.";
                    return false;
                }

                string currentHash = currentHashObj.ToString();
                bool oldValid = false;
                try
                {
                    oldValid = BCrypt.Net.BCrypt.Verify(oldPassword, currentHash);
                }
                catch { }

                if (!oldValid)
                {
                    message = "Mật khẩu hiện tại không chính xác.";
                    return false;
                }

                string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                string sqlUpdate = @"
                    UPDATE dbo.Users
                    SET PasswordHash = @PasswordHash,
                        UpdatedAtUtc = SYSUTCDATETIME()
                    WHERE UserId = @UserId";

                var pNewHash = new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 255) { Value = newHash };
                var pId2 = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };

                DatabaseHelper.ExecuteNonQuery(sqlUpdate, pNewHash, pId2);
                message = "Đổi mật khẩu thành công!";
                return true;
            }
            catch (Exception ex)
            {
                message = "Lỗi khi đổi mật khẩu: " + ex.Message;
                return false;
            }
        }

        public void GetCustomerStats(int userId, out int totalBookings, out int totalTickets, out decimal totalSpent)
        {
            totalBookings = 0;
            totalTickets = 0;
            totalSpent = 0;

            try
            {
                string sql = @"
                    SELECT 
                        COUNT(DISTINCT b.BookingId) AS TotalBookings,
                        COUNT(DISTINCT t.TicketId) AS TotalTickets,
                        ISNULL(SUM(p.Amount), 0) AS TotalSpent
                    FROM dbo.Bookings b
                    LEFT JOIN dbo.Payments p ON b.BookingId = p.BookingId AND p.Status = 'Success'
                    LEFT JOIN dbo.BookingSeats bs ON b.BookingId = bs.BookingId
                    LEFT JOIN dbo.Tickets t ON bs.BookingSeatId = t.BookingSeatId
                    WHERE b.CustomerUserId = @UserId AND b.Status = 'Confirmed'";

                var pId = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
                var table = DatabaseHelper.ExecuteQuery(sql, pId);
                if (table.Rows.Count > 0)
                {
                    DataRow row = table.Rows[0];
                    totalBookings = Convert.ToInt32(row["TotalBookings"]);
                    totalTickets = Convert.ToInt32(row["TotalTickets"]);
                    totalSpent = Convert.ToDecimal(row["TotalSpent"]);
                }
            }
            catch { }
        }
    }
}
