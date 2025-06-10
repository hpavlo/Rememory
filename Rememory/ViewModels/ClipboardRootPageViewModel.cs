using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Hooks;
using Rememory.Models;
using Rememory.Views;
using Rememory.Views.Editor;
using Rememory.Views.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
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
        private IThemeService ThemeService => App.Current.ThemeService;

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
        /// Used to set themed background color if we have disabled backdrop.
        /// </summary>
        public SolidColorBrush ThemeBackgroundColor { get; private set; }

        // Backing field for SelectedMenuItem
        private NavigationMenuItem _selectedMenuItem;
        /// <summary>
        /// Gets or sets the currently selected navigation menu item, which determines the primary filter applied to the clips.
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
                    UpdateClipsList();
                }
            }
        }

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

        // Backing field for IsClipboardMonitoringEnabled
        private bool _isClipboardMonitoringEnabled = true;
        /// <summary>
        /// When it's false, clipboard manager doesn't save any data
        /// </summary>
        public bool IsClipboardMonitoringEnabled
        {
            get => _isClipboardMonitoringEnabled;
            set
            {
                if (SetProperty(ref _isClipboardMonitoringEnabled, value))
                {
                    if (value)
                    {
                        _clipboardService.StartClipboardMonitor(App.Current.ClipboardWindowHandle);
                    }
                    else
                    {
                        _clipboardService.StopClipboardMonitor(App.Current.ClipboardWindowHandle);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the search input should be enabled.
        /// Typically disabled for filter types where search is not applicable (e.g., Images).
        /// </summary>
        public bool IsSearchEnabled => SelectedMenuItem != NavigationMenuItem.Images;

        private bool _searchMode = false;
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
            App.Current.ClipboardWindow.Showing += ClipboardWindow_Showing;

            ThemeBackgroundColor = new SolidColorBrush(Colors.Transparent);
            ChangeThemeBackdropColor();
            ThemeService.ThemeChanged += (s, a) => ChangeThemeBackdropColor();
            ThemeService.WindowBackdropChanged += (s, a) => ChangeThemeBackdropColor();

            _clipboardService.NewClipAdded += ClipboardService_NewClipAdded;
            _clipboardService.FavoriteClipChanged += ClipboardService_FavoriteClipChanged;
            _clipboardService.ClipMovedToTop += ClipboardService_ClipMovedToTop;
            _clipboardService.ClipDeleted += ClipboardService_ClipDeleted;
            _clipboardService.AllClipsDeleted += ClipboardService_AllClipsDeleted;
            _clipboardService.StartClipboardMonitor(App.Current.ClipboardWindowHandle);

            RootAppNode = new AppTreeViewNode { Title = "FilterTreeViewTitle_Apps".GetLocalizedResource(), IsExpanded = true };
            AppTreeViewNodes.Add(RootAppNode);

            _ownerService.OwnerRegistered += OwnerService_OwnerRegistered;
            _ownerService.OwnerUnregistered += OwnerService_OwnerUnregistered;
            _ownerService.AllOwnersUnregistered += OwnerService_AllOwnersUnregistered;

            UpdateClipsList();
            CleanupOldData();
        }

        public IList<TagModel> GetTags() => _tagService.Tags;

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
        /// Filters a single <see cref="ClipModel"/> based on the currently selected navigation menu item.
        /// </summary>
        /// <param name="item">The clip to check.</param>
        /// <returns><c>true</c> if the clip matches the current filter; otherwise, <c>false</c>.</returns>
        private bool ClipFilterBySelectedMenu(ClipModel? item)
        {
            if (item == null) return false;

            return SelectedMenuItem switch
            {
                NavigationMenuItem.Home => true,   // Show all clips
                NavigationMenuItem.Fovorites => item.IsFavorite,   // Show only favorites
                NavigationMenuItem.Images => item.Data.ContainsKey(ClipboardFormat.Png) || item.Data.ContainsKey(ClipboardFormat.Bitmap),   // Show only clips containing image data
                NavigationMenuItem.Links => item.IsLink,   // Show only links
                _ => false   // Default case, should not happen if enum is handled completely
            };
        }

        /// <summary>
        /// Check if the retention period of clips has expired
        /// </summary>
        private void CleanupOldData() => _cleanupDataService.CleanupByRetentionPeriod();

        private void ChangeThemeBackdropColor()
        {
            if (ThemeService.WindowBackdrop == WindowBackdropType.None)
            {
                ThemeBackgroundColor.Color = ThemeService.Theme switch
                {
                    Microsoft.UI.Xaml.ElementTheme.Light => Colors.White,
                    Microsoft.UI.Xaml.ElementTheme.Dark => Colors.Black,
                    _ => NativeHelper.ShouldSystemUseDarkMode() ? Colors.Black : Colors.White,
                };
            }
            else
            {
                ThemeBackgroundColor.Color = Colors.Transparent;
            }
            OnPropertyChanged(nameof(ThemeBackgroundColor));
        }

        private void ClipboardWindow_Showing(ClipboardWindow sender, EventArgs args)
        {
            _lastActiveWindowHandleBeforeShowing = NativeHelper.GetForegroundWindow();
        }

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
            if (SelectedMenuItem == NavigationMenuItem.Fovorites && !a.ChangedClip.IsFavorite)
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
            CleanupOldData();

            // Update relative timestamps displayed in the UI.
            foreach (var item in ClipsCollection)
            {
                item.UpdateProperty(nameof(item.ClipTime));
            }
        }

        /// <summary>
        /// Performs actions when the associated window is hiding.
        /// </summary>
        public void OnWindowHiding()
        {
            // Unpin the window automatically when it hides.
            IsWindowPinned = false;
        }

        /// <summary>
        /// Prepares the <see cref="DataPackage"/> for a drag-and-drop operation starting from a clip.
        /// </summary>
        /// <param name="clip">The <see cref="ClipModel"/> being dragged.</param>
        /// <param name="dataPackage">The <see cref="DataPackage"/> to populate.</param>
        public async Task OnDragClipStartingAsync(ClipModel? clip, DataPackage dataPackage)
        {
            if (clip?.Data is null) return;

            IStorageItem? storageItem = null;

            foreach (var dataItem in clip.Data)
            {
                try
                {
                    if (dataItem.Value.IsFile() && !clip.IsLink)
                    {
                        var storageFile = await StorageFile.GetFileFromPathAsync(dataItem.Value.Data);
                        // Set only one storage file with the most priority format
                        storageItem ??= storageFile;
                        using var storageStream = await storageFile.OpenReadAsync();

                        switch (dataItem.Key)
                        {
                            case ClipboardFormat.Rtf:
                                dataPackage.SetData("Rich Text Format", storageStream);
                                break;
                            case ClipboardFormat.Html:
                                dataPackage.SetData("HTML Format", storageStream);
                                break;
                            case ClipboardFormat.Png:
                                dataPackage.SetData("PNG", storageStream);
                                break;
                            case ClipboardFormat.Bitmap:
                                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(storageStream));
                                break;
                        }
                    }
                    else if (dataItem.Key == ClipboardFormat.Text)
                    {
                        dataPackage.SetText(dataItem.Value.Data);
                        if (clip.IsLink)
                        {
                            dataPackage.SetWebLink(new Uri(dataItem.Value.Data));
                        }
                    }
                }
                catch { }
            }

            if (storageItem is not null)
            {
                dataPackage.SetStorageItems([storageItem]);
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

        [RelayCommand]
        private void ToggleClipFavorite(ClipModel? clip)
        {
            if (clip is null) return;
            _clipboardService.ToggleClipFavorite(clip);
        }

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
        private bool CanOpenInBrowser(ClipModel? clip) => clip is not null && clip.IsLink;

        [RelayCommand]
        private void PasteClip(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, paste: true);
        }

        private bool CanPasteClipAsPlainText(ClipModel? clip) => clip is not null && clip.Data.ContainsKey(ClipboardFormat.Text);

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipAsPlainText(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, paste: true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithUpperCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.UpperCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithLowerCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.LowerCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithCapitalizeCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.CapitalizeCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        public void PasteClipWithSentenceCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.SentenceCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithInvertCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.InvertCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipAsPlainText))]
        private void PasteClipWithTrimWhitespace(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.TrimWhitespace, true);
        }

        private bool CanPasteClipWithDeveloperCase(ClipModel? clip) => SettingsContext.EnableDeveloperStringCaseConversions && CanPasteClipAsPlainText(clip);

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithCamelCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.CamelCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithPascalCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.PascalCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithSnakeCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.SnakeCase, true);
        }

        [RelayCommand(CanExecute = nameof(CanPasteClipWithDeveloperCase))]
        private void PasteClipWithKebabCase(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip, ClipboardFormat.Text, TextCaseType.KebabCase, true);
        }

        [RelayCommand]
        private void CopyClip(ClipModel? clip)
        {
            if (clip is null) return;
            SendDataToClipboard(clip);
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
                SettingsContext.OwnerAppFiltersSave();
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

        private void SendDataToClipboard(ClipModel clip, [Optional] ClipboardFormat? format, [Optional] TextCaseType? caseType, bool paste = false)
        {
            if (_clipboardService.SetClipboardData(clip, format, caseType) && paste)
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
            _clipboardService.MoveClipToTop(clip);
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
