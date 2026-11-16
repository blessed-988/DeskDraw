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

            var screens = System.Windows.Forms.Screen.AllScreens;

            foreach (var screen in screens)
            {
                var window = new MainWindow();
                window.SetScreen(screen);
                window.Show();
                _windows.Add(window);
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
