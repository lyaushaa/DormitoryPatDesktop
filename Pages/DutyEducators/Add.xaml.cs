using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DormitoryPATDesktop.Pages.DutyEducators
{
    /// <summary>
    /// Логика взаимодействия для Add.xaml
    /// </summary>
    public partial class Add : Page
    {
        private readonly Models.DutyEducators _duty;
        private readonly bool _isNewDuty;
        private readonly EmployeesContext _employeesContext;
        private readonly DutyEducatorsContext _dutyContext;

        public string TitleName => _isNewDuty ? "Добавление дежурства" : "Редактирование дежурства";

        public Add(Models.DutyEducators duty = null)
        {
            InitializeComponent();
            _duty = duty ?? new Models.DutyEducators();
            _isNewDuty = duty == null;
            _employeesContext = new EmployeesContext();
            _dutyContext = new DutyEducatorsContext();

            DataContext = this;
            LoadEducators();
            LoadDutyData();
        }

        private void LoadEducators()
        {
            try
            {
                // Загружаем только сотрудников с ролью "Дежурный воспитатель"
                var educators = _employeesContext.Employees
                    .Where(e => e.EmployeeRole == EmployeeRole.Дежурный_воспитатель)
                    .ToList();

                cmbEducator.ItemsSource = educators;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки воспитателей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDutyData()
        {
            if (!_isNewDuty)
            {
                dpDate.SelectedDate = _duty.Date;
                txtContactNumber.Text = _duty.ContactNumber;

                // Устанавливаем категорию этажей
                foreach (ComboBoxItem item in cmbFloor.Items)
                {
                    if (item.Content.ToString() == _duty.Floor.ToString().Replace("_", "-"))
                    {
                        cmbFloor.SelectedItem = item;
                        break;
                    }
                }

                // Устанавливаем воспитателя
                if (_duty.EmployeeId > 0)
                {
                    foreach (Employees educator in cmbEducator.Items)
                    {
                        if (educator.EmployeeId == _duty.EmployeeId)
                        {
                            cmbEducator.SelectedItem = educator;
                            break;
                        }
                    }
                }
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
                // Проверяем, что для выбранной даты и категории этажей еще нет записи
                var selectedDate = dpDate.SelectedDate.Value.Date;
                var selectedFloor = cmbFloor.SelectedItem is ComboBoxItem floorItem
                    ? floorItem.Content.ToString() switch
                    {
                        "Этажи 2-4" => EducatorFloor.Floor2_4,
                        "Этажи 5-7" => EducatorFloor.Floor5_7,
                        _ => throw new InvalidOperationException("Неверная категория этажей")
                    }
                    : throw new InvalidOperationException("Категория этажей не выбрана");

                // Проверяем существующую запись (кроме текущей редактируемой)
                var existingDuty = _dutyContext.DutyEducators
                    .FirstOrDefault(d => d.Date.Date == selectedDate &&
                                       d.Floor == selectedFloor &&
                                       (_isNewDuty || d.DutyId != _duty.DutyId));

                if (existingDuty != null)
                {
                    MessageBox.Show($"Для выбранной даты и категории этажей уже назначен дежурный воспитатель: {existingDuty.Employees?.FIO}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_isNewDuty)
                {
                    _duty.Date = selectedDate;
                    _duty.Floor = selectedFloor;
                    _duty.ContactNumber = txtContactNumber.Text;
                    _duty.EmployeeId = ((Employees)cmbEducator.SelectedItem).EmployeeId;

                    _dutyContext.DutyEducators.Add(_duty);
                }
                else
                {
                    var dutyToUpdate = _dutyContext.DutyEducators.Find(_duty.DutyId);
                    if (dutyToUpdate == null)
                    {
                        MessageBox.Show("Запись о дежурстве не найдена в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dutyToUpdate.Date = selectedDate;
                    dutyToUpdate.Floor = selectedFloor;
                    dutyToUpdate.ContactNumber = txtContactNumber.Text;
                    dutyToUpdate.EmployeeId = ((Employees)cmbEducator.SelectedItem).EmployeeId;
                }

                _dutyContext.SaveChanges();

                MessageBox.Show(_isNewDuty ?
                    "Дежурство успешно добавлено!" : "Изменения сохранены!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                var mainPage = new Main();
                mainPage.CurrentModule = "DutyEducator";
                mainPage.LoadModule("DutyEducator");
                MainWindow.init.OpenPages(mainPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (!dpDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату дежурства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbFloor.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию этажей", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbEducator.SelectedItem == null)
            {
                MessageBox.Show("Выберите дежурного воспитателя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContactNumber.Text))
            {
                MessageBox.Show("Введите контактный телефон", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "DutyEducator";
            mainPage.LoadModule("DutyEducator");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}
