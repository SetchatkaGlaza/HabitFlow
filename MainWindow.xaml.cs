using HabitFlow.Entity;
using HabitFlow.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Модель данных для отображения привычки в списке
    /// </summary>
    public class HabitViewModel
    {
        public int HabitId { get; set; }
        public string HabitName { get; set; }
        public string Description { get; set; }
        public string IconEmoji { get; set; }
        public string ColorCode { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public ObservableCollection<DayRecordViewModel> WeekRecords { get; set; }
    }

    /// <summary>
    /// Модель данных для отметки по дню
    /// </summary>
    public class DayRecordViewModel
    {
        public DateTime Date { get; set; }
        public int StatusId { get; set; }
        public string DisplaySymbol { get; set; }
        public string DayStatusColor { get; set; }
        public int HabitId { get; set; }
    }

    /// <summary>
    /// Модель для заголовков дней недели
    /// </summary>
    public class DayHeaderViewModel
    {
        public string DayName { get; set; }
        public string DayNumber { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private ObservableCollection<HabitViewModel> _habits;
        private List<DayHeaderViewModel> _weekDays;
        private System.Windows.Threading.DispatcherTimer _motivationTimer;
        private System.Windows.Threading.DispatcherTimer _autoSaveTimer;

        public MainWindow(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            _context = new HabitTrackerEntities();
            _habits = new ObservableCollection<HabitViewModel>();

            txtUserName.Text = user.UserName;
            UpdateCurrentDate();

            // Таймер для смены мотивационных сообщений
            _motivationTimer = new System.Windows.Threading.DispatcherTimer();
            _motivationTimer.Interval = TimeSpan.FromSeconds(10);
            _motivationTimer.Tick += MotivationTimer_Tick;
            _motivationTimer.Start();

            // Таймер для автосохранения (каждые 5 минут)
            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(5);
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWeekDays();
            LoadHabits();
            LoadHabitsForStatsCombo();
            LoadCategories();
            UpdateMotivationMessage();
            CheckForReminders();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _motivationTimer?.Stop();
            _autoSaveTimer?.Stop();
            _context?.Dispose();
        }

        // Перемещение окна
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Обновление текущей даты
        private void UpdateCurrentDate()
        {
            var today = DateTime.Now;
            txtCurrentDateValue.Text = today.ToString("dddd, dd MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));
        }

        // Загрузка дней текущей недели
        private void LoadWeekDays()
        {
            _weekDays = new List<DayHeaderViewModel>();
            var today = DateTime.Today;

            // Определяем первый день недели (понедельник)
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (today.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                _weekDays.Add(new DayHeaderViewModel
                {
                    DayName = GetRussianDayName(date.DayOfWeek),
                    DayNumber = date.Day.ToString(),
                    Date = date
                });
            }

            weekDayHeaders.ItemsSource = _weekDays;
        }

        // Получение русского названия дня недели
        private string GetRussianDayName(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "ПН";
                case DayOfWeek.Tuesday: return "ВТ";
                case DayOfWeek.Wednesday: return "СР";
                case DayOfWeek.Thursday: return "ЧТ";
                case DayOfWeek.Friday: return "ПТ";
                case DayOfWeek.Saturday: return "СБ";
                case DayOfWeek.Sunday: return "ВС";
                default: return "";
            }
        }

        // Получение символа для статуса
        private string GetStatusSymbol(int statusId)
        {
            switch (statusId)
            {
                case 1: return "◻"; // Не отмечено
                case 2: return "✅"; // Выполнено
                case 3: return "➖"; // Пропущено
                default: return "◻";
            }
        }

        // Получение цвета для статуса
        private string GetStatusColor(int statusId)
        {
            switch (statusId)
            {
                case 1: return "#E0E0E0"; // Серый
                case 2: return "#4CAF50"; // Зеленый
                case 3: return "#F44336"; // Красный
                default: return "#E0E0E0";
            }
        }

        // Загрузка привычек пользователя
        private void LoadHabits()
        {
            try
            {
                var habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .Include(h => h.HabitRecords)
                    .OrderBy(h => h.HabitName)
                    .ToList();

                _habits.Clear();

                foreach (var habit in habits)
                {
                    var habitVM = new HabitViewModel
                    {
                        HabitId = habit.HabitId,
                        HabitName = habit.HabitName,
                        Description = habit.Description,
                        IconEmoji = habit.IconEmoji ?? "📌",
                        ColorCode = habit.ColorCode ?? "#4CAF50",
                        CurrentStreak = CalculateCurrentStreak(habit.HabitId),
                        MaxStreak = CalculateMaxStreak(habit.HabitId),
                        WeekRecords = new ObservableCollection<DayRecordViewModel>()
                    };

                    // Загружаем записи за текущую неделю
                    foreach (var day in _weekDays)
                    {
                        var record = habit.HabitRecords
                            .FirstOrDefault(r => r.RecordDate == day.Date);

                        var dayVM = new DayRecordViewModel
                        {
                            Date = day.Date,
                            HabitId = habit.HabitId,
                            StatusId = record?.StatusId ?? 1,
                            DisplaySymbol = GetStatusSymbol(record?.StatusId ?? 1),
                            DayStatusColor = GetStatusColor(record?.StatusId ?? 1)
                        };

                        habitVM.WeekRecords.Add(dayVM);
                    }

                    _habits.Add(habitVM);
                }

                habitsListView.ItemsSource = _habits;
                UpdateOverallStats();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке привычек: {ex.Message}");
            }
        }

        // Расчет текущей серии
        private int CalculateCurrentStreak(int habitId)
        {
            var records = _context.HabitRecords
                .Where(r => r.HabitId == habitId && r.StatusId == 2)
                .OrderByDescending(r => r.RecordDate)
                .Select(r => r.RecordDate)
                .ToList();

            if (!records.Any())
                return 0;

            int streak = 1;
            var today = DateTime.Today;

            // Проверяем, выполнена ли привычка сегодня
            if (!records.Contains(today))
                return 0;

            for (int i = 1; i < records.Count; i++)
            {
                if (records[i] == today.AddDays(-i))
                    streak++;
                else
                    break;
            }

            return streak;
        }

        // Расчет максимальной серии
        private int CalculateMaxStreak(int habitId)
        {
            var records = _context.HabitRecords
                .Where(r => r.HabitId == habitId && r.StatusId == 2)
                .OrderBy(r => r.RecordDate)
                .Select(r => r.RecordDate)
                .ToList();

            if (!records.Any())
                return 0;

            int maxStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < records.Count; i++)
            {
                if (records[i] == records[i - 1].AddDays(1))
                {
                    currentStreak++;
                    if (currentStreak > maxStreak)
                        maxStreak = currentStreak;
                }
                else
                {
                    currentStreak = 1;
                }
            }

            return maxStreak;
        }

        // Обновление общей статистики
        private void UpdateOverallStats()
        {
            txtTotalHabits.Text = _habits.Count.ToString();

            var today = DateTime.Today;
            var completedToday = _context.HabitRecords
                .Count(r => r.RecordDate == today && r.StatusId == 2
                    && r.Habits.UserId == _currentUser.UserId);

            txtCompletedToday.Text = completedToday.ToString();
        }

        // Загрузка привычек для выпадающего списка статистики
        private void LoadHabitsForStatsCombo()
        {
            cmbHabitStats.ItemsSource = _habits.ToList();
            if (_habits.Any())
                cmbHabitStats.SelectedIndex = 0;
        }

        // Загрузка категорий (для фильтрации)
        private void LoadCategories()
        {
            try
            {
                var categories = _context.Categories
                    .Where(c => c.UserId == _currentUser.UserId && c.IsActive == true)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();

                cmbCategoryFilter.Items.Clear();

                var allItem = new ComboBoxItem
                {
                    Content = "Все категории",
                    Tag = 0
                };
                cmbCategoryFilter.Items.Add(allItem);

                foreach (var category in categories)
                {
                    var item = new ComboBoxItem
                    {
                        Content = category.CategoryName,
                        Tag = category.CategoryId
                    };
                    cmbCategoryFilter.Items.Add(item);
                }

                cmbCategoryFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        // Фильтрация по категории
        private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategoryFilter.SelectedItem is ComboBoxItem selectedItem)
            {
                int categoryId = (int)selectedItem.Tag;

                if (categoryId == 0)
                {
                    // Показываем все привычки
                    habitsListView.ItemsSource = _habits;
                }
                else
                {
                    // Фильтруем по категории
                    var habitIdsInCategory = _context.HabitCategories
                        .Where(hc => hc.CategoryId == categoryId)
                        .Select(hc => hc.HabitId)
                        .ToHashSet();

                    var filteredHabits = _habits.Where(h => habitIdsInCategory.Contains(h.HabitId)).ToList();
                    habitsListView.ItemsSource = filteredHabits;
                }
            }
        }

        // Обработчик клика по дню
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dayVM = button?.Tag as DayRecordViewModel;

            if (dayVM != null)
            {
                try
                {
                    // Проверяем, можно ли отмечать будущие даты
                    if (dayVM.Date > DateTime.Today)
                    {
                        ConfirmationDialog.ShowInfo("Отметка будущего дня",
                            "Нельзя отметить привычку на будущую дату.");
                        return;
                    }

                    // Циклическое изменение статуса: 1 -> 2 -> 3 -> 1
                    int newStatusId = dayVM.StatusId == 1 ? 2 : (dayVM.StatusId == 2 ? 3 : 1);

                    // Обновляем в базе данных
                    var existingRecord = _context.HabitRecords
                        .FirstOrDefault(r => r.HabitId == dayVM.HabitId && r.RecordDate == dayVM.Date);

                    if (existingRecord != null)
                    {
                        existingRecord.StatusId = newStatusId;
                        existingRecord.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        var newRecord = new HabitRecords
                        {
                            HabitId = dayVM.HabitId,
                            RecordDate = dayVM.Date,
                            StatusId = newStatusId,
                            UpdatedAt = DateTime.Now
                        };
                        _context.HabitRecords.Add(newRecord);
                    }

                    _context.SaveChanges();

                    // Обновляем отображение
                    dayVM.StatusId = newStatusId;
                    dayVM.DisplaySymbol = GetStatusSymbol(newStatusId);
                    dayVM.DayStatusColor = GetStatusColor(newStatusId);

                    // Обновляем статистику
                    var habit = _habits.FirstOrDefault(h => h.HabitId == dayVM.HabitId);
                    if (habit != null)
                    {
                        habit.CurrentStreak = CalculateCurrentStreak(dayVM.HabitId);
                        habit.MaxStreak = CalculateMaxStreak(dayVM.HabitId);
                    }

                    // Обновляем общую статистику
                    UpdateOverallStats();

                    // Если выбрана эта привычка в статистике, обновляем детали
                    var selectedHabit = cmbHabitStats.SelectedItem as HabitViewModel;
                    if (selectedHabit != null && selectedHabit.HabitId == dayVM.HabitId)
                    {
                        UpdateHabitStatsDetail(selectedHabit);
                    }

                    // Принудительно обновляем список
                    habitsListView.Items.Refresh();

                    // Проверяем достижения
                    CheckAchievements();
                }
                catch (Exception ex)
                {
                    ConfirmationDialog.ShowError("Ошибка", $"Ошибка при сохранении отметки: {ex.Message}");
                }
            }
        }

        // Проверка достижений
        private void CheckAchievements()
        {
            // Здесь можно добавить логику проверки достижений
            // Например, проверить, достигнут ли новый рекорд
        }

        // Добавление привычки
        private void btnAddHabit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditHabitWindow(_currentUser.UserId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                LoadHabits();
                LoadHabitsForStatsCombo();
                LoadCategories();
            }
        }

        // Редактирование привычки
        private void btnEditHabit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int habitId)
            {
                var dialog = new AddEditHabitWindow(_currentUser.UserId, habitId);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    LoadHabits();
                    LoadHabitsForStatsCombo();
                    LoadCategories();
                }
            }
        }

        // Удаление привычки
        private void btnDeleteHabit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int habitId)
            {
                var habit = _context.Habits.Find(habitId);
                if (habit == null) return;

                bool confirmed = ConfirmationDialog.ShowDelete(
                    "Удаление привычки",
                    $"Вы уверены, что хотите удалить привычку \"{habit.HabitName}\"?",
                    new List<string> { "• Все данные о выполнении будут потеряны", "• Статистика будет удалена" }
                );

                if (confirmed)
                {
                    try
                    {
                        // Soft delete
                        habit.IsActive = false;
                        _context.SaveChanges();

                        LoadHabits();
                        LoadHabitsForStatsCombo();
                        LoadCategories();

                        ConfirmationDialog.ShowSuccess("Успешно", "Привычка удалена.");
                    }
                    catch (Exception ex)
                    {
                        ConfirmationDialog.ShowError("Ошибка", $"Ошибка при удалении: {ex.Message}");
                    }
                }
            }
        }

        // Выбор привычки для детальной статистики
        private void cmbHabitStats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedHabit = cmbHabitStats.SelectedItem as HabitViewModel;
            if (selectedHabit != null)
            {
                UpdateHabitStatsDetail(selectedHabit);
            }
        }

        // Обновление детальной статистики
        private void UpdateHabitStatsDetail(HabitViewModel habit)
        {
            txtCurrentStreakDetail.Text = habit.CurrentStreak.ToString();
            txtMaxStreakDetail.Text = habit.MaxStreak.ToString();

            // Прогресс за неделю
            var weekRecords = habit.WeekRecords;
            var completedThisWeek = weekRecords.Count(r => r.StatusId == 2);
            var weeklyPercent = (completedThisWeek * 100) / 7;
            txtWeeklyProgress.Text = $"{weeklyPercent}%";

            // Прогресс за месяц
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var monthRecords = _context.HabitRecords
                .Where(r => r.HabitId == habit.HabitId
                    && r.RecordDate >= startOfMonth
                    && r.RecordDate <= endOfMonth)
                .ToList();

            var completedThisMonth = monthRecords.Count(r => r.StatusId == 2);
            var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            var monthlyPercent = daysInMonth > 0 ? (completedThisMonth * 100) / daysInMonth : 0;

            txtMonthlyProgress.Text = $"{monthlyPercent}%";
            txtMonthlyPercent.Text = $"{monthlyPercent}%";

            if (progressMonthlyFill.Parent is Border parent)
            {
                progressMonthlyFill.Width = (parent.ActualWidth * monthlyPercent) / 100;
            }
        }

        // Таймер для мотивационных сообщений
        private void MotivationTimer_Tick(object sender, EventArgs e)
        {
            UpdateMotivationMessage();
        }

        // Обновление мотивационного сообщения
        private void UpdateMotivationMessage()
        {
            string[] messages = {
                "🌟 Маленькие шаги каждый день приводят к большим результатам!",
                "💪 Ты сегодня уже молодец, что зашел отметить прогресс!",
                "🎯 Каждая отмеченная привычка - это шаг к лучшей версии себя!",
                "✨ Даже если сегодня не всё получилось - завтра новый день!",
                "🔥 Твоя продуктивность растет с каждым днем!",
                "⭐ Помни: последовательность важнее интенсивности!",
                "📈 Маленькие победы складываются в большие достижения!"
            };

            var random = new Random();
            txtMotivationMessage.Text = messages[random.Next(messages.Length)];
        }

        // Автосохранение
        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _context.SaveChanges();
                System.Diagnostics.Debug.WriteLine($"Автосохранение выполнено в {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка автосохранения: {ex.Message}");
            }
        }

        // Проверка напоминаний
        private void CheckForReminders()
        {
            // Здесь можно добавить логику проверки напоминаний
            // Например, если есть непривычки, которые нужно отметить сегодня
        }

        // Открытие профиля
        private void UserProfile_Click(object sender, MouseButtonEventArgs e)
        {
            ProfileWindow profileWindow = new ProfileWindow(_currentUser);
            profileWindow.Owner = this;
            profileWindow.ShowDialog();

            // Обновляем имя пользователя (на случай изменения)
            txtUserName.Text = _currentUser.UserName;
        }

        // Открытие статистики
        private void btnStatistics_Click(object sender, RoutedEventArgs e)
        {
            StatisticsWindow statsWindow = new StatisticsWindow(_currentUser);
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }

        // Открытие истории
        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow(_currentUser);
            historyWindow.Owner = this;
            historyWindow.ShowDialog();
        }

        // Открытие достижений
        private void btnAchievements_Click(object sender, RoutedEventArgs e)
        {
            AchievementsWindow achievementsWindow = new AchievementsWindow(_currentUser);
            achievementsWindow.Owner = this;
            achievementsWindow.ShowDialog();
        }

        // Открытие категорий
        private void btnCategories_Click(object sender, RoutedEventArgs e)
        {
            CategoryWindow categoryWindow = new CategoryWindow(_currentUser);
            categoryWindow.Owner = this;

            if (categoryWindow.ShowDialog() == true)
            {
                // Если нужно обновить отображение с сортировкой по категориям
                LoadHabits();
            }
        }

        // Открытие импорта/экспорта
        private void btnImportExport_Click(object sender, RoutedEventArgs e)
        {
            ImportExportWindow importExportWindow = new ImportExportWindow(_currentUser);
            importExportWindow.Owner = this;
            importExportWindow.ShowDialog();
        }

        // Открытие настроек
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(_currentUser);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        // Открытие информации о программе
        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow(_currentUser);
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal
                ? WindowState.Maximized
                : WindowState.Normal;
        }

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

        // Выход из аккаунта
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = ConfirmationDialog.ShowWarning(
                "Выход из аккаунта",
                "Вы уверены, что хотите выйти?"
            );

            if (result)
            {
                // Очищаем сохраненные данные
                Settings.Default.RememberedUserId = 0;
                Settings.Default.RememberedUserName = "";
                Settings.Default.Save();

                // Открываем окно входа
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                // Закрываем текущее окно
                this.Close();
            }
        }
    }
}