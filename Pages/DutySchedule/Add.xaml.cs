using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public string TitleName => _isEditMode ? "Редактирование дежурства" : "Новое дежурство";

        public Add(Models.DutySchedule schedule = null)
        {
            InitializeComponent();
            _schedule = schedule ?? new Models.DutySchedule();
            _isEditMode = schedule != null;
            DataContext = this;
            LoadData();
        }

        private void LoadData()
        {
            if (_isEditMode)
            {
                // Установка этажа
                foreach (ComboBoxItem item in cmbFloor.Items)
                {
                    if (item.Content.ToString() == _schedule.Floor.ToString())
                    {
                        cmbFloor.SelectedItem = item;
                        break;
                    }
                }

                txtRoom.Text = _schedule.Room.ToString();
                dpDate.SelectedDate = _schedule.Date;
            }
            else
            {
                dpDate.SelectedDate = DateTime.Today;
                cmbFloor.SelectedIndex = 0;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new DutyScheduleContext())
                {
                    if (_isEditMode)
                    {
                        context.Entry(_schedule).State = EntityState.Modified;
                    }
                    else
                    {
                        context.DutySchedule.Add(_schedule);
                    }

                    _schedule.Floor = int.Parse((cmbFloor.SelectedItem as ComboBoxItem).Content.ToString());
                    _schedule.Room = int.Parse(txtRoom.Text);
                    _schedule.Date = dpDate.SelectedDate ?? DateTime.Today;

                    context.SaveChanges();
                    MessageBox.Show("Дежурство сохранено успешно!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    MainWindow.init.OpenPages(new Main());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (cmbFloor.SelectedItem == null)
            {
                MessageBox.Show("Выберите этаж", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtRoom.Text, out _))
            {
                MessageBox.Show("Введите корректный номер комнаты", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "DutySchedule";
            mainPage.LoadModule("DutySchedule");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}