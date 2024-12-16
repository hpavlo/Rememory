using System;

namespace Rememory.Hooks
{
    public interface IKeyboardMonitor
    {
        event EventHandler<GlobalKeyboardHookEventArgs> KeyboardEvent;
        void StartMonitor();
        void StopMonitor();
    }
}
