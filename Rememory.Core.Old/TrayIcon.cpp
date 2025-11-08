#include "pch.h"
#include "TrayIcon.h"
#include <shellapi.h>

#pragma region External functions

bool CreateTrayIcon(HWND hWnd, WCHAR* openMenuName, WCHAR* toggleMonitoringMenuName, WCHAR* settingsMenuName, WCHAR* exitMenuName, WCHAR* description)
{
    return TrayIcon::GetInstance().TrayIconInit(hWnd, openMenuName, toggleMonitoringMenuName, settingsMenuName, exitMenuName, description);
}

void UpdateTrayIconMenuItem(UINT commandId, WCHAR* newName)
{
    TrayIcon::GetInstance().UpdateMenuItem(commandId, newName);
}

#pragma endregion

const UINT TRAY_ICON_ID = 1;

TrayIcon& TrayIcon::GetInstance()
{
    static TrayIcon instance;
    return instance;
}

bool TrayIcon::TrayIconInit(HWND hWnd, WCHAR* openMenuName, WCHAR* toggleMonitoringMenuName, WCHAR* settingsMenuName, WCHAR* exitMenuName, WCHAR* description)
{
    SetPreferredTheme(AllowDark);

    hMenu = CreatePopupMenu();

    MENUINFO mi = {};
    mi.cbSize = sizeof(mi);
    mi.fMask = MIM_APPLYTOSUBMENUS | MIM_STYLE;
    mi.dwStyle = MNS_NOCHECK;
    SetMenuInfo(hMenu, &mi);
    
    AppendMenu(hMenu, MF_STRING, TRAY_OPEN_COMMAND, openMenuName);   // Open\tWin+Shift+V
    AppendMenu(hMenu, MF_STRING, TRAY_TOGGLE_MONITORING_COMMAND, toggleMonitoringMenuName);   // Pause/resume monitoring
    AppendMenu(hMenu, MF_STRING, TRAY_SETTINGS_COMMAND, settingsMenuName);   // Settings
    AppendMenu(hMenu, MF_STRING, TRAY_EXIT_COMMAND, exitMenuName);   // Exit

    NOTIFYICONDATA nid = {};
    nid.cbSize = sizeof(NOTIFYICONDATA);
    nid.hWnd = hWnd;
    nid.uID = TRAY_ICON_ID;
    nid.uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
    nid.uCallbackMessage = TRAY_NOTIFICATION;
    nid.hIcon = (HICON)SendMessage(hWnd, WM_GETICON, ICON_SMALL, 0);
    wcscpy_s(nid.szTip, description);   // L"Rememory - Clipboard Manager"

    Shell_NotifyIcon(NIM_ADD, &nid);
    return true;
}

void TrayIcon::UpdateMenuItem(UINT commandId, WCHAR* newName)
{
    if (hMenu) {
        ModifyMenu(hMenu, commandId, MF_STRING, commandId, newName);
    }
}

void TrayIcon::TrayIconMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case TRAY_NOTIFICATION:
    {
        if (LOWORD(lParam) == WM_RBUTTONUP)
        {
            POINT pt;
            GetCursorPos(&pt);
            SetForegroundWindow(hWnd);   // Needed for the context menu to disappear
            TrackPopupMenu(hMenu, TPM_BOTTOMALIGN | TPM_LEFTALIGN, pt.x, pt.y, 0, hWnd, NULL);
        }
        break;
    }
    }
}

void TrayIcon::SetPreferredTheme(PreferredAppMode mode)
{
    HMODULE hUxTheme = LoadLibrary(L"uxtheme.dll");
    if (hUxTheme)
    {
        SetPreferredAppModeFn SetPreferredAppMode = (SetPreferredAppModeFn)GetProcAddress(hUxTheme, (LPCSTR)135);
        if (SetPreferredAppMode)
        {
            int result = SetPreferredAppMode(mode);
        }
        FreeLibrary(hUxTheme);
    }
}
