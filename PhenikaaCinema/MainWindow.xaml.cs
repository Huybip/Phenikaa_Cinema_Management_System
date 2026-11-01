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
        private void DisplayMovies(DataTable table)
        {
            MoviesItemsControl.ItemsSource = null;
            MoviesItemsControl.Items.Clear();

            if (table == null || table.Rows.Count == 0)
            {
                MoviesItemsControl.Items.Add(
                    CreateMessageCard(
                        "Hiện chưa có phim để hiển thị."));

                return;
            }

            string titleColumn = FindColumn(
                table, "Title", "MovieTitle", "MovieName", "Name");

            string genreColumn = FindColumn(
                table, "GenreName", "Genre", "Category");

            string descriptionColumn = FindColumn(
                table, "Description", "Synopsis", "Summary");

            string releaseDateColumn = FindColumn(
                table, "ReleaseDate", "ReleaseYear", "Year");

            string idColumn = FindColumn(
                table, "MovieId", "Id", "MovieID");

            if (titleColumn == null)
            {
                MoviesItemsControl.Items.Add(
                    CreateMessageCard(
                        "Không tìm thấy cột tên phim trong bảng Movies."));

                MoviesStatusText.Text =
                    "Vui lòng kiểm tra cấu trúc bảng Movies.";

                return;
            }

            foreach (DataRow row in table.Rows)
            {
                string title = GetValue(row, titleColumn);
                string genre = GetValue(row, genreColumn);
                string description = GetValue(row, descriptionColumn);
                string releaseDate = GetValue(row, releaseDateColumn);
                string movieId = GetValue(row, idColumn);

                if (string.IsNullOrWhiteSpace(title))
                    title = "Phim chưa có tên";

                MoviesItemsControl.Items.Add(
                    CreateMovieCard(
                        title,
                        genre,
                        description,
                        releaseDate,
                        movieId));
            }
        }

        // Tao the phim
        private Border CreateMovieCard(
            string title,
            string genre,
            string description,
            string releaseDate,
            string movieId)
        {
            Border card = new Border
            {
                Width = 190,
                Margin = new Thickness(0, 0, 16, 20),
                Background = new SolidColorBrush(
                    Color.FromRgb(27, 27, 34)),
                CornerRadius = new CornerRadius(9),
                ClipToBounds = true
            };

            StackPanel content = new StackPanel();

            // Poster minh hoa, chua phai poster that
            Border poster = new Border
            {
                Height = 225,
                Background = new SolidColorBrush(
                    Color.FromRgb(64, 24, 34))
            };

            Grid posterGrid = new Grid();

            TextBlock posterIcon = new TextBlock
            {
                Text = "▶",
                FontSize = 42,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock posterLabel = new TextBlock
            {
                Text = "PHENIKAA\nCINEMA",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 8, 16)
            };

            posterGrid.Children.Add(posterIcon);
            posterGrid.Children.Add(posterLabel);
            poster.Child = posterGrid;

            StackPanel details = new StackPanel
            {
                Margin = new Thickness(12, 11, 12, 12)
            };

            TextBlock titleText = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 44
            };

            TextBlock genreText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(genre)
                    ? "Phim điện ảnh"
                    : genre,
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(180, 180, 190)),
                Margin = new Thickness(0, 7, 0, 3),
                TextWrapping = TextWrapping.Wrap
            };

            TextBlock dateText = new TextBlock
            {
                Text = releaseDate,
                FontSize = 11,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(145, 145, 155)),
                Margin = new Thickness(0, 2, 0, 0)
            };

            Grid buttonGrid = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0)
            };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button detailButton = new Button
            {
                Content = "Chi tiết",
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 50)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };

            detailButton.Click += (sender, args) =>
            {
                string message = title;

                if (!string.IsNullOrWhiteSpace(genre))
                    message += "\nThể loại: " + genre;

                if (!string.IsNullOrWhiteSpace(releaseDate))
                    message += "\nNgày phát hành: " + releaseDate;

                if (!string.IsNullOrWhiteSpace(description))
                    message += "\n\n" + description;

                MessageBox.Show(
                    message,
                    "Thông tin phim",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };

            Button bookButton = new Button
            {
                Content = "Đặt vé",
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(229, 9, 20)),
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
                    Description = description
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

            details.Children.Add(titleText);
            details.Children.Add(genreText);

            if (!string.IsNullOrWhiteSpace(releaseDate))
                details.Children.Add(dateText);

            details.Children.Add(buttonGrid);

            content.Children.Add(poster);
            content.Children.Add(details);

            card.Child = content;
            card.ToolTip = "Mã phim: " + movieId;

            return card;
        }

        private Border CreateMessageCard(string message)
        {
            return new Border
            {
                Width = 320,
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 12, 12),
                Background = new SolidColorBrush(
                    Color.FromRgb(27, 27, 34)),
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
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
