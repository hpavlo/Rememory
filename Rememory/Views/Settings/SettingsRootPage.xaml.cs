using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Rememory.Contracts;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Rememory.Views.Settings
{
    public sealed partial class SettingsRootPage : Page
    {
        private readonly IThemeService _themeService = App.Current.ThemeService;
        private NavigationViewItemBase? _lastSelectedMenuItem;
        private readonly Dictionary<NavigationViewItemBase, (Type PageType, string Header)> _navigationMap;
        private readonly InputNonClientPointerSource? _inputNonClientPointerSource;

        public string Title { get; set; } = string.Empty;

        public SettingsRootPage(Window window)
        {
            InitializeComponent();

            window.SetTitleBar(WindowTitleBar);
            RequestedTheme = _themeService.Theme;
            _inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
            _navigationMap = new()
            {
                { GeneralMenuItem, (typeof(GeneralPage), "/Settings/PageTitle_General/Content".GetLocalizedResource()) },
                { PersonalizationMenuItem, (typeof(PersonalizationPage), "/Settings/PageTitle_Personalization/Content".GetLocalizedResource()) },
                { ClipboardMenuItem, (typeof(ClipboardPage), "/Settings/PageTitle_Clipboard/Content".GetLocalizedResource()) },
                { MetadataMenuItem, (typeof(MetadataPage), "/Settings/PageTitle_Metadata/Content".GetLocalizedResource()) },
                { TagsMenuItem, (typeof(TagsPage), "/Settings/PageTitle_Tags/Content".GetLocalizedResource()) },
                { StorageMenuItem, (typeof(StoragePage), "/Settings/PageTitle_Storage/Content".GetLocalizedResource()) },
                { FiltersMenuItem, (typeof(FiltersPage), "/Settings/PageTitle_Filters/Content".GetLocalizedResource()) },
                { AboutMenuItem, (typeof(AboutPage), "/Settings/PageTitle_About/Content".GetLocalizedResource()) }
            };

            SettingsWindow.WindowActivated += SettingsWindow_Activated;
            _themeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme e) => RequestedTheme = sender.Theme;

        private void SettingsWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            VisualStateManager.GoToState(this, args.WindowActivationState == WindowActivationState.Deactivated ? "Deactivated" : "Activated", true);
        }

        private void NavigationViewPanel_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                VisualStateManager.GoToState(this, "Top", true);
                _inputNonClientPointerSource?.SetRegionRects(NonClientRegionKind.Passthrough, []);
            }
            else if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                VisualStateManager.GoToState(this, "Compact", true);
                _inputNonClientPointerSource?.SetRegionRects(NonClientRegionKind.Passthrough, [new(0, 0, 96, 48)]);
            }
            else
            {
                VisualStateManager.GoToState(this, "Default", true);
                _inputNonClientPointerSource?.SetRegionRects(NonClientRegionKind.Passthrough, [new(0, 0, 48, 48)]);
            }
        }

        private void NavigationViewPanel_Loaded(object sender, RoutedEventArgs e)
        {
            NavigateTo((NavigationViewItemBase)NavigationViewPanel.SelectedItem);
        }

        private void NavigationViewPanel_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            NavigateTo(args.InvokedItemContainer, args.RecommendedNavigationTransitionInfo);
        }

        private void NavigationViewPanel_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            NavigationViewFrame.GoBack();

            var currentPage = NavigationViewFrame.CurrentSourcePageType;
            var currentMenuItem = _navigationMap.FirstOrDefault(pair => pair.Value.PageType == currentPage).Key;

            if (currentMenuItem != null)
            {
                sender.SelectedItem = currentMenuItem;
                sender.Header = _navigationMap[currentMenuItem].Header;
                _lastSelectedMenuItem = currentMenuItem;
            }
        }

        private void NavigateTo(NavigationViewItemBase navigationViewItem, [Optional] NavigationTransitionInfo navigationTransitionInfo)
        {
            if (navigationViewItem != _lastSelectedMenuItem &&
                _navigationMap.TryGetValue(navigationViewItem, out var navInfo))
            {
                var navOptions = new FrameNavigationOptions
                {
                    TransitionInfoOverride = navigationTransitionInfo,
                    IsNavigationStackEnabled = true
                };

                NavigationViewFrame.NavigateToType(navInfo.PageType, null, navOptions);
                NavigationViewPanel.Header = navInfo.Header;

                _lastSelectedMenuItem = navigationViewItem;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            NavigationViewFrame.Navigate(typeof(Page));
            SettingsWindow.WindowActivated -= SettingsWindow_Activated;
            _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            Bindings.StopTracking();
        }
    }
}
