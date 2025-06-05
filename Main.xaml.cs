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

namespace DormitoryPATDesktop
{
    /// <summary>
    /// Логика взаимодействия для Main.xaml
    /// </summary>
    public partial class Main : Page
    {
        public string CurrentModule { get; set; } = "RepairRequests"; // Модуль по умолчанию

        public Main()
        {
            InitializeComponent();
            LoadModule(CurrentModule); // Загружаем модуль при создании
        }

        public void LoadModule(string moduleName)
        {
            CurrentModule = moduleName;

            // Сбрасываем выделение всех кнопок
            foreach (var child in ((StackPanel)this.FindName("MenuPanel")).Children)
            {
                if (child is Button button)
                {
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.Black;
                }
            }

            // Находим и выделяем нужную кнопку
            var buttonName = $"{moduleName}Btn";
            if (this.FindName(buttonName) is Button activeButton)
            {
                activeButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e0e0e0");
                activeButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#58a325");
            }

            // Загружаем соответствующий модуль
            switch (moduleName)
            {
                case "RepairRequests":
                    ContentArea.Content = new Pages.RepairRequests.Item();
                    sttBtn.Visibility = Visibility.Visible; // Показываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "Complaints":
                    ContentArea.Content = new Pages.Complaints.Item();
                    sttBtn.Visibility = Visibility.Visible; // Показываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "DutyEducator":
                    ContentArea.Content = new Pages.DutyEducators.Item();
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "Students":
                    ContentArea.Content = new Pages.Students.Item();
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "Employees":
                    ContentArea.Content = new Pages.Emloyees.Item();
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "DutySchedule":
                    ContentArea.Content = new Pages.DutySchedule.Item();
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "TelegramAuth":
                    ContentArea.Content = new Pages.TelegramAuth.Item();
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                default:
                    addBtn.Visibility = Visibility.Collapsed; // По умолчанию скрываем
                    break;
            }
        }

        private void ModuleButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс выделения всех кнопок
            foreach (var child in ((StackPanel)((Button)sender).Parent).Children)
            {
                if (child is Button button)
                {
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.Black;
                }
            }

            // Выделение активной кнопки
            var activeButton = (Button)sender;
            CurrentModule = activeButton.Tag.ToString();
            activeButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e0e0e0");
            activeButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#58a325");

            // Загрузка соответствующего модуля
            switch (activeButton.Tag.ToString())
            {
                case "RepairRequests":
                    ContentArea.Content = new Pages.RepairRequests.Item();
                    break;
                case "Complaints":
                    ContentArea.Content = new Pages.Complaints.Item();
                    break;
                case "DutyEducator":
                    ContentArea.Content = new Pages.DutyEducators.Item();
                    break;
                case "Students":
                    ContentArea.Content = new Pages.Students.Item();
                    break;
                case "Employees":
                    ContentArea.Content = new Pages.Emloyees.Item();
                    break;
                case "DutySchedule":
                    ContentArea.Content = new Pages.DutySchedule.Item();
                    break;
                case "TelegramAuth":
                    ContentArea.Content = new Pages.TelegramAuth.Item();
                    break;
            }

            // Показываем/скрываем кнопку "Добавить" в зависимости от модуля
            addBtn.Visibility = activeButton.Tag.ToString() != "SomeModuleWhereAddIsNotNeeded"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            // Определяем текущий открытый модуль
            if (ContentArea.Content is Pages.RepairRequests.Item)
            {
                MainWindow.init.OpenPages(new Pages.RepairRequests.Add(null));
            }
            else if (ContentArea.Content is Pages.Complaints.Item)
            {
                MainWindow.init.OpenPages(new Pages.Complaints.Add(null));
            }
            else if (ContentArea.Content is Pages.Students.Item)
            {
                MainWindow.init.OpenPages(new Pages.Students.Add(null));
            }
            else if (ContentArea.Content is Pages.Emloyees.Item)
            {
                MainWindow.init.OpenPages(new Pages.Emloyees.Add(null));
            }
            else if (ContentArea.Content is Pages.DutyEducators.Item)
            {
                MainWindow.init.OpenPages(new Pages.DutyEducators.Add(null));
            }
            else if (ContentArea.Content is Pages.TelegramAuth.Item)
            {
                MainWindow.init.OpenPages(new Pages.TelegramAuth.Add(null));
            }
            else if (ContentArea.Content is Pages.DutySchedule.Item)
            {
                MainWindow.init.OpenPages(new Pages.DutySchedule.Add(null));
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {

        }

        private void Statistica(object sender, RoutedEventArgs e)
        {
            // Определяем текущий открытый модуль
            if (ContentArea.Content is Pages.RepairRequests.Item)
            {
                ContentArea.Content = new Pages.RepairRequests.Statistics();
            }
            else if (ContentArea.Content is Pages.Complaints.Item)
            {
                ContentArea.Content = new Pages.Complaints.Statistics();
            }
        }
    }
}
