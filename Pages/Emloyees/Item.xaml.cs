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

namespace DormitoryPATDesktop.Pages.Emloyees
{
    /// <summary>
    /// Логика взаимодействия для Item.xaml
    /// </summary>
    public partial class Item : UserControl
    {
        private readonly EmployeesContext _context;
        private List<Employees> _allEmployees;

        public Item()
        {
            InitializeComponent();
            _context = new EmployeesContext();
            _allEmployees = new List<Employees>();
            EmployeesDataGrid.BeginningEdit += (s, e) => e.Cancel = true;
            if (Session.CurrentEmployeeRole == EmployeeRole.Администратор) Action_dgtc.Visibility = Visibility.Visible;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _allEmployees = _context.Employees
                    .ToList() ?? new List<Employees>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allEmployees == null) _allEmployees = new List<Employees>();
            if (EmployeesDataGrid == null) return;

            IQueryable<Employees> filtered = _allEmployees.AsQueryable();

            // Фильтр по должности
            if (RoleFilterComboBox.SelectedIndex > 0 &&
                RoleFilterComboBox.SelectedItem is ComboBoxItem selectedRole)
            {
                var role = selectedRole.Content.ToString() switch
                {
                    "Мастер" => EmployeeRole.Мастер,
                    "Воспитатель" => EmployeeRole.Воспитатель,
                    "Заведующий общежитием" => EmployeeRole.Заведующий_общежитием,
                    "Администратор" => EmployeeRole.Администратор,
                    _ => (EmployeeRole?)null
                };

                if (role.HasValue)
                {
                    filtered = filtered.Where(e => e.EmployeeRole == role.Value);
                }
            }
            
            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(e =>
                    e.FIO.ToLower().Contains(searchText) ||
                    e.PhoneNumber.Contains(searchText) ||
                    e.Login.ToLower().Contains(searchText));
            }

            EmployeesDataGrid.ItemsSource = filtered.ToList();
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
            if (((FrameworkElement)sender).DataContext is Employees employee)
            {
                var confirm = MessageBox.Show($"Удалить сотрудника {employee.FIO}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.Employees.Remove(employee);
                        _context.SaveChanges();
                        LoadEmployees();
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
            LoadEmployees();
        }

        private void EmployeesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Session.CurrentEmployeeRole == EmployeeRole.Администратор)
            {
                if (EmployeesDataGrid.SelectedItem is Employees selectedEmployee)
                {
                    var processingPage = new Add(selectedEmployee);
                    MainWindow.init.OpenPages(processingPage);
                }
            }                
        }
    }
}
