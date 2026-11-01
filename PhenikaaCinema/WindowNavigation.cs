using System.Windows;

namespace PhenikaaCinema
{
    public static class WindowNavigation
    {
        public static void ReturnToHome(Window currentWindow)
        {
            if (currentWindow == null)
                return;

            // Tạo trang chủ ở trạng thái chưa đăng nhập.
            MainWindow home = new MainWindow();

            // Cập nhật cửa sổ chính trước khi đóng cửa sổ hiện tại.
            Application.Current.MainWindow = home;

            home.Show();
            currentWindow.Close();
        }
    }
}
