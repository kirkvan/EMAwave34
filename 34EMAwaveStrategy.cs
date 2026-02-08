using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;

namespace NinjaTrader.NinjaScript.Strategies
{
    [DisplayName("34EMAwave Strategy")]
    public class EMAwave34Strategy : Strategy, ILoggingConfig
    {
        private EMAwave34 _indicator;
        private ATR _atr;
        private const int AtrPeriod = 14;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private bool _timesValid;
        private double _sessionStartRealized;
        private bool _haltTradingForSession;
        private bool _loggedIndicatorStatus;

        private int _positionQuantity = 1;
        private double _maxLoss = 500;
        private double _maxProfit = 500;
        private string _tradingStartTime = "09:29:59 AM";
        private string _tradingEndTime = "11:29:59 AM";
        private double _profitTargetAtr = 2.0;
        private double _stopLossAtr = 1.5;
        private bool _enableDebugLogging;

        private int _emaHighPeriod = 34;
        private int _emaClosePeriod = 34;
        private int _emaLowPeriod = 34;
        private bool _useRmiFilter = true;
        private bool _drawArrows = true;
        private bool _showHistoricalArrows = true;
        private bool _showMaBands = true;
        private bool _colorZone = true;
        private bool _colorBars = true;
        private bool _colorOutline = true;
        private int _zoneOpacity = 9;
        private Brush _maUpColor = Brushes.Lime;
        private Brush _maDownColor = Brushes.Red;
        private Brush _zoneColor = Brushes.Gray;

        private Brush _barColorCondition1 = Brushes.Chartreuse;
        private Brush _barColorCondition2 = Brushes.Green;
        private Brush _barColorCondition3 = Brushes.LightBlue;
        private Brush _barColorCondition4 = Brushes.RoyalBlue;
        private Brush _barColorCondition5 = Brushes.DarkOrange;
        private Brush _barColorCondition6 = Brushes.Red;

        private Brush _candleOutlineCondition1 = Brushes.Chartreuse;
        private Brush _candleOutlineCondition2 = Brushes.Green;
        private Brush _candleOutlineCondition3 = Brushes.LightBlue;
        private Brush _candleOutlineCondition4 = Brushes.RoyalBlue;
        private Brush _candleOutlineCondition5 = Brushes.DarkOrange;
        private Brush _candleOutlineCondition6 = Brushes.Red;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "34EMAwave Strategy";
                Description = "Strategy based on the 34EMAwave indicator.";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsUnmanaged = false;
                IsOverlay = true;

                PositionQuantity = 1;
                MaxLoss = 500;
                MaxProfit = 500;
                TradingStartTime = "09:29:59 AM";
                TradingEndTime = "11:29:59 AM";
                ProfitTargetAtr = 2.0;
                StopLossAtr = 1.5;

                Emah = 34;
                Emac = 34;
                Emal = 34;
                UseRmiFilter = true;
                DrawArrows = true;
                ShowHistoricalArrows = true;
                ShowMABands = true;
                Colorzone = true;
                Colorbars = true;
                ColorOutline = true;
                Zopacity = 9;
                MaUpColor = Brushes.Lime;
                MaDownColor = Brushes.Red;
                ZoneColor = Brushes.Gray;

                BarCondition1 = Brushes.Chartreuse;
                BarCondition2 = Brushes.Green;
                BarCondition3 = Brushes.LightBlue;
                BarCondition4 = Brushes.RoyalBlue;
                BarCondition5 = Brushes.DarkOrange;
                BarCondition6 = Brushes.Red;

                CandleOutlineCondition1 = Brushes.Chartreuse;
                CandleOutlineCondition2 = Brushes.Green;
                CandleOutlineCondition3 = Brushes.LightBlue;
                CandleOutlineCondition4 = Brushes.RoyalBlue;
                CandleOutlineCondition5 = Brushes.DarkOrange;
                CandleOutlineCondition6 = Brushes.Red;

