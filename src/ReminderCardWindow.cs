using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace XieYiMiao
{
    public sealed class ReminderCardWindow : Window
    {
        private readonly CatControl _cat;
        private readonly TextBlock _kind;
        private readonly TextBlock _title;
        private readonly TextBlock _message;
        private readonly DispatcherTimer _collapseTimer;
        private ReminderNotice _notice;

        public event Action DoneRequested;
        public event Action<int> SnoozeRequested;
        public event Action SkipRequested;
        public event Action AutoCollapsed;

        public ReminderCardWindow()
        {
            Width = 430;
            Height = 224;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            ShowActivated = false;

            Border shell = new Border
            {
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(18),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(76, 54, 44),
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.18
                }
            };

            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(116)
            });
            root.ColumnDefinitions.Add(new ColumnDefinition());

            Border petPlate = new Border
            {
                Background = Theme.PeachSoft,
                CornerRadius = new CornerRadius(18),
                Margin = new Thickness(0, 0, 16, 0)
            };
            _cat = new CatControl
            {
                Width = 108,
                Height = 108,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            petPlate.Child = _cat;
            root.Children.Add(petPlate);

            StackPanel content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(content, 1);

            _kind = Theme.Text("", 11, FontWeights.Bold, Theme.MintDark);
            _kind.Margin = new Thickness(0, 0, 0, 4);
            _title = Theme.Text("", 20, FontWeights.Bold, Theme.Ink);
            _title.Margin = new Thickness(0, 0, 0, 5);
            _message = Theme.Text("", 13, FontWeights.Normal, Theme.Muted);
            _message.MinHeight = 38;
            _message.Margin = new Thickness(0, 0, 0, 13);

            WrapPanel actions = new WrapPanel();
            Button done = Theme.Button("完成", true);
            Button ten = Theme.Button("10分钟后", false);
            Button thirty = Theme.Button("30分钟后", false);
            Button skip = Theme.Button("跳过", false);
            done.Click += delegate { Raise(DoneRequested); };
            ten.Click += delegate { RaiseSnooze(10); };
            thirty.Click += delegate { RaiseSnooze(30); };
            skip.Click += delegate { Raise(SkipRequested); };
            actions.Children.Add(done);
            actions.Children.Add(ten);
            actions.Children.Add(thirty);
            actions.Children.Add(skip);

            content.Children.Add(_kind);
            content.Children.Add(_title);
            content.Children.Add(_message);
            content.Children.Add(actions);
            root.Children.Add(content);
            shell.Child = root;
            Content = shell;

            _collapseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _collapseTimer.Tick += delegate
            {
                _collapseTimer.Stop();
                Hide();
                Action handler = AutoCollapsed;
                if (handler != null)
                {
                    handler();
                }
            };
        }

        public void ShowNotice(ReminderNotice notice, PetWindow pet)
        {
            _notice = notice;
            _kind.Text = KindLabel(notice.Kind);
            _title.Text = notice.Title;
            _message.Text = notice.Message;
            _message.Visibility = string.IsNullOrWhiteSpace(notice.Message)
                ? Visibility.Collapsed
                : Visibility.Visible;
            _cat.Mood = MoodFor(notice.Kind);

            Opacity = 0;
            if (!IsVisible)
            {
                Show();
            }
            UpdateLayout();
            PositionNear(pet);
            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            _collapseTimer.Stop();
            _collapseTimer.Start();
        }

        public void HideCard()
        {
            _collapseTimer.Stop();
            Hide();
        }

        private void PositionNear(PetWindow pet)
        {
            DisplayTopology topology = DisplayManager.Capture();
            PixelBounds petBounds = DisplayManager.GetWindowBounds(pet);
            DisplayArea display = topology.FindForWindow(petBounds);
            PixelBounds area = display.WorkArea;
            PixelBounds cardBounds = DisplayManager.GetWindowBounds(this);
            PixelPoint position = CalculatePosition(
                petBounds,
                cardBounds,
                area);
            DisplayManager.MoveWindow(this, position);
        }

        internal static PixelPoint CalculatePosition(
            PixelBounds petBounds,
            PixelBounds cardBounds,
            PixelBounds area)
        {
            double x = petBounds.Right - cardBounds.Width - 16;
            double y = petBounds.Top - cardBounds.Height + 42;
            if (x < area.Left + 12)
            {
                x = area.Left + 12;
            }
            if (x + cardBounds.Width > area.Right - 12)
            {
                x = area.Right - cardBounds.Width - 12;
            }
            if (y < area.Top + 12)
            {
                y = petBounds.Bottom - 18;
            }
            if (y + cardBounds.Height > area.Bottom - 12)
            {
                y = area.Bottom - cardBounds.Height - 12;
            }
            return new PixelPoint(x, y);
        }

        private void Raise(Action action)
        {
            _collapseTimer.Stop();
            if (action != null)
            {
                action();
            }
        }

        private void RaiseSnooze(int minutes)
        {
            _collapseTimer.Stop();
            Action<int> handler = SnoozeRequested;
            if (handler != null)
            {
                handler(minutes);
            }
        }

        private static string KindLabel(ReminderKind kind)
        {
            if (kind == ReminderKind.Drink)
            {
                return "喝水提醒";
            }
            if (kind == ReminderKind.Movement)
            {
                return "活动提醒";
            }
            if (kind == ReminderKind.Meal)
            {
                return "吃饭提醒";
            }
            if (kind == ReminderKind.Dorm)
            {
                return "回宿舍提醒";
            }
            return "待办提醒";
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
            return CatMood.Idle;
        }
    }
}
