@echo off
echo ========================================
echo Project Scheduler - Quick Start
echo ========================================
echo.

echo Starting Backend API...
cd /d "%~dp0"
start "Backend API" cmd /k "dotnet run"

timeout /t 5 /nobreak > nul

echo Starting Frontend...
cd frontend
start "Frontend Dev Server" cmd /k "npm run dev"

echo.
echo ========================================
echo Both servers are starting...
echo Backend API: http://localhost:5000
echo Frontend: http://localhost:3000
echo ========================================
echo.
echo Press any key to open the application in your browser...
pause > nul

start http://localhost:3000

exit
