using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Services;

namespace Rememory.ViewModels.Settings
{
    public partial class StoragePageViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;

        public SettingsContext SettingsContext => SettingsContext.Instance;

        public bool IsRetentionPeriodParametersEnabled => SettingsContext.CleanupType == CleanupType.RetentionPeriod;
        public bool IsQuantityParametersEnabled => SettingsContext.CleanupType == CleanupType.Quantity;

        public CleanupType CleanupType
        {
            get => SettingsContext.CleanupType;
            set
            {
                if (SettingsContext.CleanupType != value) {
                    SettingsContext.CleanupType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRetentionPeriodParametersEnabled));
                    OnPropertyChanged(nameof(IsQuantityParametersEnabled));
                }
            }
        }

        #region Commands

        [RelayCommand]
        private void EraseClipboardData() => _clipboardService.DeleteAllClips();

        #endregion
    }
}
