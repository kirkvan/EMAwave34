# EMAwave34 Deployment Script
# Copies EMAwave34 indicator + strategy source files to NinjaTrader target folders
#
# NOTE: This script performs an ASCII-only + UTF-8 BOM guard
# so you can just run deploy.ps1 before compiling.

param(
    # Skip every keypress, so the deploy can run unattended.
    [switch]$NoPause,

    # Deploy even though a strategy is enabled. See Assert-NtNoEnabledStrategy
    # in NinjaTrader Documentation\tools\nt-compile.ps1 for why that is refused.
    [switch]$AllowEnabledStrategy,

    # Deploy even though a .cs file differs from the last commit. See
    # Assert-NtSourceCommitted in the same file.
    [switch]$AllowDirtySource
)

# --- Deploy guard, shared with every project (added 2026-09-24) ---------------
# NinjaTrader recompiles the WHOLE custom assembly about four seconds after any
# file lands in bin\Custom - underneath any running strategy, logging nothing.
# So nothing below may write or delete there until both checks pass: no
# strategy is enabled, and every .cs in this project is committed. Both live in
# NinjaTrader Documentation\tools\nt-compile.ps1, found by walking up. Its
# absence is a refusal, never a skip.
$ntTools = $null
$ntProbe = $PSScriptRoot
while (-not [string]::IsNullOrEmpty($ntProbe)) {
    $ntCandidate = Join-Path (Join-Path (Join-Path $ntProbe 'NinjaTrader Documentation') 'tools') 'nt-compile.ps1'
    if (Test-Path -LiteralPath $ntCandidate) { $ntTools = $ntCandidate; break }
    $ntProbe = Split-Path $ntProbe -Parent
}
if ($null -eq $ntTools) {
    Write-Host 'DEPLOY REFUSED: cannot find nt-compile.ps1 in any parent folder' -ForegroundColor Red
    Write-Host '  Clone the NinjaTrader Documentation project alongside this one.' -ForegroundColor Yellow
    exit 1
}
. $ntTools
Assert-NtNoEnabledStrategy -Allow:$AllowEnabledStrategy
# This project keeps its source at its root, not in src\, so the deployed source
# is named explicitly: every .cs file, at any depth.
Assert-NtSourceCommitted -Root $PSScriptRoot -Paths @('*.cs') -Allow:$AllowDirtySource

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = if (-not [string]::IsNullOrEmpty($PSScriptRoot)) { $PSScriptRoot } else { (Get-Location).Path }

$DeployCopied = 0
$DeployMissing = 0

Write-Host "`n=== EMAwave34 Pre-Deploy Checks ===" -ForegroundColor Cyan
Write-Host "Running ASCII/BOM check..." -ForegroundColor Yellow

function Test-AsciiNoBom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $textExtensions = @('.cs', '.ps1')
    $textFilenames = @()

    $files = Get-ChildItem -Path $Root -Recurse -File | Where-Object {
        ($textExtensions -contains $_.Extension) -or ($textFilenames -contains $_.Name)
    }

    $failures = @()
    foreach ($file in $files) {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)

        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            $failures += "BOM: $($file.FullName)"
            continue
        }

        foreach ($b in $bytes) {
            if ($b -gt 0x7F) {
                $failures += "NONASCII: $($file.FullName)"
                break
            }
        }
    }

    return $failures
}

$asciiFailures = @(Test-AsciiNoBom -Root $repoRoot)
if ($asciiFailures.Count -gt 0) {
    Write-Host "ASCII/BOM check failed. Aborting deploy." -ForegroundColor Red
    foreach ($failure in $asciiFailures) {
        Write-Host "  $failure" -ForegroundColor Red
    }
    exit 1
}

$source = $repoRoot
$strategyTarget = "C:\Users\Administrator\Documents\NinjaTrader 8\bin\Custom\Strategies"
$indicatorTarget = "C:\Users\Administrator\Documents\NinjaTrader 8\bin\Custom\Indicators"
Write-Host "`n=== EMAwave34 Deployment ===" -ForegroundColor Cyan

