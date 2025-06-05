using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.DutySchedule
{
    public partial class Item : UserControl
    {
        private readonly DutyScheduleContext _context = new DutyScheduleContext();
        private List<Models.DutySchedule> _allSchedules;

        public Item()
        {
            InitializeComponent();
            _allSchedules = new List<Models.DutySchedule>();
            ScheduleDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            LoadSchedules();
        }

        private void LoadSchedules()
        {
            try
            {
                _allSchedules = _context.DutySchedule
                    .ToList() ?? new List<Models.DutySchedule>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписаний: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allSchedules == null) _allSchedules = new List<Models.DutySchedule>();
            if (ScheduleDataGrid == null) return;

            IQueryable<Models.DutySchedule> filtered = _allSchedules.AsQueryable();

            // Фильтр по этажу
            if (cmbFloorFilter.SelectedIndex > 0 &&
                cmbFloorFilter.SelectedItem is ComboBoxItem selectedFloor &&
                int.TryParse(selectedFloor.Content.ToString(), out var floor))
            {
                filtered = filtered.Where(d => d.Floor == floor);
            }

            // Фильтр по комнате
            if (!string.IsNullOrWhiteSpace(txtRoomFilter.Text) &&
                int.TryParse(txtRoomFilter.Text, out var room))
            {
                filtered = filtered.Where(d => d.Room == room);
            }

            // Фильтр по дате
            if (dpDateFilter.SelectedDate.HasValue)
            {
                filtered = filtered.Where(d => d.Date.Date == dpDateFilter.SelectedDate.Value.Date);
            }

            // Сортировка по дате и этажу
            filtered = filtered.OrderBy(d => d.Date)
                              .ThenBy(d => d.Floor);

            ScheduleDataGrid.ItemsSource = filtered.ToList();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            LoadSchedules();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSchedules();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.DutySchedule schedule)
            {
                var confirm = MessageBox.Show($"Удалить дежурство {schedule.ScheduleId}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.DutySchedule.Remove(schedule);
                        _context.SaveChanges();
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ScheduleDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScheduleDataGrid.SelectedItem is Models.DutySchedule selectedSchedule)
            {
                var editPage = new Add(selectedSchedule);
                MainWindow.init.OpenPages(editPage);
            }
        }
    }
}