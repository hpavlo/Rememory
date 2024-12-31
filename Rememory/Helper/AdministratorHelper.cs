using System.Diagnostics;
using System;
using System.Security.Principal;
using System.Reflection;

namespace Rememory.Helper
{
    public static class AdministratorHelper
    {
        public static bool IsAppRunningAsAdministrator()
        {
            var windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void TryToRestartApp(bool asAdministrator = false, string arguments = "")
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.exe",
                UseShellExecute = true,
                Arguments = arguments,
                Verb = asAdministrator ? "runas" : string.Empty
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