# Strategy files
$strategyFiles = @(
    "EMAwave34Strategy.cs",
    "EMAwave34ServiceLogger.cs",
    "EMAwave34ServiceMacdFilter.cs",
    "EMAwave34ServiceVrocFilter.cs",
    "EMAwave34ServiceHmaFilter.cs",
    "EMAwave34InfoPanel.cs",
    "EMAwave34ControlPanel.cs"
)

# Indicator files
$indicatorFiles = @(
    "EMAwave34.cs"
)

# Copy strategy files
Write-Host "`nCopying strategy files..." -ForegroundColor Yellow
if (!(Test-Path $strategyTarget)) { New-Item -ItemType Directory -Path $strategyTarget -Force | Out-Null }
foreach ($file in $strategyFiles) {
    $sourcePath = Join-Path $source $file
    $targetPath = Join-Path $strategyTarget $file
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $targetPath -Force
        $DeployCopied++; Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        $DeployMissing++; Write-Host "  [MISSING] $file" -ForegroundColor Red
    }
}

# Copy indicator files
Write-Host "`nCopying indicator files..." -ForegroundColor Yellow
# Ensure indicator target directory exists
if (!(Test-Path $indicatorTarget)) { New-Item -ItemType Directory -Path $indicatorTarget -Force | Out-Null }
foreach ($file in $indicatorFiles) {
    $sourcePath = Join-Path $source $file
    $leaf = Split-Path $file -Leaf
    $targetPath = Join-Path $indicatorTarget $leaf
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $targetPath -Force
        $DeployCopied++; Write-Host "  [OK] $leaf" -ForegroundColor Green
    } else {
        $DeployMissing++; Write-Host "  [MISSING] $file" -ForegroundColor Red
    }
}

# Cleanup: remove legacy files if present on target
$legacyStrategyFiles = @(
    "34EMAwaveStrategy.cs"
)
foreach ($legacy in $legacyStrategyFiles) {
    $legacyPath = Join-Path $strategyTarget $legacy
    if (Test-Path $legacyPath) {
        Remove-Item $legacyPath -Force
        Write-Host "  [REMOVED] $legacy" -ForegroundColor Yellow
    }
}

$legacyIndicators = @(
    "34emawave.cs",
    "mahTrendGRaBer2.cs"
)
foreach ($legacy in $legacyIndicators) {
    $legacyPath = Join-Path $indicatorTarget $legacy
    if (Test-Path $legacyPath) {
        Remove-Item $legacyPath -Force
        Write-Host "  [REMOVED] $legacy" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "NinjaTrader compiles this automatically - F5 is not required." -ForegroundColor Yellow
Write-Host ""

# --- Deploy verification -----------------------------------------------------
# A deploy that copies nothing must FAIL, not print red text and return success.
# Octave's did exactly that: $source named a folder that did not exist, so every
# file reported [MISSING] and the script still exited 0, which looked like a
# successful run. Added 2026-09-07.
if ($DeployMissing -gt 0 -or $DeployCopied -eq 0) {
    Write-Host ""
    if ($DeployCopied -eq 0) {
        Write-Host "DEPLOY FAILED: no files were copied." -ForegroundColor Red
        Write-Host "  Nothing reached bin\Custom - check that the source path resolves." -ForegroundColor Yellow
    } else {
        Write-Host "DEPLOY FAILED: $DeployMissing file(s) missing, $DeployCopied copied." -ForegroundColor Red
    }
    exit 1
}

# --- Compile verification (added 2026-09-24) ----------------------------------
# Copying is not deploying: until NinjaTrader rebuilds the assembly the files on
# disk are inert, and a failed compile leaves the OLD code live while every file
# looks correct. Wait-NtCompile exits 1 unless the assembly ends up newer than
# every deployed source file.
Write-Host ""
Write-Host "Waiting for NinjaTrader to compile..." -ForegroundColor Yellow
Wait-NtCompile -Since $null
