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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;

namespace Rememory.Models
{
    public partial class SettingsContext : ObservableObject
    {
        private static SettingsContext? _instance;
        public static SettingsContext Instance => _instance ??= new SettingsContext();

        private static readonly ApplicationDataContainer _localSettings = ApplicationData.GetDefault().LocalSettings;

        #region General

        private readonly List<string> _supportedLanguages;
        private string? _languageCode;

        [Settings("LanguageCode", DefaultValue = "")]
        public string LanguageCode
        {
            get => _languageCode ??= GetSettingValue<string>();
            set
            {
                if (SetSettingsProperty(ref _languageCode, value))
                {
                    OnPropertyChanged(nameof(LanguageIndex));
                }
            }
        }
        public int LanguageIndex
        {
            get => _supportedLanguages.IndexOf(LanguageCode);
            set => LanguageCode = _supportedLanguages[value];
        }


        private List<int>? _activationShortcut;
        public List<int> ActivationShortcutDefault { get; private set; } = [0x10, 0x56, 0x5B];   // Win + Shift + V

        [Settings("ActivationShortcut")]
        public List<int> ActivationShortcut
        {
            get => _activationShortcut ??= GetSettingValue<List<int>>(ActivationShortcutDefault);
            set
            {
                if (SetSettingsProperty(ref _activationShortcut, value))
                {
                    unsafe
                    {
                        RememoryCoreHelper.UpdateTrayIconMenuItem(RememoryCoreHelper.TRAY_OPEN_COMMAND, new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged(
                            $"{"TrayIconMenu_Open".GetLocalizedResource()}\t{KeyboardHelper.ShortcutToString(value, "+")}")));
                    }
                }
            }
        }


        private bool? _isNotificationOnStartEnabled;

