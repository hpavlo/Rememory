using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Rememory.Views.Controls.Behavior
{
    public static class PreviewControlsHelper
    {
        public static bool IsOpenInToolTip(UserControl previewControl)
        {
            DependencyObject? currentElement = previewControl;

            while (currentElement is not null)
            {
                if (currentElement is ToolTip)
                {
                    return true;
                }

                currentElement = VisualTreeHelper.GetParent(currentElement);
            }

            return false;
        }
    }
}
