@echo off
echo Запуск Helpdesk...
start "Backend — dotnet run" cmd /k "cd /d C:\Users\vovao\OneDrive\Документы\helpdesk\backend && dotnet run"
timeout /t 3 /nobreak >nul
start "Frontend — npm run dev" cmd /k "cd /d C:\Users\vovao\OneDrive\Документы\helpdesk\frontend && npm run dev"
echo Открываю браузер...
timeout /t 5 /nobreak >nul
start http://localhost:5173
