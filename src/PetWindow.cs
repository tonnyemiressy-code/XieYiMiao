using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace XieYiMiao
{
    public sealed class PetWindow : Window
    {
        private const int HotKeyId = 0x51A0;
        private const int DiagnosticHotKeyId = 0x51A1;
        private const int WmHotKey = 0x0312;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;
        private const uint ModNoRepeat = 0x4000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(
            IntPtr hwnd,
            int id,
            uint modifiers,
            uint virtualKey);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        private readonly AppSettings _settings;
        private readonly CatControl _cat;
        private readonly Popup _whisper;
        private readonly TextBlock _whisperText;
        private readonly DispatcherTimer _timer;
        private readonly Random _random = new Random();
        private bool _dragging;
        private bool _moved;
        private Point _dragStartMouse;
        private PixelPoint _dragStartWindow;
        private PixelPoint _walkTarget;
        private readonly Queue<PixelPoint> _walkRoute =
            new Queue<PixelPoint>();
        private DateTime _nextWalkAt;
        private DateTime _nextIdleActionAt;
        private DateTime _nextWhisperAt;
        private DateTime _whisperUntil;
        private DateTime _happyUntil;
        private DateTime _specialUntil;
        private CatMood _specialMood = CatMood.Idle;
        private DateTime _lastFrame = DateTime.Now;
        private ReminderNotice _notice;
        private bool _protected;
        private IntPtr _handle;
        private bool _hotKeyRegistered;
        private bool _diagnosticHotKeyRegistered;
        private uint _activeHotKeyModifiers;
        private uint _activeHotKeyVirtualKey;
        private DateTime _nextDisplayCheck = DateTime.MinValue;
        private DisplayTopology _topology;
        private string _displaySignature;
        private double _lastPersistedPixelX = double.NaN;
        private double _lastPersistedPixelY = double.NaN;
        private bool _positionRestored;
        private string _lastWhisper = "";
        private PetInteractionLevel _lastInteractionLevel;

        public event Action SettingsRequested;
        public event Action ToggleRequested;
        public event Action ExitRequested;
        public event Action ReminderRequested;
        public event Action CareRequested;
        public event Action PetInteractionChanged;
        public event Action PositionChanged;
        public event Action<string, string, bool> ShortcutStatusChanged;

        public PetWindow(AppSettings settings)
        {
            _settings = settings;
            _lastInteractionLevel = settings.PetInteractionLevel;
            Width = 190;
            Height = 190;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            ShowActivated = false;
            Focusable = false;

            _cat = new CatControl
            {
                Margin = new Thickness(5),
                ToolTip = "摸摸小猫 · 右键打开功能菜单"
            };
            Content = _cat;

            _whisperText = Theme.Text(
                "",
                12,
                FontWeights.SemiBold,
                Theme.Ink);
            _whisperText.MaxWidth = 250;
            Border whisperCard = new Border
            {
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(14, 10, 14, 10),
                Child = _whisperText,
                IsHitTestVisible = false
            };
            _whisper = new Popup
            {
                PlacementTarget = _cat,
                Placement = PlacementMode.Top,
                AllowsTransparency = true,
                StaysOpen = true,
                PopupAnimation = PopupAnimation.Fade,
                Child = whisperCard,
                IsHitTestVisible = false
            };

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseRightButtonUp += OnMouseRightButtonUp;
            MouseEnter += OnMouseEnter;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += OnAnimationTick;
            _timer.Start();
            _nextWalkAt = DateTime.Now.AddSeconds(4);
            _nextIdleActionAt = DateTime.Now.AddSeconds(8);
            _nextWhisperAt = DateTime.Now.AddMinutes(
                NextWhisperMinutes());
            _topology = DisplayManager.Capture();
            _displaySignature = DisplayManager.Signature(_topology);

            Loaded += delegate
            {
                RestorePosition();
            };
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Topmost = true;
        }

        public void SetNotice(ReminderNotice notice)
        {
            _notice = notice;
            _cat.HasBadge = false;
            if (notice == null)
            {
                _happyUntil = DateTime.Now.AddSeconds(2.5);
                _cat.Mood = CatMood.Happy;
                return;
            }
            _cat.Mood = MoodFor(notice.Kind);
        }

        public void SetReminderBadge(bool visible)
        {
            _cat.HasBadge = visible;
        }

        public void Celebrate()
        {
            _happyUntil = DateTime.Now.AddSeconds(3.5);
            _cat.Mood = CatMood.Happy;
        }

        public void React(CatMood mood, double seconds)
        {
            _specialMood = mood;
            _specialUntil = DateTime.Now.AddSeconds(
                Math.Max(0.5, seconds));
            _cat.Mood = mood;
        }

        public void SetProtection(bool value)
        {
            _protected = value;
            if (value)
            {
                _whisper.IsOpen = false;
            }
            UpdateVisibility();
        }

        public void ApplySettings()
        {
            _cat.Outfit = _settings.SelectedPetOutfit;
            if (_lastInteractionLevel !=
                _settings.PetInteractionLevel)
            {
                _lastInteractionLevel =
                    _settings.PetInteractionLevel;
                _nextWhisperAt = DateTime.Now.AddMinutes(
                    NextWhisperMinutes());
                _nextIdleActionAt = DateTime.Now.AddSeconds(
                    NextIdleActionDelay());
                _nextWalkAt = DateTime.Now.AddSeconds(
                    NextWalkDelay());
                _whisper.IsOpen = false;
            }
            ApplyPetSize();
            RegisterConfiguredHotKey();
            UpdateVisibility();
            if (_settings.LimitedTerritory &&
                _settings.TerritoryHomeLeft < 0)
            {
                _settings.TerritoryHomeLeft = Left;
                _settings.TerritoryHomeTop = Top;
            }
        }

        public void ToggleVisibility()
        {
            _settings.PetVisible = !_settings.PetVisible;
            UpdateVisibility();
        }

        public void InitializeShortcut()
        {
            if (_handle == IntPtr.Zero)
            {
                IntPtr handle =
                    new WindowInteropHelper(this).EnsureHandle();
                AttachNativeSource(handle);
            }
        }

        internal bool HasNativeHandle
        {
            get { return _handle != IntPtr.Zero; }
        }

        internal bool HotKeyRegistered
        {
            get { return _hotKeyRegistered; }
        }

        internal CatMood CurrentMood
        {
            get { return _cat.Mood; }
        }

        internal PetOutfit CurrentOutfit
        {
            get { return _cat.Outfit; }
        }

        public string ActiveHotKeyText
        {
            get
            {
                return _hotKeyRegistered
                    ? FormatHotKey(
                        (int)_activeHotKeyModifiers,
                        (int)_activeHotKeyVirtualKey)
                    : "未注册";
            }
        }

        public bool IsUsingFallbackHotKey
        {
            get
            {
                return _hotKeyRegistered &&
                    (_activeHotKeyModifiers !=
                        ((uint)_settings.HotKeyModifiers | ModNoRepeat) ||
                     _activeHotKeyVirtualKey !=
                        (uint)_settings.HotKeyVirtualKey);
            }
        }

        internal bool SimulateHotKeyForTest()
        {
            bool handled = false;
            WndProc(
                _handle,
                WmHotKey,
                new IntPtr(HotKeyId),
                IntPtr.Zero,
                ref handled);
            return handled;
        }

        internal bool RegisterDiagnosticHotKeyForTest()
        {
            InitializeShortcut();
            if (_handle == IntPtr.Zero)
            {
                return false;
            }
            _diagnosticHotKeyRegistered = RegisterHotKey(
                _handle,
                DiagnosticHotKeyId,
                ModAlt | ModControl | ModShift,
                (uint)KeyInterop.VirtualKeyFromKey(Key.F12));
            return _diagnosticHotKeyRegistered;
        }

        public void ResetToCorner()
        {
            RefreshTopology();
            DisplayArea display = DisplayManager.CurrentDisplay(this, _topology);
            if (display == null)
            {
                display = _topology.Primary;
            }
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            PixelBounds area = display.WorkArea;
            DisplayManager.MoveWindow(
                this,
                new PixelPoint(
                    area.Right - bounds.Width - 22,
                    area.Bottom - bounds.Height - 8));
            SavePosition(true);
        }

        public void MoveToCursorDisplay()
        {
            RefreshTopology();
            DisplayArea display = DisplayManager.CursorDisplay(_topology);
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            PixelBounds area = display.WorkArea;
            DisplayManager.MoveWindow(
                this,
                new PixelPoint(
                    area.Right - bounds.Width - 22,
                    area.Bottom - bounds.Height - 8));
            SavePosition(true);
        }

        internal DisplayArea CurrentDisplayArea
        {
            get
            {
                RefreshTopology();
                return DisplayManager.CurrentDisplay(this, _topology);
            }
        }

        private void ApplyPetSize()
        {
            double newSize = 190 * _settings.PetSizePercent / 100.0;
            if (Math.Abs(Width - newSize) < 0.5 &&
                Math.Abs(Height - newSize) < 0.5)
            {
                return;
            }

            RefreshTopology();
            PixelBounds oldBounds = DisplayManager.GetWindowBounds(this);
            DisplayArea display = _topology.FindForWindow(oldBounds);
            PixelBounds area = display.WorkArea;
            double leftDistance = Math.Abs(oldBounds.Left - area.Left);
            double rightDistance = Math.Abs(oldBounds.Right - area.Right);
            double topDistance = Math.Abs(oldBounds.Top - area.Top);
            double bottomDistance = Math.Abs(oldBounds.Bottom - area.Bottom);
            double nearest = Math.Min(
                Math.Min(leftDistance, rightDistance),
                Math.Min(topDistance, bottomDistance));

            Width = newSize;
            Height = newSize;
            _cat.Margin = new Thickness(
                Math.Max(3, 5 * _settings.PetSizePercent / 100.0));

            if (!_positionRestored)
            {
                return;
            }
            UpdateLayout();
            PixelBounds newBounds = DisplayManager.GetWindowBounds(this);
            PixelPoint desired = new PixelPoint(oldBounds.Left, oldBounds.Top);
            if (nearest == rightDistance)
            {
                desired.X = area.Right - newBounds.Width;
            }
            else if (nearest == leftDistance)
            {
                desired.X = area.Left;
            }
            else if (nearest == bottomDistance)
            {
                desired.Y = area.Bottom - newBounds.Height;
            }
            else
            {
                desired.Y = area.Top;
            }
            DisplayManager.MoveWindow(this, desired);
            ConstrainToWorkArea();
            SavePosition(false);
        }

        private void UpdateVisibility()
        {
            if (_settings.PetVisible && !_protected)
            {
                if (!IsVisible)
                {
                    Show();
                }
            }
            else
            {
                _whisper.IsOpen = false;
                Hide();
            }
        }

        private void RestorePosition()
        {
            RefreshTopology();
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            DisplayArea display = _topology.FindByDevice(
                _settings.PetMonitorDevice);
            if (display == null)
            {
                display = _topology.Primary;
            }
            if (_settings.PetAnchorX >= 0 &&
                _settings.PetAnchorY >= 0)
            {
                PixelBounds area = display.WorkArea;
                DisplayManager.MoveWindow(
                    this,
                    new PixelPoint(
                        area.Left + _settings.PetAnchorX *
                            Math.Max(0, area.Width - bounds.Width),
                        area.Top + _settings.PetAnchorY *
                            Math.Max(0, area.Height - bounds.Height)));
                ConstrainToWorkArea();
            }
            else if (_settings.PetLeft != -1 &&
                     _settings.PetTop != -1)
            {
                DisplayManager.MoveWindow(
                    this,
                    new PixelPoint(
                        _settings.PetLeft * display.DpiScaleX,
                        _settings.PetTop * display.DpiScaleY));
                ConstrainToWorkArea();
            }
            else
            {
                ResetToCorner();
            }
            PixelBounds restored = DisplayManager.GetWindowBounds(this);
            _walkTarget = new PixelPoint(restored.Left, restored.Top);
            _positionRestored = true;
            SavePosition(false);
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double delta = Math.Min(0.2, (now - _lastFrame).TotalSeconds);
            _lastFrame = now;

            bool battery = Forms.SystemInformation.PowerStatus.PowerLineStatus ==
                           Forms.PowerLineStatus.Offline;
            _timer.Interval = TimeSpan.FromMilliseconds(
                AnimationIntervalMilliseconds(
                    _settings.EnergySaver,
                    battery));
            _cat.Tick(delta);
            if (_whisper.IsOpen && now >= _whisperUntil)
            {
                _whisper.IsOpen = false;
            }

            if (now >= _nextDisplayCheck)
            {
                HandleDisplayTopologyChange();
                _nextDisplayCheck = now.AddSeconds(2);
            }

            if (_notice != null || _dragging || _protected || !IsVisible)
            {
                return;
            }

            if (_happyUntil > now)
            {
                _cat.Mood = CatMood.Happy;
                return;
            }
            if (_specialUntil > now)
            {
                _cat.Mood = _specialMood;
                return;
            }

            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            double distance = Distance(
                new PixelPoint(bounds.Left, bounds.Top),
                _walkTarget);
            if (distance > 2)
            {
                _cat.Mood = CatMood.Walk;
                double speed = 72 * delta;
                double dx = _walkTarget.X - bounds.Left;
                double dy = _walkTarget.Y - bounds.Top;
                double length = Math.Sqrt(dx * dx + dy * dy);
                _cat.FacingLeft = dx < 0;
                PixelPoint next = new PixelPoint(
                    bounds.Left + dx / length * Math.Min(speed, length),
                    bounds.Top + dy / length * Math.Min(speed, length));
                PixelBounds nextBounds = new PixelBounds(
                    next.X,
                    next.Y,
                    bounds.Width,
                    bounds.Height);
                if (_topology.IsInsideDesktop(nextBounds))
                {
                    DisplayManager.MoveWindow(this, next);
                }
                else
                {
                    _walkRoute.Clear();
                    ConstrainToWorkArea();
                    _nextWalkAt = now.AddSeconds(5);
                }
                return;
            }

            if (_walkRoute.Count > 0)
            {
                _walkTarget = _walkRoute.Dequeue();
                return;
            }

            _cat.Mood = CatMood.Idle;
            SavePosition(false);
            if (now >= _nextWhisperAt)
            {
                ShowWhisper(now);
            }
            if (now >= _nextIdleActionAt)
            {
                BeginIdleAction(now);
                return;
            }
            if (now >= _nextWalkAt)
            {
                ChooseWalkTarget();
                _nextWalkAt = now.AddSeconds(NextWalkDelay());
            }
        }

        private void BeginIdleAction(DateTime now)
        {
            CatMood[] choices;
            int hour = now.Hour;
            if (hour >= 23 || hour < 7)
            {
                choices = _settings.EveningRitualEnabled
                    ? new[]
                    {
                        CatMood.DeepSleep,
                        CatMood.Loaf,
                        CatMood.Backpack,
                        CatMood.Companion
                    }
                    : new[]
                    {
                        CatMood.DeepSleep,
                        CatMood.Loaf,
                        CatMood.Companion
                    };
            }
            else if (_settings.PetInteractionLevel ==
                     PetInteractionLevel.Playful)
            {
                choices = new[]
                {
                    CatMood.Loaf,
                    CatMood.LongStretch,
                    CatMood.Companion,
                    CatMood.Happy,
                    CatMood.Petting
                };
            }
            else
            {
                List<CatMood> options = new List<CatMood>(
                    new[]
                    {
                        CatMood.Loaf,
                        CatMood.LongStretch,
                        CatMood.Companion,
                        CatMood.DeepSleep
                    });
                if (_settings.PetHeadPatCount >
                    _settings.PetBodyTapCount)
                {
                    options.Add(CatMood.Petting);
                }
                choices = options.ToArray();
            }
            _specialMood = choices[_random.Next(choices.Length)];
            _specialUntil = now.AddSeconds(
                _settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet
                    ? _random.Next(7, 13)
                    : _random.Next(4, 9));
            _cat.Mood = _specialMood;
            _nextIdleActionAt = now.AddSeconds(NextIdleActionDelay());
        }

        private int NextWalkDelay()
        {
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet)
            {
                return _random.Next(28, 50);
            }
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Playful)
            {
                return _random.Next(6, 13);
            }
            return _random.Next(11, 23);
        }

        private int NextIdleActionDelay()
        {
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet)
            {
                return _random.Next(30, 55);
            }
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Playful)
            {
                return _random.Next(8, 17);
            }
            return _random.Next(15, 30);
        }

        private int NextWhisperMinutes()
        {
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet)
            {
                return 24 * 60;
            }
            if (_settings.PetInteractionLevel ==
                PetInteractionLevel.Playful)
            {
                return _random.Next(12, 24);
            }
            return _random.Next(25, 46);
        }

        private void ShowWhisper(DateTime now)
        {
            _nextWhisperAt = now.AddMinutes(NextWhisperMinutes());
            if (_settings.PetInteractionLevel ==
                    PetInteractionLevel.Quiet ||
                _notice != null ||
                _protected ||
                !IsVisible)
            {
                return;
            }
            string[] phrases = CareContent.WhispersForHour(now.Hour);
            string value = CareContent.RandomFrom(phrases, _random);
            if (phrases.Length > 1 && value == _lastWhisper)
            {
                int index = Array.IndexOf(phrases, value);
                value = phrases[(index + 1) % phrases.Length];
            }
            _lastWhisper = value;
            _whisperText.Text = value;
            _whisper.HorizontalOffset = _cat.ActualWidth * 0.08;
            _whisper.VerticalOffset = -5;
            _whisper.IsOpen = true;
            _whisperUntil = now.AddSeconds(
                _settings.PetInteractionLevel ==
                PetInteractionLevel.Playful ? 6 : 5);
        }

        private void ChooseWalkTarget()
        {
            RefreshTopology();
            PixelBounds window = DisplayManager.GetWindowBounds(this);
            DisplayArea current = _topology.FindForWindow(window);
            DisplayArea targetDisplay = current;
            PixelBounds area;
            double targetWindowWidth;
            double targetWindowHeight;
            double minX;
            double maxX;
            double minY;
            double maxY;

            if (_settings.LimitedTerritory)
            {
                targetDisplay = _topology.FindByDevice(
                    _settings.TerritoryMonitorDevice) ?? current;
                area = targetDisplay.WorkArea;
                targetWindowWidth = Math.Max(
                    window.Width,
                    Width * targetDisplay.DpiScaleX);
                targetWindowHeight = Math.Max(
                    window.Height,
                    Height * targetDisplay.DpiScaleY);
                double homeX = _settings.TerritoryAnchorX >= 0
                    ? area.Left + _settings.TerritoryAnchorX *
                        Math.Max(0, area.Width - targetWindowWidth)
                    : window.Left;
                double homeY = _settings.TerritoryAnchorY >= 0
                    ? area.Top + _settings.TerritoryAnchorY *
                        Math.Max(0, area.Height - targetWindowHeight)
                    : window.Top;
                minX = Math.Max(area.Left, homeX - 220);
                maxX = Math.Min(
                    area.Right - targetWindowWidth,
                    homeX + 220);
                minY = Math.Max(area.Top, homeY - 40);
                maxY = Math.Min(
                    area.Bottom - targetWindowHeight,
                    homeY + 40);
            }
            else
            {
                IList<DisplayArea> displays = _topology.Areas;
                targetDisplay = displays[_random.Next(displays.Count)];
                area = targetDisplay.WorkArea;
                targetWindowWidth = Math.Max(
                    window.Width,
                    Width * targetDisplay.DpiScaleX);
                targetWindowHeight = Math.Max(
                    window.Height,
                    Height * targetDisplay.DpiScaleY);
                minX = area.Left;
                maxX = area.Right - targetWindowWidth;
                minY = area.Top;
                maxY = area.Bottom - targetWindowHeight;
            }

            PixelPoint final = new PixelPoint(
                RandomBetween(minX, maxX),
                RandomBetween(minY, maxY));
            _walkRoute.Clear();
            List<PixelPoint> route = _topology.BuildRoute(
                current,
                targetDisplay,
                final,
                Math.Max(window.Width, targetWindowWidth),
                Math.Max(window.Height, targetWindowHeight));
            if (route.Count == 0)
            {
                area = current.WorkArea;
                final = new PixelPoint(
                    RandomBetween(area.Left, area.Right - window.Width),
                    RandomBetween(area.Top, area.Bottom - window.Height));
                route.Add(final);
            }
            foreach (PixelPoint waypoint in route)
            {
                _walkRoute.Enqueue(waypoint);
            }
            _walkTarget = _walkRoute.Dequeue();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                ShowRandomInteraction();
                e.Handled = true;
                return;
            }

            _dragging = true;
            _moved = false;
            _dragStartMouse = PointToScreen(e.GetPosition(this));
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            _dragStartWindow = new PixelPoint(bounds.Left, bounds.Top);
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            Point current = PointToScreen(e.GetPosition(this));
            Vector delta = current - _dragStartMouse;
            if (delta.Length > 3)
            {
                _moved = true;
                _cat.Mood = CatMood.Held;
            }
            DisplayManager.MoveWindow(
                this,
                new PixelPoint(
                    _dragStartWindow.X + delta.X,
                    _dragStartWindow.Y + delta.Y));
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            _dragging = false;
            ReleaseMouseCapture();

            if (_moved)
            {
                SnapToEdge();
                SavePosition(true);
                React(CatMood.LongStretch, 1.8);
            }
            else if (_notice != null)
            {
                Action reminder = ReminderRequested;
                if (reminder != null)
                {
                    reminder();
                }
            }
            else
            {
                Point position = e.GetPosition(_cat);
                if (position.Y <= _cat.ActualHeight * 0.62)
                {
                    _settings.PetHeadPatCount++;
                    React(CatMood.Petting, 2.4);
                }
                else
                {
                    _settings.PetBodyTapCount++;
                    _happyUntil = DateTime.Now.AddSeconds(2);
                    _cat.Mood = CatMood.Happy;
                }
                Action changed = PetInteractionChanged;
                if (changed != null)
                {
                    changed();
                }
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (_notice != null ||
                _dragging ||
                _settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet)
            {
                return;
            }
            React(
                CatMood.Companion,
                _settings.PetInteractionLevel ==
                PetInteractionLevel.Playful ? 1.8 : 1.2);
        }

        private void ShowRandomInteraction()
        {
            CatMood[] choices =
                _settings.PetInteractionLevel ==
                PetInteractionLevel.Quiet
                    ? new[]
                    {
                        CatMood.Loaf,
                        CatMood.Companion,
                        CatMood.LongStretch
                    }
                    : new[]
                    {
                        CatMood.Happy,
                        CatMood.Petting,
                        CatMood.LongStretch,
                        CatMood.Loaf,
                        CatMood.Companion
                    };
            React(choices[_random.Next(choices.Length)], 2.5);
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.ContextMenu menu =
                new System.Windows.Controls.ContextMenu();
            menu.Items.Add(MenuItem(
                _settings.PetVisible ? "隐藏桌宠" : "显示桌宠",
                delegate
                {
                    Action toggle = ToggleRequested;
                    if (toggle != null)
                    {
                        toggle();
                    }
                }));
            menu.Items.Add(MenuItem(
                CareContent.StationName,
                delegate
                {
                    Action care = CareRequested;
                    if (care != null)
                    {
                        care();
                    }
                }));
            menu.Items.Add(MenuItem(
                "打开设置",
                delegate
                {
                    Action settings = SettingsRequested;
                    if (settings != null)
                    {
                        settings();
                    }
                }));
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(MenuItem(
                "退出歇一喵",
                delegate
                {
                    Action exit = ExitRequested;
                    if (exit != null)
                    {
                        exit();
                    }
                }));
            menu.IsOpen = true;
        }

        private static System.Windows.Controls.MenuItem MenuItem(
            string text,
            RoutedEventHandler click)
        {
            System.Windows.Controls.MenuItem item =
                new System.Windows.Controls.MenuItem { Header = text };
            item.Click += click;
            return item;
        }

        private void SnapToEdge()
        {
            RefreshTopology();
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            DisplayArea display = _topology.FindForWindow(bounds);
            PixelBounds area = display.WorkArea;
            double distanceLeft = Math.Abs(bounds.Left - area.Left);
            double distanceRight = Math.Abs(bounds.Right - area.Right);
            double distanceTop = Math.Abs(bounds.Top - area.Top);
            double distanceBottom = Math.Abs(bounds.Bottom - area.Bottom);
            double min = Math.Min(
                Math.Min(distanceLeft, distanceRight),
                Math.Min(distanceTop, distanceBottom));
            PixelPoint point = new PixelPoint(bounds.Left, bounds.Top);

            if (min == distanceLeft)
            {
                point.X = area.Left;
            }
            else if (min == distanceRight)
            {
                point.X = area.Right - bounds.Width;
            }
            else if (min == distanceTop)
            {
                point.Y = area.Top;
            }
            else
            {
                point.Y = area.Bottom - bounds.Height;
            }

            point = _topology.ClampTopLeft(
                point,
                bounds.Width,
                bounds.Height,
                display);
            DisplayManager.MoveWindow(this, point);
        }

        private void ConstrainToWorkArea()
        {
            RefreshTopology();
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            if (_topology.IsInsideDesktop(bounds))
            {
                return;
            }
            DisplayArea display = _topology.FindForWindow(bounds);
            PixelPoint safe = _topology.ClampTopLeft(
                new PixelPoint(bounds.Left, bounds.Top),
                bounds.Width,
                bounds.Height,
                display);
            DisplayManager.MoveWindow(this, safe);
            _walkTarget = safe;
            _walkRoute.Clear();
        }

        private void SavePosition(bool resetHome)
        {
            RefreshTopology();
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            DisplayArea display = _topology.FindForWindow(bounds);
            PixelBounds area = display.WorkArea;
            _settings.PetLeft = Left;
            _settings.PetTop = Top;
            _settings.PetMonitorDevice = display.DeviceName;
            _settings.PetAnchorX = Ratio(
                bounds.Left - area.Left,
                area.Width - bounds.Width);
            _settings.PetAnchorY = Ratio(
                bounds.Top - area.Top,
                area.Height - bounds.Height);
            if (resetHome)
            {
                _settings.TerritoryHomeLeft = Left;
                _settings.TerritoryHomeTop = Top;
                _settings.TerritoryMonitorDevice = display.DeviceName;
                _settings.TerritoryAnchorX = _settings.PetAnchorX;
                _settings.TerritoryAnchorY = _settings.PetAnchorY;
            }
            _walkTarget = new PixelPoint(bounds.Left, bounds.Top);
            bool changed = resetHome ||
                double.IsNaN(_lastPersistedPixelX) ||
                Math.Abs(_lastPersistedPixelX - bounds.Left) > 1 ||
                Math.Abs(_lastPersistedPixelY - bounds.Top) > 1;
            _lastPersistedPixelX = bounds.Left;
            _lastPersistedPixelY = bounds.Top;
            if (!changed)
            {
                return;
            }
            Action handler = PositionChanged;
            if (handler != null)
            {
                handler();
            }
        }

        private double RandomBetween(double min, double max)
        {
            if (max <= min)
            {
                return min;
            }
            return min + _random.NextDouble() * (max - min);
        }

        private static double Ratio(double value, double range)
        {
            if (range <= 0)
            {
                return 0;
            }
            return Math.Max(0, Math.Min(1, value / range));
        }

        private void RefreshTopology()
        {
            if (_topology == null)
            {
                _topology = DisplayManager.Capture();
                _displaySignature = DisplayManager.Signature(_topology);
            }
        }

        private void HandleDisplayTopologyChange()
        {
            DisplayTopology latest = DisplayManager.Capture();
            string signature = DisplayManager.Signature(latest);
            if (signature == _displaySignature)
            {
                return;
            }

            _topology = latest;
            _displaySignature = signature;
            PixelBounds bounds = DisplayManager.GetWindowBounds(this);
            DisplayArea remembered = _topology.FindByDevice(
                _settings.PetMonitorDevice);
            if (remembered == null)
            {
                remembered = _topology.FindForWindow(bounds);
            }
            PixelPoint safe = _topology.ClampTopLeft(
                new PixelPoint(bounds.Left, bounds.Top),
                bounds.Width,
                bounds.Height,
                remembered);
            DisplayManager.MoveWindow(this, safe);
            _walkRoute.Clear();
            _walkTarget = safe;
            SavePosition(false);
        }

        private static double Distance(PixelPoint left, PixelPoint right)
        {
            double dx = left.X - right.X;
            double dy = left.Y - right.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        internal static int AnimationIntervalMilliseconds(
            bool energySaver,
            bool onBattery)
        {
            return energySaver && onBattery ? 80 : 33;
        }

        internal static string FormatHotKey(int modifiers, int virtualKey)
        {
            string value = "";
            if ((modifiers & (int)ModControl) != 0)
            {
                value += "Ctrl + ";
            }
            if ((modifiers & (int)ModAlt) != 0)
            {
                value += "Alt + ";
            }
            if ((modifiers & (int)ModShift) != 0)
            {
                value += "Shift + ";
            }
            Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
            string keyText = key >= Key.A && key <= Key.Z
                ? key.ToString()
                : key == Key.Space
                    ? "Space"
                    : key.ToString();
            return value + keyText;
        }

        private static CatMood MoodFor(ReminderKind kind)
        {
            if (kind == ReminderKind.Drink)
            {
                return CatMood.Drink;
            }
            if (kind == ReminderKind.Movement)
            {
                return CatMood.Stretch;
            }
            if (kind == ReminderKind.Meal)
            {
                return CatMood.Meal;
            }
            if (kind == ReminderKind.Todo)
            {
                return CatMood.Todo;
            }
            if (kind == ReminderKind.Dorm)
            {
                return CatMood.Backpack;
            }
            return CatMood.Idle;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            if (_handle != IntPtr.Zero)
            {
                return;
            }

            IntPtr handle = new WindowInteropHelper(this).Handle;
            AttachNativeSource(handle);
        }

        private void AttachNativeSource(IntPtr handle)
        {
            if (_handle != IntPtr.Zero || handle == IntPtr.Zero)
            {
                return;
            }

            HwndSource source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                return;
            }
            _handle = handle;
            source.AddHook(WndProc);
            RegisterConfiguredHotKey();
        }

        private void RegisterConfiguredHotKey()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }
            if (_hotKeyRegistered)
            {
                UnregisterHotKey(_handle, HotKeyId);
                _hotKeyRegistered = false;
            }

            uint requestedModifiers =
                (uint)_settings.HotKeyModifiers | ModNoRepeat;
            uint requestedKey = (uint)_settings.HotKeyVirtualKey;
            uint[] modifiers =
            {
                requestedModifiers,
                ModControl | ModShift | ModNoRepeat,
                ModControl | ModAlt | ModShift | ModNoRepeat
            };
            uint[] keys =
            {
                requestedKey,
                (uint)KeyInterop.VirtualKeyFromKey(Key.M),
                (uint)KeyInterop.VirtualKeyFromKey(Key.M)
            };
            for (int i = 0; i < modifiers.Length; i++)
            {
                if (i > 0 &&
                    modifiers[i] == modifiers[0] &&
                    keys[i] == keys[0])
                {
                    continue;
                }
                if (RegisterHotKey(
                    _handle,
                    HotKeyId,
                    modifiers[i],
                    keys[i]))
                {
                    _hotKeyRegistered = true;
                    _activeHotKeyModifiers = modifiers[i];
                    _activeHotKeyVirtualKey = keys[i];
                    break;
                }
            }

            string requested = FormatHotKey(
                _settings.HotKeyModifiers,
                _settings.HotKeyVirtualKey);
            string status;
            if (!_hotKeyRegistered)
            {
                status = "快捷键被其他程序占用，请重新设置。";
            }
            else if (IsUsingFallbackHotKey)
            {
                status = requested + " 被占用，已自动改用 " +
                    ActiveHotKeyText + "。";
            }
            else
            {
                status = ActiveHotKeyText + " 可用。";
            }
            _cat.ToolTip = "摸摸小猫 · 右键菜单 · " +
                ActiveHotKeyText + " 显示或隐藏";
            Action<string, string, bool> handler = ShortcutStatusChanged;
            if (handler != null)
            {
                handler(
                    ActiveHotKeyText,
                    status,
                    IsUsingFallbackHotKey);
            }
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message == WmHotKey &&
                wParam.ToInt32() == HotKeyId)
            {
                Action toggle = ToggleRequested;
                if (toggle != null)
                {
                    toggle();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (_handle != IntPtr.Zero && _hotKeyRegistered)
            {
                UnregisterHotKey(_handle, HotKeyId);
                _hotKeyRegistered = false;
            }
            if (_handle != IntPtr.Zero && _diagnosticHotKeyRegistered)
            {
                UnregisterHotKey(_handle, DiagnosticHotKeyId);
                _diagnosticHotKeyRegistered = false;
            }
            _timer.Stop();
            _whisper.IsOpen = false;
        }
    }
}
