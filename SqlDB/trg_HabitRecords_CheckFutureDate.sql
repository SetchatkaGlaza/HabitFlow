-- Триггер для проверки корректности даты (нельзя отметить будущее)
CREATE TRIGGER trg_HabitRecords_CheckFutureDate
ON HabitRecords
INSTEAD OF INSERT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM inserted WHERE RecordDate > CAST(GETDATE() AS DATE))
    BEGIN
        RAISERROR('Нельзя отмечать привычки на будущие даты', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
    
    INSERT INTO HabitRecords (HabitId, RecordDate, StatusId, Note, UpdatedAt)
    SELECT HabitId, RecordDate, StatusId, Note, GETDATE()
    FROM inserted;
END;
GO