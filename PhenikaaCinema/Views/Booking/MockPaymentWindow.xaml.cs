using System;
using System.Windows;
using PhenikaaCinema.Services;

namespace PhenikaaCinema.Views.Booking
{
    public partial class MockPaymentWindow : Window
    {
        private readonly int _bookingId;
        private readonly string _bookingCode;
        private readonly decimal _amount;
        private readonly AuthUser _user;

        private readonly PaymentService _paymentService = new PaymentService();

        public MockPaymentWindow(int bookingId, AuthUser user = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _user = user ?? SessionManager.CurrentUser;

            var booking = new BookingService().GetBookingById(bookingId);
            if (booking != null)
            {
                _bookingCode = booking.BookingCode;
                _amount = booking.FinalAmount ?? booking.TotalAmount;
            }
            else
            {
                _bookingCode = $"BK{bookingId}";
                _amount = 0;
            }

            BookingCodeText.Text = _bookingCode;
            AmountText.Text = CurrencyHelper.FormatVnd(_amount);
        }

        public MockPaymentWindow(int bookingId, string bookingCode, decimal amount, AuthUser user = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _bookingCode = bookingCode;
            _amount = amount;
            _user = user ?? SessionManager.CurrentUser;

            BookingCodeText.Text = _bookingCode;
            AmountText.Text = CurrencyHelper.FormatVnd(_amount);
        }

        private void ConfirmPayButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPayButton.IsEnabled = false;
            ConfirmPayButton.Content = "ĐANG XỬ LÝ THANH TOÁN...";
            StatusMessage.Text = "";

            try
            {
                var result = _paymentService.ProcessMockPayment(_bookingId);

                if (!result.Success)
                {
                    StatusMessage.Text = result.Message;
                    ConfirmPayButton.IsEnabled = true;
                    ConfirmPayButton.Content = "XÁC NHẬN THANH TOÁN MÔ PHỎNG ✔";
                    return;
                }

                MessageBox.Show(
                    "Thanh toán thành công!\n" +
                    $"Mã giao dịch: {result.TransactionCode}\n" +
                    $"Số tiền đã thanh toán: {CurrencyHelper.FormatVnd(result.PaidAmount)}\n\n" +
                    "Hệ thống đã phát hành vé xem phim cho bạn.",
                    "Thanh toán thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Mở TicketDetailWindow
                var ticketWindow = new TicketDetailWindow(_bookingId)
                {
                    Owner = this.Owner
                };
                ticketWindow.Show();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = "Lỗi khi xử lý giao dịch: " + ex.Message;
                ConfirmPayButton.IsEnabled = true;
                ConfirmPayButton.Content = "XÁC NHẬN THANH TOÁN MÔ PHỎNG ✔";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
