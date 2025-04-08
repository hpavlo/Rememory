﻿using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rememory.Services
{
    public class OwnerService : IOwnerService
    {
        public event EventHandler<OwnerModel>? OwnerRegistered;
        public event EventHandler<string>? OwnerUnregistered;
        public event EventHandler? AllOwnersUnregistered;

        public Dictionary<string, OwnerModel> Owners { get; private set; }

        private readonly IStorageService _storageService;

        public OwnerService(IStorageService storageService)
        {
            _storageService = storageService;

            Owners = _storageService.GetOwners().ToDictionary(o => o.Path);

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
                // Trying to update existing info about owner in dictionary and DB
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
                    _storageService.UpdateOwner(owner);
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
                _storageService.AddOwner(owner);
                Owners.TryAdd(path, owner);
                owner.ClipsCount++;
                clip.Owner = owner;
            }

            // If it's a new Owner we will notify about it
            if (owner.ClipsCount == 1)
            {
                OnOwnerRegistered(owner);
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
                    // Do not remove empty owner from dictionary and DB
                    if (!string.IsNullOrEmpty(path))
                    {
                        Owners.Remove(path);
                        _storageService.DeleteOwner(knownOwner.Id);
                    }

                    // Notify about unregistered owner, including empty owner
                    OnOwnerUnregistered(path);
                }
            }

            clip.Owner = null;
        }

        public void UnregisterAllOwners()
        {
            Owners.Clear();
            OnAllOwnersUnregistered();
        }

        protected virtual void OnOwnerRegistered(OwnerModel owner)
        {
            OwnerRegistered?.Invoke(this, owner);
        }

        protected virtual void OnOwnerUnregistered(string ownerPath)
        {
            OwnerUnregistered?.Invoke(this, ownerPath);
        }

        protected virtual void OnAllOwnersUnregistered()
        {
            AllOwnersUnregistered?.Invoke(this, new());
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
