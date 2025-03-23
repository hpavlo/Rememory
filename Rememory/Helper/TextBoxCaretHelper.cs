using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UIAutomationClient;

namespace Rememory.Helper
{
    /// <summary>
    /// Helps to get caret position in most installed applications
    /// Uses GUI, Acc and UIA methods
    /// </summary>
    public class TextBoxCaretHelper
    {
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
            Rectangle tempRect = Rectangle.Empty;

            try
            {
                IUIAutomation uiAutomation = new CUIAutomation();
                IUIAutomationElement focusedElement = uiAutomation.GetFocusedElement();

                if (focusedElement is null)
                {
                    return false;
                }

                int UIA_TextPatternId = 10014;
                object textPatternObj = focusedElement.GetCurrentPattern(UIA_TextPatternId);

                if (textPatternObj is IUIAutomationTextPattern textPattern)
                {
                    IUIAutomationTextRangeArray selectionRanges = textPattern.GetSelection();

                    if (selectionRanges is not null && selectionRanges.Length > 0)
                    {
                        IUIAutomationTextRange selectionRange = selectionRanges.GetElement(0);

                        if (selectionRange is null)
                        {
                            return false;
                        }

                        double[] boundingRect = (double[])selectionRange.GetBoundingRectangles();
                        bool isExpanded = false;

                        if (boundingRect.Length == 0)
                        {
                            selectionRange.ExpandToEnclosingUnit(TextUnit.TextUnit_Character);
                            boundingRect = (double[])selectionRange.GetBoundingRectangles();
                            isExpanded = true;
                        }

                        if (boundingRect.Length >= 4)
                        {
                            int left = (int)boundingRect[0];
                            int top = (int)boundingRect[1];
                            int width = (int)boundingRect[2];
                            int height = (int)boundingRect[3];

                            tempRect = new Rectangle(left, top, isExpanded ? 0 : width, height);
                        }
                    }
                }
            }
            catch { }

            if (!tempRect.IsEmpty)
            {
                rect = tempRect;
                return true;
            }

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
