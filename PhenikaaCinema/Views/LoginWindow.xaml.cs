using PhenikaaCinema.Services;
using System;
using System.Windows;
using System.Windows.Input;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views
{
    public partial class LoginWindow : Window
    {
        private readonly MainWindow _homeWindow;

        // Dùng khi mở cửa sổ đăng nhập độc lập
        public LoginWindow()
        {
            InitializeComponent();

            _homeWindow = null;

            Loaded += LoginWindow_Loaded;
        }

        // Dùng khi mở đăng nhập từ MainWindow
        public LoginWindow(PhenikaaCinema.MainWindow homeWindow)
        {
            InitializeComponent();

            _homeWindow = homeWindow;

            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EmailTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordInput.Password;

            ErrorTextBlock.Text = "";

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorTextBlock.Text = "Vui lòng nhập địa chỉ email.";
                EmailTextBox.Focus();
                return;
            }

            if (!IsValidEmail(email))
            {
                ErrorTextBlock.Text = "Địa chỉ email không hợp lệ.";
                EmailTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Vui lòng nhập mật khẩu.";
                PasswordInput.Focus();
                return;
            }

            try
            {
                LoginSubmitButton.IsEnabled = false;
                LoginSubmitButton.Content = "ĐANG XÁC THỰC...";

                // Xác thực bằng AuthService hiện có của dự án
                AuthUser user = AuthService.Login(email, password);

                if (user == null)
                {
                    ErrorTextBlock.Text =
                        "Email hoặc mật khẩu không chính xác.";
                    PasswordInput.Clear();
                    PasswordInput.Focus();
                    return;
                }

                Window dashboard;

                switch (user.RoleId)
                {
                    case 1:
                        dashboard = new Views.Admin.AdminWindow(user);
                        break;

                    case 2:
                        dashboard = new Views.Employee.EmployeeWindow(user);
                        break;

                    case 3:
                        dashboard = new Views.Customer.CustomerWindow(user);
                        break;

                    default:
                        ErrorTextBlock.Text =
                            "Tài khoản chưa được phân quyền.";
                        return;
                }

                // Đặt dashboard làm cửa sổ chính trước khi ẩn trang chủ
                Application.Current.MainWindow = dashboard;
                dashboard.Show();

                if (_homeWindow != null)
                {
                    _homeWindow.Hide();
                }

                Close();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text =
                    "Không thể đăng nhập. Vui lòng thử lại.";

                MessageBox.Show(
                    "Đã xảy ra lỗi khi đăng nhập:\n" + ex.Message,
                    "PHENIKAA CINEMA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoginSubmitButton.IsEnabled = true;
                LoginSubmitButton.Content = "ĐĂNG NHẬP";
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                return string.Equals(
                    address.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void BackToHomeButton_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void btnOpenRegister_Click(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            var registerWindow = new PhenikaaCinema.Views.RegisterWindow
            {
                Owner = this
            };

            registerWindow.ShowDialog();
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                PerformLogin();
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}