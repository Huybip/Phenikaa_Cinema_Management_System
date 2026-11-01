using System.Windows;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class AdminWindow : Window
    {
        private DashboardView _dashboardView;
        private RevenueReportView _revenueReportView;

        public AdminWindow(AuthUser user)
        {
            InitializeComponent();

            AdminNameText.Text =
                user != null && !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : "Quáº£n trá»‹ viÃªn";

            InitializeViews();
            ShowDashboard();
        }

        private void InitializeViews()
        {
            _dashboardView = new DashboardView();
            _dashboardView.RequestNavigateReports += ShowRevenueReport;
            _dashboardView.RequestNavigateMovies += () => OpenDialog(new MovieManagementWindow());
            _dashboardView.RequestNavigateShowtimes += () => OpenDialog(new ShowtimeManagementWindow());
            _dashboardView.RequestNavigateBookings += () => OpenDialog(new BookingManagementWindow());

            _revenueReportView = new RevenueReportView();
        }

        private void ShowDashboard()
        {
            BtnSidebarOverview.Style = (Style)FindResource("ActiveSidebarButton");
            BtnSidebarRevenue.Style = (Style)FindResource("SidebarButton");
            _dashboardView.LoadDashboard();
            AdminMainContent.Content = _dashboardView;
        }

        private void ShowRevenueReport()
        {
            BtnSidebarOverview.Style = (Style)FindResource("SidebarButton");
            BtnSidebarRevenue.Style = (Style)FindResource("ActiveSidebarButton");
            _revenueReportView.LoadReport();
            AdminMainContent.Content = _revenueReportView;
        }

        private void OpenDialog(Window win)
        {
            win.Owner = this;
            win.ShowDialog();
        }

        private void OverviewButton_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void RevenueReportButton_Click(object sender, RoutedEventArgs e) => ShowRevenueReport();
        private void MoviesButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new MovieManagementWindow());
        private void GenresButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new GenreManagementWindow());
        private void ShowtimesButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new ShowtimeManagementWindow());
        private void RoomsButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new RoomManagementWindow());
        private void UsersButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new UserManagementWindow());
        private void BookingsButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new BookingManagementWindow());
        private void PromotionsButton_Click(object sender, RoutedEventArgs e) => OpenDialog(new PromotionManagementWindow());
        private void ReportsButton_Click(object sender, RoutedEventArgs e) => ShowRevenueReport();

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n Ä‘Äƒng xuáº¥t?",
                "XÃ¡c nháº­n Ä‘Äƒng xuáº¥t",
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
