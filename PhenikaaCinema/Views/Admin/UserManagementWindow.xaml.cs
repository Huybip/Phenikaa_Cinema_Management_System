using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class UserManagementWindow : Window
    {
        private readonly string connectionString;
        private DataTable usersTable;
        private bool loading;

        public UserManagementWindow()
        {
            InitializeComponent();

            connectionString =
                System.Configuration.ConfigurationManager
                    .ConnectionStrings["CinemaDb"].ConnectionString;

            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT RoleId, RoleName FROM dbo.Roles ORDER BY RoleId",
                    conn))
                {
                    DataTable roles = new DataTable();
                    adapter.Fill(roles);

                    RoleComboBox.ItemsSource = roles.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải vai trò: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            loading = true;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    @"SELECT u.UserId, u.RoleId, r.RoleName,
                             u.FullName, u.Email, u.Phone,
                             u.IsActive, u.CreatedAtUtc, u.UpdatedAtUtc
                      FROM dbo.Users u
                      INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                      ORDER BY u.UserId DESC", conn))
                {
                    usersTable = new DataTable();
                    adapter.Fill(usersTable);

                    UsersDataGrid.ItemsSource = usersTable.DefaultView;
                    ApplySearch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách người dùng: "
                    + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loading = false;
            }
        }

        private void ApplySearch()
        {
            if (usersTable == null)
                return;

            string keyword = (SearchTextBox.Text ?? "").Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            if (keyword.Length == 0)
            {
                usersTable.DefaultView.RowFilter = "";
                return;
            }

            usersTable.DefaultView.RowFilter =
                "Convert(UserId, 'System.String') LIKE '%" + keyword + "%' OR " +
                "FullName LIKE '%" + keyword + "%' OR " +
                "Email LIKE '%" + keyword + "%' OR " +
                "Phone LIKE '%" + keyword + "%' OR " +
                "RoleName LIKE '%" + keyword + "%'";
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (!loading)
                ApplySearch();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            ClearForm();
            LoadUsers();
        }

        private void UsersDataGrid_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            DataRowView row = UsersDataGrid.SelectedItem as DataRowView;

            if (row == null)
                return;

            FullNameTextBox.Text = Convert.ToString(row["FullName"]);
            EmailTextBox.Text = Convert.ToString(row["Email"]);
            PhoneTextBox.Text = row["Phone"] == DBNull.Value
                ? "" : Convert.ToString(row["Phone"]);

            RoleComboBox.SelectedValue = Convert.ToInt32(row["RoleId"]);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            if (fullName.Length == 0 || email.Length == 0)
            {
                MessageBox.Show("Vui lòng nhập họ tên và email.");
                return;
            }

            if (RoleComboBox.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn vai trò.");
                return;
            }

            if (!email.Contains("@") || email.Length > 150)
            {
                MessageBox.Show("Email không hợp lệ.");
                return;
            }

            if (fullName.Length > 100 || phone.Length > 25)
            {
                MessageBox.Show("Họ tên tối đa 100 ký tự, số điện thoại tối đa 25 ký tự.");
                return;
            }

            PasswordDialog dialog = new PasswordDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() != true)
                return;

            string password = dialog.Password;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 8 ký tự.");
                return;
            }

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.Users
                        (RoleId, FullName, Email, PasswordHash,
                         Phone, IsActive, CreatedAtUtc)
                      VALUES
                        (@RoleId, @FullName, @Email, @PasswordHash,
                         @Phone, 1, SYSUTCDATETIME())", conn))
                {
                    cmd.Parameters.Add("@RoleId", SqlDbType.Int).Value =
                        Convert.ToInt32(RoleComboBox.SelectedValue);

                    cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
                        .Value = fullName;

                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150)
                        .Value = email;

                    cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255)
                        .Value = passwordHash;

                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 25)
                        .Value = phone.Length == 0
                            ? (object)DBNull.Value : phone;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Thêm người dùng thành công.");
                ClearForm();
                LoadUsers();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    MessageBox.Show("Email đã tồn tại. Vui lòng sử dụng email khác.");
                }
                else
                {
                    MessageBox.Show("Không thể thêm người dùng: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm người dùng: " + ex.Message);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView row = UsersDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn người dùng cần cập nhật.");
                return;
            }

            int userId = Convert.ToInt32(row["UserId"]);
            string fullName = FullNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            if (fullName.Length == 0 || email.Length == 0 ||
                RoleComboBox.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ họ tên, email và vai trò.");
                return;
            }

            if (!email.Contains("@") || email.Length > 150 ||
                fullName.Length > 100 || phone.Length > 25)
            {
                MessageBox.Show("Thông tin không hợp lệ hoặc vượt quá giới hạn ký tự.");
                return;
            }

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.Users
                      SET RoleId = @RoleId,
                          FullName = @FullName,
                          Email = @Email,
                          Phone = @Phone,
                          UpdatedAtUtc = SYSUTCDATETIME()
                      WHERE UserId = @UserId", conn))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@RoleId", SqlDbType.Int).Value =
                        Convert.ToInt32(RoleComboBox.SelectedValue);
                    cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
                        .Value = fullName;
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150)
                        .Value = email;
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 25)
                        .Value = phone.Length == 0
                            ? (object)DBNull.Value : phone;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Cập nhật người dùng thành công.");
                LoadUsers();
                SelectUser(userId);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    MessageBox.Show("Email đã được sử dụng bởi tài khoản khác.");
                else
                    MessageBox.Show("Không thể cập nhật: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật: " + ex.Message);
            }
        }

        private void ToggleStatusButton_Click(
            object sender, RoutedEventArgs e)
        {
            DataRowView row = UsersDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn người dùng.");
                return;
            }

            int userId = Convert.ToInt32(row["UserId"]);
            bool isActive = Convert.ToBoolean(row["IsActive"]);
            int roleId = Convert.ToInt32(row["RoleId"]);

            if (AuthService.CurrentUser != null &&
            userId == AuthService.CurrentUser.UserId)
            {
                MessageBox.Show(
                    "Không thể tự vô hiệu hóa tài khoản đang đăng nhập.",
                    "Cảnh báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (roleId == 1 && isActive)
            {
                MessageBox.Show("Không thể vô hiệu hóa tài khoản Admin bằng chức năng này.");
                return;
            }

            string action = isActive ? "vô hiệu hóa" : "kích hoạt";

            MessageBoxResult answer = MessageBox.Show(
                "Bạn có chắc muốn " + action + " tài khoản này?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.Users
                      SET IsActive = @IsActive,
                          UpdatedAtUtc = SYSUTCDATETIME()
                      WHERE UserId = @UserId", conn))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = !isActive;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Đã cập nhật trạng thái tài khoản.");
                LoadUsers();
                SelectUser(userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật trạng thái: " + ex.Message);
            }
        }

        private void SelectUser(int userId)
        {
            foreach (DataRowView item in UsersDataGrid.Items)
            {
                if (Convert.ToInt32(item["UserId"]) == userId)
                {
                    UsersDataGrid.SelectedItem = item;
                    UsersDataGrid.ScrollIntoView(item);
                    break;
                }
            }
        }

        private void ClearForm()
        {
            FullNameTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();
            RoleComboBox.SelectedIndex = -1;
            UsersDataGrid.SelectedItem = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn người dùng cần xóa.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DataRowView row = UsersDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Không đọc được dữ liệu người dùng.");
                return;
            }

            int userId = Convert.ToInt32(row["UserId"]);
            string fullName = Convert.ToString(row["FullName"]);
            string email = Convert.ToString(row["Email"]);

            if (AuthService.CurrentUser != null &&
                userId == AuthService.CurrentUser.UserId)
            {
                MessageBox.Show(
                    "Không thể xóa tài khoản Admin đang đăng nhập.",
                    "Cảnh báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa tài khoản này không?\n\n" +
                "Họ tên: " + fullName + "\n" +
                "Email: " + email,
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                string connectionString =
                    System.Configuration.ConfigurationManager
                        .ConnectionStrings["CinemaDb"].ConnectionString;

                using (SqlConnection connection =
                    new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            // Khóa và kiểm tra tài khoản trước khi xóa.
                            using (SqlCommand checkCommand = new SqlCommand(
                                @"SELECT UserId
                          FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                          WHERE UserId = @UserId",
                                connection, transaction))
                            {
                                checkCommand.Parameters.Add(
                                    "@UserId", SqlDbType.Int).Value = userId;

                                object existing =
                                    checkCommand.ExecuteScalar();

                                if (existing == null)
                                {
                                    transaction.Rollback();

                                    MessageBox.Show(
                                        "Tài khoản không còn tồn tại.",
                                        "Thông báo",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                    return;
                                }
                            }

                            // Không xóa tài khoản có lịch sử đặt vé.
                            using (SqlCommand bookingCommand = new SqlCommand(
                                @"SELECT COUNT(1)
                          FROM dbo.Bookings
                          WHERE UserId = @UserId",
                                connection, transaction))
                            {
                                bookingCommand.Parameters.Add(
                                    "@UserId", SqlDbType.Int).Value = userId;

                                int bookingCount = Convert.ToInt32(
                                    bookingCommand.ExecuteScalar());

                                if (bookingCount > 0)
                                {
                                    transaction.Rollback();

                                    MessageBox.Show(
                                        "Không thể xóa vì tài khoản đã có lịch sử " +
                                        "đặt vé. Hãy dùng chức năng Đổi trạng thái " +
                                        "để vô hiệu hóa tài khoản.",
                                        "Không thể xóa",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            using (SqlCommand deleteCommand = new SqlCommand(
                                @"DELETE FROM dbo.Users
                          WHERE UserId = @UserId",
                                connection, transaction))
                            {
                                deleteCommand.Parameters.Add(
                                    "@UserId", SqlDbType.Int).Value = userId;

                                int affectedRows =
                                    deleteCommand.ExecuteNonQuery();

                                if (affectedRows != 1)
                                {
                                    throw new InvalidOperationException(
                                        "Không xóa được đúng một tài khoản.");
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                                // Giao dịch có thể đã kết thúc.
                            }

                            throw;
                        }
                    }
                }

                MessageBox.Show(
                    "Đã xóa người dùng thành công.",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadUsers();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    MessageBox.Show(
                        "Database từ chối xóa vì tài khoản đang được tham chiếu " +
                        "bởi bảng khác, không chỉ bảng Bookings.\n\n" +
                        "Chi tiết: " + ex.Message +
                        "\n\nHãy kiểm tra danh sách khóa ngoại ở bước 1.",
                        "Ràng buộc dữ liệu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Lỗi SQL (" + ex.Number + "): " + ex.Message,
                        "Lỗi database",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi xóa người dùng: " + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
