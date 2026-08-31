@echo off
chcp 65001 > nul
title Firebase Hosting Deployment
echo ========================================================
echo   Firebase Hosting Deployment for Mobile App
echo ========================================================
echo.
set /p PROJECT_ID="Enter Firebase Project ID: "
if "%PROJECT_ID%"=="" (
    echo [ERROR] Project ID cannot be empty!
    pause
    exit /b
)
echo.
echo [1/2] Setting project: %PROJECT_ID%...
call npx -y firebase-tools use %PROJECT_ID% --add
echo.
echo [2/2] Deploying files to Firebase Hosting...
call npx -y firebase-tools deploy --only hosting,database,firestore --project %PROJECT_ID%
echo.
echo ========================================================
echo   Deployed successfully!
echo   Mobile App URL: https://%PROJECT_ID%.web.app
echo ========================================================
echo.
pause
