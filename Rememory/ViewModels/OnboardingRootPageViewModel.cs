using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Models;
using Rememory.Services;
using System.Threading.Tasks;

namespace Rememory.ViewModels
{
    public partial class OnboardingRootPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext { get; } = App.Current.SettingsContext;

        private StartupManager? _startupManager;
        private bool _isInitialized = false;

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

            _isInitialized = true;
        }
    }
}
