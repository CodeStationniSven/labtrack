#Requires -Version 5.1
<#
.SYNOPSIS
    Push update via Pull Request workflow
.DESCRIPTION
    - Creates feature branch (kung nasa main ka)
    - Commits changes
    - Pushes to GitHub
    - Opens PR URL in browser
    - QA reviews + approves + comments
    - Merge after approval
.PARAMETER Message
    Commit message (required unless -Interactive)
.PARAMETER Branch
    Custom branch name (default: feature/update-<timestamp>)
.PARAMETER Base
    Base branch (default: main)
#>
param(
    [string]$Message,
    [string]$Branch,
    [string]$Base = "main",
    [switch]$Interactive
)

$ErrorActionPreference = "Continue"

# Move to repo root
cd C:\labtrack\backend

Write-Host ""
Write-Host ("=" * 72) -ForegroundColor Cyan
Write-Host "  PUSH UPDATE -> PULL REQUEST" -ForegroundColor Cyan
Write-Host ("=" * 72) -ForegroundColor Cyan

# ========== 1. STATUS ==========
Write-Host ""
Write-Host "[1/6] Checking status..." -ForegroundColor Yellow
$status = git status --short
if (-not $status) {
    Write-Host "  [INFO] Walang changes. Nothing to commit." -ForegroundColor Yellow
    Write-Host "  Did you save your files? (Ctrl+S)" -ForegroundColor Gray
    return
}
$changeCount = ($status | Measure-Object).Count
Write-Host "  Changes: $changeCount entries" -ForegroundColor Gray
$status | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

# ========== 2. JUNK CHECK ==========
Write-Host ""
Write-Host "[2/6] Junk check..." -ForegroundColor Yellow
$junk = $status | Select-String "(\.ps1$|venv/|/new$|\.AssetId|\.LaboratoryId|\.bak-)"
if ($junk) {
    Write-Host "  [WARN] Unwanted files detected:" -ForegroundColor Yellow
    $junk | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    $ans = Read-Host "  Continue? (y/N)"
    if ($ans -notmatch '^[yY]') { Write-Host "  Cancelled." -ForegroundColor Red; return }
} else {
    Write-Host "  [OK] Clean" -ForegroundColor Green
}

# ========== 3. BRANCH ==========
Write-Host ""
Write-Host "[3/6] Branch setup..." -ForegroundColor Yellow
$currentBranch = (git branch --show-current).Trim()

if ($currentBranch -in @("main","master")) {
    if (-not $Branch) {
        $Branch = "feature/update-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    }
    Write-Host "  On '$currentBranch' -> creating new branch: $Branch" -ForegroundColor Cyan
    git checkout -b $Branch 2>&1 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    if ($LASTEXITCODE -ne 0) { Write-Host "  [FAIL] Branch creation failed" -ForegroundColor Red; return }
} else {
    if ($Branch) {
        Write-Host "  Already on '$currentBranch' - ignoring -Branch param" -ForegroundColor Gray
    }
    $Branch = $currentBranch
    Write-Host "  Using current branch: $Branch" -ForegroundColor Green
}

# ========== 4. ADD + COMMIT ==========
Write-Host ""
Write-Host "[4/6] Staging + commit..." -ForegroundColor Yellow
git add .

$staged = @(git diff --cached --name-only)
Write-Host "  Staged: $($staged.Count) files" -ForegroundColor Gray

if ($staged.Count -eq 0) {
    Write-Host "  [INFO] Walang staged. Baka naka-stage na lahat." -ForegroundColor Yellow
    return
}

if (-not $Message -or $Interactive) {
    Write-Host ""
    Write-Host "  Commit message guide:" -ForegroundColor Cyan
    Write-Host "    feat: <new feature>" -ForegroundColor DarkGray
    Write-Host "    fix:  <bug fix>" -ForegroundColor DarkGray
    Write-Host "    refactor: <code restructure>" -ForegroundColor DarkGray
    Write-Host "    docs: <documentation>" -ForegroundColor DarkGray
    Write-Host "    test: <tests>" -ForegroundColor DarkGray
    Write-Host "    chore: <maintenance>" -ForegroundColor DarkGray
    Write-Host ""
    $Message = Read-Host "  Commit message"
}

if (-not $Message) {
    $Message = "update: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    Write-Host "  Using default: $Message" -ForegroundColor Yellow
}

$author = "$(git config user.name) <$(git config user.email)>"
Write-Host "  Author: $author" -ForegroundColor Gray
Write-Host ""

git commit -m $Message
if ($LASTEXITCODE -ne 0) { Write-Host "  [FAIL] Commit failed" -ForegroundColor Red; return }
Write-Host "  [OK] Committed" -ForegroundColor Green

# ========== 5. PUSH ==========
Write-Host ""
Write-Host "[5/6] Pushing to GitHub..." -ForegroundColor Yellow
Write-Host "  (if prompted: username=CodeStationniSven, password=Personal Access Token)" -ForegroundColor Gray
Write-Host ""

git push -u origin $Branch
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [FAIL] Push failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Common fixes:" -ForegroundColor Yellow
    Write-Host "    - Auth: gumamit ng Personal Access Token (hindi password)" -ForegroundColor Gray
    Write-Host "      https://github.com/settings/tokens" -ForegroundColor DarkGray
    Write-Host "    - Retry: git push -u origin $Branch" -ForegroundColor DarkGray
    return
}
Write-Host "  [OK] Pushed" -ForegroundColor Green

# ========== 6. PR URL ==========
Write-Host ""
Write-Host "[6/6] Generating PR URL..." -ForegroundColor Yellow

$remote = git remote get-url origin
$prUrl = $null
if ($remote -match 'github\.com[:/]([^/]+)/([^/.]+?)(\.git)?$') {
    $owner = $Matches[1]
    $repo  = $Matches[2]
    $encodedBranch = [uri]::EscapeDataString($Branch)
    $prUrl = "https://github.com/$owner/$repo/compare/$Base...$Branch`?expand=1"

    Write-Host ""
    Write-Host ("=" * 72) -ForegroundColor Green
    Write-Host "  READY FOR PULL REQUEST" -ForegroundColor Green
    Write-Host ("=" * 72) -ForegroundColor Green
    Write-Host ""
    Write-Host "  Branch : $Branch" -ForegroundColor Cyan
    Write-Host "  Commit : $(git log -1 --format='%h - %s')" -ForegroundColor Cyan
    Write-Host "  Author : $(git log -1 --format='%an <%ae>')" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Open this URL to create PR:" -ForegroundColor Yellow
    Write-Host "    $prUrl" -ForegroundColor White
    Write-Host ""
    Write-Host "  Opening browser..." -ForegroundColor Gray
    Start-Process $prUrl
    Write-Host ""
    Write-Host "  Sa PR page:" -ForegroundColor Cyan
    Write-Host "    1. Title: $Message" -ForegroundColor Gray
    Write-Host "    2. Description: ipaliwanag ang changes" -ForegroundColor Gray
    Write-Host "    3. Reviewers: pumili ng QA" -ForegroundColor Gray
    Write-Host "    4. Click 'Create pull request'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  QA reviews + comments + approves -> Merge" -ForegroundColor Green
} else {
    Write-Host "  [WARN] Hindi ma-parse ang remote URL: $remote" -ForegroundColor Yellow
    Write-Host "  Manually open: https://github.com/CodeStationniSven/labtrack/pulls" -ForegroundColor Gray
}

Write-Host ""
