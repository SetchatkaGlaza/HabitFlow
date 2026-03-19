using HabitFlow.Entity;
using HabitFlow.Properties;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace HabitFlow
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private HabitTrackerEntities _context;
        private int _failedAttempts = 0;
        private DateTime? _lockoutEndTime = null;

        public LoginWindow()
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _context = new HabitTrackerEntities();
            CheckForRememberedUser();
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
            var result = ConfirmationDialog.ShowWarning(
                "Выход из приложения",
                "Вы уверены, что хотите закрыть приложение?"
            );

            if (result)
            {
                Application.Current.Shutdown();
            }
        }

        // Проверка на запомненного пользователя
        private void CheckForRememberedUser()
        {
            try
            {
                // Проверяем, есть ли сохраненный пользователь
                if (Settings.Default.RememberedUserId > 0)
                {
                    int rememberedUserId = Settings.Default.RememberedUserId;
                    var user = _context.Users.Find(rememberedUserId);

                    if (user != null && user.IsActive == true)
                    {
                        // Подставляем имя пользователя
                        txtUsernameOrEmail.Text = user.UserName;
                        // Ставим галочку "Запомнить меня"
                        chkRememberMe.IsChecked = true;
                        // Фокусируемся на поле пароля
                        txtPassword.Focus();
                    }
                    else
                    {
                        // Если пользователь не найден или неактивен, очищаем сохраненные данные
                        Settings.Default.RememberedUserId = 0;
                        Settings.Default.RememberedUserName = "";
                        Settings.Default.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке запомненного пользователя: {ex.Message}");
            }
        }

        // Валидация формы
        private void ValidateForm()
        {
            bool isValid = !string.IsNullOrWhiteSpace(txtUsernameOrEmail.Text) &&
                          txtPassword.Password.Length > 0;

            btnLogin.IsEnabled = isValid;
        }

        private void txtUsernameOrEmail_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
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

        // Проверка блокировки из-за неудачных попыток
        private bool IsLockedOut()
        {
            if (_lockoutEndTime.HasValue && _lockoutEndTime.Value > DateTime.Now)
            {
                TimeSpan remaining = _lockoutEndTime.Value - DateTime.Now;
                ShowError($"Слишком много неудачных попыток. Попробуйте через {remaining.Seconds + 1} секунд.");
                return true;
            }

            if (_lockoutEndTime.HasValue && _lockoutEndTime.Value <= DateTime.Now)
            {
                // Сбрасываем блокировку
                _lockoutEndTime = null;
                _failedAttempts = 0;
            }

            return false;
        }

        // Обработка неудачной попытки входа
        private void HandleFailedAttempt()
        {
            _failedAttempts++;

            if (_failedAttempts >= 3)
            {
                // Блокируем на 30 секунд после 3 неудачных попыток
                _lockoutEndTime = DateTime.Now.AddSeconds(30);
                ShowError("Слишком много неудачных попыток. Подождите 30 секунд.");
                btnLogin.IsEnabled = false;

                // Запускаем таймер для разблокировки кнопки
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(30);
                timer.Tick += (s, args) =>
                {
                    _lockoutEndTime = null;
                    _failedAttempts = 0;

                    // Проверяем, можно ли включить кнопку
                    if (!string.IsNullOrWhiteSpace(txtUsernameOrEmail.Text) &&
                        txtPassword.Password.Length > 0)
                    {
                        btnLogin.IsEnabled = true;
                    }

                    timer.Stop();
                };
                timer.Start();
            }
            else
            {
                ShowError($"Неверное имя пользователя или пароль. Осталось попыток: {3 - _failedAttempts}");
            }
        }

        // Обработчик входа
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на блокировку
            if (IsLockedOut())
                return;

            try
            {
                string login = txtUsernameOrEmail.Text.Trim();
                string password = txtPassword.Password;
                string hashedPassword = HashPassword(password);

                // Ищем пользователя по имени или email
                var user = _context.Users
                    .FirstOrDefault(u => (u.UserName == login || u.Email == login)
                                        && u.IsActive == true);

                if (user != null && user.PasswordHash == hashedPassword)
                {
                    // Успешный вход
                    _failedAttempts = 0;

                    // Если отмечено "Запомнить меня", сохраняем данные
                    if (chkRememberMe.IsChecked == true)
                    {
                        Settings.Default.RememberedUserId = user.UserId;
                        Settings.Default.RememberedUserName = user.UserName;
                        Settings.Default.Save();
                    }
                    else
                    {
                        // Если галочка снята, очищаем сохраненные данные
                        Settings.Default.RememberedUserId = 0;
                        Settings.Default.RememberedUserName = "";
                        Settings.Default.Save();
                    }

                    // Открываем главное окно с сохранением состояния
                    var mainWindow = new MainWindow(user);
                    WindowStateManager.OpenWindow(this, mainWindow);
                }
                else
                {
                    // Неудачная попытка входа
                    HandleFailedAttempt();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при входе: {ex.Message}");
            }
        }

        // Переход к регистрации
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            WindowStateManager.OpenWindow(this, registerWindow);
        }

        // Обработчик "Забыли пароль?"
        private void btnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationDialog.ShowInfo("Восстановление пароля",
                "Функция восстановления пароля будет доступна в следующей версии.\n\nОбратитесь к администратору для сброса пароля.");
        }

        // Закрытие сообщения об ошибке
        private void btnCloseError_Click(object sender, RoutedEventArgs e)
        {
            borderErrorMessage.Visibility = Visibility.Collapsed;
        }

        // Отображение ошибки
        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            borderErrorMessage.Visibility = Visibility.Visible;

            // Автоматически скрываем ошибку через 5 секунд
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, args) =>
            {
                borderErrorMessage.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}