using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PhenikaaCinema
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var address = new MailAddress(email.Trim());
                return address.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true; // SĐT là tùy chọn
            return Regex.IsMatch(phone.Trim(), @"^[0-9\+\-\s]{8,20}$");
        }
    }
}
