@echo off
chcp 65001 >nul
powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0启动东北迪斯科工作台.ps1"
