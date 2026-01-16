# Deployment Documentation

## Prerequisites
- .NET 6.0 SDK or later
- SQL Server (LocalDB or Standard)

## Configuration
1. **Database**: Ensure the connection string in `appsettings.json` points to your SQL Server instance.
   ```json
   "ConnectionStrings": {
     "DBDefault": "Server=(localdb)\\mssqllocaldb;Database=SalesManagementDB;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```
2. **Migrations**: If the database is not initialized, run:
   ```bash
   dotnet ef database update
   ```

## Build and Run
1. Open a terminal in the project root (`Sales Management`).
2. Run the application:
   ```bash
   dotnet run
   ```
3. Access the application at `https://localhost:7152` (or the port shown in the console).

## Login
- Use the Admin credentials (seeded or created) to access the Dashboard.
- URL: `/Account/Login`
