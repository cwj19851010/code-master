@echo off
cd /d "%~dp0"
echo Starting CodeMaster Backend...
cd CodeMaster.WebApi
dotnet run --urls "http://localhost:5170"
pause
