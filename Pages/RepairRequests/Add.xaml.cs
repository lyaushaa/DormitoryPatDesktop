using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.RepairRequests
{
    public partial class Add : Page
    {
        private readonly Models.RepairRequests _request;
        private readonly bool _isNewRequest;
        private readonly EmployeesContext _employeesContext = new EmployeesContext();

        public string TitleName => _isNewRequest ? "Добавление новой заявки" : "Редактирование заявки";

        public Add(Models.RepairRequests request)
        {
            InitializeComponent();
            _request = request ?? new Models.RepairRequests();
            _isNewRequest = request == null;

            DataContext = this;
            LoadEmployees();
            LoadRequestData();
        }

        private void LoadRequestData()
        {
            if (!_isNewRequest)
            {
                txtRequester.Text = _request.TelegramId.ToString();
                txtLocation.Text = _request.Location;
                txtProblemDescription.Text = _request.UserComment;
                txtCreationDate.Text = _request.RequestDate.ToString("g");
                txtLastUpdateDate.Text = _request.LastStatusChange.ToString("g");
                txtMasterComment.Text = _request.MasterComment;

                // Устанавливаем тип проблемы
                foreach (ComboBoxItem item in cmbProblemType.Items)
                {
                    if (item.Content.ToString() == _request.Problem.ToString())
                    {
                        cmbProblemType.SelectedItem = item;
                        break;
                    }
                }

                // Устанавливаем статус
                foreach (ComboBoxItem item in cmbStatus.Items)
                {
                    if (item.Content.ToString() == _request.StatusDisplay)
                    {
                        cmbStatus.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                txtRequester.IsReadOnly = false;
                txtLocation.IsReadOnly = false;
                txtProblemDescription.IsReadOnly = false;
                txtCreationDate.IsReadOnly = false;
                txtLastUpdateDate.IsReadOnly = false;
                txtCreationDate.Text = DateTime.Now.ToString("g");
                txtLastUpdateDate.Text = DateTime.Now.ToString("g");

                // Устанавливаем статус "Создана" по умолчанию
                foreach (ComboBoxItem item in cmbStatus.Items)
                {
                    if (item.Content.ToString() == "Создана")
                    {
                        cmbStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void LoadEmployees()
        {
            var masters = _employeesContext.Employees
                .Where(e => e.EmployeeRole == EmployeeRole.Мастер)
                .ToList();

            cmbMaster.ItemsSource = masters;
            cmbMaster.DisplayMemberPath = "FIO"; // Убедимся, что DisplayMemberPath установлен

            if (!_isNewRequest && _request.MasterId.HasValue)
            {
                var selectedMaster = masters.FirstOrDefault(e => e.EmployeeId == _request.MasterId);
                if (selectedMaster != null)
                {
                    cmbMaster.SelectedItem = selectedMaster;
                }
                else
                {
                    cmbMaster.SelectedItem = null; // Сбрасываем выбор, если мастер не найден
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new RepairRequestsContext())
                {
                    var telegramId = long.TryParse(txtRequester.Text, out var id) ? id : 0;                    

                    Models.RepairRequests requestToSave;

                    if (_isNewRequest)
                    {
                        requestToSave = new Models.RepairRequests
                        {
                            TelegramId = (long)(telegramId != 0 ? telegramId : (long?)null),
                            Location = txtLocation.Text,
                            UserComment = txtProblemDescription.Text,
                            RequestDate = DateTime.Now,
                            LastStatusChange = DateTime.Now,
                            Status = RequestStatus.Создана
                        };
                        context.RepairRequests.Add(requestToSave);
                    }
                    else
                    {
                        requestToSave = context.RepairRequests
                            .FirstOrDefault(r => r.RequestId == _request.RequestId);

                        if (requestToSave == null)
                        {
                            MessageBox.Show("Заявка не найдена в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Общие поля для новой и существующей заявки
                    if (cmbMaster.SelectedItem is Employees selectedMaster)
                    {
                        requestToSave.MasterId = selectedMaster.EmployeeId;
                    }

                    if (cmbProblemType.SelectedItem is ComboBoxItem selectedProblemType)
                    {
                        requestToSave.Problem = selectedProblemType.Content.ToString() switch
                        {
                            "Электрика" => ProblemType.Электрика,
                            "Сантехника" => ProblemType.Сантехника,
                            "Мебель" => ProblemType.Мебель,
                            _ => requestToSave.Problem
                        };
                    }

                    if (cmbStatus.SelectedItem is ComboBoxItem selectedStatus)
                    {
                        requestToSave.Status = selectedStatus.Content.ToString() switch
                        {
                            "Создана" => RequestStatus.Создана,
                            "В обработке" => RequestStatus.В_обработке,
                            "Ожидает запчастей" => RequestStatus.Ожидает_запчастей,
                            "Завершена" => RequestStatus.Завершена,
                            "Отклонена" => RequestStatus.Отклонена,
                            _ => requestToSave.Status
                        };
                    }

                    requestToSave.MasterComment = txtMasterComment.Text;

                    context.SaveChanges();

                    MessageBox.Show(_isNewRequest ?
                        "Заявка успешно создана." : "Изменения успешно сохранены.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainPage = new Main();
                    mainPage.CurrentModule = "RepairRequests"; // Устанавливаем нужный модуль
                    mainPage.LoadModule("RepairRequests");
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
            if (string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                MessageBox.Show("Укажите место ремонта.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtProblemDescription.Text))
            {
                MessageBox.Show("Введите описание проблемы.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbProblemType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип проблемы.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус заявки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "RepairRequests"; // Устанавливаем нужный модуль
            mainPage.LoadModule("RepairRequests");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}