using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace XieYiMiao
{
    internal struct PixelPoint
    {
        public double X;
        public double Y;

        public PixelPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    internal struct PixelBounds
    {
        public double Left;
        public double Top;
        public double Width;
        public double Height;

        public PixelBounds(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double Right { get { return Left + Width; } }
        public double Bottom { get { return Top + Height; } }
        public double CenterX { get { return Left + Width / 2; } }
        public double CenterY { get { return Top + Height / 2; } }
    }

    internal sealed class DisplayArea
    {
        public string DeviceName { get; private set; }
        public PixelBounds WorkArea { get; private set; }
        public bool IsPrimary { get; private set; }
        public double DpiScaleX { get; private set; }
        public double DpiScaleY { get; private set; }

        public DisplayArea(
            string deviceName,
            PixelBounds workArea,
            bool isPrimary,
            double dpiScaleX,
            double dpiScaleY)
        {
            DeviceName = deviceName ?? "";
            WorkArea = workArea;
            IsPrimary = isPrimary;
            DpiScaleX = dpiScaleX <= 0 ? 1 : dpiScaleX;
            DpiScaleY = dpiScaleY <= 0 ? 1 : dpiScaleY;
        }
    }

    internal sealed class DisplayTopology
    {
        private readonly List<DisplayArea> _areas;

        public DisplayTopology(IEnumerable<DisplayArea> areas)
        {
            _areas = areas == null
                ? new List<DisplayArea>()
                : new List<DisplayArea>(areas);
            if (_areas.Count == 0)
            {
                _areas.Add(new DisplayArea(
                    "primary",
                    new PixelBounds(0, 0, 1920, 1080),
                    true,
                    1,
                    1));
            }
        }

        public IList<DisplayArea> Areas
        {
            get { return _areas.AsReadOnly(); }
        }

        public DisplayArea Primary
        {
            get
            {
                return _areas.FirstOrDefault(x => x.IsPrimary) ?? _areas[0];
            }
        }

        public DisplayArea FindByDevice(string device)
        {
            if (string.IsNullOrWhiteSpace(device))
            {
                return null;
            }
            return _areas.FirstOrDefault(
                x => string.Equals(
                    x.DeviceName,
                    device,
                    StringComparison.OrdinalIgnoreCase));
        }

        public DisplayArea FindForWindow(PixelBounds window)
        {
            DisplayArea best = null;
            double bestArea = -1;
            foreach (DisplayArea area in _areas)
            {
                double overlap = IntersectionArea(window, area.WorkArea);
                if (overlap > bestArea)
                {
                    bestArea = overlap;
                    best = area;
                }
            }
            if (bestArea > 0)
            {
                return best;
            }

            double bestDistance = double.MaxValue;
            foreach (DisplayArea area in _areas)
            {
                double dx = window.CenterX - area.WorkArea.CenterX;
                double dy = window.CenterY - area.WorkArea.CenterY;
                double distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = area;
                }
            }
            return best ?? Primary;
        }

        public PixelPoint ClampTopLeft(
            PixelPoint desired,
            double windowWidth,
            double windowHeight,
            DisplayArea preferred)
        {
            PixelBounds desiredBounds = new PixelBounds(
                desired.X,
                desired.Y,
                windowWidth,
                windowHeight);
            DisplayArea target = preferred ?? FindForWindow(desiredBounds);
            PixelBounds area = target.WorkArea;
            return new PixelPoint(
                Math.Max(
                    area.Left,
                    Math.Min(area.Right - windowWidth, desired.X)),
                Math.Max(
                    area.Top,
                    Math.Min(area.Bottom - windowHeight, desired.Y)));
        }

        public bool IsInsideDesktop(PixelBounds window)
        {
            double covered = 0;
            foreach (DisplayArea area in _areas)
            {
                covered += IntersectionArea(window, area.WorkArea);
            }
            double expected = Math.Max(0, window.Width) *
                              Math.Max(0, window.Height);
            return expected > 0 && covered + 1 >= expected;
        }

        public List<PixelPoint> BuildRoute(
            DisplayArea from,
            DisplayArea to,
            PixelPoint finalTarget,
            double windowWidth,
            double windowHeight)
        {
            List<PixelPoint> route = new List<PixelPoint>();
            if (from == null || to == null || from == to)
            {
                route.Add(finalTarget);
                return route;
            }

            Dictionary<DisplayArea, DisplayArea> parent =
                new Dictionary<DisplayArea, DisplayArea>();
            Queue<DisplayArea> queue = new Queue<DisplayArea>();
            parent[from] = null;
            queue.Enqueue(from);
            while (queue.Count > 0 && !parent.ContainsKey(to))
            {
                DisplayArea current = queue.Dequeue();
                foreach (DisplayArea candidate in _areas)
                {
                    if (parent.ContainsKey(candidate) ||
                        !CanCross(
                            current.WorkArea,
                            candidate.WorkArea,
                            windowWidth,
                            windowHeight))
                    {
                        continue;
                    }
                    parent[candidate] = current;
                    queue.Enqueue(candidate);
                }
            }
            if (!parent.ContainsKey(to))
            {
                return route;
            }

            List<DisplayArea> path = new List<DisplayArea>();
            DisplayArea node = to;
            while (node != null)
            {
                path.Add(node);
                node = parent[node];
            }
            path.Reverse();
            for (int i = 0; i < path.Count - 1; i++)
            {
                AddPortal(
                    route,
                    path[i].WorkArea,
                    path[i + 1].WorkArea,
                    windowWidth,
                    windowHeight);
            }
            route.Add(finalTarget);
            return route;
        }

        internal static bool CanCross(
            PixelBounds left,
            PixelBounds right,
            double windowWidth,
            double windowHeight)
        {
            const double tolerance = 3;
            bool horizontal =
                Math.Abs(left.Right - right.Left) <= tolerance ||
                Math.Abs(right.Right - left.Left) <= tolerance;
            double verticalOverlap = Math.Min(left.Bottom, right.Bottom) -
                                     Math.Max(left.Top, right.Top);
            if (horizontal && verticalOverlap + 1 >= windowHeight)
            {
                return true;
            }

            bool vertical =
                Math.Abs(left.Bottom - right.Top) <= tolerance ||
                Math.Abs(right.Bottom - left.Top) <= tolerance;
            double horizontalOverlap = Math.Min(left.Right, right.Right) -
                                       Math.Max(left.Left, right.Left);
            return vertical && horizontalOverlap + 1 >= windowWidth;
        }

        private static void AddPortal(
            ICollection<PixelPoint> route,
            PixelBounds from,
            PixelBounds to,
            double width,
            double height)
        {
            double overlapTop = Math.Max(from.Top, to.Top);
            double overlapBottom = Math.Min(from.Bottom, to.Bottom);
            double overlapLeft = Math.Max(from.Left, to.Left);
            double overlapRight = Math.Min(from.Right, to.Right);

            if (Math.Abs(from.Right - to.Left) <= 3)
            {
                double y = Clamp(
                    (overlapTop + overlapBottom - height) / 2,
                    overlapTop,
                    overlapBottom - height);
                route.Add(new PixelPoint(from.Right - width, y));
                route.Add(new PixelPoint(to.Left, y));
            }
            else if (Math.Abs(from.Left - to.Right) <= 3)
            {
                double y = Clamp(
                    (overlapTop + overlapBottom - height) / 2,
                    overlapTop,
                    overlapBottom - height);
                route.Add(new PixelPoint(from.Left, y));
                route.Add(new PixelPoint(to.Right - width, y));
            }
            else if (Math.Abs(from.Bottom - to.Top) <= 3)
            {
                double x = Clamp(
                    (overlapLeft + overlapRight - width) / 2,
                    overlapLeft,
                    overlapRight - width);
                route.Add(new PixelPoint(x, from.Bottom - height));
                route.Add(new PixelPoint(x, to.Top));
            }
            else
            {
                double x = Clamp(
                    (overlapLeft + overlapRight - width) / 2,
                    overlapLeft,
                    overlapRight - width);
                route.Add(new PixelPoint(x, from.Top));
                route.Add(new PixelPoint(x, to.Bottom - height));
            }
        }

        private static double IntersectionArea(
            PixelBounds left,
            PixelBounds right)
        {
            double width = Math.Max(
                0,
                Math.Min(left.Right, right.Right) -
                Math.Max(left.Left, right.Left));
            double height = Math.Max(
                0,
                Math.Min(left.Bottom, right.Bottom) -
                Math.Max(left.Top, right.Top));
            return width * height;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (max < min)
            {
                return min;
            }
            return Math.Max(min, Math.Min(max, value));
        }
    }

    internal static class DisplayManager
    {
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint MonitorDefaultToNearest = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr hwnd,
            out NativeRect rect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(
            System.Drawing.Point point,
            uint flags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(
            IntPtr monitor,
            int dpiType,
            out uint dpiX,
            out uint dpiY);

        public static DisplayTopology Capture()
        {
            List<DisplayArea> areas = new List<DisplayArea>();
            foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            {
                double scaleX;
                double scaleY;
                GetScale(screen, out scaleX, out scaleY);
                areas.Add(new DisplayArea(
                    screen.DeviceName,
                    new PixelBounds(
                        screen.WorkingArea.Left,
                        screen.WorkingArea.Top,
                        screen.WorkingArea.Width,
                        screen.WorkingArea.Height),
                    screen.Primary,
                    scaleX,
                    scaleY));
            }
            return new DisplayTopology(areas);
        }

        public static string Signature(DisplayTopology topology)
        {
            StringBuilder value = new StringBuilder();
            foreach (DisplayArea area in topology.Areas.OrderBy(x => x.DeviceName))
            {
                PixelBounds bounds = area.WorkArea;
                value.Append(area.DeviceName).Append('|')
                    .Append(bounds.Left).Append(',')
                    .Append(bounds.Top).Append(',')
                    .Append(bounds.Width).Append(',')
                    .Append(bounds.Height).Append(',')
                    .Append(area.DpiScaleX.ToString("0.###")).Append(';');
            }
            return value.ToString();
        }

        public static PixelBounds GetWindowBounds(Window window)
        {
            IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
            NativeRect rect;
            if (handle != IntPtr.Zero && GetWindowRect(handle, out rect))
            {
                return new PixelBounds(
                    rect.Left,
                    rect.Top,
                    Math.Max(1, rect.Right - rect.Left),
                    Math.Max(1, rect.Bottom - rect.Top));
            }
            return new PixelBounds(
                window.Left,
                window.Top,
                Math.Max(1, window.Width),
                Math.Max(1, window.Height));
        }

        public static void MoveWindow(Window window, PixelPoint point)
        {
            IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
            if (handle == IntPtr.Zero)
            {
                return;
            }
            SetWindowPos(
                handle,
                IntPtr.Zero,
                (int)Math.Round(point.X),
                (int)Math.Round(point.Y),
                0,
                0,
                SwpNoSize | SwpNoZOrder | SwpNoActivate);
        }

        public static DisplayArea CurrentDisplay(
            Window window,
            DisplayTopology topology)
        {
            return topology.FindForWindow(GetWindowBounds(window));
        }

        public static DisplayArea CursorDisplay(DisplayTopology topology)
        {
            System.Drawing.Point cursor = Forms.Cursor.Position;
            PixelBounds cursorBounds =
                new PixelBounds(cursor.X, cursor.Y, 1, 1);
            return topology.FindForWindow(cursorBounds);
        }

        public static void CenterWindow(
            Window window,
            DisplayArea display)
        {
            if (display == null)
            {
                return;
            }
            PixelBounds windowBounds = GetWindowBounds(window);
            PixelBounds area = display.WorkArea;
            PixelPoint point = new PixelPoint(
                area.Left + (area.Width - windowBounds.Width) / 2,
                area.Top + (area.Height - windowBounds.Height) / 2);
            point = new DisplayTopology(new[] { display }).ClampTopLeft(
                point,
                windowBounds.Width,
                windowBounds.Height,
                display);
            MoveWindow(window, point);
        }

        private static void GetScale(
            Forms.Screen screen,
            out double scaleX,
            out double scaleY)
        {
            scaleX = 1;
            scaleY = 1;
            try
            {
                System.Drawing.Point center = new System.Drawing.Point(
                    screen.Bounds.Left + screen.Bounds.Width / 2,
                    screen.Bounds.Top + screen.Bounds.Height / 2);
                IntPtr monitor = MonitorFromPoint(
                    center,
                    MonitorDefaultToNearest);
                uint dpiX;
                uint dpiY;
                if (monitor != IntPtr.Zero &&
                    GetDpiForMonitor(monitor, 0, out dpiX, out dpiY) == 0)
                {
                    scaleX = dpiX / 96.0;
                    scaleY = dpiY / 96.0;
                }
            }
            catch
            {
            }
        }
    }
}
