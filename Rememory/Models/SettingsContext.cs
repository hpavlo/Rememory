using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Globalization;
using Microsoft.Windows.Storage;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;

namespace Rememory.Models
{
    public class SettingsContext : ObservableObject
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

        private int _windowWidth;
        public int WindowWidthDefault { get; private set; } = 500;
        public int WindowWidth
        {
            get => _windowWidth;
            set => SetSettingsProperty(ref _windowWidth, value);
        }

        private int _windowMargin;
        public int WindowMarginDefault { get; private set; } = 10;
        public int WindowMargin
        {
            get => _windowMargin;
            set => SetSettingsProperty(ref _windowMargin, value);
        }

        private int _cleanupTimeSpanIndex;
        public int CleanupTimeSpanIndexDefault { get; private set; } = (int)CleanupTimeSpan.Month;
        public int CleanupTimeSpanIndex
        {
            get => _cleanupTimeSpanIndex;
            set => SetSettingsProperty(ref _cleanupTimeSpanIndex, value);
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
            get => _enableItemDragAndDrop;
            set => SetSettingsProperty(ref _enableItemDragAndDrop, value);
        }

        private SettingsContext()
        {
            _supportedLanguages = ApplicationLanguages.ManifestLanguages.ToList();
            _supportedLanguages.Insert(0, string.Empty);   // Default language

            _currentLanguageCode = GetSettingValue(nameof(CurrentLanguageCode), LanguageCodeDefault);
            _currentThemeIndex = GetSettingValue(nameof(CurrentThemeIndex), ThemeIndexDefault);
            _currentWindowBackdropIndex = GetSettingValue(nameof(CurrentWindowBackdropIndex), WindowBackdropIndexDefault);
            _currentWindowBackgroundColor = GetSettingValue(nameof(CurrentWindowBackgroundBrush), WindowBackgroundColorDefault);
            _windowWidth = GetSettingValue(nameof(WindowWidth), WindowWidthDefault);
            _windowMargin = GetSettingValue(nameof(WindowMargin), WindowMarginDefault);
            _cleanupTimeSpanIndex = GetSettingValue(nameof(CleanupTimeSpanIndex), CleanupTimeSpanIndexDefault);
            _activationShortcut = _localSettings.Values.TryGetValue(nameof(ActivationShortcut), out var value) ?
                JsonSerializer.Deserialize<List<int>>((string)value) : ActivationShortcutDefault;
            _enableLinkPreviewLoading = GetSettingValue(nameof(EnableLinkPreviewLoading), EnableLinkPreviewLoadingDefault);
            _enableItemDragAndDrop = GetSettingValue(nameof(EnableItemDragAndDrop), EnableItemDragAndDropDefault);
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
