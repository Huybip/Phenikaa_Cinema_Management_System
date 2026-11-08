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

                if (_allShowtimes.Count == 0)
                {
                    // Thử lấy không lọc ngày để kiểm tra dữ liệu
                    EmptyShowtimeMessage.Text = $"Không tìm thấy suất chiếu nào trong database (kiểm tra Status='Scheduled' và StartTimeUtc >= hôm nay UTC).";
                    EmptyShowtimeMessage.Visibility = Visibility.Visible;
                    return;
                }

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
                string msg = $"Không có suất chiếu cho ngày {_selectedDate:dd/MM/yyyy}.";
                if (_allShowtimes.Count > 0)
                {
                    // Gợi ý ngày có suất chiếu
                    var availableDates = new System.Collections.Generic.HashSet<string>();
                    foreach (var st in _allShowtimes) availableDates.Add(st.StartTimeVn.ToString("dd/MM/yyyy"));
                    msg += $"\n(Có suất chiếu vào ngày: {string.Join(", ", availableDates)})";
                }
                EmptyShowtimeMessage.Text = msg;
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
            bool isPast = st.StartTimeUtc <= DateTime.UtcNow;

            var border = new Border
            {
                Width = 290,
                Margin = new Thickness(0, 0, 18, 18),
                Background = isPast ? new SolidColorBrush(Color.FromRgb(248, 250, 252)) : Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black, BlurRadius = 12, ShadowDepth = 2, Opacity = 0.07
                }
            };

            var sp = new StackPanel();

            // Tiêu đề phim (nếu không có phim cụ thể)
            if (_movie == null)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = st.MovieTitle,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Margin = new Thickness(0, 0, 0, 10),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            // --- THỜI GIAN ---
            var timeGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var timeBlock = new TextBlock
            {
                Text = st.StartTimeVn.ToString("HH:mm"),
                FontSize = 32,
                FontWeight = FontWeights.Black,
                Foreground = isPast
                    ? new SolidColorBrush(Color.FromRgb(148, 163, 184))
                    : new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeBlock, 0);
            timeGrid.Children.Add(timeBlock);

            var endSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            endSp.Children.Add(new TextBlock
            {
                Text = $"đến {st.EndTimeVn:HH:mm}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
            });
            endSp.Children.Add(new TextBlock
            {
                Text = st.StartTimeVn.ToString("dd/MM/yyyy"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            });
            Grid.SetColumn(endSp, 1);
            timeGrid.Children.Add(endSp);
            sp.Children.Add(timeGrid);

            // --- ĐƯỜNG KẺ NGĂN CÁCH ---
            sp.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 0, 0, 12)
            });

            // --- PHÒNG CHIẾU ---
            var roomRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            roomRow.Children.Add(new TextBlock
            {
                Text = "🎬  Phòng: ",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                FontSize = 13
            });
            roomRow.Children.Add(new TextBlock
            {
                Text = st.RoomName,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });
            sp.Children.Add(roomRow);

            // --- GIÁ VÉ ---
            var priceRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            priceRow.Children.Add(new TextBlock
            {
                Text = "💰  Giá từ: ",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                FontSize = 13
            });
            priceRow.Children.Add(new TextBlock
            {
                Text = st.FormattedPrice,
                Foreground = new SolidColorBrush(Color.FromRgb(234, 88, 12)),
                FontWeight = FontWeights.Bold,
                FontSize = 14
            });
            sp.Children.Add(priceRow);

            // --- NÚT CHỌN / BADGE HẾT GIỜ ---
            if (isPast)
            {
                var pastBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(0, 10, 0, 10)
                };
                pastBadge.Child = new TextBlock
                {
                    Text = "⛔  Suất đã diễn ra",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                sp.Children.Add(pastBadge);
            }
            else
            {
                var btnSelect = new Button
                {
                    Content = "🎟  CHỌN GHẾ NGỒI",
                    Height = 42,
                    Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(0)
                };
                btnSelect.Click += (s, e) => OpenSeatSelection(st);
                sp.Children.Add(btnSelect);
            }

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
