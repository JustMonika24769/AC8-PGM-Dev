@echo off
setlocal
dotnet restore
if errorlevel 1 exit /b 1
dotnet publish -c Release -o dist --no-restore
exit /b %errorlevel%
