using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Модель данных для строки календаря
    /// </summary>
    

    /// <summary>
    /// Логика взаимодействия для DatePickerWindow.xaml
    /// </summary>
    public partial class DatePickerWindow : Window
    {
        // Результат
        public DateTime? SelectedDate { get; private set; }

        // Параметры
        private DateTime _currentDate;
        private DateTime? _initialDate;
        private DateTime? _minDate;
        private DateTime? _maxDate;
        private bool _allowFutureDates;
        private bool _allowPastDates;
        private ObservableCollection<CalendarRow> _calendarRows;

        public DatePickerWindow(
            string title = "Выберите дату",
            DateTime? initialDate = null,
            DateTime? minDate = null,
            DateTime? maxDate = null,
            bool allowFutureDates = true,
            bool allowPastDates = true)
        {
            InitializeComponent();

            txtTitle.Text = title;
            _initialDate = initialDate ?? DateTime.Today;
            _currentDate = _initialDate.Value;
            _minDate = minDate;
            _maxDate = maxDate;
            _allowFutureDates = allowFutureDates;
            _allowPastDates = allowPastDates;

            _calendarRows = new ObservableCollection<CalendarRow>();
            CalendarGrid.ItemsSource = _calendarRows;

            UpdateSelectedDateDisplay(_initialDate.Value);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCalendar();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Обновление календаря
        private void UpdateCalendar()
        {
            _calendarRows.Clear();
            txtCurrentMonth.Text = GetRussianMonthName(_currentDate.Month) + " " + _currentDate.Year;

            var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            // Определяем первый день недели (0 = воскресенье, 1 = понедельник, ...)
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            // Корректируем для понедельника как первого дня
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
                        var currentDate = new DateTime(_currentDate.Year, _currentDate.Month, dayNumber);
                        bool isSelectable = IsDateSelectable(currentDate);
                        bool isSelected = _initialDate.HasValue && currentDate.Date == _initialDate.Value.Date;

                        // Устанавливаем значение дня
                        SetRowProperty(row, $"Day{i + 1}", dayNumber.ToString());
                        SetRowProperty(row, $"Date{i + 1}", currentDate);

                        // Настройки цвета и стиля
                        string bgColor = isSelected ? "#4CAF50" : (isSelectable ? "White" : "#F0F0F0");
                        string textColor = isSelected ? "White" : (isSelectable ? "#333" : "#999");
                        string borderColor = isSelected ? "#4CAF50" : "#E0E0E0";
                        var borderThickness = isSelected ? new Thickness(2) : new Thickness(1);
                        var fontWeight = isSelected ? FontWeights.Bold :
                                        (currentDate.Date == DateTime.Today ? FontWeights.Bold : FontWeights.Normal);

                        SetRowProperty(row, $"Color{i + 1}", bgColor);
                        SetRowProperty(row, $"TextColor{i + 1}", textColor);
                        SetRowProperty(row, $"BorderColor{i + 1}", borderColor);
                        SetRowProperty(row, $"BorderThickness{i + 1}", borderThickness);
                        SetRowProperty(row, $"FontWeight{i + 1}", fontWeight);

                        currentDay++;
                    }
                    else
                    {
                        // Пустая ячейка
                        SetRowProperty(row, $"Day{i + 1}", "");
                        SetRowProperty(row, $"Color{i + 1}", "#F8F9FA");
                        SetRowProperty(row, $"TextColor{i + 1}", "#999");
                        SetRowProperty(row, $"BorderColor{i + 1}", "#F0F0F0");
                        SetRowProperty(row, $"BorderThickness{i + 1}", new Thickness(0));
                        SetRowProperty(row, $"FontWeight{i + 1}", FontWeights.Normal);
                        SetRowProperty(row, $"Date{i + 1}", null);
                    }
                }

                _calendarRows.Add(row);
                rowCount++;
            }
        }

        // Установка свойства через рефлексию
        private void SetRowProperty(CalendarRow row, string propertyName, object value)
        {
            var property = typeof(CalendarRow).GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(row, value);
            }
        }

        // Проверка доступности даты для выбора
        private bool IsDateSelectable(DateTime date)
        {
            if (!_allowFutureDates && date > DateTime.Today)
                return false;

            if (!_allowPastDates && date < DateTime.Today)
                return false;

            if (_minDate.HasValue && date < _minDate.Value)
                return false;

            if (_maxDate.HasValue && date > _maxDate.Value)
                return false;

            return true;
        }

        // Получение русского названия месяца
        private string GetRussianMonthName(int month)
        {
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                               "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            return months[month - 1];
        }

        // Обновление отображения выбранной даты
        private void UpdateSelectedDateDisplay(DateTime date)
        {
            txtSelectedDate.Text = date.ToString("dd.MM.yyyy");
        }

        // Обработчики навигации по месяцам
        private void btnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            UpdateCalendar();
        }

        private void btnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            UpdateCalendar();
        }

        // Обработчик выбора дня
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is DateTime date)
            {
                if (IsDateSelectable(date))
                {
                    _initialDate = date;
                    UpdateSelectedDateDisplay(date);
                    UpdateCalendar(); // Обновляем для подсветки выбранной даты
                }
            }
        }

        // Быстрые действия
        private void btnToday_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = DateTime.Today;
            _initialDate = DateTime.Today;
            UpdateSelectedDateDisplay(DateTime.Today);
            UpdateCalendar();
        }

        private void btnYesterday_Click(object sender, RoutedEventArgs e)
        {
            var yesterday = DateTime.Today.AddDays(-1);
            if (IsDateSelectable(yesterday))
            {
                _currentDate = yesterday;
                _initialDate = yesterday;
                UpdateSelectedDateDisplay(yesterday);
                UpdateCalendar();
            }
        }

        private void btnTomorrow_Click(object sender, RoutedEventArgs e)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            if (IsDateSelectable(tomorrow))
            {
                _currentDate = tomorrow;
                _initialDate = tomorrow;
                UpdateSelectedDateDisplay(tomorrow);
                UpdateCalendar();
            }
        }

        // Подтверждение выбора
        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (_initialDate.HasValue)
            {
                SelectedDate = _initialDate.Value;
                DialogResult = true;
                Close();
            }
        }

        // Отмена
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Статические методы для удобного использования

        /// <summary>
        /// Показать окно выбора даты и получить результат
        /// </summary>
        public static DateTime? PickDate(
            Window owner,
            string title = "Выберите дату",
            DateTime? initialDate = null,
            DateTime? minDate = null,
            DateTime? maxDate = null,
            bool allowFutureDates = true,
            bool allowPastDates = true)
        {
            var dialog = new DatePickerWindow(title, initialDate, minDate, maxDate, allowFutureDates, allowPastDates)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true ? dialog.SelectedDate : null;
        }

        /// <summary>
        /// Показать окно выбора даты с ограничением только прошлыми датами
        /// </summary>
        public static DateTime? PickPastDate(Window owner, string title = "Выберите дату", DateTime? initialDate = null)
        {
            return PickDate(owner, title, initialDate, null, DateTime.Today, false, true);
        }

        /// <summary>
        /// Показать окно выбора даты с ограничением только будущими датами
        /// </summary>
        public static DateTime? PickFutureDate(Window owner, string title = "Выберите дату", DateTime? initialDate = null)
        {
            return PickDate(owner, title, initialDate, DateTime.Today, null, true, false);
        }

        /// <summary>
        /// Показать окно выбора диапазона дат (упрощенно)
        /// </summary>
        public static bool PickDateRange(Window owner, out DateTime? startDate, out DateTime? endDate)
        {
            startDate = null;
            endDate = null;

            // Здесь можно реализовать выбор диапазона дат
            // Для простоты пока возвращаем одну дату
            var date = PickDate(owner, "Выберите начальную дату");
            if (date.HasValue)
            {
                startDate = date;
                endDate = PickDate(owner, "Выберите конечную дату", startDate);
                return endDate.HasValue;
            }

            return false;
        }
    }
}