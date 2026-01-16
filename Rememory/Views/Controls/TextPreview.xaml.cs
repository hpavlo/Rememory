using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;

namespace Rememory.Views.Controls
{
    public sealed partial class TextPreview : UserControl
    {
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
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(TextPreview), new PropertyMetadata(null, OnClipDataChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(TextPreview), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public TextPreview()
        {
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextPreview control && e.NewValue is DataModel clipData)
            {
                control.PreviewTextBlock.SearchHighlight(control.SearchText, clipData.Data);
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextPreview control && e.NewValue is string searchText)
            {
                control.PreviewTextBlock.SearchHighlight(searchText);
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            string visualState = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this) ? "CompactView" : "NormalView";
            VisualStateManager.GoToState(this, visualState, true);
        }
    }
}
