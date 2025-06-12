using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages
{
    public partial class WelcomeScreen : UserControl
    {
        private readonly EmployeesContext _context = new EmployeesContext();

        public WelcomeScreen()
        {
            InitializeComponent();
            LoadWelcomeMessage();
        }

        private void LoadWelcomeMessage()
        {
            if (Session.CurrentEmployeeId.HasValue)
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeId == Session.CurrentEmployeeId.Value);

                if (employee != null)
                {
                    txtWelcomeMessage.Text = $"Привет, {employee.FIO}! Вы вошли как {employee.EmployeeRoleDisplay}.";
                }
                else
                {
                    txtWelcomeMessage.Text = "Привет! Не удалось загрузить данные сотрудника.";
                }
            }
            else
            {
                txtWelcomeMessage.Text = "Привет! Пожалуйста, войдите в систему.";
            }
        }
    }
}