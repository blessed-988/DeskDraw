using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Linq;

namespace DeskDraw
{
    public partial class MainWindow : Window
    {
        private Screen _screen;
        private bool _isDrawing = false;
        private Point _lastPoint = new Point();
        private string _selectedHex = "#FF0000";
        private SolidColorBrush _currentBrush = Brushes.Red;
        private bool _isDraggingToolbar = false;
        private Point _toolbarDragStart = new Point();

        public string SelectedHex
        {
            get => _selectedHex;
            set
            {
                _selectedHex = value;
                OnPropertyChanged();
                UpdateBrush();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            BrushSettings.ThicknessChanged += OnThicknessChanged;
            BrushSettings.ColorChanged += OnColorChanged;
            ThicknessSlider.Value = BrushSettings.Thickness;
            ThicknessValue.Text = ((int)BrushSettings.Thickness).ToString();
            SelectedHex = BrushSettings.ColorHex;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source?.Handle != IntPtr.Zero)
            {
                source.AddHook(WndProc);
                HotKeyManager.RegisterHotKey(source.Handle);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HotKeyManager.HOTKEY_ID)
            {
                HotKeyManager.ResetAll();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnColorChanged(string hex)
        {
            Dispatcher.Invoke(() =>
            {
                SelectedHex = hex;
            });
        }

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
        }

        private void UpdateBrush()
        {
            switch (SelectedHex)
            {
                case "#FF0000": _currentBrush = Brushes.Red; break;
                case "#0066FF": _currentBrush = new SolidColorBrush(Color.FromRgb(0, 102, 255)); break;
                case "#00CC44": _currentBrush = new SolidColorBrush(Color.FromRgb(0, 204, 68)); break;
                case "#1A1A1A": _currentBrush = Brushes.Black; break;
                case "#F0F0F0": _currentBrush = Brushes.White; break;
                case "#FFCC00": _currentBrush = new SolidColorBrush(Color.FromRgb(255, 204, 0)); break;
                default: _currentBrush = Brushes.Red; break;
            }
            UpdateBrushPreview();
            BrushSettings.ColorHex = SelectedHex;
        }

        private void UpdateBrushPreview()
        {
            if (BrushPreview == null) return;
            BrushPreview.Children.Clear();
            var circle = new Ellipse
            {
                Width = BrushSettings.Thickness,
                Height = BrushSettings.Thickness,
                Fill = _currentBrush,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            BrushPreview.Children.Add(circle);
        }

        private void OnThicknessChanged(double thickness)
        {
            Dispatcher.Invoke(() =>
            {
                ThicknessSlider.Value = thickness;
                ThicknessValue.Text = ((int)thickness).ToString();
                UpdateBrushPreview();
            });
        }

        public void SetScreen(Screen screen)
        {
            _screen = screen;
            PositionWindow();
            LoadWallpaper();
        }

        private void PositionWindow()
        {
            Left = _screen.Bounds.X;
            Top = _screen.Bounds.Y;
            Width = _screen.Bounds.Width;
            Height = _screen.Bounds.Height;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
        }

        private void LoadWallpaper()
        {
            try
            {
                var wallpaperManager = (IDesktopWallpaper)new DesktopWallpaper();
                string wallpaperPath = wallpaperManager.GetWallpaper(null);

                if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    WallpaperImage.Source = null;
                    DrawCanvas.Background = Brushes.Black;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(wallpaperPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                WallpaperImage.Source = bitmap;
                DrawCanvas.Background = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}");
                WallpaperImage.Source = null;
                DrawCanvas.Background = Brushes.DarkSlateGray;
            }
        }

        private void DrawCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _lastPoint = e.GetPosition(DrawCanvas);
            DrawCanvas.CaptureMouse();
        }

        private void ThicknessSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            BrushSettings.Thickness = ThicknessSlider.Value;
            ThicknessValue.Text = ((int)ThicknessSlider.Value).ToString();
            UpdateBrushPreview();
        }

        private void ColorDot_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is string hex)
            {
                SelectedHex = hex;
            }
        }

        private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingToolbar = true;
            _toolbarDragStart = e.GetPosition(this);
            Toolbar.CaptureMouse();
        }

        private void Toolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDraggingToolbar) return;

            var currentPos = e.GetPosition(this);
            var delta = currentPos - _toolbarDragStart;

            var currentMargin = Toolbar.Margin;
            var newLeft = currentMargin.Left + delta.X;
            var newTop = currentMargin.Top + delta.Y;

            // Ограничиваем перемещение в пределах экрана
            var bounds = _screen.Bounds;
            var toolbarWidth = Toolbar.ActualWidth;
            var toolbarHeight = Toolbar.ActualHeight;

            newLeft = Math.Max(-toolbarWidth + 10, Math.Min(newLeft, bounds.Width - 10));
            newTop = Math.Max(-toolbarHeight + 10, Math.Min(newTop, bounds.Height - 10));

            Toolbar.Margin = new Thickness(newLeft, newTop, 0, 0);

            _toolbarDragStart = currentPos;
        }

        private void Toolbar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingToolbar = false;
            Toolbar.ReleaseMouseCapture();
        }

        private void DrawCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDrawing) return;

            var currentPoint = e.GetPosition(DrawCanvas);

            var line = new Line
            {
                X1 = _lastPoint.X,
                Y1 = _lastPoint.Y,
                X2 = currentPoint.X,
                Y2 = currentPoint.Y,
                Stroke = _currentBrush,
                StrokeThickness = BrushSettings.Thickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            DrawCanvas.Children.Add(line);

            _lastPoint = currentPoint;
        }

        private void DrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                var currentPoint = e.GetPosition(DrawCanvas);
                var dx = currentPoint.X - _lastPoint.X;
                var dy = currentPoint.Y - _lastPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 2)
                {
                    var radius = Math.Max(BrushSettings.Thickness / 2, 2);
                    var dot = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = _currentBrush,
                        StrokeThickness = 0,
                        Margin = new Thickness(
                            _lastPoint.X - radius,
                            _lastPoint.Y - radius,
                            0, 0)
                    };
                    DrawCanvas.Children.Add(dot);
                }
            }

            _isDrawing = false;
            DrawCanvas.ReleaseMouseCapture();
        }

        private void DrawCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isDrawing = false;
            DrawCanvas.ReleaseMouseCapture();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            App.ShutdownAll();
            base.OnClosed(e);
        }
    }

    static class HotKeyManager
    {
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int HOTKEY_ID = 0x1000;
        const uint MOD_NOREPEAT = 0x4000;

        public static void RegisterHotKey(IntPtr handle)
        {
            RegisterHotKey(handle, HOTKEY_ID, MOD_NOREPEAT, (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.R));
        }

        public static void ResetAll()
        {
            foreach (var window in System.Windows.Application.Current.Windows.OfType<MainWindow>())
            {
                window.Toolbar.Margin = new Thickness(10, 10, 0, 0);
            }
        }
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
    }

    [ComImport]
    [Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
    class DesktopWallpaper { }
}
