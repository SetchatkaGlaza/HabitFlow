using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HabitFlow.Entity;
using HabitFlow.Properties;
using LiveCharts;
using LiveCharts.Wpf;

namespace HabitFlow
{
    /// <summary>
    /// Модель данных для тепловой карты
    /// </summary>
    public class HeatMapMonth
    {
        public string Month { get; set; }
        public List<HeatMapWeek> Weeks { get; set; }
    }

    public class HeatMapWeek
    {
        public List<HeatMapDay> Days { get; set; }
    }

    public class HeatMapDay
    {
        public DateTime Date { get; set; }
        public string Color { get; set; }
        public string ToolTip { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private int? _selectedHabitId;
        private string _currentPeriod = "week"; // week, month, quarter, year, all

        // Коллекции для графиков
        public ChartValues<int> CompletedDailyValues { get; set; }
        public ChartValues<int> SkippedDailyValues { get; set; }
        public List<string> DailyLabels { get; set; }

        public ChartValues<int> WeekdayValues { get; set; }
        public List<string> WeekdayLabels { get; set; }

        public ChartValues<double> MonthlyPercentValues { get; set; }
        public List<string> MonthlyLabels { get; set; }

        public ChartValues<int> CompletedPieValue { get; set; }
        public ChartValues<int> SkippedPieValue { get; set; }
        public ChartValues<int> NotSetPieValue { get; set; }

        // Форматтеры для графиков
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> PercentFormatter { get; set; }
        public Func<ChartPoint, string> PieLabelPoint { get; set; }

        public StatisticsWindow(Users user)
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            if (user == null)
            {
                ConfirmationDialog.ShowError("Ошибка", "Пользователь не найден");
                Close();
                return;
            }

            _context = new HabitTrackerEntities();
            _currentUser = user;

            InitializeCharts();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadHabitsForSelector();
                LoadStatistics();
                UpdateMotivationPhrase();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке статистики: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Инициализация графиков
        private void InitializeCharts()
        {
            CompletedDailyValues = new ChartValues<int>();
            SkippedDailyValues = new ChartValues<int>();
            DailyLabels = new List<string>();

            WeekdayValues = new ChartValues<int>();
            WeekdayLabels = new List<string> { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };

            MonthlyPercentValues = new ChartValues<double>();
            MonthlyLabels = new List<string>();

            CompletedPieValue = new ChartValues<int>();
            SkippedPieValue = new ChartValues<int>();
            NotSetPieValue = new ChartValues<int>();

            YFormatter = value => value.ToString("N0");
            PercentFormatter = value => $"{value:F0}%";
            PieLabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P1})";
        }

