using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telegram.Bot;

namespace DormitoryPATDesktop.Pages.RepairRequests
{
    public partial class Add : Page
    {
        private readonly Models.RepairRequests _request;
        private readonly bool _isNewRequest;
        private readonly EmployeesContext _employeesContext = new EmployeesContext();
        private readonly StudentsContext _studentsContext = new StudentsContext();
        private readonly RepairRequestsContext _repairRequestsContext = new RepairRequestsContext();
        private readonly RepairMaterialsContext _repairMaterialsContext = new RepairMaterialsContext();
        private readonly ObservableCollection<RepairMaterials> _materials = new ObservableCollection<RepairMaterials>();
        private static readonly string BotToken = "7681929292:AAELFhLTiH3c4KZtnRrPY9aGD6gYyLWVo5E"; // Replace with your bot token
        private static readonly TelegramBotClient _telegramClient = new TelegramBotClient(BotToken);

        public string TitleName => _isNewRequest ? "Добавление новой заявки" : "Редактирование заявки";

        public Add(Models.RepairRequests request)
        {
            InitializeComponent();
            _request = request ?? new Models.RepairRequests();
            _isNewRequest = request == null;

            dgMaterials.ItemsSource = _materials;
            DataContext = this;
            LoadEmployees();
            LoadRequestData();
            UpdateMaterialsVisibility();
        }

