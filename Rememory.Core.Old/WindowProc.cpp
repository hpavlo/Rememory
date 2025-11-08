#include "pch.h"
#include "WindowProc.h"
#include "TrayIcon.h"
#include "ClipboardManager.h"

WNDPROC OriginalProc = {};

LRESULT CALLBACK MainWindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_CLIPBOARDUPDATE:
    {
        ClipboardManager::GetInstance().ClipboardManagerMessage(hWnd, uMsg, wParam, lParam);
        break;
    }

    case TRAY_NOTIFICATION:
    {
        TrayIcon::GetInstance().TrayIconMessage(hWnd, uMsg, wParam, lParam);
        return CallWindowProc(OriginalProc, hWnd, uMsg, wParam, lParam);
    }

    case WM_DESTROY:
    {
        ClipboardManager::GetInstance().StopMonitoring();
    }
    default:
        return CallWindowProc(OriginalProc, hWnd, uMsg, wParam, lParam);
    }
    return 0;
}

#pragma region External functions

bool AddWindowProc(HWND hWnd)
{
    return OriginalProc = (WNDPROC)SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)MainWindowProc);
}

#pragma endregion
