using System;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public sealed class EMAwave34ServiceHmaFilter
    {
        private readonly Strategy _strategy;
        private readonly HMA _hma;
        private readonly int _period;
        private readonly bool _enabled;
        private readonly int _minBars;

        public EMAwave34ServiceHmaFilter(Strategy strategy, int period, bool enabled)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _period = Math.Max(1, period);
            _enabled = enabled;
            _minBars = _period;
            _hma = _strategy.HMA(_period);
        }

        public bool IsReady => !_enabled || _strategy.CurrentBar >= _minBars;
        public HMA Indicator => _hma;

        public double Value => _hma != null ? _hma[0] : double.NaN;

        public bool PassLong()
        {
            if (!_enabled)
                return true;
            if (!IsReady)
                return false;
            return _strategy.Close[0] > Value;
        }

        public bool PassShort()
        {
            if (!_enabled)
                return true;
            if (!IsReady)
                return false;
            return _strategy.Close[0] < Value;
        }
    }
}
