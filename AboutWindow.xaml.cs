using HabitFlow.Entity;
using HabitFlow.Properties;
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

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _currentUser = user;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadVersionInfo();
                LoadDeveloperInfo();
                LoadCopyrightInfo();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке информации: {ex.Message}");
            }
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
                ConfirmationDialog.ShowError("Ошибка", $"Не удалось открыть ссылку: {ex.Message}");
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
                ConfirmationDialog.ShowError("Ошибка", $"Не удалось открыть ссылку: {ex.Message}");
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
                ConfirmationDialog.ShowError("Ошибка", $"Не удалось открыть почтовый клиент: {ex.Message}");
            }
        }

        // Возврат в главное окно
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }

        // Закрытие окна (через кнопку OK)
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }

        // Закрытие окна (через кнопку Close)
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            WindowStateManager.SaveWindowState(this);
        }

        // Метод для обновления информации о пользователе (если нужно)
        public void UpdateUserInfo(Users user)
        {
            _currentUser = user;
            // Здесь можно обновить какую-то информацию, связанную с пользователем
        }
    }
}