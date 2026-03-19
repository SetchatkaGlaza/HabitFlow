using System;
using System.Windows;

namespace HabitFlow
{
    /// <summary>
    /// Класс для хранения и управления состоянием окон
    /// </summary>
    public static class WindowStateManager
    {
        // Текущее состояние окна
        public static WindowState CurrentWindowState { get; set; } = WindowState.Normal;
        public static double CurrentWidth { get; set; } = 1200;
        public static double CurrentHeight { get; set; } = 850;
        public static double CurrentLeft { get; set; } = 100;
        public static double CurrentTop { get; set; } = 100;

        // Флаг для отслеживания первого запуска
        private static bool _isInitialized = false;

        /// <summary>
        /// Сохранить состояние текущего окна
        /// </summary>
        public static void SaveWindowState(Window window)
        {
            if (window == null) return;

            try
            {
                // Сохраняем состояние только если окно не свернуто
                if (window.WindowState != WindowState.Minimized)
                {
                    CurrentWindowState = window.WindowState;

                    // Для нормального состояния сохраняем размер и позицию
                    if (window.WindowState == WindowState.Normal)
                    {
                        CurrentWidth = window.Width;
                        CurrentHeight = window.Height;
                        CurrentLeft = window.Left;
                        CurrentTop = window.Top;
                    }
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения состояния окна: {ex.Message}");
            }
        }

        /// <summary>
        /// Применить сохраненное состояние к окну
        /// </summary>
        public static void ApplyWindowState(Window window)
        {
            if (window == null) return;

            try
            {
                // Если это первый запуск, применяем сохраненные размеры
                if (!_isInitialized)
                {
                    window.Width = CurrentWidth;
                    window.Height = CurrentHeight;
                    window.Left = CurrentLeft;
                    window.Top = CurrentTop;
                }

                // Устанавливаем состояние
                window.WindowState = CurrentWindowState;

                // Подписываемся на события изменения состояния
                window.StateChanged += (s, e) => SaveWindowState(window);
                window.SizeChanged += (s, e) =>
                {
                    if (window.WindowState == WindowState.Normal)
                        SaveWindowState(window);
                };
                window.LocationChanged += (s, e) =>
                {
                    if (window.WindowState == WindowState.Normal)
                        SaveWindowState(window);
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения состояния окна: {ex.Message}");
            }
        }

        /// <summary>
        /// Открыть новое окно с сохранением состояния
        /// </summary>
        public static void OpenWindow(Window currentWindow, Window newWindow)
        {
            if (currentWindow == null || newWindow == null) return;

            try
            {
                // Сохраняем состояние текущего окна
                SaveWindowState(currentWindow);

                // Применяем сохраненное состояние к новому окну
                newWindow.Width = CurrentWidth;
                newWindow.Height = CurrentHeight;
                newWindow.Left = CurrentLeft;
                newWindow.Top = CurrentTop;
                newWindow.WindowState = CurrentWindowState;

                // Подписываем новый окно на события
                newWindow.StateChanged += (s, e) => SaveWindowState(newWindow);
                newWindow.SizeChanged += (s, e) =>
                {
                    if (newWindow.WindowState == WindowState.Normal)
                        SaveWindowState(newWindow);
                };
                newWindow.LocationChanged += (s, e) =>
                {
                    if (newWindow.WindowState == WindowState.Normal)
                        SaveWindowState(newWindow);
                };

                // Показываем новое окно и закрываем текущее
                newWindow.Show();
                currentWindow.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия окна: {ex.Message}");

                // В случае ошибки просто показываем новое окно
                newWindow.Show();
                currentWindow.Close();
            }
        }
    }
}