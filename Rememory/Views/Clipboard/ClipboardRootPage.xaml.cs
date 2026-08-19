using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Rememory.Views.Clipboard
{
    public sealed partial class ClipboardRootPage : Page
    {
        public readonly ClipboardRootPageViewModel ViewModel = new();
        public Border WindowCaptionArea => WindowCaptionBorder;

        /// <summary>
        /// Contains all selected clips ordered by selection time.
        /// </summary>
        public List<ClipModel> OrderedSelectedClips { get; private set; } = [];

        private readonly IThemeService _themeService = App.Current.ThemeService;
        private readonly ClipboardWindow _window = App.Current.ClipboardWindow;

        public ClipboardRootPage()
        {
            InitializeComponent();
            _window.Showing += Window_Showing;
            _window.Hiding += Window_Hiding;
            _window.AppWindow.Closing += Window_Closing;

            RequestedTheme = _themeService.Theme;
            TriggerThemeBackgroundColor();
            _themeService.ThemeChanged += ThemeService_ThemeChanged;
            _themeService.WindowBackdropChanged += ThemeService_WindowBackdropChanged;

            ViewModel.SettingsContext.PropertyChanged += SettingsContext_PropertyChanged;
        }

        private void Window_Showing(object sender, EventArgs e)
        {
            if (ViewModel.SettingsContext.IsSearchFocusOnStartEnabled)
            {
                SearchBox.Focus(FocusState.Keyboard);
            }
            else
            {
                ClipsListViewControl.SetClipFocusedByIndex(0);
            }

            ClipsListViewControl.ScrollUpTheList();
            ViewModel.OnWindowShowing();
        }

        private void Window_Hiding(object sender, EventArgs e)
        {
            ViewModel.OnWindowHiding();
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            _window.Showing -= Window_Showing;
            _window.Hiding -= Window_Hiding;
            _window.AppWindow.Closing -= Window_Closing;
            _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            _themeService.WindowBackdropChanged -= ThemeService_WindowBackdropChanged;
            ViewModel.SettingsContext.PropertyChanged -= SettingsContext_PropertyChanged;
        }

        private void ThemeService_ThemeChanged(IThemeService sender, ElementTheme theme)
        {
            RequestedTheme = theme;
            TriggerThemeBackgroundColor();
        }

        private void ThemeService_WindowBackdropChanged(IThemeService sender, WindowBackdropType e) => TriggerThemeBackgroundColor();

        private void TriggerThemeBackgroundColor()
        {
            SolidColorBrush newBackgroundBrush = new();
            if (_themeService.WindowBackdrop == WindowBackdropType.None)
            {
                newBackgroundBrush.Color = _themeService.Theme switch
                {
                    ElementTheme.Light => Colors.White,
                    ElementTheme.Dark => Colors.Black,
                    _ => NativeHelper.ShouldSystemUseDarkMode() ? Colors.Black : Colors.White,
                };
            }
            Background = newBackgroundBrush;
        }

        private void SettingsContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs a)
        {
            switch (a.PropertyName)
            {
                case nameof(ViewModel.SettingsContext.IsSearchFocusOnStartEnabled):
                    // Swap tab indexes between SearchBox and ListView
                    (ClipsListViewControl.TabIndex, SearchBox.TabIndex) = (SearchBox.TabIndex, ClipsListViewControl.TabIndex);
                    break;
            }
        }

        // Do not use preview key down event to avoid window hiding if key pressed on flyout
        private void RootPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                _window.HideWindow();
            }
        }

        #region Apps filter

        private void FilterTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Works only for two layers tree view
            // It adds selected items from second (child) layer
            foreach (var app in ViewModel.RootAppNode.Children.Where(app => app.IsSelected))
            {
                FilterTreeView.SelectedItems.Add(app);
            }
        }

        private void FilterTreeView_Unloaded(object sender, RoutedEventArgs e)
        {
            FilterTreeView.SelectedItems.Clear();
        }

        private void FilterTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            // IsSelected property binding doesn't working for now
            // We should do it manually
            foreach (var item in args.AddedItems.Cast<AppTreeViewNode>())
            {
                item.IsSelected = true;
            }

            foreach (var item in args.RemovedItems.Cast<AppTreeViewNode>())
            {
                item.IsSelected = false;
            }

            ViewModel.OnFilterTreeViewSelectionChanged();
        }

        #endregion

        private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Down:
                    ClipsListViewControl.SetClipFocusedByIndex(0);
                    e.Handled = true;
                    break;
                case VirtualKey.Escape:
                    _window.HideWindow();
                    e.Handled = true;
                    break;
            }
        }

        private void NavigationTabList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Right && e.OriginalSource is NavigationViewItem)
            {
                ClipsListViewControl.SetClipFocusedByIndex(0);
            }
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SettingsContext.SkipWarningMessageOnMainWindowClipsErase)
            {
                if (ViewModel.EraseClipsOnSelectedTabCommand.CanExecute(null))
                {
                    ViewModel.EraseClipsOnSelectedTabCommand.Execute(null);
                }
            }
            else
            {
                EraseButtonFlyout.ShowAt(sender as FrameworkElement);
            }
        }

        private void EraseFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            EraseButtonFlyout.Hide();

            if (ViewModel.EraseClipsOnSelectedTabCommand.CanExecute(null))
            {
                ViewModel.EraseClipsOnSelectedTabCommand.Execute(null);
            }
        }

        private void ClipsListView_RequestSearchBoxFocus(object sender, EventArgs e)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void ClipsListView_RequestNavigationTabViewFocus(object sender, EventArgs e)
        {
            var selectedItemContainer = NavigationTabView.ContainerFromMenuItem(NavigationTabView.SelectedItem) as UIElement;
            selectedItemContainer?.Focus(FocusState.Programmatic);
        }
    }
}
