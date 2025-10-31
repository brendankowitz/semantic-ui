@echo off
REM SemanticUI Desktop Application Launcher
REM Simple batch script to start both backend and frontend

setlocal enabledelayedexpansion

echo.
echo ========================================================
echo   SemanticUI - Starting Application
echo ========================================================
echo.

REM Start backend in a separate window
echo Starting backend on http://localhost:5050...
start "SemanticUI Backend" cmd /k "cd src\SemanticUI.Api && dotnet run"

REM Wait for backend to start
timeout /t 3 /nobreak

REM Start frontend in a separate window
echo Starting frontend on http://localhost:5173...
start "SemanticUI Frontend" cmd /k "cd src\SemanticUI.Web && npm run dev"

REM Wait a moment
timeout /t 2 /nobreak

REM Open browser
echo.
echo ========================================================
echo   Application Started!
echo ========================================================
echo.
echo Frontend: http://localhost:5173
echo Backend:  http://localhost:5050
echo.
echo Closing this window will NOT stop the servers.
echo To stop: Close both the Backend and Frontend windows.
echo.
timeout /t 5 /nobreak

exit /b 0
