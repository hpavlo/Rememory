using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.RegularExpressions;

namespace Rememory.Models
{
    public partial class OwnerAppFilter : ObservableObject
    {
        public string Name
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        private Regex? _compiledRegex;
        public string Pattern
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    var normalizedPattern = Pattern.Replace('\\', '/').Replace("*", ".*");
                    _compiledRegex = new(normalizedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        } = string.Empty;

        public int FilteredCount
        {
            get;
            set => SetProperty(ref field, value);
        } = 0;

        public OwnerAppFilter(string name, string pattern)
        {
            Name = name;
            Pattern = pattern;
        }

        public bool IsMatch(string value) => _compiledRegex?.IsMatch(value) ?? false;
    }
}
