using System;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public sealed class EMAwave34ServiceMacdFilter
    {
        private readonly Strategy _strategy;
        private readonly MACD _macd;
        private readonly int _fast;
        private readonly int _slow;
        private readonly int _smooth;
        private readonly double _histThreshold;
        private readonly bool _enabled;
        private readonly int _minBars;

        public EMAwave34ServiceMacdFilter(Strategy strategy, int fast, int slow, int smooth, double histThreshold, bool enabled)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _fast = Math.Max(1, fast);
            _slow = Math.Max(1, slow);
            _smooth = Math.Max(1, smooth);
            _histThreshold = Math.Max(0, histThreshold);
            _enabled = enabled;
            _minBars = Math.Max(_fast, _slow) + _smooth;
            _macd = _strategy.MACD(_fast, _slow, _smooth);
        }

        public bool IsReady => !_enabled || _strategy.CurrentBar >= _minBars;

        public double Histogram => _macd != null ? _macd.Diff[0] : double.NaN;

        public bool PassLong()
        {
            if (!_enabled)
                return true;
            if (!IsReady)
                return false;
            return Histogram >= _histThreshold;
        }

        public bool PassShort()
        {
            if (!_enabled)
                return true;
            if (!IsReady)
                return false;
            return Histogram <= -_histThreshold;
        }
    }
}
