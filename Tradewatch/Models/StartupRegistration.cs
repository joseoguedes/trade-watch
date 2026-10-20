using System;
using Microsoft.Win32;

namespace Tradewatch
{
    // "Start with Windows" via the current user's Run key (the list Task Manager's Startup tab shows).
    // The registry is the source of truth, so turning it off in Task Manager is respected too.
    public static class StartupRegistration
    {
        public const string TrayArg = "--tray";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Tradewatch";

        private static string Command => $"\"{Environment.ProcessPath}\" {TrayArg}";

        // True only if the entry launches this copy of the exe (a moved exe shows as off until re-ticked).
        public static bool IsEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string cmd
                    && string.Equals(cmd, Command, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void Set(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue(ValueName, Command);
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
