#pragma once

#define TRAY_NOTIFICATION (WM_USER + 1)
#define TRAY_OPEN_COMMAND 10
#define TRAY_TOGGLE_MONITORING_COMMAND 11
#define TRAY_SETTINGS_COMMAND 12
#define TRAY_EXIT_COMMAND 13

extern "C" __declspec(dllexport) bool CreateTrayIcon(HWND hWnd, WCHAR* openMenuName, WCHAR* toggleMonitoringMenuName, WCHAR* settingsMenuName, WCHAR* exitMenuName, WCHAR* description);
extern "C" __declspec(dllexport) void UpdateTrayIconMenuItem(UINT commandId, WCHAR* newName);

typedef enum {
    Default,
    AllowDark,
    ForceDark,
    ForceLight,
    Max
} PreferredAppMode;

typedef int (WINAPI* SetPreferredAppModeFn)(PreferredAppMode);

class TrayIcon
{
public:
    static TrayIcon& GetInstance();
    bool TrayIconInit(HWND hWnd, WCHAR* openMenuName, WCHAR* toggleMonitoringMenuName, WCHAR* settingsMenuName, WCHAR* exitMenuName, WCHAR* description);
    void UpdateMenuItem(UINT commandId, WCHAR* newName);
    void TrayIconMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
private:
    HMENU hMenu;

    TrayIcon() {};
    void SetPreferredTheme(PreferredAppMode mode);

    // Prevent copying and assignment
    TrayIcon(const TrayIcon&) = delete;
    TrayIcon& operator=(const TrayIcon&) = delete;
};
