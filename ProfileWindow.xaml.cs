using HabitFlow.Entity;
using HabitFlow.Properties;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Логика взаимодействия для ProfileWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private bool _isProfileDataChanged = false;
        private string _selectedAvatarColor = "#4CAF50";
        private Button _lastSelectedColorButton;

        public ProfileWindow(Users user)
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _context = new HabitTrackerEntities();
            _currentUser = user;

            // Выделяем первый цвет по умолчанию
            _lastSelectedColorButton = btnAvatarColor1;
            btnAvatarColor1.BorderBrush = Brushes.Black;
            btnAvatarColor1.BorderThickness = new Thickness(2);

            UpdateCurrentDateTime();

            // Запускаем таймер для обновления времени
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, args) => UpdateCurrentDateTime();
            timer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
            LoadUserStatistics();
            LoadSettings();
        }

        

        // Перемещение окна
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Обновление текущей даты и времени
        private void UpdateCurrentDateTime()
        {
            txtCurrentDateTime.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy | HH:mm:ss",
                new System.Globalization.CultureInfo("ru-RU"));
        }

        // Загрузка данных пользователя
        private void LoadUserData()
        {
            // Инициалы для аватара
            string initials = GetInitials(_currentUser.UserName);
            txtInitials.Text = initials;

            txtFullName.Text = _currentUser.UserName;
            txtEmail.Text = _currentUser.Email;
            txtRegistrationDate.Text = _currentUser.CreatedAt?.ToString("dd MMMM yyyy") ?? "Неизвестно";
            txtLastLogin.Text = "Сегодня, " + DateTime.Now.ToString("HH:mm");

            // Заполняем поля редактирования
            txtEditUsername.Text = _currentUser.UserName;
            txtEditEmail.Text = _currentUser.Email;
        }

        // Получение инициалов из имени
        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "U";

            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "U";

            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(1, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
        }

        // Загрузка статистики пользователя
        private void LoadUserStatistics()
        {
            try
            {
                // Дней в приложении
                if (_currentUser.CreatedAt.HasValue)
                {
                    int daysInApp = (DateTime.Now - _currentUser.CreatedAt.Value).Days;
                    txtDaysInApp.Text = daysInApp.ToString();
                }

                // Активные привычки
                int activeHabits = _context.Habits
                    .Count(h => h.UserId == _currentUser.UserId && h.IsActive == true);
                txtActiveHabits.Text = activeHabits.ToString();

                // Всего отметок
                int totalCheckins = _context.HabitRecords
                    .Count(r => r.Habits.UserId == _currentUser.UserId);
                txtTotalCheckins.Text = totalCheckins.ToString();

                // Лучшая серия
                int bestStreak = CalculateBestStreak();
                txtBestStreak.Text = $"{bestStreak} дней";

                // Процент выполнения (за последние 30 дней)
                double successRate = CalculateSuccessRate();
                txtSuccessRate.Text = $"{successRate:F1}%";

                // Прогресс за месяц
                double monthlyProgress = CalculateMonthlyProgress();
                txtMonthlyProgress.Text = $"{monthlyProgress:F1}%";

                // Достижения
                int achievements = CalculateAchievements();
                txtAchievements.Text = achievements.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке статистики: {ex.Message}");
            }
        }

        // Загрузка настроек
        private void LoadSettings()
        {
            var settings = AppSettings.Load();

            // Тема
            foreach (var item in cmbTheme.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Tag?.ToString() == settings.Theme)
                {
                    cmbTheme.SelectedItem = comboItem;
                    break;
                }
            }

            // Язык
            foreach (var item in cmbLanguage.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Tag?.ToString() == settings.Language)
                {
                    cmbLanguage.SelectedItem = comboItem;
                    break;
                }
            }

            // Уведомления
            chkEmailNotifications.IsChecked = settings.EmailNotifications;
            chkReminderNotifications.IsChecked = settings.ReminderNotifications;
        }

        // Расчет лучшей серии
        private int CalculateBestStreak()
        {
            var habits = _context.Habits
                .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                .Select(h => h.HabitId)
                .ToList();

            int bestStreak = 0;

            foreach (var habitId in habits)
            {
                var records = _context.HabitRecords
                    .Where(r => r.HabitId == habitId && r.StatusId == 2)
                    .OrderBy(r => r.RecordDate)
                    .Select(r => r.RecordDate)
                    .ToList();

                if (records.Any())
                {
                    int currentStreak = 1;
                    for (int i = 1; i < records.Count; i++)
                    {
                        if (records[i] == records[i - 1].AddDays(1))
                        {
                            currentStreak++;
                        }
                        else
                        {
                            if (currentStreak > bestStreak)
                                bestStreak = currentStreak;
                            currentStreak = 1;
                        }
                    }
                    if (currentStreak > bestStreak)
                        bestStreak = currentStreak;
                }
            }

            return bestStreak;
        }

        // Расчет процента выполнения
        private double CalculateSuccessRate()
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30);

            var records = _context.HabitRecords
                .Where(r => r.Habits.UserId == _currentUser.UserId
                    && r.RecordDate >= startDate
                    && r.RecordDate <= endDate)
                .ToList();

            if (!records.Any())
                return 0;

            int completed = records.Count(r => r.StatusId == 2);
            return (double)completed / records.Count * 100;
        }

        // Расчет прогресса за месяц
        private double CalculateMonthlyProgress()
        {
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var records = _context.HabitRecords
                .Where(r => r.Habits.UserId == _currentUser.UserId
                    && r.RecordDate >= startOfMonth
                    && r.RecordDate <= endOfMonth)
                .ToList();

            if (!records.Any())
                return 0;

            int completed = records.Count(r => r.StatusId == 2);
            return (double)completed / records.Count * 100;
        }

        // Расчет количества достижений
        private int CalculateAchievements()
        {
            int achievements = 0;

            // За лучшую серию
            int bestStreak = CalculateBestStreak();
            if (bestStreak >= 7) achievements++;
            if (bestStreak >= 30) achievements++;
            if (bestStreak >= 100) achievements++;

            // За количество привычек
            int habitCount = _context.Habits.Count(h => h.UserId == _currentUser.UserId && h.IsActive == true);
            if (habitCount >= 3) achievements++;
            if (habitCount >= 5) achievements++;
            if (habitCount >= 10) achievements++;

            // За общее количество отметок
            int totalCheckins = _context.HabitRecords.Count(r => r.Habits.UserId == _currentUser.UserId);
            if (totalCheckins >= 100) achievements++;
            if (totalCheckins >= 500) achievements++;
            if (totalCheckins >= 1000) achievements++;

            return achievements;
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

        // Валидация email
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

        // Проверка сложности пароля
        private int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int strength = 0;
            if (password.Length >= 6) strength++;
            if (password.Length >= 8) strength++;
            if (Regex.IsMatch(password, @"[0-9]")) strength++;
            if (Regex.IsMatch(password, @"[a-z]")) strength++;
            if (Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]")) strength++;

            return strength;
        }

        // Выбор цвета аватара
        private void AvatarColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                if (_lastSelectedColorButton != null)
                {
                    _lastSelectedColorButton.BorderThickness = new Thickness(0);
                }

                button.BorderBrush = Brushes.Black;
                button.BorderThickness = new Thickness(2);
                _lastSelectedColorButton = button;
                _selectedAvatarColor = button.Tag.ToString();

                // Обновляем цвет аватара
                var avatarBorder = txtInitials.Parent as Border;
                if (avatarBorder != null)
                {
                    avatarBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedAvatarColor));
                }

                _isProfileDataChanged = true;
            }
        }

        // Обработчики изменения полей
        private void txtEditUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _isProfileDataChanged = true;
            btnSaveProfile.IsEnabled =
                !string.IsNullOrWhiteSpace(txtEditUsername.Text) &&
                !string.IsNullOrWhiteSpace(txtEditEmail.Text) &&
                IsValidEmail(txtEditEmail.Text);
        }

        private void txtPassword_Changed(object sender, RoutedEventArgs e)
        {
            string currentPassword = txtCurrentPassword.Password;
            string newPassword = txtNewPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // Проверяем заполнение всех полей
            bool isValid = !string.IsNullOrEmpty(currentPassword) &&
                          !string.IsNullOrEmpty(newPassword) &&
                          !string.IsNullOrEmpty(confirmPassword) &&
                          newPassword == confirmPassword &&
                          newPassword.Length >= 6;

            btnChangePassword.IsEnabled = isValid;

            // Обновляем индикатор сложности пароля
            if (!string.IsNullOrEmpty(newPassword))
            {
                int strength = CalculatePasswordStrength(newPassword);

                if (strength <= 2)
                    passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                else if (strength <= 4)
                    passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                else
                    passwordStrengthFill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));

                if (passwordStrengthFill.Parent is Border parent)
                {
                    passwordStrengthFill.Width = (parent.ActualWidth * strength) / 6;
                }
            }
            else
            {
                passwordStrengthFill.Width = 0;
            }
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            _isProfileDataChanged = true;
        }

        // Сохранение изменений профиля
        private void btnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newUsername = txtEditUsername.Text.Trim();
                string newEmail = txtEditEmail.Text.Trim();

                // Проверяем уникальность имени пользователя
                var existingUser = _context.Users
                    .FirstOrDefault(u => u.UserName == newUsername && u.UserId != _currentUser.UserId);
                if (existingUser != null)
                {
                    ConfirmationDialog.ShowError("Ошибка", "Пользователь с таким именем уже существует");
                    return;
                }

                // Проверяем уникальность email
                existingUser = _context.Users
                    .FirstOrDefault(u => u.Email == newEmail && u.UserId != _currentUser.UserId);
                if (existingUser != null)
                {
                    ConfirmationDialog.ShowError("Ошибка", "Пользователь с таким email уже существует");
                    return;
                }

                // Обновляем данные
                _currentUser.UserName = newUsername;
                _currentUser.Email = newEmail;

                _context.Entry(_currentUser).State = EntityState.Modified;
                _context.SaveChanges();

                // Обновляем отображение
                txtFullName.Text = newUsername;
                txtEmail.Text = newEmail;
                txtInitials.Text = GetInitials(newUsername);

                // Показываем сообщение об успехе
                txtSaveSuccess.Visibility = Visibility.Visible;
                _isProfileDataChanged = false;

                // Скрываем сообщение через 3 секунды
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, args) =>
                {
                    txtSaveSuccess.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();

                UpdateStatusMessage("Профиль обновлен");
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Смена пароля
        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPassword = txtCurrentPassword.Password;
                string newPassword = txtNewPassword.Password;

                // Проверяем текущий пароль
                string hashedCurrent = HashPassword(currentPassword);
                if (_currentUser.PasswordHash != hashedCurrent)
                {
                    ConfirmationDialog.ShowError("Ошибка", "Неверный текущий пароль");
                    return;
                }

                // Обновляем пароль
                _currentUser.PasswordHash = HashPassword(newPassword);
                _context.Entry(_currentUser).State = EntityState.Modified;
                _context.SaveChanges();

                ConfirmationDialog.ShowSuccess("Успех", "Пароль успешно изменен");

                // Очищаем поля
                txtCurrentPassword.Password = "";
                txtNewPassword.Password = "";
                txtConfirmPassword.Password = "";
                btnChangePassword.IsEnabled = false;
                passwordStrengthFill.Width = 0;

                UpdateStatusMessage("Пароль изменен");
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при смене пароля: {ex.Message}");
            }
        }

        // Двухфакторная аутентификация
        private void btn2FA_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationDialog.ShowInfo("Двухфакторная аутентификация",
                "Функция двухфакторной аутентификации будет доступна в следующей версии приложения.");
        }

        // Сброс настроек
        private void btnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = ConfirmationDialog.ShowWarning(
                "Сброс настроек",
                "Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?"
            );

            if (result)
            {
                var defaultSettings = new AppSettings();
                defaultSettings.Save();

                LoadSettings();
                UpdateStatusMessage("Настройки сброшены");

                ConfirmationDialog.ShowSuccess("Успех", "Настройки сброшены к значениям по умолчанию");
            }
        }

        // Удаление аккаунта
        private void btnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = ConfirmationDialog.ShowDelete(
                "Удаление аккаунта",
                "Вы уверены, что хотите удалить свой аккаунт?\n\nЭто действие НЕОБРАТИМО. Все ваши привычки и история отметок будут безвозвратно удалены.",
                new List<string>
                {
                    $"• {txtActiveHabits.Text} активных привычек",
                    $"• {txtTotalCheckins.Text} записей в истории",
                    $"• {txtAchievements.Text} достижений"
                }
            );

            if (result)
            {
                try
                {
                    // Полное удаление пользователя
                    _context.Users.Remove(_currentUser);
                    _context.SaveChanges();

                    // Очищаем сохраненные данные
                    Settings.Default.RememberedUserId = 0;
                    Settings.Default.RememberedUserName = "";
                    Settings.Default.Save();

                    ConfirmationDialog.ShowSuccess("До свидания",
                        "Аккаунт удален. Спасибо, что были с нами!");

                    // Возвращаемся на окно входа с сохранением состояния
                    var loginWindow = new LoginWindow();
                    WindowStateManager.OpenWindow(this, loginWindow);
                }
                catch (Exception ex)
                {
                    ConfirmationDialog.ShowError("Ошибка", $"Ошибка при удалении аккаунта: {ex.Message}");
                }
            }
        }

        // Навигация назад в главное окно
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isProfileDataChanged)
            {
                var result = ConfirmationDialog.ShowWarning(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть без сохранения?"
                );

                if (!result) return;
            }

            // Возвращаемся в главное окно с сохранением состояния
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }

        // Обновление статуса
        private void UpdateStatusMessage(string message)
        {
            txtStatusMessage.Text = message;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                txtStatusMessage.Text = "Готово";
                timer.Stop();
            };
            timer.Start();
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            WindowStateManager.SaveWindowState(this);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isProfileDataChanged)
            {
                var result = ConfirmationDialog.ShowWarning(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть без сохранения?"
                );

                if (!result) return;
            }

            // При закрытии возвращаемся в главное окно
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }
    }
}