        // Загрузка привычек в выпадающий список
        private void LoadHabitsForSelector()
        {
            try
            {
                if (_currentUser == null) return;

                var habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .OrderBy(h => h.HabitName)
                    .ToList();

                cmbHabitSelector.ItemsSource = habits;

                if (habits.Any())
                {
                    cmbHabitSelector.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки привычек: {ex.Message}");
            }
        }

        // Загрузка статистики
        private void LoadStatistics()
        {
            try
            {
                if (_currentUser == null) return;

                if (rbAllHabits.IsChecked == true)
                {
                    _selectedHabitId = null;
                    LoadAllHabitsStatistics();
                }
                else
                {
                    if (cmbHabitSelector == null)
                    {
                        System.Diagnostics.Debug.WriteLine("cmbHabitSelector is null");
                        return;
                    }

                    var selectedItem = cmbHabitSelector.SelectedItem;
                    if (selectedItem == null)
                    {
                        System.Diagnostics.Debug.WriteLine("selectedItem is null");
                        return;
                    }

                    var selectedHabit = selectedItem as Habits;
                    if (selectedHabit == null)
                    {
                        System.Diagnostics.Debug.WriteLine("selectedHabit is null");
                        return;
                    }

                    _selectedHabitId = selectedHabit.HabitId;
                    LoadHabitStatistics(_selectedHabitId.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
                ConfirmationDialog.ShowError("Ошибка", $"Не удалось загрузить статистику: {ex.Message}");
            }
        }

        // Загрузка статистики по конкретной привычке
        private void LoadHabitStatistics(int habitId)
        {
            try
            {
                if (_context == null || _currentUser == null) return;

                var habit = _context.Habits.Find(habitId);
                if (habit == null) return;

                // Получаем все записи привычки
                var records = _context.HabitRecords
                    .Where(r => r.HabitId == habitId)
                    .OrderBy(r => r.RecordDate)
                    .ToList();

                if (records == null) records = new List<HabitRecords>();

                // Обновляем быструю статистику
                UpdateQuickStats(records);

                // Обновляем ключевые показатели
                UpdateKeyMetrics(habitId, records);

                // Обновляем графики в зависимости от периода
                UpdateDailyChart(records);
                UpdateWeekdayChart(records);
                UpdateMonthlyChart(records);
                UpdatePieChart(records);
                UpdateHeatMap(records);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики привычки: {ex.Message}");
            }
        }

        // Загрузка статистики по всем привычкам
        private void LoadAllHabitsStatistics()
        {
            try
            {
                if (_context == null || _currentUser == null) return;

                var allHabitIds = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .Select(h => h.HabitId)
                    .ToList();

                if (allHabitIds == null) allHabitIds = new List<int>();

                var allRecords = _context.HabitRecords
                    .Where(r => allHabitIds.Contains(r.HabitId))
                    .OrderBy(r => r.RecordDate)
                    .ToList();

                if (allRecords == null) allRecords = new List<HabitRecords>();

                // Обновляем быструю статистику
                UpdateQuickStats(allRecords);

                // Обновляем ключевые показатели
                UpdateAllHabitsKeyMetrics(allRecords);

                // Обновляем графики
                UpdateDailyChart(allRecords);
                UpdateWeekdayChart(allRecords);
                UpdateMonthlyChart(allRecords);
                UpdatePieChart(allRecords);
                UpdateHeatMap(allRecords);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики всех привычек: {ex.Message}");
            }
        }

        // Обновление быстрой статистики
        private void UpdateQuickStats(List<HabitRecords> records)
        {
            try
            {
                if (records == null)
                {
                    txtQuickTotalRecords.Text = "0";
                    txtQuickCompleted.Text = "0";
                    txtQuickSkipped.Text = "0";
                    txtQuickPercent.Text = "0%";
                    return;
                }

                int total = records.Count;
                int completed = records.Count(r => r != null && r.StatusId == 2);
                int skipped = records.Count(r => r != null && r.StatusId == 3);

                txtQuickTotalRecords.Text = total.ToString();
                txtQuickCompleted.Text = completed.ToString();
                txtQuickSkipped.Text = skipped.ToString();

                double percent = total > 0 ? (double)completed / total * 100 : 0;
                txtQuickPercent.Text = $"{percent:F1}%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateQuickStats: {ex.Message}");
            }
        }

        // Обновление ключевых метрик для одной привычки
        private void UpdateKeyMetrics(int habitId, List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();

                // Текущая серия
                int currentStreak = CalculateCurrentStreak(habitId);
                txtCurrentStreak.Text = currentStreak.ToString();
                txtCurrentStreakDate.Text = GetStreakDateRange(habitId, currentStreak);

                // Максимальная серия
                int maxStreak = CalculateMaxStreak(habitId);
                txtMaxStreak.Text = maxStreak.ToString();
                txtMaxStreakDate.Text = maxStreak > 0 ? "дней" : "нет рекорда";

                // Всего выполнений и процент
                int completed = records.Count(r => r != null && r.StatusId == 2);
                int skipped = records.Count(r => r != null && r.StatusId == 3);
                int total = records.Count;

                txtTotalCompleted.Text = completed.ToString();
                txtTotalCompletedPercent.Text = total > 0 ? $"{(double)completed / total * 100:F1}%" : "0%";

                txtTotalSkipped.Text = skipped.ToString();
                txtTotalSkippedPercent.Text = total > 0 ? $"{(double)skipped / total * 100:F1}%" : "0%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateKeyMetrics: {ex.Message}");
            }
        }

