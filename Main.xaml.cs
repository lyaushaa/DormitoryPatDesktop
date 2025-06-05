using DormitoryPATDesktop.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DormitoryPATDesktop
{
    public partial class Main : Page
    {
        public string CurrentModule { get; set; }

        public Main()
        {
            InitializeComponent();
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
               case "Students":
                    ContentArea.Content = new Pages.Students.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "Employees":
                    ContentArea.Content = new Pages.Emloyees.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "DutySchedule":
                    ContentArea.Content = new Pages.DutySchedule.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                default:
                    sttBtn.Visibility = Visibility.Hidden; // По умолчанию скрываем
                    addBtn.Visibility = Visibility.Hidden; // По умолчанию скрываем
                    break;
            }
        }

        private void ModuleButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем выделение всех кнопок
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

            // Загружаем соответствующий модуль
            switch (activeButton.Tag.ToString())
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
                case "Students":
                    ContentArea.Content = new Pages.Students.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "Employees":
                    ContentArea.Content = new Pages.Emloyees.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                case "DutySchedule":
                    ContentArea.Content = new Pages.DutySchedule.Item();
                    sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика
                    addBtn.Visibility = Visibility.Visible; // Показываем кнопку Добавить
                    break;
                default:
                    sttBtn.Visibility = Visibility.Hidden; // По умолчанию скрываем
                    addBtn.Visibility = Visibility.Hidden; // По умолчанию скрываем
                    break;
            }
        }

        private void Add(object sender, RoutedEventArgs e)
        {
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
            else if (ContentArea.Content is Pages.DutySchedule.Item)
            {
                MainWindow.init.OpenPages(new Pages.DutySchedule.Add(null));
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            MainWindow.init.OpenPages(new Login());
        }

        private void Statistica(object sender, RoutedEventArgs e)
        {
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