                EnableDebugLogging = false;
            }
            else if (State == State.DataLoaded)
            {
                _indicator = EMAwave34(UseRmiFilter, DrawArrows);
                _atr = ATR(AtrPeriod);
                ApplyIndicatorSettings();
                ParseTradingTimes();

                if (EnableDebugLogging)
                {
                    ServiceLogger.Info(() =>
                        $"[INIT] Indicator created. UseRmiFilter={UseRmiFilter} DrawArrows={DrawArrows} ShowMABands={ShowMABands} " +
                        $"Colorzone={Colorzone} Colorbars={Colorbars} ColorOutline={ColorOutline} Zopacity={Zopacity}", this);
                }

                if (ChartControl != null && !IsInStrategyAnalyzer)
                {
                    try
                    {
                        AddChartIndicator(_indicator);
                        if (EnableDebugLogging)
                            ServiceLogger.Info(() => "[CHART] Added EMAwave34 indicator to chart.", this);
                    }
                    catch (Exception ex)
                    {
                        ServiceLogger.Warn(() => $"[CHART] AddChartIndicator failed: {ex.Message}", this);
                    }
                }
                else if (EnableDebugLogging)
                {
                    ServiceLogger.Info(() =>
                        $"[CHART] Skipped AddChartIndicator (ChartControl null={ChartControl == null}, IsInStrategyAnalyzer={IsInStrategyAnalyzer}).", this);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (_indicator == null || CurrentBar < 1)
                return;
            if (EnableDebugLogging && !_loggedIndicatorStatus)
            {
                _loggedIndicatorStatus = true;
                ServiceLogger.Debug(() =>
                    $"[INDICATOR] FirstBar={CurrentBar} EmaHigh={_indicator.EmaHigh[0]:F2} EmaLow={_indicator.EmaLow[0]:F2} " +
                    $"MAnalyzer={_indicator.MAnalyzer[0]:F2}", this);
            }

            if (Bars.IsFirstBarOfSession)
            {
                _sessionStartRealized = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                _haltTradingForSession = false;
            }

            double sessionPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - _sessionStartRealized;
            if (!_haltTradingForSession && (sessionPnL <= -MaxLoss || sessionPnL >= MaxProfit))
            {
                if (Position.MarketPosition == MarketPosition.Long)
                    ExitLong("MaxPnLExit");
                else if (Position.MarketPosition == MarketPosition.Short)
                    ExitShort("MaxPnLExit");

                _haltTradingForSession = true;
            }
            if (Position.MarketPosition == MarketPosition.Long && Close[0] < _indicator.EmaLow[0])
            {
                if (EnableDebugLogging)
                    ServiceLogger.Info(() => "[STOP] Long exit: close below EmaLow.", this);
                ExitLong("StopLong");
                return;
            }
            if (Position.MarketPosition == MarketPosition.Short && Close[0] > _indicator.EmaHigh[0])
            {
                if (EnableDebugLogging)
                    ServiceLogger.Info(() => "[STOP] Short exit: close above EmaHigh.", this);
                ExitShort("StopShort");
                return;
            }

            if (_haltTradingForSession)
                return;

            if (!IsWithinTradingWindow(Times[0][0]))
                return;

            bool longSignal = _indicator.MAnalyzer[0] > 0 && _indicator.MAnalyzer[1] <= 0;
            bool shortSignal = _indicator.MAnalyzer[0] < 0 && _indicator.MAnalyzer[1] >= 0;

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                double atrValue = _atr != null ? _atr[0] : double.NaN;
                if (double.IsNaN(atrValue) || atrValue <= 0)
                    return;
                if (longSignal)
                {
                    double targetPrice = Close[0] + atrValue * ProfitTargetAtr;
                    double stopPrice = Close[0] - atrValue * StopLossAtr;
                    SetProfitTarget("Long", CalculationMode.Price, targetPrice);
                    SetStopLoss("Long", CalculationMode.Price, stopPrice, false);
                    if (EnableDebugLogging)
                        ServiceLogger.Info(() => $"[ATR] Long entry ATR={atrValue:F2} Target={targetPrice:F2} Stop={stopPrice:F2}", this);
                    EnterLong(PositionQuantity, "Long");
                }
                else if (shortSignal)
                {
                    double targetPrice = Close[0] - atrValue * ProfitTargetAtr;
                    double stopPrice = Close[0] + atrValue * StopLossAtr;
                    SetProfitTarget("Short", CalculationMode.Price, targetPrice);
                    SetStopLoss("Short", CalculationMode.Price, stopPrice, false);
                    if (EnableDebugLogging)
                        ServiceLogger.Info(() => $"[ATR] Short entry ATR={atrValue:F2} Target={targetPrice:F2} Stop={stopPrice:F2}", this);
                    EnterShort(PositionQuantity, "Short");
                }
                return;
            }
        }

