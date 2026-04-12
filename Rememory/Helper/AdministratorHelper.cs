using System.Security.Principal;

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
    }
}
