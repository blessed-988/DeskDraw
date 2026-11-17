using System;
using System.Collections.Generic;
using System.Windows;
using Application = System.Windows.Application;

namespace DeskDraw
{
    public partial class App : Application
    {
        private static List<MainWindow> _windows = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                System.Windows.MessageBox.Show($"Критическая ошибка:\n{args.Exception}");
                args.Handled = true;
                ShutdownAll();
            };

            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;

                foreach (var screen in screens)
                {
                    var window = new MainWindow();
                    window.SetScreen(screen);
                    window.Show();
                    _windows.Add(window);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка запуска:\n{ex.Message}\n\n{ex.StackTrace}");
                Shutdown();
            }
        }

        public static void ShutdownAll()
        {
            foreach (var w in _windows)
            {
                w.Dispatcher.Invoke(() => w.Close());
            }
        }
    }
}