        private void ApplyIndicatorSettings()
        {
            _indicator.Emah = Emah;
            _indicator.Emac = Emac;
            _indicator.Emal = Emal;
            _indicator.UseRmiFilter = UseRmiFilter;
            _indicator.DrawArrows = DrawArrows;
            _indicator.ShowHistoricalArrows = ShowHistoricalArrows;
            _indicator.ShowMABands = ShowMABands;
            _indicator.Colorzone = Colorzone;
            _indicator.Colorbars = Colorbars;
            _indicator.ColorOutline = ColorOutline;
            _indicator.Zopacity = Zopacity;
            _indicator.MaUpColor = MaUpColor;
            _indicator.MaDownColor = MaDownColor;
            _indicator.ZoneColor = ZoneColor;

            _indicator.BarCondition1 = BarCondition1;
            _indicator.BarCondition2 = BarCondition2;
            _indicator.BarCondition3 = BarCondition3;
            _indicator.BarCondition4 = BarCondition4;
            _indicator.BarCondition5 = BarCondition5;
            _indicator.BarCondition6 = BarCondition6;

            _indicator.CandleOutlineCondition1 = CandleOutlineCondition1;
            _indicator.CandleOutlineCondition2 = CandleOutlineCondition2;
            _indicator.CandleOutlineCondition3 = CandleOutlineCondition3;
            _indicator.CandleOutlineCondition4 = CandleOutlineCondition4;
            _indicator.CandleOutlineCondition5 = CandleOutlineCondition5;
            _indicator.CandleOutlineCondition6 = CandleOutlineCondition6;
        }

        private void ParseTradingTimes()
        {
            _timesValid = TryParseTime(TradingStartTime, out _startTime) && TryParseTime(TradingEndTime, out _endTime);
        }

