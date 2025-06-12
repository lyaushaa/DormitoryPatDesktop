using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.DutySchedule
{
    public partial class Add : Page
    {
        private readonly Models.DutySchedule _schedule;
        private readonly bool _isEditMode;
        private readonly StudentsContext _studentsContext = new StudentsContext();
        private List<ScheduleEntry> _scheduleEntries = new List<ScheduleEntry>();

        public string TitleName => _isEditMode ? "Редактирование дежурства" : "Новое дежурство";

        public Add(Models.DutySchedule schedule = null)
        {
            InitializeComponent();
            _schedule = schedule ?? new Models.DutySchedule();
            _isEditMode = schedule != null;
            DataContext = this;
            LoadData();
            InitializeDataGrid();
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                // Загружаем все даты месяца для редактирования
                using (var context = new DutyScheduleContext())
                {
                    int year = _schedule.Date.Year;
                    int month = _schedule.Date.Month;
                    int floor = _schedule.Floor;
                    int daysInMonth = DateTime.DaysInMonth(year, month);

                    var existingSchedules = context.DutySchedule
                        .Where(ds => ds.Floor == floor && ds.Date.Year == year && ds.Date.Month == month)
                        .ToDictionary(ds => ds.Date.Day, ds => ds.Room.ToString());

                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        _scheduleEntries.Add(new ScheduleEntry
                        {
                            Year = year,
                            Month = _schedule.Date.ToString("MM"),
                            Day = day.ToString(),
                            Floor = floor,
                            Room = existingSchedules.ContainsKey(day) ? existingSchedules[day] : ""
                        });
                    }
                }
                cmbYear.SelectedItem = cmbYear.Items.Cast<ComboBoxItem>().FirstOrDefault(i => int.Parse(i.Content.ToString()) == _schedule.Date.Year);
                cmbMonth.SelectedItem = cmbMonth.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == _schedule.Date.ToString("MMMM"));
                cmbFloor.SelectedItem = cmbFloor.Items.Cast<ComboBoxItem>().FirstOrDefault(i => int.Parse(i.Content.ToString()) == _schedule.Floor);
            }
            else
            {
                cmbYear.SelectedIndex = 1; // По умолчанию
                cmbMonth.SelectedIndex = DateTime.Now.Month - 1; // Текущий месяц
                cmbFloor.SelectedIndex = 0; // По умолчанию 2 этаж
            }
            UpdateDataGrid();
        }

        private void InitializeDataGrid()
        {
            dgSchedule.ItemsSource = _scheduleEntries;
        }

        private void cmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDataGrid();
        }

        private void cmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDataGrid();
        }

        private void cmbFloor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDataGrid();
        }

        private void UpdateDataGrid()
        {
            if (cmbYear.SelectedItem == null || cmbMonth.SelectedItem == null || cmbFloor.SelectedItem == null) return;

            int year = int.Parse((cmbYear.SelectedItem as ComboBoxItem).Content.ToString());
            string monthName = (cmbMonth.SelectedItem as ComboBoxItem).Content.ToString();
            int monthNumber = DateTime.ParseExact(monthName, "MMMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            int floor = int.Parse((cmbFloor.SelectedItem as ComboBoxItem).Content.ToString());

            // Создаем записи для каждого дня месяца
            _scheduleEntries.Clear();
            int daysInMonth = DateTime.DaysInMonth(year, monthNumber);

            using (var context = new DutyScheduleContext())
            {
                var existingSchedules = context.DutySchedule
                    .Where(ds => ds.Floor == floor && ds.Date.Year == year && ds.Date.Month == monthNumber)
                    .ToDictionary(ds => ds.Date.Day, ds => ds.Room.ToString());

                for (int day = 1; day <= daysInMonth; day++)
                {
                    _scheduleEntries.Add(new ScheduleEntry
                    {
                        Year = year,
                        Month = monthName,
                        Day = day.ToString(),
                        Floor = floor,
                        Room = existingSchedules.ContainsKey(day) ? existingSchedules[day] : ""
                    });
                }
            }

            dgSchedule.ItemsSource = null;
            dgSchedule.ItemsSource = _scheduleEntries;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbYear.SelectedItem == null || cmbMonth.SelectedItem == null || cmbFloor.SelectedItem == null)
            {
                MessageBox.Show("Выберите год, месяц и этаж.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new DutyScheduleContext())
                {
                    // Удаляем существующие записи для выбранного этажа, года и месяца
                    int year = int.Parse((cmbYear.SelectedItem as ComboBoxItem).Content.ToString());
                    string monthName = (cmbMonth.SelectedItem as ComboBoxItem).Content.ToString();
                    int monthNumber = DateTime.ParseExact(monthName, "MMMM", System.Globalization.CultureInfo.CurrentCulture).Month;
                    int floor = int.Parse((cmbFloor.SelectedItem as ComboBoxItem).Content.ToString());
                    var existingSchedules = context.DutySchedule
                        .Where(ds => ds.Floor == floor && ds.Date.Year == year && ds.Date.Month == monthNumber)
                        .ToList();
                    context.DutySchedule.RemoveRange(existingSchedules);

                    // Сохраняем новые записи только если комната указана и валидна
                    foreach (var entry in _scheduleEntries)
                    {
                        if (!string.IsNullOrWhiteSpace(entry.Room) && int.TryParse(entry.Room, out int room))
                        {
                            context.DutySchedule.Add(new Models.DutySchedule
                            {
                                Floor = floor,
                                Room = room,
                                Date = new DateTime(year, monthNumber, int.Parse(entry.Day))
                            });
                        }
                    }

                    context.SaveChanges();
                    MessageBox.Show("Дежурства сохранены успешно!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    var mainPage = new Main();
                    mainPage.CurrentModule = "DutySchedule";
                    mainPage.LoadModule("DutySchedule");
                    MainWindow.init.OpenPages(mainPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "DutySchedule";
            mainPage.LoadModule("DutySchedule");
            MainWindow.init.OpenPages(mainPage);
        }
    }

    // Вспомогательный класс для таблицы
    public class ScheduleEntry
    {
        public int Year { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }
        public int Floor { get; set; }
        public string Room { get; set; }
    }
}