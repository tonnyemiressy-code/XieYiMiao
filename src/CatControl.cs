using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XieYiMiao
{
    public enum CatMood
    {
        Idle,
        Walk,
        Drink,
        Meal,
        Stretch,
        Todo,
        Happy,
        Sleep,
        Loaf,
        DeepSleep,
        LongStretch,
        Held,
        Note,
        Backpack,
        Companion,
        Petting
    }

    public sealed class CatControl : FrameworkElement
    {
        private static readonly Brush Fur = Theme.Brush("#FFF5E4");
        private static readonly Brush FurShade = Theme.Brush("#F5DFC5");
        private static readonly Brush Apricot = Theme.Brush("#F2A15F");
        private static readonly Brush InnerEar = Theme.Brush("#F7B7AE");
        private static readonly Brush Eye = Theme.Brush("#51352A");
        private static readonly Brush EyeGlow = Theme.Brush("#FFF8E9");
        private static readonly Brush Pink = Theme.Brush("#F28E88");
        private static readonly Pen Outline =
            new Pen(Theme.Brush("#D69C73"), 2.1);
        private static readonly BitmapSource[] SpriteFrames =
            LoadSpriteFrames();
        private static readonly BitmapSource[] WalkFrames =
            LoadWalkFrames();
        private static readonly BitmapSource[] CareFrames =
            LoadGridFrames(
                "XieYiMiao.Assets.CatCareSprites",
                4,
                2);
        private static readonly BitmapSource[] OutfitFrames =
            LoadGridFrames(
                "XieYiMiao.Assets.CatOutfitSprites",
                2,
                2);
        private static readonly double[] WalkFrameDurations =
        {
            0.12, 0.12, 0.12, 0.12,
            0.12, 0.12, 0.12, 0.22
        };

        private double _phase;
        private CatMood _mood;
        private bool _facingLeft;
        private bool _hasBadge;
        private PetOutfit _outfit;

        public CatMood Mood
        {
            get { return _mood; }
            set
            {
                _mood = value;
                InvalidateVisual();
            }
        }

        public bool FacingLeft
        {
            get { return _facingLeft; }
            set
            {
                _facingLeft = value;
                InvalidateVisual();
            }
        }

        public bool HasBadge
        {
            get { return _hasBadge; }
            set
            {
                _hasBadge = value;
                InvalidateVisual();
            }
        }

        public PetOutfit Outfit
        {
            get { return _outfit; }
            set
            {
                _outfit = value;
                InvalidateVisual();
            }
        }

        public void Tick(double deltaSeconds)
        {
            _phase += deltaSeconds;
            InvalidateVisual();
        }

        internal static int AvailableWalkFrameCount
        {
            get { return WalkFrames == null ? 0 : WalkFrames.Length; }
        }

        internal static int AvailableCareFrameCount
        {
            get { return CareFrames == null ? 0 : CareFrames.Length; }
        }

        internal static int AvailableOutfitFrameCount
        {
            get { return OutfitFrames == null ? 0 : OutfitFrames.Length; }
        }

        internal int CurrentWalkFrameIndex
        {
            get { return WalkFrameIndex(_phase); }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double scale = Math.Min(
                ActualWidth / 180.0,
                ActualHeight / 180.0);
            if (scale <= 0)
            {
                return;
            }

            double offsetX = (ActualWidth - 180 * scale) / 2;
            double offsetY = (ActualHeight - 180 * scale) / 2;
            TransformGroup group = new TransformGroup();
            group.Children.Add(new ScaleTransform(scale, scale));
            group.Children.Add(new TranslateTransform(offsetX, offsetY));
            dc.PushTransform(group);

            if (_facingLeft)
            {
                dc.PushTransform(new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(-1, 1),
                        new TranslateTransform(180, 0)
                    }
                });
            }

            if (!DrawSpriteCat(dc) && _mood == CatMood.Stretch)
            {
                DrawStretchingCat(dc);
            }
            else if (SpriteFrames == null)
            {
                DrawStandingCat(dc);
            }

            if (_facingLeft)
            {
                dc.Pop();
            }
            dc.Pop();
        }

        private bool DrawSpriteCat(DrawingContext dc)
        {
            BitmapSource sprite = SelectSprite();
            if (sprite == null)
            {
                return false;
            }

            double bob;
            double scaleX = 1;
            double scaleY = 1;
            double rotation = 0;
            if (_mood == CatMood.Walk)
            {
                // The generated gait already contains footfall, body lift,
                // tail follow-through and pendant movement. Extra warping
                // would make the walk look rubbery.
                bob = 0;
            }
            else if (_mood == CatMood.Happy)
            {
                bob = -Math.Abs(Math.Sin(_phase * 5.2)) * 5;
                scaleX = 1 + Math.Sin(_phase * 5.2) * 0.02;
                scaleY = 1 - Math.Sin(_phase * 5.2) * 0.02;
            }
            else if (_mood == CatMood.Stretch)
            {
                bob = Math.Sin(_phase * 1.8) * 1.2;
                scaleX = 1 + Math.Sin(_phase * 1.8) * 0.012;
                scaleY = 1 - Math.Sin(_phase * 1.8) * 0.008;
            }
            else if (_mood == CatMood.DeepSleep)
            {
                bob = Math.Sin(_phase * 1.15) * 0.5;
                scaleX = 1 - Math.Sin(_phase * 1.15) * 0.004;
                scaleY = 1 + Math.Sin(_phase * 1.15) * 0.009;
            }
            else if (_mood == CatMood.Held)
            {
                bob = Math.Sin(_phase * 3.0) * 1.5;
                rotation = Math.Sin(_phase * 2.7) * 2.4;
            }
            else if (_mood == CatMood.Petting)
            {
                bob = -Math.Abs(Math.Sin(_phase * 3.4)) * 1.8;
                rotation = Math.Sin(_phase * 2.4) * 1.2;
                scaleX = 1 + Math.Sin(_phase * 3.4) * 0.008;
                scaleY = 1 - Math.Sin(_phase * 3.4) * 0.006;
            }
            else if (_mood == CatMood.Note ||
                     _mood == CatMood.Companion ||
                     _mood == CatMood.Loaf)
            {
                bob = Math.Sin(_phase * 1.45) * 0.75;
                scaleX = 1 - Math.Sin(_phase * 1.45) * 0.004;
                scaleY = 1 + Math.Sin(_phase * 1.45) * 0.007;
            }
            else
            {
                bob = Math.Sin(_phase * 2.0) * 1.4;
                scaleX = 1 - Math.Sin(_phase * 2.0) * 0.007;
                scaleY = 1 + Math.Sin(_phase * 2.0) * 0.012;
            }

            dc.PushTransform(new TranslateTransform(0, bob));
            dc.PushTransform(new RotateTransform(rotation, 90, 145));
            dc.PushTransform(new ScaleTransform(scaleX, scaleY, 90, 150));

            double maxWidth = 174;
            double maxHeight = 174;
            double ratio = sprite.PixelWidth / (double)sprite.PixelHeight;
            double width;
            double height;
            if (ratio >= 1)
            {
                width = maxWidth;
                height = maxWidth / ratio;
            }
            else
            {
                height = maxHeight;
                width = maxHeight * ratio;
            }
            Rect destination = new Rect(
                (180 - width) / 2,
                176 - height,
                width,
                height);
            dc.DrawImage(sprite, destination);
            dc.Pop();
            dc.Pop();
            dc.Pop();

            if (_hasBadge)
            {
                dc.DrawEllipse(
                    Theme.Peach,
                    new Pen(Brushes.White, 2),
                    new Point(146, 27),
                    13,
                    13);
                TextGlyph(dc, "!", new Point(146, 26), 17, Brushes.White);
            }
            return true;
        }

        private BitmapSource SelectSprite()
        {
            if (_mood == CatMood.Walk &&
                WalkFrames != null &&
                WalkFrames.Length == 8)
            {
                return WalkFrames[WalkFrameIndex(_phase)];
            }

            int careIndex = CareSpriteIndex(_mood);
            if (careIndex >= 0 &&
                CareFrames != null &&
                CareFrames.Length == 8)
            {
                return CareFrames[careIndex];
            }

            int outfitIndex = (int)_outfit - 1;
            if (_mood == CatMood.Idle &&
                outfitIndex >= 0 &&
                OutfitFrames != null &&
                outfitIndex < OutfitFrames.Length)
            {
                return OutfitFrames[outfitIndex];
            }

            if (SpriteFrames == null || SpriteFrames.Length != 8)
            {
                return null;
            }
            return SpriteFrames[SpriteIndex(_mood)];
        }

        private static int CareSpriteIndex(CatMood mood)
        {
            if (mood == CatMood.Loaf) return 0;
            if (mood == CatMood.DeepSleep) return 1;
            if (mood == CatMood.LongStretch) return 2;
            if (mood == CatMood.Held) return 3;
            if (mood == CatMood.Note) return 4;
            if (mood == CatMood.Backpack) return 5;
            if (mood == CatMood.Companion) return 6;
            if (mood == CatMood.Petting) return 7;
            return -1;
        }

        private static int SpriteIndex(CatMood mood)
        {
            if (mood == CatMood.Sleep)
            {
                return 1;
            }
            if (mood == CatMood.Walk)
            {
                return 2;
            }
            if (mood == CatMood.Drink)
            {
                return 3;
            }
            if (mood == CatMood.Meal)
            {
                return 4;
            }
            if (mood == CatMood.Stretch)
            {
                return 5;
            }
            if (mood == CatMood.Todo)
            {
                return 6;
            }
            if (mood == CatMood.Happy)
            {
                return 7;
            }
            return 0;
        }

        private static int WalkFrameIndex(double phase)
        {
            double cycle = 0;
            for (int i = 0; i < WalkFrameDurations.Length; i++)
            {
                cycle += WalkFrameDurations[i];
            }

            double position = phase % cycle;
            if (position < 0)
            {
                position += cycle;
            }
            double elapsed = 0;
            for (int i = 0; i < WalkFrameDurations.Length; i++)
            {
                elapsed += WalkFrameDurations[i];
                if (position < elapsed)
                {
                    return i;
                }
            }
            return WalkFrameDurations.Length - 1;
        }

        private static BitmapSource[] LoadWalkFrames()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                BitmapSource[] frames = new BitmapSource[8];
                for (int i = 0; i < frames.Length; i++)
                {
                    string resourceName =
                        "XieYiMiao.Assets.CatWalk" +
                        i.ToString("00");
                    using (Stream stream =
                        assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        frames[i] = image;
                    }
                }
                return frames;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource[] LoadSpriteFrames()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(
                    "XieYiMiao.Assets.CatSprites"))
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    BitmapImage sheet = new BitmapImage();
                    sheet.BeginInit();
                    sheet.CacheOption = BitmapCacheOption.OnLoad;
                    sheet.StreamSource = stream;
                    sheet.EndInit();
                    sheet.Freeze();

                    BitmapSource[] frames = new BitmapSource[8];
                    for (int row = 0; row < 2; row++)
                    {
                        for (int column = 0; column < 4; column++)
                        {
                            int x0 = (int)Math.Round(
                                column * sheet.PixelWidth / 4.0);
                            int x1 = (int)Math.Round(
                                (column + 1) * sheet.PixelWidth / 4.0);
                            int y0 = (int)Math.Round(
                                row * sheet.PixelHeight / 2.0);
                            int y1 = (int)Math.Round(
                                (row + 1) * sheet.PixelHeight / 2.0);
                            CroppedBitmap cell = new CroppedBitmap(
                                sheet,
                                new Int32Rect(x0, y0, x1 - x0, y1 - y0));
                            cell.Freeze();
                            frames[row * 4 + column] =
                                TrimTransparent(cell);
                        }
                    }
                    return frames;
                }
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource[] LoadGridFrames(
            string resourceName,
            int columns,
            int rows)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream =
                    assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null || columns <= 0 || rows <= 0)
                    {
                        return null;
                    }
                    BitmapImage sheet = new BitmapImage();
                    sheet.BeginInit();
                    sheet.CacheOption = BitmapCacheOption.OnLoad;
                    sheet.StreamSource = stream;
                    sheet.EndInit();
                    sheet.Freeze();

                    BitmapSource[] frames =
                        new BitmapSource[columns * rows];
                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < columns; column++)
                        {
                            int x0 = (int)Math.Round(
                                column * sheet.PixelWidth /
                                (double)columns);
                            int x1 = (int)Math.Round(
                                (column + 1) * sheet.PixelWidth /
                                (double)columns);
                            int y0 = (int)Math.Round(
                                row * sheet.PixelHeight /
                                (double)rows);
                            int y1 = (int)Math.Round(
                                (row + 1) * sheet.PixelHeight /
                                (double)rows);
                            CroppedBitmap cell = new CroppedBitmap(
                                sheet,
                                new Int32Rect(
                                    x0,
                                    y0,
                                    x1 - x0,
                                    y1 - y0));
                            cell.Freeze();
                            frames[row * columns + column] =
                                TrimTransparent(cell);
                        }
                    }
                    return frames;
                }
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource TrimTransparent(BitmapSource source)
        {
            FormatConvertedBitmap converted = new FormatConvertedBitmap(
                source,
                PixelFormats.Pbgra32,
                null,
                0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[stride * height];
            converted.CopyPixels(pixels, stride, 0);

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x * 4 + 3] <= 8)
                    {
                        continue;
                    }
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return source;
            }

            const int padding = 7;
            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(width - 1, maxX + padding);
            maxY = Math.Min(height - 1, maxY + padding);
            CroppedBitmap result = new CroppedBitmap(
                source,
                new Int32Rect(
                    minX,
                    minY,
                    maxX - minX + 1,
                    maxY - minY + 1));
            result.Freeze();
            return result;
        }

        private void DrawStandingCat(DrawingContext dc)
        {
            double bob = _mood == CatMood.Walk
                ? Math.Abs(Math.Sin(_phase * 7)) * 3
                : Math.Sin(_phase * 2.2) * 1.3;
            double breath = 1 + Math.Sin(_phase * 2.0) * 0.018;

            dc.DrawEllipse(
                Theme.Brush("#26000000"),
                null,
                new Point(91, 157),
                45,
                6);

            DrawTail(dc, bob);

            dc.PushTransform(new ScaleTransform(
                1,
                breath,
                90,
                116));
            dc.DrawEllipse(
                Fur,
                Outline,
                new Point(91, 116 + bob),
                42,
                43);
            dc.Pop();

            double step = _mood == CatMood.Walk
                ? Math.Sin(_phase * 8) * 5
                : 0;
            DrawPaw(dc, 69 + step, 145 + bob);
            DrawPaw(dc, 111 - step, 145 + bob);

            DrawHead(dc, bob);
            DrawCollar(dc, bob);
            DrawStateProp(dc, bob);

            if (_hasBadge)
            {
                dc.DrawEllipse(
                    Theme.Peach,
                    new Pen(Brushes.White, 2),
                    new Point(143, 31),
                    13,
                    13);
                TextGlyph(dc, "!", new Point(143, 30), 17, Brushes.White);
            }
        }

        private void DrawHead(DrawingContext dc, double bob)
        {
            StreamGeometry leftEar = Triangle(
                new Point(49, 57 + bob),
                new Point(56, 18 + bob),
                new Point(80, 45 + bob));
            StreamGeometry rightEar = Triangle(
                new Point(105, 44 + bob),
                new Point(131, 18 + bob),
                new Point(137, 58 + bob));
            dc.DrawGeometry(Fur, Outline, leftEar);
            dc.DrawGeometry(Fur, Outline, rightEar);

            StreamGeometry leftPatch = Triangle(
                new Point(54, 47 + bob),
                new Point(59, 23 + bob),
                new Point(73, 43 + bob));
            StreamGeometry rightPatch = Triangle(
                new Point(111, 42 + bob),
                new Point(128, 23 + bob),
                new Point(133, 49 + bob));
            dc.DrawGeometry(Apricot, null, leftPatch);
            dc.DrawGeometry(Apricot, null, rightPatch);

            StreamGeometry leftInner = Triangle(
                new Point(59, 42 + bob),
                new Point(61, 29 + bob),
                new Point(69, 41 + bob));
            StreamGeometry rightInner = Triangle(
                new Point(116, 40 + bob),
                new Point(126, 29 + bob),
                new Point(130, 44 + bob));
            dc.DrawGeometry(InnerEar, null, leftInner);
            dc.DrawGeometry(InnerEar, null, rightInner);

            dc.DrawEllipse(
                Fur,
                Outline,
                new Point(92, 75 + bob),
                58,
                49);

            double blinkCycle = _phase % 6.0;
            bool blinking = blinkCycle > 5.72 ||
                            _mood == CatMood.Sleep ||
                            _mood == CatMood.Drink;
            if (blinking)
            {
                DrawClosedEye(dc, new Point(70, 72 + bob));
                DrawClosedEye(dc, new Point(113, 72 + bob));
            }
            else
            {
                DrawEye(dc, new Point(70, 72 + bob));
                DrawEye(dc, new Point(113, 72 + bob));
            }

            dc.DrawEllipse(
                Theme.Brush("#55F29C96"),
                null,
                new Point(56, 89 + bob),
                11,
                5);
            dc.DrawEllipse(
                Theme.Brush("#55F29C96"),
                null,
                new Point(128, 89 + bob),
                11,
                5);

            StreamGeometry nose = Triangle(
                new Point(87, 84 + bob),
                new Point(97, 84 + bob),
                new Point(92, 90 + bob));
            dc.DrawGeometry(Pink, null, nose);
            Pen mouthPen = new Pen(Theme.Brush("#9A5F54"), 1.7);
            dc.DrawLine(mouthPen, new Point(92, 89 + bob), new Point(92, 94 + bob));
            DrawSmile(dc, new Point(92, 93 + bob), mouthPen);

            if (_mood == CatMood.Happy)
            {
                TextGlyph(dc, "✦", new Point(31, 46), 15, Theme.Apricot);
                TextGlyph(dc, "✦", new Point(151, 58), 12, Theme.Sky);
            }
        }

        private void DrawTail(DrawingContext dc, double bob)
        {
            StreamGeometry tail = new StreamGeometry();
            using (StreamGeometryContext c = tail.Open())
            {
                c.BeginFigure(new Point(126, 126 + bob), false, false);
                c.BezierTo(
                    new Point(158, 132 + bob),
                    new Point(161, 83 + bob),
                    new Point(143, 76 + bob),
                    true,
                    false);
            }
            tail.Freeze();
            Pen tailPen = new Pen(Fur, 18);
            tailPen.StartLineCap = PenLineCap.Round;
            tailPen.EndLineCap = PenLineCap.Round;
            dc.DrawGeometry(null, new Pen(Theme.Brush("#D69C73"), 22), tail);
            dc.DrawGeometry(null, tailPen, tail);

            double wag = Math.Sin(_phase * 3.0) * 3;
            dc.DrawEllipse(
                Apricot,
                null,
                new Point(144 + wag, 77 + bob),
                10,
                12);
        }

        private void DrawPaw(DrawingContext dc, double x, double y)
        {
            dc.DrawEllipse(FurShade, Outline, new Point(x, y), 14, 12);
            Pen toePen = new Pen(Theme.Brush("#D9B48F"), 1);
            dc.DrawLine(toePen, new Point(x - 3, y + 3), new Point(x - 3, y + 8));
            dc.DrawLine(toePen, new Point(x + 3, y + 3), new Point(x + 3, y + 8));
        }

        private void DrawCollar(DrawingContext dc, double bob)
        {
            dc.DrawRoundedRectangle(
                Theme.Mint,
                new Pen(Theme.MintDark, 1.4),
                new Rect(55, 105 + bob, 74, 10),
                5,
                5);

            StreamGeometry drop = new StreamGeometry();
            using (StreamGeometryContext c = drop.Open())
            {
                c.BeginFigure(new Point(92, 112 + bob), true, true);
                c.BezierTo(
                    new Point(85, 122 + bob),
                    new Point(86, 130 + bob),
                    new Point(92, 132 + bob),
                    true,
                    true);
                c.BezierTo(
                    new Point(99, 130 + bob),
                    new Point(99, 122 + bob),
                    new Point(92, 112 + bob),
                    true,
                    true);
            }
            drop.Freeze();
            dc.DrawGeometry(
                Theme.Sky,
                new Pen(Theme.Brush("#4C9AC7"), 1.4),
                drop);
            dc.DrawEllipse(
                Theme.Brush("#BFFFFFFF"),
                null,
                new Point(90, 121 + bob),
                1.8,
                3.5);
        }

        private void DrawStateProp(DrawingContext dc, double bob)
        {
            if (_mood == CatMood.Drink)
            {
                dc.DrawRoundedRectangle(
                    Theme.Sky,
                    new Pen(Theme.Brush("#4B91B9"), 1.7),
                    new Rect(69, 115 + bob, 48, 37),
                    7,
                    7);
                dc.DrawEllipse(
                    Theme.Brush("#D8F2FF"),
                    new Pen(Theme.Brush("#4B91B9"), 1.4),
                    new Point(93, 116 + bob),
                    23,
                    5);
                dc.DrawEllipse(Fur, Outline, new Point(69, 130 + bob), 9, 10);
                dc.DrawEllipse(Fur, Outline, new Point(117, 130 + bob), 9, 10);
            }
            else if (_mood == CatMood.Meal)
            {
                dc.DrawGeometry(
                    Theme.Sky,
                    new Pen(Theme.Brush("#4B91B9"), 1.7),
                    BowlGeometry(new Rect(58, 133 + bob, 68, 24)));
                dc.DrawEllipse(
                    Theme.Brush("#C88A59"),
                    null,
                    new Point(92, 134 + bob),
                    27,
                    6);
            }
            else if (_mood == CatMood.Todo)
            {
                dc.DrawRoundedRectangle(
                    Theme.Card,
                    new Pen(Theme.Brush("#D9B98D"), 1.7),
                    new Rect(61, 111 + bob, 62, 48),
                    5,
                    5);
                Pen check = new Pen(Theme.Sky, 5);
                check.StartLineCap = PenLineCap.Round;
                check.EndLineCap = PenLineCap.Round;
                dc.DrawLine(check, new Point(76, 134 + bob), new Point(88, 145 + bob));
                dc.DrawLine(check, new Point(88, 145 + bob), new Point(110, 122 + bob));
                dc.DrawEllipse(Fur, Outline, new Point(62, 131 + bob), 8, 10);
                dc.DrawEllipse(Fur, Outline, new Point(123, 131 + bob), 8, 10);
            }
            else if (_mood == CatMood.Happy)
            {
                dc.DrawEllipse(Fur, Outline, new Point(54, 117 + bob), 10, 23);
                dc.DrawEllipse(Fur, Outline, new Point(130, 117 + bob), 10, 23);
                TextGlyph(dc, "✦", new Point(45, 106), 12, Theme.Apricot);
                TextGlyph(dc, "✦", new Point(141, 99), 14, Theme.Sky);
            }
        }

        private void DrawStretchingCat(DrawingContext dc)
        {
            double sway = Math.Sin(_phase * 2) * 1.5;
            dc.DrawEllipse(
                Theme.Brush("#26000000"),
                null,
                new Point(94, 153),
                60,
                6);

            StreamGeometry tail = new StreamGeometry();
            using (StreamGeometryContext c = tail.Open())
            {
                c.BeginFigure(new Point(132, 104), false, false);
                c.BezierTo(
                    new Point(158, 75),
                    new Point(149, 38),
                    new Point(138 + sway, 28),
                    true,
                    false);
            }
            tail.Freeze();
            dc.DrawGeometry(null, new Pen(Theme.Brush("#D69C73"), 22), tail);
            dc.DrawGeometry(null, new Pen(Fur, 18), tail);
            dc.DrawEllipse(Apricot, null, new Point(138 + sway, 29), 10, 12);

            dc.DrawEllipse(Fur, Outline, new Point(105, 104), 50, 33);
            dc.DrawEllipse(Fur, Outline, new Point(56, 124), 38, 30);
            dc.DrawEllipse(FurShade, Outline, new Point(70, 151), 33, 9);
            dc.DrawEllipse(FurShade, Outline, new Point(39, 151), 24, 8);

            StreamGeometry leftEar = Triangle(
                new Point(31, 120),
                new Point(36, 91),
                new Point(52, 113));
            StreamGeometry rightEar = Triangle(
                new Point(63, 112),
                new Point(79, 91),
                new Point(82, 121));
            dc.DrawGeometry(Fur, Outline, leftEar);
            dc.DrawGeometry(Fur, Outline, rightEar);
            dc.DrawGeometry(
                Apricot,
                null,
                Triangle(
                    new Point(35, 113),
                    new Point(38, 96),
                    new Point(47, 111)));
            dc.DrawGeometry(
                Apricot,
                null,
                Triangle(
                    new Point(67, 109),
                    new Point(77, 96),
                    new Point(79, 115)));

            DrawClosedEye(dc, new Point(45, 124));
            DrawClosedEye(dc, new Point(67, 124));
            dc.DrawEllipse(Pink, null, new Point(56, 134), 4, 3);
            Pen mouth = new Pen(Theme.Brush("#9A5F54"), 1.5);
            DrawSmile(dc, new Point(56, 136), mouth);
            dc.DrawRoundedRectangle(
                Theme.Mint,
                new Pen(Theme.MintDark, 1),
                new Rect(31, 139, 48, 7),
                3,
                3);
        }

        private static void DrawEye(DrawingContext dc, Point center)
        {
            dc.DrawEllipse(Eye, null, center, 10, 13);
            dc.DrawEllipse(
                EyeGlow,
                null,
                new Point(center.X - 3, center.Y - 5),
                3.4,
                4.2);
            dc.DrawEllipse(
                Theme.Brush("#7A4A34"),
                null,
                new Point(center.X + 2, center.Y + 5),
                2.5,
                2);
        }

        private static void DrawClosedEye(DrawingContext dc, Point center)
        {
            StreamGeometry eye = new StreamGeometry();
            using (StreamGeometryContext c = eye.Open())
            {
                c.BeginFigure(
                    new Point(center.X - 8, center.Y),
                    false,
                    false);
                c.QuadraticBezierTo(
                    new Point(center.X, center.Y + 6),
                    new Point(center.X + 8, center.Y),
                    true,
                    false);
            }
            eye.Freeze();
            Pen pen = new Pen(Eye, 2.2);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            dc.DrawGeometry(null, pen, eye);
        }

        private static void DrawSmile(DrawingContext dc, Point center, Pen pen)
        {
            StreamGeometry mouth = new StreamGeometry();
            using (StreamGeometryContext c = mouth.Open())
            {
                c.BeginFigure(center, false, false);
                c.QuadraticBezierTo(
                    new Point(center.X - 4, center.Y + 5),
                    new Point(center.X - 9, center.Y + 1),
                    true,
                    false);
                c.BeginFigure(center, false, false);
                c.QuadraticBezierTo(
                    new Point(center.X + 4, center.Y + 5),
                    new Point(center.X + 9, center.Y + 1),
                    true,
                    false);
            }
            mouth.Freeze();
            dc.DrawGeometry(null, pen, mouth);
        }

        private static StreamGeometry Triangle(Point a, Point b, Point c)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(a, true, true);
                context.LineTo(b, true, false);
                context.LineTo(c, true, false);
            }
            geometry.Freeze();
            return geometry;
        }

        private static StreamGeometry BowlGeometry(Rect rect)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext c = geometry.Open())
            {
                c.BeginFigure(new Point(rect.Left, rect.Top), true, true);
                c.LineTo(new Point(rect.Right, rect.Top), true, false);
                c.LineTo(
                    new Point(rect.Right - 8, rect.Bottom),
                    true,
                    false);
                c.LineTo(
                    new Point(rect.Left + 8, rect.Bottom),
                    true,
                    false);
            }
            geometry.Freeze();
            return geometry;
        }

        private static void TextGlyph(
            DrawingContext dc,
            string text,
            Point center,
            double size,
            Brush brush)
        {
            FormattedText formatted = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei UI"),
                size,
                brush,
                1.0);
            dc.DrawText(
                formatted,
                new Point(
                    center.X - formatted.Width / 2,
                    center.Y - formatted.Height / 2));
        }
    }
}
