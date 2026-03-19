using System;
using System.Windows;
using HabitFlow.Entity;
using HabitFlow.Properties; // Добавляем явный using для настроек

namespace HabitFlow
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем, есть ли запомненный пользователь
            if (Settings.Default.RememberedUserId > 0) // Используем Settings.Default вместо Properties.Settings.Default
            {
                try
                {
                    using (var context = new HabitTrackerEntities())
                    {
                        int rememberedUserId = Settings.Default.RememberedUserId;
                        var user = context.Users.Find(rememberedUserId);

                        if (user != null && user.IsActive == true)
                        {
                            // Автоматически входим
                            MainWindow mainWindow = new MainWindow(user);
                            mainWindow.Show();
                            return;
                        }
                        else
                        {
                            // Если пользователь не найден или неактивен, очищаем сохраненные данные
                            Settings.Default.RememberedUserId = 0;
                            Settings.Default.RememberedUserName = "";
                            Settings.Default.Save();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Если ошибка - показываем окно входа
                    System.Diagnostics.Debug.WriteLine($"Ошибка автовхода: {ex.Message}");

                    // Сбрасываем сохраненные данные при ошибке
                    Settings.Default.RememberedUserId = 0;
                    Settings.Default.RememberedUserName = "";
                    Settings.Default.Save();
                }
            }

            // Если нет запомненного пользователя или произошла ошибка, показываем окно входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}