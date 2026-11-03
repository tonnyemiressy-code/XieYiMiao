using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Win32;

namespace XieYiMiao
{
    public static class SelfTests
    {
        private sealed class FakeActivity : IActivityState
        {
            public bool IsLocked { get; set; }
            public double IdleSeconds { get; set; }
        }

        private sealed class FakeProtection : IProtectionState
        {
            public bool Protected { get; set; }

            public bool IsProtected(AppSettings settings)
            {
                return Protected || settings.ManualMeetingMode;
            }
        }

        private static int _passed;
        private static int _failed;
        private static int _skipped;
        private static readonly StringBuilder Report = new StringBuilder();

        public static int Run(string outputPath)
        {
            _passed = 0;
            _failed = 0;
            _skipped = 0;
            Report.Length = 0;

            TestDefaults();
            TestSerialization();
            TestSettingsStore();
            TestStartupSupport();
            TestHiddenPetShortcut();
            TestPetAnimationAndSizing();
            TestSettingsSizeControl();
            TestTodoRepeat();
            TestVersion5MigrationAndCategories();
            TestProgressAndRewards();
            TestDailyCareAndHumanContent();
            TestCompanionCareAndOutfits();
            TestReminderConfirmationText();
            TestDisplayTopology();
            TestShortcutPresentation();
            TestDrinkReminder();
            TestReminderActions();
            TestActiveTimeAndProtectionGrace();
            TestAwayResetsMovementOnly();
            TestDormAndExpiredMeal();
            TestScheduleRules();
            TestMissedTodo();
            TestProtectionSuppressesNotice();
            TestDailyStatsReset();
            TestRuntimeSensors();

            Report.AppendLine();
            Report.AppendLine("Passed: " + _passed);
            Report.AppendLine("Failed: " + _failed);
            Report.AppendLine("Skipped: " + _skipped);
            File.WriteAllText(outputPath, Report.ToString(), Encoding.UTF8);
            return _failed == 0 ? 0 : 1;
        }

        private static void TestDefaults()
        {
            AppSettings settings = AppSettings.CreateDefault();
            Check("default drink interval", settings.DrinkIntervalMinutes == 45);
            Check("default movement interval", settings.MovementIntervalMinutes == 60);
            Check("meal schedule starts empty", settings.MealSchedules.Count == 0);
            Check("dorm default enabled", settings.DormSchedule.Enabled);
            Check("dorm default time", settings.DormSchedule.TimeText == "00:00");
            Check("default pet size is compact", settings.PetSizePercent == 70);
            Check("default settings version is seven", settings.Version == 7);
            Check(
                "default desk-buddy settings are friendly and optional",
                settings.PetInteractionLevel ==
                    PetInteractionLevel.Normal &&
                settings.SelectedPetOutfit == PetOutfit.Classic &&
                settings.EveningRitualEnabled &&
                settings.EmotionWeather == EmotionWeather.None &&
                settings.WorryNotes.Count == 0 &&
                settings.EveningRitualHistory.Count == 0);

            AppSettings legacy = AppSettings.CreateDefault();
            legacy.Version = 1;
            legacy.DormSchedule.TimeText = "22:30";
            legacy.Normalize();
            Check(
                "legacy dorm time migrates to midnight",
                legacy.Version == 7 &&
                legacy.DormSchedule.TimeText == "00:00");

            AppSettings hiddenLegacy = AppSettings.CreateDefault();
            hiddenLegacy.Version = 2;
            hiddenLegacy.PetVisible = false;
            hiddenLegacy.ManualMeetingMode = true;
            hiddenLegacy.Normalize();
            Check(
                "v0.2 hidden pet is restored once",
                hiddenLegacy.Version == 7 &&
                hiddenLegacy.PetVisible &&
                !hiddenLegacy.ManualMeetingMode);

            AppSettings sizeLegacy = AppSettings.CreateDefault();
            sizeLegacy.Version = 3;
            sizeLegacy.PetVisible = false;
            sizeLegacy.PetSizePercent = 0;
            sizeLegacy.Normalize();
            Check(
                "v0.3 receives compact size without changing visibility",
                sizeLegacy.Version == 7 &&
                sizeLegacy.PetSizePercent == 70 &&
                !sizeLegacy.PetVisible);

            sizeLegacy.PetSizePercent = 999;
            sizeLegacy.Normalize();
            Check(
                "pet size is clamped to supported range",
                sizeLegacy.PetSizePercent == 120);
        }

