using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Views.Controls
{
    public sealed partial class FilesPreview : UserControl
    {
        public ObservableCollection<FilePreviewModel> Files { get; private set; }

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
            Files = [];
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilesPreview control)
            {
                control._iconLoadCts?.Cancel();
                control._iconLoadCts = new CancellationTokenSource();
                var token = control._iconLoadCts.Token;

                control.Files.Clear();

                if (e.NewValue is DataModel clipData && clipData.Metadata is FilesMetadataModel filesMetadata)
                {
                    var filesLimit = control.FilesPreviewLimit;

                    foreach (var path in filesMetadata.Paths.Take(filesLimit))
                    {
                        control.Files.Add(new(path));
                    }

                    if (filesMetadata.Paths.Length > filesLimit)
                    {
                        control.Files.Add(new("/Clipboard/Clip_FilesPreview_MoreFilesCount/Text".GetLocalizedFormatResource(filesMetadata.Paths.Length - filesLimit)));
                    }

                    _ = control.LoadIconsAsync(token);
                }
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        private async Task LoadIconsAsync(CancellationToken token)
        {
            try
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
            catch { }
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

        public string Name { get; private set; } = System.IO.Path.GetFileName(path);
        public string Path { get; private set; } = path;
        public bool IsPathCorrect { get; private set; } = System.IO.Path.Exists(path);
    }
}
