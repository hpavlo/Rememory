using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

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

        private Regex? _compiledRegex;
        private string _pattern = string.Empty;
        public string Pattern
        {
            get => _pattern;
            set
            {
                if (SetProperty(ref _pattern, value))
                {
                    var normalizedPattern = Pattern.Replace('\\', '/').Replace("*", ".*");
                    _compiledRegex = new(normalizedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        }

        private int _filteredCount = 0;
        public int FilteredCount
        {
            get => _filteredCount;
            set => SetProperty(ref _filteredCount, value);
        }

        public OwnerAppFilter(string name, string pattern)
        {
            Name = name;
            Pattern = pattern;
        }

        public bool IsMatch(string value) => _compiledRegex?.IsMatch(value) ?? false;
    }
}
