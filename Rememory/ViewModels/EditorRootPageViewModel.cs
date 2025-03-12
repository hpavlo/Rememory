using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Views.Editor;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;

namespace Rememory.ViewModels
{
    public class EditorRootPageViewModel : ObservableObject
    {
        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>();

        private ClipboardItem _context;

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

        public ICommand SaveTextCommand { get; private set; }

        public EditorRootPageViewModel(ClipboardItem itemContext)
        {
            _context = itemContext;
            _text = itemContext.DataMap.GetValueOrDefault(ClipboardFormat.Text, string.Empty);

            SaveTextCommand = new RelayCommand<ClipboardItem>(_ => SaveChanges());
        }

        private void SaveChanges()
        {
            var bytes = Encoding.Unicode.GetBytes(Text.EndsWith('\0') ? Text : (Text + '\0'));
            var hash = SHA256.HashData(bytes);

            var newItem = new ClipboardItem()
            {
                DataMap = { { ClipboardFormat.Text, Text } },
                HashMap = { { ClipboardFormat.Text, hash } },
                IsFavorite = _context.IsFavorite,
                Time = DateTime.Now,
                OwnerPath = _context.OwnerPath,
                OwnerIconBitmap = _context.OwnerIconBitmap
            };

            _clipboardService.AddNewItem(newItem);
            _clipboardService.DeleteItem(_context);

            EditorWindow.CloseEditorWindow();
        }
    }
}
