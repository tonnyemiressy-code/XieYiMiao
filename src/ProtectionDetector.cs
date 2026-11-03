using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XieYiMiao
{
    public interface IProtectionState
    {
        bool IsProtected(AppSettings settings);
    }

    public sealed class ProtectionDetector : IProtectionState
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        private readonly HashSet<string> _meetingProcesses =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "wemeet", "tencentmeeting", "lark", "feishu",
                "zoom", "zoomrooms", "teams", "ms-teams", "webexmta"
            };

        public bool IsProtected(AppSettings settings)
        {
            if (settings.ManualMeetingMode)
            {
                return true;
            }

            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            if (settings.AutoHideMeetings && IsMeetingWindow(foreground))
            {
                return true;
            }

            return settings.AutoHideFullScreen && IsFullScreenWindow(foreground);
        }

        private bool IsMeetingWindow(IntPtr hwnd)
        {
            try
            {
                uint id;
                GetWindowThreadProcessId(hwnd, out id);
                Process process = Process.GetProcessById((int)id);

                StringBuilder title = new StringBuilder(512);
                GetWindowText(hwnd, title, title.Capacity);
                return IsKnownMeetingIdentity(
                    process.ProcessName,
                    title.ToString());
            }
            catch
            {
                return false;
            }
        }

        internal bool IsKnownMeetingIdentity(
            string processName,
            string windowTitle)
        {
            if (!string.IsNullOrWhiteSpace(processName) &&
                _meetingProcesses.Contains(processName))
            {
                return true;
            }

            string value =
                (windowTitle ?? "").ToLowerInvariant();
            return value.Contains("腾讯会议") ||
                   value.Contains("飞书会议") ||
                   value.Contains("lark meeting") ||
                   value.Contains("zoom meeting") ||
                   value.Contains("microsoft teams meeting");
        }

        private bool IsFullScreenWindow(IntPtr hwnd)
        {
            try
            {
                uint id;
                GetWindowThreadProcessId(hwnd, out id);
                Process process = Process.GetProcessById((int)id);
                string name = process.ProcessName;
                if (name.Equals("explorer", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("ShellExperienceHost", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                RECT window;
                if (!GetWindowRect(hwnd, out window))
                {
                    return false;
                }

                IntPtr monitor = MonitorFromWindow(hwnd, 2);
                MONITORINFO info = new MONITORINFO();
                info.cbSize = Marshal.SizeOf(info);
                if (!GetMonitorInfo(monitor, ref info))
                {
                    return false;
                }

                const int tolerance = 3;
                return Math.Abs(window.Left - info.rcMonitor.Left) <= tolerance &&
                       Math.Abs(window.Top - info.rcMonitor.Top) <= tolerance &&
                       Math.Abs(window.Right - info.rcMonitor.Right) <= tolerance &&
                       Math.Abs(window.Bottom - info.rcMonitor.Bottom) <= tolerance;
            }
            catch
            {
                return false;
            }
        }
    }
}
