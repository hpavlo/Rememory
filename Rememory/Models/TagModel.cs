using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rememory.Models
{
    public partial class TagModel : ObservableObject
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _colorHex = string.Empty;
        public string ColorHex
        {
            get => _colorHex;
            set
            {
                if (SetProperty(ref _colorHex, value))
                {
                    if (App.Current.DispatcherQueue.HasThreadAccess)
                    {
                        // Already on UI thread, run directly
                        ColorBrush = new SolidColorBrush(_colorHex.ToColor());
                    }
                    else
                    {
                        // Not on UI thread, enqueue
                        App.Current.DispatcherQueue.TryEnqueue(() => ColorBrush = new SolidColorBrush(_colorHex.ToColor()));
                    }
                }
            }
        }

        private SolidColorBrush? _colorBrush;
        public SolidColorBrush? ColorBrush
        {
            get => _colorBrush;
            private set => SetProperty(ref _colorBrush, value);
        }

        private bool _isCleaningEnabled;

        public bool IsCleaningEnabled
        {
            get => _isCleaningEnabled;
            set => SetProperty(ref _isCleaningEnabled, value);
        }

        public IList<ClipModel> Clips { get; set; } = [];

        public int ClipsCount => Clips.Count;

        public TagModel(string name, string colorHex, bool isCleaningEnabled)
        {
            Name = name;
            ColorHex = colorHex;
            IsCleaningEnabled = isCleaningEnabled;
        }

        public void TogglePropertyUpdate([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }
    }
}
