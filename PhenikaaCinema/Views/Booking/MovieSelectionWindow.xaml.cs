using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public partial class MovieSelectionWindow : Window
    {
        private readonly MovieService _movieService = new MovieService();
        private List<MovieDto> _allMovies = new List<MovieDto>();
        private readonly AuthUser _user;
        private readonly int? _targetCustomerUserId;

        public MovieSelectionWindow(AuthUser user = null, int? targetCustomerUserId = null)
        {
            InitializeComponent();
            _user = user ?? SessionManager.CurrentUser;
            _targetCustomerUserId = targetCustomerUserId;

            if (_user != null)
            {
                UserGreetText.Text = $"Xin chào, {_user.FullName} ({_user.RoleName})";
            }
            else
            {
                UserGreetText.Text = "Khách hàng";
            }

            Loaded += (s, e) => LoadMovies();
        }

        private void LoadMovies()
        {
            try
            {
                StatusMessageText.Text = "Đang tải danh sách phim...";
                StatusMessageText.Visibility = Visibility.Visible;
                MoviesItemsControl.Items.Clear();

                bool onlyNowShowing = FilterNowShowingRadio.IsChecked == true;
                _allMovies = onlyNowShowing ? _movieService.GetNowShowingMovies() : _movieService.GetAllMovies();

                RenderMovies(_allMovies);
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = "Lỗi khi tải danh sách phim: " + ex.Message;
                StatusMessageText.Visibility = Visibility.Visible;
            }
        }

        private void RenderMovies(List<MovieDto> movies)
        {
            MoviesItemsControl.Items.Clear();

            if (movies == null || movies.Count == 0)
            {
                StatusMessageText.Text = "Không có phim nào phù hợp với tìm kiếm.";
                StatusMessageText.Visibility = Visibility.Visible;
                FooterStatusText.Text = "Đang hiển thị 0 phim.";
                return;
            }

            StatusMessageText.Visibility = Visibility.Collapsed;
            FooterStatusText.Text = $"Đang hiển thị {movies.Count} phim.";

            foreach (var movie in movies)
            {
                MoviesItemsControl.Items.Add(CreateMovieCard(movie));
            }
        }

        private FrameworkElement CreateMovieCard(MovieDto movie)
        {
            var cardBorder = new Border
            {
                Width = 230,
                Margin = new Thickness(0, 0, 20, 25),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 12, ShadowDepth = 2, Opacity = 0.08 }
            };

            var mainPanel = new StackPanel();

            // POSTER AREA
            var posterBorder = new Border
            {
                Height = 310,
                Background = new SolidColorBrush(Color.FromRgb(219, 234, 254)),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                ClipToBounds = true
            };

            var posterGrid = new Grid();

            bool imageLoaded = false;
            if (movie.PosterImageSource != null)
            {
                var image = new Image
                {
                    Source = movie.PosterImageSource,
                    Stretch = Stretch.UniformToFill
                };
                posterGrid.Children.Add(image);
                imageLoaded = true;
            }

            if (!imageLoaded)
            {
                var fallbackPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                fallbackPanel.Children.Add(new TextBlock
                {
                    Text = "🎬",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                fallbackPanel.Children.Add(new TextBlock
                {
                    Text = movie.Title,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(12, 10, 12, 0)
                });
                posterGrid.Children.Add(fallbackPanel);
            }

            // Duration badge
            var durationBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8)
            };
            durationBadge.Child = new TextBlock
            {
                Text = movie.FormattedDuration,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            posterGrid.Children.Add(durationBadge);

            posterBorder.Child = posterGrid;
            mainPanel.Children.Add(posterBorder);

            // INFO AREA
            var infoBorder = new Border
            {
                Padding = new Thickness(14)
            };
            var infoPanel = new StackPanel();
            infoBorder.Child = infoPanel;

            var titleText = new TextBlock
            {
                Text = movie.Title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = movie.Title,
                Height = 22
            };
            infoPanel.Children.Add(titleText);

            var genreText = new TextBlock
            {
                Text = movie.DisplayGenres,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Margin = new Thickness(0, 4, 0, 8),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            infoPanel.Children.Add(genreText);

            // Action Buttons
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnDetail = new Button
            {
                Content = "Chi tiết",
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnDetail.Click += (s, e) => ShowMovieDetail(movie);
            Grid.SetColumn(btnDetail, 0);
            btnGrid.Children.Add(btnDetail);

            var btnBook = new Button
            {
                Content = "Đặt vé",
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(229, 9, 20)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnBook.Click += (s, e) => OpenShowtimeSelection(movie);
            Grid.SetColumn(btnBook, 2);
            btnGrid.Children.Add(btnBook);

            infoPanel.Children.Add(btnGrid);
            mainPanel.Children.Add(infoBorder);

            cardBorder.Child = mainPanel;
            return cardBorder;
        }

        private void ShowMovieDetail(MovieDto movie)
        {
            string info = $"TÊN PHIM: {movie.Title}\n" +
                          $"Thời lượng: {movie.FormattedDuration}\n" +
                          $"Thể loại: {movie.DisplayGenres}\n" +
                          $"Ngày phát hành: {movie.FormattedReleaseDate}\n" +
                          $"Trạng thái: {movie.Status}\n\n" +
                          $"NỘI DUNG PHIM:\n{(string.IsNullOrWhiteSpace(movie.Description) ? "Đang cập nhật nội dung." : movie.Description)}";

            MessageBox.Show(info, $"Thông tin: {movie.Title}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenShowtimeSelection(MovieDto movie)
        {
            var showtimeWindow = new ShowtimeSelectionWindow(movie, _user, _targetCustomerUserId)
            {
                Owner = this
            };
            showtimeWindow.ShowDialog();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim();
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(keyword) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                RenderMovies(_allMovies);
                return;
            }

            var filtered = _allMovies.FindAll(m =>
                (m.Title != null && m.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (m.Description != null && m.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (m.GenreNames != null && m.GenreNames.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));

            RenderMovies(filtered);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) LoadMovies();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            LoadMovies();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
