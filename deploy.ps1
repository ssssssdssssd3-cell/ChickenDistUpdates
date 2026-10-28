﻿# ============================================================
#  Auto-Build and Deploy Script - ChickenDist
# ============================================================

$ErrorActionPreference = "Stop"

$VERSION = "4.4.24";
$CHANGELOG = "v4.4.24 - $([System.DateTime]::Now.ToString('yyyy-MM-dd')): إعادة تصميم وتنسيق شاشة إعدادات الطابعات ونماذج الفواتير والباركود بالكامل بألوان عصرية هادئة ومتناسقة وترتيب شبكي متوازن ومريح للعين.";
$UPDATE_URL = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist.bin"

# ────────────────────────────────────────────────────────────
#  Paths
# ────────────────────────────────────────────────────────────
$REPO_ROOT    = $PSScriptRoot
$PROJECT_DIR  = $REPO_ROOT
$CSPROJ       = Join-Path $PROJECT_DIR "ChickenDist.csproj"
$OUT_DIR      = Join-Path $REPO_ROOT "_build_output"
$BIN_DEST     = Join-Path $REPO_ROOT "ChickenDist.bin"
$UPDATE_TXT   = Join-Path $REPO_ROOT "update.txt"

# ------------------------------------------------------------

function Write-Step { param($msg) Write-Host "`n===[ $msg ]===" -ForegroundColor Cyan }
function Write-OK   { param($msg) Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "  [FAIL] $msg" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "==========================================" -ForegroundColor Blue
Write-Host "   ChickenDist Deploy Script v$VERSION  " -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Blue

# Step 1: Verify Project
Write-Step "Verifying Project"
if (-not (Test-Path $CSPROJ)) {
    Write-Fail "Project not found at: $CSPROJ"
}
Write-OK "Project found: $CSPROJ"

# Step 1.5: Patch Version in UpdateManager.cs
Write-Step "Patching Version in UpdateManager.cs to $VERSION"
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
$updateManagerPaths = @(
    "$PROJECT_DIR\Core\UpdateManager.cs",
    "$REPO_ROOT\Core\UpdateManager.cs"
)
foreach ($path in $updateManagerPaths) {
    if (Test-Path $path) {
        $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        $content = $content -replace 'public const string CurrentVersion = "[^"]+";', "public const string CurrentVersion = `"$VERSION`";"
        [System.IO.File]::WriteAllText($path, $content, $utf8Bom)
        Write-OK "Patched $path"
    }
}

# Step 2: Build
Write-Step "Building (Release)"
if (Test-Path $OUT_DIR) { 
    try { Remove-Item $OUT_DIR -Recurse -Force -ErrorAction SilentlyContinue } catch {}
}
New-Item -ItemType Directory -Path $OUT_DIR -Force | Out-Null

$msbuildExe = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if (Test-Path $msbuildExe) {
    & $msbuildExe $CSPROJ /p:Configuration=Release /t:Build /v:minimal /nologo /nodeReuse:false
} else {
    & dotnet build $CSPROJ -c Release /v:minimal /nologo /m
}

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed!"
}

Write-OK "Build successful"

# Step 2.5: Ensure Obfuscar is executed if needed
$obfuscarExe = "$env:USERPROFILE\.nuget\packages\obfuscar\2.2.38\tools\Obfuscar.Console.exe"
$obfuscatedSource = "$PROJECT_DIR\bin\Release\net48\Obfuscated\ProSoft.exe"
if (Test-Path $obfuscatedSource) {
    Remove-Item $obfuscatedSource -Force -ErrorAction SilentlyContinue
}
if (Test-Path $obfuscarExe) {
    Push-Location $PROJECT_DIR
    try {
        & $obfuscarExe -s "$PROJECT_DIR\obfuscar.xml"
        Write-OK "Obfuscar completed successfully"
    } finally {
        Pop-Location
    }
}

# Step 3: Verify Output
Write-Step "Checking EXE"
$exePath = Join-Path $OUT_DIR "ProSoft.exe"

# Overwrite with the obfuscated EXE from Obfuscated directory to secure the binary
$obfuscatedSource = "$PROJECT_DIR\bin\Release\net48\Obfuscated\ProSoft.exe"
if (Test-Path $obfuscatedSource) {
    Copy-Item $obfuscatedSource -Destination $exePath -Force
    Write-OK "Applied Obfuscation to output binary successfully!"
} else {
    Write-Fail "Obfuscated binary not found at $obfuscatedSource"
}

$exeSize = (Get-Item $exePath).Length
Write-OK "File size: $([math]::Round($exeSize/1024, 0)) KB"

if ($exeSize -lt 500000) {
    Write-Fail "File is too small! Build might be incomplete."
}

# Step 5: Copy Bin
Write-Step "Updating ChickenDist.bin"
Copy-Item $exePath -Destination $BIN_DEST -Force
$binSize = (Get-Item $BIN_DEST).Length
Write-OK "Copied ChickenDist.bin - $([math]::Round($binSize/1024, 0)) KB"

# Step 4 (AFTER copy): Calculate SHA256 from the final .bin file to ensure accuracy
Write-Step "Calculating SHA256"
$sha256 = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash([System.IO.File]::ReadAllBytes($BIN_DEST))).Replace("-", "")
Write-OK "SHA256: $sha256"

# Step 5.5: Copy MobileApp files to release destinations & Firebase bot public folder
Write-Step "Updating MobileApp release folders & bot public folder"
$baseRoot = Split-Path (Split-Path $REPO_ROOT -Parent) -Parent
$releaseDestinations = @(
    (Join-Path $baseRoot "FINAL_RELEASE\ChickenDist_Program\MobileApp"),
    "D:\prosoft\MobileApp"
)
foreach ($dest in $releaseDestinations) {
    if (Test-Path (Split-Path $dest -Parent)) {
        if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest -Force | Out-Null }
        Copy-Item -Path (Join-Path $REPO_ROOT "MobileApp\*") -Destination $dest -Force -Recurse
        Write-OK "Synced MobileApp files to $dest"
    }
}

$botPublicMobile = Join-Path $REPO_ROOT "bot\public\mobile.html"
Copy-Item -Path (Join-Path $REPO_ROOT "MobileApp\index.html") -Destination $botPublicMobile -Force
Write-OK "Synced MobileApp/index.html to bot/public/mobile.html"

$botPublicStore = Join-Path $REPO_ROOT "bot\public\store.html"
Copy-Item -Path (Join-Path $REPO_ROOT "MobileApp\store.html") -Destination $botPublicStore -Force
Write-OK "Synced MobileApp/store.html to bot/public/store.html"

$botPublicUpload = Join-Path $REPO_ROOT "bot\public\upload.html"
Copy-Item -Path (Join-Path $REPO_ROOT "MobileApp\upload.html") -Destination $botPublicUpload -Force
Write-OK "Synced MobileApp/upload.html to bot/public/upload.html"

# PWA Assets (manifests, logo, service worker)
$pwaAssets = @("manifest.json", "manifest_store.json", "logo.png", "sw.js")
foreach ($asset in $pwaAssets) {
    $src = Join-Path $REPO_ROOT "MobileApp\$asset"
    $dst = Join-Path $REPO_ROOT "bot\public\$asset"
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination $dst -Force
        Write-OK "Synced MobileApp/$asset to bot/public/$asset"
    }
}

# Sync Product Images
$mobileImagesSrc = Join-Path $REPO_ROOT "MobileApp\images"
$botImagesDst = Join-Path $REPO_ROOT "bot\public\images"
if (Test-Path $mobileImagesSrc) {
    if (-not (Test-Path $botImagesDst)) { New-Item -ItemType Directory -Path $botImagesDst -Force | Out-Null }
    Copy-Item -Path "$mobileImagesSrc\*" -Destination $botImagesDst -Force -Recurse
    Write-OK "Synced MobileApp/images to bot/public/images"
}

# Step 5.6: Copy final EXE to FINAL_RELEASE directory
$finalReleaseExe = Join-Path $baseRoot "FINAL_RELEASE\ChickenDist_Program\ChickenDist.exe"
if (Test-Path (Split-Path $finalReleaseExe -Parent)) {
    Write-Step "Updating FINAL_RELEASE"
    try {
        Get-Process -Name "ChickenDist", "ProSoft" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 800
        Copy-Item $exePath -Destination $finalReleaseExe -Force
        Copy-Item $exePath -Destination (Join-Path (Split-Path $finalReleaseExe -Parent) "ProSoft.exe") -Force
        Write-OK "Copied final Release EXE to $finalReleaseExe and ProSoft.exe"

        $qrDllSrc = Join-Path $REPO_ROOT "QRCoder.dll"
        $qrDllDest = Join-Path (Split-Path $finalReleaseExe -Parent) "QRCoder.dll"
        if (Test-Path $qrDllSrc) {
            Copy-Item $qrDllSrc -Destination $qrDllDest -Force
            Write-OK "Copied QRCoder.dll to $(Split-Path $finalReleaseExe -Parent)"
        }
    } catch {
        Write-Host "  [WARN] Could not overwrite running ChickenDist.exe in FINAL_RELEASE" -ForegroundColor Yellow
    }
}

# Step 6: Update update.txt
$changelogText = $CHANGELOG
# Add a cache-busting timestamp to the download URL so GitHub CDN always serves fresh content
$cbTimestamp = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$cacheBustedDlUrl = "${UPDATE_URL}?v=${VERSION}&t=${cbTimestamp}"
$updateContent  = "version=$VERSION`r`n"
$updateContent += "url=$UPDATE_URL`r`n"
$updateContent += "download=$cacheBustedDlUrl`r`n"
$updateContent += "sha256=$sha256`r`n"
$updateContent += "changelog=$changelogText`r`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($UPDATE_TXT, $updateContent, $utf8NoBom)
Write-OK "update.txt updated -> version=$VERSION"

# Step 7: Git commit + push
Write-Step "Git commit & push"
Set-Location $REPO_ROOT

$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"

# Ensure C# source files are untracked from the public update repo index (kept locally on disk)
git rm -r --cached --ignore-unmatch Core DAL Forms Services Models Properties *.cs *.csproj *.sln obfuscar.xml 2>$null | Out-Null

git add -A
if ($LASTEXITCODE -ne 0) { Write-Fail "git add failed" }
Write-OK "git add ok"

$commitMsg = "deploy: v$VERSION - " + [System.DateTime]::Now.ToString("yyyy-MM-dd HH:mm")
git commit -m $commitMsg
if ($LASTEXITCODE -ne 0) { Write-Host "  [INFO] git commit note or nothing to commit" }
else { Write-OK "git commit ok" }

$pushSuccess = $false
for ($attempt = 1; $attempt -le 3; $attempt++) {
    git push origin main
    if ($LASTEXITCODE -eq 0) {
        $pushSuccess = $true
        break
    }
    Write-Host "  [WARN] git push attempt $attempt failed, retrying in 3 seconds..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
}
if (-not $pushSuccess) { Write-Fail "git push failed after 3 attempts" }
Write-OK "git push ok"

$ErrorActionPreference = $prevEAP

# Finish
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Deploy Successful! Version v$VERSION" -ForegroundColor Green
Write-Host "  Size : $([math]::Round($binSize/1024, 0)) KB" -ForegroundColor Green
Write-Host "  SHA256: $sha256" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

Remove-Item $OUT_DIR -Recurse -Force -ErrorAction SilentlyContinue
