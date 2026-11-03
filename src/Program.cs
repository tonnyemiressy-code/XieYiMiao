using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace XieYiMiao
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--render-cat")
            {
                RenderCatPreview(
                    args[1],
                    args.Length >= 3 && args[2] == "left",
                    args.Length >= 4
                        ? (CatMood)Enum.Parse(
                            typeof(CatMood),
                            args[3],
                            true)
                        : CatMood.Walk,
                    args.Length >= 5
                        ? double.Parse(
                            args[4],
                            System.Globalization.CultureInfo.InvariantCulture)
                        : 0,
                    args.Length >= 6
                        ? (PetOutfit)Enum.Parse(
                            typeof(PetOutfit),
                            args[5],
                            true)
                        : PetOutfit.Classic);
                return;
            }

            if (args.Length >= 2 && args[0] == "--self-test")
            {
                Environment.ExitCode = SelfTests.Run(args[1]);
                return;
            }

            if (args.Length >= 2 && args[0] == "--render-preview")
            {
                RenderPreview(
                    args[1],
                    args.Length >= 3 ? args[2] : "home");
                return;
            }

            if (args.Length >= 2 &&
                args[0] == "--render-confirmation")
            {
                RenderConfirmationPreview(args[1]);
                return;
            }

            if (args.Length >= 2 &&
                args[0] == "--render-care")
            {
                RenderCarePreview(
                    args[1],
                    args.Length >= 3 ? args[2] : "weather");
                return;
            }

            if (Array.Exists(args, x => x == "--runtime-smoke"))
            {
                RunSmokeTest();
                return;
            }

            bool created;
            using (Mutex mutex = new Mutex(
                true,
                "Local\\XieYiMiao.SingleInstance",
                out created))
            {
                if (!created)
                {
                    MessageBox.Show(
                        "歇一喵已经在运行了，可以从任务栏托盘打开设置。",
                        "歇一喵",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                Application application = CreateApplication();
                AppController controller = null;
                application.DispatcherUnhandledException += delegate(
                    object sender,
                    DispatcherUnhandledExceptionEventArgs e)
                {
                    WriteCrashLog(e.Exception);
                    MessageBox.Show(
                        "程序遇到了一个问题，已经在本地保存错误记录。\n" +
                        "重新打开后可以继续使用。",
                        "歇一喵",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    e.Handled = true;
                };

                application.Startup += delegate
                {
                    controller = new AppController(application);
                    if (Array.Exists(args, x => x == "--runtime-smoke"))
                    {
                        controller.StartSmokeTest();
                    }
                    else
                    {
                        controller.Start(
                            Array.Exists(args, x => x == "--startup"));
                    }
                };
                application.Exit += delegate
                {
                    if (controller != null)
                    {
                        controller.Dispose();
                    }
                };
                application.Run();
            }
        }

        private static Application CreateApplication()
        {
            Application application = new Application();
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            return application;
        }

        private static void RunSmokeTest()
        {
            Application application = CreateApplication();
            AppController controller = null;
            application.Startup += delegate
            {
                controller = new AppController(application);
                controller.StartSmokeTest();
            };
            application.Exit += delegate
            {
                if (controller != null)
                {
                    controller.Dispose();
                }
            };
            application.Run();
        }

        private static void RenderPreview(string path, string page)
        {
            Application application = CreateApplication();
            application.Startup += delegate
            {
                AppSettings settings = AppSettings.CreateDefault();
                settings.Stats.DrinkCount = 3;
                settings.Stats.MovementCount = 2;
                settings.Stats.MealCount = 1;
                settings.Stats.TodoCount = 2;
                settings.Stats.GrowthPoints = 8;
                settings.Stats.FreedomEarnedToday = 8;
                settings.FreedomPoints = 18;
                settings.TotalFreedomEarned = 28;
                settings.MealSchedules.Add(new ScheduleEntry
                {
                    Label = "午饭",
                    TimeText = "12:10",
                    DaysMask = 31,
                    Enabled = true
                });
                settings.Todos.Add(new TodoItem
                {
                    Title = "整理今天的笔记",
                    Note = "完成后就可以安心休息了。",
                    DueLocal = DateTime.Today.AddHours(20),
                    Repeat = TodoRepeat.None,
                    CategoryId = "study",
                    Difficulty = TodoDifficulty.Normal
                });
                settings.Todos.Add(new TodoItem
                {
                    Title = "提交周报",
                    DueLocal = DateTime.Today.AddDays(1).AddHours(17),
                    Repeat = TodoRepeat.Weekly,
                    CategoryId = "research",
                    Difficulty = TodoDifficulty.Important
                });
                string previewPage = page;
                if (page == "rewards-complete")
                {
                    settings.Stats.CompleteCare(DailyCareKind.Body);
                    settings.Stats.CompleteCare(DailyCareKind.Feelings);
                    settings.Stats.CompleteCare(DailyCareKind.Boundary);
                    settings.Stats.CompleteCare(DailyCareKind.Kindness);
                    previewPage = "rewards";
                }
                else if (page == "home-quiet")
                {
                    settings.Stats.ReflectionStopped = true;
                    previewPage = "home";
                }
                SettingsWindow window = new SettingsWindow(
                    settings,
                    Path.Combine(
                        Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                        "歇一喵"));
                window.ShowPreviewPage(previewPage);
                window.Show();
                window.Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(delegate
                    {
                        try
                        {
                            window.UpdateLayout();
                            int width = Math.Max(1, (int)window.ActualWidth);
                            int height = Math.Max(1, (int)window.ActualHeight);
                            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                                width,
                                height,
                                96,
                                96,
                                PixelFormats.Pbgra32);
                            bitmap.Render(window);
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            string folder = Path.GetDirectoryName(
                                Path.GetFullPath(path));
                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                            using (FileStream stream = File.Create(path))
                            {
                                encoder.Save(stream);
                            }
                        }
                        catch (Exception exception)
                        {
                            File.WriteAllText(
                                Path.GetFullPath(path) + ".error.txt",
                                exception.ToString());
                        }
                        finally
                        {
                            window.ForceClose();
                            application.Shutdown();
                        }
                    }));
            };
            application.Run();
        }

        private static void RenderCatPreview(
            string path,
            bool facingLeft,
            CatMood mood,
            double phaseSeconds,
            PetOutfit outfit)
        {
            Application application = CreateApplication();
            application.Startup += delegate
            {
                CatControl cat = new CatControl
                {
                    Width = 190,
                    Height = 190,
                    FacingLeft = facingLeft,
                    Mood = mood,
                    Outfit = outfit,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                cat.Tick(phaseSeconds);
                Window window = new Window
                {
                    Width = 240,
                    Height = 240,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    Background = Theme.Ivory,
                    Content = cat,
                    ShowInTaskbar = false
                };
                window.Show();
                window.Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(delegate
                    {
                        try
                        {
                            window.UpdateLayout();
                            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                                (int)window.ActualWidth,
                                (int)window.ActualHeight,
                                96,
                                96,
                                PixelFormats.Pbgra32);
                            bitmap.Render(window);
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            using (FileStream stream = File.Create(path))
                            {
                                encoder.Save(stream);
                            }
                        }
                        finally
                        {
                            window.Close();
                            application.Shutdown();
                        }
                    }));
            };
            application.Run();
        }

        private static void RenderConfirmationPreview(string path)
        {
            Application application = CreateApplication();
            application.Startup += delegate
            {
                ReminderConfirmationWindow window =
                    new ReminderConfirmationWindow(
                        ReminderActionKind.Complete,
                        0,
                        new ReminderNotice
                        {
                            Kind = ReminderKind.Drink,
                            Title = "该喝水了",
                            RaisedAt = DateTime.Now
                        });
                window.Show();
                window.Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(delegate
                    {
                        try
                        {
                            window.UpdateLayout();
                            int width = Math.Max(
                                1,
                                (int)window.ActualWidth);
                            int height = Math.Max(
                                1,
                                (int)window.ActualHeight);
                            RenderTargetBitmap bitmap =
                                new RenderTargetBitmap(
                                    width,
                                    height,
                                    96,
                                    96,
                                    PixelFormats.Pbgra32);
                            bitmap.Render(window);
                            PngBitmapEncoder encoder =
                                new PngBitmapEncoder();
                            encoder.Frames.Add(
                                BitmapFrame.Create(bitmap));
                            string folder = Path.GetDirectoryName(
                                Path.GetFullPath(path));
                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                            using (FileStream stream =
                                File.Create(path))
                            {
                                encoder.Save(stream);
                            }
                        }
                        catch (Exception exception)
                        {
                            File.WriteAllText(
                                Path.GetFullPath(path) +
                                    ".error.txt",
                                exception.ToString());
                        }
                        finally
                        {
                            window.Close();
                            application.Shutdown();
                        }
                    }));
            };
            application.Run();
        }

        private static void RenderCarePreview(
            string path,
            string page)
        {
            Application application = CreateApplication();
            application.Startup += delegate
            {
                AppSettings settings = AppSettings.CreateDefault();
                settings.Stats.GrowthPoints = 68;
                settings.SelectedPetOutfit = PetOutfit.GraduateBag;
                settings.EmotionWeather = EmotionWeather.Drizzle;
                settings.EmotionWeatherDateKey =
                    DateTime.Today.ToString("yyyy-MM-dd");
                settings.WorryNotes.Add(new WorryNote
                {
                    Text = "今天又临时接到一件原本不属于我的事。",
                    Kind = "worry",
                    CreatedAtLocal = DateTime.Now.AddMinutes(-18)
                });
                settings.WorryNotes.Add(new WorryNote
                {
                    Text = "事实：实验结果不符合预期。\n\n" +
                        "最小下一步：先记录参数，明天再分析。",
                    Kind = "reframe",
                    CreatedAtLocal = DateTime.Now.AddMinutes(-5)
                });
                string previewPage = page;
                if (page == "ritual-complete")
                {
                    settings.EveningRitualHistory.Add(
                        new EveningRitualEntry
                        {
                            DateKey =
                                DateTime.Today.ToString("yyyy-MM-dd"),
                            Accomplished = "把今天的实验数据整理好了",
                            MostDraining = "临时增加的沟通和解释",
                            LeaveForTomorrow = "剩余图表和结论",
                            ClosingStyle = "quiet",
                            ClosingMessage =
                                "没做完的事情留在桌上，不需要一起带回去。",
                            CompletedAtLocal = DateTime.Now
                        });
                    previewPage = "ritual";
                }
                CareCompanionWindow window =
                    new CareCompanionWindow(settings);
                window.ShowPage(previewPage);
                window.Show();
                window.Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(delegate
                    {
                        try
                        {
                            window.UpdateLayout();
                            int width = Math.Max(
                                1,
                                (int)window.ActualWidth);
                            int height = Math.Max(
                                1,
                                (int)window.ActualHeight);
                            RenderTargetBitmap bitmap =
                                new RenderTargetBitmap(
                                    width,
                                    height,
                                    96,
                                    96,
                                    PixelFormats.Pbgra32);
                            bitmap.Render(window);
                            PngBitmapEncoder encoder =
                                new PngBitmapEncoder();
                            encoder.Frames.Add(
                                BitmapFrame.Create(bitmap));
                            string folder = Path.GetDirectoryName(
                                Path.GetFullPath(path));
                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                            using (FileStream stream =
                                File.Create(path))
                            {
                                encoder.Save(stream);
                            }
                        }
                        catch (Exception exception)
                        {
                            File.WriteAllText(
                                Path.GetFullPath(path) +
                                    ".error.txt",
                                exception.ToString());
                        }
                        finally
                        {
                            window.ForceClose();
                            application.Shutdown();
                        }
                    }));
            };
            application.Run();
        }

        private static void WriteCrashLog(Exception exception)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "歇一喵");
                Directory.CreateDirectory(folder);
                File.AppendAllText(
                    Path.Combine(folder, "error.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    Environment.NewLine +
                    exception +
                    Environment.NewLine +
                    Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
