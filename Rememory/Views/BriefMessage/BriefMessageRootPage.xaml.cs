using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using System.Timers;

namespace Rememory.Views.BriefMessage
{
    public sealed partial class BriefMessageRootPage : Page
    {
        private const int TimerInterval = 3000;
        private readonly DesktopWindowXamlSource _xamlSource;
        private readonly AppWindow _appWindow;
        private readonly Timer _timer;

        public BriefMessageRootPage(DesktopWindowXamlSource xamlSource, WindowId windowId)
        {
            InitializeComponent();

            _xamlSource = xamlSource;
            _appWindow = AppWindow.GetFromWindowId(windowId);

            _timer = new()
            {
                Interval = TimerInterval,
                AutoReset = false
            };
            _timer.Elapsed += Timer_Elapsed;

            Unloaded += BriefMessageRootPage_Unloaded;
        }

        public bool IsToolTipOpen => ToolTipMessage.IsOpen;

        public void OpenToolTip(string? iconGlyph)
        {
            _appWindow.Show(false);
            _xamlSource.SiteBridge.Show();

            if (string.IsNullOrEmpty(iconGlyph))
            {
                ToolTipIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                ToolTipIcon.Glyph = iconGlyph;
            }

            ToolTipMessage.IsOpen = true;
            _timer.Start();
        }

        public void ForceHideToolTip()
        {
            if (!ToolTipMessage.IsOpen)
            {
                return;
            }

            _timer.Stop();
            ToolTipMessage.IsOpen = false;
            _xamlSource.SiteBridge.Hide();
            _appWindow.Hide();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(ForceHideToolTip);
        }

        private void BriefMessageRootPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
        }
    }
}
