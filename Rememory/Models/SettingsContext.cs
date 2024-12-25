using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage;
using Rememory.Helper;
using Rememory.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        private int _currentThemeIndex;
        public int CurrentThemeIndexDefault = (int)ElementTheme.Default;
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

        private int _windowWidth;
        public int WindowWidthDefault = 500;
        public int WindowWidth
        {
            get => _windowWidth;
            set => SetSettingsProperty(ref _windowWidth, value);
        }

        private int _windowMargin;
        public int WindowMarginDefault = 10;
        public int WindowMargin
        {
            get => _windowMargin;
            set => SetSettingsProperty(ref _windowMargin, value);
        }

        private int _cleanupTimeSpanIndex;
        public int CleanupTimeSpanIndexDefault = (int)CleanupTimeSpan.Month;
        public int CleanupTimeSpanIndex
        {
            get => _cleanupTimeSpanIndex;
            set => SetSettingsProperty(ref _cleanupTimeSpanIndex, value);
        }

        private List<int> _activationShortcut;
        public List<int> ActivationShortcutDefault = [0x10, 0x56, 0x5B];   // Win + Shift + V
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

        private SettingsContext()
        {
            _currentThemeIndex = GetSettingValue(nameof(CurrentThemeIndex), CurrentThemeIndexDefault);
            _windowWidth = GetSettingValue(nameof(WindowWidth), WindowWidthDefault);
            _windowMargin = GetSettingValue(nameof(WindowMargin), WindowMarginDefault);
            _cleanupTimeSpanIndex = GetSettingValue(nameof(CleanupTimeSpanIndex), CleanupTimeSpanIndexDefault);
            _activationShortcut = _localSettings.Values.TryGetValue(nameof(ActivationShortcut), out var value) ?
                JsonSerializer.Deserialize<List<int>>((string)value) : ActivationShortcutDefault;
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
