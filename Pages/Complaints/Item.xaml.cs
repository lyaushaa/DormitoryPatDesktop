using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DormitoryPATDesktop.Pages.Complaints
{
    public partial class Item : UserControl
    {
        private readonly ComplaintsContext _context = new ComplaintsContext();
        private List<Models.Complaints> _allComplaints;

        public Item()
        {
            InitializeComponent();
            _allComplaints = new List<Models.Complaints>();
            ComplaintsDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            if (Session.CurrentEmployeeRole == EmployeeRole.Администратор) Action_dgtc.Visibility = Visibility.Visible;
            LoadComplaints();
        }

        private void LoadComplaints()
        {
            try
            {
                _allComplaints = _context.Complaints
                    .Include(c => c.Student) // Включаем Student, обрабатываем null
                    .Include(c => c.Reviewer)
                    .ToList() ?? new List<Models.Complaints>();

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке жалоб: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allComplaints = new List<Models.Complaints>();
                ApplyFiltersAndSort();
            }
        }

        private void ApplyFiltersAndSort()
        {
            try
            {
                if (_allComplaints == null)
                {
                    _allComplaints = new List<Models.Complaints>();
                }

                if (ComplaintsDataGrid == null)
                {
                    return;
                }

                IQueryable<Models.Complaints> filteredComplaints = _allComplaints.AsQueryable();

                // Применяем фильтр по статусу
                if (StatusFilterComboBox.SelectedIndex > 0 &&
                    StatusFilterComboBox.SelectedItem is ComboBoxItem selectedStatus)
                {
                    var statusValue = selectedStatus.Content.ToString() switch
                    {
                        "Создана" => ComplaintStatus.Создана,
                        "В обработке" => ComplaintStatus.В_обработке,
                        "Завершена" => ComplaintStatus.Завершена,
                        "Отклонена" => ComplaintStatus.Отклонена,
                        _ => (ComplaintStatus?)null
                    };

                    if (statusValue.HasValue)
                    {
                        filteredComplaints = filteredComplaints.Where(c => c.Status == statusValue.Value);
                    }
                }

                // Применяем фильтр по диапазону дат
                DateTime? startDate = StartDatePicker.SelectedDate;
                DateTime? endDate = EndDatePicker.SelectedDate;

                if (startDate.HasValue || endDate.HasValue)
                {
                    if (startDate.HasValue && !endDate.HasValue)
                    {
                        filteredComplaints = filteredComplaints.Where(c => c.SubmissionDate.Date >= startDate.Value.Date);
                    }
                    else if (!startDate.HasValue && endDate.HasValue)
                    {
                        filteredComplaints = filteredComplaints.Where(c => c.SubmissionDate.Date <= endDate.Value.Date);
                    }
                    else if (startDate.HasValue && endDate.HasValue)
                    {
                        if (endDate.Value < startDate.Value)
                        {
                            MessageBox.Show("Конечная дата не может быть меньше начальной даты.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            EndDatePicker.SelectedDate = startDate; // Устанавливаем конец равным началу
                            return;
                        }
                        filteredComplaints = filteredComplaints.Where(c => c.SubmissionDate.Date >= startDate.Value.Date &&
                                                                         c.SubmissionDate.Date <= endDate.Value.Date);
                    }
                }

                // Применяем поиск по тексту
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    var searchText = SearchTextBox.Text.ToLower();
                    filteredComplaints = filteredComplaints.Where(c =>
                        (c.ComplaintText != null && c.ComplaintText.ToLower().Contains(searchText)) ||
                        (c.Student != null && c.Student.FIO != null && c.Student.FIO.ToLower().Contains(searchText)) ||
                        (c.Comment != null && c.Comment.ToLower().Contains(searchText)));
                }

                // Сортируем: завершенные и отклоненные в конце, затем по дате
                var result = filteredComplaints
                    .OrderByDescending(c =>
                        c.Status == ComplaintStatus.Завершена ||
                        c.Status == ComplaintStatus.Отклонена ? 0 : 1)
                    .ThenByDescending(c => c.SubmissionDate)
                    .ToList();

                ComplaintsDataGrid.ItemsSource = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации жалоб: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ComplaintsDataGrid.ItemsSource = new List<Models.Complaints>();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.Complaints complaintToDelete)
            {
                var confirmResult = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пожелание №{complaintToDelete.ComplaintId}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var deleteContext = new ComplaintsContext())
                        {
                            var complaint = deleteContext.Complaints
                                .FirstOrDefault(c => c.ComplaintId == complaintToDelete.ComplaintId);

                            if (complaint != null)
                            {
                                deleteContext.Complaints.Remove(complaint);
                                deleteContext.SaveChanges();
                                LoadComplaints();
                                MessageBox.Show(
                                    $"Пожелание №{complaintToDelete.ComplaintId} успешно удалена.",
                                    "Успех",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при удалении пожелания: {ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadComplaints();
        }

        private void ComplaintsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ComplaintsDataGrid.SelectedItem is Models.Complaints selectedComplaint)
            {
                var processingPage = new Add(selectedComplaint);
                MainWindow.init.OpenPages(processingPage);
            }
        }
    }
}