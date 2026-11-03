using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace XieYiMiao
{
    public sealed class AppController : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly Application _application;
        private readonly SettingsStore _store;
        private readonly AppSettings _settings;
        private readonly ActivityTracker _activity;
        private readonly ProtectionDetector _protection;
        private readonly ReminderEngine _engine;
        private readonly PetWindow _pet;
        private readonly ReminderCardWindow _card;
        private readonly SettingsWindow _settingsWindow;
        private readonly CareCompanionWindow _careWindow;
        private readonly DispatcherTimer _tickTimer;
        private readonly DispatcherTimer _saveTimer;
        private DispatcherTimer _smokeTimer;
        private ReminderConfirmationWindow _confirmationDialog;
        private Forms.NotifyIcon _tray;
        private Forms.ToolStripMenuItem _trayVisibility;
        private Forms.ToolStripMenuItem _trayMeeting;
        private Forms.ToolStripMenuItem _trayStats;
        private DateTime _lastTick = DateTime.Now;
        private DateTime _showCardAfter = DateTime.MinValue;
        private bool _cardCollapsed;
        private bool _exiting;
        private bool _disposed;
        private bool _lastStartupState;
        private string _lastShortcutStatus = "";

        public AppController(Application application)
        {
            _application = application;
            _store = new SettingsStore();
            _settings = _store.Load();
            _activity = new ActivityTracker();
            _protection = new ProtectionDetector();
            _engine = new ReminderEngine(_settings, _activity, _protection);
            _pet = new PetWindow(_settings);
            _card = new ReminderCardWindow();
            _settingsWindow = new SettingsWindow(_settings, _store.DataFolder);
            _careWindow = new CareCompanionWindow(_settings);

            WireEvents();
            CreateTray();

            _tickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _tickTimer.Tick += OnTick;
            _tickTimer.Start();

            _saveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _saveTimer.Tick += delegate { Save(); };
            _saveTimer.Start();
            _lastStartupState = StartupManager.IsEnabled();
        }

        public void Start(bool startedWithWindows)
        {
            if (_settings.AutoStart)
            {
                ApplyStartupSetting(true);
            }

            // The shortcut must remain available even when the pet starts hidden.
            // Creating the native handle here does not make the window visible.
            _pet.InitializeShortcut();
            _pet.ApplySettings();

            if (_settings.FirstRun)
            {
                _settingsWindow.Show();
                _settingsWindow.UpdateLayout();
                DisplayManager.CenterWindow(
                    _settingsWindow,
                    _pet.CurrentDisplayArea);
                _settingsWindow.Activate();
                MessageBoxResult result = MessageBox.Show(
                    _settingsWindow,
                    "是否让“歇一喵”随 Windows 自动启动？\n\n" +
                    "启用时会将程序安放到当前用户的本地程序目录，" +
                    "不需要管理员权限。",
                    "欢迎使用歇一喵",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                _settings.AutoStart = result == MessageBoxResult.Yes;
                _settings.FirstRun = false;
                ApplyStartupSetting(true);
                Save();
                _settingsWindow.RefreshCurrentPage();
            }
            else if (!startedWithWindows)
            {
                ShowSettings();
            }
        }

        public void StartSmokeTest()
        {
            _settings.PetVisible = true;
            _pet.InitializeShortcut();
            _pet.ApplySettings();
            _settingsWindow.ShowPreviewPage("pet");
            _settingsWindow.Show();
            _card.ShowNotice(
                new ReminderNotice
                {
                    Kind = ReminderKind.Drink,
                    Title = "该喝水了",
                    Message = "喝几口水，再继续忙吧。",
                    RaisedAt = DateTime.Now
                },
                _pet);
            _smokeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _smokeTimer.Tick += delegate
            {
                _smokeTimer.Stop();
                _application.Shutdown();
            };
            _smokeTimer.Start();
        }

        private void WireEvents()
        {
            _engine.NoticeRaised += OnNoticeRaised;
            _engine.NoticeCleared += OnNoticeCleared;
            _engine.SettingsChanged += Save;
            _engine.ProtectionChanged += OnProtectionChanged;

            _card.DoneRequested += delegate
            {
                ConfirmReminderAction(
                    ReminderActionKind.Complete,
                    0);
            };
            _card.SnoozeRequested += delegate(int minutes)
            {
                ConfirmReminderAction(
                    ReminderActionKind.Snooze,
                    minutes);
            };
            _card.SkipRequested += delegate
            {
                ConfirmReminderAction(
                    ReminderActionKind.Skip,
                    0);
            };
            _card.AutoCollapsed += delegate
            {
                _cardCollapsed = true;
                _pet.SetReminderBadge(true);
            };

            _pet.SettingsRequested += ShowSettings;
            _pet.ToggleRequested += TogglePet;
            _pet.ExitRequested += Exit;
            _pet.ReminderRequested += ShowCurrentReminder;
            _pet.CareRequested += ShowCare;
            _pet.PetInteractionChanged += Save;
            _pet.PositionChanged += Save;
            _pet.ShortcutStatusChanged += OnShortcutStatusChanged;

            _settingsWindow.SettingsApplied += ApplySettings;
            _settingsWindow.ResetPetRequested += delegate
            {
                _pet.ResetToCorner();
                Save();
            };
            _settingsWindow.FindWindowsRequested += FindAllWindows;
            _settingsWindow.CelebrationRequested += delegate
            {
                _pet.Celebrate();
            };
            _settingsWindow.CareRequested += ShowCare;
            _careWindow.SaveRequested += delegate
            {
                Save();
                _settingsWindow.RefreshCurrentPage();
            };
            _careWindow.CatReactionRequested += delegate(
                CatMood mood,
                double seconds)
            {
                _pet.React(mood, seconds);
            };
        }

        private void ConfirmReminderAction(
            ReminderActionKind action,
            int minutes)
        {
            ReminderNotice notice = _engine.CurrentNotice;
            if (notice == null)
            {
                return;
            }
            ReminderConfirmationWindow dialog =
                new ReminderConfirmationWindow(
                    action,
                    minutes,
                    notice);
            dialog.Owner = _card;
            _confirmationDialog = dialog;
            bool confirmed;
            try
            {
                confirmed = dialog.ShowDialog() == true;
            }
            finally
            {
                _confirmationDialog = null;
            }
            if (!confirmed)
            {
                _card.ShowNotice(notice, _pet);
                return;
            }
            if (action == ReminderActionKind.Complete)
            {
                _engine.CompleteCurrent();
            }
            else if (action == ReminderActionKind.Snooze)
            {
                _engine.SnoozeCurrent(minutes);
            }
            else
            {
                _engine.SkipCurrent();
            }
        }

        private void OnShortcutStatusChanged(
            string shortcut,
            string status,
            bool fallback)
        {
            _settingsWindow.UpdateShortcutStatus(shortcut, status);
            RefreshTray();
            if (fallback &&
                _tray != null &&
                status != _lastShortcutStatus)
            {
                _tray.BalloonTipTitle = "歇一喵快捷键已调整";
                _tray.BalloonTipText = status;
                _tray.BalloonTipIcon = Forms.ToolTipIcon.Info;
                _tray.ShowBalloonTip(5000);
            }
            _lastShortcutStatus = status;
        }

        private void OnTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double delta = (now - _lastTick).TotalSeconds;
            _lastTick = now;
            _engine.Tick(now, delta);

            if (_engine.CurrentNotice != null &&
                !_engine.IsProtected &&
                !_cardCollapsed &&
                !_card.IsVisible &&
                now >= _showCardAfter)
            {
                _card.ShowNotice(_engine.CurrentNotice, _pet);
            }
            RefreshTray();
        }

        private void OnNoticeRaised(ReminderNotice notice)
        {
            _cardCollapsed = false;
            _pet.SetNotice(notice);
            _card.ShowNotice(notice, _pet);
        }

        private void OnNoticeCleared()
        {
            _cardCollapsed = false;
            _card.HideCard();
            _pet.SetReminderBadge(false);
            _pet.SetNotice(null);
            _settingsWindow.RefreshCurrentPage();
            Save();
        }

        private void OnProtectionChanged(bool value)
        {
            _pet.SetProtection(value);
            if (value)
            {
                if (_confirmationDialog != null)
                {
                    _confirmationDialog.Close();
                }
                _card.HideCard();
                _cardCollapsed = false;
                if (_careWindow.IsVisible)
                {
                    _careWindow.Hide();
                }
            }
            else
            {
                _showCardAfter = DateTime.Now.AddMinutes(
                    _settings.ProtectionGraceMinutes);
            }
        }

        private void ApplySettings()
        {
            ApplyStartupSetting(false);
            _pet.ApplySettings();
            _pet.SetProtection(_engine.IsProtected);
            Save();
            RefreshTray();
        }

        private void ApplyStartupSetting(bool firstRun)
        {
            bool success;
            if (_settings.AutoStart)
            {
                success = StartupManager.Enable();
            }
            else
            {
                success = StartupManager.Disable();
            }

            if (!success && !firstRun &&
                _lastStartupState != _settings.AutoStart)
            {
                MessageBox.Show(
                    _settingsWindow,
                    "开机启动设置没有成功写入。程序其他功能不受影响。",
                    "歇一喵",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                _settings.AutoStart = _lastStartupState;
            }
            else if (success)
            {
                _lastStartupState = _settings.AutoStart;
            }
        }

        private void TogglePet()
        {
            _settings.PetVisible = !_settings.PetVisible;
            _pet.ApplySettings();
            _settingsWindow.RefreshCurrentPage();
            Save();
            RefreshTray();
        }

        private void ShowSettings()
        {
            _settingsWindow.RefreshCurrentPage();
            bool wasVisible = _settingsWindow.IsVisible;
            if (!_settingsWindow.IsVisible)
            {
                _settingsWindow.Show();
            }
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            if (!wasVisible)
            {
                _settingsWindow.UpdateLayout();
                DisplayManager.CenterWindow(
                    _settingsWindow,
                    _pet.CurrentDisplayArea);
            }
            _settingsWindow.Activate();
        }

        private void ShowCare()
        {
            _careWindow.RefreshCurrentPage();
            bool wasVisible = _careWindow.IsVisible;
            if (!_careWindow.IsVisible)
            {
                _careWindow.Show();
            }
            if (_careWindow.WindowState == WindowState.Minimized)
            {
                _careWindow.WindowState = WindowState.Normal;
            }
            if (!wasVisible)
            {
                _careWindow.UpdateLayout();
                DisplayManager.CenterWindow(
                    _careWindow,
                    _pet.CurrentDisplayArea);
            }
            _careWindow.Activate();
            _pet.React(CatMood.Companion, 2.5);
        }

        private void FindAllWindows()
        {
            _settings.PetVisible = true;
            _pet.MoveToCursorDisplay();
            _pet.ApplySettings();
            if (!_settingsWindow.IsVisible)
            {
                _settingsWindow.Show();
            }
            _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.UpdateLayout();
            DisplayManager.CenterWindow(
                _settingsWindow,
                _pet.CurrentDisplayArea);
            if (_engine.CurrentNotice != null && !_engine.IsProtected)
            {
                _card.ShowNotice(_engine.CurrentNotice, _pet);
            }
            _settingsWindow.Activate();
            Save();
            RefreshTray();
        }

        private void ShowCurrentReminder()
        {
            if (_engine.CurrentNotice == null)
            {
                return;
            }
            _cardCollapsed = false;
            _pet.SetReminderBadge(false);
            _card.ShowNotice(_engine.CurrentNotice, _pet);
        }

        private void CreateTray()
        {
            _tray = new Forms.NotifyIcon();
            _tray.Text = "歇一喵";
            _tray.Icon = Icon.ExtractAssociatedIcon(
                System.Diagnostics.Process.GetCurrentProcess()
                    .MainModule.FileName) ?? CreateTrayIcon();
            _tray.Visible = true;
            _tray.DoubleClick += delegate { ShowSettings(); };

            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            _trayStats = new Forms.ToolStripMenuItem();
            _trayStats.Enabled = false;
            menu.Items.Add(_trayStats);
            menu.Items.Add(new Forms.ToolStripSeparator());

            _trayVisibility = new Forms.ToolStripMenuItem();
            _trayVisibility.Click += delegate { TogglePet(); };
            menu.Items.Add(_trayVisibility);

            Forms.ToolStripMenuItem pause =
                new Forms.ToolStripMenuItem("暂停提醒 1 小时");
            pause.Click += delegate
            {
                _engine.PauseFor(TimeSpan.FromHours(1));
                RefreshTray();
            };
            menu.Items.Add(pause);

            _trayMeeting = new Forms.ToolStripMenuItem("手动会议模式");
            _trayMeeting.CheckOnClick = true;
            _trayMeeting.CheckedChanged += delegate
            {
                if (_settings.ManualMeetingMode != _trayMeeting.Checked)
                {
                    _settings.ManualMeetingMode = _trayMeeting.Checked;
                    ApplySettings();
                }
            };
            menu.Items.Add(_trayMeeting);
            menu.Items.Add(new Forms.ToolStripSeparator());

            Forms.ToolStripMenuItem settings =
                new Forms.ToolStripMenuItem("打开设置");
            settings.Click += delegate { ShowSettings(); };
            Forms.ToolStripMenuItem care =
                new Forms.ToolStripMenuItem(CareContent.StationName);
            care.Click += delegate { ShowCare(); };
            menu.Items.Add(care);
            menu.Items.Add(settings);

            Forms.ToolStripMenuItem exit =
                new Forms.ToolStripMenuItem("退出歇一喵");
            exit.Click += delegate { Exit(); };
            menu.Items.Add(exit);
            _tray.ContextMenuStrip = menu;
            RefreshTray();
        }

        private void RefreshTray()
        {
            if (_tray == null)
            {
                return;
            }
            _settings.Stats.EnsureToday();
            _trayStats.Text = "今日小脚印  " +
                _settings.Stats.TotalToday;
            _trayVisibility.Text = _settings.PetVisible
                ? "隐藏桌宠  (" + _pet.ActiveHotKeyText + ")"
                : "显示桌宠  (" + _pet.ActiveHotKeyText + ")";
            if (_trayMeeting.Checked != _settings.ManualMeetingMode)
            {
                _trayMeeting.Checked = _settings.ManualMeetingMode;
            }
        }

        private static Icon CreateTrayIcon()
        {
            Bitmap bitmap = new Bitmap(32, 32);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (SolidBrush fur = new SolidBrush(Color.FromArgb(255, 245, 228)))
                using (SolidBrush peach = new SolidBrush(Color.FromArgb(242, 161, 95)))
                using (SolidBrush eye = new SolidBrush(Color.FromArgb(81, 53, 42)))
                using (Pen outline = new Pen(Color.FromArgb(214, 156, 115), 1.4f))
                {
                    PointF[] leftEar =
                    {
                        new PointF(5, 12),
                        new PointF(8, 3),
                        new PointF(14, 11)
                    };
                    PointF[] rightEar =
                    {
                        new PointF(18, 11),
                        new PointF(25, 3),
                        new PointF(28, 13)
                    };
                    graphics.FillPolygon(peach, leftEar);
                    graphics.FillPolygon(peach, rightEar);
                    graphics.FillEllipse(fur, 3, 7, 27, 23);
                    graphics.DrawEllipse(outline, 3, 7, 27, 23);
                    graphics.FillEllipse(eye, 9, 15, 4, 6);
                    graphics.FillEllipse(eye, 20, 15, 4, 6);
                    graphics.FillEllipse(
                        Brushes.LightPink,
                        15,
                        22,
                        3,
                        2);
                }
            }

            IntPtr handle = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(handle).Clone();
            DestroyIcon(handle);
            bitmap.Dispose();
            return icon;
        }

        private void Save()
        {
            try
            {
                _store.Save(_settings);
            }
            catch
            {
            }
        }

        public void Exit()
        {
            if (_exiting)
            {
                return;
            }
            _exiting = true;
            Save();
            Dispose();
            _application.Shutdown();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _tickTimer.Stop();
            _saveTimer.Stop();
            if (_smokeTimer != null)
            {
                _smokeTimer.Stop();
            }
            _activity.Dispose();
            if (_confirmationDialog != null)
            {
                _confirmationDialog.Close();
                _confirmationDialog = null;
            }
            _card.Close();
            _pet.Close();
            _careWindow.ForceClose();
            _settingsWindow.ForceClose();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
        }
    }
}
