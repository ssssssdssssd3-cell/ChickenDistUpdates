# ============================================================
#  ðŸš€ Auto-Build and Deploy Script - ChickenDist
# ============================================================

$ErrorActionPreference = "Stop"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# ────────────────────────────────────────────────────────────
# ⚙️ Settings
# ────────────────────────────────────────────────────────────
$VERSION   = "1.9.98"
$CHANGELOG = Get-Content -Path (Join-Path $PSScriptRoot "changelog.txt") -Raw -Encoding UTF8
$UPDATE_URL = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist.bin"

# ────────────────────────────────────────────────────────────
# 📁 Paths
# ────────────────────────────────────────────────────────────
$REPO_ROOT    = $PSScriptRoot
$PROJECT_DIR  = Join-Path $REPO_ROOT "ChickenDist"
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
$updateManagerPaths = @(
    "$PROJECT_DIR\Core\UpdateManager.cs",
    "$REPO_ROOT\Core\UpdateManager.cs"
)
foreach ($path in $updateManagerPaths) {
    if (Test-Path $path) {
        $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        $content = $content -replace 'public const string CurrentVersion = "[^"]+";', "public const string CurrentVersion = `"$VERSION`";"
        [System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
        Write-OK "Patched $path"
    }
}

# Step 2: Build
Write-Step "Building (dotnet publish)"
if (Test-Path $OUT_DIR) { Remove-Item $OUT_DIR -Recurse -Force }

$buildResult = dotnet publish $CSPROJ -c Release -f net48 -o $OUT_DIR --nologo 2>&1
$buildOutput = $buildResult -join "`n"

if ($LASTEXITCODE -ne 0) {
    Write-Host $buildOutput -ForegroundColor Red
    Write-Fail "Build failed!"
}

$errors = $buildResult | Where-Object { $_ -match "error" -and $_ -notmatch "warning" }
if ($errors) {
    Write-Host ($errors -join "`n") -ForegroundColor Red
    Write-Fail "Build has errors!"
}

Write-OK "Build successful"

# Step 3: Verify Output
Write-Step "Checking EXE"
$exePath = Join-Path $OUT_DIR "ChickenDist.exe"
if (-not (Test-Path $exePath)) {
    Write-Fail "ChickenDist.exe not found in build directory!"
}

$exeSize = (Get-Item $exePath).Length
Write-OK "File size: $([math]::Round($exeSize/1024, 0)) KB"

if ($exeSize -lt 500000) {
    Write-Fail "File is too small! Build might be incomplete."
}

# Step 4: Calculate SHA256
Write-Step "Calculating SHA256"
$sha256 = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash([System.IO.File]::ReadAllBytes($exePath))).Replace("-", "")
Write-OK "SHA256: $sha256"

# Step 5: Copy Bin
Write-Step "Updating ChickenDist.bin"
Copy-Item $exePath -Destination $BIN_DEST -Force
$binSize = (Get-Item $BIN_DEST).Length
Write-OK "Copied ChickenDist.bin - $([math]::Round($binSize/1024, 0)) KB"

# Step 6: Update update.txt
Write-Step "Updating update.txt"
$changelogText = "v$VERSION - $([System.DateTime]::Now.ToString('yyyy-MM-dd')): Bug fixes and UI improvements"
$updateContent  = "version=$VERSION`r`n"
$updateContent += "url=$UPDATE_URL`r`n"
$updateContent += "download=$UPDATE_URL`r`n"
$updateContent += "sha256=$sha256`r`n"
$updateContent += "changelog=$changelogText`r`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($UPDATE_TXT, $updateContent, $utf8NoBom)
Write-OK "update.txt updated -> version=$VERSION"

# Step 7: Git commit + push
Write-Step "Git commit & push"
Set-Location $REPO_ROOT

git add -A
if ($LASTEXITCODE -ne 0) { Write-Fail "git add failed" }
Write-OK "git add ok"

$commitMsg = "deploy: v$VERSION - " + [System.DateTime]::Now.ToString("yyyy-MM-dd HH:mm")
git commit -m $commitMsg
if ($LASTEXITCODE -ne 0) { Write-Fail "git commit failed" }
Write-OK "git commit ok"

git push origin main
if ($LASTEXITCODE -ne 0) { Write-Fail "git push failed" }
Write-OK "git push ok"

# Finish
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Deploy Successful! Version v$VERSION" -ForegroundColor Green
Write-Host "  Size : $([math]::Round($binSize/1024, 0)) KB" -ForegroundColor Green
Write-Host "  SHA256: $sha256" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

Remove-Item $OUT_DIR -Recurse -Force -ErrorAction SilentlyContinue



