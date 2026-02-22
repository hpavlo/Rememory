using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.IO;

namespace Rememory.Models
{
    public partial class AppTreeViewNode : ObservableObject
    {
        public string Title
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public SoftwareBitmapSource? Image
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string OwnerPath
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public bool IsExpanded
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public ObservableCollection<AppTreeViewNode> Children
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        public bool IsSelected = true;

        public AppTreeViewNode() { }

        public AppTreeViewNode(OwnerModel owner)
        {
            Title = owner.Name ?? Path.GetFileName(owner.Path);
            Image = owner.IconBitmap;
            OwnerPath = owner.Path;
        }
    }
}
