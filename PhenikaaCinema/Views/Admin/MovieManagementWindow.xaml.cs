using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public class MovieItem
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PosterUrl { get; set; }
        public string TrailerUrl { get; set; }
        public string Status { get; set; }
    }

    public class GenreSelectionItem
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; }
        public bool IsSelected { get; set; }
    }

    public partial class MovieManagementWindow : Window
    {
        private readonly string _connectionString;
        private readonly GenreService _genreService = new GenreService();
        private List<GenreSelectionItem> _genreCheckboxes = new List<GenreSelectionItem>();
        private List<MovieItem> _allMovies = new List<MovieItem>();
        private bool _isLoading;

        public MovieManagementWindow()
        {
            InitializeComponent();

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["CinemaDb"];

            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                MessageBox.Show(
                    "Không tìm thấy connection string 'CinemaDb' trong App.config.",
                    "Lỗi cấu hình",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
                return;
            }

            _connectionString = settings.ConnectionString;

            StatusComboBox.SelectedIndex = 0;
            LoadGenresList();
            LoadMovies();
        }

        private void LoadGenresList()
        {
            try
            {
                var genres = _genreService.GetAllGenres(activeOnly: false);
                _genreCheckboxes = genres.Select(g => new GenreSelectionItem
                {
                    GenreId = g.GenreId,
                    GenreName = g.GenreName,
                    IsSelected = false
                }).ToList();
                GenresItemsControl.ItemsSource = _genreCheckboxes;
            }
            catch { }
        }

        private void SetMovieGenresSelection(int movieId)
        {
            try
            {
                var selectedIds = _genreService.GetGenreIdsForMovie(movieId);
                foreach (var item in _genreCheckboxes)
                {
                    item.IsSelected = selectedIds.Contains(item.GenreId);
                }
                GenresItemsControl.ItemsSource = null;
                GenresItemsControl.ItemsSource = _genreCheckboxes;
            }
            catch { }
        }

        private List<int> GetSelectedGenreIds()
        {
            return _genreCheckboxes.Where(g => g.IsSelected).Select(g => g.GenreId).ToList();
        }

        private void LoadMovies()
        {
            try
            {
                List<MovieItem> movies = new List<MovieItem>();

                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    string sql = @"
                        SELECT MovieId, Title, Description, DurationMinutes,
                               ReleaseDate, EndDate, PosterUrl, TrailerUrl, Status
                        FROM dbo.Movies
                        ORDER BY MovieId DESC;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                movies.Add(new MovieItem
                                {
                                    MovieId = Convert.ToInt32(reader["MovieId"]),
                                    Title = Convert.ToString(reader["Title"]),
                                    Description = reader["Description"] == DBNull.Value
                                        ? null : Convert.ToString(reader["Description"]),
                                    DurationMinutes = Convert.ToInt32(reader["DurationMinutes"]),
                                    ReleaseDate = reader["ReleaseDate"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(reader["ReleaseDate"]),
                                    EndDate = reader["EndDate"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(reader["EndDate"]),
                                    PosterUrl = reader["PosterUrl"] == DBNull.Value
                                        ? null : Convert.ToString(reader["PosterUrl"]),
                                    TrailerUrl = reader["TrailerUrl"] == DBNull.Value
                                        ? null : Convert.ToString(reader["TrailerUrl"]),
                                    Status = Convert.ToString(reader["Status"])
                                });
                            }
                        }
                    }
                }

                _allMovies = movies;
                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách phim.\n\n" + ex.Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplySearch()
        {
            string keyword = SearchTextBox.Text == null
                ? ""
                : SearchTextBox.Text.Trim();

            List<MovieItem> filtered = _allMovies
                .Where(m => m.Title != null &&
                    m.Title.IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            MoviesDataGrid.ItemsSource = filtered;
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
                ApplySearch();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMovies();
            ClearForm();
        }

        private void MoviesDataGrid_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            MovieItem movie = MoviesDataGrid.SelectedItem as MovieItem;

            if (movie == null)
                return;

            _isLoading = true;

            try
            {
                TitleTextBox.Text = movie.Title ?? "";
                DescriptionTextBox.Text = movie.Description ?? "";
                DurationTextBox.Text = movie.DurationMinutes.ToString();

                ReleaseDatePicker.SelectedDate = movie.ReleaseDate;
                EndDatePicker.SelectedDate = movie.EndDate;

                PosterUrlTextBox.Text = movie.PosterUrl ?? "";
                UpdatePosterPreview(movie.PosterUrl);
                TrailerUrlTextBox.Text = movie.TrailerUrl ?? "";

                SelectStatus(movie.Status);
                SetMovieGenresSelection(movie.MovieId);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SelectStatus(string status)
        {
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (string.Equals(
                    Convert.ToString(item.Content),
                    status,
                    StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedItem = item;
                    return;
                }
            }

            StatusComboBox.SelectedIndex = -1;
        }

        private bool TryReadForm(out MovieItem movie)
        {
            movie = null;

            string title = TitleTextBox.Text.Trim();
            string durationText = DurationTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ShowValidationError("Vui lòng nhập tên phim.");
                TitleTextBox.Focus();
                return false;
            }

            if (!int.TryParse(durationText, out int duration) ||
                duration <= 0)
            {
                ShowValidationError(
                    "Thời lượng phim phải là số nguyên lớn hơn 0.");
                DurationTextBox.Focus();
                return false;
            }

            string status = (StatusComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(status))
            {
                ShowValidationError("Vui lòng chọn trạng thái phim.");
                StatusComboBox.Focus();
                return false;
            }

            DateTime? releaseDate = ReleaseDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (releaseDate.HasValue &&
                endDate.HasValue &&
                endDate.Value.Date < releaseDate.Value.Date)
            {
                ShowValidationError(
                    "Ngày kết thúc không được trước ngày phát hành.");
                return false;
            }

            movie = new MovieItem
            {
                Title = title,
                Description = EmptyToNull(DescriptionTextBox.Text),
                DurationMinutes = duration,
                ReleaseDate = releaseDate,
                EndDate = endDate,
                PosterUrl = EmptyToNull(PosterUrlTextBox.Text),
                TrailerUrl = EmptyToNull(TrailerUrlTextBox.Text),
                Status = status
            };

            return true;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void AddNullableParameter(
            SqlCommand command,
            string name,
            SqlDbType type,
            string value,
            int size)
        {
            SqlParameter parameter = command.Parameters.Add(name, type, size);
            parameter.Value = string.IsNullOrWhiteSpace(value)
                ? (object)DBNull.Value
                : value.Trim();
        }

        private static void AddDateParameter(
            SqlCommand command,
            string name,
            DateTime? value)
        {
            SqlParameter parameter =
                command.Parameters.Add(name, SqlDbType.Date);

            parameter.Value = value.HasValue
                ? (object)value.Value.Date
                : DBNull.Value;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadForm(out MovieItem movie))
                return;

            try
            {
                int newMovieId = 0;
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    string sql = @"
                        INSERT INTO dbo.Movies
                        (
                            Title, Description, DurationMinutes,
                            ReleaseDate, EndDate, PosterUrl, TrailerUrl,
                            Status, CreatedAtUtc, UpdatedAtUtc
                        )
                        OUTPUT INSERTED.MovieId
                        VALUES
                        (
                            @Title, @Description, @DurationMinutes,
                            @ReleaseDate, @EndDate, @PosterUrl, @TrailerUrl,
                            @Status, SYSUTCDATETIME(), NULL
                        );";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddMovieParameters(command, movie);
                        connection.Open();
                        object idObj = command.ExecuteScalar();
                        if (idObj != null) newMovieId = Convert.ToInt32(idObj);
                    }
                }

                if (newMovieId > 0)
                {
                    _genreService.SetGenresForMovie(newMovieId, GetSelectedGenreIds());
                }

                MessageBox.Show(
                    "Đã thêm phim thành công.",
                    "PHENIKAA CINEMA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadMovies();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("thêm phim", ex);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MovieItem selected = MoviesDataGrid.SelectedItem as MovieItem;

            if (selected == null)
            {
                ShowValidationError("Vui lòng chọn phim cần cập nhật.");
                return;
            }

            if (!TryReadForm(out MovieItem movie))
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    string sql = @"
                        UPDATE dbo.Movies
                        SET Title = @Title,
                            Description = @Description,
                            DurationMinutes = @DurationMinutes,
                            ReleaseDate = @ReleaseDate,
                            EndDate = @EndDate,
                            PosterUrl = @PosterUrl,
                            TrailerUrl = @TrailerUrl,
                            Status = @Status,
                            UpdatedAtUtc = SYSUTCDATETIME()
                        WHERE MovieId = @MovieId;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddMovieParameters(command, movie);
                        command.Parameters.Add(
                            "@MovieId", SqlDbType.Int).Value = selected.MovieId;

                        connection.Open();

                        if (command.ExecuteNonQuery() == 0)
                        {
                            ShowValidationError(
                                "Không tìm thấy phim cần cập nhật.");
                            LoadMovies();
                            return;
                        }
                    }
                }

                _genreService.SetGenresForMovie(selected.MovieId, GetSelectedGenreIds());

                MessageBox.Show(
                    "Đã cập nhật phim thành công.",
                    "PHENIKAA CINEMA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadMovies();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("cập nhật phim", ex);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MovieItem selected = MoviesDataGrid.SelectedItem as MovieItem;

            if (selected == null)
            {
                ShowValidationError("Vui lòng chọn phim cần xóa.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa phim \"" + selected.Title + "\"?\n\n" +
                "Nếu phim đã có suất chiếu hoặc dữ liệu liên quan, database có thể từ chối xóa.",
                "Xác nhận xóa phim",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Xóa các liên kết thể loại trước khi xóa phim
                _genreService.SetGenresForMovie(selected.MovieId, new List<int>());

                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    string sql =
                        "DELETE FROM dbo.Movies WHERE MovieId = @MovieId;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.Add(
                            "@MovieId", SqlDbType.Int).Value = selected.MovieId;

                        connection.Open();

                        if (command.ExecuteNonQuery() == 0)
                        {
                            ShowValidationError(
                                "Phim không còn tồn tại trong database.");
                            LoadMovies();
                            return;
                        }
                    }
                }

                MessageBox.Show(
                    "Đã xóa phim thành công.",
                    "PHENIKAA CINEMA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadMovies();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Không thể xóa phim. Phim có thể đang được tham chiếu " +
                    "bởi suất chiếu hoặc dữ liệu đặt vé.\n\n" + ex.Message,
                    "Không thể xóa phim",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                ShowDatabaseError("xóa phim", ex);
            }
        }

        private void AddMovieParameters(
            SqlCommand command, MovieItem movie)
        {
            command.Parameters.Add(
                "@Title", SqlDbType.NVarChar, 200).Value = movie.Title;

            SqlParameter description =
                command.Parameters.Add("@Description", SqlDbType.NVarChar, -1);
            description.Value = movie.Description == null
                ? (object)DBNull.Value
                : movie.Description;

            command.Parameters.Add(
                "@DurationMinutes", SqlDbType.Int).Value =
                movie.DurationMinutes;

            AddDateParameter(command, "@ReleaseDate", movie.ReleaseDate);
            AddDateParameter(command, "@EndDate", movie.EndDate);

            AddNullableParameter(
                command, "@PosterUrl", SqlDbType.NVarChar,
                movie.PosterUrl, 500);

            AddNullableParameter(
                command, "@TrailerUrl", SqlDbType.NVarChar,
                movie.TrailerUrl, 500);

            command.Parameters.Add(
                "@Status", SqlDbType.NVarChar, 20).Value = movie.Status;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isLoading = true;

            try
            {
                MoviesDataGrid.SelectedItem = null;

                TitleTextBox.Clear();
                DescriptionTextBox.Clear();
                DurationTextBox.Clear();

                ReleaseDatePicker.SelectedDate = null;
                EndDatePicker.SelectedDate = null;

                PosterUrlTextBox.Clear();
                UpdatePosterPreview(null);
                TrailerUrlTextBox.Clear();

                StatusComboBox.SelectedIndex = 0;

                foreach (var item in _genreCheckboxes)
                {
                    item.IsSelected = false;
                }
                GenresItemsControl.ItemsSource = null;
                GenresItemsControl.ItemsSource = _genreCheckboxes;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(
                message,
                "Kiểm tra dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void ShowDatabaseError(string action, Exception ex)
        {
            MessageBox.Show(
                "Không thể " + action + ".\n\n" + ex.Message,
                "Lỗi database",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    
        private void BrowsePosterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Chọn ảnh poster phim",
                    Filter = "Tệp hình ảnh (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|Tất cả tệp (*.*)|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    string selectedFile = dialog.FileName;
                    if (!File.Exists(selectedFile)) return;

                    string postersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Posters");
                    if (!Directory.Exists(postersDir))
                    {
                        Directory.CreateDirectory(postersDir);
                    }

                    string ext = Path.GetExtension(selectedFile);
                    string safeFileName = "poster_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString().Substring(0, 6) + ext;
                    string destPath = Path.Combine(postersDir, safeFileName);

                    File.Copy(selectedFile, destPath, true);

                    string relativePath = Path.Combine("Images", "Posters", safeFileName);
                    PosterUrlTextBox.Text = relativePath;
                    UpdatePosterPreview(destPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message, "PHENIKAA CINEMA", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PosterUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePosterPreview(PosterUrlTextBox.Text);
        }

        private void UpdatePosterPreview(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                PosterPreviewImage.Source = null;
                PosterPlaceholderPanel.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    bitmap.UriSource = new Uri(pathOrUrl, UriKind.Absolute);
                }
                else
                {
                    string fullPath = Path.IsPathRooted(pathOrUrl)
                        ? pathOrUrl
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathOrUrl.TrimStart(new char[] { '\\', '/' }));

                    if (!File.Exists(fullPath))
                    {
                        PosterPreviewImage.Source = null;
                        PosterPlaceholderPanel.Visibility = Visibility.Visible;
                        return;
                    }
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                }

                bitmap.EndInit();
                PosterPreviewImage.Source = bitmap;
                PosterPlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            catch
            {
                PosterPreviewImage.Source = null;
                PosterPlaceholderPanel.Visibility = Visibility.Visible;
            }
        }
}
}

