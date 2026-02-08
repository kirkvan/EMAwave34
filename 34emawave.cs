
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Indicators
{
    [DisplayName("34EMAwave")]
    public class EMAwave34 : Indicator
    {
        private const string BandRegionTag = "MABands";

        private bool _colorBars = true;
        private bool _colorZone = true;
        private bool _colorOutline = true;
        private bool _showMaBands = true;
        private bool _drawArrows = true;
        private bool _useRmiFilter = true;

        private int _zoneOpacity = 3;
        private Brush _zoneColor = Brushes.Gray;
        private Brush _maUpColor = Brushes.Lime;
        private Brush _maDownColor = Brushes.Red;

        private int _emaHighPeriod = 34;
        private int _emaClosePeriod = 34;
        private int _emaLowPeriod = 34;

        private int _rmiPeriod = 14;
        private int _rmiShift = 3;

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

        private EMA _emaHigh;
        private EMA _emaClose;
        private EMA _emaLow;
        private EMA _emaOne;

        private Series<double> _rmiAvgUp;
        private Series<double> _rmiAvgDown;
        private Series<double> _rmiValue;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "34EMAwave";
                Description = "34EMAwave is an adaptation of the GRAB (GRB) setup used by Raghee Horner.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsSuspendedWhileInactive = true;

                DrawArrows = true;
                UseRmiFilter = true;
                ShowHistoricalArrows = true;
                ShowMABands = true;
                Colorzone = true;
                Colorbars = true;
                ColorOutline = true;
                Zopacity = 9;
                Emah = 34;
                Emac = 34;
                Emal = 34;

		        AddPlot(new Stroke(Brushes.ForestGreen), PlotStyle.Line, "EmaHigh");
		        AddPlot(new Stroke(Brushes.MediumBlue), PlotStyle.Line, "EmaClose");
		        AddPlot(new Stroke(Brushes.Red), PlotStyle.Line, "EmaLow");
		        AddPlot(new Stroke(Brushes.Transparent), PlotStyle.Line, "MAnalyzer");
            }
			
			else if (State == State.Configure)
			{
	            Plots[0].Width = 1;
				Plots[1].Width = 2;
				Plots[2].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Dot;
				Plots[1].DashStyleHelper = DashStyleHelper.Dot;
				Plots[2].DashStyleHelper = DashStyleHelper.Dot;
			}
            else if (State == State.DataLoaded)
            {
                _emaHigh = EMA(High, Emah);
                _emaClose = EMA(Close, Emac);
                _emaLow = EMA(Low, Emal);
                _emaOne = EMA(1);

                _rmiAvgUp = new Series<double>(this);
                _rmiAvgDown = new Series<double>(this);
                _rmiValue = new Series<double>(this);
            }
        }

        private int GetMinimumBarsRequired()
        {
            int emaBars = Math.Max(Emah, Math.Max(Emac, Emal));
            int rmiBars = _rmiPeriod + _rmiShift;
            return Math.Max(emaBars, rmiBars);
        }

        private void UpdateRmi()
        {
            if (CurrentBar == 0)
            {
                _rmiAvgUp[0] = 0;
                _rmiAvgDown[0] = 0;
                _rmiValue[0] = 0;
                return;
            }

            int warmupBars = _rmiPeriod + _rmiShift;
            if (CurrentBar < warmupBars)
            {
                _rmiValue[0] = 0;
                return;
            }

            double amountUp;
            double amountDown;

            if (CurrentBar == warmupBars)
            {
                double sumUp = 0;
                double sumDown = 0;

                for (int barsAgo = 0; barsAgo < _rmiPeriod; barsAgo++)
                {
                    amountUp = Input[barsAgo] - Input[barsAgo + _rmiShift];
                    if (amountUp >= 0)
                    {
                        amountDown = 0;
                    }
                    else
                    {
                        amountDown = -amountUp;
                        amountUp = 0;
                    }

                    sumUp += amountUp;
                    sumDown += amountDown;
                }

                _rmiAvgUp[0] = sumUp / _rmiPeriod;
                _rmiAvgDown[0] = sumDown / _rmiPeriod;
            }
            else
            {
                amountUp = Input[0] - Input[_rmiShift];
                if (amountUp >= 0)
                {
                    amountDown = 0;
                }
                else
                {
                    amountDown = -amountUp;
                    amountUp = 0;
                }

                _rmiAvgUp[0] = (_rmiAvgUp[1] * (_rmiPeriod - 1) + amountUp) / _rmiPeriod;
                _rmiAvgDown[0] = (_rmiAvgDown[1] * (_rmiPeriod - 1) + amountDown) / _rmiPeriod;
            }

            double denom = _rmiAvgUp[0] + _rmiAvgDown[0];
            _rmiValue[0] = denom != 0 ? 100.0 * _rmiAvgUp[0] / denom : 0;
        }

        private void UpdateEmaPlots(double emaHigh, double emaClose, double emaLow)
        {
            EmaHigh[0] = emaHigh;
            EmaClose[0] = emaClose;
            EmaLow[0] = emaLow;
        }

        private void UpdateMaPlotColors()
        {
            if (IsRising(_emaClose))
                PlotBrushes[1][0] = MaUpColor;

            if (IsFalling(_emaClose))
                PlotBrushes[1][0] = MaDownColor;

            if (!ShowMABands)
            {
                PlotBrushes[0][0] = Brushes.Transparent;
                PlotBrushes[2][0] = Brushes.Transparent;
            }
        }

        private void UpdateBarColors(double emaHigh, double emaLow)
        {
            double open = Open[0];
            double close = Close[0];

            if (open <= close && close > emaHigh)
                ApplyBarColor(_barColorCondition1, _candleOutlineCondition1);

            if (open >= close && close > emaHigh)
                ApplyBarColor(_barColorCondition2, _candleOutlineCondition2);

            if (open <= close && close < emaHigh && close > emaLow)
                ApplyBarColor(_barColorCondition3, _candleOutlineCondition3);

            if (open >= close && close < emaHigh && close > emaLow)
                ApplyBarColor(_barColorCondition4, _candleOutlineCondition4);

            if (open <= close && close < emaLow)
                ApplyBarColor(_barColorCondition5, _candleOutlineCondition5);

            if (open >= close && close < emaLow)
                ApplyBarColor(_barColorCondition6, _candleOutlineCondition6);
        }

        private void ApplyBarColor(Brush barBrush, Brush outlineBrush)
        {
            if (_colorBars)
                BarBrush = barBrush;

            if (_colorOutline)
                CandleOutlineBrush = outlineBrush;
        }

        private void UpdateSignals(double emaHigh, double emaLow)
        {
            if ((!UseRmiFilter || IsFalling(_rmiValue))
                && Close[1] < Open[1]
                && Close[0] < Open[0]
                && CrossBelow(_emaOne, emaLow, 1))
            {
                if (_drawArrows)
                    Draw.ArrowDown(this, "ARROWDOWN" + (ShowHistoricalArrows ? CurrentBar.ToString() : ToString()), false, 0, High[0] + TickSize, Brushes.White);

                MAnalyzer[0] = -1;
            }

            if ((!UseRmiFilter || IsRising(_rmiValue))
                && Close[1] > Open[1]
                && Close[0] > Open[0]
                && CrossAbove(_emaOne, emaHigh, 1))
            {
                if (_drawArrows)
                    Draw.ArrowUp(this, "ARROWUP" + (ShowHistoricalArrows ? CurrentBar.ToString() : ToString()), false, 0, Low[0] - TickSize, Brushes.White);

                MAnalyzer[0] = 1;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < GetMinimumBarsRequired())
                return;

            UpdateRmi();

            double emaHigh = _emaHigh[0];
            double emaClose = _emaClose[0];
            double emaLow = _emaLow[0];

            UpdateBarColors(emaHigh, emaLow);
            UpdateEmaPlots(emaHigh, emaClose, emaLow);
            UpdateMaPlotColors();

            if (Colorzone)
                Draw.Region(this, BandRegionTag, CurrentBar, 0, EmaHigh, EmaLow, Brushes.Transparent, ZoneColor, Zopacity);

            UpdateSignals(emaHigh, emaLow);
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> EmaHigh
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> EmaClose
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> EmaLow
        {
            get { return Values[2]; }
        }
		
        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> MAnalyzer
        {
            get { return Values[3]; }
        }		

        [Display(Name = "EMA High Period", Description = "Number of bars used for the EMA calculated on High prices.", GroupName = "Parameters", Order = 1)]
        public int Emah
        {
            get { return _emaHighPeriod; }
            set { _emaHighPeriod = Math.Max(1, value); }
        }
        [Display(Name = "EMA Close Period", Description = "Number of bars used for the EMA calculated on Close prices.", GroupName = "Parameters", Order = 2)]
        public int Emac
        {
            get { return _emaClosePeriod; }
            set { _emaClosePeriod = Math.Max(1, value); }
        }
        [Display(Name = "EMA Low Period", Description = "Number of bars used for the EMA calculated on Low prices.", GroupName = "Parameters", Order = 3)]
        public int Emal
        {
            get { return _emaLowPeriod; }
            set { _emaLowPeriod = Math.Max(1, value); }
        }

		[NinjaScriptProperty]
		[Display(Name = "Use RMI Filter", Description = "When enabled, arrows require RMI to be rising for buys and falling for sells.", GroupName = "Properties", Order = 0)]
		public bool UseRmiFilter
		{
			get { return _useRmiFilter; }
			set { _useRmiFilter = value; }
		}
		
		[XmlIgnore]
		[Display(Name = "BarCondition1", Description = "Color of BarCondition1.", GroupName = "Visual", Order = 1)]
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
		
		[Display(Name = "BarCondition2", Description = "Color of BarCondition2.", GroupName = "Visual", Order = 2)]
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
		
		[Display(Name = "BarCondition3", Description = "Color of BarCondition3.", GroupName = "Visual", Order = 3)]
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
		
		[Display(Name = "BarCondition4", Description = "Color of BarCondition4.", GroupName = "Visual", Order = 4)]
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
		
		[Display(Name = "BarCondition5", Description = "Color of BarCondition5.", GroupName = "Visual", Order = 5)]
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
		
		[Display(Name = "BarCondition6", Description = "Color of BarCondition6.", GroupName = "Visual", Order = 6)]
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
		
		[Display(Name = "CandleOutlineCondition1", Description = "Color of CandleOutlineCondition1.", GroupName = "Visual", Order = 1)]
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
				
		[Display(Name = "CandleOutlineCondition2", Description = "Color of CandleOutlineCondition2.", GroupName = "Visual", Order = 2)]
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
		
		[Display(Name = "CandleOutlineCondition3", Description = "Color of CandleOutlineCondition3.", GroupName = "Visual", Order = 3)]
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
		
		[Display(Name = "CandleOutlineCondition4", Description = "Color of CandleOutlineCondition4.", GroupName = "Visual", Order = 4)]
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
		
		[Display(Name = "CandleOutlineCondition5", Description = "Color of CandleOutlineCondition5.", GroupName = "Visual", Order = 5)]
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
		
		[Display(Name = "CandleOutlineCondition6", Description = "Color of CandleOutlineCondition6.", GroupName = "Visual", Order = 6)]
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
		
		[NinjaScriptProperty]	
		[Display(Name = "1. Draw Arrows", Description = "Draw Buy/Sell Arrows", GroupName = "Colors", Order = 0)]		
		public bool DrawArrows
		{
			get { return _drawArrows; }
			set { _drawArrows = value; }
		}
		
		
		[Display(Name = "2. Show Historical Arrows", Description = "When true, keep all arrows; when false, only the latest arrow is shown.", GroupName = "Colors", Order = 1)]		
		public bool ShowHistoricalArrows
		{get; set;} 		

		[Display(Name = "3. Show MA Band", Description = "Show the EMA High/Low band.", GroupName = "Colors", Order = 2)]		
		public bool ShowMABands
		{
			get { return _showMaBands; }
			set { _showMaBands = value; }
		}		
		
		[Display(Name = "4. Color Zone", Description = "Color Zone", GroupName = "Colors", Order = 3)]		
		public bool Colorzone
		{
			get { return _colorZone; }
			set { _colorZone = value; }
		}
		
		[Display(Name = "5. Color Bars", Description = "Color Bars", GroupName = "Colors", Order = 4)]		
		public bool Colorbars
		{
			get { return _colorBars; }
			set { _colorBars = value; }
		}
		
		[Display(Name = "6. Color Bar Outline", Description = "Color Bars Outline", GroupName = "Colors", Order = 5)]		
		public bool ColorOutline
		{
			get { return _colorOutline; }
			set { _colorOutline = value; }
		}		
		
		[Display(Name = "7. Color Zone Opacity", Description = "Zone Opacity 1-9", GroupName = "Colors", Order = 6)]		
		public int Zopacity
		{
			get { return _zoneOpacity; }
			set { _zoneOpacity = Math.Min(9, Math.Max(1, value)); }
		}
		
		[XmlIgnore()]
		
		[Display(Name = "8. Color for Rising MA", Description = "Color for Rising MA", GroupName = "Colors", Order = 7)]
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
		
		[XmlIgnore()]
		
		[Display(Name = "9. Color for Falling MA", Description = "Color for Falling MA", GroupName = "Colors", Order = 7)]
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
		
		[XmlIgnore()]
		
		[Display(Name = "10. Color for Zone", Description = "Color for Zone", GroupName = "Colors", Order = 7)]
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
    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class _34EMAwave : EMAwave34
    {
    }
}

