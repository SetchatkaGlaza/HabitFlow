-- Процедура получения статистики за период
CREATE PROCEDURE sp_GetHabitStatsByPeriod
    @HabitId INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SELECT 
        RecordDate,
        StatusId,
        Note,
        CASE 
            WHEN LAG(StatusId, 1) OVER (ORDER BY RecordDate) = 2 AND StatusId = 2 THEN 1
            ELSE 0
        END AS IsStreakContinued
    FROM HabitRecords
    WHERE HabitId = @HabitId
        AND RecordDate BETWEEN @StartDate AND @EndDate
    ORDER BY RecordDate;
END;
GO