        private void LoadRequestData()
        {
            if (!_isNewRequest)
            {
                var student = _studentsContext.Students
                    .FirstOrDefault(s => s.StudentId == _request.StudentId);
                txtRequester.Text = student?.FIO ?? "Не указан";
                txtLocation.Text = _request.Location;
                txtProblemDescription.Text = _request.UserComment;
                txtCreationDate.Text = _request.RequestDate.ToString("g");
                txtLastUpdateDate.Text = _request.LastStatusChange.ToString("g");
                txtMasterComment.Text = _request.MasterComment;

                foreach (ComboBoxItem item in cmbProblemType.Items)
                {
                    if (item.Content.ToString() == _request.Problem.ToString())
                    {
                        cmbProblemType.SelectedItem = item;
                        cmbProblemType.IsEditable = true;
                        break;
                    }
                }

                foreach (ComboBoxItem item in cmbStatus.Items)
                {
                    if (item.Content.ToString() == _request.StatusDisplay)
                    {
                        cmbStatus.SelectedItem = item;
                        break;
                    }
                }

                // Загружаем материалы для существующей заявки, если они есть
                var materials = _repairMaterialsContext.RepairMaterials
                    .Where(m => m.RequestId == _request.RequestId)
                    .ToList();
                foreach (var material in materials)
                {
                    _materials.Add(material);
                }
            }
            else
            {
                txtRequester.IsReadOnly = false;
                txtLocation.IsReadOnly = false;
                txtProblemDescription.IsReadOnly = false;
                cmbProblemType.IsEditable = false;
                txtCreationDate.Text = DateTime.Now.ToString("g");
                txtLastUpdateDate.Text = DateTime.Now.ToString("g");

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

            if (!_isNewRequest && _request.MasterId.HasValue)
            {
                var selectedMaster = masters.FirstOrDefault(e => e.EmployeeId == _request.MasterId);
                cmbMaster.SelectedItem = selectedMaster;
            }
        }

        private void BtnAddMaterials_Click(object sender, RoutedEventArgs e)
        {
            _materials.Add(new RepairMaterials { RequestId = _isNewRequest ? 0 : _request.RequestId });
            UpdateMaterialsVisibility();
        }

        private void UpdateMaterialsVisibility()
        {
            dgMaterials.Visibility = _materials.Any() ? Visibility.Visible : Visibility.Collapsed;
            btnAddMaterials.Visibility = _materials.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private async Task SendTelegramNotification(long? telegramId, string userComment, RequestStatus newStatus, string? masterComment = null)
        {
            if (!telegramId.HasValue)
            {
                return;
            }

            try
            {
                var message = $"🔔 Статус вашей заявки изменён на {newStatus}.\nТекст заявки: {userComment}";
                if (!string.IsNullOrEmpty(masterComment))
                {
                    message += $"\nКомментарий мастера: {masterComment}";
                }
                await _telegramClient.SendMessage(telegramId.Value, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке уведомления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var repairRequestsContext = new RepairRequestsContext())
                using (var repairMaterialsContext = new RepairMaterialsContext())
                {
                    Models.RepairRequests requestToSave;
                    long studentId;

                    if (_isNewRequest)
                    {
                        var student = _studentsContext.Students
                            .FirstOrDefault(s => s.FIO.ToLower() == txtRequester.Text.ToLower());
                        if (student == null)
                        {
                            MessageBox.Show("Студент с таким ФИО не найден.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        studentId = student.StudentId;

                        requestToSave = new Models.RepairRequests
                        {
                            StudentId = studentId,
                            Location = txtLocation.Text,
                            UserComment = txtProblemDescription.Text,
                            RequestDate = DateTime.Now,
                            LastStatusChange = DateTime.Now,
                            Status = RequestStatus.Создана
                        };
                        repairRequestsContext.RepairRequests.Add(requestToSave);
                        repairRequestsContext.SaveChanges();

                        // Обновляем RequestId для всех материалов
                        foreach (var material in _materials)
                        {
                            material.RequestId = requestToSave.RequestId;
                        }
                    }
                    else
                    {
                        requestToSave = repairRequestsContext.RepairRequests
                            .FirstOrDefault(r => r.RequestId == _request.RequestId); // Removed .Include(r => r.Student)

                        if (requestToSave == null)
                        {
                            MessageBox.Show("Заявка не найдена в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        studentId = requestToSave.StudentId;
                        requestToSave.Location = txtLocation.Text;
                        requestToSave.UserComment = txtProblemDescription.Text;

                        // Удаляем старые материалы для этой заявки
                        var existingMaterials = repairMaterialsContext.RepairMaterials
                            .Where(m => m.RequestId == requestToSave.RequestId)
                            .ToList();
                        repairMaterialsContext.RepairMaterials.RemoveRange(existingMaterials);
                        repairMaterialsContext.SaveChanges();
                    }

                    if (cmbMaster.SelectedItem is Employees selectedMaster)
                    {
                        requestToSave.MasterId = selectedMaster.EmployeeId;
                    }
                    else
                    {
                        requestToSave.MasterId = null;
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
                        var oldStatus = requestToSave.Status;
                        requestToSave.Status = selectedStatus.Content.ToString() switch
                        {
                            "Создана" => RequestStatus.Создана,
                            "В обработке" => RequestStatus.В_обработке,
                            "Ожидает запчастей" => RequestStatus.Ожидает_запчастей,
                            "Завершена" => RequestStatus.Завершена,
                            "Отклонена" => RequestStatus.Отклонена,
                            _ => requestToSave.Status
                        };

                        if (oldStatus != requestToSave.Status)
                        {
                            // Ask for confirmation to send master comment in desktop app
                            string? masterCommentToSend = null;
                            if (!string.IsNullOrWhiteSpace(txtMasterComment.Text))
                            {
                                var result = MessageBox.Show("Хотите отправить комментарий мастера студенту?", "Подтверждение комментария",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (result == MessageBoxResult.Yes)
                                {
                                    masterCommentToSend = txtMasterComment.Text;
                                }
                            }

                            // Fetch student TelegramId using StudentsContext
                            var student = _studentsContext.Students
                                .FirstOrDefault(s => s.StudentId == studentId);
                            if (student?.TelegramId != null)
                            {
                                _ = SendTelegramNotification(student.TelegramId, requestToSave.UserComment, requestToSave.Status, masterCommentToSend);
                            }
                        }
                    }

                    requestToSave.MasterComment = txtMasterComment.Text;
                    requestToSave.LastStatusChange = DateTime.Now;

                    repairRequestsContext.SaveChanges();

                    // Сохраняем материалы только если они заполнены
                    if (_materials.Any(m => !string.IsNullOrEmpty(m.MaterialName) && m.Quantity > 0 && m.CostPerUnit > 0))
                    {
                        foreach (var material in _materials)
                        {
                            if (material.MaterialId == 0) // Новый материал
                            {
                                material.RequestId = requestToSave.RequestId;
                                repairMaterialsContext.RepairMaterials.Add(material);
                            }
                            else // Существующий материал
                            {
                                var existingMaterial = repairMaterialsContext.RepairMaterials
                                    .FirstOrDefault(m => m.MaterialId == material.MaterialId);
                                if (existingMaterial != null)
                                {
                                    existingMaterial.MaterialName = material.MaterialName;
                                    existingMaterial.Quantity = material.Quantity;
                                    existingMaterial.CostPerUnit = material.CostPerUnit;
                                }
                            }
                        }
                        repairMaterialsContext.SaveChanges();
                    }

                    MessageBox.Show(_isNewRequest ?
                        "Заявка успешно создана." : "Изменения успешно сохранены.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainPage = new Main();
                    mainPage.CurrentModule = "RepairRequests";
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
            if (string.IsNullOrWhiteSpace(txtRequester.Text))
            {
                MessageBox.Show("Введите ФИО студента.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

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
            mainPage.CurrentModule = "RepairRequests";
            mainPage.LoadModule("RepairRequests");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}