using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Rememory.Views.Settings.Controls
{
    public sealed partial class TagEditorDialog : UserControl
    {
        public IList<SolidColorBrush> Colors { get; } = [
            new SolidColorBrush("#6615F514".ToColor()),
            new SolidColorBrush("#66008080".ToColor()),
            new SolidColorBrush("#664682B4".ToColor()),
            new SolidColorBrush("#660077FF".ToColor()),
            new SolidColorBrush("#6650F9FF".ToColor()),
            new SolidColorBrush("#66FFEA00".ToColor()),
            new SolidColorBrush("#66FF4500".ToColor()),
            new SolidColorBrush("#66DC143C".ToColor()),
            new SolidColorBrush("#66F036D7".ToColor()),
            new SolidColorBrush("#669370DB".ToColor()),
            ];

        public SolidColorBrush SelectedColor
        {
            get => SelectedColorBorder.Background as SolidColorBrush ?? Colors[0];
            set => SelectedColorBorder.Background = value;
        }

        public string TagName { get; set; } = string.Empty;

        private ContentDialog? _dialog;

        public TagEditorDialog()
        {
            InitializeComponent();
            SelectedColor = Colors[0];
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (Parent is ContentDialog dialog)
            {
                _dialog = dialog;
            }
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dialog is null)
            {
                return;
            }

            _dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(TagName);
        }

        private void ColorsList_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is SolidColorBrush brush)
            {
                SelectedColor = brush;
                SelectColorFlyout.Hide();
            }
        }
    }
}
