using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Storage.Pickers;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views;
using Rememory.Views.Editor;
using Rememory.Views.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace Rememory.ViewModels
{
    /// <summary>
    /// ViewModel for the main clipboard history page/view.
    /// Manages the collection of clips, handles filtering, searching, UI commands,
    /// and orchestrates interactions with various application services.
    /// </summary>
    public partial class ClipboardRootPageViewModel : ObservableObject
    {
        // Services
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;
        private readonly ICleanupDataService _cleanupDataService = App.Current.Services.GetService<ICleanupDataService>()!;
        private readonly ISearchService _searchService = App.Current.Services.GetService<ISearchService>()!;
        private readonly IOwnerService _ownerService = App.Current.Services.GetService<IOwnerService>()!;
        private readonly ITagService _tagService = App.Current.Services.GetService<ITagService>()!;

        // Using to get last active window if clipboard window is pinned
        private readonly ActiveWindowHook _activeWindowHook = new();

        // Using to get last active window before clipboard window is opened
        private IntPtr _lastActiveWindowHandleBeforeShowing = IntPtr.Zero;

        /// <summary>
        /// User settings context class.
        /// Stores all settings for the entire application
        /// </summary>
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private ObservableCollection<ClipModel> _clipsCollection = [];
        /// <summary>
        /// Visible collection on the main page
        /// </summary>
        public ObservableCollection<ClipModel> ClipsCollection
        {
            get => _clipsCollection;
            set => SetProperty(ref _clipsCollection, value);
        }

        /// <summary>
        /// Contains all navigation tabs
        /// </summary>
        public ObservableCollection<TabItemModel> NavigationTabItems { get; private set; } = [];

        // Backing field for SelectedTab
        private TabItemModel? _selectedTab;
        /// <summary>
        /// Gets or sets the currently selected tab item, which determinates the promafy filter applied to the clips
        /// </summary>
        public TabItemModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    SearchString = string.Empty;
                    OnPropertyChanged(nameof(IsSearchEnabled));
                    OnPropertyChanged(nameof(SelectedTabHeader));
                    UpdateClipsList();
                }
            }
        }
        public string SelectedTabHeader => SelectedTab?.Title ?? string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the main clipboard window is currently pinned (always on top).
        /// Manages the activation of the active window hook when pinning changes.
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
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// When it's false, clipboard manager doesn't save any data
        /// </summary>
        public bool IsClipboardMonitoringEnabled
        {
            get => SettingsContext.IsClipboardMonitoringEnabled;
            set
            {
                if (IsClipboardMonitoringEnabled != value)
                {
                    SettingsContext.IsClipboardMonitoringEnabled = value;
                    if (value)
                    {
                        _clipboardService.StartClipboardMonitor(App.Current.ClipboardWindowHandle);
                    }
                    else
                    {
                        _clipboardService.StopClipboardMonitor(App.Current.ClipboardWindowHandle);
                    }
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the search input should be enabled.
        /// Typically disabled for filter types where search is not applicable (e.g., Images).
        /// </summary>
        public bool IsSearchEnabled => SelectedTab?.Type != NavigationTabItemType.Images;

        private bool _searchMode = false;
        /// <summary>
        /// Return true if <see cref="SearchString"/> contains search pattern
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
                        _searchContext = ClipsCollection;
                        ClipsCollection = _searchBuffer;
                    }
                    else
                    {
                        ClipsCollection = _searchContext;
                        _searchContext = [];
                        _searchBuffer.Clear();
                    }
                }
            }
        }

        // We use this collection to save current clips and search for elements within it 
        private ObservableCollection<ClipModel> _searchContext = [];

        // Saves all clips we have founded. Uses only in search mode
        private readonly ObservableCollection<ClipModel> _searchBuffer = [];

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
                        _searchService?.StartSearching(_searchContext, _searchString, ClipsCollection);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the collection of tree view nodes used for filtering clips by owner application.
        /// </summary>
        public ObservableCollection<AppTreeViewNode> AppTreeViewNodes { get; } = [];

        /// <summary>
        /// Gets the root node in the <see cref="AppTreeViewNodes"/> collection, representing "All Apps".
        /// </summary>
        public AppTreeViewNode RootAppNode { get; private set; }

        public ClipboardRootPageViewModel()
        {
            NavigationTabItemsInit();

            _tagService.TagRegistered += TagService_TagRegistered;
            _tagService.TagUnregistered += TagService_TagUnregistered;

            App.Current.ClipboardWindow.Showing += ClipboardWindow_Showing;

            _clipboardService.NewClipAdded += ClipboardService_NewClipAdded;
            _clipboardService.FavoriteClipChanged += ClipboardService_FavoriteClipChanged;
            _clipboardService.ClipMovedToTop += ClipboardService_ClipMovedToTop;
            _clipboardService.ClipDeleted += ClipboardService_ClipDeleted;
            _clipboardService.AllClipsDeleted += ClipboardService_AllClipsDeleted;
            if (IsClipboardMonitoringEnabled)
            {
                _clipboardService.StartClipboardMonitor(App.Current.ClipboardWindowHandle);
            }

            RootAppNode = new AppTreeViewNode { Title = "/Clipboard/FilterTreeView_Apps/Text".GetLocalizedResource(), IsExpanded = true };
            AppTreeViewNodes.Add(RootAppNode);

            _ownerService.OwnerRegistered += OwnerService_OwnerRegistered;
            _ownerService.OwnerUnregistered += OwnerService_OwnerUnregistered;
            _ownerService.AllOwnersUnregistered += OwnerService_AllOwnersUnregistered;

            UpdateClipsList();
            CleanupOldData();
        }

        public IList<TagModel> GetTags() => _tagService.Tags;

        private void NavigationTabItemsInit()
        {
            NavigationTabItems = [
                new("/Clipboard/NavigationTab_Home/Text".GetLocalizedResource(), "\uE80F", NavigationTabItemType.Home),
                new("/Clipboard/NavigationTab_Favorites/Text".GetLocalizedResource(), "\uE734", NavigationTabItemType.Fovorites),
                new("/Clipboard/NavigationTab_Images/Text".GetLocalizedResource(), "\uE8B9", NavigationTabItemType.Images),
                new("/Clipboard/NavigationTab_Files/Text".GetLocalizedResource(), "\uE8B7", NavigationTabItemType.Files),
                new("/Clipboard/NavigationTab_Links/Text".GetLocalizedResource(), "\uE71B", NavigationTabItemType.Links),
            ];

            _selectedTab = NavigationTabItems.First();

            foreach (var tag in _tagService.Tags)
            {
                NavigationTabItems.Add(new(tag));
            }
        }

        private void UpdateClipsList()
        {
            ClipsCollection?.Clear();
            ClipsCollection = [.. _clipboardService.Clips.Where(ClipFilterBySelectedMenu)];

            HashSet<string> distinctPaths = [.. ClipsCollection.Select(item => item.Owner?.Path).Distinct()];
            // Update app filter tree view
            RootAppNode.Children.Clear();
            RootAppNode.Children = [.._ownerService.Owners.Values
                .Where(owner => distinctPaths.Contains(owner.Path))
                .Select(owner => new AppTreeViewNode(owner))
                .OrderBy(owner => owner.Title)
                .ThenBy(owner => owner.OwnerPath)];
        }

        /// <summary>
        /// Filters a single <see cref="ClipModel"/> based on the currently selected navigation tab.
        /// </summary>
        /// <param name="item">The clip to check.</param>
        /// <returns><c>true</c> if the clip matches the current filter; otherwise, <c>false</c>.</returns>
        private bool ClipFilterBySelectedMenu(ClipModel? item)
        {
            if (item is null)
            {
                return false;
            }

            return SelectedTab?.Type switch
            {
                NavigationTabItemType.Home => true,
                NavigationTabItemType.Fovorites => item.IsFavorite,
                NavigationTabItemType.Images => item.Data.ContainsKey(ClipboardFormat.Png) || item.Data.ContainsKey(ClipboardFormat.Bitmap),
                NavigationTabItemType.Files => item.Data.ContainsKey(ClipboardFormat.Files),
                NavigationTabItemType.Links => item.IsLink,
                NavigationTabItemType.Tag => SelectedTab.Tag is not null && item.Tags.Contains(SelectedTab.Tag),
                _ => false
            };
        }

        /// <summary>
        /// Check if the retention period of clips has expired
        /// </summary>
        private void CleanupOldData() => _cleanupDataService.CleanupByRetentionPeriod();

        private void ClipboardWindow_Showing(ClipboardWindow sender, EventArgs args)
        {
            _lastActiveWindowHandleBeforeShowing = NativeHelper.GetForegroundWindow();
        }

        #region TabService events

        private void TagService_TagRegistered(object? sender, TagModel tagModel)
        {
            NavigationTabItems.Add(new(tagModel));
        }

        private void TagService_TagUnregistered(object? sender, int tagId)
        {
            TabItemModel? tabItemToRemove = NavigationTabItems.FirstOrDefault(tab => tab.IsTag && tab.Tag?.Id == tagId);
            if (tabItemToRemove is not null)
            {
                NavigationTabItems.Remove(tabItemToRemove);
            }
        }

        #endregion

        #region ClipboardService events

        private void ClipboardService_NewClipAdded(object? sender, ClipboardEventArgs a)
        {
            // Check if the new clip matches the current filters
            if (ClipFilterBySelectedMenu(a.ChangedClip)
                && RootAppNode.Children.Where(app => app.IsSelected).Select(app => app.OwnerPath).Contains(a.ChangedClip.Owner?.Path))
            {
                // Insert in main collection only
                if (SearchMode)
                {
                    _searchContext.Insert(0, a.ChangedClip);
                }
                else
                {
                    ClipsCollection.Insert(0, a.ChangedClip);
                }
            }
        }

        private void ClipboardService_FavoriteClipChanged(object? sender, ClipboardEventArgs a)
        {
            // If the user is currently viewing the Favorites list and the item is no longer a favorite, remove it
            if (SelectedTab?.Type == NavigationTabItemType.Fovorites && !a.ChangedClip.IsFavorite)
            {
                ClipsCollection.Remove(a.ChangedClip);
            }
        }

        private void ClipboardService_ClipMovedToTop(object? sender, ClipboardEventArgs a)
        {
            // If the moved clip is in current collection, move it
            if (ClipsCollection.Remove(a.ChangedClip))
            {
                ClipsCollection.Insert(0, a.ChangedClip);
            }

            // If we are in search mode, move clip in main context collection too
            if (SearchMode && _searchContext.Remove(a.ChangedClip))
            {
                _searchContext.Insert(0, a.ChangedClip);
            }
        }

        private void ClipboardService_ClipDeleted(object? sender, ClipboardEventArgs a)
        {
            ClipsCollection.Remove(a.ChangedClip);
            _ = SearchMode && _searchContext.Remove(a.ChangedClip);
        }

        private void ClipboardService_AllClipsDeleted(object? sender, ClipboardEventArgs a)
        {
            SearchMode = false;
            ClipsCollection.Clear();
        }

        #endregion

        #region OwnerAppService events

        private void OwnerService_OwnerRegistered(object? sender, OwnerModel owner)
        {
            RootAppNode.Children = [.. RootAppNode.Children
                    .Append(new AppTreeViewNode(owner))
                    .OrderBy(app => app.Title)
                    .ThenBy(app => app.OwnerPath)];
        }

        private void OwnerService_OwnerUnregistered(object? sender, string a)
        {
            if (RootAppNode.Children.FirstOrDefault(app => string.Equals(app.OwnerPath, a)) is AppTreeViewNode nodeToRemove)
            {
                RootAppNode.Children.Remove(nodeToRemove);
            }
        }

        private void OwnerService_AllOwnersUnregistered(object? sender, EventArgs e)
        {
            RootAppNode.Children.Clear();
        }

        #endregion

        #region Called from View

        /// <summary>
        /// Performs actions when the associated window is about to be shown.
        /// </summary>
        public void OnWindowShowing()
        {
            // Update relative timestamps displayed in the UI.
            foreach (var item in ClipsCollection)
            {
                item.TogglePropertyUpdate(nameof(item.ClipTime));
            }
        }

        /// <summary>
        /// Performs actions when the associated window is hiding.
        /// </summary>
        public void OnWindowHiding()
        {
            CleanupOldData();

            if (!SettingsContext.IsRememberWindowPinStateEnabled)
            {
                IsWindowPinned = false;
            }

            if (SettingsContext.IsClearSearchOnOpenEnabled)
            {
                SearchString = string.Empty;
            }

            if (SettingsContext.IsSetInitialTabOnOpenEnabled)
            {
                SelectedTab = NavigationTabItems.First();
            }
        }

        /// <summary>
        /// Prepares the <see cref="DataPackage"/> for a drag-and-drop operation starting from a clip.
        /// </summary>
        /// <param name="clip">The <see cref="ClipModel"/> being dragged.</param>
        /// <param name="dataPackage">The <see cref="DataPackage"/> to populate.</param>
        public async Task OnDragClipStartingAsync(ClipModel clip, DataPackage dataPackage)
        {
            List<IStorageItem> storageItems = [];
            bool hasDraggableData = false;

            foreach (var dataItem in clip.Data)
            {
                try
                {
                    if (dataItem.Value.IsFile() && !clip.IsLink)
                    {
                        var storageFile = await StorageFile.GetFileFromPathAsync(dataItem.Value.Data)
                            .AsTask().ConfigureAwait(false);
                        using var storageStream = await storageFile.OpenReadAsync()
                            .AsTask().ConfigureAwait(false);

                        switch (dataItem.Key)
                        {
                            case ClipboardFormat.Html:
                                dataPackage.SetData(ClipboardFormat.Html.GetDescription(), storageStream);
                                break;
                            case ClipboardFormat.Rtf:
                                dataPackage.SetData(ClipboardFormat.Rtf.GetDescription(), storageStream);
                                break;
                            case ClipboardFormat.Png:
                                dataPackage.SetData(ClipboardFormat.Png.GetDescription(), storageStream);
                                break;
                            case ClipboardFormat.Bitmap:
                                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(storageStream));
                                break;
                        }

                        hasDraggableData = true;
                    }
                    else
                    {
                        switch (dataItem.Key)
                        {
                            case ClipboardFormat.Text:
                                dataPackage.SetText(dataItem.Value.Data);
                                if (clip.IsLink)
                                {
                                    dataPackage.SetWebLink(new Uri(dataItem.Value.Data));
                                }
                                hasDraggableData = true;
                                break;
                            case ClipboardFormat.Files when dataItem.Value.Metadata is FilesMetadataModel filesMetadata:
                                storageItems.AddRange(GetStorageItemsFromPaths(filesMetadata.Paths)
                                    .ToBlockingEnumerable());
                                break;
                        }
                    }
                }
                catch { }
            }

            if (storageItems.Count > 0)
            {
                dataPackage.SetStorageItems(storageItems);
                hasDraggableData = true;
            }

            dataPackage.RequestedOperation = hasDraggableData ? DataPackageOperation.Copy : DataPackageOperation.None;
        }

        private readonly HashSet<ClipboardFormat> _draggableStorageItemFormats = [ClipboardFormat.Png, ClipboardFormat.Bitmap, ClipboardFormat.Files];

        /// <summary>
        /// Prepares the <see cref="DataPackage"/> for a drag-and-drop operation starting from selected clips.
        /// </summary>
        /// <param name="clips">Collection of <see cref="ClipModel"/> being dragged.</param>
        /// <param name="dataPackage">The <see cref="DataPackage"/> to populate.</param>
        /// <returns></returns>
        public async Task OnDragMultipleClipsStartingAsync(IEnumerable<ClipModel> clips, DataPackage dataPackage)
        {
            List<IStorageItem> storageItems = [];
            StringBuilder textBuilder = new();
            bool allClipsAreStorageItems = clips.All(clip => _draggableStorageItemFormats.Any(storageFormat => clip.Data.ContainsKey(storageFormat)));

            foreach (var clip in clips)
            {
                if (allClipsAreStorageItems)
                {
                    foreach (var dataItem in clip.Data.Where(item => _draggableStorageItemFormats.Contains(item.Key)))
                    {
                        try
                        {
                            if (dataItem.Key == ClipboardFormat.Files && dataItem.Value.Metadata is FilesMetadataModel filesMetadata)
                            {
                                storageItems.AddRange(GetStorageItemsFromPaths(filesMetadata.Paths)
                                    .ToBlockingEnumerable());
                            }
                            else
                            {
                                storageItems.Add(await StorageFile.GetFileFromPathAsync(dataItem.Value.Data)
                                    .AsTask().ConfigureAwait(false));
                            }
                        }
                        catch { }
                    }
                }
                else if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
                {
                    textBuilder.Append(textData.Data);
                    textBuilder.Append(Environment.NewLine);
                }
            }

            if (storageItems.Count > 0)
            {
                dataPackage.SetStorageItems(storageItems);
            }
            else if (textBuilder.Length > 0)
            {
                dataPackage.SetText(textBuilder.ToString());
            }

            dataPackage.RequestedOperation = storageItems.Count > 0 || textBuilder.Length > 0
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
        }

        private async IAsyncEnumerable<IStorageItem> GetStorageItemsFromPaths(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                IStorageItem? file = null;

                try
                {
                    if (File.Exists(path))
                    {
                        file = await StorageFile.GetFileFromPathAsync(path)
                            .AsTask().ConfigureAwait(false);
                    }
                    else if (Directory.Exists(path))
                    {
                        file = await StorageFolder.GetFolderFromPathAsync(path)
                            .AsTask().ConfigureAwait(false);
                    }
                }
                catch (UnauthorizedAccessException) { }

                if (file is not null)
                {
                    yield return file;
                }
            }
        }

        // Call it from view when filter selection is changed
        public void OnFilterTreeViewSelectionChanged()
        {
            // Pre-calculate the filtered OwnerPaths
            HashSet<string> selectedOwnerPaths = [.. RootAppNode.Children
                .Where(app => app.IsSelected)
                .Select(app => app.OwnerPath)];

            // Apply the filters
            var filteredClips = _clipboardService.Clips
                .Where(item => ClipFilterBySelectedMenu(item) && selectedOwnerPaths.Contains(item.Owner?.Path ?? string.Empty))
                .ToList();

            if (SearchMode)
            {
                _searchContext.Clear();
                _searchContext = [.. filteredClips];
                _searchService?.StartSearching(_searchContext, _searchString, ClipsCollection);
            }
            else
            {
                ClipsCollection?.Clear();
                ClipsCollection = [.. filteredClips];
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ToggleWindowPinned() => IsWindowPinned = !IsWindowPinned;

        [RelayCommand]
        private void ToggleClipboardMonitoringEnabled() => IsClipboardMonitoringEnabled = !IsClipboardMonitoringEnabled;

        [RelayCommand]
        private void OpenSettingsWindow() => SettingsWindow.ShowSettingsWindow();

        [RelayCommand]
        private void QuitApp() => App.Current.Exit();

        [RelayCommand]
        private void CloseWindow() => App.Current.ClipboardWindow.HideWindow();

        #region Single Clip context menu commands

        [RelayCommand]
        private void ToggleClipFavorite(ClipModel? clip)
        {
            if (clip is null) return;
            _clipboardService.ToggleClipFavorite(clip);
        }

        private bool CanOpenInBrowser(ClipModel? clip) => clip is not null && clip.IsLink;

        [RelayCommand(CanExecute = nameof(CanOpenInBrowser))]
        private async Task OpenInBrowser(ClipModel? clip)
        {
            if (clip is null || !clip.IsLink) return;
            
            if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData)
                && Uri.TryCreate(textData.Data, UriKind.Absolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
            }
        }

        [RelayCommand]
        private void PasteClip(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, paste: true);
        }

        private bool CanPasteClipAsPlainText(ClipModel? clip) => clip is not null && clip.Data.ContainsKey(ClipboardFormat.Text);

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipAsPlainText(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, paste: true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithUpperCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.UpperCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithLowerCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.LowerCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithCapitalizedCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.CapitalizedCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        public void PasteClipWithSentenceCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.SentenceCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithInvertCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.InvertCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithTrimWhitespace(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.TrimWhitespace, true);
        }

        private bool CanPasteClipWithDeveloperCase(ClipModel? clip) => SettingsContext.IsDeveloperStringCaseConversionsEnabled && CanPasteClipAsPlainText(clip);

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithCamelCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.CamelCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithPascalCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.PascalCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithSnakeCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.SnakeCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithKebabCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip, ClipboardFormat.Text, TextCaseType.KebabCase, true);
        }

        [RelayCommand]
        private void CopyClip(ClipModel? clip)
        {
            if (clip is null) return;
            SendClipToClipboard(clip);
        }

        [RelayCommand]
        private async Task SaveClipData(Tuple<ClipModel, ClipboardFormat>? clipDataFormat)
        {
            if (clipDataFormat is null || !clipDataFormat.Item1.Data.TryGetValue(clipDataFormat.Item2, out var dataModel))
            {
                return;
            }

            var picker = new FileSavePicker(App.Current.ClipboardWindow.AppWindow.OwnerWindowId);

            if (dataModel.IsFile())
            {
                picker.SuggestedFileName = Path.GetFileName(dataModel.Data);
            }
            else if (dataModel.Format == ClipboardFormat.Text)
            {
                picker.SuggestedFileName = string.Format($"{ClipboardFormatHelper.FILE_NAME_FORMAT}.txt", clipDataFormat.Item1.ClipTime);
            }

            var fileFilter = ClipboardFormatHelper.SaveAsFormatFilters.GetValueOrDefault(dataModel.Format);
            picker.FileTypeChoices.Add(fileFilter);

            var pickFileResult = await picker.PickSaveFileAsync();

            if (!string.IsNullOrEmpty(pickFileResult.Path))
            {
                await _clipboardService.SaveClipToFileAsync(dataModel, pickFileResult.Path);
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditClip))]
        private void EditClip(ClipModel? clip)
        {
            if (clip is null) return;
            EditorWindow.ShowEditorWindow(clip);
        }
        private bool CanEditClip(ClipModel? clip) => clip is not null
            && !EditorWindow.TryGetEditorContext(out _)
            && clip.Data.ContainsKey(ClipboardFormat.Text);

        [RelayCommand]
        private void DeleteClip(ClipModel? clip)
        {
            if (clip is null) return;
            _clipboardService.DeleteClip(clip);
        }

        private bool CanAddOwnerToFilters(OwnerModel? owner) => owner is not null
            && !string.IsNullOrEmpty(owner.Path)
            && !owner.Path.EndsWith("svchost.exe");   // check svchost.exe for UWP app sources

        [RelayCommand(CanExecute = nameof(CanAddOwnerToFilters))]
        private void AddOwnerToFilters(OwnerModel? owner)
        {
            if (owner is null || string.IsNullOrEmpty(owner.Path)) return;

            if (!SettingsContext.OwnerAppFilters.Any(filter => filter.Pattern.Equals(owner.Path)))
            {
                OwnerAppFilter filter = new()
                {
                    Pattern = owner.Path,
                    Name = owner.Name ?? string.Empty
                };

                SettingsContext.OwnerAppFilters.Add(filter);
                SettingsContext.SaveOwnerAppFilters();
            }
        }

        [RelayCommand]
        private void ToggleClipTag(Tuple<ClipModel, TagModel>? clipTagData)
        {
            if (clipTagData is null) return;

            var clip = clipTagData.Item1;
            var tag = clipTagData.Item2;

            if (clip.Tags.Contains(tag))
            {
                _tagService.RemoveClipFromTag(tag, clip);
            }
            else
            {
                _tagService.AddClipToTag(tag, clip);
            }
        }

        #endregion

        #region Multiple Clips context menu commands

        [RelayCommand]
        private void AddClipsToFavorites(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            var filteredClips = clips.Where(clip => !clip.IsFavorite).ToArray();
            foreach (var clip in filteredClips)
            {
                _clipboardService.ToggleClipFavorite(clip);
            }
        }

        [RelayCommand]
        private void RemoveClipsFromFavorites(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            var filteredClips = clips.Where(clip => clip.IsFavorite).ToArray();
            foreach (var clip in filteredClips)
            {
                _clipboardService.ToggleClipFavorite(clip);
            }
        }

        private bool CanPasteClipsAsPlainText(IEnumerable<ClipModel>? clips) => clips is not null && clips.Any(clip => clip.Data.ContainsKey(ClipboardFormat.Text));

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClips(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClipsWithUpperCase(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.UpperCase);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClipsWithLowerCase(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.LowerCase);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClipsWithCapitalizedCase(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.CapitalizedCase);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        public void PasteClipsWithSentenceCase(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.SentenceCase);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClipsWithInvertCase(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.InvertCase);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipsAsPlainText))]
        private void PasteClipsWithTrimWhitespace(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            PasteClipsAsCombinedText(clips, TextCaseType.TrimWhitespace);
        }

        private bool CanCopyClips(IEnumerable<ClipModel>? clips) => clips is not null && clips.Any(clip => clip.Data.ContainsKey(ClipboardFormat.Text));

        [RelayCommand(CanExecute = nameof(CanCopyClips))]
        private void CopyClips(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            var filteredClips = clips.Where(clip => clip.Data.ContainsKey(ClipboardFormat.Text)).ToArray();
            var dataModel = GenerateCombinedTextDataModel(filteredClips);
            SendDataToClipboard(new Dictionary<ClipboardFormat, DataModel> { { ClipboardFormat.Text, dataModel } });
        }

        [RelayCommand]
        private void DeleteClips(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            var clipsCopy = clips.ToArray();
            foreach (var clip in clipsCopy)
            {
                _clipboardService.DeleteClip(clip);
            }
        }

        private bool CanAddOwnersToFilters(IEnumerable<ClipModel>? clips) => clips is not null
            && clips.Any(clip => !string.IsNullOrEmpty(clip.Owner?.Path) && !clip.Owner.Path.EndsWith("svchost.exe"));   // check svchost.exe for UWP app sources

        [RelayCommand(CanExecute = nameof(CanAddOwnersToFilters))]
        private void AddOwnersToFilters(IEnumerable<ClipModel>? clips)
        {
            if (clips is null) return;
            var filteredClips = clips.Where(clip => CanAddOwnerToFilters(clip.Owner)).ToArray();
            foreach (var clip in filteredClips)
            {
                AddOwnerToFilters(clip.Owner);
            }
        }

        private void PasteClipsAsCombinedText(IEnumerable<ClipModel> clips, [Optional] TextCaseType? caseType)
        {
            var filteredClips = clips.Where(clip => clip.Data.ContainsKey(ClipboardFormat.Text)).ToArray();
            var dataModel = GenerateCombinedTextDataModel(filteredClips);
            SendDataToClipboard(new Dictionary<ClipboardFormat, DataModel> { { ClipboardFormat.Text, dataModel } }, caseType, true);
        }

        private DataModel GenerateCombinedTextDataModel(IEnumerable<ClipModel> clips)
        {
            StringBuilder textBuilder = new();
            foreach (var clip in clips)
            {
                if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
                {
                    textBuilder.Append(textData.Data);
                    textBuilder.Append(Environment.NewLine);
                }
            }
            return new DataModel(ClipboardFormat.Text, textBuilder.ToString(), []);
        }

        #endregion

        private void SendClipToClipboard(ClipModel clip, [Optional] ClipboardFormat? format, [Optional] TextCaseType? caseType, bool paste = false)
        {
            if (format.HasValue && clip.Data.TryGetValue(format.Value, out var formatData))
            {
                var dataDictionary = new Dictionary<ClipboardFormat, DataModel> { {format.Value, formatData} };
                SendDataToClipboard(dataDictionary, caseType, paste);
            }
            else {
                SendDataToClipboard(clip.Data, caseType, paste);
            }
            _clipboardService.MoveClipToTop(clip);
        }

        private void SendDataToClipboard(Dictionary<ClipboardFormat, DataModel> data, [Optional] TextCaseType? caseType, bool paste = false)
        {
            if (_clipboardService.SetClipboardData(data, caseType) && paste)
            {
                var windowToActivate = IntPtr.Zero;

                // Check case if we pinned window and right after we're trying to paste some clip
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
        }

        #endregion
    }
}
