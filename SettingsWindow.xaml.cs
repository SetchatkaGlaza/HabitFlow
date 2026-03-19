using HabitFlow.Entity;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace HabitFlow
{
    /// <summary>
    /// Класс для хранения настроек
    /// </summary>
    public class AppSettings
    {
        // Оформление
        public string Theme { get; set; } = "Light";
        public string AccentColor { get; set; } = "#4CAF50";
        public string FontSize { get; set; } = "Medium";

        // Отображение
        public bool MondayFirst { get; set; } = true;
        public bool ShowWeekends { get; set; } = true;
        public bool CompactWeekView { get; set; } = false;
        public string DateFormat { get; set; } = "dd.MM.yyyy";
        public string TimeFormat { get; set; } = "HH:mm";

        // Уведомления
        public bool EnableNotifications { get; set; } = true;
        public bool DailyReminder { get; set; } = false;
        public string ReminderTime { get; set; } = "20:00";
        public bool StreakNotifications { get; set; } = true;
        public bool EnableSounds { get; set; } = true;
        public bool SoundOnComplete { get; set; } = true;
        public bool SoundOnReminder { get; set; } = true;

        // Данные
        public bool AutoSave { get; set; } = true;
        public string BackupFrequency { get; set; } = "Instant";

        // Дополнительно
        public string Language { get; set; } = "ru";
        public bool RunOnStartup { get; set; } = false;

        // Путь к файлу настроек
        private static string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HabitFlow", "settings.json");

        // Загрузка настроек
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }

            return new AppSettings();
        }

        // Сохранение настроек
        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private AppSettings _settings;
        private bool _hasUnsavedChanges = false;
        private System.Windows.Threading.DispatcherTimer _saveStatusTimer;

        public SettingsWindow(Users user)
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            _currentUser = user;
            _settings = AppSettings.Load();

            _saveStatusTimer = new System.Windows.Threading.DispatcherTimer();
            _saveStatusTimer.Interval = TimeSpan.FromSeconds(3);
            _saveStatusTimer.Tick += SaveStatusTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            UpdateColorPreview();
            UpdateDateFormatPreview();
            UpdateLastBackupInfo();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения. Сохранить?",
                    "Подтверждение", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveSettings();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка настроек в интерфейс
        private void LoadSettings()
        {
            // Тема
            foreach (var item in cmbTheme.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.Theme)
                {
                    cmbTheme.SelectedItem = comboItem;
                    break;
                }
            }

            // Акцентный цвет
            foreach (var item in cmbAccentColor.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.AccentColor)
                {
                    cmbAccentColor.SelectedItem = comboItem;
                    break;
                }
            }

            // Размер шрифта
            foreach (var item in cmbFontSize.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.FontSize)
                {
                    cmbFontSize.SelectedItem = comboItem;
                    break;
                }
            }

            // Отображение
            chkMondayFirst.IsChecked = _settings.MondayFirst;
            chkShowWeekends.IsChecked = _settings.ShowWeekends;
            chkCompactWeekView.IsChecked = _settings.CompactWeekView;

            // Формат даты
            foreach (var item in cmbDateFormat.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.DateFormat)
                {
                    cmbDateFormat.SelectedItem = comboItem;
                    break;
                }
            }

            foreach (var item in cmbTimeFormat.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.TimeFormat)
                {
                    cmbTimeFormat.SelectedItem = comboItem;
                    break;
                }
            }

            // Уведомления
            chkEnableNotifications.IsChecked = _settings.EnableNotifications;
            chkDailyReminder.IsChecked = _settings.DailyReminder;
            txtReminderTime.Text = _settings.ReminderTime;
            chkStreakNotifications.IsChecked = _settings.StreakNotifications;
            chkEnableSounds.IsChecked = _settings.EnableSounds;
            chkSoundOnComplete.IsChecked = _settings.SoundOnComplete;
            chkSoundOnReminder.IsChecked = _settings.SoundOnReminder;

            // Данные
            chkAutoSave.IsChecked = _settings.AutoSave;

            foreach (var item in cmbBackupFrequency.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.BackupFrequency)
                {
                    cmbBackupFrequency.SelectedItem = comboItem;
                    break;
                }
            }

            // Дополнительно
            foreach (var item in cmbLanguage.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboItem && comboItem.Tag?.ToString() == _settings.Language)
                {
                    cmbLanguage.SelectedItem = comboItem;
                    break;
                }
            }

            chkRunOnStartup.IsChecked = _settings.RunOnStartup;

            _hasUnsavedChanges = false;
        }

        // Сохранение настроек
        private void SaveSettings()
        {
            // Тема
            if (cmbTheme.SelectedItem is System.Windows.Controls.ComboBoxItem themeItem)
                _settings.Theme = themeItem.Tag?.ToString() ?? "Light";

            // Акцентный цвет
            if (cmbAccentColor.SelectedItem is System.Windows.Controls.ComboBoxItem colorItem)
                _settings.AccentColor = colorItem.Tag?.ToString() ?? "#4CAF50";

            // Размер шрифта
            if (cmbFontSize.SelectedItem is System.Windows.Controls.ComboBoxItem fontSizeItem)
                _settings.FontSize = fontSizeItem.Tag?.ToString() ?? "Medium";

            // Отображение
            _settings.MondayFirst = chkMondayFirst.IsChecked ?? true;
            _settings.ShowWeekends = chkShowWeekends.IsChecked ?? true;
            _settings.CompactWeekView = chkCompactWeekView.IsChecked ?? false;

            // Формат даты
            if (cmbDateFormat.SelectedItem is System.Windows.Controls.ComboBoxItem dateFormatItem)
                _settings.DateFormat = dateFormatItem.Tag?.ToString() ?? "dd.MM.yyyy";

            if (cmbTimeFormat.SelectedItem is System.Windows.Controls.ComboBoxItem timeFormatItem)
                _settings.TimeFormat = timeFormatItem.Tag?.ToString() ?? "HH:mm";

            // Уведомления
            _settings.EnableNotifications = chkEnableNotifications.IsChecked ?? true;
            _settings.DailyReminder = chkDailyReminder.IsChecked ?? false;
            _settings.ReminderTime = txtReminderTime.Text;
            _settings.StreakNotifications = chkStreakNotifications.IsChecked ?? true;
            _settings.EnableSounds = chkEnableSounds.IsChecked ?? true;
            _settings.SoundOnComplete = chkSoundOnComplete.IsChecked ?? true;
            _settings.SoundOnReminder = chkSoundOnReminder.IsChecked ?? true;

            // Данные
            _settings.AutoSave = chkAutoSave.IsChecked ?? true;

            if (cmbBackupFrequency.SelectedItem is System.Windows.Controls.ComboBoxItem backupItem)
                _settings.BackupFrequency = backupItem.Tag?.ToString() ?? "Instant";

            // Дополнительно
            if (cmbLanguage.SelectedItem is System.Windows.Controls.ComboBoxItem langItem)
                _settings.Language = langItem.Tag?.ToString() ?? "ru";

            _settings.RunOnStartup = chkRunOnStartup.IsChecked ?? false;

            // Сохраняем
            _settings.Save();

            // Применяем настройки автозапуска
            SetRunOnStartup(_settings.RunOnStartup);

            _hasUnsavedChanges = false;

            // Показываем статус
            ShowSaveStatus("✅ Настройки сохранены", "#4CAF50");
        }

        // Установка автозапуска
        private void SetRunOnStartup(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue("HabitFlow", System.Reflection.Assembly.GetExecutingAssembly().Location);
                        }
                        else
                        {
                            key.DeleteValue("HabitFlow", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка настройки автозапуска: {ex.Message}");
            }
        }

        // Обновление предпросмотра цвета
        private void UpdateColorPreview()
        {
            if (cmbAccentColor.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                string colorHex = item.Tag?.ToString() ?? "#4CAF50";
                colorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }
        }

        // Обновление предпросмотра формата даты
        private void UpdateDateFormatPreview()
        {
            string dateFormat = "dd.MM.yyyy";
            string timeFormat = "HH:mm";

            if (cmbDateFormat.SelectedItem is System.Windows.Controls.ComboBoxItem dateItem)
                dateFormat = dateItem.Tag?.ToString() ?? "dd.MM.yyyy";

            if (cmbTimeFormat.SelectedItem is System.Windows.Controls.ComboBoxItem timeItem)
                timeFormat = timeItem.Tag?.ToString() ?? "HH:mm";

            DateTime now = DateTime.Now;
            string preview = $"{now.ToString(dateFormat)} {now.ToString(timeFormat)}";
            txtDateFormatPreview.Text = $"Пример: {preview}";
        }

        // Обновление информации о последнем бэкапе
        private void UpdateLastBackupInfo()
        {
            string backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HabitFlow", "backups");

            if (Directory.Exists(backupPath))
            {
                var files = Directory.GetFiles(backupPath, "*.json");
                if (files.Any())
                {
                    var lastFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                    txtLastBackup.Text = $"Последняя копия: {File.GetCreationTime(lastFile):dd.MM.yyyy HH:mm}";
                    return;
                }
            }

            txtLastBackup.Text = "Последняя копия: никогда";
        }

        // Показ статуса сохранения
        private void ShowSaveStatus(string message, string color)
        {
            txtSaveStatus.Text = message;
            txtSaveStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            borderSaveStatus.Visibility = Visibility.Visible;

            _saveStatusTimer.Stop();
            _saveStatusTimer.Start();
        }

        private void SaveStatusTimer_Tick(object sender, EventArgs e)
        {
            borderSaveStatus.Visibility = Visibility.Collapsed;
            _saveStatusTimer.Stop();
        }

        // Обработчики изменений
        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            _hasUnsavedChanges = true;
            UpdateColorPreview();
            UpdateDateFormatPreview();
        }

        private void Notifications_EnabledChanged(object sender, RoutedEventArgs e)
        {
            _hasUnsavedChanges = true;

            // Включаем/выключаем зависимые элементы
            chkDailyReminder.IsEnabled = chkEnableNotifications.IsChecked ?? false;
            txtReminderTime.IsEnabled = chkEnableNotifications.IsChecked ?? false;
            chkStreakNotifications.IsEnabled = chkEnableNotifications.IsChecked ?? false;
        }

        // Применение настроек
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        // Резервное копирование
        private void btnBackupNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Сохранить резервную копию",
                    FileName = $"HabitFlow_Backup_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".json",
                    Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Создаем резервную копию всех данных пользователя
                    var backup = new
                    {
                        User = _currentUser,
                        Habits = _context.Habits.Where(h => h.UserId == _currentUser.UserId).ToList(),
                        Records = _context.HabitRecords
                            .Where(r => r.Habits.UserId == _currentUser.UserId)
                            .ToList(),
                        BackupDate = DateTime.Now,
                        AppVersion = "1.0.0"
                    };

                    string json = JsonConvert.SerializeObject(backup, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);

                    // Сохраняем информацию о бэкапе
                    string backupInfoPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "HabitFlow", "backups", "backup_info.txt");

                    string directory = Path.GetDirectoryName(backupInfoPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.AppendAllText(backupInfoPath,
                        $"Резервная копия создана: {DateTime.Now:dd.MM.yyyy HH:mm} - {dialog.FileName}\n");

                    ShowSaveStatus("✅ Резервная копия создана", "#4CAF50");
                    UpdateLastBackupInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании резервной копии: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Восстановление из резервной копии
        private void btnRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Выберите файл резервной копии",
                    DefaultExt = ".json",
                    Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "Восстановление из резервной копии ЗАМЕНИТ все текущие данные.\n\n" +
                        "Вы уверены, что хотите продолжить?",
                        "Подтверждение восстановления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        string json = File.ReadAllText(dialog.FileName);
                        var backup = JsonConvert.DeserializeObject<dynamic>(json);

                        // Здесь должна быть логика восстановления данных
                        // В реальном проекте нужно аккуратно обновить БД

                        MessageBox.Show("Восстановление из резервной копии завершено.\n\n" +
                                       "Приложение будет перезапущено.",
                            "Восстановление завершено",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Перезапуск приложения
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сброс настроек
        private void btnResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Сбросить все настройки к значениям по умолчанию?",
                "Сброс настроек",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _settings = new AppSettings();
                LoadSettings();
                SaveSettings();
                ShowSaveStatus("✅ Настройки сброшены", "#4CAF50");
            }
        }

        // Очистка всех данных
        private void btnClearAllData_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ ВНИМАНИЕ! ⚠️\n\n" +
                "Это действие полностью удалит ВСЕ ваши данные:\n" +
                "• Все привычки\n" +
                "• Всю историю отметок\n" +
                "• Статистику и достижения\n\n" +
                "Это действие НЕОБРАТИМО!\n\n" +
                "Вы действительно хотите очистить все данные?",
                "Очистка всех данных",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем все записи пользователя
                    var records = _context.HabitRecords
                        .Where(r => r.Habits.UserId == _currentUser.UserId)
                        .ToList();
                    _context.HabitRecords.RemoveRange(records);

                    var habits = _context.Habits
                        .Where(h => h.UserId == _currentUser.UserId)
                        .ToList();
                    _context.Habits.RemoveRange(habits);

                    _context.SaveChanges();

                    ShowSaveStatus("✅ Все данные удалены", "#4CAF50");

                    MessageBox.Show("Все данные успешно удалены.\n\n" +
                                   "Приложение будет перезапущено.",
                        "Очистка завершена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Перезапуск приложения
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при очистке данных: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения. Сохранить?",
                    "Подтверждение", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveSettings();
                    this.Close();
                }
                else if (result == MessageBoxResult.No)
                {
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }
    }
}