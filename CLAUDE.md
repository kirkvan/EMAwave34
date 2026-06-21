# CLAUDE.md - EMAwave34

Project agent contract for `kirkvan/EMAwave34`. NinjaTrader 8 NinjaScript C# strategy for
futures, built around an EMA-wave entry model with HMA, MACD, and VROC service filters and a
floating control/info panel.

Universal NinjaScript rules - safety (no deploy/compile/launch/Playback/Analyzer runs), git
handling, ASCII/no-BOM source, and the NT8-owned `#region NinjaScript generated code` - live in
the shared `..\..\NinjaTrader Documentation\CLAUDE.md` and apply here. This file covers only
EMAwave34 specifics.

## Documentation map

- `STRATEGY_REFERENCE.md` (in-repo) - agent handoff notes and project-specific guidance;
  consult before non-trivial work. (Was previously `AGENTS.md`.)
- `README.md` - project overview and change history.
- Shared NinjaTrader references live in the My Work-level `..\..\NinjaTrader Documentation\`
  folder (NT8 order/state, NinjaScript conventions, Analyzer troubleshooting). Read the NT8
  docs before order-lifecycle or state-transition work.

## Layout

- `EMAwave34Strategy.cs`, `EMAwave34.cs` - strategy implementation.
- `EMAwave34ControlPanel.cs`, `EMAwave34InfoPanel.cs` - floating WPF panels.
- `EMAwave34ServiceHmaFilter.cs`, `EMAwave34ServiceMacdFilter.cs`,
  `EMAwave34ServiceVrocFilter.cs` - entry filter services.
- `EMAwave34ServiceLogger.cs` - logging service.
- `deploy.ps1` - manual copy script for NinjaTrader Custom folders. Do not run it.
