using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.AppLifecycle;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Services;
using Rememory.Views.Settings;
using System.Threading.Tasks;

namespace Rememory.ViewModels.Settings
{
    public partial class GeneralPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext { get; } = App.Current.SettingsContext;

        private StartupManager? _startupManager;
        private bool _isInitialized = false;

        public bool IsAdministratorSettingsEnabled => RunAtStartupToggle && AdministratorHelper.IsAppRunningAsAdministrator();

        public bool IsStartupOptionAvailable => !(_startupManager?.IsDisabledByUser ?? false);

        private bool _runAtStartupToggle = false;
        public bool RunAtStartupToggle
        {
            get => _runAtStartupToggle;
            set
            {
                if (SetProperty(ref _runAtStartupToggle, value))
                {
                    if (value)
                    {
                        _ = _startupManager?.StartupTask.RequestEnableAsync();
                    }
                    else
                    {
                        _startupManager?.StartupTask.Disable();
                    }
                    OnPropertyChanged(nameof(IsAdministratorSettingsEnabled));
                }
            }
        }

        private bool? _runAsAdministratorToggle;
        public bool RunAsAdministratorToggle
        {
            get => _runAsAdministratorToggle ??= StartupManager.IsElevatedTaskEnabled(out _);
            set
            {
                if (SetProperty(ref _runAsAdministratorToggle, value))
                {
                    try
                    {
                        if (value)
                        {
                            StartupManager.EnableElevatedStartup();
                        }
                        else
                        {
                            StartupManager.DisableElevatedStartup();
                        }
                    }
                    catch
                    {
                        ShowAccessExceptionMessageBox();
                        SetProperty(ref _runAsAdministratorToggle, !value);
                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _startupManager = await StartupManager.CreateAsync();

            // Trigger properties, that depend on _startupManager
            _runAtStartupToggle = _startupManager.IsStartupEnabled;
            OnPropertyChanged(nameof(RunAtStartupToggle));
            OnPropertyChanged(nameof(IsAdministratorSettingsEnabled));
            OnPropertyChanged(nameof(IsStartupOptionAvailable));

            _isInitialized = true;
        }

        #region Commands

        [RelayCommand]
        private void Restart() => AppInstance.Restart("-settings -silent");

        #endregion

        private void ShowAccessExceptionMessageBox()
        {
            _ = NativeHelper.MessageBox(SettingsWindow.WindowHandle,
                "To do this action please restart this app as Administrator",
                "Access denied",
                0x00000030);   // MB_OK and MB_ICONWARNING
        }
    }
}
