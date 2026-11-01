using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;

namespace PhenikaaCinema.Views.Customer
{
    public partial class MyTicketsView : UserControl
    {
        private int? _userId;
        private readonly TicketService _ticketService = new TicketService();
        private List<TicketDto> _allTickets = new List<TicketDto>();

        public MyTicketsView()
        {
            InitializeComponent();
            _userId = SessionManager.CurrentUserId;
            Loaded += MyTicketsView_Loaded;
        }

        public MyTicketsView(int? userId) : this()
        {
            _userId = userId ?? SessionManager.CurrentUserId;
        }

        public void SetUserId(int? userId)
        {
            _userId = userId;
            LoadTickets();
        }

        private void MyTicketsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTickets();
        }

        public void LoadTickets()
        {
            try
            {
                int? uid = _userId ?? SessionManager.CurrentUserId;
                if (!uid.HasValue)
                {
                    TicketSummaryText.Text = "Chưa đăng nhập.";
                    _allTickets = new List<TicketDto>();
                    FilterTickets();
                    return;
                }

                _allTickets = _ticketService.GetTicketsByCustomer(uid.Value);
                FilterTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách vé: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTickets()
        {
            // XAML can raise SelectionChanged while InitializeComponent is still building the view.
            if (TicketsItemsControl == null || EmptyTicketsText == null || TicketSummaryText == null)
                return;

            if (_allTickets == null) _allTickets = new List<TicketDto>();

            string keyword = SearchBox?.Text?.Trim() ?? "";
            string statusFilter = "";
            if (TicketSummaryText != null) // placeholder - no filter combobox in this view
                statusFilter = "";

            var filtered = _allTickets.FindAll(t =>
            {
                bool matchKeyword = string.IsNullOrWhiteSpace(keyword) ||
                    (t.TicketCode != null && t.TicketCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.MovieTitle != null && t.MovieTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.RoomName != null && t.RoomName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (t.SeatCode != null && t.SeatCode.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                bool matchStatus = true;
                if (statusFilter.Contains("Chưa sử dụng"))
                {
                    matchStatus = !t.IsUsed;
                }
                else if (statusFilter.Contains("Đã soát vé"))
                {
                    matchStatus = t.IsUsed;
                }

                return matchKeyword && matchStatus;
            });

            TicketsItemsControl.ItemsSource = filtered;
            EmptyTicketsText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TicketSummaryText.Text = $"Hiển thị {filtered.Count} / {_allTickets.Count} vé";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTickets();
        }

        private void TicketFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTickets();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTickets();
        }

        private void BtnViewSingleTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string code = btn.Tag.ToString();
                var window = new TicketDetailWindow(code);
                window.ShowDialog();
            }
        }
    }
}

