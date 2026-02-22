using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Rememory.Models
{
    public partial class TabItemModel : ObservableObject
    {
        private const string TAG_GLYPH = "\uEA3B";

        public string Title
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string Glyph
        {
            get;
            set => SetProperty(ref field, value);
        }

        public NavigationTabItemType Type { get; private set; }

        public TagModel? Tag { get; private set; }

        public bool IsTag => Type == NavigationTabItemType.Tag && Tag is not null;

        public TabItemModel(string title, string glyph, NavigationTabItemType type)
        {
            Title = title;
            Glyph = glyph;
            Type = type;
        }

        public TabItemModel(TagModel tag)
        {
            Title = tag.Name;
            Glyph = TAG_GLYPH;
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
        Files,
        Links,
        Tag
    }
}
