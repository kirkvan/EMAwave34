# EMAwave34 Deployment Script
# Copies EMAwave34 indicator + strategy source files to NinjaTrader target folders
#
# NOTE: This script performs an ASCII-only + UTF-8 BOM guard
# so you can just run deploy.ps1 before compiling.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = if (-not [string]::IsNullOrEmpty($PSScriptRoot)) { $PSScriptRoot } else { (Get-Location).Path }

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
        Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $file" -ForegroundColor Red
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
        Write-Host "  [OK] $leaf" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $file" -ForegroundColor Red
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
Write-Host "Now compile in NinjaTrader (F5)" -ForegroundColor Yellow
Write-Host ""