        // Обновление ключевых метрик для всех привычек
        private void UpdateAllHabitsKeyMetrics(List<HabitRecords> allRecords)
        {
            try
            {
                if (allRecords == null) allRecords = new List<HabitRecords>();
                if (_context == null || _currentUser == null) return;

                // Текущая серия (максимальная среди всех привычек)
                var habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .Select(h => h.HabitId)
                    .ToList();

                if (habits == null) habits = new List<int>();

                int bestCurrentStreak = 0;
                foreach (var habitId in habits)
                {
                    int streak = CalculateCurrentStreak(habitId);
                    if (streak > bestCurrentStreak)
                        bestCurrentStreak = streak;
                }

                txtCurrentStreak.Text = bestCurrentStreak.ToString();
                txtCurrentStreakDate.Text = bestCurrentStreak > 0 ? "лучшая серия" : "нет серии";

                // Максимальная серия (максимальная среди всех привычек)
                int bestMaxStreak = 0;
                foreach (var habitId in habits)
                {
                    int maxStreak = CalculateMaxStreak(habitId);
                    if (maxStreak > bestMaxStreak)
                        bestMaxStreak = maxStreak;
                }

                txtMaxStreak.Text = bestMaxStreak.ToString();
                txtMaxStreakDate.Text = bestMaxStreak > 0 ? "рекорд" : "нет рекорда";

                // Всего выполнений
                int completed = allRecords.Count(r => r != null && r.StatusId == 2);
                int skipped = allRecords.Count(r => r != null && r.StatusId == 3);
                int total = allRecords.Count;

                txtTotalCompleted.Text = completed.ToString();
                txtTotalCompletedPercent.Text = total > 0 ? $"{(double)completed / total * 100:F1}%" : "0%";

                txtTotalSkipped.Text = skipped.ToString();
                txtTotalSkippedPercent.Text = total > 0 ? $"{(double)skipped / total * 100:F1}%" : "0%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateAllHabitsKeyMetrics: {ex.Message}");
            }
        }

        // Расчет текущей серии
        private int CalculateCurrentStreak(int habitId)
        {
            try
            {
                if (_context == null) return 0;

                var records = _context.HabitRecords
                    .Where(r => r.HabitId == habitId && r.StatusId == 2)
                    .OrderByDescending(r => r.RecordDate)
                    .Select(r => r.RecordDate)
                    .ToList();

                if (!records.Any())
                    return 0;

                int streak = 0;
                var currentDate = DateTime.Today;

                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] == currentDate.AddDays(-i))
                        streak++;
                    else
                        break;
                }

                return streak;
            }
            catch
            {
                return 0;
            }
        }

        // Получение диапазона дат для серии
        private string GetStreakDateRange(int habitId, int streak)
        {
            if (streak == 0) return "нет серии";

            try
            {
                if (_context == null) return "дней";

                var records = _context.HabitRecords
                    .Where(r => r.HabitId == habitId && r.StatusId == 2)
                    .OrderByDescending(r => r.RecordDate)
                    .Take(streak)
                    .ToList();

                if (records.Any())
                {
                    var startDate = records.Last().RecordDate;
                    var endDate = records.First().RecordDate;
                    return $"{startDate:dd.MM} - {endDate:dd.MM}";
                }
            }
            catch { }

            return "дней";
        }

        // Расчет максимальной серии
        private int CalculateMaxStreak(int habitId)
        {
            try
            {
                if (_context == null) return 0;

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
            catch
            {
                return 0;
            }
        }

        // Обновление дневного графика
        private void UpdateDailyChart(List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();
                if (CompletedDailyValues == null || SkippedDailyValues == null || DailyLabels == null) return;

                CompletedDailyValues.Clear();
                SkippedDailyValues.Clear();
                DailyLabels.Clear();

                DateTime startDate, endDate;

                switch (_currentPeriod)
                {
                    case "week":
                        startDate = DateTime.Today.AddDays(-6);
                        endDate = DateTime.Today;
                        break;
                    case "month":
                        startDate = DateTime.Today.AddDays(-29);
                        endDate = DateTime.Today;
                        break;
                    case "quarter":
                        startDate = DateTime.Today.AddDays(-89);
                        endDate = DateTime.Today;
                        break;
                    case "year":
                        startDate = DateTime.Today.AddDays(-364);
                        endDate = DateTime.Today;
                        break;
                    case "all":
                        startDate = records.Any() ? records.Min(r => r.RecordDate) : DateTime.Today;
                        endDate = DateTime.Today;
                        break;
                    default:
                        startDate = DateTime.Today.AddDays(-6);
                        endDate = DateTime.Today;
                        break;
                }

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayRecords = records.Where(r => r != null && r.RecordDate == date).ToList();

                    int completed = dayRecords.Count(r => r.StatusId == 2);
                    int skipped = dayRecords.Count(r => r.StatusId == 3);

                    CompletedDailyValues.Add(completed);
                    SkippedDailyValues.Add(skipped);
                    DailyLabels.Add(date.ToString("dd.MM"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateDailyChart: {ex.Message}");
            }
        }

        // Обновление графика по дням недели
        private void UpdateWeekdayChart(List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();
                if (WeekdayValues == null) return;

                WeekdayValues.Clear();

                for (int day = 1; day <= 7; day++)
                {
                    int count = records.Count(r =>
                        r != null &&
                        (int)r.RecordDate.DayOfWeek == (day % 7) &&
                        r.StatusId == 2);

                    WeekdayValues.Add(count);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateWeekdayChart: {ex.Message}");
            }
        }

        // Обновление месячного графика
        private void UpdateMonthlyChart(List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();
                if (MonthlyPercentValues == null || MonthlyLabels == null) return;

                MonthlyPercentValues.Clear();
                MonthlyLabels.Clear();

                var groupedByMonth = records
                    .Where(r => r != null && r.StatusId == 2)
                    .GroupBy(r => new { r.RecordDate.Year, r.RecordDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .ToList();

                foreach (var month in groupedByMonth)
                {
                    int daysInMonth = DateTime.DaysInMonth(month.Key.Year, month.Key.Month);
                    int completedDays = month.Count();
                    double percent = daysInMonth > 0 ? (double)completedDays / daysInMonth * 100 : 0;

                    MonthlyPercentValues.Add(percent);
                    MonthlyLabels.Add($"{month.Key.Month:00}.{month.Key.Year}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateMonthlyChart: {ex.Message}");
            }
        }

        // Обновление круговой диаграммы
        private void UpdatePieChart(List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();
                if (CompletedPieValue == null || SkippedPieValue == null || NotSetPieValue == null) return;

                CompletedPieValue.Clear();
                SkippedPieValue.Clear();
                NotSetPieValue.Clear();

                int completed = records.Count(r => r != null && r.StatusId == 2);
                int skipped = records.Count(r => r != null && r.StatusId == 3);
                int notSet = records.Count(r => r != null && r.StatusId == 1);

                CompletedPieValue.Add(completed);
                SkippedPieValue.Add(skipped);
                NotSetPieValue.Add(notSet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdatePieChart: {ex.Message}");
            }
        }

        // Обновление тепловой карты
        private void UpdateHeatMap(List<HabitRecords> records)
        {
            try
            {
                if (records == null) records = new List<HabitRecords>();
                if (HeatMapControl == null) return;

                var heatMapData = new List<HeatMapMonth>();

                var startDate = DateTime.Today.AddDays(-364); // Последние 365 дней
                var endDate = DateTime.Today;

                var recordsByDate = records
                    .Where(r => r != null && r.RecordDate >= startDate && r.RecordDate <= endDate)
                    .GroupBy(r => r.RecordDate)
                    .ToDictionary(g => g.Key, g => g.Count(r => r.StatusId == 2));

                // Группируем по месяцам
                for (int i = 0; i < 12; i++)
                {
                    var monthDate = startDate.AddMonths(i);
                    var month = new HeatMapMonth
                    {
                        Month = GetRussianMonthName(monthDate.Month),
                        Weeks = new List<HeatMapWeek>()
                    };

                    // Создаем 5-6 недель в месяце
                    var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    for (int week = 0; week < 6; week++)
                    {
                        var weekData = new HeatMapWeek
                        {
                            Days = new List<HeatMapDay>()
                        };

                        for (int day = 0; day < 7; day++)
                        {
                            var currentDate = monthStart.AddDays(week * 7 + day);
                            if (currentDate <= monthEnd && currentDate <= endDate)
                            {
                                int count = recordsByDate != null && recordsByDate.ContainsKey(currentDate) ? recordsByDate[currentDate] : 0;
                                weekData.Days.Add(new HeatMapDay
                                {
                                    Date = currentDate,
                                    Count = count,
                                    Color = GetHeatMapColor(count),
                                    ToolTip = $"{currentDate:dd.MM.yyyy}\nВыполнено: {count} привычек"
                                });
                            }
                            else
                            {
                                weekData.Days.Add(new HeatMapDay
                                {
                                    Date = currentDate,
                                    Count = 0,
                                    Color = "#EBEDF0",
                                    ToolTip = "Нет данных"
                                });
                            }
                        }

                        month.Weeks.Add(weekData);
                    }

                    heatMapData.Add(month);
                }

                HeatMapControl.ItemsSource = heatMapData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления тепловой карты: {ex.Message}");
            }
        }

        // Получение цвета для тепловой карты
        private string GetHeatMapColor(int count)
        {
            if (count == 0) return "#EBEDF0";
            if (count <= 2) return "#9BE9A8";
            if (count <= 4) return "#40C463";
            if (count <= 6) return "#30A14E";
            return "#216E39";
        }

        // Получение русского названия месяца
        private string GetRussianMonthName(int month)
        {
            string[] months = { "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
                               "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };
            return months[month - 1];
        }

        // Обновление мотивационной фразы
        private void UpdateMotivationPhrase()
        {
            try
            {
                if (txtMotivationPhrase == null) return;

                string[] phrases = {
                    "📊 Анализируй прогресс, становись лучше!",
                    "📈 Каждая статистика показывает твой рост!",
                    "🎯 Данные не врут - ты становишься лучше!",
                    "⭐ Маленькие победы складываются в большие достижения!",
                    "🔥 Твоя активность растет с каждым днем!",
                    "💪 Статистика - зеркало твоего прогресса!"
                };

                var random = new Random();
                txtMotivationPhrase.Text = phrases[random.Next(phrases.Length)];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateMotivationPhrase: {ex.Message}");
            }
        }

        // Обработчики событий
        private void Period_Changed(object sender, RoutedEventArgs e)
        {
            if (rbWeek.IsChecked == true) _currentPeriod = "week";
            else if (rbMonth.IsChecked == true) _currentPeriod = "month";
            else if (rbQuarter.IsChecked == true) _currentPeriod = "quarter";
            else if (rbYear.IsChecked == true) _currentPeriod = "year";
            else if (rbAll.IsChecked == true) _currentPeriod = "all";

            LoadStatistics();
        }

        private void cmbHabitSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbHabitSelector.SelectedItem != null)
                {
                    rbAllHabits.IsChecked = false;
                    LoadStatistics();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка SelectionChanged: {ex.Message}");
            }
        }

        private void rbAllHabits_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbHabitSelector != null)
                    cmbHabitSelector.IsEnabled = false;
                LoadStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка rbAllHabits_Checked: {ex.Message}");
            }
        }

        private void rbAllHabits_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbHabitSelector != null)
                    cmbHabitSelector.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка rbAllHabits_Unchecked: {ex.Message}");
            }
        }

        // Экспорт статистики
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"HabitFlow_Stats_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv",
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToCsv(dialog.FileName);
                    ConfirmationDialog.ShowSuccess("Экспорт завершен",
                        "Статистика успешно экспортирована!");
                }
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка экспорта",
                    $"Ошибка при экспорте: {ex.Message}");
            }
        }

        // Экспорт в CSV
        private void ExportToCsv(string filename)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(filename))
                {
                    writer.WriteLine("Дата;Привычка;Статус");

                    var habits = _context.Habits
                        .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                        .ToList();

                    foreach (var habit in habits)
                    {
                        var records = _context.HabitRecords
                            .Where(r => r.HabitId == habit.HabitId)
                            .OrderBy(r => r.RecordDate)
                            .ToList();

                        foreach (var record in records)
                        {
                            string status = record.StatusId == 2 ? "Выполнено" :
                                           record.StatusId == 3 ? "Пропущено" : "Не отмечено";

                            writer.WriteLine($"{record.RecordDate:yyyy-MM-dd};{habit.HabitName};{status}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении CSV: {ex.Message}");
            }
        }

        // Возврат в главное окно
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = new MainWindow(_currentUser);
                WindowStateManager.OpenWindow(this, mainWindow);
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при переходе: {ex.Message}");
            }
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            WindowStateManager.SaveWindowState(this);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = new MainWindow(_currentUser);
                WindowStateManager.OpenWindow(this, mainWindow);
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при закрытии: {ex.Message}");
            }
        }
    }
}