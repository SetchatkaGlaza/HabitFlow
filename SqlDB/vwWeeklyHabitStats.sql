-- Представление для получения статистики по привычкам за текущую неделю
CREATE VIEW vwWeeklyHabitStats AS
SELECT 
    h.HabitId,
    h.HabitName,
    w.Date,
    w.DayName,
    w.DayOfWeek,
    ISNULL(hr.StatusId, 1) AS StatusId,
    s.StatusName,
    s.DisplaySymbol,
    CASE 
        WHEN hr.StatusId = 2 THEN 1 
        ELSE 0 
    END AS IsCompleted
FROM Habits h
CROSS JOIN vwCurrentWeek w
LEFT JOIN HabitRecords hr ON h.HabitId = hr.HabitId AND w.Date = hr.RecordDate
LEFT JOIN Statuses s ON ISNULL(hr.StatusId, 1) = s.StatusId
WHERE h.IsActive = 1;