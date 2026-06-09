# ============================================================
#  ðŸš€ Ø³ÙƒØ±ÙŠØ¨Øª Ø§Ù„Ø¨Ù†Ø§Ø¡ ÙˆØ§Ù„Ù†Ø´Ø± Ø§Ù„ØªÙ„Ù‚Ø§Ø¦ÙŠ â€” ChickenDist
#  Ø§Ù„Ø§Ø³ØªØ®Ø¯Ø§Ù…: Ø§Ù†Ù‚Ø± Ø¨Ø§Ù„Ø²Ø± Ø§Ù„Ø£ÙŠÙ…Ù† â†’ Run with PowerShell
#  Ø£Ùˆ Ù…Ù† Ø§Ù„Ù€ Terminal: .\deploy.ps1
# ============================================================

$ErrorActionPreference = "Stop"

# ————————————————————————————————————————————————————————
# ⚙️ الإعدادات — عدِّل هنا فقط عند كل إصدار جديد
# ————————————————————————————————————————————————————————
$VERSION   = "1.3.1"
$CHANGELOG = Get-Content -Path (Join-Path $PSScriptRoot "changelog.txt") -Raw -Encoding UTF8
$UPDATE_URL = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist.bin"

$PROJECT_DIR  = Join-Path $REPO_ROOT "ChickenDist"                 # âœ… Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ Ø§Ù„ØµØ­ÙŠØ­
$CSPROJ       = Join-Path $PROJECT_DIR "ChickenDist.csproj"        # Ù…Ù„Ù Ø§Ù„Ù…Ø´Ø±ÙˆØ¹
$OUT_DIR      = Join-Path $REPO_ROOT "_build_output"               # Ù…Ø¬Ù„Ø¯ Ù†Ø§ØªØ¬ Ø§Ù„Ø¨Ù†Ø§Ø¡
$BIN_DEST     = Join-Path $REPO_ROOT "ChickenDist.bin"             # Ø§Ù„Ù…Ù„Ù Ø§Ù„Ù†Ù‡Ø§Ø¦ÙŠ
$UPDATE_TXT   = Join-Path $REPO_ROOT "update.txt"                  # Ù…Ù„Ù Ø§Ù„ØªØ­Ø¯ÙŠØ«

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

