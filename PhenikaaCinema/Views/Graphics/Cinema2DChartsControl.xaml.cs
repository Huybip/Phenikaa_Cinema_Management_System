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
    public partial class Cinema2DChartsControl : UserControl
    {
        private readonly StatisticsService _statsService = new StatisticsService();
        private List<DayRevenueDto> _dayRevenues = new List<DayRevenueDto>();
        private List<RoomOccupancyDto> _occupancies = new List<RoomOccupancyDto>();

        public Cinema2DChartsControl()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadChartData();
        }

        public void LoadChartData()
        {
            try
            {
                _dayRevenues = _statsService.GetLast7DaysRevenue();
                _occupancies = _statsService.GetRoomOccupancies();

                RenderRevenueLineChart();
                RenderOccupancyBars();
            }
            catch
            {
            }
        }

        private void RevenueChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderRevenueLineChart();
        }

        private void RenderRevenueLineChart()
        {
            RevenueChartCanvas.Children.Clear();
            if (_dayRevenues == null || _dayRevenues.Count == 0) return;

            double width = RevenueChartCanvas.ActualWidth;
            double height = RevenueChartCanvas.ActualHeight;

            if (width < 50 || height < 50) return;

            double paddingLeft = 60;
            double paddingRight = 25;
            double paddingTop = 20;
            double paddingBottom = 35;

            double chartW = width - paddingLeft - paddingRight;
            double chartH = height - paddingTop - paddingBottom;

            decimal maxRev = _dayRevenues.Max(d => d.Revenue);
            if (maxRev <= 0) maxRev = 500000;

            PeakRevenueText.Text = $"Cao nhất: {CurrencyHelper.FormatVnd(_dayRevenues.Max(d => d.Revenue))}";

            // 1. HORIZONTAL GRID LINES & Y-AXIS LABELS
            int gridSteps = 4;
            for (int i = 0; i <= gridSteps; i++)
            {
                double y = paddingTop + (chartH / gridSteps) * i;
                decimal value = maxRev - (maxRev / gridSteps) * i;

                // Light Grid line
                var line = new Line
                {
                    X1 = paddingLeft,
                    Y1 = y,
                    X2 = paddingLeft + chartW,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 3 }
                };
                RevenueChartCanvas.Children.Add(line);

                // Y Label
                var label = new TextBlock
                {
                    Text = FormatCompactVnd(value),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 11,
                    FontFamily = new FontFamily("Segoe UI"),
                    TextAlignment = TextAlignment.Right,
                    Width = 52
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, y - 8);
                RevenueChartCanvas.Children.Add(label);
            }

            // 2. CALCULATE POINTS
            int count = _dayRevenues.Count;
            var points = new List<Point>();
            double stepX = chartW / Math.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                double px = paddingLeft + i * stepX;
                double ratio = (double)(_dayRevenues[i].Revenue / maxRev);
                double py = paddingTop + chartH - (ratio * chartH);
                points.Add(new Point(px, py));

                // X Label (Date)
                var dateLabel = new TextBlock
                {
                    Text = _dayRevenues[i].DateLabel,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    FontFamily = new FontFamily("Segoe UI"),
                    Width = 46,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(dateLabel, px - 23);
                Canvas.SetTop(dateLabel, height - 24);
                RevenueChartCanvas.Children.Add(dateLabel);
            }

            // 3. DRAW AREA GRADIENT UNDER CURVE (Primary Blue Fade)
            var areaFigure = new PathFigure { StartPoint = new Point(points[0].X, paddingTop + chartH), IsClosed = true };
            for (int i = 0; i < points.Count; i++)
            {
                areaFigure.Segments.Add(new LineSegment(points[i], true));
            }
            areaFigure.Segments.Add(new LineSegment(new Point(points[points.Count - 1].X, paddingTop + chartH), true));

            var areaGeometry = new PathGeometry();
            areaGeometry.Figures.Add(areaFigure);

            var areaPath = new Path
            {
                Data = areaGeometry,
                Fill = new LinearGradientBrush(
                    Color.FromArgb(60, 37, 99, 235),
                    Color.FromArgb(5, 37, 99, 235),
                    new Point(0, 0), new Point(0, 1))
            };
            RevenueChartCanvas.Children.Add(areaPath);

            // 4. DRAW 2D LINE CHART (Primary Blue Stroke)
            var lineFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
            for (int i = 1; i < points.Count; i++)
            {
                lineFigure.Segments.Add(new LineSegment(points[i], true));
            }
            var lineGeometry = new PathGeometry();
            lineGeometry.Figures.Add(lineFigure);

            var linePath = new Path
            {
                Data = lineGeometry,
                Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(37, 99, 235),
                    BlurRadius = 8,
                    ShadowDepth = 1,
                    Opacity = 0.3
                }
            };
            RevenueChartCanvas.Children.Add(linePath);

            // 5. DRAW DATA NODES & HOVER TOOLTIPS
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                var revItem = _dayRevenues[i];

                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    StrokeThickness = 2.5,
                    Cursor = Cursors.Hand,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new ScaleTransform(1.0, 1.0)
                };

                dot.ToolTip = new ToolTip
                {
                    Background = Brushes.White,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    Content = $"Ngày: {revItem.Date:dd/MM/yyyy}\nDoanh thu: {CurrencyHelper.FormatVnd(revItem.Revenue)}\nVé bán: {revItem.TicketsCount} vé"
                };

                dot.MouseEnter += (s, e) =>
                {
                    var tr = dot.RenderTransform as ScaleTransform;
                    tr?.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.6, TimeSpan.FromMilliseconds(120)));
                    tr?.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.6, TimeSpan.FromMilliseconds(120)));
                };
                dot.MouseLeave += (s, e) =>
                {
                    var tr = dot.RenderTransform as ScaleTransform;
                    tr?.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120)));
                    tr?.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120)));
                };

                Canvas.SetLeft(dot, pt.X - 5);
                Canvas.SetTop(dot, pt.Y - 5);
                RevenueChartCanvas.Children.Add(dot);
            }
        }

        private void RenderOccupancyBars()
        {
            RoomOccupancyBarsPanel.Children.Clear();
            if (_occupancies == null || _occupancies.Count == 0)
            {
                RoomOccupancyBarsPanel.Children.Add(new TextBlock
                {
                    Text = "Chưa có dữ liệu phòng chiếu.",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 12
                });
                return;
            }

            foreach (var room in _occupancies)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1)
                };

                var stack = new StackPanel();

                // Room Name and Rate header
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var roomTitle = new TextBlock
                {
                    Text = room.RoomName,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13
                };
                Grid.SetColumn(roomTitle, 0);

                var rateText = new TextBlock
                {
                    Text = $"{room.OccupancyRate}%",
                    Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    FontWeight = FontWeights.ExtraBold,
                    FontSize = 13
                };
                Grid.SetColumn(rateText, 1);

                headerGrid.Children.Add(roomTitle);
                headerGrid.Children.Add(rateText);
                stack.Children.Add(headerGrid);

                // 2D Progress bar track
                var trackBorder = new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    ClipToBounds = true
                };

                var barGrid = new Grid();
                var fillRect = new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = new LinearGradientBrush(
                        Color.FromRgb(37, 99, 235),
                        Color.FromRgb(59, 130, 246),
                        new Point(0, 0), new Point(1, 0))
                };

                // Animate bar width on load
                fillRect.Loaded += (s, e) =>
                {
                    double targetW = trackBorder.ActualWidth * (room.OccupancyRate / 100.0);
                    var anim = new DoubleAnimation
                    {
                        From = 0,
                        To = Math.Max(0, targetW),
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    fillRect.BeginAnimation(FrameworkElement.WidthProperty, anim);
                };

                barGrid.Children.Add(fillRect);
                trackBorder.Child = barGrid;
                stack.Children.Add(trackBorder);

                // Sub detail text
                var detailText = new TextBlock
                {
                    Text = $"Đã đặt: {room.BookedSeats}/{room.TotalSeats} ghế",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 11,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                stack.Children.Add(detailText);

                card.Child = stack;
                RoomOccupancyBarsPanel.Children.Add(card);
            }
        }

        private string FormatCompactVnd(decimal amount)
        {
            if (amount >= 1000000)
            {
                return $"{(amount / 1000000m):F1}Tr";
            }
            if (amount >= 1000)
            {
                return $"{(amount / 1000m):F0}k";
            }
            return amount.ToString("N0");
        }
    }
}
