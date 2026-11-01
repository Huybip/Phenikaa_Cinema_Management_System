using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class PromotionManagementWindow : Window
    {
        private readonly string _connectionString;
        private DataTable _promotionsTable;
        private int _selectedPromotionId;

        public PromotionManagementWindow()
        {
            InitializeComponent();

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["CinemaDb"];

            _connectionString = settings != null
                ? settings.ConnectionString
                : null;

            Loaded += PromotionManagementWindow_Loaded;
        }

        private void PromotionManagementWindow_Loaded(
            object sender, RoutedEventArgs e)
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

            LoadPromotions();
        }

        private void LoadPromotions()
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
                            PromotionId,
                            PromotionCode,
                            Description,
                            DiscountType,
                            DiscountValue,
                            StartAtUtc,
                            EndAtUtc,
                            MaxUsage,
                            UsedCount,
                            IsActive
                        FROM dbo.Promotions
                        ORDER BY PromotionId DESC;";

                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        _promotionsTable = table;
                    }
                }

                PromotionsGrid.ItemsSource = _promotionsTable.DefaultView;
                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách khuyến mãi.\n\n" + ex.Message,
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplySearch()
        {
            if (_promotionsTable == null)
                return;

            string search = SearchTextBox.Text.Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    _promotionsTable.DefaultView.RowFilter = "";
                }
                else
                {
                    _promotionsTable.DefaultView.RowFilter =
                        "PromotionCode LIKE '%" + search +
                        "%' OR Description LIKE '%" + search + "%'";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tìm kiếm: " + ex.Message,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void SearchButton_Click(
            object sender, RoutedEventArgs e)
        {
            ApplySearch();
        }

        private void RefreshButton_Click(
            object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            LoadPromotions();
        }

        private void PromotionsGrid_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            DataRowView row = PromotionsGrid.SelectedItem as DataRowView;

            if (row == null)
                return;

            _selectedPromotionId = Convert.ToInt32(row["PromotionId"]);

            PromotionCodeTextBox.Text =
                Convert.ToString(row["PromotionCode"]);

            DescriptionTextBox.Text =
                row["Description"] == DBNull.Value
                    ? ""
                    : Convert.ToString(row["Description"]);

            DiscountValueTextBox.Text =
                Convert.ToDecimal(row["DiscountValue"])
                    .ToString(CultureInfo.CurrentCulture);

            MaxUsageTextBox.Text =
                row["MaxUsage"] == DBNull.Value
                    ? ""
                    : Convert.ToString(row["MaxUsage"]);

            IsActiveCheckBox.IsChecked =
                Convert.ToBoolean(row["IsActive"]);

            string discountType = Convert.ToString(row["DiscountType"]);

            foreach (ComboBoxItem item in DiscountTypeComboBox.Items)
            {
                if (item.Content != null &&
                    item.Content.ToString() == discountType)
                {
                    DiscountTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            if (row["StartAtUtc"] != DBNull.Value)
            {
                DateTime startUtc = DateTime.SpecifyKind(
                    Convert.ToDateTime(row["StartAtUtc"]),
                    DateTimeKind.Utc);

                StartDatePicker.SelectedDate = startUtc.AddHours(7).Date;
            }

            if (row["EndAtUtc"] != DBNull.Value)
            {
                DateTime endUtc = DateTime.SpecifyKind(
                    Convert.ToDateTime(row["EndAtUtc"]),
                    DateTimeKind.Utc);

                EndDatePicker.SelectedDate = endUtc.AddHours(7).Date;
            }
        }

        private bool ValidateForm(
            out decimal discountValue,
            out int? maxUsage,
            out DateTime startUtc,
            out DateTime endUtc)
        {
            discountValue = 0;
            maxUsage = null;
            startUtc = DateTime.MinValue;
            endUtc = DateTime.MinValue;

            string code = PromotionCodeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Vui lòng nhập mã khuyến mãi.");
                PromotionCodeTextBox.Focus();
                return false;
            }

            if (code.Length > 30)
            {
                MessageBox.Show(
                    "Mã khuyến mãi không được vượt quá 30 ký tự.");
                PromotionCodeTextBox.Focus();
                return false;
            }

            if (DiscountTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại giảm giá.");
                return false;
            }

            if (!decimal.TryParse(
                    DiscountValueTextBox.Text.Trim(),
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out discountValue))
            {
                // Cho phép nhập dấu chấm thập phân trên một số thiết lập vùng.
                if (!decimal.TryParse(
                    DiscountValueTextBox.Text.Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out discountValue))
                {
                    MessageBox.Show("Giá trị giảm phải là số hợp lệ.");
                    return false;
                }
            }

            if (discountValue <= 0)
            {
                MessageBox.Show("Giá trị giảm phải lớn hơn 0.");
                return false;
            }

            ComboBoxItem selectedType =
                DiscountTypeComboBox.SelectedItem as ComboBoxItem;

            if (selectedType == null || selectedType.Content == null)
            {
                MessageBox.Show("Loại giảm giá không hợp lệ.");
                return false;
            }

            string discountType = selectedType.Content.ToString();

            if (discountType != "Percent" &&
                discountType != "FixedAmount")
            {
                MessageBox.Show("Loại giảm giá không được hỗ trợ.");
                return false;
            }

            if (discountType == "Percent" && discountValue > 100)
            {
                MessageBox.Show(
                    "Giảm theo phần trăm không được vượt quá 100.");
                return false;
            }

            if (discountType == "FixedAmount" &&
                discountValue > 9999999999999999m)
            {
                MessageBox.Show("Giá trị giảm quá lớn.");
                return false;
            }

            if (!StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Vui lòng chọn ngày bắt đầu và ngày kết thúc.");
                return false;
            }

            DateTime startLocal =
                StartDatePicker.SelectedDate.Value.Date;

            DateTime endLocal =
                EndDatePicker.SelectedDate.Value.Date;

            if (endLocal < startLocal)
            {
                MessageBox.Show(
                    "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
                return false;
            }

            // Chuyển thời gian Việt Nam sang UTC.
            startUtc = DateTime.SpecifyKind(
                startLocal.AddHours(-7),
                DateTimeKind.Utc);

            // Bao gồm toàn bộ ngày kết thúc theo giờ Việt Nam.
            endUtc = DateTime.SpecifyKind(
                endLocal.AddDays(1).AddTicks(-1).AddHours(-7),
                DateTimeKind.Utc);

            string maxUsageText = MaxUsageTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(maxUsageText))
            {
                int parsedMaxUsage;

                if (!int.TryParse(
                    maxUsageText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsedMaxUsage) ||
                    parsedMaxUsage < 0)
                {
                    MessageBox.Show(
                        "Số lượt tối đa phải là số nguyên không âm.");
                    return false;
                }

                maxUsage = parsedMaxUsage;
            }

            return true;
        }

        private void AddButton_Click(
            object sender, RoutedEventArgs e)
        {
            decimal discountValue;
            int? maxUsage;
            DateTime startUtc;
            DateTime endUtc;

            if (!ValidateForm(
                out discountValue,
                out maxUsage,
                out startUtc,
                out endUtc))
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO dbo.Promotions
                        (
                            PromotionCode,
                            Description,
                            DiscountType,
                            DiscountValue,
                            StartAtUtc,
                            EndAtUtc,
                            MaxUsage,
                            UsedCount,
                            IsActive
                        )
                        VALUES
                        (
                            @Code,
                            @Description,
                            @DiscountType,
                            @DiscountValue,
                            @StartAtUtc,
                            @EndAtUtc,
                            @MaxUsage,
                            0,
                            @IsActive
                        );";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        AddPromotionParameters(
                            command,
                            discountValue,
                            maxUsage,
                            startUtc,
                            endUtc);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Thêm khuyến mãi thành công.",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ClearForm();
                LoadPromotions();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Không thể thêm khuyến mãi.\n" +
                    "Kiểm tra mã khuyến mãi có bị trùng hoặc dữ liệu " +
                    "có vi phạm ràng buộc không.\n\n" + ex.Message,
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedPromotionId <= 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn khuyến mãi cần cập nhật.");
                return;
            }

            decimal discountValue;
            int? maxUsage;
            DateTime startUtc;
            DateTime endUtc;

            if (!ValidateForm(
                out discountValue,
                out maxUsage,
                out startUtc,
                out endUtc))
                return;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        UPDATE dbo.Promotions
                        SET PromotionCode = @Code,
                            Description = @Description,
                            DiscountType = @DiscountType,
                            DiscountValue = @DiscountValue,
                            StartAtUtc = @StartAtUtc,
                            EndAtUtc = @EndAtUtc,
                            MaxUsage = @MaxUsage,
                            IsActive = @IsActive
                        WHERE PromotionId = @PromotionId;";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        AddPromotionParameters(
                            command,
                            discountValue,
                            maxUsage,
                            startUtc,
                            endUtc);

                        command.Parameters.Add(
                            "@PromotionId",
                            SqlDbType.Int).Value = _selectedPromotionId;

                        int affectedRows = command.ExecuteNonQuery();

                        if (affectedRows == 0)
                        {
                            MessageBox.Show(
                                "Khuyến mãi không còn tồn tại. " +
                                "Vui lòng làm mới danh sách.");
                            LoadPromotions();
                            return;
                        }
                    }
                }

                MessageBox.Show(
                    "Cập nhật khuyến mãi thành công.",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ClearForm();
                LoadPromotions();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Không thể cập nhật khuyến mãi.\n" + ex.Message,
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể cập nhật khuyến mãi.\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddPromotionParameters(
            SqlCommand command,
            decimal discountValue,
            int? maxUsage,
            DateTime startUtc,
            DateTime endUtc)
        {
            ComboBoxItem selectedType =
                DiscountTypeComboBox.SelectedItem as ComboBoxItem;

            command.Parameters.Add(
                "@Code", SqlDbType.NVarChar, 30).Value =
                PromotionCodeTextBox.Text.Trim();

            command.Parameters.Add(
                "@Description", SqlDbType.NVarChar, 255).Value =
                string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                    ? (object)DBNull.Value
                    : DescriptionTextBox.Text.Trim();

            command.Parameters.Add(
                "@DiscountType", SqlDbType.NVarChar, 20).Value =
                selectedType.Content.ToString();

            SqlParameter discountParameter =
                command.Parameters.Add(
                    "@DiscountValue", SqlDbType.Decimal);

            discountParameter.Precision = 18;
            discountParameter.Scale = 2;
            discountParameter.Value = discountValue;

            command.Parameters.Add(
                "@StartAtUtc", SqlDbType.DateTime2).Value = startUtc;

            command.Parameters.Add(
                "@EndAtUtc", SqlDbType.DateTime2).Value = endUtc;

            command.Parameters.Add(
                "@MaxUsage", SqlDbType.Int).Value =
                maxUsage.HasValue
                    ? (object)maxUsage.Value
                    : DBNull.Value;

            command.Parameters.Add(
                "@IsActive", SqlDbType.Bit).Value =
                IsActiveCheckBox.IsChecked == true;
        }

        private void ClearButton_Click(
            object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _selectedPromotionId = 0;

            PromotionsGrid.SelectedItem = null;

            PromotionCodeTextBox.Clear();
            DescriptionTextBox.Clear();
            DiscountValueTextBox.Clear();
            MaxUsageTextBox.Clear();

            DiscountTypeComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            IsActiveCheckBox.IsChecked = true;
        }
    }
}
