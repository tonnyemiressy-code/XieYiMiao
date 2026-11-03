using System;
using System.Collections.Generic;
using System.Linq;

namespace XieYiMiao
{
    public sealed class ReminderEngine
    {
        private sealed class DeferredNotice
        {
            public ReminderNotice Notice;
            public DateTime DueAt;
        }

        private readonly AppSettings _settings;
        private readonly IActivityState _activity;
        private readonly IProtectionState _protection;
        private readonly List<DeferredNotice> _deferred =
            new List<DeferredNotice>();
        private bool _wasAway;
        private bool _wasProtected;
        private DateTime _returnGraceUntil = DateTime.MinValue;
        private DateTime _protectionGraceUntil = DateTime.MinValue;

        public event Action<ReminderNotice> NoticeRaised;
        public event Action NoticeCleared;
        public event Action SettingsChanged;
        public event Action<bool> ProtectionChanged;

        public ReminderNotice CurrentNotice { get; private set; }
        public bool IsProtected { get; private set; }

        public ReminderEngine(
            AppSettings settings,
            IActivityState activity,
            IProtectionState protection)
        {
            _settings = settings;
            _activity = activity;
            _protection = protection;
        }

        public void Tick(DateTime now, double deltaSeconds)
        {
            _settings.Stats.EnsureToday();
            deltaSeconds = Math.Max(0, Math.Min(10, deltaSeconds));

            bool protectedNow = _protection.IsProtected(_settings);
            if (protectedNow != _wasProtected)
            {
                _wasProtected = protectedNow;
                IsProtected = protectedNow;
                if (!protectedNow)
                {
                    _protectionGraceUntil = now.AddMinutes(
                        _settings.ProtectionGraceMinutes);
                }
                Action<bool> handler = ProtectionChanged;
                if (handler != null)
                {
                    handler(protectedNow);
                }
            }

            double idleSeconds = _activity.IdleSeconds;
            bool away = _activity.IsLocked ||
                        (!protectedNow &&
                         idleSeconds >= _settings.IdleThresholdMinutes * 60.0);

            if (away && !_wasAway)
            {
                _settings.MovementActiveSeconds = 0;
                RaiseSettingsChanged();
            }
            else if (!away && _wasAway)
            {
                _returnGraceUntil = now.AddMinutes(2);
            }
            _wasAway = away;

            bool viewingProtectedContent =
                protectedNow && idleSeconds < 30 * 60.0 && !_activity.IsLocked;
            bool activelyUsing = !_activity.IsLocked &&
                (idleSeconds < _settings.IdleThresholdMinutes * 60.0 ||
                 viewingProtectedContent);

            if (activelyUsing)
            {
                if (_settings.DrinkEnabled)
                {
                    _settings.DrinkActiveSeconds += deltaSeconds;
                }
                if (_settings.MovementEnabled)
                {
                    _settings.MovementActiveSeconds += deltaSeconds;
                }
            }

            if (CurrentNotice != null ||
                protectedNow ||
                away ||
                now < _returnGraceUntil ||
                now < _protectionGraceUntil ||
                now < _settings.PausedUntil)
            {
                return;
            }

            ReminderNotice next = FindDeferred(now);
            if (next == null)
            {
                next = FindDueTodo(now);
            }
            if (next == null)
            {
                next = FindDueSchedule(now, _settings.MealSchedules, ReminderKind.Meal);
            }
            if (next == null && _settings.DormSchedule != null)
            {
                next = FindDueSchedule(
                    now,
                    new[] { _settings.DormSchedule },
                    ReminderKind.Dorm);
            }
            if (next == null)
            {
                next = FindDueInterval(now);
            }

            if (next != null)
            {
                CurrentNotice = next;
                Action<ReminderNotice> handler = NoticeRaised;
                if (handler != null)
                {
                    handler(next);
                }
                RaiseSettingsChanged();
            }
        }

        public void CompleteCurrent()
        {
            ReminderNotice notice = CurrentNotice;
            if (notice == null)
            {
                return;
            }

            TodoDifficulty todoDifficulty = TodoDifficulty.Simple;
            if (notice.Kind == ReminderKind.Drink)
            {
                _settings.DrinkActiveSeconds = 0;
                _settings.DrinkSnoozedUntil = AppTime.Never;
                _settings.Stats.DrinkCount++;
            }
            else if (notice.Kind == ReminderKind.Movement)
            {
                _settings.MovementActiveSeconds = 0;
                _settings.MovementSnoozedUntil = AppTime.Never;
                _settings.Stats.MovementCount++;
            }
            else if (notice.Kind == ReminderKind.Meal)
            {
                _settings.Stats.MealCount++;
            }
            else if (notice.Kind == ReminderKind.Dorm)
            {
                _settings.Stats.DormCount++;
            }
            else if (notice.Kind == ReminderKind.Todo)
            {
                TodoItem item = FindTodo(notice.SourceId);
                if (item != null)
                {
                    todoDifficulty = item.Difficulty;
                    item.AdvanceAfterCompletion();
                }
                _settings.Stats.TodoCount++;
            }

            ProgressService.Award(
                _settings,
                notice.Kind,
                todoDifficulty);
            ClearCurrent();
        }

        public void SkipCurrent()
        {
            ReminderNotice notice = CurrentNotice;
            if (notice == null)
            {
                return;
            }

            if (notice.Kind == ReminderKind.Drink)
            {
                _settings.DrinkActiveSeconds = 0;
                _settings.DrinkSnoozedUntil = AppTime.Never;
            }
            else if (notice.Kind == ReminderKind.Movement)
            {
                _settings.MovementActiveSeconds = 0;
                _settings.MovementSnoozedUntil = AppTime.Never;
            }
            else if (notice.Kind == ReminderKind.Todo)
            {
                TodoItem item = FindTodo(notice.SourceId);
                if (item != null)
                {
                    item.AdvanceAfterCompletion();
                }
            }

            ClearCurrent();
        }

