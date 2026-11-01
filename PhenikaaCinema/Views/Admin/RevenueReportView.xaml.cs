using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhenikaaCinema.Services;
using PhenikaaCinema.Helpers;

namespace PhenikaaCinema.Views.Admin
{
    public partial class RevenueReportView : UserControl
    {
        private readonly ReportService _reportService = new ReportService();
        private List<MovieRevenueDto> _movieRevenues = new List<MovieRevenueDto>();

        public RevenueReportView()
        {
            InitializeComponent();
            Loaded += RevenueReportView_Loaded;
        }

        private void RevenueReportView_Loaded(object sender, RoutedEventArgs e)
        {
            // Mặc định: tháng này
            StartDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }

        public void LoadReport()
        {
            try
            {
                DateTime? fromUtc = null, toUtc = null;

                if (StartDatePicker.SelectedDate.HasValue)
                {
                    fromUtc = StartDatePicker.SelectedDate.Value.Date.ToUniversalTime();
                }

                if (EndDatePicker.SelectedDate.HasValue)
                {
                    toUtc = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();
                }

                // Tổng hợp
                var summary = _reportService.GetRevenueSummary(fromUtc, toUtc);
                TotalRevenueText.Text = summary.FormattedRevenue;
                TotalTicketsText.Text = summary.TotalTicketsSold.ToString();
                TotalBookingsText.Text = summary.TotalConfirmedBookings.ToString();
                TotalMoviesText.Text = summary.TotalMoviesShown.ToString();

                // Chi tiết theo phim
                _movieRevenues = _reportService.GetRevenueByMovie(fromUtc, toUtc);
                MovieRevenueDataGrid.ItemsSource = _movieRevenues;

                EmptyText.Visibility = _movieRevenues.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // Đánh số thứ tự
                var numbered = new List<object>();
                for (int i = 0; i < _movieRevenues.Count; i++)
                {
                    // Wrap để có STT - dùng anonymous object
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải báo cáo: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadReport_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void BtnThisMonth_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }

        private void BtnLastMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTime firstOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
            StartDatePicker.SelectedDate = firstOfLastMonth;
            EndDatePicker.SelectedDate = firstOfThisMonth.AddDays(-1);
            LoadReport();
        }

        private void BtnLast7Days_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-6);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadReport();
        }
    }
}

