CREATE DATABASE Resolve360;
GO

USE Resolve360;
GO

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(20),
    Email VARCHAR(20) UNIQUE,
    PasswordHash VARCHAR(50),
    Role VARCHAR(20),
    DepartmentId INT,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Departments (
    DepartmentId INT PRIMARY KEY IDENTITY(1,1),
    DepartmentName VARCHAR(20),
    Description VARCHAR(100)
);

CREATE TABLE Categories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName VARCHAR(50)
);

CREATE TABLE Priorities (
    PriorityId INT PRIMARY KEY IDENTITY(1,1),
    PriorityName VARCHAR(20)
);

CREATE TABLE Statuses (
    StatusId INT PRIMARY KEY IDENTITY(1,1),
    StatusName VARCHAR(20)
);

CREATE TABLE Grievances (
    GrievanceId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT,
    Title VARCHAR(100),
    Description VARCHAR(250),
    CategoryId INT,
    PriorityId INT,
    StatusId INT,
    DepartmentId INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    FOREIGN KEY (PriorityId) REFERENCES Priorities(PriorityId),
    FOREIGN KEY (StatusId) REFERENCES Statuses(StatusId),
    FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId)
);

CREATE TABLE GrievanceActions (
    ActionId INT PRIMARY KEY IDENTITY(1,1),
    GrievanceId INT,
    ActionBy INT,
    ActionType VARCHAR(30),
    Remarks VARCHAR(150),
    ActionDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (GrievanceId) REFERENCES Grievances(GrievanceId),
    FOREIGN KEY (ActionBy) REFERENCES Users(UserId)
);

CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT,
    GrievanceId INT,
    Message VARCHAR(100),
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE AnonymousFeedback (
    FeedbackId INT PRIMARY KEY IDENTITY(1,1),
    Message VARCHAR(200),
    CategoryId INT,
    Sentiment VARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

CREATE TABLE ChatbotLogs (
    ChatId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT,
    Query VARCHAR(150),
    SuggestedCategory INT,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (SuggestedCategory) REFERENCES Categories(CategoryId)
);

CREATE TABLE TrendPredictions (
    PredictionId INT PRIMARY KEY IDENTITY(1,1),
    CategoryId INT,
    PeriodLabel VARCHAR(30),
    PredictedCount INT,
    GeneratedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

CREATE TABLE Attachments (
    AttachmentId INT PRIMARY KEY IDENTITY(1,1),
    GrievanceId INT,
    FilePath VARCHAR(200),
    FOREIGN KEY (GrievanceId) REFERENCES Grievances(GrievanceId)
);

CREATE TABLE RolesPermissions (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName VARCHAR(20),
    Permission VARCHAR(50)
);

CREATE TABLE Escalations (
    EscalationId INT PRIMARY KEY IDENTITY(1,1),
    GrievanceId INT,
    EscalatedTo INT,
    EscalationDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (GrievanceId) REFERENCES Grievances(GrievanceId),
    FOREIGN KEY (EscalatedTo) REFERENCES Users(UserId)
);
GO

CREATE PROCEDURE sp_RegisterUser
    @Name VARCHAR(20),
    @Email VARCHAR(20),
    @PasswordHash VARCHAR(50),
    @Role VARCHAR(20),
    @DepartmentId INT
AS
BEGIN
    INSERT INTO Users (Name, Email, PasswordHash, Role, DepartmentId)
    VALUES (@Name, @Email, @PasswordHash, @Role, @DepartmentId);
END;
GO

CREATE PROCEDURE sp_LoginUser
    @Email VARCHAR(20),
    @PasswordHash VARCHAR(50)
AS
BEGIN
    SELECT * FROM Users
    WHERE Email = @Email 
      AND PasswordHash = @PasswordHash 
      AND IsActive = 1;
END;
GO

CREATE PROCEDURE sp_CreateGrievance
    @UserId INT,
    @Title VARCHAR(100),
    @Description VARCHAR(250),
    @CategoryId INT,
    @PriorityId INT,
    @DepartmentId INT
AS
BEGIN
    INSERT INTO Grievances
    (UserId, Title, Description, CategoryId, PriorityId, StatusId, DepartmentId)
    VALUES
    (@UserId, @Title, @Description, @CategoryId, @PriorityId, 1, @DepartmentId);
END;
GO

CREATE PROCEDURE sp_GetUserGrievances
    @UserId INT
AS
BEGIN
    SELECT * FROM Grievances
    WHERE UserId = @UserId;
END;
GO

CREATE PROCEDURE sp_GetAllGrievances
AS
BEGIN
    SELECT * FROM Grievances;
END;
GO

CREATE PROCEDURE sp_AssignDepartment
    @GrievanceId INT,
    @DepartmentId INT
AS
BEGIN
    UPDATE Grievances
    SET DepartmentId = @DepartmentId
    WHERE GrievanceId = @GrievanceId;
END;
GO

CREATE PROCEDURE sp_UpdateGrievanceStatus
    @GrievanceId INT,
    @StatusId INT,
    @Remarks VARCHAR(150),
    @ActionBy INT
AS
BEGIN
    UPDATE Grievances
    SET StatusId = @StatusId
    WHERE GrievanceId = @GrievanceId;

    INSERT INTO GrievanceActions (GrievanceId, ActionBy, ActionType, Remarks)
    VALUES (@GrievanceId, @ActionBy, 'Status Update', @Remarks);
END;
GO

CREATE PROCEDURE sp_AddGrievanceAction
    @GrievanceId INT,
    @ActionBy INT,
    @ActionType VARCHAR(30),
    @Remarks VARCHAR(150)
AS
BEGIN
    INSERT INTO GrievanceActions (GrievanceId, ActionBy, ActionType, Remarks)
    VALUES (@GrievanceId, @ActionBy, @ActionType, @Remarks);
END;
GO

CREATE PROCEDURE sp_GetDashboardCounts
AS
BEGIN
    SELECT 
        COUNT(*) AS TotalGrievances,
        SUM(CASE WHEN StatusId = 1 THEN 1 ELSE 0 END) AS OpenCount,
        SUM(CASE WHEN StatusId = 2 THEN 1 ELSE 0 END) AS InProgressCount,
        SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) AS ResolvedCount
    FROM Grievances;
END;
GO

CREATE PROCEDURE sp_GetCategoryTrends
AS
BEGIN
    SELECT CategoryId, COUNT(*) AS Total
    FROM Grievances
    GROUP BY CategoryId;
END;
GO

CREATE PROCEDURE sp_SaveAnonymousFeedback
    @Message VARCHAR(200),
    @CategoryId INT,
    @Sentiment VARCHAR(20)
AS
BEGIN
    INSERT INTO AnonymousFeedback (Message, CategoryId, Sentiment)
    VALUES (@Message, @CategoryId, @Sentiment);
END;
GO

CREATE PROCEDURE sp_SaveChatbotLog
    @UserId INT,
    @Query VARCHAR(150),
    @SuggestedCategory INT
AS
BEGIN
    INSERT INTO ChatbotLogs (UserId, Query, SuggestedCategory)
    VALUES (@UserId, @Query, @SuggestedCategory);
END;
GO

CREATE PROCEDURE sp_GetPendingEscalations
AS
BEGIN
    SELECT * FROM Grievances
    WHERE StatusId = 1
      AND DATEDIFF(DAY, CreatedAt, GETDATE()) > 3;
END;
GO

CREATE PROCEDURE sp_AddAttachment
    @GrievanceId INT,
    @FilePath VARCHAR(200)
AS
BEGIN
    INSERT INTO Attachments (GrievanceId, FilePath)
    VALUES (@GrievanceId, @FilePath);
END;
GO

CREATE PROCEDURE sp_EscalateGrievance
    @GrievanceId INT,
    @EscalatedTo INT
AS
BEGIN
    INSERT INTO Escalations (GrievanceId, EscalatedTo)
    VALUES (@GrievanceId, @EscalatedTo);
END;
GO
