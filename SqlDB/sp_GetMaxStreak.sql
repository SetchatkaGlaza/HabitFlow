-- Процедура получения максимальной серии
CREATE PROCEDURE sp_GetMaxStreak
    @HabitId INT
AS
BEGIN
    WITH StreakGroups AS (
        SELECT 
            RecordDate,
            StatusId,
            DATEADD(DAY, -ROW_NUMBER() OVER (ORDER BY RecordDate), RecordDate) AS StreakGroup
        FROM HabitRecords
        WHERE HabitId = @HabitId AND StatusId = 2
    )
    SELECT 
        MAX(StreakLength) AS MaxStreak
    FROM (
        SELECT 
            COUNT(*) AS StreakLength,
            MIN(RecordDate) AS StreakStart,
            MAX(RecordDate) AS StreakEnd
        FROM StreakGroups
        GROUP BY StreakGroup
    ) AS Streaks;
END;
GO