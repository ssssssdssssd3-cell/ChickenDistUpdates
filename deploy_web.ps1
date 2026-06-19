$ErrorActionPreference = "Stop"

Write-Host "=========================================="
Write-Host "      Firebase Web Deploy Script"
Write-Host "=========================================="

Write-Host "1. Installing Firebase Tools globally..."
try {
    npm install -g firebase-tools
    Write-Host "Firebase tools installed successfully."
} catch {
    Write-Host "Failed to install firebase-tools. Please make sure Node.js is installed."
    exit 1
}

Write-Host "2. Opening browser for Firebase login..."
firebase login

Write-Host "3. Deploying web app to Firebase Hosting..."
Set-Location -Path (Join-Path $PSScriptRoot "bot")

try {
    firebase deploy --only hosting --project checkin-192ab
    Write-Host "=========================================="
    Write-Host "SUCCESS: Web App deployed successfully!"
    Write-Host "URL: https://checkin-192ab.web.app"
    Write-Host "=========================================="
} catch {
    Write-Host "Deployment failed. Please check your internet connection and login status."
    exit 1
}
