using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public partial class SeatSelectionWindow : Window
    {
        private readonly ShowtimeDto _showtime;
        private readonly AuthUser _user;
        private readonly int? _targetCustomerUserId;

        private readonly SeatService _seatService = new SeatService();
        private readonly BookingService _bookingService = new BookingService();

        private List<SeatDto> _allSeats = new List<SeatDto>();

        public SeatSelectionWindow(ShowtimeDto showtime, AuthUser user = null, int? targetCustomerUserId = null)
        {
            InitializeComponent();
            _showtime = showtime ?? throw new ArgumentNullException(nameof(showtime));
            _user = user ?? SessionManager.CurrentUser;
            _targetCustomerUserId = targetCustomerUserId;

            HeaderMovieTitle.Text = _showtime.MovieTitle;
            HeaderShowtime.Text = $" - {_showtime.FormattedTimeRange}, {_showtime.FormattedDate}";
            RoomBadgeText.Text = _showtime.RoomName;

            SeatMapControl.SeatSelectionChanged += OnSeatSelectionChanged;

            Loaded += (s, e) => LoadSeatMap();
        }

        public SeatSelectionWindow(int showtimeId, AuthUser user = null, int? targetCustomerUserId = null)
            : this(new ShowtimeService().GetShowtimeById(showtimeId), user, targetCustomerUserId)
        {
        }

        private void LoadSeatMap()
        {
            try
            {
                _allSeats = _seatService.GetSeatsForShowtime(_showtime.ShowtimeId, _showtime.RoomId, _showtime.BasePrice);
                SeatMapControl.LoadSeats(_allSeats);
                Cinema3DControl.LoadRoom(_showtime.RoomName, _allSeats);
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải sơ đồ ghế: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSeatSelectionChanged(SeatDto seat, bool isSelected)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var selected = SeatMapControl.SelectedSeats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).ToList();
            if (selected.Count == 0)
            {
                SelectedSeatsText.Text = "Chưa chọn ghế nào";
                TotalPriceText.Text = "0 ₫";
                ProceedBookingButton.IsEnabled = false;
                return;
            }

            SelectedSeatsText.Text = string.Join(", ", selected.Select(s => s.SeatCode));
            decimal total = selected.Sum(s => s.CalculatedPrice);
            TotalPriceText.Text = CurrencyHelper.FormatVnd(total);
            ProceedBookingButton.IsEnabled = true;
        }

        private void RadioMode_Checked(object sender, RoutedEventArgs e)
        {
            if (SeatMapControl == null || Cinema3DControl == null) return;

            if (Radio2DMode != null && Radio2DMode.IsChecked == true)
            {
                SeatMapControl.Visibility = Visibility.Visible;
                Cinema3DControl.Visibility = Visibility.Collapsed;
            }
            else if (Radio3DMode != null && Radio3DMode.IsChecked == true)
            {
                // Đồng bộ trạng thái ghế vào phòng chiếu 3D
                Cinema3DControl.LoadRoom(_showtime.RoomName, _allSeats);
                SeatMapControl.Visibility = Visibility.Collapsed;
                Cinema3DControl.Visibility = Visibility.Visible;
            }
        }

        private void ProceedBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSeats = SeatMapControl.SelectedSeats.ToList();
            if (selectedSeats.Count == 0) return;

            AuthUser activeUser = _user ?? SessionManager.CurrentUser;
            if (activeUser == null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Bạn cần đăng nhập để hoàn tất đặt vé. Bạn có muốn mở cửa sổ đăng nhập ngay bây giờ không?",
                    "Yêu cầu đăng nhập",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();
                    activeUser = SessionManager.CurrentUser;
                    if (activeUser == null) return;
                }
                else
                {
                    return;
                }
            }

            int createdByUserId = activeUser.UserId;
            int? customerUserId = (activeUser.RoleId == 3) ? activeUser.UserId : _targetCustomerUserId;

            ProceedBookingButton.IsEnabled = false;
            ProceedBookingButton.Content = "ĐANG GIỮ CHỖ...";

            try
            {
                var seatIds = selectedSeats.Select(s => s.SeatId).ToList();

                var holdResult = _bookingService.CreateBookingHold(
                    _showtime.ShowtimeId,
                    createdByUserId,
                    customerUserId,
                    seatIds,
                    holdMinutes: 10);

                if (!holdResult.Success)
                {
                    MessageBox.Show(holdResult.Message, "Không thể giữ ghế", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LoadSeatMap();
                    return;
                }

                var confirmationWindow = new BookingConfirmationWindow(holdResult, _showtime, selectedSeats, activeUser)
                {
                    Owner = this
                };

                bool? dialogResult = confirmationWindow.ShowDialog();
                if (dialogResult == true)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    LoadSeatMap();
                }
            }
            finally
            {
                ProceedBookingButton.IsEnabled = true;
                ProceedBookingButton.Content = "TIẾP TỤC ĐẶT VÉ ▶";
            }
        }

        private void RefreshSeats_Click(object sender, RoutedEventArgs e)
        {
            LoadSeatMap();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
