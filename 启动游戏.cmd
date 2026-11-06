@echo off
cd /d "%~dp0"
where node >nul 2>nul
if errorlevel 1 (
  echo Please install Node.js 22.12 or later.
  pause
  exit /b 1
)
if not exist node_modules\vite\bin\vite.js (
  call npm.cmd ci
  if errorlevel 1 exit /b 1
)
call npm.cmd run dev -- --open
pause
