using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class DashboardView : UserControl
    {
        private readonly ReportService _reportService = new ReportService();
        private readonly MovieService _movieService = new MovieService();
        private readonly BookingService _bookingService = new BookingService();

        // Events for parent window navigation
        public event Action RequestNavigateReports;
        public event Action RequestNavigateMovies;
        public event Action RequestNavigateShowtimes;
        public event Action RequestNavigateBookings;

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        public void LoadDashboard()
        {
            try
            {
                // Tháng này
                DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeKind.Local);
                DateTime endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
                DateTime startOfMonthUtc = startOfMonth.ToUniversalTime();
                DateTime endOfMonthUtc = endOfMonth.ToUniversalTime();

                var monthSummary = _reportService.GetRevenueSummary(startOfMonthUtc, endOfMonthUtc);
                MonthRevenueText.Text = monthSummary.FormattedRevenue;
                MonthTicketsText.Text = $"{monthSummary.TotalTicketsSold} vé";
                MonthConfirmedText.Text = $"{monthSummary.TotalConfirmedBookings} đơn";
                MonthMoviesText.Text = $"{monthSummary.TotalMoviesShown} phim";

                // Hôm nay
                DateTime startOfDayUtc = DateTime.UtcNow.Date;
                DateTime endOfDayUtc = startOfDayUtc.AddDays(1).AddSeconds(-1);
                var todaySummary = _reportService.GetRevenueSummary(startOfDayUtc, endOfDayUtc);
                RevenueTodayText.Text = todaySummary.FormattedRevenue;
                RevenueTodaySubText.Text = $"{todaySummary.TotalTicketsSold} vé hôm nay";
                TicketsMonthText.Text = monthSummary.TotalTicketsSold.ToString();
                TicketsMonthSubText.Text = "Cả tháng này";

                // Đơn chờ thanh toán
                try
                {
                    var allBookings = _bookingService.GetAllBookings(null, "PendingPayment");
                    PendingBookingsText.Text = allBookings.Count.ToString();
                }
                catch
                {
                    PendingBookingsText.Text = "N/A";
                }

                // Số phim đang chiếu
                try
                {
                    var movies = _movieService.GetNowShowingMovies();
                    NowShowingCountText.Text = (movies?.Count ?? 0).ToString();
                }
                catch
                {
                    NowShowingCountText.Text = "N/A";
                }

                // Top phim
                var topMovies = _reportService.GetRevenueByMovie(startOfMonthUtc, endOfMonthUtc);
                TopMoviesDataGrid.ItemsSource = topMovies;

                // Tải dữ liệu biểu đồ 2D
                ChartsControl2D?.LoadChartData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dashboard: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void BtnViewReports_Click(object sender, RoutedEventArgs e)
        {
            RequestNavigateReports?.Invoke();
        }

        private void QuickAccessMovies_Click(object sender, MouseButtonEventArgs e)
        {
            RequestNavigateMovies?.Invoke();
        }

        private void QuickAccessShowtimes_Click(object sender, MouseButtonEventArgs e)
        {
            RequestNavigateShowtimes?.Invoke();
        }

        private void QuickAccessBookings_Click(object sender, MouseButtonEventArgs e)
        {
            RequestNavigateBookings?.Invoke();
        }
    }
}

