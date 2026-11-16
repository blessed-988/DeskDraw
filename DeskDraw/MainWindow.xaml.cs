using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskDraw // Обрати внимание: здесь должно быть имя твоего проекта!
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadWallpaper(); // Вызываем загрузку обоев при старте
        }

        private void LoadWallpaper()
        {
            try
            {
                // Создаём объект для работы с обоями Windows
                var wallpaperManager = (IDesktopWallpaper)new DesktopWallpaper();

                // Получаем путь к файлу обоев (null означает "все мониторы сразу")
                string wallpaperPath = wallpaperManager.GetWallpaper(null);

                // Если путь пустой (например, стоят сплошные обои), ставим чёрный фон
                if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    MainGrid.Background = Brushes.Black;
                    return;
                }

                // Загружаем картинку и ставим её на фон
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(wallpaperPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Важно: чтобы файл не блокировался
                bitmap.EndInit();

                // Создаём кисть и растягиваем её как "Заполнить"
                var brush = new ImageBrush(bitmap);
                brush.Stretch = Stretch.UniformToFill; // Аналог режима "Заполнить"
                MainGrid.Background = brush;
            }
            catch (Exception ex)
            {
                // Если что-то пошло не так, покажем ошибку
                MessageBox.Show($"Не удалось загрузить обои: {ex.Message}");
                MainGrid.Background = Brushes.DarkSlateGray;
            }
        }

        // Закрытие по Esc
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
            base.OnKeyDown(e);
        }
    }

    // --- Вспомогательные классы для работы с системным API (можно не трогать) ---

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