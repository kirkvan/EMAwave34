using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;

namespace NinjaTrader.NinjaScript.Strategies
{
    [DisplayName("EMAwave34 Strategy")]
    public class EMAwave34Strategy : Strategy, IEMAwave34LoggingConfig
    {
        private EMAwave34 _indicator;
        private ATR _atr;
        private const int AtrPeriod = 14;
        private EMAwave34InfoPanel _infoPanel;
        private EMAwave34ControlPanel _controlPanel;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private bool _timesValid;
        private double _sessionStartRealized;
        private bool _haltTradingForSession;
        private bool _loggedIndicatorStatus;
        private bool _strategyEnabled = true;
        private bool _enableReverseOnSignal = true;
        private bool _isInPosition;
        private bool _lastControlPanelInPosition;
        private bool _lastControlPanelStrategyEnabled = true;
        private bool _lastControlPanelDisplayInfoPanel = true;
        private DateTime _lastDisplayInfoPanelToggleTime = DateTime.MinValue;
        private MarketPosition _lastMarketPosition = MarketPosition.Flat;
        private double _entryPrice;
        private double _highestSinceEntry;
        private double _lowestSinceEntry;
        private double _activeStopPrice = double.NaN;
        private double _activeTargetPrice = double.NaN;
        private bool _breakevenActivated;
        private int _lastScaleInBar = -1;

        private int _positionQuantity = 1;
        private double _maxLoss = 500;
        private double _maxProfit = 500;
        private string _tradingStartTime = "09:29:59 AM";
        private string _tradingEndTime = "11:29:59 AM";
        private bool _enableTradingHours;
        private double _profitTargetAtr = 2.0;
        private double _stopLossAtr = 1.5;
        private bool _enableTrailingStop = true;
        private double _trailActivationAtr = 1.5;
        private double _trailAtrMult = 1.2;
        private bool _enableBreakeven = true;
        private double _breakevenAtr = 1.0;
        private int _breakevenPlusTicks = 1;
        private double _scaleInStartAtr;
        private double _scaleInStopAtr;
        private int _maxAdditionalEntries = 1;
        private bool _displayInfoPanel = true;
        private int _infoPanelFontSize = 11;
        private TextPosition _infoPanelPosition = TextPosition.TopRight;
        private bool _enableDebugLogging;

        private int _emaHighPeriod = 34;
        private int _emaClosePeriod = 34;
        private int _emaLowPeriod = 34;
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
        private EMAwave34ServiceMacdFilter _macdFilter;
        private EMAwave34ServiceVrocFilter _vrocFilter;
        private EMAwave34ServiceHmaFilter _hmaFilter;
        private HMA _hmaIndicator;

        private bool _enableMacdFilter;
        private int _macdFast = 12;
        private int _macdSlow = 26;
        private int _macdSmooth = 9;
        private double _macdHistogramThreshold = 0.0;
        private bool _macdReadyLogged;

        private bool _enableVrocFilter;
        private int _vrocPeriod = 14;
        private int _vrocSmooth = 3;
        private double _vrocMin = 0.0;
        private bool _vrocReadyLogged;
        private bool _enableHmaFilter = true;
        private int _hmaPeriod = 144;
        private bool _hmaReadyLogged;
        private Brush _hmaLineColor = Brushes.Purple;
        private int _hmaLineWidth = 2;

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

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "EMAwave34 Strategy";
                Description = "Strategy based on the EMAwave34 indicator.";
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
                EnableTradingHours = false;
                ProfitTargetAtr = 2.0;
                StopLossAtr = 1.5;
                EnableTrailingStop = true;
                TrailActivationAtr = 1.5;
                TrailAtrMult = 1.2;
                EnableBreakeven = true;
                BreakevenAtr = 1.0;
                BreakevenPlusTicks = 1;
                ScaleInStartAtr = 1.0;
                ScaleInStopAtr = 3.0;
                MaxAdditionalEntries = 70;
                DisplayInfoPanel = true;
                InfoPanelFontSize = 11;
                InfoPanelPosition = TextPosition.TopRight;
                EnableReverseOnSignal = true;
                _strategyEnabled = true;

                Emah = 34;
                Emac = 34;
                Emal = 34;
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

                BarColorBullishAboveEmaHigh = Brushes.Chartreuse;
                BarColorBearishAboveEmaHigh = Brushes.Green;
                BarColorBullishInsideBand = Brushes.LightBlue;
                BarColorBearishInsideBand = Brushes.RoyalBlue;
                BarColorBullishBelowEmaLow = Brushes.DarkOrange;
                BarColorBearishBelowEmaLow = Brushes.Red;

                OutlineColorBullishAboveEmaHigh = Brushes.Chartreuse;
                OutlineColorBearishAboveEmaHigh = Brushes.Green;
                OutlineColorBullishInsideBand = Brushes.LightBlue;
                OutlineColorBearishInsideBand = Brushes.RoyalBlue;
                OutlineColorBullishBelowEmaLow = Brushes.DarkOrange;
                OutlineColorBearishBelowEmaLow = Brushes.Red;
                EnableMacdFilter = false;
                MacdFast = 12;
                MacdSlow = 26;
                MacdSmooth = 9;
                MacdHistogramThreshold = 0.0;

                EnableVrocFilter = false;
                VrocPeriod = 14;
                VrocSmooth = 3;
                VrocMin = 0.0;
                EnableHmaFilter = true;
                HmaPeriod = 144;
                HmaLineColor = Brushes.Purple;
                HmaLineWidth = 2;

