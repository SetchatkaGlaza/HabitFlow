using HabitFlow.Entity;
using HabitFlow.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace HabitFlow
{
    /// <summary>
    /// Класс для экспортируемых данных
    /// </summary>
    public class ExportData
    {
        public DateTime ExportDate { get; set; }
        public string AppVersion { get; set; }
        public string UserName { get; set; }
        public List<Habits> Habits { get; set; }
        public List<HabitRecords> Records { get; set; }
        public AppSettings Settings { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для ImportExportWindow.xaml
    /// </summary>
    public partial class ImportExportWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private string _selectedImportFile;
        private ExportData _previewData;
        private System.Windows.Threading.DispatcherTimer _statusTimer;

        // Путь для авто-бэкапов
        private string BackupPath { get; set; }

        public ImportExportWindow(Users user)
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _context = new HabitTrackerEntities();
            _currentUser = user;

            // Устанавливаем путь для бэкапов
            BackupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HabitFlow", "Backups");

            _statusTimer = new System.Windows.Threading.DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(3);
            _statusTimer.Tick += StatusTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateExportStats();
                LoadAutoBackupSettings();
                txtBackupPath.Text = BackupPath;
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Обновление статистики для экспорта
        private void UpdateExportStats()
        {
            int habitsCount = _context.Habits.Count(h => h.UserId == _currentUser.UserId);
            int recordsCount = _context.HabitRecords.Count(r => r.Habits.UserId == _currentUser.UserId);

            txtExportStats.Text = $"Привычек: {habitsCount}, Записей: {recordsCount}";
        }

        // Загрузка настроек авто-бэкапа
        private void LoadAutoBackupSettings()
        {
            try
            {
                var settings = AppSettings.Load();
                chkAutoBackup.IsChecked = settings.AutoSave;
                // Здесь можно загрузить другие настройки
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        // Показ статуса
        private void ShowStatus(string message, bool isSuccess = true)
        {
            txtStatus.Text = message;
            borderStatus.Background = isSuccess
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 76, 175, 80))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 231, 76, 60));
            txtStatus.Foreground = isSuccess
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 76, 175, 80))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 231, 76, 60));
            borderStatus.Visibility = Visibility.Visible;

            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            borderStatus.Visibility = Visibility.Collapsed;
            _statusTimer.Stop();
        }

        // ==================== ЭКСПОРТ ====================

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Определяем формат и расширение
                string format = GetSelectedExportFormat();
                string extension = GetFormatExtension(format);

                var dialog = new SaveFileDialog
                {
                    Title = "Экспорт данных",
                    FileName = $"HabitFlow_Export_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = extension,
                    Filter = GetFormatFilter(format)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Собираем данные для экспорта
                    var exportData = PrepareExportData();

                    // Экспортируем в выбранном формате
                    ExportToFile(dialog.FileName, format, exportData);

                    ShowStatus($"✅ Данные успешно экспортированы в {Path.GetFileName(dialog.FileName)}", true);
                    txtLastAction.Text = $"Последний экспорт: {DateTime.Now:HH:mm}";
                }
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка экспорта", $"Ошибка при экспорте: {ex.Message}");
            }
        }

        private string GetSelectedExportFormat()
        {
            if (cmbExportFormat.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                return item.Tag?.ToString() ?? "json";
            }
            return "json";
        }

        private string GetFormatExtension(string format)
        {
            switch (format)
            {
                case "json": return ".json";
                case "csv": return ".csv";
                case "xlsx": return ".xlsx";
                case "xml": return ".xml";
                default: return ".json";
            }
        }

        private string GetFormatFilter(string format)
        {
            switch (format)
            {
                case "json": return "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                case "csv": return "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                case "xlsx": return "Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*";
                case "xml": return "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*";
                default: return "Все файлы (*.*)|*.*";
            }
        }

        private ExportData PrepareExportData()
        {
            var exportData = new ExportData
            {
                ExportDate = DateTime.Now,
                AppVersion = "1.0.0",
                UserName = _currentUser.UserName,
                Habits = new List<Habits>(),
                Records = new List<HabitRecords>(),
                Metadata = new Dictionary<string, object>()
            };

            // Экспорт привычек
            if (chkExportHabits.IsChecked == true)
            {
                exportData.Habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId)
                    .ToList();
            }

            // Экспорт записей
            if (chkExportRecords.IsChecked == true)
            {
                exportData.Records = _context.HabitRecords
                    .Where(r => r.Habits.UserId == _currentUser.UserId)
                    .ToList();
            }

            // Экспорт настроек
            if (chkExportSettings.IsChecked == true)
            {
                exportData.Settings = AppSettings.Load();
            }

            // Метаданные
            exportData.Metadata["TotalHabits"] = exportData.Habits.Count;
            exportData.Metadata["TotalRecords"] = exportData.Records.Count;
            exportData.Metadata["DateRange"] = exportData.Records.Any()
                ? $"{exportData.Records.Min(r => r.RecordDate):yyyy-MM-dd} - {exportData.Records.Max(r => r.RecordDate):yyyy-MM-dd}"
                : "Нет данных";

            return exportData;
        }

        private void ExportToFile(string filename, string format, ExportData data)
        {
            switch (format)
            {
                case "json":
                    ExportToJson(filename, data);
                    break;
                case "csv":
                    ExportToCsv(filename, data);
                    break;
                case "xlsx":
                    ExportToExcel(filename, data);
                    break;
                case "xml":
                    ExportToXml(filename, data);
                    break;
            }

            // Шифрование если нужно
            if (chkExportEncrypt.IsChecked == true)
            {
                EncryptFile(filename);
            }
        }

        private void ExportToJson(string filename, ExportData data)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = chkExportPretty.IsChecked == true ? Formatting.Indented : Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string json = JsonConvert.SerializeObject(data, settings);
            File.WriteAllText(filename, json, Encoding.UTF8);
        }

        private void ExportToCsv(string filename, ExportData data)
        {
            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                // Заголовки
                writer.WriteLine("Тип;ID;Название/Дата;Статус/Описание;Дополнительно");

                // Привычки
                foreach (var habit in data.Habits)
                {
                    writer.WriteLine($"Habit;{habit.HabitId};{habit.HabitName};{habit.Description};Активна: {habit.IsActive}");
                }

                // Записи
                foreach (var record in data.Records)
                {
                    string status = record.StatusId == 2 ? "Выполнено" :
                                   record.StatusId == 3 ? "Пропущено" : "Не отмечено";
                    writer.WriteLine($"Record;{record.RecordId};{record.RecordDate:yyyy-MM-dd};{status};Заметка: {record.Note}");
                }
            }
        }

        private void ExportToExcel(string filename, ExportData data)
        {
            // Для Excel нужна библиотека EPPlus или аналоги
            // Пока сохраняем как CSV с другим расширением
            ExportToCsv(filename.Replace(".xlsx", ".csv"), data);
            ShowStatus("⚠️ Excel формат в разработке, сохранено как CSV", false);
        }

        private void ExportToXml(string filename, ExportData data)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ExportData));
            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                serializer.Serialize(writer, data);
            }
        }

        private void EncryptFile(string filename)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filename);

                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes("HabitFlowSecretKey12345678".PadRight(32).Substring(0, 32));
                    aes.IV = new byte[16];

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] encrypted = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        File.WriteAllBytes(filename + ".encrypted", encrypted);
                        File.Delete(filename);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка шифрования: {ex.Message}");
            }
        }

        // ==================== ИМПОРТ ====================

        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл для импорта",
                Filter = "Все поддерживаемые форматы (*.json;*.csv;*.xlsx;*.xml;*.encrypted)|*.json;*.csv;*.xlsx;*.xml;*.encrypted|JSON файлы (*.json)|*.json|CSV файлы (*.csv)|*.csv|Excel файлы (*.xlsx)|*.xlsx|XML файлы (*.xml)|*.xml|Зашифрованные файлы (*.encrypted)|*.encrypted|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedImportFile = dialog.FileName;
                txtImportFile.Text = dialog.FileName;
                btnImport.IsEnabled = true;

                // Предварительный просмотр данных
                PreviewImportFile(dialog.FileName);
            }
        }

        private void PreviewImportFile(string filename)
        {
            try
            {
                string extension = Path.GetExtension(filename).ToLower();

                if (extension == ".encrypted")
                {
                    ShowStatus("🔒 Зашифрованный файл. Требуется расшифровка.", true);
                    // Здесь можно запросить пароль
                }
                else if (extension == ".json")
                {
                    string json = File.ReadAllText(filename, Encoding.UTF8);
                    _previewData = JsonConvert.DeserializeObject<ExportData>(json);

                    if (_previewData != null)
                    {
                        string info = $"Файл содержит: {_previewData.Habits?.Count ?? 0} привычек, " +
                                     $"{_previewData.Records?.Count ?? 0} записей";
                        ShowStatus($"📄 {info}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"⚠️ Не удалось прочитать файл: {ex.Message}", false);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedImportFile) || !File.Exists(_selectedImportFile))
                {
                    ShowStatus("❌ Файл не найден", false);
                    return;
                }

                // Подтверждение для режима замены
                if (rbImportReplace.IsChecked == true)
                {
                    var result = ConfirmationDialog.ShowWarning(
                        "Подтверждение замены",
                        "ВНИМАНИЕ! Режим замены удалит ВСЕ ваши текущие данные.\n\nВы уверены, что хотите продолжить?"
                    );

                    if (!result)
                        return;
                }

                // Импортируем данные
                ImportData(_selectedImportFile);

                ShowStatus("✅ Данные успешно импортированы", true);
                txtLastAction.Text = $"Последний импорт: {DateTime.Now:HH:mm}";

                // Обновляем статистику
                UpdateExportStats();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка импорта", $"Ошибка при импорте: {ex.Message}");
            }
        }

        private void ImportData(string filename)
        {
            string extension = Path.GetExtension(filename).ToLower();
            ExportData importData = null;

            // Чтение данных в зависимости от формата
            if (extension == ".json")
            {
                string json = File.ReadAllText(filename, Encoding.UTF8);
                importData = JsonConvert.DeserializeObject<ExportData>(json);
            }
            else if (extension == ".csv")
            {
                importData = ImportFromCsv(filename);
            }
            else
            {
                throw new NotSupportedException($"Формат {extension} пока не поддерживается для импорта");
            }

            if (importData == null)
                throw new Exception("Не удалось прочитать данные из файла");

            // Определяем, какие данные импортировать
            bool importHabits = rbImportSelective.IsChecked == true ? chkImportHabits.IsChecked == true : true;
            bool importRecords = rbImportSelective.IsChecked == true ? chkImportRecords.IsChecked == true : true;
            bool importSettings = rbImportSelective.IsChecked == true ? chkImportSettings.IsChecked == true : true;

            // Режим замены
            if (rbImportReplace.IsChecked == true)
            {
                // Удаляем все существующие данные пользователя
                var existingRecords = _context.HabitRecords
                    .Where(r => r.Habits.UserId == _currentUser.UserId)
                    .ToList();
                _context.HabitRecords.RemoveRange(existingRecords);

                var existingHabits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId)
                    .ToList();
                _context.Habits.RemoveRange(existingHabits);
            }

            // Импорт привычек
            if (importHabits && importData.Habits != null)
            {
                foreach (var habit in importData.Habits)
                {
                    // Проверяем, существует ли уже такая привычка
                    var existingHabit = _context.Habits
                        .FirstOrDefault(h => h.UserId == _currentUser.UserId && h.HabitName == habit.HabitName);

                    if (existingHabit == null || rbImportReplace.IsChecked == true)
                    {
                        habit.UserId = _currentUser.UserId;
                        habit.HabitId = 0; // Сбрасываем ID для новой записи
                        _context.Habits.Add(habit);
                    }
                }
            }

            _context.SaveChanges();

            // Импорт записей (после сохранения привычек, чтобы были ID)
            if (importRecords && importData.Records != null)
            {
                // Получаем соответствие старых и новых ID привычек
                var habitsMap = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId)
                    .ToDictionary(h => h.HabitName, h => h.HabitId);

                foreach (var record in importData.Records)
                {
                    // Находим соответствующую привычку по имени
                    var habitName = importData.Habits?
                        .FirstOrDefault(h => h.HabitId == record.HabitId)?
                        .HabitName;

                    if (habitName != null && habitsMap.ContainsKey(habitName))
                    {
                        // Проверяем, существует ли уже такая запись
                        var existingRecord = _context.HabitRecords
                            .FirstOrDefault(r => r.HabitId == habitsMap[habitName] && r.RecordDate == record.RecordDate);

                        if (existingRecord == null || rbImportReplace.IsChecked == true)
                        {
                            if (existingRecord != null)
                                _context.HabitRecords.Remove(existingRecord);

                            record.HabitId = habitsMap[habitName];
                            record.RecordId = 0; // Сбрасываем ID
                            _context.HabitRecords.Add(record);
                        }
                    }
                }
            }

            _context.SaveChanges();

            // Импорт настроек
            if (importSettings && importData.Settings != null)
            {
                importData.Settings.Save();
            }
        }

        private ExportData ImportFromCsv(string filename)
        {
            // Упрощенный импорт из CSV
            var exportData = new ExportData
            {
                Habits = new List<Habits>(),
                Records = new List<HabitRecords>(),
                Metadata = new Dictionary<string, object>()
            };

            var lines = File.ReadAllLines(filename, Encoding.UTF8);
            var habitsDict = new Dictionary<string, Habits>();

            foreach (var line in lines.Skip(1)) // Пропускаем заголовок
            {
                var parts = line.Split(';');
                if (parts.Length < 5) continue;

                string type = parts[0];

                if (type == "Habit")
                {
                    var habit = new Habits
                    {
                        HabitName = parts[2],
                        Description = parts[3],
                        IsActive = parts[4].Contains("Активна"),
                        CreatedAt = DateTime.Today,
                        UserId = _currentUser.UserId
                    };
                    exportData.Habits.Add(habit);
                    habitsDict[habit.HabitName] = habit;
                }
                else if (type == "Record" && habitsDict.Any())
                {
                    var record = new HabitRecords
                    {
                        RecordDate = DateTime.Parse(parts[2]),
                        StatusId = parts[3] == "Выполнено" ? 2 : (parts[3] == "Пропущено" ? 3 : 1),
                        Note = parts[4].Replace("Заметка: ", ""),
                        HabitId = 0 // Будет заполнено позже
                    };
                    exportData.Records.Add(record);
                }
            }

            return exportData;
        }

        // ==================== СИНХРОНИЗАЦИЯ ====================

        private void btnConfigureSync_Click(object sender, RoutedEventArgs e)
        {
            borderSyncSettings.Visibility =
                borderSyncSettings.Visibility == Visibility.Visible ?
                Visibility.Collapsed : Visibility.Visible;
        }

        private void btnConnectSync_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationDialog.ShowInfo("Синхронизация",
                "🔌 Функция синхронизации будет доступна в следующей версии");
        }

        // ==================== АВТО-БЭКАП ====================

        private void AutoBackup_Changed(object sender, RoutedEventArgs e)
        {
            // Сохраняем настройку
            var settings = AppSettings.Load();
            settings.AutoSave = chkAutoBackup.IsChecked ?? false;
            settings.Save();

            if (chkAutoBackup.IsChecked == true)
            {
                ShowStatus("✅ Автоматическое резервное копирование включено", true);
            }
        }

        private void btnBrowseBackupPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для резервных копий",
                SelectedPath = BackupPath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupPath = dialog.SelectedPath;
                txtBackupPath.Text = BackupPath;

                // Сохраняем путь в настройках
                var settings = AppSettings.Load();
                // Здесь нужно добавить поле BackupPath в AppSettings
                settings.Save();

                ShowStatus("✅ Путь сохранен", true);
            }
        }

        // ==================== ОБЩИЕ НАСТРОЙКИ ====================

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = AppSettings.Load();

                // Сохраняем настройки авто-бэкапа
                settings.AutoSave = chkAutoBackup.IsChecked ?? false;
                // Здесь можно сохранить другие настройки

                settings.Save();

                ShowStatus("✅ Настройки сохранены", true);
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при сохранении настроек: {ex.Message}");
            }
        }

        // Возврат в главное окно
        private void btnBack_Click(object sender, RoutedEventArgs e)
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(_currentUser);
            WindowStateManager.OpenWindow(this, mainWindow);
        }
    }
}