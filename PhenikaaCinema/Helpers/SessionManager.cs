using System;

namespace PhenikaaCinema.Helpers
{
    public static class SessionManager
    {
        public static AuthUser CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static int? CurrentUserId => CurrentUser?.UserId;

        public static int? CurrentRoleId => CurrentUser?.RoleId;

        public static string CurrentFullName => CurrentUser?.FullName ?? "Khách";

        public static string CurrentEmail => CurrentUser?.Email ?? "";

        public static string CurrentRoleName => CurrentUser?.RoleName ?? "Khách";

        public static bool IsAdmin => CurrentUser != null && CurrentUser.RoleId == 1;

        public static bool IsEmployee => CurrentUser != null && CurrentUser.RoleId == 2;

        public static bool IsCustomer => CurrentUser != null && CurrentUser.RoleId == 3;

        public static event Action<AuthUser> UserChanged;

        public static void SetUser(AuthUser user)
        {
            CurrentUser = user;
            UserChanged?.Invoke(CurrentUser);
        }

        public static void Logout()
        {
            CurrentUser = null;
            UserChanged?.Invoke(null);
        }
    }
}
