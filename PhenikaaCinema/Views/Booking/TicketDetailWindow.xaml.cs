using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public partial class TicketDetailWindow : Window
    {
        private readonly int? _bookingId;
        private readonly string _ticketCode;
        private readonly TicketService _ticketService = new TicketService();

        public TicketDetailWindow(int bookingId)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _ticketCode = null;

            Loaded += (s, e) => LoadTickets();
        }

        public TicketDetailWindow(string ticketCode)
        {
            InitializeComponent();
            _bookingId = null;
            _ticketCode = ticketCode;

            Loaded += (s, e) => LoadTickets();
        }

        private void LoadTickets()
        {
            try
            {
                List<TicketDto> tickets = new List<TicketDto>();

                if (_bookingId.HasValue)
                {
                    tickets = _ticketService.GetTicketsByBookingId(_bookingId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(_ticketCode))
                {
                    var singleTicket = _ticketService.GetTicketByCode(_ticketCode);
                    if (singleTicket != null) tickets.Add(singleTicket);
                }

                RenderTickets(tickets);
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = "Lỗi khi tải thông tin vé: " + ex.Message;
                StatusMessageText.Visibility = Visibility.Visible;
            }
        }

        private void RenderTickets(List<TicketDto> tickets)
        {
            TicketsContainer.Children.Clear();

            if (tickets == null || tickets.Count == 0)
            {
                StatusMessageText.Text = "Không tìm thấy vé xem phim.";
                StatusMessageText.Visibility = Visibility.Visible;
                TicketsContainer.Children.Add(StatusMessageText);
                return;
            }

            foreach (var ticket in tickets)
            {
                TicketsContainer.Children.Add(CreateTicketCard(ticket));
            }
        }

        private FrameworkElement CreateTicketCard(TicketDto ticket)
        {
            var ticketBorder = new Border
            {
                Margin = new Thickness(0, 0, 0, 20),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 12, ShadowDepth = 2, Opacity = 0.08 }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) }); // Divider line
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            // LEFT SIDE: MOVIE & SHOWTIME INFO
            var leftBorder = new Border { Padding = new Thickness(25, 20, 25, 20) };
            var leftPanel = new StackPanel();
            leftBorder.Child = leftPanel;

            var movieTitle = new TextBlock
            {
                Text = ticket.MovieTitle,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            leftPanel.Children.Add(movieTitle);

            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var col1 = new StackPanel();
            col1.Children.Add(new TextBlock { Text = "SUẤT CHIẾU", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)) });
            col1.Children.Add(new TextBlock { Text = ticket.FormattedShowtime, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(229, 9, 20)), Margin = new Thickness(0, 2, 0, 10) });

            col1.Children.Add(new TextBlock { Text = "PHÒNG CHIẾU", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)) });
            col1.Children.Add(new TextBlock { Text = ticket.RoomName, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            Grid.SetColumn(col1, 0);
            infoGrid.Children.Add(col1);

            var col2 = new StackPanel();
            col2.Children.Add(new TextBlock { Text = "VỊ TRÍ GHẾ", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)) });
            col2.Children.Add(new TextBlock { Text = $"{ticket.SeatCode} ({ticket.SeatType})", FontSize = 16, FontWeight = FontWeights.Black, Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0)), Margin = new Thickness(0, 2, 0, 10) });

            col2.Children.Add(new TextBlock { Text = "GIÁ VÉ", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)) });
            col2.Children.Add(new TextBlock { Text = ticket.FormattedPrice, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            Grid.SetColumn(col2, 1);
            infoGrid.Children.Add(col2);

            leftPanel.Children.Add(infoGrid);
            Grid.SetColumn(leftBorder, 0);
            grid.Children.Add(leftBorder);

            // VERTICAL DIVIDER DASHED
            var divider = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Width = 1
            };
            Grid.SetColumn(divider, 1);
            grid.Children.Add(divider);

            // RIGHT SIDE: TICKET STUB / CODE
            var rightBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Padding = new Thickness(20)
            };
            var rightPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            rightBorder.Child = rightPanel;

            rightPanel.Children.Add(new TextBlock
            {
                Text = "PHENIKAA CINEMA",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(229, 9, 20)),
                TextAlignment = TextAlignment.Center
            });

            rightPanel.Children.Add(new TextBlock
            {
                Text = "MÃ VÉ ĐIỆN TỬ",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 2)
            });

            // Ticket code
            var codeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 36)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 8)
            };
            codeBorder.Child = new TextBlock
            {
                Text = ticket.TicketCode,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            rightPanel.Children.Add(codeBorder);

            // Status Badge
            var statusBadge = new Border
            {
                Background = ticket.IsUsed ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : new SolidColorBrush(Color.FromRgb(15, 45, 20)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            statusBadge.Child = new TextBlock
            {
                Text = ticket.StatusText,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = ticket.IsUsed ? Brushes.Gray : new SolidColorBrush(Color.FromRgb(76, 175, 80))
            };
            rightPanel.Children.Add(statusBadge);

            Grid.SetColumn(rightBorder, 2);
            grid.Children.Add(rightBorder);

            ticketBorder.Child = grid;
            return ticketBorder;
        }

        private void PrintTickets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(TicketsContainer, "Vé xem phim Phenikaa Cinema");
                    MessageBox.Show("Đã gửi lệnh in thành công!", "In vé", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in vé: " + ex.Message, "Lỗi in", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
