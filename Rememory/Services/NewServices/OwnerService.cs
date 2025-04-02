using Rememory.Helper;
using Rememory.Models.NewModels;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rememory.Services.NewServices
{
    public class OwnerService
    {
        private readonly NewSqliteService _sqliteService = new();

        public Dictionary<string, OwnerModel> Owners { get; private set; }

        public OwnerService()
        {
            Owners = _sqliteService.GetOwners().ToDictionary(o => o.Path);

            OwnerModel emptyOwner = CreateEmptyOwner();
            Owners.Add(emptyOwner.Path, emptyOwner);
        }

        public void RegisterClipOwner(ClipModel clip, string? path, byte[]? icon)
        {
            // If we don't have owner info, we will take the empty owner
            path ??= string.Empty;
            string? ownerName = File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).ProductName : null;

            if (Owners.TryGetValue(path, out var owner))
            {
                bool toUpdate = false;
                if (!string.Equals(owner.Name, ownerName))
                {
                    owner.Name = ownerName;
                    toUpdate = true;
                }

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(owner.Icon, icon))
                {
                    owner.Icon = icon;
                    toUpdate = true;
                }

                if (toUpdate)
                {
                    _sqliteService.UpdateOwner(owner);
                }

                owner.ClipsCount++;
                clip.Owner = owner;
            }
            else
            {
                owner = new(path)
                {
                    Icon = icon,
                    Name = ownerName
                };
                _sqliteService.AddOwner(owner);
                Owners.TryAdd(path, owner);
                owner.ClipsCount++;
                clip.Owner = owner;
            }

            if (owner.ClipsCount == 1)
            {
                // OwnerRegistered
            }
        }

        public void UnregisterClipOwner(ClipModel clip)
        {
            if (clip.Owner is null)
            {
                return;
            }

            string path = clip.Owner.Path ?? string.Empty;

            if (Owners.TryGetValue(path, out var knownOwner))
            {
                knownOwner.ClipsCount--;

                if (knownOwner.ClipsCount == 0)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        Owners.Remove(path);
                        _sqliteService.DeleteOwner(knownOwner.Id);
                    }

                    // OwnerUnregistered
                }
            }

            clip.Owner = null;
        }

        public void UnregisterAllOwners()
        {
            Owners.Clear();
            // AllOwnersUnregistered
        }

        private OwnerModel CreateEmptyOwner()
        {
            return new OwnerModel(string.Empty)
            {
                Id = 0,
                Name = "UnknownOwnerAppTitle".GetLocalizedResource()
            };
        }
    }
}
