using Class;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Vector2.Class;
using Vector2.Theme;
using Color = System.Windows.Media.Color;

namespace Visuality
{
    public partial class DetectedPlayerWindow : Window
    {
        // Windows API for forcing window position
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private bool _isInitialized = false;

        // cacheing these to stop recreation
        private SolidColorBrush? _cachedBorderBrush;
        private SolidColorBrush? _cachedForegroundBrush;
        private SolidColorBrush? _cachedTracerBrush;

        public DetectedPlayerWindow()
        {
            InitializeComponent();

            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;

            //Subscribe to my Onlyfans to exclude bad Behavior!
            ThemeManager.ExcludeWindowFromBackground(this);

            Title = "";

            // Subscribe to display changes early
            DisplayManager.DisplayChanged += OnDisplayChanged;

            // Subscribe to property changes
            PropertyChanger.ReceiveDPColor = UpdateDPColor;
            PropertyChanger.ReceiveDPFontSize = UpdateDPFontSize;
            PropertyChanger.ReceiveDPWCornerRadius = ChangeCornerRadius;
            PropertyChanger.ReceiveDPWBorderThickness = ChangeBorderThickness;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Make window click-through
            ClickThroughOverlay.MakeClickThrough(new WindowInteropHelper(this).Handle);

            // Now that we have a window handle, position the window
            if (!_isInitialized)
            {
                _isInitialized = true;
                ForceReposition();
            }
        }

        private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
        {

            // Update position when display changes
            Application.Current.Dispatcher.Invoke(() =>
            {
                ForceReposition();
            });
        }

        public void ForceReposition()
        {
            try
            {

                // Get window handle
                var hwnd = _isInitialized ? new WindowInteropHelper(this).Handle : IntPtr.Zero;

                // Set window state to normal first
                this.WindowState = WindowState.Normal;

                // Position window to cover the current display (accounting for DPI scaling)
                this.Left = DisplayManager.ScreenLeft / WinAPICaller.scalingFactorX;
                this.Top = DisplayManager.ScreenTop / WinAPICaller.scalingFactorY;
                this.Width = DisplayManager.ScreenWidth / WinAPICaller.scalingFactorX;
                this.Height = DisplayManager.ScreenHeight / WinAPICaller.scalingFactorY;

                // Force position with Windows API if we have a handle
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, IntPtr.Zero,
                        DisplayManager.ScreenLeft,
                        DisplayManager.ScreenTop,
                        DisplayManager.ScreenWidth,
                        DisplayManager.ScreenHeight,
                        SWP_NOZORDER | SWP_NOACTIVATE);
                }

                // Maximize to cover entire display
                this.WindowState = WindowState.Maximized;

                // Update tracer start position (changed to be dynamic)
                DetectedTracers.X1 = (DisplayManager.ScreenWidth / 2.0) / WinAPICaller.scalingFactorX;

                string tracerPosition = "Bottom"; // default value
                if (Dictionary.dropdownState.TryGetValue("Tracer Position", out var position))
                {
                    tracerPosition = position.ToString();
                }

                switch (tracerPosition)
                {
                    case "Bottom":
                        DetectedTracers.Y1 = DisplayManager.ScreenHeight / WinAPICaller.scalingFactorY;
                        break;
                    case "Middle":
                        DetectedTracers.Y1 = (DisplayManager.ScreenHeight / 2.0) / WinAPICaller.scalingFactorY;
                        break;
                    case "Top":
                        DetectedTracers.Y1 = 0;
                        break;
                }

                // Force layout update
                this.UpdateLayout();

            }
            catch (Exception ex)
            {
            }
        }

        private void UpdateDPColor(Color NewColor)
        {
            _cachedBorderBrush = new SolidColorBrush(NewColor);
            _cachedForegroundBrush = new SolidColorBrush(NewColor);
            _cachedTracerBrush = new SolidColorBrush(NewColor);

            DetectedPlayerFocus.BorderBrush = _cachedBorderBrush;
            DetectedPlayerHighlight.Background = new SolidColorBrush(Color.FromArgb(50, NewColor.R, NewColor.G, NewColor.B));
            DetectedPlayerCornerBox.Stroke = _cachedBorderBrush;
            DetectedPlayerConfidence.Foreground = _cachedForegroundBrush;
            DetectedTracers.Stroke = _cachedTracerBrush;
        }

        private void UpdateDPFontSize(int newint) => DetectedPlayerConfidence.FontSize = newint;

        private void ChangeCornerRadius(int newint)
        {
            DetectedPlayerFocus.CornerRadius = new CornerRadius(newint);
            DetectedPlayerHighlight.CornerRadius = new CornerRadius(newint);
        }

        private void ChangeBorderThickness(double newdouble)
        {
            DetectedPlayerFocus.BorderThickness = new Thickness(newdouble);
            DetectedPlayerCornerBox.StrokeThickness = newdouble;
            DetectedTracers.StrokeThickness = newdouble;
        }
        public void UpdateHighlightBox(double x, double y, double width, double height)
        {
            DetectedPlayerHighlight.Margin = new Thickness(x, y, 0, 0);
            DetectedPlayerHighlight.Width = width;
            DetectedPlayerHighlight.Height = height;
        }
        public void UpdateCornerBox(double x, double y, double width, double height, int cornerLength)
        {
            // Clamp cornerLength so corners never overlap each other
            var maxCorner = Math.Floor(Math.Min(width, height) / 2.0);
            var cornerLengthDouble = Math.Min((double)cornerLength, maxCorner);

            // Top-left corner
            var topLeft = new PathFigure
            {
                StartPoint = new Point(x, y + cornerLengthDouble),
                Segments = new PathSegmentCollection
                {
                    new LineSegment(new Point(x, y), true),
                    new LineSegment(new Point(x + cornerLengthDouble, y), true)
                }
            };

            // Top-right corner
            var topRight = new PathFigure
            {
                StartPoint = new Point(x + width - cornerLengthDouble, y),
                Segments = new PathSegmentCollection
                {
                    new LineSegment(new Point(x + width, y), true),
                    new LineSegment(new Point(x + width, y + cornerLengthDouble), true)
                }
            };

            // Bottom-right corner
            var bottomRight = new PathFigure
            {
                StartPoint = new Point(x + width, y + height - cornerLengthDouble),
                Segments = new PathSegmentCollection
                {
                    new LineSegment(new Point(x + width, y + height), true),
                    new LineSegment(new Point(x + width - cornerLengthDouble, y + height), true)
                }
            };

            // Bottom-left corner
            var bottomLeft = new PathFigure
            {
                StartPoint = new Point(x + cornerLengthDouble, y + height),
                Segments = new PathSegmentCollection
                {
                    new LineSegment(new Point(x, y + height), true),
                    new LineSegment(new Point(x, y + height - cornerLengthDouble), true)
                }
            };

            var geometry = new PathGeometry
            {
                Figures = new PathFigureCollection
                {
                    topLeft,
                    topRight,
                    bottomRight,
                    bottomLeft
                }
            };

            DetectedPlayerCornerBox.Data = geometry;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        // Clean up event subscription
        protected override void OnClosed(EventArgs e)
        {
            DisplayManager.DisplayChanged -= OnDisplayChanged;
            base.OnClosed(e);
        }
    }
}