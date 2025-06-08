using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rememory.Models
{
    public partial class TagModel(string name, SolidColorBrush colorBrush) : ObservableObject
    {
        public int Id { get; set; }

        private string _name = name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private SolidColorBrush _colorBrush = colorBrush;
        public SolidColorBrush ColorBrush
        {
            get => _colorBrush;
            set => SetProperty(ref _colorBrush, value);
        }

        public IList<ClipModel> Clips { get; set; } = [];

        public int ClipsCount => Clips.Count;

        public void UpdateProperty([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }
    }
}
