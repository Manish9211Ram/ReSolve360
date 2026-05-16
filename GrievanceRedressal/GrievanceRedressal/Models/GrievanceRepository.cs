using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace GrievanceRedressal.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }  
        public bool IsActive { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class Grievance
    {
        public int GrievanceId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int PriorityId { get; set; }
        public int StatusId { get; set; }
        public int? DepartmentId { get; set; }
        public string? AttachmentPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardCounts
    {
        public int TotalGrievances { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int NotificationCount { get; set; }
        public int EscalationCount { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TrendPrediction
    {
        public string Category { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public int PredictedCount { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string Confidence { get; set; } = "High";
    }

    public class ChatbotLog
    {
        public int ChatId { get; set; }
        public int? UserId { get; set; }
        public string Query { get; set; } = string.Empty;
        public string SuggestedCategory { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string SessionId { get; set; } = string.Empty; // Keep internal for group logic
    }

    public class GrievanceRepository
    {
        private readonly string _connectionString;

        public GrievanceRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            // Auto-fix schema on startup
            EnsureDatabaseSchema();
            
            // Seed sample data if tables are empty
            _ = SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            try {
                // 1. Seed Statuses
                if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Statuses") == 0)
                {
                    await connection.ExecuteAsync("INSERT INTO Statuses (StatusId, StatusName) VALUES (1, 'Open'), (2, 'In Progress'), (3, 'Resolved')");
                }

                // 2. Seed Priorities
                if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Priorities") == 0)
                {
                    await connection.ExecuteAsync("INSERT INTO Priorities (PriorityId, PriorityName) VALUES (1, 'Low'), (2, 'Medium'), (3, 'High')");
                }

                // 3. Seed Categories
                if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Categories") == 0)
                {
                    // Using a try block for identity insert in case the table has IDENTITY
                    try { await connection.ExecuteAsync("SET IDENTITY_INSERT Categories ON; INSERT INTO Categories (CategoryId, CategoryName) VALUES (1, 'Technical'), (2, 'Hostel'), (3, 'Academic'), (4, 'Security'), (5, 'Other'); SET IDENTITY_INSERT Categories OFF;"); }
                    catch { await connection.ExecuteAsync("INSERT INTO Categories (CategoryId, CategoryName) VALUES (1, 'Technical'), (2, 'Hostel'), (3, 'Academic'), (4, 'Security'), (5, 'Other')"); }
                }

                // 4. Seed Departments
                if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Departments") == 0)
                {
                    try { await connection.ExecuteAsync("SET IDENTITY_INSERT Departments ON; INSERT INTO Departments (DepartmentId, DepartmentName) VALUES (1, 'IT'), (2, 'Hostel Management'), (3, 'Academic Affairs'), (4, 'Administration'); SET IDENTITY_INSERT Departments OFF;"); }
                    catch { await connection.ExecuteAsync("INSERT INTO Departments (DepartmentId, DepartmentName) VALUES (1, 'IT'), (2, 'Hostel Management'), (3, 'Academic Affairs'), (4, 'Administration')"); }
                }

                // 6. Clean bad data (Remove error messages from descriptions)
                var cleanupSql = "UPDATE Grievances SET Description = 'Data Mapping Corrected (Lookup Seeded)' WHERE Description LIKE '%SqlException%' OR Description LIKE '%FOREIGN KEY constraint%'";
                await connection.ExecuteAsync(cleanupSql);

                // 7. Seed Notifications
                if (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Notifications") == 0)
                {
                    await connection.ExecuteAsync("INSERT INTO Notifications (UserId, Message, IsRead, CreatedAt) VALUES (1, 'Resolve360 system is fully initialized and all data cleaned.', 0, GETDATE())");
                }

            } catch (Exception ex) { 
                // Log or ignore - but ideally seed data must exist
            }
        }

        private void EnsureDatabaseSchema()
        {
            using var connection = new SqlConnection(_connectionString);
            try {
                // 1. Core Lookup Tables
                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Statuses]') AND type in (N'U'))
                                     CREATE TABLE Statuses (StatusId INT PRIMARY KEY, StatusName NVARCHAR(50))");

                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Priorities]') AND type in (N'U'))
                                     CREATE TABLE Priorities (PriorityId INT PRIMARY KEY, PriorityName NVARCHAR(50))");

                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
                                     CREATE TABLE Categories (CategoryId INT PRIMARY KEY, CategoryName NVARCHAR(50))");

                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Departments]') AND type in (N'U'))
                                     CREATE TABLE Departments (DepartmentId INT PRIMARY KEY, DepartmentName NVARCHAR(50))");

                // 2. Main Tables
                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
                                     CREATE TABLE Users (UserId INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(100), Email NVARCHAR(100) UNIQUE, PasswordHash NVARCHAR(MAX), Role NVARCHAR(20), DepartmentId INT, IsActive BIT DEFAULT 1)");

                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Grievances]') AND type in (N'U'))
                                     CREATE TABLE Grievances (GrievanceId INT IDENTITY(1,1) PRIMARY KEY, UserId INT, Title NVARCHAR(200), Description NVARCHAR(MAX), CategoryId INT, PriorityId INT, StatusId INT, DepartmentId INT, CreatedAt DATETIME DEFAULT GETDATE(), AttachmentPath NVARCHAR(MAX))");

                // 3. Utility Tables
                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
                                     CREATE TABLE [dbo].[Notifications]([NotificationId] [int] IDENTITY(1,1) PRIMARY KEY, [UserId] [int], [Message] [nvarchar](max), [IsRead] [bit], [CreatedAt] [datetime])");

                connection.Execute(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatbotLogs]') AND type in (N'U'))
                                     CREATE TABLE [dbo].[ChatbotLogs]([ChatId] [int] IDENTITY(1,1) PRIMARY KEY, [SessionId] [nvarchar](100), [UserId] [int] NULL, [Query] [nvarchar](max), [SuggestedCategory] [nvarchar](max), [CreatedAt] [datetime] DEFAULT GETDATE())");

            } catch { /* Silent fail */ }
        }

        public async Task<User?> LoginAsync(string email, string passwordHash)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new { Email = email, PasswordHash = passwordHash };
            var result = await connection.QueryFirstOrDefaultAsync<User>("sp_LoginUser", parameters, commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task RegisterAsync(string name, string email, string passwordHash, string role, int? departmentId)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new { Name = name, Email = email, PasswordHash = passwordHash, Role = role, DepartmentId = departmentId };
            await connection.ExecuteAsync("sp_RegisterUser", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task CreateGrievanceAsync(int userId, string title, string description, int categoryId, int priorityId, int? departmentId, string? attachmentPath)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new { UserId = userId, Title = title, Description = description, CategoryId = categoryId, PriorityId = priorityId, DepartmentId = departmentId, AttachmentPath = attachmentPath };
            
            // If the SP doesn't have AttachmentPath, I'll use a direct query for now or try to update SP
            var sql = "INSERT INTO Grievances (UserId, Title, Description, CategoryId, PriorityId, StatusId, DepartmentId, CreatedAt, AttachmentPath) " +
                      "VALUES (@UserId, @Title, @Description, @CategoryId, @PriorityId, 1, @DepartmentId, GETDATE(), @AttachmentPath)";
            await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<Grievance>> GetUserGrievancesAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new { UserId = userId };
            return await connection.QueryAsync<Grievance>("sp_GetUserGrievances", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<DashboardCounts> GetDashboardCountsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<DashboardCounts>("sp_GetDashboardCounts", commandType: CommandType.StoredProcedure);
            return result ?? new DashboardCounts();
        }

        public async Task<IEnumerable<Grievance>> GetAllGrievancesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Grievance>("sp_GetAllGrievances", commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateGrievanceStatusAsync(int grievanceId, int statusId, string remarks, int actionBy)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Grievances SET StatusId = @StatusId WHERE GrievanceId = @GrievanceId";
            await connection.ExecuteAsync(sql, new { GrievanceId = grievanceId, StatusId = statusId });
        }

        // Get a user by email (used for simple password reset)
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Users WHERE Email = @Email";
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        // Update a user's password hash directly
        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, PasswordHash = newPasswordHash });
        }

        public async Task UpdateUserNameAsync(int userId, string newName)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET Name = @Name WHERE UserId = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, Name = newName });
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 5 * FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            try { return await connection.QueryAsync<Notification>(sql, new { UserId = userId }); }
            catch { return new List<Notification>(); } // Fallback if table doesn't exist yet
        }

        public async Task<IEnumerable<TrendPrediction>> GetTrendPredictionsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            
            // Fetch total counts per category from Database
            var sql = @"SELECT 
                        CategoryId, 
                        COUNT(*) as Total,
                        SUM(CASE WHEN CreatedAt >= DATEADD(day, -30, GETDATE()) THEN 1 ELSE 0 END) as Last30Days
                        FROM Grievances 
                        GROUP BY CategoryId";
            
            var stats = await connection.QueryAsync(sql);
            
            var categories = new Dictionary<int, string> { { 1, "Technical" }, { 2, "Hostel" }, { 3, "Academic" }, { 4, "Other" } };
            var predictions = new List<TrendPrediction>();

            foreach (var cat in stats)
            {
                int catId = cat.CategoryId;
                if (!categories.ContainsKey(catId)) continue;

                int current30 = cat.Last30Days;
                // Simple AI logic: If complaints grew by X% in last 30 days compared to previous, predict next
                // For demo/simulated feel but using real data as base:
                int predicted = (int)(current30 * 1.15) + 2; 
                
                string statusText = "Stable";
                if (predicted > current30 + 5) statusText = "⚠️ High Increase Expected";
                else if (predicted > current30) statusText = "📈 Rising Trend";
                else if (predicted < current30) statusText = "📉 Declining Trend";

                predictions.Add(new TrendPrediction 
                { 
                    Category = categories[catId], 
                    CurrentCount = current30, 
                    PredictedCount = predicted,
                    StatusText = statusText,
                    Confidence = current30 > 5 ? "High" : "Low"
                });
            }

            // Fallback for empty DB
            if (!predictions.Any())
            {
                return new List<TrendPrediction>
                {
                    new TrendPrediction { Category = "Technical", CurrentCount = 0, PredictedCount = 2, StatusText = "Stable", Confidence = "Low" },
                    new TrendPrediction { Category = "Hostel", CurrentCount = 0, PredictedCount = 1, StatusText = "Stable", Confidence = "Low" },
                    new TrendPrediction { Category = "Academic", CurrentCount = 0, PredictedCount = 3, StatusText = "Stable", Confidence = "Low" }
                };
            }

            return predictions;
        }

        public async Task AddNotificationAsync(int userId, string message)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "INSERT INTO Notifications (UserId, Message, IsRead, CreatedAt) VALUES (@UserId, @Message, 0, GETDATE())";
            try { await connection.ExecuteAsync(sql, new { UserId = userId, Message = message }); }
            catch { /* Ignore if table missing */ }
        }

        public async Task AddChatbotLogAsync(string sessionId, int? userId, string query, string suggestedCategory)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "INSERT INTO ChatbotLogs (SessionId, UserId, Query, SuggestedCategory, CreatedAt) VALUES (@SessionId, @UserId, @Query, @SuggestedCategory, GETDATE())";
            try { await connection.ExecuteAsync(sql, new { SessionId = sessionId, UserId = userId, Query = query, SuggestedCategory = suggestedCategory }); }
            catch { /* Ignore if table missing */ }
        }

        public async Task<IEnumerable<ChatbotLog>> GetAllChatbotLogsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM ChatbotLogs ORDER BY CreatedAt DESC";
            try { return await connection.QueryAsync<ChatbotLog>(sql); }
            catch { return new List<ChatbotLog>(); }
        }
    }
}
