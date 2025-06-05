using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DormitoryPATDesktop.Pages.RepairRequests
{
    public partial class Item : UserControl
    {
        private readonly RepairRequestsContext _context;
        private List<Models.RepairRequests> _allRequests;

        public Item()
        {
            InitializeComponent();
            _context = new RepairRequestsContext();
            _allRequests = new List<Models.RepairRequests>();
            RequestDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                _allRequests = _context.RepairRequests
                    .Include(r => r.Master)
                    .ToList() ?? new List<Models.RepairRequests>();

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allRequests = new List<Models.RepairRequests>();
                ApplyFiltersAndSort();
            }
        }

        private void ApplyFiltersAndSort()
        {
            try
            {
                if (_allRequests == null) _allRequests = new List<Models.RepairRequests>();
                if (RequestDataGrid == null) return;

                IQueryable<Models.RepairRequests> filteredRequests = _allRequests.AsQueryable();

                // Фильтр по типу проблемы
                if (ProblemTypeFilterComboBox.SelectedIndex > 0 &&
                    ProblemTypeFilterComboBox.SelectedItem is ComboBoxItem selectedProblemType)
                {
                    var problemTypeValue = selectedProblemType.Content.ToString() switch
                    {
                        "Электрика" => ProblemType.Электрика,
                        "Сантехника" => ProblemType.Сантехника,
                        "Мебель" => ProblemType.Мебель,
                        _ => (ProblemType?)null
                    };

                    if (problemTypeValue.HasValue)
                    {
                        filteredRequests = filteredRequests.Where(r => r.Problem == problemTypeValue.Value);
                    }
                }

                // Фильтр по статусу
                if (StatusFilterComboBox.SelectedIndex > 0 &&
                    StatusFilterComboBox.SelectedItem is ComboBoxItem selectedStatus)
                {
                    var statusValue = selectedStatus.Content.ToString() switch
                    {
                        "Создана" => RequestStatus.Создана,
                        "В обработке" => RequestStatus.В_обработке,
                        "Ожидает запчастей" => RequestStatus.Ожидает_запчастей,
                        "Завершена" => RequestStatus.Завершена,
                        "Отклонена" => RequestStatus.Отклонена,
                        _ => (RequestStatus?)null
                    };

                    if (statusValue.HasValue)
                    {
                        filteredRequests = filteredRequests.Where(r => r.Status == statusValue.Value);
                    }
                }

                // Поиск по тексту
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    var searchText = SearchTextBox.Text.ToLower();
                    filteredRequests = filteredRequests.Where(r =>
                        (r.Location != null && r.Location.ToLower().Contains(searchText)) ||
                        (r.UserComment != null && r.UserComment.ToLower().Contains(searchText)));
                }

                // Сортировка: завершенные и отклоненные в конце
                var result = filteredRequests
                    .OrderByDescending(r =>
                        r.Status == RequestStatus.Завершена ||
                        r.Status == RequestStatus.Отклонена ? 0 : 1)
                    .ThenByDescending(r => r.LastStatusChange)
                    .ToList();

                RequestDataGrid.ItemsSource = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                RequestDataGrid.ItemsSource = new List<Models.RepairRequests>();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.RepairRequests requestToDelete)
            {
                var confirmResult = MessageBox.Show(
                    $"Вы уверены, что хотите удалить заявку №{requestToDelete.RequestId}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var deleteContext = new RepairRequestsContext())
                        {
                            var request = deleteContext.RepairRequests
                                .FirstOrDefault(r => r.RequestId == requestToDelete.RequestId);

                            if (request != null)
                            {
                                deleteContext.RepairRequests.Remove(request);
                                deleteContext.SaveChanges();
                                LoadRequests();
                                MessageBox.Show(
                                    $"Заявка №{requestToDelete.RequestId} успешно удалена.",
                                    "Успех",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при удалении заявки: {ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void RequestDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RequestDataGrid.SelectedItem is Models.RepairRequests selectedRequest)
            {
                OpenRequestProcessingPage(selectedRequest);
            }
        }

        private void OpenRequestProcessingPage(Models.RepairRequests request)
        {
            var processingPage = new Add(request);
            MainWindow.init.OpenPages(processingPage);
        }
    }
}