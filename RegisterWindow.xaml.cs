using HabitFlow.Entity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Логика взаимодействия для RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private HabitTrackerEntities _context;
        private bool _isPasswordVisible = false;

        public RegisterWindow()
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            btnRegister.IsEnabled = false;
        }

        // Перемещение окна
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Закрытие окна
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Проверка сложности пароля
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = txtPassword.Password;
            int strength = CalculatePasswordStrength(password);

            // Изменение цвета индикатора в зависимости от сложности
            if (strength <= 2)
                passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // Красный
            else if (strength <= 4)
                passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // Оранжевый
            else
                passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Зеленый

            // Обновление ширины индикатора
            passwordStrengthFill.Width = (passwordStrengthIndicator.ActualWidth * strength) / 6;

            ValidateForm();
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        // Расчет сложности пароля (от 0 до 6)
        private int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int strength = 0;

            // Длина пароля
            if (password.Length >= 6) strength++;
            if (password.Length >= 8) strength++;

            // Наличие цифр
            if (Regex.IsMatch(password, @"[0-9]")) strength++;

            // Наличие букв в нижнем регистре
            if (Regex.IsMatch(password, @"[a-z]")) strength++;

            // Наличие букв в верхнем регистре
            if (Regex.IsMatch(password, @"[A-Z]")) strength++;

            // Наличие специальных символов
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]")) strength++;

            return strength;
        }

        // Валидация формы
        private void ValidateForm()
        {
            bool isValid = true;

            // Проверка имени пользователя
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || txtUsername.Text.Length < 3)
                isValid = false;

            // Проверка email
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !IsValidEmail(txtEmail.Text))
                isValid = false;

            // Проверка пароля
            string password = txtPassword.Password;
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                isValid = false;

            // Проверка подтверждения пароля
            if (password != txtConfirmPassword.Password)
                isValid = false;

            btnRegister.IsEnabled = isValid;
        }

        // Проверка корректности email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Хеширование пароля
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Обработчик регистрации
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка существования пользователя с таким же именем
                var existingUser = _context.Users
                    .FirstOrDefault(u => u.UserName == txtUsername.Text);

                if (existingUser != null)
                {
                    ShowError("Пользователь с таким именем уже существует");
                    return;
                }

                // Проверка существования пользователя с таким же email
                existingUser = _context.Users
                    .FirstOrDefault(u => u.Email == txtEmail.Text);

                if (existingUser != null)
                {
                    ShowError("Пользователь с таким email уже существует");
                    return;
                }

                // Создание нового пользователя
                var newUser = new Users
                {
                    UserName = txtUsername.Text,
                    Email = txtEmail.Text,
                    // В реальном проекте пароль должен хешироваться!
                    // Здесь для примера, но лучше хранить хеш
                    PasswordHash = HashPassword(txtPassword.Password),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                // Если отмечено "Запомнить меня", сохраняем данные
                if (chkRememberMe.IsChecked == true)
                {
                    Properties.Settings.Default.RememberedUserId = newUser.UserId;
                    Properties.Settings.Default.RememberedUserName = newUser.UserName;
                    Properties.Settings.Default.Save();
                }

                // Открываем главное окно приложения
                MainWindow mainWindow = new MainWindow(newUser);
                mainWindow.Show();

                // Закрываем окно регистрации
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при регистрации: {ex.Message}");
            }
        }

        // Переход к окну авторизации
        private void btnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        // Отображение ошибки
        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            txtErrorMessage.Visibility = Visibility.Visible;

            // Автоматически скрываем ошибку через 5 секунд
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, args) =>
            {
                txtErrorMessage.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}