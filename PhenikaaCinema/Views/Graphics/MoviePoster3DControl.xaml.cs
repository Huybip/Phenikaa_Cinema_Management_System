using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using PhenikaaCinema.Services;

namespace PhenikaaCinema.Views.Graphics
{
    public partial class MoviePoster3DControl : UserControl
    {
        public MoviePoster3DControl()
        {
            InitializeComponent();
        }

        public void SetMovie(MovieDto movie)
        {
            if (movie == null) return;

            PosterTitleText.Text = movie.Title;
            PosterGenreText.Text = $"{movie.DisplayGenres} • {movie.FormattedDuration}";

            if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(movie.PosterUrl, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PosterImage.Source = bitmap;
                    PosterFallbackIcon.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    PosterFallbackIcon.Visibility = Visibility.Visible;
                }
            }
            else
            {
                PosterFallbackIcon.Visibility = Visibility.Visible;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Smooth 3D tilt animation on hover
            AnimateRotation(PosterYRotation, 18.0);
            AnimateRotation(PosterXRotation, -8.0);
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            // Return to flat facing
            AnimateRotation(PosterYRotation, 0.0);
            AnimateRotation(PosterXRotation, 0.0);
        }

        private void AnimateRotation(System.Windows.Media.Media3D.AxisAngleRotation3D rotation, double toAngle)
        {
            var anim = new DoubleAnimation
            {
                To = toAngle,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            rotation.BeginAnimation(System.Windows.Media.Media3D.AxisAngleRotation3D.AngleProperty, anim);
        }
    }
}

