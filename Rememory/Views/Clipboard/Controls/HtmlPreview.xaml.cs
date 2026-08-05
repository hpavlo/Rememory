using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Rememory.Models;
using System;
using System.Linq;

namespace Rememory.Views.Clipboard.Controls
{
    /// <summary>
    /// Used only for Clip preview flyout.
    /// Do not use int the clips list to preview HTML data
    /// </summary>
    public sealed partial class HtmlPreview : DataPreviewBase
    {
        private const int CleanUpDelayMinutes = 5;
        private readonly DispatcherQueueTimer _cleanupTimer;
        private WebView2? _webViewBlock;
        private string _lastAllowedUriToNavigate = string.Empty;

        public HtmlPreview()
        {
            InitializeComponent();
            _cleanupTimer = App.Current.DispatcherQueue.CreateTimer();
            _cleanupTimer.Interval = TimeSpan.FromMinutes(CleanUpDelayMinutes);
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (args.NewValue is DataModel clipData)
            {
                StopCleanupTimer();

                App.Current.DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        _webViewBlock ??= RootGrid.Children.OfType<WebView2>().FirstOrDefault();
                        if (_webViewBlock is null)
                        {
                            _webViewBlock = new WebView2();
                            RootGrid.Children.Add(_webViewBlock);

                            await _webViewBlock.EnsureCoreWebView2Async();
                            WebViewSettingsConfigure(_webViewBlock.CoreWebView2.Settings);

                            _webViewBlock.NavigationStarting += WebView_NavigationStarting;
                            _webViewBlock.CoreWebView2.NewWindowRequested += WebView_CoreWebView2_NewWindowRequested;
                        }

                        NavigateTo(clipData.Data);
                    }
                    catch { }
                });
            }
            else
            {
                StartCleanupTimer();
                NavigateTo("about:blank");
            }
        }

        private void StartCleanupTimer()
        {
            StopCleanupTimer();
            _cleanupTimer.Tick += CleanupTimer_Tick;
            _cleanupTimer.Start();
        }

        private void StopCleanupTimer()
        {
            _cleanupTimer.Stop();
            _cleanupTimer.Tick -= CleanupTimer_Tick;
        }

        private void NavigateTo(string uriString)
        {
            var uri = new Uri(uriString);
            _lastAllowedUriToNavigate = uri.AbsoluteUri;
            _webViewBlock?.Source = uri;
        }

        private void CleanupTimer_Tick(DispatcherQueueTimer timer, object e)
        {
            StopCleanupTimer();

            // Safely close and dispose of the WebView
            _webViewBlock ??= RootGrid.Children.OfType<WebView2>().FirstOrDefault();
            if (_webViewBlock is not null)
            {
                _webViewBlock.NavigationStarting -= WebView_NavigationStarting;
                _webViewBlock.CoreWebView2.NewWindowRequested -= WebView_CoreWebView2_NewWindowRequested;
                _webViewBlock.Close();

                RootGrid.Children.Remove(_webViewBlock);

                // We should create new WebView2 component each time after Close the old one
                _webViewBlock = null;
            }
        }

        private void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (_lastAllowedUriToNavigate == args.Uri)
            {
                _lastAllowedUriToNavigate = string.Empty;
            }
            else
            {
                args.Cancel = true;
            }
        }

        private void WebView_CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;
        }

        private static void WebViewSettingsConfigure(CoreWebView2Settings settings)
        {
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreHostObjectsAllowed = false;
            settings.AreDevToolsEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            settings.IsBuiltInErrorPageEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsReputationCheckingRequired = false;
            settings.IsStatusBarEnabled = false;
            settings.IsScriptEnabled = false;
            settings.IsWebMessageEnabled = false;
            settings.IsPinchZoomEnabled = true;
            settings.IsZoomControlEnabled = true;
        }
    }
}