#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMAwave34[] cache_EMAwave34;
		public EMAwave34 EMAwave34(bool useRmiFilter, bool drawArrows)
		{
			return EMAwave34(Input, useRmiFilter, drawArrows);
		}

		public EMAwave34 EMAwave34(ISeries<double> input, bool useRmiFilter, bool drawArrows)
		{
			if (cache_EMAwave34 != null)
				for (int idx = 0; idx < cache_EMAwave34.Length; idx++)
					if (cache_EMAwave34[idx] != null && cache_EMAwave34[idx].UseRmiFilter == useRmiFilter && cache_EMAwave34[idx].DrawArrows == drawArrows && cache_EMAwave34[idx].EqualsInput(input))
						return cache_EMAwave34[idx];
			return CacheIndicator<EMAwave34>(new EMAwave34(){ UseRmiFilter = useRmiFilter, DrawArrows = drawArrows }, input, ref cache_EMAwave34);
		}

		private _34EMAwave[] cache__34EMAwave;
		public _34EMAwave _34EMAwave(bool useRmiFilter, bool drawArrows)
		{
			return _34EMAwave(Input, useRmiFilter, drawArrows);
		}

		public _34EMAwave _34EMAwave(ISeries<double> input, bool useRmiFilter, bool drawArrows)
		{
			if (cache__34EMAwave != null)
				for (int idx = 0; idx < cache__34EMAwave.Length; idx++)
					if (cache__34EMAwave[idx] != null && cache__34EMAwave[idx].UseRmiFilter == useRmiFilter && cache__34EMAwave[idx].DrawArrows == drawArrows && cache__34EMAwave[idx].EqualsInput(input))
						return cache__34EMAwave[idx];
			return CacheIndicator<_34EMAwave>(new _34EMAwave(){ UseRmiFilter = useRmiFilter, DrawArrows = drawArrows }, input, ref cache__34EMAwave);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMAwave34 EMAwave34(bool useRmiFilter, bool drawArrows)
		{
			return indicator.EMAwave34(Input, useRmiFilter, drawArrows);
		}

		public Indicators.EMAwave34 EMAwave34(ISeries<double> input , bool useRmiFilter, bool drawArrows)
		{
			return indicator.EMAwave34(input, useRmiFilter, drawArrows);
		}

		public Indicators._34EMAwave _34EMAwave(bool useRmiFilter, bool drawArrows)
		{
			return indicator._34EMAwave(Input, useRmiFilter, drawArrows);
		}

		public Indicators._34EMAwave _34EMAwave(ISeries<double> input , bool useRmiFilter, bool drawArrows)
		{
			return indicator._34EMAwave(input, useRmiFilter, drawArrows);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMAwave34 EMAwave34(bool useRmiFilter, bool drawArrows)
		{
			return indicator.EMAwave34(Input, useRmiFilter, drawArrows);
		}

		public Indicators.EMAwave34 EMAwave34(ISeries<double> input , bool useRmiFilter, bool drawArrows)
		{
			return indicator.EMAwave34(input, useRmiFilter, drawArrows);
		}

		public Indicators._34EMAwave _34EMAwave(bool useRmiFilter, bool drawArrows)
		{
			return indicator._34EMAwave(Input, useRmiFilter, drawArrows);
		}

		public Indicators._34EMAwave _34EMAwave(ISeries<double> input , bool useRmiFilter, bool drawArrows)
		{
			return indicator._34EMAwave(input, useRmiFilter, drawArrows);
		}
	}
}
#endregion

