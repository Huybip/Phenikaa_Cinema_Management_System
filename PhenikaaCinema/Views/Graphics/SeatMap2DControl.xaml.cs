using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Graphics
{
    public partial class SeatMap2DControl : UserControl
    {
        private List<SeatDto> _allSeats = new List<SeatDto>();
        private readonly HashSet<SeatDto> _selectedSeats = new HashSet<SeatDto>();
        private readonly Dictionary<int, Border> _seatElements = new Dictionary<int, Border>();

        public event Action<SeatDto, bool> SeatSelectionChanged;
        public IReadOnlyCollection<SeatDto> SelectedSeats => _selectedSeats;

        public SeatMap2DControl()
        {
            InitializeComponent();
        }

        public void LoadSeats(List<SeatDto> seats)
        {
            _allSeats = seats ?? new List<SeatDto>();
            _selectedSeats.Clear();
            _seatElements.Clear();
            Render2DSeatMap();
        }

        public void ClearSelection()
        {
            foreach (var seat in _selectedSeats.ToList())
            {
                if (_seatElements.TryGetValue(seat.SeatId, out var element))
                {
                    AnimateSeatDeselected(seat, element);
                }
            }
            _selectedSeats.Clear();
        }

        private void Render2DSeatMap()
        {
            SeatsCanvas.Children.Clear();
            if (_allSeats == null || _allSeats.Count == 0)
            {
                var emptyNotice = new TextBlock
                {
                    Text = "Chưa có cấu hình sơ đồ ghế cho phòng chiếu này.",
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 155)),
                    FontSize = 14,
                    FontWeight = FontWeights.Medium
                };
                Canvas.SetLeft(emptyNotice, 50);
                Canvas.SetTop(emptyNotice, 50);
                SeatsCanvas.Children.Add(emptyNotice);
                SeatsCanvas.Width = 400;
                SeatsCanvas.Height = 120;
                return;
            }

            var rows = _allSeats.Select(s => s.RowLabel).Distinct().OrderBy(r => r).ToList();
            int maxSeatNumber = _allSeats.Max(s => s.SeatNumber);

            double seatWidth = 38;
            double seatHeight = 36;
            double seatGapX = 8;
            double seatGapY = 10;
            double labelWidth = 35;

            double totalWidth = labelWidth * 2 + (maxSeatNumber * (seatWidth + seatGapX)) + 40;
            double totalHeight = (rows.Count * (seatHeight + seatGapY)) + 40;

            SeatsCanvas.Width = Math.Max(totalWidth, 700);
            SeatsCanvas.Height = Math.Max(totalHeight, 350);

            double startX = (SeatsCanvas.Width - (labelWidth * 2 + (maxSeatNumber * (seatWidth + seatGapX)))) / 2;
            if (startX < 20) startX = 20;
            double startY = 20;

            for (int rIdx = 0; rIdx < rows.Count; rIdx++)
            {
                string rowLabel = rows[rIdx];
                double currentY = startY + rIdx * (seatHeight + seatGapY);

                // Row label Left
                var labelLeft = CreateRowLabel(rowLabel);
                Canvas.SetLeft(labelLeft, startX);
                Canvas.SetTop(labelLeft, currentY + 7);
                SeatsCanvas.Children.Add(labelLeft);

                // Row label Right
                var labelRight = CreateRowLabel(rowLabel);
                Canvas.SetLeft(labelRight, startX + labelWidth + (maxSeatNumber * (seatWidth + seatGapX)) + 5);
                Canvas.SetTop(labelRight, currentY + 7);
                SeatsCanvas.Children.Add(labelRight);

                var seatsInRow = _allSeats.Where(s => s.RowLabel == rowLabel).ToList();
                foreach (var seat in seatsInRow)
                {
                    double currentX = startX + labelWidth + ((seat.SeatNumber - 1) * (seatWidth + seatGapX));
                    var seatElement = CreateSeatGraphic(seat, seatWidth, seatHeight);
                    Canvas.SetLeft(seatElement, currentX);
                    Canvas.SetTop(seatElement, currentY);

                    _seatElements[seat.SeatId] = seatElement;
                    SeatsCanvas.Children.Add(seatElement);
                }
            }
        }

        private TextBlock CreateRowLabel(string label)
        {
            return new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Width = 25,
                TextAlignment = TextAlignment.Center
            };
        }

        private Border CreateSeatGraphic(SeatDto seat, double width, double height)
        {
            double actualWidth = seat.IsCouple ? (width * 1.6) : width;

            var border = new Border
            {
                Width = actualWidth,
                Height = height,
                CornerRadius = new CornerRadius(5),
                Cursor = seat.CanSelect ? Cursors.Hand : Cursors.No,
                Tag = seat,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1.0, 1.0)
            };

            // Custom tooltip
            var toolTip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 42)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 80)),
                BorderThickness = new Thickness(1),
                Content = $"{seat.SeatCode} • {seat.SeatType}\nGiá: {CurrencyHelper.FormatVnd(seat.CalculatedPrice)}\nTrạng thái: {GetStatusDisplayName(seat)}"
            };
            border.ToolTip = toolTip;

            ApplySeatVisualState(border, seat, false);

            if (seat.CanSelect)
            {
                border.MouseEnter += (s, e) =>
                {
                    if (!_selectedSeats.Contains(seat))
                    {
                        AnimateScale(border, 1.12);
                    }
                };

                border.MouseLeave += (s, e) =>
                {
                    if (!_selectedSeats.Contains(seat))
                    {
                        AnimateScale(border, 1.0);
                    }
                };

                border.MouseLeftButtonDown += (s, e) =>
                {
                    ToggleSeat(seat, border);
                };
            }

            // Seat content: Cushion shape & number
            var grid = new Grid();
            
            // Seat top bar / headrest 2D line
            var headrest = new Rectangle
            {
                Height = 4,
                RadiusX = 2,
                RadiusY = 2,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(4, 3, 4, 0),
                Fill = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255))
            };
            grid.Children.Add(headrest);

            var numberText = new TextBlock
            {
                Text = seat.SeatNumber.ToString(),
                Foreground = Brushes.White,
                FontSize = seat.IsCouple ? 10 : 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            grid.Children.Add(numberText);

            border.Child = grid;
            return border;
        }

        private string GetStatusDisplayName(SeatDto seat)
        {
            switch (seat.Status)
            {
                case SeatStatus.Booked: return "Đã bán";
                case SeatStatus.Held: return "Đang giữ chỗ";
                case SeatStatus.Selected: return "Đang chọn";
                default: return "Có thể chọn";
            }
        }

        private void ApplySeatVisualState(Border border, SeatDto seat, bool isSelected)
        {
            if (isSelected)
            {
                // Orange selected
                border.Background = new LinearGradientBrush(
                    Color.FromRgb(249, 115, 22),
                    Color.FromRgb(234, 88, 12),
                    new Point(0, 0), new Point(0, 1));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 146, 60));
                border.BorderThickness = new Thickness(1.5);
                border.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(249, 115, 22),
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Opacity = 0.85
                };
                return;
            }

            border.Effect = null;
            border.BorderThickness = new Thickness(0);

            switch (seat.Status)
            {
                case SeatStatus.Booked:
                    border.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    border.BorderThickness = new Thickness(1);
                    break;

                case SeatStatus.Held:
                    border.Background = new LinearGradientBrush(
                        Color.FromRgb(239, 68, 68),
                        Color.FromRgb(220, 38, 38),
                        new Point(0, 0), new Point(0, 1));
                    break;

                case SeatStatus.Available:
                default:
                    if (seat.IsVip)
                    {
                        border.Background = new LinearGradientBrush(
                            Color.FromRgb(245, 158, 11),
                            Color.FromRgb(217, 119, 6),
                            new Point(0, 0), new Point(0, 1));
                    }
                    else if (seat.IsCouple)
                    {
                        border.Background = new LinearGradientBrush(
                            Color.FromRgb(236, 72, 153),
                            Color.FromRgb(219, 39, 119),
                            new Point(0, 0), new Point(0, 1));
                    }
                    else
                    {
                        border.Background = new LinearGradientBrush(
                            Color.FromRgb(59, 130, 246),
                            Color.FromRgb(37, 99, 235),
                            new Point(0, 0), new Point(0, 1));
                    }
                    break;
            }
        }

        private void ToggleSeat(SeatDto seat, Border border)
        {
            if (_selectedSeats.Contains(seat))
            {
                _selectedSeats.Remove(seat);
                seat.Status = SeatStatus.Available;
                AnimateSeatDeselected(seat, border);
                SeatSelectionChanged?.Invoke(seat, false);
            }
            else
            {
                if (_selectedSeats.Count >= 8)
                {
                    MessageBox.Show("Mỗi lần đặt vé chỉ được chọn tối đa 8 ghế.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedSeats.Add(seat);
                seat.Status = SeatStatus.Selected;
                AnimateSeatSelected(seat, border);
                SeatSelectionChanged?.Invoke(seat, true);
            }
        }

        private void AnimateSeatSelected(SeatDto seat, Border border)
        {
            ApplySeatVisualState(border, seat, true);

            // Pop-up bounce animation
            var transform = border.RenderTransform as ScaleTransform;
            if (transform != null)
            {
                var anim = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.22,
                    Duration = TimeSpan.FromMilliseconds(120),
                    AutoReverse = true,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        private void AnimateSeatDeselected(SeatDto seat, Border border)
        {
            ApplySeatVisualState(border, seat, false);

            var transform = border.RenderTransform as ScaleTransform;
            if (transform != null)
            {
                var anim = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        private void AnimateScale(Border border, double targetScale)
        {
            var transform = border.RenderTransform as ScaleTransform;
            if (transform != null)
            {
                var anim = new DoubleAnimation
                {
                    To = targetScale,
                    Duration = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }
    }
}

