using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using System.Collections.ObjectModel;
using System.IO;

namespace Rememory.Models
{
    public partial class AppTreeViewNode : ObservableObject
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private SoftwareBitmapSource _image;
        public SoftwareBitmapSource Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        private string _ownerPath = string.Empty;
        public string OwnerPath
        {
            get => _ownerPath;
            set => SetProperty(ref _ownerPath, value);
        }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private ObservableCollection<AppTreeViewNode> _children = [];
        public ObservableCollection<AppTreeViewNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        public bool IsSelected = true;

        public AppTreeViewNode() { }

        public AppTreeViewNode(OwnerApp app)
        {
            Title = Path.GetFileName(app.Path) is string path && !string.IsNullOrWhiteSpace(path) ? path : "UnknownOwnerAppTitle".GetLocalizedResource();
            Image = app.IconBitmap;
            OwnerPath = app.Path;
        }
    }
}
