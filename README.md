# EMAwave34 – Handoff for New Conversation

## Operational note
In `C:\Users\Administrator\Documents\EMAwave34`, stage all changes, create a commit with an appropriate message, then force‑push the local master branch so it overwrites both origin/main and origin/master, ensuring the remote repo kirkvan/EMAwave34 exactly matches the local folder.

## Current status
- **Control Panel** is working (Enable Strategy + Display Info Panel buttons).
- **Info Panel** toggles correctly and reappears; strategy enable toggle gates entries only.
- **MACD histogram + VROC filters** added as separate services and entry gates.
- **Analyzer/backtest adjustments** applied:
  - Indicator drawings suppressed when no `ChartControl` (Analyzer) to keep runs clean.
  - Info panel disposed on termination.
  - Trading-hours gate uses wall-clock **only** in realtime non-Playback; otherwise bar time.
## Latest changes (2026-02-09)
- **Indicator Visual renaming:** bar/outline color properties renamed to descriptive names (e.g., “Bullish Above EMA High”). Note: serialized property names changed, so older templates/workspaces may reset these colors.
- **Info Panel readability:** MACD/VROC lines are compact; `Hist`/`Value` now appear on an indented second line to reduce line length.
- **Filter logging clarity:** per-filter block logs replaced by a single `[ENTRY_DECISION]` line per signal; added one-time `[FILTER_READY]` logs when MACD/VROC warm up; added one-time `[FILTER] ... disabled` logs.
- **Session PnL baseline** resets when the strategy is enabled (in addition to session start), so the info panel reflects trades taken since enable.

## Latest logging (for debugging trade rule violations)
Conditional (EnableDebugLogging = true) logs now include:
- Per-bar EMA snapshot: `[BAR] Time=... Close=... EmaHigh=... EmaClose=... EmaLow=...`
- Filter decision snapshot (per signal): `[ENTRY_DECISION] Signal=Long/Short Allowed=... MACD(...) VROC(...)`
- Filter readiness (one-time): `[FILTER_READY] MACD/VROC ready ...`
- Filter disabled (one-time): `[FILTER] MACD/VROC disabled; gating bypassed.`
- Entry signals:
  - `[ENTRY_SIGNAL] Long/Short` with Close, EMA values, MAnalyzer cross, ATR
- Exit rules:
  - `[EXIT_RULE] Long` when `Close < EmaLow`
  - `[EXIT_RULE] Short` when `Close > EmaHigh`
- MaxPnL exit:
  - `[EXIT_MAXPNL]` with PnL + thresholds + EMA values

Control Panel debug spam **removed** (UpdateState logs).

## Files touched / new
- `EMAwave34Strategy.cs`
  - Control panel wiring, internal `_strategyEnabled` gate, info panel immediate hide/show
  - Added detailed entry/exit/bar logs and filter decision/ready logs
  - Trading-hours uses wall-clock in realtime non-Playback
  - Disposes info panel on termination
- `EMAwave34ControlPanel.cs` (new)
  - WPF control panel module (Enable Strategy / Display Info Panel)
- `EMAwave34InfoPanel.cs`
  - MACD/VROC lines condensed; Hist/Value moved to indented detail line
- `EMAwave34.cs`
  - Drawing gated by `ChartControl != null` (Analyzer-safe)
  - `UpdateSignals` now takes `canDraw`
  - Renamed bar/outline color properties to descriptive names
- `EMAwave34ServiceMacdFilter.cs` (new)
- `EMAwave34ServiceVrocFilter.cs` (new)
- `deploy.ps1`
  - Deploys `EMAwave34ControlPanel.cs`
  - Removes legacy `34emawave.cs` / `34EMAwaveStrategy.cs` from NT targets
  - Does NOT remove `ServiceLogger.cs` (Leapfrog dependency)
- `AGENTS.md`
  - Expanded Warp/LLM guide + “I manually deploy/compile/test and report back.”

## Known behavior
- Strategy uses **managed orders**, **OnBarClose**.
- Entry signals from MAnalyzer cross.
- Stops/targets = ATR multiples per entry.
- Stop-outs execute even outside trading window; **window gates entries only**.
- Control Panel internal enable toggles entries only; NT strategy remains enabled.

## Recent compile errors fixed
- `IsInStrategyAnalyzer` removed from indicator.
- Duplicate `UpdateSignals` call removed.
- Duplicate `TimeSpan t` removed.

## What the user will do
- Manually deploy, compile, test, and report feedback.

## Command the user runs to deploy
`powershell -ExecutionPolicy Bypass -File .\deploy.ps1`

## If issues recur
- Gather NinjaTrader grid CSV errors and log excerpt.
- Most likely hot spots:
  - `EMAwave34Strategy.cs` (control panel, logs, trading hours)
  - `EMAwave34.cs` (indicator drawing, signals)

