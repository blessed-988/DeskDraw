using System;

namespace DeskDraw
{
    public static class BrushSettings
    {
        private static double _thickness = 5;
        public static double Thickness
        {
            get => _thickness;
            set
            {
                if (_thickness != value)
                {
                    _thickness = value;
                    ThicknessChanged?.Invoke(_thickness);
                }
            }
        }

        private static string _colorHex = "#FF0000";
        public static string ColorHex
        {
            get => _colorHex;
            set
            {
                if (_colorHex != value)
                {
                    _colorHex = value;
                    ColorChanged?.Invoke(_colorHex);
                }
            }
        }

        public static event Action<double>? ThicknessChanged;
        public static event Action<string>? ColorChanged;
    }
}
