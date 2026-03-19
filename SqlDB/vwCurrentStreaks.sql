-- Представление для расчета серий (streak)
CREATE VIEW vwCurrentStreaks AS
WITH RankedRecords AS (
    SELECT 
        HabitId,
        RecordDate,
        StatusId,
        ROW_NUMBER() OVER (PARTITION BY HabitId ORDER BY RecordDate DESC) AS rn
    FROM HabitRecords
    WHERE StatusId = 2 -- только выполненные
),
ConsecutiveDays AS (
    SELECT 
        HabitId,
        RecordDate,
        DATEDIFF(DAY, RecordDate, GETDATE()) AS DaysAgo,
        CASE 
            WHEN DATEDIFF(DAY, RecordDate, GETDATE()) = rn - 1 
            THEN 1 ELSE 0 
        END AS IsConsecutive
    FROM RankedRecords
)
SELECT 
    HabitId,
    COUNT(*) AS CurrentStreak
FROM ConsecutiveDays
WHERE IsConsecutive = 1
GROUP BY HabitId;