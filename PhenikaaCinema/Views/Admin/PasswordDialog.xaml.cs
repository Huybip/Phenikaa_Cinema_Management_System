using System.Windows;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class PasswordDialog : Window
    {
        public string Password
        {
            get { return PasswordInput.Password; }
        }

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordInput.Password) ||
                PasswordInput.Password.Length < 8)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 8 ký tự.");
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
