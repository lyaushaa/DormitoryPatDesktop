using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.Students
{
    public partial class Add : Page
    {
        private readonly Models.Students _student;
        private readonly bool _isNewStudent;

        public string TitleName => _isNewStudent ? "Добавление нового студента" : "Редактирование студента";

        public Add(Models.Students student = null)
        {
            InitializeComponent();
            _student = student ?? new Models.Students();
            _isNewStudent = student == null;

            DataContext = this;
            LoadStudentData();
        }

        private void LoadStudentData()
        {
            if (!_isNewStudent)
            {
                txtFIO.Text = _student.FIO;
                txtGroup.Text = _student.Group;
                txtPhone.Text = _student.PhoneNumber;
                dpBirthDate.SelectedDate = _student.DateOfBirth;
                dpCheckInDate.SelectedDate = _student.CheckInDate;
                dpCheckOutDate.SelectedDate = _student.CheckOutDate;

                foreach (ComboBoxItem item in cmbFloor.Items)
                {
                    if (item.Content.ToString() == _student.Floor.ToString())
                    {
                        cmbFloor.SelectedItem = item;
                        break;
                    }
                }

                txtRoom.Text = _student.Room.ToString();

                foreach (ComboBoxItem item in cmbRole.Items)
                {
                    if (item.Content.ToString() == _student.StudentRole.ToString().Replace("_", " "))
                    {
                        cmbRole.SelectedItem = item;
                        break;
                    }
                }

                txtTelegramId.Text = _student.TelegramId?.ToString() ?? "";
            }
            else
            {
                dpBirthDate.SelectedDate = DateTime.Now.AddYears(-16);
                dpCheckInDate.SelectedDate = DateTime.Now; // Значение по умолчанию
                cmbFloor.SelectedIndex = 0;
                cmbRole.SelectedIndex = 0;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new StudentsContext())
                {
                    Models.Students studentToSave;

                    if (_isNewStudent)
                    {
                        studentToSave = new Models.Students
                        {
                            FIO = txtFIO.Text,
                            Group = txtGroup.Text,
                            PhoneNumber = txtPhone.Text,
                            DateOfBirth = dpBirthDate.SelectedDate ?? DateTime.Now,
                            CheckInDate = dpCheckInDate.SelectedDate,
                            CheckOutDate = dpCheckOutDate.SelectedDate,
                            Floor = int.Parse((cmbFloor.SelectedItem as ComboBoxItem).Content.ToString()),
                            Room = int.Parse(txtRoom.Text),
                            TelegramId = long.TryParse(txtTelegramId.Text, out var id) ? id : (long?)null,
                            StudentRole = StudentRole.Студент
                        };
                        context.Students.Add(studentToSave);
                    }
                    else
                    {
                        studentToSave = context.Students
                            .FirstOrDefault(s => s.StudentId == _student.StudentId);

                        if (studentToSave == null)
                        {
                            MessageBox.Show("Студент не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Общие поля для нового и существующего студента
                    studentToSave.FIO = txtFIO.Text;
                    studentToSave.Group = txtGroup.Text;
                    studentToSave.PhoneNumber = txtPhone.Text;
                    studentToSave.DateOfBirth = dpBirthDate.SelectedDate ?? DateTime.Now;
                    studentToSave.CheckInDate = dpCheckInDate.SelectedDate;
                    studentToSave.CheckOutDate = dpCheckOutDate.SelectedDate;
                    studentToSave.Floor = int.Parse((cmbFloor.SelectedItem as ComboBoxItem).Content.ToString());
                    studentToSave.Room = int.Parse(txtRoom.Text);
                    studentToSave.TelegramId = long.TryParse(txtTelegramId.Text, out var telegramId) ? telegramId : (long?)null;

                    if (cmbRole.SelectedItem is ComboBoxItem selectedRole)
                    {
                        studentToSave.StudentRole = selectedRole.Content.ToString() switch
                        {
                            "Староста этажа" => StudentRole.Староста_этажа,
                            "Председатель Студенческого совета общежития" => StudentRole.Председатель_Студенческого_совета_общежития,
                            _ => StudentRole.Студент
                        };
                    }

                    context.SaveChanges();

                    MessageBox.Show(_isNewStudent ?
                        "Студент успешно добавлен!" : "Изменения сохранены!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainPage = new Main();
                    mainPage.CurrentModule = "Students";
                    mainPage.LoadModule("Students");
                    MainWindow.init.OpenPages(mainPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFIO.Text))
            {
                MessageBox.Show("Введите ФИО студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(txtFIO.Text, @"^[А-ЯЁ][а-яё]+\s[А-ЯЁ][а-яё]+\s[А-ЯЁ][а-яё]+$"))
            {
                MessageBox.Show("Неверный формат ФИО. Требуется: Кириллица, формат 'Фамилия Имя Отчество'", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtGroup.Text) || !Regex.IsMatch(txtGroup.Text, @"^[А-Яа-я]{2,3}-\d{2}-\d$"))
            {
                MessageBox.Show("Введите группу в формате: ИСП-21-2 (специальность-год-номер группы)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите номер телефона", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(txtPhone.Text, @"^7\d{10}$"))
            {
                MessageBox.Show("Неверный формат телефона. Требуется: 11 цифр, начинается с 7 (например: 79161234567)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpBirthDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpCheckInDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату заселения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpCheckOutDate.SelectedDate.HasValue && dpCheckOutDate.SelectedDate < dpCheckInDate.SelectedDate)
            {
                MessageBox.Show("Дата выселения не может быть меньше даты заселения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbFloor.SelectedItem == null)
            {
                MessageBox.Show("Выберите этаж", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int selectedFloor;
            if (!int.TryParse(((ComboBoxItem)cmbFloor.SelectedItem).Content.ToString(), out selectedFloor))
            {
                MessageBox.Show("Не удалось определить выбранный этаж", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtRoom.Text, out var room) || room <= 0)
            {
                MessageBox.Show("Введите корректный номер комнаты", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int minRoom = selectedFloor * 100 + 1;
            int maxRoom = selectedFloor * 100 + 22;

            if (room < minRoom || room > maxRoom)
            {
                MessageBox.Show($"Для {selectedFloor} этажа номер комнаты должен быть в диапазоне от {minRoom} до {maxRoom}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "Students";
            mainPage.LoadModule("Students");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}