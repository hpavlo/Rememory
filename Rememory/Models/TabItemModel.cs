using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Rememory.Models
{
    public partial class TabItemModel : ObservableObject
    {
        private const string TAG_GLYPH = "\uEA3B";

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _glyph;
        public string Glyph
        {
            get => _glyph;
            set => SetProperty(ref _glyph, value);
        }

        public NavigationTabItemType Type { get; private set; }

        public TagModel? Tag { get; private set; }

        public bool IsTag => Type == NavigationTabItemType.Tag && Tag is not null;

        public TabItemModel(string title, string glyph, NavigationTabItemType type)
        {
            _title = title;
            _glyph = glyph;
            Type = type;
        }

        public TabItemModel(TagModel tag)
        {
            _title = tag.Name;
            _glyph = TAG_GLYPH;
            Type = NavigationTabItemType.Tag;
            Tag = tag;
            tag.PropertyChanged += Tag_PropertyChanged;
        }

        private void Tag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tag.Name))
            {
                Title = Tag?.Name ?? string.Empty;
            }
        }
    }

    public enum NavigationTabItemType
    {
        Home,
        Fovorites,
        Images,
        Links,
        Tag
    }
}
