# AGENTS.md
Always: I manually deploy, compile, test, and report back my feedback.
This file provides guidance to WARP (warp.dev) and LLM agents when working in this repository.

## Critical workflow rules
- Edit source only in `C:\Users\Administrator\Documents\EMAwave34\` (this repo). Do not edit NinjaTrader target folders directly.
- Deploy using `deploy.ps1`. NinjaTrader compiles on restart or F5. No CLI build is defined here.
- Do not stage/commit/push unless explicitly asked by the user after they deploy/compile/test.
- Naming: use **EMAwave34** consistently everywhere (classes, files, strings). Avoid digit-leading names.
- Documentation: do not programmatically access NinjaTrader websites. If NT docs are needed, ask the user for excerpts or links they can open manually.

## Common commands
- Deploy to NinjaTrader 8 (also runs ASCII/BOM guard):
  `powershell -ExecutionPolicy Bypass -File .\deploy.ps1`
- Build/compile:
  Use NinjaTrader 8 UI (press **F5**). No CLI build is defined in this repo.
- Tests/lint:
  None documented.

## Repository overview
- **Indicator:** `EMAwave34.cs`
  - `EMAwave34` indicator: EMA High/Close/Low + MAnalyzer plot, bar/zone coloring, arrows.
  - `Calculate.OnBarClose`, overlay on price.
  - Public properties exposed for strategy binding and serialization.

- **Strategy:** `EMAwave34Strategy.cs`
  - Managed-orders strategy using the `EMAwave34` indicator.
  - Entry signals from MAnalyzer cross on closed bars.
  - ATR-based single target/stop set at entry (ATR period fixed at 14).
  - Stop-outs on close beyond EMA band (no reversal).
  - Trading-hours filter gates **entries only**; exits always allowed.
  - Session MaxLoss/MaxProfit (realized PnL) halts new entries for session.
  - Optional MACD histogram and VROC filters gate entries when enabled.
  - Info panel rendered via `EMAwave34InfoPanel`.

- **Info Panel:** `EMAwave34InfoPanel.cs`
  - Builds display text (status, entry/stop, risk, trading hours).
  - Rendered using `Draw.TextFixed` with strategy display settings.

- **Logging:** `EMAwave34ServiceLogger.cs`
  - Shared logger (copied from Leapfrog) with level filtering via `IEMAwave34LoggingConfig`.

- **Deployment:** `deploy.ps1`
  - Copies EMAwave34 strategy/indicator/logger files into NinjaTrader `bin\Custom`.
  - Enforces ASCII-only / no BOM for `.cs` and `.ps1`.
  - Removes legacy files (`34emawave.cs`, `34EMAwaveStrategy.cs`) from targets.
  - **Does not** remove `ServiceLogger.cs` in NinjaTrader `Strategies` (Leapfrog depends on it).

## Key behavior summary (strategy)
- `Calculate = OnBarClose`.
- Entry signals:
  - Long: MAnalyzer crosses from <= 0 to > 0
  - Short: MAnalyzer crosses from >= 0 to < 0
- ATR target/stop applied **per entry**:
  - Long target: `Close + ATR * ProfitTargetAtr`
  - Long stop: `Close - ATR * StopLossAtr`
  - Short target: `Close - ATR * ProfitTargetAtr`
  - Short stop: `Close + ATR * StopLossAtr`
- Stop-outs:
  - Long exits if `Close < EmaLow`
  - Short exits if `Close > EmaHigh`
- Trading hours:
  - `EnableTradingHours` gates **entries** only; stop-outs and exits still process outside window.
  - Start/end times are string properties (AM/PM).
- Session PnL gate:
  - Uses realized PnL since session start.
  - If MaxLoss/MaxProfit hit, new entries halt for session.

## Strategy property groups (NinjaTrader UI)
- **Strategy Settings:** `PositionQuantity`
- **Risk:** `MaxLoss`, `MaxProfit`, `ProfitTargetAtr`, `StopLossAtr`
- **Trading Hours:** `EnableTradingHours`, `TradingStartTime`, `TradingEndTime`
- **Display Settings:** `Risk & Info Panel Text Font Size`, `Info Panel Position`, `Display Info Panel`
- **Diagnostics:** `EnableDebugLogging`
- **Indicator Parameters/Settings/Colors/Visual:** EMA periods, arrow/band/zone toggles, color brushes, opacity
- **Momentum Filters:** MACD (fast/slow/smooth/hist min) and VROC (period/smooth/min)

## Notes for LLM changes
- Prefer lean changes; avoid over-engineering and bloat.
- Keep info panel updates in `EMAwave34InfoPanel.cs`; rendering settings in `EMAwave34Strategy.cs`.
- If you add parameters, place them in the appropriate UI group and keep ordering consistent.
