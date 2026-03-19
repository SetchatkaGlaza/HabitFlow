using HabitFlow.Entity;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace HabitFlow
{
    /// <summary>
    /// Логика взаимодействия для AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private Users _currentUser;

        public AboutWindow(Users user = null)
        {
            InitializeComponent();
            _currentUser = user;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVersionInfo();
            LoadDeveloperInfo();
            LoadCopyrightInfo();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка информации о версии
        private void LoadVersionInfo()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        // Загрузка информации о разработчике
        private void LoadDeveloperInfo()
        {
            // Здесь можно загрузить реальные данные из конфигурации
            txtDeveloperName.Text = "Иван Петров";
            txtDeveloperEmail.Text = "ivan.petrov@habitflow.ru";
        }

        // Загрузка информации о копирайте
        private void LoadCopyrightInfo()
        {
            int year = DateTime.Now.Year;
            txtCopyright.Text = $"© {year} HabitFlow. Все права защищены.";
        }

        // Открытие ссылок в браузере
        private void btnWebsite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.habitflow.ru");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/yourusername/habitflow");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Открытие почтового клиента
                string mailto = "mailto:support@habitflow.ru?subject=Поддержка HabitFlow";
                Process.Start(mailto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть почтовый клиент: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Закрытие окна
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Метод для обновления информации о пользователе (если нужно)
        public void UpdateUserInfo(Users user)
        {
            _currentUser = user;
            // Здесь можно обновить какую-то информацию, связанную с пользователем
        }
    }
}