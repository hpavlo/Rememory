using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Globalization;
using Microsoft.Windows.Storage;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Services;
using Rememory.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;

namespace Rememory.Models
{
    public partial class SettingsContext : ObservableObject
    {
        private static SettingsContext _instance;
        public static SettingsContext Instance => _instance ??= new SettingsContext();

        private static ApplicationDataContainer _localSettings = ApplicationData.GetDefault().LocalSettings;

        private IList<string> _supportedLanguages;
        private string _currentLanguageCode;
        public string LanguageCodeDefault { get; private set; } = string.Empty;
        public string CurrentLanguageCode => _currentLanguageCode;
        public int CurrentLanguageIndex
        {
            get => _supportedLanguages.IndexOf(_currentLanguageCode);
            set => SetSettingsProperty(ref _currentLanguageCode, _supportedLanguages[value], nameof(CurrentLanguageCode));
        }

        private int _currentThemeIndex;
        public int ThemeIndexDefault { get; private set; } = (int)ElementTheme.Default;
        public int CurrentThemeIndex
        {
            get => _currentThemeIndex;
            set
            {
                if (SetSettingsProperty(ref _currentThemeIndex, value))
                {
                    App.Current.ThemeService.ApplyTheme();
                }
            }
        }

        private int _currentWindowBackdropIndex;
        public int WindowBackdropIndexDefault { get; private set; } = WindowBackdropHelper.IsSystemBackdropSupported ? (int)WindowBackdropType.Acrylic : (int)WindowBackdropType.None;
        public int CurrentWindowBackdropIndex
        {
            get => _currentWindowBackdropIndex;
            set
            {
                if (SetSettingsProperty(ref _currentWindowBackdropIndex, value))
                {
                    App.Current.ThemeService.ApplyWindowBackdrop();
                }
            }
        }

        private string _currentWindowBackgroundColor;
        public string WindowBackgroundColorDefault { get; private set; } = WindowBackdropHelper.IsSystemBackdropSupported ? "#00000000" : "#3269797E";
        public SolidColorBrush CurrentWindowBackgroundBrush
        {
            get => new SolidColorBrush(_currentWindowBackgroundColor.ToColor());
            set => SetSettingsProperty(ref _currentWindowBackgroundColor, value.Color.ToHex());
        }

        private int _clipboardWindowPositionIndex;
        public int ClipboardWindowPositionIndexDefault { get; private set; } = (int)ClipboardWindowPosition.Caret;
        public int ClipboardWindowPositionIndex
        {
            get => _clipboardWindowPositionIndex;
            set => SetSettingsProperty(ref _clipboardWindowPositionIndex, value);
        }

        private int _windowWidth;
        public int WindowWidthDefault { get; private set; } = 320;
        public int WindowWidth
        {
            get => _windowWidth;
            set
            {
                if (value >= 320 && value <= 1200)
                {
                    SetSettingsProperty(ref _windowWidth, value);
                }
            }
        }

        private int _windowHeight;
        public int WindowHeightDefault { get; private set; } = 400;
        public int WindowHeight
        {
            get => _windowHeight;
            set
            {
                if (value >= 320 && value <= 1200)
                {
                    SetSettingsProperty(ref _windowHeight, value);
                }
            }
        }

        private int _windowMargin;
        public int WindowMarginDefault { get; private set; } = 10;
        public int WindowMargin
        {
            get => _windowMargin;
            set
            {
                if (value >= 0 && value <= 50)
                {
                    SetSettingsProperty(ref _windowMargin, value);
                }
            }
        }

        private int _cleanupTypeIndex;
        public int CleanupTypeIndexDefault { get; private set; } = (int)CleanupType.RetentionPeriod;
        public int CleanupTypeIndex
        {
            get => _cleanupTypeIndex;
            set => SetSettingsProperty(ref _cleanupTypeIndex, value);
        }

        private int _cleanupTimeSpanIndex;
        public int CleanupTimeSpanIndexDefault { get; private set; } = (int)CleanupTimeSpan.Month;
        public int CleanupTimeSpanIndex
        {
            get => _cleanupTimeSpanIndex;
            set => SetSettingsProperty(ref _cleanupTimeSpanIndex, value);
        }

        private bool _cleanFavoriteItems;
        public bool CleanFavoriteItemsDefault { get; private set; } = false;
        public bool CleanFavoriteItems
        {
            get => _cleanFavoriteItems;
            set => SetSettingsProperty(ref _cleanFavoriteItems, value);
        }

        private int _cleanupQuantity;
        public int CleanupQuantityDefault { get; private set; } = 50;
        public int CleanupQuantity
        {
            get => _cleanupQuantity;
            set
            {
                if (value >= 10 && value <= 10_000)
                {
                    SetSettingsProperty(ref _cleanupQuantity, value);
                }
            }
        }

        private List<int> _activationShortcut;
        public List<int> ActivationShortcutDefault { get; private set; } = [0x10, 0x56, 0x5B];   // Win + Shift + V
        public List<int> ActivationShortcut
        {
            get => _activationShortcut;
            set
            {
                if (SetProperty(ref _activationShortcut, value))
                {
                    _localSettings.Values[nameof(ActivationShortcut)] = JsonSerializer.Serialize(value);
                    unsafe {
                        RememoryCoreHelper.UpdateTrayIconMenuItem(RememoryCoreHelper.TRAY_OPEN_COMMAND, new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged(
                            $"{"TrayIconMenu_Open".GetLocalizedResource()}\t{KeyboardHelper.ShortcutToString(value, "+")}")));
                    }
                }
            }
        }

