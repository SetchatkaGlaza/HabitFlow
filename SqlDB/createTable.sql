-- Таблица пользователей
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Справочник статусов выполнения
CREATE TABLE Statuses (
    StatusId INT PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL,
    StatusCode NVARCHAR(20) NOT NULL, -- 'completed', 'skipped', 'not_set'
    DisplaySymbol NVARCHAR(10) -- для отображения (✅, ➖, ◻)
);

-- Таблица привычек
CREATE TABLE Habits (
    HabitId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    HabitName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATE DEFAULT CAST(GETDATE() AS DATE),
    IsActive BIT DEFAULT 1,
    ColorCode NVARCHAR(7) NULL, -- для визуального оформления (#FF5733)
    IconEmoji NVARCHAR(10) NULL, -- эмодзи для привычки
    
    CONSTRAINT FK_Habits_Users FOREIGN KEY (UserId) 
        REFERENCES Users(UserId) ON DELETE CASCADE
);

-- Таблица записей о выполнении привычек
CREATE TABLE HabitRecords (
    RecordId INT IDENTITY(1,1) PRIMARY KEY,
    HabitId INT NOT NULL,
    RecordDate DATE NOT NULL,
    StatusId INT NOT NULL,
    Note NVARCHAR(200) NULL, -- заметка на день (почему пропустил и т.д.)
    UpdatedAt DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_HabitRecords_Habits FOREIGN KEY (HabitId) 
        REFERENCES Habits(HabitId) ON DELETE CASCADE,
    CONSTRAINT FK_HabitRecords_Statuses FOREIGN KEY (StatusId) 
        REFERENCES Statuses(StatusId),
    -- У одной привычки может быть только одна запись на конкретную дату
    CONSTRAINT UQ_HabitRecords_HabitDate UNIQUE (HabitId, RecordDate)
);