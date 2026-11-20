using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
        private enum ToolType { Brush, Rectangle }

        private Screen _screen;
        private bool _isDrawing = false;
        private ToolType _currentTool = ToolType.Brush;
        private Point _lastPoint = new Point();
        private Point _rectangleStartPoint = new Point();
        private System.Windows.Shapes.Rectangle? _rectanglePreview = null;
        private string _selectedHex = "#FF0000";
        private SolidColorBrush _currentBrush = Brushes.Red;
        private bool _isDraggingToolbar = false;
        private Point _toolbarDragStart = new Point();
        private Stack<List<Shape>> _undoStack = new Stack<List<Shape>>();
        private Stack<List<Shape>> _redoStack = new Stack<List<Shape>>();
        private List<Shape> _currentStrokeShapes = new List<Shape>();
        private bool _isUndoEnabled = false;
        private bool _isRedoEnabled = false;

        public bool IsUndoEnabled
        {
            get => _isUndoEnabled;
            set
            {
                _isUndoEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsRedoEnabled
        {
            get => _isRedoEnabled;
            set
            {
                _isRedoEnabled = value;
                OnPropertyChanged();
            }
        }

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
            UpdateToolIndicator();
            UpdateButtonStates();
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
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (id == HotKeyManager.HOTKEY_ID)
                {
                    HotKeyManager.ResetAll();
                }
                else if (id == HotKeyManager.HOTKEY_WALLPAPER)
                {
                    SetWallpaperAsDesktop();
                }
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

        private void SetTool(ToolType tool)
        {
            _currentTool = tool;
            UpdateToolIndicator();
        }

        private void UpdateToolIndicator()
        {
            if (BrushIconBorder != null)
            {
                BrushIconBorder.Background = _currentTool == ToolType.Brush
                    ? new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))
                    : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
            }
            if (RectangleIconBorder != null)
            {
                RectangleIconBorder.Background = _currentTool == ToolType.Rectangle
                    ? new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF))
                    : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
            }
            if (ToolNameText != null)
            {
                ToolNameText.Text = _currentTool == ToolType.Brush ? "Кисть" : "Прямоугольник";
            }
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
            if (_isDraggingToolbar) return;

            _isDrawing = true;
            _lastPoint = e.GetPosition(DrawCanvas);
            _rectangleStartPoint = _lastPoint;
            _currentStrokeShapes.Clear();
            if (_currentTool == ToolType.Rectangle)
            {
                _rectanglePreview = new System.Windows.Shapes.Rectangle
                {
                    Fill = _currentBrush,
                    Stroke = _currentBrush,
                    StrokeThickness = BrushSettings.Thickness,
                    RadiusX = 2,
                    RadiusY = 2
                };
                DrawCanvas.Children.Add(_rectanglePreview);
                _currentStrokeShapes.Add(_rectanglePreview);
            }
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

        private void BrushTool_Click(object sender, MouseButtonEventArgs e)
        {
            SetTool(ToolType.Brush);
            e.Handled = true;
        }

        private void RectangleTool_Click(object sender, MouseButtonEventArgs e)
        {
            SetTool(ToolType.Rectangle);
            e.Handled = true;
        }

        private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingToolbar = true;
            _toolbarDragStart = e.GetPosition(this);
            Toolbar.CaptureMouse();
            e.Handled = true;
        }

        private void Grip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingToolbar)
            {
                _isDraggingToolbar = false;
                Toolbar.ReleaseMouseCapture();
            }
            e.Handled = true;
        }

        private void Toolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDraggingToolbar) return;

            var currentPos = e.GetPosition(this);
            var delta = currentPos - _toolbarDragStart;

            var currentMargin = Toolbar.Margin;
            var newLeft = currentMargin.Left + delta.X;
            var newTop = currentMargin.Top + delta.Y;

            // Панель не выходит за границы экрана (окно покрывает весь экран)
            var toolbarWidth = Toolbar.ActualWidth;
            var toolbarHeight = Toolbar.ActualHeight;

            newLeft = Math.Max(0, Math.Min(Width - toolbarWidth, newLeft));
            newTop = Math.Max(0, Math.Min(Height - toolbarHeight, newTop));

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

            if (_currentTool == ToolType.Rectangle && _rectanglePreview != null)
            {
                double x = Math.Min(_rectangleStartPoint.X, currentPoint.X);
                double y = Math.Min(_rectangleStartPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _rectangleStartPoint.X);
                double height = Math.Abs(currentPoint.Y - _rectangleStartPoint.Y);

                _rectanglePreview.Width = width;
                _rectanglePreview.Height = height;
                Canvas.SetLeft(_rectanglePreview, x);
                Canvas.SetTop(_rectanglePreview, y);
                return;
            }

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
            _currentStrokeShapes.Add(line);

            _lastPoint = currentPoint;
        }

        private void DrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                if (_currentTool == ToolType.Rectangle)
                {
                    if (_rectanglePreview != null)
                    {
                        if (_rectanglePreview.Width < 3 && _rectanglePreview.Height < 3)
                        {
                            DrawCanvas.Children.Remove(_rectanglePreview);
                            _currentStrokeShapes.Remove(_rectanglePreview);
                            var radius = Math.Max(BrushSettings.Thickness / 2, 2);
                            var dot = new Ellipse
                            {
                                Width = radius * 2,
                                Height = radius * 2,
                                Fill = _currentBrush,
                                StrokeThickness = 0,
                                Margin = new Thickness(
                                    _rectangleStartPoint.X - radius,
                                    _rectangleStartPoint.Y - radius,
                                    0, 0)
                            };
                            DrawCanvas.Children.Add(dot);
                            _currentStrokeShapes.Add(dot);
                        }
                        _rectanglePreview = null;
                    }
                }
                else
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
                        _currentStrokeShapes.Add(dot);
                    }
                }

                // Push completed stroke to undo stack
                if (_currentStrokeShapes.Count > 0)
                {
                    _undoStack.Push(new List<Shape>(_currentStrokeShapes));
                    _redoStack.Clear();
                    UpdateButtonStates();
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

        private void UpdateButtonStates()
        {
            IsUndoEnabled = _undoStack.Count > 0;
            IsRedoEnabled = _redoStack.Count > 0;

            if (UndoButtonBorder != null)
            {
                UndoButtonBorder.Cursor = IsUndoEnabled ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
            }
            if (RedoButtonBorder != null)
            {
                RedoButtonBorder.Cursor = IsRedoEnabled ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
            }
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            var shapes = _undoStack.Pop();
            var redoGroup = new List<Shape>();

            foreach (var shape in shapes)
            {
                if (DrawCanvas.Children.Contains(shape))
                {
                    DrawCanvas.Children.Remove(shape);
                    redoGroup.Add(shape);
                }
            }

            if (redoGroup.Count > 0)
            {
                _redoStack.Push(redoGroup);
            }

            UpdateButtonStates();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            var shapes = _redoStack.Pop();
            var undoGroup = new List<Shape>();

            foreach (var shape in shapes)
            {
                DrawCanvas.Children.Add(shape);
                undoGroup.Add(shape);
            }

            if (undoGroup.Count > 0)
            {
                _undoStack.Push(undoGroup);
            }

            UpdateButtonStates();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Q)
            {
                Close();
                return;
            }
            if (e.Key == Key.S)
            {
                SetWallpaperAsDesktop();
                return;
            }
            if (e.Key == Key.Z && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                Undo();
                e.Handled = true;
                return;
            }
            if ((e.Key == Key.Z && e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ||
                (e.Key == Key.Y && e.KeyboardDevice.Modifiers == ModifierKeys.Control))
            {
                Redo();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void WallpaperButton_Click(object sender, MouseButtonEventArgs e)
        {
            SetWallpaperAsDesktop();
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void UndoButton_Click(object sender, MouseButtonEventArgs e)
        {
            Undo();
            e.Handled = true;
        }

        private void RedoButton_Click(object sender, MouseButtonEventArgs e)
        {
            Redo();
            e.Handled = true;
        }

        private void SetWallpaperAsDesktop()
        {
            try
            {
                if (_screen == null) return;

                // Конвертируем логические единицы WPF в пиксели монитора
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget == null) return;

                var transform = source.CompositionTarget.TransformToDevice;
                int pixelWidth = (int)(_screen.Bounds.Width * transform.M11);
                int pixelHeight = (int)(_screen.Bounds.Height * transform.M22);

                // Рендерим содержимое Grid (обои + рисунок) в битмап
                var renderTarget = new RenderTargetBitmap(
                    pixelWidth, pixelHeight,
                    96 * transform.M11, 96 * transform.M22,
                    PixelFormats.Pbgra32);

                var drawingVisual = new DrawingVisual();
                using (var ctx = drawingVisual.RenderOpen())
                {
                    // Рисуем обои с высоким качеством
                    if (WallpaperImage.Source is BitmapImage bitmapSource)
                    {
                        RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);

                        // Вычисляем пропорции UniformToFill (обрезка с сохранением пропорций)
                        double targetRatio = (double)pixelWidth / pixelHeight;
                        double imageRatio = (double)bitmapSource.PixelWidth / bitmapSource.PixelHeight;

                        double scale;
                        double offsetX = 0, offsetY = 0;

                        if (imageRatio > targetRatio)
                        {
                            // Изображение шире — масштабируем по высоте
                            scale = pixelHeight / (double)bitmapSource.PixelHeight;
                            double scaledWidth = bitmapSource.PixelWidth * scale;
                            offsetX = (pixelWidth - scaledWidth) / 2;
                        }
                        else
                        {
                            // Изображение выше — масштабируем по ширине
                            scale = pixelWidth / (double)bitmapSource.PixelWidth;
                            double scaledHeight = bitmapSource.PixelHeight * scale;
                            offsetY = (pixelHeight - scaledHeight) / 2;
                        }

                        ctx.DrawImage(bitmapSource, new Rect(offsetX, offsetY,
                            pixelWidth - offsetX * 2, pixelHeight - offsetY * 2));
                    }
                    else
                    {
                        ctx.DrawRectangle(Brushes.Black, null, new Rect(0, 0, pixelWidth, pixelHeight));
                    }

                    // Рисунок поверх обоев
                    RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
                    var canvasBrush = new VisualBrush(DrawCanvas)
                    {
                        Stretch = Stretch.None
                    };
                    ctx.DrawRectangle(canvasBrush, null, new Rect(0, 0, pixelWidth, pixelHeight));
                }

                renderTarget.Render(drawingVisual);
                renderTarget.Freeze();

                // Сохраняем в PNG файл
                string monitorName = _screen.Primary ? "Primary" : _screen.DeviceName.Replace("\\", "");
                string tempPath = global::System.IO.Path.Combine(
                    global::System.IO.Path.GetTempPath(),
                    $"DeskDraw_Wallpaper_{monitorName}.png");

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (var stream = File.Create(tempPath))
                {
                    encoder.Save(stream);
                }

                // Устанавливаем как обои для текущего монитора
                var wallpaperManager = (IDesktopWallpaper)new DesktopWallpaper();
                wallpaperManager.SetWallpaper(null, tempPath);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Ошибка при установке обоев:\n{ex.Message}",
                        "DeskDraw",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
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
        public const int HOTKEY_WALLPAPER = 0x1001;
        const uint MOD_NOREPEAT = 0x4000;
        const uint MOD_CONTROL = 0x0002;

        public static void RegisterHotKey(IntPtr handle)
        {
            RegisterHotKey(handle, HOTKEY_ID, MOD_NOREPEAT, (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.R));
            RegisterHotKey(handle, HOTKEY_WALLPAPER, MOD_NOREPEAT | MOD_CONTROL, (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.S));
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
