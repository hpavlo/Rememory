using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Storage.Pickers;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Services;
using Rememory.Views.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rememory.ViewModels.Settings
{
    public partial class StoragePageViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;
        private readonly IClipTransferService _clipTransferService = App.Current.Services.GetService<IClipTransferService>()!;

        public SettingsContext SettingsContext { get; } = App.Current.SettingsContext;
        public bool IsExportInProgress
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && value)
                {
                    IsExportedSuccessfully = null;
                }
            }
        }

        public bool IsImportInProgress
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && value)
                {
                    IsImportedSuccessfully = null;
                }
            }
        }

        public bool? IsExportedSuccessfully
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool? IsImportedSuccessfully
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsRetentionPeriodParametersEnabled => SettingsContext.CleanupType == CleanupType.RetentionPeriod;
        public bool IsQuantityParametersEnabled => SettingsContext.CleanupType == CleanupType.Quantity;

        public CleanupType CleanupType
        {
            get => SettingsContext.CleanupType;
            set
            {
                if (SettingsContext.CleanupType != value)
                {
                    SettingsContext.CleanupType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRetentionPeriodParametersEnabled));
                    OnPropertyChanged(nameof(IsQuantityParametersEnabled));
                }
            }
        }

        #region Commands

        [RelayCommand]
        private void EraseClips()
        {
            var eraseFavorites = SettingsContext.IsFavoriteClipsErasingEnabled;
            var eraseWithProtectedTags = SettingsContext.IsTagProtectedClipsErasingEnabled;

            _clipboardService.DeleteClipsByFilter(clip => (eraseFavorites || !clip.IsFavorite)
                && (eraseWithProtectedTags || !clip.Tags.Any(tag => !tag.IsCleaningEnabled)));
        }

        [RelayCommand]
        private async Task ExportAllClips()
        {
            var picker = new FileSavePicker(SettingsWindow.WindowId);
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
            var picker = new FileOpenPicker(SettingsWindow.WindowId);
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
