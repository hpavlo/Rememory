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
        private IThemeService ThemeService => App.Current.ThemeService;
        private readonly Window _window;
        private NavigationViewItemBase _prevSelectedMenuItem;
        private readonly Dictionary<NavigationViewItemBase, (Type PageType, string Header)> _navigationMap;

        public SettingsRootPage(Window window)
        {
            this.InitializeComponent();
            _window = window;

            _window.SetTitleBar(WindowTitleBar);
            _window.Activated += SettingsWindow_Activated;

            ApplyTheme();
            ThemeService.ThemeChanged += (s, a) => ApplyTheme();

            _navigationMap = new()
            {
                { GeneralMenuItem, (typeof(GeneralPage), "SettingsPageTitle_General/Content".GetLocalizedResource()) },
                { PersonalizationMenuItem, (typeof(PersonalizationPage), "SettingsPageTitle_Personalization/Content".GetLocalizedResource()) },
                { StorageMenuItem, (typeof(StoragePage), "SettingsPageTitle_Storage/Content".GetLocalizedResource()) },
                { AboutMenuItem, (typeof(AboutPage), "SettingsPageTitle_About/Content".GetLocalizedResource()) }
            };
        }

        private void ApplyTheme()
        {
            RequestedTheme = ThemeService.Theme;
        }

        private void SettingsWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            VisualStateManager.GoToState(this,
                args.WindowActivationState == WindowActivationState.Deactivated ? "Deactivated" : "Activated",
                true);
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
                _prevSelectedMenuItem = currentMenuItem;
            }
        }

        private void NavigateTo(NavigationViewItemBase navigationViewItem, [Optional] NavigationTransitionInfo navigationTransitionInfo)
        {
            if (navigationViewItem != _prevSelectedMenuItem &&
                _navigationMap.TryGetValue(navigationViewItem, out var navInfo))
            {
                var navOptions = new FrameNavigationOptions
                {
                    TransitionInfoOverride = navigationTransitionInfo,
                    IsNavigationStackEnabled = true
                };

                NavigationViewFrame.NavigateToType(navInfo.PageType, null, navOptions);
                NavigationViewPanel.Header = navInfo.Header;

                _prevSelectedMenuItem = navigationViewItem;
            }
        }
    }
}
