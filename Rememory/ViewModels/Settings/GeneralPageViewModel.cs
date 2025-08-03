using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Views.Settings;
using System;

namespace Rememory.ViewModels.Settings
{
    public partial class GeneralPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private IStartupService _startupService = App.Current.Services.GetService<IStartupService>()!;

        public bool IsAdministratorSettingsEnabled => AdministratorHelper.IsAppRunningAsAdministrator() && RunAtStartupToggle;

        private bool _runAtStartupToggle;
        public bool RunAtStartupToggle
        {
            get => _runAtStartupToggle;
            set
            {
                if (SetProperty(ref _runAtStartupToggle, value))
                {
                    try
                    {
                        _startupService.IsStartupEnabled = value;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowAccessExceptionMessageBox();
                        SetProperty(ref _runAtStartupToggle, !value);
                    }

                    OnPropertyChanged(nameof(IsAdministratorSettingsEnabled));
                }
            }
        }

        private bool _runAsAdministratorToggle;
        public bool RunAsAdministratorToggle
        {
            get => _runAsAdministratorToggle;
            set
            {
                if (SetProperty(ref _runAsAdministratorToggle, value))
                {
                    try
                    {
                        _startupService.IsStartupAsAdministratorEnabled = value;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowAccessExceptionMessageBox();
                        SetProperty(ref _runAsAdministratorToggle, !value);
                    }
                }
            }
        }

        public GeneralPageViewModel()
        {
            _runAtStartupToggle = _startupService.IsStartupEnabled;
            _runAsAdministratorToggle = _startupService.IsStartupAsAdministratorEnabled;
        }

        #region Commands

        [RelayCommand]
        private void Restart() => AdministratorHelper.TryToRestartApp(false, "-settings -silent");

        private bool CanRestartAsAdministrator() => !AdministratorHelper.IsAppRunningAsAdministrator();

        [RelayCommand(CanExecute = nameof(CanRestartAsAdministrator))]
        private void RestartAsAdministrator() => AdministratorHelper.TryToRestartApp(true, "-settings -silent");

        #endregion

        private void ShowAccessExceptionMessageBox()
        {
            var res = NativeHelper.MessageBox(SettingsWindow.WindowHandle,
                            "To do this action please restart this app as Administrator",
                            "Access denied",
                            0x00000031);   // MB_OKCANCEL and MB_ICONWARNING
            if (res == 1 && RestartAsAdministratorCommand.CanExecute(null))
            {
                RestartAsAdministratorCommand.Execute(null);
            }
        }
    }
}
