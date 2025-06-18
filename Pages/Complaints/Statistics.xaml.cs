using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.Complaints
{
    public partial class Statistics : UserControl
    {
        private readonly ComplaintsContext _context;
        private DateTime _startDate;
        private DateTime _endDate;

        public Statistics()
        {
            InitializeComponent();
            _context = new ComplaintsContext();

            // Set default time range to current month
            _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _endDate = DateTime.Today;
            cbTimeRange.SelectedIndex = 0;

            LoadData();
        }

        private void CbTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTimeRange.SelectedIndex == -1) return;

            spCustomRange.Visibility = cbTimeRange.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;

            if (cbTimeRange.SelectedIndex != 3)
            {
                switch (cbTimeRange.SelectedIndex)
                {
                    case 0: // Current Month
                        _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        _endDate = DateTime.Today;
                        break;
                    case 1: // Last 7 Days
                        _startDate = DateTime.Today.AddDays(-7);
                        _endDate = DateTime.Today;
                        break;
                    case 2: // Last 30 Days
                        _startDate = DateTime.Today.AddDays(-30);
                        _endDate = DateTime.Today;
                        break;
                }
                dpStartDate.SelectedDate = _startDate;
                dpEndDate.SelectedDate = _endDate;
                LoadData();
            }
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue && dpEndDate.SelectedDate.HasValue)
            {
                _startDate = dpStartDate.SelectedDate.Value;
                _endDate = dpEndDate.SelectedDate.Value;

                if (_startDate > _endDate)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadData();
            }
        }

        private void LoadData()
        {
            var complaints = _context.Complaints
                .Include(c => c.Reviewer)
                .Where(c => c.SubmissionDate >= _startDate && c.LastStatusChange <= _endDate)
                .ToList();

            int total = complaints.Count;
            double totalNonZero = total > 0 ? total : 1; // Avoid division by zero

            // По статусам
            int created = complaints.Count(c => c.Status == ComplaintStatus.Создана);
            int inProgress = complaints.Count(c => c.Status == ComplaintStatus.В_обработке);
            int completed = complaints.Count(c => c.Status == ComplaintStatus.Завершена);
            int rejected = complaints.Count(c => c.Status == ComplaintStatus.Отклонена);

            txtTotalComplaints.Text = $"Всего пожеланий: {total}";
            txtCreatedComplaints.Text = $"Создана: {created} ({(created / totalNonZero * 100):F1}%)";
            txtInProgressComplaints.Text = $"В обработке: {inProgress} ({(inProgress / totalNonZero * 100):F1}%)";
            txtCompletedComplaints.Text = $"Завершена: {completed} ({(completed / totalNonZero * 100):F1}%)";
            txtRejectedComplaints.Text = $"Отклонена: {rejected} ({(rejected / totalNonZero * 100):F1}%)";

            // По завершению
            var completedComplaints = complaints.Where(c => c.Status == ComplaintStatus.Завершена).ToList();
            double avgResolutionDays = completedComplaints.Any()
                ? completedComplaints.Average(c => (c.LastStatusChange - c.SubmissionDate).TotalDays)
                : 0;

            txtCompletedCount.Text = $"Завершенных пожеланий: {completed}";
            txtCompletedPercentage.Text = $"Процент завершенных: {(completed / totalNonZero * 100):F1}%";
            txtAvgResolutionTime.Text = $"Среднее время решения: {avgResolutionDays:F1} дней";

            // Дополнительно
            int anonymous = complaints.Count(c => !c.StudentId.HasValue || c.StudentId == 0);
            int commented = complaints.Count(c => !string.IsNullOrEmpty(c.Comment));
            double days = (_endDate - _startDate).TotalDays + 1;
            double avgDaily = total / (days > 0 ? days : 1);

            var topReviewer = complaints
                .Where(c => c.ReviewedBy.HasValue)
                .GroupBy(c => c.ReviewedBy)
                .Select(g => new { EmployeeId = g.Key, Count = g.Count(), Name = g.First().Reviewer?.FIO ?? "Неизвестно" })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            txtAnonymousComplaints.Text = $"Анонимные пожелания: {anonymous} ({(anonymous / totalNonZero * 100):F1}%)";
            txtAvgDailySubmissions.Text = $"Среднее количество пожеланий в день: {avgDaily:F1}";
            txtTopReviewer.Text = topReviewer != null
                ? $"Самый активный ревьюер: {topReviewer.Name} ({topReviewer.Count} пожеланий)"
                : "Ревьюеры отсутствуют";
        }
    }
}