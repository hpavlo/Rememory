using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Service;
using Rememory.Views.Settings;
using System;
using System.Windows.Input;

namespace Rememory.ViewModels
{
    public class SettingsGeneralPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private IStartupService _startupService = App.Current.Services.GetService<IStartupService>();

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

        public SettingsGeneralPageViewModel()
        {
            _runAtStartupToggle = _startupService.IsStartupEnabled;
            _runAsAdministratorToggle = _startupService.IsStartupAsAdministratorEnabled;

            InitializeCommands();
        }

        public ICommand RestartCommand { get; private set; }
        public ICommand RestartAsAdministratorCommand { get; private set; }

        private void InitializeCommands()
        {
            RestartAsAdministratorCommand = new RelayCommand(
                () => AdministratorHelper.TryToRestartApp(true, "-settings -silent"),
                () => !AdministratorHelper.IsAppRunningAsAdministrator());

            RestartCommand = new RelayCommand(() => AdministratorHelper.TryToRestartApp(false, "-settings -silent"));
        }

        private void ShowAccessExceptionMessageBox()
        {
            var res = NativeHelper.MessageBox(SettingsWindow.WindowHandle,
                            "MessageBox_AccessDenied/Text".GetLocalizedResource(),
                            "MessageBox_AccessDenied/Caption".GetLocalizedResource(),
                            0x00000031);   // MB_OKCANCEL and MB_ICONWARNING
            if (res == 1 && RestartAsAdministratorCommand.CanExecute(null))
            {
                RestartAsAdministratorCommand.Execute(null);
            }
        }
    }
}