function Write-Step { param($msg) Write-Host "`n===[ $msg ]===" -ForegroundColor Cyan }
function Write-OK   { param($msg) Write-Host "  âœ… $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "  âŒ $msg" -ForegroundColor Red; Read-Host "Ø§Ø¶ØºØ· Enter Ù„Ù„Ø®Ø±ÙˆØ¬"; exit 1 }

Write-Host ""
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Blue
Write-Host "   ðŸ£ ChickenDist Deploy Script v$VERSION  " -ForegroundColor Yellow
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Blue

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 1: Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ Ø§Ù„ØµØ­ÙŠØ­
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø§Ù„Ù…Ø´Ø±ÙˆØ¹"
if (-not (Test-Path $CSPROJ)) {
    Write-Fail "Ù„Ù… ÙŠÙØ¹Ø«Ø± Ø¹Ù„Ù‰ Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ ÙÙŠ: $CSPROJ"
}
Write-OK "Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ Ù…ÙˆØ¬ÙˆØ¯: $CSPROJ"
Write-Host "  âš ï¸  Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ Ø§Ù„ØµØ­ÙŠØ­ Ù‡Ùˆ ChickenDist\ (Ø§Ù„Ø¯Ø§Ø®Ù„ÙŠ) ÙˆÙ„ÙŠØ³ Ø§Ù„Ø¬Ø°Ø±!" -ForegroundColor Yellow

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 2: Ø§Ù„Ø¨Ù†Ø§Ø¡
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "Ø§Ù„Ø¨Ù†Ø§Ø¡ (dotnet publish)"
if (Test-Path $OUT_DIR) { Remove-Item $OUT_DIR -Recurse -Force }

$buildResult = dotnet publish $CSPROJ -c Release -f net48 -o $OUT_DIR --nologo 2>&1
$buildOutput = $buildResult -join "`n"

if ($LASTEXITCODE -ne 0) {
    Write-Host $buildOutput -ForegroundColor Red
    Write-Fail "ÙØ´Ù„ Ø§Ù„Ø¨Ù†Ø§Ø¡! Ø±Ø§Ø¬Ø¹ Ø§Ù„Ø£Ø®Ø·Ø§Ø¡ Ø£Ø¹Ù„Ø§Ù‡."
}

$errors = $buildResult | Where-Object { $_ -match "error" -and $_ -notmatch "warning" }
if ($errors) {
    Write-Host ($errors -join "`n") -ForegroundColor Red
    Write-Fail "ÙŠÙˆØ¬Ø¯ Ø£Ø®Ø·Ø§Ø¡ ÙÙŠ Ø§Ù„Ø¨Ù†Ø§Ø¡!"
}

Write-OK "Ø§Ù„Ø¨Ù†Ø§Ø¡ Ù†Ø§Ø¬Ø­"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 3: Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ø§Ù„Ù…Ù„Ù Ø§Ù„Ù†Ø§ØªØ¬
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "Ø§Ù„ØªØ­Ù‚Ù‚ Ù…Ù† Ù…Ù„Ù Ø§Ù„Ù€ EXE"
$exePath = Join-Path $OUT_DIR "ChickenDist.exe"
if (-not (Test-Path $exePath)) {
    Write-Fail "Ù„Ù… ÙŠÙØ¹Ø«Ø± Ø¹Ù„Ù‰ ChickenDist.exe ÙÙŠ Ù…Ø¬Ù„Ø¯ Ø§Ù„Ø¨Ù†Ø§Ø¡!"
}

$exeSize = (Get-Item $exePath).Length
Write-OK "Ø­Ø¬Ù… Ø§Ù„Ù…Ù„Ù: $([math]::Round($exeSize/1024, 0)) KB"

if ($exeSize -lt 500000) {
    Write-Host "  âš ï¸  ØªØ­Ø°ÙŠØ±: Ø§Ù„Ù…Ù„Ù Ø£ØµØºØ± Ù…Ù† Ø§Ù„Ù…ØªÙˆÙ‚Ø¹! ØªØ£ÙƒØ¯ Ø£Ù† Ø§Ù„Ø¨Ù†Ø§Ø¡ ØµØ­ÙŠØ­." -ForegroundColor Yellow
    $confirm = Read-Host "  Ù‡Ù„ ØªØ±ÙŠØ¯ Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø©ØŸ (y/n)"
    if ($confirm -ne "y") { exit 1 }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 4: Ø­Ø³Ø§Ø¨ SHA256
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "Ø­Ø³Ø§Ø¨ SHA256"
$sha256 = (Get-FileHash $exePath -Algorithm SHA256).Hash
Write-OK "SHA256: $sha256"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 5: Ù†Ø³Ø® Ø§Ù„Ù€ .bin
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "ØªØ­Ø¯ÙŠØ« ChickenDist.bin"
Copy-Item $exePath -Destination $BIN_DEST -Force
$binSize = (Get-Item $BIN_DEST).Length
Write-OK "ØªÙ… Ù†Ø³Ø® ChickenDist.bin - $([math]::Round($binSize/1024, 0)) KB"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 6: ØªØ­Ø¯ÙŠØ« update.txt
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "ØªØ­Ø¯ÙŠØ« update.txt"
$updateContent = @"
version=$VERSION
url=$UPDATE_URL
sha256=$sha256
changelog=$CHANGELOG
"@
[System.IO.File]::WriteAllText($UPDATE_TXT, $updateContent, [System.Text.Encoding]::UTF8)
Write-OK "update.txt Ù…Ø­Ø¯ÙŽÙ‘Ø« â†’ version=$VERSION"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Ø§Ù„Ø®Ø·ÙˆØ© 7: Git commit + push
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Step "Git commit & push"

Set-Location $REPO_ROOT

git add ChickenDist.bin update.txt
if ($LASTEXITCODE -ne 0) { Write-Fail "ÙØ´Ù„ git add" }
Write-OK "git add âœ“"

$commitMsg = "deploy: v$VERSION - $([System.DateTime]::Now.ToString('yyyy-MM-dd HH:mm'))"
git commit -m $commitMsg
if ($LASTEXITCODE -ne 0) { Write-Fail "ÙØ´Ù„ git commit" }
Write-OK "git commit âœ“"

git push origin main
Write-OK "git push âœ“"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# âœ… Ø§Ù„Ø§Ù†ØªÙ‡Ø§Ø¡
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Green
Write-Host "  âœ… ØªÙ… Ø§Ù„Ù†Ø´Ø± Ø¨Ù†Ø¬Ø§Ø­! Ø§Ù„Ø¥ØµØ¯Ø§Ø± v$VERSION" -ForegroundColor Green
Write-Host "  ðŸ“¦ Ø§Ù„Ø­Ø¬Ù… : $([math]::Round($binSize/1024, 0)) KB" -ForegroundColor Green
Write-Host "  ðŸ” SHA256: $sha256" -ForegroundColor Green
Write-Host "  ðŸ• Ø§Ù„ÙˆÙ‚Øª : $([System.DateTime]::Now.ToString('dd/MM/yyyy HH:mm:ss'))" -ForegroundColor Green
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Green
Write-Host ""

# ØªÙ†Ø¸ÙŠÙ Ù…Ø¬Ù„Ø¯ Ø§Ù„Ø¨Ù†Ø§Ø¡ Ø§Ù„Ù…Ø¤Ù‚Øª
Remove-Item $OUT_DIR -Recurse -Force -ErrorAction SilentlyContinue

# Read-Host "Ø§Ø¶ØºØ· Enter Ù„Ù„Ø¥ØºÙ„Ø§Ù‚"