                EnableDebugLogging = false;
            }
            else if (State == State.Configure)
            {
                EntriesPerDirection = Math.Max(1, 1 + MaxAdditionalEntries);
            }
            else if (State == State.DataLoaded)
            {
                _indicator = EMAwave34(DrawArrows);
                _atr = ATR(AtrPeriod);
                _macdFilter = new EMAwave34ServiceMacdFilter(this, MacdFast, MacdSlow, MacdSmooth, MacdHistogramThreshold, EnableMacdFilter);
                _vrocFilter = new EMAwave34ServiceVrocFilter(this, VrocPeriod, VrocSmooth, VrocMin, EnableVrocFilter);
                _hmaFilter = new EMAwave34ServiceHmaFilter(this, HmaPeriod, EnableHmaFilter);
                _hmaIndicator = _hmaFilter?.Indicator;
                _infoPanel = new EMAwave34InfoPanel(this);
                _infoPanel.Initialize();
                ApplyIndicatorSettings();
                ParseTradingTimes();
                if (ChartControl != null && !IsInStrategyAnalyzer)
                    EnsureControlPanel();

                if (EnableDebugLogging)
                {
                    EMAwave34ServiceLogger.Info(() =>
                        $"[INIT] Indicator created. DrawArrows={DrawArrows} ShowMABands={ShowMABands} " +
                        $"Colorzone={Colorzone} Colorbars={Colorbars} ColorOutline={ColorOutline} Zopacity={Zopacity}", this);
                    if (!EnableMacdFilter)
                        EMAwave34ServiceLogger.Info(() => "[FILTER] MACD disabled; gating bypassed.", this);
                    if (!EnableVrocFilter)
                        EMAwave34ServiceLogger.Info(() => "[FILTER] VROC disabled; gating bypassed.", this);
                    if (!EnableHmaFilter)
                        EMAwave34ServiceLogger.Info(() => "[FILTER] HMA disabled; gating bypassed.", this);
                }

                if (ChartControl != null && !IsInStrategyAnalyzer)
                {
                    try
                    {
                        AddChartIndicator(_indicator);
                        if (EnableDebugLogging)
                            EMAwave34ServiceLogger.Info(() => "[CHART] Added EMAwave34 indicator to chart.", this);
                    }
                    catch (Exception ex)
                    {
                        EMAwave34ServiceLogger.Warn(() => $"[CHART] AddChartIndicator failed: {ex.Message}", this);
                    }

                    if (EnableHmaFilter && _hmaIndicator != null)
                    {
                        try
                        {
                            _hmaIndicator.Plots[0].Brush = HmaLineColor;
                            _hmaIndicator.Plots[0].Width = HmaLineWidth;
                            AddChartIndicator(_hmaIndicator);
                            if (EnableDebugLogging)
                                EMAwave34ServiceLogger.Info(() => "[CHART] Added HMA indicator to chart.", this);
                        }
                        catch (Exception ex)
                        {
                            EMAwave34ServiceLogger.Warn(() => $"[CHART] AddChartIndicator (HMA) failed: {ex.Message}", this);
                        }
                    }
                }
                else if (EnableDebugLogging)
                {
                    EMAwave34ServiceLogger.Info(() =>
                        $"[CHART] Skipped AddChartIndicator (ChartControl null={ChartControl == null}, IsInStrategyAnalyzer={IsInStrategyAnalyzer}).", this);
                }
            }
            else if (State == State.Terminated)
            {
                RemoveControlPanel();
                _infoPanel?.Dispose();
                _infoPanel = null;
            }
        }

        protected override void OnBarUpdate()
        {
            _infoPanel?.OnBarUpdate();
            EnsureControlPanel();
            if (_indicator == null || CurrentBar < 1)
            {
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }

            if (EnableDebugLogging && !_loggedIndicatorStatus)
            {
                _loggedIndicatorStatus = true;
                EMAwave34ServiceLogger.Debug(() =>
                    $"[INDICATOR] FirstBar={CurrentBar} EmaHigh={_indicator.EmaHigh[0]:F2} EmaLow={_indicator.EmaLow[0]:F2} " +
                    $"MAnalyzer={_indicator.MAnalyzer[0]:F2}", this);
            }
            if (EnableDebugLogging)
            {
                EMAwave34ServiceLogger.Debug(() =>
                    $"[BAR] Time={Times[0][0]:yyyy-MM-dd HH:mm:ss} Close={Close[0]:F2} " +
                    $"EmaHigh={_indicator.EmaHigh[0]:F2} EmaClose={_indicator.EmaClose[0]:F2} EmaLow={_indicator.EmaLow[0]:F2}",
                    this);
            }
            if (EnableDebugLogging)
            {
                if (!EnableMacdFilter)
                {
                    _macdReadyLogged = false;
                }
                else if (_macdFilter != null && !_macdReadyLogged && _macdFilter.IsReady)
                {
                    _macdReadyLogged = true;
                    EMAwave34ServiceLogger.Info(() =>
                        $"[FILTER_READY] MACD ready. Hist={_macdFilter.Histogram:F2} Threshold={MacdHistogramThreshold:F2}",
                        this);
                }

                if (!EnableVrocFilter)
                {
                    _vrocReadyLogged = false;
                }
                else if (_vrocFilter != null && !_vrocReadyLogged && _vrocFilter.IsReady)
                {
                    _vrocReadyLogged = true;
                    EMAwave34ServiceLogger.Info(() =>
                        $"[FILTER_READY] VROC ready. Value={_vrocFilter.Value:F2} Min={VrocMin:F2}",
                        this);
                }

                if (!EnableHmaFilter)
                {
                    _hmaReadyLogged = false;
                }
                else if (_hmaFilter != null && !_hmaReadyLogged && _hmaFilter.IsReady)
                {
                    _hmaReadyLogged = true;
                    EMAwave34ServiceLogger.Info(() =>
                        $"[FILTER_READY] HMA ready. Value={_hmaFilter.Value:F2} Period={HmaPeriod}",
                        this);
                }
            }

            if (Bars.IsFirstBarOfSession)
            {
                _sessionStartRealized = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                _haltTradingForSession = false;
            }

            double sessionPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - _sessionStartRealized;
            if (!_haltTradingForSession && (sessionPnL <= -MaxLoss || sessionPnL >= MaxProfit))
            {
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[EXIT_MAXPNL] PnL={sessionPnL:F2} MaxLoss=-{MaxLoss:F0} MaxProfit={MaxProfit:F0} " +
                        $"Pos={Position.MarketPosition} Close={Close[0]:F2} EmaHigh={_indicator.EmaHigh[0]:F2} EmaLow={_indicator.EmaLow[0]:F2}",
                        this);
                if (Position.MarketPosition == MarketPosition.Long)
                    ExitLong("MaxPnLExit");
                else if (Position.MarketPosition == MarketPosition.Short)
                    ExitShort("MaxPnLExit");

                _haltTradingForSession = true;
            }
            bool longSignal = _indicator.MAnalyzer[0] > 0 && _indicator.MAnalyzer[1] <= 0;
            bool shortSignal = _indicator.MAnalyzer[0] < 0 && _indicator.MAnalyzer[1] >= 0;
            bool macdLongPass = _macdFilter == null || _macdFilter.PassLong();
            bool macdShortPass = _macdFilter == null || _macdFilter.PassShort();
            bool vrocPass = _vrocFilter == null || _vrocFilter.Pass();
            bool hmaLongPass = _hmaFilter == null || _hmaFilter.PassLong();
            bool hmaShortPass = _hmaFilter == null || _hmaFilter.PassShort();
            bool longAllowed = longSignal && macdLongPass && vrocPass && hmaLongPass;
            bool shortAllowed = shortSignal && macdShortPass && vrocPass && hmaShortPass;
            bool reverseGate = EnableReverseOnSignal &&
                               !_haltTradingForSession &&
                               _strategyEnabled &&
                               IsWithinTradingWindow(Times[0][0]);

            if (reverseGate && Position.MarketPosition == MarketPosition.Long && shortAllowed)
            {
                double atrValue = _atr != null ? _atr[0] : double.NaN;
                if (double.IsNaN(atrValue) || atrValue <= 0)
                {
                    UpdateControlPanelState();
                    RenderInfoPanel();
                    return;
                }
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[REVERSE] Long -> Short signal accepted. ATR={atrValue:F2} qty={Position.Quantity}", this);

                double targetPrice = Close[0] - atrValue * ProfitTargetAtr;
                double stopPrice = Close[0] + atrValue * StopLossAtr;
                SetProfitTarget("Short", CalculationMode.Price, targetPrice);
                SetStopLoss("Short", CalculationMode.Price, stopPrice, false);
                _activeStopPrice = stopPrice;
                _activeTargetPrice = targetPrice;

                int reverseQty = Math.Max(1, Position.Quantity);
                ExitLong("ReverseLong");
                EnterShort(reverseQty, "Short");
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }

            if (reverseGate && Position.MarketPosition == MarketPosition.Short && longAllowed)
            {
                double atrValue = _atr != null ? _atr[0] : double.NaN;
                if (double.IsNaN(atrValue) || atrValue <= 0)
                {
                    UpdateControlPanelState();
                    RenderInfoPanel();
                    return;
                }
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[REVERSE] Short -> Long signal accepted. ATR={atrValue:F2} qty={Position.Quantity}", this);

                double targetPrice = Close[0] + atrValue * ProfitTargetAtr;
                double stopPrice = Close[0] - atrValue * StopLossAtr;
                SetProfitTarget("Long", CalculationMode.Price, targetPrice);
                SetStopLoss("Long", CalculationMode.Price, stopPrice, false);
                _activeStopPrice = stopPrice;
                _activeTargetPrice = targetPrice;

                int reverseQty = Math.Max(1, Position.Quantity);
                ExitShort("ReverseShort");
                EnterLong(reverseQty, "Long");
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }

            if (Position.MarketPosition == MarketPosition.Long && Close[0] < _indicator.EmaLow[0])
            {
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[EXIT_RULE] Long: Close={Close[0]:F2} < EmaLow={_indicator.EmaLow[0]:F2} " +
                        $"EmaHigh={_indicator.EmaHigh[0]:F2} EmaClose={_indicator.EmaClose[0]:F2}",
                        this);
                ExitLong("StopLong");
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }
            if (Position.MarketPosition == MarketPosition.Short && Close[0] > _indicator.EmaHigh[0])
            {
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[EXIT_RULE] Short: Close={Close[0]:F2} > EmaHigh={_indicator.EmaHigh[0]:F2} " +
                        $"EmaLow={_indicator.EmaLow[0]:F2} EmaClose={_indicator.EmaClose[0]:F2}",
                        this);
                ExitShort("StopShort");
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }
            UpdatePositionTracking();
            UpdateTrailingStopsIfNeeded();

            if (_haltTradingForSession)
            {
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }
            if (!_strategyEnabled)
            {
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }

            if (!IsWithinTradingWindow(Times[0][0]))
            {
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }
            TryScaleInByAtrWindow();

            if (EnableDebugLogging && (longSignal || shortSignal))
            {
                double macdHist = _macdFilter != null ? _macdFilter.Histogram : double.NaN;
                bool macdReady = _macdFilter != null && _macdFilter.IsReady;
                double vrocValue = _vrocFilter != null ? _vrocFilter.Value : double.NaN;
                bool vrocReady = _vrocFilter != null && _vrocFilter.IsReady;
                double hmaValue = _hmaFilter != null ? _hmaFilter.Value : double.NaN;
                bool hmaReady = _hmaFilter != null && _hmaFilter.IsReady;

                if (longSignal)
                {
                    bool allowed = macdLongPass && vrocPass && hmaLongPass;
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[ENTRY_DECISION] Signal=Long Allowed={allowed} " +
                        $"MACD(pass={macdLongPass} hist={macdHist:F2} thr={MacdHistogramThreshold:F2} ready={macdReady} enabled={EnableMacdFilter}) " +
                        $"VROC(pass={vrocPass} value={vrocValue:F2} min={VrocMin:F2} ready={vrocReady} enabled={EnableVrocFilter}) " +
                        $"HMA(pass={hmaLongPass} value={hmaValue:F2} period={HmaPeriod} ready={hmaReady} enabled={EnableHmaFilter})",
                        this);
                }

                if (shortSignal)
                {
                    bool allowed = macdShortPass && vrocPass && hmaShortPass;
                    EMAwave34ServiceLogger.Debug(() =>
                        $"[ENTRY_DECISION] Signal=Short Allowed={allowed} " +
                        $"MACD(pass={macdShortPass} hist={macdHist:F2} thr={MacdHistogramThreshold:F2} ready={macdReady} enabled={EnableMacdFilter}) " +
                        $"VROC(pass={vrocPass} value={vrocValue:F2} min={VrocMin:F2} ready={vrocReady} enabled={EnableVrocFilter}) " +
                        $"HMA(pass={hmaShortPass} value={hmaValue:F2} period={HmaPeriod} ready={hmaReady} enabled={EnableHmaFilter})",
                        this);
                }
            }
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                double atrValue = _atr != null ? _atr[0] : double.NaN;
                if (double.IsNaN(atrValue) || atrValue <= 0)
                {
                    UpdateControlPanelState();
                    RenderInfoPanel();
                    return;
                }
                if (longAllowed)
                {
                    if (EnableDebugLogging)
                        EMAwave34ServiceLogger.Debug(() =>
                            $"[ENTRY_SIGNAL] Long: Close={Close[0]:F2} EmaHigh={_indicator.EmaHigh[0]:F2} " +
                            $"EmaLow={_indicator.EmaLow[0]:F2} MAnalyzer={_indicator.MAnalyzer[0]:F2}->{_indicator.MAnalyzer[1]:F2} ATR={atrValue:F2}",
                            this);
                    double targetPrice = Close[0] + atrValue * ProfitTargetAtr;
                    double stopPrice = Close[0] - atrValue * StopLossAtr;
                    SetProfitTarget("Long", CalculationMode.Price, targetPrice);
                    SetStopLoss("Long", CalculationMode.Price, stopPrice, false);
                    _activeStopPrice = stopPrice;
                    _activeTargetPrice = targetPrice;
                    if (EnableDebugLogging)
                        EMAwave34ServiceLogger.Info(() => $"[ATR] Long entry ATR={atrValue:F2} Target={targetPrice:F2} Stop={stopPrice:F2}", this);
                    EnterLong(PositionQuantity, "Long");
                }
                else if (shortAllowed)
                {
                    if (EnableDebugLogging)
                        EMAwave34ServiceLogger.Debug(() =>
                            $"[ENTRY_SIGNAL] Short: Close={Close[0]:F2} EmaHigh={_indicator.EmaHigh[0]:F2} " +
                            $"EmaLow={_indicator.EmaLow[0]:F2} MAnalyzer={_indicator.MAnalyzer[0]:F2}->{_indicator.MAnalyzer[1]:F2} ATR={atrValue:F2}",
                            this);
                    double targetPrice = Close[0] - atrValue * ProfitTargetAtr;
                    double stopPrice = Close[0] + atrValue * StopLossAtr;
                    SetProfitTarget("Short", CalculationMode.Price, targetPrice);
                    SetStopLoss("Short", CalculationMode.Price, stopPrice, false);
                    _activeStopPrice = stopPrice;
                    _activeTargetPrice = targetPrice;
                    if (EnableDebugLogging)
                        EMAwave34ServiceLogger.Info(() => $"[ATR] Short entry ATR={atrValue:F2} Target={targetPrice:F2} Stop={stopPrice:F2}", this);
                    EnterShort(PositionQuantity, "Short");
                }
                UpdateControlPanelState();
                RenderInfoPanel();
                return;
            }
            UpdateControlPanelState();

            RenderInfoPanel();
        }

        private void ApplyIndicatorSettings()
        {
            _indicator.Emah = Emah;
            _indicator.Emac = Emac;
            _indicator.Emal = Emal;
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

            _indicator.BarColorBullishAboveEmaHigh = BarColorBullishAboveEmaHigh;
            _indicator.BarColorBearishAboveEmaHigh = BarColorBearishAboveEmaHigh;
            _indicator.BarColorBullishInsideBand = BarColorBullishInsideBand;
            _indicator.BarColorBearishInsideBand = BarColorBearishInsideBand;
            _indicator.BarColorBullishBelowEmaLow = BarColorBullishBelowEmaLow;
            _indicator.BarColorBearishBelowEmaLow = BarColorBearishBelowEmaLow;

            _indicator.OutlineColorBullishAboveEmaHigh = OutlineColorBullishAboveEmaHigh;
            _indicator.OutlineColorBearishAboveEmaHigh = OutlineColorBearishAboveEmaHigh;
            _indicator.OutlineColorBullishInsideBand = OutlineColorBullishInsideBand;
            _indicator.OutlineColorBearishInsideBand = OutlineColorBearishInsideBand;
            _indicator.OutlineColorBullishBelowEmaLow = OutlineColorBullishBelowEmaLow;
            _indicator.OutlineColorBearishBelowEmaLow = OutlineColorBearishBelowEmaLow;
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
            if (!EnableTradingHours)
                return true;
            if (!_timesValid)
                return true;
            bool useWallClock = State == State.Realtime && !IsInStrategyAnalyzer &&
                                (Account == null || !string.Equals(Account.Name, "Playback101", StringComparison.OrdinalIgnoreCase));
            TimeSpan t = useWallClock ? DateTime.Now.TimeOfDay : barTime.TimeOfDay;
            if (_startTime <= _endTime)
                return t >= _startTime && t <= _endTime;

            return t >= _startTime || t <= _endTime;
        }

        private void RenderInfoPanel()
        {
            if (ChartControl == null || State == State.Terminated)
                return;

            if (IsInStrategyAnalyzer)
                return;

            if (!DisplayInfoPanel)
            {
                try { RemoveDrawObject("InfoPanel"); } catch { }
                return;
            }

            if (_infoPanel == null)
                return;

            try
            {
                string displayText = _infoPanel.GenerateDisplayText();
                if (string.IsNullOrEmpty(displayText))
                    return;

                Draw.TextFixed(this, "InfoPanel", displayText, InfoPanelPosition,
                    Brushes.White, new SimpleFont("Arial", InfoPanelFontSize),
                    Brushes.Transparent, Brushes.Transparent, 0);
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[INFO_PANEL] Error rendering info panel: {ex.Message}", this);
            }
        }

        #region Control Panel

        private void EnsureControlPanel()
        {
            if (_controlPanel != null || ChartControl == null || IsInStrategyAnalyzer)
                return;
            if (EnableDebugLogging)
                EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] EnsureControlPanel: dispatch insert (ChartControl null={ChartControl == null}, Analyzer={IsInStrategyAnalyzer})", this);

            if (ChartControl.Dispatcher.CheckAccess())
            {
                InsertControlPanel();
            }
            else
            {
                ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                {
                    if (_controlPanel == null && !IsInStrategyAnalyzer)
                        InsertControlPanel();
                }));
            }
        }

        private void InsertControlPanel()
        {
            try
            {
                if (ChartControl == null)
                    return;
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] InsertControlPanel: creating panel (StrategyEnabled={_strategyEnabled}, DisplayInfoPanel={DisplayInfoPanel}, InPosition={_isInPosition})", this);

                _controlPanel = new EMAwave34ControlPanel(this);

                if (InfoPanelPosition == TextPosition.TopLeft)
                {
                    _controlPanel.HorizontalAlignment = HorizontalAlignment.Right;
                    _controlPanel.VerticalAlignment = VerticalAlignment.Top;
                    _controlPanel.Margin = new Thickness(0, 10, 10, 0);
                }
                else
                {
                    _controlPanel.HorizontalAlignment = HorizontalAlignment.Left;
                    _controlPanel.VerticalAlignment = VerticalAlignment.Top;
                    _controlPanel.Margin = new Thickness(10, 10, 0, 0);
                }

                _controlPanel.EnableStrategyClicked += OnControlPanel_EnableStrategyClicked;
                _controlPanel.DisplayInfoPanelClicked += OnControlPanel_DisplayInfoPanelClicked;

                UserControlCollection.Add(_controlPanel);

                _controlPanel.SetStrategyEnabled(_strategyEnabled);
                _controlPanel.SetInPosition(_isInPosition);
                _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);

                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => "[CONTROL_PANEL] InsertControlPanel: initialized and added to UserControlCollection", this);
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[CONTROL_PANEL] Error inserting control panel: {ex.Message}", this);
            }
        }

        private void RemoveControlPanel()
        {
            try
            {
                if (_controlPanel == null)
                    return;
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => "[CONTROL_PANEL] RemoveControlPanel: removing panel", this);

                if (ChartControl != null && !ChartControl.Dispatcher.CheckAccess())
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(RemoveControlPanel));
                    return;
                }

                _controlPanel.EnableStrategyClicked -= OnControlPanel_EnableStrategyClicked;
                _controlPanel.DisplayInfoPanelClicked -= OnControlPanel_DisplayInfoPanelClicked;

                UserControlCollection.Remove(_controlPanel);
                _controlPanel = null;
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[CONTROL_PANEL] Error removing control panel: {ex.Message}", this);
            }
        }

        private void OnControlPanel_EnableStrategyClicked(object sender, EventArgs e)
        {
            try
            {
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] EnableStrategy CLICK: before toggle StrategyEnabled={_strategyEnabled}, DisplayInfoPanel={DisplayInfoPanel}", this);
                _strategyEnabled = !_strategyEnabled;
                if (_strategyEnabled && !DisplayInfoPanel)
                {
                    DisplayInfoPanel = true;
                    _infoPanel?.Invalidate();
                }
                if (_strategyEnabled)
                {
                    _sessionStartRealized = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                    _haltTradingForSession = false;
                }

                if (_controlPanel != null && ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        _controlPanel.SetStrategyEnabled(_strategyEnabled);
                        _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);
                    }));
                }
                else if (_controlPanel != null)
                {
                    _controlPanel.SetStrategyEnabled(_strategyEnabled);
                    _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);
                }

                _infoPanel?.Invalidate();
                UpdateControlPanelState();
                if (!DisplayInfoPanel)
                {
                    try { RemoveDrawObject("InfoPanel"); } catch { }
                }
                else
                {
                    RenderInfoPanel();
                }
                ChartControl?.InvalidateVisual();

                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] EnableStrategy CLICK: after toggle StrategyEnabled={_strategyEnabled}, DisplayInfoPanel={DisplayInfoPanel}", this);
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[CONTROL_PANEL] Error toggling strategy enabled: {ex.Message}", this);
            }
        }

        private void OnControlPanel_DisplayInfoPanelClicked(object sender, EventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                if ((now - _lastDisplayInfoPanelToggleTime).TotalMilliseconds < 250)
                    return;
                _lastDisplayInfoPanelToggleTime = now;
                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] DisplayInfoPanel CLICK: before toggle DisplayInfoPanel={DisplayInfoPanel}, StrategyEnabled={_strategyEnabled}", this);

                DisplayInfoPanel = !DisplayInfoPanel;
                _infoPanel?.Invalidate();
                UpdateControlPanelState();
                if (!DisplayInfoPanel)
                {
                    try { RemoveDrawObject("InfoPanel"); } catch { }
                }
                else
                {
                    RenderInfoPanel();
                }

                if (_controlPanel != null && ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);
                    }));
                }
                else if (_controlPanel != null)
                {
                    _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);
                }

                ChartControl?.InvalidateVisual();

                if (EnableDebugLogging)
                    EMAwave34ServiceLogger.Debug(() => $"[CONTROL_PANEL] DisplayInfoPanel CLICK: after toggle DisplayInfoPanel={DisplayInfoPanel}", this);
            }
            catch (Exception ex)
            {
                EMAwave34ServiceLogger.Error(() => $"[CONTROL_PANEL] Error toggling Display Info Panel: {ex.Message}", this);
            }
        }

        private void UpdateControlPanelState()
        {
            bool inPosition = Position.MarketPosition != MarketPosition.Flat;
            if (_isInPosition != inPosition)
                _isInPosition = inPosition;

            if (_lastControlPanelInPosition == _isInPosition &&
                _lastControlPanelStrategyEnabled == _strategyEnabled &&
                _lastControlPanelDisplayInfoPanel == DisplayInfoPanel)
                return;

            _lastControlPanelInPosition = _isInPosition;
            _lastControlPanelStrategyEnabled = _strategyEnabled;
            _lastControlPanelDisplayInfoPanel = DisplayInfoPanel;

            if (_controlPanel != null && ChartControl != null && !IsInStrategyAnalyzer)
            {
                ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                {
                    _controlPanel.SetStrategyEnabled(_strategyEnabled);
                    _controlPanel.SetInPosition(_isInPosition);
                    _controlPanel.SetDisplayInfoPanel(DisplayInfoPanel);
                }));
            }
        }

        private void UpdatePositionTracking()
        {
            if (Position.MarketPosition != _lastMarketPosition)
            {
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    _entryPrice = 0;
                    _highestSinceEntry = 0;
                    _lowestSinceEntry = 0;
                    _activeStopPrice = double.NaN;
                    _activeTargetPrice = double.NaN;
                    _breakevenActivated = false;
                    _lastScaleInBar = -1;
                }
                else
                {
                    _entryPrice = Position.AveragePrice > 0 ? Position.AveragePrice : Close[0];
                    _highestSinceEntry = High[0];
                    _lowestSinceEntry = Low[0];
                    _breakevenActivated = false;
                    _lastScaleInBar = -1;
                }
                _lastMarketPosition = Position.MarketPosition;
            }

            if (Position.MarketPosition == MarketPosition.Long)
                _highestSinceEntry = Math.Max(_highestSinceEntry, High[0]);
            else if (Position.MarketPosition == MarketPosition.Short)
                _lowestSinceEntry = Math.Min(_lowestSinceEntry, Low[0]);
        }

        private void UpdateTrailingStopsIfNeeded()
        {
            if (Position.MarketPosition == MarketPosition.Flat)
                return;

            double atrValue = _atr != null ? _atr[0] : double.NaN;
            if (double.IsNaN(atrValue) || atrValue <= 0)
                return;

            if (Position.MarketPosition == MarketPosition.Long)
            {
                double stopPrice = _activeStopPrice;

                if (EnableBreakeven && Close[0] - _entryPrice >= BreakevenAtr * atrValue)
                {
                    if (!_breakevenActivated)
                    {
                        _breakevenActivated = true;
                        if (EnableDebugLogging)
                            EMAwave34ServiceLogger.Debug(() => $"[BREAKEVEN] Long activated at bar {CurrentBar}.", this);
                    }
                    double breakevenStop = _entryPrice + BreakevenPlusTicks * TickSize;
                    stopPrice = double.IsNaN(stopPrice) ? breakevenStop : Math.Max(stopPrice, breakevenStop);
                }

                if (EnableTrailingStop && Close[0] - _entryPrice >= TrailActivationAtr * atrValue)
                {
                    double trailStop = _highestSinceEntry - atrValue * TrailAtrMult;
                    stopPrice = double.IsNaN(stopPrice) ? trailStop : Math.Max(stopPrice, trailStop);
                }

                if (!double.IsNaN(stopPrice))
                    UpdateStopLossPrice("Long", stopPrice);
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                double stopPrice = _activeStopPrice;

                if (EnableBreakeven && _entryPrice - Close[0] >= BreakevenAtr * atrValue)
                {
                    if (!_breakevenActivated)
                    {
                        _breakevenActivated = true;
                        if (EnableDebugLogging)
                            EMAwave34ServiceLogger.Debug(() => $"[BREAKEVEN] Short activated at bar {CurrentBar}.", this);
                    }
                    double breakevenStop = _entryPrice - BreakevenPlusTicks * TickSize;
                    stopPrice = double.IsNaN(stopPrice) ? breakevenStop : Math.Min(stopPrice, breakevenStop);
                }

                if (EnableTrailingStop && _entryPrice - Close[0] >= TrailActivationAtr * atrValue)
                {
                    double trailStop = _lowestSinceEntry + atrValue * TrailAtrMult;
                    stopPrice = double.IsNaN(stopPrice) ? trailStop : Math.Min(stopPrice, trailStop);
                }

                if (!double.IsNaN(stopPrice))
                    UpdateStopLossPrice("Short", stopPrice);
            }
        }
        private void TryScaleInByAtrWindow()
        {
            if (Position.MarketPosition == MarketPosition.Flat)
                return;
            if (_lastScaleInBar == CurrentBar)
                return;
            if (ScaleInStartAtr <= 0 || ScaleInStopAtr <= 0 || ScaleInStopAtr < ScaleInStartAtr)
                return;

            double atrValue = _atr != null ? _atr[0] : double.NaN;
            if (double.IsNaN(atrValue) || atrValue <= 0)
                return;

            double atrGain = Position.MarketPosition == MarketPosition.Long
                ? (Close[0] - _entryPrice) / atrValue
                : (_entryPrice - Close[0]) / atrValue;

            if (atrGain < ScaleInStartAtr || atrGain > ScaleInStopAtr)
                return;

            int additionalEntries = Math.Max(0, Position.Quantity - PositionQuantity);
            if (additionalEntries >= MaxAdditionalEntries)
                return;

            if (Position.MarketPosition == MarketPosition.Long)
            {
                ApplyActiveExitOrders("Long");
                EnterLong(1, "Long");
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                ApplyActiveExitOrders("Short");
                EnterShort(1, "Short");
            }

            _lastScaleInBar = CurrentBar;
            if (EnableDebugLogging)
                EMAwave34ServiceLogger.Debug(() =>
                    $"[SCALE_IN] {Position.MarketPosition} add=1 atrGain={atrGain:F2} window={ScaleInStartAtr:F2}-{ScaleInStopAtr:F2} " +
                    $"currentQty={Position.Quantity} maxAdditional={MaxAdditionalEntries} bar={CurrentBar}",
                    this);
        }

        private void ApplyActiveExitOrders(string signalName)
        {
            if (!double.IsNaN(_activeTargetPrice))
                SetProfitTarget(signalName, CalculationMode.Price, _activeTargetPrice);
            if (!double.IsNaN(_activeStopPrice))
                SetStopLoss(signalName, CalculationMode.Price, _activeStopPrice, false);
        }

        private void UpdateStopLossPrice(string signalName, double proposedStop)
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (proposedStop >= Close[0])
                    proposedStop = Close[0] - TickSize;
                if (double.IsNaN(_activeStopPrice) || proposedStop > _activeStopPrice + TickSize * 0.5)
                {
                    SetStopLoss(signalName, CalculationMode.Price, proposedStop, false);
                    _activeStopPrice = proposedStop;
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                if (proposedStop <= Close[0])
                    proposedStop = Close[0] + TickSize;
                if (double.IsNaN(_activeStopPrice) || proposedStop < _activeStopPrice - TickSize * 0.5)
                {
                    SetStopLoss(signalName, CalculationMode.Price, proposedStop, false);
                    _activeStopPrice = proposedStop;
                }
            }
        }

        #endregion

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Position Quantity", GroupName = "Strategy Settings", Order = 0)]
        public int PositionQuantity
        {
            get { return _positionQuantity; }
            set { _positionQuantity = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Reverse On Opposite Signal", Description = "Reverse position when the opposite signal passes all filters.", GroupName = "Strategy Settings", Order = 1)]
        public bool EnableReverseOnSignal
        {
            get { return _enableReverseOnSignal; }
            set { _enableReverseOnSignal = value; }
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
        [Display(Name = "Enable Trading Hours", Description = "Enable/disable the trading window filter.", GroupName = "Trading Hours", Order = 0)]
        public bool EnableTradingHours
        {
            get { return _enableTradingHours; }
            set { _enableTradingHours = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Trading Start Time", Description = "AM/PM format, e.g. 09:29:59 AM", GroupName = "Trading Hours", Order = 1)]
        public string TradingStartTime
        {
            get { return _tradingStartTime; }
            set { _tradingStartTime = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Trading End Time", Description = "AM/PM format, e.g. 11:29:59 AM", GroupName = "Trading Hours", Order = 2)]
        public string TradingEndTime
        {
            get { return _tradingEndTime; }
            set { _tradingEndTime = value; }
        }

        [Range(6, 48), NinjaScriptProperty]
        [Display(Name = "Risk & Info Panel Text Font Size", Description = "Font size for the info panel text.", GroupName = "Display Settings", Order = 0)]
        public int InfoPanelFontSize
        {
            get { return _infoPanelFontSize; }
            set { _infoPanelFontSize = Math.Min(48, Math.Max(6, value)); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Info Panel Position", Description = "Fixed position for the info panel.", GroupName = "Display Settings", Order = 1)]
        public TextPosition InfoPanelPosition
        {
            get { return _infoPanelPosition; }
            set { _infoPanelPosition = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Display Info Panel", Description = "Show the on-chart info panel.", GroupName = "Display Settings", Order = 3)]
        public bool DisplayInfoPanel
        {
            get { return _displayInfoPanel; }
            set { _displayInfoPanel = value; }
        }
        [Range(0, 100), NinjaScriptProperty]
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
        [Display(Name = "Enable ATR Chandelier Trail", Description = "Enable ATR-based chandelier trailing stop.", GroupName = "Risk", Order = 4)]
        public bool EnableTrailingStop
        {
            get { return _enableTrailingStop; }
            set { _enableTrailingStop = value; }
        }

        [Range(0.1, 100), NinjaScriptProperty]
        [Display(Name = "Trail Activation ATR", Description = "ATR gain required before trailing activates.", GroupName = "Risk", Order = 5)]
        public double TrailActivationAtr
        {
            get { return _trailActivationAtr; }
            set { _trailActivationAtr = Math.Max(0, value); }
        }

        [Range(0.1, 100), NinjaScriptProperty]
        [Display(Name = "Trail ATR Mult", Description = "ATR multiple for chandelier trail distance.", GroupName = "Risk", Order = 6)]
        public double TrailAtrMult
        {
            get { return _trailAtrMult; }
            set { _trailAtrMult = Math.Max(0.1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable Breakeven", Description = "Move stop to breakeven after ATR gain.", GroupName = "Risk", Order = 7)]
        public bool EnableBreakeven
        {
            get { return _enableBreakeven; }
            set { _enableBreakeven = value; }
        }

        [Range(0.1, 100), NinjaScriptProperty]
        [Display(Name = "Breakeven ATR", Description = "ATR gain required before breakeven move.", GroupName = "Risk", Order = 8)]
        public double BreakevenAtr
        {
            get { return _breakevenAtr; }
            set { _breakevenAtr = Math.Max(0, value); }
        }

        [Range(0, 200), NinjaScriptProperty]
        [Display(Name = "Breakeven Plus (ticks)", Description = "Extra ticks beyond breakeven.", GroupName = "Risk", Order = 9)]
        public int BreakevenPlusTicks
        {
            get { return _breakevenPlusTicks; }
            set { _breakevenPlusTicks = Math.Max(0, value); }
        }

        [Range(0, 100), NinjaScriptProperty]
        [Display(Name = "Scale-In Start ATR", Description = "ATR gain required before scale-ins begin (<= 0 disables).", GroupName = "Scale-In", Order = 0)]
        public double ScaleInStartAtr
        {
            get { return _scaleInStartAtr; }
            set { _scaleInStartAtr = Math.Max(0, value); }
        }

        [Range(0, 100), NinjaScriptProperty]
        [Display(Name = "Scale-In Stop ATR", Description = "ATR gain at which scale-ins stop (<= 0 disables; must be >= Start).", GroupName = "Scale-In", Order = 1)]
        public double ScaleInStopAtr
        {
            get { return _scaleInStopAtr; }
            set { _scaleInStopAtr = Math.Max(0, value); }
        }

        [Range(1, 1000), NinjaScriptProperty]
        [Display(Name = "Max Additional Entries", Description = "Maximum number of additional scale-in entries (min 1, max 1000).", GroupName = "Scale-In", Order = 2)]
        public int MaxAdditionalEntries
        {
            get { return _maxAdditionalEntries; }
            set { _maxAdditionalEntries = Math.Min(1000, Math.Max(1, value)); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable Debug Logging", Description = "Enable verbose EMAwave34ServiceLogger output.", GroupName = "Diagnostics", Order = 0)]
        public bool EnableDebugLogging
        {
            get { return _enableDebugLogging; }
            set { _enableDebugLogging = value; }
        }

        [Browsable(false)]
        public bool IsSessionHalted => _haltTradingForSession;

        [Browsable(false)]
        public double SessionPnL => SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - _sessionStartRealized;

        [Browsable(false)]
        public bool IsWithinTradingWindowNow
        {
            get
            {
                try
                {
                    if (Time == null || Time.Count == 0)
                        return true;
                    return IsWithinTradingWindow(Time[0]);
                }
                catch
                {
                    return true;
                }
            }
        }

        [Browsable(false)]
        public double CurrentAtr
        {
            get
            {
                try
                {
                    return _atr != null ? _atr[0] : double.NaN;
                }
                catch
                {
                    return double.NaN;
                }
            }
        }

        [Browsable(false)]
        public int OriginalPositions => PositionQuantity;

        [Browsable(false)]
        public int ScaleInPositions => Math.Max(0, Position.Quantity - PositionQuantity);

        [Browsable(false)]
        public int MaxScaleInPositions => MaxAdditionalEntries;

        [Browsable(false)]
        public double MacdHistogram
        {
            get { return _macdFilter != null ? _macdFilter.Histogram : double.NaN; }
        }

        [Browsable(false)]
        public bool MacdFilterReady
        {
            get { return _macdFilter != null && _macdFilter.IsReady; }
        }
        [Browsable(false)]
        public int MacdWarmupBarsRequired
        {
            get { return Math.Max(MacdFast, MacdSlow) + MacdSmooth; }
        }

        [Browsable(false)]
        public int MacdWarmupBarsRemaining
        {
            get { return EnableMacdFilter ? Math.Max(0, MacdWarmupBarsRequired - CurrentBar) : 0; }
        }

        [Browsable(false)]
        public double VrocValue
        {
            get { return _vrocFilter != null ? _vrocFilter.Value : double.NaN; }
        }

        [Browsable(false)]
        public bool VrocFilterReady
        {
            get { return _vrocFilter != null && _vrocFilter.IsReady; }
        }

        [Browsable(false)]
        public int VrocWarmupBarsRequired
        {
            get { return VrocPeriod + VrocSmooth; }
        }

        [Browsable(false)]
        public int VrocWarmupBarsRemaining
        {
            get { return EnableVrocFilter ? Math.Max(0, VrocWarmupBarsRequired - CurrentBar) : 0; }
        }
        [Browsable(false)]
        public double HmaValue
        {
            get { return _hmaFilter != null ? _hmaFilter.Value : double.NaN; }
        }

        [Browsable(false)]
        public bool HmaFilterReady
        {
            get { return _hmaFilter != null && _hmaFilter.IsReady; }
        }

        [Browsable(false)]
        public int HmaWarmupBarsRequired
        {
            get { return HmaPeriod; }
        }

        [Browsable(false)]
        public int HmaWarmupBarsRemaining
        {
            get { return EnableHmaFilter ? Math.Max(0, HmaWarmupBarsRequired - CurrentBar) : 0; }
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
        [Display(Name = "Enable MACD Filter", Description = "Gate entries using MACD histogram.", GroupName = "Momentum Filters", Order = 0)]
        public bool EnableMacdFilter
        {
            get { return _enableMacdFilter; }
            set { _enableMacdFilter = value; }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "MACD Fast", Description = "MACD fast EMA period.", GroupName = "Momentum Filters", Order = 1)]
        public int MacdFast
        {
            get { return _macdFast; }
            set { _macdFast = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "MACD Slow", Description = "MACD slow EMA period.", GroupName = "Momentum Filters", Order = 2)]
        public int MacdSlow
        {
            get { return _macdSlow; }
            set { _macdSlow = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "MACD Smooth", Description = "MACD signal smoothing period.", GroupName = "Momentum Filters", Order = 3)]
        public int MacdSmooth
        {
            get { return _macdSmooth; }
            set { _macdSmooth = Math.Max(1, value); }
        }

        [Range(0, double.MaxValue), NinjaScriptProperty]
        [Display(Name = "MACD Histogram Threshold Filter (points)", Description = "Minimum histogram magnitude required (long >=, short <= -).", GroupName = "Momentum Filters", Order = 4)]
        public double MacdHistogramThreshold
        {
            get { return _macdHistogramThreshold; }
            set { _macdHistogramThreshold = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Enable VROC Filter", Description = "Require VROC >= minimum to confirm participation.", GroupName = "Momentum Filters", Order = 10)]
        public bool EnableVrocFilter
        {
            get { return _enableVrocFilter; }
            set { _enableVrocFilter = value; }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "VROC Period", Description = "Volume lookback period for VROC.", GroupName = "Momentum Filters", Order = 11)]
        public int VrocPeriod
        {
            get { return _vrocPeriod; }
            set { _vrocPeriod = Math.Max(1, value); }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "VROC Smooth", Description = "SMA smoothing period for VROC.", GroupName = "Momentum Filters", Order = 12)]
        public int VrocSmooth
        {
            get { return _vrocSmooth; }
            set { _vrocSmooth = Math.Max(1, value); }
        }

        [Range(0, double.MaxValue), NinjaScriptProperty]
        [Display(Name = "VROC Min (%)", Description = "Minimum VROC percent required for entry.", GroupName = "Momentum Filters", Order = 13)]
        public double VrocMin
        {
            get { return _vrocMin; }
            set { _vrocMin = Math.Max(0, value); }
        }
        [NinjaScriptProperty]
        [Display(Name = "Enable HMA Filter", Description = "Require Close relative to HMA for entries.", GroupName = "Momentum Filters", Order = 20)]
        public bool EnableHmaFilter
        {
            get { return _enableHmaFilter; }
            set { _enableHmaFilter = value; }
        }

        [Range(3, 293), NinjaScriptProperty]
        [Display(Name = "HMA Period", Description = "Hull Moving Average period (min 3, max 293).", GroupName = "Momentum Filters", Order = 21)]
        public int HmaPeriod
        {
            get { return _hmaPeriod; }
            set { _hmaPeriod = Math.Max(3, Math.Min(293, value)); }
        }

        [XmlIgnore]
        [Display(Name = "HMA Line Color", Description = "Line color for the HMA on the chart.", GroupName = "Momentum Filters", Order = 22)]
        public Brush HmaLineColor
        {
            get { return _hmaLineColor; }
            set { _hmaLineColor = value; }
        }

        [Browsable(false)]
        public string HmaLineColorSerialize
        {
            get { return Serialize.BrushToString(_hmaLineColor); }
            set { _hmaLineColor = Serialize.StringToBrush(value); }
        }

        [Range(1, 10), NinjaScriptProperty]
        [Display(Name = "HMA Line Width", Description = "Line width for the HMA on the chart.", GroupName = "Momentum Filters", Order = 23)]
        public int HmaLineWidth
        {
            get { return _hmaLineWidth; }
            set { _hmaLineWidth = Math.Min(10, Math.Max(1, value)); }
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
        [Display(Name = "Bar Color: Bullish Above EMA High", Description = "Bar color when Close > EMA High and the bar is bullish.", GroupName = "Indicator Visual", Order = 1)]
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
        [Display(Name = "Bar Color: Bearish Above EMA High", Description = "Bar color when Close > EMA High and the bar is bearish.", GroupName = "Indicator Visual", Order = 2)]
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
        [Display(Name = "Bar Color: Bullish Inside EMA Band", Description = "Bar color when Close is between EMA High/Low and the bar is bullish.", GroupName = "Indicator Visual", Order = 3)]
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
        [Display(Name = "Bar Color: Bearish Inside EMA Band", Description = "Bar color when Close is between EMA High/Low and the bar is bearish.", GroupName = "Indicator Visual", Order = 4)]
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
        [Display(Name = "Bar Color: Bullish Below EMA Low", Description = "Bar color when Close < EMA Low and the bar is bullish.", GroupName = "Indicator Visual", Order = 5)]
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
        [Display(Name = "Bar Color: Bearish Below EMA Low", Description = "Bar color when Close < EMA Low and the bar is bearish.", GroupName = "Indicator Visual", Order = 6)]
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
        [Display(Name = "Outline Color: Bullish Above EMA High", Description = "Outline color when Close > EMA High and the bar is bullish.", GroupName = "Indicator Visual", Order = 7)]
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
        [Display(Name = "Outline Color: Bearish Above EMA High", Description = "Outline color when Close > EMA High and the bar is bearish.", GroupName = "Indicator Visual", Order = 8)]
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
        [Display(Name = "Outline Color: Bullish Inside EMA Band", Description = "Outline color when Close is between EMA High/Low and the bar is bullish.", GroupName = "Indicator Visual", Order = 9)]
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
        [Display(Name = "Outline Color: Bearish Inside EMA Band", Description = "Outline color when Close is between EMA High/Low and the bar is bearish.", GroupName = "Indicator Visual", Order = 10)]
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
        [Display(Name = "Outline Color: Bullish Below EMA Low", Description = "Outline color when Close < EMA Low and the bar is bullish.", GroupName = "Indicator Visual", Order = 11)]
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
        [Display(Name = "Outline Color: Bearish Below EMA Low", Description = "Outline color when Close < EMA Low and the bar is bearish.", GroupName = "Indicator Visual", Order = 12)]
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

        bool IEMAwave34LoggingConfig.EnableDebugLogging => EnableDebugLogging;

        bool IEMAwave34LoggingConfig.IsBacktestContext => State == State.Historical || IsInStrategyAnalyzer;

        EMAwave34LogLevel IEMAwave34LoggingConfig.MinimumLogLevel
        {
            get
            {
                if (EnableDebugLogging)
                    return EMAwave34LogLevel.Debug;
                return ((IEMAwave34LoggingConfig)this).IsBacktestContext ? EMAwave34LogLevel.Error : EMAwave34LogLevel.Warn;
            }
        }
    }
}
