using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SkipDrama_YuanShen
{
    internal static class ForegroundGameGuard
    {
        private static readonly object CacheGate = new object();
        private static uint _cachedProcessId;
        private static bool _cachedResult;

        internal static bool IsGenshinForeground()
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(window, out var processId);
            if (processId == 0)
            {
                return false;
            }

            lock (CacheGate)
            {
                if (_cachedProcessId == processId)
                {
                    return _cachedResult;
                }

                _cachedProcessId = processId;
                _cachedResult = IsGenshinProcess(processId);
                return _cachedResult;
            }
        }

        private static bool IsGenshinProcess(uint processId)
        {
            try
            {
                using (var process = Process.GetProcessById(unchecked((int)processId)))
                {
                    var name = process.ProcessName;
                    return name.Equals("YuanShen", StringComparison.OrdinalIgnoreCase) ||
                           name.Equals("GenshinImpact", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
