using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
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
        private IThemeService ThemeService => App.Current.ThemeService;
        private readonly Window _window;
        private NavigationViewItemBase? _lastSelectedMenuItem;
        private readonly Dictionary<NavigationViewItemBase, (Type PageType, string Header)> _navigationMap;

        public SettingsRootPage(Window window)
        {
            InitializeComponent();
            _window = window;

            _window.SetTitleBar(WindowTitleBar);
            _window.Activated += SettingsWindow_Activated;
            _window.Closed += SettingsWindow_Closed;

            ApplyTheme();
            ThemeService.ThemeChanged += ThemeService_ThemeChanged;

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
        }

        private void ApplyTheme()
        {
            RequestedTheme = ThemeService.Theme;
            // TitleBarTheme has first Legacy value, we use + 1 to ignore it
            _window.AppWindow.TitleBar.PreferredTheme = (TitleBarTheme)(ThemeService.Theme + 1);
        }

        private void ThemeService_ThemeChanged(object? sender, ElementTheme e) => ApplyTheme();

        private void SettingsWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            VisualStateManager.GoToState(this, args.WindowActivationState == WindowActivationState.Deactivated ? "Deactivated" : "Activated", true);
        }

        private void SettingsWindow_Closed(object sender, WindowEventArgs args)
        {
            _window.Activated -= SettingsWindow_Activated;
            _window.Closed -= SettingsWindow_Closed;
            ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
        }

        private void NavigationViewPanel_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            var inputNonClient = InputNonClientPointerSource.GetForWindowId(_window.AppWindow.Id);

            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                VisualStateManager.GoToState(this, "Top", true);
                inputNonClient.SetRegionRects(NonClientRegionKind.Passthrough, []);
            }
            else if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                VisualStateManager.GoToState(this, "Compact", true);
                inputNonClient.SetRegionRects(NonClientRegionKind.Passthrough, [new(0, 0, 96, 48)]);
            }
            else
            {
                VisualStateManager.GoToState(this, "Default", true);
                inputNonClient.SetRegionRects(NonClientRegionKind.Passthrough, [new(0, 0, 48, 48)]);
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
    }
}
