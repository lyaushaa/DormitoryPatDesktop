using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using DormitoryPATDesktop.Models;
using DormitoryPATDesktop.Context;

namespace DormitoryPATDesktop.Pages
{
    public partial class Login : Page
    {
        private readonly EmployeesContext _context;
        private bool _isUpdating; // Flag to prevent recursive updates

        public Login()
        {
            InitializeComponent();
            _context = new EmployeesContext();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string phoneOrLogin = txtPhoneOrLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(phoneOrLogin) || string.IsNullOrEmpty(password))
            {
                ShowError("Заполните все поля.");
                return;
            }

            var employee = _context.Employees
                .FirstOrDefault(emp => emp.PhoneNumber == phoneOrLogin || emp.Login == phoneOrLogin);

            if (employee == null)
            {
                ShowError("Пользователь не найден.");
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, employee.Password))
            {
                ShowError("Неверный пароль.");
                return;
            }

            Session.CurrentEmployeeId = employee.EmployeeId;
            Session.CurrentEmployeeRole = employee.EmployeeRole;

            MainWindow.init.OpenPages(new Main());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            txtPhoneOrLogin.Text = string.Empty;
            txtPassword.Password = string.Empty;
            txtPasswordVisible.Text = string.Empty;
            txtErrorMessage.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
            btnShowPassword.Content = "👁";
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPasswordVisible.Visibility == Visibility.Visible)
            {
                // Hide password
                _isUpdating = true;
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnShowPassword.Content = "👁";
                _isUpdating = false;
            }
            else
            {
                // Show password
                _isUpdating = true;
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnShowPassword.Content = "👁‍🗨";
                _isUpdating = false;
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isUpdating && txtPasswordVisible.Visibility == Visibility.Visible)
            {
                _isUpdating = true;
                txtPasswordVisible.Text = txtPassword.Password;
                _isUpdating = false;
            }
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating && txtPasswordVisible.Visibility == Visibility.Visible)
            {
                _isUpdating = true;
                txtPassword.Password = txtPasswordVisible.Text;
                _isUpdating = false;
            }
        }

        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            txtErrorMessage.Visibility = Visibility.Visible;
        }
    }

    public static class Session
    {
        public static long? CurrentEmployeeId { get; set; }
        public static EmployeeRole? CurrentEmployeeRole { get; set; }
    }
}