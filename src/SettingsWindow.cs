using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace XieYiMiao
{
    public sealed class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly string _dataFolder;
        private readonly ContentControl _content;
        private readonly TextBlock _pageTitle;
        private readonly TextBlock _pageSubtitle;
        private readonly StackPanel _navigation;
        private readonly TextBlock _shortcutSummary;
        private bool _forceClose;
        private string _currentPage = "home";
        private string _todoFilterCategoryId = "";
        private int _pendingHotKeyModifiers;
        private int _pendingHotKeyVirtualKey;
        private TextBox _shortcutCapture;
        private TextBlock _shortcutStatus;
        private string _runtimeShortcutText;
        private string _runtimeShortcutStatus;
        private readonly Random _random = new Random();

        private CheckBox _drinkEnabled;
        private TextBox _drinkInterval;
        private CheckBox _movementEnabled;
        private TextBox _movementInterval;
        private CheckBox _dormEnabled;
        private TextBox _dormTime;
        private CheckBox _petVisible;
        private Slider _petSize;
        private TextBlock _petSizeValue;
        private ComboBox _petInteraction;
        private ComboBox _petOutfit;
        private CheckBox _limitedTerritory;
        private CheckBox _energySaver;
        private CheckBox _fullScreen;
        private CheckBox _meetings;
        private CheckBox _manualMeeting;
        private CheckBox _warmMessages;
        private CheckBox _autoStart;
        private CheckBox _eveningRitual;
        private TextBox _idleThreshold;

        public event Action SettingsApplied;
        public event Action ResetPetRequested;
        public event Action FindWindowsRequested;
        public event Action CelebrationRequested;
        public event Action CareRequested;

        public SettingsWindow(AppSettings settings, string dataFolder)
        {
            _settings = settings;
            _dataFolder = dataFolder;
            Title = "歇一喵";
            Width = 1040;
            Height = 720;
            MinWidth = 930;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(210)
            });
            root.ColumnDefinitions.Add(new ColumnDefinition());

            Border side = new Border
            {
                Background = Theme.Cream,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            _navigation = new StackPanel
            {
                Margin = new Thickness(18, 22, 18, 18)
            };

            StackPanel brand = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 26)
            };
            Border brandPet = new Border
            {
                Width = 56,
                Height = 56,
                Background = Theme.PeachSoft,
                CornerRadius = new CornerRadius(18)
            };
            brandPet.Child = new CatControl
            {
                Width = 54,
                Height = 54
            };
            StackPanel brandText = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            brandText.Children.Add(Theme.Text(
                "歇一喵",
                21,
                FontWeights.Bold,
                Theme.Ink));
            brandText.Children.Add(Theme.Text(
                "安静地提醒你",
                11,
                FontWeights.Normal,
                Theme.Muted));
            brand.Children.Add(brandPet);
            brand.Children.Add(brandText);
            _navigation.Children.Add(brand);

            _navigation.Children.Add(NavButton("home", "今日小脚印"));
            _navigation.Children.Add(NavButton(
                "care",
                CareContent.StationName));
            _navigation.Children.Add(NavButton("reminders", "提醒安排"));
            _navigation.Children.Add(NavButton("todos", "待办事项"));
            _navigation.Children.Add(NavButton("rewards", "奖励中心"));
            _navigation.Children.Add(NavButton("pet", "桌宠行为"));
            _navigation.Children.Add(NavButton("other", "其他设置"));

            Border shortcut = new Border
            {
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12),
                Margin = new Thickness(2, 28, 2, 0)
            };
            StackPanel shortcutText = new StackPanel();
            shortcutText.Children.Add(Theme.Text(
                "快捷隐藏",
                11,
                FontWeights.Bold,
                Theme.MintDark));
            _shortcutSummary = Theme.Text(
                PetWindow.FormatHotKey(
                    _settings.HotKeyModifiers,
                    _settings.HotKeyVirtualKey),
                13,
                FontWeights.SemiBold,
                Theme.Ink);
            shortcutText.Children.Add(_shortcutSummary);
            shortcut.Child = shortcutText;
            _navigation.Children.Add(shortcut);
            side.Child = _navigation;
            root.Children.Add(side);

            Grid main = new Grid();
            main.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(106)
            });
            main.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(main, 1);

            Grid header = new Grid
            {
                Margin = new Thickness(34, 22, 34, 10)
            };
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel heading = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            _pageTitle = Theme.Text(
                "今日小脚印",
                26,
                FontWeights.Bold,
                Theme.Ink);
            _pageSubtitle = Theme.Text(
                "今天也按自己的节奏来",
                12,
                FontWeights.Normal,
                Theme.Muted);
            _pageSubtitle.Margin = new Thickness(0, 4, 0, 0);
            heading.Children.Add(_pageTitle);
            heading.Children.Add(_pageSubtitle);
            header.Children.Add(heading);

            Button save = Theme.Button("保存设置", true);
            save.VerticalAlignment = VerticalAlignment.Center;
            save.Click += delegate { SaveAll(true); };
            Grid.SetColumn(save, 1);
            header.Children.Add(save);
            main.Children.Add(header);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(34, 0, 34, 28)
            };
            _content = new ContentControl();
            scroll.Content = _content;
            Grid.SetRow(scroll, 1);
            main.Children.Add(scroll);
            root.Children.Add(main);
            Content = root;

            Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (!_forceClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            ShowPage("home");
        }

        public void RefreshCurrentPage()
        {
            ShowPage(_currentPage);
        }

        public void ShowPreviewPage(string page)
        {
            ShowPage(page);
        }

        internal double PetSizeControlValue
        {
            get { return _petSize == null ? -1 : _petSize.Value; }
            set
            {
                if (_petSize != null)
                {
                    _petSize.Value = value;
                }
            }
        }

        public void OpenTodosAndAdd()
        {
            ShowPage("todos");
            AddTodo();
        }

        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }

        private Button NavButton(string key, string label)
        {
            Button button = new Button
            {
                Tag = key,
                Content = label,
                Height = 44,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(14, 0, 10, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Theme.Ink,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += delegate
            {
                SaveAll(false);
                ShowPage(key);
            };
            return button;
        }

        private void ShowPage(string key)
        {
            _currentPage = key;
            UpdateNavigation();
            if (key == "home")
            {
                _pageTitle.Text = "今日小脚印";
                _pageSubtitle.Text = "今天也按自己的节奏来";
                _content.Content = BuildHome();
            }
            else if (key == "care")
            {
                _pageTitle.Text = CareContent.StationName;
                _pageSubtitle.Text = CareContent.FriendshipPositioning;
                _content.Content = BuildCareLanding();
            }
            else if (key == "reminders")
            {
                _pageTitle.Text = "提醒安排";
                _pageSubtitle.Text = "只计算实际使用电脑的时间";
                _content.Content = BuildReminders();
            }
            else if (key == "todos")
            {
                _pageTitle.Text = "待办事项";
                _pageSubtitle.Text = "到时间后，小猫会安静地提醒你";
                _content.Content = BuildTodos();
            }
            else if (key == "rewards")
            {
                _pageTitle.Text = "奖励中心";
                _pageSubtitle.Text = "成长被记住，自由度由你支配";
                _content.Content = BuildRewards();
            }
            else if (key == "pet")
            {
                _pageTitle.Text = "桌宠行为";
                _pageSubtitle.Text = "决定小猫在哪里活动、何时休息";
                _content.Content = BuildPet();
            }
            else
            {
                _pageTitle.Text = "其他设置";
                _pageSubtitle.Text = "开机启动、文案和智能离开判断";
                _content.Content = BuildOther();
            }
        }

        public void UpdateShortcutStatus(
            string shortcutText,
            string status)
        {
            _runtimeShortcutText = shortcutText;
            _runtimeShortcutStatus = status;
            if (_shortcutSummary != null)
            {
                _shortcutSummary.Text = string.IsNullOrWhiteSpace(shortcutText)
                    ? "未注册"
                    : shortcutText;
            }
            if (_shortcutStatus != null)
            {
                _shortcutStatus.Text = status ?? "";
            }
        }

        private void UpdateNavigation()
        {
            foreach (object child in _navigation.Children)
            {
                Button button = child as Button;
                if (button == null)
                {
                    continue;
                }
                bool active = (string)button.Tag == _currentPage;
                button.Background = active ? Theme.PeachSoft : Brushes.Transparent;
                button.Foreground = active ? Theme.MintDark : Theme.Ink;
            }
        }

        private FrameworkElement BuildHome()
        {
            _settings.Stats.EnsureToday();
            StackPanel page = new StackPanel();

            Border hero = new Border
            {
                Background = Theme.Brush("#EAF7F1"),
                BorderBrush = Theme.Brush("#CDEBDD"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(28, 22, 28, 22),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition());
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(150)
            });
            StackPanel heroText = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            heroText.Children.Add(Theme.Text(
                _settings.Stats.ReflectionStopped
                    ? "今天不复盘了。"
                    : "慢一点，也是在向前。",
                25,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock summary = Theme.Text(
                _settings.Stats.ReflectionStopped
                    ? "今天的统计先收起来。没有总结，也不会影响已经发生的成长。"
                    : "今天已经留下 " + _settings.Stats.TotalToday +
                        " 个小脚印。照顾好自己，再继续手上的事。",
                13,
                FontWeights.Normal,
                Theme.Muted);
            summary.Margin = new Thickness(0, 8, 0, 18);
            heroText.Children.Add(summary);

            WrapPanel quick = new WrapPanel();
            Button addTodo = Theme.Button("新增待办", true);
            Button addMeal = Theme.Button("添加吃饭时间", false);
            Button care = Theme.Button(CareContent.StationName, false);
            addTodo.Click += delegate { OpenTodosAndAdd(); };
            addMeal.Click += delegate
            {
                ShowPage("reminders");
                AddMealSchedule();
            };
            quick.Children.Add(addTodo);
            quick.Children.Add(addMeal);
            care.Click += delegate
            {
                Action handler = CareRequested;
                if (handler != null)
                {
                    handler();
                }
            };
            quick.Children.Add(care);
            heroText.Children.Add(quick);
            heroGrid.Children.Add(heroText);

            Border petPlate = new Border
            {
                Background = Theme.Card,
                CornerRadius = new CornerRadius(22)
            };
            petPlate.Child = new CatControl
            {
                Width = 135,
                Height = 135,
                Mood = CatMood.Happy
            };
            Grid.SetColumn(petPlate, 1);
            heroGrid.Children.Add(petPlate);
            hero.Child = heroGrid;
            page.Children.Add(hero);

            if (_settings.Stats.ReflectionStopped)
            {
                Border quiet = Theme.CardBorder();
                StackPanel quietText = new StackPanel();
                quietText.Children.Add(Theme.Text(
                    "停止反省券正在生效",
                    15,
                    FontWeights.Bold,
                    Theme.MintDark));
                quietText.Children.Add(Theme.Text(
                    "今日小脚印、成长点和未完成统计已经隐藏，明天会自然恢复。",
                    12,
                    FontWeights.Normal,
                    Theme.Muted));
                quiet.Child = quietText;
                page.Children.Add(quiet);
                return page;
            }

            UniformGrid stats = new UniformGrid
            {
                Columns = 5,
                Margin = new Thickness(0, 0, -10, 0)
            };
            stats.Children.Add(StatCard("喝水", _settings.Stats.DrinkCount, Theme.Sky));
            stats.Children.Add(StatCard("活动", _settings.Stats.MovementCount, Theme.Mint));
            stats.Children.Add(StatCard("吃饭", _settings.Stats.MealCount, Theme.Peach));
            stats.Children.Add(StatCard("回宿舍", _settings.Stats.DormCount, Theme.Apricot));
            stats.Children.Add(StatCard("待办", _settings.Stats.TodoCount, Theme.MintDark));
            page.Children.Add(stats);

            Grid progress = new Grid();
            progress.ColumnDefinitions.Add(new ColumnDefinition());
            progress.ColumnDefinitions.Add(new ColumnDefinition());
            Border growthCard = Theme.CardBorder();
            growthCard.Margin = new Thickness(0, 0, 7, 14);
            StackPanel growthText = new StackPanel();
            growthText.Children.Add(Theme.Text(
                "成长点 · " + _settings.Stats.GrowthPoints,
                17,
                FontWeights.Bold,
                Theme.MintDark));
            growthText.Children.Add(Theme.Text(
                "只累计、不消耗，记录长期坚持。",
                12,
                FontWeights.Normal,
                Theme.Muted));
            growthCard.Child = growthText;
            progress.Children.Add(growthCard);

            Border freedomCard = Theme.CardBorder();
            freedomCard.Margin = new Thickness(7, 0, 0, 14);
            StackPanel freedomText = new StackPanel();
            freedomText.Children.Add(Theme.Text(
                "自由度 · " + _settings.FreedomPoints,
                17,
                FontWeights.Bold,
                Theme.Apricot));
            freedomText.Children.Add(Theme.Text(
                "今天获得 " + _settings.Stats.FreedomEarnedToday +
                " 点，可以兑换真正想要的休息。",
                12,
                FontWeights.Normal,
                Theme.Muted));
            Button rewards = Theme.Button("去奖励中心", false);
            rewards.Margin = new Thickness(0, 10, 0, 0);
            rewards.HorizontalAlignment = HorizontalAlignment.Left;
            rewards.Click += delegate { ShowPage("rewards"); };
            freedomText.Children.Add(rewards);
            freedomCard.Child = freedomText;
            Grid.SetColumn(freedomCard, 1);
            progress.Children.Add(freedomCard);
            page.Children.Add(progress);
            return page;
        }

        private FrameworkElement BuildCareLanding()
        {
            StackPanel page = new StackPanel();
            Border hero = new Border
            {
                Background = Theme.MintSoft,
                BorderBrush = Theme.Brush("#CFE4DC"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(26, 20, 26, 20),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(150)
            });
            StackPanel text = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            text.Children.Add(Theme.Text(
                "桌面搭子已上线。",
                23,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock body = Theme.Text(
                "选一种天气、留张便签，或者和本喵一起暂停脑内会议。",
                13,
                FontWeights.Normal,
                Theme.Muted);
            body.Margin = new Thickness(0, 8, 0, 16);
            text.Children.Add(body);
            Button open = Theme.Button(
                "打开“" + CareContent.StationName + "”",
                true);
            open.HorizontalAlignment = HorizontalAlignment.Left;
            open.Click += delegate
            {
                Action handler = CareRequested;
                if (handler != null)
                {
                    handler();
                }
            };
            text.Children.Add(open);
            grid.Children.Add(text);

            Border petPlate = new Border
            {
                Background = Theme.Card,
                CornerRadius = new CornerRadius(22)
            };
            petPlate.Child = new CatControl
            {
                Width = 138,
                Height = 138,
                Mood = CatMood.Note,
                Outfit = _settings.SelectedPetOutfit
            };
            Grid.SetColumn(petPlate, 1);
            grid.Children.Add(petPlate);
            hero.Child = grid;
            page.Children.Add(hero);

            UniformGrid summaries = new UniformGrid
            {
                Columns = 3,
                Margin = new Thickness(0, 0, -10, 0)
            };
            EmotionWeatherCard weather =
                _settings.EmotionWeatherDateKey ==
                DateTime.Today.ToString("yyyy-MM-dd")
                    ? CareContent.FindWeather(
                        _settings.EmotionWeather)
                    : null;
            summaries.Children.Add(CareSummaryCard(
                "今日天气",
                weather == null
                    ? "还没有选择"
                    : weather.Symbol + " · " + weather.Title,
                Theme.Sky));
            summaries.Children.Add(CareSummaryCard(
                "纸条盒",
                _settings.WorryNotes.Count + " 张正在保管",
                Theme.Peach));
            bool ritualDone = _settings.EveningRitualHistory.Exists(
                x => x.DateKey ==
                    DateTime.Today.ToString("yyyy-MM-dd"));
            summaries.Children.Add(CareSummaryCard(
                "今天的收尾",
                ritualDone ? "已经收好了" : "还没有开始",
                Theme.Mint));
            page.Children.Add(summaries);

            Border principle = Theme.CardBorder();
            StackPanel principleText = new StackPanel();
            principleText.Children.Add(Theme.Text(
                "休息站不打卡，也不算分",
                15,
                FontWeights.Bold,
                Theme.MintDark));
            principleText.Children.Add(Theme.Text(
                "情绪天气、便签、自责整理和晚间收尾都不增加积分、连续天数或兑换门槛，随时可以关闭。",
                12,
                FontWeights.Normal,
                Theme.Muted));
            principle.Child = principleText;
            page.Children.Add(principle);
            return page;
        }

        private Border CareSummaryCard(
            string title,
            string value,
            Brush accent)
        {
            Border card = Theme.CardBorder();
            card.Margin = new Thickness(0, 0, 10, 14);
            StackPanel content = new StackPanel();
            Border mark = new Border
            {
                Width = 30,
                Height = 5,
                Background = accent,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            content.Children.Add(mark);
            content.Children.Add(Theme.Text(
                title,
                12,
                FontWeights.Bold,
                Theme.Muted));
            TextBlock valueText = Theme.Text(
                value,
                16,
                FontWeights.Bold,
                Theme.Ink);
            valueText.Margin = new Thickness(0, 4, 0, 0);
            content.Children.Add(valueText);
            card.Child = content;
            return card;
        }

        private FrameworkElement BuildReminders()
        {
            StackPanel page = new StackPanel();

            _drinkEnabled = new CheckBox
            {
                IsChecked = _settings.DrinkEnabled,
                Content = "启用",
                Foreground = Theme.Ink
            };
            _drinkInterval = SmallInput(_settings.DrinkIntervalMinutes.ToString());
            page.Children.Add(IntervalCard(
                "喝水",
                "按实际使用电脑的时间累计",
                _drinkEnabled,
                _drinkInterval,
                "分钟"));

            _movementEnabled = new CheckBox
            {
                IsChecked = _settings.MovementEnabled,
                Content = "启用",
                Foreground = Theme.Ink
            };
            _movementInterval =
                SmallInput(_settings.MovementIntervalMinutes.ToString());
            page.Children.Add(IntervalCard(
                "起身活动",
                "离开电脑达到设定阈值后会自动重新计时",
                _movementEnabled,
                _movementInterval,
                "分钟"));

            Border meals = Theme.CardBorder();
            StackPanel mealContent = new StackPanel();
            Grid mealHeader = new Grid();
            mealHeader.ColumnDefinitions.Add(new ColumnDefinition());
            mealHeader.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel mealTitle = new StackPanel();
            mealTitle.Children.Add(Theme.Text(
                "吃饭",
                16,
                FontWeights.Bold,
                Theme.Ink));
            mealTitle.Children.Add(Theme.Text(
                "不设置统一时间，由使用者自行添加",
                12,
                FontWeights.Normal,
                Theme.Muted));
            mealHeader.Children.Add(mealTitle);
            Button addMeal = Theme.Button("添加时间", false);
            addMeal.Click += delegate { AddMealSchedule(); };
            Grid.SetColumn(addMeal, 1);
            mealHeader.Children.Add(addMeal);
            mealContent.Children.Add(mealHeader);

            if (_settings.MealSchedules.Count == 0)
            {
                TextBlock empty = Theme.Text(
                    "还没有吃饭提醒。",
                    12,
                    FontWeights.Normal,
                    Theme.Muted);
                empty.Margin = new Thickness(0, 18, 0, 2);
                mealContent.Children.Add(empty);
            }
            else
            {
                foreach (ScheduleEntry entry in _settings.MealSchedules.ToList())
                {
                    mealContent.Children.Add(ScheduleRow(entry, false));
                }
            }
            meals.Child = mealContent;
            page.Children.Add(meals);

            Border dorm = Theme.CardBorder();
            Grid dormGrid = new Grid();
            dormGrid.ColumnDefinitions.Add(new ColumnDefinition());
            dormGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(90)
            });
            dormGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(80)
            });
            StackPanel dormText = new StackPanel();
            dormText.Children.Add(Theme.Text(
                "回宿舍",
                16,
                FontWeights.Bold,
                Theme.Ink));
            dormText.Children.Add(Theme.Text(
                "默认每天提醒，超过2小时自动过期",
                12,
                FontWeights.Normal,
                Theme.Muted));
            dormGrid.Children.Add(dormText);
            _dormTime = SmallInput(_settings.DormSchedule.TimeText);
            _dormTime.Margin = new Thickness(8, 0, 8, 0);
            Grid.SetColumn(_dormTime, 1);
            dormGrid.Children.Add(_dormTime);
            _dormEnabled = new CheckBox
            {
                Content = "启用",
                IsChecked = _settings.DormSchedule.Enabled,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Theme.Ink
            };
            Grid.SetColumn(_dormEnabled, 2);
            dormGrid.Children.Add(_dormEnabled);
            dorm.Child = dormGrid;
            page.Children.Add(dorm);
            return page;
        }

        private FrameworkElement BuildTodos()
        {
            StackPanel page = new StackPanel();
            Border toolbar = new Border
            {
                Background = Theme.Brush("#EAF7F1"),
                BorderBrush = Theme.Brush("#CDEBDD"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid toolGrid = new Grid();
            toolGrid.ColumnDefinitions.Add(new ColumnDefinition());
            toolGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            toolGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            toolGrid.Children.Add(Theme.Text(
                "把要记住的事交给小猫。",
                14,
                FontWeights.SemiBold,
                Theme.Ink));
            ComboBox filter = new ComboBox
            {
                MinWidth = 126,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(8, 0, 8, 0),
                Background = Theme.Card,
                BorderBrush = Theme.Line
            };
            filter.Items.Add(new ComboBoxItem
            {
                Content = "全部分类",
                Tag = ""
            });
            foreach (TodoCategory category in _settings.TodoCategories
                .OrderBy(x => x.SortOrder))
            {
                filter.Items.Add(new ComboBoxItem
                {
                    Content = category.Name,
                    Tag = category.Id
                });
            }
            filter.SelectedIndex = 0;
            for (int i = 0; i < filter.Items.Count; i++)
            {
                ComboBoxItem choice = (ComboBoxItem)filter.Items[i];
                if ((string)choice.Tag == _todoFilterCategoryId)
                {
                    filter.SelectedIndex = i;
                    break;
                }
            }
            filter.SelectionChanged += delegate
            {
                ComboBoxItem selected = filter.SelectedItem as ComboBoxItem;
                _todoFilterCategoryId =
                    selected == null ? "" : (string)selected.Tag;
                ShowPage("todos");
            };
            Grid.SetColumn(filter, 1);
            toolGrid.Children.Add(filter);
            WrapPanel todoActions = new WrapPanel();
            Button manage = Theme.Button("管理分类", false);
            manage.Click += delegate { ManageTodoCategories(); };
            Button add = Theme.Button("新增待办", true);
            add.Click += delegate { AddTodo(); };
            todoActions.Children.Add(manage);
            todoActions.Children.Add(add);
            Grid.SetColumn(todoActions, 2);
            toolGrid.Children.Add(todoActions);
            toolbar.Child = toolGrid;
            page.Children.Add(toolbar);

            TodoItem[] active = _settings.Todos
                .Where(x => !x.Completed)
                .Where(x => string.IsNullOrWhiteSpace(
                    _todoFilterCategoryId) ||
                    x.CategoryId == _todoFilterCategoryId)
                .OrderBy(x => x.DueLocal)
                .ToArray();
            if (active.Length == 0)
            {
                Border empty = Theme.CardBorder();
                StackPanel emptyContent = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 30)
                };
                CatControl cat = new CatControl
                {
                    Width = 120,
                    Height = 120,
                    Mood = CatMood.Todo
                };
                emptyContent.Children.Add(cat);
                TextBlock emptyText = Theme.Text(
                    "暂时没有待办",
                    15,
                    FontWeights.Bold,
                    Theme.Ink);
                emptyText.HorizontalAlignment = HorizontalAlignment.Center;
                emptyContent.Children.Add(emptyText);
                empty.Child = emptyContent;
                page.Children.Add(empty);
            }
            else
            {
                foreach (TodoItem item in active)
                {
                    page.Children.Add(TodoRow(item));
                }
            }
            return page;
        }

        private FrameworkElement BuildRewards()
        {
            _settings.Stats.EnsureToday();
            StackPanel page = new StackPanel();
            Border hero = new Border
            {
                Background = Theme.Brush("#FFF0E4"),
                BorderBrush = Theme.Brush("#F6D2BA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition());
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(130)
            });
            StackPanel heroText = new StackPanel();
            heroText.Children.Add(Theme.Text(
                "可用自由度  " + _settings.FreedomPoints,
                27,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock balance = Theme.Text(
                "成长点 " + _settings.Stats.GrowthPoints +
                " · 今日获得 " + _settings.Stats.FreedomEarnedToday +
                " · 累计获得 " + _settings.TotalFreedomEarned,
                12,
                FontWeights.Normal,
                Theme.Muted);
            balance.Margin = new Thickness(0, 6, 0, 12);
            heroText.Children.Add(balance);
            heroText.Children.Add(Theme.Text(
                _settings.Stats.AllCareCompleted
                    ? "今天的四枚猫爪已经点亮。现在，自由度由你支配。"
                    : "先把四份照顾留给自己，再去兑换其他快乐。",
                13,
                FontWeights.SemiBold,
                _settings.Stats.AllCareCompleted
                    ? Theme.MintDark
                    : Theme.Apricot));
            heroGrid.Children.Add(heroText);
            Border catPlate = new Border
            {
                Background = Theme.Card,
                CornerRadius = new CornerRadius(20)
            };
            catPlate.Child = new CatControl
            {
                Width = 120,
                Height = 120,
                Mood = CatMood.Happy
            };
            Grid.SetColumn(catPlate, 1);
            heroGrid.Children.Add(catPlate);
            hero.Child = heroGrid;
            page.Children.Add(hero);

            page.Children.Add(BuildDailyCareSection());
            AddRewardCategorySection(
                page,
                RewardCategory.SelfFirst,
                "今天先考虑自己 · 免费",
                "不用花自由度，可以不限次数使用。",
                true);
            AddRewardCategorySection(
                page,
                RewardCategory.Comfort,
                "舒服一点 · 免费",
                "没有建议时长，什么时候休息够了由你决定。",
                true);
            page.Children.Add(BuildHumanCareTools());
            page.Children.Add(BuildMoodLanding());

            Border toolbar = new Border
            {
                Background = Theme.Brush("#EAF7F1"),
                BorderBrush = Theme.Brush("#CDEBDD"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid toolGrid = new Grid();
            toolGrid.ColumnDefinitions.Add(new ColumnDefinition());
            toolGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            long weekMinutes = _settings.RewardHistory
                .Where(x => x.RedeemedAtLocal >= StartOfWeek(DateTime.Now))
                .Sum(x => (long)x.DurationMinutes);
            StackPanel toolText = new StackPanel();
            toolText.Children.Add(Theme.Text(
                "需要自由度的奖励",
                15,
                FontWeights.Bold,
                Theme.Ink));
            toolText.Children.Add(Theme.Text(
                _settings.Stats.AllCareCompleted
                    ? "四爪已完成 · 本周已安排 " +
                        FriendlyDuration(weekMinutes) + " 的自由时间"
                    : "完成上面的四张照顾卡后即可解锁",
                11,
                FontWeights.Normal,
                _settings.Stats.AllCareCompleted
                    ? Theme.MintDark
                    : Theme.Apricot));
            toolGrid.Children.Add(toolText);
            WrapPanel toolActions = new WrapPanel();
            Button random = Theme.Button("随机一个可兑换奖励", false);
            Button add = Theme.Button("新增奖励", true);
            random.Click += delegate { PickRandomReward(); };
            add.Click += delegate { AddReward(); };
            toolActions.Children.Add(random);
            toolActions.Children.Add(add);
            Grid.SetColumn(toolActions, 1);
            toolGrid.Children.Add(toolActions);
            toolbar.Child = toolGrid;
            page.Children.Add(toolbar);

            Border rules = Theme.CardBorder();
            StackPanel ruleText = new StackPanel();
            ruleText.Children.Add(Theme.Text(
                "自由度获得规则 · 不设每日上限",
                14,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock ruleLine = Theme.Text(
                "喝水 +1　活动 +2　吃饭 +2　回宿舍 +2　" +
                "简单待办 +1　普通待办 +3　重要待办 +5　" +
                "当天五类都完成过一次，再奖励 +3",
                12,
                FontWeights.Normal,
                Theme.Muted);
            ruleLine.Margin = new Thickness(0, 6, 0, 0);
            ruleText.Children.Add(ruleLine);
            rules.Child = ruleText;
            page.Children.Add(rules);

            RewardCategory[] paidOrder =
            {
                RewardCategory.Venting,
                RewardCategory.Food,
                RewardCategory.Social,
                RewardCategory.Entertainment,
                RewardCategory.Outdoors,
                RewardCategory.Hobby,
                RewardCategory.Rest,
                RewardCategory.Personal,
                RewardCategory.Custom
            };
            foreach (RewardCategory category in paidOrder)
            {
                if (_settings.Rewards.Any(x =>
                    x.Category == category && !x.IsFree))
                {
                    AddRewardCategorySection(
                        page,
                        category,
                        RewardCategoryText(category),
                        PaidCategorySubtitle(category),
                        category == RewardCategory.Venting);
                }
            }

            if (_settings.RewardHistory.Count > 0)
            {
                Border history = Theme.CardBorder();
                StackPanel historyText = new StackPanel();
                historyText.Children.Add(Theme.Text(
                    "最近兑换",
                    15,
                    FontWeights.Bold,
                    Theme.Ink));
                foreach (RewardRedemption entry in _settings.RewardHistory
                    .Take(8))
                {
                    string line = entry.RedeemedAtLocal.ToString(
                        "MM-dd HH:mm") + "　" + entry.RewardTitle +
                        (entry.FreedomCost == 0
                            ? "　免费使用"
                            : "　-" + entry.FreedomCost + " 自由度");
                    TextBlock item = Theme.Text(
                        line,
                        11,
                        FontWeights.Normal,
                        Theme.Muted);
                    item.Margin = new Thickness(0, 7, 0, 0);
                    historyText.Children.Add(item);
                }
                history.Child = historyText;
                page.Children.Add(history);
            }
            return page;
        }

        private FrameworkElement BuildDailyCareSection()
        {
            Border shell = new Border
            {
                Background = Theme.Brush("#EAF7F1"),
                BorderBrush = Theme.Brush("#CDEBDD"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 14)
            };
            StackPanel content = new StackPanel();
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel heading = new StackPanel();
            heading.Children.Add(Theme.Text(
                "今日先照顾自己",
                18,
                FontWeights.Bold,
                Theme.Ink));
            heading.Children.Add(Theme.Text(
                "每天四张、每张一次。没有断签、欠账或失败。",
                11,
                FontWeights.Normal,
                Theme.Muted));
            header.Children.Add(heading);
            TextBlock progress = Theme.Text(
                _settings.Stats.CareCompletedCount + " / 4 枚猫爪",
                13,
                FontWeights.Bold,
                _settings.Stats.AllCareCompleted
                    ? Theme.MintDark
                    : Theme.Apricot);
            Grid.SetColumn(progress, 1);
            header.Children.Add(progress);
            content.Children.Add(header);

            UniformGrid cards = new UniformGrid
            {
                Columns = 2,
                Margin = new Thickness(0, 14, -10, 0)
            };
            foreach (DailyCareCard card in CareContent.DailyCards)
            {
                bool completed = _settings.Stats.IsCareCompleted(card.Kind);
                Border item = new Border
                {
                    Background = completed
                        ? Theme.Brush("#DDF2E8")
                        : Theme.Card,
                    BorderBrush = completed
                        ? Theme.Mint
                        : Theme.Line,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(14),
                    Margin = new Thickness(0, 0, 10, 10)
                };
                StackPanel itemContent = new StackPanel();
                WrapPanel titleLine = new WrapPanel();
                TextBlock paw = Theme.Text(
                    completed ? "● " : "○ ",
                    16,
                    FontWeights.Bold,
                    completed ? Theme.MintDark : Theme.Apricot);
                titleLine.Children.Add(paw);
                titleLine.Children.Add(Theme.Text(
                    card.PawLabel + " · " + card.Title,
                    13,
                    FontWeights.Bold,
                    Theme.Ink));
                itemContent.Children.Add(titleLine);
                TextBlock prompt = Theme.Text(
                    completed ? card.CompletedText : card.Prompt,
                    11,
                    FontWeights.Normal,
                    Theme.Muted);
                prompt.Margin = new Thickness(0, 6, 0, 9);
                prompt.MinHeight = 34;
                itemContent.Children.Add(prompt);
                Button complete = Theme.Button(
                    completed ? "今天已经做到了" : "我今天做到了",
                    !completed);
                complete.HorizontalAlignment = HorizontalAlignment.Left;
                complete.Padding = new Thickness(11, 6, 11, 6);
                complete.IsEnabled = !completed;
                DailyCareKind kind = card.Kind;
                complete.Click += delegate { CompleteDailyCare(kind); };
                itemContent.Children.Add(complete);
                item.Child = itemContent;
                cards.Children.Add(item);
            }
            content.Children.Add(cards);
            if (_settings.Stats.AllCareCompleted)
            {
                TextBlock completedText = Theme.Text(
                    "四枚猫爪都亮了。今天不用更优秀，也已经值得被好好照顾。",
                    12,
                    FontWeights.SemiBold,
                    Theme.MintDark);
                completedText.Margin = new Thickness(2, 3, 0, 0);
                content.Children.Add(completedText);
            }
            shell.Child = content;
            return shell;
        }

        private void AddRewardCategorySection(
            Panel page,
            RewardCategory category,
            string title,
            string subtitle,
            bool expanded)
        {
            RewardItem[] rewards = _settings.Rewards
                .Where(x => x.Category == category)
                .OrderByDescending(x => x.Favorite)
                .ThenBy(x => x.FreedomCost)
                .ToArray();
            if (rewards.Length == 0)
            {
                return;
            }
            Expander section = new Expander
            {
                IsExpanded = expanded,
                Margin = new Thickness(0, 0, 0, 10),
                Background = Brushes.Transparent
            };
            StackPanel header = new StackPanel();
            header.Children.Add(Theme.Text(
                title,
                15,
                FontWeights.Bold,
                category == RewardCategory.SelfFirst ||
                category == RewardCategory.Comfort
                    ? Theme.MintDark
                    : Theme.Ink));
            header.Children.Add(Theme.Text(
                subtitle,
                11,
                FontWeights.Normal,
                Theme.Muted));
            section.Header = header;
            StackPanel items = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };
            foreach (RewardItem reward in rewards)
            {
                items.Children.Add(RewardCard(reward));
            }
            section.Content = items;
            page.Children.Add(section);
        }

        private FrameworkElement BuildHumanCareTools()
        {
            Border card = Theme.CardBorder();
            StackPanel content = new StackPanel();
            content.Children.Add(Theme.Text(
                "小猫关怀工具 · 免费",
                15,
                FontWeights.Bold,
                Theme.Ink));
            content.Children.Add(Theme.Text(
                "不计分、不分析，只在需要时借你一句话。",
                11,
                FontWeights.Normal,
                Theme.Muted));

            TextBlock permission = Theme.Text(
                "点击按钮，抽一张今天可以相信的许可条。",
                13,
                FontWeights.SemiBold,
                Theme.MintDark);
            permission.Margin = new Thickness(0, 14, 0, 8);
            content.Children.Add(permission);
            WrapPanel permissionActions = new WrapPanel();
            Button draw = Theme.Button("抽一张小猫许可条", true);
            draw.Click += delegate
            {
                permission.Text = CareContent.RandomPermission(_random);
            };
            permissionActions.Children.Add(draw);
            content.Children.Add(permissionActions);

            Grid boundary = new Grid
            {
                Margin = new Thickness(0, 15, 0, 0)
            };
            boundary.ColumnDefinitions.Add(new ColumnDefinition());
            boundary.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            ComboBox phrases = new ComboBox
            {
                ItemsSource = CareContent.BoundaryPhrases,
                SelectedIndex = 0,
                Padding = new Thickness(8),
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                Margin = new Thickness(0, 0, 10, 0)
            };
            boundary.Children.Add(phrases);
            Button copy = Theme.Button("复制边界表达", false);
            copy.Click += delegate
            {
                try
                {
                    Clipboard.SetText((string)phrases.SelectedItem);
                    copy.Content = "已复制";
                }
                catch
                {
                    copy.Content = "复制失败";
                }
            };
            Grid.SetColumn(copy, 1);
            boundary.Children.Add(copy);
            content.Children.Add(boundary);

            Button stopReview = Theme.Button(
                _settings.Stats.ReflectionStopped
                    ? "今天已经停止复盘"
                    : "使用“停止反省券”",
                false);
            stopReview.HorizontalAlignment = HorizontalAlignment.Left;
            stopReview.Margin = new Thickness(0, 14, 0, 0);
            stopReview.IsEnabled = !_settings.Stats.ReflectionStopped;
            stopReview.Click += delegate
            {
                _settings.Stats.ReflectionStopped = true;
                RaiseSettingsApplied();
                ShowPage("rewards");
            };
            content.Children.Add(stopReview);
            card.Child = content;
            return card;
        }

        private FrameworkElement BuildMoodLanding()
        {
            Border card = Theme.CardBorder();
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(150)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel moodPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 12, 0)
            };
            moodPanel.Children.Add(Theme.Text(
                "今天的感受",
                12,
                FontWeights.Bold,
                Theme.Ink));
            ComboBox mood = new ComboBox
            {
                ItemsSource = CareContent.MoodKeys,
                SelectedItem = string.IsNullOrWhiteSpace(
                    _settings.Stats.MoodKey)
                    ? CareContent.MoodKeys[0]
                    : _settings.Stats.MoodKey,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 6, 0, 0),
                Background = Theme.Card,
                BorderBrush = Theme.Line
            };
            moodPanel.Children.Add(mood);
            grid.Children.Add(moodPanel);
            StackPanel notePanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 12, 0)
            };
            notePanel.Children.Add(Theme.Text(
                "留一句只存在本机的话（可选）",
                12,
                FontWeights.Bold,
                Theme.Ink));
            TextBox note = SmallInput(_settings.Stats.MoodNote);
            note.HorizontalContentAlignment = HorizontalAlignment.Left;
            note.Margin = new Thickness(0, 6, 0, 0);
            notePanel.Children.Add(note);
            Grid.SetColumn(notePanel, 1);
            grid.Children.Add(notePanel);
            Button save = Theme.Button("保存情绪落点", true);
            save.VerticalAlignment = VerticalAlignment.Bottom;
            save.Click += delegate
            {
                _settings.Stats.MoodKey =
                    mood.SelectedItem == null
                        ? ""
                        : mood.SelectedItem.ToString();
                _settings.Stats.MoodNote = note.Text.Trim();
                RaiseSettingsApplied();
                save.Content = "已保存";
            };
            Grid.SetColumn(save, 2);
            grid.Children.Add(save);
            card.Child = grid;
            return card;
        }

        private FrameworkElement RewardCard(RewardItem reward)
        {
            Border card = Theme.CardBorder();
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel info = new StackPanel();
            WrapPanel titleRow = new WrapPanel();
            TextBlock title = Theme.Text(
                reward.Title,
                15,
                FontWeights.Bold,
                Theme.Ink);
            titleRow.Children.Add(title);
            TextBlock cost = Theme.Text(
                reward.IsFree
                    ? "  免费 · 不限次数"
                    : "  " + reward.FreedomCost + " 自由度",
                12,
                FontWeights.Bold,
                reward.IsFree ||
                (reward.FreedomCost <= _settings.FreedomPoints &&
                 _settings.Stats.AllCareCompleted)
                    ? Theme.MintDark
                    : Theme.Muted);
            titleRow.Children.Add(cost);
            info.Children.Add(titleRow);
            if (!string.IsNullOrWhiteSpace(reward.Description))
            {
                TextBlock description = Theme.Text(
                    reward.Description,
                    11,
                    FontWeights.Normal,
                    Theme.Muted);
                description.Margin = new Thickness(0, 5, 0, 0);
                info.Children.Add(description);
            }
            if (reward.DurationMinutes > 0)
            {
                TextBlock duration = Theme.Text(
                    "建议时长 " + FriendlyDuration(
                        reward.DurationMinutes),
                    11,
                    FontWeights.SemiBold,
                    Theme.Apricot);
                duration.Margin = new Thickness(0, 5, 0, 0);
                info.Children.Add(duration);
            }
            grid.Children.Add(info);

            WrapPanel actions = new WrapPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Button favorite = Theme.Button(
                reward.Favorite ? "★" : "☆",
                false);
            Button redeem = Theme.Button(
                reward.IsFree ? "使用" : "兑换",
                true);
            Button edit = Theme.Button("编辑", false);
            Button remove = Theme.Button("删除", false);
            favorite.MinWidth = 40;
            favorite.Padding = new Thickness(8, 6, 8, 6);
            redeem.Padding = edit.Padding = remove.Padding =
                new Thickness(10, 6, 10, 6);
            redeem.IsEnabled = reward.IsFree ||
                (_settings.Stats.AllCareCompleted &&
                 reward.FreedomCost <= _settings.FreedomPoints);
            if (!reward.IsFree && !_settings.Stats.AllCareCompleted)
            {
                redeem.ToolTip = "先完成今天的四张自我照顾卡";
            }
            favorite.Click += delegate
            {
                reward.Favorite = !reward.Favorite;
                RaiseSettingsApplied();
                ShowPage("rewards");
            };
            redeem.Click += delegate { RedeemReward(reward); };
            edit.Click += delegate { EditReward(reward); };
            remove.Click += delegate
            {
                if (MessageBox.Show(
                    this,
                    "删除奖励“" + reward.Title + "”吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _settings.Rewards.Remove(reward);
                    RaiseSettingsApplied();
                    ShowPage("rewards");
                }
            };
            actions.Children.Add(favorite);
            actions.Children.Add(redeem);
            actions.Children.Add(edit);
            actions.Children.Add(remove);
            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);
            card.Child = grid;
            return card;
        }

        private FrameworkElement BuildPet()
        {
            StackPanel page = new StackPanel();
            _petVisible = ToggleCard(
                page,
                "显示桌宠",
                "隐藏后提醒仍会正常运行",
                _settings.PetVisible);

            Border size = Theme.CardBorder();
            Grid sizeGrid = new Grid();
            sizeGrid.ColumnDefinitions.Add(new ColumnDefinition());
            sizeGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(300)
            });
            StackPanel sizeText = new StackPanel();
            sizeText.Children.Add(Theme.Text(
                "桌宠大小",
                15,
                FontWeights.Bold,
                Theme.Ink));
            sizeText.Children.Add(Theme.Text(
                "默认是更轻巧的 70%，可按桌面空间自由调整",
                12,
                FontWeights.Normal,
                Theme.Muted));
            sizeGrid.Children.Add(sizeText);

            StackPanel sizeControl = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            _petSize = new Slider
            {
                Minimum = 50,
                Maximum = 120,
                Value = _settings.PetSizePercent,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Width = 220,
                VerticalAlignment = VerticalAlignment.Center
            };
            _petSizeValue = Theme.Text(
                ((int)_petSize.Value) + "%",
                13,
                FontWeights.Bold,
                Theme.MintDark);
            _petSizeValue.Width = 52;
            _petSizeValue.TextAlignment = TextAlignment.Right;
            _petSize.ValueChanged += delegate
            {
                int value = (int)Math.Round(_petSize.Value);
                _petSizeValue.Text = value + "%";
                _settings.PetSizePercent = value;
                RaiseSettingsApplied();
            };
            sizeControl.Children.Add(_petSize);
            sizeControl.Children.Add(_petSizeValue);
            Grid.SetColumn(sizeControl, 1);
            sizeGrid.Children.Add(sizeControl);
            size.Child = sizeGrid;
            page.Children.Add(size);

            Border interaction = Theme.CardBorder();
            Grid interactionGrid = new Grid();
            interactionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            interactionGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(280)
            });
            StackPanel interactionText = new StackPanel();
            interactionText.Children.Add(Theme.Text(
                "互动频率",
                15,
                FontWeights.Bold,
                Theme.Ink));
            interactionText.Children.Add(Theme.Text(
                "控制自主散步、发呆和主动回应的活跃程度",
                12,
                FontWeights.Normal,
                Theme.Muted));
            interactionGrid.Children.Add(interactionText);
            _petInteraction = new ComboBox
            {
                Width = 235,
                Padding = new Thickness(9, 6, 9, 6),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddInteractionOption(
                "安静待机",
                PetInteractionLevel.Quiet);
            AddInteractionOption(
                "正常活动（推荐）",
                PetInteractionLevel.Normal);
            AddInteractionOption(
                "活泼捣乱",
                PetInteractionLevel.Playful);
            _petInteraction.SelectionChanged += delegate
            {
                ComboBoxItem selected =
                    _petInteraction.SelectedItem as ComboBoxItem;
                if (selected != null)
                {
                    _settings.PetInteractionLevel =
                        (PetInteractionLevel)selected.Tag;
                    RaiseSettingsApplied();
                }
            };
            Grid.SetColumn(_petInteraction, 1);
            interactionGrid.Children.Add(_petInteraction);
            interaction.Child = interactionGrid;
            page.Children.Add(interaction);

            Border outfit = Theme.CardBorder();
            Grid outfitGrid = new Grid();
            outfitGrid.ColumnDefinitions.Add(new ColumnDefinition());
            outfitGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(280)
            });
            StackPanel outfitText = new StackPanel();
            outfitText.Children.Add(Theme.Text(
                "成长点装扮",
                15,
                FontWeights.Bold,
                Theme.Ink));
            PetOutfitOption nextOutfit =
                PetOutfitService.Options.FirstOrDefault(
                    x => x.RequiredGrowthPoints >
                        _settings.Stats.GrowthPoints);
            outfitText.Children.Add(Theme.Text(
                nextOutfit == null
                    ? "所有装扮都已解锁，成长点不会被消耗"
                    : "下一件：" + nextOutfit.Name + " · " +
                        nextOutfit.RequiredGrowthPoints + " 成长点",
                12,
                FontWeights.Normal,
                Theme.Muted));
            outfitGrid.Children.Add(outfitText);
            _petOutfit = new ComboBox
            {
                Width = 235,
                Padding = new Thickness(9, 6, 9, 6),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink,
                ItemsSource = PetOutfitService.Options.Where(
                    x => PetOutfitService.IsUnlocked(
                        x.Outfit,
                        _settings.Stats.GrowthPoints)).ToList(),
                DisplayMemberPath = "Name",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            _petOutfit.SelectedItem =
                ((System.Collections.Generic.List<PetOutfitOption>)
                    _petOutfit.ItemsSource).Find(
                        x => x.Outfit ==
                            _settings.SelectedPetOutfit);
            _petOutfit.SelectionChanged += delegate
            {
                PetOutfitOption selected =
                    _petOutfit.SelectedItem as PetOutfitOption;
                if (selected != null)
                {
                    _settings.SelectedPetOutfit = selected.Outfit;
                    RaiseSettingsApplied();
                }
            };
            Grid.SetColumn(_petOutfit, 1);
            outfitGrid.Children.Add(_petOutfit);
            outfit.Child = outfitGrid;
            page.Children.Add(outfit);

            _limitedTerritory = ToggleCard(
                page,
                "限定活动范围",
                "关闭后，小猫可以在当前桌面范围内散步",
                _settings.LimitedTerritory);
            _energySaver = ToggleCard(
                page,
                "性能模式",
                "使用电池时自动降低动画帧率",
                _settings.EnergySaver);
            _fullScreen = ToggleCard(
                page,
                "全屏保护",
                "全屏游戏、视频或演示时自动隐藏",
                _settings.AutoHideFullScreen);
            _meetings = ToggleCard(
                page,
                "会议保护",
                "腾讯会议、飞书会议等处于前台时自动隐藏",
                _settings.AutoHideMeetings);
            _manualMeeting = ToggleCard(
                page,
                "手动会议模式",
                "无法自动识别时，可以暂时强制隐藏",
                _settings.ManualMeetingMode);
            _eveningRitual = ToggleCard(
                page,
                "晚间收尾入口",
                "临近回宿舍时间时允许小猫背上书包；不会额外弹窗催促",
                _settings.EveningRitualEnabled);

            Border reset = Theme.CardBorder();
            Grid resetGrid = new Grid();
            resetGrid.ColumnDefinitions.Add(new ColumnDefinition());
            resetGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel resetText = new StackPanel();
            resetText.Children.Add(Theme.Text(
                "桌宠位置",
                15,
                FontWeights.Bold,
                Theme.Ink));
            resetText.Children.Add(Theme.Text(
                "拖动后会吸附边缘；扩展屏变化后会自动找回可见区域",
                12,
                FontWeights.Normal,
                Theme.Muted));
            resetGrid.Children.Add(resetText);
            WrapPanel positionActions = new WrapPanel();
            Button resetButton = Theme.Button("移到右下角", false);
            resetButton.Click += delegate
            {
                Action handler = ResetPetRequested;
                if (handler != null)
                {
                    handler();
                }
            };
            Button findButton = Theme.Button("找回所有窗口", true);
            findButton.Click += delegate
            {
                Action handler = FindWindowsRequested;
                if (handler != null)
                {
                    handler();
                }
            };
            positionActions.Children.Add(resetButton);
            positionActions.Children.Add(findButton);
            Grid.SetColumn(positionActions, 1);
            resetGrid.Children.Add(positionActions);
            reset.Child = resetGrid;
            page.Children.Add(reset);
            return page;
        }

        private FrameworkElement BuildOther()
        {
            StackPanel page = new StackPanel();
            _autoStart = ToggleCard(
                page,
                "开机自动启动",
                "启用后会将程序安放到当前用户的本地程序目录",
                _settings.AutoStart);
            _warmMessages = ToggleCard(
                page,
                "显示暖心补充语",
                "提醒标题始终只说明事件，补充语可以关闭",
                _settings.WarmMessages);

            _pendingHotKeyModifiers = _settings.HotKeyModifiers;
            _pendingHotKeyVirtualKey = _settings.HotKeyVirtualKey;
            Border shortcut = Theme.CardBorder();
            Grid shortcutGrid = new Grid();
            shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition());
            shortcutGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(330)
            });
            StackPanel shortcutText = new StackPanel();
            shortcutText.Children.Add(Theme.Text(
                "全局显示 / 隐藏快捷键",
                15,
                FontWeights.Bold,
                Theme.Ink));
            shortcutText.Children.Add(Theme.Text(
                "点击右侧输入框，再按下新的组合键；至少包含 Ctrl 或 Alt。",
                12,
                FontWeights.Normal,
                Theme.Muted));
            _shortcutStatus = Theme.Text(
                string.IsNullOrWhiteSpace(_runtimeShortcutStatus)
                    ? "当前请求：" + PetWindow.FormatHotKey(
                        _settings.HotKeyModifiers,
                        _settings.HotKeyVirtualKey)
                    : _runtimeShortcutStatus,
                11,
                FontWeights.SemiBold,
                Theme.MintDark);
            _shortcutStatus.Margin = new Thickness(0, 6, 0, 0);
            shortcutText.Children.Add(_shortcutStatus);
            shortcutGrid.Children.Add(shortcutText);

            StackPanel shortcutControls = new StackPanel();
            _shortcutCapture = SmallInput(PetWindow.FormatHotKey(
                _pendingHotKeyModifiers,
                _pendingHotKeyVirtualKey));
            _shortcutCapture.IsReadOnly = true;
            _shortcutCapture.Cursor = Cursors.Hand;
            _shortcutCapture.ToolTip = "点击后直接按下组合键";
            _shortcutCapture.PreviewKeyDown += CaptureShortcut;
            shortcutControls.Children.Add(_shortcutCapture);
            WrapPanel shortcutActions = new WrapPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            Button resetShortcut = Theme.Button("恢复默认", false);
            Button testShortcut = Theme.Button("保存并检测", true);
            resetShortcut.Padding = testShortcut.Padding =
                new Thickness(10, 6, 10, 6);
            resetShortcut.Click += delegate
            {
                _pendingHotKeyModifiers = 3;
                _pendingHotKeyVirtualKey = 0x4D;
                _shortcutCapture.Text = "Ctrl + Alt + M";
            };
            testShortcut.Click += delegate
            {
                SaveAll(false);
                MessageBox.Show(
                    this,
                    string.IsNullOrWhiteSpace(_runtimeShortcutStatus)
                        ? "快捷键设置已提交。"
                        : _runtimeShortcutStatus,
                    "快捷键检测",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            shortcutActions.Children.Add(resetShortcut);
            shortcutActions.Children.Add(testShortcut);
            shortcutControls.Children.Add(shortcutActions);
            Grid.SetColumn(shortcutControls, 1);
            shortcutGrid.Children.Add(shortcutControls);
            shortcut.Child = shortcutGrid;
            page.Children.Add(shortcut);

            Border idle = Theme.CardBorder();
            Grid idleGrid = new Grid();
            idleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            idleGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(100)
            });
            StackPanel idleText = new StackPanel();
            idleText.Children.Add(Theme.Text(
                "判断离开电脑",
                15,
                FontWeights.Bold,
                Theme.Ink));
            idleText.Children.Add(Theme.Text(
                "连续没有键盘和鼠标操作达到这个时间，视为已经离开",
                12,
                FontWeights.Normal,
                Theme.Muted));
            idleGrid.Children.Add(idleText);
            StackPanel idleInput = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            _idleThreshold =
                SmallInput(_settings.IdleThresholdMinutes.ToString());
            _idleThreshold.Width = 54;
            idleInput.Children.Add(_idleThreshold);
            TextBlock minutes = Theme.Text(
                "分钟",
                12,
                FontWeights.Normal,
                Theme.Muted);
            minutes.Margin = new Thickness(8, 0, 0, 0);
            idleInput.Children.Add(minutes);
            Grid.SetColumn(idleInput, 1);
            idleGrid.Children.Add(idleInput);
            idle.Child = idleGrid;
            page.Children.Add(idle);

            Border data = Theme.CardBorder();
            Grid dataGrid = new Grid();
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition());
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel dataText = new StackPanel();
            dataText.Children.Add(Theme.Text(
                "本地数据",
                15,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock path = Theme.Text(
                _dataFolder,
                11,
                FontWeights.Normal,
                Theme.Muted);
            path.TextTrimming = TextTrimming.CharacterEllipsis;
            dataText.Children.Add(path);
            dataGrid.Children.Add(dataText);
            Button open = Theme.Button("打开文件夹", false);
            open.Click += delegate
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_dataFolder);
                    Process.Start("explorer.exe", _dataFolder);
                }
                catch
                {
                }
            };
            Grid.SetColumn(open, 1);
            dataGrid.Children.Add(open);
            data.Child = dataGrid;
            page.Children.Add(data);
            return page;
        }

        private Border IntervalCard(
            string title,
            string subtitle,
            CheckBox enabled,
            TextBox input,
            string suffix)
        {
            Border card = Theme.CardBorder();
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(150)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(70)
            });

            StackPanel text = new StackPanel();
            text.Children.Add(Theme.Text(
                title,
                16,
                FontWeights.Bold,
                Theme.Ink));
            text.Children.Add(Theme.Text(
                subtitle,
                12,
                FontWeights.Normal,
                Theme.Muted));
            grid.Children.Add(text);

            StackPanel value = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            input.Width = 68;
            value.Children.Add(input);
            TextBlock label = Theme.Text(
                suffix,
                12,
                FontWeights.Normal,
                Theme.Muted);
            label.Margin = new Thickness(8, 0, 0, 0);
            value.Children.Add(label);
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
            enabled.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(enabled, 2);
            grid.Children.Add(enabled);
            card.Child = grid;
            return card;
        }

        private CheckBox ToggleCard(
            Panel page,
            string title,
            string subtitle,
            bool value)
        {
            Border card = Theme.CardBorder();
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(80)
            });
            StackPanel text = new StackPanel();
            text.Children.Add(Theme.Text(
                title,
                15,
                FontWeights.Bold,
                Theme.Ink));
            text.Children.Add(Theme.Text(
                subtitle,
                12,
                FontWeights.Normal,
                Theme.Muted));
            grid.Children.Add(text);
            CheckBox toggle = new CheckBox
            {
                Content = "启用",
                IsChecked = value,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Theme.Ink
            };
            Grid.SetColumn(toggle, 1);
            grid.Children.Add(toggle);
            card.Child = grid;
            page.Children.Add(card);
            return toggle;
        }

        private FrameworkElement ScheduleRow(ScheduleEntry entry, bool dorm)
        {
            Border row = new Border
            {
                Background = Theme.Ivory,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(13, 10, 13, 10),
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(78)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel info = new StackPanel();
            info.Children.Add(Theme.Text(
                entry.Label,
                13,
                FontWeights.SemiBold,
                Theme.Ink));
            info.Children.Add(Theme.Text(
                DaysText(entry.DaysMask),
                11,
                FontWeights.Normal,
                Theme.Muted));
            grid.Children.Add(info);
            TextBlock time = Theme.Text(
                entry.TimeText,
                14,
                FontWeights.Bold,
                Theme.MintDark);
            Grid.SetColumn(time, 1);
            grid.Children.Add(time);
            WrapPanel actions = new WrapPanel();
            Button edit = Theme.Button("编辑", false);
            Button remove = Theme.Button("删除", false);
            edit.Padding = new Thickness(10, 5, 10, 5);
            edit.MinWidth = 52;
            remove.Padding = new Thickness(10, 5, 10, 5);
            remove.MinWidth = 52;
            edit.Click += delegate { EditMealSchedule(entry); };
            remove.Click += delegate
            {
                _settings.MealSchedules.Remove(entry);
                RaiseSettingsApplied();
                ShowPage("reminders");
            };
            actions.Children.Add(edit);
            actions.Children.Add(remove);
            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);
            row.Child = grid;
            return row;
        }

        private FrameworkElement TodoRow(TodoItem item)
        {
            Border row = Theme.CardBorder();
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            StackPanel info = new StackPanel();
            TextBlock title = Theme.Text(
                item.Title,
                15,
                FontWeights.Bold,
                Theme.Ink);
            info.Children.Add(title);
            TodoCategory category = _settings.FindCategory(item.CategoryId);
            string categoryName = category == null
                ? "未分类"
                : category.Name;
            TextBlock metadata = Theme.Text(
                categoryName + " · " +
                DifficultyText(item.Difficulty) + " · 完成 +" +
                item.DifficultyPoints + " 点",
                11,
                FontWeights.SemiBold,
                Theme.MintDark);
            metadata.Margin = new Thickness(0, 4, 0, 0);
            info.Children.Add(metadata);
            string due = item.DueLocal.ToString("yyyy-MM-dd HH:mm");
            if (item.Repeat != TodoRepeat.None)
            {
                due += " · " + RepeatText(item.Repeat);
            }
            TextBlock dueText = Theme.Text(
                due,
                11,
                FontWeights.Normal,
                item.DueLocal < DateTime.Now ? Theme.Peach : Theme.Muted);
            dueText.Margin = new Thickness(0, 4, 0, 0);
            info.Children.Add(dueText);
            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                TextBlock note = Theme.Text(
                    item.Note,
                    11,
                    FontWeights.Normal,
                    Theme.Muted);
                note.Margin = new Thickness(0, 4, 0, 0);
                info.Children.Add(note);
            }
            grid.Children.Add(info);

            WrapPanel actions = new WrapPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Button done = Theme.Button("完成", true);
            Button edit = Theme.Button("编辑", false);
            Button remove = Theme.Button("删除", false);
            done.Padding = edit.Padding = remove.Padding =
                new Thickness(10, 6, 10, 6);
            done.MinWidth = edit.MinWidth = remove.MinWidth = 54;
            done.Click += delegate
            {
                item.AdvanceAfterCompletion();
                _settings.Stats.EnsureToday();
                _settings.Stats.TodoCount++;
                ProgressService.Award(
                    _settings,
                    ReminderKind.Todo,
                    item.Difficulty);
                RaiseSettingsApplied();
                ShowPage("todos");
            };
            edit.Click += delegate { EditTodo(item); };
            remove.Click += delegate
            {
                if (MessageBox.Show(
                    this,
                    "删除这条待办吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _settings.Todos.Remove(item);
                    RaiseSettingsApplied();
                    ShowPage("todos");
                }
            };
            actions.Children.Add(done);
            actions.Children.Add(edit);
            actions.Children.Add(remove);
            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);
            row.Child = grid;
            return row;
        }

        private Border StatCard(string label, int count, Brush accent)
        {
            Border card = new Border
            {
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 10, 14)
            };
            StackPanel content = new StackPanel();
            Border dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = accent,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            content.Children.Add(dot);
            content.Children.Add(Theme.Text(
                count.ToString(),
                24,
                FontWeights.Bold,
                Theme.Ink));
            content.Children.Add(Theme.Text(
                label,
                11,
                FontWeights.Normal,
                Theme.Muted));
            card.Child = content;
            return card;
        }

        private void AddMealSchedule()
        {
            ScheduleEditorWindow dialog = new ScheduleEditorWindow(null);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _settings.MealSchedules.Add(dialog.ResultEntry);
                RaiseSettingsApplied();
                ShowPage("reminders");
            }
        }

        private void EditMealSchedule(ScheduleEntry entry)
        {
            ScheduleEditorWindow dialog = new ScheduleEditorWindow(entry);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                RaiseSettingsApplied();
                ShowPage("reminders");
            }
        }

        private void AddTodo()
        {
            TodoEditorWindow dialog =
                new TodoEditorWindow(null, _settings.TodoCategories);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _settings.Todos.Add(dialog.ResultItem);
                RaiseSettingsApplied();
                ShowPage("todos");
            }
        }

        private void EditTodo(TodoItem item)
        {
            TodoEditorWindow dialog =
                new TodoEditorWindow(item, _settings.TodoCategories);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                RaiseSettingsApplied();
                ShowPage("todos");
            }
        }

        private void ManageTodoCategories()
        {
            CategoryManagerWindow dialog =
                new CategoryManagerWindow(_settings);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                RaiseSettingsApplied();
                ShowPage("todos");
            }
        }

        private void CompleteDailyCare(DailyCareKind kind)
        {
            _settings.Stats.EnsureToday();
            bool wasComplete = _settings.Stats.AllCareCompleted;
            _settings.Stats.CompleteCare(kind);
            RaiseSettingsApplied();
            if (!wasComplete && _settings.Stats.AllCareCompleted)
            {
                Action celebrate = CelebrationRequested;
                if (celebrate != null)
                {
                    celebrate();
                }
            }
            ShowPage("rewards");
        }

        private void AddReward()
        {
            RewardEditorWindow dialog = new RewardEditorWindow(null);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _settings.Rewards.Add(dialog.ResultItem);
                RaiseSettingsApplied();
                ShowPage("rewards");
            }
        }

        private void EditReward(RewardItem reward)
        {
            RewardEditorWindow dialog = new RewardEditorWindow(reward);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                RaiseSettingsApplied();
                ShowPage("rewards");
            }
        }

        private void PickRandomReward()
        {
            if (!_settings.Stats.AllCareCompleted)
            {
                MessageBox.Show(
                    this,
                    "先把今天的四枚猫爪点亮，再让小猫替你抽一个奖励。",
                    "先照顾自己",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            RewardItem[] eligible = _settings.Rewards
                .Where(x => !x.IsFree &&
                    x.FreedomCost <= _settings.FreedomPoints)
                .ToArray();
            if (eligible.Length == 0)
            {
                MessageBox.Show(
                    this,
                    "当前还没有可兑换的奖励。先攒一点自由度，或者新增一个轻量奖励吧。",
                    "歇一喵",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            RewardItem picked = eligible[_random.Next(eligible.Length)];
            if (MessageBox.Show(
                this,
                "小猫抽到了：\n\n" + picked.Title +
                "\n需要 " + picked.FreedomCost +
                " 自由度。\n\n现在兑换吗？",
                "随机奖励",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RedeemReward(picked);
            }
        }

        private void RedeemReward(RewardItem reward)
        {
            if (MessageBox.Show(
                this,
                reward.IsFree
                    ? "免费使用“" + reward.Title + "”吗？"
                    : "使用 " + reward.FreedomCost +
                        " 自由度兑换“" + reward.Title + "”吗？",
                reward.IsFree ? "照顾自己" : "确认兑换",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            string error;
            if (!ProgressService.TryRedeem(
                _settings,
                reward,
                out error))
            {
                MessageBox.Show(
                    this,
                    error,
                    "暂时不能兑换",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            RaiseSettingsApplied();
            MessageBox.Show(
                this,
                (reward.IsFree
                    ? "这份照顾已经留给你：\n\n"
                    : "兑换成功。现在可以安心去做：\n\n") +
                reward.Title,
                reward.IsFree
                    ? "小猫把它放进了今天"
                    : "小猫批准了这段自由时间",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            if (reward.CaresForOthers)
            {
                MessageBox.Show(
                    this,
                    "照顾朋友当然很好。\n\n" +
                    "小猫只想轻轻问一句：也要不要给自己留一份？\n" +
                    "不用现在回答，也不会记录花费。",
                    "也给自己留一份",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            if (reward.DurationMinutes > 0 &&
                MessageBox.Show(
                    this,
                    "要打开一个安静的倒计时吗？",
                    "自由时间",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RewardTimerWindow timer = new RewardTimerWindow(
                    reward.Title,
                    reward.DurationMinutes);
                timer.Show();
                DisplayTopology topology = DisplayManager.Capture();
                DisplayManager.CenterWindow(
                    timer,
                    topology.FindForWindow(
                        DisplayManager.GetWindowBounds(this)));
            }
            ShowPage("rewards");
        }

        private static DateTime StartOfWeek(DateTime value)
        {
            int day = ((int)value.DayOfWeek + 6) % 7;
            return value.Date.AddDays(-day);
        }

        private static string FriendlyDuration(long minutes)
        {
            if (minutes <= 0)
            {
                return "0 分钟";
            }
            if (minutes < 60)
            {
                return minutes + " 分钟";
            }
            long hours = minutes / 60;
            long remainder = minutes % 60;
            return remainder == 0
                ? hours + " 小时"
                : hours + " 小时 " + remainder + " 分钟";
        }

        private static string RewardCategoryText(RewardCategory category)
        {
            if (category == RewardCategory.Venting)
            {
                return "课题组吐槽局";
            }
            if (category == RewardCategory.Food)
            {
                return "吃点真正想吃的";
            }
            if (category == RewardCategory.SelfFirst)
            {
                return "今天先考虑自己";
            }
            if (category == RewardCategory.Comfort)
            {
                return "舒服一点";
            }
            if (category == RewardCategory.Rest)
            {
                return "休息";
            }
            if (category == RewardCategory.Entertainment)
            {
                return "轻松娱乐";
            }
            if (category == RewardCategory.Outdoors)
            {
                return "户外活动";
            }
            if (category == RewardCategory.Social)
            {
                return "社交";
            }
            if (category == RewardCategory.Hobby)
            {
                return "兴趣创作";
            }
            if (category == RewardCategory.Personal)
            {
                return "个人奖励";
            }
            return "自定义";
        }

        private static string PaidCategorySubtitle(
            RewardCategory category)
        {
            if (category == RewardCategory.Venting)
            {
                return "把荒谬变成素材，把情绪放到可信的关系里。";
            }
            if (category == RewardCategory.Food)
            {
                return "水果、咖啡、外卖和真正想吃的一顿饭；程序不记录花费。";
            }
            if (category == RewardCategory.Social)
            {
                return "和喜欢的人分享一点不需要产出的时间。";
            }
            if (category == RewardCategory.Entertainment)
            {
                return "游戏、电影等可以保留可选时长。";
            }
            if (category == RewardCategory.Rest)
            {
                return "休息类没有时长建议，什么时候结束由你决定。";
            }
            if (category == RewardCategory.Custom)
            {
                return "由你自己定义真正想兑换的事情。";
            }
            return "按自己的兴趣安排这一份自由。";
        }

        private void CaptureShortcut(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                e.Handled = true;
                return;
            }
            ModifierKeys pressed = Keyboard.Modifiers;
            int modifiers = 0;
            if ((pressed & ModifierKeys.Alt) != 0)
            {
                modifiers |= 1;
            }
            if ((pressed & ModifierKeys.Control) != 0)
            {
                modifiers |= 2;
            }
            if ((pressed & ModifierKeys.Shift) != 0)
            {
                modifiers |= 4;
            }
            if ((modifiers & 3) == 0)
            {
                _shortcutStatus.Text =
                    "组合键至少需要包含 Ctrl 或 Alt。";
                e.Handled = true;
                return;
            }
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey <= 0)
            {
                _shortcutStatus.Text = "这个按键不能注册为全局快捷键。";
                e.Handled = true;
                return;
            }
            _pendingHotKeyModifiers = modifiers;
            _pendingHotKeyVirtualKey = virtualKey;
            _shortcutCapture.Text = PetWindow.FormatHotKey(
                modifiers,
                virtualKey);
            _shortcutStatus.Text =
                "待保存：" + _shortcutCapture.Text;
            e.Handled = true;
        }

        private void SaveAll(bool showConfirmation)
        {
            if (_drinkEnabled != null)
            {
                _settings.DrinkEnabled = _drinkEnabled.IsChecked == true;
                _settings.DrinkIntervalMinutes =
                    ParseInt(_drinkInterval.Text, 5, 240, 45);
            }
            if (_movementEnabled != null)
            {
                _settings.MovementEnabled =
                    _movementEnabled.IsChecked == true;
                _settings.MovementIntervalMinutes =
                    ParseInt(_movementInterval.Text, 5, 240, 60);
            }
            if (_dormEnabled != null)
            {
                TimeSpan dormTime;
                if (TimeSpan.TryParse(_dormTime.Text, out dormTime) &&
                    dormTime.TotalHours < 24)
                {
                    _settings.DormSchedule.TimeText =
                        dormTime.ToString(@"hh\:mm");
                }
                _settings.DormSchedule.Enabled =
                    _dormEnabled.IsChecked == true;
            }
            if (_petVisible != null)
            {
                _settings.PetVisible = _petVisible.IsChecked == true;
                _settings.PetSizePercent =
                    (int)Math.Round(_petSize.Value);
                _settings.LimitedTerritory =
                    _limitedTerritory.IsChecked == true;
                _settings.EnergySaver = _energySaver.IsChecked == true;
                _settings.AutoHideFullScreen =
                    _fullScreen.IsChecked == true;
                _settings.AutoHideMeetings = _meetings.IsChecked == true;
                _settings.ManualMeetingMode =
                    _manualMeeting.IsChecked == true;
                _settings.EveningRitualEnabled =
                    _eveningRitual.IsChecked == true;
                ComboBoxItem interaction =
                    _petInteraction.SelectedItem as ComboBoxItem;
                if (interaction != null)
                {
                    _settings.PetInteractionLevel =
                        (PetInteractionLevel)interaction.Tag;
                }
                PetOutfitOption outfit =
                    _petOutfit.SelectedItem as PetOutfitOption;
                if (outfit != null)
                {
                    _settings.SelectedPetOutfit = outfit.Outfit;
                }
            }
            if (_autoStart != null)
            {
                _settings.AutoStart = _autoStart.IsChecked == true;
                _settings.WarmMessages =
                    _warmMessages.IsChecked == true;
                _settings.IdleThresholdMinutes =
                    ParseInt(_idleThreshold.Text, 2, 60, 5);
            }
            if (_shortcutCapture != null)
            {
                _settings.HotKeyModifiers =
                    _pendingHotKeyModifiers;
                _settings.HotKeyVirtualKey =
                    _pendingHotKeyVirtualKey;
            }

            RaiseSettingsApplied();
            if (showConfirmation)
            {
                MessageBox.Show(
                    this,
                    "设置已保存。",
                    "歇一喵",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void RaiseSettingsApplied()
        {
            Action handler = SettingsApplied;
            if (handler != null)
            {
                handler();
            }
        }

        private static TextBox SmallInput(string value)
        {
            return new TextBox
            {
                Text = value,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                Padding = new Thickness(9, 6, 9, 6),
                BorderBrush = Theme.Line,
                Background = Theme.Ivory,
                Foreground = Theme.Ink,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private void AddInteractionOption(
            string text,
            PetInteractionLevel value)
        {
            ComboBoxItem item = new ComboBoxItem
            {
                Content = text,
                Tag = value
            };
            _petInteraction.Items.Add(item);
            if (value == _settings.PetInteractionLevel)
            {
                _petInteraction.SelectedItem = item;
            }
        }

        private static int ParseInt(
            string value,
            int min,
            int max,
            int fallback)
        {
            int parsed;
            if (!int.TryParse(value, out parsed))
            {
                return fallback;
            }
            return Math.Max(min, Math.Min(max, parsed));
        }

        private static string DaysText(int mask)
        {
            if (mask == 127)
            {
                return "每天";
            }
            if (mask == 31)
            {
                return "工作日";
            }
            string[] names = { "一", "二", "三", "四", "五", "六", "日" };
            string value = "";
            for (int i = 0; i < names.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    value += (value.Length == 0 ? "周" : "、") + names[i];
                }
            }
            return value;
        }

        private static string RepeatText(TodoRepeat repeat)
        {
            if (repeat == TodoRepeat.Daily)
            {
                return "每天";
            }
            if (repeat == TodoRepeat.Weekly)
            {
                return "每周";
            }
            if (repeat == TodoRepeat.Weekdays)
            {
                return "工作日";
            }
            return "不重复";
        }

        private static string DifficultyText(TodoDifficulty difficulty)
        {
            if (difficulty == TodoDifficulty.Important)
            {
                return "重要";
            }
            return difficulty == TodoDifficulty.Normal ? "普通" : "简单";
        }
    }
}
