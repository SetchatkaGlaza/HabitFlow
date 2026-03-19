using HabitFlow.Entity;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    public partial class AddEditHabitWindow : Window
    {
        private HabitTrackerEntities _context;
        private int _userId;
        private int? _habitId;
        private Habits _habit;

        private string _selectedEmoji = "🏃";
        private string _selectedColor = "#4CAF50";
        private bool _hasUnsavedChanges = false;

        private Button _lastSelectedEmojiButton;
        private Button _lastSelectedColorButton;

        public AddEditHabitWindow(int userId, int? habitId = null)
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            _userId = userId;
            _habitId = habitId;

            // Выделяем первый эмодзи
            _lastSelectedEmojiButton = btnEmoji1;
            btnEmoji1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            btnEmoji1.Foreground = Brushes.White;

            // Выделяем первый цвет
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
                txtReminderTime.Text = DateTime.Now.ToString("HH:mm");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtHabitName.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = ConfirmationDialog.ShowWarning(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Продолжить"
                );

                if (!result) // Если пользователь нажал "Отмена" (result == false)
                {
                    e.Cancel = true; // Отменяем закрытие
                }
                // Если result == true (нажал "Продолжить"), окно закроется
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void LoadHabitData(int habitId)
        {
            try
            {
                _habit = _context.Habits.Find(habitId);
                if (_habit == null) return;

                txtHabitName.Text = _habit.HabitName;
                txtDescription.Text = _habit.Description;

                _selectedEmoji = _habit.IconEmoji ?? "🏃";
                HighlightSelectedEmoji(_selectedEmoji);

                _selectedColor = _habit.ColorCode ?? "#4CAF50";
                HighlightSelectedColor(_selectedColor);

                _hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void HighlightSelectedEmoji(string emoji)
        {
            if (_lastSelectedEmojiButton != null)
            {
                _lastSelectedEmojiButton.ClearValue(Button.BackgroundProperty);
                _lastSelectedEmojiButton.ClearValue(Button.ForegroundProperty);
                _lastSelectedEmojiButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                _lastSelectedEmojiButton.BorderThickness = new Thickness(1);
            }

            var buttons = new[] { btnEmoji1, btnEmoji2, btnEmoji3, btnEmoji4, btnEmoji5,
                                   btnEmoji6, btnEmoji7, btnEmoji8, btnEmoji9, btnEmoji10 };

            foreach (var btn in buttons)
            {
                if (btn.Tag?.ToString() == emoji)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    btn.Foreground = Brushes.White;
                    btn.BorderBrush = Brushes.Transparent;
                    btn.BorderThickness = new Thickness(0);
                    _lastSelectedEmojiButton = btn;
                    break;
                }
            }
        }

        private void HighlightSelectedColor(string color)
        {
            if (_lastSelectedColorButton != null)
            {
                _lastSelectedColorButton.BorderThickness = new Thickness(0);
            }

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

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                _selectedEmoji = button.Tag.ToString();
                HighlightSelectedEmoji(_selectedEmoji);
                _hasUnsavedChanges = true;
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                _selectedColor = button.Tag.ToString();
                HighlightSelectedColor(_selectedColor);
                _hasUnsavedChanges = true;
            }
        }

        private void txtHabitName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(txtHabitName.Text);
            _hasUnsavedChanges = true;
        }

        private void txtDescription_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _hasUnsavedChanges = true;
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            _hasUnsavedChanges = true;
        }

        private bool IsValidTime(string time)
        {
            return TimeSpan.TryParse(time, out _);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkReminder.IsChecked == true && !IsValidTime(txtReminderTime.Text))
                {
                    ConfirmationDialog.ShowError("Ошибка", "Введите корректное время в формате ЧЧ:ММ");
                    return;
                }

                if (_habitId.HasValue)
                {
                    if (_habit == null)
                        _habit = _context.Habits.Find(_habitId.Value);

                    if (_habit != null)
                    {
                        _habit.HabitName = txtHabitName.Text.Trim();
                        _habit.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
                        _habit.IconEmoji = _selectedEmoji;
                        _habit.ColorCode = _selectedColor;
                        _context.SaveChanges();
                    }
                }
                else
                {
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
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка", $"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = ConfirmationDialog.ShowWarning(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Продолжить"
                );

                if (result) // Если пользователь нажал "Продолжить"
                {
                    DialogResult = false;
                    Close();
                }
                // Если result == false (нажал "Отмена"), ничего не делаем - окно остается открытым
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = ConfirmationDialog.ShowWarning(
                    "Несохраненные изменения",
                    "У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Продолжить"
                );

                if (result) // Если пользователь нажал "Продолжить"
                {
                    DialogResult = false;
                    Close();
                }
                // Если result == false (нажал "Отмена"), ничего не делаем
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }
    }
}