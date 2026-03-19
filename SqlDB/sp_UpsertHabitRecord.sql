-- Процедура отметки выполнения привычки
CREATE PROCEDURE sp_UpsertHabitRecord
    @HabitId INT,
    @RecordDate DATE,
    @StatusId INT,
    @Note NVARCHAR(200) = NULL
AS
BEGIN
    IF EXISTS (SELECT 1 FROM HabitRecords WHERE HabitId = @HabitId AND RecordDate = @RecordDate)
    BEGIN
        -- Обновляем существующую запись
        UPDATE HabitRecords
        SET StatusId = @StatusId,
            Note = ISNULL(@Note, Note),
            UpdatedAt = GETDATE()
        WHERE HabitId = @HabitId AND RecordDate = @RecordDate;
    END
    ELSE
    BEGIN
        -- Вставляем новую запись
        INSERT INTO HabitRecords (HabitId, RecordDate, StatusId, Note)
        VALUES (@HabitId, @RecordDate, @StatusId, @Note);
    END
END;
GO