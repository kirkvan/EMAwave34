using System;

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// Information panel service for EMAwave34Strategy.
    /// Displays key strategy settings and current trading status.
    /// </summary>
    public class EMAwave34InfoPanel : IDisposable
    {
        private readonly EMAwave34Strategy _strategy;
        private bool _isDisposed;

        private string _cachedDisplayText;
        private int _lastDisplayUpdateBar = -1;

        public EMAwave34InfoPanel(EMAwave34Strategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public void Initialize()
        {
            _cachedDisplayText = null;
            _lastDisplayUpdateBar = -1;
        }

        public void OnBarUpdate()
        {
            if (_strategy.CurrentBar != _lastDisplayUpdateBar)
                _cachedDisplayText = null;
        }

        public void Invalidate()
        {
            _cachedDisplayText = null;
            _lastDisplayUpdateBar = -1;
        }

        public string GetStatus()
        {
            return "Info Panel: Active";
        }

        public string GenerateDisplayText()
        {
            try
            {
                if (_lastDisplayUpdateBar == _strategy.CurrentBar && !string.IsNullOrEmpty(_cachedDisplayText))
                    return _cachedDisplayText;

                string statusDisplay = GetStatusDisplay();
                string entryDisplay = GetEntryDisplay();
                string riskDisplay = GetRiskDisplay();
                string hoursDisplay = GetTradingHoursDisplay();

                _cachedDisplayText = $"{statusDisplay}\n\n{entryDisplay}\n\n{riskDisplay}\n\n{hoursDisplay}";
                _lastDisplayUpdateBar = _strategy.CurrentBar;
                return _cachedDisplayText;
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[INFO_PANEL] Error generating display text: {ex.Message}", _strategy);
                return "Info Panel\n(Error - Check Logs)";
            }
        }

        private string GetStatusDisplay()
        {
            string pos = _strategy.Position.MarketPosition.ToString();
            int qty = _strategy.Position.Quantity;
            string halted = _strategy.IsSessionHalted ? "YES" : "NO";
            string window = _strategy.EnableTradingHours
                ? (_strategy.IsWithinTradingWindowNow ? "WITHIN" : "OUTSIDE")
                : "OFF";

            return "=== Status ===\n" +
                   $"Position: {pos} ({qty})\n" +
                   $"Session Halt: {halted}\n" +
                   $"Trading Window: {window}";
        }

        private string GetEntryDisplay()
        {
            double atr = _strategy.CurrentAtr;
            string atrText = double.IsNaN(atr) ? "n/a" : atr.ToString("F2");
            string macdText;
            if (_strategy.EnableMacdFilter)
            {
                string histText = double.IsNaN(_strategy.MacdHistogram) ? "n/a" : _strategy.MacdHistogram.ToString("F2");
                string macdWarmupText = _strategy.MacdFilterReady
                    ? "READY"
                    : $"WARMUP {_strategy.MacdWarmupBarsRemaining}/{_strategy.MacdWarmupBarsRequired}";
                bool macdLongAllowed = _strategy.MacdFilterReady &&
                                       _strategy.MacdHistogram >= _strategy.MacdHistogramThreshold;
                bool macdShortAllowed = _strategy.MacdFilterReady &&
                                        _strategy.MacdHistogram <= -_strategy.MacdHistogramThreshold;
                string macdEntryText = $"Longs {(macdLongAllowed ? "Allowed" : "Blocked")}, Shorts {(macdShortAllowed ? "Allowed" : "Blocked")}";
                macdText = $"MACD: ON | {macdWarmupText} | {macdEntryText}\n" +
                           $"  Hist {histText} (Min {_strategy.MacdHistogramThreshold:F2})";
            }
            else
            {
                macdText = "MACD: OFF";
            }
            string vrocText;
            if (_strategy.EnableVrocFilter)
            {
                string vrocValue = double.IsNaN(_strategy.VrocValue) ? "n/a" : _strategy.VrocValue.ToString("F2");
                string vrocWarmupText = _strategy.VrocFilterReady
                    ? "READY"
                    : $"WARMUP {_strategy.VrocWarmupBarsRemaining}/{_strategy.VrocWarmupBarsRequired}";
                bool vrocAllowsEntries = _strategy.VrocFilterReady &&
                                         _strategy.VrocValue >= _strategy.VrocMin;
                string vrocEntryText = vrocAllowsEntries ? "Longs Allowed, Shorts Allowed" : "Longs Blocked, Shorts Blocked";
                vrocText = $"VROC: ON | {vrocWarmupText} | {vrocEntryText}\n" +
                           $"  Value {vrocValue}% (Min {_strategy.VrocMin:F2}%)";
            }
            else
            {
                vrocText = "VROC: OFF";
            }

            return "=== Entry/Stops ===\n" +
                   $"Qty: {_strategy.PositionQuantity}\n" +
                   $"Scale-In: {_strategy.ScaleInPositions}/{_strategy.MaxScaleInPositions} (Orig {_strategy.OriginalPositions}) " +
                   $"(Start x{_strategy.ScaleInStartAtr:F1} Stop x{_strategy.ScaleInStopAtr:F1})\\n" +
                   $"ATR(14): {atrText}\n" +
                   $"Target x{_strategy.ProfitTargetAtr:F1}  Stop x{_strategy.StopLossAtr:F1}\n" +
                   $"Trail Stop: {(_strategy.EnableTrailingStop ? "ON" : "OFF")}\n" +
                   $"Breakeven: {(_strategy.EnableBreakeven ? "ON" : "OFF")}\n" +
                   $"{macdText}\n" +
                   $"{vrocText}";
        }

        private string GetRiskDisplay()
        {
            string pnlText = double.IsNaN(_strategy.SessionPnL) ? "n/a" : _strategy.SessionPnL.ToString("F2");

            return "=== Risk ===\n" +
                   $"Max Loss: ${_strategy.MaxLoss:F0}\n" +
                   $"Max Profit: ${_strategy.MaxProfit:F0}\n" +
                   $"Session PnL: ${pnlText}";
        }

        private string GetTradingHoursDisplay()
        {
            if (!_strategy.EnableTradingHours)
                return "=== Trading Hours ===\nDisabled";

            return "=== Trading Hours ===\n" +
                   $"{_strategy.TradingStartTime} - {_strategy.TradingEndTime}";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _cachedDisplayText = null;
            }

            _isDisposed = true;
        }

        ~EMAwave34InfoPanel()
        {
            Dispose(false);
        }
    }
}
