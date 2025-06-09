using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.RepairRequests
{
    public partial class MaterialsAnalytics : UserControl
    {
        private readonly RepairRequestsContext _repairRequestsContext = new RepairRequestsContext();
        private readonly RepairMaterialsContext _repairMaterialsContext = new RepairMaterialsContext();
        private DateTime _startDate;
        private DateTime _endDate;

        public MaterialsAnalytics()
        {
            InitializeComponent();
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
            var requests = _repairRequestsContext.RepairRequests
                .Where(r => r.RequestDate >= _startDate && r.LastStatusChange <= _endDate)
                .Select(r => r.RequestId)
                .ToList();

            var materials = _repairMaterialsContext.RepairMaterials
                .Where(m => requests.Contains(m.RequestId))
                .ToList();
            int totalMaterials = materials.Count;
            double totalNonZero = totalMaterials > 0 ? totalMaterials : 1;

            // Общая стоимость материалов
            decimal totalCost = materials.Sum(m => m.Quantity * m.CostPerUnit);
            txtTotalMaterialsCost.Text = $"Общая стоимость материалов: {totalCost:F2} руб.";

            // Самый часто используемый материал
            var mostUsedMaterial = materials
                .GroupBy(m => m.MaterialName)
                .Select(g => new { MaterialName = g.Key, TotalQuantity = g.Sum(m => m.Quantity) })
                .OrderByDescending(g => g.TotalQuantity)
                .FirstOrDefault();
            txtMostUsedMaterial.Text = mostUsedMaterial != null
                ? $"Самый часто используемый материал: {mostUsedMaterial.MaterialName} ({mostUsedMaterial.TotalQuantity} ед.)"
                : "Нет данных о материалах";

            // Ежемесячные траты
            var monthlyCost = materials
                .Join(_repairRequestsContext.RepairRequests,
                      m => m.RequestId,
                      r => r.RequestId,
                      (m, r) => new { m, r.RequestDate })
                .GroupBy(x => new { x.RequestDate.Year, x.RequestDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Cost = g.Sum(x => x.m.Quantity * x.m.CostPerUnit) })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                .FirstOrDefault();
            txtMonthlyCost.Text = monthlyCost != null
                ? $"Траты за {new DateTime(monthlyCost.Year, monthlyCost.Month, 1):MMMM yyyy}: {monthlyCost.Cost:F2} руб."
                : "Нет данных за месяц";

            // Материалы по типам проблем
            var materialByProblem = materials
                .Join(_repairRequestsContext.RepairRequests,
                      m => m.RequestId,
                      r => r.RequestId,
                      (m, r) => new { m, r.Problem })
                .GroupBy(x => x.Problem)
                .Select(g => new { Problem = g.Key, TotalCost = g.Sum(x => x.m.Quantity * x.m.CostPerUnit) })
                .ToList();
            txtMaterialByProblem.Text = string.Join("\n", materialByProblem.Select(p =>
                $"{p.Problem}: {p.TotalCost:F2} руб."));

            // Рекомендации по закупкам (простая логика: материалы с количеством < 5)
            var suggestedMaterials = materials
                .GroupBy(m => m.MaterialName)
                .Select(g => new { MaterialName = g.Key, TotalQuantity = g.Sum(m => m.Quantity) })
                .Where(g => g.TotalQuantity < 5)
                .Select(g => g.MaterialName)
                .ToList();
            txtSuggestedMaterials.Text = suggestedMaterials.Any()
                ? $"Рекомендуется закупить: {string.Join(", ", suggestedMaterials)}"
                : "Нет необходимости в закупках";
        }
    }
}