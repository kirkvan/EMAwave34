using System;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public sealed class EMAwave34ServiceVrocFilter
    {
        private readonly Strategy _strategy;
        private readonly VROC _vroc;
        private readonly int _period;
        private readonly int _smooth;
        private readonly double _minVroc;
        private readonly bool _enabled;
        private readonly int _minBars;

        public EMAwave34ServiceVrocFilter(Strategy strategy, int period, int smooth, double minVroc, bool enabled)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _period = Math.Max(1, period);
            _smooth = Math.Max(1, smooth);
            _minVroc = Math.Max(0, minVroc);
            _enabled = enabled;
            _minBars = _period + _smooth;
            _vroc = _strategy.VROC(_period, _smooth);
        }

        public bool IsReady => !_enabled || _strategy.CurrentBar >= _minBars;

        public double Value => _vroc != null ? _vroc[0] : double.NaN;

        public bool Pass()
        {
            if (!_enabled)
                return true;
            if (!IsReady)
                return false;
            return Value >= _minVroc;
        }
    }
}
