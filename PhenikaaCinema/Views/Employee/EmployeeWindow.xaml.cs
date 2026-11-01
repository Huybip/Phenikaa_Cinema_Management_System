using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;

namespace PhenikaaCinema.Views.Employee
{
    public partial class EmployeeWindow : Window
    {
        private readonly AuthUser _user;
        private SellTicketView _sellTicketView;
        private BookingLookupView _bookingLookupView;
        private TicketLookupView _ticketLookupView;

        public EmployeeWindow(AuthUser user)
        {
            InitializeComponent();
            _user = user ?? SessionManager.CurrentUser;

            EmployeeNameText.Text =
                _user != null && !string.IsNullOrWhiteSpace(_user.FullName)
                ? _user.FullName
                : "Nhân viên";

            InitializeViews();
            ShowTab("Sell");
        }

        private void InitializeViews()
        {
            _sellTicketView = new SellTicketView(_user);
            _bookingLookupView = new BookingLookupView();
            _ticketLookupView = new TicketLookupView();
        }

        public void ShowTab(string tabName)
        {
            BtnNavSell.Style = (Style)FindResource("NavTabButton");
            BtnNavBookings.Style = (Style)FindResource("NavTabButton");
            BtnNavTickets.Style = (Style)FindResource("NavTabButton");
            BtnNavShowtimes.Style = (Style)FindResource("NavTabButton");

            switch (tabName)
            {
                case "Sell":
                    BtnNavSell.Style = (Style)FindResource("ActiveNavTabButton");
                    MainContentControl.Content = _sellTicketView;
                    break;

                case "Bookings":
                    BtnNavBookings.Style = (Style)FindResource("ActiveNavTabButton");
                    _bookingLookupView.LoadData();
                    MainContentControl.Content = _bookingLookupView;
                    break;

                case "Tickets":
                    BtnNavTickets.Style = (Style)FindResource("ActiveNavTabButton");
                    MainContentControl.Content = _ticketLookupView;
                    break;

                case "Showtimes":
                    BtnNavShowtimes.Style = (Style)FindResource("ActiveNavTabButton");
                    var showtimeWindow = new ShowtimeSelectionWindow(null, _user)
                    {
                        Owner = this
                    };
                    showtimeWindow.ShowDialog();
                    // Return to Sell tab
                    ShowTab("Sell");
                    break;
            }
        }

        private void BtnNavSell_Click(object sender, RoutedEventArgs e) => ShowTab("Sell");
        private void BtnNavBookings_Click(object sender, RoutedEventArgs e) => ShowTab("Bookings");
        private void BtnNavTickets_Click(object sender, RoutedEventArgs e) => ShowTab("Tickets");
        private void BtnNavShowtimes_Click(object sender, RoutedEventArgs e) => ShowTab("Showtimes");

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Logout();
                WindowNavigation.ReturnToHome(this);
            }
        }
    }
}
