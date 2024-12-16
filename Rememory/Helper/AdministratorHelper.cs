using System.Diagnostics;
using System;
using System.Security.Principal;
using Windows.ApplicationModel;

namespace Rememory.Helper
{
    public static class AdministratorHelper
    {
        public static bool IsAppRunningAsAdministrator()
        {
            var windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void TryToRestartAppAsAdministrator(string arguments = "")
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = $"Rememory.exe",
                UseShellExecute = true,
                Arguments = arguments,
                Verb = "runas"
            };
            try
            {
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch { }
        }
    }
}
