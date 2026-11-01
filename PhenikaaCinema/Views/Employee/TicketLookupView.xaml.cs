using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Employee
{
    public partial class TicketLookupView : UserControl
    {
        private readonly TicketService _ticketService = new TicketService();
        private List<TicketDto> _allTickets = new List<TicketDto>();
        private bool _isViewInitialized;

        public TicketLookupView()
        {
            InitializeComponent();
            _isViewInitialized = true;
            FilterTickets();
            Loaded += TicketLookupView_Loaded;
        }

        private void TicketLookupView_Loaded(object sender, RoutedEventArgs e)
        {
            QuickScanBox.Focus();
        }

        private void QuickScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnQuickSearch_Click(sender, null);
            }
        }

        private void BtnQuickSearch_Click(object sender, RoutedEventArgs e)
        {
            string code = QuickScanBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowQuickResult(false, "Vui lòng nhập mã vé", "");
                return;
            }

            try
            {
                var ticket = _ticketService.GetTicketByCode(code);
                if (ticket == null)
                {
                    ShowQuickResult(false, "Không tìm thấy vé", $"Mã vé '{code}' không tồn tại trong hệ thống.");
                }
                else if (ticket.IsUsed)
                {
                    ShowQuickResult(null, "Vé đã được soát trước đó",
                        $"Phim: {ticket.MovieTitle} | Ghế: {ticket.SeatCode} | Phòng: {ticket.RoomName} | Suất: {ticket.FormattedShowtime}");
                }
                else
                {
                    ShowQuickResult(true, "Vé hợp lệ - Chưa sử dụng",
                        $"Phim: {ticket.MovieTitle} | Ghế: {ticket.SeatCode} | Phòng: {ticket.RoomName} | Suất: {ticket.FormattedShowtime}");
                }
            }
            catch (Exception ex)
            {
                ShowQuickResult(false, "Lỗi tra cứu", ex.Message);
            }
        }

        private void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            string code = QuickScanBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowQuickResult(false, "Vui lòng nhập mã vé để soát", "");
                return;
            }

            var confirm = MessageBox.Show($"Xác nhận soát vé:\nMã vé: {code}",
                "Xác nhận soát vé", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = _ticketService.CheckInTicket(code, out string msg);
                ShowQuickResult(success, success ? "✅ SOÁT VÉ THÀNH CÔNG" : "❌ Soát vé thất bại", msg);
                if (success) QuickScanBox.Clear();
            }
            catch (Exception ex)
            {
                ShowQuickResult(false, "Lỗi hệ thống", ex.Message);
            }
        }

        private void ShowQuickResult(bool? isSuccess, string title, string detail)
        {
            QuickResultBanner.Visibility = Visibility.Visible;

            if (isSuccess == true)
            {
                QuickResultBanner.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                QuickResultBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(165, 214, 167));
                QuickResultIcon.Text = "✅";
                QuickResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
            }
            else if (isSuccess == false)
            {
                QuickResultBanner.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                QuickResultBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 154, 154));
                QuickResultIcon.Text = "❌";
                QuickResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28));
            }
            else
            {
                QuickResultBanner.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                QuickResultBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 204, 128));
                QuickResultIcon.Text = "⚠️";
                QuickResultTitle.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
            }

            QuickResultTitle.Text = title;
            QuickResultDetail.Text = detail;
            QuickResultDetail.Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81));
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTickets();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTickets();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string code = QuickScanBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(code))
            {
                LoadByCode(code);
            }
            else
            {
                _allTickets = new List<TicketDto>();
                FilterTickets();
            }
        }

        private void LoadByCode(string code)
        {
            try
            {
                var ticket = _ticketService.GetTicketByCode(code);
                _allTickets = ticket != null ? new List<TicketDto> { ticket } : new List<TicketDto>();
                FilterTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tra cứu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTickets()
        {
            if (!_isViewInitialized) return;

            if (_allTickets == null) _allTickets = new List<TicketDto>();

            string keyword = SearchBox?.Text?.Trim() ?? "";
            string statusFilter = (StatusFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            var filtered = _allTickets.FindAll(t =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                    (t.TicketCode != null && t.TicketCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.MovieTitle != null && t.MovieTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.SeatCode != null && t.SeatCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                bool matchStatus = true;
                if (statusFilter.Contains("Chưa soát")) matchStatus = !t.IsUsed;
                else if (statusFilter.Contains("Đã soát")) matchStatus = t.IsUsed;

                return matchKeyword && matchStatus;
            });

            TicketsDataGrid.ItemsSource = filtered;
            TicketCountText.Text = $"{filtered.Count} vé";
            UpdateActionButtons(null);
        }

        private void TicketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = TicketsDataGrid.SelectedItem as TicketDto;
            UpdateActionButtons(item);
        }

        private void UpdateActionButtons(TicketDto item)
        {
            if (item == null)
            {
                SelectedTicketInfoText.Text = "Chọn vé trong bảng để soát";
                BtnCheckInSelected.IsEnabled = false;
                BtnViewDetail.IsEnabled = false;
                return;
            }

            SelectedTicketInfoText.Text = $"{item.TicketCode} | {item.MovieTitle} | Ghế {item.SeatCode} | {item.StatusText}";
            BtnCheckInSelected.IsEnabled = !item.IsUsed;
            BtnViewDetail.IsEnabled = true;
        }

        private void BtnCheckInSelected_Click(object sender, RoutedEventArgs e)
        {
            var item = TicketsDataGrid.SelectedItem as TicketDto;
            if (item == null) return;

            var confirm = MessageBox.Show($"Soát vé: {item.TicketCode}\nPhim: {item.MovieTitle} | Ghế: {item.SeatCode}",
                "Xác nhận soát vé", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            bool success = _ticketService.CheckInTicket(item.TicketCode, out string msg);
            MessageBox.Show(msg, "Kết quả soát vé", MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            if (success) FilterTickets();
        }

        private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            var item = TicketsDataGrid.SelectedItem as TicketDto;
            if (item == null) return;

            var win = new TicketDetailWindow(item.TicketCode);
            win.ShowDialog();
        }
    }
}

