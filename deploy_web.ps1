# ============================================================
#  🚀 Firebase Web Deploy Script - ChickenDist
# ============================================================
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Blue
Write-Host "   🔥 Firebase Web Deploy Script v1.0   " -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Blue

# Step 1: Install firebase-tools globally if not installed
Write-Host "`n1. جاري التحقق من أدوات Firebase وتثبيتها..." -ForegroundColor Cyan
try {
    npm install -g firebase-tools
    Write-Host "✅ تم تثبيت أدوات Firebase بنجاح." -ForegroundColor Green
} catch {
    Write-Host "❌ فشل تثبيت أدوات Firebase عبر npm. تأكد من تثبيت Node.js بشكل صحيح." -ForegroundColor Red
    exit 1
}

# Step 2: Login to Firebase
Write-Host "`n2. سيتم الآن فتح المتصفح لتسجيل الدخول إلى حساب جوجل الخاص بك..." -ForegroundColor Cyan
Write-Host "👉 يرجى تأكيد الدخول في المتصفح ثم العودة هنا لإكمال العملية." -ForegroundColor Yellow
firebase login

# Step 3: Deploy to Firebase Hosting
Write-Host "`n3. جاري رفع لوحة تحكم المحاسب إلى السيرفر السحابي (Firebase Hosting)..." -ForegroundColor Cyan
Set-Location -Path (Join-Path $PSScriptRoot "bot")

try {
    firebase deploy --only hosting --project checkin-192ab
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "  ✅ تم رفع لوحة المحاسب بنجاح إلى السحابة!" -ForegroundColor Green
    Write-Host "  🔗 الرابط الدائم والمستقر للمحاسب هو:" -ForegroundColor Green
    Write-Host "  https://checkin-192ab.web.app" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ فشل رفع الملفات. تأكد من أنك قمت بتسجيل الدخول إلى نفس الحساب الذي يحتوي على المشروع checkin-192ab." -ForegroundColor Red
    exit 1
}
