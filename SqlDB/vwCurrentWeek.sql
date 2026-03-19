-- Представление для получения текущей недели с датами
CREATE VIEW vwCurrentWeek AS
WITH WeekDays AS (
    SELECT 
        DATEADD(DAY, - (DATEPART(WEEKDAY, GETDATE()) - 1), CAST(GETDATE() AS DATE)) AS WeekStart,
        DATEADD(DAY, (7 - DATEPART(WEEKDAY, GETDATE())), CAST(GETDATE() AS DATE)) AS WeekEnd
)
SELECT 
    DATEADD(DAY, number, WeekStart) AS Date,
    CASE DATEPART(WEEKDAY, DATEADD(DAY, number, WeekStart))
        WHEN 1 THEN N'ВС'
        WHEN 2 THEN N'ПН'
        WHEN 3 THEN N'ВТ'
        WHEN 4 THEN N'СР'
        WHEN 5 THEN N'ЧТ'
        WHEN 6 THEN N'ПТ'
        WHEN 7 THEN N'СБ'
    END AS DayName,
    CASE DATEPART(WEEKDAY, DATEADD(DAY, number, WeekStart))
        WHEN 1 THEN 7
        ELSE DATEPART(WEEKDAY, DATEADD(DAY, number, WeekStart)) - 1
    END AS DayOfWeek -- ПН=1, ВС=7
FROM WeekDays
CROSS JOIN (SELECT TOP 7 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS number FROM sys.objects) numbers;