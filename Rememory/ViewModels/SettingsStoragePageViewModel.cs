using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Models;
using Rememory.Service;
using System.Windows.Input;

namespace Rememory.ViewModels
{
    public class SettingsStoragePageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>();

        public SettingsStoragePageViewModel()
        {
            InitializeCommands();
        }

        public ICommand EraseClipboardDataCommand { get; private set; }

        private void InitializeCommands()
        {
            EraseClipboardDataCommand = new RelayCommand(_clipboardService.DeleteAllItems);
        }
    }
}