        public void SnoozeCurrent(int minutes)
        {
            ReminderNotice notice = CurrentNotice;
            if (notice == null)
            {
                return;
            }

            DateTime due = DateTime.Now.AddMinutes(minutes);
            if (notice.Kind == ReminderKind.Drink)
            {
                _settings.DrinkSnoozedUntil = due;
            }
            else if (notice.Kind == ReminderKind.Movement)
            {
                _settings.MovementSnoozedUntil = due;
            }
            else if (notice.Kind == ReminderKind.Todo)
            {
                TodoItem item = FindTodo(notice.SourceId);
                if (item != null)
                {
                    item.SnoozedUntilLocal = due;
                }
            }
            else
            {
                _deferred.Add(new DeferredNotice
                {
                    Notice = notice,
                    DueAt = due
                });
            }

            ClearCurrent();
        }

        public void PauseFor(TimeSpan duration)
        {
            _settings.PausedUntil = DateTime.Now.Add(duration);
            RaiseSettingsChanged();
        }

        private ReminderNotice FindDeferred(DateTime now)
        {
            DeferredNotice due = _deferred
                .Where(x => x.DueAt <= now)
                .OrderBy(x => x.DueAt)
                .FirstOrDefault();
            if (due == null)
            {
                return null;
            }
            _deferred.Remove(due);
            return due.Notice;
        }

        private ReminderNotice FindDueTodo(DateTime now)
        {
            foreach (TodoItem item in _settings.Todos
                .Where(x => !x.Completed)
                .OrderBy(x => x.DueLocal))
            {
                if (item.DueLocal > now)
                {
                    continue;
                }
                if (item.SnoozedUntilLocal > now)
                {
                    continue;
                }

                bool snoozeJustFinished =
                    !AppTime.IsNever(item.SnoozedUntilLocal) &&
                    item.SnoozedUntilLocal <= now;
                if (!snoozeJustFinished &&
                    item.LastNotifiedDueTicks == item.DueLocal.Ticks)
                {
                    continue;
                }

                item.LastNotifiedDueTicks = item.DueLocal.Ticks;
                item.SnoozedUntilLocal = AppTime.Never;
                return CreateNotice(
                    ReminderKind.Todo,
                    item.Id,
                    item.Title + " 到时间了",
                    string.IsNullOrWhiteSpace(item.Note)
                        ? Warm("按自己的节奏完成就好。")
                        : item.Note);
            }
            return null;
        }

        private ReminderNotice FindDueSchedule(
            DateTime now,
            IEnumerable<ScheduleEntry> entries,
            ReminderKind kind)
        {
            foreach (ScheduleEntry entry in entries)
            {
                TimeSpan time;
                if (!entry.Enabled ||
                    !entry.AppliesTo(now) ||
                    !entry.TryGetTime(out time))
                {
                    continue;
                }

                DateTime due = now.Date.Add(time);
                string dateKey = now.ToString("yyyy-MM-dd");
                if (now < due ||
                    now > due.AddHours(2) ||
                    entry.LastTriggeredDateKey == dateKey)
                {
                    continue;
                }

                entry.LastTriggeredDateKey = dateKey;
                if (kind == ReminderKind.Meal)
                {
                    return CreateNotice(
                        kind,
                        entry.Id,
                        "到" + entry.Label + "时间了",
                        Warm("先照顾好自己，再继续忙吧。"));
                }

                return CreateNotice(
                    kind,
                    entry.Id,
                    "到回宿舍的时间了",
                    Warm("记得带好随身物品，路上注意安全。"));
            }
            return null;
        }

        private ReminderNotice FindDueInterval(DateTime now)
        {
            if (_settings.DrinkEnabled &&
                _settings.DrinkActiveSeconds >=
                    _settings.DrinkIntervalMinutes * 60.0 &&
                now >= _settings.DrinkSnoozedUntil)
            {
                return CreateNotice(
                    ReminderKind.Drink,
                    "drink",
                    "该喝水了",
                    Warm("喝几口水，再继续忙吧。"));
            }

            if (_settings.MovementEnabled &&
                _settings.MovementActiveSeconds >=
                    _settings.MovementIntervalMinutes * 60.0 &&
                now >= _settings.MovementSnoozedUntil)
            {
                return CreateNotice(
                    ReminderKind.Movement,
                    "movement",
                    "该起身活动了",
                    Warm("走一走，舒展一下肩颈吧。"));
            }
            return null;
        }

        private ReminderNotice CreateNotice(
            ReminderKind kind,
            string sourceId,
            string title,
            string message)
        {
            return new ReminderNotice
            {
                Kind = kind,
                SourceId = sourceId,
                Title = title,
                Message = message,
                RaisedAt = DateTime.Now
            };
        }

        private string Warm(string warmText)
        {
            return _settings.WarmMessages ? warmText : "";
        }

        private TodoItem FindTodo(string id)
        {
            return _settings.Todos.FirstOrDefault(x => x.Id == id);
        }

        private void ClearCurrent()
        {
            CurrentNotice = null;
            Action handler = NoticeCleared;
            if (handler != null)
            {
                handler();
            }
            RaiseSettingsChanged();
        }

        private void RaiseSettingsChanged()
        {
            Action handler = SettingsChanged;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
