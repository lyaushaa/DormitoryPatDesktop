using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
        private List<MaterialUsage> _materialUsageHistory = new List<MaterialUsage>();

        public MaterialsAnalytics()
        {
            InitializeComponent();
            Loaded += MaterialsAnalytics_Loaded;
        }

        private async void MaterialsAnalytics_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем период по умолчанию - текущий месяц
            _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _endDate = _startDate.AddMonths(1).AddDays(-1);

            // Загружаем историю использования материалов
            await LoadMaterialUsageHistory();

            // Обновляем аналитику
            UpdateAnalytics();

            // Генерируем рекомендации (не зависит от выбранного периода)
            GeneratePurchaseRecommendations();
        }

        private async Task LoadMaterialUsageHistory()
        {
            try
            {
                // Получаем все завершенные заявки
                var completedRequests = await _repairRequestsContext.RepairRequests
                    .Where(r => r.Status == RequestStatus.Завершена)
                    .ToListAsync();

                // Получаем все материалы для этих заявок
                var materials = await _repairMaterialsContext.RepairMaterials
                    .Where(rm => completedRequests.Select(cr => cr.RequestId).Contains(rm.RequestId))
                    .ToListAsync();

                // Соединяем данные
                var joinedData = from material in materials
                                 join request in completedRequests on material.RequestId equals request.RequestId
                                 select new { material, request };

                _materialUsageHistory = joinedData
                    .GroupBy(x => new { x.material.MaterialName, x.request.RequestDate.Month, x.request.RequestDate.Year })
                    .Select(g => new MaterialUsage
                    {
                        MaterialName = g.Key.MaterialName,
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        TotalQuantity = g.Sum(x => x.material.Quantity),
                        TotalCost = g.Sum(x => x.material.Quantity * x.material.CostPerUnit)
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private async void UpdateAnalytics()
        {
            try
            {
                // Получаем завершенные заявки за выбранный период
                var completedRequests = await _repairRequestsContext.RepairRequests
                    .Where(r => r.Status == RequestStatus.Завершена &&
                                r.RequestDate >= _startDate &&
                                r.RequestDate <= _endDate)
                    .ToListAsync();

                // Получаем материалы для этих заявок
                var periodMaterials = await _repairMaterialsContext.RepairMaterials
                    .Where(rm => completedRequests.Select(cr => cr.RequestId).Contains(rm.RequestId))
                    .ToListAsync();

                // Общая стоимость материалов за период
                decimal totalCost = periodMaterials.Sum(m => m.Quantity * m.CostPerUnit);
                txtTotalMaterialsCost.Text = $"Общая стоимость материалов за период: {totalCost:C}";

                // Самый используемый материал
                var mostUsed = periodMaterials
                    .GroupBy(m => m.MaterialName)
                    .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault();

                if (mostUsed != null)
                {
                    txtMostUsedMaterial.Text = $"Самый используемый материал: {mostUsed.Name} ({mostUsed.Total} ед.)";
                }

                // Среднемесячные затраты
                var monthlyAverage = _materialUsageHistory
                    .Where(m => m.Year >= DateTime.Now.Year - 1) // Берем данные за последний год
                    .GroupBy(m => m.MaterialName)
                    .Select(g => new { Name = g.Key, Avg = g.Average(x => x.TotalCost) })
                    .ToList();

                if (monthlyAverage.Any())
                {
                    txtMonthlyCost.Text = $"Среднемесячные затраты по материалам:";
                    foreach (var item in monthlyAverage.OrderByDescending(x => x.Avg).Take(3))
                    {
                        txtMonthlyCost.Text += $"\n- {item.Name}: {item.Avg:C}";
                    }
                }

                // Материалы по типам проблем
                var requestsWithProblems = await _repairRequestsContext.RepairRequests
                    .Where(r => r.Status == RequestStatus.Завершена &&
                                r.RequestDate >= _startDate &&
                                r.RequestDate <= _endDate)
                    .ToListAsync();

                var materialsByProblem = from material in await _repairMaterialsContext.RepairMaterials.ToListAsync()
                                         join request in requestsWithProblems on material.RequestId equals request.RequestId
                                         group new { material, request } by request.Problem into g
                                         select new { Problem = g.Key, Materials = g.GroupBy(x => x.material.MaterialName) };

                txtMaterialByProblem.Text = "Материалы по типам проблем:";
                foreach (var problem in materialsByProblem)
                {
                    txtMaterialByProblem.Text += $"\n\n{problem.Problem}:";
                    foreach (var material in problem.Materials.OrderByDescending(m => m.Sum(x => x.material.Quantity)).Take(3))
                    {
                        txtMaterialByProblem.Text += $"\n- {material.Key}: {material.Sum(x => x.material.Quantity)} ед.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении аналитики: {ex.Message}");
            }
        }

        private void GeneratePurchaseRecommendations()
        {
            try
            {
                if (!_materialUsageHistory.Any()) return;

                // Рассчитываем рекомендации на 12 месяцев вперед
                var recommendations = new List<MaterialRecommendation>();
                var materialNames = _materialUsageHistory.Select(m => m.MaterialName).Distinct();

                foreach (var material in materialNames)
                {
                    var materialData = _materialUsageHistory
                        .Where(m => m.MaterialName == material)
                        .OrderBy(m => m.Year)
                        .ThenBy(m => m.Month)
                        .ToList();

                    if (materialData.Count < 3) continue; // Недостаточно данных для прогноза

                    // Рассчитываем среднемесячное потребление
                    double avgMonthly = materialData.Average(m => m.TotalQuantity);

                    // Рассчитываем сезонные коэффициенты (если есть данные за несколько лет)
                    var seasonalFactors = CalculateSeasonalFactors(materialData);

                    // Прогноз на 12 месяцев
                    var forecast = new List<MonthlyForecast>();
                    for (int i = 1; i <= 12; i++)
                    {
                        var forecastMonth = DateTime.Now.AddMonths(i);
                        int month = forecastMonth.Month;
                        int year = forecastMonth.Year;

                        // Базовый прогноз = среднее потребление * сезонный коэффициент
                        double baseForecast = avgMonthly;
                        if (seasonalFactors.ContainsKey(month))
                        {
                            baseForecast *= seasonalFactors[month];
                        }

                        // Добавляем 10% на рост (можно настроить или сделать динамическим)
                        double forecastValue = baseForecast * 1.1;

                        forecast.Add(new MonthlyForecast
                        {
                            Month = month,
                            Year = year,
                            ForecastQuantity = (int)Math.Ceiling(forecastValue)
                        });
                    }

                    // Общее рекомендуемое количество на год
                    int totalRecommendation = (int)Math.Ceiling(forecast.Average(f => f.ForecastQuantity) * 12);

                    recommendations.Add(new MaterialRecommendation
                    {
                        MaterialName = material,
                        RecommendedQuantity = totalRecommendation,
                        MonthlyForecast = forecast
                    });
                }

                // Формируем текст рекомендаций
                txtSuggestedMaterials.Text = "Рекомендуемые закупки на следующие 12 месяцев:\n\n";
                foreach (var rec in recommendations.OrderByDescending(r => r.RecommendedQuantity).Take(5))
                {
                    txtSuggestedMaterials.Text += $"- {rec.MaterialName}: {rec.RecommendedQuantity} ед.\n";

                    // Добавляем сезонные рекомендации
                    var peakMonths = rec.MonthlyForecast
                        .OrderByDescending(f => f.ForecastQuantity)
                        .Take(2)
                        .Select(f => $"{GetMonthName(f.Month)} {f.Year} (≈{f.ForecastQuantity} ед.)");

                    if (peakMonths.Any())
                    {
                        txtSuggestedMaterials.Text += $"  Пик спроса: {string.Join(", ", peakMonths)}\n";
                    }
                }

                // Добавляем общие рекомендации
                txtSuggestedMaterials.Text += "\nОбщие рекомендации:\n";
                txtSuggestedMaterials.Text += "- Увеличить закупки перед началом учебного года (август-сентябрь)\n";
                txtSuggestedMaterials.Text += "- Сантехнические материалы чаще требуются зимой\n";
                txtSuggestedMaterials.Text += "- Электротехника - равномерный спрос в течение года";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации рекомендаций: {ex.Message}");
            }
        }

        private Dictionary<int, double> CalculateSeasonalFactors(List<MaterialUsage> materialData)
        {
            var factors = new Dictionary<int, double>();
            var monthlyGroups = materialData.GroupBy(m => m.Month);

            foreach (var group in monthlyGroups)
            {
                double avgForMonth = group.Average(m => m.TotalQuantity);
                double overallAvg = materialData.Average(m => m.TotalQuantity);
                factors[group.Key] = avgForMonth / overallAvg;
            }

            // Заполняем недостающие месяцы нейтральным коэффициентом 1.0
            for (int month = 1; month <= 12; month++)
            {
                if (!factors.ContainsKey(month))
                {
                    factors[month] = 1.0;
                }
            }

            return factors;
        }

        private string GetMonthName(int month)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
        }

        private void CbTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTimeRange.SelectedItem == null) return;

            string selectedRange = (cbTimeRange.SelectedItem as ComboBoxItem).Content.ToString();
            DateTime now = DateTime.Now;

            switch (selectedRange)
            {
                case "Текущий месяц":
                    _startDate = new DateTime(now.Year, now.Month, 1);
                    _endDate = _startDate.AddMonths(1).AddDays(-1);
                    spCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Последние 7 дней":
                    _startDate = now.AddDays(-7);
                    _endDate = now;
                    spCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Последние 30 дней":
                    _startDate = now.AddDays(-30);
                    _endDate = now;
                    spCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Пользовательский":
                    spCustomRange.Visibility = Visibility.Visible;
                    dpStartDate.SelectedDate = _startDate;
                    dpEndDate.SelectedDate = _endDate;
                    return;
            }

            UpdateAnalytics();
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null) return;

            _startDate = dpStartDate.SelectedDate.Value;
            _endDate = dpEndDate.SelectedDate.Value;

            if (_startDate > _endDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания");
                return;
            }

            UpdateAnalytics();
        }

        public class MaterialUsage
        {
            public string MaterialName { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public int TotalQuantity { get; set; }
            public decimal TotalCost { get; set; }
        }

        public class MaterialRecommendation
        {
            public string MaterialName { get; set; }
            public int RecommendedQuantity { get; set; }
            public List<MonthlyForecast> MonthlyForecast { get; set; }
        }

        public class MonthlyForecast
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public int ForecastQuantity { get; set; }
        }
    }
}