using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DormitoryPATDesktop.Pages.RepairRequests
{
    /// <summary>
    /// Логика взаимодействия для Statistics.xaml
    /// </summary>
    public partial class Statistics : UserControl
    {
        private readonly RepairRequestsContext _context;
        private DateTime _startDate;
        private DateTime _endDate;

        public Statistics()
        {
            InitializeComponent();
            _context = new RepairRequestsContext();

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
            var requests = _context.RepairRequests
                .Include(r => r.Master)
                .Where(r => r.RequestDate >= _startDate && r.LastStatusChange <= _endDate)
                .ToList();

            int total = requests.Count;
            double totalNonZero = total > 0 ? total : 1; // Avoid division by zero

            // По статусам
            int created = requests.Count(r => r.Status == RequestStatus.Создана);
            int inProgress = requests.Count(r => r.Status == RequestStatus.В_обработке);
            int awaitingParts = requests.Count(r => r.Status == RequestStatus.Ожидает_запчастей);
            int completed = requests.Count(r => r.Status == RequestStatus.Завершена);
            int rejected = requests.Count(r => r.Status == RequestStatus.Отклонена);

            txtTotalRequests.Text = $"Всего заявок: {total}";
            txtCreatedRequests.Text = $"Создана: {created} ({(created / totalNonZero * 100):F1}%)";
            txtInProgressRequests.Text = $"В обработке: {inProgress} ({(inProgress / totalNonZero * 100):F1}%)";
            txtAwaitingPartsRequests.Text = $"Ожидает запчастей: {awaitingParts} ({(awaitingParts / totalNonZero * 100):F1}%)";
            txtCompletedRequests.Text = $"Завершена: {completed} ({(completed / totalNonZero * 100):F1}%)";
            txtRejectedRequests.Text = $"Отклонена: {rejected} ({(rejected / totalNonZero * 100):F1}%)";

            // По типам проблем
            int electrical = requests.Count(r => r.Problem == ProblemType.Электрика);
            int plumbing = requests.Count(r => r.Problem == ProblemType.Сантехника);
            int furniture = requests.Count(r => r.Problem == ProblemType.Мебель);

            txtElectricalRequests.Text = $"Электрика: {electrical} ({(electrical / totalNonZero * 100):F1}%)";
            txtPlumbingRequests.Text = $"Сантехника: {plumbing} ({(plumbing / totalNonZero * 100):F1}%)";
            txtFurnitureRequests.Text = $"Мебель: {furniture} ({(furniture / totalNonZero * 100):F1}%)";

            // По завершению и дополнительно
            var completedRequests = requests.Where(r => r.Status == RequestStatus.Завершена).ToList();
            double avgResolutionDays = completedRequests.Any()
                ? completedRequests.Average(r => (r.LastStatusChange - r.RequestDate).TotalDays)
                : 0;

            var topMaster = requests
                .Where(r => r.MasterId.HasValue)
                .GroupBy(r => r.MasterId)
                .Select(g => new { MasterId = g.Key, Count = g.Count(), Name = g.First().Master?.FIO ?? "Неизвестно" })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            int userCommented = requests.Count(r => !string.IsNullOrEmpty(r.UserComment));
            int masterCommented = requests.Count(r => !string.IsNullOrEmpty(r.MasterComment));

            var topLocation = requests
                .GroupBy(r => r.Location)
                .Select(g => new { Location = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            txtCompletedCount.Text = $"Завершенных заявок: {completed}";
            txtCompletedPercentage.Text = $"Процент завершенных: {(completed / totalNonZero * 100):F1}%";
            txtAvgResolutionTime.Text = $"Среднее время выполнения: {avgResolutionDays:F1} дней";
            txtTopMaster.Text = topMaster != null
                ? $"Самый активный мастер: {topMaster.Name} ({topMaster.Count} заявок)"
                : "Мастера отсутствуют";
            txtUserCommentedRequests.Text = $"Заявки с комментариями пользователя: {userCommented} ({(userCommented / totalNonZero * 100):F1}%)";
            txtMasterCommentedRequests.Text = $"Заявки с комментариями мастера: {masterCommented} ({(masterCommented / totalNonZero * 100):F1}%)";
            txtTopLocation.Text = topLocation != null
                ? $"Самое частое место: {topLocation.Location} ({topLocation.Count} заявок)"
                : "Места не указаны";
        }
    }
}
