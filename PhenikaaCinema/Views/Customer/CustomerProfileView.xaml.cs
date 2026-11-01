using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Customer
{
    public partial class CustomerProfileView : UserControl
    {
        private int? _userId;
        private readonly CustomerService _customerService = new CustomerService();

        public CustomerProfileView()
        {
            InitializeComponent();
            _userId = SessionManager.CurrentUserId;
            Loaded += CustomerProfileView_Loaded;
        }

        public CustomerProfileView(int? userId) : this()
        {
            _userId = userId ?? SessionManager.CurrentUserId;
        }

        public void SetUserId(int? userId)
        {
            _userId = userId;
            LoadProfile();
        }

        private void CustomerProfileView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        public void LoadProfile()
        {
            HideMessage();
            int? uid = _userId ?? SessionManager.CurrentUserId;
            if (!uid.HasValue)
            {
                ShowMessage("Bạn chưa đăng nhập.", false);
                return;
            }

            try
            {
                var profile = _customerService.GetProfile(uid.Value);
                if (profile != null)
                {
                    ProfileNameText.Text = string.IsNullOrWhiteSpace(profile.FullName) ? "Khách hàng" : profile.FullName;
                    ProfileEmailText.Text = profile.Email ?? "";
                    FullNameTextBox.Text = profile.FullName ?? "";
                    EmailTextBox.Text = profile.Email ?? "";
                    PhoneTextBox.Text = profile.Phone ?? "";
                    JoinedDateText.Text = profile.FormattedCreatedDate;

                    if (!string.IsNullOrWhiteSpace(profile.FullName))
                    {
                        AvatarInitialText.Text = profile.FullName.Substring(0, 1).ToUpper();
                    }
                    else
                    {
                        AvatarInitialText.Text = "P";
                    }
                }

                _customerService.GetCustomerStats(uid.Value, out int totalBookings, out int totalTickets, out decimal totalSpent);
                StatsBookingsText.Text = totalBookings.ToString();
                StatsTicketsText.Text = $"{totalTickets} vé";
                StatsSpentText.Text = CurrencyHelper.FormatVnd(totalSpent);
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi khi tải thông tin hồ sơ: " + ex.Message, false);
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            HideMessage();
            int? uid = _userId ?? SessionManager.CurrentUserId;
            if (!uid.HasValue)
            {
                ShowMessage("Bạn chưa đăng nhập.", false);
                return;
            }

            string fullName = FullNameTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            bool ok = _customerService.UpdateProfile(uid.Value, fullName, phone, out string msg);
            if (ok)
            {
                ShowMessage(msg, true);
                ProfileNameText.Text = fullName;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    AvatarInitialText.Text = fullName.Substring(0, 1).ToUpper();
                }

                if (SessionManager.CurrentUser != null && SessionManager.CurrentUser.UserId == uid.Value)
                {
                    SessionManager.CurrentUser.FullName = fullName;
                }
            }
            else
            {
                ShowMessage(msg, false);
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            HideMessage();
            int? uid = _userId ?? SessionManager.CurrentUserId;
            if (!uid.HasValue)
            {
                ShowMessage("Bạn chưa đăng nhập.", false);
                return;
            }

            string oldPass = OldPasswordBox.Password;
            string newPass = NewPasswordBox.Password;
            string confirmPass = ConfirmPasswordBox.Password;

            if (string.IsNullOrEmpty(oldPass))
            {
                ShowMessage("Vui lòng nhập mật khẩu hiện tại.", false);
                OldPasswordBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newPass) || newPass.Length < 8)
            {
                ShowMessage("Mật khẩu mới phải có ít nhất 8 ký tự.", false);
                NewPasswordBox.Focus();
                return;
            }

            if (newPass != confirmPass)
            {
                ShowMessage("Mật khẩu mới và xác nhận mật khẩu không trùng khớp.", false);
                ConfirmPasswordBox.Focus();
                return;
            }

            bool ok = _customerService.ChangePassword(uid.Value, oldPass, newPass, out string msg);
            if (ok)
            {
                ShowMessage(msg, true);
                OldPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
            }
            else
            {
                ShowMessage(msg, false);
            }
        }

        private void ShowMessage(string text, bool isSuccess)
        {
            MessageBanner.Visibility = Visibility.Visible;
            MessageBanner.Background = isSuccess ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : new SolidColorBrush(Color.FromRgb(255, 235, 238));
            MessageBannerText.Foreground = isSuccess ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) : new SolidColorBrush(Color.FromRgb(198, 40, 40));
            MessageBannerText.Text = (isSuccess ? "✅ " : "⚠️ ") + text;
        }

        private void HideMessage()
        {
            MessageBanner.Visibility = Visibility.Collapsed;
        }
    }
}

