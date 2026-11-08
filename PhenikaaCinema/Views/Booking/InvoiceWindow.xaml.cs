using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public class InvoiceSeatItem
    {
        public int Index { get; set; }
        public string SeatLabel { get; set; }
        public string SeatType { get; set; }
        public string TicketCode { get; set; }
        public decimal Price { get; set; }
        public string FormattedPrice => CurrencyHelper.FormatVnd(Price);
    }

    public partial class InvoiceWindow : Window
    {
        private readonly int _bookingId;
        private readonly string _paymentMethod;
        private string _bookingCode = "";

        private readonly BookingService _bookingService = new BookingService();
        private readonly TicketService _ticketService = new TicketService();

        public InvoiceWindow(int bookingId, string paymentMethod = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _paymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cổng trực tuyến (VNPAY/MoMo)" : paymentMethod;

            Loaded += (s, e) => LoadInvoiceData();
        }

        private void LoadInvoiceData()
        {
            try
            {
                var booking = _bookingService.GetBookingById(_bookingId);
                if (booking == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin đơn hàng này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                _bookingCode = booking.BookingCode;

                TxtBookingCode.Text = booking.BookingCode;
                TxtCustomerName.Text = string.IsNullOrWhiteSpace(booking.CustomerName) ? (SessionManager.CurrentFullName ?? "Khách hàng") : booking.CustomerName;
                TxtCustomerPhone.Text = string.IsNullOrWhiteSpace(booking.CustomerPhone) ? "Chưa cập nhật" : booking.CustomerPhone;

                TxtPaymentDate.Text = booking.BookingTimeVn.ToString("dd/MM/yyyy HH:mm:ss");
                TxtPaymentMethod.Text = _paymentMethod;

                TxtMovieTitle.Text = booking.MovieTitle ?? "Phim chiếu rạp";
                TxtRoomName.Text = booking.RoomName ?? "Phòng chiếu tiêu chuẩn";

                DateTime startVn = booking.StartTimeVn;
                DateTime endVn = DateTimeHelper.ToVietnamTime(booking.EndTimeUtc);
                TxtShowtime.Text = $"{startVn:HH:mm} - {endVn:HH:mm} • {startVn:dd/MM/yyyy}";

                // Load Tickets / Seats
                var tickets = _ticketService.GetTicketsByBookingId(_bookingId);
                var seatItems = new List<InvoiceSeatItem>();

                int idx = 1;
                foreach (var t in tickets)
                {
                    seatItems.Add(new InvoiceSeatItem
                    {
                        Index = idx++,
                        SeatLabel = "Ghế " + t.SeatCode,
                        SeatType = t.SeatType ?? "Thường",
                        TicketCode = t.TicketCode,
                        Price = t.SeatPrice
                    });
                }

                InvoiceSeatsList.ItemsSource = seatItems;

                // Financials
                decimal subtotal = booking.TotalAmount;
                decimal discount = booking.DiscountAmount;
                decimal finalTotal = booking.FinalAmount ?? (subtotal - discount);

                TxtSubTotal.Text = CurrencyHelper.FormatVnd(subtotal);
                TxtDiscount.Text = discount > 0 ? $"-{CurrencyHelper.FormatVnd(discount)}" : "0 ₫";
                TxtFinalTotal.Text = CurrencyHelper.FormatVnd(finalTotal);

                TxtAmountInWords.Text = NumberToVietnameseWords((long)finalTotal);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thông tin hóa đơn: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Đặt lề và in trực tiếp visual InvoicePaper
                    printDialog.PrintVisual(InvoicePaper, $"HoaDon_{_bookingCode}");
                    MessageBox.Show("Đã gửi lệnh in hóa đơn thành công!", "In hóa đơn", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in hóa đơn: " + ex.Message, "Lỗi in", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Vietnamese Number to Words
        private static readonly string[] ChuSo = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static readonly string[] Tien = { "", " nghìn", " triệu", " tỷ", " nghìn tỷ", " triệu tỷ" };

        private static string DocSo3ChuSo(int baso)
        {
            int tram = baso / 100;
            int chuc = (baso % 100) / 10;
            int donvi = baso % 10;
            string ketQua = "";

            if (tram == 0 && chuc == 0 && donvi == 0) return "";

            if (tram != 0)
            {
                ketQua += ChuSo[tram] + " trăm ";
                if (chuc == 0 && donvi != 0) ketQua += "lẻ ";
            }

            if (chuc != 0 && chuc != 1)
            {
                ketQua += ChuSo[chuc] + " mươi";
                if (chuc == 0 && donvi != 0) ketQua += " linh ";
            }

            if (chuc == 1) ketQua += "mười";

            switch (donvi)
            {
                case 1:
                    if (chuc != 0 && chuc != 1) ketQua += " mốt";
                    else ketQua += " một";
                    break;
                case 5:
                    if (chuc == 0) ketQua += " năm";
                    else ketQua += " lăm";
                    break;
                default:
                    if (donvi != 0) ketQua += " " + ChuSo[donvi];
                    break;
            }

            return ketQua.Trim();
        }

        public static string NumberToVietnameseWords(long soTien)
        {
            if (soTien <= 0) return "Không đồng.";

            long so = soTien;
            string ketQua = "";
            var viTri = new List<int>();

            while (so > 0)
            {
                viTri.Add((int)(so % 1000));
                so /= 1000;
            }

            for (int i = viTri.Count - 1; i >= 0; i--)
            {
                string docBaSo = DocSo3ChuSo(viTri[i]);
                if (!string.IsNullOrWhiteSpace(docBaSo))
                {
                    ketQua += docBaSo + Tien[i] + ", ";
                }
            }

            ketQua = ketQua.Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(ketQua)) return "Không đồng.";

            ketQua = char.ToUpper(ketQua[0]) + ketQua.Substring(1) + " đồng chẵn.";
            return ketQua;
        }
        #endregion
    }
}
