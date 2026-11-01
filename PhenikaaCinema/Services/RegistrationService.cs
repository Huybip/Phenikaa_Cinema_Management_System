using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PhenikaaCinema.Data;
using PhenikaaCinema.Models;

namespace PhenikaaCinema.Services
{
    public static class RegistrationService
    {
        public static string Register(
            string fullName,
            string email,
            string phone,
            string password,
            string confirmPassword)
        {
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();
            phone = (phone ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return "Vui lòng nhập họ và tên.";

            if (fullName.Length > 100)
                return "Họ và tên không được vượt quá 100 ký tự.";

            if (string.IsNullOrWhiteSpace(email))
                return "Vui lòng nhập email.";

            if (email.Length > 150 ||
                !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email không hợp lệ.";

            if (phone.Length > 25)
                return "Số điện thoại không được vượt quá 25 ký tự.";

            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return "Mật khẩu phải có ít nhất 8 ký tự.";

            if (Encoding.UTF8.GetByteCount(password) > 72)
                return "Mật khẩu quá dài. Vui lòng sử dụng mật khẩu ngắn hơn.";

            if (password != confirmPassword)
                return "Mật khẩu xác nhận không khớp.";

            try
            {
                using (var db = new CinemaContext())
                {
                    bool emailExists = db.Users.Any(
                        u => u.Email == email);

                    if (emailExists)
                        return "Email này đã được đăng ký.";

                    var customerRole = db.Roles.SingleOrDefault(
                        r => r.RoleId == 3);

                    if (customerRole == null)
                        return "Không tìm thấy vai trò Customer trong cơ sở dữ liệu.";

                    var user = new User
                    {
                        RoleId = customerRole.RoleId,
                        FullName = fullName,
                        Email = email,
                        PasswordHash =
                            BCrypt.Net.BCrypt.HashPassword(password),
                        Phone = string.IsNullOrWhiteSpace(phone)
                            ? null
                            : phone,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = null
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    return "SUCCESS";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RegistrationService.Register: " + ex);

                return "Đăng ký thất bại. Vui lòng kiểm tra kết nối " +
                       "cơ sở dữ liệu hoặc thử lại.";
            }
        }
    }
}
