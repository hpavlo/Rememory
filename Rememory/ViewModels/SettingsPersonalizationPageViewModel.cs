﻿using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Models;

namespace Rememory.ViewModels
{
    public class SettingsPersonalizationPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;
    }
}
