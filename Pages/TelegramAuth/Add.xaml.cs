using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.TelegramAuth
{
    public partial class Add : Page
    {
        private readonly Models.TelegramAuth _auth;
        private readonly bool _isEditMode;

        public string TitleName => _isEditMode ? "Редактирование Telegram аккаунта" : "Новый Telegram аккаунт";
        public bool IsEditMode => _isEditMode;

        public Add(Models.TelegramAuth auth = null)
        {
            InitializeComponent();
            _auth = auth ?? new Models.TelegramAuth();
            _isEditMode = auth != null;
            DataContext = this;
            LoadAuthData();
        }

        private void LoadAuthData()
        {
            if (_isEditMode)
            {
                txtTelegramId.Text = _auth.TelegramId.ToString();
                txtFirstName.Text = _auth.FirstName;
                txtLastName.Text = _auth.LastName;
                txtUsername.Text = _auth.Username;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new TelegramAuthContext())
                {
                    if (_isEditMode)
                    {
                        context.Entry(_auth).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                    else
                    {
                        _auth.TelegramId = long.Parse(txtTelegramId.Text);
                        context.TelegramAuth.Add(_auth);
                    }

                    _auth.FirstName = txtFirstName.Text;
                    _auth.LastName = txtLastName.Text;
                    _auth.Username = txtUsername.Text;

                    context.SaveChanges();
                    MessageBox.Show("Данные сохранены успешно!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    MainWindow.init.OpenPages(new Main());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (!long.TryParse(txtTelegramId.Text, out _))
            {
                MessageBox.Show("Введите корректный Telegram ID", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.init.OpenPages(new Main());
        }
    }
}