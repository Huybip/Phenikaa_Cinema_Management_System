using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;

namespace PhenikaaCinema.Views.Customer
{
    public partial class MyBookingsView : UserControl
    {
        private int? _userId;
        private bool _isViewReady;
        private readonly BookingService _bookingService = new BookingService();
        private List<BookingDetailDto> _allBookings = new List<BookingDetailDto>();

        public MyBookingsView()
        {
            InitializeComponent();
            _isViewReady = true;
            _userId = SessionManager.CurrentUserId;
            Loaded += MyBookingsView_Loaded;
        }

        public MyBookingsView(int? userId) : this()
        {
            _userId = userId ?? SessionManager.CurrentUserId;
        }

        public void SetUserId(int? userId)
        {
            _userId = userId;
            LoadBookings();
        }

        private void MyBookingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBookings();
        }

        public void LoadBookings()
        {
            try
            {
                int? uid = _userId;
                if (!uid.HasValue && SessionManager.CurrentUser != null && SessionManager.CurrentUser.RoleId == 3)
                {
                    uid = SessionManager.CurrentUserId;
                }

                if (uid.HasValue)
                {
                    _allBookings = _bookingService.GetBookingsByCustomer(uid.Value);
                }
                else
                {
                    _allBookings = _bookingService.GetAllBookings();
                }

                FilterData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải lịch sử đặt vé: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterData()
        {
            if (!_isViewReady) return;

            if (_allBookings == null) _allBookings = new List<BookingDetailDto>();

            string keyword = SearchBox?.Text?.Trim() ?? "";
            string statusFilter = (StatusFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            var filtered = _allBookings.FindAll(b =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                    (b.BookingCode != null && b.BookingCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.MovieTitle != null && b.MovieTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.RoomName != null && b.RoomName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                bool matchStatus = true;
                if (statusFilter.Contains("Confirmed"))
                {
                    matchStatus = string.Equals(b.Status, "Confirmed", StringComparison.OrdinalIgnoreCase);
                }
                else if (statusFilter.Contains("Pending"))
                {
                    matchStatus = string.Equals(b.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase);
                }
                else if (statusFilter.Contains("Cancelled"))
                {
                    matchStatus = string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(b.Status, "Expired", StringComparison.OrdinalIgnoreCase);
                }

                return matchKeyword && matchStatus;
            });

            BookingsDataGrid.ItemsSource = filtered;
            EmptyMessageText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SummaryCountText.Text = $"Hiển thị {filtered.Count} / {_allBookings.Count} đơn đặt vé";

            UpdateActionButtons(null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBookings();
        }

        private void BookingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = BookingsDataGrid.SelectedItem as BookingDetailDto;
            UpdateActionButtons(selected);
        }

        private void UpdateActionButtons(BookingDetailDto item)
        {
            if (item == null)
            {
                SelectedBookingInfoText.Text = "(Chưa chọn đơn nào)";
                BtnViewTickets.IsEnabled = false;
                BtnPayNow.IsEnabled = false;
                BtnCancelBooking.IsEnabled = false;
                return;
            }

            SelectedBookingInfoText.Text = $"{item.BookingCode} - {item.MovieTitle} ({item.FormattedTotal})";

            BtnViewTickets.IsEnabled = string.Equals(item.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) || item.TicketCount > 0;
            BtnPayNow.IsEnabled = item.CanPay;
            BtnCancelBooking.IsEnabled = item.CanCancel;
        }

        private void BtnViewTickets_Click(object sender, RoutedEventArgs e)
        {
            var selected = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (selected == null) return;

            var window = new TicketDetailWindow(selected.BookingId);
            window.ShowDialog();
        }

        private void BtnPayNow_Click(object sender, RoutedEventArgs e)
        {
            var selected = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (selected == null) return;

            decimal amount = selected.FinalAmount ?? selected.TotalAmount;
            var window = new MockPaymentWindow(selected.BookingId, selected.BookingCode, amount, SessionManager.CurrentUser);
            bool? res = window.ShowDialog();
            if (res == true)
            {
                LoadBookings();
            }
        }

        private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            var selected = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (selected == null) return;

            var res = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy đơn '{selected.BookingCode}' không?\nCác ghế đang giữ chỗ sẽ được giải phóng ngay.",
                "Xác nhận hủy đơn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            bool success = _bookingService.CancelPendingBooking(selected.BookingId, out string msg);
            MessageBox.Show(msg, "Kết quả", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            LoadBookings();
        }
    }
}

