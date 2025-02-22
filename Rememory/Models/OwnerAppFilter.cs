using CommunityToolkit.Mvvm.ComponentModel;

namespace Rememory.Models
{
    public partial class OwnerAppFilter : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _pattern = string.Empty;
        public string Pattern
        {
            get => _pattern;
            set => SetProperty(ref _pattern, value);
        }

        private int _filteredCount = 0;
        public int FilteredCount
        {
            get => _filteredCount;
            set => SetProperty(ref _filteredCount, value);
        }

        public OwnerAppFilter() { }

        public OwnerAppFilter(string name, string pattern)
        {
            _name = name;
            _pattern = pattern;
        }
    }
}
