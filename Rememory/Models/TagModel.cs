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

        public string Name
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string ColorHex
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    if (App.Current.DispatcherQueue.HasThreadAccess)
                    {
                        // Already on UI thread, run directly
                        ColorBrush = new SolidColorBrush(ColorHex.ToColor());
                    }
                    else
                    {
                        // Not on UI thread, enqueue
                        App.Current.DispatcherQueue.TryEnqueue(() => ColorBrush = new SolidColorBrush(ColorHex.ToColor()));
                    }
                }
            }
        } = string.Empty;

        public SolidColorBrush? ColorBrush
        {
            get;
            private set => SetProperty(ref field, value);
        }

        public bool IsCleaningEnabled
        {
            get;
            set => SetProperty(ref field, value);
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
