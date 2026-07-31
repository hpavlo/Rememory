using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Rememory.Models;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class ClipPreviewFlyout : FlyoutBase
    {
        private readonly ClipPreviewFlyoutPresenter _presenter = new();

        public ClipPreviewFlyout()
        {
            ShowMode = FlyoutShowMode.Transient;
            ShouldConstrainToRootBounds = false;
            Placement = FlyoutPlacementMode.LeftEdgeAlignedTop;
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }

        public void ShowDataPreview(ClipModel? clip, string searchText, FrameworkElement placementTarget)
        {
            _presenter.TargetSize = new(placementTarget.ActualWidth, placementTarget.ActualHeight);
            _presenter.ClipModel = clip;
            _presenter.SearchText = searchText;

            ShowAt(placementTarget);
        }

        public void ShowNextFormatPreview()
        {
            if (IsOpen)
            {
                _presenter.ShowNextFormatPreview();
            }
        }

        public void ShowPreviousFormatPreview()
        {
            if (IsOpen)
            {
                _presenter.ShowPreviousFormatPreview();
            }
        }

        protected override Control CreatePresenter() => _presenter;
    }
}
