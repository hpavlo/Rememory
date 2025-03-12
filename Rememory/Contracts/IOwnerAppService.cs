using Microsoft.UI.Xaml.Media.Imaging;
using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    public interface IOwnerAppService
    {
        /// <summary>
        /// Occurs when we registered new app to dictionary
        /// </summary>
        event EventHandler<OwnerApp> AppRegistered;
        /// <summary>
        /// Occurs when we removed redistered app from dictionary
        /// </summary>
        event EventHandler<string> AppUnregistered;
        /// <summary>
        /// Occurs when we removed all apps from dictionary
        /// </summary>
        event EventHandler AllAppsUnregistered;

        /// <returns>Dictionary of owner apps info with owner path as a key</returns>
        Dictionary<string, OwnerApp> GetOwnerApps();

        /// <summary>
        /// Add new item to dictionary
        /// If owner app is already registered it will increase count of items
        /// </summary>
        /// <param name="ownerPath">Owner app path</param>
        /// <param name="ownerIconBitmap">Owner app bitmap</param>
        void RegisterNewItem(string ownerPath, byte[] ownerIconBitmap);

        /// <summary>
        /// Remove item from the dictionary
        /// </summary>
        /// <param name="ownerPath">Owner app path</param>
        void UnregisterItem(string ownerPath);

        /// <summary>
        /// Clear the dictionary
        /// </summary>
        void UnregisterAllItems();

        /// <summary>
        /// Used to get owner app icon by app path
        /// </summary>
        /// <param name="ownerPath">Owner app path</param>
        /// <returns>Icon bitmap</returns>
        SoftwareBitmapSource GetOwnerBitmap(string ownerPath);
    }
}
