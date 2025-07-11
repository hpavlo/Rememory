using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Rememory.Views.Controls;

[TemplatePart(Name = NavigationTabListScrollViewerName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = NavigationTabListScrollUpButtonName, Type = typeof(ButtonBase))]
[TemplatePart(Name = NavigationTabListScrollDownButtonName, Type = typeof(ButtonBase))]
public sealed partial class NavigationTabList : ListView
{
    private const string NavigationTabListScrollViewerName = "ScrollViewer";
    private const string NavigationTabListScrollUpButtonName = "ScrollUpButton";
    private const string NavigationTabListScrollDownButtonName = "ScrollDownButton";

    private ScrollViewer? _navigationTabListScrollViewer;
    private ButtonBase? _navigationTabListScrollUpButton;
    private ButtonBase? _navigationTabListScrollDownButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        SizeChanged += NavigationTabList_SizeChanged;
        
        if (_navigationTabListScrollViewer is not null)
        {
            _navigationTabListScrollViewer.Loaded -= ScrollViewer_Loaded;
        }

        _navigationTabListScrollViewer = GetTemplateChild(NavigationTabListScrollViewerName) as ScrollViewer;

        if (_navigationTabListScrollViewer is not null)
        {
            _navigationTabListScrollViewer.Loaded += ScrollViewer_Loaded;
        }
    }

    private void UpdateScrollButtonsVisibility()
    {
        if (_navigationTabListScrollDownButton is not null && _navigationTabListScrollViewer is not null)
        {
            if (_navigationTabListScrollViewer.ScrollableHeight - _navigationTabListScrollDownButton.ActualHeight > 0)
            {
                _navigationTabListScrollDownButton.Visibility = Visibility.Visible;
            }
            else
            {
                _navigationTabListScrollDownButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void NavigationTabList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScrollButtonsVisibility();
    }

    private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_navigationTabListScrollViewer is not null)
        {
            _navigationTabListScrollViewer.Loaded -= ScrollViewer_Loaded;
        }

        if (_navigationTabListScrollUpButton is not null)
        {
            _navigationTabListScrollUpButton.Click -= ScrollUpButton_Click;
        }
        if (_navigationTabListScrollDownButton is not null)
        {
            _navigationTabListScrollDownButton.Click -= ScrollDownButton_Click;
        }

        if (_navigationTabListScrollViewer is not null)
        {
            _navigationTabListScrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            _navigationTabListScrollUpButton = _navigationTabListScrollViewer.FindDescendant(NavigationTabListScrollUpButtonName) as ButtonBase;
            _navigationTabListScrollDownButton = _navigationTabListScrollViewer.FindDescendant(NavigationTabListScrollDownButtonName) as ButtonBase;
        }

        if (_navigationTabListScrollUpButton is not null)
        {
            _navigationTabListScrollUpButton.Click += ScrollUpButton_Click;
        }
        if (_navigationTabListScrollDownButton is not null)
        {
            _navigationTabListScrollDownButton.Click += ScrollDownButton_Click;
        }

        UpdateScrollButtonsVisibility();
    }

    private void ScrollViewer_ViewChanging(object? sender, ScrollViewerViewChangingEventArgs e)
    {
        if (_navigationTabListScrollUpButton is not null)
        {
            if (e.FinalView.VerticalOffset < 1)
            {
                _navigationTabListScrollUpButton.Visibility = Visibility.Collapsed;
            }
            else if (e.FinalView.VerticalOffset > 1)
            {
                _navigationTabListScrollUpButton.Visibility = Visibility.Visible;
            }
        }

        if (_navigationTabListScrollDownButton is not null && _navigationTabListScrollViewer is not null)
        {
            if (e.FinalView.VerticalOffset > _navigationTabListScrollViewer.ScrollableHeight - 1)
            {
                _navigationTabListScrollDownButton.Visibility = Visibility.Collapsed;
            }
            else if (e.FinalView.VerticalOffset < _navigationTabListScrollViewer.ScrollableHeight - 1)
            {
                _navigationTabListScrollDownButton.Visibility = Visibility.Visible;
            }
        }
    }

    private void ScrollUpButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationTabListScrollViewer?.ChangeView(null, _navigationTabListScrollViewer.VerticalOffset - _navigationTabListScrollViewer.ViewportHeight, null);
    }

    private void ScrollDownButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationTabListScrollViewer?.ChangeView(null, _navigationTabListScrollViewer.VerticalOffset + _navigationTabListScrollViewer.ViewportHeight, null);
    }
}
