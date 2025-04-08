using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Rememory.Contracts
{
    public class ClipboardEventArgs(IList<ClipModel> clips, [Optional] ClipModel changedClip) : EventArgs
    {
        /// <summary>
        /// All clips we have after update
        /// </summary>
        public IList<ClipModel> Clips { get; set; } = clips;
        /// <summary>
        /// Only changed clip
        /// </summary>
        public ClipModel ChangedClip { get; set; } = changedClip;
    }
}
