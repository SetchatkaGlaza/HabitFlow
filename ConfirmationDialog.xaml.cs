using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HabitFlow
{
    /// <summary>
    /// Типы диалогов подтверждения
    /// </summary>
    public enum ConfirmationType
    {
        Question,      // Вопрос (да/нет)
        Warning,       // Предупреждение
        Error,         // Ошибка
        Info,          // Информация
        Success,       // Успех
        Delete,        // Удаление (особое предупреждение)
        Input,         // Ввод текста
        Password       // Ввод пароля
    }

    /// <summary>
    /// Результат диалога
    /// </summary>
    public enum ConfirmationResult
    {
        None,
        Confirmed,
        Cancelled,
        InputConfirmed  // Подтверждено с вводом текста
    }

    /// <summary>
    /// Логика взаимодействия для ConfirmationDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog : Window
    {
        // Результат
        public ConfirmationResult Result { get; private set; } = ConfirmationResult.None;
        public string InputValue { get; private set; } = "";
        public bool DontAskAgain { get; private set; } = false;

        // Параметры
        private ConfirmationType _type;
        private string _confirmText;
        private string _cancelText;
        private string _expectedInput;
        private bool _isInputCaseSensitive;
        private Action _confirmAction;
        private Action _cancelAction;

        public ConfirmationDialog(string title, string message,
                                  ConfirmationType type = ConfirmationType.Question,
                                  string confirmText = "Подтвердить",
                                  string cancelText = "Отмена",
                                  string expectedInput = null,
                                  bool isInputCaseSensitive = true,
                                  Action confirmAction = null,
                                  Action cancelAction = null)
        {
            InitializeComponent();

            _type = type;
            _confirmText = confirmText;
            _cancelText = cancelText;
            _expectedInput = expectedInput;
            _isInputCaseSensitive = isInputCaseSensitive;
            _confirmAction = confirmAction;
            _cancelAction = cancelAction;

            TitleText.Text = title;
            MessageText.Text = message;

            ConfigureDialog();
            ApplyAnimation();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус
            if (_type == ConfirmationType.Input && InputTextBox.Visibility == Visibility.Visible)
            {
                InputTextBox.Focus();
            }
            else if (_type == ConfirmationType.Password && InputPasswordBox.Visibility == Visibility.Visible)
            {
                InputPasswordBox.Focus();
            }
            else
            {
                CancelButton.Focus();
            }
        }

        // Настройка диалога в зависимости от типа
        private void ConfigureDialog()
        {
            // Устанавливаем текст кнопок
            ConfirmButton.Content = _confirmText;
            CancelButton.Content = _cancelText;

            // Настраиваем внешний вид в зависимости от типа
            switch (_type)
            {
                case ConfirmationType.Question:
                    IconText.Text = "❓";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#3498DB");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#2980B9");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    break;

                case ConfirmationType.Warning:
                    IconText.Text = "⚠️";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#F39C12");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#E67E22");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                    WarningBorder.Visibility = Visibility.Visible;
                    break;

                case ConfirmationType.Error:
                    IconText.Text = "❌";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#E74C3C");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#C0392B");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    break;

                case ConfirmationType.Info:
                    IconText.Text = "ℹ️";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#3498DB");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#2980B9");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    btnCloseIcon.Visibility = Visibility.Visible;
                    break;

                case ConfirmationType.Success:
                    IconText.Text = "✅";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#4CAF50");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#45A049");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    btnCloseIcon.Visibility = Visibility.Visible;
                    break;

                case ConfirmationType.Delete:
                    IconText.Text = "🗑️";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#E74C3C");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#C0392B");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    ConfirmButton.Content = "Удалить";
                    CancelButton.Content = "Отмена";
                    WarningBorder.Visibility = Visibility.Visible;
                    WarningText.Text = "Это действие нельзя отменить! Все данные будут безвозвратно удалены.";
                    break;

                case ConfirmationType.Input:
                    IconText.Text = "✏️";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#9B59B6");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#8E44AD");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6"));
                    InputBorder.Visibility = Visibility.Visible;
                    InputTextBox.Visibility = Visibility.Visible;
                    InputPasswordBox.Visibility = Visibility.Collapsed;
                    ConfirmButton.IsEnabled = false;
                    break;

                case ConfirmationType.Password:
                    IconText.Text = "🔒";
                    GradientStart.Color = (Color)ColorConverter.ConvertFromString("#E67E22");
                    GradientEnd.Color = (Color)ColorConverter.ConvertFromString("#D35400");
                    ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                    InputBorder.Visibility = Visibility.Visible;
                    InputTextBox.Visibility = Visibility.Collapsed;
                    InputPasswordBox.Visibility = Visibility.Visible;
                    ConfirmButton.IsEnabled = false;
                    break;
            }
        }

        // Анимация появления
        private void ApplyAnimation()
        {
            var storyboard = new Storyboard();

            var scaleAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleAnimation);

            var scaleAnimationY = scaleAnimation.Clone();
            Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleAnimationY);

            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(opacityAnimation);

            storyboard.Begin(this);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void InputPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            if (_type == ConfirmationType.Input)
            {
                string input = InputTextBox.Text;

                if (!string.IsNullOrEmpty(_expectedInput))
                {
                    bool isValid = _isInputCaseSensitive
                        ? input == _expectedInput
                        : input.Equals(_expectedInput, StringComparison.OrdinalIgnoreCase);

                    ConfirmButton.IsEnabled = isValid;
                }
                else
                {
                    ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(input);
                }
            }
            else if (_type == ConfirmationType.Password)
            {
                ConfirmButton.IsEnabled = InputPasswordBox.Password.Length > 0;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Result = _type == ConfirmationType.Input || _type == ConfirmationType.Password
                ? ConfirmationResult.InputConfirmed
                : ConfirmationResult.Confirmed;

            InputValue = _type == ConfirmationType.Input ? InputTextBox.Text : InputPasswordBox.Password;
            DontAskAgain = DontAskAgainCheckBox.IsChecked ?? false;

            _confirmAction?.Invoke();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmationResult.Cancelled;
            _cancelAction?.Invoke();

            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmationResult.Cancelled;
            DialogResult = false;
            Close();
        }

        // Дополнительные методы
        public void SetDontAskAgain(bool show, string text = "Больше не спрашивать")
        {
            DontAskAgainCheckBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            DontAskAgainCheckBox.Content = text;
        }

        public void SetDetails(IEnumerable<string> details, string title = "Детали:")
        {
            if (details != null && details.Any())
            {
                DetailsBorder.Visibility = Visibility.Visible;
                var detailsList = new List<string> { title };
                detailsList.AddRange(details);
                DetailsList.ItemsSource = detailsList;
            }
        }

        public void SetInfo(string info, string icon = "ℹ️")
        {
            if (!string.IsNullOrEmpty(info))
            {
                InfoBorder.Visibility = Visibility.Visible;
                InfoText.Text = info;
                InfoIcon.Text = icon;
            }
        }

        public void SetProgress(bool isIndeterminate = true, string text = "Выполняется операция...")
        {
            ProgressBorder.Visibility = Visibility.Visible;
            ProgressText.Text = text;
            ProgressBar.IsIndeterminate = isIndeterminate;
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }

        public void UpdateProgress(int value, string text = null)
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = value;

            if (!string.IsNullOrEmpty(text))
            {
                ProgressText.Text = text;
            }
        }

        // Статические методы
        public static bool Show(string title, string message,
                                ConfirmationType type = ConfirmationType.Question,
                                string confirmText = "Подтвердить",
                                string cancelText = "Отмена")
        {
            var dialog = new ConfirmationDialog(title, message, type, confirmText, cancelText);
            dialog.ShowDialog();
            return dialog.Result == ConfirmationResult.Confirmed;
        }

        public static ConfirmationResult ShowInput(string title, string message, out string input)
        {
            return ShowInput(title, message, out input, null, true);
        }

        public static ConfirmationResult ShowInput(string title, string message, out string input,
                                                   string expectedInput, bool isCaseSensitive = true)
        {
            var dialog = new ConfirmationDialog(
                title,
                message,
                ConfirmationType.Input,
                "Подтвердить",
                "Отмена",
                expectedInput,
                isCaseSensitive
            );

            dialog.ShowDialog();
            input = dialog.InputValue;
            return dialog.Result;
        }

        public static bool ShowDelete(string title, string message,
                                       IEnumerable<string> details = null)
        {
            var dialog = new ConfirmationDialog(title, message, ConfirmationType.Delete, "Удалить", "Отмена");

            if (details != null)
            {
                dialog.SetDetails(details);
            }

            dialog.ShowDialog();
            return dialog.Result == ConfirmationResult.Confirmed;
        }

        public static void ShowInfo(string title, string message)
        {
            var dialog = new ConfirmationDialog(title, message, ConfirmationType.Info, "OK", "");
            dialog.CancelButton.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(dialog.ConfirmButton, 2);
            Grid.SetColumn(dialog.ConfirmButton, 0);
            dialog.ShowDialog();
        }

        public static void ShowSuccess(string title, string message)
        {
            var dialog = new ConfirmationDialog(title, message, ConfirmationType.Success, "OK", "");
            dialog.CancelButton.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(dialog.ConfirmButton, 2);
            Grid.SetColumn(dialog.ConfirmButton, 0);
            dialog.ShowDialog();
        }

        public static void ShowError(string title, string message)
        {
            var dialog = new ConfirmationDialog(title, message, ConfirmationType.Error, "OK", "");
            dialog.CancelButton.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(dialog.ConfirmButton, 2);
            Grid.SetColumn(dialog.ConfirmButton, 0);
            dialog.ShowDialog();
        }

        public static bool ShowWarning(string title, string message, string confirmText = "Продолжить")
        {
            var dialog = new ConfirmationDialog(title, message, ConfirmationType.Warning, confirmText, "Отмена");
            dialog.ShowDialog();
            return dialog.Result == ConfirmationResult.Confirmed;
        }
    }
}