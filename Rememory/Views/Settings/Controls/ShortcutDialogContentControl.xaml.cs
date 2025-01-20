using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Rememory.Views.Settings.Controls
{
    public sealed partial class ShortcutDialogContentControl : UserControl
    {
        public IList<int> ShortcutKeys
        {
            get => (IList<int>)GetValue(ShortcutKeysProperty);
            set => SetValue(ShortcutKeysProperty, value);
        }

        public static readonly DependencyProperty ShortcutKeysProperty = 
            DependencyProperty.Register("ShortcutKeys", typeof(IList<int>), typeof(ShortcutDialogContentControl), 
                new PropertyMetadata(default(IList<int>)));

        public bool IsError
        {
            get => (bool)GetValue(IsErrorProperty);
            set => SetValue(IsErrorProperty, value);
        }

        public static readonly DependencyProperty IsErrorProperty =
            DependencyProperty.Register("IsError", typeof(bool), typeof(ShortcutDialogContentControl),
                new PropertyMetadata(default(bool), OnIsErrorChanged));

        public ShortcutDialogContentControl()
        {
            this.InitializeComponent();
        }

        private static void OnIsErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VisualStateManager.GoToState((Control)d, (bool)e.NewValue ? "Error" :"Normal", true);
        }
    }
}
