using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Views.Editor;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Rememory.ViewModels
{
    public partial class EditorRootPageViewModel : ObservableObject
    {
        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;
        private IOwnerService _ownerService = App.Current.Services.GetService<IOwnerService>()!;

        private readonly ClipModel _context;

        private bool _isTextChanged;
        public  bool IsTextChanged
        {
            get => _isTextChanged;
            set => SetProperty(ref _isTextChanged, value);
        }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (SetProperty(ref _text, value) && !IsTextChanged)
                {
                    IsTextChanged = true;
                }
            }
        }

        public EditorRootPageViewModel(ClipModel clipContext)
        {
            _context = clipContext;
            _text = clipContext.Data.TryGetValue(ClipboardFormat.Text, out DataModel? dataModel) ? dataModel.Data : string.Empty;
        }

        [RelayCommand]
        private void SaveText()
        {
            byte[] bytes = Encoding.Unicode.GetBytes(Text.EndsWith('\0') ? Text : (Text + '\0'));
            byte[] hash = SHA256.HashData(bytes);

            ClipModel newClip = new()
            {
                Data = { { ClipboardFormat.Text, new DataModel(ClipboardFormat.Text, Text, hash) } },
                IsFavorite = _context.IsFavorite,
                ClipTime = DateTime.Now
            };

            _ownerService.RegisterClipOwner(newClip, _context.Owner?.Path, _context.Owner?.Icon);
            _clipboardService.AddClip(newClip);
            _clipboardService.DeleteClip(_context);

            EditorWindow.CloseEditorWindow();
        }
    }
}
