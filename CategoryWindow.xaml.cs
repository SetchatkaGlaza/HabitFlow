using HabitFlow.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// ViewModel для отображения категории в списке
    /// </summary>
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public int HabitsCount { get; set; }

        // Визуальные свойства
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public string TextColor { get; set; }
        public string CountBgColor { get; set; }
        public string CountTextColor { get; set; }

        public CategoryViewModel(Categories category, int habitsCount)
        {
            CategoryId = category.CategoryId;
            CategoryName = category.CategoryName;
            Description = category.Description;
            Color = category.Color ?? "#4CAF50";
            Icon = category.Icon ?? "📁";
            HabitsCount = habitsCount;

            // Настройка визуальных цветов
            BackgroundColor = Color + "20"; // Добавляем прозрачность
            BorderColor = Color;
            TextColor = "#333";
            CountBgColor = Color;
            CountTextColor = "White";
        }
    }

    /// <summary>
    /// ViewModel для отображения привычки в списке категорий
    /// </summary>
    public class HabitCategoryViewModel
    {
        public int HabitId { get; set; }
        public string HabitName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsInCategory { get; set; }
        public int CategoryId { get; set; }

        public HabitCategoryViewModel(Habits habit, int categoryId, bool isInCategory)
        {
            HabitId = habit.HabitId;
            HabitName = habit.HabitName;
            Description = habit.Description ?? "Нет описания";
            Icon = habit.IconEmoji ?? "📌";
            CategoryId = categoryId;
            IsInCategory = isInCategory;
        }
    }

    /// <summary>
    /// Логика взаимодействия для CategoryWindow.xaml
    /// </summary>
    public partial class CategoryWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;

        private ObservableCollection<CategoryViewModel> _categories;
        private ObservableCollection<HabitCategoryViewModel> _categoryHabits;

        private Categories _selectedCategory;
        private Dictionary<int, bool> _originalHabitStates;
        private bool _hasChanges;

        public CategoryWindow(Users user)
        {
            InitializeComponent();
            _context = new HabitTrackerEntities();
            _currentUser = user;

            _categories = new ObservableCollection<CategoryViewModel>();
            _categoryHabits = new ObservableCollection<HabitCategoryViewModel>();
            _originalHabitStates = new Dictionary<int, bool>();

            listCategories.ItemsSource = _categories;
            listCategoryHabits.ItemsSource = _categoryHabits;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            UpdateTotalStats();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка категорий пользователя
        private void LoadCategories()
        {
            _categories.Clear();

            var categories = _context.Categories
                .Where(c => c.UserId == _currentUser.UserId && (c.IsActive ?? true))
                .OrderBy(c => c.DisplayOrder ?? 0)
                .ThenBy(c => c.CategoryName)
                .ToList();

            foreach (var category in categories)
            {
                int habitsCount = _context.HabitCategories
                    .Count(hc => hc.CategoryId == category.CategoryId);

                _categories.Add(new CategoryViewModel(category, habitsCount));
            }
        }

        // Обновление общей статистики
        private void UpdateTotalStats()
        {
            txtTotalCategories.Text = _categories.Count.ToString();

            int totalHabitsInCategories = _context.HabitCategories
                .Count(hc => hc.Categories.Users.UserId == _currentUser.UserId);

            txtTotalHabitsInCategories.Text = totalHabitsInCategories.ToString();
        }

        // Загрузка привычек для выбранной категории
        private void LoadCategoryHabits(Categories category)
        {
            _categoryHabits.Clear();
            _originalHabitStates.Clear();

            if (category == null) return;

            var allHabits = _context.Habits
                .Where(h => h.UserId == _currentUser.UserId && (h.IsActive ?? true))
                .OrderBy(h => h.HabitName)
                .ToList();

            var categoryHabitIds = _context.HabitCategories
                .Where(hc => hc.CategoryId == category.CategoryId)
                .Select(hc => hc.HabitId)
                .ToHashSet();

            foreach (var habit in allHabits)
            {
                bool isInCategory = categoryHabitIds.Contains(habit.HabitId);
                _originalHabitStates[habit.HabitId] = isInCategory;

                _categoryHabits.Add(new HabitCategoryViewModel(habit, category.CategoryId, isInCategory));
            }

            txtCategoryHabitsCount.Text = categoryHabitIds.Count.ToString();
            _hasChanges = false;
            btnApplyChanges.IsEnabled = false;
        }

        // Обновление отображения выбранной категории
        private void UpdateSelectedCategoryDisplay(Categories category)
        {
            if (category == null)
            {
                txtSelectedCategory.Text = "Выберите категорию";
                colorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
                btnEditCategory.IsEnabled = false;
                btnDeleteCategory.IsEnabled = false;
                return;
            }

            txtSelectedCategory.Text = category.CategoryName;
            colorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(category.Color ?? "#4CAF50"));
            btnEditCategory.IsEnabled = true;
            btnDeleteCategory.IsEnabled = true;
        }

        // Обработчик выбора категории
        private void listCategories_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (listCategories.SelectedItem is CategoryViewModel selectedVM)
            {
                _selectedCategory = _context.Categories.Find(selectedVM.CategoryId);
                UpdateSelectedCategoryDisplay(_selectedCategory);
                LoadCategoryHabits(_selectedCategory);
            }
            else
            {
                _selectedCategory = null;
                UpdateSelectedCategoryDisplay(null);
                _categoryHabits.Clear();
            }
        }

        // Обработчик изменения назначения привычки
        private void HabitCategory_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox?.Tag is int habitId)
            {
                // Находим привычку в коллекции и обновляем ее состояние
                var habit = _categoryHabits.FirstOrDefault(h => h.HabitId == habitId);
                if (habit != null)
                {
                    habit.IsInCategory = checkBox.IsChecked ?? false;
                }

                // Проверяем, есть ли изменения
                _hasChanges = _categoryHabits.Any(h =>
                    _originalHabitStates.ContainsKey(h.HabitId) &&
                    _originalHabitStates[h.HabitId] != h.IsInCategory);

                btnApplyChanges.IsEnabled = _hasChanges;

                // Обновляем счетчик
                if (_selectedCategory != null)
                {
                    int countInCategory = _categoryHabits.Count(h => h.IsInCategory);
                    txtCategoryHabitsCount.Text = countInCategory.ToString();
                }
            }
        }

        // Применение изменений
        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategory == null) return;

                // Получаем текущие ID привычек в категории
                var currentHabitIds = _categoryHabits
                    .Where(h => h.IsInCategory)
                    .Select(h => h.HabitId)
                    .ToHashSet();

                var existingLinks = _context.HabitCategories
                    .Where(hc => hc.CategoryId == _selectedCategory.CategoryId)
                    .ToList();

                // Удаляем связи, которых больше нет
                foreach (var link in existingLinks)
                {
                    if (!currentHabitIds.Contains(link.HabitId))
                    {
                        _context.HabitCategories.Remove(link);
                    }
                }

                // Добавляем новые связи
                var existingHabitIds = existingLinks.Select(l => l.HabitId).ToHashSet();
                foreach (int habitId in currentHabitIds)
                {
                    if (!existingHabitIds.Contains(habitId))
                    {
                        _context.HabitCategories.Add(new HabitCategories
                        {
                            HabitId = habitId,
                            CategoryId = _selectedCategory.CategoryId,
                            AssignedAt = DateTime.Now
                        });
                    }
                }

                _context.SaveChanges();

                // Обновляем оригинальные состояния
                foreach (var habit in _categoryHabits)
                {
                    _originalHabitStates[habit.HabitId] = habit.IsInCategory;
                }

                _hasChanges = false;
                btnApplyChanges.IsEnabled = false;

                // Обновляем счетчик в списке категорий
                var categoryVM = _categories.FirstOrDefault(c => c.CategoryId == _selectedCategory.CategoryId);
                if (categoryVM != null)
                {
                    categoryVM.HabitsCount = currentHabitIds.Count;
                }

                // Обновляем список категорий
                listCategories.Items.Refresh();
                UpdateTotalStats();

                ShowStatus("✅ Изменения сохранены");
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Ошибка: {ex.Message}", false);
            }
        }

        // Добавление категории
        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditCategoryWindow(_currentUser.UserId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                LoadCategories();
                UpdateTotalStats();
                ShowStatus("✅ Категория добавлена");
            }
        }

        // Редактирование категории
        private void btnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null) return;

            var dialog = new AddEditCategoryWindow(_currentUser.UserId, _selectedCategory.CategoryId);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                // Обновляем контекст
                _context.Entry(_selectedCategory).Reload();

                LoadCategories();
                UpdateSelectedCategoryDisplay(_selectedCategory);

                // Перезагружаем привычки для обновления цвета и иконки
                LoadCategoryHabits(_selectedCategory);
                ShowStatus("✅ Категория обновлена");
            }
        }

        // Удаление категории
        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию \"{_selectedCategory.CategoryName}\"?\n\n" +
                "Привычки в этой категории НЕ будут удалены, но потеряют привязку к категории.",
                "Удаление категории",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем связи с привычками
                    var links = _context.HabitCategories
                        .Where(hc => hc.CategoryId == _selectedCategory.CategoryId)
                        .ToList();
                    _context.HabitCategories.RemoveRange(links);

                    // Удаляем категорию
                    _context.Categories.Remove(_selectedCategory);
                    _context.SaveChanges();

                    LoadCategories();
                    _selectedCategory = null;
                    listCategories.SelectedItem = null;
                    UpdateTotalStats();

                    ShowStatus("✅ Категория удалена");
                }
                catch (Exception ex)
                {
                    ShowStatus($"❌ Ошибка: {ex.Message}", false);
                }
            }
        }

        // Сортировка по категориям
        private void btnSortByCategory_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // Показ статуса
        private void ShowStatus(string message, bool isSuccess = true)
        {
            txtStatus.Text = message;
            borderStatus.Background = isSuccess
                ? new SolidColorBrush(Color.FromArgb(32, 76, 175, 80))
                : new SolidColorBrush(Color.FromArgb(32, 231, 76, 60));
            txtStatus.Foreground = isSuccess
                ? new SolidColorBrush(Color.FromArgb(255, 76, 175, 80))
                : new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
            borderStatus.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                borderStatus.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        // Управление окном
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No) return;
            }

            this.Close();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No) return;
            }

            this.Close();
        }
    }
}