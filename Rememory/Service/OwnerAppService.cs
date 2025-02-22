using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Helper;
using Rememory.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Rememory.Service
{
    public class OwnerAppService : IOwnerAppService
    {
        /// <summary>
        /// Keeps info about owner app.
        /// Key is the owner app path
        /// </summary>
        private readonly Dictionary<string, OwnerApp> OwnerAppsDictionary = [];

        public Dictionary<string, OwnerApp> GetOwnerApps() => OwnerAppsDictionary;

        public void RegisterNewItem(string ownerPath, byte[] ownerIconBitmap)
        {
            // If owner app is not specified we save it as empty string
            ownerPath ??= string.Empty;

            if (OwnerAppsDictionary.TryGetValue(ownerPath, out var ownerApp))
            {
                ownerApp.ItemsCount++;
            }
            else
            {
                ownerApp = new OwnerApp()
                {
                    Path = ownerPath,
                    IconBitmap = BitmapHelper.GetBitmapFromBytes(ownerIconBitmap)
                };

                if (Path.Exists(ownerPath))
                {
                    ownerApp.Name = FileVersionInfo.GetVersionInfo(ownerPath).ProductName;
                }

                if (string.IsNullOrEmpty(ownerApp.Name))
                {
                    ownerApp.Name = "UnknownOwnerAppTitle".GetLocalizedResource();
                }

                ownerApp.ItemsCount++;
                OwnerAppsDictionary.Add(ownerPath, ownerApp);
            }
        }

        public void UnregisterItem(string ownerPath)
        {
            ownerPath ??= string.Empty;

            if (OwnerAppsDictionary.TryGetValue(ownerPath, out var ownerApp) && ownerApp.ItemsCount > 1)
            {
                ownerApp.ItemsCount--;
            }
            else
            {
                OwnerAppsDictionary.Remove(ownerPath);
            }
        }

        public void UnregisterAllItems()
        {
            OwnerAppsDictionary.Clear();
        }

        public SoftwareBitmapSource GetOwnerBitmap(string ownerPath)
        {
            if (ownerPath is null)
            {
                return null;
            }

            return OwnerAppsDictionary.GetValueOrDefault(ownerPath)?.IconBitmap;
        }
    }
}
