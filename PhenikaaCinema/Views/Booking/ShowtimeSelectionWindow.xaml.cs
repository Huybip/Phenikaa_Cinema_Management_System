using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Booking
{
    public partial class ShowtimeSelectionWindow : Window
    {
        private readonly ShowtimeService _showtimeService = new ShowtimeService();
        private readonly MovieDto _movie;
        private readonly AuthUser _user;
        private readonly int? _targetCustomerUserId;
        private DateTime _selectedDate;
        private List<ShowtimeDto> _allShowtimes = new List<ShowtimeDto>();

        public ShowtimeSelectionWindow(MovieDto movie = null, AuthUser user = null, int? targetCustomerUserId = null)
        {
            InitializeComponent();
            _movie = movie;
            _user = user ?? SessionManager.CurrentUser;
            _targetCustomerUserId = targetCustomerUserId;

            _selectedDate = DateTime.Today;

            if (_movie != null)
            {
                MovieTitleText.Text = _movie.Title;
                MovieDurationText.Text = _movie.FormattedDuration;
                MovieGenreText.Text = _movie.DisplayGenres;
            }
            else
            {
                MovieTitleText.Text = "Lịch chiếu toàn bộ phim";
                MovieDurationText.Text = "Tất cả";
                MovieGenreText.Text = "Chọn suất chiếu để đặt vé";
            }

            BuildDateTabs();
            Loaded += (s, e) => LoadShowtimes();
        }

        public ShowtimeSelectionWindow(int movieId, AuthUser user = null, int? targetCustomerUserId = null)
            : this(new MovieService().GetMovieById(movieId), user, targetCustomerUserId)
        {
        }

        private void BuildDateTabs()
        {
            DateTabsPanel.Children.Clear();
            DateTime today = DateTime.Today;

            for (int i = 0; i < 7; i++)
            {
                DateTime day = today.AddDays(i);
                string dayLabel = i == 0 ? "Hôm nay" : (i == 1 ? "Ngày mai" : day.ToString("dd/MM"));
                string dayOfWeek = day.ToString("ddd");

                bool isSelected = day.Date == _selectedDate.Date;
                var btn = new Button
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    Padding = new Thickness(14, 8, 14, 8),
                    Background = isSelected ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : Brushes.White,
                    Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = day
                };

                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock { Text = dayOfWeek, FontSize = 11, Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = dayLabel, FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
                btn.Content = sp;

                btn.Click += (s, e) =>
                {
                    _selectedDate = (DateTime)((Button)s).Tag;
                    BuildDateTabs();
                    FilterShowtimes();
                };

                DateTabsPanel.Children.Add(btn);
            }
        }

        private void LoadShowtimes()
        {
            try
            {
                int? movieId = _movie?.MovieId;
                _allShowtimes = _showtimeService.GetAvailableShowtimes(movieId);
                FilterShowtimes();
            }
            catch (Exception ex)
            {
                EmptyShowtimeMessage.Text = "Lỗi tải lịch chiếu: " + ex.Message;
                EmptyShowtimeMessage.Visibility = Visibility.Visible;
            }
        }

        private void FilterShowtimes()
        {
            ShowtimeItemsControl.Items.Clear();

            var filtered = _allShowtimes.FindAll(st => st.StartTimeVn.Date == _selectedDate.Date);

            if (filtered.Count == 0)
            {
                EmptyShowtimeMessage.Text = $"Không có suất chiếu khả dụng cho ngày {_selectedDate:dd/MM/yyyy}.";
                EmptyShowtimeMessage.Visibility = Visibility.Visible;
                return;
            }

            EmptyShowtimeMessage.Visibility = Visibility.Collapsed;

            foreach (var st in filtered)
            {
                ShowtimeItemsControl.Items.Add(CreateShowtimeCard(st));
            }
        }

        private FrameworkElement CreateShowtimeCard(ShowtimeDto st)
        {
            var border = new Border
            {
                Width = 280,
                Margin = new Thickness(0, 0, 18, 18),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 12, ShadowDepth = 2, Opacity = 0.08 }
            };

            var sp = new StackPanel();

            if (_movie == null)
            {
                var titleBlock = new TextBlock
                {
                    Text = st.MovieTitle,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Margin = new Thickness(0, 0, 0, 8),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                sp.Children.Add(titleBlock);
            }

            // TIME ROW
            var timeRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            var timeBlock = new TextBlock
            {
                Text = st.StartTimeVn.ToString("HH:mm"),
                FontSize = 24,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var endTimeBlock = new TextBlock
            {
                Text = $"~ {st.EndTimeVn:HH:mm}",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(75, 0, 0, 4)
            };
            timeRow.Children.Add(timeBlock);
            timeRow.Children.Add(endTimeBlock);
            sp.Children.Add(timeRow);

            // ROOM & PRICE INFO
            var roomPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            roomPanel.Children.Add(new TextBlock { Text = "Phòng chiếu: ", Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)), FontSize = 12 });
            roomPanel.Children.Add(new TextBlock { Text = st.RoomName, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 12 });
            sp.Children.Add(roomPanel);

            var pricePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            pricePanel.Children.Add(new TextBlock { Text = "Giá vé từ: ", Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)), FontSize = 12 });
            pricePanel.Children.Add(new TextBlock { Text = st.FormattedPrice, Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0)), FontWeight = FontWeights.Bold, FontSize = 13 });
            sp.Children.Add(pricePanel);

            // SELECT BUTTON
            var btnSelect = new Button
            {
                Content = "CHỌN GHẾ NGỒI",
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(229, 9, 20)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSelect.Click += (s, e) => OpenSeatSelection(st);
            sp.Children.Add(btnSelect);

            border.Child = sp;
            return border;
        }

        private void OpenSeatSelection(ShowtimeDto showtime)
        {
            var seatWindow = new SeatSelectionWindow(showtime, _user, _targetCustomerUserId)
            {
                Owner = this
            };
            seatWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
