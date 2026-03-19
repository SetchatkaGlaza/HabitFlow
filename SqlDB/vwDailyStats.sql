-- Представление для статистики выполнения по дням (для графиков)
CREATE VIEW vwDailyStats AS
SELECT 
    h.HabitId,
    h.HabitName,
    hr.RecordDate,
    DATEPART(YEAR, hr.RecordDate) AS Year,
    DATEPART(MONTH, hr.RecordDate) AS Month,
    DATEPART(WEEK, hr.RecordDate) AS WeekNumber,
    DATEPART(WEEKDAY, hr.RecordDate) AS WeekDay,
    s.StatusId,
    s.StatusName,
    CASE WHEN s.StatusId = 2 THEN 1 ELSE 0 END AS IsCompleted
FROM Habits h
JOIN HabitRecords hr ON h.HabitId = hr.HabitId
JOIN Statuses s ON hr.StatusId = s.StatusId
WHERE h.IsActive = 1;