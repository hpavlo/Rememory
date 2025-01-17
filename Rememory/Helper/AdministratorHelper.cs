using System.Diagnostics;
using System;
using System.Security.Principal;
using System.Reflection;

namespace Rememory.Helper
{
    public static class AdministratorHelper
    {
        private static bool? _isRunningAsAdministrator;

        public static bool IsAppRunningAsAdministrator()
        {
            if (!_isRunningAsAdministrator.HasValue)
            {
                var windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                _isRunningAsAdministrator = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return _isRunningAsAdministrator.Value; 
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
