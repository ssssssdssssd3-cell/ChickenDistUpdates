# ============================================================
#  🚀 سكريبت البناء والنشر التلقائي — ChickenDist
#  الاستخدام: انقر بالزر الأيمن → Run with PowerShell
#  أو من الـ Terminal: .\deploy.ps1
# ============================================================

$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────
# ⚙️ الإعدادات — عدّل هنا فقط عند كل إصدار جديد
# ─────────────────────────────────────────────
$VERSION   = "1.3.1"
$CHANGELOG = Get-Content -Path (Join-Path $PSScriptRoot "changelog.txt") -Raw -Encoding UTF8
$UPDATE_URL = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist.bin"

# ─────────────────────────────────────────────
# 📁 المسارات — لا تعدّل هذا القسم
# ─────────────────────────────────────────────
$REPO_ROOT    = $PSScriptRoot                                      # مجلد الـ repo الجذر
$PROJECT_DIR  = Join-Path $REPO_ROOT "ChickenDist"                 # ✅ المشروع الصحيح
$CSPROJ       = Join-Path $PROJECT_DIR "ChickenDist.csproj"        # ملف المشروع
$OUT_DIR      = Join-Path $REPO_ROOT "_build_output"               # مجلد ناتج البناء
$BIN_DEST     = Join-Path $REPO_ROOT "ChickenDist.bin"             # الملف النهائي
$UPDATE_TXT   = Join-Path $REPO_ROOT "update.txt"                  # ملف التحديث

# ─────────────────────────────────────────────

function Write-Step { param($msg) Write-Host "`n===[ $msg ]===" -ForegroundColor Cyan }
function Write-OK   { param($msg) Write-Host "  ✅ $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "  ❌ $msg" -ForegroundColor Red; Read-Host "اضغط Enter للخروج"; exit 1 }

Write-Host ""
Write-Host "══════════════════════════════════════════" -ForegroundColor Blue
Write-Host "   🐣 ChickenDist Deploy Script v$VERSION  " -ForegroundColor Yellow
Write-Host "══════════════════════════════════════════" -ForegroundColor Blue

# ─────────────────────────────────────────────
# الخطوة 1: التحقق من المشروع الصحيح
# ─────────────────────────────────────────────
Write-Step "التحقق من المشروع"
if (-not (Test-Path $CSPROJ)) {
    Write-Fail "لم يُعثر على المشروع في: $CSPROJ"
}
Write-OK "المشروع موجود: $CSPROJ"
Write-Host "  ⚠️  المشروع الصحيح هو ChickenDist\ (الداخلي) وليس الجذر!" -ForegroundColor Yellow

# ─────────────────────────────────────────────
# الخطوة 2: البناء
# ─────────────────────────────────────────────
Write-Step "البناء (dotnet publish)"
if (Test-Path $OUT_DIR) { Remove-Item $OUT_DIR -Recurse -Force }

$buildResult = dotnet publish $CSPROJ -c Release -f net48 -o $OUT_DIR --nologo 2>&1
$buildOutput = $buildResult -join "`n"

if ($LASTEXITCODE -ne 0) {
    Write-Host $buildOutput -ForegroundColor Red
    Write-Fail "فشل البناء! راجع الأخطاء أعلاه."
}

$errors = $buildResult | Where-Object { $_ -match "error" -and $_ -notmatch "warning" }
if ($errors) {
    Write-Host ($errors -join "`n") -ForegroundColor Red
    Write-Fail "يوجد أخطاء في البناء!"
}

Write-OK "البناء ناجح"

# ─────────────────────────────────────────────
# الخطوة 3: التحقق من الملف الناتج
# ─────────────────────────────────────────────
Write-Step "التحقق من ملف الـ EXE"
$exePath = Join-Path $OUT_DIR "ChickenDist.exe"
if (-not (Test-Path $exePath)) {
    Write-Fail "لم يُعثر على ChickenDist.exe في مجلد البناء!"
}

$exeSize = (Get-Item $exePath).Length
Write-OK "حجم الملف: $([math]::Round($exeSize/1024, 0)) KB"

if ($exeSize -lt 500000) {
    Write-Host "  ⚠️  تحذير: الملف أصغر من المتوقع! تأكد أن البناء صحيح." -ForegroundColor Yellow
    $confirm = Read-Host "  هل تريد المتابعة؟ (y/n)"
    if ($confirm -ne "y") { exit 1 }
}

# ─────────────────────────────────────────────
# الخطوة 4: حساب SHA256
# ─────────────────────────────────────────────
Write-Step "حساب SHA256"
$sha256 = (Get-FileHash $exePath -Algorithm SHA256).Hash
Write-OK "SHA256: $sha256"

# ─────────────────────────────────────────────
# الخطوة 5: نسخ الـ .bin
# ─────────────────────────────────────────────
Write-Step "تحديث ChickenDist.bin"
Copy-Item $exePath -Destination $BIN_DEST -Force
$binSize = (Get-Item $BIN_DEST).Length
Write-OK "تم نسخ ChickenDist.bin — $([math]::Round($binSize/1024, 0)) KB"

# ─────────────────────────────────────────────
# الخطوة 6: تحديث update.txt
# ─────────────────────────────────────────────
Write-Step "تحديث update.txt"
$updateContent = @"
version=$VERSION
url=$UPDATE_URL
sha256=$sha256
changelog=$CHANGELOG
"@
[System.IO.File]::WriteAllText($UPDATE_TXT, $updateContent, [System.Text.Encoding]::UTF8)
Write-OK "update.txt محدَّث → version=$VERSION"

# ─────────────────────────────────────────────
# الخطوة 7: Git commit + push
# ─────────────────────────────────────────────
Write-Step "Git commit & push"

Set-Location $REPO_ROOT

git add ChickenDist.bin update.txt
if ($LASTEXITCODE -ne 0) { Write-Fail "فشل git add" }
Write-OK "git add ✓"

$commitMsg = "deploy: v$VERSION — $([System.DateTime]::Now.ToString('yyyy-MM-dd HH:mm'))"
git commit -m $commitMsg
if ($LASTEXITCODE -ne 0) { Write-Fail "فشل git commit" }
Write-OK "git commit ✓"

git push origin main
Write-OK "git push ✓"

# ─────────────────────────────────────────────
# ✅ الانتهاء
# ─────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ تم النشر بنجاح! الإصدار v$VERSION" -ForegroundColor Green
Write-Host "  📦 الحجم : $([math]::Round($binSize/1024, 0)) KB" -ForegroundColor Green
Write-Host "  🔐 SHA256: $sha256" -ForegroundColor Green
Write-Host "  🕐 الوقت : $([System.DateTime]::Now.ToString('dd/MM/yyyy HH:mm:ss'))" -ForegroundColor Green
Write-Host "══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

# تنظيف مجلد البناء المؤقت
Remove-Item $OUT_DIR -Recurse -Force -ErrorAction SilentlyContinue

# Read-Host "اضغط Enter للإغلاق"
