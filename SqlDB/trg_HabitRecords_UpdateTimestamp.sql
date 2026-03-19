-- Триггер для автоматического обновления даты изменения
CREATE TRIGGER trg_HabitRecords_UpdateTimestamp
ON HabitRecords
AFTER UPDATE
AS
BEGIN
    UPDATE hr
    SET UpdatedAt = GETDATE()
    FROM HabitRecords hr
    INNER JOIN inserted i ON hr.RecordId = i.RecordId;
END;
GO
