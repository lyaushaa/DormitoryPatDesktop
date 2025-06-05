using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DormitoryPATDesktop.Pages.DutyEducators
{
    public partial class Item : UserControl
    {
        private readonly DutyEducatorsContext _context;
        private List<Models.DutyEducators> _allDuties;

        public Item()
        {
            InitializeComponent();
            _context = new DutyEducatorsContext();
            _allDuties = new List<Models.DutyEducators>();
            LoadDuties();
        }

        private void LoadDuties()
        {
            try
            {
                _allDuties = _context.DutyEducators
                    .Include(d => d.Employees)
                    .OrderBy(d => d.Date)
                    .ThenBy(d => d.Floor)
                    .ToList();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дежурств: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allDuties == null) return;

            var filtered = _allDuties.AsQueryable();

            // Фильтр по дате (применяется только если дата выбрана)
            if (dpDateFilter.SelectedDate.HasValue)
            {
                filtered = filtered.Where(d => d.Date.Date == dpDateFilter.SelectedDate.Value.Date);
            }

            // Фильтр по категории этажей
            if (cmbFloorFilter.SelectedIndex > 0 && cmbFloorFilter.SelectedItem is ComboBoxItem selectedFloor)
            {
                var floor = selectedFloor.Content.ToString() switch
                {
                    "Этажи 2-4" => EducatorFloor.Floor2_4,
                    "Этажи 5-7" => EducatorFloor.Floor5_7,
                    _ => (EducatorFloor?)null
                };

                if (floor.HasValue)
                {
                    filtered = filtered.Where(d => d.Floor == floor.Value);
                }
            }

            DutyDataGrid.ItemsSource = filtered.ToList();
        }

        private void ResetDateFilter_Click(object sender, RoutedEventArgs e)
        {
            dpDateFilter.SelectedDate = null;
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDuties();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.DutyEducators duty)
            {
                var confirm = MessageBox.Show($"Удалить запись о дежурстве {duty.Employees?.FIO}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.DutyEducators.Remove(duty);
                        _context.SaveChanges();
                        LoadDuties();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DutyDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DutyDataGrid.SelectedItem is Models.DutyEducators selectedDuty)
            {
                OpenDutyProcessingPage(selectedDuty);
            }
        }

        private void OpenDutyProcessingPage(Models.DutyEducators duty)
        {
            var processingPage = new Add(duty);
            MainWindow.init.OpenPages(processingPage);
        }
    }
}