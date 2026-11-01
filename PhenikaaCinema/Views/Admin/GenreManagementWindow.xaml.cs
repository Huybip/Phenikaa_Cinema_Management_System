using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class GenreManagementWindow : Window
    {
        private readonly GenreService _genreService = new GenreService();
        private List<GenreDto> _allGenres = new List<GenreDto>();
        private bool _isLoading = false;

        public GenreManagementWindow()
        {
            InitializeComponent();
            LoadGenres();
        }

        private void LoadGenres()
        {
            try
            {
                _allGenres = _genreService.GetAllGenres(activeOnly: false);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách thể loại: " + ex.Message,
                    "Lỗi tải dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            string keyword = (SearchTextBox.Text ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                GenresDataGrid.ItemsSource = _allGenres;
            }
            else
            {
                GenresDataGrid.ItemsSource = _allGenres.Where(g =>
                    (g.GenreName != null && g.GenreName.ToLower().Contains(keyword)) ||
                    (g.Description != null && g.Description.ToLower().Contains(keyword))).ToList();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
            {
                ApplyFilter();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGenres();
            ClearForm();
        }

        private void GenresDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            var genre = GenresDataGrid.SelectedItem as GenreDto;
            if (genre == null) return;

            _isLoading = true;
            try
            {
                GenreIdTextBox.Text = genre.GenreId.ToString();
                GenreNameTextBox.Text = genre.GenreName;
                DescriptionTextBox.Text = genre.Description ?? "";
                IsActiveCheckBox.IsChecked = genre.IsActive;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = (GenreNameTextBox.Text ?? "").Trim();
            string desc = (DescriptionTextBox.Text ?? "").Trim();
            bool isActive = IsActiveCheckBox.IsChecked == true;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên thể loại phim.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenreNameTextBox.Focus();
                return;
            }

            if (_genreService.AddGenre(name, desc, isActive, out string error))
            {
                MessageBox.Show("Thêm thể loại thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGenres();
                ClearForm();
            }
            else
            {
                MessageBox.Show("Không thể thêm thể loại: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(GenreIdTextBox.Text, out int genreId))
            {
                MessageBox.Show("Vui lòng chọn thể loại cần cập nhật từ danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = (GenreNameTextBox.Text ?? "").Trim();
            string desc = (DescriptionTextBox.Text ?? "").Trim();
            bool isActive = IsActiveCheckBox.IsChecked == true;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên thể loại phim.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenreNameTextBox.Focus();
                return;
            }

            if (_genreService.UpdateGenre(genreId, name, desc, isActive, out string error))
            {
                MessageBox.Show("Cập nhật thể loại thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGenres();
                ClearForm();
            }
            else
            {
                MessageBox.Show("Không thể cập nhật thể loại: " + error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(GenreIdTextBox.Text, out int genreId))
            {
                MessageBox.Show("Vui lòng chọn thể loại cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa thể loại '{GenreNameTextBox.Text}'?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (_genreService.DeleteGenre(genreId, out string error))
            {
                MessageBox.Show("Xóa thể loại thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGenres();
                ClearForm();
            }
            else
            {
                MessageBox.Show(error, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _isLoading = true;
            try
            {
                GenresDataGrid.SelectedItem = null;
                GenreIdTextBox.Clear();
                GenreNameTextBox.Clear();
                DescriptionTextBox.Clear();
                IsActiveCheckBox.IsChecked = true;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

