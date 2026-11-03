using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace XieYiMiao
{
    public static class AppTime
    {
        public static readonly DateTime Never =
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

        public static bool IsNever(DateTime value)
        {
            return value.Year < 2001;
        }
    }

    public enum ReminderKind
    {
        Drink,
        Movement,
        Meal,
        Dorm,
        Todo
    }

    public enum TodoRepeat
    {
        None,
        Daily,
        Weekly,
        Weekdays
    }

    public enum TodoDifficulty
    {
        Simple,
        Normal,
        Important
    }

    public enum RewardCategory
    {
        Rest,
        Entertainment,
        Outdoors,
        Social,
        Hobby,
        Personal,
        Custom,
        Venting,
        Food,
        SelfFirst,
        Comfort
    }

    public enum DailyCareKind
    {
        Body,
        Feelings,
        Boundary,
        Kindness
    }

    public enum PetInteractionLevel
    {
        Normal,
        Quiet,
        Playful
    }

    public enum PetOutfit
    {
        Classic,
        ApricotScarf,
        SleepCap,
        GraduateBag,
        SafetyGear
    }

    public enum EmotionWeather
    {
        None,
        Sunny,
        Cloudy,
        Drizzle,
        Windy,
        Foggy,
        Thunderstorm
    }

    [DataContract]
    public sealed class WorryNote
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Text { get; set; }
        [DataMember] public string Kind { get; set; }
        [DataMember] public DateTime CreatedAtLocal { get; set; }

        public WorryNote()
        {
            Id = Guid.NewGuid().ToString("N");
            Text = "";
            Kind = "worry";
            CreatedAtLocal = DateTime.Now;
        }
    }

    [DataContract]
    public sealed class EveningRitualEntry
    {
        [DataMember] public string DateKey { get; set; }
        [DataMember] public DateTime CompletedAtLocal { get; set; }
        [DataMember] public string Accomplished { get; set; }
        [DataMember] public string MostDraining { get; set; }
        [DataMember] public string LeaveForTomorrow { get; set; }
        [DataMember] public string ClosingStyle { get; set; }
        [DataMember] public string ClosingMessage { get; set; }

        public EveningRitualEntry()
        {
            DateKey = DateTime.Today.ToString("yyyy-MM-dd");
            CompletedAtLocal = DateTime.Now;
            Accomplished = "";
            MostDraining = "";
            LeaveForTomorrow = "";
            ClosingStyle = "quiet";
            ClosingMessage = "";
        }
    }

    [DataContract]
    public sealed class ScheduleEntry
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Label { get; set; }
        [DataMember] public string TimeText { get; set; }
        [DataMember] public int DaysMask { get; set; }
        [DataMember] public bool Enabled { get; set; }
        [DataMember] public string LastTriggeredDateKey { get; set; }

        public ScheduleEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Label = "吃饭";
            TimeText = "12:00";
            DaysMask = 127;
            Enabled = true;
            LastTriggeredDateKey = "";
        }

        public bool AppliesTo(DateTime value)
        {
            int bit = 1 << (((int)value.DayOfWeek + 6) % 7);
            return (DaysMask & bit) != 0;
        }

        public bool TryGetTime(out TimeSpan result)
        {
            return TimeSpan.TryParse(TimeText, out result);
        }
    }

    [DataContract]
    public sealed class TodoItem
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public string Note { get; set; }
        [DataMember] public DateTime DueLocal { get; set; }
        [DataMember] public TodoRepeat Repeat { get; set; }
        [DataMember] public bool Completed { get; set; }
        [DataMember] public DateTime SnoozedUntilLocal { get; set; }
        [DataMember] public long LastNotifiedDueTicks { get; set; }
        [DataMember] public string CategoryId { get; set; }
        [DataMember] public TodoDifficulty Difficulty { get; set; }

        public TodoItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "";
            Note = "";
            DueLocal = DateTime.Now.AddHours(1);
            Repeat = TodoRepeat.None;
            SnoozedUntilLocal = AppTime.Never;
            CategoryId = "uncategorized";
            Difficulty = TodoDifficulty.Normal;
        }

        public int DifficultyPoints
        {
            get
            {
                if (Difficulty == TodoDifficulty.Important)
                {
                    return 5;
                }
                return Difficulty == TodoDifficulty.Normal ? 3 : 1;
            }
        }

        public void AdvanceAfterCompletion()
        {
            if (Repeat == TodoRepeat.None)
            {
                Completed = true;
                return;
            }

            DateTime next = DueLocal;
            if (Repeat == TodoRepeat.Daily)
            {
                next = next.AddDays(1);
            }
            else if (Repeat == TodoRepeat.Weekly)
            {
                next = next.AddDays(7);
            }
            else
            {
                do
                {
                    next = next.AddDays(1);
                }
                while (next.DayOfWeek == DayOfWeek.Saturday ||
                       next.DayOfWeek == DayOfWeek.Sunday);
            }

            while (next <= DateTime.Now)
            {
                if (Repeat == TodoRepeat.Weekly)
                {
                    next = next.AddDays(7);
                }
                else
                {
                    next = next.AddDays(1);
                    if (Repeat == TodoRepeat.Weekdays)
                    {
                        while (next.DayOfWeek == DayOfWeek.Saturday ||
                               next.DayOfWeek == DayOfWeek.Sunday)
                        {
                            next = next.AddDays(1);
                        }
                    }
                }
            }

            DueLocal = next;
            Completed = false;
            SnoozedUntilLocal = AppTime.Never;
            LastNotifiedDueTicks = 0;
        }
    }

    [DataContract]
    public sealed class TodoCategory
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string ColorHex { get; set; }
        [DataMember] public int SortOrder { get; set; }
        [DataMember] public bool IsFallback { get; set; }

        public TodoCategory()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "新分类";
            ColorHex = "#8CCEB5";
        }
    }

    [DataContract]
    public sealed class RewardItem
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public RewardCategory Category { get; set; }
        [DataMember] public int FreedomCost { get; set; }
        [DataMember] public int DurationMinutes { get; set; }
        [DataMember] public bool Favorite { get; set; }
        [DataMember] public bool Repeatable { get; set; }
        [DataMember] public bool BuiltIn { get; set; }
        [DataMember] public bool IsFree { get; set; }
        [DataMember] public bool CaresForOthers { get; set; }

        public RewardItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "新的奖励";
            Description = "";
            Category = RewardCategory.Custom;
            FreedomCost = 10;
            Repeatable = true;
        }
    }

    [DataContract]
    public sealed class RewardRedemption
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string RewardId { get; set; }
        [DataMember] public string RewardTitle { get; set; }
        [DataMember] public int FreedomCost { get; set; }
        [DataMember] public int DurationMinutes { get; set; }
        [DataMember] public DateTime RedeemedAtLocal { get; set; }

        public RewardRedemption()
        {
            Id = Guid.NewGuid().ToString("N");
            RedeemedAtLocal = DateTime.Now;
        }
    }

    [DataContract]
    public sealed class DailyStats
    {
        [DataMember] public string DateKey { get; set; }
        [DataMember] public int DrinkCount { get; set; }
        [DataMember] public int MovementCount { get; set; }
        [DataMember] public int MealCount { get; set; }
        [DataMember] public int DormCount { get; set; }
        [DataMember] public int TodoCount { get; set; }
        [DataMember] public long GrowthPoints { get; set; }
        [DataMember] public long FreedomEarnedToday { get; set; }
        [DataMember] public bool BalancedBonusGranted { get; set; }
        [DataMember] public bool CareBodyCompleted { get; set; }
        [DataMember] public bool CareFeelingsCompleted { get; set; }
        [DataMember] public bool CareBoundaryCompleted { get; set; }
        [DataMember] public bool CareKindnessCompleted { get; set; }
        [DataMember] public bool ReflectionStopped { get; set; }
        [DataMember] public string MoodKey { get; set; }
        [DataMember] public string MoodNote { get; set; }

        public DailyStats()
        {
            DateKey = DateTime.Today.ToString("yyyy-MM-dd");
        }

        public int TotalToday
        {
            get
            {
                return DrinkCount + MovementCount + MealCount + DormCount + TodoCount;
            }
        }

        public int CareCompletedCount
        {
            get
            {
                int value = 0;
                if (CareBodyCompleted) value++;
                if (CareFeelingsCompleted) value++;
                if (CareBoundaryCompleted) value++;
                if (CareKindnessCompleted) value++;
                return value;
            }
        }

        public bool AllCareCompleted
        {
            get { return CareCompletedCount == 4; }
        }

        public bool IsCareCompleted(DailyCareKind kind)
        {
            if (kind == DailyCareKind.Body)
            {
                return CareBodyCompleted;
            }
            if (kind == DailyCareKind.Feelings)
            {
                return CareFeelingsCompleted;
            }
            if (kind == DailyCareKind.Boundary)
            {
                return CareBoundaryCompleted;
            }
            return CareKindnessCompleted;
        }

        public void CompleteCare(DailyCareKind kind)
        {
            if (kind == DailyCareKind.Body)
            {
                CareBodyCompleted = true;
            }
            else if (kind == DailyCareKind.Feelings)
            {
                CareFeelingsCompleted = true;
            }
            else if (kind == DailyCareKind.Boundary)
            {
                CareBoundaryCompleted = true;
            }
            else
            {
                CareKindnessCompleted = true;
            }
        }

        public void EnsureToday()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (DateKey == today)
            {
                return;
            }

            DateKey = today;
            DrinkCount = 0;
            MovementCount = 0;
            MealCount = 0;
            DormCount = 0;
            TodoCount = 0;
            FreedomEarnedToday = 0;
            BalancedBonusGranted = false;
            CareBodyCompleted = false;
            CareFeelingsCompleted = false;
            CareBoundaryCompleted = false;
            CareKindnessCompleted = false;
            ReflectionStopped = false;
            MoodKey = "";
            MoodNote = "";
        }
    }

    [DataContract]
    public sealed class AppSettings
    {
        [DataMember] public int Version { get; set; }
        [DataMember] public bool FirstRun { get; set; }
        [DataMember] public bool DrinkEnabled { get; set; }
        [DataMember] public int DrinkIntervalMinutes { get; set; }
        [DataMember] public bool MovementEnabled { get; set; }
        [DataMember] public int MovementIntervalMinutes { get; set; }
        [DataMember] public List<ScheduleEntry> MealSchedules { get; set; }
        [DataMember] public ScheduleEntry DormSchedule { get; set; }
        [DataMember] public List<TodoItem> Todos { get; set; }
        [DataMember] public List<TodoCategory> TodoCategories { get; set; }
        [DataMember] public List<RewardItem> Rewards { get; set; }
        [DataMember] public List<RewardRedemption> RewardHistory { get; set; }
        [DataMember] public long FreedomPoints { get; set; }
        [DataMember] public long TotalFreedomEarned { get; set; }
        [DataMember] public bool PetVisible { get; set; }
        [DataMember] public int PetSizePercent { get; set; }
        [DataMember] public bool LimitedTerritory { get; set; }
        [DataMember] public bool EnergySaver { get; set; }
        [DataMember] public bool AutoHideFullScreen { get; set; }
        [DataMember] public bool AutoHideMeetings { get; set; }
        public bool ManualMeetingMode { get; set; }
        [DataMember] public int IdleThresholdMinutes { get; set; }
        [DataMember] public int ProtectionGraceMinutes { get; set; }
        [DataMember] public bool WarmMessages { get; set; }
        [DataMember] public bool AutoStart { get; set; }
        [DataMember] public double PetLeft { get; set; }
        [DataMember] public double PetTop { get; set; }
        [DataMember] public double TerritoryHomeLeft { get; set; }
        [DataMember] public double TerritoryHomeTop { get; set; }
        [DataMember] public double DrinkActiveSeconds { get; set; }
        [DataMember] public double MovementActiveSeconds { get; set; }
        [DataMember] public DateTime DrinkSnoozedUntil { get; set; }
        [DataMember] public DateTime MovementSnoozedUntil { get; set; }
        [DataMember] public DateTime PausedUntil { get; set; }
        [DataMember] public DailyStats Stats { get; set; }
        [DataMember] public int HotKeyModifiers { get; set; }
        [DataMember] public int HotKeyVirtualKey { get; set; }
        [DataMember] public string PetMonitorDevice { get; set; }
        [DataMember] public double PetAnchorX { get; set; }
        [DataMember] public double PetAnchorY { get; set; }
        [DataMember] public string TerritoryMonitorDevice { get; set; }
        [DataMember] public double TerritoryAnchorX { get; set; }
        [DataMember] public double TerritoryAnchorY { get; set; }
        [DataMember] public PetInteractionLevel PetInteractionLevel { get; set; }
        [DataMember] public PetOutfit SelectedPetOutfit { get; set; }
        [DataMember] public bool EveningRitualEnabled { get; set; }
        [DataMember] public EmotionWeather EmotionWeather { get; set; }
        [DataMember] public string EmotionWeatherDateKey { get; set; }
        [DataMember] public List<WorryNote> WorryNotes { get; set; }
        [DataMember] public List<EveningRitualEntry> EveningRitualHistory { get; set; }
        [DataMember] public long PetHeadPatCount { get; set; }
        [DataMember] public long PetBodyTapCount { get; set; }

        public static AppSettings CreateDefault()
        {
            AppSettings value = new AppSettings();
            value.Version = 7;
            value.FirstRun = true;
            value.DrinkEnabled = true;
            value.DrinkIntervalMinutes = 45;
            value.MovementEnabled = true;
            value.MovementIntervalMinutes = 60;
            value.MealSchedules = new List<ScheduleEntry>();
            value.DormSchedule = new ScheduleEntry
            {
                Label = "回宿舍",
                TimeText = "00:00",
                DaysMask = 127,
                Enabled = true
            };
            value.Todos = new List<TodoItem>();
            value.TodoCategories = CreateDefaultTodoCategories();
            value.Rewards = CreateDefaultRewards();
            value.RewardHistory = new List<RewardRedemption>();
            value.PetVisible = true;
            value.PetSizePercent = 70;
            value.LimitedTerritory = true;
            value.EnergySaver = true;
            value.AutoHideFullScreen = true;
            value.AutoHideMeetings = true;
            value.IdleThresholdMinutes = 5;
            value.ProtectionGraceMinutes = 3;
            value.WarmMessages = true;
            value.AutoStart = false;
            value.PetLeft = -1;
            value.PetTop = -1;
            value.TerritoryHomeLeft = -1;
            value.TerritoryHomeTop = -1;
            value.DrinkSnoozedUntil = AppTime.Never;
            value.MovementSnoozedUntil = AppTime.Never;
            value.PausedUntil = AppTime.Never;
            value.Stats = new DailyStats();
            value.HotKeyModifiers = 3;
            value.HotKeyVirtualKey = 0x4D;
            value.PetMonitorDevice = "";
            value.PetAnchorX = -1;
            value.PetAnchorY = -1;
            value.TerritoryMonitorDevice = "";
            value.TerritoryAnchorX = -1;
            value.TerritoryAnchorY = -1;
            value.PetInteractionLevel = PetInteractionLevel.Normal;
            value.SelectedPetOutfit = PetOutfit.Classic;
            value.EveningRitualEnabled = true;
            value.EmotionWeather = EmotionWeather.None;
            value.EmotionWeatherDateKey = "";
            value.WorryNotes = new List<WorryNote>();
            value.EveningRitualHistory = new List<EveningRitualEntry>();
            return value;
        }

        public void Normalize()
        {
            int loadedVersion = Version;
            DrinkIntervalMinutes = Clamp(DrinkIntervalMinutes, 5, 240, 45);
            MovementIntervalMinutes = Clamp(MovementIntervalMinutes, 5, 240, 60);
            IdleThresholdMinutes = Clamp(IdleThresholdMinutes, 2, 60, 5);
            ProtectionGraceMinutes = Clamp(ProtectionGraceMinutes, 0, 30, 3);
            PetSizePercent = Clamp(PetSizePercent, 50, 120, 70);
            if (MealSchedules == null)
            {
                MealSchedules = new List<ScheduleEntry>();
            }
            if (Todos == null)
            {
                Todos = new List<TodoItem>();
            }
            if (TodoCategories == null || TodoCategories.Count == 0)
            {
                TodoCategories = CreateDefaultTodoCategories();
            }
            EnsureFallbackCategory();
            if (Rewards == null)
            {
                Rewards = CreateDefaultRewards();
            }
            if (RewardHistory == null)
            {
                RewardHistory = new List<RewardRedemption>();
            }
            if (DormSchedule == null)
            {
                DormSchedule = new ScheduleEntry
                {
                    Label = "回宿舍",
                    TimeText = "00:00",
                    DaysMask = 127,
                    Enabled = true
                };
            }
            else if (loadedVersion < 2 &&
                     DormSchedule.TimeText == "22:30")
            {
                DormSchedule.TimeText = "00:00";
            }
            if (loadedVersion < 3)
            {
                // v0.2 could get stuck invisible because its global shortcut was
                // only registered after the pet had already been shown.
                PetVisible = true;
                ManualMeetingMode = false;
            }
            if (loadedVersion < 5)
            {
                foreach (TodoItem legacyTodo in Todos)
                {
                    legacyTodo.CategoryId = "uncategorized";
                    legacyTodo.Difficulty = TodoDifficulty.Normal;
                }
                FreedomPoints = Stats == null ? 0 : Stats.GrowthPoints;
                TotalFreedomEarned = FreedomPoints;
            }
            if (loadedVersion < 6)
            {
                List<RewardItem> customRewards =
                    new List<RewardItem>();
                foreach (RewardItem oldReward in Rewards)
                {
                    if (!oldReward.BuiltIn)
                    {
                        customRewards.Add(oldReward);
                    }
                }
                Rewards = CreateDefaultRewards();
                Rewards.AddRange(customRewards);
            }
            if (loadedVersion < 7)
            {
                PetInteractionLevel = PetInteractionLevel.Normal;
                SelectedPetOutfit = PetOutfit.Classic;
                EveningRitualEnabled = true;
            }
            if (WorryNotes == null)
            {
                WorryNotes = new List<WorryNote>();
            }
            if (EveningRitualHistory == null)
            {
                EveningRitualHistory =
                    new List<EveningRitualEntry>();
            }
            if (EmotionWeatherDateKey == null)
            {
                EmotionWeatherDateKey = "";
            }
            if (EmotionWeatherDateKey !=
                DateTime.Today.ToString("yyyy-MM-dd"))
            {
                EmotionWeatherDateKey = "";
                EmotionWeather = EmotionWeather.None;
            }
            if (HotKeyModifiers == 0)
            {
                HotKeyModifiers = 3;
            }
            if (HotKeyVirtualKey <= 0)
            {
                HotKeyVirtualKey = 0x4D;
            }
            if (PetAnchorX < 0 || PetAnchorX > 1 ||
                PetAnchorY < 0 || PetAnchorY > 1)
            {
                PetAnchorX = -1;
                PetAnchorY = -1;
            }
            if (TerritoryAnchorX < 0 || TerritoryAnchorX > 1 ||
                TerritoryAnchorY < 0 || TerritoryAnchorY > 1)
            {
                TerritoryAnchorX = -1;
                TerritoryAnchorY = -1;
            }
            Version = 7;
            if (Stats == null)
            {
                Stats = new DailyStats();
            }
            if (AppTime.IsNever(DrinkSnoozedUntil))
            {
                DrinkSnoozedUntil = AppTime.Never;
            }
            if (AppTime.IsNever(MovementSnoozedUntil))
            {
                MovementSnoozedUntil = AppTime.Never;
            }
            if (AppTime.IsNever(PausedUntil))
            {
                PausedUntil = AppTime.Never;
            }
            foreach (TodoItem item in Todos)
            {
                if (string.IsNullOrWhiteSpace(item.CategoryId) ||
                    !TodoCategories.Exists(x => x.Id == item.CategoryId))
                {
                    item.CategoryId = "uncategorized";
                }
                if (AppTime.IsNever(item.SnoozedUntilLocal))
                {
                    item.SnoozedUntilLocal = AppTime.Never;
                }
            }
            foreach (RewardItem reward in Rewards)
            {
                if (string.IsNullOrWhiteSpace(reward.Id))
                {
                    reward.Id = Guid.NewGuid().ToString("N");
                }
                reward.FreedomCost = reward.IsFree
                    ? 0
                    : Math.Max(1, reward.FreedomCost);
                reward.DurationMinutes =
                    reward.Category == RewardCategory.Rest ||
                    reward.Category == RewardCategory.SelfFirst ||
                    reward.Category == RewardCategory.Comfort
                        ? 0
                        : Math.Max(0, reward.DurationMinutes);
            }
            foreach (WorryNote note in WorryNotes)
            {
                if (string.IsNullOrWhiteSpace(note.Id))
                {
                    note.Id = Guid.NewGuid().ToString("N");
                }
                if (note.Text == null) note.Text = "";
                if (note.Kind == null) note.Kind = "worry";
            }
            foreach (EveningRitualEntry entry in EveningRitualHistory)
            {
                if (entry.DateKey == null) entry.DateKey = "";
                if (entry.Accomplished == null) entry.Accomplished = "";
                if (entry.MostDraining == null) entry.MostDraining = "";
                if (entry.LeaveForTomorrow == null)
                {
                    entry.LeaveForTomorrow = "";
                }
                if (entry.ClosingStyle == null)
                {
                    entry.ClosingStyle = "quiet";
                }
                if (entry.ClosingMessage == null)
                {
                    entry.ClosingMessage = "";
                }
            }
            if (!PetOutfitService.IsUnlocked(
                SelectedPetOutfit,
                Stats == null ? 0 : Stats.GrowthPoints))
            {
                SelectedPetOutfit = PetOutfit.Classic;
            }
            if (Stats.MoodKey == null) Stats.MoodKey = "";
            if (Stats.MoodNote == null) Stats.MoodNote = "";
            Stats.EnsureToday();
        }

        public TodoCategory FindCategory(string id)
        {
            TodoCategory value = TodoCategories == null
                ? null
                : TodoCategories.Find(x => x.Id == id);
            return value ?? TodoCategories.Find(x => x.IsFallback);
        }

        public void DeleteCategory(string id)
        {
            TodoCategory category = TodoCategories.Find(x => x.Id == id);
            if (category == null || category.IsFallback)
            {
                return;
            }
            TodoCategories.Remove(category);
            foreach (TodoItem item in Todos)
            {
                if (item.CategoryId == id)
                {
                    item.CategoryId = "uncategorized";
                }
            }
        }

        private void EnsureFallbackCategory()
        {
            TodoCategory fallback =
                TodoCategories.Find(x => x.Id == "uncategorized");
            if (fallback == null)
            {
                fallback = new TodoCategory
                {
                    Id = "uncategorized",
                    Name = "未分类",
                    ColorHex = "#B8AAA3",
                    SortOrder = 999,
                    IsFallback = true
                };
                TodoCategories.Add(fallback);
            }
            fallback.IsFallback = true;
        }

        public static List<TodoCategory> CreateDefaultTodoCategories()
        {
            return new List<TodoCategory>
            {
                Category("research", "科研", "#79BDE3", 0, false),
                Category("study", "学习", "#8CCEB5", 1, false),
                Category("work", "工作", "#F2A15F", 2, false),
                Category("life", "生活", "#F5B58F", 3, false),
                Category("health", "健康", "#73B99A", 4, false),
                Category("other", "其他", "#B597C7", 5, false),
                Category("uncategorized", "未分类", "#B8AAA3", 999, true)
            };
        }

        public static List<RewardItem> CreateDefaultRewards()
        {
            return new List<RewardItem>
            {
                // 免费区：永远排在自由度奖励之前，不限次数。
                Reward("free-self-01", "拒绝一个不合理的临时要求", "不需要立刻为别人的临时安排让路。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-02", "不立刻回复一条有压力的消息", "先让自己缓一缓，再决定什么时候回复。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-03", "取消一件非必要安排", "把今天有限的精力留给更重要的自己。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-04", "今天不替别人收拾一次残局", "别人的责任，可以先留在别人手里。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-05", "说一次“我现在没有余力”", "说明真实状态不是自私。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-06", "不为别人的情绪负责一次", "你可以关心别人，但不用承担全部。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-07", "选择自己真正想吃的东西", "今天有一次选择只听自己的感受。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-08", "请别人分担一件事", "被照顾和被帮助也是正常的。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-09", "把自己的需求讲清楚", "不用把希望全部藏在体谅里。", RewardCategory.SelfFirst, 0, 0, true, false),
                Reward("free-self-10", "今天不解释一个属于自己的决定", "有些选择不需要写答辩材料。", RewardCategory.SelfFirst, 0, 0, true, false),

                Reward("free-comfort-01", "换上最舒服的衣服", "身体舒服一点，就是充分的理由。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-02", "洗一个不赶时间的澡", "让今天的疲惫慢慢散掉。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-03", "整理一下睡觉的角落", "只整理到能让自己更舒服，不做大扫除。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-04", "提前回宿舍", "没必要每一次都坚持到最后离开。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-05", "今晚不再复盘工作", "今天已经结束，不继续审判自己。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-06", "什么都不安排", "休息什么时候结束，由你自己决定。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-07", "认真吃一顿热饭", "不边赶任务边对付一口。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-08", "晒晒太阳或吹吹风", "换一点空气，不需要完成运动指标。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-09", "抱一会儿枕头或盖上喜欢的毯子", "给身体一个安全、柔软的信号。", RewardCategory.Comfort, 0, 0, true, false),
                Reward("free-comfort-10", "安静待着，暂时不解决问题", "不是所有事情都必须今天想明白。", RewardCategory.Comfort, 0, 0, true, false),

                // 课题组吐槽局。
                Reward("vent-01", "跟可信的朋友吐槽一件课题组的离谱事", "讲完就先放在朋友那里，不带回脑内继续开会。", RewardCategory.Venting, 6, 0, false, false),
                Reward("vent-02", "评选“今日最迷惑行为”", "给离谱事件颁个奖，让它从压力变成素材。", RewardCategory.Venting, 5, 0, false, false),
                Reward("vent-03", "给离谱事件取一个新闻标题", "越一本正经越好笑。", RewardCategory.Venting, 4, 0, false, false),
                Reward("vent-04", "把想吐槽的话写进私人草稿", "不发送也可以，先让这些话离开脑子。", RewardCategory.Venting, 3, 0, false, false),
                Reward("vent-05", "做一张只有朋友能懂的梗图", "把荒谬变成一点可分享的快乐。", RewardCategory.Venting, 7, 0, false, false),
                Reward("vent-06", "约朋友边散步边吐槽", "走一走，让情绪也换个地方。", RewardCategory.Venting, 9, 0, false, false),
                Reward("vent-07", "开一场十分钟“离谱事件发布会”", "只陈述事实和笑点，不要求解决方案。", RewardCategory.Venting, 6, 10, false, false),
                Reward("vent-08", "写一份“课题组迷惑行为年报”素材", "只收录今天这一条，不需要真的做年报。", RewardCategory.Venting, 5, 0, false, false),

                // 吃点真正想吃的。
                Reward("food-01", "点一份水果外卖", "给忙碌的今天补一点新鲜和甜。", RewardCategory.Food, 10, 0, false, false),
                Reward("food-02", "买一杯喜欢的咖啡", "不是续命指标，是一杯自己想喝的。", RewardCategory.Food, 9, 0, false, false),
                Reward("food-03", "买奶茶或喜欢的饮料", "口味由今天的你决定。", RewardCategory.Food, 9, 0, false, false),
                Reward("food-04", "点一份甜品", "不需要先证明今天足够辛苦。", RewardCategory.Food, 11, 0, false, false),
                Reward("food-05", "点一份平时舍不得点的外卖", "认真选自己真正想吃的。", RewardCategory.Food, 18, 0, false, false),
                Reward("food-06", "给今天的饭加一道喜欢的菜", "不是随便吃饱，是让这一顿更满意。", RewardCategory.Food, 8, 0, false, false),
                Reward("food-07", "吃一顿喜欢的早餐", "让一天从照顾自己开始。", RewardCategory.Food, 8, 0, false, false),
                Reward("food-08", "尝试一家收藏很久的店", "把“下次”安排到这一次。", RewardCategory.Food, 20, 0, false, false),
                Reward("food-09", "买酸奶、坚果或喜欢的零食", "给抽屉补一点能让自己开心的东西。", RewardCategory.Food, 8, 0, false, false),
                Reward("food-10", "给朋友们点一次外卖", "照顾朋友很快乐，也记得问问自己想吃什么。", RewardCategory.Food, 24, 0, false, true),
                Reward("food-11", "请朋友吃一顿饭", "和喜欢的人共享一顿轻松的饭。", RewardCategory.Food, 28, 0, false, true),
                Reward("food-12", "约朋友一起吃宵夜", "今天的话可以留在热乎的食物旁边说。", RewardCategory.Food, 16, 0, false, true),

                // 和朋友一起快乐。
                Reward("social-01", "找朋友聊天或玩一会儿", "联系一个相处时可以放松的人。", RewardCategory.Social, 9, 30, false, false),
                Reward("social-02", "发一个很想分享的梗图", "不用组织长篇语言，先分享一点快乐。", RewardCategory.Social, 4, 0, false, false),
                Reward("social-03", "和朋友打一局不计输赢的游戏", "这局只负责好玩。", RewardCategory.Social, 10, 30, false, false),
                Reward("social-04", "给朋友打一个轻松的电话", "不汇报进度，只聊生活。", RewardCategory.Social, 8, 30, false, false),
                Reward("social-05", "约朋友一起散步", "没有目的地也没关系。", RewardCategory.Social, 8, 0, false, false),
                Reward("social-06", "分享一首最近喜欢的歌", "把当下的心情交给音乐表达。", RewardCategory.Social, 5, 0, false, false),
                Reward("social-07", "联系一个很久没见的朋友", "只说想说的话，不必写近况总结。", RewardCategory.Social, 10, 0, false, false),
                Reward("social-08", "和朋友安排一次不聊科研的见面", "让关系里有一些课题之外的东西。", RewardCategory.Social, 15, 0, false, false),

                // 娱乐仍可保留可选时长。
                Reward("ent-01", "看一集喜欢的剧", "安心进入另一个故事。", RewardCategory.Entertainment, 10, 45, false, false),
                Reward("ent-02", "玩游戏 30 分钟", "选一款现在真正想玩的游戏。", RewardCategory.Entertainment, 8, 30, false, false),
                Reward("ent-03", "玩游戏 60 分钟", "留出一段完整娱乐时间。", RewardCategory.Entertainment, 15, 60, false, false),
                Reward("ent-04", "玩游戏 120 分钟", "不被打断地玩一场。", RewardCategory.Entertainment, 28, 120, false, false),
                Reward("ent-05", "看一部电影", "挑一部收藏很久的电影。", RewardCategory.Entertainment, 22, 120, false, false),
                Reward("ent-06", "读小说或漫画", "暂时离开论文和技术资料。", RewardCategory.Entertainment, 8, 30, false, false),
                Reward("ent-07", "听完一张专辑", "从第一首听到最后一首。", RewardCategory.Entertainment, 8, 45, false, false),
                Reward("ent-08", "看收藏夹里真正想看的视频", "不是学习资料，只选喜欢的。", RewardCategory.Entertainment, 5, 20, false, false),
                Reward("ent-09", "看一段喜剧或脱口秀", "让大脑暂时只负责笑。", RewardCategory.Entertainment, 7, 30, false, false),
                Reward("ent-10", "看一期轻松的综艺", "不需要从中学到任何东西。", RewardCategory.Entertainment, 9, 60, false, false),

                Reward("out-01", "散步或慢跑", "不追求配速，只换换空气。", RewardCategory.Outdoors, 8, 30, false, false),
                Reward("out-02", "骑车兜风", "选一条安全、轻松的路线。", RewardCategory.Outdoors, 14, 60, false, false),
                Reward("out-03", "打一场球", "羽毛球、篮球或其他喜欢的运动。", RewardCategory.Outdoors, 16, 60, false, false),
                Reward("out-04", "去一个没去过的小公园", "不用打卡，随便走走。", RewardCategory.Outdoors, 12, 0, false, false),
                Reward("out-05", "找个地方看看日落", "把一小段时间留给天色变化。", RewardCategory.Outdoors, 10, 0, false, false),
                Reward("out-06", "带着喜欢的饮料出去坐一会儿", "不处理消息，只看看周围。", RewardCategory.Outdoors, 11, 0, false, false),

                Reward("hobby-01", "摄影、画画或做手工", "做一点不需要产出指标的创作。", RewardCategory.Hobby, 12, 45, false, false),
                Reward("hobby-02", "练琴或纯粹听音乐", "只为喜欢，不做考核。", RewardCategory.Hobby, 10, 40, false, false),
                Reward("hobby-03", "写一小段与论文无关的文字", "可以荒诞、松散，也可以没有结尾。", RewardCategory.Hobby, 8, 20, false, false),
                Reward("hobby-04", "学一个完全没用但有趣的小知识", "知识今天不需要转化成生产力。", RewardCategory.Hobby, 7, 20, false, false),
                Reward("hobby-05", "整理一次喜欢的照片或歌单", "只按自己的审美来。", RewardCategory.Hobby, 9, 30, false, false),
                Reward("hobby-06", "做一顿自己想学的菜", "允许第一次做得不完美。", RewardCategory.Hobby, 15, 60, false, false),

                // 休息类彻底取消时长建议。
                Reward("rest-01", "今晚剩余时间自由安排", "完成必要事项后，允许自己彻底下班。", RewardCategory.Rest, 28, 0, false, false),
                Reward("rest-02", "周末留一段时间不碰科研", "把这段时间完整留给生活。", RewardCategory.Rest, 38, 0, false, false),
                Reward("rest-03", "睡到自然醒", "不设置休息时长，也不要求醒来后立刻高效。", RewardCategory.Rest, 25, 0, false, false),
                Reward("rest-04", "今晚不安排任何额外任务", "必要的事情之外，全部暂停。", RewardCategory.Rest, 20, 0, false, false),
                Reward("rest-05", "允许今天到这里就结束", "没有完成的事情可以属于明天。", RewardCategory.Rest, 18, 0, false, false),
                Reward("rest-06", "取消一件可去可不去的安排", "把空出来的位置留给自己。", RewardCategory.Rest, 16, 0, false, false),

                Reward("personal-01", "买一束喜欢的花", "让生活里出现一点不为实用服务的颜色。", RewardCategory.Personal, 18, 0, false, false),
                Reward("personal-02", "买一个喜欢的小物件", "它只需要让你开心，不需要解释用途。", RewardCategory.Personal, 24, 0, false, false),
                Reward("personal-03", "换一件更舒服的生活用品", "把每天都会接触的东西变好一点。", RewardCategory.Personal, 22, 0, false, false),
                Reward("personal-04", "给小猫换一件装扮", "在奖励中心举行一次装扮仪式。", RewardCategory.Personal, 20, 0, false, false)
            };
        }

        private static TodoCategory Category(
            string id,
            string name,
            string color,
            int order,
            bool fallback)
        {
            return new TodoCategory
            {
                Id = id,
                Name = name,
                ColorHex = color,
                SortOrder = order,
                IsFallback = fallback
            };
        }

        private static RewardItem Reward(
            string id,
            string title,
            string description,
            RewardCategory category,
            int cost,
            int duration,
            bool isFree,
            bool caresForOthers)
        {
            return new RewardItem
            {
                Id = id,
                Title = title,
                Description = description,
                Category = category,
                FreedomCost = cost,
                DurationMinutes = duration,
                Repeatable = true,
                BuiltIn = true,
                IsFree = isFree,
                CaresForOthers = caresForOthers
            };
        }

        private static int Clamp(int value, int min, int max, int fallback)
        {
            if (value == 0)
            {
                return fallback;
            }
            return Math.Max(min, Math.Min(max, value));
        }
    }

    public static class ProgressService
    {
        public static int PointsFor(
            ReminderKind kind,
            TodoDifficulty todoDifficulty)
        {
            if (kind == ReminderKind.Drink)
            {
                return 1;
            }
            if (kind == ReminderKind.Movement ||
                kind == ReminderKind.Meal ||
                kind == ReminderKind.Dorm)
            {
                return 2;
            }
            if (kind == ReminderKind.Todo)
            {
                if (todoDifficulty == TodoDifficulty.Important)
                {
                    return 5;
                }
                return todoDifficulty == TodoDifficulty.Normal ? 3 : 1;
            }
            return 0;
        }

        public static int Award(
            AppSettings settings,
            ReminderKind kind,
            TodoDifficulty todoDifficulty)
        {
            settings.Stats.EnsureToday();
            int points = PointsFor(kind, todoDifficulty);
            Add(settings, points);
            if (!settings.Stats.BalancedBonusGranted &&
                settings.Stats.DrinkCount > 0 &&
                settings.Stats.MovementCount > 0 &&
                settings.Stats.MealCount > 0 &&
                settings.Stats.DormCount > 0 &&
                settings.Stats.TodoCount > 0)
            {
                settings.Stats.BalancedBonusGranted = true;
                Add(settings, 3);
                points += 3;
            }
            return points;
        }

        public static bool TryRedeem(
            AppSettings settings,
            RewardItem reward,
            out string error)
        {
            error = "";
            if (reward == null)
            {
                error = "没有找到这项奖励。";
                return false;
            }
            settings.Stats.EnsureToday();
            if (!reward.IsFree && !settings.Stats.AllCareCompleted)
            {
                error = "先完成今天的四张自我照顾卡，" +
                    "再来使用自由度。";
                return false;
            }
            int cost = reward.IsFree ? 0 : reward.FreedomCost;
            if (settings.FreedomPoints < cost)
            {
                error = "自由度还差 " +
                    (cost - settings.FreedomPoints) + " 点。";
                return false;
            }
            settings.FreedomPoints -= cost;
            settings.RewardHistory.Insert(0, new RewardRedemption
            {
                RewardId = reward.Id,
                RewardTitle = reward.Title,
                FreedomCost = cost,
                DurationMinutes = reward.DurationMinutes,
                RedeemedAtLocal = DateTime.Now
            });
            if (!reward.Repeatable)
            {
                settings.Rewards.Remove(reward);
            }
            return true;
        }

        private static void Add(AppSettings settings, int points)
        {
            settings.Stats.GrowthPoints += points;
            settings.FreedomPoints += points;
            settings.TotalFreedomEarned += points;
            settings.Stats.FreedomEarnedToday += points;
        }
    }

    public sealed class ReminderNotice
    {
        public ReminderKind Kind { get; set; }
        public string SourceId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime RaisedAt { get; set; }
    }
}
