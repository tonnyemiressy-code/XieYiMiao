using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace XieYiMiao
{
    public static class Theme
    {
        public static readonly SolidColorBrush Ink = Brush("#4B3A35");
        public static readonly SolidColorBrush Muted = Brush("#877570");
        public static readonly SolidColorBrush Ivory = Brush("#FFF9F0");
        public static readonly SolidColorBrush Card = Brush("#FFFFFC");
        public static readonly SolidColorBrush Mint = Brush("#8CCEB5");
        public static readonly SolidColorBrush MintDark = Brush("#4E9A80");
        public static readonly SolidColorBrush MintSoft = Brush("#EAF7F1");
        public static readonly SolidColorBrush Sky = Brush("#79BDE3");
        public static readonly SolidColorBrush SkySoft = Brush("#EAF4FA");
        public static readonly SolidColorBrush Peach = Brush("#F5B58F");
        public static readonly SolidColorBrush PeachSoft = Brush("#FFE7D6");
        public static readonly SolidColorBrush ApricotSoft = Brush("#FFF1E7");
        public static readonly SolidColorBrush Line = Brush("#EADFD4");
        public static readonly SolidColorBrush Cream = Brush("#FFF5E3");
        public static readonly SolidColorBrush Apricot = Brush("#F2A15F");

        public static SolidColorBrush Brush(string hex)
        {
            SolidColorBrush value =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            value.Freeze();
            return value;
        }

        public static TextBlock Text(
            string value,
            double size,
            FontWeight weight,
            Brush color)
        {
            return new TextBlock
            {
                Text = value,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = size,
                FontWeight = weight,
                Foreground = color,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static Button Button(string text, bool primary)
        {
            Button button = new Button
            {
                Content = text,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(16, 9, 16, 9),
                MinWidth = 84,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = primary ? Mint : Card,
                Foreground = primary ? Brushes.White : Ink,
                BorderBrush = primary ? Mint : Line,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0)
            };
            return button;
        }

        public static Border CardBorder()
        {
            return new Border
            {
                Background = Card,
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 14)
            };
        }
    }
}
