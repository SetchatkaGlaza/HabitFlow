using HabitFlow.Entity;
using HabitFlow.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HabitFlow
{
    /// <summary>
    /// Модель данных для достижения
    /// </summary>
    public class AchievementModel
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // streak, count, habits, special
        public int Points { get; set; }
        public int Requirement { get; set; }
        public int CurrentProgress { get; set; }

        // Визуальные свойства
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public string IconBgColor { get; set; }
        public string TextColor { get; set; }
        public string ProgressBgColor { get; set; }
        public string ProgressTextColor { get; set; }
        public string ProgressText { get; set; }
        public double Opacity { get; set; }
        public Visibility AchievedVisibility { get; set; }

        public bool IsAchieved => CurrentProgress >= Requirement;
    }

    /// <summary>
    /// Логика взаимодействия для AchievementsWindow.xaml
    /// </summary>
    public partial class AchievementsWindow : Window
    {
        private HabitTrackerEntities _context;
        private Users _currentUser;

        private ObservableCollection<AchievementModel> _streakAchievements;
        private ObservableCollection<AchievementModel> _countAchievements;
        private ObservableCollection<AchievementModel> _habitsAchievements;
        private ObservableCollection<AchievementModel> _specialAchievements;

        // Статистика пользователя
        private int _totalCompleted;
        private int _bestStreak;
        private int _totalHabits;
        private int _totalXP;
        private int _achievementPoints;
        private int _level;
        private string _rank;
        private int _xpForNextLevel;

        public AchievementsWindow(Users user)
        {
            InitializeComponent();

            // Применяем сохраненное состояние окна
            WindowStateManager.ApplyWindowState(this);

            _context = new HabitTrackerEntities();
            _currentUser = user;

            _streakAchievements = new ObservableCollection<AchievementModel>();
            _countAchievements = new ObservableCollection<AchievementModel>();
            _habitsAchievements = new ObservableCollection<AchievementModel>();
            _specialAchievements = new ObservableCollection<AchievementModel>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadUserStatistics();
                InitializeAchievements();
                UpdateDisplay();
                UpdateMotivationMessage();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.ShowError("Ошибка загрузки", $"Ошибка при загрузке достижений: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // Загрузка статистики пользователя
        private void LoadUserStatistics()
        {
            try
            {
                // Общее количество выполнений
                _totalCompleted = _context.HabitRecords
                    .Count(r => r.Habits.UserId == _currentUser.UserId && r.StatusId == 2);

                // Лучшая серия
                var habits = _context.Habits
                    .Where(h => h.UserId == _currentUser.UserId && h.IsActive == true)
                    .Select(h => h.HabitId)
                    .ToList();

                _bestStreak = 0;
                foreach (var habitId in habits)
                {
                    int streak = CalculateMaxStreak(habitId);
                    if (streak > _bestStreak)
                        _bestStreak = streak;
                }

                // Количество активных привычек
                _totalHabits = habits.Count;

                // Расчет опыта и уровня
                CalculateXPAndLevel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
                ConfirmationDialog.ShowError("Ошибка", "Не удалось загрузить статистику пользователя.");
            }
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

        // Расчет опыта и уровня
        private void CalculateXPAndLevel()
        {
            // Базовый опыт: 10 за каждое выполнение + бонусы
            _totalXP = _totalCompleted * 10;

            // Бонус за серии
            _totalXP += _bestStreak * 50;

            // Бонус за привычки
            _totalXP += _totalHabits * 100;

            // Расчет уровня (каждые 1000 опыта - новый уровень)
            _level = (_totalXP / 1000) + 1;
            _xpForNextLevel = _level * 1000;

            int currentLevelXP = (_level - 1) * 1000;
            int xpInCurrentLevel = _totalXP - currentLevelXP;
            int xpNeeded = _xpForNextLevel - currentLevelXP;

            int progressPercent = xpNeeded > 0 ? (xpInCurrentLevel * 100) / xpNeeded : 100;

            // Определение ранга
            if (_level <= 3)
                _rank = "Новичок";
            else if (_level <= 6)
                _rank = "Ученик";
            else if (_level <= 10)
                _rank = "Любитель";
            else if (_level <= 15)
                _rank = "Профессионал";
            else if (_level <= 20)
                _rank = "Мастер";
            else
                _rank = "Легенда";

            string nextRank = GetNextRank(_rank);

            // Обновление UI
            txtLevel.Text = _level.ToString();
            txtXP.Text = _totalXP.ToString();
            progressXP.Value = progressPercent;
            txtXPNextLevel.Text = $"{xpInCurrentLevel}/{xpNeeded} до след. уровня";
            txtRank.Text = _rank;
            txtNextRank.Text = string.IsNullOrEmpty(nextRank) ? "Максимальный ранг!" : $"Следующий: {nextRank}";
        }

        private string GetNextRank(string currentRank)
        {
            switch (currentRank)
            {
                case "Новичок": return "Ученик";
                case "Ученик": return "Любитель";
                case "Любитель": return "Профессионал";
                case "Профессионал": return "Мастер";
                case "Мастер": return "Легенда";
                default: return "";
            }
        }

        // Инициализация достижений
        private void InitializeAchievements()
        {
            // Достижения за серии
            AddStreakAchievement("streak_7", "7️⃣", "Начинающий", "7 дней подряд", 7, 10);
            AddStreakAchievement("streak_14", "🔟", "Стабильный", "14 дней подряд", 14, 20);
            AddStreakAchievement("streak_21", "2️⃣1️⃣", "Целеустремленный", "21 день подряд", 21, 30);
            AddStreakAchievement("streak_30", "3️⃣0️⃣", "Месячный ветеран", "30 дней подряд", 30, 40);
            AddStreakAchievement("streak_60", "6️⃣0️⃣", "Двухмесячный", "60 дней подряд", 60, 50);
            AddStreakAchievement("streak_100", "💯", "Сотник", "100 дней подряд", 100, 100);

            // Достижения за количество выполнений
            AddCountAchievement("count_50", "5️⃣0️⃣", "Первые шаги", "50 выполнений", 50, 10);
            AddCountAchievement("count_100", "1️⃣0️⃣0️⃣", "Сотня", "100 выполнений", 100, 20);
            AddCountAchievement("count_250", "2️⃣5️⃣0️⃣", "Трудяга", "250 выполнений", 250, 30);
            AddCountAchievement("count_500", "5️⃣0️⃣0️⃣", "Полутысячник", "500 выполнений", 500, 40);
            AddCountAchievement("count_1000", "1️⃣0️⃣0️⃣0️⃣", "Тысячник", "1000 выполнений", 1000, 50);
            AddCountAchievement("count_5000", "5️⃣0️⃣0️⃣0️⃣", "Легендарный", "5000 выполнений", 5000, 100);

            // Достижения за количество привычек
            AddHabitsAchievement("habits_3", "3️⃣", "Начинающий", "3 активные привычки", 3, 10);
            AddHabitsAchievement("habits_5", "5️⃣", "Разнообразие", "5 активных привычек", 5, 20);
            AddHabitsAchievement("habits_7", "7️⃣", "Семь дней", "7 активных привычек", 7, 30);
            AddHabitsAchievement("habits_10", "🔟", "Десятка", "10 активных привычек", 10, 40);
            AddHabitsAchievement("habits_15", "1️⃣5️⃣", "Мастер на все руки", "15 активных привычек", 15, 50);
            AddHabitsAchievement("habits_20", "2️⃣0️⃣", "Гуру привычек", "20 активных привычек", 20, 100);

            // Особые достижения
            AddSpecialAchievement("special_week", "📅", "Идеальная неделя", "Выполнять все привычки 7 дней подряд", 7, 30);
            AddSpecialAchievement("special_month", "📆", "Идеальный месяц", "Выполнять все привычки 30 дней подряд", 30, 50);
            AddSpecialAchievement("special_early", "🌅", "Ранняя пташка", "Отметить привычку до 8 утра 10 раз", 10, 20);
            AddSpecialAchievement("special_night", "🌙", "Ночная сова", "Отметить привычку после 23:00 10 раз", 10, 20);
            AddSpecialAchievement("special_all", "🎯", "Абсолют", "Получить все достижения", 23, 100);
            AddSpecialAchievement("special_year", "🎉", "Годовщина", "Пользоваться приложением 1 год", 1, 50);

            // Обновление прогресса
            UpdateAchievementsProgress();
        }

        private void AddStreakAchievement(string id, string icon, string title, string description, int requirement, int points)
        {
            _streakAchievements.Add(new AchievementModel
            {
                Id = id,
                Icon = icon,
                Title = title,
                Description = description,
                Category = "streak",
                Points = points,
                Requirement = requirement,
                CurrentProgress = 0,
                BackgroundColor = "#FFF9E6",
                BorderColor = "#FFD700",
                IconBgColor = "#FFD700",
                TextColor = "#333",
                ProgressBgColor = "#FFD70040",
                ProgressTextColor = "#B8860B",
                Opacity = 1.0,
                AchievedVisibility = Visibility.Collapsed
            });
        }

        private void AddCountAchievement(string id, string icon, string title, string description, int requirement, int points)
        {
            _countAchievements.Add(new AchievementModel
            {
                Id = id,
                Icon = icon,
                Title = title,
                Description = description,
                Category = "count",
                Points = points,
                Requirement = requirement,
                CurrentProgress = 0,
                BackgroundColor = "#E6F3FF",
                BorderColor = "#3498DB",
                IconBgColor = "#3498DB",
                TextColor = "#333",
                ProgressBgColor = "#3498DB40",
                ProgressTextColor = "#2875A7",
                Opacity = 1.0,
                AchievedVisibility = Visibility.Collapsed
            });
        }

        private void AddHabitsAchievement(string id, string icon, string title, string description, int requirement, int points)
        {
            _habitsAchievements.Add(new AchievementModel
            {
                Id = id,
                Icon = icon,
                Title = title,
                Description = description,
                Category = "habits",
                Points = points,
                Requirement = requirement,
                CurrentProgress = 0,
                BackgroundColor = "#FFF0E6",
                BorderColor = "#E67E22",
                IconBgColor = "#E67E22",
                TextColor = "#333",
                ProgressBgColor = "#E67E2240",
                ProgressTextColor = "#B85E0F",
                Opacity = 1.0,
                AchievedVisibility = Visibility.Collapsed
            });
        }

        private void AddSpecialAchievement(string id, string icon, string title, string description, int requirement, int points)
        {
            _specialAchievements.Add(new AchievementModel
            {
                Id = id,
                Icon = icon,
                Title = title,
                Description = description,
                Category = "special",
                Points = points,
                Requirement = requirement,
                CurrentProgress = 0,
                BackgroundColor = "#F3E6FF",
                BorderColor = "#9B59B6",
                IconBgColor = "#9B59B6",
                TextColor = "#333",
                ProgressBgColor = "#9B59B640",
                ProgressTextColor = "#6C3483",
                Opacity = 1.0,
                AchievedVisibility = Visibility.Collapsed
            });
        }

        // Обновление прогресса достижений
        private void UpdateAchievementsProgress()
        {
            int totalAchieved = 0;
            _achievementPoints = 0;

            // Серии
            foreach (var ach in _streakAchievements)
            {
                ach.CurrentProgress = Math.Min(_bestStreak, ach.Requirement);
                UpdateAchievementVisual(ach);
                if (ach.IsAchieved)
                {
                    totalAchieved++;
                    _achievementPoints += ach.Points;
                }
            }

            // Количество выполнений
            foreach (var ach in _countAchievements)
            {
                ach.CurrentProgress = Math.Min(_totalCompleted, ach.Requirement);
                UpdateAchievementVisual(ach);
                if (ach.IsAchieved)
                {
                    totalAchieved++;
                    _achievementPoints += ach.Points;
                }
            }

            // Количество привычек
            foreach (var ach in _habitsAchievements)
            {
                ach.CurrentProgress = Math.Min(_totalHabits, ach.Requirement);
                UpdateAchievementVisual(ach);
                if (ach.IsAchieved)
                {
                    totalAchieved++;
                    _achievementPoints += ach.Points;
                }
            }

            // Особые достижения
            UpdateSpecialAchievements();

            foreach (var ach in _specialAchievements)
            {
                if (ach.IsAchieved)
                {
                    totalAchieved++;
                    _achievementPoints += ach.Points;
                }
            }

            // Обновление категорий
            txtStreakCategoryProgress.Text = $"{_streakAchievements.Count(a => a.IsAchieved)}/6";
            txtCountCategoryProgress.Text = $"{_countAchievements.Count(a => a.IsAchieved)}/6";
            txtHabitsCategoryProgress.Text = $"{_habitsAchievements.Count(a => a.IsAchieved)}/6";
            txtSpecialCategoryProgress.Text = $"{_specialAchievements.Count(a => a.IsAchieved)}/6";

            txtTotalAchievements.Text = $"{totalAchieved}/24";
            txtAchievementPoints.Text = _achievementPoints.ToString();
        }

        private void UpdateAchievementVisual(AchievementModel ach)
        {
            if (ach.IsAchieved)
            {
                ach.ProgressText = "Получено!";
                ach.ProgressBgColor = "#4CAF50";
                ach.ProgressTextColor = "White";
                ach.AchievedVisibility = Visibility.Visible;
                ach.Opacity = 1.0;
            }
            else
            {
                ach.ProgressText = $"{ach.CurrentProgress}/{ach.Requirement}";
                ach.AchievedVisibility = Visibility.Collapsed;
                ach.Opacity = 0.7;
            }
        }

        private void UpdateSpecialAchievements()
        {
            try
            {
                // Идеальная неделя
                var specialWeek = _specialAchievements.FirstOrDefault(a => a.Id == "special_week");
                if (specialWeek != null)
                {
                    specialWeek.CurrentProgress = _bestStreak >= 7 ? 7 : 0;
                    UpdateAchievementVisual(specialWeek);
                }

                // Идеальный месяц
                var specialMonth = _specialAchievements.FirstOrDefault(a => a.Id == "special_month");
                if (specialMonth != null)
                {
                    specialMonth.CurrentProgress = _bestStreak >= 30 ? 30 : 0;
                    UpdateAchievementVisual(specialMonth);
                }

                // Ранняя пташка
                var specialEarly = _specialAchievements.FirstOrDefault(a => a.Id == "special_early");
                if (specialEarly != null)
                {
                    // Здесь должна быть проверка на отметки до 8 утра
                    specialEarly.CurrentProgress = 0;
                    UpdateAchievementVisual(specialEarly);
                }

                // Ночная сова
                var specialNight = _specialAchievements.FirstOrDefault(a => a.Id == "special_night");
                if (specialNight != null)
                {
                    specialNight.CurrentProgress = 0;
                    UpdateAchievementVisual(specialNight);
                }

                // Абсолют (все достижения)
                var specialAll = _specialAchievements.FirstOrDefault(a => a.Id == "special_all");
                if (specialAll != null)
                {
                    int totalAchievements = _streakAchievements.Count(a => a.IsAchieved) +
                                           _countAchievements.Count(a => a.IsAchieved) +
                                           _habitsAchievements.Count(a => a.IsAchieved) +
                                           _specialAchievements.Where(a => a.Id != "special_all" && a.Id != "special_year").Count(a => a.IsAchieved);
                    specialAll.CurrentProgress = Math.Min(totalAchievements, 23);
                    UpdateAchievementVisual(specialAll);
                }

                // Годовщина
                var specialYear = _specialAchievements.FirstOrDefault(a => a.Id == "special_year");
                if (specialYear != null && _currentUser.CreatedAt.HasValue)
                {
                    int years = (DateTime.Now - _currentUser.CreatedAt.Value).Days / 365;
                    specialYear.CurrentProgress = years >= 1 ? 1 : 0;
                    UpdateAchievementVisual(specialYear);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления особых достижений: {ex.Message}");
            }
        }

        // Обновление отображения
        private void UpdateDisplay()
        {
            StreakAchievementsList.ItemsSource = _streakAchievements;
            CountAchievementsList.ItemsSource = _countAchievements;
            HabitsAchievementsList.ItemsSource = _habitsAchievements;
            SpecialAchievementsList.ItemsSource = _specialAchievements;
        }

        // Обновление мотивационного сообщения
        private void UpdateMotivationMessage()
        {
            string[] messages = {
                "🏆 Продолжай в том же духе! Следующее достижение уже близко!",
                "⭐ Ты на правильном пути к новым рекордам!",
                "🔥 Каждый день - шаг к новым достижениям!",
                "💪 Твои успехи растут с каждым днем!",
                "🎯 Сосредоточься на следующих целях!",
                "📈 Прогресс не остановить!",
                "🌟 Ты становишься лучше каждый день!"
            };

            var random = new Random();
            txtMotivationMessage.Text = messages[random.Next(messages.Length)];
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