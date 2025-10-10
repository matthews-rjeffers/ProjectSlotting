@echo off
echo ========================================
echo Project Scheduler - Initial Setup
echo ========================================
echo.

echo Step 1: Installing Backend Dependencies...
cd /d "%~dp0"
dotnet restore ProjectScheduler.csproj
if errorlevel 1 (
    echo ERROR: Failed to restore .NET packages
    pause
    exit /b 1
)
echo Backend dependencies installed successfully!
echo.

echo Step 2: Installing Frontend Dependencies...
cd frontend
call npm install
if errorlevel 1 (
    echo ERROR: Failed to install npm packages
    pause
    exit /b 1
)
cd ..
echo Frontend dependencies installed successfully!
echo.

echo Step 3: Setting up Database...
echo Creating EF Core migrations...
dotnet ef migrations add InitialCreate --project ProjectScheduler.csproj
if errorlevel 1 (
    echo ERROR: Failed to create migrations
    echo Make sure you have dotnet-ef tool installed:
    echo Run: dotnet tool install --global dotnet-ef
    pause
    exit /b 1
)

echo Applying migrations to database...
dotnet ef database update --project ProjectScheduler.csproj
if errorlevel 1 (
    echo ERROR: Failed to update database
    echo Make sure SQL Server LocalDB is installed
    pause
    exit /b 1
)
echo Database setup completed successfully!
echo.

echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Next steps:
echo 1. Review appsettings.json to verify database connection string
echo 2. Run start.bat to launch the application
echo 3. Navigate to http://localhost:3000
echo.
echo The database has been seeded with:
echo - 3 Squads (Alpha, Beta, Gamma)
echo - 11 Team Members per squad (3 Project Leads + 8 Developers)
echo.
pause
