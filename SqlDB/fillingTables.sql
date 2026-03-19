-- Заполнение статусов выполнения
INSERT INTO Statuses (StatusId, StatusName, StatusCode, DisplaySymbol) VALUES 
(1, 'Не отмечено', 'not_set', '◻'),
(2, 'Выполнено', 'completed', '✅'),
(3, 'Пропущено', 'skipped', '➖');

-- Создание тестового пользователя (только для разработки)
INSERT INTO Users (UserName, Email) VALUES 
('Тестовый пользователь', 'test@example.com');
-- Второй пользователь на всякий случай
INSERT INTO Users (UserName, Email) VALUES 
('Демо пользователь', 'demo@example.com');

-- Привычки НЕ заполняем - это дело пользователя!