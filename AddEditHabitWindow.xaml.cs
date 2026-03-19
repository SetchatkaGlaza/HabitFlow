using HabitFlow.Entity;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Логика взаимодействия для AddEditHabitWindow.xaml
    /// </summary>
    public partial class AddEditHabitWindow : Window
    {
        private HabitTrackerEntities _context;
        private int _userId;
        private int? _habitId;
        private string _selectedEmoji = "🏃";
        private string _selectedColor = "#4CAF50";
        private Button _lastSelectedEmojiButton;
        private Button _lastSelectedColorButton;

        public AddEditHabitWindow(int userId, int? habitId = null)
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            _userId = userId;
            _habitId = habitId;

            // Выделяем первый эмодзи по умолчанию
            _lastSelectedEmojiButton = btnEmoji1;
            btnEmoji1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            btnEmoji1.Foreground = Brushes.White;

            // Выделяем первый цвет по умолчанию
            _lastSelectedColorButton = btnColor1;
            btnColor1.BorderBrush = Brushes.Black;
            btnColor1.BorderThickness = new Thickness(2);

            if (habitId.HasValue)
            {
                txtWindowTitle.Text = "Редактирование привычки";
                LoadHabitData(habitId.Value);
            }
            else
            {
                // Устанавливаем время по умолчанию
                txtReminderTime.Text = DateTime.Now.ToString("HH:mm");
            }
        }

        // Перемещение окна
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка данных привычки для редактирования
        private void LoadHabitData(int habitId)
        {
            try
            {
                var habit = _context.Habits.Find(habitId);
                if (habit != null)
                {
                    txtHabitName.Text = habit.HabitName;
                    txtDescription.Text = habit.Description;

                    // Загружаем эмодзи если есть
                    if (!string.IsNullOrEmpty(habit.IconEmoji))
                    {
                        _selectedEmoji = habit.IconEmoji;
                        HighlightSelectedEmoji(habit.IconEmoji);
                    }

                    // Загружаем цвет если есть
                    if (!string.IsNullOrEmpty(habit.ColorCode))
                    {
                        _selectedColor = habit.ColorCode;
                        HighlightSelectedColor(habit.ColorCode);
                    }

                    // Здесь можно добавить загрузку цели и напоминания
                    // если вы добавите эти поля в таблицу Habits
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выделение выбранного эмодзи
        private void HighlightSelectedEmoji(string emoji)
        {
            // Сбрасываем стиль предыдущей кнопки
            if (_lastSelectedEmojiButton != null)
            {
                _lastSelectedEmojiButton.ClearValue(Button.BackgroundProperty);
                _lastSelectedEmojiButton.ClearValue(Button.ForegroundProperty);
            }

            // Находим и выделяем новую кнопку
            var buttons = new[] { btnEmoji1, btnEmoji2, btnEmoji3, btnEmoji4, btnEmoji5,
                                   btnEmoji6, btnEmoji7, btnEmoji8, btnEmoji9, btnEmoji10 };

            foreach (var btn in buttons)
            {
                if (btn.Tag?.ToString() == emoji)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    btn.Foreground = Brushes.White;
                    _lastSelectedEmojiButton = btn;
                    break;
                }
            }
        }

        // Выделение выбранного цвета
        private void HighlightSelectedColor(string color)
        {
            // Сбрасываем стиль предыдущей кнопки
            if (_lastSelectedColorButton != null)
            {
                _lastSelectedColorButton.BorderThickness = new Thickness(0);
            }

            // Находим и выделяем новую кнопку
            var buttons = new[] { btnColor1, btnColor2, btnColor3, btnColor4, btnColor5 };

            foreach (var btn in buttons)
            {
                if (btn.Tag?.ToString() == color)
                {
                    btn.BorderBrush = Brushes.Black;
                    btn.BorderThickness = new Thickness(2);
                    _lastSelectedColorButton = btn;
                    _selectedColor = color;
                    break;
                }
            }
        }

        // Обработчик выбора эмодзи
        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                _selectedEmoji = button.Tag.ToString();

                // Сбрасываем стиль предыдущей кнопки
                if (_lastSelectedEmojiButton != null)
                {
                    _lastSelectedEmojiButton.ClearValue(Button.BackgroundProperty);
                    _lastSelectedEmojiButton.ClearValue(Button.ForegroundProperty);
                }

                // Выделяем текущую кнопку
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                button.Foreground = Brushes.White;
                _lastSelectedEmojiButton = button;
            }
        }

        // Обработчик выбора цвета
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                // Сбрасываем стиль предыдущей кнопки
                if (_lastSelectedColorButton != null)
                {
                    _lastSelectedColorButton.BorderThickness = new Thickness(0);
                }

                // Выделяем текущую кнопку
                button.BorderBrush = Brushes.Black;
                button.BorderThickness = new Thickness(2);
                _lastSelectedColorButton = button;
                _selectedColor = button.Tag.ToString();
            }
        }

        // Валидация формы
        private void txtHabitName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(txtHabitName.Text);
        }

        // Сохранение
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация времени напоминания если чекбокс отмечен
                if (chkReminder.IsChecked == true)
                {
                    if (!IsValidTime(txtReminderTime.Text))
                    {
                        MessageBox.Show("Введите корректное время в формате ЧЧ:ММ (например, 09:00)",
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (_habitId.HasValue)
                {
                    // Редактирование существующей привычки
                    var habit = _context.Habits.Find(_habitId.Value);
                    if (habit != null)
                    {
                        habit.HabitName = txtHabitName.Text.Trim();
                        habit.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
                        habit.IconEmoji = _selectedEmoji;
                        habit.ColorCode = _selectedColor;

                        _context.SaveChanges();
                    }
                }
                else
                {
                    // Создание новой привычки
                    var newHabit = new Habits
                    {
                        UserId = _userId,
                        HabitName = txtHabitName.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                        CreatedAt = DateTime.Today,
                        IsActive = true,
                        IconEmoji = _selectedEmoji,
                        ColorCode = _selectedColor
                    };

                    _context.Habits.Add(newHabit);
                    _context.SaveChanges();

                    // Если установлено напоминание, можно добавить логику для создания напоминания
                    if (chkReminder.IsChecked == true)
                    {
                        // Здесь будет код для создания напоминания
                        // Например, сохранить в отдельную таблицу Reminders
                        string reminderTime = txtReminderTime.Text;
                        // TODO: Сохранить напоминание
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверка формата времени
        private bool IsValidTime(string time)
        {
            TimeSpan result;
            return TimeSpan.TryParse(time, out result);
        }

        // Отмена
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Закрытие
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}