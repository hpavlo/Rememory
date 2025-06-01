using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Rememory.Models
{
    public partial class TagModel(string name) : ObservableObject
    {
        public int Id { get; set; }

        private string _name = name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public IList<ClipModel> Clips { get; set; } = [];
    }
}
