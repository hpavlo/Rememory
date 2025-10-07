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

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            _iconLoadCts?.Cancel();
            _iconLoadCts = new CancellationTokenSource();
            var token = _iconLoadCts.Token;
            var showInCompactMode = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this);

            Files.Clear();

            if (ClipData is DataModel clipData && clipData.Metadata is FilesMetadataModel filesMetadata)
            {
                var filesLimit = showInCompactMode ? 1 : FilesPreviewLimit;

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
                _ = LoadIconsAsync(token);
            }
        }

        private async Task LoadIconsAsync(CancellationToken token)
        {
            foreach (var fileItem in Files)
            {
                if (!fileItem.IsPathCorrect)
                {
                    continue;
                }

                token.ThrowIfCancellationRequested();
                fileItem.ImageSource = await FileIconHelper.GetFileIconAsync(fileItem.Path);
            }
        }
    }

    public partial class FilePreviewModel(string path) : ObservableObject
    {
        private SoftwareBitmapSource? _imageSource;
        public SoftwareBitmapSource? ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        private bool _showInCompactMode = false;
        public bool ShowInCompactMode
        {
            get => _showInCompactMode;
            set => SetProperty(ref _showInCompactMode, value);
        }

        private string? _rightSideInfo;
        public string? RightSideInfo
        {
            get => _rightSideInfo;
            set => SetProperty(ref _rightSideInfo, value);
        }

        public string Name { get; private set; } = System.IO.Path.GetFileName(path);
        public string Path { get; private set; } = path;
        public bool IsPathCorrect { get; private set; } = System.IO.Path.Exists(path);
    }
}
