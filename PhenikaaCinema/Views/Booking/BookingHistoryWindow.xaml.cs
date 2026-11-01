using System.Windows;
using PhenikaaCinema.Services;

namespace PhenikaaCinema.Views.Booking
{
    public partial class BookingHistoryWindow : Window
    {
        private readonly int? _customerUserId;

        public BookingHistoryWindow(int? customerUserId = null)
        {
            InitializeComponent();
            _customerUserId = customerUserId;
            Loaded += (s, e) =>
            {
                BookingsViewControl.SetUserId(_customerUserId);
            };
        }
    }
}
