using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace XieYiMiao
{
    public sealed class ScheduleEditorWindow : Window
    {
        private readonly TextBox _label;
        private readonly TextBox _time;
        private readonly CheckBox[] _days = new CheckBox[7];
        private readonly ScheduleEntry _source;

        public ScheduleEntry ResultEntry { get; private set; }

        public ScheduleEditorWindow(ScheduleEntry source)
        {
            _source = source;
            Title = source == null ? "添加吃饭时间" : "编辑提醒时间";
            Width = 470;
            Height = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            StackPanel root = new StackPanel
            {
                Margin = new Thickness(28)
            };
            root.Children.Add(Theme.Text(
                source == null ? "添加吃饭时间" : "编辑提醒时间",
                23,
                FontWeights.Bold,
                Theme.Ink));
            root.Children.Add(Spacer(16));
            root.Children.Add(Label("名称"));
            _label = Input();
            _label.Text = source == null ? "吃饭" : source.Label;
            root.Children.Add(_label);
            root.Children.Add(Spacer(12));
            root.Children.Add(Label("时间（24小时制）"));
            _time = Input();
            _time.Text = source == null ? "12:00" : source.TimeText;
            root.Children.Add(_time);
            root.Children.Add(Spacer(12));
            root.Children.Add(Label("重复"));

            WrapPanel dayPanel = new WrapPanel();
            string[] names = { "一", "二", "三", "四", "五", "六", "日" };
            int mask = source == null ? 127 : source.DaysMask;
            for (int i = 0; i < names.Length; i++)
            {
                _days[i] = new CheckBox
                {
                    Content = names[i],
                    IsChecked = (mask & (1 << i)) != 0,
                    Margin = new Thickness(0, 6, 14, 6),
                    Foreground = Theme.Ink
                };
                dayPanel.Children.Add(_days[i]);
            }
            root.Children.Add(dayPanel);

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 22, 0, 0)
            };
            Button cancel = Theme.Button("取消", false);
            Button save = Theme.Button("保存", true);
            cancel.Click += delegate { DialogResult = false; };
            save.Click += OnSave;
            actions.Children.Add(cancel);
            actions.Children.Add(save);
            root.Children.Add(actions);
            Content = root;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            TimeSpan parsed;
            if (string.IsNullOrWhiteSpace(_label.Text) ||
                !TimeSpan.TryParse(_time.Text.Trim(), out parsed) ||
                parsed.TotalHours >= 24)
            {
                MessageBox.Show(
                    this,
                    "请填写名称，并使用类似 12:30 的时间格式。",
                    "检查一下",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            int mask = 0;
            for (int i = 0; i < _days.Length; i++)
            {
                if (_days[i].IsChecked == true)
                {
                    mask |= 1 << i;
                }
            }
            if (mask == 0)
            {
                MessageBox.Show(
                    this,
                    "请至少选择一天。",
                    "检查一下",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ResultEntry = _source ?? new ScheduleEntry();
            ResultEntry.Label = _label.Text.Trim();
            ResultEntry.TimeText = parsed.ToString(@"hh\:mm");
            ResultEntry.DaysMask = mask;
            ResultEntry.Enabled = true;
            DialogResult = true;
        }

        private static TextBlock Label(string value)
        {
            TextBlock block = Theme.Text(
                value,
                12,
                FontWeights.SemiBold,
                Theme.Muted);
            block.Margin = new Thickness(0, 0, 0, 6);
            return block;
        }

        private static TextBox Input()
        {
            return new TextBox
            {
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                Padding = new Thickness(11, 8, 11, 8),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink
            };
        }

        private static FrameworkElement Spacer(double height)
        {
            return new Border { Height = height };
        }
    }

    public sealed class TodoEditorWindow : Window
    {
        private readonly TextBox _title;
        private readonly TextBox _note;
        private readonly DatePicker _date;
        private readonly TextBox _time;
        private readonly ComboBox _repeat;
        private readonly ComboBox _category;
        private readonly ComboBox _difficulty;
        private readonly TodoItem _source;
        private readonly IList<TodoCategory> _categories;

        public TodoItem ResultItem { get; private set; }

        public TodoEditorWindow(TodoItem source)
            : this(source, AppSettings.CreateDefault().TodoCategories)
        {
        }

        public TodoEditorWindow(
            TodoItem source,
            IList<TodoCategory> categories)
        {
            _source = source;
            _categories = categories ?? new List<TodoCategory>();
            Title = source == null ? "新增待办" : "编辑待办";
            Width = 510;
            Height = 660;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            StackPanel root = new StackPanel
            {
                Margin = new Thickness(28)
            };
            root.Children.Add(Theme.Text(
                source == null ? "新增待办" : "编辑待办",
                23,
                FontWeights.Bold,
                Theme.Ink));
            root.Children.Add(Spacer(16));
            root.Children.Add(Label("待办事项"));
            _title = Input();
            _title.Text = source == null ? "" : source.Title;
            root.Children.Add(_title);
            root.Children.Add(Spacer(12));
            root.Children.Add(Label("备注（可选）"));
            _note = Input();
            _note.AcceptsReturn = true;
            _note.TextWrapping = TextWrapping.Wrap;
            _note.Height = 78;
            _note.Text = source == null ? "" : source.Note;
            root.Children.Add(_note);
            root.Children.Add(Spacer(12));

            Grid dueGrid = new Grid();
            dueGrid.ColumnDefinitions.Add(new ColumnDefinition());
            dueGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(130)
            });

            StackPanel datePanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 12, 0)
            };
            datePanel.Children.Add(Label("日期"));
            _date = new DatePicker
            {
                SelectedDate = source == null
                    ? DateTime.Today
                    : source.DueLocal.Date,
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line
            };
            datePanel.Children.Add(_date);
            dueGrid.Children.Add(datePanel);

            StackPanel timePanel = new StackPanel();
            Grid.SetColumn(timePanel, 1);
            timePanel.Children.Add(Label("时间"));
            _time = Input();
            _time.Text = (source == null
                    ? DateTime.Now.AddHours(1)
                    : source.DueLocal)
                .ToString("HH:mm");
            timePanel.Children.Add(_time);
            dueGrid.Children.Add(timePanel);
            root.Children.Add(dueGrid);
            root.Children.Add(Spacer(12));
            root.Children.Add(Label("重复"));

            _repeat = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line,
                Background = Theme.Card
            };
            _repeat.Items.Add("不重复");
            _repeat.Items.Add("每天");
            _repeat.Items.Add("每周");
            _repeat.Items.Add("工作日");
            _repeat.SelectedIndex = source == null ? 0 : (int)source.Repeat;
            root.Children.Add(_repeat);
            root.Children.Add(Spacer(12));

            Grid metadata = new Grid();
            metadata.ColumnDefinitions.Add(new ColumnDefinition());
            metadata.ColumnDefinitions.Add(new ColumnDefinition());
            StackPanel categoryPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 12, 0)
            };
            categoryPanel.Children.Add(Label("分类"));
            _category = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line,
                Background = Theme.Card
            };
            foreach (TodoCategory category in _categories
                .OrderBy(x => x.SortOrder))
            {
                _category.Items.Add(new ComboBoxItem
                {
                    Content = category.Name,
                    Tag = category.Id
                });
            }
            string selectedCategory = source == null
                ? "uncategorized"
                : source.CategoryId;
            _category.SelectedIndex = 0;
            for (int i = 0; i < _category.Items.Count; i++)
            {
                ComboBoxItem categoryItem =
                    (ComboBoxItem)_category.Items[i];
                if ((string)categoryItem.Tag == selectedCategory)
                {
                    _category.SelectedIndex = i;
                    break;
                }
            }
            categoryPanel.Children.Add(_category);
            metadata.Children.Add(categoryPanel);

            StackPanel difficultyPanel = new StackPanel();
            Grid.SetColumn(difficultyPanel, 1);
            difficultyPanel.Children.Add(Label("难度与奖励"));
            _difficulty = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line,
                Background = Theme.Card
            };
            _difficulty.Items.Add("简单 · 1 点");
            _difficulty.Items.Add("普通 · 3 点");
            _difficulty.Items.Add("重要 · 5 点");
            _difficulty.SelectedIndex = source == null
                ? (int)TodoDifficulty.Normal
                : (int)source.Difficulty;
            difficultyPanel.Children.Add(_difficulty);
            metadata.Children.Add(difficultyPanel);
            root.Children.Add(metadata);

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 24, 0, 0)
            };
            Button cancel = Theme.Button("取消", false);
            Button save = Theme.Button("保存", true);
            cancel.Click += delegate { DialogResult = false; };
            save.Click += OnSave;
            actions.Children.Add(cancel);
            actions.Children.Add(save);
            root.Children.Add(actions);
            Content = root;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            TimeSpan time;
            if (string.IsNullOrWhiteSpace(_title.Text) ||
                _date.SelectedDate == null ||
                !TimeSpan.TryParse(_time.Text.Trim(), out time) ||
                time.TotalHours >= 24)
            {
                MessageBox.Show(
                    this,
                    "请填写待办事项，并使用类似 18:30 的时间格式。",
                    "检查一下",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ResultItem = _source ?? new TodoItem();
            ResultItem.Title = _title.Text.Trim();
            ResultItem.Note = _note.Text.Trim();
            ResultItem.DueLocal = _date.SelectedDate.Value.Date.Add(time);
            ResultItem.Repeat = (TodoRepeat)_repeat.SelectedIndex;
            ComboBoxItem category =
                _category.SelectedItem as ComboBoxItem;
            ResultItem.CategoryId = category == null
                ? "uncategorized"
                : (string)category.Tag;
            ResultItem.Difficulty =
                (TodoDifficulty)Math.Max(0, _difficulty.SelectedIndex);
            ResultItem.Completed = false;
            ResultItem.SnoozedUntilLocal = AppTime.Never;
            ResultItem.LastNotifiedDueTicks = 0;
            DialogResult = true;
        }

        private static TextBlock Label(string value)
        {
            TextBlock block = Theme.Text(
                value,
                12,
                FontWeights.SemiBold,
                Theme.Muted);
            block.Margin = new Thickness(0, 0, 0, 6);
            return block;
        }

        private static TextBox Input()
        {
            return new TextBox
            {
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                Padding = new Thickness(11, 8, 11, 8),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink
            };
        }

        private static FrameworkElement Spacer(double height)
        {
            return new Border { Height = height };
        }
    }

    public sealed class CategoryManagerWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly StackPanel _rows;

        public CategoryManagerWindow(AppSettings settings)
        {
            _settings = settings;
            Title = "管理待办分类";
            Width = 560;
            Height = 610;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            Grid root = new Grid { Margin = new Thickness(26) };
            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            StackPanel heading = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            heading.Children.Add(Theme.Text(
                "待办分类",
                23,
                FontWeights.Bold,
                Theme.Ink));
            heading.Children.Add(Theme.Text(
                "可以新建、重命名和删除；删除后，原待办会归入“未分类”。",
                12,
                FontWeights.Normal,
                Theme.Muted));
            root.Children.Add(heading);

            _rows = new StackPanel();
            ScrollViewer scroll = new ScrollViewer
            {
                Content = _rows,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            WrapPanel actions = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Button add = Theme.Button("新建分类", false);
            Button done = Theme.Button("完成", true);
            add.Click += delegate { AddCategory(); };
            done.Click += delegate { DialogResult = true; };
            actions.Children.Add(add);
            actions.Children.Add(done);
            Grid.SetRow(actions, 2);
            root.Children.Add(actions);
            Content = root;
            RebuildRows();
        }

        private void RebuildRows()
        {
            _rows.Children.Clear();
            foreach (TodoCategory category in _settings.TodoCategories
                .OrderBy(x => x.SortOrder)
                .ToArray())
            {
                Border row = Theme.CardBorder();
                row.Padding = new Thickness(15, 12, 15, 12);
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(28)
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto
                });
                Border dot = new Border
                {
                    Width = 12,
                    Height = 12,
                    CornerRadius = new CornerRadius(6),
                    Background = SafeBrush(category.ColorHex),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                grid.Children.Add(dot);
                TextBlock name = Theme.Text(
                    category.Name,
                    14,
                    FontWeights.SemiBold,
                    Theme.Ink);
                Grid.SetColumn(name, 1);
                grid.Children.Add(name);

                WrapPanel rowActions = new WrapPanel();
                Button rename = Theme.Button("重命名", false);
                rename.Padding = new Thickness(10, 6, 10, 6);
                rename.MinWidth = 62;
                rename.Click += delegate
                {
                    EditCategory(category);
                };
                rowActions.Children.Add(rename);
                if (!category.IsFallback)
                {
                    Button remove = Theme.Button("删除", false);
                    remove.Padding = new Thickness(10, 6, 10, 6);
                    remove.MinWidth = 52;
                    remove.Click += delegate
                    {
                        if (MessageBox.Show(
                            this,
                            "删除“" + category.Name +
                            "”吗？相关待办会移到“未分类”。",
                            "确认删除",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            _settings.DeleteCategory(category.Id);
                            RebuildRows();
                        }
                    };
                    rowActions.Children.Add(remove);
                }
                Grid.SetColumn(rowActions, 2);
                grid.Children.Add(rowActions);
                row.Child = grid;
                _rows.Children.Add(row);
            }
        }

        private void AddCategory()
        {
            CategoryEditorWindow dialog = new CategoryEditorWindow(null);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                int order = _settings.TodoCategories
                    .Where(x => !x.IsFallback)
                    .Select(x => x.SortOrder)
                    .DefaultIfEmpty(-1)
                    .Max() + 1;
                _settings.TodoCategories.Add(new TodoCategory
                {
                    Name = dialog.CategoryName,
                    ColorHex = dialog.ColorHex,
                    SortOrder = order
                });
                RebuildRows();
            }
        }

        private void EditCategory(TodoCategory category)
        {
            CategoryEditorWindow dialog =
                new CategoryEditorWindow(category);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                category.Name = dialog.CategoryName;
                category.ColorHex = dialog.ColorHex;
                RebuildRows();
            }
        }

        private static Brush SafeBrush(string color)
        {
            try
            {
                return Theme.Brush(color);
            }
            catch
            {
                return Theme.Mint;
            }
        }
    }

    public sealed class CategoryEditorWindow : Window
    {
        private readonly TextBox _name;
        private readonly ComboBox _color;
        private static readonly string[] Colors =
        {
            "#79BDE3",
            "#8CCEB5",
            "#F2A15F",
            "#F5B58F",
            "#73B99A",
            "#B597C7",
            "#B8AAA3"
        };

        public string CategoryName { get; private set; }
        public string ColorHex { get; private set; }

        public CategoryEditorWindow(TodoCategory source)
        {
            Title = source == null ? "新建分类" : "编辑分类";
            Width = 420;
            Height = 330;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            StackPanel root = new StackPanel { Margin = new Thickness(26) };
            root.Children.Add(Theme.Text(
                Title,
                22,
                FontWeights.Bold,
                Theme.Ink));
            root.Children.Add(new Border { Height = 18 });
            root.Children.Add(FormLabel("分类名称"));
            _name = FormInput();
            _name.Text = source == null ? "" : source.Name;
            root.Children.Add(_name);
            root.Children.Add(new Border { Height = 14 });
            root.Children.Add(FormLabel("标记颜色"));
            _color = new ComboBox
            {
                FontSize = 13,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line,
                Background = Theme.Card
            };
            for (int i = 0; i < Colors.Length; i++)
            {
                _color.Items.Add("颜色 " + (i + 1) + "  " + Colors[i]);
            }
            _color.SelectedIndex = Math.Max(
                0,
                Array.IndexOf(
                    Colors,
                    source == null ? "#8CCEB5" : source.ColorHex));
            root.Children.Add(_color);
            WrapPanel actions = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 22, 0, 0)
            };
            Button cancel = Theme.Button("取消", false);
            Button save = Theme.Button("保存", true);
            cancel.Click += delegate { DialogResult = false; };
            save.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(_name.Text))
                {
                    MessageBox.Show(
                        this,
                        "请填写分类名称。",
                        "检查一下",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
                CategoryName = _name.Text.Trim();
                ColorHex = Colors[Math.Max(0, _color.SelectedIndex)];
                DialogResult = true;
            };
            actions.Children.Add(cancel);
            actions.Children.Add(save);
            root.Children.Add(actions);
            Content = root;
        }

        private static TextBlock FormLabel(string text)
        {
            TextBlock label = Theme.Text(
                text,
                12,
                FontWeights.SemiBold,
                Theme.Muted);
            label.Margin = new Thickness(0, 0, 0, 6);
            return label;
        }

        private static TextBox FormInput()
        {
            return new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink
            };
        }
    }

    public sealed class RewardEditorWindow : Window
    {
        private readonly RewardItem _source;
        private readonly TextBox _title;
        private readonly TextBox _description;
        private readonly ComboBox _category;
        private readonly TextBox _cost;
        private readonly TextBox _duration;
        private readonly CheckBox _repeatable;

        public RewardItem ResultItem { get; private set; }

        public RewardEditorWindow(RewardItem source)
        {
            _source = source;
            Title = source == null ? "新增奖励" : "编辑奖励";
            Width = 520;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            StackPanel root = new StackPanel { Margin = new Thickness(28) };
            root.Children.Add(Theme.Text(
                Title,
                23,
                FontWeights.Bold,
                Theme.Ink));
            root.Children.Add(new Border { Height = 16 });
            root.Children.Add(EditorLabel("奖励名称"));
            _title = EditorInput(source == null ? "" : source.Title);
            root.Children.Add(_title);
            root.Children.Add(new Border { Height = 12 });
            root.Children.Add(EditorLabel("说明（可选）"));
            _description = EditorInput(
                source == null ? "" : source.Description);
            _description.AcceptsReturn = true;
            _description.TextWrapping = TextWrapping.Wrap;
            _description.Height = 76;
            root.Children.Add(_description);
            root.Children.Add(new Border { Height = 12 });
            root.Children.Add(EditorLabel("类型"));
            _category = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = Theme.Line,
                Background = Theme.Card
            };
            _category.Items.Add("休息");
            _category.Items.Add("轻松娱乐");
            _category.Items.Add("户外活动");
            _category.Items.Add("社交");
            _category.Items.Add("兴趣创作");
            _category.Items.Add("个人奖励");
            _category.Items.Add("自定义");
            _category.Items.Add("课题组吐槽局");
            _category.Items.Add("吃点真正想吃的");
            _category.Items.Add("今天先考虑自己（免费）");
            _category.Items.Add("舒服一点（免费）");
            _category.SelectedIndex = source == null
                ? (int)RewardCategory.Custom
                : (int)source.Category;
            root.Children.Add(_category);
            root.Children.Add(new Border { Height = 12 });

            Grid numbers = new Grid();
            numbers.ColumnDefinitions.Add(new ColumnDefinition());
            numbers.ColumnDefinitions.Add(new ColumnDefinition());
            StackPanel costPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 12, 0)
            };
            costPanel.Children.Add(EditorLabel("需要自由度"));
            _cost = EditorInput(
                source == null ? "10" : source.FreedomCost.ToString());
            costPanel.Children.Add(_cost);
            numbers.Children.Add(costPanel);
            StackPanel durationPanel = new StackPanel();
            Grid.SetColumn(durationPanel, 1);
            durationPanel.Children.Add(EditorLabel("时长（分钟，0 表示不限）"));
            _duration = EditorInput(
                source == null ? "0" : source.DurationMinutes.ToString());
            durationPanel.Children.Add(_duration);
            numbers.Children.Add(durationPanel);
            root.Children.Add(numbers);
            root.Children.Add(new Border { Height = 14 });
            _repeatable = new CheckBox
            {
                Content = "允许重复兑换",
                IsChecked = source == null || source.Repeatable,
                Foreground = Theme.Ink
            };
            root.Children.Add(_repeatable);

            WrapPanel actions = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 24, 0, 0)
            };
            Button cancel = Theme.Button("取消", false);
            Button save = Theme.Button("保存", true);
            cancel.Click += delegate { DialogResult = false; };
            save.Click += OnSave;
            actions.Children.Add(cancel);
            actions.Children.Add(save);
            root.Children.Add(actions);
            Content = root;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            int cost;
            int duration;
            RewardCategory selectedCategory =
                (RewardCategory)Math.Max(0, _category.SelectedIndex);
            bool isFree =
                selectedCategory == RewardCategory.SelfFirst ||
                selectedCategory == RewardCategory.Comfort;
            if (string.IsNullOrWhiteSpace(_title.Text) ||
                !int.TryParse(_cost.Text, out cost) ||
                !int.TryParse(_duration.Text, out duration) ||
                cost < (isFree ? 0 : 1) ||
                duration < 0)
            {
                MessageBox.Show(
                    this,
                    "请填写奖励名称；普通奖励自由度至少为 1，时长不能为负数。",
                    "检查一下",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            ResultItem = _source ?? new RewardItem();
            ResultItem.Title = _title.Text.Trim();
            ResultItem.Description = _description.Text.Trim();
            ResultItem.Category = selectedCategory;
            ResultItem.IsFree = isFree;
            ResultItem.FreedomCost = isFree ? 0 : cost;
            ResultItem.DurationMinutes =
                isFree || selectedCategory == RewardCategory.Rest
                    ? 0
                    : duration;
            ResultItem.Repeatable = _repeatable.IsChecked == true;
            if (_source == null)
            {
                ResultItem.BuiltIn = false;
            }
            DialogResult = true;
        }

        private static TextBlock EditorLabel(string text)
        {
            TextBlock label = Theme.Text(
                text,
                12,
                FontWeights.SemiBold,
                Theme.Muted);
            label.Margin = new Thickness(0, 0, 0, 6);
            return label;
        }

        private static TextBox EditorInput(string value)
        {
            return new TextBox
            {
                Text = value,
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                BorderBrush = Theme.Line,
                Background = Theme.Card,
                Foreground = Theme.Ink
            };
        }
    }

    public sealed class RewardTimerWindow : Window
    {
        private readonly TextBlock _remaining;
        private readonly DispatcherTimer _timer;
        private readonly DateTime _endsAt;

        public RewardTimerWindow(string title, int minutes)
        {
            Title = "自由时间 · " + title;
            Width = 390;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            Topmost = false;
            _endsAt = DateTime.Now.AddMinutes(Math.Max(1, minutes));

            StackPanel root = new StackPanel
            {
                Margin = new Thickness(28),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            root.Children.Add(new CatControl
            {
                Width = 105,
                Height = 105,
                Mood = CatMood.Happy
            });
            TextBlock reward = Theme.Text(
                title,
                15,
                FontWeights.Bold,
                Theme.Ink);
            reward.HorizontalAlignment = HorizontalAlignment.Center;
            root.Children.Add(reward);
            _remaining = Theme.Text(
                "",
                27,
                FontWeights.Bold,
                Theme.MintDark);
            _remaining.HorizontalAlignment = HorizontalAlignment.Center;
            _remaining.Margin = new Thickness(0, 8, 0, 12);
            root.Children.Add(_remaining);
            Button finish = Theme.Button("提前结束", false);
            finish.HorizontalAlignment = HorizontalAlignment.Center;
            finish.Click += delegate { Close(); };
            root.Children.Add(finish);
            Content = root;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += delegate { UpdateRemaining(); };
            Closed += delegate { _timer.Stop(); };
            UpdateRemaining();
            _timer.Start();
        }

        private void UpdateRemaining()
        {
            TimeSpan value = _endsAt - DateTime.Now;
            if (value <= TimeSpan.Zero)
            {
                _timer.Stop();
                _remaining.Text = "自由时间完成";
                return;
            }
            _remaining.Text = string.Format(
                "{0:00}:{1:00}:{2:00}",
                (int)value.TotalHours,
                value.Minutes,
                value.Seconds);
        }
    }
}
