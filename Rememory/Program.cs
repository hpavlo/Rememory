using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using Rememory.Helper;
using WinRT;
using Rememory.Models;

namespace Rememory
{
    public class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ComWrappersSupport.InitializeComWrappers();
            bool isRedirect;
            try
            {
                isRedirect = DecideRedirection();
            }
            catch (COMException)
            {
                NativeHelper.MessageBox(IntPtr.Zero,
                    "MessageBox_UnableToOpenApp/Text".GetLocalizedResource(),
                    "MessageBox_UnableToOpenApp/Caption".GetLocalizedResource(),
                    0);
                return 0;
            }
            
            if (!isRedirect)
            {
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App(args);
                });
            }
            else
            {
                AppNotificationManager.Default.Show(new AppNotificationBuilder()
                    .AddText("AppNotification_AppIsRunning".GetLocalizedResource())
                    .AddText("AppNotification_UseShortcutToOpen".GetLocalizedFormatResource(
                        KeyboardHelper.ShortcutToString(SettingsContext.Instance.ActivationShortcut, "+")))
                    .BuildNotification());
            }

            return 0;
        }

        private static bool DecideRedirection()
        {
            bool isRedirect = false;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            AppInstance keyInstance = AppInstance.FindOrRegisterForKey(Windows.ApplicationModel.Package.Current.DisplayName);
            if (!keyInstance.IsCurrent)
            {
                isRedirect = true;
                RedirectActivationTo(args, keyInstance);
            }

            return isRedirect;
        }

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
        {
            redirectEventHandle = NativeHelper.CreateEvent(IntPtr.Zero, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                NativeHelper.SetEvent(redirectEventHandle);
            });

            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = NativeHelper.CoWaitForMultipleObjects(CWMO_DEFAULT, INFINITE, 1, [redirectEventHandle], out uint handleIndex);
        }
    }
}
