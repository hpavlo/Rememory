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
    public class ClipboardRootPageViewModel : ObservableObject
    {
        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>();
        private ICleanupDataService _cleanupDataService = App.Current.Services.GetService<ICleanupDataService>();
        private ISearchService _searchService = App.Current.Services.GetService<ISearchService>();

        public SettingsContext SettingsContext => SettingsContext.Instance;

        private ObservableCollection<ClipboardItem> _itemsCollection;
        public ObservableCollection<ClipboardItem> ItemsCollection
        {
            get => _itemsCollection;
            set => SetProperty(ref _itemsCollection, value);
        }

        private NavigationMenuItem _selectedMenuItem;
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
        public bool IsSearchEnabled => SelectedMenuItem != NavigationMenuItem.Images;

        private bool _searchMode;
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

        private ObservableCollection<ClipboardItem> _searchContext;
        private ObservableCollection<ClipboardItem> _searchBuffer = [];
        private string _searchString = string.Empty;
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

        public void OnWindowActivated()
        {
            if (CleanupOldData())
            {
                UpdateItemsList();
                return;
            }

            foreach (var item in ItemsCollection)
            {
                item.UpdateProperty(nameof(item.Time));
            }
        }

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
