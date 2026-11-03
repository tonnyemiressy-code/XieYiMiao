using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace XieYiMiao
{
    public interface IActivityState
    {
        bool IsLocked { get; }
        double IdleSeconds { get; }
    }

    public sealed class ActivityTracker : IActivityState, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

        private bool _locked;
        private bool _disposed;

        public ActivityTracker()
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public bool IsLocked
        {
            get { return _locked; }
        }

        public double IdleSeconds
        {
            get
            {
                LASTINPUTINFO info = new LASTINPUTINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                if (!GetLastInputInfo(ref info))
                {
                    return 0;
                }

                uint now = unchecked((uint)Environment.TickCount);
                uint elapsed = unchecked(now - info.dwTime);
                return elapsed / 1000.0;
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _locked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _locked = false;
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _locked = true;
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _locked = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }
    }
}
