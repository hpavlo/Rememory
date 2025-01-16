using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Service;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            set {
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    SearchString = string.Empty;
                    OnPropertyChanged(nameof(IsSearchEnabled));
                    UpdateItemsList();
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
            set {
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
                        ItemsCollection.Clear();
                        _searchService?.StartSearching(_searchContext, _searchString, ItemsCollection.Add);
                    }
                }
            }
        }

        public ClipboardRootPageViewModel()
        {
            _clipboardService.NewItemAdded += (s, a) =>
            {
                if (ItemFilter(a.ChangedClipboardItem))
                {
                    SearchString = string.Empty;
                    ItemsCollection.Insert(0, a.ChangedClipboardItem);
                }
            };
            _clipboardService.FavoriteItemChanged += (s, a) =>
            {
                if (SelectedMenuItem == NavigationMenuItem.Fovorites && !a.ChangedClipboardItem.IsFavorite)
                {
                    ItemsCollection.Remove(a.ChangedClipboardItem);
                }
            };
            _clipboardService.ItemMovedToTop += (s, a) =>
            {
                int index = ItemsCollection.IndexOf(a.ChangedClipboardItem);
                if (index >= 0)
                {
                    ItemsCollection.RemoveAt(index);
                    ItemsCollection.Insert(0, a.ChangedClipboardItem);
                }
            };
            _clipboardService.ItemDeleted += (s, a) =>
            {
                ItemsCollection.Remove(a.ChangedClipboardItem);
            };
            _clipboardService.OldItemsDeleted += (s, a) =>
            {
                UpdateItemsList();
            };
            _clipboardService.StartClipboardMonitor();
            CleanupOldData();
            InitializeCommands();
        }

        // Call it from view when window is activated
        public void OnWindowActivated()
        {
            CleanupOldData();

            // Update time on view
            foreach (var item in ItemsCollection)
            {
                item.UpdateProperty(nameof(item.Time));
            }
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

        /// <summary>
        /// Check if the retention period of items has expired
        /// </summary>
        /// <returns>true if all items were checked</returns>
        private bool CleanupOldData() => _cleanupDataService.Cleanup();

        private void UpdateItemsList()
        {
            ItemsCollection?.Clear();
            ItemsCollection = new(_clipboardService.ClipboardItems.Where(ItemFilter));
        }

        private bool ItemFilter(ClipboardItem item)
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

        #region Commands

        public ICommand ChangeItemFavoriteCommand { get; private set; }
        public ICommand PasteItemCommand { get; private set; }
        public ICommand PastePlainTextItemCommand { get; private set; }
        public ICommand CopyItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }

        private void InitializeCommands()
        {
            ChangeItemFavoriteCommand = new RelayCommand<ClipboardItem>(_clipboardService.ChangeFavoriteItem);
            PasteItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item, true));
            PastePlainTextItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item, true, ClipboardFormat.Text),
                item => item is not null && item.DataMap.ContainsKey(ClipboardFormat.Text));
            CopyItemCommand = new RelayCommand<ClipboardItem>(item => SendDataToClipboard(item));
            DeleteItemCommand = new RelayCommand<ClipboardItem>(_clipboardService.DeleteItem);
        }

        private void SendDataToClipboard(ClipboardItem item, bool paste = false, ClipboardFormat? type = null)
        {
            if (_clipboardService.SetClipboardData(item, type) && paste)
            {
                App.Current.HideClipboardWindow();
                Thread.Sleep(10);
                KeyboardHelper.MultiKeyAction([VirtualKey.Control, VirtualKey.V], KeyboardHelper.KeyAction.DownUp);
            }
            _clipboardService.MoveItemToTop(item);
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
