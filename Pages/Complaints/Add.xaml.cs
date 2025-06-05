using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.Complaints
{
    public partial class Add : Page
    {
        private readonly Models.Complaints _complaint;
        private readonly bool _isNewComplaint;
        private readonly EmployeesContext _employeesContext = new EmployeesContext();
        private readonly StudentsContext _studentsContext = new StudentsContext();

        public string TitleName => _isNewComplaint ? "Добавление новой жалобы" : "Редактирование жалобы";

        public Add(Models.Complaints complaint)
        {
            InitializeComponent();
            _complaint = complaint ?? new Models.Complaints();
            _isNewComplaint = complaint == null;

            InitializeUI();
            LoadEmployees();
            LoadComplaintData();
        }

        private void InitializeUI()
        {
            if (_isNewComplaint)
            {
                txtComplaintText.IsReadOnly = false;
                txtComplainer.IsReadOnly = false;
                txtCreationDate.IsReadOnly = false;
                txtLastUpdateDate.IsReadOnly = false;
                txtCreationDate.Text = DateTime.Now.ToString("g");
                txtLastUpdateDate.Text = DateTime.Now.ToString("g");

                // Устанавливаем статус "Создана" по умолчанию для новой жалобы
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

        private void LoadComplaintData()
        {
            if (!_isNewComplaint)
            {
                // Отображаем ФИО студента или "Анонимно"
                var student = _complaint.StudentId.HasValue
                    ? _studentsContext.Students.FirstOrDefault(s => s.StudentId == _complaint.StudentId)
                    : null;
                txtComplainer.Text = student?.FIO ?? "Анонимно";
                txtComplainer.IsReadOnly = true; // Только для чтения при редактировании

                txtComplaintText.Text = _complaint.ComplaintText;
                txtCreationDate.Text = _complaint.SubmissionDate.ToString("g");
                txtLastUpdateDate.Text = _complaint.LastStatusChange.ToString("g");

                foreach (ComboBoxItem item in cmbStatus.Items)
                {
                    if (item.Content.ToString() == _complaint.StatusDisplay)
                    {
                        cmbStatus.SelectedItem = item;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(_complaint.Comment))
                {
                    txtComment.Text = _complaint.Comment;
                }
            }
        }

        private void LoadEmployees()
        {
            var employees = _employeesContext.Employees
                .Where(e => e.EmployeeRole == Models.EmployeeRole.Воспитатель ||
                           e.EmployeeRole == Models.EmployeeRole.Заведующий_общежитием)
                .ToList();

            cmbProcessor.ItemsSource = employees;
            cmbProcessor.DisplayMemberPath = "FIO";

            if (!_isNewComplaint && _complaint.ReviewedBy.HasValue)
            {
                var reviewer = employees.FirstOrDefault(e => e.EmployeeId == _complaint.ReviewedBy);
                if (reviewer != null)
                {
                    cmbProcessor.SelectedItem = reviewer;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new ComplaintsContext())
                {
                    Models.Complaints complaintToSave;

                    if (_isNewComplaint)
                    {
                        complaintToSave = new Models.Complaints
                        {
                            ComplaintText = txtComplaintText.Text,
                            SubmissionDate = DateTime.Now,
                            LastStatusChange = DateTime.Now,
                            Status = Models.ComplaintStatus.Создана
                        };

                        // Проверяем, является ли ввод "анонимно" (без учёта регистра)
                        string complainerText = txtComplainer.Text.Trim().ToLower();
                        if (complainerText != "анонимно")
                        {
                            var student = _studentsContext.Students
                                .FirstOrDefault(s => s.FIO.ToLower() == complainerText);
                            if (student == null)
                            {
                                MessageBox.Show("Студент с таким ФИО не найден.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            complaintToSave.StudentId = student.StudentId;
                        }
                        // Если "анонимно" или пусто, StudentId остаётся null
                        context.Complaints.Add(complaintToSave);
                    }
                    else
                    {
                        complaintToSave = context.Complaints
                            .FirstOrDefault(c => c.ComplaintId == _complaint.ComplaintId);

                        if (complaintToSave == null)
                        {
                            MessageBox.Show("Жалоба не найдена в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Общие поля для новой и существующей жалобы
                    if (cmbProcessor.SelectedItem is Employees selectedEmployee)
                    {
                        complaintToSave.ReviewedBy = selectedEmployee.EmployeeId;
                    }

                    if (cmbStatus.SelectedItem is ComboBoxItem selectedStatus)
                    {
                        complaintToSave.Status = selectedStatus.Content.ToString() switch
                        {
                            "Создана" => Models.ComplaintStatus.Создана,
                            "В обработке" => Models.ComplaintStatus.В_обработке,
                            "Завершена" => Models.ComplaintStatus.Завершена,
                            "Отклонена" => Models.ComplaintStatus.Отклонена,
                            _ => complaintToSave.Status
                        };
                    }

                    complaintToSave.Comment = txtComment.Text;
                    complaintToSave.LastStatusChange = DateTime.Now;

                    context.SaveChanges();

                    MessageBox.Show(_isNewComplaint ?
                        "Жалоба успешно создана." : "Изменения успешно сохранены.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainPage = new Main();
                    mainPage.CurrentModule = "Complaints";
                    mainPage.LoadModule("Complaints");
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
            if (string.IsNullOrWhiteSpace(txtComplaintText.Text))
            {
                MessageBox.Show("Введите текст жалобы.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_isNewComplaint)
            {
                string complainerText = txtComplainer.Text.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(complainerText) && complainerText != "анонимно")
                {
                    var student = _studentsContext.Students
                        .FirstOrDefault(s => s.FIO.ToLower() == complainerText);
                    if (student == null)
                    {
                        MessageBox.Show("Студент с таким ФИО не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            if (cmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус жалобы.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new Main();
            mainPage.CurrentModule = "Complaints";
            mainPage.LoadModule("Complaints");
            MainWindow.init.OpenPages(mainPage);
        }
    }
}