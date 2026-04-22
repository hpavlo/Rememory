using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper;
using System.Collections.Generic;
using System.ComponentModel;

namespace Rememory.Models
{
    public static class TabItemFactory
    {
        public static IEnumerable<TabItemModel> GetDefaultTabs() => [
                new(NavigationTabItemType.Home, "/Clipboard/NavigationTab_Home/Text".GetLocalizedResource(), "\uE80F", "1", "/Clipboard/NavigationTab_Home/Description".GetLocalizedResource(), "\uF0E3"),
                new(NavigationTabItemType.Fovorites, "/Clipboard/NavigationTab_Favorites/Text".GetLocalizedResource(), "\uE734", "2", "/Clipboard/NavigationTab_Favorites/Description".GetLocalizedResource()),
                new(NavigationTabItemType.Text, "/Clipboard/NavigationTab_Text/Text".GetLocalizedResource(), "\uE8E9", "3", "/Clipboard/NavigationTab_Text/Description".GetLocalizedResource()),
                new(NavigationTabItemType.Images, "/Clipboard/NavigationTab_Images/Text".GetLocalizedResource(), "\uE8B9", "4", "/Clipboard/NavigationTab_Images/Description".GetLocalizedResource()),
                new(NavigationTabItemType.Files, "/Clipboard/NavigationTab_Files/Text".GetLocalizedResource(), "\uE8B7", "5", "/Clipboard/NavigationTab_Files/Description".GetLocalizedResource()),
                new(NavigationTabItemType.Links, "/Clipboard/NavigationTab_Links/Text".GetLocalizedResource(), "\uE71B", "6", "/Clipboard/NavigationTab_Links/Description".GetLocalizedResource())
            ];
    }

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

        public string AccessKey
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string BigGlyph
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string EmptyListMessage
        {
            get;
            set => SetProperty(ref field, value);
        }

        public NavigationTabItemType Type { get; private set; }

        public TagModel? Tag { get; private set; }

        public bool IsTag => Type == NavigationTabItemType.Tag && Tag is not null;

        public TabItemModel(NavigationTabItemType type, string title, string glyph, string accessKey, string emptyListMessage, string? bigGlyph = null)
        {
            Type = type;
            Title = title;
            Glyph = glyph;
            AccessKey = accessKey;
            BigGlyph = bigGlyph ?? glyph;
            EmptyListMessage = emptyListMessage;
        }

        public TabItemModel(TagModel tag)
        {
            Type = NavigationTabItemType.Tag;
            Tag = tag;
            Title = tag.Name;
            Glyph = TAG_GLYPH;
            BigGlyph = TAG_GLYPH;
            AccessKey = string.Empty;
            EmptyListMessage = "Clipboard/NavigationTab_Tag/Description".GetLocalizedResource();
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
        Text,
        Images,
        Files,
        Links,
        Tag
    }
}
