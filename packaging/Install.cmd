@echo off
rem Modified by lnkiai (2026): pass -Ask so that the Meltype IME registration is offered
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Ask
pause
