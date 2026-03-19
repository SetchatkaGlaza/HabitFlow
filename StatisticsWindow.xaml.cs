using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HabitFlow.Entity;
using LiveCharts;
using LiveCharts.Configurations;
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
            _context = new HabitTrackerEntities();
            _currentUser = user;

            InitializeCharts();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHabitsForSelector();
            LoadStatistics();
            UpdateMotivationPhrase();
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

        // Загрузка статистики
        private void LoadStatistics()
        {
            if (rbAllHabits.IsChecked == true)
            {
                _selectedHabitId = null;
                LoadAllHabitsStatistics();
            }
            else if (cmbHabitSelector.SelectedItem != null)
            {
                var selectedHabit = cmbHabitSelector.SelectedItem as Habits;
                _selectedHabitId = selectedHabit?.HabitId;
                LoadHabitStatistics(_selectedHabitId.Value);
            }
        }

        // Загрузка статистики по конкретной привычке
        private void LoadHabitStatistics(int habitId)
        {
            var habit = _context.Habits.Find(habitId);
            if (habit == null) return;

            // Получаем все записи привычки
            var records = _context.HabitRecords
                .Where(r => r.HabitId == habitId)
                .OrderBy(r => r.RecordDate)
                .ToList();

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

        // Загрузка статистики по всем привычкам
        private void LoadAllHabitsStatistics()
        {
            var allHabitIds = _context.Habits
                .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                .Select(h => h.HabitId)
                .ToList();

            var allRecords = _context.HabitRecords
                .Where(r => allHabitIds.Contains(r.HabitId))
                .OrderBy(r => r.RecordDate)
                .ToList();

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

        // Обновление быстрой статистики
        private void UpdateQuickStats(List<HabitRecords> records)
        {
            int total = records.Count;
            int completed = records.Count(r => r.StatusId == 2);
            int skipped = records.Count(r => r.StatusId == 3);
            int notSet = records.Count(r => r.StatusId == 1);

            txtQuickTotalRecords.Text = total.ToString();
            txtQuickCompleted.Text = completed.ToString();
            txtQuickSkipped.Text = skipped.ToString();

            double percent = total > 0 ? (double)completed / total * 100 : 0;
            txtQuickPercent.Text = $"{percent:F1}%";
        }

        // Обновление ключевых метрик для одной привычки
        private void UpdateKeyMetrics(int habitId, List<HabitRecords> records)
        {
            // Текущая серия
            int currentStreak = CalculateCurrentStreak(habitId);
            txtCurrentStreak.Text = currentStreak.ToString();
            txtCurrentStreakDate.Text = GetStreakDateRange(habitId, currentStreak);

            // Максимальная серия
            int maxStreak = CalculateMaxStreak(habitId);
            txtMaxStreak.Text = maxStreak.ToString();
            txtMaxStreakDate.Text = "дней";

            // Всего выполнений и процент
            int completed = records.Count(r => r.StatusId == 2);
            int skipped = records.Count(r => r.StatusId == 3);
            int total = records.Count;

            txtTotalCompleted.Text = completed.ToString();
            txtTotalCompletedPercent.Text = total > 0 ? $"{(double)completed / total * 100:F1}%" : "0%";

            txtTotalSkipped.Text = skipped.ToString();
            txtTotalSkippedPercent.Text = total > 0 ? $"{(double)skipped / total * 100:F1}%" : "0%";
        }

        // Обновление ключевых метрик для всех привычек
        private void UpdateAllHabitsKeyMetrics(List<HabitRecords> allRecords)
        {
            // Текущая серия (максимальная среди всех привычек)
            var habits = _context.Habits
                .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                .Select(h => h.HabitId)
                .ToList();

            int bestCurrentStreak = 0;
            foreach (var habitId in habits)
            {
                int streak = CalculateCurrentStreak(habitId);
                if (streak > bestCurrentStreak)
                    bestCurrentStreak = streak;
            }

            txtCurrentStreak.Text = bestCurrentStreak.ToString();
            txtCurrentStreakDate.Text = "лучшая серия";

            // Максимальная серия (максимальная среди всех привычек)
            int bestMaxStreak = 0;
            foreach (var habitId in habits)
            {
                int maxStreak = CalculateMaxStreak(habitId);
                if (maxStreak > bestMaxStreak)
                    bestMaxStreak = maxStreak;
            }

            txtMaxStreak.Text = bestMaxStreak.ToString();
            txtMaxStreakDate.Text = "рекорд";

            // Всего выполнений
            int completed = allRecords.Count(r => r.StatusId == 2);
            int skipped = allRecords.Count(r => r.StatusId == 3);
            int total = allRecords.Count;

            txtTotalCompleted.Text = completed.ToString();
            txtTotalCompletedPercent.Text = total > 0 ? $"{(double)completed / total * 100:F1}%" : "0%";

            txtTotalSkipped.Text = skipped.ToString();
            txtTotalSkippedPercent.Text = total > 0 ? $"{(double)skipped / total * 100:F1}%" : "0%";
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

        // Получение диапазона дат для серии
        private string GetStreakDateRange(int habitId, int streak)
        {
            if (streak == 0) return "нет серии";

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

            return "дней";
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

        // Обновление дневного графика
        private void UpdateDailyChart(List<HabitRecords> records)
        {
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
                var dayRecords = records.Where(r => r.RecordDate == date).ToList();

                int completed = dayRecords.Count(r => r.StatusId == 2);
                int skipped = dayRecords.Count(r => r.StatusId == 3);

                CompletedDailyValues.Add(completed);
                SkippedDailyValues.Add(skipped);
                DailyLabels.Add(date.ToString("dd.MM"));
            }
        }

        // Обновление графика по дням недели
        private void UpdateWeekdayChart(List<HabitRecords> records)
        {
            WeekdayValues.Clear();

            for (int day = 1; day <= 7; day++)
            {
                int count = records.Count(r =>
                    (int)r.RecordDate.DayOfWeek == (day % 7) &&
                    r.StatusId == 2);

                WeekdayValues.Add(count);
            }
        }

        // Обновление месячного графика
        private void UpdateMonthlyChart(List<HabitRecords> records)
        {
            MonthlyPercentValues.Clear();
            MonthlyLabels.Clear();

            var groupedByMonth = records
                .Where(r => r.StatusId == 2)
                .GroupBy(r => new { r.RecordDate.Year, r.RecordDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ToList();

            foreach (var month in groupedByMonth)
            {
                int daysInMonth = DateTime.DaysInMonth(month.Key.Year, month.Key.Month);
                int completedDays = month.Count();
                double percent = (double)completedDays / daysInMonth * 100;

                MonthlyPercentValues.Add(percent);
                MonthlyLabels.Add($"{month.Key.Month:00}.{month.Key.Year}");
            }
        }

        // Обновление круговой диаграммы
        private void UpdatePieChart(List<HabitRecords> records)
        {
            CompletedPieValue.Clear();
            SkippedPieValue.Clear();
            NotSetPieValue.Clear();

            int completed = records.Count(r => r.StatusId == 2);
            int skipped = records.Count(r => r.StatusId == 3);
            int notSet = records.Count(r => r.StatusId == 1);

            CompletedPieValue.Add(completed);
            SkippedPieValue.Add(skipped);
            NotSetPieValue.Add(notSet);
        }

        // Обновление тепловой карты
        private void UpdateHeatMap(List<HabitRecords> records)
        {
            var heatMapData = new List<HeatMapMonth>();

            var startDate = DateTime.Today.AddDays(-364); // Последние 365 дней
            var endDate = DateTime.Today;

            var recordsByDate = records
                .Where(r => r.RecordDate >= startDate && r.RecordDate <= endDate)
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
                            int count = recordsByDate.ContainsKey(currentDate) ? recordsByDate[currentDate] : 0;
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
            if (cmbHabitSelector.SelectedItem != null)
            {
                rbAllHabits.IsChecked = false;
                LoadStatistics();
            }
        }

        private void rbAllHabits_Checked(object sender, RoutedEventArgs e)
        {
            cmbHabitSelector.IsEnabled = false;
            LoadStatistics();
        }

        private void rbAllHabits_Unchecked(object sender, RoutedEventArgs e)
        {
            cmbHabitSelector.IsEnabled = true;
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
                    MessageBox.Show("Статистика успешно экспортирована!", "Экспорт",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Экспорт в CSV
        private void ExportToCsv(string filename)
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

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}