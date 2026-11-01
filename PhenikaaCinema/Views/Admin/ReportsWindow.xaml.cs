using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PhenikaaCinema.Views.Admin
{
    public partial class ReportsWindow : Window
    {
        private readonly string _connectionString;
        private DataTable _reportTable;

        private readonly Brush _orangeBrush =
            new SolidColorBrush(Color.FromRgb(245, 130, 32));

        private readonly Brush _blueBrush =
            new SolidColorBrush(Color.FromRgb(36, 113, 163));

        private readonly Brush _grayBrush =
            new SolidColorBrush(Color.FromRgb(113, 128, 150));

        private readonly Brush _lightBrush =
            new SolidColorBrush(Color.FromRgb(224, 229, 234));

        public ReportsWindow()
        {
            InitializeComponent();

            _connectionString =
                ConfigurationManager.ConnectionStrings["CinemaDb"] == null
                    ? null
                    : ConfigurationManager.ConnectionStrings["CinemaDb"].ConnectionString;

            Loaded += ReportsWindow_Loaded;
            SizeChanged += ReportsWindow_SizeChanged;
        }

        private void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                MessageBox.Show(
                    "Không tìm thấy chuỗi kết nối CinemaDb trong App.config.",
                    "Lỗi cấu hình",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;

            LoadReport();
        }

        private void ReportsWindow_SizeChanged(
            object sender, SizeChangedEventArgs e)
        {
            DrawRevenueChart();
            DrawBookingStatusChart();
        }

        private bool TryGetDateRange(
            out DateTime startUtc, out DateTime endUtc)
        {
            startUtc = DateTime.MinValue;
            endUtc = DateTime.MinValue;

            if (!StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Vui lòng chọn ngày bắt đầu và ngày kết thúc.",
                    "Thiếu thông tin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            DateTime startLocal =
                StartDatePicker.SelectedDate.Value.Date;

            DateTime endLocal =
                EndDatePicker.SelectedDate.Value.Date;

            if (endLocal < startLocal)
            {
                MessageBox.Show(
                    "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.",
                    "Khoảng thời gian không hợp lệ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // Thời gian trên giao diện là giờ Việt Nam UTC+7.
            startUtc = DateTime.SpecifyKind(
                startLocal.AddHours(-7),
                DateTimeKind.Utc);

            // Khoảng lọc [startUtc, endUtc).
            endUtc = DateTime.SpecifyKind(
                endLocal.AddDays(1).AddHours(-7),
                DateTimeKind.Utc);

            return true;
        }

        private void LoadReportButton_Click(
        object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nút Xem báo cáo đã được nhấn.");
            LoadReport();
        }

        private void LoadReport()
        {
            DateTime startUtc;
            DateTime endUtc;

            if (!TryGetDateRange(out startUtc, out endUtc))
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Doanh thu theo thời điểm thanh toán thành công.
                    string revenueQuery = @"
                        SELECT
                            ISNULL(SUM(p.Amount), 0) AS TotalRevenue
                        FROM dbo.Payments p
                        WHERE p.Status = N'Success'
                          AND p.PaidAtUtc >= @StartUtc
                          AND p.PaidAtUtc < @EndUtc;";

                    decimal totalRevenue = 0;

                    using (SqlCommand command =
                        new SqlCommand(revenueQuery, connection))
                    {
                        AddDateParameters(command, startUtc, endUtc);

                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            totalRevenue = Convert.ToDecimal(result);
                    }

                    // 2. Đếm đơn đã xác nhận theo thời điểm đặt vé.
                    // Bảng Bookings sử dụng BookingTimeUtc.
                    string bookingsQuery = @"
                        SELECT COUNT(*)
                        FROM dbo.Bookings b
                        WHERE b.Status = N'Confirmed'
                          AND b.BookingTimeUtc >= @StartUtc
                          AND b.BookingTimeUtc < @EndUtc;";

                    int totalBookings;

                    using (SqlCommand command =
                        new SqlCommand(bookingsQuery, connection))
                    {
                        AddDateParameters(command, startUtc, endUtc);
                        totalBookings =
                            Convert.ToInt32(command.ExecuteScalar());
                    }

                    // 3. Đếm vé phát hành trong khoảng ngày đã chọn.
                    string ticketsQuery = @"
                        SELECT COUNT(*)
                        FROM dbo.Tickets t
                        WHERE t.IssuedAtUtc >= @StartUtc
                          AND t.IssuedAtUtc < @EndUtc;";

                    int totalTickets;

                    using (SqlCommand command =
                        new SqlCommand(ticketsQuery, connection))
                    {
                        AddDateParameters(command, startUtc, endUtc);
                        totalTickets =
                            Convert.ToInt32(command.ExecuteScalar());
                    }

                    // 4. Dữ liệu biểu đồ doanh thu theo ngày.
                    string dailyRevenueQuery = @"
                        SELECT
                            CONVERT(date, DATEADD(HOUR, 7, p.PaidAtUtc))
                                AS RevenueDate,
                            SUM(p.Amount) AS DailyRevenue
                        FROM dbo.Payments p
                        WHERE p.Status = N'Success'
                          AND p.PaidAtUtc >= @StartUtc
                          AND p.PaidAtUtc < @EndUtc
                        GROUP BY CONVERT(
                            date, DATEADD(HOUR, 7, p.PaidAtUtc))
                        ORDER BY RevenueDate;";

                    DataTable dailyRevenueTable = new DataTable();

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(dailyRevenueQuery, connection))
                    {
                        AddDateParameters(
                            adapter.SelectCommand, startUtc, endUtc);

                        adapter.Fill(dailyRevenueTable);
                    }

                    // 5. Dữ liệu biểu đồ trạng thái đặt vé.
                    string statusQuery = @"
                        SELECT
                            b.Status,
                            COUNT(*) AS BookingCount
                        FROM dbo.Bookings b
                        WHERE b.BookingTimeUtc >= @StartUtc
                          AND b.BookingTimeUtc < @EndUtc
                        GROUP BY b.Status
                        ORDER BY b.Status;";

                    DataTable statusTable = new DataTable();

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(statusQuery, connection))
                    {
                        AddDateParameters(
                            adapter.SelectCommand, startUtc, endUtc);

                        adapter.Fill(statusTable);
                    }

                    // 6. Chi tiết các khoản thanh toán.
                    string detailQuery = @"
                        SELECT
                            p.PaymentId AS [Mã thanh toán],
                            p.BookingId AS [Mã đặt vé],
                            p.PaymentMethod AS [Phương thức],
                            p.Amount AS [Số tiền],
                            p.Status AS [Trạng thái thanh toán],
                            DATEADD(HOUR, 7, p.PaidAtUtc)
                                AS [Thời gian thanh toán (VN)]
                        FROM dbo.Payments p
                        WHERE p.PaidAtUtc >= @StartUtc
                          AND p.PaidAtUtc < @EndUtc
                        ORDER BY p.PaidAtUtc DESC;";

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(detailQuery, connection))
                    {
                        AddDateParameters(
                            adapter.SelectCommand, startUtc, endUtc);

                        _reportTable = new DataTable();
                        adapter.Fill(_reportTable);
                    }

                    RevenueTextBlock.Text =
                        totalRevenue.ToString("N0") + " đ";

                    BookingsTextBlock.Text =
                        totalBookings.ToString("N0");

                    TicketsTextBlock.Text =
                        totalTickets.ToString("N0");

                    ReportGrid.ItemsSource = _reportTable.DefaultView;

                    DrawRevenueChart(dailyRevenueTable);
                    DrawBookingStatusChart(statusTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải báo cáo thống kê.\n\n" + ex.Message,
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddDateParameters(
            SqlCommand command,
            DateTime startUtc,
            DateTime endUtc)
        {
            command.Parameters.Add(
                "@StartUtc", SqlDbType.DateTime2).Value = startUtc;

            command.Parameters.Add(
                "@EndUtc", SqlDbType.DateTime2).Value = endUtc;
        }

        // =========================================================
        // BIỂU ĐỒ 1: DOANH THU THEO NGÀY
        // =========================================================

        private void DrawRevenueChart()
        {
            // Dùng khi cửa sổ thay đổi kích thước.
            // Biểu đồ được vẽ lại sau lần tải báo cáo tiếp theo.
        }

        private void DrawRevenueChart(DataTable table)
        {
            RevenueChartCanvas.Children.Clear();

            if (table == null || table.Rows.Count == 0)
            {
                AddCanvasText(
                    RevenueChartCanvas,
                    "Chưa có doanh thu trong khoảng thời gian này.",
                    15, 100, 13, _grayBrush);

                return;
            }

            int count = table.Rows.Count;

            double chartHeight = 180;
            double top = 12;
            double baseY = 205;
            double barWidth = Math.Max(
                12, Math.Min(32, 560.0 / count - 8));

            double gap = Math.Max(
                6, (560.0 - count * barWidth) / (count + 1));

            decimal maxRevenue = 0;

            foreach (DataRow row in table.Rows)
            {
                decimal value = Convert.ToDecimal(row["DailyRevenue"]);

                if (value > maxRevenue)
                    maxRevenue = value;
            }

            if (maxRevenue <= 0)
                maxRevenue = 1;

            AddLine(
                RevenueChartCanvas,
                30, baseY, 630, baseY, _lightBrush, 1);

            for (int i = 0; i < count; i++)
            {
                DataRow row = table.Rows[i];

                DateTime date =
                    Convert.ToDateTime(row["RevenueDate"]);

                decimal revenue =
                    Convert.ToDecimal(row["DailyRevenue"]);

                double barHeight =
                    (double)(revenue / maxRevenue) * chartHeight;

                if (barHeight < 2)
                    barHeight = 2;

                double x = 38 + i * (barWidth + gap);
                double y = baseY - barHeight;

                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = _orangeBrush,
                    RadiusX = 3,
                    RadiusY = 3,
                    ToolTip = date.ToString("dd/MM/yyyy") +
                              "\nDoanh thu: " +
                              revenue.ToString("N0") + " đ"
                };

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                RevenueChartCanvas.Children.Add(bar);

                // Hiển thị giá trị trên cột nếu đủ chỗ.
                if (count <= 12)
                {
                    AddCanvasText(
                        RevenueChartCanvas,
                        FormatShortMoney(revenue),
                        x - 12,
                        Math.Max(top, y - 19),
                        10,
                        _grayBrush);
                }

                AddCanvasText(
                    RevenueChartCanvas,
                    date.ToString("dd/MM"),
                    x - 8,
                    baseY + 8,
                    10,
                    _grayBrush);
            }
        }

        private string FormatShortMoney(decimal amount)
        {
            if (amount >= 1000000000m)
                return (amount / 1000000000m).ToString("0.#") + "B";

            if (amount >= 1000000m)
                return (amount / 1000000m).ToString("0.#") + "M";

            if (amount >= 1000m)
                return (amount / 1000m).ToString("0.#") + "K";

            return amount.ToString("0");
        }

        // =========================================================
        // BIỂU ĐỒ 2: TRẠNG THÁI ĐẶT VÉ
        // =========================================================

        private void DrawBookingStatusChart()
        {
            // Được vẽ lại khi LoadReport lấy dữ liệu mới.
        }

        private void DrawBookingStatusChart(DataTable table)
        {
            BookingStatusChartCanvas.Children.Clear();

            string[] statuses =
            {
                "Confirmed",
                "PendingPayment",
                "Cancelled",
                "Expired"
            };

            string[] labels =
            {
                "Đã xác nhận",
                "Chờ thanh toán",
                "Đã hủy",
                "Đã hết hạn"
            };

            Brush[] brushes =
            {
                new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                new SolidColorBrush(Color.FromRgb(149, 165, 166))
            };

            Dictionary<string, int> counts =
                new Dictionary<string, int>();

            foreach (string status in statuses)
                counts[status] = 0;

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    string status =
                        Convert.ToString(row["Status"]);

                    int count =
                        Convert.ToInt32(row["BookingCount"]);

                    if (counts.ContainsKey(status))
                        counts[status] = count;
                }
            }

            int maxCount = counts.Values.Max();

            if (maxCount == 0)
                maxCount = 1;

            double labelX = 5;
            double barX = 125;
            double maxBarWidth = 175;
            double rowHeight = 53;
            double startY = 15;

            for (int i = 0; i < statuses.Length; i++)
            {
                double y = startY + i * rowHeight;

                AddCanvasText(
                    BookingStatusChartCanvas,
                    labels[i],
                    labelX,
                    y + 4,
                    11,
                    _grayBrush);

                Rectangle backgroundBar = new Rectangle
                {
                    Width = maxBarWidth,
                    Height = 18,
                    Fill = new SolidColorBrush(
                        Color.FromRgb(240, 243, 246)),
                    RadiusX = 4,
                    RadiusY = 4
                };

                Canvas.SetLeft(backgroundBar, barX);
                Canvas.SetTop(backgroundBar, y);
                BookingStatusChartCanvas.Children.Add(backgroundBar);

                int count = counts[statuses[i]];

                double barWidth =
                    maxBarWidth * count / maxCount;

                if (count > 0 && barWidth < 4)
                    barWidth = 4;

                Rectangle bar = new Rectangle
                {
                    Width = barWidth,
                    Height = 18,
                    Fill = brushes[i],
                    RadiusX = 4,
                    RadiusY = 4,
                    ToolTip = labels[i] + ": " + count + " đơn"
                };

                Canvas.SetLeft(bar, barX);
                Canvas.SetTop(bar, y);
                BookingStatusChartCanvas.Children.Add(bar);

                AddCanvasText(
                    BookingStatusChartCanvas,
                    count.ToString(),
                    barX + maxBarWidth + 8,
                    y + 1,
                    12,
                    _grayBrush);
            }
        }

        // =========================================================
        // HÀM VẼ ĐỒ HỌA DÙNG CHUNG
        // =========================================================

        private void AddCanvasText(
            Canvas canvas,
            string text,
            double x,
            double y,
            double fontSize,
            Brush foreground)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);

            canvas.Children.Add(textBlock);
        }

        private void AddLine(
            Canvas canvas,
            double x1,
            double y1,
            double x2,
            double y2,
            Brush brush,
            double thickness)
        {
            Line line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = brush,
                StrokeThickness = thickness
            };

            canvas.Children.Add(line);
        }

        // =========================================================
        // XUẤT CSV
        // =========================================================

        private void ExportCsvButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_reportTable == null ||
                _reportTable.Rows.Count == 0)
            {
                MessageBox.Show(
                    "Không có dữ liệu thanh toán để xuất CSV.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Microsoft.Win32.SaveFileDialog dialog =
                new Microsoft.Win32.SaveFileDialog();

            dialog.Title = "Xuất báo cáo thống kê";

            dialog.Filter = "CSV file (*.csv)|*.csv";

            dialog.FileName =
                "PhenikaaCinema_Report_" +
                DateTime.Now.ToString("yyyyMMdd_HHmm") +
                ".csv";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                StringBuilder csv = new StringBuilder();

                for (int columnIndex = 0;
                     columnIndex < _reportTable.Columns.Count;
                     columnIndex++)
                {
                    if (columnIndex > 0)
                        csv.Append(",");

                    csv.Append(EscapeCsv(
                        _reportTable.Columns[columnIndex].ColumnName));
                }

                csv.AppendLine();

                foreach (DataRow row in _reportTable.Rows)
                {
                    for (int columnIndex = 0;
                         columnIndex < _reportTable.Columns.Count;
                         columnIndex++)
                    {
                        if (columnIndex > 0)
                            csv.Append(",");

                        object value = row[columnIndex];

                        if (value == DBNull.Value)
                        {
                            csv.Append("\"\"");
                        }
                        else if (value is DateTime)
                        {
                            csv.Append(EscapeCsv(
                                ((DateTime)value).ToString(
                                    "dd/MM/yyyy HH:mm:ss")));
                        }
                        else
                        {
                            csv.Append(EscapeCsv(value.ToString()));
                        }
                    }

                    csv.AppendLine();
                }

                File.WriteAllText(
                    dialog.FileName,
                    csv.ToString(),
                    new UTF8Encoding(true));

                MessageBox.Show(
                    "Xuất báo cáo thành công.\n" +
                    dialog.FileName,
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xuất CSV.\n\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string EscapeCsv(string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
