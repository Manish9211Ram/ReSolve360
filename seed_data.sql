USE Resolve360;

-- Seed Categories
SET IDENTITY_INSERT Categories ON;
IF NOT EXISTS (SELECT * FROM Categories WHERE CategoryId = 1) INSERT INTO Categories (CategoryId, CategoryName) VALUES (1, 'Technical Issue');
IF NOT EXISTS (SELECT * FROM Categories WHERE CategoryId = 2) INSERT INTO Categories (CategoryId, CategoryName) VALUES (2, 'Hostel/Facility');
IF NOT EXISTS (SELECT * FROM Categories WHERE CategoryId = 3) INSERT INTO Categories (CategoryId, CategoryName) VALUES (3, 'Academic Query');
IF NOT EXISTS (SELECT * FROM Categories WHERE CategoryId = 4) INSERT INTO Categories (CategoryId, CategoryName) VALUES (4, 'Other');
SET IDENTITY_INSERT Categories OFF;

-- Seed Departments
SET IDENTITY_INSERT Departments ON;
IF NOT EXISTS (SELECT * FROM Departments WHERE DepartmentId = 1) INSERT INTO Departments (DepartmentId, DepartmentName) VALUES (1, 'IT Support');
IF NOT EXISTS (SELECT * FROM Departments WHERE DepartmentId = 2) INSERT INTO Departments (DepartmentId, DepartmentName) VALUES (2, 'Administration');
IF NOT EXISTS (SELECT * FROM Departments WHERE DepartmentId = 3) INSERT INTO Departments (DepartmentId, DepartmentName) VALUES (3, 'Finance');
SET IDENTITY_INSERT Departments OFF;

-- Seed Priorities
SET IDENTITY_INSERT Priorities ON;
IF NOT EXISTS (SELECT * FROM Priorities WHERE PriorityId = 1) INSERT INTO Priorities (PriorityId, PriorityName) VALUES (1, 'Low');
IF NOT EXISTS (SELECT * FROM Priorities WHERE PriorityId = 2) INSERT INTO Priorities (PriorityId, PriorityName) VALUES (2, 'Medium');
IF NOT EXISTS (SELECT * FROM Priorities WHERE PriorityId = 3) INSERT INTO Priorities (PriorityId, PriorityName) VALUES (3, 'High');
SET IDENTITY_INSERT Priorities OFF;

-- Seed Statuses
SET IDENTITY_INSERT Statuses ON;
IF NOT EXISTS (SELECT * FROM Statuses WHERE StatusId = 1) INSERT INTO Statuses (StatusId, StatusName) VALUES (1, 'Open');
IF NOT EXISTS (SELECT * FROM Statuses WHERE StatusId = 2) INSERT INTO Statuses (StatusId, StatusName) VALUES (2, 'In Progress');
IF NOT EXISTS (SELECT * FROM Statuses WHERE StatusId = 3) INSERT INTO Statuses (StatusId, StatusName) VALUES (3, 'Resolved');
SET IDENTITY_INSERT Statuses OFF;
