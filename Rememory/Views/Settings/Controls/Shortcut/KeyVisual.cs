using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Windows.System;

namespace Rememory.Views.Settings.Controls.Shortcut
{
    [TemplatePart(Name = KeyPresenter, Type = typeof(KeyCharPresenter))]
    public sealed partial class KeyVisual : Control
    {
        private const string KeyPresenter = "KeyPresenter";

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(KeyVisual), new PropertyMetadata(default(string)));

        private KeyCharPresenter _keyPresenter = null!;

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public KeyVisual()
        {
            DefaultStyleKey = typeof(KeyVisual);
        }

        protected override void OnApplyTemplate()
        {
            _keyPresenter = (KeyCharPresenter)GetTemplateChild(KeyPresenter);
            UpdateContent();
            base.OnApplyTemplate();
        }

        private void UpdateContent()
        {
            if (Content is null)
            {
                return;
            }

            if (Content is int keyCode)
            {
                VirtualKey vKey = (VirtualKey)keyCode;
                switch (vKey)
                {
                    case VirtualKey.Enter:
                        SetGlyphContent("\uE751");
                        break;

                    case VirtualKey.Back:
                        SetGlyphContent("\uE750");
                        break;

                    case VirtualKey.Space:
                        SetGlyphContent("\uE75D");
                        break;

                    case VirtualKey.Shift:
                    case VirtualKey.LeftShift:
                    case VirtualKey.RightShift:
                        SetGlyphContent("\uE752");
                        break;

                    case VirtualKey.Up:
                        SetGlyphContent("\uE0E4");
                        break;

                    case VirtualKey.Down:
                        SetGlyphContent("\uE0E5");
                        break;

                    case VirtualKey.Left:
                        SetGlyphContent("\uE0E2");
                        break;

                    case VirtualKey.Right:
                        SetGlyphContent("\uE0E3");
                        break;

                    case VirtualKey.Multiply:
                        SetGlyphContent("\uE947");
                        break;

                    case VirtualKey.Add:
                        SetGlyphContent("\uE948");
                        break;

                    case VirtualKey.Subtract:
                        SetGlyphContent("\uE949");
                        break;

                    case VirtualKey.Divide:
                        SetGlyphContent("\uE94A");
                        break;

                    case VirtualKey.LeftWindows:
                    case VirtualKey.RightWindows:
                        _keyPresenter.Style = (Style)Application.Current.Resources["WindowsKeyCharPresenterStyle"];
                        break;

                    default:
                        _keyPresenter.Content = KeyboardHelper.VirtualKeyToString(keyCode);
                        break;
                }
            }
        }

        private void SetGlyphContent(string glyph)
        {
            _keyPresenter.Content = glyph;
            _keyPresenter.Style = (Style)Application.Current.Resources["GlyphKeyCharPresenterStyle"];
        }
    }
}
