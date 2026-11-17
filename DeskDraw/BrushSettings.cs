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

        public static event Action<double>? ThicknessChanged;
    }
}
