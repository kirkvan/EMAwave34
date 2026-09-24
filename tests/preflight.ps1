# EMAwave34 - pre-flight gate.
#
# The checks live in NinjaTrader Documentation\tools\project-preflight.ps1,
# shared by every project without a gate of its own design, so an improvement
# there reaches all of them at once. Read that file for what is checked and
# what is not. The pre-commit hook runs this file for every commit here.
#
# Exit code 0 = all gates passed.

$shared = Join-Path $PSScriptRoot '..\..\NinjaTrader Documentation\tools\project-preflight.ps1'
if (-not (Test-Path -LiteralPath $shared)) {
    # Fail closed: without the shared gate nothing here is checked at all.
    Write-Host "  [FAIL] the shared gate is missing: $shared"
    Write-Host "=== PRE-FLIGHT FAILED (shared gate missing) ==="
    exit 1
}
& pwsh -NoProfile -File $shared -ProjectRoot (Split-Path $PSScriptRoot -Parent)
exit $LASTEXITCODE