        private bool _enableLinkPreviewLoading;
        public bool EnableLinkPreviewLoadingDefault { get; private set; } = true;
        public bool EnableLinkPreviewLoading
        {
            get => _enableLinkPreviewLoading;
            set => SetSettingsProperty(ref _enableLinkPreviewLoading, value);
        }

        private bool _enableItemDragAndDrop;
        public bool EnableItemDragAndDropDefault { get; private set; } = false;
        public bool EnableItemDragAndDrop
        {
            // TODO
            // Update after resolving the drag and drop issue
            // See https://github.com/hpavlo/Rememory/issues/2
            get => _enableItemDragAndDrop && !AdministratorHelper.IsAppRunningAsAdministrator();
            set => SetSettingsProperty(ref _enableItemDragAndDrop, value);
        }

        private bool _enableSearchFocusOnStart;
        public bool EnableSearchFocusOnStartDefault { get; private set; } = false;
        public bool EnableSearchFocusOnStart
        {
            get => _enableSearchFocusOnStart;
            set => SetSettingsProperty(ref _enableSearchFocusOnStart, value);
        }

        private bool _showNotificationOnStart;
        public bool ShowNotificationOnStartDefault { get; private set; } = true;
        public bool ShowNotificationOnStart
        {
            get => _showNotificationOnStart;
            set => SetSettingsProperty(ref _showNotificationOnStart, value);
        }

        private bool _requireHexColorPrefix;
        public bool RequireHexColorPrefixDefault { get; private set; } = true;
        public bool RequireHexColorPrefix
        {
            get => _requireHexColorPrefix;
            set => SetSettingsProperty(ref _requireHexColorPrefix, value);
        }

        private bool _enableDeveloperStringCaseConversions;
        public bool EnableDeveloperStringCaseConversionsDefault { get; private set; } = false;
        public bool EnableDeveloperStringCaseConversions
        {
            get => _enableDeveloperStringCaseConversions;
            set => SetSettingsProperty(ref _enableDeveloperStringCaseConversions, value);
        }

        /// <summary>
        /// Use <see cref="OwnerAppFiltersSave"/> to save changes
        /// </summary>
        public ObservableCollection<OwnerAppFilter> OwnerAppFilters { get; private set; }
        public void OwnerAppFiltersSave() => _localSettings.Values[nameof(OwnerAppFilters)] = JsonSerializer.Serialize(OwnerAppFilters);

        private SettingsContext()
        {
            _supportedLanguages = ApplicationLanguages.ManifestLanguages.ToList();
            _supportedLanguages.Insert(0, string.Empty);   // Default language

            _currentLanguageCode = GetSettingValue(nameof(CurrentLanguageCode), LanguageCodeDefault);
            _currentThemeIndex = GetSettingValue(nameof(CurrentThemeIndex), ThemeIndexDefault);
            _currentWindowBackdropIndex = GetSettingValue(nameof(CurrentWindowBackdropIndex), WindowBackdropIndexDefault);
            _currentWindowBackgroundColor = GetSettingValue(nameof(CurrentWindowBackgroundBrush), WindowBackgroundColorDefault);
            _clipboardWindowPositionIndex = GetSettingValue(nameof(ClipboardWindowPositionIndex), ClipboardWindowPositionIndexDefault);
            _windowWidth = GetSettingValue(nameof(WindowWidth), WindowWidthDefault);
            _windowHeight = GetSettingValue(nameof(WindowHeight), WindowHeightDefault);
            _windowMargin = GetSettingValue(nameof(WindowMargin), WindowMarginDefault);
            _cleanupTypeIndex = GetSettingValue(nameof(CleanupTypeIndex), CleanupTypeIndexDefault);
            _cleanupTimeSpanIndex = GetSettingValue(nameof(CleanupTimeSpanIndex), CleanupTimeSpanIndexDefault);
            _cleanFavoriteItems = GetSettingValue(nameof(CleanFavoriteItems), CleanFavoriteItemsDefault);
            _cleanupQuantity = GetSettingValue(nameof(CleanupQuantity), CleanupQuantityDefault);
            _activationShortcut = _localSettings.Values.TryGetValue(nameof(ActivationShortcut), out var activationShortcutValue) ?
                JsonSerializer.Deserialize<List<int>>((string)activationShortcutValue) : ActivationShortcutDefault;
            _enableLinkPreviewLoading = GetSettingValue(nameof(EnableLinkPreviewLoading), EnableLinkPreviewLoadingDefault);
            _enableItemDragAndDrop = GetSettingValue(nameof(EnableItemDragAndDrop), EnableItemDragAndDropDefault);
            _enableSearchFocusOnStart = GetSettingValue(nameof(EnableSearchFocusOnStart), EnableSearchFocusOnStartDefault);
            OwnerAppFilters = _localSettings.Values.TryGetValue(nameof(OwnerAppFilters), out var filterSourceValue) ?
                JsonSerializer.Deserialize<ObservableCollection<OwnerAppFilter>>((string)filterSourceValue) : [];
            _showNotificationOnStart = GetSettingValue(nameof(ShowNotificationOnStart), ShowNotificationOnStartDefault);
            _requireHexColorPrefix = GetSettingValue(nameof(RequireHexColorPrefix), RequireHexColorPrefixDefault);
            _enableDeveloperStringCaseConversions = GetSettingValue(nameof(EnableDeveloperStringCaseConversions), EnableDeveloperStringCaseConversionsDefault);
        }

        private T GetSettingValue<T>(string key, T defaultValue)
        {
            if (_localSettings.Values.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        private bool SetSettingsProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref field, newValue, propertyName))
            {
                _localSettings.Values[propertyName] = newValue;
                return true;
            }
            return false;
        }
    }
}
