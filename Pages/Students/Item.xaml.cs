using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DormitoryPATDesktop.Pages.Students
{
    public partial class Item : UserControl
    {
        private readonly StudentsContext _context;
        private List<Models.Students> _allStudents;

        public Item()
        {
            InitializeComponent();
            _context = new StudentsContext();
            _allStudents = new List<Models.Students>();
            StudentsDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            if (Session.CurrentEmployeeRole == EmployeeRole.Администратор || Session.CurrentEmployeeRole == EmployeeRole.Заведующий_общежитием) Action_dgtc.Visibility = Visibility.Visible;
            LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                _allStudents = _context.Students
                    .ToList() ?? new List<Models.Students>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки студентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allStudents == null) _allStudents = new List<Models.Students>();
            if (StudentsDataGrid == null) return;

            IQueryable<Models.Students> filtered = _allStudents.AsQueryable();

            // Фильтр по роли
            if (RoleFilterComboBox.SelectedIndex > 0 &&
                RoleFilterComboBox.SelectedItem is ComboBoxItem selectedRole)
            {
                var role = selectedRole.Content.ToString() switch
                {
                    "Студент" => StudentRole.Студент,
                    "Староста этажа" => StudentRole.Староста_этажа,
                    "Председатель Студенческого совета общежития" => StudentRole.Председатель_Студенческого_совета_общежития,
                    _ => (StudentRole?)null
                };

                if (role.HasValue)
                {
                    filtered = filtered.Where(s => s.StudentRole == role.Value);
                }
            }

            // Фильтр по этажу
            if (FloorFilterComboBox.SelectedIndex > 0 &&
                FloorFilterComboBox.SelectedItem is ComboBoxItem selectedFloor &&
                int.TryParse(selectedFloor.Content.ToString(), out var floor))
            {
                filtered = filtered.Where(s => s.Floor == floor);
            }

            // Фильтр по наличию Telegram
            if (chkHasTelegram.IsChecked == true)
            {
                filtered = filtered.Where(s => s.TelegramId.HasValue);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(s =>
                    s.FIO.ToLower().Contains(searchText) ||
                    s.Group.ToLower().Contains(searchText) || // Добавлен поиск по группе
                    s.PhoneNumber.Contains(searchText) ||
                    s.Room.ToString().Contains(searchText) ||
                    s.DateOfBirth.ToString().Contains(searchText) ||
                    s.TelegramId.ToString().Contains(searchText));
            }

            StudentsDataGrid.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.Students student)
            {
                var confirm = MessageBox.Show($"Удалить студента {student.FIO}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.Students.Remove(student);
                        _context.SaveChanges();
                        LoadStudents();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStudents();
        }

        private void StudentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StudentsDataGrid.SelectedItem is Models.Students selectedStudents)
            {
                var processingPage = new Add(selectedStudents);
                MainWindow.init.OpenPages(processingPage);
            }
        }
    }
}