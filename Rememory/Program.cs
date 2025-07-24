using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WinRT;

namespace Rememory
{
    public class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ComWrappersSupport.InitializeComWrappers();
            AppActivationArguments activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            bool isRedirect;
            try
            {
                isRedirect = DecideRedirection(activationArgs);
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
            else if (SettingsContext.Instance.IsNotificationOnStartEnabled && activationArgs.Kind != ExtendedActivationKind.ToastNotification)
            {
                AppNotificationManager.Default.Show(new AppNotificationBuilder()
                    .AddText("AppNotification_AppIsRunning".GetLocalizedResource())
                    .AddText("AppNotification_UseShortcutToOpen".GetLocalizedFormatResource(
                        KeyboardHelper.ShortcutToString(SettingsContext.Instance.ActivationShortcut, "+")))
                    .BuildNotification());
            }

            return 0;
        }

        private static bool DecideRedirection(AppActivationArguments args)
        {
            bool isRedirect = false;
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
