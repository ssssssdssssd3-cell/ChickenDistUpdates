@echo off
:: ============================================================
::  ✅ ChickenDist Auto-Backup Script
:: ============================================================
chcp 65001 > nul

:: Configurations
set DB_NAME=ChickenDist
set BACKUP_DIR=D:\ChickenDist_Backups
set KEEP_DAYS=7

:: Generate timestamp: YYYY-MM-DD_HHMM
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,4%-%datetime:~4,2%-%datetime:~6,2%_%datetime:~8,2%%datetime:~10,2%

set BACKUP_FILE=%BACKUP_DIR%\%DB_NAME%_Backup_%TIMESTAMP%.bak

echo Starting backup for database %DB_NAME%...
if not exist "%BACKUP_DIR%" (
    mkdir "%BACKUP_DIR%"
)

:: Run SQL backup command
sqlcmd -S . -E -Q "BACKUP DATABASE [%DB_NAME%] TO DISK='%BACKUP_FILE%' WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS=10"

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Backup failed!
    exit /b %ERRORLEVEL%
)

echo [OK] Backup created successfully: %BACKUP_FILE%

:: Purge old backups (older than %KEEP_DAYS% days)
echo Cleaning up backups older than %KEEP_DAYS% days...
forfiles /p "%BACKUP_DIR%" /m *.bak /d -%KEEP_DAYS% /c "cmd /c del @path" 2>nul

echo Done!
