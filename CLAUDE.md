# CLAUDE.md - EMAwave34

Project agent contract for `kirkvan/EMAwave34`. NinjaTrader 8 NinjaScript C# strategy for
futures, built around an EMA-wave entry model with HMA, MACD, and VROC service filters and a
floating control/info panel.

Universal NinjaScript rules - safety (no compile/launch/Playback/Analyzer runs), git
handling, ASCII/no-BOM source, and the NT8-owned `#region NinjaScript generated code` - live in
the shared `..\NinjaTrader Documentation\CLAUDE.md` and apply here. This file covers only
EMAwave34 specifics.

## Not yet deployed

- **The Colors group's dialog Orders** (commit `017e69e`, 2026-09-24). "9. Color
  for Falling MA" and "10. Color for Zone" shared Order 7 with "8. Color for
  Rising MA"; they are now 8 and 9. Dialog order only, no trading change. The
  account holder chose not to deploy it yet. The copy in NinjaTrader still has
  the collision until `deploy.ps1 -NoPause` runs. Remove this entry once it has.

## Documentation map

- `STRATEGY_REFERENCE.md` (in-repo) - agent handoff notes and project-specific guidance;
  consult before non-trivial work. (Was previously `AGENTS.md`.)
- `README.md` - project overview and change history.
- Shared NinjaTrader references live in the My Work-level `..\NinjaTrader Documentation\`
  folder (NT8 order/state, NinjaScript conventions, Analyzer troubleshooting). Read the NT8
  docs before order-lifecycle or state-transition work.

## Layout

- `EMAwave34Strategy.cs`, `EMAwave34.cs` - strategy implementation.
- `EMAwave34ControlPanel.cs`, `EMAwave34InfoPanel.cs` - floating WPF panels.
- `EMAwave34ServiceHmaFilter.cs`, `EMAwave34ServiceMacdFilter.cs`,
  `EMAwave34ServiceVrocFilter.cs` - entry filter services.
- `EMAwave34ServiceLogger.cs` - logging service.
- `deploy.ps1` - copies the source to NinjaTrader's Custom folders; run it as
  `deploy.ps1 -NoPause`. It refuses while a strategy is enabled or any `.cs`
  is uncommitted, and fails unless NinjaTrader then rebuilds the assembly.

## Gate

`tests\preflight.ps1` calls the shared minimal gate,
`..\NinjaTrader Documentation\tools\project-preflight.ps1`: ASCII and no BOM,
a syntax parse with NinjaTrader's own Roslyn, a unique `[Display]` Order within
each group, documentation links, PowerShell static analysis, and the version
rule. The pre-commit hook runs it on every commit here. It proves syntax, not a
compile, and nothing about trading behavior - F5 and a SIM run remain the
acceptance tests.
