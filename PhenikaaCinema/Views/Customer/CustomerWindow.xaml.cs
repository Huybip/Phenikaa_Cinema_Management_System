using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Customer
{
    public partial class CustomerWindow : Window
    {
        private readonly AuthUser _user;
        private CustomerHomeView _homeView;
        private MyBookingsView _bookingsView;
        private MyTicketsView _ticketsView;
        private CustomerProfileView _profileView;

        public CustomerWindow(AuthUser user)
        {
            InitializeComponent();
            _user = user ?? SessionManager.CurrentUser;

            CustomerNameText.Text =
                _user != null && !string.IsNullOrWhiteSpace(_user.FullName)
                ? _user.FullName
                : "Khách hàng";

            InitializeViews();
            ShowTab("Home");
        }

        private void InitializeViews()
        {
            _homeView = new CustomerHomeView(_user);
            _homeView.RequestNavigateBookings += () => ShowTab("Bookings");
            _homeView.RequestNavigateTickets += () => ShowTab("Tickets");
            _homeView.RequestNavigateProfile += () => ShowTab("Profile");

            int? uid = _user?.UserId ?? SessionManager.CurrentUserId;
            _bookingsView = new MyBookingsView(uid);
            _ticketsView = new MyTicketsView(uid);
            _profileView = new CustomerProfileView(uid);
        }

        public void ShowTab(string tabName)
        {
            BtnNavHome.Style = (Style)FindResource("NavTabButton");
            BtnNavBookings.Style = (Style)FindResource("NavTabButton");
            BtnNavTickets.Style = (Style)FindResource("NavTabButton");
            BtnNavProfile.Style = (Style)FindResource("NavTabButton");

            switch (tabName)
            {
                case "Home":
                    BtnNavHome.Style = (Style)FindResource("ActiveNavTabButton");
                    _homeView.LoadData();
                    MainContentControl.Content = _homeView;
                    break;

                case "Bookings":
                    BtnNavBookings.Style = (Style)FindResource("ActiveNavTabButton");
                    _bookingsView.LoadBookings();
                    MainContentControl.Content = _bookingsView;
                    break;

                case "Tickets":
                    BtnNavTickets.Style = (Style)FindResource("ActiveNavTabButton");
                    _ticketsView.LoadTickets();
                    MainContentControl.Content = _ticketsView;
                    break;

                case "Profile":
                    BtnNavProfile.Style = (Style)FindResource("ActiveNavTabButton");
                    _profileView.LoadProfile();
                    MainContentControl.Content = _profileView;
                    break;
            }
        }

        private void BtnNavHome_Click(object sender, RoutedEventArgs e) => ShowTab("Home");
        private void BtnNavBookings_Click(object sender, RoutedEventArgs e) => ShowTab("Bookings");
        private void BtnNavTickets_Click(object sender, RoutedEventArgs e) => ShowTab("Tickets");
        private void BtnNavProfile_Click(object sender, RoutedEventArgs e) => ShowTab("Profile");

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
