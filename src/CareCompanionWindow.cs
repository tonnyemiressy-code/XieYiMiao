using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace XieYiMiao
{
    public sealed class CareCompanionWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly ContentControl _content;
        private readonly StackPanel _navigation;
        private readonly TextBlock _pageTitle;
        private readonly TextBlock _pageSubtitle;
        private readonly CatControl _cat;
        private readonly Random _random = new Random();
        private bool _forceClose;
        private bool _editingRitual;
        private string _currentPage = "weather";
        private string _weatherActionText = "";
        private string _worryActionText = "";
        private string _reframeSeed = "";
        private string _reframeResultText = "";
        private string _ritualStatusText = "";
        private string _closingStyle = "quiet";
        private bool _hasRitualDraft;
        private string _ritualDraftAccomplished = "";
        private string _ritualDraftDraining = "";
        private string _ritualDraftTomorrow = "";

        private TextBox _worryInput;
        private TextBox _criticismInput;
        private TextBox _factInput;
        private TextBox _friendInput;
        private TextBox _nextStepInput;
        private TextBox _consequenceInput;
        private TextBox _ritualAccomplished;
        private TextBox _ritualDraining;
        private TextBox _ritualTomorrow;

        public event Action SaveRequested;
        public event Action<CatMood, double> CatReactionRequested;

        public CareCompanionWindow(AppSettings settings)
        {
            _settings = settings;
            Title = CareContent.StationName + " · 歇一喵";
            Width = 980;
            Height = 700;
            MinWidth = 900;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Theme.Ivory;
            FontFamily = new FontFamily("Microsoft YaHei UI");

            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(230)
            });
            root.ColumnDefinitions.Add(new ColumnDefinition());

            Border side = new Border
            {
                Background = Theme.Brush("#F0F7F5"),
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            _navigation = new StackPanel
            {
                Margin = new Thickness(18, 20, 18, 18)
            };
            _navigation.Children.Add(Theme.Text(
                CareContent.StationName,
                22,
                FontWeights.Bold,
                Theme.Ink));
            TextBlock sideNote = Theme.Text(
                "不交作业，不打分，来这儿缓口气",
                11,
                FontWeights.Normal,
                Theme.Muted);
            sideNote.Margin = new Thickness(0, 3, 0, 12);
            _navigation.Children.Add(sideNote);

            Border catPlate = new Border
            {
                Height = 170,
                Background = Theme.Card,
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(24),
                Margin = new Thickness(0, 0, 0, 18)
            };
            _cat = new CatControl
            {
                Width = 158,
                Height = 158,
                Mood = CatMood.Companion,
                Outfit = _settings.SelectedPetOutfit
            };
            catPlate.Child = _cat;
            _navigation.Children.Add(catPlate);
            _navigation.Children.Add(NavButton(
                "weather",
                "今天是什么天气"));
            _navigation.Children.Add(NavButton(
                "worry",
                "先交给小猫保管"));
            _navigation.Children.Add(NavButton(
                "reframe",
                "不要急着怪自己"));
            _navigation.Children.Add(NavButton(
                "ritual",
                "把今天收起来"));

            Border promise = new Border
            {
                Background = Theme.SkySoft,
                BorderBrush = Theme.Brush("#CFE4EA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 22, 0, 0)
            };
            promise.Child = Theme.Text(
                "随时可以关掉。\n本喵不追问，也不记小账。",
                11,
                FontWeights.SemiBold,
                Theme.Muted);
            _navigation.Children.Add(promise);
            side.Child = _navigation;
            root.Children.Add(side);

            Grid main = new Grid();
            main.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(104)
            });
            main.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(main, 1);

            StackPanel heading = new StackPanel
            {
                Margin = new Thickness(34, 22, 34, 10),
                VerticalAlignment = VerticalAlignment.Center
            };
            _pageTitle = Theme.Text(
                "今天是什么天气",
                25,
                FontWeights.Bold,
                Theme.Ink);
            _pageSubtitle = Theme.Text(
                CareContent.FriendshipPositioning,
                12,
                FontWeights.Normal,
                Theme.Muted);
            _pageSubtitle.Margin = new Thickness(0, 4, 0, 0);
            heading.Children.Add(_pageTitle);
            heading.Children.Add(_pageSubtitle);
            main.Children.Add(heading);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Disabled,
                Margin = new Thickness(34, 0, 34, 28)
            };
            _content = new ContentControl();
            scroll.Content = _content;
            Grid.SetRow(scroll, 1);
            main.Children.Add(scroll);
            root.Children.Add(main);
            Content = root;

            Closing += delegate(
                object sender,
                System.ComponentModel.CancelEventArgs e)
            {
                if (!_forceClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    Hide();
                }
            };
            ShowPage("weather");
        }

        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }

        public void RefreshCurrentPage()
        {
            _cat.Outfit = _settings.SelectedPetOutfit;
            ShowPage(_currentPage);
        }

        public void ShowPage(string key)
        {
            _currentPage = key;
            UpdateNavigation();
            if (key == "weather")
            {
                _pageTitle.Text = "今天是什么天气";
                _pageSubtitle.Text =
                    "不用解释原因，挑一个最接近此刻的状态";
                _cat.Mood = CatMood.Companion;
                _content.Content = BuildWeatherPage();
            }
            else if (key == "worry")
            {
                _pageTitle.Text = "先交给小猫保管";
                _pageSubtitle.Text =
                    "写成便签，暂存、整理或直接揉掉都可以";
                _cat.Mood = CatMood.Note;
                _content.Content = BuildWorryPage();
            }
            else if (key == "reframe")
            {
                _pageTitle.Text = "不要急着怪自己";
                _pageSubtitle.Text =
                    "像朋友帮忙理线一样，把事实和批评分开";
                _cat.Mood = CatMood.Loaf;
                _content.Content = BuildReframePage();
            }
            else
            {
                _pageTitle.Text = "把今天收起来";
                _pageSubtitle.Text =
                    "朋友式收尾：不写漂亮总结，也不追加任务";
                _cat.Mood = CatMood.Backpack;
                _content.Content = BuildRitualPage();
            }
        }

        private Button NavButton(string key, string text)
        {
            Button button = new Button
            {
                Tag = key,
                Content = text,
                Height = 46,
                Margin = new Thickness(0, 0, 0, 7),
                Padding = new Thickness(13, 0, 10, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Theme.Ink,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            button.Click += delegate { ShowPage(key); };
            return button;
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
                button.Background = active
                    ? Theme.MintSoft
                    : Brushes.Transparent;
                button.Foreground = active
                    ? Theme.MintDark
                    : Theme.Ink;
            }
        }

        private FrameworkElement BuildWeatherPage()
        {
            EnsureWeatherToday();
            StackPanel page = new StackPanel();
            Border intro = PaperCard(
                "先别急着分析",
                "给此刻挑个天气就行。它不是诊断，也不是一整天必须保持的标签。",
                Theme.Mint);
            page.Children.Add(intro);

            UniformGrid weatherGrid = new UniformGrid
            {
                Columns = 3,
                Margin = new Thickness(0, 0, -10, 12)
            };
            foreach (EmotionWeatherCard card in
                     CareContent.EmotionWeatherCards)
            {
                bool selected = _settings.EmotionWeather == card.Weather;
                Button button = new Button
                {
                    Tag = card.Weather,
                    Background = selected
                        ? Theme.Brush("#EAF7F1")
                        : Theme.Card,
                    BorderBrush = selected ? Theme.Mint : Theme.Line,
                    BorderThickness = new Thickness(selected ? 2 : 1),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 10, 10),
                    MinHeight = 112,
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment =
                        HorizontalAlignment.Stretch
                };
                StackPanel text = new StackPanel();
                text.Children.Add(Theme.Text(
                    card.Symbol + " · " + card.Title,
                    16,
                    FontWeights.Bold,
                    selected ? Theme.MintDark : Theme.Ink));
                TextBlock meaning = Theme.Text(
                    card.Meaning,
                    11,
                    FontWeights.Normal,
                    Theme.Muted);
                meaning.Margin = new Thickness(0, 6, 0, 0);
                text.Children.Add(meaning);
                button.Content = text;
                button.Click += delegate(object sender, RoutedEventArgs e)
                {
                    EmotionWeather selectedWeather =
                        (EmotionWeather)((Button)sender).Tag;
                    _settings.EmotionWeather = selectedWeather;
                    _settings.EmotionWeatherDateKey =
                        DateTime.Today.ToString("yyyy-MM-dd");
                    _weatherActionText = "";
                    Save();
                    React(CatMood.Companion, 2.5);
                    ShowPage("weather");
                };
                weatherGrid.Children.Add(button);
            }
            page.Children.Add(weatherGrid);

            EmotionWeatherCard current =
                CareContent.FindWeather(_settings.EmotionWeather);
            if (current != null)
            {
                Border response = PaperCard(
                    "收到，先放这儿",
                    current.Response,
                    Theme.Sky);
                StackPanel responseContent =
                    (StackPanel)response.Child;
                WrapPanel actions = new WrapPanel
                {
                    Margin = new Thickness(0, 14, 0, 0)
                };
                actions.Children.Add(ActionButton(
                    "一起放空一下",
                    delegate
                    {
                        _weatherActionText = CareContent.RandomFrom(
                            CareContent.QuietBreakResponses,
                            _random);
                        React(CatMood.Companion, 8);
                        ShowPage("weather");
                    }));
                actions.Children.Add(ActionButton(
                    "逗我笑一下",
                    delegate
                    {
                        _weatherActionText = CareContent.RandomFrom(
                            CareContent.FunnyResponses,
                            _random);
                        React(CatMood.Happy, 3);
                        ShowPage("weather");
                    }));
                actions.Children.Add(ActionButton(
                    "来句朋友式支持",
                    delegate
                    {
                        _weatherActionText = CareContent.RandomFrom(
                            CareContent.FriendSupportResponses,
                            _random);
                        React(CatMood.Petting, 3);
                        ShowPage("weather");
                    }));
                actions.Children.Add(ActionButton(
                    "我想记张便签",
                    delegate { ShowPage("worry"); }));
                responseContent.Children.Add(actions);
                page.Children.Add(response);
            }
            if (!string.IsNullOrWhiteSpace(_weatherActionText))
            {
                page.Children.Add(MessageCard(_weatherActionText));
            }
            return page;
        }

        private FrameworkElement BuildWorryPage()
        {
            StackPanel page = new StackPanel();
            Border inputCard = PaperCard(
                "写下一句话就够了",
                "可以是一件委屈、一个烦恼，或者一句暂时不想继续占用脑内存的话。",
                Theme.Sky);
            StackPanel inputContent = (StackPanel)inputCard.Child;
            _worryInput = MultiLineInput("", 96);
            _worryInput.Margin = new Thickness(0, 14, 0, 12);
            inputContent.Children.Add(_worryInput);

            WrapPanel mainActions = new WrapPanel();
            mainActions.Children.Add(ActionButton(
                "先放在这里",
                StoreWorry));
            mainActions.Children.Add(ActionButton(
                "揉成纸团丢掉",
                DismissWorry));
            mainActions.Children.Add(ActionButton(
                "帮我分开看看",
                delegate
                {
                    if (string.IsNullOrWhiteSpace(_worryInput.Text))
                    {
                        _worryActionText =
                            "先写下一句话，本喵才知道要收哪张便签。";
                        ShowPage("worry");
                        return;
                    }
                    _reframeSeed = _worryInput.Text.Trim();
                    ShowPage("reframe");
                }));
            inputContent.Children.Add(mainActions);

            TextBlock boundaryTitle = Theme.Text(
                "如果你需要一句可以直接使用的话",
                12,
                FontWeights.Bold,
                Theme.Ink);
            boundaryTitle.Margin = new Thickness(0, 14, 0, 8);
            inputContent.Children.Add(boundaryTitle);
            WrapPanel boundaryActions = new WrapPanel();
            boundaryActions.Children.Add(ActionButton(
                "温和说法",
                delegate { SuggestBoundary("gentle"); }));
            boundaryActions.Children.Add(ActionButton(
                "直接说法",
                delegate { SuggestBoundary("neutral"); }));
            boundaryActions.Children.Add(ActionButton(
                "坚定说法",
                delegate { SuggestBoundary("firm"); }));
            inputContent.Children.Add(boundaryActions);
            page.Children.Add(inputCard);

            if (!string.IsNullOrWhiteSpace(_worryActionText))
            {
                Border message = MessageCard(_worryActionText);
                StackPanel messageContent = (StackPanel)message.Child;
                Button copy = Theme.Button("复制这句话", false);
                copy.HorizontalAlignment = HorizontalAlignment.Left;
                copy.Margin = new Thickness(0, 12, 0, 0);
                copy.Click += delegate
                {
                    CopyText(_worryActionText);
                };
                messageContent.Children.Add(copy);
                page.Children.Add(message);
            }

            TextBlock savedTitle = Theme.Text(
                "小猫正在保管的纸条 · " +
                _settings.WorryNotes.Count,
                16,
                FontWeights.Bold,
                Theme.Ink);
            savedTitle.Margin = new Thickness(0, 8, 0, 12);
            page.Children.Add(savedTitle);
            if (_settings.WorryNotes.Count == 0)
            {
                page.Children.Add(PaperCard(
                    "纸条盒现在是空的",
                    "空着也很好。这里不要求你一定要有话可说。",
                    Theme.Line));
            }
            else
            {
                foreach (WorryNote note in _settings.WorryNotes
                    .OrderByDescending(x => x.CreatedAtLocal))
                {
                    Border noteCard = Theme.CardBorder();
                    Grid grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    });
                    StackPanel noteText = new StackPanel();
                    noteText.Children.Add(Theme.Text(
                        note.Text,
                        13,
                        FontWeights.Normal,
                        Theme.Ink));
                    TextBlock time = Theme.Text(
                        note.CreatedAtLocal.ToString("MM-dd HH:mm") +
                        (note.Kind == "reframe"
                            ? " · 整理过的纸条"
                            : " · 暂存"),
                        10,
                        FontWeights.Normal,
                        Theme.Muted);
                    time.Margin = new Thickness(0, 6, 0, 0);
                    noteText.Children.Add(time);
                    grid.Children.Add(noteText);
                    Button remove = Theme.Button("取回并放下", false);
                    remove.Tag = note.Id;
                    remove.Click += delegate(object sender, RoutedEventArgs e)
                    {
                        string id = (string)((Button)sender).Tag;
                        WorryNote found =
                            _settings.WorryNotes.Find(x => x.Id == id);
                        if (found != null)
                        {
                            _settings.WorryNotes.Remove(found);
                            _worryActionText =
                                "这张纸条已经从盒子里取出来了。它不再需要被保管。";
                            Save();
                            ShowPage("worry");
                        }
                    };
                    Grid.SetColumn(remove, 1);
                    grid.Children.Add(remove);
                    noteCard.Child = grid;
                    page.Children.Add(noteCard);
                }
            }
            return page;
        }

        private FrameworkElement BuildReframePage()
        {
            StackPanel page = new StackPanel();
            page.Children.Add(PaperCard(
                "本喵不替你判案",
                "这里是朋友式理线：不假装看透复杂情绪，只把不同部分放到不同的位置。",
                Theme.Mint));

            Border form = Theme.CardBorder();
            StackPanel fields = new StackPanel();
            _criticismInput = LabeledInput(
                fields,
                "1. 我现在正在怎样批评自己？",
                _reframeSeed,
                64);
            _factInput = LabeledInput(
                fields,
                "2. 已经发生、可以确认的事实是什么？",
                "",
                64);
            _friendInput = LabeledInput(
                fields,
                "3. 如果是朋友遇到这件事，我会怎样对他说？",
                "",
                64);
            _nextStepInput = LabeledInput(
                fields,
                "4. 现在最小、最实际的一步是什么？",
                "",
                64);
            _consequenceInput = LabeledInput(
                fields,
                "5. 如果今天不处理，会有什么真实后果？",
                "",
                64);
            Button organize = Theme.Button("整理成一张纸条", true);
            organize.HorizontalAlignment = HorizontalAlignment.Left;
            organize.Margin = new Thickness(0, 6, 0, 0);
            organize.Click += delegate { BuildReframeResult(); };
            fields.Children.Add(organize);
            form.Child = fields;
            page.Children.Add(form);

            if (!string.IsNullOrWhiteSpace(_reframeResultText))
            {
                Border result = PaperCard(
                    "整理后的纸条",
                    _reframeResultText,
                    Theme.Peach);
                StackPanel resultContent = (StackPanel)result.Child;
                WrapPanel actions = new WrapPanel
                {
                    Margin = new Thickness(0, 12, 0, 0)
                };
                actions.Children.Add(ActionButton(
                    "复制纸条",
                    delegate { CopyText(_reframeResultText); }));
                actions.Children.Add(ActionButton(
                    "交给小猫保管",
                    delegate
                    {
                        _settings.WorryNotes.Add(new WorryNote
                        {
                            Text = _reframeResultText,
                            Kind = "reframe",
                            CreatedAtLocal = DateTime.Now
                        });
                        Save();
                        React(CatMood.Note, 3);
                        _worryActionText =
                            "整理后的纸条已经放进盒子里了。";
                        ShowPage("worry");
                    }));
                actions.Children.Add(ActionButton(
                    "清空",
                    delegate
                    {
                        _reframeSeed = "";
                        _reframeResultText = "";
                        ShowPage("reframe");
                    }));
                resultContent.Children.Add(actions);
                page.Children.Add(result);
            }
            return page;
        }

        private FrameworkElement BuildRitualPage()
        {
            StackPanel page = new StackPanel();
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            EveningRitualEntry existing =
                _settings.EveningRitualHistory.Find(
                    x => x.DateKey == today);
            if (existing != null && !_editingRitual)
            {
                Border closed = PaperCard(
                    "今天已经收好了",
                    existing.ClosingMessage,
                    Theme.Mint);
                StackPanel closedContent = (StackPanel)closed.Child;
                AddAnswer(
                    closedContent,
                    "已经完成",
                    existing.Accomplished);
                AddAnswer(
                    closedContent,
                    "最消耗我的",
                    existing.MostDraining);
                AddAnswer(
                    closedContent,
                    "留给明天",
                    existing.LeaveForTomorrow);
                Button edit = Theme.Button("重新写一下", false);
                edit.HorizontalAlignment = HorizontalAlignment.Left;
                edit.Margin = new Thickness(0, 14, 0, 0);
                edit.Click += delegate
                {
                    _editingRitual = true;
                    _hasRitualDraft = false;
                    ShowPage("ritual");
                };
                closedContent.Children.Add(edit);
                page.Children.Add(closed);
                return page;
            }

            page.Children.Add(PaperCard(
                "今天不需要写成总结报告",
                "只留下愿意留下的部分。空着一项，也不会影响今天结束。",
                Theme.Peach));
            Border form = Theme.CardBorder();
            StackPanel fields = new StackPanel();
            _ritualAccomplished = LabeledInput(
                fields,
                "今天有哪件事已经完成了？",
                _hasRitualDraft
                    ? _ritualDraftAccomplished
                    : existing == null ? "" : existing.Accomplished,
                68);
            _ritualDraining = LabeledInput(
                fields,
                "哪件事最消耗你？",
                _hasRitualDraft
                    ? _ritualDraftDraining
                    : existing == null ? "" : existing.MostDraining,
                68);
            _ritualTomorrow = LabeledInput(
                fields,
                "有什么可以名正言顺地留给明天？",
                _hasRitualDraft
                    ? _ritualDraftTomorrow
                    : existing == null ? "" : existing.LeaveForTomorrow,
                68);
            TextBlock choose = Theme.Text(
                "现在更想怎样结束？",
                12,
                FontWeights.Bold,
                Theme.Ink);
            choose.Margin = new Thickness(0, 4, 0, 8);
            fields.Children.Add(choose);
            WrapPanel styles = new WrapPanel();
            styles.Children.Add(StyleButton(
                "朋友式肯定",
                "praise"));
            styles.Children.Add(StyleButton(
                "讲个冷笑话",
                "funny"));
            styles.Children.Add(StyleButton(
                "直接收工",
                "quiet"));
            fields.Children.Add(styles);
            Button finish = Theme.Button("把今天收起来", true);
            finish.HorizontalAlignment = HorizontalAlignment.Left;
            finish.Margin = new Thickness(0, 14, 0, 0);
            finish.Click += delegate { SaveRitual(existing); };
            fields.Children.Add(finish);
            form.Child = fields;
            page.Children.Add(form);
            if (!string.IsNullOrWhiteSpace(_ritualStatusText))
            {
                page.Children.Add(MessageCard(_ritualStatusText));
            }
            return page;
        }

        private void StoreWorry()
        {
            if (_worryInput == null ||
                string.IsNullOrWhiteSpace(_worryInput.Text))
            {
                _worryActionText =
                    "先写下一句话，本喵会把它收进便签盒。";
                ShowPage("worry");
                return;
            }
            _settings.WorryNotes.Add(new WorryNote
            {
                Text = _worryInput.Text.Trim(),
                Kind = "worry",
                CreatedAtLocal = DateTime.Now
            });
            _worryActionText =
                "收好了。现在不处理，也是一种处理方式。";
            Save();
            React(CatMood.Note, 3);
            ShowPage("worry");
        }

        private void DismissWorry()
        {
            if (_worryInput == null ||
                string.IsNullOrWhiteSpace(_worryInput.Text))
            {
                _worryActionText =
                    "纸还是空的，本喵先不表演拍纸团。";
                ShowPage("worry");
                return;
            }
            _worryActionText = CareContent.RandomFrom(
                CareContent.PaperDismissalResponses,
                _random);
            React(CatMood.Happy, 2.5);
            ShowPage("worry");
        }

        private void SuggestBoundary(string tone)
        {
            _worryActionText = CareContent.RandomFrom(
                CareContent.BoundarySet(tone),
                _random);
            React(CatMood.Companion, 2);
            ShowPage("worry");
        }

        private void BuildReframeResult()
        {
            if (_criticismInput == null ||
                string.IsNullOrWhiteSpace(_criticismInput.Text))
            {
                _reframeResultText =
                    "先写下正在责怪自己的那句话。它不需要写得合理。";
                ShowPage("reframe");
                return;
            }
            _reframeSeed = _criticismInput.Text.Trim();
            string fact = ValueOr(
                _factInput.Text,
                "这部分暂时还没有写清楚。");
            string friend = ValueOr(
                _friendInput.Text,
                "如果暂时说不出安慰的话，也不责怪自己。");
            string next = ValueOr(
                _nextStepInput.Text,
                "今天可以先不决定下一步。");
            string consequence = ValueOr(
                _consequenceInput.Text,
                "真实后果暂时不明确，不把想象当成结论。");
            _reframeResultText =
                "事实：" + fact + "\n\n" +
                "我正在对自己说：" + _reframeSeed + "\n\n" +
                "如果是朋友：" + friend + "\n\n" +
                "最小下一步：" + next + "\n\n" +
                "今天不处理的真实后果：" + consequence;
            React(CatMood.Loaf, 3);
            ShowPage("reframe");
        }

        private void SaveRitual(EveningRitualEntry existing)
        {
            EveningRitualEntry entry = existing;
            if (entry == null)
            {
                entry = new EveningRitualEntry();
                _settings.EveningRitualHistory.Add(entry);
            }
            entry.DateKey = DateTime.Today.ToString("yyyy-MM-dd");
            entry.CompletedAtLocal = DateTime.Now;
            entry.Accomplished = ValueOr(
                _ritualAccomplished.Text,
                "今天走到了这里");
            entry.MostDraining = ValueOr(
                _ritualDraining.Text,
                "这部分今天不想展开");
            entry.LeaveForTomorrow = ValueOr(
                _ritualTomorrow.Text,
                "没有写下来的事情，都可以先留在今天");
            entry.ClosingStyle = _closingStyle;
            entry.ClosingMessage = CareContent.ClosingFor(
                _closingStyle,
                _random);
            _ritualStatusText = entry.ClosingMessage;
            _editingRitual = false;
            _hasRitualDraft = false;
            Save();
            React(CatMood.Backpack, 4);
            ShowPage("ritual");
        }

        private Button StyleButton(string text, string style)
        {
            bool selected = _closingStyle == style;
            Button button = Theme.Button(text, selected);
            button.Tag = style;
            button.Click += delegate(object sender, RoutedEventArgs e)
            {
                CaptureRitualDraft();
                _closingStyle = (string)((Button)sender).Tag;
                ShowPage("ritual");
            };
            return button;
        }

        private void CaptureRitualDraft()
        {
            if (_ritualAccomplished == null ||
                _ritualDraining == null ||
                _ritualTomorrow == null)
            {
                return;
            }
            _ritualDraftAccomplished = _ritualAccomplished.Text;
            _ritualDraftDraining = _ritualDraining.Text;
            _ritualDraftTomorrow = _ritualTomorrow.Text;
            _hasRitualDraft = true;
        }

        private static void AddAnswer(
            StackPanel panel,
            string label,
            string value)
        {
            TextBlock title = Theme.Text(
                label,
                11,
                FontWeights.Bold,
                Theme.MintDark);
            title.Margin = new Thickness(0, 14, 0, 3);
            panel.Children.Add(title);
            panel.Children.Add(Theme.Text(
                value,
                13,
                FontWeights.Normal,
                Theme.Ink));
        }

        private TextBox LabeledInput(
            StackPanel panel,
            string label,
            string value,
            double height)
        {
            TextBlock title = Theme.Text(
                label,
                12,
                FontWeights.Bold,
                Theme.Ink);
            title.Margin = new Thickness(0, 0, 0, 7);
            panel.Children.Add(title);
            TextBox input = MultiLineInput(value, height);
            input.Margin = new Thickness(0, 0, 0, 14);
            panel.Children.Add(input);
            return input;
        }

        private static TextBox MultiLineInput(
            string value,
            double height)
        {
            return new TextBox
            {
                Text = value ?? "",
                Height = height,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = Theme.Line,
                Background = Theme.Ivory,
                Foreground = Theme.Ink,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13
            };
        }

        private Border PaperCard(
            string title,
            string body,
            Brush accent)
        {
            Border card = new Border
            {
                Background = Theme.Brush("#FFFDF8"),
                BorderBrush = Theme.Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 14)
            };
            StackPanel content = new StackPanel();
            Grid heading = new Grid();
            heading.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(6)
            });
            heading.ColumnDefinitions.Add(new ColumnDefinition());
            Border mark = new Border
            {
                Background = accent,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 1, 0, 1)
            };
            heading.Children.Add(mark);
            TextBlock titleText = Theme.Text(
                title,
                16,
                FontWeights.Bold,
                Theme.Ink);
            titleText.Margin = new Thickness(11, 0, 0, 0);
            Grid.SetColumn(titleText, 1);
            heading.Children.Add(titleText);
            content.Children.Add(heading);
            TextBlock bodyText = Theme.Text(
                body,
                12,
                FontWeights.Normal,
                Theme.Muted);
            bodyText.Margin = new Thickness(17, 8, 0, 0);
            content.Children.Add(bodyText);
            card.Child = content;
            return card;
        }

        private Border MessageCard(string text)
        {
            return PaperCard(
                "桌面搭子说",
                text,
                Theme.Mint);
        }

        private Button ActionButton(string text, Action action)
        {
            Button button = Theme.Button(text, false);
            button.Margin = new Thickness(0, 0, 8, 8);
            button.Click += delegate { action(); };
            return button;
        }

        private void EnsureWeatherToday()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (_settings.EmotionWeatherDateKey == today)
            {
                return;
            }
            _settings.EmotionWeatherDateKey = "";
            _settings.EmotionWeather = EmotionWeather.None;
        }

        private void Save()
        {
            Action handler = SaveRequested;
            if (handler != null)
            {
                handler();
            }
        }

        private void React(CatMood mood, double seconds)
        {
            _cat.Mood = mood;
            Action<CatMood, double> handler = CatReactionRequested;
            if (handler != null)
            {
                handler(mood, seconds);
            }
        }

        private static string ValueOr(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }

        private static void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text ?? "");
            }
            catch
            {
            }
        }
    }
}
