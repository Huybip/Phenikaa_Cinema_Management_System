using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PhenikaaCinema.Views.Admin
{
    public partial class ShowtimeManagementWindow : Window
    {
        private readonly string _connectionString;
        private bool _loadingSelection;
        private List<ShowtimeItem> _allShowtimes = new List<ShowtimeItem>();
        private List<MovieOption> _movies = new List<MovieOption>();
        private List<RoomOption> _rooms = new List<RoomOption>();

        private static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public ShowtimeManagementWindow()
        {
            InitializeComponent();
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["CinemaDb"];
            if (setting == null)
            {
                MessageBox.Show("Không tìm thấy connection string CinemaDb trong App.config.",
                    "Lỗi cấu hình", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _connectionString = setting.ConnectionString;
            ShowDatePicker.SelectedDate = DateTime.Today;
            try
            {
                LoadMovies();
                LoadRooms();
                LoadShowtimes();
                UpdateEndTime();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải dữ liệu suất chiếu.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMovies()
        {
            _movies = new List<MovieOption>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT MovieId, Title, DurationMinutes
                  FROM dbo.Movies
                  WHERE DurationMinutes > 0
                  ORDER BY Title;", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _movies.Add(new MovieOption {
                            MovieId = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            DurationMinutes = reader.GetInt32(2)
                        });
                    }
                }
            }
            MovieComboBox.ItemsSource = _movies;
        }

        private void LoadRooms()
        {
            _rooms = new List<RoomOption>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT RoomId, RoomName, Capacity
                  FROM dbo.Rooms
                  WHERE IsActive = 1
                  ORDER BY RoomName;", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        _rooms.Add(new RoomOption {
                            RoomId = reader.GetInt32(0),
                            RoomName = reader.GetString(1),
                            Capacity = reader.GetInt32(2)
                        });
                }
            }
            RoomComboBox.ItemsSource = _rooms;
        }

        private void LoadShowtimes()
        {
            _allShowtimes = new List<ShowtimeItem>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT s.ShowtimeId, s.MovieId, m.Title, m.DurationMinutes,
                         s.RoomId, r.RoomName, s.StartTimeUtc, s.EndTimeUtc,
                         s.BasePrice, s.Status, r.IsActive
                  FROM dbo.Showtimes s
                  INNER JOIN dbo.Movies m ON s.MovieId = m.MovieId
                  INNER JOIN dbo.Rooms r ON s.RoomId = r.RoomId
                  ORDER BY s.StartTimeUtc DESC;", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime startUtc = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
                        DateTime endUtc = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc);
                        _allShowtimes.Add(new ShowtimeItem {
                            ShowtimeId = reader.GetInt32(0),
                            MovieId = reader.GetInt32(1),
                            MovieTitle = reader.GetString(2),
                            DurationMinutes = reader.GetInt32(3),
                            RoomId = reader.GetInt32(4),
                            RoomName = reader.GetString(5),
                            StartTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, VietnamTimeZone),
                            EndTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(endUtc, VietnamTimeZone),
                            BasePrice = reader.GetDecimal(8),
                            Status = reader.GetString(9),
                            RoomIsActive = reader.GetBoolean(10)
                        });
                    }
                }
            }
            ApplySearch();
        }

        private DateTime ConvertLocalToUtc(DateTime localTime)
        {
            DateTime unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, VietnamTimeZone);
        }

        private bool TryReadForm(out int movieId, out int roomId,
            out DateTime startUtc, out DateTime endUtc, out decimal basePrice, out string status)
        {
            movieId = 0; roomId = 0; startUtc = DateTime.MinValue; endUtc = DateTime.MinValue;
            basePrice = 0; status = "Scheduled";

            if (MovieComboBox.SelectedValue == null || RoomComboBox.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn phim và phòng chiếu.");
                return false;
            }
            if (!ShowDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Vui lòng chọn ngày chiếu.");
                return false;
            }

            TimeSpan startTime;
            if (!TimeSpan.TryParseExact(StartTimeTextBox.Text.Trim(), @"hh\:mm",
                CultureInfo.InvariantCulture, out startTime) || startTime < TimeSpan.Zero ||
                startTime >= TimeSpan.FromDays(1))
            {
                MessageBox.Show("Giờ bắt đầu phải theo định dạng HH:mm, ví dụ 19:00.");
                StartTimeTextBox.Focus();
                return false;
            }

            MovieOption selectedMovie = MovieComboBox.SelectedItem as MovieOption;
            if (selectedMovie == null || selectedMovie.DurationMinutes <= 0)
            {
                MessageBox.Show("Phim được chọn không có thời lượng hợp lệ.");
                return false;
            }

            DateTime startLocal = ShowDatePicker.SelectedDate.Value.Date.Add(startTime);
            DateTime endLocal = startLocal.AddMinutes(selectedMovie.DurationMinutes);
            if (endLocal.Date != startLocal.Date)
            {
                MessageBox.Show("Suất chiếu kết thúc sang ngày hôm sau. Vui lòng chọn giờ bắt đầu sớm hơn.");
                return false;
            }

            if (!decimal.TryParse(BasePriceTextBox.Text.Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out basePrice) || basePrice <= 0)
            {
                MessageBox.Show("Giá vé phải là số lớn hơn 0, ví dụ 80000.");
                BasePriceTextBox.Focus();
                return false;
            }

            movieId = (int)MovieComboBox.SelectedValue;
            roomId = (int)RoomComboBox.SelectedValue;
            startUtc = ConvertLocalToUtc(startLocal);
            endUtc = ConvertLocalToUtc(endLocal);
            ComboBoxItem statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            if (statusItem != null && statusItem.Content != null)
                status = statusItem.Content.ToString();
            return true;
        }

        private bool HasRoomConflict(int roomId, DateTime startUtc, DateTime endUtc, int? excludeId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM dbo.Showtimes
                  WHERE RoomId = @RoomId
                    AND StartTimeUtc < @EndTimeUtc
                    AND EndTimeUtc > @StartTimeUtc
                    AND Status = N'Scheduled'
                    AND (@ExcludeId IS NULL OR ShowtimeId <> @ExcludeId);", connection))
            {
                command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
                command.Parameters.Add("@StartTimeUtc", SqlDbType.DateTime2).Value = startUtc;
                command.Parameters.Add("@EndTimeUtc", SqlDbType.DateTime2).Value = endUtc;
                command.Parameters.Add("@ExcludeId", SqlDbType.Int).Value =
                    excludeId.HasValue ? (object)excludeId.Value : DBNull.Value;
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool HasBookings(int showtimeId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.BookingSeats bs
                    WHERE bs.ShowtimeId = @ShowtimeId
                  ) THEN 1 ELSE 0 END;", connection))
            {
                command.Parameters.Add("@ShowtimeId", SqlDbType.Int).Value = showtimeId;
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private bool IsRoomActive(int roomId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT IsActive FROM dbo.Rooms WHERE RoomId = @RoomId;", connection))
            {
                command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
                connection.Open();
                object value = command.ExecuteScalar();
                return value != null && value != DBNull.Value && Convert.ToBoolean(value);
            }
        }

        private void AddParameters(SqlCommand command, int movieId, int roomId,
            DateTime startUtc, DateTime endUtc, decimal price, string status)
        {
            command.Parameters.Add("@MovieId", SqlDbType.Int).Value = movieId;
            command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
            command.Parameters.Add("@StartTimeUtc", SqlDbType.DateTime2).Value = startUtc;
            command.Parameters.Add("@EndTimeUtc", SqlDbType.DateTime2).Value = endUtc;
            SqlParameter priceParameter = command.Parameters.Add("@BasePrice", SqlDbType.Decimal);
            priceParameter.Precision = 18;
            priceParameter.Scale = 2;
            priceParameter.Value = price;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            int movieId, roomId; DateTime startUtc, endUtc; decimal price; string status;
            if (!TryReadForm(out movieId, out roomId, out startUtc, out endUtc, out price, out status))
                return;
            try
            {
                if (!IsRoomActive(roomId))
                {
                    MessageBox.Show("Phòng đã ngừng hoạt động. Vui lòng chọn phòng khác.");
                    return;
                }
                if (HasRoomConflict(roomId, startUtc, endUtc, null))
                {
                    MessageBox.Show("Phòng này đã có suất chiếu bị trùng thời gian.",
                        "Trùng lịch", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.Showtimes
                      (MovieId, RoomId, StartTimeUtc, EndTimeUtc, BasePrice, Status, CreatedAtUtc)
                      VALUES (@MovieId, @RoomId, @StartTimeUtc, @EndTimeUtc, @BasePrice, @Status, SYSUTCDATETIME());",
                    connection))
                {
                    AddParameters(command, movieId, roomId, startUtc, endUtc, price, status);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                MessageBox.Show("Thêm suất chiếu thành công.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadShowtimes(); ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm suất chiếu.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            int showtimeId;
            if (!int.TryParse(ShowtimeIdTextBox.Text, out showtimeId))
            {
                MessageBox.Show("Vui lòng chọn suất chiếu cần cập nhật.");
                return;
            }
            int movieId, roomId; DateTime startUtc, endUtc; decimal price; string status;
            if (!TryReadForm(out movieId, out roomId, out startUtc, out endUtc, out price, out status))
                return;
            try
            {
                if (HasBookings(showtimeId))
                {
                    MessageBox.Show("Suất chiếu này đã có ghế/đơn đặt vé liên quan. Không thể thay đổi phim, phòng hoặc thời gian. Nếu cần ngừng bán, hãy dùng 'Hủy suất' sau khi xử lý các đơn đặt vé.",
                        "Bảo vệ dữ liệu đặt vé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!IsRoomActive(roomId))
                {
                    MessageBox.Show("Phòng đã ngừng hoạt động. Vui lòng chọn phòng đang hoạt động.");
                    return;
                }
                if (HasRoomConflict(roomId, startUtc, endUtc, showtimeId))
                {
                    MessageBox.Show("Phòng này đã có suất chiếu bị trùng thời gian.",
                        "Trùng lịch", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Showtimes
                      SET MovieId=@MovieId, RoomId=@RoomId, StartTimeUtc=@StartTimeUtc,
                          EndTimeUtc=@EndTimeUtc, BasePrice=@BasePrice, Status=@Status
                      WHERE ShowtimeId=@ShowtimeId;", connection))
                {
                    AddParameters(command, movieId, roomId, startUtc, endUtc, price, status);
                    command.Parameters.Add("@ShowtimeId", SqlDbType.Int).Value = showtimeId;
                    connection.Open();
                    if (command.ExecuteNonQuery() == 0)
                    {
                        MessageBox.Show("Không tìm thấy suất chiếu cần cập nhật.");
                        return;
                    }
                }
                MessageBox.Show("Cập nhật suất chiếu thành công.");
                LoadShowtimes(); ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật suất chiếu.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            int showtimeId;
            if (!int.TryParse(ShowtimeIdTextBox.Text, out showtimeId))
            {
                MessageBox.Show("Vui lòng chọn suất chiếu cần hủy.");
                return;
            }
            if (HasBookings(showtimeId))
            {
                MessageBox.Show("Suất chiếu đã có ghế/đơn đặt vé liên quan. Để bảo vệ dữ liệu, ứng dụng không tự hủy suất này. Hãy xử lý các đơn đặt vé trước.",
                    "Không thể hủy tự động", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Bạn có chắc chắn muốn hủy suất chiếu này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Showtimes SET Status=N'Cancelled' WHERE ShowtimeId=@Id;", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = showtimeId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                MessageBox.Show("Đã hủy suất chiếu.");
                LoadShowtimes(); ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể hủy suất chiếu.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Để tránh mất dữ liệu lịch sử, hãy dùng chức năng Hủy suất thay vì xóa cứng suất chiếu.",
                "Khuyến nghị", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowtimesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingSelection) return;
            ShowtimeItem item = ShowtimesDataGrid.SelectedItem as ShowtimeItem;
            if (item == null) return;
            _loadingSelection = true;
            try
            {
                ShowtimeIdTextBox.Text = item.ShowtimeId.ToString(CultureInfo.InvariantCulture);
                MovieComboBox.SelectedValue = item.MovieId;
                RoomComboBox.SelectedValue = item.RoomId;
                ShowDatePicker.SelectedDate = item.StartTimeLocal.Date;
                StartTimeTextBox.Text = item.StartTimeLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
                BasePriceTextBox.Text = item.BasePrice.ToString("0.##", CultureInfo.InvariantCulture);
                SelectStatus(item.Status);
                UpdateDurationDisplay();
                UpdateEndTime();
            }
            finally { _loadingSelection = false; }
        }

        private void SelectStatus(string status)
        {
            for (int i = 0; i < StatusComboBox.Items.Count; i++)
            {
                ComboBoxItem item = StatusComboBox.Items[i] as ComboBoxItem;
                if (item != null && string.Equals(Convert.ToString(item.Content), status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedIndex = i;
                    return;
                }
            }
            StatusComboBox.SelectedIndex = 0;
        }

        private void MovieComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingSelection) return;
            UpdateDurationDisplay();
            UpdateEndTime();
        }

        private void ScheduleInput_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_loadingSelection) UpdateEndTime();
        }

        private void ScheduleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized && !_loadingSelection) UpdateEndTime();
        }

        private void UpdateDurationDisplay()
        {
            MovieOption movie = MovieComboBox.SelectedItem as MovieOption;
            DurationTextBox.Text = movie == null ? "" : movie.DurationMinutes + " phút";
        }

        private void UpdateEndTime()
        {
            if (EndTimeTextBox == null || StartTimeTextBox == null || ShowDatePicker == null)
                return;
            MovieOption movie = MovieComboBox.SelectedItem as MovieOption;
            TimeSpan start;
            if (movie == null || !ShowDatePicker.SelectedDate.HasValue ||
                !TimeSpan.TryParseExact(StartTimeTextBox.Text.Trim(), @"hh\:mm",
                    CultureInfo.InvariantCulture, out start) || start < TimeSpan.Zero ||
                start >= TimeSpan.FromDays(1))
            {
                EndTimeTextBox.Text = "";
                return;
            }
            DateTime end = ShowDatePicker.SelectedDate.Value.Date.Add(start).AddMinutes(movie.DurationMinutes);
            EndTimeTextBox.Text = end.ToString("HH:mm", CultureInfo.InvariantCulture) +
                (end.Date != ShowDatePicker.SelectedDate.Value.Date ? " (ngày hôm sau)" : "");
        }

        private void ClearForm()
        {
            _loadingSelection = true;
            try
            {
                ShowtimeIdTextBox.Clear();
                ShowtimesDataGrid.SelectedItem = null;
                MovieComboBox.SelectedIndex = -1;
                RoomComboBox.SelectedIndex = -1;
                ShowDatePicker.SelectedDate = DateTime.Today;
                StartTimeTextBox.Text = "19:00";
                BasePriceTextBox.Text = "80000";
                StatusComboBox.SelectedIndex = 0;
                UpdateDurationDisplay();
                UpdateEndTime();
            }
            finally { _loadingSelection = false; }
        }

        private void ApplySearch()
        {
            string keyword = SearchTextBox.Text == null ? "" : SearchTextBox.Text.Trim();
            IEnumerable<ShowtimeItem> query = _allShowtimes;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(item =>
                    item.MovieTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.RoomName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Status.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.ShowtimeId.ToString(CultureInfo.InvariantCulture).Contains(keyword));
            }
            ShowtimesDataGrid.ItemsSource = query.ToList();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) { ApplySearch(); }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized) ApplySearch();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchTextBox.Clear();
                LoadMovies(); LoadRooms(); LoadShowtimes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể làm mới dữ liệu.\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) { ClearForm(); }
        private void CloseButton_Click(object sender, RoutedEventArgs e) { Close(); }
    }

    public class MovieOption
    {
        public int MovieId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class RoomOption
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
    }

    public class ShowtimeItem
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public int DurationMinutes { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public DateTime StartTimeLocal { get; set; }
        public DateTime EndTimeLocal { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; }
        public bool RoomIsActive { get; set; }
    }
}
