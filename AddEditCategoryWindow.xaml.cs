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
    /// Логика взаимодействия для AddEditCategoryWindow.xaml
    /// </summary>
    public partial class AddEditCategoryWindow : Window
    {
        private HabitTrackerEntities _context;
        private int _userId;
        private int? _categoryId;
        private Categories _category;

        private string _selectedIcon = "📁";
        private string _selectedColor = "#4CAF50";
        private Button _lastSelectedIconButton;
        private Button _lastSelectedColorButton;

        public AddEditCategoryWindow(int userId, int? categoryId = null)
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            _userId = userId;
            _categoryId = categoryId;

            // Выделяем первую иконку по умолчанию
            _lastSelectedIconButton = btnIcon1;
            btnIcon1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            btnIcon1.Foreground = Brushes.White;

            // Выделяем первый цвет по умолчанию
            _lastSelectedColorButton = btnColor1;
            btnColor1.BorderBrush = Brushes.Black;
            btnColor1.BorderThickness = new Thickness(2);

            if (categoryId.HasValue)
            {
                txtWindowTitle.Text = "Редактирование категории";
                LoadCategoryData(categoryId.Value);
            }

            // Обновляем предпросмотр при изменении
            UpdatePreview();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем фокус на поле названия
            txtCategoryName.Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка данных категории для редактирования
        private void LoadCategoryData(int categoryId)
        {
            try
            {
                _category = _context.Categories.Find(categoryId);
                if (_category == null) return;

                txtCategoryName.Text = _category.CategoryName;
                txtDescription.Text = _category.Description;

                _selectedIcon = _category.Icon ?? "📁";
                HighlightSelectedIcon(_selectedIcon);

                _selectedColor = _category.Color ?? "#4CAF50";
                HighlightSelectedColor(_selectedColor);

                // Устанавливаем порядок отображения
                int orderIndex = Math.Min(_category.DisplayOrder ?? 0, 9);
                cmbDisplayOrder.SelectedIndex = orderIndex;

                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выделение выбранной иконки
        private void HighlightSelectedIcon(string icon)
        {
            if (_lastSelectedIconButton != null)
            {
                _lastSelectedIconButton.ClearValue(Button.BackgroundProperty);
                _lastSelectedIconButton.ClearValue(Button.ForegroundProperty);
                _lastSelectedIconButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                _lastSelectedIconButton.BorderThickness = new Thickness(1);
            }

            var buttons = new[] { btnIcon1, btnIcon2, btnIcon3, btnIcon4, btnIcon5,
                                   btnIcon6, btnIcon7, btnIcon8, btnIcon9, btnIcon10,
                                   btnIcon11, btnIcon12 };

            foreach (var btn in buttons)
            {
                if (btn.Tag?.ToString() == icon)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    btn.Foreground = Brushes.White;
                    btn.BorderBrush = Brushes.Transparent;
                    btn.BorderThickness = new Thickness(0);
                    _lastSelectedIconButton = btn;
                    break;
                }
            }
        }

        // Выделение выбранного цвета
        private void HighlightSelectedColor(string color)
        {
            if (_lastSelectedColorButton != null)
            {
                _lastSelectedColorButton.BorderThickness = new Thickness(0);
            }

            var buttons = new[] { btnColor1, btnColor2, btnColor3,
                                   btnColor4, btnColor5, btnColor6 };

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

            UpdatePreview();
        }

        // Обновление предпросмотра
        private void UpdatePreview()
        {
            try
            {
                // Обновляем иконку
                previewIcon.Text = _selectedIcon;

                // Обновляем название
                previewName.Text = string.IsNullOrWhiteSpace(txtCategoryName.Text)
                    ? "Название категории"
                    : txtCategoryName.Text;

                // Обновляем цвет
                var previewBorder = previewIcon.Parent as Border;
                if (previewBorder != null)
                {
                    previewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedColor));
                }

                // Обновляем счетчик (для примера)
                previewCount.Text = "0";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления предпросмотра: {ex.Message}");
            }
        }

        // Обработчик выбора иконки
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                _selectedIcon = button.Tag.ToString();
                HighlightSelectedIcon(_selectedIcon);
                UpdatePreview();
            }
        }

        // Обработчик выбора цвета
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                _selectedColor = button.Tag.ToString();
                HighlightSelectedColor(_selectedColor);
            }
        }

        // Валидация формы
        private void txtCategoryName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(txtCategoryName.Text);
            UpdatePreview();
        }

        // Сохранение
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранный порядок отображения
                int displayOrder = 0;
                if (cmbDisplayOrder.SelectedItem is ComboBoxItem item)
                {
                    int.TryParse(item.Tag.ToString(), out displayOrder);
                }

                if (_categoryId.HasValue)
                {
                    // Редактирование существующей категории
                    _category.CategoryName = txtCategoryName.Text.Trim();
                    _category.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
                    _category.Icon = _selectedIcon;
                    _category.Color = _selectedColor;
                    _category.DisplayOrder = displayOrder;

                    _context.Entry(_category).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    // Создание новой категории
                    var newCategory = new Categories
                    {
                        UserId = _userId,
                        CategoryName = txtCategoryName.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                        Icon = _selectedIcon,
                        Color = _selectedColor,
                        DisplayOrder = displayOrder,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.Categories.Add(newCategory);
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}