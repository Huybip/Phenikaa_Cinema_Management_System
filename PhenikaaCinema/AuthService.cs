using PhenikaaCinema.Data;
using PhenikaaCinema.Models;
using System;
using System.Data.Entity;
using System.Linq;

namespace PhenikaaCinema
{
    public class AuthUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public static class AuthService
    {
        // Dang nhap bang Entity Framework 6 va BCrypt
        public static AuthUser CurrentUser { get; private set; }
        public static AuthUser Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                using (var db = new CinemaContext())
                {
                    string normalizedEmail = email.Trim();

                    var user = db.Users
                        .AsNoTracking()
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Email == normalizedEmail);

                    if (user == null)
                        return null;

                    if (!user.IsActive)
                        return null;

                    if (user.Role == null)
                        return null;

                    string passwordHash = user.PasswordHash;

                    if (string.IsNullOrWhiteSpace(passwordHash) ||
                        passwordHash.StartsWith(
                            "TEST_ONLY_", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    bool passwordValid;

                    try
                    {
                        passwordValid =
                            BCrypt.Net.BCrypt.Verify(
                                password, passwordHash);
                    }
                    catch
                    {
                        // Hash khong dung dinh dang BCrypt
                        return null;
                    }

                    if (!passwordValid)
                        return null;

                    var authUser = new AuthUser
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        RoleId = user.RoleId,
                        RoleName = user.Role.RoleName
                    };

                    CurrentUser = authUser;
                    SessionManager.SetUser(authUser);
                    return authUser;
                }
            }
            catch (Exception ex)
            {
                // Ghi log loi trong giai doan phat trien.
                System.Diagnostics.Debug.WriteLine(
                    "AuthService.Login: " + ex);

                return null;
            }
        }

        public static string GenerateAdminPasswordHash()
        {
            return BCrypt.Net.BCrypt.HashPassword("Admin@Cinema2026!");
        }


        // Chan doan nguyen nhan dang nhap that bai
        public static string DiagnoseLogin(
            string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return "Email hoac mat khau dang de trong.";
            }

            try
            {
                using (var db = new CinemaContext())
                {
                    string normalizedEmail = email.Trim();

                    var user = db.Users
                        .AsNoTracking()
                        .FirstOrDefault(
                            u => u.Email == normalizedEmail);

                    if (user == null)
                        return "Khong tim thay email trong database.";

                    if (!user.IsActive)
                        return "Tai khoan dang bi vo hieu hoa.";

                    string hash = user.PasswordHash;

                    if (string.IsNullOrWhiteSpace(hash))
                        return "PasswordHash dang trong.";

                    if (hash.StartsWith(
                        "TEST_ONLY_", StringComparison.Ordinal))
                    {
                        return "Tai khoan van dang dung TEST_ONLY.";
                    }

                    try
                    {
                        bool valid =
                            BCrypt.Net.BCrypt.Verify(password, hash);

                        if (!valid)
                        {
                            return "BCrypt.Verify tra ve false. " +
                                   "Mat khau khong khop voi hash.";
                        }

                        return "BCrypt hop le. " +
                               "Tai khoan va mat khau dung. " +
                               "Hay kiem tra tiep luong xu ly giao dien " +
                               "neu van khong dang nhap duoc.";
                    }
                    catch (Exception ex)
                    {
                        return "Loi kiem tra BCrypt: " +
                               ex.GetType().Name;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "AuthService.DiagnoseLogin: " + ex);

                return "Loi truy van Entity Framework: " +
                       ex.GetType().Name + " - " + ex.Message;
            }
        }
    }
}