        private static void TestSerialization()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.Todos.Add(new TodoItem
            {
                Title = "测试待办",
                DueLocal = DateTime.Today.AddHours(20)
            });
            settings.PetSizePercent = 90;
            settings.PetInteractionLevel = PetInteractionLevel.Playful;
            settings.SelectedPetOutfit = PetOutfit.GraduateBag;
            settings.Stats.GrowthPoints = 80;
            settings.EmotionWeather = EmotionWeather.Foggy;
            settings.EmotionWeatherDateKey =
                DateTime.Today.ToString("yyyy-MM-dd");
            settings.WorryNotes.Add(new WorryNote
            {
                Text = "先放在这里",
                Kind = "worry"
            });
            settings.PetHeadPatCount = 9;
            settings.PetBodyTapCount = 3;
            settings.EveningRitualHistory.Add(
                new EveningRitualEntry
                {
                    Accomplished = "完成了一件事",
                    ClosingMessage = "今天到这里。"
                });
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(AppSettings));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, settings);
                stream.Position = 0;
                AppSettings copy =
                    (AppSettings)serializer.ReadObject(stream);
                copy.Normalize();
                Check("settings JSON round trip", copy.Todos.Count == 1);
                Check("Chinese text round trip", copy.Todos[0].Title == "测试待办");
                Check("pet size JSON round trip", copy.PetSizePercent == 90);
                Check(
                    "companion settings JSON round trip",
                    copy.PetInteractionLevel ==
                        PetInteractionLevel.Playful &&
                    copy.SelectedPetOutfit ==
                        PetOutfit.GraduateBag &&
                    copy.EmotionWeather == EmotionWeather.Foggy &&
                    copy.WorryNotes.Count == 1 &&
                    copy.WorryNotes[0].Text == "先放在这里" &&
                    copy.EveningRitualHistory.Count == 1 &&
                    copy.PetHeadPatCount == 9 &&
                    copy.PetBodyTapCount == 3);
            }
        }

        private static void TestTodoRepeat()
        {
            TodoItem daily = new TodoItem
            {
                DueLocal = DateTime.Now.AddDays(-2),
                Repeat = TodoRepeat.Daily
            };
            daily.AdvanceAfterCompletion();
            Check("daily todo advances to future", daily.DueLocal > DateTime.Now);
            Check("repeating todo stays active", !daily.Completed);

            TodoItem once = new TodoItem
            {
                Repeat = TodoRepeat.None
            };
            once.AdvanceAfterCompletion();
            Check("one-time todo completes", once.Completed);

            TodoItem weekly = new TodoItem
            {
                DueLocal = DateTime.Now.AddDays(-15),
                Repeat = TodoRepeat.Weekly
            };
            weekly.AdvanceAfterCompletion();
            Check(
                "weekly todo advances by weeks to the future",
                weekly.DueLocal > DateTime.Now &&
                !weekly.Completed);

            TodoItem weekdays = new TodoItem
            {
                DueLocal = DateTime.Now.AddDays(-7),
                Repeat = TodoRepeat.Weekdays
            };
            weekdays.AdvanceAfterCompletion();
            Check(
                "weekday todo skips weekends",
                weekdays.DueLocal > DateTime.Now &&
                weekdays.DueLocal.DayOfWeek != DayOfWeek.Saturday &&
                weekdays.DueLocal.DayOfWeek != DayOfWeek.Sunday);
        }

        private static void TestVersion5MigrationAndCategories()
        {
            AppSettings legacy = AppSettings.CreateDefault();
            legacy.Version = 4;
            legacy.Stats.GrowthPoints = 12;
            legacy.FreedomPoints = 0;
            legacy.TotalFreedomEarned = 0;
            legacy.Todos.Add(new TodoItem
            {
                Title = "旧待办",
                CategoryId = "",
                Difficulty = TodoDifficulty.Simple
            });
            legacy.Normalize();
            Check("v0.4 settings migrate through v0.7", legacy.Version == 7);
            Check(
                "legacy growth becomes spendable freedom",
                legacy.FreedomPoints == 12 &&
                legacy.TotalFreedomEarned == 12);
            Check(
                "legacy todo receives normal difficulty",
                legacy.Todos[0].Difficulty == TodoDifficulty.Normal);
            Check(
                "legacy todo moves to uncategorized",
                legacy.Todos[0].CategoryId == "uncategorized");
            Check(
                "default todo categories are available",
                legacy.TodoCategories.Count >= 7 &&
                legacy.FindCategory("study").Name == "学习");

            TodoCategory custom = new TodoCategory
            {
                Name = "实验室",
                ColorHex = "#79BDE3",
                SortOrder = 10
            };
            legacy.TodoCategories.Add(custom);
            legacy.Todos[0].CategoryId = custom.Id;
            legacy.DeleteCategory(custom.Id);
            Check(
                "deleting category reassigns its todos",
                legacy.Todos[0].CategoryId == "uncategorized");
            int categoryCount = legacy.TodoCategories.Count;
            legacy.DeleteCategory("uncategorized");
            Check(
                "uncategorized fallback cannot be deleted",
                legacy.TodoCategories.Count == categoryCount);
        }

        private static void TestProgressAndRewards()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.Stats.DrinkCount++;
            ProgressService.Award(
                settings,
                ReminderKind.Drink,
                TodoDifficulty.Simple);
            settings.Stats.MovementCount++;
            ProgressService.Award(
                settings,
                ReminderKind.Movement,
                TodoDifficulty.Simple);
            settings.Stats.MealCount++;
            ProgressService.Award(
                settings,
                ReminderKind.Meal,
                TodoDifficulty.Simple);
            settings.Stats.DormCount++;
            ProgressService.Award(
                settings,
                ReminderKind.Dorm,
                TodoDifficulty.Simple);
            settings.Stats.TodoCount++;
            int finalAward = ProgressService.Award(
                settings,
                ReminderKind.Todo,
                TodoDifficulty.Important);
            Check(
                "behavior rewards use configured values",
                ProgressService.PointsFor(
                    ReminderKind.Drink,
                    TodoDifficulty.Simple) == 1 &&
                ProgressService.PointsFor(
                    ReminderKind.Movement,
                    TodoDifficulty.Simple) == 2 &&
                ProgressService.PointsFor(
                    ReminderKind.Todo,
                    TodoDifficulty.Simple) == 1 &&
                ProgressService.PointsFor(
                    ReminderKind.Todo,
                    TodoDifficulty.Normal) == 3 &&
                ProgressService.PointsFor(
                    ReminderKind.Todo,
                    TodoDifficulty.Important) == 5);
            Check(
                "balanced day awards one three-point bonus",
                finalAward == 8 &&
                settings.Stats.GrowthPoints == 15 &&
                settings.FreedomPoints == 15);
            long beforeRepeated = settings.FreedomPoints;
            for (int i = 0; i < 2000; i++)
            {
                ProgressService.Award(
                    settings,
                    ReminderKind.Drink,
                    TodoDifficulty.Simple);
            }
            Check(
                "behavior points have no daily cap",
                settings.FreedomPoints == beforeRepeated + 2000);

            RewardItem reward = new RewardItem
            {
                Title = "测试奖励",
                FreedomCost = 10,
                Repeatable = false
            };
            settings.Rewards.Add(reward);
            string error;
            bool gated = !ProgressService.TryRedeem(
                settings,
                reward,
                out error);
            Check(
                "paid rewards require four daily care cards",
                gated && error.Contains("四张"));
            settings.Stats.CompleteCare(DailyCareKind.Body);
            settings.Stats.CompleteCare(DailyCareKind.Feelings);
            settings.Stats.CompleteCare(DailyCareKind.Boundary);
            settings.Stats.CompleteCare(DailyCareKind.Kindness);
            bool redeemed = ProgressService.TryRedeem(
                settings,
                reward,
                out error);
            Check(
                "reward redemption spends freedom only",
                redeemed &&
                settings.FreedomPoints == beforeRepeated + 1990 &&
                settings.Stats.GrowthPoints == beforeRepeated + 2000);
            Check(
                "redemption history is recorded",
                settings.RewardHistory.Count == 1 &&
                settings.RewardHistory[0].RewardTitle == "测试奖励");
            Check(
                "non-repeatable reward is removed",
                !settings.Rewards.Contains(reward));

            settings.FreedomPoints = 0;
            RewardItem paidReward =
                settings.Rewards.First(x => !x.IsFree);
            bool rejected = !ProgressService.TryRedeem(
                settings,
                paidReward,
                out error);
            Check(
                "insufficient freedom blocks redemption",
                rejected && !string.IsNullOrWhiteSpace(error));
            Check(
                "default reward library is diverse and non-monetary",
                settings.Rewards.Select(x => x.Category).Distinct().Count() >= 6 &&
                !settings.Rewards.Any(x =>
                    (x.Title + x.Description).Contains("元") ||
                    (x.Title + x.Description).Contains("¥")));
        }

        private static void TestDailyCareAndHumanContent()
        {
            AppSettings settings = AppSettings.CreateDefault();
            Check(
                "default reward library is richly populated",
                settings.Rewards.Count >= 75);
            RewardItem[] free = settings.Rewards
                .Where(x => x.IsFree)
                .ToArray();
            Check(
                "self-first and comfort libraries are free",
                free.Length >= 20 &&
                free.All(x => x.FreedomCost == 0) &&
                free.All(x =>
                    x.Category == RewardCategory.SelfFirst ||
                    x.Category == RewardCategory.Comfort));
            Check(
                "free care rewards have no duration",
                free.All(x => x.DurationMinutes == 0));
            Check(
                "all rest rewards have no duration",
                settings.Rewards
                    .Where(x => x.Category == RewardCategory.Rest)
                    .All(x => x.DurationMinutes == 0));
            Check(
                "food reward library includes requested treats",
                settings.Rewards.Any(x => x.Title.Contains("水果")) &&
                settings.Rewards.Any(x => x.Title.Contains("咖啡")) &&
                settings.Rewards.Any(x => x.Title.Contains("朋友们点一次外卖")));
            string allRewards = string.Join(
                "\n",
                settings.Rewards.Select(x => x.Title + x.Description));
            Check(
                "removed awkward rewards stay removed",
                !allRewards.Contains("太敏感") &&
                !allRewards.Contains("实验室朋友点") &&
                !allRewards.Contains("过度自责") &&
                !allRewards.Contains("机器人"));

            RewardItem freeReward = free[0];
            long freedomBefore = settings.FreedomPoints;
            string error;
            bool freeRedeemed = ProgressService.TryRedeem(
                settings,
                freeReward,
                out error);
            Check(
                "free care reward works before daily gate",
                freeRedeemed &&
                settings.FreedomPoints == freedomBefore &&
                settings.RewardHistory[0].FreedomCost == 0);

            Check(
                "four daily care cards start incomplete",
                CareContent.DailyCards.Length == 4 &&
                !settings.Stats.AllCareCompleted);
            foreach (DailyCareCard card in CareContent.DailyCards)
            {
                settings.Stats.CompleteCare(card.Kind);
            }
            Check(
                "four daily care cards unlock rewards",
                settings.Stats.AllCareCompleted &&
                settings.Stats.CareCompletedCount == 4);
            settings.Stats.ReflectionStopped = true;
            settings.Stats.MoodKey = "委屈";
            settings.Stats.MoodNote = "今天有点难受";
            settings.Stats.DateKey = DateTime.Today
                .AddDays(-1)
                .ToString("yyyy-MM-dd");
            settings.Stats.EnsureToday();
            Check(
                "daily care state resets without debt",
                !settings.Stats.AllCareCompleted &&
                !settings.Stats.ReflectionStopped &&
                settings.Stats.MoodKey == "" &&
                settings.Stats.MoodNote == "");
            Check(
                "human care content is sufficiently varied",
                CareContent.PermissionSlips.Length >= 25 &&
                CareContent.BoundaryPhrases.Length >= 10 &&
                CareContent.MoodKeys.Length >= 7);

            AppSettings migration = AppSettings.CreateDefault();
            RewardItem custom = new RewardItem
            {
                Title = "我自己的奖励",
                BuiltIn = false
            };
            migration.Rewards.Add(custom);
            migration.Version = 5;
            migration.Normalize();
            Check(
                "v0.5 migration keeps custom rewards",
                migration.Version == 7 &&
                migration.Rewards.Any(x => x.Title == "我自己的奖励"));
            Check(
                "v0.5 built-ins migrate to enriched library",
                migration.Rewards.Count >= 76 &&
                migration.Rewards.Any(x => x.Id == "vent-01"));
        }

        private static void TestCompanionCareAndOutfits()
        {
            AppSettings migration = AppSettings.CreateDefault();
            migration.Version = 6;
            migration.PetInteractionLevel =
                PetInteractionLevel.Playful;
            migration.EveningRitualEnabled = false;
            migration.WorryNotes = null;
            migration.EveningRitualHistory = null;
            migration.EmotionWeatherDateKey = null;
            migration.Normalize();
            Check(
                "v0.6 companion settings migrate safely",
                migration.Version == 7 &&
                migration.PetInteractionLevel ==
                    PetInteractionLevel.Normal &&
                migration.EveningRitualEnabled &&
                migration.WorryNotes != null &&
                migration.EveningRitualHistory != null &&
                migration.EmotionWeatherDateKey == "");

            Check(
                "emotion weather offers six non-diagnostic states",
                CareContent.EmotionWeatherCards.Length == 6 &&
                CareContent.FindWeather(
                    EmotionWeather.Foggy).Title == "起雾" &&
                CareContent.FindWeather(
                    EmotionWeather.Thunderstorm).Response.Contains(
                        "批斗会"));
            Check(
                "desk-buddy response libraries are varied",
                CareContent.QuietBreakResponses.Length >= 4 &&
                CareContent.FunnyResponses.Length >= 6 &&
                CareContent.FriendSupportResponses.Length >= 8 &&
                CareContent.PaperDismissalResponses.Length >= 5 &&
                CareContent.ClosingPraise.Length >= 4 &&
                CareContent.ClosingFunny.Length >= 4 &&
                CareContent.ClosingQuiet.Length >= 4);
            string friendshipCopy = string.Join(
                "\n",
                CareContent.EmotionWeatherCards
                    .Select(x => x.Response)
                    .Concat(CareContent.QuietBreakResponses)
                    .Concat(CareContent.FunnyResponses)
                    .Concat(CareContent.FriendSupportResponses)
                    .Concat(CareContent.MorningWhispers)
                    .Concat(CareContent.DayWhispers)
                    .Concat(CareContent.NoonWhispers)
                    .Concat(CareContent.EveningWhispers)
                    .Concat(CareContent.LateNightWhispers));
            string[] dependencyPhrases =
            {
                "永远陪着你",
                "只属于你",
                "不会离开",
                "你只需要我",
                "我会一直等你",
                "站在我这边",
                "我在这里",
                "尾巴放在旁边"
            };
            Check(
                "desk-buddy copy avoids romantic or dependent framing",
                dependencyPhrases.All(
                    x => !friendshipCopy.Contains(x)));
            Check(
                "friendship positioning is explicit",
                CareContent.StationName == "猫猫休息站" &&
                CareContent.FriendshipPositioning.Contains("桌面搭子"));
            Check(
                "time-aware whispers cover the whole day",
                CareContent.WhispersForHour(2).Length >= 4 &&
                CareContent.WhispersForHour(8).Length >= 3 &&
                CareContent.WhispersForHour(12).Length >= 3 &&
                CareContent.WhispersForHour(16).Length >= 4 &&
                CareContent.WhispersForHour(21).Length >= 3);
            Check(
                "boundary phrases support three tones",
                CareContent.BoundarySet("gentle").Length >= 4 &&
                CareContent.BoundarySet("neutral").Length >= 4 &&
                CareContent.BoundarySet("firm").Length >= 4);

            Check(
                "outfits unlock without spending growth points",
                PetOutfitService.Options.Length == 5 &&
                PetOutfitService.IsUnlocked(
                    PetOutfit.Classic,
                    0) &&
                !PetOutfitService.IsUnlocked(
                    PetOutfit.ApricotScarf,
                    14) &&
                PetOutfitService.IsUnlocked(
                    PetOutfit.ApricotScarf,
                    15) &&
                !PetOutfitService.IsUnlocked(
                    PetOutfit.SafetyGear,
                    99) &&
                PetOutfitService.IsUnlocked(
                    PetOutfit.SafetyGear,
                    100));

            AppSettings care = AppSettings.CreateDefault();
            long growthBefore = care.Stats.GrowthPoints;
            long freedomBefore = care.FreedomPoints;
            care.EmotionWeather = EmotionWeather.Drizzle;
            care.EmotionWeatherDateKey =
                DateTime.Today.ToString("yyyy-MM-dd");
            care.WorryNotes.Add(new WorryNote
            {
                Text = "今天有一点委屈。"
            });
            care.EveningRitualHistory.Add(
                new EveningRitualEntry
                {
                    Accomplished = "今天走到了这里",
                    ClosingMessage =
                        CareContent.ClosingFor("quiet", new Random(7))
                });
            Check(
                "companion activities do not create points",
                care.Stats.GrowthPoints == growthBefore &&
                care.FreedomPoints == freedomBefore);

            care.EmotionWeatherDateKey = DateTime.Today
                .AddDays(-1)
                .ToString("yyyy-MM-dd");
            care.Normalize();
            Check(
                "emotion weather resets on a new day",
                care.EmotionWeather == EmotionWeather.None &&
                care.EmotionWeatherDateKey == "");

            CareCompanionWindow window =
                new CareCompanionWindow(care);
            try
            {
                window.ShowPage("weather");
                window.ShowPage("worry");
                window.ShowPage("reframe");
                window.ShowPage("ritual");
                Check(
                    "all desk-buddy pages construct",
                    window.Title.Contains(CareContent.StationName));
            }
            finally
            {
                window.ForceClose();
            }
        }

        private static void TestReminderConfirmationText()
        {
            Check(
                "reminder confirmation contains exactly fifty phrases",
                ReminderConfirmationText.PhraseCount == 50);
            string first = ReminderConfirmationText.Next(
                ReminderActionKind.Complete,
                0);
            string second = ReminderConfirmationText.Next(
                ReminderActionKind.Complete,
                0);
            Check(
                "confirmation avoids immediate repetition",
                first != second);
            string snooze = ReminderConfirmationText.Next(
                ReminderActionKind.Snooze,
                30);
            Check(
                "snooze confirmation includes selected delay",
                snooze.Contains("30"));
            string skip = ReminderConfirmationText.Next(
                ReminderActionKind.Skip,
                0);
            Check(
                "skip confirmation has action-specific wording",
                skip.Contains("跳过") ||
                skip.Contains("不做") ||
                skip.Contains("退场") ||
                skip.Contains("收起来") ||
                skip.Contains("放回") ||
                skip.Contains("放过") ||
                skip.Contains("不安排") ||
                skip.Contains("先到这里") ||
                skip.Contains("不再提醒"));
        }

        private static void TestDisplayTopology()
        {
            DisplayArea primary = new DisplayArea(
                "primary",
                new PixelBounds(0, 0, 1920, 1040),
                true,
                1,
                1);
            DisplayArea right = new DisplayArea(
                "right-150",
                new PixelBounds(1920, 0, 2560, 1400),
                false,
                1.5,
                1.5);
            DisplayArea left = new DisplayArea(
                "left-125",
                new PixelBounds(-1280, 200, 1280, 720),
                false,
                1.25,
                1.25);
            DisplayTopology topology = new DisplayTopology(
                new[] { primary, right, left });
            Check(
                "negative-coordinate monitor is detected",
                topology.FindForWindow(
                    new PixelBounds(-900, 400, 140, 140)) == left);
            PixelPoint clamped = topology.ClampTopLeft(
                new PixelPoint(4500, 1500),
                180,
                180,
                right);
            Check(
                "window clamps inside chosen monitor work area",
                clamped.X == 4300 &&
                clamped.Y == 1220);

            List<PixelPoint> route = topology.BuildRoute(
                left,
                right,
                new PixelPoint(2100, 300),
                160,
                160);
            Check(
                "cross-screen route traverses adjacent displays",
                route.Count >= 5);
            bool routeSafe = true;
            foreach (PixelPoint point in route)
            {
                if (!topology.IsInsideDesktop(
                    new PixelBounds(point.X, point.Y, 160, 160)))
                {
                    routeSafe = false;
                    break;
                }
            }
            Check(
                "cross-screen route waypoints stay on desktop",
                routeSafe);

            DisplayArea isolated = new DisplayArea(
                "isolated",
                new PixelBounds(6000, 0, 1920, 1080),
                false,
                1,
                1);
            DisplayTopology withGap = new DisplayTopology(
                new[] { primary, isolated });
            Check(
                "virtual desktop gaps are not crossed",
                withGap.BuildRoute(
                    primary,
                    isolated,
                    new PixelPoint(6100, 100),
                    160,
                    160).Count == 0);
            Check(
                "mixed DPI identity is retained",
                topology.FindByDevice("right-150").DpiScaleX == 1.5);

            DisplayArea top = new DisplayArea(
                "top",
                new PixelBounds(0, -900, 1600, 900),
                false,
                1,
                1);
            DisplayArea bottom = new DisplayArea(
                "bottom",
                new PixelBounds(0, 1040, 1600, 900),
                false,
                1,
                1);
            Check(
                "top and bottom monitor paths are supported",
                DisplayTopology.CanCross(
                    primary.WorkArea,
                    top.WorkArea,
                    160,
                    160) &&
                DisplayTopology.CanCross(
                    primary.WorkArea,
                    bottom.WorkArea,
                    160,
                    160));

            DisplayArea promoted = new DisplayArea(
                "right-150",
                right.WorkArea,
                true,
                1.5,
                1.5);
            DisplayTopology switchedPrimary = new DisplayTopology(
                new[] { promoted, left });
            Check(
                "primary monitor switching is recognized",
                switchedPrimary.Primary.DeviceName == "right-150");
            Check(
                "disconnected monitor identity falls back safely",
                switchedPrimary.FindByDevice("primary") == null &&
                switchedPrimary.Primary != null);

            PixelBounds popupArea =
                new PixelBounds(-1280, 200, 1280, 720);
            PixelPoint popup = ReminderCardWindow.CalculatePosition(
                new PixelBounds(-1270, 210, 140, 140),
                new PixelBounds(0, 0, 430, 224),
                popupArea);
            Check(
                "reminder card stays on pet monitor",
                popup.X >= popupArea.Left + 12 &&
                popup.X + 430 <= popupArea.Right - 12 &&
                popup.Y >= popupArea.Top + 12 &&
                popup.Y + 224 <= popupArea.Bottom - 12);
        }

        private static void TestShortcutPresentation()
        {
            Check(
                "default shortcut is presented clearly",
                PetWindow.FormatHotKey(3, 0x4D) == "Ctrl + Alt + M");
            Check(
                "fallback shortcut is presented clearly",
                PetWindow.FormatHotKey(6, 0x4D) == "Ctrl + Shift + M");
        }

        private static void TestSettingsStore()
        {
            string folder = Path.Combine(
                Path.GetTempPath(),
                "XieYiMiao-self-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                SettingsStore store = new SettingsStore(folder);
                AppSettings settings = AppSettings.CreateDefault();
                settings.DrinkIntervalMinutes = 37;
                settings.Todos.Add(new TodoItem
                {
                    Title = "保存测试",
                    DueLocal = DateTime.Today.AddHours(18)
                });
                store.Save(settings);
                AppSettings loaded = store.Load();
                Check(
                    "settings store writes and loads",
                    loaded.DrinkIntervalMinutes == 37 &&
                    loaded.Todos.Count == 1);
                Check(
                    "atomic settings file exists",
                    File.Exists(Path.Combine(folder, "settings.json")));
            }
            finally
            {
                if (Directory.Exists(folder) &&
                    folder.StartsWith(
                        Path.GetTempPath(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        private static void TestStartupSupport()
        {
            string installed = StartupManager.InstalledExecutablePath;
            string localData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            Check(
                "startup install path stays in LocalAppData",
                installed.StartsWith(
                    localData,
                    StringComparison.OrdinalIgnoreCase) &&
                installed.EndsWith(
                    Path.Combine("歇一喵", "歇一喵.exe"),
                    StringComparison.OrdinalIgnoreCase));
            Check(
                "startup command quotes paths and passes startup flag",
                StartupManager.BuildStartupCommand(installed) ==
                "\"" + installed + "\" --startup");
            Check(
                "startup path comparison ignores case and trailing slash",
                StartupManager.PathsEqual(
                    Path.Combine(localData, "歇一喵"),
                    Path.Combine(localData, "歇一喵") + "\\"));

            string testKey = @"Software\XieYiMiao-SelfTest-" +
                Guid.NewGuid().ToString("N");
            bool registryRoundTrip = false;
            bool registryTestAvailable = true;
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(testKey))
                {
                    if (key == null)
                    {
                        throw new UnauthorizedAccessException(
                            "The current-user registry key could not be created.");
                    }
                    key.SetValue("startup-test", "ok");
                    registryRoundTrip =
                        string.Equals(
                            key.GetValue("startup-test") as string,
                            "ok",
                            StringComparison.Ordinal);
                }
            }
            catch (UnauthorizedAccessException)
            {
                registryTestAvailable = false;
                Skip(
                    "current-user startup registry is writable",
                    "the test environment blocks registry writes");
            }
            catch (System.Security.SecurityException)
            {
                registryTestAvailable = false;
                Skip(
                    "current-user startup registry is writable",
                    "the test environment blocks registry writes");
            }
            finally
            {
                if (registryTestAvailable)
                {
                    Registry.CurrentUser.DeleteSubKeyTree(testKey, false);
                }
            }
            if (registryTestAvailable)
            {
                Check(
                    "current-user startup registry is writable",
                    registryRoundTrip);
            }

            string installFolder = Path.Combine(
                Path.GetTempPath(),
                "XieYiMiao-startup-test-" +
                Guid.NewGuid().ToString("N"));
            try
            {
                string source =
                    Process.GetCurrentProcess().MainModule.FileName;
                string target =
                    Path.Combine(installFolder, "歇一喵.exe");
                StartupManager.InstallExecutable(source, target);
                Check(
                    "startup executable copy succeeds",
                    File.Exists(target) &&
                    new FileInfo(target).Length ==
                    new FileInfo(source).Length);
            }
            finally
            {
                if (Directory.Exists(installFolder) &&
                    installFolder.StartsWith(
                        Path.GetTempPath(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Delete(installFolder, true);
                }
            }
        }

        private static void TestHiddenPetShortcut()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.PetVisible = false;
            PetWindow pet = new PetWindow(settings);
            int toggleCount = 0;
            pet.ToggleRequested += delegate { toggleCount++; };

            try
            {
                pet.InitializeShortcut();
                Check(
                    "hidden pet creates a native shortcut handle",
                    pet.HasNativeHandle && !pet.IsVisible);
                Check(
                    "hidden pet registers a Windows global hotkey",
                    pet.HotKeyRegistered ||
                    pet.RegisterDiagnosticHotKeyForTest());
                Check(
                    "hidden pet handles the shortcut message",
                    pet.SimulateHotKeyForTest() && toggleCount == 1);
            }
            finally
            {
                pet.Close();
            }

            AppSettings blockerSettings = AppSettings.CreateDefault();
            blockerSettings.PetVisible = false;
            blockerSettings.HotKeyModifiers = 3;
            blockerSettings.HotKeyVirtualKey = 0x7A;
            AppSettings conflictSettings = AppSettings.CreateDefault();
            conflictSettings.PetVisible = false;
            conflictSettings.HotKeyModifiers = 3;
            conflictSettings.HotKeyVirtualKey = 0x7A;
            PetWindow blocker = new PetWindow(blockerSettings);
            PetWindow conflicted = new PetWindow(conflictSettings);
            try
            {
                blocker.InitializeShortcut();
                conflicted.InitializeShortcut();
                Check(
                    "occupied shortcut receives a working fallback",
                    blocker.HotKeyRegistered &&
                    conflicted.HotKeyRegistered &&
                    conflicted.IsUsingFallbackHotKey &&
                    conflicted.ActiveHotKeyText !=
                        PetWindow.FormatHotKey(3, 0x7A));
            }
            finally
            {
                conflicted.Close();
                blocker.Close();
            }
        }

        private static void TestPetAnimationAndSizing()
        {
            Check(
                "walk animation embeds eight distinct frame slots",
                CatControl.AvailableWalkFrameCount == 8);
            Check(
                "companion animation embeds eight approved poses",
                CatControl.AvailableCareFrameCount == 8);
            Check(
                "outfit animation embeds four unlockable looks",
                CatControl.AvailableOutfitFrameCount == 4);

            CatControl cat = new CatControl { Mood = CatMood.Walk };
            int first = cat.CurrentWalkFrameIndex;
            cat.Tick(0.13);
            int second = cat.CurrentWalkFrameIndex;
            cat.Tick(0.12);
            int third = cat.CurrentWalkFrameIndex;
            Check(
                "walk animation advances through multiple frames",
                first == 0 && second == 1 && third == 2);
            Check(
                "performance mode lowers animation rate on battery",
                PetWindow.AnimationIntervalMilliseconds(true, true) == 80);
            Check(
                "normal animation rate remains smooth",
                PetWindow.AnimationIntervalMilliseconds(true, false) == 33 &&
                PetWindow.AnimationIntervalMilliseconds(false, true) == 33);

            AppSettings settings = AppSettings.CreateDefault();
            settings.PetVisible = false;
            settings.PetSizePercent = 50;
            settings.PetMonitorDevice = "remembered-display";
            settings.PetAnchorX = 0.72;
            settings.PetAnchorY = 0.64;
            PetWindow pet = new PetWindow(settings);
            try
            {
                pet.ApplySettings();
                Check(
                    "pet minimum size applies",
                    Math.Abs(pet.Width - 95) < 0.5 &&
                    Math.Abs(pet.Height - 95) < 0.5);
                Check(
                    "hidden resize preserves remembered monitor position",
                    settings.PetMonitorDevice == "remembered-display" &&
                    Math.Abs(settings.PetAnchorX - 0.72) < 0.001 &&
                    Math.Abs(settings.PetAnchorY - 0.64) < 0.001);

                settings.PetSizePercent = 120;
                settings.Stats.GrowthPoints = 80;
                settings.SelectedPetOutfit = PetOutfit.GraduateBag;
                pet.ApplySettings();
                Check(
                    "pet maximum size applies",
                    Math.Abs(pet.Width - 228) < 0.5 &&
                    Math.Abs(pet.Height - 228) < 0.5);
                Check(
                    "pet applies selected unlocked outfit",
                    pet.CurrentOutfit == PetOutfit.GraduateBag);
                pet.React(CatMood.Note, 2);
                Check(
                    "pet accepts companion reaction states",
                    pet.CurrentMood == CatMood.Note);
            }
            finally
            {
                pet.Close();
            }
        }

        private static void TestSettingsSizeControl()
        {
            AppSettings settings = AppSettings.CreateDefault();
            SettingsWindow window = new SettingsWindow(
                settings,
                Path.GetTempPath());
            int applied = 0;
            window.SettingsApplied += delegate { applied++; };
            try
            {
                window.ShowPreviewPage("pet");
                Check(
                    "pet size slider reflects saved value",
                    Math.Abs(
                        window.PetSizeControlValue -
                        settings.PetSizePercent) < 0.5);
                window.PetSizeControlValue = 90;
                Check(
                    "pet size slider applies changes live",
                    settings.PetSizePercent == 90 &&
                    applied == 1);
            }
            finally
            {
                window.ForceClose();
            }
        }

        private static void TestDrinkReminder()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.MovementEnabled = false;
            settings.DrinkActiveSeconds = 45 * 60;
            ReminderEngine engine = Engine(settings, new FakeActivity());
            engine.Tick(DateTime.Now, 1);
            Check(
                "drink reminder becomes due",
                engine.CurrentNotice != null &&
                engine.CurrentNotice.Kind == ReminderKind.Drink);
            engine.CompleteCurrent();
            Check("drink completion resets timer", settings.DrinkActiveSeconds == 0);
            Check("drink completion adds footprint", settings.Stats.DrinkCount == 1);
        }

        private static void TestReminderActions()
        {
            DateTime now = DateTime.Now;
            AppSettings drink = AppSettings.CreateDefault();
            drink.MovementEnabled = false;
            drink.DormSchedule.Enabled = false;
            drink.DrinkActiveSeconds = drink.DrinkIntervalMinutes * 60;
            ReminderEngine drinkEngine =
                Engine(drink, new FakeActivity());
            drinkEngine.Tick(now, 0);
            drinkEngine.SnoozeCurrent(10);
            Check(
                "drink snooze clears notice and stores future time",
                drinkEngine.CurrentNotice == null &&
                drink.DrinkSnoozedUntil > DateTime.Now);
            drink.DrinkSnoozedUntil = now.AddSeconds(-1);
            drinkEngine.Tick(now.AddSeconds(1), 0);
            Check(
                "snoozed drink reminder returns when due",
                drinkEngine.CurrentNotice != null &&
                drinkEngine.CurrentNotice.Kind == ReminderKind.Drink);
            drinkEngine.SkipCurrent();
            Check(
                "skipping drink clears and resets interval",
                drinkEngine.CurrentNotice == null &&
                drink.DrinkActiveSeconds == 0);

            AppSettings movement = AppSettings.CreateDefault();
            movement.DrinkEnabled = false;
            movement.DormSchedule.Enabled = false;
            movement.MovementActiveSeconds =
                movement.MovementIntervalMinutes * 60;
            ReminderEngine movementEngine =
                Engine(movement, new FakeActivity());
            movementEngine.Tick(now, 0);
            movementEngine.CompleteCurrent();
            Check(
                "movement completion records footprint",
                movement.MovementActiveSeconds == 0 &&
                movement.Stats.MovementCount == 1);

            AppSettings todoSettings = AppSettings.CreateDefault();
            todoSettings.DrinkEnabled = false;
            todoSettings.MovementEnabled = false;
            todoSettings.DormSchedule.Enabled = false;
            TodoItem todo = new TodoItem
            {
                Title = "完成测试待办",
                DueLocal = now.AddMinutes(-1)
            };
            todoSettings.Todos.Add(todo);
            ReminderEngine todoEngine =
                Engine(todoSettings, new FakeActivity());
            todoEngine.Tick(now, 0);
            todoEngine.CompleteCurrent();
            Check(
                "todo completion closes item and records footprint",
                todo.Completed &&
                todoSettings.Stats.TodoCount == 1);

            AppSettings paused = AppSettings.CreateDefault();
            paused.MovementEnabled = false;
            paused.DormSchedule.Enabled = false;
            paused.DrinkActiveSeconds =
                paused.DrinkIntervalMinutes * 60;
            ReminderEngine pausedEngine =
                Engine(paused, new FakeActivity());
            pausedEngine.PauseFor(TimeSpan.FromMinutes(10));
            pausedEngine.Tick(now, 0);
            Check(
                "pause suppresses due reminders",
                pausedEngine.CurrentNotice == null &&
                paused.PausedUntil > DateTime.Now);

            AppSettings priority = AppSettings.CreateDefault();
            priority.MovementEnabled = false;
            priority.DormSchedule.Enabled = false;
            priority.DrinkActiveSeconds =
                priority.DrinkIntervalMinutes * 60;
            priority.Todos.Add(new TodoItem
            {
                Title = "优先待办",
                DueLocal = now.AddMinutes(-1)
            });
            ReminderEngine priorityEngine =
                Engine(priority, new FakeActivity());
            priorityEngine.Tick(now, 0);
            Check(
                "todo takes priority over interval reminder",
                priorityEngine.CurrentNotice != null &&
                priorityEngine.CurrentNotice.Kind == ReminderKind.Todo);

            AppSettings quiet = AppSettings.CreateDefault();
            quiet.MovementEnabled = false;
            quiet.DormSchedule.Enabled = false;
            quiet.WarmMessages = false;
            quiet.DrinkActiveSeconds =
                quiet.DrinkIntervalMinutes * 60;
            ReminderEngine quietEngine =
                Engine(quiet, new FakeActivity());
            quietEngine.Tick(now, 0);
            Check(
                "warm reminder text can be disabled",
                quietEngine.CurrentNotice != null &&
                quietEngine.CurrentNotice.Message == "");
        }

        private static void TestActiveTimeAndProtectionGrace()
        {
            DateTime now = DateTime.Now;
            AppSettings active = AppSettings.CreateDefault();
            active.DormSchedule.Enabled = false;
            ReminderEngine activeEngine =
                Engine(active, new FakeActivity());
            activeEngine.Tick(now, 5);
            Check(
                "actual active use increments both interval timers",
                Math.Abs(active.DrinkActiveSeconds - 5) < 0.1 &&
                Math.Abs(active.MovementActiveSeconds - 5) < 0.1);

            AppSettings meeting = AppSettings.CreateDefault();
            meeting.DormSchedule.Enabled = false;
            FakeActivity meetingActivity = new FakeActivity
            {
                IdleSeconds = 10 * 60
            };
            FakeProtection protection = new FakeProtection
            {
                Protected = true
            };
            ReminderEngine meetingEngine = new ReminderEngine(
                meeting,
                meetingActivity,
                protection);
            meetingEngine.Tick(now, 5);
            Check(
                "meeting viewing still counts as active computer use",
                Math.Abs(meeting.DrinkActiveSeconds - 5) < 0.1 &&
                Math.Abs(meeting.MovementActiveSeconds - 5) < 0.1);

            meeting.DrinkActiveSeconds =
                meeting.DrinkIntervalMinutes * 60;
            protection.Protected = false;
            meetingActivity.IdleSeconds = 0;
            meetingEngine.Tick(now.AddSeconds(1), 0);
            Check(
                "leaving protection starts a reminder grace period",
                meetingEngine.CurrentNotice == null);
            meetingEngine.Tick(
                now.AddMinutes(
                    meeting.ProtectionGraceMinutes).AddSeconds(2),
                0);
            Check(
                "due reminder appears after protection grace",
                meetingEngine.CurrentNotice != null);
        }

        private static void TestAwayResetsMovementOnly()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.DrinkActiveSeconds = 900;
            settings.MovementActiveSeconds = 1200;
            FakeActivity activity = new FakeActivity
            {
                IdleSeconds = 10 * 60
            };
            ReminderEngine engine = Engine(settings, activity);
            engine.Tick(DateTime.Now, 1);
            Check(
                "away resets movement timer",
                settings.MovementActiveSeconds == 0);
            Check(
                "away does not reset drink timer",
                settings.DrinkActiveSeconds == 900);
        }

        private static void TestDormAndExpiredMeal()
        {
            DateTime now = DateTime.Now;
            AppSettings settings = AppSettings.CreateDefault();
            settings.DrinkEnabled = false;
            settings.MovementEnabled = false;
            settings.DormSchedule.TimeText = now.AddMinutes(-1).ToString("HH:mm");
            ReminderEngine engine = Engine(settings, new FakeActivity());
            engine.Tick(now, 1);
            Check(
                "dorm schedule triggers",
                engine.CurrentNotice != null &&
                engine.CurrentNotice.Kind == ReminderKind.Dorm);

            AppSettings expired = AppSettings.CreateDefault();
            expired.DrinkEnabled = false;
            expired.MovementEnabled = false;
            expired.DormSchedule.Enabled = false;
            expired.MealSchedules.Add(new ScheduleEntry
            {
                Label = "午饭",
                TimeText = now.AddHours(-3).ToString("HH:mm"),
                DaysMask = 127,
                Enabled = true
            });
            ReminderEngine expiredEngine = Engine(
                expired,
                new FakeActivity());
            expiredEngine.Tick(now, 1);
            Check(
                "meal reminder expires after two hours",
                expiredEngine.CurrentNotice == null);
        }

        private static void TestScheduleRules()
        {
            ScheduleEntry mondayOnly = new ScheduleEntry
            {
                DaysMask = 1
            };
            Check(
                "schedule weekday mask includes Monday",
                mondayOnly.AppliesTo(new DateTime(2026, 7, 27)));
            Check(
                "schedule weekday mask excludes Tuesday",
                !mondayOnly.AppliesTo(new DateTime(2026, 7, 28)));

            DateTime now = DateTime.Now;
            AppSettings meal = AppSettings.CreateDefault();
            meal.DrinkEnabled = false;
            meal.MovementEnabled = false;
            meal.DormSchedule.Enabled = false;
            meal.MealSchedules.Add(new ScheduleEntry
            {
                Label = "测试用餐",
                TimeText = now.AddMinutes(-1).ToString("HH:mm"),
                DaysMask = 127,
                Enabled = true
            });
            ReminderEngine engine =
                Engine(meal, new FakeActivity());
            engine.Tick(now, 0);
            Check(
                "meal schedule raises correct reminder",
                engine.CurrentNotice != null &&
                engine.CurrentNotice.Kind == ReminderKind.Meal);
            engine.CompleteCurrent();
            engine.Tick(now.AddMinutes(1), 0);
            Check(
                "schedule triggers only once per date",
                engine.CurrentNotice == null);
        }

        private static void TestMissedTodo()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.DrinkEnabled = false;
            settings.MovementEnabled = false;
            settings.DormSchedule.Enabled = false;
            settings.Todos.Add(new TodoItem
            {
                Title = "已错过的待办",
                DueLocal = DateTime.Now.AddDays(-1)
            });
            ReminderEngine engine = Engine(settings, new FakeActivity());
            engine.Tick(DateTime.Now, 1);
            Check(
                "missed todo remains due",
                engine.CurrentNotice != null &&
                engine.CurrentNotice.Kind == ReminderKind.Todo);
        }

        private static void TestProtectionSuppressesNotice()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.MovementEnabled = false;
            settings.DrinkActiveSeconds = 45 * 60;
            FakeProtection protection = new FakeProtection
            {
                Protected = true
            };
            ReminderEngine engine = new ReminderEngine(
                settings,
                new FakeActivity(),
                protection);
            engine.Tick(DateTime.Now, 1);
            Check(
                "meeting protection suppresses card",
                engine.CurrentNotice == null);
        }

        private static void TestDailyStatsReset()
        {
            DailyStats stats = new DailyStats
            {
                DateKey = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"),
                DrinkCount = 2,
                MovementCount = 3,
                MealCount = 1,
                DormCount = 1,
                TodoCount = 4,
                GrowthPoints = 12
            };
            stats.EnsureToday();
            Check(
                "daily footprint counters reset on a new date",
                stats.TotalToday == 0 &&
                stats.DateKey == DateTime.Today.ToString("yyyy-MM-dd"));
            Check(
                "growth points persist across dates",
                stats.GrowthPoints == 12);
        }

        private static void TestRuntimeSensors()
        {
            double idle = -1;
            using (ActivityTracker activity = new ActivityTracker())
            {
                idle = activity.IdleSeconds;
            }
            Check(
                "Windows idle-time sensor responds",
                idle >= 0);

            AppSettings settings = AppSettings.CreateDefault();
            settings.ManualMeetingMode = true;
            ProtectionDetector detector = new ProtectionDetector();
            Check(
                "Tencent Meeting process is recognized",
                detector.IsKnownMeetingIdentity("wemeet", ""));
            Check(
                "Feishu meeting title is recognized",
                detector.IsKnownMeetingIdentity(
                    "chrome",
                    "项目讨论 - 飞书会议"));
            Check(
                "ordinary application is not treated as a meeting",
                !detector.IsKnownMeetingIdentity(
                    "notepad",
                    "普通记事本"));
            Check(
                "manual meeting protection is detected",
                detector.IsProtected(settings));

            settings.ManualMeetingMode = false;
            settings.AutoHideFullScreen = false;
            settings.AutoHideMeetings = false;
            Check(
                "protection detector respects disabled automatic modes",
                !detector.IsProtected(settings));
        }

        private static ReminderEngine Engine(
            AppSettings settings,
            FakeActivity activity)
        {
            return new ReminderEngine(
                settings,
                activity,
                new FakeProtection());
        }

        private static void Check(string name, bool condition)
        {
            if (condition)
            {
                _passed++;
                Report.AppendLine("PASS  " + name);
            }
            else
            {
                _failed++;
                Report.AppendLine("FAIL  " + name);
            }
        }

        private static void Skip(string name, string reason)
        {
            _skipped++;
            Report.AppendLine("SKIP  " + name + " (" + reason + ")");
        }
    }
}
