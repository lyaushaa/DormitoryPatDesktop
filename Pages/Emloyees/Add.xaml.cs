using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.Emloyees
{
    public partial class Add : Page
    {
        private readonly Employees _employee;
        private readonly bool _isNewEmployee;
        private bool _isPasswordVisible = false;
        private string _currentPassword = "";

        public string TitleName => _isNewEmployee ? "Добавление нового сотрудника" : "Редактирование сотрудника";
        public string PasswordHint => _isNewEmployee ? "* Пароль будет зашифрован" : "* Пароль нельзя посмотреть, только изменить";

        public Add(Employees employee = null)
        {
            InitializeComponent();
            _employee = employee ?? new Employees();
            _isNewEmployee = employee == null;

            DataContext = this;
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            if (!_isNewEmployee)
            {
                txtFIO.Text = _employee.FIO;
                txtPhone.Text = _employee.PhoneNumber;
                txtLogin.Text = _employee.Login;

                foreach (ComboBoxItem item in cmbRole.Items)
                {
                    if (item.Content.ToString() == _employee.EmployeeRole.ToString().Replace("_", " "))
                    {
                        cmbRole.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                cmbRole.SelectedIndex = 0;
            }
        }

        private void btnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Показать пароль
                txtPasswordVisible.Text = _currentPassword;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                btnShowPassword.Content = "🔒";
                btnShowPassword.ToolTip = "Скрыть пароль";
            }
            else
            {
                // Скрыть пароль
                _currentPassword = txtPasswordVisible.Text;
                txtPassword.Password = _currentPassword;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnShowPassword.Content = "👁";
                btnShowPassword.ToolTip = "Показать пароль";
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _currentPassword = txtPassword.Password;
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPassword = txtPasswordVisible.Text;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new EmployeesContext())
                {
                    Employees employeeToSave;

                    if (_isNewEmployee)
                    {
                        employeeToSave = new Employees
                        {
                            FIO = txtFIO.Text,
                            PhoneNumber = txtPhone.Text,
                            Login = txtLogin.Text,
                            Password = HashPassword(_currentPassword),
                            EmployeeRole = EmployeeRole.Мастер
                        };
                        context.Employees.Add(employeeToSave);
                    }
                    else
                    {
                        employeeToSave = context.Employees
                            .FirstOrDefault(emp => emp.EmployeeId == _employee.EmployeeId);

                        if (employeeToSave == null)
                        {
                            MessageBox.Show("Сотрудник не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        employeeToSave.FIO = txtFIO.Text;
                        employeeToSave.PhoneNumber = txtPhone.Text;
                        employeeToSave.Login = txtLogin.Text;

                        if (!string.IsNullOrEmpty(_currentPassword))
                        {
                            employeeToSave.Password = HashPassword(_currentPassword);
                        }
                        
                        if (cmbRole.SelectedItem is ComboBoxItem selectedRole)
                        {
                            employeeToSave.EmployeeRole = selectedRole.Content.ToString() switch
                            {
                                "Мастер" => EmployeeRole.Мастер,
                                "Воспитатель" => EmployeeRole.Воспитатель,
                                "Заведующий общежитием" => EmployeeRole.Заведующий_общежитием,
                                "Администратор" => EmployeeRole.Администратор,
                                _ => employeeToSave.EmployeeRole
                            };
                        }
                    }

                    context.SaveChanges();

                    MessageBox.Show(_isNewEmployee ?
                        "Сотрудник успешно добавлен!" : "Изменения сохранены!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainPage = new Main();
                    mainPage.CurrentModule = "Employees";
                    mainPage.LoadModule("Employees");
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
                MessageBox.Show("Введите ФИО сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите номер телефона", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_isNewEmployee && string.IsNullOrEmpty(_currentPassword))
            {
                MessageBox.Show("Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите должность", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "Employees";
            mainPage.LoadModule("Employees");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}