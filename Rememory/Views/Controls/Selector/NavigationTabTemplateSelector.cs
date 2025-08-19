using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Models;

namespace Rememory.Views.Controls.Selector
{
    public partial class NavigationTabTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TabItemTemplate { get; set; }
        public DataTemplate? TagTabItemTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is TabItemModel tabItem)
            {
                return (tabItem.IsTag ? TagTabItemTemplate : TabItemTemplate) ?? base.SelectTemplateCore(item);
            }

            return base.SelectTemplateCore(item);
        }
    }
}
