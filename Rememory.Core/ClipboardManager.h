#pragma once
#include "ClipboardDataHelper.h"

typedef bool(__stdcall* Callback)(RtfPreviewInfo& dataInfo);

extern "C" __declspec(dllexport) bool StartClipboardMonitor(HWND hWnd, Callback handler);
extern "C" __declspec(dllexport) bool StopClipboardMonitor(HWND hWnd);
extern "C" __declspec(dllexport) bool SetDataToClipboard(RtfPreviewInfo& dataInfo);

class ClipboardManager
{
public:
    static ClipboardManager& GetInstance();
    bool StartMonitoring(HWND hWnd, Callback handler);
    bool StopMonitoring();
    bool SetDataToClipboard(RtfPreviewInfo& dataInfo);
    void ClipboardManagerMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

private:
    HWND _hWnd;
    Callback _handler;
    DWORD _oldClipboardSequenceNumber;
    bool _isMyChanges;
    HWND _nextClipboardViewer;
    WCHAR* _ownerPath;
    UINT_PTR _timerId;

    ClipboardManager() {};
    static void CALLBACK MonitorTimerProc(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime);
    void HandleClipboardData();
    bool TryOpenClipboard();

    // Prevent copying and assignment
    ClipboardManager(const ClipboardManager&) = delete;
    ClipboardManager& operator=(const ClipboardManager&) = delete;
};
