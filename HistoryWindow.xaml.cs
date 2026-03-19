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
    /// Модель данных для ячейки календаря
    /// </summary>
    public class CalendarRow
    {
        public string Day1 { get; set; }
        public string Day2 { get; set; }
        public string Day3 { get; set; }
        public string Day4 { get; set; }
        public string Day5 { get; set; }
        public string Day6 { get; set; }
        public string Day7 { get; set; }

        public string Color1 { get; set; }
        public string Color2 { get; set; }
        public string Color3 { get; set; }
        public string Color4 { get; set; }
        public string Color5 { get; set; }
        public string Color6 { get; set; }
        public string Color7 { get; set; }

        public string TextColor1 { get; set; }
        public string TextColor2 { get; set; }
        public string TextColor3 { get; set; }
        public string TextColor4 { get; set; }
        public string TextColor5 { get; set; }
        public string TextColor6 { get; set; }
        public string TextColor7 { get; set; }

        public FontWeight FontWeight1 { get; set; }
        public FontWeight FontWeight2 { get; set; }
        public FontWeight FontWeight3 { get; set; }
        public FontWeight FontWeight4 { get; set; }
        public FontWeight FontWeight5 { get; set; }
        public FontWeight FontWeight6 { get; set; }
        public FontWeight FontWeight7 { get; set; }

        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public DateTime? Date3 { get; set; }
        public DateTime? Date4 { get; set; }
        public DateTime? Date5 { get; set; }
        public DateTime? Date6 { get; set; }
        public DateTime? Date7 { get; set; }
    }

    /// <summary>
    /// Модель данных для привычки в списке дня
    /// </summary>
    public class DayHabitItem
    {
        public int HabitId { get; set; }
        public string Icon { get; set; }
        public string HabitName { get; set; }
        public string Description { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public string TextColor { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;
        private Dictionary<DateTime, Dictionary<int, int>> _monthData; // Date -> (HabitId -> StatusId)

        public HistoryWindow(Users user)
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _context = new HabitTrackerEntities();
            _currentUser = user;
            _currentMonth = DateTime.Today;
            _selectedDate = DateTime.Today;
            _monthData = new Dictionary<DateTime, Dictionary<int, int>>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadHabitsFilter();
                LoadMonthData();
                UpdateCalendar();
                UpdateSelectedDateDetails();
                UpdateMonthStats();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке истории: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка фильтра привычек
        private void LoadHabitsFilter()
        {
            try
            {
                var habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .OrderBy(h => h.HabitName)
                    .ToList();

                cmbHistoryHabitFilter.Items.Clear();

                // Добавляем пункт "Все привычки"
                var allItem = new ComboBoxItem
                {
                    Content = "Все привычки",
                    Tag = 0
                };
                cmbHistoryHabitFilter.Items.Add(allItem);

                foreach (var habit in habits)
                {
                    var item = new ComboBoxItem
                    {
                        Content = habit.HabitName,
                        Tag = habit.HabitId
                    };
                    cmbHistoryHabitFilter.Items.Add(item);
                }

                cmbHistoryHabitFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фильтра: {ex.Message}");
            }
        }

        // Загрузка данных за месяц
        private void LoadMonthData()
        {
            _monthData.Clear();

            var startDate = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var records = _context.HabitRecords
                .Where(r => r.Habits.UserId == _currentUser.UserId
                    && r.RecordDate >= startDate
                    && r.RecordDate <= endDate)
                .ToList();

            foreach (var record in records)
            {
                if (!_monthData.ContainsKey(record.RecordDate))
                {
                    _monthData[record.RecordDate] = new Dictionary<int, int>();
                }

                if (!_monthData[record.RecordDate].ContainsKey(record.HabitId))
                {
                    _monthData[record.RecordDate][record.HabitId] = record.StatusId;
                }
            }

            txtCurrentMonth.Text = GetRussianMonthName(_currentMonth.Month) + " " + _currentMonth.Year;
        }

        // Обновление календаря
        private void UpdateCalendar()
        {
            var calendarRows = new ObservableCollection<CalendarRow>();

            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            // Корректировка для понедельника как первого дня
            firstDayOfWeek = firstDayOfWeek == 0 ? 6 : firstDayOfWeek - 1;

            int currentDay = 1;
            int rowCount = 0;

            while (currentDay <= daysInMonth)
            {
                var row = new CalendarRow();

                for (int i = 0; i < 7; i++)
                {
                    int dayNumber = rowCount == 0 && i < firstDayOfWeek ? 0 : currentDay;

                    if (dayNumber > 0 && dayNumber <= daysInMonth)
                    {
                        var currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, dayNumber);

                        // Устанавливаем значение дня
                        SetRowProperty(row, $"Day{i + 1}", dayNumber.ToString());
                        SetRowProperty(row, $"Date{i + 1}", currentDate);

                        // Определяем цвет ячейки
                        string color = GetDayColor(currentDate);
                        SetRowProperty(row, $"Color{i + 1}", color);

                        // Цвет текста
                        string textColor = color == "#4CAF50" || color == "#E74C3C" ? "White" : "#333";
                        SetRowProperty(row, $"TextColor{i + 1}", textColor);

                        // Жирный шрифт для сегодняшнего дня
                        bool isToday = currentDate.Date == DateTime.Today;
                        SetRowProperty(row, $"FontWeight{i + 1}", isToday ? FontWeights.Bold : FontWeights.Normal);

                        currentDay++;
                    }
                    else
                    {
                        // Пустая ячейка
                        SetRowProperty(row, $"Day{i + 1}", "");
                        SetRowProperty(row, $"Color{i + 1}", "#F8F9FA");
                        SetRowProperty(row, $"TextColor{i + 1}", "#999");
                        SetRowProperty(row, $"FontWeight{i + 1}", FontWeights.Normal);
                        SetRowProperty(row, $"Date{i + 1}", null);
                    }
                }

                calendarRows.Add(row);
                rowCount++;
            }

            CalendarGrid.ItemsSource = calendarRows;
        }

        // Вспомогательный метод для установки свойств через рефлексию
        private void SetRowProperty(CalendarRow row, string propertyName, object value)
        {
            var property = typeof(CalendarRow).GetProperty(propertyName);
            property?.SetValue(row, value);
        }

        // Получение цвета для дня
        private string GetDayColor(DateTime date)
        {
            if (!_monthData.ContainsKey(date))
                return "#F8F9FA";

            var dayData = _monthData[date];
            int totalHabits = _context.Habits.Count(h => h.UserId == _currentUser.UserId && h.IsActive == true);

            if (totalHabits == 0) return "#F8F9FA";

            int completed = dayData.Count(d => d.Value == 2);
            int skipped = dayData.Count(d => d.Value == 3);

            if (completed == totalHabits)
                return "#4CAF50"; // Все выполнено
            else if (completed > 0)
                return "#F39C12"; // Частично выполнено
            else if (skipped > 0)
                return "#E74C3C"; // Все пропущено
            else
                return "#F8F9FA"; // Нет отметок
        }

        // Обновление деталей выбранного дня
        private void UpdateSelectedDateDetails()
        {
            if (!_selectedDate.HasValue) return;

            txtSelectedDate.Text = _selectedDate.Value.ToString("dd MMMM yyyy",
                new System.Globalization.CultureInfo("ru-RU"));

            string[] weekDays = { "Воскресенье", "Понедельник", "Вторник", "Среда",
                                  "Четверг", "Пятница", "Суббота" };
            txtSelectedWeekDay.Text = weekDays[(int)_selectedDate.Value.DayOfWeek];

            LoadDayHabits();
        }

        // Загрузка привычек за выбранный день
        private void LoadDayHabits()
        {
            if (!_selectedDate.HasValue) return;

            var dayHabits = new ObservableCollection<DayHabitItem>();

            var habits = _context.Habits
                .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                .OrderBy(h => h.HabitName)
                .ToList();

            foreach (var habit in habits)
            {
                // Проверяем фильтр
                if (cmbHistoryHabitFilter.SelectedItem is ComboBoxItem filterItem
                    && filterItem.Tag != null
                    && (int)filterItem.Tag != 0
                    && (int)filterItem.Tag != habit.HabitId)
                {
                    continue;
                }

                int statusId = 1; // По умолчанию не отмечено

                if (_monthData.ContainsKey(_selectedDate.Value)
                    && _monthData[_selectedDate.Value].ContainsKey(habit.HabitId))
                {
                    statusId = _monthData[_selectedDate.Value][habit.HabitId];
                }

                var item = new DayHabitItem
                {
                    HabitId = habit.HabitId,
                    Icon = habit.IconEmoji ?? "📌",
                    HabitName = habit.HabitName,
                    Description = habit.Description ?? "Нет описания",
                    StatusText = GetStatusText(statusId),
                    StatusColor = GetStatusColor(statusId),
                    BackgroundColor = GetItemBackgroundColor(statusId),
                    BorderColor = GetItemBorderColor(statusId),
                    TextColor = statusId == 2 || statusId == 3 ? "White" : "#333"
                };

                dayHabits.Add(item);
            }

            lstDayHabits.ItemsSource = dayHabits;
        }

        // Получение текста статуса
        private string GetStatusText(int statusId)
        {
            switch (statusId)
            {
                case 1: return "Не отмечено";
                case 2: return "Выполнено";
                case 3: return "Пропущено";
                default: return "Неизвестно";
            }
        }

        // Получение цвета статуса
        private string GetStatusColor(int statusId)
        {
            switch (statusId)
            {
                case 1: return "#999999";
                case 2: return "#4CAF50";
                case 3: return "#F39C12";
                default: return "#999999";
            }
        }

        // Получение фона элемента
        private string GetItemBackgroundColor(int statusId)
        {
            switch (statusId)
            {
                case 2: return "#4CAF5020"; // Зеленый с прозрачностью
                case 3: return "#F39C1220"; // Оранжевый с прозрачностью
                default: return "#F0F0F0";
            }
        }

        // Получение цвета границы элемента
        private string GetItemBorderColor(int statusId)
        {
            switch (statusId)
            {
                case 2: return "#4CAF50";
                case 3: return "#F39C12";
                default: return "#E0E0E0";
            }
        }

        // Обновление статистики месяца
        private void UpdateMonthStats()
        {
            int totalRecords = 0;
            int completed = 0;
            int skipped = 0;

            foreach (var day in _monthData.Values)
            {
                foreach (var record in day.Values)
                {
                    totalRecords++;
                    if (record == 2) completed++;
                    else if (record == 3) skipped++;
                }
            }

            txtMonthStats.Text = $"{completed} выполнено / {skipped} пропущено / {totalRecords} всего";
        }

        // Получение русского названия месяца
        private string GetRussianMonthName(int month)
        {
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                               "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            return months[month - 1];
        }

        // Обработчик клика по дню календаря
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is DateTime date)
            {
                _selectedDate = date;
                UpdateSelectedDateDetails();
            }
        }

        // Навигация по месяцам
        private void btnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            LoadMonthData();
            UpdateCalendar();
            UpdateMonthStats();
        }

        private void btnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            LoadMonthData();
            UpdateCalendar();
            UpdateMonthStats();
        }

        private void btnToday_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = DateTime.Today;
            _selectedDate = DateTime.Today;
            LoadMonthData();
            UpdateCalendar();
            UpdateSelectedDateDetails();
            UpdateMonthStats();
        }

        // Изменение фильтра
        private void cmbHistoryHabitFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDayHabits();
        }

        // Редактирование выбранного дня
        private void btnEditDay_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedDate.HasValue) return;

            ConfirmationDialog.ShowInfo("Редактирование дня",
                $"Функция редактирования дня {_selectedDate.Value:dd.MM.yyyy} будет доступна в следующей версии.\n\nСейчас вы можете вернуться на главный экран и отметить этот день там.");
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