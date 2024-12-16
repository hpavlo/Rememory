using Rememory.Helper;
using System.ComponentModel;

namespace Rememory.Hooks
{
    public sealed class GlobalKeyboardHookEventArgs : HandledEventArgs
    {
        internal GlobalKeyboardHook.KeyboardState KeyboardState { get; private set; }

        internal KeyboardHelper.LowLevelKeyboardInputEvent KeyboardData { get; private set; }

        internal GlobalKeyboardHookEventArgs(
            KeyboardHelper.LowLevelKeyboardInputEvent keyboardData,
            GlobalKeyboardHook.KeyboardState keyboardState)
        {
            KeyboardData = keyboardData;
            KeyboardState = keyboardState;
        }
    }
}
