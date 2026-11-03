using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace XieYiMiao
{
    public enum ReminderActionKind
    {
        Complete,
        Snooze,
        Skip
    }

    public static class ReminderConfirmationText
    {
        private static readonly string[] CompleteTexts =
        {
            "真的完成啦？骗人是小狗，小猫可记住了。",
            "小猫已经举起完成印章，确定要盖下去吗？",
            "确认不是手滑，而是确实完成了？",
            "最后确认一次：这件事真的可以画上小勾了吗？",
            "小猫准备记一枚脚印啦，事实核对完毕吗？",
            "真的做到了？那小猫可要认真替你高兴了。",
            "完成按钮已经到站，确定在这里下车吗？",
            "这不是预告片，是正式完成，对吧？",
            "小猫的记录本已翻开，确定写“完成”？",
            "完成印章很可爱，但也要诚实使用哦。",
            "确定不是“马上就做”，而是“已经做完”？",
            "小猫眯起眼睛确认了一遍：真的好了吗？",
            "答案锁定后就会增加小脚印，确定锁定？",
            "这件事已经从现实世界里完成了吗？",
            "小猫准备庆祝了，确定不会让它白高兴吧？",
            "完成可以，先和事实碰个爪：真的完成了吗？",
            "你说完成，小猫就信；再给你一次确认机会。",
            "盖章之前再看一眼：不是点快了吧？",
            "完成记录即将生效，小猫请你郑重按爪。",
            "如果真的完成了，就放心收下这枚小脚印。"
        };

        private static readonly string[] SnoozeTexts =
        {
            "把它寄给 {0} 分钟后的你，确定发货吗？",
            "{0} 分钟后的你即将收到一个提醒包裹。",
            "小猫可以晚点再来，确定约在 {0} 分钟后？",
            "提醒先去门外坐 {0} 分钟，真的这样安排吗？",
            "这件事准备乘坐 {0} 分钟后的班车，确认上车？",
            "把决定交给 {0} 分钟后的自己可以，小猫再确认一次。",
            "确定需要 {0} 分钟缓冲，而不是刚才点快了？",
            "小猫先收起爪子，过 {0} 分钟再敲门？",
            "提醒将暂时隐身 {0} 分钟，确定启动隐身术？",
            "给现在的自己缓一缓，{0} 分钟后再见？",
            "延后 {0} 分钟的申请已写好，只差你最后按个爪。",
            "确定把这件事放到 {0} 分钟后的桌面上？",
            "小猫会在 {0} 分钟后准时回来，确定延后？",
            "{0} 分钟后的你将接收这条提醒，确认转交？",
            "暂缓不是消失：{0} 分钟后继续，确定吗？"
        };

        private static readonly string[] SkipTexts =
        {
            "确定跳过这一回？不批评，纯确认。",
            "小猫允许你跳过，只是检查一下是不是点错了。",
            "今天决定先放过这件事，对吗？",
            "跳过申请已送到猫爪办公室，确认批准？",
            "这次不做也可以，确定这是你现在的选择？",
            "小猫不会说教，只问一句：真的跳过吗？",
            "提醒准备退场，确定不是误触？",
            "今天先不安排这件事，答案锁定吗？",
            "小猫把这条提醒收起来，可以吗？",
            "跳过不等于失败，只确认这是清醒的决定。",
            "这件事今天先到这里，确定收工？",
            "小猫准备把提醒放回抽屉，真的放回去？",
            "可以跳过，不用解释；只需要再确认一次。",
            "这一回不记完成，也不再提醒，确定吗？",
            "最后看一眼：确实想跳过，而不是按错按钮？"
        };

        private static readonly Random Random = new Random();
        private static int _lastComplete = -1;
        private static int _lastSnooze = -1;
        private static int _lastSkip = -1;

        public static int PhraseCount
        {
            get
            {
                return CompleteTexts.Length +
                    SnoozeTexts.Length +
                    SkipTexts.Length;
            }
        }

        public static string Next(
            ReminderActionKind action,
            int minutes)
        {
            string[] values;
            int last;
            if (action == ReminderActionKind.Complete)
            {
                values = CompleteTexts;
                last = _lastComplete;
            }
            else if (action == ReminderActionKind.Snooze)
            {
                values = SnoozeTexts;
                last = _lastSnooze;
            }
            else
            {
                values = SkipTexts;
                last = _lastSkip;
            }

            int index;
            lock (Random)
            {
                do
                {
                    index = Random.Next(values.Length);
                }
                while (values.Length > 1 && index == last);
            }
            if (action == ReminderActionKind.Complete)
            {
                _lastComplete = index;
            }
            else if (action == ReminderActionKind.Snooze)
            {
                _lastSnooze = index;
            }
            else
            {
                _lastSkip = index;
            }
            return string.Format(values[index], minutes);
        }
    }

    public sealed class ReminderConfirmationWindow : Window
    {
        public ReminderConfirmationWindow(
            ReminderActionKind action,
            int minutes,
            ReminderNotice notice)
        {
            Title = "再确认一下";
            Width = 470;
            Height = 318;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            Border shell = new Border
            {
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(22),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(76, 54, 44),
                    BlurRadius = 26,
                    ShadowDepth = 7,
                    Opacity = 0.2
                }
            };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(118)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            Border catPlate = new Border
            {
                Background = Theme.PeachSoft,
                CornerRadius = new CornerRadius(20),
                Margin = new Thickness(0, 0, 18, 0)
            };
            catPlate.Child = new CatControl
            {
                Width = 108,
                Height = 108,
                Mood = MoodFor(notice == null
                    ? ReminderKind.Todo
                    : notice.Kind),
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(catPlate);

            StackPanel content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(content, 1);
            TextBlock eyebrow = Theme.Text(
                ActionLabel(action, minutes),
                11,
                FontWeights.Bold,
                Theme.MintDark);
            TextBlock prompt = Theme.Text(
                ReminderConfirmationText.Next(action, minutes),
                18,
                FontWeights.Bold,
                Theme.Ink);
            prompt.Margin = new Thickness(0, 7, 0, 7);
            TextBlock note = Theme.Text(
                "原来的操作还没有执行，选好以后再按一次。",
                11,
                FontWeights.Normal,
                Theme.Muted);
            note.Margin = new Thickness(0, 0, 0, 17);
            content.Children.Add(eyebrow);
            content.Children.Add(prompt);
            content.Children.Add(note);

            WrapPanel actions = new WrapPanel();
            Button cancel = Theme.Button(CancelText(action), false);
            Button confirm = Theme.Button(ConfirmText(action, minutes), true);
            cancel.Click += delegate { DialogResult = false; };
            confirm.Click += delegate { DialogResult = true; };
            actions.Children.Add(cancel);
            actions.Children.Add(confirm);
            content.Children.Add(actions);
            grid.Children.Add(content);
            shell.Child = grid;
            Content = shell;

            Loaded += delegate
            {
                if (Owner != null)
                {
                    DisplayTopology topology = DisplayManager.Capture();
                    DisplayManager.CenterWindow(
                        this,
                        topology.FindForWindow(
                            DisplayManager.GetWindowBounds(Owner)));
                }
            };
        }

        private static string ActionLabel(
            ReminderActionKind action,
            int minutes)
        {
            if (action == ReminderActionKind.Complete)
            {
                return "准备记录完成";
            }
            if (action == ReminderActionKind.Snooze)
            {
                return "准备延后 " + minutes + " 分钟";
            }
            return "准备跳过本次提醒";
        }

        private static string ConfirmText(
            ReminderActionKind action,
            int minutes)
        {
            if (action == ReminderActionKind.Complete)
            {
                return "真的完成了";
            }
            if (action == ReminderActionKind.Snooze)
            {
                return "确定，" + minutes + "分钟后见";
            }
            return "确认跳过";
        }

        private static string CancelText(ReminderActionKind action)
        {
            return action == ReminderActionKind.Complete
                ? "我再确认一下"
                : "返回提醒";
        }

        private static CatMood MoodFor(ReminderKind kind)
        {
            if (kind == ReminderKind.Drink) return CatMood.Drink;
            if (kind == ReminderKind.Movement) return CatMood.Stretch;
            if (kind == ReminderKind.Meal) return CatMood.Meal;
            if (kind == ReminderKind.Todo) return CatMood.Todo;
            return CatMood.Idle;
        }
    }
}
