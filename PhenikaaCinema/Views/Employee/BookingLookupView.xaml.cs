using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;

namespace PhenikaaCinema.Views.Employee
{
    public partial class BookingLookupView : UserControl
    {
        private readonly BookingService _bookingService = new BookingService();
        private List<BookingDetailDto> _allBookings = new List<BookingDetailDto>();

        public BookingLookupView()
        {
            InitializeComponent();
            Loaded += BookingLookupView_Loaded;
        }

        private void BookingLookupView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                _allBookings = _bookingService.GetAllBookings();
                FilterData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách đặt vé: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterData()
        {
            if (BookingsDataGrid == null || EmptyText == null || RecordCountText == null ||
                SelectedBookingInfoText == null || BtnConfirmPayment == null ||
                BtnViewTickets == null || BtnCancelBooking == null)
            {
                return;
            }

            if (_allBookings == null) _allBookings = new List<BookingDetailDto>();

            string keyword = SearchBox?.Text?.Trim() ?? "";
            string statusFilter = (StatusFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            var filtered = _allBookings.FindAll(b =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                    (b.BookingCode != null && b.BookingCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.CustomerName != null && b.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.CustomerPhone != null && b.CustomerPhone.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.CustomerEmail != null && b.CustomerEmail.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (b.MovieTitle != null && b.MovieTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

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
            EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RecordCountText.Text = $"Hiển thị {filtered.Count} / {_allBookings.Count} đơn đặt vé";

            UpdateButtons(null);
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
            LoadData();
        }

        private void BookingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = BookingsDataGrid.SelectedItem as BookingDetailDto;
            UpdateButtons(item);
        }

        private void UpdateButtons(BookingDetailDto item)
        {
            if (item == null)
            {
                SelectedBookingInfoText.Text = "(Chưa chọn đơn nào)";
                BtnConfirmPayment.IsEnabled = false;
                BtnViewTickets.IsEnabled = false;
                BtnCancelBooking.IsEnabled = false;
                return;
            }

            string cust = string.IsNullOrWhiteSpace(item.CustomerName) ? "Khách vãng lai" : item.CustomerName;
            SelectedBookingInfoText.Text = $"{item.BookingCode} | {cust} | {item.MovieTitle} ({item.FormattedTotal})";

            BtnConfirmPayment.IsEnabled = string.Equals(item.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase);
            BtnViewTickets.IsEnabled = string.Equals(item.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) || item.TicketCount > 0;
            BtnCancelBooking.IsEnabled = string.Equals(item.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase);
        }

        private void BtnConfirmPayment_Click(object sender, RoutedEventArgs e)
        {
            var item = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (item == null) return;

            var res = MessageBox.Show(
                $"Xác nhận thu tiền tại quầy cho đơn '{item.BookingCode}'?\nSố tiền: {item.FormattedTotal}",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            var payResult = _bookingService.ConfirmMockPayment(item.BookingId);
            if (payResult.Success)
            {
                MessageBox.Show($"Xác nhận thanh toán thành công!\nĐã phát hành {payResult.Tickets.Count} vé.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
                var ticketWin = new TicketDetailWindow(item.BookingId);
                ticketWin.ShowDialog();
            }
            else
            {
                MessageBox.Show("Lỗi thanh toán: " + payResult.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewTickets_Click(object sender, RoutedEventArgs e)
        {
            var item = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (item == null) return;

            var ticketWin = new TicketDetailWindow(item.BookingId);
            ticketWin.ShowDialog();
        }

        private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            var item = BookingsDataGrid.SelectedItem as BookingDetailDto;
            if (item == null) return;

            var res = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy đơn '{item.BookingCode}' không?",
                "Xác nhận hủy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            bool success = _bookingService.CancelPendingBooking(item.BookingId, out string msg);
            MessageBox.Show(msg, "Kết quả", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            LoadData();
        }
    }
}

