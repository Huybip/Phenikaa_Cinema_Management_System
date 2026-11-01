using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhenikaaCinema.Views.Admin
{
    public partial class RoomManagementWindow : Window
    {
        private readonly string _connectionString;
        private bool _loadingSelection;
        private bool _loadingSeatSelection;
        private List<RoomManagementItem> _allRooms = new List<RoomManagementItem>();
        private List<SeatManagementItem> _allSeats = new List<SeatManagementItem>();

        public RoomManagementWindow()
        {
            InitializeComponent();
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["CinemaDb"];
            if (setting == null)
            {
                MessageBox.Show("Không tìm thấy connection string CinemaDb trong App.config.", "Lỗi cấu hình", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            _connectionString = setting.ConnectionString;
            try { LoadRooms(); }
            catch (Exception ex) { MessageBox.Show("Không thể tải danh sách phòng.\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadRooms()
        {
            var rooms = new List<RoomManagementItem>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                SELECT r.RoomId, r.RoomName, r.Capacity, r.IsActive, r.CreatedAtUtc,
                       (SELECT COUNT(*) FROM dbo.Seats s WHERE s.RoomId = r.RoomId) AS SeatCount
                FROM dbo.Rooms r ORDER BY r.RoomId;", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new RoomManagementItem
                        {
                            RoomId = reader.GetInt32(0),
                            RoomName = reader.GetString(1),
                            Capacity = reader.GetInt32(2),
                            IsActive = reader.GetBoolean(3),
                            CreatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                            SeatCount = reader.GetInt32(5)
                        });
                    }
                }
            }
            _allRooms = rooms;
            ApplySearch();
            int selectedRoomId;
            if (int.TryParse(RoomIdTextBox.Text, out selectedRoomId))
            {
                var selected = _allRooms.FirstOrDefault(r => r.RoomId == selectedRoomId);
                if (selected != null) RoomsDataGrid.SelectedItem = selected;
            }
        }

        private void ApplySearch()
        {
            string keyword = (SearchTextBox.Text ?? string.Empty).Trim();
            IEnumerable<RoomManagementItem> query = _allRooms;
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(r => r.RoomId.ToString().Contains(keyword) ||
                    r.RoomName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.StatusText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            RoomsDataGrid.ItemsSource = query.ToList();
        }

        private bool TryReadRoomForm(out string roomName, out int capacity, out bool isActive)
        {
            roomName = (RoomNameTextBox.Text ?? string.Empty).Trim();
            capacity = 0; isActive = IsActiveCheckBox.IsChecked == true;
            if (roomName.Length == 0) { MessageBox.Show("Vui lòng nhập tên phòng."); RoomNameTextBox.Focus(); return false; }
            if (roomName.Length > 100) { MessageBox.Show("Tên phòng không được vượt quá 100 ký tự."); return false; }
            if (!int.TryParse(CapacityTextBox.Text.Trim(), out capacity) || capacity < 1)
            { MessageBox.Show("Sức chứa phải là số nguyên lớn hơn hoặc bằng 1."); CapacityTextBox.Focus(); return false; }
            return true;
        }

        private bool RoomNameExists(string roomName, int? excludeRoomId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"SELECT COUNT(*) FROM dbo.Rooms WHERE RoomName=@RoomName AND (@ExcludeRoomId IS NULL OR RoomId<>@ExcludeRoomId);", connection))
            {
                command.Parameters.Add("@RoomName", SqlDbType.NVarChar, 100).Value = roomName;
                command.Parameters.Add("@ExcludeRoomId", SqlDbType.Int).Value = excludeRoomId.HasValue ? (object)excludeRoomId.Value : DBNull.Value;
                connection.Open(); return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name; int capacity; bool active;
            if (!TryReadRoomForm(out name, out capacity, out active)) return;
            try
            {
                if (RoomNameExists(name, null)) { MessageBox.Show("Tên phòng đã tồn tại."); return; }
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"INSERT INTO dbo.Rooms(RoomName, Capacity, IsActive, CreatedAtUtc) VALUES(@Name,@Capacity,@Active,SYSUTCDATETIME());", connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                    command.Parameters.Add("@Capacity", SqlDbType.Int).Value = capacity;
                    command.Parameters.Add("@Active", SqlDbType.Bit).Value = active;
                    connection.Open(); command.ExecuteNonQuery();
                }
                MessageBox.Show("Thêm phòng thành công."); LoadRooms(); ClearRoomForm();
            }
            catch (Exception ex) { ShowError("Không thể thêm phòng", ex); }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            int roomId; string name; int capacity; bool active;
            if (!int.TryParse(RoomIdTextBox.Text, out roomId)) { MessageBox.Show("Vui lòng chọn phòng cần cập nhật."); return; }
            if (!TryReadRoomForm(out name, out capacity, out active)) return;
            try
            {
                if (RoomNameExists(name, roomId)) { MessageBox.Show("Tên phòng đã được sử dụng bởi phòng khác."); return; }
                int seatCount = GetRoomSeatCount(roomId);
                if (capacity < seatCount) { MessageBox.Show("Sức chứa không thể nhỏ hơn số ghế đã tạo (" + seatCount + " ghế)."); return; }
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"UPDATE dbo.Rooms SET RoomName=@Name, Capacity=@Capacity, IsActive=@Active WHERE RoomId=@RoomId;", connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                    command.Parameters.Add("@Capacity", SqlDbType.Int).Value = capacity;
                    command.Parameters.Add("@Active", SqlDbType.Bit).Value = active;
                    command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
                    connection.Open(); if (command.ExecuteNonQuery() == 0) { MessageBox.Show("Không tìm thấy phòng."); return; }
                }
                MessageBox.Show("Cập nhật phòng thành công."); LoadRooms(); SelectRoom(roomId);
            }
            catch (Exception ex) { ShowError("Không thể cập nhật phòng", ex); }
        }

        private int GetRoomSeatCount(int roomId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Seats WHERE RoomId=@RoomId;", connection))
            { command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId; connection.Open(); return Convert.ToInt32(command.ExecuteScalar()); }
        }

        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingSelection) return;
            var room = RoomsDataGrid.SelectedItem as RoomManagementItem;
            if (room == null) return;
            _loadingSelection = true;
            try
            {
                RoomIdTextBox.Text = room.RoomId.ToString(); RoomNameTextBox.Text = room.RoomName;
                CapacityTextBox.Text = room.Capacity.ToString(); IsActiveCheckBox.IsChecked = room.IsActive;
                CreatedAtTextBox.Text = room.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
                SeatRoomLabel.Text = "Phòng: " + room.RoomName + " (mã " + room.RoomId + ") — sức chứa " + room.Capacity +
                    " | " + room.SeatCount + " ghế";
            }
            finally { _loadingSelection = false; }
            try { LoadSeats(room.RoomId); }
            catch (Exception ex) { ShowError("Không thể tải danh sách ghế", ex); }
        }

        private void LoadSeats(int roomId)
        {
            var seats = new List<SeatManagementItem>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"SELECT SeatId, RoomId, RowLabel, SeatNumber, SeatType, ExtraPrice, IsActive
                FROM dbo.Seats WHERE RoomId=@RoomId ORDER BY RowLabel, SeatNumber;", connection))
            {
                command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId; connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        seats.Add(new SeatManagementItem
                        {
                            SeatId = reader.GetInt32(0),
                            RoomId = reader.GetInt32(1),
                            RowLabel = reader.GetString(2),
                            SeatNumber = reader.GetInt32(3),
                            SeatType = reader.GetString(4),
                            ExtraPrice = reader.GetDecimal(5),
                            IsActive = reader.GetBoolean(6)
                        });
            }
            _allSeats = seats;
            SeatsDataGrid.ItemsSource = seats;
            BuildSeatMap(seats);
        }

        private void BuildSeatMap(List<SeatManagementItem> seats)
        {
            var rows = seats.GroupBy(s => s.RowLabel, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new SeatMapRow
                {
                    RowLabel = g.Key,
                    Seats = g.OrderBy(s => s.SeatNumber).Select(s => new SeatMapCell
                    {
                        SeatId = s.SeatId,
                        SeatLabel = g.Key + s.SeatNumber,
                        SeatColor = !s.IsActive ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : (s.SeatType.Equals("VIP", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(124, 58, 237)) : (s.SeatType.Equals("Couple", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.FromRgb(225, 29, 72)) : new SolidColorBrush(Color.FromRgb(23, 105, 170)))),
                        TextColor = Brushes.White,
                        ToolTipText = g.Key + s.SeatNumber + " | " + s.SeatType + " | " +
                            (s.IsActive ? "Đang hoạt động" : "Ngừng hoạt động") +
                            " | Phụ phí: " + s.ExtraPrice.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " VND"
                    }).ToList()
                }).ToList();
            SeatMapItemsControl.ItemsSource = rows;
        }

        private void SeatMapButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null) return;
            int seatId;
            if (!int.TryParse(button.Tag.ToString(), out seatId)) return;
            var seat = _allSeats.FirstOrDefault(s => s.SeatId == seatId);
            if (seat == null) return;
            SeatsDataGrid.SelectedItem = seat;
            SeatsDataGrid.ScrollIntoView(seat);
            SeatRowTextBox.Text = seat.RowLabel;
            SeatNumberTextBox.Text = seat.SeatNumber.ToString();
            SeatTypeComboBox.Text = seat.SeatType;
            SeatExtraPriceTextBox.Text = seat.ExtraPrice.ToString("0.##", CultureInfo.InvariantCulture);
            SeatIsActiveCheckBox.IsChecked = seat.IsActive;
        }

        private bool TryReadSeatForm(out string rowLabel, out int seatNumber, out string seatType, out decimal extraPrice, out bool isActive)
        {
            rowLabel = (SeatRowTextBox.Text ?? string.Empty).Trim().ToUpperInvariant();
            seatNumber = 0; seatType = (SeatTypeComboBox.Text ?? string.Empty).Trim(); extraPrice = 0;
            isActive = SeatIsActiveCheckBox.IsChecked == true;
            if (!int.TryParse(RoomIdTextBox.Text, out int roomId)) { MessageBox.Show("Hãy chọn phòng trước khi quản lý ghế."); return false; }
            if (rowLabel.Length == 0 || rowLabel.Length > 5) { MessageBox.Show("Hàng ghế phải có từ 1 đến 5 ký tự, ví dụ A hoặc AA."); return false; }
            if (!int.TryParse(SeatNumberTextBox.Text.Trim(), out seatNumber) || seatNumber < 1) { MessageBox.Show("Số ghế phải là số nguyên lớn hơn 0."); return false; }
            if (seatType.Length == 0 || seatType.Length > 20) { MessageBox.Show("Loại ghế không được để trống và tối đa 20 ký tự."); return false; }
            if (!decimal.TryParse(SeatExtraPriceTextBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out extraPrice) || extraPrice < 0)
            { MessageBox.Show("Phụ phí phải là số không âm, ví dụ 0 hoặc 20000."); return false; }
            return true;
        }

        private bool SeatPositionExists(int roomId, string row, int number, int? excludeSeatId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"SELECT COUNT(*) FROM dbo.Seats WHERE RoomId=@RoomId AND RowLabel=@RowLabel AND SeatNumber=@SeatNumber AND (@ExcludeSeatId IS NULL OR SeatId<>@ExcludeSeatId);", connection))
            {
                command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
                command.Parameters.Add("@RowLabel", SqlDbType.NVarChar, 5).Value = row;
                command.Parameters.Add("@SeatNumber", SqlDbType.Int).Value = number;
                command.Parameters.Add("@ExcludeSeatId", SqlDbType.Int).Value = excludeSeatId.HasValue ? (object)excludeSeatId.Value : DBNull.Value;
                connection.Open(); return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void AddSeatButton_Click(object sender, RoutedEventArgs e)
        {
            int roomId; if (!int.TryParse(RoomIdTextBox.Text, out roomId)) { MessageBox.Show("Hãy chọn phòng trước."); return; }
            string row, type; int number; decimal extra; bool active;
            if (!TryReadSeatForm(out row, out number, out type, out extra, out active)) return;
            try
            {
                if (GetRoomSeatCount(roomId) >= GetRoomCapacity(roomId)) { MessageBox.Show("Số ghế đã đạt sức chứa của phòng."); return; }
                if (SeatPositionExists(roomId, row, number, null)) { MessageBox.Show("Vị trí ghế này đã tồn tại trong phòng."); return; }
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"INSERT INTO dbo.Seats(RoomId, RowLabel, SeatNumber, SeatType, ExtraPrice, IsActive) VALUES(@RoomId,@RowLabel,@SeatNumber,@SeatType,@ExtraPrice,@IsActive);", connection))
                { AddSeatParameters(command, roomId, row, number, type, extra, active); connection.Open(); command.ExecuteNonQuery(); }
                LoadSeats(roomId); LoadRooms(); SelectRoom(roomId); ClearSeatForm(); MessageBox.Show("Thêm ghế thành công.");
            }
            catch (Exception ex) { ShowError("Không thể thêm ghế", ex); }
        }

        private void UpdateSeatButton_Click(object sender, RoutedEventArgs e)
        {
            var seat = SeatsDataGrid.SelectedItem as SeatManagementItem;
            if (seat == null) { MessageBox.Show("Chọn ghế cần cập nhật trong danh sách hoặc sơ đồ."); return; }
            string row, type; int number; decimal extra; bool active;
            if (!TryReadSeatForm(out row, out number, out type, out extra, out active)) return;
            try
            {
                if (SeatPositionExists(seat.RoomId, row, number, seat.SeatId)) { MessageBox.Show("Vị trí ghế này đã tồn tại trong phòng."); return; }
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(@"UPDATE dbo.Seats SET RowLabel=@RowLabel, SeatNumber=@SeatNumber, SeatType=@SeatType, ExtraPrice=@ExtraPrice, IsActive=@IsActive WHERE SeatId=@SeatId AND RoomId=@RoomId;", connection))
                {
                    AddSeatParameters(command, seat.RoomId, row, number, type, extra, active);
                    command.Parameters.Add("@SeatId", SqlDbType.Int).Value = seat.SeatId;
                    connection.Open(); if (command.ExecuteNonQuery() == 0) { MessageBox.Show("Không tìm thấy ghế."); return; }
                }
                LoadSeats(seat.RoomId); LoadRooms(); SelectRoom(seat.RoomId); ClearSeatForm(); MessageBox.Show("Cập nhật ghế thành công.");
            }
            catch (Exception ex) { ShowError("Không thể cập nhật ghế", ex); }
        }

        private void AddSeatParameters(SqlCommand command, int roomId, string row, int number, string type, decimal extra, bool active)
        {
            command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId;
            command.Parameters.Add("@RowLabel", SqlDbType.NVarChar, 5).Value = row;
            command.Parameters.Add("@SeatNumber", SqlDbType.Int).Value = number;
            command.Parameters.Add("@SeatType", SqlDbType.NVarChar, 20).Value = type;
            var price = command.Parameters.Add("@ExtraPrice", SqlDbType.Decimal); price.Precision = 18; price.Scale = 2; price.Value = extra;
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = active;
        }

        private void DeleteSeatButton_Click(object sender, RoutedEventArgs e)
        {
            var seat = SeatsDataGrid.SelectedItem as SeatManagementItem;
            if (seat == null) { MessageBox.Show("Chọn ghế cần xóa."); return; }
            if (MessageBox.Show("Bạn có chắc muốn xóa ghế này? Nếu ghế đã được dùng trong đặt vé, hệ thống sẽ không cho xóa.", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var check = new SqlCommand("SELECT COUNT(*) FROM dbo.BookingSeats WHERE SeatId=@SeatId;", connection))
                    {
                        check.Parameters.Add("@SeatId", SqlDbType.Int).Value = seat.SeatId;
                        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        { MessageBox.Show("Ghế đã liên kết với dữ liệu đặt vé. Hãy tắt ghế thay vì xóa.", "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    }
                    using (var delete = new SqlCommand("DELETE FROM dbo.Seats WHERE SeatId=@SeatId AND RoomId=@RoomId;", connection))
                    { delete.Parameters.Add("@SeatId", SqlDbType.Int).Value = seat.SeatId; delete.Parameters.Add("@RoomId", SqlDbType.Int).Value = seat.RoomId; delete.ExecuteNonQuery(); }
                }
                LoadSeats(seat.RoomId); LoadRooms(); SelectRoom(seat.RoomId); ClearSeatForm(); MessageBox.Show("Đã xóa ghế.");
            }
            catch (Exception ex) { ShowError("Không thể xóa ghế", ex); }
        }

        private int GetRoomCapacity(int roomId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT Capacity FROM dbo.Rooms WHERE RoomId=@RoomId;", connection))
            { command.Parameters.Add("@RoomId", SqlDbType.Int).Value = roomId; connection.Open(); return Convert.ToInt32(command.ExecuteScalar()); }
        }

        private void SeatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingSeatSelection) return;
            var seat = SeatsDataGrid.SelectedItem as SeatManagementItem;
            if (seat == null) return;
            _loadingSeatSelection = true;
            try
            {
                SeatRowTextBox.Text = seat.RowLabel; SeatNumberTextBox.Text = seat.SeatNumber.ToString();
                SeatTypeComboBox.Text = seat.SeatType; SeatExtraPriceTextBox.Text = seat.ExtraPrice.ToString("0.##", CultureInfo.InvariantCulture);
                SeatIsActiveCheckBox.IsChecked = seat.IsActive;
            }
            finally { _loadingSeatSelection = false; }
        }

        private void SelectRoom(int roomId)
        {
            var room = _allRooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room != null) RoomsDataGrid.SelectedItem = room;
        }

        private void ClearRoomForm()
        {
            _loadingSelection = true;
            try { RoomsDataGrid.SelectedItem = null; RoomIdTextBox.Clear(); RoomNameTextBox.Clear(); CapacityTextBox.Text = "20"; IsActiveCheckBox.IsChecked = true; CreatedAtTextBox.Clear(); }
            finally { _loadingSelection = false; }
            _allSeats.Clear(); SeatsDataGrid.ItemsSource = null; SeatMapItemsControl.ItemsSource = null;
            SeatRoomLabel.Text = "Chọn một phòng ở danh sách bên trái."; ClearSeatForm();
        }

        private void ClearSeatForm()
        {
            _loadingSeatSelection = true;
            try { SeatsDataGrid.SelectedItem = null; SeatRowTextBox.Clear(); SeatNumberTextBox.Clear(); SeatTypeComboBox.Text = "Standard"; SeatExtraPriceTextBox.Text = "0"; SeatIsActiveCheckBox.IsChecked = true; }
            finally { _loadingSeatSelection = false; }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) { ClearRoomForm(); }
        private void ClearSeatButton_Click(object sender, RoutedEventArgs e) { ClearSeatForm(); }
        private void SearchButton_Click(object sender, RoutedEventArgs e) { ApplySearch(); }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsInitialized) ApplySearch(); }

        private void RefreshSeatsButton_Click(object sender, RoutedEventArgs e)
        {
            int roomId;
            if (!int.TryParse(RoomIdTextBox.Text, out roomId)) { MessageBox.Show("Hãy chọn phòng trước."); return; }
            try { LoadSeats(roomId); } catch (Exception ex) { ShowError("Không thể tải danh sách ghế", ex); }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try { SearchTextBox.Clear(); LoadRooms(); int roomId; if (int.TryParse(RoomIdTextBox.Text, out roomId)) LoadSeats(roomId); }
            catch (Exception ex) { ShowError("Không thể làm mới dữ liệu", ex); }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) { Close(); }
        private void ShowError(string message, Exception ex) { MessageBox.Show(message + ".\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    public class RoomManagementItem
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int SeatCount { get; set; }
        public string StatusText { get { return IsActive ? "Hoạt động" : "Ngừng hoạt động"; } }
    }

    public class SeatManagementItem
    {
        public int SeatId { get; set; }
        public int RoomId { get; set; }
        public string RowLabel { get; set; }
        public int SeatNumber { get; set; }
        public string SeatType { get; set; }
        public decimal ExtraPrice { get; set; }
        public bool IsActive { get; set; }
    }

    public class SeatMapRow
    {
        public string RowLabel { get; set; }
        public List<SeatMapCell> Seats { get; set; }
    }

    public class SeatMapCell
    {
        public int SeatId { get; set; }
        public string SeatLabel { get; set; }
        public Brush SeatColor { get; set; }
        public Brush TextColor { get; set; }
        public string ToolTipText { get; set; }
    }
}
