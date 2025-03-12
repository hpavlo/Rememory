using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Services;
using Rememory.Views.Editor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Rememory.ViewModels
{
    /// <summary>
    /// View model for the clipboard window root page
    /// </summary>
    public class ClipboardRootPageViewModel : ObservableObject
    {
        // Services
        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>();
        private ICleanupDataService _cleanupDataService = App.Current.Services.GetService<ICleanupDataService>();
        private ISearchService _searchService = App.Current.Services.GetService<ISearchService>();
        private IOwnerAppService _ownerAppService = App.Current.Services.GetService<IOwnerAppService>();

        // Using to get last active window if clipboard window is pinned
        private ActiveWindowHook _activeWindowHook = new();

        // Using to get last active window before clipboard window is opened
        private IntPtr _lastActiveWindowHandleBeforeShowing = IntPtr.Zero;

        /// <summary>
        /// User settings context class.
        /// Stores all settings for the entire application
        /// </summary>
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private ObservableCollection<ClipboardItem> _itemsCollection;
        /// <summary>
        /// Visible collection on the main page
        /// </summary>
        public ObservableCollection<ClipboardItem> ItemsCollection
        {
            get => _itemsCollection;
            set => SetProperty(ref _itemsCollection, value);
        }

        private NavigationMenuItem _selectedMenuItem;
        /// <summary>
        /// Return <seealso cref="NavigationMenuItem"/> that user selected on the main page
        /// </summary>
        public NavigationMenuItem SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    SearchString = string.Empty;
                    OnPropertyChanged(nameof(IsSearchEnabled));
                    UpdateItemsList();
                }
            }
        }

        /// <summary>
        /// Return true if main clipboard window is pinned
        /// </summary>
        public bool IsWindowPinned
        {
            get => App.Current.ClipboardWindow.IsPinned;
            set
            {
                if (App.Current.ClipboardWindow.IsPinned != value)
                {
                    App.Current.ClipboardWindow.IsPinned = value;
                    // Use event hook only if window is pinned
                    if (value)
                    {
                        _activeWindowHook.AddEventHook();
                    }
                    else
                    {
                        _activeWindowHook.RemoveEventHook();
                    }
                    OnPropertyChanged(nameof(IsWindowPinned));
                }
            }
        }

        private bool _isClipboardMonitoringPaused = false;
        /// <summary>
        /// When it's true, clipboard manager doesn't save any data
        /// </summary>
        public bool IsClipboardMonitoringPaused
        {
            get => _isClipboardMonitoringPaused;
            set
            {
                if (SetProperty(ref _isClipboardMonitoringPaused, value))
                {
                    if (value)
                    {
                        _clipboardService.StopClipboardMonitor();
                    }
                    else
                    {
                        _clipboardService.StartClipboardMonitor();
                    }
                }
            }
        }

        // Disable search area if images filter is selected
        public bool IsSearchEnabled => SelectedMenuItem != NavigationMenuItem.Images;

        private bool _searchMode;
        /// <summary>
        /// Return true if <seealso cref="SearchString"/> contains search pattern
        /// </summary>
        public bool SearchMode
        {
            get => _searchMode;
            set
            {
                if (SetProperty(ref _searchMode, value))
                {
                    if (_searchMode)
                    {
                        _searchContext = ItemsCollection;
                        ItemsCollection = _searchBuffer;
                    }
                    else
                    {
                        ItemsCollection = _searchContext;
                        _searchContext = null;
                        _searchBuffer.Clear();
                    }
                }
            }
        }

        // We use this collection to save current items and search for elements within it 
        private ObservableCollection<ClipboardItem> _searchContext;

        // Saves all items we founded. Uses only in search mode
        private ObservableCollection<ClipboardItem> _searchBuffer = [];

        private string _searchString = string.Empty;
        /// <summary>
        /// Search pattern. Linked to the search box
        /// </summary>
        public string SearchString
        {
            get => _searchString;
            set
            {
                if (SetProperty(ref _searchString, value))
                {
                    if (string.IsNullOrWhiteSpace(_searchString))
                    {
                        _searchService?.StopSearching();
                        SearchMode = false;
                    }
                    else
                    {
                        SearchMode = true;
                        _searchService?.StartSearching(_searchContext, _searchString, ItemsCollection);
                    }
                }
            }
        }

        /// <summary>
        /// Collection of nodes using for item filtering by owner app
        /// </summary>
        public ObservableCollection<AppTreeViewNode> AppTreeViewNodes = [];
        /// <summary>
        /// First node in <see cref="AppTreeViewNodes"/> collection. Contains all apps
        /// </summary>
        public AppTreeViewNode RootAppNode;

        public ClipboardRootPageViewModel()
        {
            App.Current.ClipboardWindow.Showing += ClipboardWindow_Showing;

            _clipboardService.NewItemAdded += ClipboardService_NewItemAdded;
            _clipboardService.FavoriteItemChanged += ClipboardService_FavoriteItemChanged;
            _clipboardService.ItemMovedToTop += ClipboardService_ItemMovedToTop;
            _clipboardService.ItemDeleted += ClipboardService_ItemDeleted;
            _clipboardService.OldItemsDeleted += ClipboardService_OldItemsDeleted;
            _clipboardService.AllItemsDeleted += ClipboardService_AllItemsDeleted;
            _clipboardService.StartClipboardMonitor();

            RootAppNode = new AppTreeViewNode { Title = "FilterTreeViewTitle_Apps".GetLocalizedResource(), IsExpanded = true };
            AppTreeViewNodes.Add(RootAppNode);

            _ownerAppService.AppRegistered += OwnerAppService_AppRegistered;
            _ownerAppService.AppUnregistered += OwnerAppService_AppUnregistered;
            _ownerAppService.AllAppsUnregistered += OwnerAppService_AllAppsUnregistered;

            UpdateItemsList();
            CleanupOldData();
            InitializeCommands();
        }

        private void UpdateItemsList()
        {
            ItemsCollection?.Clear();
            ItemsCollection = [.. _clipboardService.ClipboardItems.Where(ItemFilterBySelectedMenu)];

            HashSet<string> distinctPathes = [.. ItemsCollection.Select(item => item.OwnerPath).Distinct()];
            // Update app filter tree view
            RootAppNode.Children.Clear();
            RootAppNode.Children = [.._ownerAppService.GetOwnerApps().Values
                .Where(app => distinctPathes.Contains(app.Path))
                .Select(app => new AppTreeViewNode(app))
                .OrderBy(app => app.Title)
                .ThenBy(app => app.OwnerPath)];
        }

        private bool ItemFilterBySelectedMenu(ClipboardItem item)
        {
            return SelectedMenuItem switch
            {
                NavigationMenuItem.Home => true,
                NavigationMenuItem.Fovorites => item.IsFavorite,
                NavigationMenuItem.Images => item.DataMap.ContainsKey(ClipboardFormat.Png),
                NavigationMenuItem.Links => item is ClipboardLinkItem,
                _ => false
            };
        }

        /// <summary>
        /// Check if the retention period of items has expired
        /// </summary>
        /// <returns>true if all items were checked</returns>
        private bool CleanupOldData() => _cleanupDataService.Cleanup();

        private void ClipboardWindow_Showing(object sender, EventArgs e)
        {
            _lastActiveWindowHandleBeforeShowing = NativeHelper.GetForegroundWindow();
        }

        # region ClipboardService events

        private void ClipboardService_NewItemAdded(object sender, ClipboardEventArgs a)
        {
            if (ItemFilterBySelectedMenu(a.ChangedClipboardItem)
                && RootAppNode.Children.Where(app => app.IsSelected).Select(app => app.OwnerPath).Contains(a.ChangedClipboardItem.OwnerPath))
            {
                SearchString = string.Empty;
                ItemsCollection.Insert(0, a.ChangedClipboardItem);
            }
        }

        private void ClipboardService_FavoriteItemChanged(object sender, ClipboardEventArgs a)
        {
            if (SelectedMenuItem == NavigationMenuItem.Fovorites && !a.ChangedClipboardItem.IsFavorite)
            {
                ItemsCollection.Remove(a.ChangedClipboardItem);
            }
        }

        private void ClipboardService_ItemMovedToTop(object sender, ClipboardEventArgs a)
        {
            int index = ItemsCollection.IndexOf(a.ChangedClipboardItem);
            if (index >= 0)
            {
                ItemsCollection.RemoveAt(index);
                ItemsCollection.Insert(0, a.ChangedClipboardItem);
            }
        }

        private void ClipboardService_ItemDeleted(object sender, ClipboardEventArgs a)
        {
            ItemsCollection.Remove(a.ChangedClipboardItem);
        }

        private void ClipboardService_OldItemsDeleted(object sender, ClipboardEventArgs a)
        {
            a.ChangedClipboardItems.ForEach(item => ItemsCollection.Remove(item));
        }

        private void ClipboardService_AllItemsDeleted(object sender, ClipboardEventArgs a)
        {
            ItemsCollection.Clear();
        }

        #endregion

        #region OwnerAppService events

        private void OwnerAppService_AppRegistered(object sender, OwnerApp a)
        {
            RootAppNode.Children = [.. RootAppNode.Children
                    .Append(new AppTreeViewNode(a))
                    .OrderBy(app => app.Title)
                    .ThenBy(app => app.OwnerPath)];
        }

        private void OwnerAppService_AppUnregistered(object sender, string a)
        {
            if (RootAppNode.Children.FirstOrDefault(app => string.Equals(app.OwnerPath, a)) is AppTreeViewNode nodeToRemove)
            {
                RootAppNode.Children.Remove(nodeToRemove);
            }
        }

        private void OwnerAppService_AllAppsUnregistered(object sender, EventArgs e)
        {
            RootAppNode.Children.Clear();
        }

        #endregion

        #region Called from View

        // Call it from view when window is showing
        public void OnWindowShowing()
        {
            CleanupOldData();

            // Update time on view
            foreach (var item in ItemsCollection)
            {
                item.UpdateProperty(nameof(item.Time));
            }
        }

        // Call it from view when window is hiding
        public void OnWindowHiding()
        {
            IsWindowPinned = false;
        }

        // Call it from view when user starts dragging
        public void OnDragItemStarting(ClipboardItem item, DataPackage dataPackage)
        {
            foreach (var itemData in item.DataMap)
            {
                try
                {
                    switch (itemData.Key)
                    {
                        case ClipboardFormat.Png:
                            dataPackage.SetData("PNG", File.OpenRead(itemData.Value).AsRandomAccessStream());
                            dataPackage.SetStorageItems([StorageFile.GetFileFromPathAsync(itemData.Value).AsTask().Result]);
                            break;
                        case ClipboardFormat.Text:
                            dataPackage.SetText(itemData.Value);
                            break;
                        case ClipboardFormat.Html:
                            dataPackage.SetData("HTML Format", File.OpenRead(itemData.Value).AsRandomAccessStream());
                            break;
                        case ClipboardFormat.Rtf:
                            dataPackage.SetData("Rich Text Format", File.OpenRead(itemData.Value).AsRandomAccessStream());
                            break;
                    }
                }
                catch { }
            }
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
        }

        // Call it from view when filter selection is changed
        public void OnFilterTreeViewSelectionChanged()
        {
            // Pre-calculate the filtered OwnerPaths
            HashSet<string> selectedOwnerPaths = [.. RootAppNode.Children
                .Where(app => app.IsSelected)
                .Select(app => app.OwnerPath)];

            // Apply the filters
            var filteredItems = _clipboardService.ClipboardItems
                .Where(item => ItemFilterBySelectedMenu(item) && selectedOwnerPaths.Contains(item.OwnerPath))
                .ToList();

            if (SearchMode)
            {
                _searchContext.Clear();
                _searchContext = [.. filteredItems];
                _searchService?.StartSearching(_searchContext, _searchString, ItemsCollection);
            }
            else
            {
                ItemsCollection?.Clear();
                ItemsCollection = [.. filteredItems];
            }
        }

        #endregion

        #region Commands

        public ICommand ChangeItemFavoriteCommand { get; private set; }
        public ICommand PasteItemCommand { get; private set; }
        public ICommand PastePlainTextItemCommand { get; private set; }
        public ICommand CopyItemCommand { get; private set; }
        public ICommand EditItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand AddOwnerToFiltersCommand { get; private set; }

        private void InitializeCommands()
        {
            ChangeItemFavoriteCommand = new RelayCommand<ClipboardItem>(_clipboardService.ChangeFavoriteItem);
            PasteItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item, paste: true));
            PastePlainTextItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item, ClipboardFormat.Text, true),
                item => item is not null && item.DataMap.ContainsKey(ClipboardFormat.Text));
            CopyItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item));
            EditItemCommand = new RelayCommand<ClipboardItem>(EditorWindow.ShowEditorWindow,
                item => item is not null && item.DataMap.ContainsKey(ClipboardFormat.Text) && !EditorWindow.TryGetEditorContext(out _));
            DeleteItemCommand = new RelayCommand<ClipboardItem>(_clipboardService.DeleteItem);
            AddOwnerToFiltersCommand = new RelayCommand<string>(AddOwnerToFilters, item =>
            {
                // check svchost.exe for UWP app sources
                return !string.IsNullOrEmpty(item) && !item.EndsWith("svchost.exe");
            });
        }

        private void SendDataToClipboard(ClipboardItem item, [Optional] ClipboardFormat? type, bool paste = false)
        {
            if (_clipboardService.SetClipboardData(item, type) && paste)
            {
                var windowToActivate = IntPtr.Zero;

                // Check case if we pinned window and right after we're trying to paste some item
                // In this case _activeWindowHook.LastActiveWindowHandle will be zero 
                if (IsWindowPinned)
                {
                    windowToActivate = _activeWindowHook.LastActiveWindowHandle != IntPtr.Zero
                        ? _activeWindowHook.LastActiveWindowHandle
                        : _lastActiveWindowHandleBeforeShowing;
                }

                if (windowToActivate != IntPtr.Zero)
                {
                    NativeHelper.SetForegroundWindow(windowToActivate);
                }
                else
                {
                    App.Current.ClipboardWindow.HideWindow();
                }

                Thread.Sleep(10);
                KeyboardHelper.MultiKeyAction([VirtualKey.Control, VirtualKey.V], KeyboardHelper.KeyAction.DownUp);
            }
            _clipboardService.MoveItemToTop(item);
        }

        private void AddOwnerToFilters(string sourcePath)
        {
            if (!SettingsContext.OwnerAppFilters.Any(filter => filter.Pattern.Equals(sourcePath)))
            {
                OwnerAppFilter filter = new();
                filter.Pattern = sourcePath;

                if (Path.Exists(sourcePath))
                {
                    filter.Name = FileVersionInfo.GetVersionInfo(sourcePath).ProductName ?? string.Empty;
                }

                SettingsContext.OwnerAppFilters.Add(filter);
                SettingsContext.OwnerAppFiltersSave();
            }
        }

        #endregion

        public enum NavigationMenuItem
        {
            Home,
            Fovorites,
            Images,
            Links
        }
    }
}
