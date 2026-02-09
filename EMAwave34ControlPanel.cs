using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// WPF UserControl for EMAwave34 strategy control panel.
    /// Provides buttons for internal strategy enable and Info Panel toggle.
    /// </summary>
    public class EMAwave34ControlPanel : UserControl
    {
        private const byte PanelBackgroundR = 30;
        private const byte PanelBackgroundG = 30;
        private const byte PanelBackgroundB = 30;

        private const byte ButtonDisabledR = 128;
        private const byte ButtonDisabledG = 128;
        private const byte ButtonDisabledB = 128;

        private const byte ButtonStrategyR = 100;
        private const byte ButtonStrategyG = 181;
        private const byte ButtonStrategyB = 246;

        private Button _enableStrategyButton;
        private Button _displayInfoPanelButton;


        private bool _isStrategyEnabled;
        private bool _isInPosition;
        private bool _displayInfoPanelEnabled = true;

        private readonly SolidColorBrush _grayBrush = new SolidColorBrush(Color.FromRgb(ButtonDisabledR, ButtonDisabledG, ButtonDisabledB));
        private readonly SolidColorBrush _lightBlueBrush = new SolidColorBrush(Color.FromRgb(ButtonStrategyR, ButtonStrategyG, ButtonStrategyB));
        private readonly SolidColorBrush _whiteBrush = new SolidColorBrush(Colors.White);
        private readonly SolidColorBrush _blackBrush = new SolidColorBrush(Colors.Black);

        public event EventHandler EnableStrategyClicked;
        public event EventHandler DisplayInfoPanelClicked;

        public EMAwave34ControlPanel(EMAwave34Strategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var mainGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(PanelBackgroundR, PanelBackgroundG, PanelBackgroundB)),
                Margin = new Thickness(7)
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            _enableStrategyButton = CreateButton("Enable Strategy", 0, _grayBrush);
            _enableStrategyButton.Click += OnEnableStrategyClick;
            mainGrid.Children.Add(_enableStrategyButton);

            _displayInfoPanelButton = CreateButton("Display Info Panel", 2, _grayBrush);
            _displayInfoPanelButton.Click += OnDisplayInfoPanelClick;
            _displayInfoPanelButton.IsEnabled = true;
            mainGrid.Children.Add(_displayInfoPanelButton);

            Content = mainGrid;
            Width = 210;
            Height = 84;
        }

        private Button CreateButton(string content, int row, SolidColorBrush backgroundColor)
        {
            var button = new Button
            {
                Content = content,
                Height = 28,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Background = backgroundColor,
                Foreground = _blackBrush,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            if (row >= 0)
                Grid.SetRow(button, row);

            return button;
        }

        private void OnEnableStrategyClick(object sender, RoutedEventArgs e)
        {
            _isStrategyEnabled = !_isStrategyEnabled;
            UpdateButtonStates();
            EnableStrategyClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnDisplayInfoPanelClick(object sender, RoutedEventArgs e)
        {
            _displayInfoPanelEnabled = !_displayInfoPanelEnabled;
            UpdateButtonStates();
            DisplayInfoPanelClicked?.Invoke(this, EventArgs.Empty);
        }

        public void SetStrategyEnabled(bool enabled)
        {
            if (_isStrategyEnabled != enabled)
            {
                _isStrategyEnabled = enabled;
                UpdateButtonStates();
            }
        }

        public void SetInPosition(bool inPosition)
        {
            if (_isInPosition != inPosition)
            {
                _isInPosition = inPosition;
                UpdateButtonStates();
            }
        }

        public void SetDisplayInfoPanel(bool enabled)
        {
            if (_displayInfoPanelEnabled != enabled)
            {
                _displayInfoPanelEnabled = enabled;
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            if (_isStrategyEnabled)
            {
                _enableStrategyButton.Background = _lightBlueBrush;
                _enableStrategyButton.Foreground = _whiteBrush;
                _enableStrategyButton.IsEnabled = !_isInPosition;
                _enableStrategyButton.Cursor = _isInPosition ? System.Windows.Input.Cursors.No : System.Windows.Input.Cursors.Hand;
            }
            else
            {
                _enableStrategyButton.Background = _grayBrush;
                _enableStrategyButton.Foreground = _blackBrush;
                _enableStrategyButton.IsEnabled = true;
                _enableStrategyButton.Cursor = System.Windows.Input.Cursors.Hand;
            }

            bool canToggleInfoPanel = _isStrategyEnabled;
            _displayInfoPanelButton.IsEnabled = canToggleInfoPanel;

            if (!_isStrategyEnabled)
            {
                _displayInfoPanelButton.Background = _grayBrush;
                _displayInfoPanelButton.Foreground = _blackBrush;
                _displayInfoPanelButton.Cursor = System.Windows.Input.Cursors.No;
            }
            else if (_displayInfoPanelEnabled)
            {
                _displayInfoPanelButton.Background = new SolidColorBrush(Colors.LightGray);
                _displayInfoPanelButton.Foreground = _blackBrush;
                _displayInfoPanelButton.Cursor = System.Windows.Input.Cursors.Hand;
            }
            else
            {
                _displayInfoPanelButton.Background = _grayBrush;
                _displayInfoPanelButton.Foreground = _blackBrush;
                _displayInfoPanelButton.Cursor = System.Windows.Input.Cursors.Hand;
            }
        }
    }
}
