using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhenikaaCinema.Services;
using System.Diagnostics;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema
{
    public partial class MainWindow : Window
    {
        private DataTable _moviesTable;

        public MainWindow()
        {
            InitializeComponent();

            // Homepage cong khai: khong bat buoc dang nhap
            LoginButton.Visibility = Visibility.Visible;
            LoggedInPanel.Visibility = Visibility.Collapsed;

            LoadMovies();
        }

        // Hien thi thong tin sau khi dang nhap thanh cong
        public void ShowLoggedInMode(AuthUser user)
        {
            if (user == null)
                return;

            LoginButton.Visibility = Visibility.Collapsed;
            LoggedInPanel.Visibility = Visibility.Visible;

            TopUserName.Text = user.FullName;
            TopUserName.ToolTip = "Vai trò: " + user.RoleName;
        }

        // Tai danh sach phim tu SQL Server
        private void LoadMovies()
        {
            try
            {
                _moviesTable = DatabaseHelper.GetMovies();

                if (_moviesTable == null)
                {
                    MoviesItemsControl.ItemsSource = null;
                    MoviesItemsControl.Items.Clear();
                    MoviesStatusText.Text =
                        "Không nhận được dữ liệu từ database.";
                    return;
                }

                DisplayMovies(_moviesTable);

                MoviesStatusText.Text =
                    "Đã tải " + _moviesTable.Rows.Count +
                    " phim từ database.";
            }
            catch (Exception ex)
            {
                MoviesItemsControl.ItemsSource = null;
                MoviesItemsControl.Items.Clear();

                MoviesStatusText.Text =
                    "Không thể tải danh sách phim.";

                MessageBox.Show(
                    "Không thể tải danh sách phim:\n" + ex.Message,
                    "PHENIKAA CINEMA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Tim cot trong DataTable, khong phan biet chu hoa/thuong
        private string FindColumn(DataTable table, params string[] names)
        {
            if (table == null)
                return null;

            foreach (string name in names)
            {
                foreach (DataColumn column in table.Columns)
                {
                    if (string.Equals(
                        column.ColumnName,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return column.ColumnName;
                    }
                }
            }

            return null;
        }

        private void btnTestEF_Click(object sender, RoutedEventArgs e)
        {
            var service = new EntityFrameworkTestService();

            MessageBox.Show(
                service.TestConnection(),
                "Kiểm tra Entity Framework",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string GetValue(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName))
                return "";

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
                return "";

            return value.ToString();
        }

        // Hien thi cac the phim tren homepage
                // Hien thi cac the phim tren homepage
        private void DisplayMovies(DataTable table)
        {
            MoviesItemsControl.ItemsSource = null;
            MoviesItemsControl.Items.Clear();

            if (table == null || table.Rows.Count == 0)
            {
                MoviesItemsControl.Items.Add(
                    CreateMessageCard("Hiện chưa có phim nào để hiển thị trong mục này."));
                return;
            }

            string titleColumn = FindColumn(table, "Title", "MovieTitle", "MovieName", "Name");
            string genreColumn = FindColumn(table, "GenreNames", "GenreName", "Genre", "Category");
            string descriptionColumn = FindColumn(table, "Description", "Synopsis", "Summary");
            string releaseDateColumn = FindColumn(table, "ReleaseDate", "ReleaseYear", "Year");
            string idColumn = FindColumn(table, "MovieId", "Id", "MovieID");
            string posterColumn = FindColumn(table, "PosterUrl", "Poster", "ImagePath", "Image");
            string durationColumn = FindColumn(table, "DurationMinutes", "Duration");

            if (titleColumn == null)
            {
                MoviesItemsControl.Items.Add(
                    CreateMessageCard("Không tìm thấy cột tên phim trong bảng Movies."));
                MoviesStatusText.Text = "Vui lòng kiểm tra cấu trúc bảng Movies.";
                return;
            }

            foreach (DataRow row in table.Rows)
            {
                string title = GetValue(row, titleColumn);
                string genre = GetValue(row, genreColumn);
                string description = GetValue(row, descriptionColumn);
                string releaseDate = GetValue(row, releaseDateColumn);
                string movieId = GetValue(row, idColumn);
                string posterUrl = posterColumn != null ? GetValue(row, posterColumn) : null;
                string duration = durationColumn != null ? GetValue(row, durationColumn) : null;

                if (string.IsNullOrWhiteSpace(title))
                    title = "Phim chưa có tên";

                MoviesItemsControl.Items.Add(
                    CreateMovieCard(
                        title,
                        genre,
                        description,
                        releaseDate,
                        movieId,
                        posterUrl,
                        duration));
            }
        }

        // Tao the phim hien dai (White Card + Blue/Orange Theme + Real Poster Support)
        private Border CreateMovieCard(
            string title,
            string genre,
            string description,
            string releaseDate,
            string movieId,
            string posterUrl = null,
            string duration = null)
        {
            Border card = new Border
            {
                Width = 220,
                Margin = new Thickness(0, 0, 20, 24),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 2,
                    Direction = 270,
                    Color = Colors.Black,
                    Opacity = 0.08
                }
            };

            StackPanel content = new StackPanel();

            // Khung poster
            Grid posterContainer = new Grid
            {
                Height = 230,
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };

            bool imageLoaded = false;
            if (!string.IsNullOrWhiteSpace(posterUrl))
            {
                try
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;

                    if (posterUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        posterUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        bmp.UriSource = new Uri(posterUrl, UriKind.Absolute);
                    }
                    else
                    {
                        string fullPath = Path.IsPathRooted(posterUrl)
                            ? posterUrl
                            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, posterUrl.TrimStart('\\', '/'));

                        if (File.Exists(fullPath))
                        {
                            bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                        }
                    }

                    if (bmp.UriSource != null)
                    {
                        bmp.EndInit();
                        Image img = new Image
                        {
                            Source = bmp,
                            Stretch = Stretch.UniformToFill,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        posterContainer.Children.Add(img);
                        imageLoaded = true;
                    }
                }
                catch { }
            }

            if (!imageLoaded)
            {
                // Fallback poster phong cách Phenikaa Cinema
                Border fallbackBg = new Border
                {
                    Background = new LinearGradientBrush(
                        Color.FromRgb(30, 58, 138),
                        Color.FromRgb(15, 23, 42),
                        new Point(0, 0),
                        new Point(1, 1))
                };
                posterContainer.Children.Add(fallbackBg);

                StackPanel fallbackContent = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock filmIcon = new TextBlock
                {
                    Text = "\uE8B2",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 38,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                TextBlock brandText = new TextBlock
                {
                    Text = "PHENIKAA\nCINEMA",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                fallbackContent.Children.Add(filmIcon);
                fallbackContent.Children.Add(brandText);
                posterContainer.Children.Add(fallbackContent);
            }

            // Duration Pill ở góc trên phải
            if (!string.IsNullOrWhiteSpace(duration) && int.TryParse(duration, out int durVal) && durVal > 0)
            {
                Border durationBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(7, 3, 7, 3),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 8, 0)
                };
                durationBadge.Child = new TextBlock
                {
                    Text = durVal + "p",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                posterContainer.Children.Add(durationBadge);
            }

            // Thong tin chi tiet
            StackPanel details = new StackPanel
            {
                Margin = new Thickness(14, 12, 14, 14)
            };

            // The loai pill
            Border genreBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(219, 234, 254)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6)
            };
            genreBadge.Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(genre) ? "Phim chiếu rạp" : genre,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 180
            };

            TextBlock titleText = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 42,
                Margin = new Thickness(0, 0, 0, 4)
            };

            // Format date
            string dateStr = "";
            if (!string.IsNullOrWhiteSpace(releaseDate))
            {
                if (DateTime.TryParse(releaseDate, out DateTime dt))
                    dateStr = "Khởi chiếu: " + dt.ToString("dd/MM/yyyy");
                else
                    dateStr = "Khởi chiếu: " + releaseDate;
            }

            TextBlock dateText = new TextBlock
            {
                Text = string.IsNullOrEmpty(dateStr) ? "Đang cập nhật" : dateStr,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 12)
            };

            // Nút Thao tác: Chi tiết & Đặt vé
            Grid buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            Button detailButton = new Button
            {
                Content = "Chi tiết",
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            detailButton.Click += (sender, args) =>
            {
                string message = title;
                if (!string.IsNullOrWhiteSpace(genre)) message += "\nThể loại: " + genre;
                if (!string.IsNullOrWhiteSpace(releaseDate)) message += "\nNgày phát hành: " + releaseDate;
                if (!string.IsNullOrWhiteSpace(duration)) message += "\nThời lượng: " + duration + " phút";
                if (!string.IsNullOrWhiteSpace(description)) message += "\n\n" + description;

                MessageBox.Show(message, "Thông tin phim", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            Button bookButton = new Button
            {
                Content = "Đặt vé",
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(249, 115, 22)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };

            bookButton.Click += (sender, args) =>
            {
                int.TryParse(movieId, out int parsedId);
                var movieDto = new PhenikaaCinema.Services.MovieDto
                {
                    MovieId = parsedId,
                    Title = title,
                    GenreNames = genre,
                    Description = description,
                    PosterUrl = posterUrl
                };

                var showtimeWindow = new PhenikaaCinema.Views.Booking.ShowtimeSelectionWindow(movieDto, SessionManager.CurrentUser)
                {
                    Owner = this
                };
                showtimeWindow.ShowDialog();
            };

            Grid.SetColumn(detailButton, 0);
            Grid.SetColumn(bookButton, 2);
            buttonGrid.Children.Add(detailButton);
            buttonGrid.Children.Add(bookButton);

            details.Children.Add(genreBadge);
            details.Children.Add(titleText);
            details.Children.Add(dateText);
            details.Children.Add(buttonGrid);

            content.Children.Add(posterContainer);
            content.Children.Add(details);

            card.Child = content;
            card.ToolTip = "Mã phim: " + movieId;
            return card;
        }

        private Border CreateMessageCard(string message)
        {
            return new Border
            {
                Width = 340,
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 16, 16),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Direction = 270, Color = Colors.Black, Opacity = 0.08 },
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };
        }

        // Tim kiem phim theo ten
        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_moviesTable == null)
                return;

            string keyword = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                DisplayMovies(_moviesTable);

                MoviesStatusText.Text =
                    "Đang hiển thị " + _moviesTable.Rows.Count + " phim.";

                return;
            }

            string titleColumn = FindColumn(
                _moviesTable,
                "Title", "MovieTitle", "MovieName", "Name");

            if (titleColumn == null)
            {
                MoviesStatusText.Text =
                    "Không tìm thấy cột tên phim.";

                return;
            }

            DataTable filteredTable = _moviesTable.Clone();

            foreach (DataRow row in _moviesTable.Rows)
            {
                string title = GetValue(row, titleColumn);

                if (title.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredTable.ImportRow(row);
                }
            }

            DisplayMovies(filteredTable);

            MoviesStatusText.Text =
                "Tìm thấy " + filteredTable.Rows.Count + " phim.";
        }

        // Nut tai lai danh sach phim
        private void LoadMovies_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadMovies();
        }

        // Kiem tra ket noi database
        private void TestConnection_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool success = DatabaseHelper.TestConnection(
                out string message);

            MessageBox.Show(
                message,
                "PHENIKAA CINEMA",
                MessageBoxButton.OK,
                success
                    ? MessageBoxImage.Information
                    : MessageBoxImage.Error);
        }

        // Dieu huong homepage
        private void HomeMenuButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            MoviesHeading.Text = "Phim đang chiếu";
            LoadMovies();
        }

        private void NowShowingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MoviesHeading.Text = "Phim đang chiếu";
            LoadMovies();
        }

        private void ComingSoonButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MoviesHeading.Text = "Phim sắp chiếu";

            MessageBox.Show(
                "Chức năng lọc phim sắp chiếu cần được kết nối với dữ liệu ngày phát hành.",
                "PHENIKAA CINEMA",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AboutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "PHENIKAA CINEMA\n\n" +
                "Không gian dành cho những người yêu điện ảnh.\n" +
                "Khám phá phim và trải nghiệm đặt vé thuận tiện.",
                "Về chúng tôi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Mo cua so dang nhap
        private void OpenLoginButton_Click(
        object sender,
        RoutedEventArgs e)
        {
            PhenikaaCinema.Views.LoginWindow loginWindow =
            new PhenikaaCinema.Views.LoginWindow(this);

            loginWindow.ShowDialog();
        }

        // Cap nhat giao dien khi dang xuat
        private void LogoutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Bạn có muốn đăng xuất không?",
                "PHENIKAA CINEMA",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoggedInPanel.Visibility = Visibility.Collapsed;
                LoginButton.Visibility = Visibility.Visible;
                TopUserName.Text = "";
            }
        }
    }
}
