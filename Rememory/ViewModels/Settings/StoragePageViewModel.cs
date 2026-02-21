using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Storage.Pickers;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Services;
using System;
using System.Threading.Tasks;

namespace Rememory.ViewModels.Settings
{
    public partial class StoragePageViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;
        private readonly IClipTransferService _clipTransferService = App.Current.Services.GetService<IClipTransferService>()!;

        public SettingsContext SettingsContext => SettingsContext.Instance;

        private bool _isExportInProgress;
        public bool IsExportInProgress
        {
            get => _isExportInProgress;
            set
            {
                if (SetProperty(ref _isExportInProgress, value) && value)
                {
                    IsExportedSuccessfully = null;
                }
            }
        }

        private bool _isImportInProgress;
        public bool IsImportInProgress
        {
            get => _isImportInProgress;
            set
            {
                if (SetProperty(ref _isImportInProgress, value) && value)
                {
                    IsImportedSuccessfully = null;
                }
            }
        }

        private bool? _isExportedSuccessfully;
        public bool? IsExportedSuccessfully
        {
            get => _isExportedSuccessfully;
            set => SetProperty(ref _isExportedSuccessfully, value);
        }

        private bool? _isImportedSuccessfully;
        public bool? IsImportedSuccessfully
        {
            get => _isImportedSuccessfully;
            set => SetProperty(ref _isImportedSuccessfully, value);
        }

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

        [RelayCommand]
        private async Task ExportAllClips()
        {
            var picker = new FileSavePicker(App.Current.ClipboardWindow.AppWindow.OwnerWindowId);
            picker.SuggestedFileName = string.Format(ClipTransferService.BackupFileNameFormat_, DateTime.Now);
            picker.FileTypeChoices.Add(ClipTransferService.BackupFileType_);

            var pickFileResult = await picker.PickSaveFileAsync();

            if (!string.IsNullOrEmpty(pickFileResult?.Path))
            {
                IsExportInProgress = true;
                IsExportedSuccessfully = null;
                try
                {
                    IsExportedSuccessfully = await _clipTransferService.ExportAsync(_clipboardService.Clips, pickFileResult.Path);
                }
                catch
                {
                    IsExportedSuccessfully = false;
                }
                IsExportInProgress = false;
            }
        }

        [RelayCommand]
        private async Task ImportClips()
        {
            var picker = new FileOpenPicker(App.Current.ClipboardWindow.AppWindow.OwnerWindowId);
            foreach (var fileType in ClipTransferService.BackupFileType_.Value)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            var pickFileResult = await picker.PickSingleFileAsync();

            if (!string.IsNullOrEmpty(pickFileResult?.Path))
            {
                IsImportInProgress = true;
                IsImportedSuccessfully = null;
                try
                {
                    IsImportedSuccessfully = await _clipTransferService.ImportAsync(pickFileResult.Path);
                }
                catch
                {
                    IsImportedSuccessfully = false;
                }
                IsImportInProgress = false;
            }
        }

        #endregion
    }
}
