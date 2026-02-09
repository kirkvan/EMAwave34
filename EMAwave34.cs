
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
    [DisplayName("EMAwave34")]
    public class EMAwave34 : Indicator
    {
        private const string BandRegionTag = "MABands";

        private bool _colorBars = true;
        private bool _colorZone = true;
        private bool _colorOutline = true;
        private bool _showMaBands = true;
        private bool _drawArrows = true;

        private int _zoneOpacity = 3;
        private Brush _zoneColor = Brushes.Gray;
        private Brush _maUpColor = Brushes.Lime;
        private Brush _maDownColor = Brushes.Red;

        private int _emaHighPeriod = 34;
        private int _emaClosePeriod = 34;
        private int _emaLowPeriod = 34;

        private Brush _barColorBullishAboveEmaHigh = Brushes.Chartreuse;
        private Brush _barColorBearishAboveEmaHigh = Brushes.Green;
        private Brush _barColorBullishInsideBand = Brushes.LightBlue;
        private Brush _barColorBearishInsideBand = Brushes.RoyalBlue;
        private Brush _barColorBullishBelowEmaLow = Brushes.DarkOrange;
        private Brush _barColorBearishBelowEmaLow = Brushes.Red;

        private Brush _outlineColorBullishAboveEmaHigh = Brushes.Chartreuse;
        private Brush _outlineColorBearishAboveEmaHigh = Brushes.Green;
        private Brush _outlineColorBullishInsideBand = Brushes.LightBlue;
        private Brush _outlineColorBearishInsideBand = Brushes.RoyalBlue;
        private Brush _outlineColorBullishBelowEmaLow = Brushes.DarkOrange;
        private Brush _outlineColorBearishBelowEmaLow = Brushes.Red;

        private EMA _emaHigh;
        private EMA _emaClose;
        private EMA _emaLow;
        private EMA _emaOne;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "EMAwave34";
                Description = "EMAwave34 is an adaptation of the GRAB (GRB) setup used by Raghee Horner.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
                PaintPriceMarkers = false;

                DrawArrows = true;
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
            }
        }

        private int GetMinimumBarsRequired()
        {
            return Math.Max(Emah, Math.Max(Emac, Emal));
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
                ApplyBarColor(_barColorBullishAboveEmaHigh, _outlineColorBullishAboveEmaHigh);

            if (open >= close && close > emaHigh)
                ApplyBarColor(_barColorBearishAboveEmaHigh, _outlineColorBearishAboveEmaHigh);

            if (open <= close && close < emaHigh && close > emaLow)
                ApplyBarColor(_barColorBullishInsideBand, _outlineColorBullishInsideBand);

            if (open >= close && close < emaHigh && close > emaLow)
                ApplyBarColor(_barColorBearishInsideBand, _outlineColorBearishInsideBand);

            if (open <= close && close < emaLow)
                ApplyBarColor(_barColorBullishBelowEmaLow, _outlineColorBullishBelowEmaLow);

            if (open >= close && close < emaLow)
                ApplyBarColor(_barColorBearishBelowEmaLow, _outlineColorBearishBelowEmaLow);
        }

        private void ApplyBarColor(Brush barBrush, Brush outlineBrush)
        {
            if (_colorBars)
                BarBrush = barBrush;

            if (_colorOutline)
                CandleOutlineBrush = outlineBrush;
        }

        private void UpdateSignals(double emaHigh, double emaLow, bool canDraw)
        {
            if (Close[1] < Open[1]
                && Close[0] < Open[0]
                && CrossBelow(_emaOne, emaLow, 1))
            {
                if (_drawArrows && canDraw)
                    Draw.ArrowDown(this, "ARROWDOWN" + (ShowHistoricalArrows ? CurrentBar.ToString() : ToString()), false, 0, High[0] + TickSize, Brushes.White);

                MAnalyzer[0] = -1;
            }
            if (Close[1] > Open[1]
                && Close[0] > Open[0]
                && CrossAbove(_emaOne, emaHigh, 1))
            {
                if (_drawArrows && canDraw)
                    Draw.ArrowUp(this, "ARROWUP" + (ShowHistoricalArrows ? CurrentBar.ToString() : ToString()), false, 0, Low[0] - TickSize, Brushes.White);

                MAnalyzer[0] = 1;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < GetMinimumBarsRequired())
                return;

            double emaHigh = _emaHigh[0];
            double emaClose = _emaClose[0];
            double emaLow = _emaLow[0];

            bool canDraw = ChartControl != null;
            UpdateEmaPlots(emaHigh, emaClose, emaLow);
            if (canDraw)
            {
                UpdateBarColors(emaHigh, emaLow);
                UpdateMaPlotColors();
                if (Colorzone)
                    Draw.Region(this, BandRegionTag, CurrentBar, 0, EmaHigh, EmaLow, Brushes.Transparent, ZoneColor, Zopacity);
            }
            UpdateSignals(emaHigh, emaLow, canDraw);
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
		
		[XmlIgnore]
		[Display(Name = "Bar Color: Bullish Above EMA High", Description = "Bar color when Close > EMA High and the bar is bullish.", GroupName = "Visual", Order = 1)]
        public Brush BarColorBullishAboveEmaHigh
        {
            get { return _barColorBullishAboveEmaHigh; }
            set { _barColorBullishAboveEmaHigh = value; }
        }

        [Browsable(false)]
        public string BarColorBullishAboveEmaHighSerialize
        {
            get { return Serialize.BrushToString(_barColorBullishAboveEmaHigh); }
            set { _barColorBullishAboveEmaHigh = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Bar Color: Bearish Above EMA High", Description = "Bar color when Close > EMA High and the bar is bearish.", GroupName = "Visual", Order = 2)]
        public Brush BarColorBearishAboveEmaHigh
        {
            get { return _barColorBearishAboveEmaHigh; }
            set { _barColorBearishAboveEmaHigh = value; }
        }

        [Browsable(false)]
        public string BarColorBearishAboveEmaHighSerialize
        {
            get { return Serialize.BrushToString(_barColorBearishAboveEmaHigh); }
            set { _barColorBearishAboveEmaHigh = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Bar Color: Bullish Inside EMA Band", Description = "Bar color when Close is between EMA High/Low and the bar is bullish.", GroupName = "Visual", Order = 3)]
        public Brush BarColorBullishInsideBand
        {
            get { return _barColorBullishInsideBand; }
            set { _barColorBullishInsideBand = value; }
        }

        [Browsable(false)]
        public string BarColorBullishInsideBandSerialize
        {
            get { return Serialize.BrushToString(_barColorBullishInsideBand); }
            set { _barColorBullishInsideBand = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Bar Color: Bearish Inside EMA Band", Description = "Bar color when Close is between EMA High/Low and the bar is bearish.", GroupName = "Visual", Order = 4)]
        public Brush BarColorBearishInsideBand
        {
            get { return _barColorBearishInsideBand; }
            set { _barColorBearishInsideBand = value; }
        }

        [Browsable(false)]
        public string BarColorBearishInsideBandSerialize
        {
            get { return Serialize.BrushToString(_barColorBearishInsideBand); }
            set { _barColorBearishInsideBand = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Bar Color: Bullish Below EMA Low", Description = "Bar color when Close < EMA Low and the bar is bullish.", GroupName = "Visual", Order = 5)]
        public Brush BarColorBullishBelowEmaLow
        {
            get { return _barColorBullishBelowEmaLow; }
            set { _barColorBullishBelowEmaLow = value; }
        }

        [Browsable(false)]
        public string BarColorBullishBelowEmaLowSerialize
        {
            get { return Serialize.BrushToString(_barColorBullishBelowEmaLow); }
            set { _barColorBullishBelowEmaLow = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Bar Color: Bearish Below EMA Low", Description = "Bar color when Close < EMA Low and the bar is bearish.", GroupName = "Visual", Order = 6)]
        public Brush BarColorBearishBelowEmaLow
        {
            get { return _barColorBearishBelowEmaLow; }
            set { _barColorBearishBelowEmaLow = value; }
        }

        [Browsable(false)]
        public string BarColorBearishBelowEmaLowSerialize
        {
            get { return Serialize.BrushToString(_barColorBearishBelowEmaLow); }
            set { _barColorBearishBelowEmaLow = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Outline Color: Bullish Above EMA High", Description = "Outline color when Close > EMA High and the bar is bullish.", GroupName = "Visual", Order = 7)]
        public Brush OutlineColorBullishAboveEmaHigh
        {
            get { return _outlineColorBullishAboveEmaHigh; }
            set { _outlineColorBullishAboveEmaHigh = value; }
        }

        [Browsable(false)]
        public string OutlineColorBullishAboveEmaHighSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBullishAboveEmaHigh); }
            set { _outlineColorBullishAboveEmaHigh = Serialize.StringToBrush(value); }
        }
				
		[XmlIgnore]
				
		[Display(Name = "Outline Color: Bearish Above EMA High", Description = "Outline color when Close > EMA High and the bar is bearish.", GroupName = "Visual", Order = 8)]
        public Brush OutlineColorBearishAboveEmaHigh
        {
            get { return _outlineColorBearishAboveEmaHigh; }
            set { _outlineColorBearishAboveEmaHigh = value; }
        }

        [Browsable(false)]
        public string OutlineColorBearishAboveEmaHighSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBearishAboveEmaHigh); }
            set { _outlineColorBearishAboveEmaHigh = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Outline Color: Bullish Inside EMA Band", Description = "Outline color when Close is between EMA High/Low and the bar is bullish.", GroupName = "Visual", Order = 9)]
        public Brush OutlineColorBullishInsideBand
        {
            get { return _outlineColorBullishInsideBand; }
            set { _outlineColorBullishInsideBand = value; }
        }

        [Browsable(false)]
        public string OutlineColorBullishInsideBandSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBullishInsideBand); }
            set { _outlineColorBullishInsideBand = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Outline Color: Bearish Inside EMA Band", Description = "Outline color when Close is between EMA High/Low and the bar is bearish.", GroupName = "Visual", Order = 10)]
        public Brush OutlineColorBearishInsideBand
        {
            get { return _outlineColorBearishInsideBand; }
            set { _outlineColorBearishInsideBand = value; }
        }

        [Browsable(false)]
        public string OutlineColorBearishInsideBandSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBearishInsideBand); }
            set { _outlineColorBearishInsideBand = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Outline Color: Bullish Below EMA Low", Description = "Outline color when Close < EMA Low and the bar is bullish.", GroupName = "Visual", Order = 11)]
        public Brush OutlineColorBullishBelowEmaLow
        {
            get { return _outlineColorBullishBelowEmaLow; }
            set { _outlineColorBullishBelowEmaLow = value; }
        }

        [Browsable(false)]
        public string OutlineColorBullishBelowEmaLowSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBullishBelowEmaLow); }
            set { _outlineColorBullishBelowEmaLow = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		
		[Display(Name = "Outline Color: Bearish Below EMA Low", Description = "Outline color when Close < EMA Low and the bar is bearish.", GroupName = "Visual", Order = 12)]
        public Brush OutlineColorBearishBelowEmaLow
        {
            get { return _outlineColorBearishBelowEmaLow; }
            set { _outlineColorBearishBelowEmaLow = value; }
        }

        [Browsable(false)]
        public string OutlineColorBearishBelowEmaLowSerialize
        {
            get { return Serialize.BrushToString(_outlineColorBearishBelowEmaLow); }
            set { _outlineColorBearishBelowEmaLow = Serialize.StringToBrush(value); }
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
    public class _EMAwave34 : EMAwave34
    {
    }
}

#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMAwave34[] cache_EMAwave34;
		public EMAwave34 EMAwave34(bool drawArrows)
		{
			return EMAwave34(Input, drawArrows);
		}

		public EMAwave34 EMAwave34(ISeries<double> input, bool drawArrows)
		{
			if (cache_EMAwave34 != null)
				for (int idx = 0; idx < cache_EMAwave34.Length; idx++)
					if (cache_EMAwave34[idx] != null && cache_EMAwave34[idx].DrawArrows == drawArrows && cache_EMAwave34[idx].EqualsInput(input))
						return cache_EMAwave34[idx];
			return CacheIndicator<EMAwave34>(new EMAwave34(){ DrawArrows = drawArrows }, input, ref cache_EMAwave34);
		}

		private _EMAwave34[] cache__EMAwave34;
		public _EMAwave34 _EMAwave34(bool drawArrows)
		{
			return _EMAwave34(Input, drawArrows);
		}

		public _EMAwave34 _EMAwave34(ISeries<double> input, bool drawArrows)
		{
			if (cache__EMAwave34 != null)
				for (int idx = 0; idx < cache__EMAwave34.Length; idx++)
					if (cache__EMAwave34[idx] != null && cache__EMAwave34[idx].DrawArrows == drawArrows && cache__EMAwave34[idx].EqualsInput(input))
						return cache__EMAwave34[idx];
			return CacheIndicator<_EMAwave34>(new _EMAwave34(){ DrawArrows = drawArrows }, input, ref cache__EMAwave34);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMAwave34 EMAwave34(bool drawArrows)
		{
			return indicator.EMAwave34(Input, drawArrows);
		}

		public Indicators.EMAwave34 EMAwave34(ISeries<double> input , bool drawArrows)
		{
			return indicator.EMAwave34(input, drawArrows);
		}

		public Indicators._EMAwave34 _EMAwave34(bool drawArrows)
		{
			return indicator._EMAwave34(Input, drawArrows);
		}

		public Indicators._EMAwave34 _EMAwave34(ISeries<double> input , bool drawArrows)
		{
			return indicator._EMAwave34(input, drawArrows);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMAwave34 EMAwave34(bool drawArrows)
		{
			return indicator.EMAwave34(Input, drawArrows);
		}

		public Indicators.EMAwave34 EMAwave34(ISeries<double> input , bool drawArrows)
		{
			return indicator.EMAwave34(input, drawArrows);
		}

		public Indicators._EMAwave34 _EMAwave34(bool drawArrows)
		{
			return indicator._EMAwave34(Input, drawArrows);
		}

		public Indicators._EMAwave34 _EMAwave34(ISeries<double> input , bool drawArrows)
		{
			return indicator._EMAwave34(input, drawArrows);
		}
	}
}
#endregion

