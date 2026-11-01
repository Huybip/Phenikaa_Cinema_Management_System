using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public partial class BookingConfirmationWindow : Window
    {
        private readonly BookingHoldResult _holdResult;
        private readonly ShowtimeDto _showtime;
        private readonly List<SeatDto> _seats;
        private readonly AuthUser _user;

        private readonly BookingService _bookingService = new BookingService();
        private readonly PromotionService _promotionService = new PromotionService();

        private DispatcherTimer _timer;
        private decimal _discountAmount = 0;
        private decimal _finalAmount;
        private int? _appliedPromotionId;

        public BookingConfirmationWindow(BookingHoldResult holdResult, ShowtimeDto showtime, List<SeatDto> seats, AuthUser user)
        {
            InitializeComponent();
            _holdResult = holdResult ?? throw new ArgumentNullException(nameof(holdResult));
            _showtime = showtime ?? throw new ArgumentNullException(nameof(showtime));
            _seats = seats ?? new List<SeatDto>();
            _user = user;

            _finalAmount = _holdResult.TotalAmount;

            InitDisplay();
            StartTimer();
        }

        private void InitDisplay()
        {
            BookingCodeText.Text = _holdResult.BookingCode;
            MovieTitleText.Text = _showtime.MovieTitle;
            MovieDetailText.Text = $"{_showtime.MovieDurationMinutes} phút";
            ShowtimeText.Text = $"{_showtime.FormattedTimeRange} ({_showtime.FormattedDate})";
            RoomNameText.Text = _showtime.RoomName;

            SubtotalText.Text = CurrencyHelper.FormatVnd(_holdResult.TotalAmount);
            DiscountText.Text = "- 0 ₫";
            FinalTotalText.Text = CurrencyHelper.FormatVnd(_finalAmount);

            // Render danh sách ghế
            SeatsItemsControl.Items.Clear();
            foreach (var seat in _seats)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var seatBlock = new TextBlock
                {
                    Text = $"Ghế {seat.SeatCode}",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13
                };
                Grid.SetColumn(seatBlock, 0);

                var typeBlock = new TextBlock
                {
                    Text = $"Loại: {seat.SeatType}",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 13
                };
                Grid.SetColumn(typeBlock, 1);

                var priceBlock = new TextBlock
                {
                    Text = CurrencyHelper.FormatVnd(seat.CalculatedPrice),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0)),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(priceBlock, 2);

                grid.Children.Add(seatBlock);
                grid.Children.Add(typeBlock);
                grid.Children.Add(priceBlock);

                SeatsItemsControl.Items.Add(grid);
            }
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            var remaining = _holdResult.HoldExpiresAtUtc - DateTime.UtcNow;

            if (remaining.TotalSeconds <= 0)
            {
                _timer.Stop();
                CountdownTimerText.Text = "00:00 (HẾT HẠN)";
                CountdownTimerText.Foreground = Brushes.Gray;
                StatusBadgeText.Text = "HẾT HẠN";
                PayNowButton.IsEnabled = false;

                MessageBox.Show(
                    "Thời gian giữ ghế cho đơn hàng này đã hết hạn. Ghế đã được tự động giải phóng.",
                    "Hết hạn giữ chỗ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            CountdownTimerText.Text = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
        }

        private void ApplyPromo_Click(object sender, RoutedEventArgs e)
        {
            string code = PromoCodeInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Vui lòng nhập mã khuyến mãi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var promoResult = _promotionService.ValidateAndCalculateDiscount(code, _holdResult.TotalAmount);
            if (!promoResult.Success)
            {
                PromoMessageText.Text = promoResult.Message;
                PromoMessageText.Foreground = Brushes.IndianRed;
                PromoMessageText.Visibility = Visibility.Visible;
                return;
            }

            _discountAmount = promoResult.DiscountAmount;
            _finalAmount = promoResult.FinalAmount;
            _appliedPromotionId = promoResult.PromotionId;

            // Cập nhật DiscountAmount vào Booking DB nếu cần
            try
            {
                string sqlUpdate = @"
                    UPDATE dbo.Bookings
                    SET DiscountAmount = @Discount,
                        FinalAmount = @Final
                    WHERE BookingId = @BookingId";

                var pDiscount = new System.Data.SqlClient.SqlParameter("@Discount", System.Data.SqlDbType.Decimal) { Value = _discountAmount };
                var pFinal = new System.Data.SqlClient.SqlParameter("@Final", System.Data.SqlDbType.Decimal) { Value = _finalAmount };
                var pId = new System.Data.SqlClient.SqlParameter("@BookingId", System.Data.SqlDbType.Int) { Value = _holdResult.BookingId };

                DatabaseHelper.ExecuteNonQuery(sqlUpdate, pDiscount, pFinal, pId);
            }
            catch { }

            DiscountText.Text = $"- {CurrencyHelper.FormatVnd(_discountAmount)}";
            FinalTotalText.Text = CurrencyHelper.FormatVnd(_finalAmount);

            PromoMessageText.Text = promoResult.Message;
            PromoMessageText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            PromoMessageText.Visibility = Visibility.Visible;
        }

        private void PayNow_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.UtcNow >= _holdResult.HoldExpiresAtUtc)
            {
                MessageBox.Show("Thời gian giữ ghế đã hết hạn.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var paymentWindow = new MockPaymentWindow(_holdResult.BookingId, _holdResult.BookingCode, _finalAmount, _user)
            {
                Owner = this
            };

            bool? paid = paymentWindow.ShowDialog();
            if (paid == true)
            {
                _timer?.Stop();
                DialogResult = true;
                Close();
            }
        }

        private void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn hủy đơn đặt vé này không? Ghế sẽ được giải phóng ngay lập tức.",
                "Xác nhận hủy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _timer?.Stop();

            bool cancelled = _bookingService.CancelPendingBooking(_holdResult.BookingId, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, cancelled ? MessageBoxImage.Information : MessageBoxImage.Warning);

            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
        }
    }
}
