using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views.Controls.Behavior;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Views.Controls
{
    public sealed partial class FilesPreview : UserControl
    {
        public ObservableCollection<FilePreviewModel> Files { get; private set; } = [];
        public int FilesPreviewLimit { get; set; } = 5;

        public DataModel ClipData
        {
            get => (DataModel)GetValue(ClipDataProperty);
            set => SetValue(ClipDataProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty ClipDataProperty =
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(FilesPreview), new PropertyMetadata(null, OnClipDataChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(FilesPreview), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        private CancellationTokenSource? _iconLoadCts;

        public FilesPreview()
        {
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FilesPreview { IsLoaded: true } control)
            {
                return;
            }

            // Prepare file preview only if the control is loaded
            // This will be called if virtualization reuses the same item container for new data

            if (e.NewValue is DataModel { Metadata: FilesMetadataModel filesMetadata })
            {
                control.PrepareFilesPreview(filesMetadata);
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

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
            var showInCompactMode = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this);
            var filesLimit = showInCompactMode ? 1 : FilesPreviewLimit;

            Files.Clear();

            foreach (var path in filesMetadata.Paths.Take(filesLimit))
            {
                Files.Add(new(path) { ShowInCompactMode = showInCompactMode });
            }

            if (filesMetadata.Paths.Length > filesLimit)
            {
                string moreFilesText = "/Clipboard/Clip_FilesPreview_MoreFilesCount/Text".GetLocalizedFormatResource(filesMetadata.Paths.Length - filesLimit);

                if (filesLimit == 1 && Files.FirstOrDefault() is FilePreviewModel fileModel)
                {
                    fileModel.RightSideInfo = moreFilesText;
                }
                else
                {
                    Files.Add(new(moreFilesText));
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

    public partial class FilePreviewModel(string path) : ObservableObject
    {
        [ObservableProperty]
        public partial SoftwareBitmapSource? ImageSource { get; set; }

        [ObservableProperty]
        public partial bool ShowInCompactMode { get; set; }

        [ObservableProperty]
        public partial string? RightSideInfo { get; set; }

        [ObservableProperty]
        public partial bool IsPathCorrect { get; set; }

        public string Name { get; } = System.IO.Path.GetFileName(path);
        public string Path { get; } = path;
    }
}
