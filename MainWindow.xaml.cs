using HabitFlow.Entity;
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWeekDays();
            LoadHabits();
            LoadHabitsForStatsCombo();
            UpdateMotivationMessage();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _motivationTimer?.Stop();
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
                    .ToList();

                _habits.Clear();

                foreach (var habit in habits)
                {
                    var habitVM = new HabitViewModel
                    {
                        HabitId = habit.HabitId,
                        HabitName = habit.HabitName,
                        Description = habit.Description,
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
                MessageBox.Show($"Ошибка при загрузке привычек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Расчет текущей серии
        private int CalculateCurrentStreak(int habitId)
        {
            var records = _context.HabitRecords
                .Where(r => r.HabitId == habitId && r.StatusId == 2)
                .OrderByDescending(r => r.RecordDate)
                .ToList();

            if (!records.Any())
                return 0;

            int streak = 0;
            var currentDate = DateTime.Today;

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].RecordDate == currentDate.AddDays(-i))
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
                .ToList();

            if (!records.Any())
                return 0;

            int maxStreak = 0;
            int currentStreak = 1;

            for (int i = 1; i < records.Count; i++)
            {
                if (records[i].RecordDate == records[i - 1].RecordDate.AddDays(1))
                {
                    currentStreak++;
                }
                else
                {
                    if (currentStreak > maxStreak)
                        maxStreak = currentStreak;
                    currentStreak = 1;
                }
            }

            if (currentStreak > maxStreak)
                maxStreak = currentStreak;

            return maxStreak;
        }

        // Обновление общей статистики
        private void UpdateOverallStats()
        {
            txtTotalHabits.Text = _habits.Count.ToString();

            var today = DateTime.Today;
            var completedToday = _context.HabitRecords
                .Count(r => r.RecordDate == today && r.StatusId == 2
                    && _habits.Select(h => h.HabitId).Contains(r.HabitId));

            txtCompletedToday.Text = completedToday.ToString();
        }

        // Загрузка привычек для выпадающего списка статистики
        private void LoadHabitsForStatsCombo()
        {
            cmbHabitStats.ItemsSource = _habits.ToList();
            if (_habits.Any())
                cmbHabitStats.SelectedIndex = 0;
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении отметки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
            }
        }

        // Редактирование привычки
        private void btnEditHabit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int habitId = (int)button.Tag;

            var dialog = new AddEditHabitWindow(_currentUser.UserId, habitId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                LoadHabits();
                LoadHabitsForStatsCombo();
            }
        }

        // Удаление привычки
        private void btnDeleteHabit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int habitId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту привычку? Все данные будут потеряны.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var habit = _context.Habits.Find(habitId);
                    if (habit != null)
                    {
                        habit.IsActive = false;
                        _context.SaveChanges();

                        LoadHabits();
                        LoadHabitsForStatsCombo();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Выбор привычки для детальной статистики
        private void cmbHabitStats_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
            var monthlyPercent = (completedThisMonth * 100) / daysInMonth;

            txtMonthlyProgress.Text = $"{monthlyPercent}%";
            txtMonthlyPercent.Text = $"{monthlyPercent}%";
            progressMonthlyFill.Width = (progressMonthlyFill.Parent as Border).ActualWidth * monthlyPercent / 100;
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
            Application.Current.Shutdown();
        }

        // Выход из аккаунта
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Очищаем сохраненные данные
                Properties.Settings.Default.RememberedUserId = 0;
                Properties.Settings.Default.RememberedUserName = "";
                Properties.Settings.Default.Save();

                // Открываем окно входа
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                // Закрываем текущее окно
                this.Close();
            }
        }
    }
}