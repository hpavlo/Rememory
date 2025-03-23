using FlaUI.UIA3;
using Interop.UIAutomationClient;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rememory.Helper
{
    /// <summary>
    /// Helps to get caret position in most installed applications
    /// Uses GUI, Acc and UIA methods
    /// </summary>
    public class TextBoxCaretHelper
    {
        private static UIA3Automation _uiAutomation = new UIA3Automation();

        public static bool GetCaretPosition(out Rectangle rect)
        {
            rect = Rectangle.Empty;

            if (TryGetCaretPosGUI(ref rect)) return true;
            if (TryGetCaretPosAcc(ref rect)) return true;
            if (TryGetCaretPosUIA(ref rect)) return true;

            return false;
        }

        private static bool TryGetCaretPosGUI(ref Rectangle rect)
        {
            Rectangle tempRect = Rectangle.Empty;

            IntPtr foregroundWindow = NativeHelper.GetForegroundWindow();
            uint threadId = NativeHelper.GetWindowThreadProcessId(foregroundWindow, out var processId);
            GUITHREADINFO guiThreadInfo = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };

            if (GetGUIThreadInfo(threadId, ref guiThreadInfo))
            {
                Point point = new(guiThreadInfo.rectCaret.left, guiThreadInfo.rectCaret.top);

                if (NativeHelper.ClientToScreen(guiThreadInfo.hwndCaret, ref point))
                {
                    tempRect = new Rectangle(point.X, point.Y, guiThreadInfo.rectCaret.right - guiThreadInfo.rectCaret.left, guiThreadInfo.rectCaret.bottom - guiThreadInfo.rectCaret.top);
                }
            }

            if (!tempRect.IsEmpty)
            {
                rect = tempRect;
                return true;
            }

            return false;
        }

        private static bool TryGetCaretPosUIA(ref Rectangle rect)
        {
            try
            {
                var focusedElement = _uiAutomation.FocusedElement();

                if (focusedElement.Patterns.Value.TryGetPattern(out var valuePattern)
                    && valuePattern.IsReadOnly.TryGetValue(out var _isReadonly)
                    && _isReadonly)
                {
                    return false;
                }

                if (focusedElement.Patterns.Text.TryGetPattern(out var textPattern) && textPattern.GetSelection() is var selections && selections.Length > 0)
                {
                    var rectangles = selections[0].GetBoundingRectangles();
                    if (rectangles.Length > 0 && !rectangles[0].IsEmpty)
                    {
                        rect = rectangles[0];
                        return true;
                    }

                    selections[0].ExpandToEnclosingUnit(FlaUI.Core.Definitions.TextUnit.Character);
                    rectangles = selections[0].GetBoundingRectangles();
                    if (rectangles.Length > 0 && !rectangles[0].IsEmpty)
                    {
                        rect = rectangles[0];
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool TryGetCaretPosAcc(ref Rectangle rect)
        {
            Rectangle tempRect = Rectangle.Empty;

            Guid guid = typeof(IAccessible).GUID;
            int objidCaret = -8;   // 0xFFFFFFF8
            object accessibleObject = null;
            IntPtr foregroundWindow = NativeHelper.GetForegroundWindow();

            try
            {
                Task.Run(() =>
                {
                    AccessibleObjectFromWindow(foregroundWindow, objidCaret, ref guid, out accessibleObject);
                    if (accessibleObject is IAccessible accessible)
                    {
                        accessible.accLocation(out int x, out int y, out int w, out int h, 0);
                        tempRect = new Rectangle(x, y, w, h);
                    }
                }).GetAwaiter().GetResult();
            }
            catch { }

            if (!tempRect.IsEmpty)
            {
                rect = tempRect;
                return true;
            }

            return false;
        }

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, int idObject, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(uint hTreadID, ref GUITHREADINFO lpgui);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rectCaret;
        }
    }
}
