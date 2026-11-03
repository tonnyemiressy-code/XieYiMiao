using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace XieYiMiao
{
    public static class StartupManager
    {
        private const string RunKey =
            @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "XieYiMiao";

        public static string InstalledExecutablePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "歇一喵",
                    "歇一喵.exe");
            }
        }

        public static bool Enable()
        {
            try
            {
                string current = Process.GetCurrentProcess().MainModule.FileName;
                string installed = InstalledExecutablePath;
                InstallExecutable(current, installed);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    key.SetValue(
                        ValueName,
                        BuildStartupCommand(installed));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Disable()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    return key != null && key.GetValue(ValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        internal static string BuildStartupCommand(string executablePath)
        {
            return "\"" + executablePath + "\" --startup";
        }

        internal static void InstallExecutable(
            string sourcePath,
            string installedPath)
        {
            string folder = Path.GetDirectoryName(installedPath);
            Directory.CreateDirectory(folder);
            if (!PathsEqual(sourcePath, installedPath))
            {
                File.Copy(sourcePath, installedPath, true);
            }
        }

        internal static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd('\\'),
                Path.GetFullPath(right).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
