﻿using DormitoryPATDesktop.Models;
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
            ConfigureMenuBasedOnRole();
        }        
        
        private void ConfigureMenuBasedOnRole()
        {
            var role = Session.CurrentEmployeeRole;
            if (role.HasValue)
            {
                switch (role.Value)
                {
                    case EmployeeRole.Администратор:
                        // Все модули доступны
                        RepairRequestsBtn.Visibility = Visibility.Visible;
                        ComplaintsBtn.Visibility = Visibility.Visible;
                        StudentsBtn.Visibility = Visibility.Visible;
                        EmployeesBtn.Visibility = Visibility.Visible;
                        DutyScheduleBtn.Visibility = Visibility.Visible;
                        break;

                    case EmployeeRole.Заведующий_общежитием:
                        // Все модули доступны
                        RepairRequestsBtn.Visibility = Visibility.Visible;
                        ComplaintsBtn.Visibility = Visibility.Visible;
                        StudentsBtn.Visibility = Visibility.Visible;
                        EmployeesBtn.Visibility = Visibility.Visible;
                        DutyScheduleBtn.Visibility = Visibility.Visible;
                        break;

                    case EmployeeRole.Мастер:
                        // Только заявки на ремонт
                        RepairRequestsBtn.Visibility = Visibility.Visible;
                        ComplaintsBtn.Visibility = Visibility.Collapsed;
                        StudentsBtn.Visibility = Visibility.Collapsed;
                        EmployeesBtn.Visibility = Visibility.Collapsed;
                        DutyScheduleBtn.Visibility = Visibility.Collapsed;
                        break;

                    case EmployeeRole.Воспитатель:
                        // Только жалобы
                        RepairRequestsBtn.Visibility = Visibility.Collapsed;
                        ComplaintsBtn.Visibility = Visibility.Visible;
                        StudentsBtn.Visibility = Visibility.Visible;
                        EmployeesBtn.Visibility = Visibility.Collapsed;
                        DutyScheduleBtn.Visibility = Visibility.Visible;
                        break;
                }
            }
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
                    sttBtn.Visibility = Visibility.Visible;
                    if (Session.CurrentEmployeeRole == EmployeeRole.Администратор)
                    {
                        addBackBtn.Content = "Добавить";
                        addBackBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        addBackBtn.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Complaints":
                    ContentArea.Content = new Pages.Complaints.Item();
                    sttBtn.Visibility = Visibility.Visible;
                    if (Session.CurrentEmployeeRole == EmployeeRole.Администратор)
                    {
                        addBackBtn.Content = "Добавить";
                        addBackBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        addBackBtn.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Students":
                    ContentArea.Content = new Pages.Students.Item();
                    sttBtn.Visibility = Visibility.Hidden;
                    if(Session.CurrentEmployeeRole == EmployeeRole.Администратор || Session.CurrentEmployeeRole == EmployeeRole.Заведующий_общежитием)
                    {
                        addBackBtn.Content = "Добавить";
                        addBackBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        addBackBtn.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Employees":
                    ContentArea.Content = new Pages.Emloyees.Item();
                    sttBtn.Visibility = Visibility.Hidden;
                    if (Session.CurrentEmployeeRole == EmployeeRole.Администратор)
                    {
                        addBackBtn.Content = "Добавить";
                        addBackBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        addBackBtn.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "DutySchedule":
                    ContentArea.Content = new Pages.DutySchedule.Item();
                    sttBtn.Visibility = Visibility.Hidden;
                    addBackBtn.Content = "Добавить";
                    break;
                default:
                    sttBtn.Visibility = Visibility.Hidden;
                    addBackBtn.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void ModuleButton_Click(object sender, RoutedEventArgs e)
        {
            var activeButton = (Button)sender;
            CurrentModule = activeButton.Tag.ToString();

            // Сбрасываем выделение всех кнопок
            foreach (var child in ((StackPanel)activeButton.Parent).Children)
            {
                if (child is Button button)
                {
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.Black;
                }
            }

            activeButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#e0e0e0");
            activeButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#58a325");

            LoadModule(CurrentModule);
        }

        private void AddBack_Click(object sender, RoutedEventArgs e)
        {
            // Переходим к добавлению
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
            else if (ContentArea.Content is Pages.Complaints.Statistics)
            {
                ContentArea.Content = new Pages.Complaints.Item();
            }
            else if (ContentArea.Content is Pages.RepairRequests.Statistics)
            {
                ContentArea.Content = new Pages.RepairRequests.Item();
            }
        }

        private void Statistica(object sender, RoutedEventArgs e)
        {
            if (ContentArea.Content is Pages.RepairRequests.Item)
            {
                ContentArea.Content = new Pages.RepairRequests.Statistics();
                addBackBtn.Visibility = Visibility.Visible;
                addBackBtn.Content = "Назад";
            }
            else if (ContentArea.Content is Pages.Complaints.Item)
            {
                ContentArea.Content = new Pages.Complaints.Statistics();
                addBackBtn.Visibility = Visibility.Visible;
                addBackBtn.Content = "Назад";
            }
            sttBtn.Visibility = Visibility.Hidden; // Скрываем кнопку Статистика при переходе
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Session.CurrentEmployeeId = null;
            Session.CurrentEmployeeRole = null;
            MainWindow.init.OpenPages(new Login());
        }
    }
}