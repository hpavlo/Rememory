using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class FilesPreview : DataPreviewBase
    {
        public ObservableCollection<FilePreviewModel> Files { get; private set; } = [];

        public static readonly DependencyProperty FilePresenterPaddingProperty =
            DependencyProperty.Register(nameof(FilePresenterPadding), typeof(Thickness), typeof(FilesPreview), new PropertyMetadata(new Thickness(0, 2, 0, 2)));
        public Thickness FilePresenterPadding
        {
            get => (Thickness)GetValue(FilePresenterPaddingProperty);
            set => SetValue(FilePresenterPaddingProperty, value);
        }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(FilesPreview), new PropertyMetadata(TextWrapping.NoWrap));
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly DependencyProperty ShowFullPathProperty =
            DependencyProperty.Register(nameof(ShowFullPath), typeof(bool), typeof(FilesPreview), new PropertyMetadata(false));
        public bool ShowFullPath
        {
            get => (bool)GetValue(ShowFullPathProperty);
            set => SetValue(ShowFullPathProperty, value);
        }

        private CancellationTokenSource? _iconLoadCts;

        public FilesPreview()
        {
            InitializeComponent();
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (!IsLoaded)
            {
                return;
            }

            /// Prepare file preview only if the control is loaded
            /// This will be called if virtualization reuses the same item container for new data

            if (args.NewValue is DataModel { Metadata: FilesMetadataModel filesMetadata })
            {
                PrepareFilesPreview(filesMetadata);
            }
            else
            {
                Files.Clear();
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ClipData is DataModel { Metadata: FilesMetadataModel filesMetadata })
            {
                PrepareFilesPreview(filesMetadata);
            }
        }

        private void PrepareFilesPreview(FilesMetadataModel filesMetadata)
        {
            _iconLoadCts?.Cancel();
            _iconLoadCts = new CancellationTokenSource();
            var token = _iconLoadCts.Token;

            var showInCompactMode = PreviewVisualState == DataPreviewVisualState.Compact;
            var filesLimit = showInCompactMode ? 1 : 5;
            bool useSeparateLineForMoreFielesInfo = false;

            Files.Clear();

            if (filesMetadata.Paths.Length > filesLimit && filesLimit > 1)
            {
                filesLimit--;   // For moreFilesText
                useSeparateLineForMoreFielesInfo = true;
            }

            foreach (var path in filesMetadata.Paths.Take(filesLimit))
            {
                Files.Add(new(path, ShowFullPath) { ShowInCompactMode = showInCompactMode });
            }

            if (filesMetadata.Paths.Length > filesLimit)
            {
                string moreFilesText = "/Clipboard/Clip_FilesPreview_MoreFilesCount/Text".GetLocalizedFormatResource(filesMetadata.Paths.Length - filesLimit);

                if (!useSeparateLineForMoreFielesInfo && filesLimit == 1 && Files.FirstOrDefault() is FilePreviewModel fileModel)
                {
                    fileModel.RightSideInfo = moreFilesText;
                }
                else
                {
                    Files.Add(new(moreFilesText) { ShowInCompactMode = showInCompactMode });
                }
            }

            var iconSize = (double)Resources["FilePreviewImageSize"];
            var scale = XamlRoot?.RasterizationScale ?? 1;
            var scaledIconSize = (int)(iconSize * scale);

            _ = LoadIconsAsync(scaledIconSize, token);
        }

        private async Task LoadIconsAsync(int iconSize, CancellationToken token)
        {
            foreach (var fileItem in Files)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                fileItem.IsPathCorrect = await Task.Run(() => System.IO.Path.Exists(fileItem.Path));

                if (!fileItem.IsPathCorrect)
                {
                    continue;
                }

                fileItem.ImageSource = await FileIconHelper.GetFileIconAsync(fileItem.Path, iconSize);
            }
        }
    }

    public partial class FilePreviewModel : ObservableObject
    {
        [ObservableProperty]
        public partial SoftwareBitmapSource? ImageSource { get; set; }

        [ObservableProperty]
        public partial bool ShowInCompactMode { get; set; }

        [ObservableProperty]
        public partial string? RightSideInfo { get; set; }

        [ObservableProperty]
        public partial bool IsPathCorrect { get; set; }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string DisplayText { get; private set; }

        public FilePreviewModel(string path, bool showFullPath = false)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
            DisplayText = showFullPath ? Path : Name;
        }
    }
}
