using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;

namespace PhenikaaCinema.Views.Customer
{
    public partial class CustomerHomeView : UserControl
    {
        private AuthUser _user;
        private readonly MovieService _movieService = new MovieService();
        private readonly PromotionService _promotionService = new PromotionService();

        public event Action RequestNavigateBookings;
        public event Action RequestNavigateTickets;
        public event Action RequestNavigateProfile;

        public CustomerHomeView()
        {
            InitializeComponent();
            _user = SessionManager.CurrentUser;
            Loaded += CustomerHomeView_Loaded;
        }

        public CustomerHomeView(AuthUser user) : this()
        {
            _user = user ?? SessionManager.CurrentUser;
        }

        public void SetUser(AuthUser user)
        {
            _user = user;
            UpdateWelcomeText();
        }

        private void CustomerHomeView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWelcomeText();
            LoadData();
        }

        private void UpdateWelcomeText()
        {
            if (_user != null && !string.IsNullOrWhiteSpace(_user.FullName))
            {
                WelcomeTitleText.Text = $"Chào mừng, {_user.FullName}!";
            }
            else
            {
                WelcomeTitleText.Text = "Chào mừng bạn đến với PHENIKAA CINEMA!";
            }
        }

        public void LoadData()
        {
            try
            {
                var movies = _movieService.GetNowShowingMovies();
                if (movies == null || movies.Count == 0)
                {
                    movies = _movieService.GetAllMovies();
                }

                MoviesListControl.ItemsSource = movies;
                MovieCountText.Text = $"{movies?.Count ?? 0} phim đang sẵn sàng";

                // Đưa phim đầu tiên vào Showcase 3D Poster
                if (movies != null && movies.Count > 0 && HeroPoster3D != null)
                {
                    HeroPoster3D.SetMovie(movies[0]);
                }

                var promos = _promotionService.GetActivePromotions();
                PromotionsListControl.ItemsSource = promos;
            }
            catch (Exception ex)
            {
                MovieCountText.Text = "Không thể tải dữ liệu";
                MessageBox.Show("Lỗi khi tải dữ liệu trang chủ: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnBookNow_Click(object sender, RoutedEventArgs e)
        {
            var window = new MovieSelectionWindow(_user);
            window.ShowDialog();
        }

        private void BtnBrowseMovies_Click(object sender, RoutedEventArgs e)
        {
            var window = new MovieSelectionWindow(_user);
            window.ShowDialog();
        }

        private void BtnShowtimes_Click(object sender, RoutedEventArgs e)
        {
            var window = new ShowtimeSelectionWindow(null, _user);
            window.ShowDialog();
        }

        private void BtnMyBookings_Click(object sender, RoutedEventArgs e)
        {
            if (RequestNavigateBookings != null)
            {
                RequestNavigateBookings.Invoke();
            }
            else
            {
                int? uid = _user?.UserId ?? SessionManager.CurrentUserId;
                var window = new BookingHistoryWindow(uid);
                window.ShowDialog();
            }
        }

        private void BtnMyTickets_Click(object sender, RoutedEventArgs e)
        {
            if (RequestNavigateTickets != null)
            {
                RequestNavigateTickets.Invoke();
            }
            else
            {
                int? uid = _user?.UserId ?? SessionManager.CurrentUserId;
                var window = new BookingHistoryWindow(uid);
                window.ShowDialog();
            }
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            RequestNavigateProfile?.Invoke();
        }

        private void BtnBookMovieItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int movieId))
            {
                var movie = _movieService.GetMovieById(movieId);
                var window = new ShowtimeSelectionWindow(movie, _user);
                window.ShowDialog();
            }
            else
            {
                var window = new MovieSelectionWindow(_user);
                window.ShowDialog();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}