        private bool TryParseTime(string value, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] formats = { "h:mm:ss tt", "hh:mm:ss tt" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime dt))
            {
                time = dt.TimeOfDay;
                return true;
            }

            return false;
        }

        private bool IsWithinTradingWindow(DateTime barTime)
        {
            if (!_timesValid)
                return true;

            TimeSpan t = barTime.TimeOfDay;
            if (_startTime <= _endTime)
                return t >= _startTime && t <= _endTime;

            return t >= _startTime || t <= _endTime;
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Position Quantity", GroupName = "Strategy Settings", Order = 0)]
        public int PositionQuantity
        {
            get { return _positionQuantity; }
            set { _positionQuantity = Math.Max(1, value); }
        }

        [Range(1, 10000), NinjaScriptProperty]
        [Display(Name = "Max Loss (Session)", GroupName = "Risk", Order = 0)]
        public double MaxLoss
        {
            get { return _maxLoss; }
            set { _maxLoss = Math.Min(10000, Math.Max(1, value)); }
        }

        [Range(1, 10000), NinjaScriptProperty]
        [Display(Name = "Max Profit (Session)", GroupName = "Risk", Order = 1)]
        public double MaxProfit
        {
            get { return _maxProfit; }
            set { _maxProfit = Math.Min(10000, Math.Max(1, value)); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Trading Start Time", Description = "AM/PM format, e.g. 09:29:59 AM", GroupName = "Trading Hours", Order = 0)]
        public string TradingStartTime
        {
            get { return _tradingStartTime; }
            set { _tradingStartTime = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Trading End Time", Description = "AM/PM format, e.g. 11:29:59 AM", GroupName = "Trading Hours", Order = 1)]
        public string TradingEndTime
        {
            get { return _tradingEndTime; }
            set { _tradingEndTime = value; }
        }
        [Range(0.1, 100), NinjaScriptProperty]
        [Display(Name = "Profit Target ATR", Description = "ATR multiple for profit target (e.g., 2.0).", GroupName = "Risk", Order = 2)]
        public double ProfitTargetAtr
        {
            get { return _profitTargetAtr; }
            set { _profitTargetAtr = Math.Max(0.1, value); }
        }

        [Range(0.1, 100), NinjaScriptProperty]
        [Display(Name = "Stop Loss ATR", Description = "ATR multiple for stop loss (e.g., 1.5).", GroupName = "Risk", Order = 3)]
        public double StopLossAtr
        {
            get { return _stopLossAtr; }
            set { _stopLossAtr = Math.Max(0.1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable Debug Logging", Description = "Enable verbose ServiceLogger output.", GroupName = "Diagnostics", Order = 0)]
        public bool EnableDebugLogging
        {
            get { return _enableDebugLogging; }
            set { _enableDebugLogging = value; }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "EMA High Period", Description = "Number of bars used for the EMA calculated on High prices.", GroupName = "Indicator Parameters", Order = 1)]
        public int Emah
        {
            get { return _emaHighPeriod; }
            set { _emaHighPeriod = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "EMA Close Period", Description = "Number of bars used for the EMA calculated on Close prices.", GroupName = "Indicator Parameters", Order = 2)]
        public int Emac
        {
            get { return _emaClosePeriod; }
            set { _emaClosePeriod = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "EMA Low Period", Description = "Number of bars used for the EMA calculated on Low prices.", GroupName = "Indicator Parameters", Order = 3)]
        public int Emal
        {
            get { return _emaLowPeriod; }
            set { _emaLowPeriod = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Use RMI Filter", Description = "When enabled, arrows require RMI to be rising for buys and falling for sells.", GroupName = "Indicator Settings", Order = 0)]
        public bool UseRmiFilter
        {
            get { return _useRmiFilter; }
            set { _useRmiFilter = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Draw Arrows", Description = "Draw Buy/Sell arrows on the indicator.", GroupName = "Indicator Settings", Order = 1)]
        public bool DrawArrows
        {
            get { return _drawArrows; }
            set { _drawArrows = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Show Historical Arrows", Description = "When true, keep all arrows; when false, only the latest arrow is shown.", GroupName = "Indicator Settings", Order = 2)]
        public bool ShowHistoricalArrows
        {
            get { return _showHistoricalArrows; }
            set { _showHistoricalArrows = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Show MA Band", Description = "Show the EMA High/Low band.", GroupName = "Indicator Settings", Order = 3)]
        public bool ShowMABands
        {
            get { return _showMaBands; }
            set { _showMaBands = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Color Zone", Description = "Enable zone fill between EMA High/Low.", GroupName = "Indicator Settings", Order = 4)]
        public bool Colorzone
        {
            get { return _colorZone; }
            set { _colorZone = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Color Bars", Description = "Color bars based on EMA band position.", GroupName = "Indicator Settings", Order = 5)]
        public bool Colorbars
        {
            get { return _colorBars; }
            set { _colorBars = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Color Bar Outline", Description = "Color bar outlines based on EMA band position.", GroupName = "Indicator Settings", Order = 6)]
        public bool ColorOutline
        {
            get { return _colorOutline; }
            set { _colorOutline = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Color Zone Opacity", Description = "Zone opacity 1-9.", GroupName = "Indicator Settings", Order = 7)]
        public int Zopacity
        {
            get { return _zoneOpacity; }
            set { _zoneOpacity = Math.Min(9, Math.Max(1, value)); }
        }

        [XmlIgnore]
        [Display(Name = "Color for Rising MA", Description = "Color for rising EMA close line.", GroupName = "Indicator Colors", Order = 0)]
        public Brush MaUpColor
        {
            get { return _maUpColor; }
            set { _maUpColor = value; }
        }

        [Browsable(false)]
        public string MaUpColorSerialize
        {
            get { return Serialize.BrushToString(MaUpColor); }
            set { MaUpColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Color for Falling MA", Description = "Color for falling EMA close line.", GroupName = "Indicator Colors", Order = 1)]
        public Brush MaDownColor
        {
            get { return _maDownColor; }
            set { _maDownColor = value; }
        }

        [Browsable(false)]
        public string MaDownColorSerialize
        {
            get { return Serialize.BrushToString(MaDownColor); }
            set { MaDownColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Color for Zone", Description = "Fill color for the EMA band zone.", GroupName = "Indicator Colors", Order = 2)]
        public Brush ZoneColor
        {
            get { return _zoneColor; }
            set { _zoneColor = value; }
        }

        [Browsable(false)]
        public string ZoneColorSerialize
        {
            get { return Serialize.BrushToString(ZoneColor); }
            set { ZoneColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition1", Description = "Color of BarCondition1.", GroupName = "Indicator Visual", Order = 1)]
        public Brush BarCondition1
        {
            get { return _barColorCondition1; }
            set { _barColorCondition1 = value; }
        }

        [Browsable(false)]
        public string BarCondition1Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition1); }
            set { _barColorCondition1 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition2", Description = "Color of BarCondition2.", GroupName = "Indicator Visual", Order = 2)]
        public Brush BarCondition2
        {
            get { return _barColorCondition2; }
            set { _barColorCondition2 = value; }
        }

        [Browsable(false)]
        public string BarCondition2Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition2); }
            set { _barColorCondition2 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition3", Description = "Color of BarCondition3.", GroupName = "Indicator Visual", Order = 3)]
        public Brush BarCondition3
        {
            get { return _barColorCondition3; }
            set { _barColorCondition3 = value; }
        }

        [Browsable(false)]
        public string BarCondition3Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition3); }
            set { _barColorCondition3 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition4", Description = "Color of BarCondition4.", GroupName = "Indicator Visual", Order = 4)]
        public Brush BarCondition4
        {
            get { return _barColorCondition4; }
            set { _barColorCondition4 = value; }
        }

        [Browsable(false)]
        public string BarCondition4Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition4); }
            set { _barColorCondition4 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition5", Description = "Color of BarCondition5.", GroupName = "Indicator Visual", Order = 5)]
        public Brush BarCondition5
        {
            get { return _barColorCondition5; }
            set { _barColorCondition5 = value; }
        }

        [Browsable(false)]
        public string BarCondition5Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition5); }
            set { _barColorCondition5 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "BarCondition6", Description = "Color of BarCondition6.", GroupName = "Indicator Visual", Order = 6)]
        public Brush BarCondition6
        {
            get { return _barColorCondition6; }
            set { _barColorCondition6 = value; }
        }

        [Browsable(false)]
        public string BarCondition6Serialize
        {
            get { return Serialize.BrushToString(_barColorCondition6); }
            set { _barColorCondition6 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition1", Description = "Color of CandleOutlineCondition1.", GroupName = "Indicator Visual", Order = 7)]
        public Brush CandleOutlineCondition1
        {
            get { return _candleOutlineCondition1; }
            set { _candleOutlineCondition1 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition1Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition1); }
            set { _candleOutlineCondition1 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition2", Description = "Color of CandleOutlineCondition2.", GroupName = "Indicator Visual", Order = 8)]
        public Brush CandleOutlineCondition2
        {
            get { return _candleOutlineCondition2; }
            set { _candleOutlineCondition2 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition2Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition2); }
            set { _candleOutlineCondition2 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition3", Description = "Color of CandleOutlineCondition3.", GroupName = "Indicator Visual", Order = 9)]
        public Brush CandleOutlineCondition3
        {
            get { return _candleOutlineCondition3; }
            set { _candleOutlineCondition3 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition3Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition3); }
            set { _candleOutlineCondition3 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition4", Description = "Color of CandleOutlineCondition4.", GroupName = "Indicator Visual", Order = 10)]
        public Brush CandleOutlineCondition4
        {
            get { return _candleOutlineCondition4; }
            set { _candleOutlineCondition4 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition4Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition4); }
            set { _candleOutlineCondition4 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition5", Description = "Color of CandleOutlineCondition5.", GroupName = "Indicator Visual", Order = 11)]
        public Brush CandleOutlineCondition5
        {
            get { return _candleOutlineCondition5; }
            set { _candleOutlineCondition5 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition5Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition5); }
            set { _candleOutlineCondition5 = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "CandleOutlineCondition6", Description = "Color of CandleOutlineCondition6.", GroupName = "Indicator Visual", Order = 12)]
        public Brush CandleOutlineCondition6
        {
            get { return _candleOutlineCondition6; }
            set { _candleOutlineCondition6 = value; }
        }

        [Browsable(false)]
        public string CandleOutlineCondition6Serialize
        {
            get { return Serialize.BrushToString(_candleOutlineCondition6); }
            set { _candleOutlineCondition6 = Serialize.StringToBrush(value); }
        }

        bool ILoggingConfig.EnableDebugLogging => EnableDebugLogging;

        bool ILoggingConfig.IsBacktestContext => State == State.Historical || IsInStrategyAnalyzer;

        LogLevel ILoggingConfig.MinimumLogLevel
        {
            get
            {
                if (EnableDebugLogging)
                    return LogLevel.Debug;
                return ((ILoggingConfig)this).IsBacktestContext ? LogLevel.Error : LogLevel.Warn;
            }
        }
    }
}
