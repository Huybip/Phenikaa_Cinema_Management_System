using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace PhenikaaCinema.Views.Admin
{
    public partial class BookingManagementWindow : Window
    {
        private readonly string _connectionString;
        private DataTable _bookingsTable;

        public BookingManagementWindow()
        {
            InitializeComponent();

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["CinemaDb"];

            _connectionString = settings != null
                ? settings.ConnectionString
                : null;

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                MessageBox.Show(
                    "Không tìm thấy chuỗi kết nối CinemaDb trong App.config.",
                    "Lỗi cấu hình",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Loaded += BookingManagementWindow_Loaded;
        }

        private void BookingManagementWindow_Loaded(
            object sender, RoutedEventArgs e)
        {
            LoadBookings();
        }

        // Tải danh sách đặt vé từ database
        private void LoadBookings()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            b.BookingId,
                            b.BookingCode,
                            COALESCE(u.FullName, N'Khách vãng lai')
                                AS CustomerName,
                            COALESCE(m.Title, N'Chưa xác định')
                                AS MovieTitle,
                            b.BookingTimeUtc,
                            CONVERT(VARCHAR(16),
                                DATEADD(HOUR, 7, b.BookingTimeUtc),
                                120) AS BookingTimeLocal,
                            b.Status,
                            COALESCE(b.FinalAmount, b.TotalAmount, 0)
                                AS FinalAmount
                        FROM dbo.Bookings AS b
                        LEFT JOIN dbo.Users AS u
                            ON b.CustomerUserId = u.UserId
                        LEFT JOIN dbo.Showtimes AS s
                            ON b.ShowtimeId = s.ShowtimeId
                        LEFT JOIN dbo.Movies AS m
                            ON s.MovieId = m.MovieId
                        ORDER BY b.BookingTimeUtc DESC;";

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        _bookingsTable = table;
                    }
                }

                BookingsGrid.ItemsSource = _bookingsTable.DefaultView;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách đặt vé.\n\nChi tiết: "
                    + ex.Message,
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Tìm kiếm theo mã đặt vé, ID hoặc tên khách hàng
        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Lọc theo trạng thái
        private void StatusComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_bookingsTable == null)
                return;

            string searchText = SearchTextBox.Text.Trim();

            // Escape ký tự đặc biệt của DataView.RowFilter
            searchText = searchText
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            string filter = "";

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filter = string.Format(
                    CultureInfo.InvariantCulture,
                    "(Convert(BookingId, 'System.String') LIKE '%{0}%' " +
                    "OR BookingCode LIKE '%{0}%' " +
                    "OR CustomerName LIKE '%{0}%' " +
                    "OR MovieTitle LIKE '%{0}%')",
                    searchText);
            }

            ComboBoxItem selectedItem =
                StatusComboBox.SelectedItem as ComboBoxItem;

            if (selectedItem != null)
            {
                string status = selectedItem.Content != null
                    ? selectedItem.Content.ToString()
                    : "";

                if (!string.IsNullOrWhiteSpace(status) &&
                    status != "Tất cả trạng thái")
                {
                    string statusFilter =
                        "Status = '" + status.Replace("'", "''") + "'";

                    filter = string.IsNullOrWhiteSpace(filter)
                        ? statusFilter
                        : filter + " AND " + statusFilter;
                }
            }

            try
            {
                _bookingsTable.DefaultView.RowFilter = filter;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể lọc dữ liệu: " + ex.Message,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Làm mới danh sách
        private void RefreshButton_Click(
            object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            StatusComboBox.SelectedIndex = 0;
            LoadBookings();
        }

        // Xem thông tin thanh toán của đơn được chọn
        private void PaymentButton_Click(
            object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow =
                BookingsGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn một đơn đặt vé.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            int bookingId = Convert.ToInt32(
                selectedRow["BookingId"]);

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            PaymentId,
                            BookingId,
                            PaymentMethod,
                            Amount,
                            Status,
                            PaidAtUtc,
                            TransactionCode
                        FROM dbo.Payments
                        WHERE BookingId = @BookingId
                        ORDER BY PaymentId DESC;";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@BookingId", SqlDbType.Int).Value = bookingId;

                        using (SqlDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                MessageBox.Show(
                                    "Đơn đặt vé này chưa có thông tin thanh toán.",
                                    "Thông tin thanh toán",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                return;
                            }

                            StringBuilder details = new StringBuilder();

                            while (reader.Read())
                            {
                                details.AppendLine(
                                    "Mã thanh toán: " +
                                    reader["PaymentId"]);

                                details.AppendLine(
                                    "Mã đặt vé: " +
                                    reader["BookingId"]);

                                details.AppendLine(
                                    "Phương thức: " +
                                    reader["PaymentMethod"]);

                                details.AppendLine(
                                    "Số tiền: " +
                                    Convert.ToDecimal(reader["Amount"])
                                        .ToString("N0") + " đ");

                                details.AppendLine(
                                    "Trạng thái: " +
                                    reader["Status"]);

                                if (reader["PaidAtUtc"] != DBNull.Value)
                                {
                                    DateTime paidAtUtc =
                                        DateTime.SpecifyKind(
                                            Convert.ToDateTime(
                                                reader["PaidAtUtc"]),
                                            DateTimeKind.Utc);

                                    details.AppendLine(
                                        "Thời gian thanh toán (VN): " +
                                        paidAtUtc.AddHours(7)
                                            .ToString("dd/MM/yyyy HH:mm"));
                                }
                                else
                                {
                                    details.AppendLine(
                                        "Thời gian thanh toán: Chưa thanh toán");
                                }

                                if (reader["TransactionCode"] != DBNull.Value)
                                {
                                    details.AppendLine(
                                        "Mã giao dịch: " +
                                        reader["TransactionCode"]);
                                }

                                details.AppendLine(
                                    "------------------------------");
                            }

                            MessageBox.Show(
                                details.ToString(),
                                "Thông tin thanh toán",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể lấy thông tin thanh toán.\n\nChi tiết: "
                    + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Hủy đơn đang chờ thanh toán
        private void CancelButton_Click(
            object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow =
                BookingsGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn đơn đặt vé cần hủy.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            int bookingId = Convert.ToInt32(
                selectedRow["BookingId"]);

            string status = Convert.ToString(
                selectedRow["Status"]);

            if (status != "PendingPayment")
            {
                MessageBox.Show(
                    "Chỉ có thể hủy đơn đang chờ thanh toán. " +
                    "Trạng thái hiện tại: " + status,
                    "Không thể hủy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn hủy đơn đặt vé " +
                selectedRow["BookingCode"] + "?",
                "Xác nhận hủy đơn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(
                        "dbo.sp_CancelPendingBooking", connection))
                    {
                        command.CommandType =
                            CommandType.StoredProcedure;

                        command.Parameters.Add(
                            "@BookingId", SqlDbType.Int).Value = bookingId;

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Đã thực hiện yêu cầu hủy đơn đặt vé.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể hủy đơn đặt vé.\n\nChi tiết: "
                    + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
