using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhenikaaCinema.Services;
using PhenikaaCinema.Views.Booking;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Employee
{
    public partial class SellTicketView : UserControl
    {
        private AuthUser _user;
        private readonly MovieService _movieService = new MovieService();
        private readonly ShowtimeService _showtimeService = new ShowtimeService();
        private readonly SeatService _seatService = new SeatService();
        private readonly PromotionService _promotionService = new PromotionService();
        private readonly BookingService _bookingService = new BookingService();

        private MovieDto _selectedMovie;
        private ShowtimeDto _selectedShowtime;
        private List<SeatDto> _availableSeats = new List<SeatDto>();
        private readonly List<SeatDto> _selectedSeats = new List<SeatDto>();
        private int? _customerUserId = null;
        private decimal _discountAmount = 0;

        public SellTicketView()
        {
            InitializeComponent();
            WalkInCustomerCheckBox.IsChecked = true;
            _user = SessionManager.CurrentUser;
            Loaded += SellTicketView_Loaded;
        }

        public SellTicketView(AuthUser user) : this()
        {
            _user = user ?? SessionManager.CurrentUser;
        }

        private void SellTicketView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMovies();
        }

        private void LoadMovies()
        {
            try
            {
                var movies = _movieService.GetNowShowingMovies();
                if (movies == null || movies.Count == 0)
                {
                    movies = _movieService.GetAllMovies();
                }

                MovieComboBox.ItemsSource = movies;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách phim: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MovieComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMovie = MovieComboBox.SelectedItem as MovieDto;
            _selectedShowtime = null;
            _selectedSeats.Clear();
            _availableSeats.Clear();
            QuickSeatsWrapPanel.Children.Clear();
            UpdatePriceCalculation();

            if (_selectedMovie != null)
            {
                MovieDetailText.Text = $"Thời lượng: {_selectedMovie.DurationMinutes} phút | Trạng thái: {_selectedMovie.Status}";
                try
                {
                    var showtimes = _showtimeService.GetShowtimesByMovie(_selectedMovie.MovieId);
                    ShowtimeComboBox.ItemsSource = showtimes;
                    if (showtimes.Count == 0)
                    {
                        ShowtimeDetailText.Text = "Phim này hiện chưa có lịch chiếu khả dụng.";
                    }
                    else
                    {
                        ShowtimeDetailText.Text = $"Có {showtimes.Count} suất chiếu.";
                    }
                }
                catch (Exception ex)
                {
                    ShowtimeDetailText.Text = "Lỗi tải suất chiếu: " + ex.Message;
                }
            }
            else
            {
                MovieDetailText.Text = "Vui lòng chọn phim để tiếp tục";
                ShowtimeComboBox.ItemsSource = null;
                ShowtimeDetailText.Text = "Vui lòng chọn suất chiếu";
            }
        }

        private void ShowtimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedShowtime = ShowtimeComboBox.SelectedItem as ShowtimeDto;
            _selectedSeats.Clear();
            QuickSeatsWrapPanel.Children.Clear();
            UpdatePriceCalculation();

            if (_selectedShowtime != null)
            {
                ShowtimeDetailText.Text = $"Phòng: {_selectedShowtime.RoomName} | Bắt đầu: {_selectedShowtime.FormattedTimeRange} | Giá gốc: {CurrencyHelper.FormatVnd(_selectedShowtime.BasePrice)}";
                LoadSeatsForShowtime();
            }
            else
            {
                ShowtimeDetailText.Text = "Vui lòng chọn suất chiếu";
            }
        }

        private void LoadSeatsForShowtime()
        {
            if (_selectedShowtime == null) return;

            try
            {
                _availableSeats = _seatService.GetSeatsForShowtime(_selectedShowtime.ShowtimeId, _selectedShowtime.RoomId, _selectedShowtime.BasePrice);

                QuickSeatsWrapPanel.Children.Clear();
                foreach (var seat in _availableSeats)
                {
                    var btn = new Button
                    {
                        Content = seat.SeatCode,
                        Width = 48,
                        Height = 36,
                        Margin = new Thickness(4),
                        Tag = seat,
                        FontWeight = FontWeights.Bold,
                        FontSize = 11,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    if (!seat.IsAvailable)
                    {
                        btn.IsEnabled = false;
                        btn.Background = new SolidColorBrush(Color.FromRgb(220, 224, 230));
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(160, 165, 175));
                    }
                    else
                    {
                        btn.Background = seat.SeatType == "VIP" ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) : new SolidColorBrush(Color.FromRgb(240, 244, 248));
                        btn.Foreground = seat.SeatType == "VIP" ? new SolidColorBrush(Color.FromRgb(180, 83, 9)) : new SolidColorBrush(Color.FromRgb(21, 50, 75));
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                        btn.BorderThickness = new Thickness(1);
                        btn.Click += QuickSeatButton_Click;
                    }

                    QuickSeatsWrapPanel.Children.Add(btn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách ghế: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickSeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SeatDto seat)
            {
                if (_selectedSeats.Contains(seat))
                {
                    _selectedSeats.Remove(seat);
                    btn.Background = seat.SeatType == "VIP" ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) : new SolidColorBrush(Color.FromRgb(240, 244, 248));
                    btn.Foreground = seat.SeatType == "VIP" ? new SolidColorBrush(Color.FromRgb(180, 83, 9)) : new SolidColorBrush(Color.FromRgb(21, 50, 75));
                }
                else
                {
                    _selectedSeats.Add(seat);
                    btn.Background = new SolidColorBrush(Color.FromRgb(245, 130, 32));
                    btn.Foreground = Brushes.White;
                }

                UpdatePriceCalculation();
            }
        }

        private void UpdatePriceCalculation()
        {
            if (_selectedSeats.Count == 0)
            {
                SelectedSeatsSummaryText.Text = "Chưa chọn ghế nào";
                SelectedSeatsCountText.Text = "Số lượng: 0 ghế";
                SubTotalText.Text = "0 đ";
                FinalTotalText.Text = "0 đ";
                _discountAmount = 0;
                DiscountText.Text = "0 đ";
                CalculateChange();
                return;
            }

            string seatCodes = string.Join(", ", _selectedSeats.Select(s => s.SeatCode));
            decimal subtotal = _selectedSeats.Sum(s => s.Price);

            SelectedSeatsSummaryText.Text = seatCodes;
            SelectedSeatsCountText.Text = $"Số lượng: {_selectedSeats.Count} ghế";
            SubTotalText.Text = CurrencyHelper.FormatVnd(subtotal);

            decimal finalAmount = Math.Max(0, subtotal - _discountAmount);
            FinalTotalText.Text = CurrencyHelper.FormatVnd(finalAmount);

            CalculateChange();
        }

        private void CalculateChange()
        {
            decimal subtotal = _selectedSeats.Sum(s => s.Price);
            decimal finalAmount = Math.Max(0, subtotal - _discountAmount);

            string cashText = CashGivenBox?.Text?.Trim() ?? "";
            if (decimal.TryParse(cashText, out decimal cashGiven))
            {
                decimal change = cashGiven - finalAmount;
                if (change >= 0)
                {
                    ChangeReturnText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    ChangeReturnText.Text = $"Tiền thừa trả khách: {CurrencyHelper.FormatVnd(change)}";
                }
                else
                {
                    ChangeReturnText.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    ChangeReturnText.Text = $"Còn thiếu: {CurrencyHelper.FormatVnd(Math.Abs(change))}";
                }
            }
            else
            {
                ChangeReturnText.Text = "Tiền thừa trả khách: 0 đ";
            }
        }

        private void CashGivenBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateChange();
        }

        private void WalkInCustomerCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isWalkIn = WalkInCustomerCheckBox.IsChecked == true;
            CustomerInfoPanel.Visibility = isWalkIn ? Visibility.Collapsed : Visibility.Visible;
            if (isWalkIn)
            {
                _customerUserId = null;
                CustomerFoundText.Text = "";
            }
        }

        private void BtnLookupCustomer_Click(object sender, RoutedEventArgs e)
        {
            string keyword = CustomerLookupBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                CustomerFoundText.Foreground = Brushes.Red;
                CustomerFoundText.Text = "Vui lòng nhập email hoặc SĐT";
                return;
            }

            try
            {
                string sql = "SELECT UserId, FullName, Email, Phone FROM dbo.Users WHERE Email = @Kw OR Phone = @Kw";
                var pKw = new SqlParameter("@Kw", SqlDbType.NVarChar, 100) { Value = keyword };
                var dt = DatabaseHelper.ExecuteQuery(sql, pKw);
                if (dt.Rows.Count > 0)
                {
                    _customerUserId = Convert.ToInt32(dt.Rows[0]["UserId"]);
                    string name = dt.Rows[0]["FullName"].ToString();
                    CustomerFoundText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    CustomerFoundText.Text = $"✓ Tìm thấy: {name} (ID: {_customerUserId})";
                }
                else
                {
                    _customerUserId = null;
                    CustomerFoundText.Foreground = Brushes.Red;
                    CustomerFoundText.Text = "Không tìm thấy khách hàng.";
                }
            }
            catch (Exception ex)
            {
                CustomerFoundText.Foreground = Brushes.Red;
                CustomerFoundText.Text = "Lỗi: " + ex.Message;
            }
        }

        private void BtnApplyPromo_Click(object sender, RoutedEventArgs e)
        {
            string code = PromoCodeBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                _discountAmount = 0;
                PromoStatusText.Text = "";
                UpdatePriceCalculation();
                return;
            }

            decimal subtotal = _selectedSeats.Sum(s => s.Price);
            var result = _promotionService.ValidateAndCalculateDiscount(code, subtotal);
            if (result.IsValid)
            {
                _discountAmount = result.DiscountAmount;
                PromoStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                PromoStatusText.Text = $"✓ Áp dụng: {result.Message} (-{CurrencyHelper.FormatVnd(_discountAmount)})";
                DiscountText.Text = $"-{CurrencyHelper.FormatVnd(_discountAmount)}";
            }
            else
            {
                _discountAmount = 0;
                PromoStatusText.Foreground = Brushes.Red;
                PromoStatusText.Text = result.Message;
                DiscountText.Text = "0 đ";
            }

            UpdatePriceCalculation();
        }

        private void BtnOpenSeatSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShowtime == null)
            {
                MessageBox.Show("Vui lòng chọn suất chiếu trước khi mở sơ đồ ghế.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new SeatSelectionWindow(_selectedShowtime, _user);
            win.ShowDialog();
            LoadSeatsForShowtime();
        }

        private void BtnConfirmSell_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShowtime == null)
            {
                MessageBox.Show("Vui lòng chọn phim và suất chiếu.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedSeats.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 ghế để bán.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int employeeId = _user?.UserId ?? SessionManager.CurrentUserId ?? 1;
            var seatIds = _selectedSeats.Select(s => s.SeatId).ToList();

            var holdResult = _bookingService.CreateBookingHold(_selectedShowtime.ShowtimeId, employeeId, _customerUserId, seatIds, 15);
            if (!holdResult.Success)
            {
                MessageBox.Show("Không thể giữ chỗ: " + holdResult.Message, "Lỗi bán vé", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var payResult = _bookingService.ConfirmMockPayment(holdResult.BookingId);
            if (!payResult.Success)
            {
                MessageBox.Show("Lỗi xác nhận thanh toán: " + payResult.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult printChoice = MessageBox.Show(
                $"Bán vé thành công!\nMã đơn: {payResult.BookingCode}\nSố lượng vé: {payResult.Tickets.Count}\nTổng tiền: {CurrencyHelper.FormatVnd(payResult.PaidAmount)}\n\nBạn có muốn IN HÓA ĐƠN cho khách hàng ngay bây giờ không?",
                "Bán vé thành công",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (printChoice == MessageBoxResult.Yes)
            {
                var invoiceWin = new PhenikaaCinema.Views.Booking.InvoiceWindow(payResult.BookingId, "Tiền mặt tại quầy")
                {
                    Owner = Window.GetWindow(this)
                };
                invoiceWin.ShowDialog();
            }

            var ticketWin = new TicketDetailWindow(payResult.BookingId);
            ticketWin.ShowDialog();

            BtnReset_Click(null, null);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            MovieComboBox.SelectedItem = null;
            ShowtimeComboBox.SelectedItem = null;
            _selectedMovie = null;
            _selectedShowtime = null;
            _selectedSeats.Clear();
            _availableSeats.Clear();
            QuickSeatsWrapPanel.Children.Clear();
            _discountAmount = 0;
            _customerUserId = null;
            CustomerLookupBox.Clear();
            CustomerFoundText.Text = "";
            PromoCodeBox.Clear();
            PromoStatusText.Text = "";
            CashGivenBox.Clear();
            WalkInCustomerCheckBox.IsChecked = true;
            UpdatePriceCalculation();
        }
    }
}

