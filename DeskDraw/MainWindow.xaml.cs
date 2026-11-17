using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace DeskDraw
{
    public partial class MainWindow : Window
    {
        private Screen _screen;
        private bool _isDrawing = false;
        private Point _lastPoint = new Point();

        public MainWindow()
        {
            InitializeComponent();
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
                    MainGrid.Background = Brushes.Black;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(wallpaperPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var brush = new ImageBrush(bitmap);
                brush.Stretch = Stretch.UniformToFill;
                MainGrid.Background = brush;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}");
                MainGrid.Background = Brushes.DarkSlateGray;
            }
        }

        private void DrawCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _lastPoint = e.GetPosition(DrawCanvas);
            DrawCanvas.CaptureMouse();
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
                Stroke = Brushes.Red,
                StrokeThickness = 5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            DrawCanvas.Children.Add(line);

            _lastPoint = currentPoint;
        }

        private void DrawCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
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
