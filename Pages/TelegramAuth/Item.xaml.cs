using DormitoryPATDesktop.Context;
using DormitoryPATDesktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DormitoryPATDesktop.Pages.TelegramAuth
{
    public partial class Item : UserControl
    {
        private readonly TelegramAuthContext _context;
        private List<Models.TelegramAuth> _allAuths;

        public Item()
        {
            InitializeComponent();
            _context = new TelegramAuthContext();
            LoadAuths();
        }

        private void LoadAuths()
        {
            try
            {
                _allAuths = _context.TelegramAuth.ToList();
                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                AuthDataGrid.ItemsSource = _allAuths;
                return;
            }

            var searchText = SearchTextBox.Text.ToLower();
            AuthDataGrid.ItemsSource = _allAuths.Where(a =>
                a.TelegramId.ToString().Contains(searchText) ||
                (a.FirstName?.ToLower().Contains(searchText) ?? false) ||
                (a.LastName?.ToLower().Contains(searchText) ?? false) ||
                (a.Username?.ToLower().Contains(searchText) ?? false));
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAuths();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Models.TelegramAuth auth)
            {
                var confirm = MessageBox.Show($"Удалить аккаунт {auth.TelegramId}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.TelegramAuth.Remove(auth);
                        _context.SaveChanges();
                        LoadAuths();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AuthDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AuthDataGrid.SelectedItem is Models.TelegramAuth selectedAuth)
            {
                var editPage = new Add(selectedAuth);
                MainWindow.init.OpenPages(editPage);
            }
        }
    }
}