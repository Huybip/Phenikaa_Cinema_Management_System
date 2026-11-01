using System.Windows;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(
            object sender, RoutedEventArgs e)
        {
            btnRegister.IsEnabled = false;

            try
            {
                string result = RegistrationService.Register(
                    txtFullName.Text,
                    txtEmail.Text,
                    txtPhone.Text,
                    txtPassword.Password,
                    txtConfirmPassword.Password);

                if (result == "SUCCESS")
                {
                    MessageBox.Show(
                        "Đăng ký thành công! Bạn có thể đăng nhập ngay.",
                        "PHENIKAA CINEMA",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                    return;
                }

                MessageBox.Show(
                    result,
                    "Đăng ký chưa thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                btnRegister.IsEnabled = true;
            }
        }

        private void btnBackToLogin_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