        [Settings("IsNotificationOnStartEnabled", DefaultValue = true)]
        public bool IsNotificationOnStartEnabled
        {
            get => _isNotificationOnStartEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isNotificationOnStartEnabled, value);
        }

        #endregion

        #region Personalisation

        private string? _theme;

        [Settings("Theme")]
        public ElementTheme Theme
        {
            get => EnumExtensions.FromDescription<ElementTheme>(_theme ??= GetSettingValue<string>(ElementTheme.Default.GetDescription()));
            set
            {
                if (SetSettingsProperty(ref _theme, value.GetDescription()))
                {
                    App.Current.ThemeService.ApplyTheme();
                }
            }
        }

        private string? _windowBackdrop;
        private readonly WindowBackdropType _windowBackdropDefault = WindowBackdropHelper.IsSystemBackdropSupported ? WindowBackdropType.Acrylic : WindowBackdropType.None;

        [Settings("WindowBackdrop")]
        public WindowBackdropType WindowBackdrop
        {
            get => EnumExtensions.FromDescription<WindowBackdropType>(_windowBackdrop ??= GetSettingValue<string>(_windowBackdropDefault.GetDescription()));
            set
            {
                if (SetSettingsProperty(ref _windowBackdrop, value.GetDescription()))
                {
                    App.Current.ThemeService.ApplyWindowBackdrop();
                }
            }
        }


        private string? _windowBackgroundColor;
        private readonly string _windowBackgroundColorDefault = WindowBackdropHelper.IsSystemBackdropSupported ? "#00000000" : "#3269797E";

        [Settings("WindowBackgroundColor")]
        public SolidColorBrush WindowBackgroundBrush
        {
            get => new((_windowBackgroundColor ??= GetSettingValue<string>(_windowBackgroundColorDefault)).ToColor());
            set => SetSettingsProperty(ref _windowBackgroundColor, value.Color.ToHex());
        }


        private string? _windowPosition;

        [Settings("WindowPosition")]
        public ClipboardWindowPosition WindowPosition
        {
            get => EnumExtensions.FromDescription<ClipboardWindowPosition>(_windowPosition ??= GetSettingValue<string>(ClipboardWindowPosition.Caret.GetDescription()));
            set => SetSettingsProperty(ref _windowPosition, value.GetDescription());
        }


        private int? _windowWidth;
        private static bool WindowWidthValidate(int value) => value >= WindowWidthLowerBound && value <= WindowWidthUpperBound;

        public static readonly int WindowWidthLowerBound = 320;
        public static readonly int WindowWidthUpperBound = 1200;

        [Settings("WindowWidth", DefaultValue = 380, Validator = nameof(WindowWidthValidate))]
        public int WindowWidth
        {
            get => _windowWidth ??= GetSettingValue<int>();
            set => SetSettingsProperty(ref _windowWidth, value);
        }


        private int? _windowHeight;
        private static bool WindowHeightValidate(int value) => value >= WindowHeightLowerBound && value <= WindowHeightUpperBound;

        public static readonly int WindowHeightLowerBound = 320;
        public static readonly int WindowHeightUpperBound = 1200;

        [Settings("WindowHeight", DefaultValue = 400, Validator = nameof(WindowHeightValidate))]
        public int WindowHeight
        {
            get => _windowHeight ??= GetSettingValue<int>();
            set => SetSettingsProperty(ref _windowHeight, value);
        }


        private int? _windowMargin;
        private static bool WindowMarginValidate(int value) => value >= WindowMarginLowerBound && value <= WindowMarginUpperBound;

        public static readonly int WindowMarginLowerBound = 0;
        public static readonly int WindowMarginUpperBound = 50;

        [Settings("WindowMargin", DefaultValue = 10, Validator = nameof(WindowMarginValidate))]
        public int WindowMargin
        {
            get => _windowMargin ??= GetSettingValue<int>();
            set => SetSettingsProperty(ref _windowMargin, value);
        }

        #endregion

        #region Clipboard

        private bool? _isClipsDragAndDropEnabled;

        [Settings("IsClipsDragAndDropEnabled", DefaultValue = true)]
        public bool IsClipsDragAndDropEnabled
        {
            // TODO
            // Update after resolving the drag and drop issue
            // See https://github.com/hpavlo/Rememory/issues/2
            get => _isClipsDragAndDropEnabled ??= GetSettingValue<bool>() && !AdministratorHelper.IsAppRunningAsAdministrator();
            set => SetSettingsProperty(ref _isClipsDragAndDropEnabled, value);
        }


        private bool? _isSearchFocusOnStartEnabled;

        [Settings("IsSearchFocusOnStartEnabled", DefaultValue = false)]
        public bool IsSearchFocusOnStartEnabled
        {
            get => _isSearchFocusOnStartEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isSearchFocusOnStartEnabled, value);
        }


        private bool? _isDeveloperStringCaseConversionsEnabled;

        [Settings("IsDeveloperStringCaseConversionsEnabled", DefaultValue = false)]
        public bool IsDeveloperStringCaseConversionsEnabled
        {
            get => _isDeveloperStringCaseConversionsEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isDeveloperStringCaseConversionsEnabled, value);
        }


        private bool? _isRememberWindowPinStateEnabled;

        [Settings("IsRememberWindowPinStateEnabled", DefaultValue = false)]
        public bool IsRememberWindowPinStateEnabled
        {
            get => _isRememberWindowPinStateEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isRememberWindowPinStateEnabled, value);
        }


        private bool? _isClearSearchOnOpenEnabled;

        [Settings("IsClearSearchOnOpenEnabled", DefaultValue = true)]
        public bool IsClearSearchOnOpenEnabled
        {
            get => _isClearSearchOnOpenEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isClearSearchOnOpenEnabled, value);
        }


        private bool? _isSetInitialTabOnOpenEnabled;

        [Settings("IsSetInitialTabOnOpenEnabled", DefaultValue = true)]
        public bool IsSetInitialTabOnOpenEnabled
        {
            get => _isSetInitialTabOnOpenEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isSetInitialTabOnOpenEnabled, value);
        }

        #endregion

        #region Metadata

        private bool? _isLinkPreviewLoadingEnabled;

        [Settings("IsLinkPreviewLoadingEnabled", DefaultValue = true)]
        public bool IsLinkPreviewLoadingEnabled
        {
            get => _isLinkPreviewLoadingEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isLinkPreviewLoadingEnabled, value);
        }


        private bool? _isHexColorPrefixRequired;

        [Settings("IsHexColorPrefixRequired", DefaultValue = true)]
        public bool IsHexColorPrefixRequired
        {
            get => _isHexColorPrefixRequired ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isHexColorPrefixRequired, value);
        }

        #endregion

        #region Tags

        //private bool? _isTagCountVisible;

        //[Settings("IsTagCountVisible", DefaultValue = false)]
        //public bool IsTagCountVisible
        //{
        //    get => _isTagCountVisible ??= GetSettingValue<bool>();
        //    set => SetSettingsProperty(ref _isTagCountVisible, value);
        //}

        #endregion

        #region Storage

        private string? _cleanupType;

        [Settings("CleanupType")]
        public CleanupType CleanupType
        {
            get => EnumExtensions.FromDescription<CleanupType>(_cleanupType ??= GetSettingValue<string>(CleanupType.RetentionPeriod.GetDescription()));
            set => SetSettingsProperty(ref _cleanupType, value.GetDescription());
        }


        private string? _cleanupTimeSpan;

        [Settings("CleanupTimeSpan")]
        public CleanupTimeSpan CleanupTimeSpan
        {
            get => EnumExtensions.FromDescription<CleanupTimeSpan>(_cleanupTimeSpan ??= GetSettingValue<string>(CleanupTimeSpan.Month.GetDescription()));
            set => SetSettingsProperty(ref _cleanupTimeSpan, value.GetDescription());
        }


        private int? _cleanupQuantity;
        private static bool CleanupQuantityValidate(int value) => value >= CleanupQuantityLowerBound && value <= CleanupQuantityUpperBound;

        public static readonly int CleanupQuantityLowerBound = 10;
        public static readonly int CleanupQuantityUpperBound = 10_000;

        [Settings("CleanupQuantity", DefaultValue = 50, Validator = nameof(CleanupQuantityValidate))]
        public int CleanupQuantity
        {
            get => _cleanupQuantity ??= GetSettingValue<int>();
            set => SetSettingsProperty(ref _cleanupQuantity, value);
        }


        private bool? _isFavoriteClipsCleaningEnabled;

        [Settings("IsFavoriteClipsCleaningEnabled", DefaultValue = false)]
        public bool IsFavoriteClipsCleaningEnabled
        {
            get => _isFavoriteClipsCleaningEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isFavoriteClipsCleaningEnabled, value);
        }


        private bool? _isClipSizeValidationEnabled;

        [Settings("IsClipSizeValidationEnabled", DefaultValue = true)]
        public bool IsClipSizeValidationEnabled
        {
            get => _isClipSizeValidationEnabled ??= GetSettingValue<bool>();
            set => SetSettingsProperty(ref _isClipSizeValidationEnabled, value);
        }


        private int? _maxClipSize;
        private static bool MaxClipSizeValidate(int value) => value >= ClipSizeLowerBound && value <= ClipSizeUpperBound;

        public static readonly int ClipSizeLowerBound = 1;
        public static readonly int ClipSizeUpperBound = 64;

        [Settings("MaxClipSize", DefaultValue = 8, Validator = nameof(MaxClipSizeValidate))]
        public int MaxClipSize
        {
            get => _maxClipSize ??= GetSettingValue<int>();
            set => SetSettingsProperty(ref _maxClipSize, value);
        }

        #endregion

        #region Filters

        private ObservableCollection<OwnerAppFilter>? _ownerAppFilters;
        /// <summary>
        /// Use <see cref="SaveOwnerAppFilters"/> to save changes
        /// </summary>
        [Settings("OwnerAppFilters")]
        public ObservableCollection<OwnerAppFilter> OwnerAppFilters => _ownerAppFilters ??= GetSettingValue<ObservableCollection<OwnerAppFilter>>(new ObservableCollection<OwnerAppFilter>());
        public void SaveOwnerAppFilters() => _localSettings.Values["OwnerAppFilters"] = JsonSerializer.Serialize(OwnerAppFilters);

        #endregion

        private bool? _isClipboardMonitoringEnabled;

        [Settings("IsClipboardMonitoringEnabled", DefaultValue = true)]
        public bool IsClipboardMonitoringEnabled
        {
            get => _isClipboardMonitoringEnabled ??= GetSettingValue<bool>();
            set
            {
                if (SetSettingsProperty(ref _isClipboardMonitoringEnabled, value))
                {
                    unsafe
                    {
                        RememoryCoreHelper.UpdateTrayIconMenuItem(RememoryCoreHelper.TRAY_TOGGLE_MONITORING_COMMAND, new IntPtr(Utf16StringMarshaller.ConvertToUnmanaged(
                            value ? "Pause monitoring" : "Resume monitoring")));
                    }
                }
            }
        }

        private SettingsContext()
        {
            _supportedLanguages = ApplicationLanguages.ManifestLanguages.ToList();
            _supportedLanguages.Insert(0, string.Empty);   // Default language
        }

        private T GetSettingValue<T>(object? overrideDefault = null, [CallerMemberName] string? propertyName = null)
        {
            // Validate propertyName
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // Fetch PropertyInfo
            var propInfo = GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {GetType().Name}.");

            // Fetch SettingsAttribute
            var attr = propInfo.GetCustomAttribute<Settings>()
                ?? throw new InvalidOperationException($"Property '{propertyName}' is missing [Settings].");

            if (string.IsNullOrEmpty(attr.Key))
            {
                throw new InvalidOperationException($"[Settings] on '{propertyName}' must have a non-empty Key.");
            }

            // Read raw stored value
            object? rawValue = null;
            if (_localSettings.Values.TryGetValue(attr.Key, out var saved))
            {
                rawValue = (saved is string s && typeof(T) != typeof(string)) ? JsonSerializer.Deserialize<T>(s) : saved;
            }

            // Determine candidate result
            T result = PickValue(rawValue, overrideDefault, attr.DefaultValue);

            // Validate via method, if specified
            if (!string.IsNullOrEmpty(attr.Validator))
            {
                var method = GetType().GetMethod(attr.Validator, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException($"Validator method '{attr.Validator}' not found on {GetType().Name}.");

                var isValid = method.Invoke(this, [result]) as bool?;
                if (isValid != true)
                {
                    // Fallback to attribute default if validation fails
                    result = PickValue(null, overrideDefault, attr.DefaultValue);
                }
            }

            return result;

            T PickValue(object? rawValue, object? overrideDefault, object? defaultValue)
            {
                // If the saved value is of the right type, use it
                if (rawValue is T t)
                {
                    return t;
                }
                // Otherwise, if there’s an override default passed in, use that
                else if (overrideDefault is T o)
                {
                    return o;
                }
                // Otherwise, if the attribute provided a default, use it
                else if (attr.DefaultValue is T d)
                {
                    return d;
                }
                // If all else fails, fall back to T’s default
                else
                {
                    return default!;
                }
            }
        }

        private bool SetSettingsProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            // Validate propertyName
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // Fetch PropertyInfo
            var propInfo = GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {GetType().Name}.");

            // Fetch SettingsAttribute
            var attr = propInfo.GetCustomAttribute<Settings>()
                ?? throw new InvalidOperationException($"Property '{propertyName}' is missing [Settings].");

            if (string.IsNullOrEmpty(attr.Key))
            {
                throw new InvalidOperationException($"[Settings] on '{propertyName}' must have a non-empty Key.");
            }

            // Validate incoming value
            if (!string.IsNullOrEmpty(attr.Validator))
            {
                var method = GetType().GetMethod(attr.Validator, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException($"Validator method '{attr.Validator}' not found on {GetType().Name}.");

                var isValid = method.Invoke(this, [newValue]) as bool?;
                if (isValid != true)
                {
                    return false;
                }
            }

            // Update backing field
            if (!SetProperty(ref field, newValue, propertyName))
            {
                return false;
            }

            // Persist to settings store
            _localSettings.Values[attr.Key] = (newValue is string || typeof(T).IsValueType) ? newValue : JsonSerializer.Serialize(newValue);

            return true;
        }


        [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class Settings : Attribute
        {
            // Required named argument
            public string Key { get; set; } = string.Empty;

            // Optional named arguments
            public object? DefaultValue { get; set; }
            public string? Validator { get; set; }

            public Settings() { }

            public Settings(string key)
            {
                Key = key;
            }
        }
    }
}
