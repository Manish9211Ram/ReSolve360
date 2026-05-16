# Resolve360 — Grievance Redressal System

**Resolve360** is a comprehensive, web-based Grievance Redressal portal built with ASP.NET Core MVC. It provides a seamless platform for users (students/employees) to submit complaints and for administrators to manage, track, and resolve them efficiently.

## 🚀 Features

### For Users:
- **Easy Registration & Login:** Secure authentication using SHA-256 password hashing.
- **Raise Grievances:** Submit complaints categorizing them by priority and department.
- **Track Status:** View the real-time status of submitted grievances (Open, In Progress, Resolved).
- **Dashboard:** Personalized view summarizing all raised issues.

### For Administrators:
- **Admin Dashboard:** Get aggregate counts and insights of total, open, in-progress, and resolved grievances.
- **Manage Grievances:** View all system grievances and filter them by status.
- **Update Status:** Change grievance states and log actions with remarks.

## 💻 Technology Stack

- **Framework:** ASP.NET Core MVC (.NET 8+)
- **Database:** Microsoft SQL Server / SQL Express
- **ORM:** Dapper (micro-ORM for high performance)
- **Authentication:** Cookie-based Authentication
- **Architecture Pattern:** Repository Pattern (centralized DB logic via Stored Procedures)

## 🗄️ Database Setup

The project uses SQL Server to manage the application state using efficient stored procedures.

1. Ensure you have **Microsoft SQL Server** (or SQL Express) installed.
2. The default connection string expects the server at `MANN\SQLEXPRESS`. Update the `appsettings.json` connection string as per your local setup:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=Resolve360;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```
3. Run the database scripts provided in the root directory in your SQL Server Management Studio:
   - Execute `schema.sql` to create the database, tables, and stored procedures.
   - Execute `seed_data.sql` to populate master data (Categories, Departments, Priorities, Statuses).

## 🛠️ How to Run Locally

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Manish9211Ram/ReSolve360.git
   ```
2. **Navigate to the project directory:**
   ```bash
   cd ReSolve360/GrievanceRedressal/GrievanceRedressal
   ```
3. **Open the Solution:**
   Open the solution file in Visual Studio 2022.
4. **Configure Secrets:**
   If you intend to use the AI features, replace the `"YOUR_GROQ_API_KEY"` placeholder in `appsettings.json` with your actual Groq API key (or ideally use .NET User Secrets to store it securely).
5. **Run the Application:**
   Press `F5` in Visual Studio or run the following command using the .NET CLI:
   ```bash
   dotnet run
   ```
6. The app will start and default to the Login page (`/Account/Login`).

## 🏗️ System Architecture

The application is built on a clean **Repository Pattern**. All database interactions are centralized in the `GrievanceRepository.cs` class, which uses **Dapper** to execute optimized **Stored Procedures** in SQL Server. This keeps the controllers thin and the SQL out of the C# code.

For full architectural details, database schemas, and system flows, please refer to the detailed [Resolve360_SRS.md](Resolve360_SRS.md) document included in this repository.